using System;
using System.ComponentModel;
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
    //индексы
    //create index are.ixtrf_6 on tarif (nzp_kvar,nzp_serv,dat_s,dat_po);
    //update statistics for table tarif;

    //----------------------------------------------------------------------
    public class DbEditInterData : DataBaseHead
    //----------------------------------------------------------------------
    {
        //----------------------------------------------------------------------
        public int GetSeriesProc(string pref, int kod, out Returns ret)
        //----------------------------------------------------------------------
        {
            int cur_val = Constants._ZERO_;
            string cur_txt = "Значение не инициализировано";
            ret = Utils.InitReturns();

            string conn_kernel = Points.GetConnByPref(pref);
            IDbConnection conn_db = GetConnection(conn_kernel);

            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                return cur_val;
            }

            if (ProcedureInWebCashe(conn_db, "get_series", pref + "_data"))
            {
                IDataReader reader;

#if PG
                string sql = "select " + pref + "_data.get_series (" + kod + ")";
#else
                string sql = "execute procedure " + pref + "_data:get_series (" + kod + ")";
#endif
                if (!ExecRead(conn_db, out reader, sql, true).result)
                {
                    conn_db.Close();

                    MonitorLog.WriteLog("Ошибка выборки " + sql, MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    ret.text = "Ошибка sql";
                    ret.tag = cur_val;

                    return cur_val;
                }
                if (reader.Read())
                {
                    if (reader["cur_val"] != DBNull.Value) cur_val = Convert.ToInt32(reader["cur_val"]);
                    if (reader["ret_message"] != DBNull.Value) cur_txt = Convert.ToString(reader["ret_message"]);
                }
            }

            if (cur_val < 0)
            {
                ret.result = false;
                ret.text = cur_txt;
                ret.tag = cur_val;
            }

            conn_db.Close();
            return cur_val;
        }

        //----------------------------------------------------------------------
        public int GetSupgSeriesProc(int kod, out Returns ret)
        //----------------------------------------------------------------------
        {
            int cur_val = Constants._ZERO_;
            string cur_txt = "Значение не инициализировано";
            ret = Utils.InitReturns();

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);

            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                return cur_val;
            }

            if (ProcedureInWebCashe(conn_db, "get_series", Points.Pref + "_supg"))
            {
                IDataReader reader;

#if PG
                string sql = "select * FROM " + Points.Pref + "_supg.get_series (" + kod + ")";
#else
                string sql = "execute procedure " + Points.Pref + "_supg:get_series (" + kod + ")";
#endif
                if (!ExecRead(conn_db, out reader, sql, true).result)
                {
                    conn_db.Close();

                    MonitorLog.WriteLog("Ошибка выборки " + sql, MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    ret.text = "Ошибка sql";
                    ret.tag = cur_val;

                    return cur_val;
                }
                if (reader.Read())
                {
                    if (reader["cur_val"] != DBNull.Value) cur_val = Convert.ToInt32(reader["cur_val"]);
                    if (reader["ret_message"] != DBNull.Value) cur_txt = Convert.ToString(reader["ret_message"]);
                }
            }

            if (cur_val < 0)
            {
                ret.result = false;
                ret.text = cur_txt;
                ret.tag = cur_val;
            }

            conn_db.Close();
            return cur_val;
        }

        //объединение пересекающихся интервалов во временной таблице
        //----------------------------------------------------------------------
        void UnionTempTable(IDbConnection connection, IDbTransaction transaction, string t_selected, int user, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            string sel = "";

            string t_union = "t_union";
            ExecSQL(connection, transaction, " Drop table " + t_union, false);

#if PG
            sel = " Select a.nzp_kvar, a.nzp_serv, min(a.dat_s) as dat_s, max(a.dat_po) as dat_po " + " From " + t_selected + " a, " + t_selected
                  + " b " + " Where a.nzp_kvar = b.nzp_kvar " + " and a.nzp_serv = b.nzp_serv " + " and a.dat_s  <= b.dat_po "
                  + " and a.dat_po >= b.dat_s  " + " and a.oid <> b.oid   " + " Group by 1,2 ";
#else
            sel = " Select a.nzp_kvar, a.nzp_serv, min(a.dat_s) as dat_s, max(a.dat_po) as dat_po " + " From " + t_selected + " a, " + t_selected
                  + " b " + " Where a.nzp_kvar = b.nzp_kvar " + " and a.nzp_serv = b.nzp_serv " + " and a.dat_s  <= b.dat_po "
                  + " and a.dat_po >= b.dat_s  " + " and a.rowid <> b.rowid   " + " Group by 1,2 ";
#endif
#if PG
            sel = sel.AddIntoStatement(" Into temp " + t_union);
#else
            sel += " Into temp " + t_union + " With no log ";
#endif

            ret = ExecSQL(connection, transaction, sel, true);
            if (!ret.result)
            {
                ret.text = "Ошибка выборки данных из целевой таблицы ";
                return;
            }

            //создать случайный индекс 
            sel = "Create index uni_" + RandomText.Generate() + "_" + Math.Abs(user) + "1 on " + t_union + "(nzp_kvar,nzp_serv,dat_s,dat_po)";
            ret = ExecSQL(connection, transaction, sel, true);
            if (!ret.result)
            {
                ret.text = "Ошибка создания индексов";

                ExecSQL(connection, transaction, " Drop table " + t_selected, false);
                ExecSQL(connection, transaction, " Drop table " + t_union, false);
                return;
            }
#if PG
            ExecSQL(connection, transaction, " analyze  " + t_union, true);
#else
            ExecSQL(connection, transaction, " Update statistics for table  " + t_union, true);
#endif

            //удалить пересекающиеся интервалы
            sel = " Update " + t_selected +
                  " Set kod = -1 " +
                  " Where Exists ( " +
                            " Select 1 From " + t_union + " u " +
                            " Where u.nzp_kvar = " + t_selected + ".nzp_kvar " +
                            "   and u.nzp_serv = " + t_selected + ".nzp_serv " +
                            "   and u.dat_s  <= " + t_selected + ".dat_po " +
                            "   and u.dat_po >= " + t_selected + ".dat_s " +
                            " ) ";

            ret = ExecSQL(connection, transaction, sel, true);
            if (!ret.result)
            {
                ret.text = "Ошибка поиска дублирования";

                ExecSQL(connection, transaction, " Drop table " + t_selected, false);
                ExecSQL(connection, transaction, " Drop table " + t_union, false);
                return;
            }

            //вставка объединенных интервалов
            sel = " Insert into " + t_selected + " (nzp_kvar,nzp_serv,dat_s,dat_po,work_s,work_po,kod)  " +
                  " Select nzp_kvar,nzp_serv,dat_s,dat_po,dat_s,dat_po,0 " +
                  " From " + t_union;
            ret = ExecSQL(connection, transaction, sel, true);
            if (!ret.result)
            {
                ret.text = "Ошибка поиска дублирования";

                ExecSQL(connection, transaction, " Drop table " + t_selected, false);
                ExecSQL(connection, transaction, " Drop table " + t_union, false);
                return;
            }

            //удалить t_union - он больше не нужен
            ExecSQL(connection, transaction, " Drop table " + t_union, false);
        }

        //----------------------------------------------------------------------
        //Выставление must_calc:
        //----------------------------------------------------------------------
        [Obsolete("Функция устарела. Не использовать")]
        public void MustCalc(IDbConnection connection, IDbTransaction transaction, EditInterDataMustCalc editData, out Returns ret)
        {
            PreEdit(connection, transaction, editData, out ret);
            if (!ret.result)
            {
                return;
            }

            //построим индекс на must_calc по 
            // тормоза дл Челнов!
            //ExecSQL(conn_db, " update statistics for table must_calc ", true);

            //Убрал Андрей К. 14.01.2014
            //используется для выбора по полю month_calc, но при выставлении признаков перерасчета всегда используется текущий расчетный месяц
            //так что передача этих параметров извне не нужна
            //string s_s = " cast (" + editData.dat_s + " as date) ";
            //string s_po = " cast (" + editData.dat_po + " as date) ";

            DateTime d1 = new DateTime(editData.year, editData.month, 1);
            DateTime d2 = new DateTime(editData.year, editData.month, 1).AddMonths(1).AddDays(-1);
            string s_s = MDY(d1.Month, d1.Day, d1.Year);
            string s_po = MDY(d2.Month, d2.Day, d2.Year);

            string t_selected = "t_mc_selected";
            ExecSQL(connection, transaction, " Drop table " + t_selected, false);

            string sel = "";
            string sql = "";

            //условие выборки данных из целевой таблицы
            foreach (string s in editData.dopFind)
            {
                if (s.Trim() != "") sql += s;
            }

            //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            //выбрать измененые данные в текущем месяце
            //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            //nedop_kvar, counters_xx, tarif
            //prm_xx
            //pere_gilec
            MustCalcReasons kod1 = MustCalcReasons.Undefined;
            int kod2 = editData.kod2;
#if PG
            string MDY_s = "public.mdy(" + Points.BeginCalc.month_ + ",1," + Points.BeginCalc.year_ + ")"; //public.mdy(1,1,1901)
            string selectDates = " ,case when p.dat_s  is null then " + MDY_s + " else public.mdy(" + "p.dat_s".Month().CastTo("integer") + ",1," + "p.dat_s".Year().CastTo("integer") + ") end as dat_s, " +
                //" case when p.dat_po is null then public.mdy(1,1,3000) " +
                " case when coalesce(p.dat_po,public.mdy(1,1,3000)) >= public.mdy(" + editData.month + ",1," + editData.year + ") " +
                " then public.mdy(" + editData.month + ",1," + editData.year + ") - INTERVAL '1 days' " +
                " else public.mdy(" + "p.dat_po".Month().CastTo("integer") + ",1," + "p.dat_po".Year().CastTo("integer") + ") + interval '1 months' - interval '1 days' end as dat_po, " + //последний день месяца
                " 0 as kod, " +
                " case when p.dat_s  is null then " + MDY_s + " else public.mdy(" + "p.dat_s".Month().CastTo("integer") + ",1," + "p.dat_s".Year().CastTo("integer") + ") end as work_s, " +
                //" case when p.dat_po is null then public.mdy(1,1,3000) " +
                " case when coalesce(p.dat_po,public.mdy(1,1,3000)) >= public.mdy(" + editData.month + ",1," + editData.year + ") " +
                " then public.mdy(" + editData.month + ",1," + editData.year + ") - interval '1 days' " +
                " else public.mdy( " + "p.dat_po".Month().CastTo("integer") + ",1," + "p.dat_po".Year().CastTo("integer") + " ) + interval '1 months' - interval '1 days' end as work_po "; //последний день месяца

            string sql1 = "create temp table " + t_selected + " ( nzp_kvar integer, nzp_serv integer, dat_s date, dat_po date,kod integer, work_s date, work_po date) with oids";
            ret = ExecSQL(connection, transaction, sql1, false);
#else
            string MDY_s = "MDY(" + Points.BeginCalc.month_ + ",1," + Points.BeginCalc.year_ + ")"; //mdy(1,1,1901)

            string selectDates = " ,case when p.dat_s  is null then " + MDY_s + " else mdy( month(p.dat_s),1,year(p.dat_s) ) end as dat_s, " +

                //" case when p.dat_po is null then mdy(1,1,3000) " +
                " case when nvl(p.dat_po,mdy(1,1,3000)) >= MDY(" + editData.month + ",1," + editData.year + ") " +
                " then date(MDY(" + editData.month + ",1," + editData.year + ") - 1 units day) " +
                " else date( mdy( month(p.dat_po),1,year(p.dat_po) ) + 1 units month - 1 units day ) end as dat_po, " + //последний день месяца

                " 0 as kod, " +
                " case when p.dat_s  is null then " + MDY_s + " else mdy( month(p.dat_s),1,year(p.dat_s) ) end as work_s, " +

                //" case when p.dat_po is null then mdy(1,1,3000) " +
                " case when nvl(p.dat_po,mdy(1,1,3000)) >= MDY(" + editData.month + ",1," + editData.year + ") " +
                " then date(MDY(" + editData.month + ",1," + editData.year + ") - 1 units day) " +
                " else date( mdy( month(p.dat_po),1,year(p.dat_po) ) + 1 units month - 1 units day ) end as work_po "; //последний день месяца
#endif

            switch (editData.mcalcType)
            {
                //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                // прямая выборка
                //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                case enMustCalcType.mcalc_Serv:
                case enMustCalcType.Counter:
                case enMustCalcType.Nedop:

                    if (editData.mcalcType == enMustCalcType.Counter) kod1 = MustCalcReasons.Counter;
                    else if (editData.mcalcType == enMustCalcType.Nedop) kod1 = MustCalcReasons.Nedop;
                    else kod1 = MustCalcReasons.Service;
#if PG
                    sel = "insert into " + t_selected +
                        " Select distinct nzp_kvar, nzp_serv " + selectDates +
                        " From " + editData.database + editData.table + " p " +
                        " Where month_calc <= " + s_po + " and month_calc >= " + s_s
                            + sql;

#else
                    sel = " Select unique nzp_kvar, nzp_serv " + selectDates +
                        " From " + editData.database + ":" + editData.table + " p " +
                        " Where month_calc <= " + s_po + " and month_calc >= " + s_s
                            + sql +
                        " Into temp " + t_selected + " With no log ";
#endif
                    break;


                //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                // выборка из prm_1,3 - связка prm_1, prm_frm,
                //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                case enMustCalcType.mcalc_Prm1:

                    kod1 = MustCalcReasons.Parameter;
#if PG
                    sel = "insert into " + t_selected +
                        " Select distinct t.nzp_kvar, t.nzp_serv " + selectDates +
                        " From " + editData.database + "." + editData.table + " p, " + editData.pref + "_data.tarif t, " + editData.pref + "_kernel.prm_frm f " +
                        " Where p.nzp = t.nzp_kvar " +
                          " and t.is_actual <> 100 " +
                          " and p.nzp_prm = f.nzp_prm " +
                          " and p.nzp_prm not in (5,10,51,130,90) " + //кроме итоговых параметров (nzp_serv = 1)
                          " and f.nzp_frm = t.nzp_frm and f.is_prm = 1" +
                          " and p.month_calc <= " + s_po + " and p.month_calc >= " + s_s
                            + sql +
                        " Union " +
                        " Select distinct p.nzp as nzp_kvar, 1 as nzp_serv " + selectDates +
                        " From " + editData.database + "." + editData.table + " p " +
                        " Where p.nzp_prm in (5,10,51,130,90) " + //итоговые параметры (nzp_serv = 1)
                          " and p.month_calc <= " + s_po + " and p.month_calc >= " + s_s
                            + sql;
#else
                    sel = " Select unique t.nzp_kvar, t.nzp_serv " + selectDates +
                        " From " + editData.database + ":" + editData.table + " p, " + editData.pref + "_data:tarif t, " + editData.pref + "_kernel:prm_frm f " +
                        " Where p.nzp = t.nzp_kvar " +
                          " and t.is_actual <> 100 " +
                          " and p.nzp_prm = f.nzp_prm " +
                          " and p.nzp_prm not in (5,10,51,130,90) " + //кроме итоговых параметров (nzp_serv = 1)
                          " and f.nzp_frm = t.nzp_frm and f.is_prm = 1" +
                          " and p.month_calc <= " + s_po + " and p.month_calc >= " + s_s
                            + sql +

                        " Union " +

                        " Select unique p.nzp as nzp_kvar, 1 as nzp_serv " + selectDates +
                        " From " + editData.database + ":" + editData.table + " p " +
                        " Where p.nzp_prm in (5,10,51,130,90) " + //итоговые параметры (nzp_serv = 1)
                          " and p.month_calc <= " + s_po + " and p.month_calc >= " + s_s
                            + sql +
                        " Into temp " + t_selected + " With no log ";
#endif
                    break;


                //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                // выборка из prm_2,4 - связка prm_2, prm_frm, kvar (по дому)
                //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                case enMustCalcType.mcalc_Prm2:

                    ret = ExecSQL(connection, transaction, "drop table temp_pp", false);

#if PG
                    sel = " Select * into temp temp_pp From " + editData.database + "." + editData.table + " p " +
                          " Where p.month_calc <= " + s_po + " and p.month_calc >= " + s_s;
#else
                    sel = " Select * From " + editData.database + ":" + editData.table + " p " +
                          " Where p.month_calc <= " + s_po + " and p.month_calc >= " + s_s +
                          " Into temp temp_pp With no log ";
#endif
                    ret = ExecSQL(connection, transaction, sel, true);
                    if (!ret.result)
                    {
                        ret.text = "Ошибка создания индексов";
                        ExecSQL(connection, transaction, " Drop table " + t_selected, false);
                        return;
                    }

                    sel = "Create index uni_temp_pp1 on temp_pp (nzp,month_calc, nzp_prm)";
                    ret = ExecSQL(connection, transaction, sel, true);
                    if (!ret.result)
                    {
                        ret.text = "Ошибка создания индексов";

                        ExecSQL(connection, transaction, " Drop table temp_pp ", false);
                        return;
                    }
#if PG
                    ExecSQL(connection, transaction, " analyze temp_pp ", true);
#else
                    ExecSQL(connection, transaction, " Update statistics for table temp_pp ", true);
#endif
                    kod1 = MustCalcReasons.Parameter;
#if PG
                    sel = "insert into " + t_selected +
                        " Select distinct t.nzp_kvar, t.nzp_serv " + selectDates +

                        " From temp_pp p, " + editData.pref + "_data.kvar k, " + editData.pref + "_data.tarif t, " + editData.pref + "_kernel.prm_frm f " +
                        " Where p.nzp = k.nzp_dom " +
                          " and k.nzp_kvar = t.nzp_kvar " +
                          " and t.is_actual <> 100 " +
                          " and p.nzp_prm = f.nzp_prm " +
                          " and f.nzp_frm = t.nzp_frm and f.is_prm = 1" +
                            sql;
#else
                    sel = " Select unique t.nzp_kvar, t.nzp_serv " + selectDates +
                        " From temp_pp p, " + editData.pref + "_data:kvar k, " + editData.pref + "_data:tarif t, " + editData.pref + "_kernel:prm_frm f " +
                        " Where p.nzp = k.nzp_dom " +
                          " and k.nzp_kvar = t.nzp_kvar " +
                          " and t.is_actual <> 100 " +
                          " and p.nzp_prm = f.nzp_prm " +
                        //" and p.nzp_prm not in (5,10,51,130,90) " + //кроме итоговых параметров (nzp_serv = 1)
                          " and f.nzp_frm = t.nzp_frm and f.is_prm = 1" +
                        //" and p.month_calc <= " + s_po + " and p.month_calc >= " + s_s +
                            sql +
                        //Анэс сказал, что НЕТ домовых параметров, которые влияют на ИТОГО
                        //" Union " +
                        //" Select unique k.nzp as nzp_kvar, 1 as nzp_serv, " +
                        //       " mdy( month(p.dat_s),1,year(p.dat_s) ) as dat_s, " +
                        //       " date( mdy( month(p.dat_po),1,year(p.dat_po) ) + 1 units month - 1 units day ) as dat_po, " + //последний день месяца
                        //       " 0 as kod, " +
                        //       " mdy( month(p.dat_s),1,year(p.dat_s) ) as work_s, " +
                        //       " date( mdy( month(p.dat_po),1,year(p.dat_po) ) + 1 units month - 1 units day ) as work_po " +
                        //" From " + editData.table + " p, kvar k " +
                        //" Where p.nzp = k.nzp_dom " +
                        //  //" and p.nzp_prm in (5,10,51,130,90) " + //итоговые параметры (nzp_serv = 1)
                        //  " and p.month_calc <= " + s_po + " and p.month_calc >= " + s_s
                        //    + sql +
                        " Into temp " + t_selected + " With no log ";
#endif
                    break;

                    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                    // выборка из pere_gilec
                    //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                    break;

                case enMustCalcType.DomCounter:

                    kod1 = MustCalcReasons.Gil;
#if PG
                    sel = "insert into " + t_selected +
                        " Select distinct p.nzp_kvar, t.nzp_serv " + selectDates +
                        " From " + editData.database + "." + editData.table + " p " +
                            ", " + editData.pref + "_data.tarif t " +
                        " Where t.nzp_kvar = p.nzp_kvar " +
                            sql;
#else
                    sel = " Select unique p.nzp_kvar, t.nzp_serv " + selectDates +
                        " From " + editData.database + ":" + editData.table + " p " +
                            ", " + editData.pref + "_data:tarif t " +
                        " Where  t.nzp_kvar = p.nzp_kvar  " +
                            sql +
                        " Into temp " + t_selected + " With no log ";
#endif
                    break;

                case enMustCalcType.GroupCounter:

                    kod1 = MustCalcReasons.Counter;
#if PG
                    sel = "insert into " + t_selected +
                        " Select distinct k.nzp_kvar, p.nzp_serv " + selectDates +
                        " From " + editData.database + "." + editData.table + " p" +
                            ", " + editData.pref + "_data.counters_link k" +
                        " Where month_calc <= " + s_po + " and month_calc >= " + s_s +
                            " and k.nzp_counter = p.nzp_counter " +
                            sql;
#else
                    sel = " Select unique k.nzp_kvar, p.nzp_serv " + selectDates +
                        " From " + editData.database + ":" + editData.table + " p" +
                            ", " + editData.pref + "_data:counters_link k" +
                        " Where month_calc <= " + s_po + " and month_calc >= " + s_s +
                            " and k.nzp_counter = p.nzp_counter " +
                            sql +
                        " Into temp " + t_selected + " With no log ";
#endif
                    break;

                case enMustCalcType.Prm17:

                    kod1 = MustCalcReasons.Parameter;

#if PG
                    ret = ExecSQL(connection, transaction, "drop table if exists temp_pp", false);
#else
                    ret = ExecSQL(connection, transaction, "drop table temp_pp", false);
#endif
#if PG
                    sel = " Select distinct nzp_counter, nzp_type, nzp " +
                        " Into temp temp_pp " +
                        " From " + editData.database + "." + editData.table + " p, " + editData.pref + "_data.counters_spis c " +
                        " Where p.nzp = c.nzp_counter " + sql;
#else
                    sel = " Select unique nzp_counter, nzp_type, nzp " +
                        " From " + editData.database + ":" + editData.table + " p, " + editData.pref + "_data:counters_spis c " +
                        " Where p.nzp = c.nzp_counter " + sql +
                        " Into temp temp_pp With no log";
#endif
                    ret = ExecSQL(connection, transaction, sel, true);
                    if (!ret.result)
                    {
                        return;
                    }

                    sel = "Create index uni_temp_pp2 on temp_pp (nzp_counter, nzp_type, nzp)";
                    ret = ExecSQL(connection, transaction, sel, true);
                    if (!ret.result)
                    {
#if PG
                        ExecSQL(connection, transaction, " Drop table if exists temp_pp ", false);
#else
                        ExecSQL(connection, transaction, " Drop table temp_pp ", false);
#endif
                        return;
                    }

#if PG
                    ExecSQL(connection, transaction, " analyze temp_pp ", true);
#else
                    ExecSQL(connection, transaction, " Update statistics for table temp_pp ", true);
#endif

#if PG
                    sel = "insert into " + t_selected +
                        " Select distinct t.nzp_kvar, t.nzp_serv " + selectDates +

                        " From temp_pp c, " + editData.database + "." + editData.table + " p " + ", " + editData.pref + "_data.tarif t, " + editData.pref + "_kernel.prm_frm f " +
                        " Where c.nzp_counter = p.nzp and c.nzp_type = " + (int)CounterKinds.Kvar +
                          " and c.nzp = t.nzp_kvar " +
                          " and t.is_actual <> 100 " +
                          " and p.nzp_prm = f.nzp_prm " +
                          " and f.nzp_frm = t.nzp_frm and f.is_prm = 1" +
                          " and p.month_calc <= " + s_po + " and p.month_calc >= " + s_s
                            + sql +
                        " Union " +
                        " Select distinct t.nzp_kvar, t.nzp_serv " + selectDates +
                        " From temp_pp c, " + editData.database + "." + editData.table + " p " +
                        ", " + editData.pref + "_data.kvar k" +
                        ", " + editData.pref + "_data.tarif t, " + editData.pref + "_kernel.prm_frm f " +
                        " Where c.nzp_counter = p.nzp and c.nzp_type = " + (int)CounterKinds.Dom +
                          " and c.nzp = k.nzp_dom " +
                          " and k.nzp_kvar = t.nzp_kvar " +
                          " and t.is_actual <> 100 " +
                          " and p.nzp_prm = f.nzp_prm " +
                          " and f.nzp_frm = t.nzp_frm and f.is_prm = 1" +
                          " and p.month_calc <= " + s_po + " and p.month_calc >= " + s_s
                            + sql +
                        " Union " +
                        " Select distinct t.nzp_kvar, t.nzp_serv " + selectDates +
                        " From temp_pp c, " + editData.database + "." + editData.table + " p " +
                        ", " + editData.pref + "_data.counters_link k " +
                        ", " + editData.pref + "_data.tarif t, " + editData.pref + "_kernel.prm_frm f " +
                        " Where c.nzp_counter = p.nzp and c.nzp_type in (" + (int)CounterKinds.Group + "," + (int)CounterKinds.Communal + ") " +
                          " and c.nzp_counter = k.nzp_counter " +
                          " and k.nzp_kvar = t.nzp_kvar " +
                          " and t.is_actual <> 100 " +
                          " and p.nzp_prm = f.nzp_prm " +
                          " and f.nzp_frm = t.nzp_frm and f.is_prm = 1" +
                          " and p.month_calc <= " + s_po + " and p.month_calc >= " + s_s
                            + sql;
#else
                    sel = " Select unique t.nzp_kvar, t.nzp_serv " + selectDates +
                        " From temp_pp c, " + editData.database + ":" + editData.table + " p " + ", " + editData.pref + "_data:tarif t, " + editData.pref + "_kernel:prm_frm f " +
                        " Where c.nzp_counter = p.nzp and c.nzp_type = " + (int)CounterKinds.Kvar +
                          " and c.nzp = t.nzp_kvar " +
                          " and t.is_actual <> 100 " +
                          " and p.nzp_prm = f.nzp_prm " +
                          " and f.nzp_frm = t.nzp_frm and f.is_prm = 1" +
                          " and p.month_calc <= " + s_po + " and p.month_calc >= " + s_s
                            + sql +

                        " Union " +

                        " Select unique t.nzp_kvar, t.nzp_serv " + selectDates +
                        " From temp_pp c, " + editData.database + ":" + editData.table + " p " +
                        ", " + editData.pref + "_data:kvar k" +
                        ", " + editData.pref + "_data:tarif t, " + editData.pref + "_kernel:prm_frm f " +
                        " Where c.nzp_counter = p.nzp and c.nzp_type = " + (int)CounterKinds.Dom +
                          " and c.nzp = k.nzp_dom " +
                          " and k.nzp_kvar = t.nzp_kvar " +
                          " and t.is_actual <> 100 " +
                          " and p.nzp_prm = f.nzp_prm " +
                          " and f.nzp_frm = t.nzp_frm and f.is_prm = 1" +
                          " and p.month_calc <= " + s_po + " and p.month_calc >= " + s_s
                            + sql +

                        " Union " +

                        " Select unique t.nzp_kvar, t.nzp_serv " + selectDates +
                        " From temp_pp c, " + editData.database + ":" + editData.table + " p " +
                        ", " + editData.pref + "_data:counters_link k " +
                        ", " + editData.pref + "_data:tarif t, " + editData.pref + "_kernel:prm_frm f " +
                        " Where c.nzp_counter = p.nzp and c.nzp_type in (" + (int)CounterKinds.Group + "," + (int)CounterKinds.Communal + ") " +
                          " and c.nzp_counter = k.nzp_counter " +
                          " and k.nzp_kvar = t.nzp_kvar " +
                          " and t.is_actual <> 100 " +
                          " and p.nzp_prm = f.nzp_prm " +
                          " and f.nzp_frm = t.nzp_frm and f.is_prm = 1" +
                          " and p.month_calc <= " + s_po + " and p.month_calc >= " + s_s
                            + sql +

                        " Into temp " + t_selected + " With no log ";
#endif
                    break;
            }
            ret = ExecSQL(connection, transaction, sel, true);
            if (!ret.result)
            {
                ret.text = "Ошибка выборки данных из целевой таблицы ";
                return;
            }
            ExecSQL(connection, transaction, "drop table temp_pp", false);

            if (editData.mcalcType == enMustCalcType.Counter)
            {
                #region Установка перерасчета для связанных услуг
                //---------------------------------------------------------------------------------------------------------------------------------------------------
                // сохранить данные из t_selected во временную таблицу 
                string tMCDepServ = "t_mc_dep_serv";

                ExecSQL(connection, transaction, "Drop table " + tMCDepServ, false);

                sel = "select t.*, k.nzp_area " + (tableDelimiter == "." ? " into temp " + tMCDepServ : "") +
                    " from " + t_selected + " t, " + Points.Pref + "_data" + tableDelimiter + "kvar k " +
                    " where t.nzp_kvar = k.nzp_kvar " +
                    (tableDelimiter == ":" ? " into temp " + tMCDepServ + " with no log" : "");

                ret = ExecSQL(connection, transaction, sel, true);
                if (!ret.result) throw new Exception(ret.text);

                MyDataReader rdr = null;
                sel = "SELECT distinct nzp_serv, nzp_area FROM " + tMCDepServ;
                ret = ExecRead(connection, transaction, out rdr, sel, true);
                if (!ret.result) throw new Exception("Ошибка выборки данных лицевых счетов и услуг");

                int nzp_serv = 0;
                int nzp_area = 0;
                List<int> lstServices = new List<int>();

                try
                {
                    while (rdr.Read())
                    {
                        if (Convert.ToString(rdr["nzp_serv"]).Trim() != "") nzp_serv = Convert.ToInt32(rdr["nzp_serv"]);
                        if (Convert.ToString(rdr["nzp_area"]).Trim() != "") nzp_area = Convert.ToInt32(rdr["nzp_area"]);

                        DbServKernel srv = new DbServKernel();
                        lstServices = srv.GetDependenciesServicesList(connection, transaction, nzp_serv, nzp_area, out ret);
                        if (!ret.result) throw new Exception("Ошибка определения связанных услуг " + ret.text);

                        if (lstServices.Count <= 0) continue;

                        for (int i = 0; i < lstServices.Count; i++)
                        {
                            sel = "INSERT INTO " + t_selected +
                                  " (nzp_serv, nzp_kvar, dat_s, dat_po, kod, work_s, work_po) " +
                                  " select " + lstServices[i] + ", nzp_kvar, dat_s, dat_po, kod, work_s, work_po from " +
                                  tMCDepServ +
                                  " where nzp_area = " + nzp_area + " and nzp_serv = " + nzp_serv;
                            ret = ExecSQL(connection, transaction, sel, true);
                            if (!ret.result) throw new Exception(ret.text);
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message, ex);
                }
                finally
                {
                    rdr.Close();
                }
                rdr.Close();

                ExecSQL(connection, transaction, "Drop table " + tMCDepServ, false);
                //---------------------------------------------------------------------------------------------------------------------------------------------------
                #endregion
            }

            //создать случайный индекс 
            sel = "Create index sel_" + RandomText.Generate() + "_" + editData.local_user + "_1 on " + t_selected + "(nzp_kvar,nzp_serv,dat_s,dat_po,kod)";
            ret = ExecSQL(connection, transaction, sel, true);
            if (!ret.result)
            {
                ret.text = "Ошибка создания индексов";

                ExecSQL(connection, transaction, " Drop table " + t_selected, false);
                return;
            }
            sel = "Create index sel_" + RandomText.Generate() + "_" + editData.local_user + "_2 on " + t_selected + "(dat_s,dat_po)";
            ret = ExecSQL(connection, transaction, sel, true);
            if (!ret.result)
            {
                ret.text = "Ошибка создания индексов";

                ExecSQL(connection, transaction, " Drop table " + t_selected, false);
                return;
            }
            sel = "Create index sel_" + RandomText.Generate() + "_" + editData.local_user + "_3 on " + t_selected + "(kod)";
            ret = ExecSQL(connection, transaction, sel, true);
            if (!ret.result)
            {
                ret.text = "Ошибка создания индексов";

                ExecSQL(connection, transaction, " Drop table " + t_selected, false);
                return;
            }
#if PG
            ExecSQL(connection, transaction, " analyze  " + t_selected, true);
#else
            ExecSQL(connection, transaction, " Update statistics for table  " + t_selected, true);
#endif


            //отсечем перерсчеты будущего
            /*
            sel = " Update " + t_selected +
                  " Set dat_po = MDY(" + Points.CalcMonth.month_ + ",1," + Points.CalcMonth.year_ + ") - 1 units day " +
                   " Where dat_po > MDY(" + Points.CalcMonth.month_ + ",1," + Points.CalcMonth.year_ + ") ";
            ret = ExecSQL(conn_db, sel, true);
            if (!ret.result)
            {
                ret.text = "Ошибка усечения интервала";

                ExecSQL(conn_db, " Drop table " + t_selected, false);
                return;
            }
            */

            sel = " Delete From " + t_selected +
                  " Where dat_s > dat_po  ";
            ret = ExecSQL(connection, transaction, sel, true);
            if (!ret.result)
            {
                ret.text = "Ошибка усечения интервала";

                ExecSQL(connection, transaction, " Drop table " + t_selected, false);
                return;
            }
#if PG
            ExecSQL(connection, transaction, " analyze  " + t_selected, true);
#else
            ExecSQL(connection, transaction, " Update statistics for table  " + t_selected, true);
#endif

            //для быстроты сначала выберим все из must_calc
            string t_must = "t_must";
            ExecSQL(connection, transaction, " Drop table " + t_must, false);

#if PG
            string str = "Create temp table " + t_must + " (nzp_kvar integer, nzp_serv integer, dat_s date, dat_po date, kod integer, work_s date, work_po date) with oids";
            ret = ExecSQL(connection, transaction, str, true);

            sel = "insert into " + t_must +
                " Select distinct m.nzp_kvar, m.nzp_serv, m.dat_s, m.dat_po, 0 kod, current_date as work_s, current_date as work_po " +

                " From " + editData.database + ".must_calc m, " + t_selected + " t " +
                " Where t.nzp_kvar = m.nzp_kvar " +
                  " and t.nzp_serv = m.nzp_serv " +
                  " and m.year_  = " + editData.year +
                  " and m.month_ = " + editData.month;
#else
            sel =
                " Select unique m.nzp_kvar, m.nzp_serv, m.dat_s, m.dat_po, 0 kod, today as work_s, today as work_po " +
                " From " + editData.database + ":must_calc m, " + t_selected + " t " +
                " Where t.nzp_kvar = m.nzp_kvar " +
                  " and t.nzp_serv = m.nzp_serv " +
                  " and m.year_  = " + editData.year +
                  " and m.month_ = " + editData.month +
                " Into temp " + t_must + " With no log ";
#endif
            ret = ExecSQL(connection, transaction, sel, true);
            if (!ret.result)
            {
                ret.text = "Ошибка выборки данных ";

                ExecSQL(connection, transaction, " Drop table " + t_selected, false);
                return;
            }
            //создать случайный индекс 
            sel = "Create index msel_" + RandomText.Generate() + "_" + Math.Abs(editData.local_user) + "_1 on " + t_must + "(nzp_kvar,nzp_serv,dat_s,dat_po,kod)";
            ret = ExecSQL(connection, transaction, sel, true);
            if (!ret.result)
            {
                ret.text = "Ошибка создания индексов";

                ExecSQL(connection, transaction, " Drop table " + t_selected, false);
                ExecSQL(connection, transaction, " Drop table " + t_must, false);
                return;
            }
            sel = "Create index msel_" + RandomText.Generate() + "_" + Math.Abs(editData.local_user) + "_2 on " + t_must + "(kod)";
            ret = ExecSQL(connection, transaction, sel, true);
            if (!ret.result)
            {
                ret.text = "Ошибка создания индексов";

                ExecSQL(connection, transaction, " Drop table " + t_must, false);
                ExecSQL(connection, transaction, " Drop table " + t_selected, false);
                return;
            }
#if PG
            ExecSQL(connection, transaction, " analyze  " + t_must, true);
#else
            ExecSQL(connection, transaction, " Update statistics for table  " + t_must, true);
#endif
#if PG
            // Используем для postgres новый алгоритм, так как старый работает неправильно. 
            // Для Informix оставим старый, так как для него необходима доработка запроса с учётом его возможностей
            ret = ExecSQL(connection, transaction, DbMustCalcPicking.GetIntervalsSubstractQuery(t_selected, t_must), true);
            if (!ret.result)
            {
                ret.text = "Ошибка вычитания существующих интервалов";

                ExecSQL(connection, transaction, " Drop table " + t_selected, false);
                ExecSQL(connection, transaction, " Drop table " + t_must, false);
                return;
            }

            // Убиваем исходные интервалы, все нужные с kod=1
            ret = ExecSQL(connection, transaction, @"
UPDATE t_mc_selected
SET kod = -1
WHERE kod = 0;", true);
            if (!ret.result)
            {
                ret.text = "Ошибка очистки ненужных интервалов";

                ExecSQL(connection, transaction, " Drop table " + t_selected, false);
                ExecSQL(connection, transaction, " Drop table " + t_must, false);
                return;
            }

            InsertMustCalc(connection, transaction, editData.database, t_selected, " kod = 1 ", kod1, kod2, editData.local_user, out ret, editData.year, editData.month, editData.comment_action);
#else
            //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            //объединим пересекающиеся интервалы в t_selected
            //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            UnionTempTable(connection, transaction, t_selected, editData.local_user, out ret);
            if (!ret.result)
            {
                return;
            }

            //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            //объединим пересекающиеся интервалы в t_must
            //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            UnionTempTable(connection, transaction, t_must, editData.local_user, out ret);
            if (!ret.result)
            {
                return;
            }


            //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            //начинаем выцеплять непересекающиеся интервалы
            //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

            //сначала определим интервалы, которые непересекаются вообще 
            sel = " Update " + t_selected +
                  " Set kod = 1 " +
                  " Where kod <> -1 " +
                    " and not Exists ( " +
                                 " Select 1 From " + t_must + " m " +
                                 " Where " + t_selected + ".nzp_kvar = m.nzp_kvar " +
                                   " and " + t_selected + ".nzp_serv = m.nzp_serv " +
                                   " and " + t_selected + ".dat_s  <= m.dat_po " +
                                   " and " + t_selected + ".dat_po >= m.dat_s  " +
                                   " and m.kod <> -1 " +
#if PG
 " limit 1) ";
#else
                               " ) ";
#endif
            ret = ExecSQL(connection, transaction, sel, true);
            if (!ret.result)
            {
                ret.text = "Ошибка поиска интервала";

                ExecSQL(connection, transaction, " Drop table " + t_must, false);
                ExecSQL(connection, transaction, " Drop table " + t_selected, false);
                return;
            }

            //и сразу вставим эти интервалы в базу
            InsertMustCalc(connection, transaction, editData.database, t_selected, " kod = 1 ", kod1, kod2, editData.local_user, out ret, editData.year, editData.month, editData.comment_action);
            if (!ret.result)
            {
                ExecSQL(connection, transaction, " Drop table " + t_selected, false);
                ExecSQL(connection, transaction, " Drop table " + t_must, false);
                return;
            }

            //потом определим интервалы, которые полностью покрываются из must_calc, чтобы потом их игнорировать 
            sel = " Update " + t_selected +
                  " Set kod = -1 " +
                  " Where kod <> -1 " +
                    " and Exists ( " +
                                 "Select 1 From " + t_must + " m " +
                                 " Where " + t_selected + ".nzp_kvar = m.nzp_kvar " +
                                   " and " + t_selected + ".nzp_serv = m.nzp_serv " +
                                   " and " + t_selected + ".dat_s  >= m.dat_s  " +
                                   " and " + t_selected + ".dat_po <= m.dat_po " +
                                   " and m.kod <> -1 " +
#if PG
 " limit 1) ";
#else
                               " ) ";
#endif
            ret = ExecSQL(connection, transaction, sel, true);
            if (!ret.result)
            {
                ret.text = "Ошибка поиска интервала";

                ExecSQL(connection, transaction, " Drop table " + t_must, false);
                ExecSQL(connection, transaction, " Drop table " + t_selected, false);
                return;
            }

            //ищем ближайшие края среди пересекающихся интервалов в must_calc
            //чтобы ужать рабочую область
            //      [-------]       :table
            // [------]             :must_calc
#if PG
            sel = " with tt as (select max(m.dat_po + 1) as max1,max(m.dat_s + 1) as max2 From " + t_must + " m," + t_selected +
                                 " Where " + t_selected + ".nzp_kvar = m.nzp_kvar " +
                                   " and " + t_selected + ".nzp_serv = m.nzp_serv " +
                                   " and " + t_selected + ".dat_s >= m.dat_s " +
                                   " and " + t_selected + ".dat_s <= m.dat_po " +
                                   " and m.kod <> -1) " +
                    " Update " + t_selected
                  + " Set dat_s = max1,"
                  + " work_s = max2"
                  + " from tt"
                  + " Where kod = 0 "
                  + " and exists ( select 1 from tt ) ";
#else
            sel = " Update " + t_selected +
                  " Set (dat_s,work_s) = (( "+
                                 " Select max(m.dat_po+1),max(m.dat_po+1) From " + t_must + " m " +
                                 " Where " + t_selected + ".nzp_kvar = m.nzp_kvar " +
                                   " and " + t_selected + ".nzp_serv = m.nzp_serv " +
                                   " and " + t_selected + ".dat_s >= m.dat_s " +
                                   " and " + t_selected + ".dat_s <= m.dat_po " +
                                   " and m.kod <> -1 " +
                               " )) " +
                   " Where kod = 0 " +
                   "  and Exists (" + 
                                 " Select 1 From " + t_must + " m " +
                                 " Where " + t_selected + ".nzp_kvar = m.nzp_kvar " +
                                   " and " + t_selected + ".nzp_serv = m.nzp_serv " +
                                   " and " + t_selected + ".dat_s >= m.dat_s " +
                                   " and " + t_selected + ".dat_s <= m.dat_po " +
                                   " and m.kod <> -1 " +
                               " ) ";
#endif
            ret = ExecSQL(connection, transaction, sel, true);
            if (!ret.result)
            {
                ret.text = "Ошибка поиска интервала";

                ExecSQL(connection, transaction, " Drop table " + t_must, false);
                ExecSQL(connection, transaction, " Drop table " + t_selected, false);
                return;
            }

            //      [-------]       :table
            //           [-----]    :must_calc
#if PG
            sel = " with tt as (select min(m.dat_s - 1) as min1,min(m.dat_s - 1) as min2 From " + t_must + " m, " + t_selected +
                                 " Where " + t_selected + ".nzp_kvar = m.nzp_kvar " +
                                   " and " + t_selected + ".nzp_serv = m.nzp_serv " +
                                   " and " + t_selected + ".dat_po >= m.dat_s " +
                                   " and " + t_selected + ".dat_po <= m.dat_po " +
                                   " and m.kod <> -1) " +
                " Update " + t_selected +
                  " Set dat_po = min1,work_po = min2 from tt " +
                   " Where kod = 0 " +
                   "  and Exists ( Select 1 From tt ) ";
#else
            sel = " Update " + t_selected +
                  " Set (dat_po,work_po) = (( "+
                                 " Select min(m.dat_s - 1),min(m.dat_s - 1) From " + t_must + " m " +
                                 " Where " + t_selected + ".nzp_kvar = m.nzp_kvar " +
                                   " and " + t_selected + ".nzp_serv = m.nzp_serv " +
                                   " and " + t_selected + ".dat_po >= m.dat_s " +
                                   " and " + t_selected + ".dat_po <= m.dat_po " +
                                   " and m.kod <> -1 " +
                               " )) " +
                   " Where kod = 0 " +
                   "  and Exists ( Select 1 From " + t_must + " m " +
                                 " Where " + t_selected + ".nzp_kvar = m.nzp_kvar " +
                                   " and " + t_selected + ".nzp_serv = m.nzp_serv " +
                                   " and " + t_selected + ".dat_po >= m.dat_s " +
                                   " and " + t_selected + ".dat_po <= m.dat_po " +
                                   " and m.kod <> -1 " +
                               " ) ";
#endif
            ret = ExecSQL(connection, transaction, sel, true);
            if (!ret.result)
            {
                ret.text = "Ошибка поиска интервала";

                ExecSQL(connection, transaction, " Drop table " + t_must, false);
                ExecSQL(connection, transaction, " Drop table " + t_selected, false);
                return;
            }



            IDataReader reader;
            //затем работаем по пересекающимся интервалам
            int kod = 1;
            //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            while (true) //делаем это в цикле, пока не перебрем все интервалы в t_selected
            //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            {
                kod += 1;

                //ищем ближайший правый край после work_s
                //при этом не должны покрываться другими интервалами из must_calc
                //      [-------]       :table
                //         [--]         :must_calc
                sel = " Update " + t_selected +
                      " Set work_po= ( Select min(m.dat_s - 1) From " + t_must + " m " +
                                     " Where " + t_selected + ".nzp_kvar = m.nzp_kvar " +
                                       " and " + t_selected + ".nzp_serv = m.nzp_serv " +
                                       " and " + t_selected + ".work_s <= m.dat_s - 1 " +
                                       " and m.kod <> -1 " +
                                   " ) " +
                       " Where kod = 0 " +
                       "  and ( Select count(*) From " + t_must + " m " +
                                     " Where " + t_selected + ".nzp_kvar = m.nzp_kvar " +
                                       " and " + t_selected + ".nzp_serv = m.nzp_serv " +
                                       " and " + t_selected + ".work_s <= m.dat_s - 1 " +
                                       " and m.kod <> -1 " +
                                   " ) > 0 ";
                ret = ExecSQL(connection, transaction, sel, true);
                if (!ret.result)
                {
                    ret.text = "Ошибка поиска интервала";

                    ExecSQL(connection, transaction, " Drop table " + t_must, false);
                    ExecSQL(connection, transaction, " Drop table " + t_selected, false);
                    return;
                }

                //выкинем все ошибочные интервалы
                ret = ExecSQL(connection, transaction,
                        " Update " + t_selected +
                        " Set kod = -1 " +
                        " Where work_s > work_po "
                        , true);
                if (!ret.result)
                {
                    ret.text = "Ошибка поиска интервала";

                    ExecSQL(connection, transaction, " Drop table " + t_must, false);
                    ExecSQL(connection, transaction, " Drop table " + t_selected, false);
                    return;
                }

                //проверим, что интервалы непересекаются
                sel = " Update " + t_selected +
                      " Set kod = " + kod +
                      " Where kod = 0 " +
                        " and not Exists ( " +
                                     " Select 1 From " + t_must + " m " +
                                     " Where " + t_selected + ".nzp_kvar = m.nzp_kvar " +
                                       " and " + t_selected + ".nzp_serv = m.nzp_serv " +
                                       " and " + t_selected + ".work_s  <= m.dat_po " +
                                       " and " + t_selected + ".work_po >= m.dat_s  " +
                                       " and m.kod <> -1 " +
                                   " ) ";
                ret = ExecSQL(connection, transaction, sel, true);
                if (!ret.result)
                {
                    ret.text = "Ошибка поиска интервала";

                    ExecSQL(connection, transaction, " Drop table " + t_must, false);
                    ExecSQL(connection, transaction, " Drop table " + t_selected, false);
                    return;
                }

                //вставим найденные интервалы в базу 
                InsertMustCalc(connection, transaction, editData.database, t_selected, " kod = " + kod, kod1, kod2, editData.local_user, out ret, editData.year, editData.month, editData.comment_action);
                if (!ret.result)
                {
                    ExecSQL(connection, transaction, " Drop table " + t_selected, false);
                    ExecSQL(connection, transaction, " Drop table " + t_must, false);
                    return;
                }

                //передвинем work_s
                sel = " Update " + t_selected +
                      " Set kod = 0 " +
                         ", work_po = dat_po " +
                         ", work_s = ( Select min(m.dat_po + 1) From " + t_must + " m " +
                                     " Where " + t_selected + ".nzp_kvar = m.nzp_kvar " +
                                       " and " + t_selected + ".nzp_serv = m.nzp_serv " +
                                       " and " + t_selected + ".work_s  < m.dat_po    " +
                                       " and " + t_selected + ".work_po < m.dat_po    " +
                                       " and m.kod <> -1 " +
                                   " ) " +
                       " Where kod >= 0 " +
                       "  and Exists ( Select dat_po From " + t_must + " m " +
                                     " Where " + t_selected + ".nzp_kvar = m.nzp_kvar " +
                                       " and " + t_selected + ".nzp_serv = m.nzp_serv " +
                                       " and " + t_selected + ".work_s  < m.dat_po    " +
                                       " and " + t_selected + ".work_po < m.dat_po    " +
                                       " and m.kod <> -1 " +
                                   " ) ";
                ret = ExecSQL(connection, transaction, sel, true);
                if (!ret.result)
                {
                    ret.text = "Ошибка поиска интервала";

                    ExecSQL(connection, transaction, " Drop table " + t_must, false);
                    ExecSQL(connection, transaction, " Drop table " + t_selected, false);
                    return;
                }

                //выкинем все ошибочные интервалы
                ret = ExecSQL(connection, transaction,
                        " Update " + t_selected +
                        " Set kod = -1 " +
                        " Where work_s > work_po "
                        , true);
                if (!ret.result)
                {
                    ret.text = "Ошибка поиска интервала";

                    ExecSQL(connection, transaction, " Drop table " + t_must, false);
                    ExecSQL(connection, transaction, " Drop table " + t_selected, false);
                    return;
                }


                //определим, есть ли еще необработанные интервалы
                ret = ExecRead(connection, transaction, out reader,
#if PG
 " Select oid From " + t_selected +
#else
                     " Select rowid From " + t_selected +
#endif
 " Where kod = 0 "
                     , true);
                if (!ret.result)
                {
                    ret.text = "Ошибка проверки интервалов";

                    ExecSQL(connection, transaction, " Drop table " + t_must, false);
                    ExecSQL(connection, transaction, " Drop table " + t_selected, false);
                    return;
                }
                if (!reader.Read())
                {
                    //все интервалы обработаны, выходим из цикла
                    break;
                }
                reader.Close();
            }
            reader.Close();
#endif
            ExecSQL(connection, transaction, " Drop table " + t_must, true);
            ExecSQL(connection, transaction, " Drop table " + t_selected, true);

            // тормоза дл Челнов!
            //ExecSQL(conn_db, " update statistics for table must_calc ", true);
        }

        //вставка в must_calc
        //----------------------------------------------------------------------
        void InsertMustCalc(IDbConnection connection, IDbTransaction transaction, string database, string t_selected, string kod, MustCalcReasons kod1, int kod2, int local_user, out Returns ret, int currentYear, int currentMonth, string comment)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            string sel =
#if PG
 " Insert into " + database + ".must_calc (nzp_kvar, nzp_serv, nzp_supp, month_, year_, dat_s, dat_po, cnt_add, kod1, kod2, nzp_user, dat_when, comment ) " +
#else
                " Insert into " + database + ":must_calc (nzp_kvar, nzp_serv, nzp_supp, month_, year_, dat_s, dat_po, cnt_add, kod1, kod2, nzp_user, dat_when, comment ) " +
#endif
 " Select nzp_kvar, nzp_serv, 0, " + currentMonth + "," + currentYear +
#if PG
 ", work_s, work_po, 113, " + (int)kod1 + ", " + kod2 + ", " + local_user + ", current_date " +
#else
                      ", work_s, work_po, 113, " + (int)kod1 + ", " + kod2 + ", " + local_user + ", today " +
#endif
 ", '" + comment + "' " +
 " From " + t_selected +
                " Where " + kod;
            ret = ExecSQL(connection, transaction, sel, true);
            if (!ret.result)
            {
                ret.text = "Ошибка вставки перерасчетов ";
                return;
            }
        }


        // Начальный этап сохранения - добавляются поля в целевую таблицу
        //----------------------------------------------------------------------
        public void PreEdit(IDbConnection connection, IDbTransaction transaction, EditInterData editData, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            if (editData.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return;
            }

            if (editData.keys == null || editData.vals == null || editData.dopFind == null)
            {
                ret.result = false;
                ret.text = "Не определены ключи выборки данных";
                return;
            }
            if (editData.pref == "") //здесь должна содержаться база данных, где находится editData.table
            {
                if (editData.isCentral ||
                    editData.table.Trim().ToUpper() == "PRM_7" ||
                    editData.table.Trim().ToUpper() == "PRM_8" ||
                    editData.table.Trim().ToUpper() == "PRM_12" ||
                    editData.table.Trim().ToUpper() == "servpriority".ToUpper())
                {
                    editData.pref = Points.Pref;
                }
                else
                {
                    ret.result = false;
                    ret.text = "Не указана целевая база";
                    return;
                }

                RecordMonth rm = Points.GetCalcMonth(new CalcMonthParams());
                editData.year = rm.year_;
                editData.month = rm.month_;
            }
            else
            {
                RecordMonth rm = Points.GetCalcMonth(new CalcMonthParams(editData.pref));
                editData.year = rm.year_;
                editData.month = rm.month_;
            }

            //определение локального пользователя
            //не знаю, к чему это приведет, но пока убрал определение локального пользователя, если он уже определен и база не менялась. (с)Айдар 01.03.2012
            if (editData.local_user < 1 || editData.database != editData.pref.Trim() + "_data")
            {
                editData.local_user = editData.nzp_user;

                /*DbWorkUser db = new DbWorkUser();
                editData.local_user = db.GetLocalUser(connection, transaction, editData, out ret);
                db.Close();
                if (!ret.result)
                {
                    return;
                }*/

                if (editData.databaseType == enDataBaseType.kernel ||
                    editData.table.Trim().ToUpper() == "servpriority".ToUpper())
                {
#if PG
                    editData.database = editData.pref.Trim() + "_kernel." + DBManager.getServer(connection);
#else
                    editData.database = editData.pref.Trim() + "_kernel@" + DBManager.getServer(connection);
#endif
                }
#if PG
                else editData.database = editData.pref.Trim() + "_data." + DBManager.getServer(connection);
#else
                else editData.database = editData.pref.Trim() + "_data@" + DBManager.getServer(connection);
#endif
            }
        }

        //Порядок сохранения интервальных данных:
        // 1) приготовить temp1-таблицу с набором записей, которые будут изменяться (лс, параметры и т.д.), создается по Keys или dopFind
        // 2) Установить отметки редактировнаия (заблокировать запись) 
        // 3) Выбрать задетые интервалы в temp2-таблицу
        // 4) Вычислить измененные интервалы 
        // 5) Вставка (изменения) данных
        // 6) Снятие заблокированных записей
        //----------------------------------------------------------------------
        public void Saver(EditInterData editData, out Returns ret)
        {
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                return;
            }

            Saver(conn_db, null, editData, out ret);

            conn_db.Close();
        }

        public void Saver(IDbConnection connection, IDbTransaction transaction, EditInterData editData, out Returns ret)
        {
            PreEdit(connection, transaction, editData, out ret);
            if (!ret.result)
            {
                return;
            }

            //выбрать все записи с полями по целевым ключам (dat_s, dat_po считаем, что везде одинаково называются)
            string key_fields = "";
            string sql = " ";

            foreach (KeyValuePair<string, string> kvp in editData.keys)
            {
                key_fields += "," + kvp.Key;
            }

            string val_fields = "";
            foreach (KeyValuePair<string, string> kvp in editData.vals)
            {
                val_fields += "," + kvp.Key;
            }

            //условие выборки данных из целевой таблицы
            foreach (string s in editData.dopFind)
            {
                if (s.Trim() != "") sql += s;
            }
#warning замена имя таблицы с t_selected на t_saver_s
            string t_selected = "t_saver_s ";
            ExecSQL(connection, transaction, " Drop table " + t_selected, false);

#if PG
            string sel =
                " Select " + editData.primary + key_fields + val_fields + " ,dat_s, dat_po " +
                         " , coalesce(user_block,0) as user_block, coalesce(dat_block,public.mdy(1,1,2001)) as dat_block  " +
                " Into temp " + t_selected +
                " From " + editData.database + editData.table +
                " Where is_actual <> 100 " + sql;
#else
            string sel =
                " Select " + editData.primary + key_fields + val_fields + " ,dat_s, dat_po " +
                         " ,nvl(user_block,0) as user_block, nvl(dat_block,mdy(1,1,2001)) as dat_block  " +
                " From " + editData.database + ":" + editData.table +
                " Where is_actual <> 100 " + sql +
                " Into temp " + t_selected + " With no log";
#endif

            ret = ExecSQL(connection, transaction, sel, true);
            if (!ret.result)
            {
                ret.text = "Ошибка выборки данных, возможно данные заблокированы, обратитесь позднее ";
                return;
            }

            //создать случайный индекс 
            sel = "Create index sel_" + RandomText.Generate() + "_" + editData.local_user + "_2 on " + t_selected + "(" + key_fields.Remove(0, 1) + ")";
            ret = ExecSQL(connection, transaction, sel, true);
            if (!ret.result)
            {
                ret.text = "Ошибка создания индексов";

                ExecSQL(connection, transaction, " Drop table " + t_selected, false);
                return;
            }
            sel = "Create unique index sel_" + RandomText.Generate() + "_" + editData.local_user + "_1 on " + t_selected + "(" + editData.primary + ")";
            ret = ExecSQL(connection, transaction, sel, true);
            if (!ret.result)
            {
                ret.text = "Ошибка создания индексов";

                ExecSQL(connection, transaction, " Drop table " + t_selected, false);
                return;
            }

#if PG
            ExecSQL(connection, transaction, " analyze  " + t_selected, true);
#else
            ExecSQL(connection, transaction, " Update statistics for table  " + t_selected, true);
#endif
            //проверить блокировки записей
            IDataReader reader;
            ret = ExecRead(connection, transaction, out reader,
#if PG
 " Select * From " + t_selected +
                 " Where coalesce(user_block,0) <> " + editData.local_user +
                 "   and now() - coalesce(dat_block,public.mdy(1,1,2001)) < " + string.Format(" INTERVAL '{0} minutes' ", Constants.users_min)
#else
                 " Select * From " + t_selected +
                 " Where nvl(user_block,0) <> " + editData.local_user +
                 "   and current - nvl(dat_block,mdy(1,1,2001)) < " + Constants.users_min + " units minute "
#endif
, true);
            if (!ret.result)
            {
                ret.text = "Ошибка проверки блокировки записей";

                ExecSQL(connection, transaction, " Drop table " + t_selected, false);
                return;
            }
            if (reader.Read())
            {
                reader.Close();
                ret.result = false;
                ret.text = "Данные уже заблокированы для изменения другим пользователем";

                ExecSQL(connection, transaction, " Drop table " + t_selected, false);
                return;
            }

            //блокировка данных
            IDbTransaction trans;
            if (transaction != null) trans = transaction;
            else trans = connection.BeginTransaction();

            string[] m_ixs = key_fields.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            sql = " ";
            foreach (string zap in m_ixs)
            {
#if PG
                sql += " and a." + zap + " = " + editData.database + editData.table + "." + zap;
#else
                sql += " and a." + zap + " = " + editData.database + ":" + editData.table + "." + zap;
#endif
            }
#if PG
            sel = " Update " + editData.database + editData.table +
                  " Set user_block = " + editData.local_user +
                  "    ,dat_block = now() " +
                  " Where is_actual <> 100 and exists ( Select 1 From " + t_selected + " a Where 1 = 1 " + sql + ")";
#else
            sel = " Update " + editData.database + ":" + editData.table +
                  " Set user_block = " + editData.local_user +
                  "    ,dat_block = current " +
                  " Where is_actual <> 100 and exists ( Select rowid From " + t_selected + " a Where 1 = 1 " + sql + ")";
#endif
            ret = ExecSQL(connection, trans, sel, true);
            if (!ret.result)
            {
                if (transaction == null) trans.Rollback();
                ret.text = "Ошибка блокирования данных";

                ExecSQL(connection, transaction, " Drop table " + t_selected, false);
                return;
            }

            //проверить все ли записи мною заблокированы (т.е. есть ли другие блокировки)
#if PG
            sel = " Select CTID From " + editData.database + editData.table +
                  " Where is_actual <> 100 and exists ( Select 1 From " + t_selected + " a Where 1 = 1 " + sql + ")" +
                  "   and coalesce(user_block,0) <> " + editData.local_user +
                  "   and now() - coalesce(dat_block,public.mdy(1,1,2001)) < " + string.Format(" INTERVAL '{0} minutes' ", Constants.users_min);
#else
            sel = " Select rowid From " + editData.database + ":" + editData.table +
                  " Where is_actual <> 100 and exists ( Select rowid From " + t_selected + " a Where 1 = 1 " + sql + ")" +
                  "   and nvl(user_block,0) <> " + editData.local_user +
                  "   and current - nvl(dat_block,mdy(1,1,2001)) < " + Constants.users_min + " units minute ";
#endif
            ret = ExecRead(connection, trans, out reader, sel, true);
            if (!ret.result)
            {
                if (transaction == null) trans.Rollback();
                ret.text = "Ошибка проверки блокировки записей";

                ExecSQL(connection, transaction, " Drop table " + t_selected, false);
                return;
            }
            if (reader.Read())
            {
                reader.Close();

                //кто-то уже успел как-то блокирнуть записи, откатываем блокировку через Rollback
                if (transaction == null) trans.Rollback();
                ret.result = false;
                ret.text = "Данные блокированы для изменения другим пользователем";

                ExecSQL(connection, transaction, " Drop table " + t_selected, false);
                return;
            }
            if (transaction == null) trans.Commit();

            //теперь выбеpем задетые интервалы
            string t_interdata = "t_interdata";
            ExecSQL(connection, transaction, " Drop table " + t_interdata, false);

            sql = " Select " + editData.primary + key_fields + val_fields + ", 0 as kod_inter, ";


            #region отформатируем входные dat_s dat_po
            string s_s = editData.dat_s;
            string s_po = editData.dat_po;

            DateTime d1;
            if (!DateTime.TryParse(s_s, out d1))
            {
                ret.result = false;
                ret.text = "Ошибка при преобразовании строки \"" + s_s + "\" в дату";
                MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Error, true);

                ExecSQL(connection, transaction, " Drop table " + t_selected, false);
                return;
            }
            DateTime d2;
            if (!DateTime.TryParse(s_po, out d2))
            {
                ret.result = false;
                ret.text = "Ошибка при преобразовании строки \"" + s_po + "\" в дату";
                MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Error, true);

                ExecSQL(connection, transaction, " Drop table " + t_selected, false);
                return;
            }

            if (editData.intvType == enIntvType.intv_Hour)
            {
                //привести "дд.мм.гггг чч:мм" к формату "гггг-мм-дд ч"
#if PG
                s_s = Utils.EStrNull(d1.ToString("yyyy-MM-dd H:mm:ss")) + "::timestamp";
                s_po = Utils.EStrNull(d2.ToString("yyyy-MM-dd H:mm:ss")) + "::timestamp";
#else
                s_s = "cast (" + Utils.EStrNull(d1.ToString("yyyy-MM-dd H")) + " as datetime year to hour) ";
                s_po = "cast (" + Utils.EStrNull(d2.ToString("yyyy-MM-dd H")) + " as datetime year to hour) ";
#endif
            }
            else if (editData.intvType == enIntvType.intv_Month)
            {
                //выправим по-месячные даты на первое число
                s_s = MDY(d1.Month, 1, d1.Year);
                s_po = MDY(d2.Month, 1, d2.Year);
            }
            else //Подневной интервал
            {
                s_s = MDY(d1.Month, d1.Day, d1.Year);
                s_po = MDY(d2.Month, d2.Day, d2.Year);
            }

            editData.dat_s = s_s;
            editData.dat_po = s_po;
            #endregion

            sql += editData.dat_s + " as new_s, " + editData.dat_po + " as new_po ";

            if (editData.intvType == enIntvType.intv_Month)
                //выправим по-месячные даты на первое число
