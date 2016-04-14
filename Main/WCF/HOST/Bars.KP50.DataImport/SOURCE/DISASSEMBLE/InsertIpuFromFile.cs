using System;
using System.Collections.Generic;
using System.Data;
using Bars.KP50.DataImport.SOURCE;
using Bars.KP50.DataImport.SOURCE.DISASSEMBLE;
using Bars.KP50.Utils.Annotations;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase
{
    class InsertIpuFromFile : DbAdminClient
    {
        /// <summary>
        /// Разбор ИПУ
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="finder"></param>
        /// <returns></returns>
        public ReturnsType Run(IDbConnection conn_db, FilesDisassemble finder)
        {
            //  return new ReturnsType(true, "Данные сохранены ", 1);
            try
            {
                string sql;
                string sSQL_Text = "";
                //изменение статуса загрузки
                sql = "update " + Points.Pref + DBManager.sUploadAliasRest + "files_imported set diss_status = 'Разбор ИПУ' where nzp_file = " + finder.nzp_file;
                ClassDBUtils.ExecSQL(sql, conn_db, ClassDBUtils.ExecMode.Log);

                sql = "select nzp_version from " + Points.Pref + DBManager.sUploadAliasRest + "files_imported where nzp_file = " + finder.nzp_file;
                var dt = ClassDBUtils.OpenSQL(sql, conn_db);
                if (dt.resultData.Rows.Count > 0)
                {
                    if (dt.resultData.Rows[0]["nzp_version"].ToString().Trim() == "1")
                    {
                        return new ReturnsType(false, "Версия 1.0 не поддерживается!", 1);
                    }
                    else 
                    {
                        #region Версия 1.2
                        
                        #region разбираем счетчики ИПУ, внутри разбираем показания этих счетчиков
                        MonitorLog.WriteLog("разбираем счетчики ИПУ, внутри разбираем показания этих счетчиков ", MonitorLog.typelog.Info, true);
                        var dbCntr = new DbCounterKernel();
                        //список объектов для добавления периодов перерасчета
                        //List<CounterValLight> listCtr = new List<CounterValLight>();

                        sSQL_Text = 
                            " SELECT a.id, a.nzp_file, k.nzp_kvar as ls_id, fs.nzp_serv, a.rashod_type, a.serv_type, a.counter_type, " +
                            " a.cnt_stage, a.mmnog, trim(a.num_cnt) " + /*||'('||trim(a.local_id)||')')*/ " as num_cnt, a.dat_uchet," +
                            " a.val_cnt, a.nzp_measure, a.dat_prov, a.dat_provnext, a.nzp_kvar, a.nzp_counter, a.local_id as local_id, a.dat_close  " +
                            " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_ipu a," +
                            Points.Pref + DBManager.sUploadAliasRest + "file_kvar b,  " +
                            Points.Pref + DBManager.sUploadAliasRest + "file_services fs,  " +
                            finder.bank + DBManager.sDataAliasRest + "kvar k " +
                            " WHERE trim(b.id) = trim(a.ls_id) AND b.nzp_file = a.nzp_file " +
                            " AND b.nzp_kvar > 0 and k.nzp_kvar = b.nzp_kvar AND" +
                            " fs.id_serv  = (a.kod_serv " + DBManager.sConvToInt + ") AND fs.nzp_file = a.nzp_file AND a.nzp_file = " + finder.nzp_file;
                        DataTable dtnote = ClassDBUtils.OpenSQL(sSQL_Text, conn_db, ClassDBUtils.ExecMode.Exception).GetData();
                        foreach (DataRow rr in dtnote.Rows)
                        {
                            MonitorLog.WriteLog("ИПУ ЛС " + rr["ls_id"].ToString() + " ид ипу " + rr["id"].ToString(), MonitorLog.typelog.Info, true); 
                            string ptype = "";
                            string pnzp_cnt = "";
                            string pnzp_counter = "";

                            #region получаем значения nzp_kvar, nzp_serv, pnzp_cnt, local_id
                            //берем nzp_kvar из file_kvar
                            int nzp_kvar = Convert.ToInt32(rr["ls_id"]);

                            int nzp_serv;
                            if (rr["nzp_serv"] != DBNull.Value) nzp_serv = Convert.ToInt32(rr["nzp_serv"]);
                            else nzp_serv = 0;


                            string sql1 = "select nzp_cnt from " + finder.bank + "_kernel" + tableDelimiter + "s_counts where nzp_serv =" + nzp_serv;

                            var dt1 = ClassDBUtils.OpenSQL(sql1, conn_db);
                            if (dt1.resultData.Rows.Count > 0 && dt1.resultData.Rows[0]["nzp_cnt"] != DBNull.Value) pnzp_cnt = Convert.ToString(dt1.resultData.Rows[0]["nzp_cnt"]);
                            else pnzp_cnt = "0";

                            string local_id;
                            if (rr["local_id"] != DBNull.Value) local_id = rr["local_id"].ToString().Trim();
                            else local_id = "0";
                            #endregion

                            //дата закрытия счетчика
                            string datClose = (rr["dat_close"] != DBNull.Value ? rr["dat_close"].ToString() : ""); 

                            #region проверяем наличие счетчика в _kernel"+tableDelimiter+"s_counttypes, если его нет - вставляем

                            sSQL_Text = "  select nzp_cnttype from  " + finder.bank + "_kernel" + tableDelimiter + "s_counttypes where name_type='" + Convert.ToString(rr["counter_type"]).Trim() + "'";

                            DataTable dtnote2 = ClassDBUtils.OpenSQL(sSQL_Text, conn_db, ClassDBUtils.ExecMode.Exception).GetData();
                            foreach (DataRow rr2 in dtnote2.Rows)
                            {
                                ptype = Convert.ToString(rr2["nzp_cnttype"]);
                            };

                            // Проверить тип счетчика 
                            if (ptype.Length == 0)
                            {
                                if (ptype.Length == 0)
                                {
                                    //если не указана разрядность, то ставим 10
                                    MonitorLog.WriteLog("если не указана разрядность, то ставим 10" , MonitorLog.typelog.Info, true); 
                                    int cnt_stage = Convert.ToInt32(rr["cnt_stage"]);
                                    if (cnt_stage == 0) cnt_stage = 10;

                                    sSQL_Text = "  insert into  " + finder.bank + "_kernel" + tableDelimiter + "s_counttypes ( cnt_stage, name_type, formula,  mmnog) " +
                                                " values ( " + cnt_stage + ",'" + Convert.ToString(rr["counter_type"]).Trim() + "', 1, " + Convert.ToString(rr["mmnog"]) + ")";
                                    ClassDBUtils.ExecSQL(sSQL_Text, conn_db, ClassDBUtils.ExecMode.Exception);

                                    sSQL_Text = "  insert into  " + Points.Pref + "_kernel" + tableDelimiter + "s_counttypes ( cnt_stage, name_type, formula,  mmnog) " +
                                                " values ( " + cnt_stage + ",'" + Convert.ToString(rr["counter_type"]).Trim() + "', 1," + Convert.ToString(rr["mmnog"]) + ")";
                                    ClassDBUtils.ExecSQL(sSQL_Text, conn_db, ClassDBUtils.ExecMode.Exception);
                                }

                                sSQL_Text = "  select nzp_cnttype from  " + finder.bank + "_kernel" + tableDelimiter + "s_counttypes where name_type='" + Convert.ToString(rr["counter_type"]).Trim() + "'";

                                DataTable dtnote3 = ClassDBUtils.OpenSQL(sSQL_Text, conn_db, ClassDBUtils.ExecMode.Exception).GetData();
                                foreach (DataRow rr3 in dtnote3.Rows)
                                {
                                    ptype = Convert.ToString(rr3["nzp_cnttype"]);
                                };
                            };
                            #endregion


                            #region Проверить есть ли счетчик для данного лицевого счета


                            sSQL_Text = "  select nzp_counter  from  " + finder.bank + "_data" + tableDelimiter + "counters_spis where nzp=" + nzp_kvar +
                                   " and nzp_serv =" + nzp_serv + " and  num_cnt= '" + Convert.ToString(rr["num_cnt"]).Trim() + "'";

                            DataTable dtnote4 = ClassDBUtils.OpenSQL(sSQL_Text, conn_db, ClassDBUtils.ExecMode.Exception).GetData();
                            //if (dtnote4.Rows.Count == 0)
                            //{
                            //    sSQL_Text = "  select nzp_counter  from  " + lpref + "_data" + tableDelimiter + "counters_spis where nzp=" + nzp_kvar +
                            //       " and nzp_serv =" + nzp_serv + " and  num_cnt= '" + Convert.ToString(rr["num_cnt"]).Trim() + "'";

                            //    dtnote4 = ClassDBUtils.OpenSQL(sSQL_Text, conn_db, ClassDBUtils.ExecMode.Exception).GetData();
                            //}
                            MonitorLog.WriteLog(sSQL_Text, MonitorLog.typelog.Info, true); 
                            foreach (DataRow rr4 in dtnote4.Rows)
                            {

                                pnzp_counter = Convert.ToString(rr4["nzp_counter"]);
                                MonitorLog.WriteLog("nzp_counter=" + pnzp_counter.ToString(), MonitorLog.typelog.Info, true); 
                            };
                            #endregion

                            #region если нет - вставляем
                            //is_gkal: если 4, то 1, иначе 0. см. file_measures
                            int is_gkal;
                            //if (Convert.ToInt32(rr["nzp_measure"]) == 4) is_gkal = 1;
                            // кодировка идиотского кода 3=4
                            if (Convert.ToInt32(rr["nzp_measure"]) == 3) is_gkal = 1;
                            else is_gkal = 0;

                            //в kod_pu записываем nzp_measure, чтобы не потерять информацию
                            int kod_pu = Convert.ToInt32(rr["nzp_measure"]);

                            // Готовы вставить значения счетчиков 
                            //if (Convert.ToString(rr["nzp_serv"]) == "6") { pnzp_cnt = "4"; };

                            string dat_prov;
                            if (rr["dat_prov"] != DBNull.Value) dat_prov = "'" + Convert.ToString(rr["dat_prov"]).Substring(0, 10) + "'";
                            else dat_prov = "null";

                            string dat_provnext;
                            if (rr["dat_provnext"] != DBNull.Value) dat_provnext = "'" + Convert.ToString(rr["dat_provnext"]).Substring(0, 10) + "'";
                            else dat_provnext = "null";

                            if (pnzp_counter.Length == 0)
                            {
                                #region вставить счетчики

                                string seq = Points.Pref + "_data" + tableDelimiter + "counters_spis_nzp_counter_seq";
                                string strNzp_cnt;
#if PG
                                strNzp_cnt = " nextval('" + seq + "') ";
#else
                                strNzp_cnt = seq + ".nextval ";
#endif


                                sSQL_Text = " insert into " + finder.bank + "_data" + tableDelimiter + "counters_spis (" +
                                            " nzp_counter, nzp_type, nzp, nzp_serv, nzp_cnttype, num_cnt, is_gkal," +
                                            " kod_pu, kod_info, dat_prov, dat_provnext, dat_oblom, dat_poch, dat_close," +
                                            " comment, is_actual, nzp_cnt, nzp_user, dat_when, is_pl, cnt_ls, dat_block," +
                                            " user_block " + //", user_del " +
                                            ")" +
                                            " values (" +
                                            //    " nzp_counter, nzp_type, nzp,      nzp_serv,       nzp_cnttype,               num_cnt,                            is_gkal," +
                                            strNzp_cnt + ",    3 ,  " + nzp_kvar + "," + nzp_serv + "," + ptype + ",'" +
                                            Convert.ToString(rr["num_cnt"]).Trim() + "', " + is_gkal + " ," +
                                            //  " kod_pu,         kod_info,          dat_prov,         dat_provnext,       dat_oblom, dat_poch, dat_close," +
                                            "       " + kod_pu + ",    0,       " + dat_prov + ", " + dat_provnext +
                                            ", null,        null,     null, '" +
                                            //  " comment, is_actual, nzp_cnt, nzp_user, dat_when, is_pl, cnt_ls, dat_block," +
                                            local_id + "',      1, " + pnzp_cnt + "," + finder.nzp_user + ",  " + sCurDate + ",  null, 0,  null, " +
                                            //  " user_block " +
                                            " " + finder.nzp_file + " ) "; //", " + local_id + " ) ";


                                #endregion

                                ClassDBUtils.ExecSQL(sSQL_Text, conn_db, ClassDBUtils.ExecMode.Exception);

                                // decimal nzpcounter = GetSerialValue(conn_db);
                                #region найти вставленное значение


                                sSQL_Text = "  select nzp_counter  from  " + finder.bank + "_data" + tableDelimiter + "counters_spis where nzp=" + nzp_kvar +
                                                    " and nzp_serv =" + nzp_serv + " and  num_cnt= '" + Convert.ToString(rr["num_cnt"]).Trim() + "'";

                                DataTable dtnote5 = ClassDBUtils.OpenSQL(sSQL_Text, conn_db, ClassDBUtils.ExecMode.Exception).GetData();
                                foreach (DataRow rr5 in dtnote5.Rows)
                                {
                                    pnzp_counter = Convert.ToString(rr5["nzp_counter"]);

                                };
                                MonitorLog.WriteLog("добавлен nzp_counter=" + pnzp_counter.ToString(), MonitorLog.typelog.Info, true); 
                                #endregion
                            }

                            //если есть дата закрытия, то обновляем counters_spis
                            if (datClose != "")
                            {
                                sql = " UPDATE " + finder.bank + DBManager.sDataAliasRest + "counters_spis " +
                                      " SET dat_close = CAST('" + datClose + "' AS DATE) WHERE nzp_counter = " + pnzp_counter + ";";
                                ClassDBUtils.ExecSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception);
                            }

                            #endregion
                            

                            #region разбор показаний ИПУ

                            sSQL_Text = " select a.id as id_p, a.nzp_file as nzp_file_p, a.id_ipu as id_ipu_p, a.rashod_type as rashod_type_p," +
                                        " a.dat_uchet as dat_uchet_p, a.val_cnt as val_cnt_p, a.kod_serv as kod_serv_p" +
                                        "  from " + Points.Pref + DBManager.sUploadAliasRest + "file_ipu_p a where a.nzp_file = " + finder.nzp_file + " and trim(id_ipu) = '" + Convert.ToString(rr["local_id"]).Trim() + "'";
                            MonitorLog.WriteLog(sSQL_Text, MonitorLog.typelog.Info, true); 
                            DataTable dtnote_p = ClassDBUtils.OpenSQL(sSQL_Text, conn_db, ClassDBUtils.ExecMode.Exception).GetData();
                            int ZZ = 0;
                            foreach (DataRow rr_p in dtnote_p.Rows)
                            {
                                //MonitorLog.WriteLog("Показание на:"+rr_p["dat_uchet_p"].ToString()+" номер= "+(++ZZ).ToString(), MonitorLog.typelog.Info, true); 
                                string dat_uchet_p;
                                if (rr_p["dat_uchet_p"] != DBNull.Value) dat_uchet_p = "'" + Convert.ToString(rr_p["dat_uchet_p"]).Substring(0, 10) + "'";
                                else dat_uchet_p = "null";

                                #region проверяем наличие такой записи в counters

                                sSQL_Text = "  select  nzp_cr  from  " + finder.bank + "_data" + tableDelimiter + "counters where nzp_kvar=" + nzp_kvar +
                                            " and nzp_serv =" + nzp_serv + " and  num_cnt= '" + Convert.ToString(rr["num_cnt"]).Trim() + "' and val_cnt=" + Convert.ToString(rr_p["val_cnt_p"]) +
                                            " and dat_uchet=" + dat_uchet_p;
                                //MonitorLog.WriteLog("наверно здесь :"+sSQL_Text, MonitorLog.typelog.Info, true); 
                                DataTable dtnote6 = ClassDBUtils.OpenSQL(sSQL_Text, conn_db, ClassDBUtils.ExecMode.Exception).GetData();
                                Int32 pnzp_cr = 0;
                                foreach (DataRow rr6 in dtnote6.Rows)
                                {
                                    
                                    pnzp_cr = Convert.ToInt32(rr6["nzp_cr"]);
                                    //MonitorLog.WriteLog("показание уже есть" + pnzp_cr.ToString(), MonitorLog.typelog.Info, true); 
                                    //  break;
                                }
                                #endregion

                                #region вставляем запись в counters в случае отсутствия
                                if (pnzp_cr == 0)
                                {
                                  //  MonitorLog.WriteLog("показание нет добавить " + pnzp_cr.ToString(), MonitorLog.typelog.Info, true); 
                                    sSQL_Text = " insert into  " + finder.bank + "_data" + tableDelimiter + "counters (" +
                                                "  nzp_kvar, num_ls, nzp_serv, nzp_cnttype, num_cnt, " +
                                                " dat_prov, dat_provnext, dat_uchet, val_cnt, norma, " +
                                                " is_actual, nzp_user, dat_when, dat_close, cur_unl, nzp_wp," +
                                                " ist, dat_oblom, dat_poch,  " +
                                                " user_del, nzp_counter" + //", user_block " +
                                                " )" +
                                                " values (" +
                                        //"nzp_kvar,                             num_ls               nzp_serv,        nzp_cnttype,                num_cnt, "+
                                                " " + nzp_kvar + "," + Convert.ToString(rr["ls_id"]) + "," + nzp_serv + ",'" + ptype + "','" + Convert.ToString(rr["num_cnt"]) + "'," +
                                        // "          dat_prov,           dat_provnext,          dat_uchet,                     val_cnt,                   norma, " +
                                                " " + dat_prov + ", " + dat_provnext + "," + dat_uchet_p + "," + Convert.ToString(rr_p["val_cnt_p"]) + ",null," +
                                        //" is_actual,   nzp_user,        dat_when, dat_close, cur_unl, nzp_wp,"+
                                                " 1," + finder.nzp_user + ", " + sCurDate + " , null, 1, 1 ," +
                                        //" ist, dat_oblom, dat_poch, dat_del, " +
                                                " 0,null,null," +
                                        //" user_del, nzp_counter,  user_block "+
                                                " " + finder.nzp_file + "," + pnzp_counter + " ) ";  //"," + local_id + " ) ";


                                    ClassDBUtils.ExecSQL(sSQL_Text, conn_db, ClassDBUtils.ExecMode.Exception);

                                    //получить айди вставленного показания
                                    int nzp_key = GetSerialValue(conn_db);

                                    #region Вставка перерасчетов в must_calc

                                    string table_name = "counters";
                                    DbDisUtils u = new DbDisUtils(finder);
                                    u.InsertIntoMustCalc(finder, conn_db, dat_uchet_p.Replace("'",""), Convert.ToInt32(pnzp_counter), nzp_kvar, nzp_serv, 3, 0, table_name);

                                    #endregion Вставка перерасчетов в must_calc

                                    //формируем объекты
                                    //var obj = new CounterValLight
                                    //{
                                    //    pref = finder.bank,
                                    //    nzp_user = finder.nzp_user,
                                    //    nzp_kvar = nzp_kvar,
                                    //    nzp_serv = nzp_serv,
                                    //    dat_uchet =
                                    //        Convert.ToDateTime(dat_uchet_p.Replace("\'", "")).ToString("dd-MM-yyyy"),
                                    //    nzp_cnttype = Convert.ToInt32(ptype),
                                    //    val_cnt = Convert.ToDecimal(rr_p["val_cnt_p"]),
                                    //    val_cnt_s = Convert.ToString(rr_p["val_cnt_p"]),
                                    //    nzp_counter = Convert.ToInt32(pnzp_counter),
                                    //    ist = 1,
                                    //    nzp_key = nzp_key,
                                    //    nzp_type = 3
                                    //};

                                    //listCtr.Add(obj);
                                }
                                #endregion
                            }

                            #endregion
                        }

                        //Добавление периодов перерасчета
                        //dbCntr.SaveCountersValsLight(listCtr);

                        #endregion
                        #endregion
                    }
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры insertIpuFromFile. " + ex.Message + "\n" + ex.StackTrace, MonitorLog.typelog.Error, true);
                return new ReturnsType(false, "Ошибка выполнения процедуры insertIpuFromFile ", -1);
            }
            return new ReturnsType(true, "Данные сохранены ", 1);
        }     
    }
}
