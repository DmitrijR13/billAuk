using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using FastReport;
using System.IO;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase
{
    //----------------------------------------------------------------------
    public partial class DbCounter
    //----------------------------------------------------------------------
    {
        /// <summary>
        /// Формирование данных для отчета 4.18
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="houseList"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public DataTable PrepareReportRashodPu(CounterVal finder, List<Dom> houseList, out Returns ret)
        {
            ret = Utils.InitReturns();

            #region проверка значений
            //-----------------------------------------------------------------------
            // проверка наличия пользователя
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Не определен пользователь", -1);
                return null;
            }

            // проверка даты учета
            if (finder.dat_uchet.Length <= 0)
            {
                ret = new Returns(false, "Не задана дата начала учета", -3);
                return null;
            }

            if (finder.dat_uchet_po.Length <= 0)
            {
                ret = new Returns(false, "Не задана дата окончания учета", -4);
                return null;
            }

            DateTime dat_uchet;
            DateTime dat_uchet_po;

            try
            {
                dat_uchet = Convert.ToDateTime(finder.dat_uchet);
            }
            catch
            {
                ret = new Returns(false, "Неверный формат даты начала учета", -5);
                return null;
            }

            try
            {
                dat_uchet_po = Convert.ToDateTime(finder.dat_uchet_po);
            }
            catch
            {
                ret = new Returns(false, "Неверный формат даты окончания учета", -6);
                return null;
            }
            //-----------------------------------------------------------------------
            #endregion

            string conn_kernel = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(conn_kernel);

            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                return null;
            }

            string where_dom = GetWhereKvar(finder, houseList);
            string where_service = GetWhereServ(finder, houseList);

            List<string> prefList = GetPrefList(finder, houseList, conn_db);
            
            DataTable table = new DataTable();
            table.TableName = "Q_master";

            table.Columns.Add("nzp_serv", typeof(string));
            table.Columns.Add("service", typeof(string));
            table.Columns.Add("measure", typeof(string));

            table.Columns.Add("ulica", typeof(string));
            table.Columns.Add("ndom", typeof(string));
            table.Columns.Add("nkor", typeof(string));

            table.Columns.Add("dom_rashod", typeof(decimal));
            table.Columns.Add("negil_rashod", typeof(decimal));
            table.Columns.Add("kvar_rashod", typeof(decimal));
            table.Columns.Add("ipu_ngp_cnt", typeof(decimal));
            table.Columns.Add("group_rashod", typeof(decimal));

            table.Columns.Add("dat_uchet", typeof(string));

            // периоды учета                                 
            //---------------------------------------------------
            List<DateTime> periods = new List<DateTime>();

            while (dat_uchet <= dat_uchet_po)
            {
                periods.Add(dat_uchet);
                dat_uchet = dat_uchet.AddMonths(1);
            }

            _Service service;
            List<_Service> listService = new List<_Service>();

            object obj;

            int k;
            int counterCount;
            int totalCounterCount;

            IDataReader reader = null;

            try
            {
                #region получить список услуг
                //----------------------------------------------------------------------------
                string sql = " Select distinct s.nzp_serv, s.service, m.measure " +
                      " From " + Points.Pref + DBManager.sKernelAliasRest + "s_counts cs, " +
                      Points.Pref + DBManager.sKernelAliasRest + "s_measure m, " +
                      Points.Pref + DBManager.sKernelAliasRest + "services s " +
                      " Where cs.nzp_measure = m.nzp_measure " +
                      " and cs.nzp_serv = s.nzp_serv " + where_service +
                      " Order by s.service";

                ret = ExecRead(conn_db, out reader, sql, true);
                if (!ret.result) throw new Exception(ret.text);

                while (reader.Read())
                {
                    service = new _Service();
                    if (reader["nzp_serv"] != DBNull.Value) service.nzp_serv = Convert.ToInt32(reader["nzp_serv"]);
                    if (reader["service"] != DBNull.Value) service.service = Convert.ToString(reader["service"]).Trim();
                    if (reader["measure"] != DBNull.Value) service.ed_izmer = Convert.ToString(reader["measure"]).Trim();

                    totalCounterCount = 0;

                    #region по всем банкам подсчитать количество приборов учета для конкретной услуги
                    //----------------------------------------------------------------------------
                    foreach (string cur_pref in prefList)
                    {

                        sql = " Select " + DBManager.sNvlWord + "(count(*), 0) " +
                              " From " + cur_pref + DBManager.sDataAliasRest + "counters_spis cs " +
                              " Where cs.nzp_serv = " + service.nzp_serv;


                        obj = ExecScalar(conn_db, sql, out ret, true);

                        if (!ret.result) throw new Exception(ret.text);

                        try
                        { counterCount = Convert.ToInt32(obj); }
                        catch
                        { counterCount = 0; }

                        totalCounterCount += counterCount;
                    }
                    //----------------------------------------------------------------------------
                    #endregion

                    if (totalCounterCount > 0) listService.Add(service);
                }

                reader.Close();
                reader = null;
                //----------------------------------------------------------------------------
                #endregion



                #region создать временные таблицы
                //---------------------------------------------------------------------------------------------------
                // удалить временную таблицу, ошибки не обрабатывать

                ExecSQL(conn_db, " Drop table tmp_rashod ", false);
                sql = "Create temp table tmp_rashod" +
                    "( nzp_serv   integer, " +
                    "  nzp_dom    integer, " +
                    "  dat_uchet  date, " +
                    "  service    char(100), " +
                    "  measure    char(20), " +
                    "  ulica      char(40), " +
                    "  idom       integer, " +
                    "  ndom       char(15), " +
                    "  nkor       char(15), " +
                    " rashod      " + DBManager.sDecimalType + "(18,4), " +
                    " ngp_cnt     " + DBManager.sDecimalType + "(14,4), " +
                    " nzp_type    integer " +
                    ") " + DBManager.sUnlogTempTable;
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) throw new Exception(ret.text);

                ExecSQL(conn_db, " Drop table tmp_rashod_dom ", false);
                sql = "Create temp table tmp_rashod_dom" +
                      "(ulica          char(40), " +
                      " idom           integer, " +
                      " ndom           char(15), " +
                      " nkor           char(15), " +
                      " nzp_dom        integer, " +
                      " nzp_counter    integer, " +
                      " cnt_stage      integer, " +
                      " mmnog          " + DBManager.sDecimalType + "(8,4), " +
                      " ngp_cnt        " + DBManager.sDecimalType + "(14,4), " +
                      " ngp_lift       " + DBManager.sDecimalType + "(14,4), " +
                      " val_cnt_pred   " + DBManager.sDecimalType + "(18,4), " +
                      " dat_uchet_pred date, " +
                      " val_cnt        " + DBManager.sDecimalType + "(18,4), " +
                      " dat_uchet      date " +
                      ") " + DBManager.sUnlogTempTable;

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) throw new Exception(ret.text);
                //---------------------------------------------------------------------------------------------------
                #endregion

                int j = 0;

                for (k = 0; k < listService.Count; k++)
                {
                    for (j = 0; j < periods.Count; j++)
                    {
                        string where_pu =
                            // соединение таблиц
                            " and u.nzp_ul = d.nzp_ul " +
                            " and d.nzp_dom = k.nzp_dom " +
                            " and cs.nzp_cnttype = t.nzp_cnttype " +
                            " and cs.nzp_counter = v.nzp_counter " +
                            // услуга
                            " and cs.nzp_serv = " + listService[k].nzp_serv +
                            // текущее показание
                            " and v.dat_uchet between " + Utils.EStrNull(periods[j].AddDays(1).ToShortDateString()) +
                            " and " + Utils.EStrNull(periods[j].AddMonths(1).ToShortDateString()) +
                            " and v.val_cnt is not null " +
                            " and v.is_actual <> 100 " + where_dom;

                        #region ОДПУ
                        //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                        #region 1. Очистить таблицу
                        //---------------------------------------------------------------------------------------------------------
                        ret = ExecSQL(conn_db, " Delete From tmp_rashod_dom", true);
                        if (!ret.result) throw new Exception(ret.text);
                        //---------------------------------------------------------------------------------------------------------
                        #endregion

                        foreach (string cur_pref in prefList)
                        {
                            string from_ = cur_pref + DBManager.sDataAliasRest + "kvar k," +
                                           cur_pref + DBManager.sDataAliasRest + "dom d, " +
                                           cur_pref + DBManager.sDataAliasRest + "s_ulica u, " +
                                           cur_pref + DBManager.sKernelAliasRest + "s_counttypes t, " +
                                           cur_pref + DBManager.sDataAliasRest + "counters_spis cs ";
                            //точка 						
                            #region 2. Получить данные по расходу
                            //---------------------------------------------------------------------------------------------------------

                            ExecSQL(conn_db, " Drop table tmp_insert_pu ", false);

                            sql = " Create temp table tmp_insert_pu (" +
                                  " ulica char(100)," +
                                  " idom integer, " +
                                  " ndom char(10)," +
                                  " nkor char(10)," +
                                  " nzp_dom integer," +
                                  " nzp_counter integer," +
                                  " cnt_stage integer," +
                                  " mmnog " + DBManager.sDecimalType + "(8,4)," +
                                  " ngp_cnt " + DBManager.sDecimalType + "(14,4)," +
                                  " ngp_lift " + DBManager.sDecimalType + "(14,4)," +
                                  " val_cnt " + DBManager.sDecimalType + "(14,4)," +
                                  " val_cnt_pred " + DBManager.sDecimalType + "(14,4)," +
                                  " dat_uchet Date," +
                                  " dat_uchet_pred Date)" + DBManager.sUnlogTempTable;
                            ExecSQL(conn_db, sql, true);


                            sql = " insert into tmp_insert_pu(ulica, idom, ndom, nkor, nzp_dom, nzp_counter, " +
                                  " cnt_stage, mmnog, ngp_cnt, ngp_lift, val_cnt, dat_uchet) " +
                                  " Select u.ulica, " + DBManager.sNvlWord + "(d.idom, 0) as idom, " +
                                  "     d.ndom, d.nkor, d.nzp_dom," +
                                  "     cs.nzp_counter, t.cnt_stage, t.mmnog, " +
                                  DBManager.sNvlWord + "(v.ngp_cnt, 0) as ngp_cnt, " +
                                  DBManager.sNvlWord + "(v.ngp_lift, 0) as ngp_lift, " +
                                  "     v.val_cnt, max(v.dat_uchet) as dat_uchet " +
                                  "  From " + from_ + ", " +
                                  cur_pref + DBManager.sDataAliasRest + "counters_dom v " +
                                  " Where cs.nzp_type = 1 " + //ОДПУ
                                  " and cs.nzp = d.nzp_dom " + //дом
                                  where_pu +
                                  " group by 1,2,3,4,5,6,7,8,9,10,11 ";
                            ret = ExecSQL(conn_db, sql, true);
                            if (!ret.result) throw new Exception(ret.text);

                            sql = " update tmp_insert_pu set dat_uchet_pred = (select max(p.dat_uchet) " +
                                  " from " + cur_pref + DBManager.sDataAliasRest + "counters_dom p " +
                                  " where tmp_insert_pu.nzp_counter =p.nzp_counter " +
                                  "     and p.is_actual<>100 and p.dat_uchet<= " + Utils.EStrNull(periods[j].ToShortDateString()) +
                                  "     and p.val_cnt is not null) ";

                            ret = ExecSQL(conn_db, sql, true);
                            if (!ret.result) throw new Exception(ret.text);

                            sql = " update tmp_insert_pu set val_cnt_pred = " + DBManager.sNvlWord + "((select max(p.val_cnt) " +
                                  "                 from " + cur_pref + DBManager.sDataAliasRest + "counters_dom p " +
                                  "                 where tmp_insert_pu.nzp_counter =p.nzp_counter " +
                                  "                         and p.is_actual<>100 " +
                                  "                         and tmp_insert_pu.dat_uchet_pred = p.dat_uchet" +
                                  "                         and p.val_cnt is not null),0) " +
                                  " where dat_uchet_pred is not null ";
                            ret = ExecSQL(conn_db, sql, true);
                            if (!ret.result) throw new Exception(ret.text);

                            //---------------------------------------------------------------------------------------------------------
                            #endregion

                            #region 3. Сохранить данные
                            //---------------------------------------------------------------------------------------------------------
                            sql = " Insert into tmp_rashod_dom (ulica, idom, ndom, nkor, nzp_dom, " +
                                  " nzp_counter, cnt_stage, mmnog, ngp_cnt, ngp_lift, val_cnt_pred, " +
                                  " dat_uchet_pred, val_cnt, dat_uchet) " +
                                  " Select t.ulica, t.idom, t.ndom, t.nkor, t.nzp_dom, t.nzp_counter, " +
                                  " t.cnt_stage, t.mmnog, t.ngp_cnt, t.ngp_lift, t.val_cnt_pred, " +
                                  " t.dat_uchet_pred, t.val_cnt, t.dat_uchet " +
                                  " From tmp_insert_pu t ";
                            ret = ExecSQL(conn_db, sql, true);
                            if (!ret.result) throw new Exception(ret.text);
                            //---------------------------------------------------------------------------------------------------------
                            #endregion
                        }

                        #region 4. Подсчитать расходы
                        //---------------------------------------------------------------------------------------------------------
                        ExecSQL(conn_db, " Drop table tmp_dom_itog ", false);

                        sql = " Create temp table tmp_dom_itog( " +
                              " ulica char(100)," +
                              " idom integer, " +
                              " ndom char(10)," +
                              " nkor char(10)," +
                              " nzp_dom integer," +
                              " rashod " + DBManager.sDecimalType + "(18,4)," +
                              " ngp_cnt " + DBManager.sDecimalType + "(14,4)) " + DBManager.sUnlogTempTable;

                        ret = ExecSQL(conn_db, sql, true);
                        if (!ret.result) throw new Exception(ret.text);


                        sql = " insert into tmp_dom_itog(ulica, idom, ndom, nkor, nzp_dom, rashod,  ngp_cnt) " +
                              " Select t.ulica, t.idom, t.ndom, t.nkor, t.nzp_dom, " +
                              "     sum(case when t.val_cnt >= " +
                              DBManager.sNvlWord + "(t.val_cnt_pred,0) then (t.val_cnt - " +
                              DBManager.sNvlWord + "(t.val_cnt_pred,0)) * t.mmnog - " +
                              DBManager.sNvlWord + "(t.ngp_cnt,0) - " +
                              DBManager.sNvlWord + "(t.ngp_lift,0) " +
                              "     else (pow(10, t.cnt_stage) + " +
                              DBManager.sNvlWord + "(t.val_cnt,0) - " +
                              DBManager.sNvlWord + "(t.val_cnt_pred,0)) * t.mmnog - " +
                              DBManager.sNvlWord + "(t.ngp_cnt,0) - " +
                              DBManager.sNvlWord + "(t.ngp_lift,0) end) as rashod, " +
                              " sum(" +
                              DBManager.sNvlWord + "(t.ngp_cnt,0) + " +
                              DBManager.sNvlWord + "(t.ngp_lift,0)) as ngp_cnt " +
                              " From tmp_rashod_dom t " +
                              " Group by 1,2,3,4,5  ";


                        ret = ExecSQL(conn_db, sql, true);
                        if (!ret.result) throw new Exception(ret.text);
                        //---------------------------------------------------------------------------------------------------------
                        #endregion

                        #region 5. Сохранить итоги
                        //---------------------------------------------------------------------------------------------------------
                        sql =
                            " Insert into tmp_rashod (nzp_serv, nzp_dom, dat_uchet, service, measure, ulica, idom, ndom, nkor, rashod, ngp_cnt, nzp_type) " +
                            " Select " + listService[k].nzp_serv + ", t.nzp_dom, " +
                            Utils.EStrNull(periods[j].ToShortDateString()) + ", " +
                            Utils.EStrNull(listService[k].service) + ", " + Utils.EStrNull(listService[k].ed_izmer) +
                            ", " +
                            " t.ulica, t.idom, t.ndom, t.nkor, t.rashod, t.ngp_cnt, 1 " +
                            " From tmp_dom_itog t ";
                        ret = ExecSQL(conn_db, sql, true);
                        if (!ret.result) throw new Exception(ret.text);
                        //---------------------------------------------------------------------------------------------------------
                        #endregion
                        //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                        #endregion

                        #region ИПУ
                        //#########################################################################################################
                        #region 6. Очистить таблицу
                        //---------------------------------------------------------------------------------------------------------
                        ret = ExecSQL(conn_db, " Delete From tmp_rashod_dom", true);
                        if (!ret.result) throw new Exception(ret.text);
                        //---------------------------------------------------------------------------------------------------------
                        #endregion

                        foreach (string cur_pref in prefList)
                        {


                            #region 7. Получить данные по расходу
                            //---------------------------------------------------------------------------------------------------------


                            string from_ = cur_pref + DBManager.sDataAliasRest + "kvar k," +
                                          cur_pref + DBManager.sDataAliasRest + "dom d, " +
                                          cur_pref + DBManager.sDataAliasRest + "s_ulica u, " +
                                          cur_pref + DBManager.sKernelAliasRest + "s_counttypes t, " +
                                          cur_pref + DBManager.sDataAliasRest + "counters_spis cs ";
                            ExecSQL(conn_db, " Drop table tmp_insert_pu ", false);

                            sql = " Create temp table tmp_insert_pu (" +
                                  " ulica char(100)," +
                                  " idom integer, " +
                                  " ndom char(10)," +
                                  " nkor char(10)," +
                                  " nzp_dom integer," +
                                  " nzp_counter integer," +
                                  " cnt_stage integer," +
                                  " mmnog " + DBManager.sDecimalType + "(8,4)," +
                                  " ngp_cnt " + DBManager.sDecimalType + "(14,4)," +
                                  " ngp_lift " + DBManager.sDecimalType + "(14,4)," +
                                  " val_cnt " + DBManager.sDecimalType + "(14,4)," +
                                  " val_cnt_pred " + DBManager.sDecimalType + "(14,4) default 0," +
                                  " dat_uchet Date," +
                                  " dat_uchet_pred Date)" + DBManager.sUnlogTempTable;
                            ExecSQL(conn_db, sql, true);


                            sql = " insert into tmp_insert_pu(ulica, idom, ndom, nkor, nzp_dom, nzp_counter, " +
                                  " cnt_stage, mmnog, ngp_cnt, val_cnt, dat_uchet) " +
                                  " Select u.ulica, " + DBManager.sNvlWord + "(d.idom, 0) as idom, d.ndom," +
                                  " d.nkor, d.nzp_dom, cs.nzp_counter, t.cnt_stage, " +
                                  " t.mmnog, " + (Points.IsIpuHasNgpCnt
                                      ? "" + DBManager.sNvlWord + "(v.ngp_cnt, 0) as ngp_cnt"
                                      : "0 as ngp_cnt") + ", " +
                                  " v.val_cnt, max(v.dat_uchet) as dat_uchet  " +
                                  "  From " + from_ + ", " + cur_pref + DBManager.sDataAliasRest + "counters v " +
                                  " Where cs.nzp_type = 3 " + //ИПУ
                                  " and cs.nzp = k.nzp_kvar " + //дом
                                  where_pu +
                                  " group by 1,2,3,4,5,6,7,8,9,10 ";

                            ret = ExecSQL(conn_db, sql, true);
                            if (!ret.result) throw new Exception(ret.text);

                            sql = " update tmp_insert_pu set dat_uchet_pred = (select max(p.dat_uchet) " +
                                  " from " + cur_pref + DBManager.sDataAliasRest + "counters p " +
                                  " where tmp_insert_pu.nzp_counter =p.nzp_counter " +
                                  "     and p.is_actual<>100 and p.dat_uchet<=" + Utils.EStrNull(periods[j].ToShortDateString()) + "  " +
                                  "     and p.val_cnt is not null) ";

                            ret = ExecSQL(conn_db, sql, true);
                            if (!ret.result) throw new Exception(ret.text);

                            sql = " update tmp_insert_pu set val_cnt_pred =" + DBManager.sNvlWord + "((select max(p.val_cnt) " +
                                  "                 from " + cur_pref + DBManager.sDataAliasRest + "counters p " +
                                  "                 where tmp_insert_pu.nzp_counter =p.nzp_counter " +
                                  "                         and p.is_actual<>100 " +
                                  "                         and tmp_insert_pu.dat_uchet_pred = p.dat_uchet" +
                                  "                         and p.val_cnt is not null),0) " +
                                  " where dat_uchet_pred is not null ";
                            ret = ExecSQL(conn_db, sql, true);
                            if (!ret.result) throw new Exception(ret.text);



                            // только открытые лицевые счета
                            //" and (select count(*) from " + cur_pref + "_data.prm_3 p3 where p3.nzp_prm = 51 " +
                            //" and p3.val_prm in ('1', '2') and now() between p3.dat_s and p3.dat_po and p3.nzp = k.nzp_kvar and p3.is_actual <> 100) > 0 " +

                            //string from_ = cur_pref + "_data:kvar k," +
                            //               cur_pref + "_data:dom d, " +
                            //               cur_pref + "_data:s_ulica u, " +
                            //               cur_pref + "_kernel:s_counttypes t, " +
                            //               cur_pref + "_data:counters_spis cs ";
                            //ExecSQL(conn_db, " Drop table tmp_insert_pu ", false);
                            //sql = " Select u.ulica, nvl(d.idom, 0) as idom, d.ndom, d.nkor, d.nzp_dom, cs.nzp_counter, t.cnt_stage, t.mmnog, " + (Points.IsIpuHasNgpCnt ? "nvl(v.ngp_cnt, 0) as ngp_cnt" : "0 as ngp_cnt") + ", " +
                            //            " p.val_cnt as val_cnt_pred, p.dat_uchet as dat_uchet_pred, " +
                            //            " v.val_cnt, max(v.dat_uchet) as dat_uchet " +
                            //    "  From " + from_ + ", " + cur_pref + "_data:counters v, " +
                            //                "outer " + cur_pref + "_data:counters p " +
                            //    " Where cs.nzp_type = 3 " +                            //ИПУ
                            //        " and cs.nzp = k.nzp_kvar " +                      //дом
                            //        " and p.dat_uchet = (select max(a.dat_uchet) from " + cur_pref + "_data:counters a where a.nzp_counter = p.nzp_counter " +
                            //                                 " and a.dat_uchet <= " + Utils.EStrNull(periods[j].ToShortDateString()) + " and a.is_actual <> 100 and a.val_cnt is not null) " +
                            //        where_pu +
                            //        " group by 1,2,3,4,5,6,7,8,9,10,11,12 into temp tmp_insert_pu with no log ";
                            //ret = ExecSQL(conn_db, sql, true);
                            //if (!ret.result) throw new Exception(ret.text);
                            // только открытые лицевые счета
                            //" and (select count(*) from " + cur_pref + "_data:prm_3 p3 where p3.nzp_prm = 51 " +
                            //" and p3.val_prm in ('1', '2') and current between p3.dat_s and p3.dat_po and p3.nzp = k.nzp_kvar and p3.is_actual <> 100) > 0 " +

                            //---------------------------------------------------------------------------------------------------------
                            #endregion

                            #region 8. Сохранить данные
                            //---------------------------------------------------------------------------------------------------------
                            sql = " Insert into tmp_rashod_dom (ulica, idom, ndom, nkor, nzp_dom, nzp_counter, cnt_stage, mmnog, ngp_cnt, val_cnt_pred, dat_uchet_pred, val_cnt, dat_uchet) " +
                                " Select t.ulica, t.idom, t.ndom, t.nkor, t.nzp_dom, t.nzp_counter, t.cnt_stage, t.mmnog, t.ngp_cnt, t.val_cnt_pred, t.dat_uchet_pred, t.val_cnt, t.dat_uchet " +
                                " From tmp_insert_pu t ";
                            ret = ExecSQL(conn_db, sql, true);
                            if (!ret.result) throw new Exception(ret.text);
                            //---------------------------------------------------------------------------------------------------------
                            #endregion
                        }

                        #region 9. Подсчитать расходы
                        //---------------------------------------------------------------------------------------------------------
                        ExecSQL(conn_db, " Drop table tmp_dom_itog ", false);



                        sql = " Create temp table tmp_dom_itog( " +
                              " ulica char(100)," +
                              " idom integer, " +
                              " ndom char(10)," +
                              " nkor char(10)," +
                              " nzp_dom integer," +
                              " rashod " + DBManager.sDecimalType + "(18,4)," +
                              " ngp_cnt " + DBManager.sDecimalType + "(18,4)) " + DBManager.sUnlogTempTable;

                        ret = ExecSQL(conn_db, sql, true);
                        if (!ret.result) throw new Exception(ret.text);


                        sql = " insert into tmp_dom_itog(ulica, idom, ndom, nkor, nzp_dom, rashod,  ngp_cnt) " +
                              " Select t.ulica, t.idom, t.ndom, t.nkor, t.nzp_dom, " +
                              "     sum(case when t.val_cnt >= t.val_cnt_pred " +
                              "  then (t.val_cnt - " + DBManager.sNvlWord + "(t.val_cnt_pred,0)) * t.mmnog - " +
                              DBManager.sNvlWord + "(t.ngp_cnt,0) - " +
                              DBManager.sNvlWord + "(t.ngp_lift,0)    else (pow(10, t.cnt_stage) + t.val_cnt - " +
                              DBManager.sNvlWord + "(t.val_cnt_pred,0)) * t.mmnog - " +
                              DBManager.sNvlWord + "(t.ngp_cnt,0) - " +
                              DBManager.sNvlWord + "(t.ngp_lift,0) end) as rashod, " +
                              " sum(" + DBManager.sNvlWord + "(t.ngp_cnt,0) + " +
                              DBManager.sNvlWord + "(t.ngp_lift,0)) as ngp_cnt " +
                              " From tmp_rashod_dom t " +
                              " Group by 1,2,3,4,5  ";

                        ret = ExecSQL(conn_db, sql, true);
                        if (!ret.result) throw new Exception(ret.text);
                        //---------------------------------------------------------------------------------------------------------
                        #endregion

                        #region 10. Сохранить итоги
                        //---------------------------------------------------------------------------------------------------------
                        sql = " Insert into tmp_rashod (nzp_serv, nzp_dom, dat_uchet, service, measure, ulica, idom, ndom, nkor, rashod, ngp_cnt, nzp_type) " +
                              " Select " + listService[k].nzp_serv + ", t.nzp_dom, " + Utils.EStrNull(periods[j].ToShortDateString()) + ", " + Utils.EStrNull(listService[k].service) + ", " + Utils.EStrNull(listService[k].ed_izmer) + ", " +
                                  " t.ulica, t.idom, t.ndom, t.nkor, t.rashod, t.ngp_cnt, 3 From tmp_dom_itog t ";
                        ret = ExecSQL(conn_db, sql, true);
                        if (!ret.result) throw new Exception(ret.text);

                        //---------------------------------------------------------------------------------------------------------
                        #endregion
                        //#########################################################################################################
                        #endregion

                        #region ГПУ
                        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
                        #region 11. Очистить таблицу
                        //---------------------------------------------------------------------------------------------------------
                        ret = ExecSQL(conn_db, " Delete From tmp_rashod_dom", true);
                        if (!ret.result) throw new Exception(ret.text);
                        //---------------------------------------------------------------------------------------------------------
                        #endregion

                        foreach (string cur_pref in prefList)
                        {

                            #region 12. Получить данные по расходу
                            //---------------------------------------------------------------------------------------------------------

                            string from_ = cur_pref + DBManager.sDataAliasRest + "kvar k," +
                                          cur_pref + DBManager.sDataAliasRest + "dom d, " +
                                          cur_pref + DBManager.sDataAliasRest + "s_ulica u, " +
                                         cur_pref + DBManager.sKernelAliasRest + "s_counttypes t, " +
                                         cur_pref + DBManager.sDataAliasRest + "counters_spis cs ";
                            ExecSQL(conn_db, " Drop table tmp_insert_pu ", false);


                            sql = " Create temp table tmp_insert_pu (" +
                               " ulica char(100)," +
                               " idom integer, " +
                               " ndom char(10)," +
                               " nkor char(10)," +
                               " nzp_dom integer," +
                               " nzp_counter integer," +
                               " cnt_stage integer," +
                               " mmnog " + DBManager.sDecimalType + "(8,4)," +
                               " ngp_cnt " + DBManager.sDecimalType + "(14,4)," +
                               " ngp_lift " + DBManager.sDecimalType + "(14,4)," +
                               " val_cnt " + DBManager.sDecimalType + "(14,4)," +
                               " val_cnt_pred " + DBManager.sDecimalType + "(14,4)," +
                               " dat_uchet Date," +
                               " dat_uchet_pred Date)" + DBManager.sUnlogTempTable;
                            ExecSQL(conn_db, sql, true);




                            sql = " insert into tmp_insert_pu(ulica, idom, ndom, nkor, nzp_dom, nzp_counter, " +
                                  "     cnt_stage, mmnog, val_cnt, dat_uchet) " +
                                  " Select u.ulica, " + DBManager.sNvlWord + "(d.idom, 0) as idom, d.ndom, d.nkor," +
                                  "     d.nzp_dom, cs.nzp_counter, t.cnt_stage, t.mmnog, " +
                                  "     v.val_cnt, max(v.dat_uchet) as dat_uchet " +
                                  " From " + from_ + ", " + cur_pref + DBManager.sDataAliasRest + "counters_group v " +
                                  " Where cs.nzp_type = 2 " + //ГПУ
                                  " and cs.nzp = d.nzp_dom " + //дом
                                  where_pu +
                                  " group by 1,2,3,4,5,6,7,8,9 ";

                            ret = ExecSQL(conn_db, sql, true);
                            if (!ret.result) throw new Exception(ret.text);


                            sql = " update tmp_insert_pu set dat_uchet_pred = (select max(p.dat_uchet) " +
                                " from " + cur_pref + DBManager.sDataAliasRest + "counters_group p " +
                                " where tmp_insert_pu.nzp_counter =p.nzp_counter " +
                                "     and p.is_actual<>100 and p.dat_uchet<= " + Utils.EStrNull(periods[j].ToShortDateString()) +
                                "     and p.val_cnt is not null) ";

                            ret = ExecSQL(conn_db, sql, true);
                            if (!ret.result) throw new Exception(ret.text);

                            sql = " update tmp_insert_pu set val_cnt_pred = " + DBManager.sNvlWord + "((select max(p.val_cnt) " +
                                  "                 from " + cur_pref + DBManager.sDataAliasRest + "counters_group p " +
                                  "                 where tmp_insert_pu.nzp_counter =p.nzp_counter " +
                                  "                         and p.is_actual<>100 " +
                                  "                         and tmp_insert_pu.dat_uchet_pred = p.dat_uchet" +
                                  "                         and p.val_cnt is not null),0) " +
                                  " where dat_uchet_pred is not null ";
                            ret = ExecSQL(conn_db, sql, true);
                            if (!ret.result) throw new Exception(ret.text);

                            //string from_ = cur_pref + "_data:kvar k," +
                            //               cur_pref + "_data:dom d, " +
                            //               cur_pref + "_data:s_ulica u, " +
                            //               cur_pref + "_kernel:s_counttypes t, " +
                            //               cur_pref + "_data:counters_spis cs ";
                            //ExecSQL(conn_db, " Drop table tmp_insert_pu ", false);
                            //sql = " Select u.ulica, nvl(d.idom, 0) as idom, d.ndom, d.nkor, d.nzp_dom, cs.nzp_counter, t.cnt_stage, t.mmnog, " +
                            //            " p.val_cnt as val_cnt_pred, p.dat_uchet as dat_uchet_pred, " +
                            //            " v.val_cnt, max(v.dat_uchet) as dat_uchet " +
                            //    "  From " + from_ + ", " + cur_pref + "_data:counters_group v, " +
                            //                "outer " + cur_pref + "_data:counters_group p " +
                            //    " Where cs.nzp_type = 2 " +                            //ГПУ
                            //        " and cs.nzp = d.nzp_dom " +                       //дом
                            //        " and p.dat_uchet = (select max(a.dat_uchet) from " + cur_pref + "_data:counters_group a where a.nzp_counter = p.nzp_counter " +
                            //                                 " and a.dat_uchet <= " + Utils.EStrNull(periods[j].ToShortDateString()) + " and a.is_actual <> 100 and a.val_cnt is not null) " +
                            //        where_pu +
                            //        " group by 1,2,3,4,5,6,7,8,9,10,11 into temp tmp_insert_pu with no log ";
                            //ret = ExecSQL(conn_db, sql, true);
                            //if (!ret.result) throw new Exception(ret.text);

                            //---------------------------------------------------------------------------------------------------------
                            #endregion

                            #region 13. Сохранить данные
                            //---------------------------------------------------------------------------------------------------------
                            sql = " Insert into tmp_rashod_dom (ulica, idom, ndom, nkor, nzp_dom, nzp_counter, cnt_stage, mmnog, val_cnt_pred, dat_uchet_pred, val_cnt, dat_uchet) " +
                                " Select t.ulica, t.idom, t.ndom, t.nkor, t.nzp_dom, t.nzp_counter, t.cnt_stage, t.mmnog, t.val_cnt_pred, t.dat_uchet_pred, t.val_cnt, t.dat_uchet " +
                                " From tmp_insert_pu t ";
                            ret = ExecSQL(conn_db, sql, true);
                            if (!ret.result) throw new Exception(ret.text);
                            //---------------------------------------------------------------------------------------------------------
                            #endregion
                        }

                        #region 14. Подсчитать расходы
                        //---------------------------------------------------------------------------------------------------------
                        ExecSQL(conn_db, " Drop table tmp_dom_itog ", false);

                        sql = " Create temp table tmp_dom_itog( " +
                           " ulica char(100)," +
                           " idom integer, " +
                           " ndom char(10)," +
                           " nkor char(10)," +
                           " nzp_dom integer," +
                           " rashod " + DBManager.sDecimalType + "(18,4)," +
                           " ngp_cnt " + DBManager.sDecimalType + "(18,4)) " + DBManager.sUnlogTempTable;

                        ret = ExecSQL(conn_db, sql, true);
                        if (!ret.result) throw new Exception(ret.text);


                        sql = " insert into tmp_dom_itog(ulica, idom, ndom, nkor, nzp_dom, rashod) " +
                              " Select t.ulica, t.idom, t.ndom, t.nkor, t.nzp_dom, " +
                              " sum(case when t.val_cnt >= " +
                              DBManager.sNvlWord + "(t.val_cnt_pred,0) then (t.val_cnt - " +
                              DBManager.sNvlWord + "(t.val_cnt_pred,0)) * t.mmnog " +
                              " else (pow(10, t.cnt_stage) + t.val_cnt - " +
                              DBManager.sNvlWord + "(t.val_cnt_pred,0)) * t.mmnog end) as rashod " +
                              "  From tmp_rashod_dom t " +
                              " Group by 1,2,3,4,5  ";

                        ret = ExecSQL(conn_db, sql, true);
                        if (!ret.result) throw new Exception(ret.text);
                        //---------------------------------------------------------------------------------------------------------
                        #endregion

                        #region 15. Сохранить итоги
                        //---------------------------------------------------------------------------------------------------------
                        sql = " Insert into tmp_rashod (nzp_serv, nzp_dom, dat_uchet, service, measure, ulica, idom, ndom, nkor, rashod, nzp_type) " +
                              " Select " + listService[k].nzp_serv + ", t.nzp_dom, " + Utils.EStrNull(periods[j].ToShortDateString()) + ", " + Utils.EStrNull(listService[k].service) + ", " + Utils.EStrNull(listService[k].ed_izmer) + ", " +
                                  " t.ulica, t.idom, t.ndom, t.nkor, t.rashod, 2 From tmp_dom_itog t ";
                        ret = ExecSQL(conn_db, sql, true);
                        if (!ret.result) throw new Exception(ret.text);
                        //---------------------------------------------------------------------------------------------------------
                        #endregion
                        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
                        #endregion
                    }
                }


                sql = " Select distinct t.nzp_serv, t.service, t.measure, t.ulica, t.nzp_dom, t.idom, t.ndom, t.nkor, t.dat_uchet, " +
                    // ОДПУ
                        " (Select  d.rashod From tmp_rashod d where d.nzp_dom = t.nzp_dom and d.nzp_type = 1 and d.nzp_serv = t.nzp_serv and d.dat_uchet = t.dat_uchet) as odpu_rashod, " +
                        " (Select d.ngp_cnt From tmp_rashod d where d.nzp_dom = t.nzp_dom and d.nzp_type = 1 and d.nzp_serv = t.nzp_serv and d.dat_uchet = t.dat_uchet) as odpu_ngp_cnt, " +
                    // ИПУ
                        " (Select  i.rashod From tmp_rashod i where i.nzp_dom = t.nzp_dom and i.nzp_type = 3 and i.nzp_serv = t.nzp_serv and i.dat_uchet = t.dat_uchet) as ipu_rashod, " +
                        " (Select i.ngp_cnt From tmp_rashod i where i.nzp_dom = t.nzp_dom and i.nzp_type = 3 and i.nzp_serv = t.nzp_serv and i.dat_uchet = t.dat_uchet) as ipu_ngp_cnt, " +
                    // ГПУ
                        " (Select  g.rashod From tmp_rashod g where g.nzp_dom = t.nzp_dom and g.nzp_type = 2 and g.nzp_serv = t.nzp_serv and g.dat_uchet = t.dat_uchet) as gpu_rashod " +
                    " From tmp_rashod t " +
                    " Order by t.service, t.ulica, t.idom, t.ndom, t.nkor, t.dat_uchet ";
                ret = ExecRead(conn_db, out reader, sql, true);
                if (!ret.result) throw new Exception(ret.text);

                CounterVal cv = new CounterVal();

                Decimal odpu_rashod = 0;
                Decimal odpu_ngp_cnt = 0;

                Decimal ipu_rashod = 0;
                Decimal ipu_ngp_cnt = 0;

                Decimal gpu_rashod = 0;

                while (reader.Read())
                {
                    odpu_rashod = 0;
                    odpu_ngp_cnt = 0;

                    ipu_rashod = 0;
                    ipu_ngp_cnt = 0;

                    gpu_rashod = 0;

                    if (reader["nzp_serv"] != DBNull.Value) cv.nzp_serv = Convert.ToInt32(reader["nzp_serv"]);
                    if (reader["service"] != DBNull.Value) cv.service = Convert.ToString(reader["service"]).Trim();
                    if (reader["measure"] != DBNull.Value) cv.measure = Convert.ToString(reader["measure"]).Trim();
                    if (reader["ulica"] != DBNull.Value) cv.ulica = Convert.ToString(reader["ulica"]).Trim();
                    if (reader["ndom"] != DBNull.Value) cv.ndom = Convert.ToString(reader["ndom"]).Trim();
                    if (reader["nkor"] != DBNull.Value) cv.nkor = Convert.ToString(reader["nkor"]).Trim();
                    if (reader["dat_uchet"] != DBNull.Value) cv.dat_uchet = Convert.ToDateTime(reader["dat_uchet"]).ToShortDateString();

                    if (reader["odpu_rashod"] != DBNull.Value) odpu_rashod = Convert.ToDecimal(reader["odpu_rashod"]);
                    if (reader["odpu_ngp_cnt"] != DBNull.Value) odpu_ngp_cnt = Convert.ToDecimal(reader["odpu_ngp_cnt"]);

                    if (reader["ipu_rashod"] != DBNull.Value) ipu_rashod = Convert.ToDecimal(reader["ipu_rashod"]);
                    if (reader["ipu_ngp_cnt"] != DBNull.Value) ipu_ngp_cnt = Convert.ToDecimal(reader["ipu_ngp_cnt"]);

                    if (reader["gpu_rashod"] != DBNull.Value) gpu_rashod = Convert.ToDecimal(reader["gpu_rashod"]);

                    table.Rows.Add(
                            cv.nzp_serv,
                            cv.service,
                            cv.measure,
                            cv.ulica,
                            cv.ndom,
                            cv.nkor,

                            odpu_rashod,
                            odpu_ngp_cnt,
                            ipu_rashod,
                            ipu_ngp_cnt,
                            gpu_rashod,
                            cv.dat_uchet
                    );
                }

                reader.Close();
                ExecSQL(conn_db, " Drop table tmp_rashod ", false);
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                conn_db.Close();
                prefList.Clear();
                if (reader != null) reader.Close();
                listService.Clear();
                periods.Clear();

                ExecSQL(conn_db, " Drop table tmp_insert_pu ", false);
                ExecSQL(conn_db, " Drop table tmp_rashod ", false);

                return null;
            }

            prefList.Clear();
            conn_db.Close();
            periods.Clear();

            return table;
        }
    }
}