#if PG
                sql += ", public.mdy(" + "dat_s".Month().CastTo("integer") + ",1," + "dat_s".Year().CastTo("integer") + ") as dat_s,  public.mdy(" + "dat_po".Month().CastTo("integer") + ",1," + "dat_po".Year().CastTo("integer") + ") as dat_po  " +
                       ", public.mdy(" + "dat_s".Month().CastTo("integer") + ",1," + "dat_s".Year().CastTo("integer") + ") as isp_s,  public.mdy(" + "dat_po".Month().CastTo("integer") + ",1," + "dat_po".Year().CastTo("integer") + ") as isp_po  " +
                       ", public.mdy(" + "dat_s".Month().CastTo("integer") + ",1," + "dat_s".Year().CastTo("integer") + ") as isp2_s, public.mdy(" + "dat_po".Month().CastTo("integer") + ",1," + "dat_po".Year().CastTo("integer") + ") as isp2_po ";
#else
                sql += ", mdy(month(dat_s),1,year(dat_s)) as dat_s,  mdy(month(dat_po),1,extract(year from dat_s)) as dat_po  " +
                       ", mdy(month(dat_s),1,year(dat_s)) as isp_s,  mdy(month(dat_po),1,extract(year from dat_s)) as isp_po  " +
                       ", mdy(month(dat_s),1,year(dat_s)) as isp2_s, mdy(month(dat_po),1,extract(year from dat_s)) as isp2_po ";
