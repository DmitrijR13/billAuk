using STCLINE.KP50.Global;
using STCLINE.KP50.IFMX.Kernel.source.CommonType;
using STCLINE.KP50.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;


namespace STCLINE.KP50.DataBase
{
    
    /// <summary>
    /// Расчет одного лицевого счета
    /// </summary>
    public class PrintInvoicesFonTask : BaseFonTask
    {
        public override Returns StartTask()
        {
            string pref = Points.GetPref(_task.nzpt);
            //выполнить контрольные проверки
            DbCharge dbCharge = new DbCharge();
            ReturnsType retType = dbCharge.MakeChecksBeforeCloseCalcMonth(new Finder {nzp_user = _task.nzp_user},
                new List<string>() {pref});
            dbCharge.Close();
            // retType.result = false; 
            if (!retType.result)
            {
                #region запись ошибок в текстовый файл

                StringBuilder sql = new StringBuilder();
                IDbConnection conn_db1 = GetConnection(Constants.cons_Kernel);
                string fileName = Constants.Directories.ReportDir + "//Протокол_ошибок_" +
                                  DateTime.Now.ToShortDateString() + "_" + DateTime.Now.Ticks;
                //постановка на поток
                ExcelRepClient excelRep = new ExcelRepClient();
                Returns ret = excelRep.AddMyFile(new ExcelUtility()
                {
                    nzp_user = _task.nzp_user,
                    status = ExcelUtility.Statuses.InProcess,
                    rep_name = "Протокол ошибок от " + DateTime.Now.ToShortDateString()
                });
                if (!ret.result) return ret; // return ret;
                var nzp_exc = ret.tag;

                ret = OpenDb(conn_db1, true);
                if (!ret.result) return ret; // return ret;

                MyDataReader reader;
                string ErrorText = "";
                sql.Append(" select dat_check, note, name_prov from ");
                sql.Append(Points.Pref + DBManager.sDataAliasRest + "checkchmon c ");
                sql.Append(" where c.status_=2 and c.dat_check='" + DateTime.Now.ToShortDateString() + "'");
                ret = ExecRead(conn_db1, out reader, sql.ToString(), true);
                if (!ret.result) return ret; // return ret;

                while (reader.Read())
                {
                    ErrorText += "Дата:" + reader["dat_check"].ToString().ToLower().Trim() + "  Текст ошибки:" +
                                 reader["note"].ToString().ToLower().Trim() + ".  " +
                                 reader["name_prov"].ToString().ToLower().Trim() + ".  Название проверки" +
                                 Environment.NewLine;
                }
                conn_db1.Close();
                StreamWriter sw = File.CreateText(fileName + ".txt");
                sw.Write(ErrorText);
                sw.Flush();
                sw.Close();


                //смена статуса
                IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
                ret = OpenDb(conn_web, true);
                if (!ret.result) return ret; // return ret;


                sql = new StringBuilder();
                sql.Append(" update " + sDefaultSchema + "excel_utility set stats = " + (int) ExcelUtility.Statuses.Success);
                sql.Append(", dat_out = " + Utils.EStrNull(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                if (fileName != "") sql.Append(", exc_path = '" + fileName + "'");
                sql.Append(" where nzp_exc =" + nzp_exc);
                ret = ExecSQL(conn_web, sql.ToString(), true);
                conn_web.Close();


                ret.result = retType.result;
                ret.text = retType.text;
                ret = excelRep.SetMyFileState(new ExcelUtility()
                {
                    nzp_exc = nzp_exc,
                    status = ExcelUtility.Statuses.Success,
                    exc_path = fileName,
                    nzp_user = _task.nzp_user,
                    rep_name = "Протокол ошибок от " + DateTime.Now.ToShortDateString()
                });


                //отметка об ошибочном выполнении вцелом
                ret.result = retType.result;
                //ret.text = ret.text;
                return ret; // return ret;

                #endregion
            }

            //выполняется подготовка данных
            bool fPrepareData = _task.nzp == 1 ? true : false;
            if (fPrepareData)
            {
                Returns rets = PrepaeDataForPrintInvoces(pref, _task.year_, _task.month_);
                if (!rets.result)
                {
                    return rets; // return rets;
                }
            }

            //передвинуть расчетный месяц банка
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            Returns ret1 = OpenDb(conn_db, true);
            if (!ret1.result) return ret1; // return ret1;
            dbCharge = new DbCharge();
            retType = dbCharge.CloseCalcMonth_actions(conn_db, pref, new Finder());
            if (!retType.result)
            {
                ret1.result = retType.result;
                ret1.text = retType.text;
                return ret1; // return ret1;
            }
            dbCharge.Close();
            conn_db.Close();

            //обновить список локальных банков
            DbSprav dbSprav = new DbSprav();
            bool res = dbSprav.PointLoad(GlobalSettings.WorkOnlyWithCentralBank, out ret1);
            dbSprav.Close();
            try
            {
                using (var db = new DbAdmin())
                {
                    var finder = new Finder();
                    finder.nzp_wp = _task.nzp_wp;
                    finder.pref = pref;
                    finder.nzp_user = _task.nzp_user;
                    //запись проводок
                    var retprov = db.GetProvForClosedMonth(finder);
                    if (!retprov.result)
                    {
                        MonitorLog.WriteLog("Ошибка записи проводок по банку данных:" + finder.pref, MonitorLog.typelog.Error,
                            true);
                    }
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка записи проводок по банку данных:" + pref, MonitorLog.typelog.Error, true);
                MonitorLog.WriteLog("Ошибка записи проводок по банку данных:" + ex.Message, MonitorLog.typelog.Error, true);
            }

            #region Добавление задачи на расчет

            Returns ret2;
            CalcOnFon(0, pref, true, out ret2);

            //if (ret2.result)
            //{
            //    try
            //    {
            //        using (var admin = new DbAdmin())
            //        {
            //            admin.InsertSysEvent(new SysEvents()
            //            {
            //                pref = pref,
            //                nzp_user = calcfon.nzp_user,
            //                nzp_dict = 6594,
            //                nzp_obj = 0,
            //                note = "Добавлена задача на расчет начислений банка данных " + Points.GetPoint(pref).point
            //            });
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
            //    }
            //}

            #endregion

            return ret2;
        }


        public void CalcOnFon(int _nzp_dom, string pref/*, int calc_yy, int calc_mm, int cur_yy, int cur_mm*/, bool reval, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            CalcFonTask.Types task = CalcFonTask.Types.taskFull;
            if (reval)
                task = CalcFonTask.Types.taskWithReval;

            CalcOnFon(_nzp_dom, pref/*, calc_yy, calc_mm, cur_yy, cur_mm*/, task, out ret);
        }

        public void CalcOnFon(int _nzp_dom, string pref /*, int calc_yy, int calc_mm, int cur_yy, int cur_mm*/,
            CalcFonTask.Types task, out Returns ret)
        {

            CalcOnFon(_nzp_dom, pref, 0, task, out ret);
        }


        public void CalcOnFon(int _nzp_dom, string pref, int nzp_user, CalcFonTask.Types task, out Returns ret)
        {
            ret = Utils.InitReturns();

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result)
            {
                return;
            }

            CalcFonTask calcfon = new CalcFonTask();
            calcfon.nzp = _nzp_dom;
            //calcfon.yy      = calc_yy;
            //calcfon.mm      = calc_mm;
            //calcfon.cur_yy  = cur_yy; //надо явно подставить тек. расчетный месяц, что случайно не испортить данные через базу!!!!
            //calcfon.cur_mm  = cur_mm;
            calcfon.TaskType = task;
            /*
            calcfon.yy      = 2012;
            calcfon.mm      = 2;
            calcfon.cur_yy  = 2012;
            calcfon.cur_mm  = 2;
            */

            int numTot = 0;
            int numSuccess = 0;

            if (true)
            {
                //сначала проверим, что нет ли выполняемых заданий
                //calcfon.status = FonTask.Statuses.InProcess; //контроль

                //foreach (_Point zap in Points.PointList)
                //{
                //    if (pref != "AllBases")
                //    {
                //        if (zap.pref != pref) continue;
                //        RecordMonth rm = Points.GetCalcMonth(new CalcMonthParams(zap.pref))
                //    }

                //    calcfon.nzpt   = zap.nzp_wp;
                //    calcfon.number = Points.GetCalcNum(zap.nzp_wp); //определить номер потока расчета

                //    CheckCalcAndPutInFon(conn_web, calcfon, out ret);
                //    if (!ret.result || ret.tag == Constants.workinfon)
                //    {
                //        //ошибка, либо есть задания в расчете
                //        conn_web.Close();
                //        return;
                //    }
                //}

                //затем выставим задание на расчет
                calcfon.Status = FonTask.Statuses.New; //на расчет

                DbAdminClient dba = new DbAdminClient();

                try
                {
                    foreach (_Point zap in Points.PointList)
                    {
                        numTot++;

                        if (pref != "AllBases")
                        {
                            if (zap.pref != pref) continue;
                        }

                        bool allow = dba.IsAllowCalcByPref(zap.pref, out ret);

                        if (!ret.result) return;

                        if (!allow)
                        {
                            MonitorLog.WriteLog("Задание на расчет начислений для банка данных " + pref + " не добавлено. Операция не разрешена.", MonitorLog.typelog.Warn, true);
                            continue;
                        }

                        RecordMonth rm = Points.GetCalcMonth(new CalcMonthParams(zap.pref));
                        calcfon.year_ = rm.year_;
                        calcfon.month_ = rm.month_;
                        calcfon.nzpt = zap.nzp_wp;
                        calcfon.QueueNumber = Points.GetCalcNum(zap.nzp_wp); //определить номер потока расчета
                        calcfon.nzp_user = nzp_user;

                        var db = new DbCalcQueueClient();
                        ret = db.AddTask(conn_web, null, calcfon);
                        db.Close();

                        if (!ret.result)
                        {
                            if (ret.tag == Constants.workinfon)
                            {
                                continue;
                            }
                            else
                            {
                                return;
                            }
                        }
                        numSuccess++;
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка CalcChargeDom " + ex.Message, MonitorLog.typelog.Error, 30, 301, true);
                }
                finally
                {
                    conn_web.Close();
                    dba.Close();
                }
            }
            else
            {
                //пока прямой расчет!
                //TestCalc0(_nzp_dom, pref, calc_yy, calc_mm, cur_yy, cur_mm, clc, out ret);

            }

            if (ret.result)
            {
                if (pref == "AllBases") ret.text = "Добавлено заданий на расчет начислений по " + numSuccess + " из " + numTot + " банков данных";
                else if (numSuccess > 0) ret.text = "Задание на расчет начислений добавлено";
                else ret.text = "Задание на расчет не добавлено";
                ret.tag = CalcFonTask.Types.taskWithRevalOntoListHouses == task ? ret.tag : -1;
            }
        }


        public Returns PrepaeDataForPrintInvoces(string pref, int year, int month)
        {
            string mo = month.ToString("00");
            //int Year = Convert.ToInt32(year);
            Returns ret = Utils.InitReturns();
            StringBuilder sql = new StringBuilder();
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;
            //sql.Append("select* from ");
            //sql.Append(pref  + "_charge_" + (Year - 2000).ToString("00") + DBManager.tableDelimiter + "charge_" +Month);

            try
            {


                #region проверка существования табл charge_XX_T

                sql.Append("select * from ");
                sql.Append(pref + "_charge_" + (year - 2000).ToString("00") +
                           DBManager.tableDelimiter + "charge_" + month.ToString("00") + "_T ");
                ret = ExecSQL(conn_db, sql.ToString(), false);
                if (ret.result)
                {
                    //таблица есть, удаляем все данные
                    sql.Remove(0, sql.Length);
                    sql.Append(" DELETE FROM ");
                    sql.Append(pref + "_charge_" + (year - 2000).ToString("00") +
                               DBManager.tableDelimiter + "charge_" + month.ToString("00") + "_T ");
                    ret = ExecSQL(conn_db, sql.ToString(), true);

                    //todo check result!
                    if (!ret.result)
                    {
                        conn_db.Close();
                        return ret;
                    }
                }
                else
                {
                    //таблицы нет, создаем таблицу
                    sql.Remove(0, sql.Length);
#if PG
                    sql.Append(" SET search_path TO  " + pref + "_charge_" + (year - 2000).ToString("00") + ";");

#else

                    sql.Append(" DATABASE  " + pref + "_charge_" + (year - 2000).ToString("00") + ";");
#endif
                    ret = ExecSQL(conn_db, sql.ToString(), true);


                    if (!ret.result)
                    {
                        conn_db.Close();
                        return ret;
                    }

                    sql.Remove(0, sql.Length);
                    sql.Append("CREATE TABLE charge_" + month.ToString("00") + "_T (");
                    sql.Append(
                        "nzp_charge SERIAL NOT NULL,   nzp_kvar INTEGER,   num_ls INTEGER,   nzp_serv INTEGER,   nzp_supp INTEGER,   nzp_frm INTEGER,   dat_charge DATE, ");
                    sql.Append(
                        "tarif DECIMAL(14,3),   tarif_p DECIMAL(14,3),   rsum_tarif DECIMAL(14,2),   rsum_lgota DECIMAL(14,2),   sum_tarif DECIMAL(14,2),   sum_dlt_tarif DECIMAL(14,2), ");
                    sql.Append(
                        " sum_dlt_tarif_p DECIMAL(14,2),   sum_tarif_p DECIMAL(14,2),   sum_lgota DECIMAL(14,2),   sum_dlt_lgota DECIMAL(14,2),   sum_dlt_lgota_p DECIMAL(14,2),");
                    sql.Append(
                        "  sum_lgota_p DECIMAL(14,2),   sum_nedop DECIMAL(14,2),   sum_nedop_p DECIMAL(14,2),   sum_real DECIMAL(14,2),   sum_charge DECIMAL(14,2),");
                    sql.Append(
                        "  reval DECIMAL(14,2),   real_pere DECIMAL(14,2),   sum_pere DECIMAL(14,2),   real_charge DECIMAL(14,2),   sum_money DECIMAL(14,2),   money_to DECIMAL(14,2),");
                    sql.Append(
                        "   money_from DECIMAL(14,2),   money_del DECIMAL(14,2),   sum_fakt DECIMAL(14,2),   fakt_to DECIMAL(14,2),   fakt_from DECIMAL(14,2),   fakt_del DECIMAL(14,2),");
                    sql.Append(
                        "   sum_insaldo DECIMAL(14,2),   izm_saldo DECIMAL(14,2),   sum_outsaldo DECIMAL(14,2),   isblocked INTEGER,   is_device INTEGER default 0,   c_calc DECIMAL(14,2),");
                    sql.Append(
                        "   c_sn DECIMAL(14,2) default 0.00,   c_okaz DECIMAL(14,2),   c_nedop DECIMAL(14,2),   isdel INTEGER,   c_reval DECIMAL(14,2),  ");
                    sql.Append(
                        "     tarif_f DECIMAL(14,3),      sum_tarif_sn_eot DECIMAL(14,2) default 0.00,");
                    sql.Append(
                        "  sum_tarif_sn_f DECIMAL(14,2) default 0.00,     sum_subsidy DECIMAL(14,2) default 0.00,   sum_subsidy_p DECIMAL(14,2) default 0.00,");
                    sql.Append(
                        "   sum_subsidy_reval DECIMAL(14,2) default 0.00,   sum_subsidy_all DECIMAL(14,2) default 0.00,   ");
                    sql.Append(
                        "    tarif_f_p DECIMAL(14,3),    ");
                    sql.Append(
                        "   sum_tarif_sn_f_p DECIMAL(14,2) default 0.00,  ");
                    sql.Append(
                        "  order_print INTEGER default 0,   sum_tarif_f DECIMAL(14,2) default 0.00 NOT NULL,     gsum_tarif DECIMAL(14,2) default 0.00 NOT NULL)");
                    ret = ExecSQL(conn_db, sql.ToString(), true);
                    if (!ret.result)
                    {
                        conn_db.Close();
                        return ret;
                    }

                }

                #endregion

                #region проверка существования табл to_supplierXX
                sql.Remove(0, sql.Length);
                sql.Append("select * from ");
                sql.Append(pref + "_charge_" + (year - 2000).ToString("00") +
                           DBManager.tableDelimiter + "to_supplier" + month.ToString("00"));
                ret = ExecSQL(conn_db, sql.ToString(), false);
                if (ret.result)
                {
                    //таблица есть, удаляем все данные
                    sql.Remove(0, sql.Length);
                    sql.Append(" DELETE FROM ");
                    sql.Append(pref + "_charge_" + (year - 2000).ToString("00") +
                               DBManager.tableDelimiter + "to_supplier" + month.ToString("00"));
                    ret = ExecSQL(conn_db, sql.ToString(), true);

                    if (!ret.result)
                    {
                        conn_db.Close();
                        return ret;
                    }
                }
                else
                {
                    //таблицы нет, создаем таблицу
                    sql.Remove(0, sql.Length);

#if PG
                    sql.Append(" SET search_path TO  " + pref + "_charge_" + (year - 2000).ToString("00") + ";");
#else
         sql.Append(" DATABASE  " + pref + "_charge_" + (year - 2000).ToString("00") + ";");
#endif
                    ret = ExecSQL(conn_db, sql.ToString(), true);
                    if (!ret.result)
                    {
                        conn_db.Close();
                        return ret;
                    }
                    sql.Remove(0, sql.Length);
                    sql.Append(" CREATE TABLE to_supplier" + month.ToString("00") + " ( ");
                    sql.Append(
                        " nzp_to SERIAL NOT NULL,   nzp_serv INTEGER,   nzp_supp INTEGER,   nzp_pack_ls INTEGER,   nzp_charge INTEGER,   num_charge SMALLINT,   num_ls INTEGER, ");
                    sql.Append(
                        "sum_prih FLOAT,   kod_sum SMALLINT,   dat_month DATE,   dat_prih DATE,   dat_uchet DATE,   dat_plat DATE,   s_user FLOAT,   s_dolg FLOAT,   s_forw FLOAT) ");
                    // sql.Append("nzp_rs INTEGER default 1 NOT NULL) ");
                    ret = ExecSQL(conn_db, sql.ToString(), true);

                    if (!ret.result)
                    {
                        conn_db.Close();
                        return ret;
                    }
                }

                #endregion

                #region вставка в charge_XX_T

                sql.Remove(0, sql.Length);
                sql.Append("insert into " + pref + "_charge_" + (year - 2000).ToString("00") +
                           DBManager.tableDelimiter + "charge_" + month.ToString("00") + "_T ( ");
                sql.Append("nzp_charge  , nzp_kvar  , num_ls  , nzp_serv  , nzp_supp  , nzp_frm  , dat_charge, ");
                sql.Append("tarif  , tarif_p  , rsum_tarif  , rsum_lgota  , sum_tarif  , sum_dlt_tarif  , ");
                sql.Append(" sum_dlt_tarif_p  , sum_tarif_p  , sum_lgota  , sum_dlt_lgota  , sum_dlt_lgota_p  ,");
                sql.Append("  sum_lgota_p  , sum_nedop  , sum_nedop_p  , sum_real  , sum_charge  ,");
                sql.Append("  reval  , real_pere  , sum_pere  , real_charge  , sum_money  , money_to  ,");
                sql.Append(" money_from  , money_del  , sum_fakt  , fakt_to  , fakt_from  , fakt_del  ,");
                sql.Append(" sum_insaldo  , izm_saldo  , sum_outsaldo  , isblocked  , is_device  , c_calc  ,");
                sql.Append(" c_sn  , c_okaz  , c_nedop  , isdel  , c_reval  ,  ");
                sql.Append("    tarif_f  ,   sum_tarif_sn_eot  ,");
                sql.Append("  sum_tarif_sn_f  ,   sum_subsidy  , sum_subsidy_p  , ");
                sql.Append(" sum_subsidy_reval  , sum_subsidy_all  ,    ");
                sql.Append("  tarif_f_p  ,  ");
                sql.Append(" sum_tarif_sn_f_p  ,    ");
                sql.Append("  order_print  , sum_tarif_f  ,   gsum_tarif  )");

                sql.Append("select ");
                sql.Append(" nzp_charge  , nzp_kvar  , num_ls  , nzp_serv  , nzp_supp  , nzp_frm  , dat_charge , ");
                sql.Append(" tarif  , tarif_p  , rsum_tarif  , rsum_lgota  , sum_tarif  , sum_dlt_tarif  , ");
                sql.Append(" sum_dlt_tarif_p  , sum_tarif_p  , sum_lgota  , sum_dlt_lgota  , sum_dlt_lgota_p  ,");
                sql.Append("  sum_lgota_p  , sum_nedop  , sum_nedop_p  , sum_real  , sum_charge  ,");
                sql.Append("  reval  , real_pere  , sum_pere  , real_charge  , sum_money  , money_to  ,");
                sql.Append(" money_from  , money_del  , sum_fakt  , fakt_to  , fakt_from  , fakt_del  ,");
                sql.Append(" sum_insaldo  , izm_saldo  , sum_outsaldo  , isblocked  , is_device  , c_calc  ,");
                sql.Append(" c_sn  , c_okaz  , c_nedop  , isdel  , c_reval , ");
                sql.Append("    tarif_f  ,   sum_tarif_sn_eot  ,");
                sql.Append("  sum_tarif_sn_f  ,  sum_subsidy  , sum_subsidy_p  ,");
                sql.Append(" sum_subsidy_reval  , sum_subsidy_all  ,    ");
                sql.Append("   tarif_f_p  , ");
                sql.Append(" sum_tarif_sn_f_p  ,  ");
                sql.Append("  order_print  , sum_tarif_f  ,   gsum_tarif  ");
                sql.Append(" from ");
                sql.Append(pref + "_charge_" + (year - 2000).ToString("00") +
                           DBManager.tableDelimiter + "charge_" + month.ToString("00"));
                ret = ExecSQL(conn_db, sql.ToString(), true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return ret;
                }
                DBManager.CreateIndexIfNotExists(conn_db, "ix_t_ch" + mo + "_supp", pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter + "charge_" + month.ToString("00") + "_T", "nzp_supp", true, null);
                DBManager.CreateIndexIfNotExists(conn_db, "ix_t_kvsrdt_ch" + mo, pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter + "charge_" + month.ToString("00") + "_T", "nzp_kvar, nzp_serv, dat_charge", true, null);
                DBManager.CreateIndexIfNotExists(conn_db, "ix_t_kvsrv_ch" + mo, pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter + "charge_" + month.ToString("00") + "_T", "nzp_kvar, nzp_serv, nzp_supp", true, null);
                DBManager.CreateIndexIfNotExists(conn_db, "ix_t_kvsup_ch" + mo, pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter + "charge_" + month.ToString("00") + "_T", "nzp_kvar, nzp_supp, dat_charge", true, null);
                DBManager.CreateIndexIfNotExists(conn_db, "ix_t_ls_ch" + mo, pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter + "charge_" + month.ToString("00") + "_T", "num_ls", true, null);
                DBManager.CreateIndexIfNotExists(conn_db, "ix_t_lssrv_ch" + mo, pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter + "charge_" + month.ToString("00") + "_T", "num_ls, nzp_serv, nzp_supp", true, null);
                DBManager.CreateIndexIfNotExists(conn_db, "ix_t_nzpch_ch" + mo, pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter + "charge_" + month.ToString("00") + "_T", "nzp_charge", true, null);
                DBManager.CreateIndexIfNotExists(conn_db, "ix_t_ser_ch" + mo + "_supp", pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter + "charge_" + month.ToString("00") + "_T", "nzp_serv", true, null);

                // sql.Remove(0, sql.Length);
                // sql.Append("create index tmp_charge_subs_2 on t_charge (pref)");            




                sql.Remove(0, sql.Length);
                sql.Append(DBManager.sUpdStat + " " + pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter + "charge_" + month.ToString("00") + "_T");
                ret = ExecSQL(conn_db, sql.ToString(), true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return ret;
                }

                #endregion

                #region вставка в to_supplierXX

                sql.Remove(0, sql.Length);
                sql.Append("insert into ");
                sql.Append(pref + "_charge_" + (year - 2000).ToString("00"));
                sql.Append(DBManager.tableDelimiter + "to_supplier" + month.ToString("00") + " (");
                sql.Append(" nzp_to, nzp_serv, nzp_supp , nzp_pack_ls , nzp_charge, num_charge , num_ls , ");
                sql.Append(
                    "sum_prih, kod_sum , dat_month, dat_prih , dat_uchet, dat_plat,s_user, s_dolg ,s_forw )");
                //sql.Append("nzp_rs ) ");
                sql.Append(" select ");
                sql.Append(" nzp_to, nzp_serv, nzp_supp , nzp_pack_ls , nzp_charge ,  num_charge , num_ls , ");
                sql.Append("sum_prih, kod_sum , dat_month, dat_prih , dat_uchet, dat_plat, s_user, s_dolg , s_forw ");
                //sql.Append("nzp_rs  ");
                sql.Append(" FROM " + pref + "_charge_" + (year - 2000).ToString("00"));
                sql.Append(DBManager.tableDelimiter + "fn_supplier" + month.ToString("00") + " ");
                ret = ExecSQL(conn_db, sql.ToString(), true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return ret;
                }


                DBManager.CreateIndexIfNotExists(conn_db, "ix_t_fnts" + mo + "_1", pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter + "to_supplier" + mo, "nzp_to", true, null);
                DBManager.CreateIndexIfNotExists(conn_db, "ix_t_fnts" + mo + "_2", pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter + "to_supplier" + mo, "nzp_serv", true, null);
                DBManager.CreateIndexIfNotExists(conn_db, "ix_t_fnts" + mo + "_3", pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter + "to_supplier" + mo, "dat_uchet, nzp_supp, num_ls, sum_prih", true, null);
                DBManager.CreateIndexIfNotExists(conn_db, "ix_t_fnts" + mo + "_4", pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter + "to_supplier" + mo, "nzp_supp", true, null);
                DBManager.CreateIndexIfNotExists(conn_db, "ix_t_fnts" + mo + "_5", pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter + "to_supplier" + mo, "num_ls", true, null);
                DBManager.CreateIndexIfNotExists(conn_db, "ix_t_fnts" + mo + "_6", pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter + "to_supplier" + mo, "dat_prih", true, null);
                DBManager.CreateIndexIfNotExists(conn_db, "ix_t_fnts" + mo + "_7", pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter + "to_supplier" + mo, "dat_uchet", true, null);
                DBManager.CreateIndexIfNotExists(conn_db, "ix_t_fnts" + mo + "_8", pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter + "to_supplier" + mo, "num_ls, nzp_serv, dat_uchet", true, null);
                DBManager.CreateIndexIfNotExists(conn_db, "ix_t_fnts" + mo + "_80", pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter + "to_supplier" + mo, "num_ls, nzp_serv, nzp_supp, dat_uchet", true, null);
                DBManager.CreateIndexIfNotExists(conn_db, "ix_t_fnts" + mo + "_9", pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter + "to_supplier" + mo, "nzp_pack_ls", true, null);

                sql.Remove(0, sql.Length);
                sql.Append(DBManager.sUpdStat + " " + pref + "_charge_" + (year - 2000).ToString("00") + DBManager.tableDelimiter + "to_supplier" + mo + "; ");
                ret = ExecSQL(conn_db, sql.ToString(), true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return ret;
                }

                #endregion

                return ret;
            }
            catch
            {
                //todo add code 
                ret.result = false;
                return ret;
            }
            finally
            {
                conn_db.Close();
            }
        }
        public PrintInvoicesFonTask(CalcFonTask task)
            : base(task)
        {
            
        }

    }
}
