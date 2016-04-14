using System;
using System.Collections.Generic;
using System.Data;
using Bars.KP50.DataImport.SOURCE;
using Bars.KP50.DataImport.SOURCE.DISASSEMBLE;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase
{
    public class InsertOdpuFromFile : DbAdminClient
    {
        /// <summary>
        /// Разбор ОДПУ
        /// </summary>
        /// <param name="conDb"></param>
        /// <param name="finder"></param>
        /// <returns></returns>
        public ReturnsType Run(IDbConnection conDb, FilesDisassemble finder)
        {
            MonitorLog.WriteLog("Привет Артем будем смотреть функцию которая один трай ", MonitorLog.typelog.Info, true);
            try
            {
                #region Разбор когда ukas выставлен
                string sql;
                //изменение статуса загрузки
                sql = "update " + Points.Pref + DBManager.sUploadAliasRest + "files_imported set diss_status = 'Разбор ОДПУ' where nzp_file = " + finder.nzp_file;
                ClassDBUtils.ExecSQL(sql, conDb, ClassDBUtils.ExecMode.Log);

                MonitorLog.WriteLog(" start 1 ", MonitorLog.typelog.Info, true);
                sql = "select nzp_version from " + Points.Pref + DBManager.sUploadAliasRest + "files_imported where nzp_file = " + finder.nzp_file;
                MonitorLog.WriteLog(" start 2 ", MonitorLog.typelog.Info, true);
                var dt = ClassDBUtils.OpenSQL(sql, conDb);
                if (dt.resultData.Rows.Count > 0)
                {
                    MonitorLog.WriteLog(" start 3 ", MonitorLog.typelog.Info, true);
                    if (dt.resultData.Rows[0]["nzp_version"].ToString().Trim() == "1")
                    {
                        MonitorLog.WriteLog(" start 4 ", MonitorLog.typelog.Info, true);
                        return new ReturnsType(false, "Версия 1.0 не поддерживается!", 1);
                    }
                    else 
                    {
                        MonitorLog.WriteLog(" start 5 ", MonitorLog.typelog.Info, true);
                        #region Версия 1.2
                        // выбрать все данные из загруженного charge
                       
                        string sSQL_Text = "";
                        int nzp_serv = 0;
                        int is_odn = 0;

                        //Исправить номер дома  если вдруг не был сделан разбор
                        sSQL_Text =
                            "  update " + Points.Pref + DBManager.sUploadAliasRest + "file_odpu set nzp_dom  =" +
                            " (" +
                            " select distinct b.nzp_dom  from " + Points.Pref +DBManager.sUploadAliasRest + "file_dom b " +
                            " where b.id=" + Points.Pref + DBManager.sUploadAliasRest +"file_odpu.dom_id " +
                            " and b.nzp_dom >0  and b.nzp_file=" + finder.nzp_file +
                            " ) " +
                            " where " + Points.Pref + DBManager.sUploadAliasRest +"file_odpu.nzp_dom is null " +
                            " and nzp_file =" + finder.nzp_file;
                        ClassDBUtils.ExecSQL(sSQL_Text, conDb, ClassDBUtils.ExecMode.Exception);

                        MonitorLog.WriteLog(" start 1 ", MonitorLog.typelog.Info, true);
                        #region разбираем счетчики ОДПУ, внутри разбираем показания этих счетчиков

                        //var dbCntr = new DbCounterKernel();
                        //список объектов для добавления периодов перерасчета
                        //List<CounterValLight> listCtr = new List<CounterValLight>();

                        sSQL_Text =
                                " select a.id, a.nzp_file, a.dom_id, d.nzp_serv, a.rashod_type, a.serv_type, a.counter_type, a.cnt_stage, a.mmnog, a.num_cnt, a.dat_uchet," +
                                " a.val_cnt, m.nzp_measure, a.dat_prov, a.dat_provnext, b.nzp_dom, a.nzp_counter, a.local_id, a.doppar, coalesce( a.type_pu,1) type_pu   " +
                                " from " + Points.Pref + DBManager.sUploadAliasRest + "file_odpu a, " + Points.Pref +DBManager.sUploadAliasRest + "file_dom b , " +
                                Points.Pref + DBManager.sUploadAliasRest + "file_services d,  " + Points.Pref + DBManager.sUploadAliasRest + "file_measures m " +
                                " where b.id=a.dom_id  and a.nzp_measure=m.id_measure " +
                                " and m.nzp_file=a.nzp_file and d.nzp_file=a.nzp_file " +
                                " and b.nzp_file=a.nzp_file and b.nzp_dom >0 " +
                                " and d.id_serv=a.nzp_serv and a.nzp_file = " + finder.nzp_file;
                        MonitorLog.WriteLog(" start 6 ", MonitorLog.typelog.Info, true);
                        MonitorLog.WriteLog(sSQL_Text  , MonitorLog.typelog.Info, true);
                        DataTable dtnote = ClassDBUtils.OpenSQL(sSQL_Text, conDb, ClassDBUtils.ExecMode.Exception).GetData();
                        foreach (DataRow rr in dtnote.Rows)
                        {
                            // Перебор строк разбираемых файлов 
                          //  MonitorLog.WriteLog(" start 7 ", MonitorLog.typelog.Info, true);
                            #region Подготовка к разбору показаний
                            string ptype = "";
                            string pnzp_cnt = "";
                            string pnzp_counter = "";

                            #region получаем значения nzp_kvar, nzp_serv, pnzp_cnt
                          //  MonitorLog.WriteLog(" start 7 1 ", MonitorLog.typelog.Info, true);
                            int nzp_dom = Convert.ToInt32(rr["nzp_dom"]);  // Если выставился дом, то получим его здесь 
                            int type_pu = Convert.ToInt32(rr["type_pu"]); // Если выставился тип ПУ, то получим его здесь

                            string table = "";

                            if (type_pu == 1)
                                table = "counters_dom";
                            if (type_pu == 2 || type_pu == 3)
                                table = "counters_group";
                         //   MonitorLog.WriteLog(" start 7 2 ", MonitorLog.typelog.Info, true);

                            if (rr["nzp_serv"] != DBNull.Value) nzp_serv = Convert.ToInt32(rr["nzp_serv"]);
                            else if (rr["kod_serv"] != DBNull.Value) nzp_serv = Convert.ToInt32(rr["kod_serv"]);
                            else nzp_serv = 0;

                            if (nzp_serv == 515)
                            {
                                is_odn = 1; ////  загнать признак что счетчик измеряет только ОДН select * from gub01_kernel :prm_name where nzp_prm =2068 
                                nzp_serv = 25;////  если выгрузили ОДН , тогда исправить на  электроснабжение 
                            }
                            else { is_odn = 0; }
                            #endregion получаем значения nzp_kvar, nzp_serv, pnzp_cnt

                            #endregion Подготовка к разбору показаний

                         //   MonitorLog.WriteLog(" start 8 ", MonitorLog.typelog.Info, true);
                            #region Определить тип счетчика
                            string sql1 = "select nzp_cnt from " + finder.bank + "_kernel" + tableDelimiter + "s_counts where nzp_serv =" + nzp_serv;
                            var dt1 = ClassDBUtils.OpenSQL(sql1, conDb);

                            if (dt1.resultData.Rows.Count > 0 && dt1.resultData.Rows[0]["nzp_cnt"] != DBNull.Value) pnzp_cnt = Convert.ToString(dt1.resultData.Rows[0]["nzp_cnt"]);
                            else pnzp_cnt = "0";


                            #region проверяем наличие счетчика в _kernel"+tableDelimiter+"s_counttypes, если его нет - вставляем
                            sSQL_Text = "  select nzp_cnttype from  " + finder.bank + "_kernel" + tableDelimiter + "s_counttypes where name_type='" + Convert.ToString(rr["counter_type"]).Trim() + "'";
                            DataTable dtnote2 = ClassDBUtils.OpenSQL(sSQL_Text, conDb, ClassDBUtils.ExecMode.Exception).GetData();
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
                                    int cnt_stage = Convert.ToInt32(rr["cnt_stage"]);
                                    if (cnt_stage == 0) cnt_stage = 10;

                                    sSQL_Text = "  insert into  " + finder.bank + "_kernel" + tableDelimiter + "s_counttypes ( cnt_stage, name_type, formula,  mmnog) " +
                                                " values ( " + cnt_stage + ",'" + Convert.ToString(rr["counter_type"]).Trim() + "', 1, " + Convert.ToString(rr["mmnog"]) + ")";
                                    ClassDBUtils.ExecSQL(sSQL_Text, conDb, ClassDBUtils.ExecMode.Exception);

                                    sSQL_Text = "  insert into  " + Points.Pref + "_kernel" + tableDelimiter + "s_counttypes ( cnt_stage, name_type, formula,  mmnog) " +
                                                " values ( " + cnt_stage + ",'" + Convert.ToString(rr["counter_type"]).Trim() + "', 1," + Convert.ToString(rr["mmnog"]) + ")";
                                    ClassDBUtils.ExecSQL(sSQL_Text, conDb, ClassDBUtils.ExecMode.Exception);
                                
                                }

                                sSQL_Text = "  select nzp_cnttype from  " + finder.bank + "_kernel" + tableDelimiter + "s_counttypes where name_type='" + Convert.ToString(rr["counter_type"]).Trim() + "'";

                                DataTable dtnote3 = ClassDBUtils.OpenSQL(sSQL_Text, conDb, ClassDBUtils.ExecMode.Exception).GetData();
                                foreach (DataRow rr3 in dtnote3.Rows)
                                {
                                    ptype = Convert.ToString(rr3["nzp_cnttype"]);
                                };
                            };
                            #endregion проверяем наличие счетчика в _kernel"+tableDelimiter+"s_counttypes, если его нет - вставляем


                            #endregion Определить тип счетчика

                            #region Проверить есть ли счетчик для данного лицевого счета (результат имеем nzp_counter)

                            sSQL_Text = "  select nzp_counter  from  " + finder.bank + "_data" + tableDelimiter + "counters_spis where nzp=" + nzp_dom +
                                   " and nzp_serv =" + nzp_serv + " and  num_cnt= '" + Convert.ToString(rr["num_cnt"]).Trim() + "' and nzp_type= " + type_pu;

                            DataTable dtnote4 = ClassDBUtils.OpenSQL(sSQL_Text, conDb, ClassDBUtils.ExecMode.Exception).GetData();
                            foreach (DataRow rr4 in dtnote4.Rows)
                            {
                                pnzp_counter = Convert.ToString(rr4["nzp_counter"]);

                            };
                            #endregion Проверить есть ли счетчик для данного лицевого счета

                            MonitorLog.WriteLog(" start 9 ", MonitorLog.typelog.Info, true);
                            #region если нет (nzp_counter)- вставляем

                            //is_gkal: если 4, то 1, иначе 0. см. file_measures
                            int is_gkal;
                            if (Convert.ToInt32(rr["nzp_measure"]) == 4) is_gkal = 1;
                            else is_gkal = 0;

                            //в kod_pu записываем nzp_measure, чтобы не потерять информацию
                            int kod_pu = Convert.ToInt32(rr["nzp_measure"]);


                            string dat_prov;
                            if (rr["dat_prov"] != DBNull.Value) dat_prov = "'" + Convert.ToString(rr["dat_prov"]).Substring(0, 10) + "'";
                            else dat_prov = "null";

                            string dat_provnext;
                            if (rr["dat_provnext"] != DBNull.Value) dat_provnext = "'" + Convert.ToString(rr["dat_provnext"]).Substring(0, 10) + "'";
                            else dat_provnext = "null";
                            MonitorLog.WriteLog(" start 10 ", MonitorLog.typelog.Info, true);
                            if (pnzp_counter.Length == 0)
                            {
                                MonitorLog.WriteLog(" start 11 ", MonitorLog.typelog.Info, true);
                                string seq = Points.Pref + "_data" + tableDelimiter + "counters_spis_nzp_counter_seq";
                                string strNzp_cnt;
#if PG
                                strNzp_cnt = " nextval('" + seq + "') ";
#else
                                strNzp_cnt = seq + ".nextval ";
#endif

                                #region вставить счетчики -получить nzp_counter


                                sSQL_Text = " insert into " + finder.bank + "_data" + tableDelimiter + "counters_spis (" +
                                            " nzp_counter, nzp_type, nzp, nzp_serv, nzp_cnttype, num_cnt, is_gkal," +
                                            " kod_pu, kod_info, dat_prov, dat_provnext, dat_oblom, dat_poch, dat_close," +
                                            " comment, is_actual, nzp_cnt, nzp_user, dat_when, is_pl, cnt_ls, dat_block," +
                                            " user_block " +
                                            ")" +
                                            " values (" +
                                    //    " nzp_counter, nzp_type, nzp,      nzp_serv,       nzp_cnttype,               num_cnt,                            is_gkal," +
                                             strNzp_cnt + ", " + type_pu + " ,  " + nzp_dom + "," + nzp_serv + "," + ptype + ",'" + Convert.ToString(rr["num_cnt"]).Trim() + "', " + is_gkal + " ," +
                                    //  " kod_pu,         kod_info,          dat_prov,         dat_provnext,       dat_oblom, dat_poch, dat_close," +
                                             "       " + kod_pu + ",    0,       " + dat_prov + ", " + dat_provnext + ", null,        null,     null, " +
                                    //  " comment, is_actual, nzp_cnt, nzp_user, dat_when, is_pl, cnt_ls, dat_block," +
                                              " null,      1, " + pnzp_cnt + "," + finder.nzp_user + ",  " + DBManager.sCurDate + ",    null, 0,       null, " +
                                    //  " user_block " +
                                              " " + finder.nzp_file + " ) ";




                                ClassDBUtils.ExecSQL(sSQL_Text, conDb, ClassDBUtils.ExecMode.Exception);

                                decimal nzpcounter = GetSerialValue(conDb);
                                #endregion вставить счетчики

                                #region найти вставленное значение - контрольная проверка nzp_counter


                                sSQL_Text = "  select nzp_counter  from  " + finder.bank + "_data" + tableDelimiter + "counters_spis where nzp=" + nzp_dom +
                                                    " and nzp_serv =" + nzp_serv + " and  num_cnt= '" + Convert.ToString(rr["num_cnt"]).Trim() + "' and nzp_type = " + type_pu; // в зависимости от type_pu
                                                                                                                                                                                // 1-общедомовой, 2-групповой
                                DataTable dtnote5 = ClassDBUtils.OpenSQL(sSQL_Text, conDb, ClassDBUtils.ExecMode.Exception).GetData();
                                foreach (DataRow rr5 in dtnote5.Rows)
                                {
                                    pnzp_counter = Convert.ToString(rr5["nzp_counter"]); 

                                };

                               //pnzp_counter = Convert.ToString(nzpcounter);
                                #endregion найти вставленное значение
                                string dat_s = "'01." + finder.month.ToString("00") + "." + finder.year.ToString("0000") + "'";

                                #region Если счетчик считает только ОДН
                                if (is_odn == 1)
                                {
                                    // Вставить параметр 2068 , в prm_17
#if PG
                                    sSQL_Text = " insert into " + finder.bank + "_data" + tableDelimiter + "prm_17  (val_prm, nzp_prm, nzp, dat_s, dat_po,is_actual, user_del) values (  " +
                                                                                           " default,     2068  ," + pnzp_counter.ToString() + ",cast(" + dat_s + " as date),  cast('01.01.3000' as date),1," + finder.nzp_file + ")";
#else
                                    sSQL_Text = " insert into " + finder.bank + "_data" + tableDelimiter + "prm_17 (val_prm, nzp_prm, nzp, dat_s, dat_po,is_actual, user_del) values (  " +
                                                                                           " 1,     2068  ," + pnzp_counter.ToString() + "," + dat_s + ",  '01.01.3000',1," + finder.nzp_file + ")";
#endif
                                    ClassDBUtils.ExecSQL(sSQL_Text, conDb, ClassDBUtils.ExecMode.Exception);
                                }
                                #endregion Если счетчик считает только ОДН
                                MonitorLog.WriteLog(" start 12 ", MonitorLog.typelog.Info, true);
                            }
                            #endregion если нет (nzp_counter)- вставляем
                            MonitorLog.WriteLog(" start 13 ", MonitorLog.typelog.Info, true);
                            #region разбор показаний ОДПУ

                            Int32 pnzp_cr = 0;



                            sSQL_Text = " select a.id , a.nzp_file as nzp_file_p, a.id_odpu as id_odpu_p, " +
                                        " a.dat_uchet as dat_uchet_p, a.val_cnt as val_cnt_p, a.kod_serv as kod_serv_p" +
                                        "  from " + Points.Pref + DBManager.sUploadAliasRest + "file_odpu_p a" +
                                        " where a.nzp_file = " + finder.nzp_file + " and id_odpu = '" + Convert.ToString(rr["local_id"]).Trim() + "'";

                            DataTable dtnote_p = ClassDBUtils.OpenSQL(sSQL_Text, conDb, ClassDBUtils.ExecMode.Exception).GetData();

                            foreach (DataRow rr_p in dtnote_p.Rows)
                            {
                                string dat_uchet_p;
                                if (rr_p["dat_uchet_p"] != DBNull.Value) dat_uchet_p = "'" + Convert.ToString(rr_p["dat_uchet_p"]).Substring(0, 10) + "'";
                                else dat_uchet_p = "null";

                                #region проверяем наличие такой записи в counters

                                if (table == "counters_dom")
                                {
                                    sSQL_Text = " select  nzp_crd  from  " + finder.bank + "_data" + tableDelimiter +
                                                "counters_dom where nzp_dom = " + nzp_dom +
                                                "  and nzp_serv  = " + nzp_serv + " and  num_cnt= '" +
                                                Convert.ToString(rr["num_cnt"]).Trim() + "' and val_cnt= " +
                                                Convert.ToString(rr_p["val_cnt_p"]) +
                                                "  and dat_uchet = " + dat_uchet_p;

                                    DataTable dtnote6 = ClassDBUtils.OpenSQL(sSQL_Text, conDb, ClassDBUtils.ExecMode.Exception).GetData();
                                    foreach (DataRow rr6 in dtnote6.Rows)
                                    {
                                        pnzp_cr = Convert.ToInt32(rr6["nzp_crd"]);
                                        //  break;
                                    }
                                }

                                if (table == "counters_group")  
                                {
                                    sSQL_Text = " select  *  from  " + finder.bank + "_data" + tableDelimiter +
                                                "counters_group where val_cnt= " +
                                                Convert.ToString(rr_p["val_cnt_p"]) +
                                                " and dat_uchet = " + dat_uchet_p +
                                                " and nzp_counter = " + pnzp_counter;

                                    DataTable dtnote6 = ClassDBUtils.OpenSQL(sSQL_Text, conDb, ClassDBUtils.ExecMode.Exception).GetData();
                                    foreach (DataRow rr6 in dtnote6.Rows)
                                    {
                                        pnzp_cr = Convert.ToInt32(rr6["nzp_cg"]);
                                        //  break;
                                    }
                                }
                                
                                #endregion проверяем наличие такой записи в counters               // нет проверки по counters_link, ссылка на xxx_data.kvar по номеру ЛС

                                #region вставляем запись в counters в случае отсутствия
                                if (pnzp_cr == 0)
                                {
                                    if (table == "counters_dom") //если тип счетчика 1-общедомовой
                                    {
                                        sSQL_Text = " insert into  " + finder.bank + "_data" + tableDelimiter +
                                                    "counters_dom (" +
                                                    "  nzp_dom, nzp_serv, nzp_cnttype, num_cnt, " +
                                                    " dat_prov, dat_provnext, dat_uchet, val_cnt,  " +
                                                    " is_actual, nzp_user, dat_when, dat_close, cur_unl, nzp_wp," +
                                                    "  dat_oblom, dat_poch,  " +
                                                    " user_del, nzp_counter " +
                                                    " )" +
                                                    " values (" +
                                                    //" nzp_crd,      nzp_dom,        nzp_serv,        nzp_cnttype,                num_cnt, "+
                                                    "  " + nzp_dom + "," + nzp_serv + ",'" + ptype + "','" +
                                                    Convert.ToString(rr["num_cnt"]).Trim() + "'," +
                                                    // "          dat_prov,           dat_provnext,          dat_uchet,                     val_cnt,                 
                                                    " " + dat_prov + ", " + dat_provnext + "," + dat_uchet_p + "," +
                                                    Convert.ToString(rr_p["val_cnt_p"]) + "," +
                                                    //" is_actual,   nzp_user,        dat_when, dat_close, cur_unl, nzp_wp,"+
                                                    " 1," + finder.nzp_user + ", " + sCurDate + ", null, 1, 1 ," +
                                                    //" ist, dat_oblom, dat_poch, dat_del, " +
                                                    " null,null," +
                                                    //" user_del, nzp_counter, month_calc, dat_s, dat_po, dat_block, user_block "+
                                                    " " + finder.nzp_file + "," + pnzp_counter + " ) ";

                                        ClassDBUtils.ExecSQL(sSQL_Text, conDb, ClassDBUtils.ExecMode.Exception);

                                        //получить айди вставленного показания
                                        int nzp_key = GetSerialValue(conDb);

                                        #region Вставка перерасчетов в must_calc

                                        string table_name = "counters_dom";
                                        DbDisUtils u = new DbDisUtils(finder);
                                        u.InsertIntoMustCalc(finder, conDb, dat_uchet_p.Replace("'", ""), Convert.ToInt32(pnzp_counter), 0, nzp_serv, 1, nzp_dom, table_name);

                                        #endregion Вставка перерасчетов в must_calc

                                        //формируем объекты
                                        //var obj = new CounterValLight
                                        //{
                                        //    pref = finder.bank,
                                        //    nzp_user = finder.nzp_user,
                                        //    nzp_dom = nzp_dom,
                                        //    nzp_serv = nzp_serv,
                                        //    dat_uchet =
                                        //        Convert.ToDateTime(dat_uchet_p.Replace("\'", "")).ToString("MM-dd-yyyy"),
                                        //    nzp_cnttype = Convert.ToInt32(ptype),
                                        //    val_cnt = Convert.ToDecimal(rr_p["val_cnt_p"]),
                                        //    val_cnt_s = Convert.ToString(rr_p["val_cnt_p"]),
                                        //    nzp_counter = Convert.ToInt32(pnzp_counter),
                                        //    ist = 1,
                                        //    nzp_key = nzp_key,
                                        //    nzp_type = 1
                                        //};

                                        //listCtr.Add(obj);
                                    }

                                    if (table == "counters_group")    //если тип счетчика 2-групповой
                                    {
                                        sSQL_Text = " insert into  " + finder.bank + "_data" + tableDelimiter +
                                                    " counters_group (" +
                                                    " dat_uchet, val_cnt, is_uchet_ls, is_gkal, is_pl," +
                                                    " is_doit, is_actual, nzp_user, dat_when, cur_unl, " +
                                                    " user_del, nzp_counter " +
                                                    " )" +
                                                    " values (" +
                                                    dat_uchet_p + "," + Convert.ToString(rr_p["val_cnt_p"]) + ", 0 , 1 , 0, " +
                                                    " 1, 1, " + finder.nzp_user + ", " + sCurDate + ", 1, " + 
                                                    finder.nzp_file + "," + pnzp_counter + " )";

                                        ClassDBUtils.ExecSQL(sSQL_Text, conDb, ClassDBUtils.ExecMode.Exception);

                                        //получить айди вставленного показания
                                        int nzp_key = GetSerialValue(conDb);

                                        #region Вставка перерасчетов в must_calc

                                        string table_name = "counters_group";
                                        DbDisUtils u = new DbDisUtils(finder);
                                        u.InsertIntoMustCalc(finder, conDb, dat_uchet_p.Replace("'", ""), Convert.ToInt32(pnzp_counter), 0, nzp_serv, 2, 0, table_name);

                                        #endregion Вставка перерасчетов в must_calc

                                        //формируем объекты
                                        //var obj = new CounterValLight
                                        //{
                                        //    pref = finder.bank,
                                        //    nzp_user = finder.nzp_user,
                                        //    nzp_dom = nzp_dom,
                                        //    nzp_serv = nzp_serv,
                                        //    dat_uchet =
                                        //        Convert.ToDateTime(dat_uchet_p.Replace("\'", "")).ToString("MM-dd-yyyy"),
                                        //    nzp_cnttype = Convert.ToInt32(ptype),
                                        //    val_cnt = Convert.ToDecimal(rr_p["val_cnt_p"]),
                                        //    val_cnt_s = Convert.ToString(rr_p["val_cnt_p"]),
                                        //    nzp_counter = Convert.ToInt32(pnzp_counter),
                                        //    ist = 1,
                                        //    nzp_key = nzp_key,
                                        //    nzp_type = 2
                                        //};

                                        //listCtr.Add(obj);
                                    }
                                }
                                #endregion вставляем запись в counters в случае отсутствия
                                
                            }
                            #endregion разбор показаний ОДПУ
                            MonitorLog.WriteLog(" start 14 ", MonitorLog.typelog.Info, true);
                            //Проставляем nzp_counter в fxxx_upload.file_odpu
                            sql =
                                " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "file_odpu " +
                                " SET nzp_counter = " + pnzp_counter +
                                " WHERE id = " + rr["id"] +
                                " AND nzp_file = " + rr["nzp_file"];
                            MonitorLog.WriteLog(" start 15 ", MonitorLog.typelog.Info, true);
                            ClassDBUtils.ExecSQL(sql, conDb, ClassDBUtils.ExecMode.Exception);
                            MonitorLog.WriteLog(" start 16 ", MonitorLog.typelog.Info, true);
                        }
                        #endregion разбираем счетчики ОДПУ, внутри разбираем показания этих счетчиков

                        //Добавление периодов перерасчета
                        //dbCntr.SaveCountersValsLight(listCtr);

                        #endregion Версия 1.2
                    }
                }

                #endregion Разбор когда ukas выставлен
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры insertOdpuFromFile " + "\n" + ex.Message + "\n" + ex.StackTrace, MonitorLog.typelog.Error, true);
                return new ReturnsType(false, "Ошибка выполнения процедуры insertOdpuFromFile ", -1);
            }
            return new ReturnsType(true, "Данные сохранены ", 1);
        }
    }
}