#endif
            else
                sql += ", dat_s,dat_po, dat_s as isp_s, dat_po as isp_po, dat_s as isp2_s, dat_po as isp2_po ";


#if PG
            sql += (" From " + t_selected +
                   " Where dat_s  <= " + s_po +
                   "   and dat_po >= " + s_s).AddIntoStatement(" Into temp " + t_interdata);
#else
            sql += " From " + t_selected +
                   " Where dat_s  <= " + s_po +
                   "   and dat_po >= " + s_s +
                   " Into temp " + t_interdata + " With no log ";
#endif

            ret = ExecSQL(connection, transaction, sql, true);
            if (!ret.result)
            {
                ret.text = "Ошибка выборки интервалов ";

                ExecSQL(connection, transaction, " Drop table " + t_selected, false);
                ExecSQL(connection, transaction, " Drop table " + t_interdata, false);
                return;
            }

            //создать случайный индекс 
            sel = "Create index int_" + RandomText.Generate() + "_" + editData.local_user + "_2 on " + t_interdata + "(" + key_fields.Remove(0, 1) + ")";
            ret = ExecSQL(connection, transaction, sel, true);
            if (!ret.result)
            {
                ret.text = "Ошибка создания индексов";

                ExecSQL(connection, transaction, " Drop table " + t_selected, false);
                ExecSQL(connection, transaction, " Drop table " + t_interdata, false);
                return;
            }
            sel = "Create unique index int_" + RandomText.Generate() + "_" + editData.local_user + "_1 on " + t_interdata + "(" + editData.primary + ")";
            ret = ExecSQL(connection, transaction, sel, true);
            if (!ret.result)
            {
                ret.text = "Ошибка создания индексов";

                ExecSQL(connection, transaction, " Drop table " + t_selected, false);
                ExecSQL(connection, transaction, " Drop table " + t_interdata, false);
                return;
            }
            sel = "Create index int_" + RandomText.Generate() + "_" + editData.local_user + "_3 on " + t_interdata + "(kod_inter)";
            ret = ExecSQL(connection, transaction, sel, true);
            if (!ret.result)
            {
                ret.result = false;
                ret.text = "Ошибка создания индексов";

                ExecSQL(connection, transaction, " Drop table " + t_selected, false);
                ExecSQL(connection, transaction, " Drop table " + t_interdata, false);
                return;
            }

            //проверим, выбраны ли записи, если нет, то вставим новые данные и закончим
            //.....

            //начинаем вычислять новые интервалы, перебираем варианты: 
            //поля dat,dat_po - исходный период
            //   new_s,new_po - новый период
            //   isp_s,isp_po - исправленный исходный период

            //'   and dat_s  - mdy(month(dat_s),  day(dat_s),  year(dat_s) ) in ( "0 23", "0 00", "0 01", "0 02", "0 03", "0 04", "0 05", "0 06" ) '+
            //'   and dat_po - mdy(month(dat_po), day(dat_po), extract(year from dat_s)) in (         "0 00", "0 01", "0 02", "0 03", "0 04", "0 05", "0 06" ) '


            if (editData.intvType == enIntvType.intv_Hour)
#if PG
                sel = " INTERVAL '{0} hours' ";
#else
                sel = " units hour ";
#endif
            else
                if (editData.intvType == enIntvType.intv_Month)
#if PG
                    sel = " INTERVAL '{0} months' ";
#else
                    sel = " units month ";
#endif
                else
#if PG
                    sel = " INTERVAL '{0} days' ";
#else
            sel = " units day ";
#endif
            //.......................................................
            //  исх.    <-------->
            //  нов.  <------------>
            // старый интервал удалить 
            //.......................................................
            sql = " Update " + t_interdata +
                  " Set kod_inter = 3 " +
                  " Where kod_inter = 0 and new_s <= dat_s and new_po >= dat_po ";
            ret = ExecSQL(connection, transaction, sql, true);
            if (!ret.result)
            {
                ret.text = "Ошибка определения интервалов";

                ExecSQL(connection, transaction, " Drop table " + t_selected, false);
                ExecSQL(connection, transaction, " Drop table " + t_interdata, false);
                return;
            }
            //.......................................................
            //  исх.  <--------->
            //  нов.     <--------->
            // исправляем левый край
            //.......................................................
            if (editData.intvType == enIntvType.intv_Hour)
                sql = " Update " + t_interdata +
                  " Set kod_inter = 1 " +
                     ", isp_s = dat_s, isp_po = new_s " +
                  " Where kod_inter = 0 and new_s >= dat_s and new_s <= dat_po and new_po >= dat_po ";
            else
                sql = " Update " + t_interdata +
                      " Set kod_inter = 1 " +
#if PG
 ", isp_s = dat_s, isp_po = new_s -  " + string.Format(sel, 1) +
#else
                         ", isp_s = dat_s, isp_po = new_s - 1 " + sel +
#endif
 " Where kod_inter = 0 and new_s >= dat_s and new_s <= dat_po and new_po >= dat_po ";
            ret = ExecSQL(connection, transaction, sql, true);
            if (!ret.result)
            {
                ret.text = "Ошибка интервалов";

                ExecSQL(connection, transaction, " Drop table " + t_selected, false);
                ExecSQL(connection, transaction, " Drop table " + t_interdata, false);
                return;
            }
            //.......................................................
            //  исх.    <---------->
            //  нов.  <------->
            // исправляем правый край
            //.......................................................
            if (editData.intvType == enIntvType.intv_Hour)
                sql = " Update " + t_interdata +
                  " Set kod_inter = 2 " +
                     ", isp_po = dat_po, isp_s = new_po" +
                  " Where kod_inter = 0 and new_s <= dat_s and new_po >= dat_s and new_po <= dat_po ";
            else
                sql = " Update " + t_interdata +
                      " Set kod_inter = 2 " +
#if PG
 ", isp_po = dat_po, isp_s = new_po +  " + string.Format(sel, 1) +
#else
                         ", isp_po = dat_po, isp_s = new_po + 1 " + sel +
#endif
 " Where kod_inter = 0 and new_s <= dat_s and new_po >= dat_s and new_po <= dat_po ";
            ret = ExecSQL(connection, transaction, sql, true);
            if (!ret.result)
            {
                ret.text = "Ошибка определения интервалов";

                ExecSQL(connection, transaction, " Drop table " + t_selected, false);
                ExecSQL(connection, transaction, " Drop table " + t_interdata, false);
                return;
            }
            //.......................................................
            //  исх.  <------------>
            //  нов.   <--------->
            // надо породить два исправленных интервала
            //.......................................................
            if (editData.intvType == enIntvType.intv_Hour)
                sql = " Update " + t_interdata +
                  " Set kod_inter = 4 " +
                     ", isp_s = dat_s, isp_po = new_s " +
                     ", isp2_s = new_po, isp2_po = dat_po " +
                  " Where kod_inter = 0 and new_s > dat_s and new_po < dat_po ";
            else
                sql = " Update " + t_interdata +
                      " Set kod_inter = 4 " +
#if PG
 ", isp_s = dat_s, isp_po = new_s -  " + string.Format(sel, 1) +
                         ", isp2_po = dat_po, isp2_s = new_po +  " + string.Format(sel, 1) +
#else
                         ", isp_s = dat_s, isp_po = new_s - 1 " + sel +
                         ", isp2_po = dat_po, isp2_s = new_po + 1 " + sel +
#endif
 " Where kod_inter = 0 and new_s > dat_s and new_po < dat_po ";
            ret = ExecSQL(connection, transaction, sql, true);
            if (!ret.result)
            {
                ret.text = "Ошибка определения интервалов";

                ExecSQL(connection, transaction, " Drop table " + t_selected, false);
                ExecSQL(connection, transaction, " Drop table " + t_interdata, false);
                return;
            }


            //.......................................................
            //начинаем изменять в основной базе
            //для начала надо выяснить сколько записей в t_interdata, где kod_inter > 0 - потянет ли транзакция
            //.......................................................
            long icount = 0;
            object count = ExecScalar(connection, transaction, " Select count(*) From " + t_interdata + " Where kod_inter > 0 ", out ret, true);
            if (!ret.result)
            {
                ret.text = "Ошибка проверки кол-ва интервалов";

                ExecSQL(connection, transaction, " Drop table " + t_selected, false);
                ExecSQL(connection, transaction, " Drop table " + t_interdata, false);
                return;
            }
            try
            {
                icount = Convert.ToInt32(count);
            }
            catch
            {
                ret.result = false;
                ret.text = "Ошибка проверки кол-ва интервалов";

                ExecSQL(connection, transaction, " Drop table " + t_selected, false);
                ExecSQL(connection, transaction, " Drop table " + t_interdata, false);
                return;
            }
            if (icount > 500)
            {
                //сохранение порциями
                //надо делить  траназации по 500 записей
#if PG
                ret = ExecRead(connection, transaction, out reader,
                     " Select min(oid) as i_min, max(oid) as i_max From " + t_interdata +
                     " Where kod_inter > 0 "
                     , true);
#else
                ret = ExecRead(connection, transaction, out reader,
                     " Select min(rowid) as i_min, max(rowid) as i_max From " + t_interdata +
                     " Where kod_inter > 0 "
                     , true);
#endif
                if (!ret.result)
                {
                    ret.text = "Ошибка выборки интервалов для изменения";

                    ExecSQL(connection, transaction, " Drop table " + t_selected, false);
                    ExecSQL(connection, transaction, " Drop table " + t_interdata, false);
                    return;
                }
                if (!reader.Read())
                {
                    ret.result = false;
                    ret.text = "Ошибка выборки интервалов для изменения";

                    ExecSQL(connection, transaction, " Drop table " + t_selected, false);
                    ExecSQL(connection, transaction, " Drop table " + t_interdata, false);
                    return;
                }

                long i_min = -1;
                long i_max = -1;
                try
                {
                    if (reader["i_min"] != DBNull.Value) i_min = Convert.ToInt64(reader["i_min"]);
                    if (reader["i_max"] != DBNull.Value) i_max = Convert.ToInt64(reader["i_max"]);
                }
                catch
                {
                }
                reader.Close();

                if (i_min < 1 || i_max < 1)
                {
                    ret.result = false;
                    ret.text = "Ошибка выборки интервалов для изменения";

                    ExecSQL(connection, transaction, " Drop table " + t_selected, false);
                    ExecSQL(connection, transaction, " Drop table " + t_interdata, false);
                    return;
                }

                icount = i_min;
                while (icount <= i_max)
                {
                    bool b = (icount + 501 > i_max); //признак вставки нового интервала, вызывается в конце

                    ret = ChangeInterData(
                        editData, t_interdata, t_selected, key_fields,
#if PG
 " oid >= " + icount + " and oid < " + (icount + 500),
#else
                        " rowid >= " + icount + " and rowid < " + (icount + 500),
#endif
 connection, transaction, b);

                    if (!ret.result)
                    {
                        ret.text = "Ошибка сохранения интервалов";

                        ExecSQL(connection, transaction, " Drop table " + t_selected, false);
                        ExecSQL(connection, transaction, " Drop table " + t_interdata, false);
                        return;
                        //break;
                    }
                    icount += 501;
                }
            }
            else
            {
                //изменение сразу скопом
                ret = ChangeInterData(editData, t_interdata, t_selected, key_fields, " 1 = 1 ", connection, transaction, true);
                if (!ret.result)
                {
                    ret.text = "Ошибка сохранения интервалов";

                    ExecSQL(connection, transaction, " Drop table " + t_selected, false);
                    ExecSQL(connection, transaction, " Drop table " + t_interdata, false);
                    return;
                }
            }

            //проведем операцию объединения интервалов среди заблокированных записей
            //ret = UnionInterData(editData, connection, transaction);
            //if (!ret.result)
            //{
            //    ret.text = "Ошибка объединения интервалов";

            //    ExecSQL(connection, transaction, " Drop table " + t_selected, false);
            //    ExecSQL(connection, transaction, " Drop table " + t_interdata, false);
            //    return;
            //}

            //снять блокировки

#if PG
            sel = " Update " + editData.database + editData.table +
" Set user_block = null " +
"    ,dat_block = null " +
" Where user_block = " + editData.local_user +
"   and exists ( Select " + editData.primary +
          " From " + t_selected + " a " + " Where a." + editData.primary + " = " + editData.database + "." + editData.table + "." + editData.primary + ")";
#else

            sel = " Update " + editData.database + ":" + editData.table +
                " Set user_block = null " +
                "    ,dat_block = null " +
                " Where user_block = " + editData.local_user +
                "   and exists ( Select " + editData.primary +
                               " From " + t_selected + " a " + " Where a." + editData.primary + " = " + editData.database + ":" + editData.table + "." + editData.primary + ")";
#endif
            ret = ExecSQL(connection, transaction, sel, true);
            if (!ret.result)
            {
                ret.text = "Ошибка разблокирования данных";

                ExecSQL(connection, transaction, " Drop table " + t_selected, false);
                ExecSQL(connection, transaction, " Drop table " + t_interdata, false);
                return;
            }

            ExecSQL(connection, transaction, " Drop table " + t_selected, false);
            ExecSQL(connection, transaction, " Drop table " + t_interdata, false);
        }

        //----------------------------------------------------------------------
        Returns ChangeInterData(EditInterData editData,
                                 string t_interdata, string t_selected,
                                 string key_fields, string sw,
                                 IDbConnection connection,
                                IDbTransaction transaction,
                                 bool insertNew)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();

            //начинаем изменять основную базу
            IDbTransaction trans;
            if (transaction != null) trans = transaction;
            else trans = connection.BeginTransaction();

            //finder.YM.year_  == Points.CalcMonth.year_ &&
            //finder.YM.month_ == Points.CalcMonth.month_
            //.......................................................
            //сначало сохраним в архиве предыдущие интервалы
            //пока сделаем через is_actual, затем надо переделать и бросать в таблицу (_arx)!
            //.......................................................
            string sql =
#if PG
 " Update " + editData.database + "." + editData.table +
                " Set is_actual = 100, dat_del = current_date, user_del = " + editData.local_user +
                " Where exists ( Select " + editData.primary +
                               " From " + t_interdata + " a " +
                               " Where " + sw +
                               "   and a.kod_inter > 0 and a." + editData.primary + " = " + editData.database + "." + editData.table + "." + editData.primary + ")";
#else
                " Update " + editData.database + ":" + editData.table +
                " Set is_actual = 100, dat_del = today, user_del = " + editData.local_user + 
                " Where exists ( Select " + editData.primary +
                               " From " + t_interdata + " a " +
                               " Where " + sw +
                               "   and a.kod_inter > 0 and a." + editData.primary + " = " + editData.database + ":" + editData.table + "." + editData.primary + ")";
#endif
            ret = ExecSQL(connection, trans, sql, true);
            if (!ret.result)
            {
                if (transaction == null) trans.Rollback();
                ret.text = "Ошибка сохранения интервалов в архиве";
                return ret;
            }

            string val_fields = "";
            string val_vals = "";
#if PG
            val_fields += "," + string.Join(",", editData.vals.Keys.ToArray());
            val_vals += "," + string.Join(
                                    ",",
                editData.vals.Values.Select(
                                            x =>
                                            {
                                                var value = Utils.ENull(x);

                                                long tmp;
                                                return long.TryParse(value, out tmp) ? tmp.ToString() : string.Format("'{0}'", value);
                                            }).ToArray());
#else
            foreach (KeyValuePair<string, string> kvp in editData.vals)
            {
                val_fields += "," + kvp.Key;
                val_vals   += ",'" + Utils.ENull(kvp.Value)+"'";
            }
#endif
            //.......................................................
            //потом сохраним исправленные сосоедние интервалы
            //.......................................................
#if PG
            sql = " Insert into " + editData.database + "." + editData.table + "( " + key_fields + val_fields + ", dat_s,dat_po,is_actual,dat_when,nzp_user ) " +
                  " Select distinct " + key_fields + val_fields + ", isp_s,isp_po,1,current_date," + editData.local_user +
#else
            sql = " Insert into " + editData.database + ":" + editData.table + "( " + editData.primary + key_fields + val_fields + ", dat_s,dat_po,is_actual,dat_when,nzp_user ) " +
                  " Select unique 0 " + key_fields + val_fields + ", isp_s,isp_po,1,today," + editData.local_user + 
#endif
 " From " + t_interdata +
                  " Where " + sw + " and kod_inter > 0 and kod_inter <> 3";
            ret = ExecSQL(connection, trans, sql, true);
            if (!ret.result)
            {
                if (transaction == null) trans.Rollback();
                ret.text = "Ошибка исправления соседних интервалов";
                return ret;
            }
            //.......................................................
            //также внесем второй исправленный интервал для 4-го варианта
            //.......................................................
#if PG
            sql = " Insert into " + editData.database + "." + editData.table + "( " + key_fields + val_fields + ", dat_s,dat_po,is_actual,dat_when,nzp_user ) " +
                  " Select " + key_fields + val_fields + ", isp2_s,isp2_po,1,current_date," + editData.local_user +
#else
            sql = " Insert into " + editData.database + ":" + editData.table + "( " + editData.primary + key_fields + val_fields + ", dat_s,dat_po,is_actual,dat_when,nzp_user ) " +
                  " Select 0 " + key_fields + val_fields + ", isp2_s,isp2_po,1,today," + editData.local_user +
#endif
 " From " + t_interdata +
                  " Where " + sw + " and kod_inter = 4 ";
            ret = ExecSQL(connection, trans, sql, true);
            if (!ret.result)
            {
                if (transaction == null) trans.Rollback();
                ret.text = "Ошибка исправления соседних интервалов";
                return ret;
            }
            //.......................................................
            //и наконец введем новый интервал, в самом конце один раз
            //.......................................................
            if (insertNew)
            {
                string is_actual = "1 ";
                if (editData.todelete)
                {
                    //значит выполняется удаление интервала
                    is_actual = "100 ";
                }
#if PG
                sql = " Insert into " + editData.database + "." + editData.table + "( " + key_fields + val_fields + ", dat_s,dat_po,dat_when,nzp_user, is_actual, month_calc ) " +
                      " Select distinct " + key_fields + val_vals + ", new_s,new_po,current_date," + editData.local_user + "," + is_actual +
                            ", public.mdy(" + editData.month + ",1," + editData.year + ")" +
#else
                sql = " Insert into " + editData.database + ":" + editData.table + "( " + editData.primary + key_fields + val_fields + ", dat_s,dat_po,dat_when,nzp_user, is_actual, month_calc ) " +
                      " Select unique 0 " + key_fields + val_vals + ", new_s,new_po,today," + editData.local_user + "," + is_actual +
                            ", MDY(" + editData.month + ",1," + editData.year + ")" +
#endif
 " From " + t_interdata;

                ret = ExecSQL(connection, trans, sql, true);
                if (!ret.result)
                {
                    if (transaction == null) trans.Rollback();
                    ret.text = "Ошибка исправления соседних интервалов";
                    return ret;
                }

                //выставили удаленный интервал и дальше ничего не делаем
                if (editData.todelete)
                {
                    if (transaction == null) trans.Commit();
                    return ret;
                }

                //вставка записей, где ключевых запсией нет в t_interdata
                //выберем такие записи

                string key_vals = "";
                string From_Table = "";
                //0|   1    |  2 |     3
                //keys.Add("nzp_kvar", "1|nzp_kvar|kvar|nzp_kvar=303080"); //ссылка на ключевую таблицу
                //keys.Add("nzp_serv", "2|5");

                foreach (KeyValuePair<string, string> kvp in editData.keys)
                {
                    //сопоставить первичный ключ с базовой таблицой (nzp.prm_1 -> nzp_kvar.kvar)
                    string[] m_ins = kvp.Value.Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries);
                    key_vals += "," + m_ins[1];

                    if (m_ins[0] == "1")
                    {
                        string table = m_ins[2].Trim().ToLower();
                        if (table.IndexOf(tableDelimiter) < 0)
                        {
#if PG
                            if (table == "services" || table == "supplier") table = Points.Pref + "_kernel." + table;

#else
                            if (table == "services" || table == "supplier") table = Points.Pref + "_kernel@" + DBManager.getServer(connection) + ":" + table;
#endif
                            else table = editData.database + tableDelimiter + table;
                        }
                        From_Table =
                            " From " + table + " k " +
                            " Where " + m_ins[3] +
                              " and 1 > ( Select count(*) From " + t_selected + " i " +
                                        " Where i." + kvp.Key + " = k." + m_ins[1] +
                                        "   and i.dat_s  <= " + editData.dat_po +
                                        "   and i.dat_po >= " + editData.dat_s + ")";
                    }
                    if (m_ins[0] == "5")
                    {
                        //обработка prm_5
                        From_Table =
#if PG
 " From " + Points.Pref + "_data.dual Where 1 > ( Select count(*) From " + t_selected + " i " +
#else
                            " From " + Points.Pref + "_data@" + DBManager.getServer(connection) + ":dual Where 1 > ( Select count(*) From " + t_selected + " i " +
#endif
 " Where i.dat_s  <= " + editData.dat_po +
                            "   and i.dat_po >= " + editData.dat_s + ")";

                    }
                }

#if PG
                sql = " Insert into " + editData.database + "." + editData.table + "( " + key_fields + val_fields + ", dat_s,dat_po,is_actual,dat_when,nzp_user, month_calc ) " +
                      " Select distinct " + key_vals + val_vals + ", " + editData.dat_s + "," + editData.dat_po + ",1,current_date," + editData.local_user +
                                    ", public.mdy(" + editData.month + ",1," + editData.year + ")" +
#else
                sql = " Insert into " + editData.database + ":" + editData.table + "( " + editData.primary + key_fields + val_fields + ", dat_s,dat_po,is_actual,dat_when,nzp_user, month_calc ) " +
                      " Select unique 0 " + key_vals + val_vals + ", " + editData.dat_s + "," + editData.dat_po + ",1,today," + editData.local_user +
                                    ", MDY(" + editData.month + ",1," + editData.year + ")" +
#endif
 From_Table;
                ret = ExecSQL(connection, trans, sql, true);
                if (!ret.result)
                {
                    if (transaction == null) trans.Rollback();
                    ret.text = "Ошибка вставки данных";
                    return ret;
                }
            }
            if (transaction == null) trans.Commit();

            return ret;
        }

        //----------------------------------------------------------------------
        public Returns UnionInterData(EditInterData editData, IDbConnection connection, IDbTransaction transaction)
        //----------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();

            string t_union = "t_union";
            ExecSQL(connection, transaction, " Drop table " + t_union, false);

            string key_fields = "";
            string sql = " ";
            string ravno = " ";
            string ravtab = " ";

            int groupby = 0;
            foreach (KeyValuePair<string, string> kvp in editData.keys)
            {
                key_fields += "," + kvp.Key;
                ravno += " and a." + kvp.Key + " = b." + kvp.Key;
#if PG
                ravtab += " a." + kvp.Key + " = " + editData.database + "." + editData.table + "." + kvp.Key + " and ";
#else
                ravtab += " a." + kvp.Key + " = " + editData.database + ":" + editData.table + "." + kvp.Key + " and ";
#endif
                groupby += 1;
            }

            string val_fields = "";
            foreach (KeyValuePair<string, string> kvp in editData.vals)
            {
                val_fields += "," + kvp.Key;
#if PG
                ravno += " and coalesce(a." + kvp.Key + ",'0') = coalesce(b." + kvp.Key + ",'0') ";
                ravtab += " coalesce(a." + kvp.Key + ",'0') = coalesce(" + editData.database + "." + editData.table + "." + kvp.Key + ",'0') and ";
#else
                ravno += " and nvl(a." + kvp.Key + ",'0') = nvl(b." + kvp.Key + ",0) ";
                ravtab += " nvl(a." + kvp.Key + ",'0') = nvl(" + editData.database + ":" + editData.table + "." + kvp.Key + ",0) and ";
#endif
                groupby += 1;
            }

            //условие выборки данных из целевой таблицы
            foreach (string s in editData.dopFind)
            {
                if (s.Trim() != "") sql += s;
            }

#if PG
            string sel =
                " Select " + editData.primary + key_fields + val_fields + " ,dat_s, dat_po " +
                " Into temp " + t_union +
                " From " + editData.database + "." + editData.table +
                " Where is_actual <> 100 " + sql;
#else
            string sel =
                " Select " + editData.primary + key_fields + val_fields + " ,dat_s, dat_po " +
                " From " + editData.database + ":" + editData.table +
                " Where is_actual <> 100 " + sql +
                " Into temp " + t_union + " With no log";
#endif

            ret = ExecSQL(connection, transaction, sel, true);
            if (!ret.result)
            {
                ret.text = "Ошибка выборки данных ";
                return ret;
            }

            //создать случайный индекс 
            sel = "Create index uni_" + RandomText.Generate() + "_" + editData.local_user + "_2 on " + t_union + "(" + key_fields.Remove(0, 1) + ")";
            ret = ExecSQL(connection, transaction, sel, true);
            if (!ret.result)
            {
                ret.text = "Ошибка создания индексов";
                ExecSQL(connection, transaction, " Drop table " + t_union, false);
                return ret;
            }
            sel = "Create unique index uni_" + RandomText.Generate() + "_" + editData.local_user + "_1 on " + t_union + "(" + editData.primary + ")";
            ret = ExecSQL(connection, transaction, sel, true);
            if (!ret.result)
            {
                ret.text = "Ошибка создания индексов";
                ExecSQL(connection, transaction, " Drop table " + t_union, false);
                return ret;
            }

#if PG
            ExecSQL(connection, transaction, " analyze  " + t_union, true);
#else
            ExecSQL(connection, transaction, " Update statistics for table  " + t_union, true);
#endif

            string t_ravno = "t_ravno";
            ExecSQL(connection, transaction, " Drop table " + t_ravno, false);

            groupby += 3; //+pirmary,dat_s,dat_po
            string sgroupby = "1";
            for (int i = 2; i <= groupby; i++)
            {
                sgroupby += "," + i;
            }


            if (editData.intvType == enIntvType.intv_Hour)
            {
                sql =
#if PG
 "   and a.dat_s - interval '1 hours'  <= b.dat_po " +
                "   and a.dat_po+ interval '1 hours'  >= b.dat_s ";
#else
                "   and a.dat_s - 1 units hour  <= b.dat_po " +
                "   and a.dat_po+ 1 units hour  >= b.dat_s ";
#endif
            }
            else
            {
                if (editData.intvType == enIntvType.intv_Month)
                {
                    //обработаем ошибку Informix + 1 units month
#if PG
#warning А на постгре есть ли такая ошибка? Что за ошибка
                    sql =
                    "   and public.mdy(" + "a.dat_s".Month().CastTo("integer") + " ,1," + "a.dat_s".Year().CastTo("integer") + "  ) - interval '1 days'  <= b.dat_po " +
                    "   and public.mdy( " + "a.dat_po".Month().CastTo("integer") + ",1," + "a.dat_po".Year().CastTo("integer") + " ) + interval '1 days'  >= b.dat_s  ";
#else
                    sql = 
                    "   and MDY( month(a.dat_s) ,1,year(a.dat_s)  ) - 1 units day  <= b.dat_po " +
                    "   and MDY( month(a.dat_po),1,year(a.dat_po) ) + 1 units day  >= b.dat_s  ";
#endif
                }
                else
                {
#if PG
                    sql =
                    "   and a.dat_s - interval '1 days'  <= b.dat_po " +
                    "   and a.dat_po+ interval '1 days'  >= b.dat_s ";
#else
                    sql = 
                    "   and a.dat_s - 1 units day  <= b.dat_po " +
                    "   and a.dat_po+ 1 units day  >= b.dat_s ";
#endif
                }
            }

            sel =
#if PG
 " Select a.*, min(a.dat_s) as min_s, max(b.dat_po) as max_po  " +
                " Into temp " + t_ravno +
                " From " + t_union + " a, " + t_union + " b " +
                " Where a." + editData.primary + " <> b." + editData.primary +
                        sql +
                        ravno +
                " Group by " + sgroupby;
#else
                " Select a.*, min(a.dat_s) as min_s, max(b.dat_po) as max_po  " +
                " From " + t_union + " a, " + t_union + " b " +
                " Where a." + editData.primary + " <> b." + editData.primary +
                //"   and a.dat_s - 1 " + sql + " <= b.dat_po " +
                //"   and a.dat_po+ 1 " + sql + " >= b.dat_s " +
                        sql +
                        ravno +
                " Group by " + sgroupby +
                " Into temp " + t_ravno + " With no log";
#endif

            ret = ExecSQL(connection, transaction, sel, true);
            if (!ret.result)
            {
                ret.text = "Ошибка выборки данных ";
                ExecSQL(connection, transaction, " Drop table " + t_union, false);
                return ret;
            }

            //создать случайный индекс 
            sel = "Create index rav_" + RandomText.Generate() + "_" + editData.local_user + "_2 on " + t_ravno + "(" + key_fields.Remove(0, 1) + ", dat_s,dat_po)";
            ret = ExecSQL(connection, transaction, sel, true);
            if (!ret.result)
            {
                ret.text = "Ошибка создания индексов";
                ExecSQL(connection, transaction, " Drop table " + t_ravno, false);
                return ret;
            }

#if PG
            ExecSQL(connection, transaction, " analyze  " + t_ravno, true);
#else
            ExecSQL(connection, transaction, " Update statistics for table  " + t_ravno, true);
#endif

            //сохранение данных
            IDbTransaction trans;
            if (transaction != null) trans = transaction;
            else trans = connection.BeginTransaction();

            //кинем в архив записи из t_ravno
#if PG
            ravtab = ravtab + " a.min_s <= " + editData.database + "." + editData.table + ".dat_po and a.max_po >= " + editData.database + "." + editData.table + ".dat_s ";
#else
            ravtab = ravtab + " a.min_s <= " + editData.database + ":" + editData.table + ".dat_po and a.max_po >= " + editData.database + ":" + editData.table + ".dat_s ";
#endif
            sel =
#if PG
 " Update " + editData.database + "." + editData.table +
                " Set is_actual = 100, dat_del = current_date, user_del = " + editData.local_user +
#else
                " Update " + editData.database + ":" + editData.table +
                " Set is_actual = 100, dat_del = today, user_del = " + editData.local_user +
#endif
                //" Where exists ( Select rowid From " + t_ravno + " a " +
                //               " Where 1 = 1 " + ravtab + ")";
#if PG
                " Where exists ( Select 1 From " + t_union + " s Where " + editData.database + "." + editData.table + "." + editData.primary + " = s." + editData.primary + ")" +
#else
                " Where exists ( Select 1 From " + t_union + " s Where " + editData.database + ":" + editData.table + "." + editData.primary + " = s." + editData.primary + ")" +
#endif
 "   and exists ( Select 1 From " + t_ravno + " a Where " + ravtab + " )";

            ret = ExecSQL(connection, trans, sel, true);
            if (!ret.result)
            {
                if (transaction == null) trans.Rollback();
                ret.text = "Ошибка исправления соседних интервалов";

                ExecSQL(connection, transaction, " Drop table " + t_union, false);
                ExecSQL(connection, transaction, " Drop table " + t_ravno, false);
                return ret;
            }

            groupby += 1; //+ еще nzp_user
            sgroupby = "1";
            for (int i = 2; i <= groupby; i++)
            {
                sgroupby += "," + i;
            }

            //ввод объединенных интервалов
#if PG
            sel = " Insert into " + editData.database + "." + editData.table + "( " + key_fields + val_fields + ",is_actual,dat_when,nzp_user, dat_s,dat_po ) " +
                  " Select " + key_fields + val_fields + ",1,current_date," + editData.local_user + ", min(min_s), max(max_po) " +
                  " From " + t_ravno +
                  " Where min_s < max_po " +
                  " Group by 1,2,3,4,5,6";
#else
            sel = " Insert into " + editData.database + ":" + editData.table + "( " + editData.primary + key_fields + val_fields + ",is_actual,dat_when,nzp_user, dat_s,dat_po ) " +
                  " Select 0 " + key_fields + val_fields + ",1,today," + editData.local_user + ", min(min_s), max(max_po) " +
                  " From " + t_ravno +
                  " Where min_s < max_po " +
                  " Group by " + sgroupby;
#endif
            ret = ExecSQL(connection, trans, sel, true);

            if (!ret.result)
            {
                if (transaction == null) trans.Rollback();
                ret.text = "Ошибка вставки объединенных интервалов";

                ExecSQL(connection, transaction, " Drop table " + t_union, false);
                ExecSQL(connection, transaction, " Drop table " + t_ravno, false);
                return ret;
            }

            if (transaction == null) trans.Commit();
            return ret;
        }

        /// <summary>
        /// Добавляет признаки перерасчета из указанной таблицы
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="finder"></param>
        /// <param name="FromTable"></param>
        /// <param name="comment"></param>
        /// <returns></returns>
        public Returns AddMustCalcFromTable(IDbConnection conn_db, MustCalc finder, string FromTable, string comment = "")
        {
            if (finder.pref == "") return new Returns(false, "Не задан префикс");
            if (finder.nzp_serv <= 0) return new Returns(false, "Не задана услуга");
            if (finder.month_ <= 0) return new Returns(false, "Не задан месяц");
            if (finder.year_ <= 0) return new Returns(false, "Не задан год");
            if (finder.nzp_user <= 0) return new Returns(false, "Не код пользователя");

            //период 
            DateTime dt_s;
            DateTime.TryParse(finder.dat_s, out dt_s);
            DateTime dt_po;
            DateTime.TryParse(finder.dat_po, out dt_po);

            //перерасчет ведется помесячно, приводим все даты в соответствии с этим правилом
            if (dt_s == DateTime.MinValue) return new Returns(false, "Не задан период");
            dt_s = new DateTime(dt_s.Year, dt_s.Month, 1);
            if (dt_po == DateTime.MinValue) return new Returns(false, "Не задан период");
            dt_po = new DateTime(dt_po.Year, dt_po.Month, DateTime.DaysInMonth(dt_po.Year, dt_po.Month));

            //проверка периода перерасчета и наложение ограничений на него
            var ret = CheckPeriodForMustCalc(conn_db, finder, ref dt_s, ref dt_po);
            if (!ret.result)
            {
                return ret;
            }
            var date_from = Utils.EStrNull(dt_s.ToShortDateString());
            var date_to = Utils.EStrNull(dt_po.ToShortDateString());
            var mustCalcTable = finder.pref + sDataAliasRest + "must_calc";
            var sql = string.Format("INSERT  INTO {0} (nzp_kvar,nzp_serv,nzp_supp,month_,year_,dat_s,dat_po,kod1, kod2,nzp_user,dat_when, comment)  " +
                                    " SELECT t.nzp_kvar,{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11} FROM {12} t " +
                                    " WHERE NOT EXISTS (SELECT 1 FROM {0} ex WHERE t.nzp_kvar=ex.nzp_kvar " +
                                    "                   AND {1}=ex.nzp_serv " +
                                    "                   AND {2}=ex.nzp_supp" +
                                    "                   AND {5}>=ex.dat_s AND {6}<=ex.dat_po)",
                                    mustCalcTable, finder.nzp_serv, finder.nzp_supp, finder.month_, finder.year_,
                                   date_from, date_to, finder.kod1,
                                    finder.kod2, finder.nzp_user, sCurDateTime, Utils.EStrNull(comment), FromTable);
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result) return ret;

            return ret;
        }

        /// <summary>
        /// Функция проверки периода для перерасчета
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="finder"></param>
        /// <param name="dat_s"></param>
        /// <param name="dat_po"></param>
        /// <returns></returns>
        private Returns CheckPeriodForMustCalc(IDbConnection conn_db, MustCalc finder, ref DateTime dat_s, ref DateTime dat_po)
        {
            Returns ret;

            if (finder.pref == "") return new Returns(false, "Не задан префикс", -1);
            if (finder.month_ <= 0) return new Returns(false, "Не задан месяц", -1);
            if (finder.year_ <= 0) return new Returns(false, "Не задан год", -1);
            if (dat_s == DateTime.MinValue || dat_po == DateTime.MinValue) return new Returns(false, "Не задан период", -1);
            //текущий расчетный месяц
            DateTime dtCalcYear = new DateTime(finder.year_, finder.month_, 1);
            //максимальная дата из дат начала расчета системы и даты начала перерасчета
            var dateStartSystemOrRecalc =
                DBManager.CastValue<DateTime>(
                ExecScalar(conn_db, "SELECT max(val_prm) FROM " + finder.pref + sDataAliasRest +
                                    "prm_10  WHERE nzp_prm IN (82, 771) AND is_actual = 1", out ret, true));
            if (!ret.result)
            {
                ret.text =
                    "Ошибка получения параметров: \"Дата начала работы системы\" или \"Дата начала расчета/перерасчета \"";
                ret.tag = -1;
                return ret;
            }
            //перерасчет ведется помесячно, приводим все даты в соответствии с этим правилом
            dateStartSystemOrRecalc = new DateTime(dateStartSystemOrRecalc.Year, dateStartSystemOrRecalc.Month, 1);


            //проверка на корректность данных
            if (dat_s >= dat_po)
            {
                ret.tag = -1;
                ret.result = false;
                ret.text = "Дата начала периода перерасчета не может быть больше даты окончания периода";
                return ret;
            }

            //если период перерасчета целиком находится до даты начала расчета  
            //1) |--------------------|
            //   ----------------------------------(dateStart)---------------------------------------------
            if (dateStartSystemOrRecalc >= dat_po && dateStartSystemOrRecalc >= dat_s)
            {
                ret.tag = -1;
                ret.result = false;
                ret.text =
                    "Не возможно выставить период перерасчета до \"Дата начала работы системы\" или \"Дата начала расчета/перерасчета \"";
                return ret;
            }

            //если дата начала периода перерасчета < даты начала расчета, дата конца периода > даты начала расчета
            //2)                              |--------------------|
            //   ----------------------------------(dateStart)---------------------------------------------
            if (dateStartSystemOrRecalc < dat_po && dateStartSystemOrRecalc > dat_s)
            {
                ret.text =
                    "Дата начала периода перерасчета была ограничена параметром \"Дата начала работы системы\" или \"Дата начала расчета/перерасчета \"";
                //режем дату начала перерасчета
                dat_s = dateStartSystemOrRecalc;
            }
            //3)
            //ограничение по дате текущего расчетного месяца, в случае если период целиком выходит за пределы 
            if (dtCalcYear < dat_s && dtCalcYear < dat_po)
            {
                ret.tag = -1;
                ret.result = false;
                ret.text = "Период перерасчет не может быть больше, чем дата текущего расчетного месяца";
                return ret;
            }
            //4)
            //ограничение по дате окончания периода, когда дата начала меньше даты текущего РС, а дата окончания больше текущей даты РС
            if (dtCalcYear < dat_po)
            {
                ret.text = "Период действия перерасчета был ограничен датой текущего расчетного месяца";
                dat_po = dtCalcYear.AddDays(-1);
            }
            return ret;
        }
 
    }
}
