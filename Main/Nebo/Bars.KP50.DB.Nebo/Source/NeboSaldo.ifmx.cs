using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.EPaspXsd;
using System.Data;
using STCLINE.KP50.Global;
using STCLINE.KP50.Utility;
using STCLINE.KP50.IFMX.Server.SOURCE.NEBO;

namespace STCLINE.KP50.DataBase
{

    public class DbNeboSaldo : DbBase
    {
        public DbNeboSaldo()
            : base()
        {

        }


        public Returns CreateReestrNebo(int nzp_type, string dat_s, string dat_po)
        {
            Returns ret = Utils.InitReturns();
            IDbConnection conn_db = null;
            #region Коннект к базе
            conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;
            #endregion
            StringBuilder sql = new StringBuilder();
            List<_Point> prefixs = new List<_Point>();
            prefixs = Points.PointList;

            DateTime pred_month_calc = new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1).AddMonths(-1); //предыдущий рассчетный месяц
            int pred_year = pred_month_calc.Year;
            string yy = (pred_year - 2000).ToString("00");
            #region Выгрузка (сальдо по населению и арендаторам)||(поступления оплат)



            var DT = new DataTable();
            List<int> area = new List<int>();
            foreach (var point in prefixs)
            {
                sql.Remove(0, sql.Length);
                sql.Append(" select a.nzp_area from " + point.pref + "_data:s_area a, " + point.pref + "_data:prm_7 p, " + Points.Pref + "_data:nebo_reestr n ");
                sql.Append(" where a.nzp_area=p.nzp and p.val_prm='1' and p.is_actual<>100 ");
                sql.Append(" and (n.nzp_area<>a.nzp_area or n.dat_charge<>'" + pred_month_calc.ToString("dd.MM.yyyy") + "' or n.type_reestr<>" + nzp_type + ") ");
                DT.Merge(ClassDBUtils.OpenSQL(sql.ToString(), conn_db).resultData);
            }
            for (int i = 0; i < DT.Rows.Count; i++)
            {
                int nzp_area = Convert.ToInt32(DT.Rows[i]["nzp_area"]);
                area.Add(nzp_area);
            }
            area = area.Distinct().ToList(); // получаем уникальные nzp_area
            for (int i = 0; i < area.Count; i++)
            {
                //Создаем отдельные выгрузки сальдо для каждой выбранной УК 
                int nzp_area = area[i];

                sql.Remove(0, sql.Length);
                //для сальдо
                if (nzp_type == Convert.ToInt32(NeboReestr.NeboReestrTypes.Saldo))
                {
                    sql.Append(" insert into " + Points.Pref + "_data:nebo_reestr (type_reestr, dat_charge, is_prepare, nzp_area) values");
                    sql.Append(" (" + Convert.ToInt32(NeboReestr.NeboReestrTypes.Saldo) + ",'" + pred_month_calc.ToString("dd.MM.yyyy") + "', " + Convert.ToInt32(NeboReestr.NeboReestrStatuses.OnStart) + " ");
                }
                //для поступлений
                if (nzp_type == Convert.ToInt32(NeboReestr.NeboReestrTypes.Income))
                {
                    sql.Append(" insert into " + Points.Pref + "_data:nebo_reestr (type_reestr, dat_oper,dat_charge, is_prepare, nzp_area) values");
                    sql.Append(" (" + Convert.ToInt32(NeboReestr.NeboReestrTypes.Income) + ",'" + dat_s + "', '" + pred_month_calc.ToString("dd.MM.yyyy") + "'," + Convert.ToInt32(NeboReestr.NeboReestrStatuses.OnStart) + " ");
                }
                    sql.Append(" ," + nzp_area + " ) ");
                    if (!ExecSQL(conn_db, sql.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("CreateReestrNebo: Ошибка записи реестра в nebo_reestr, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);
                        ret.text = "Ошибка записи в реестр";
                        ret.result = false;
                        return ret;
                    }
                    int nzp_nebo_reestr = GetSerialValue(conn_db);

                    // постановка на выполнение --временно
                    sql.Remove(0, sql.Length);
                    sql.Append("update " + Points.Pref + "_data:nebo_reestr set (is_prepare)=(2) where nzp_nebo_reestr =" + nzp_nebo_reestr + "");
                    if (!ExecSQL(conn_db, sql.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("CreateReestrNebo: Ошибка обновления статуса реестра в nebo_reestr, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);
                        ret.result = false;
                        return ret;
                    }
                    if (nzp_type == Convert.ToInt32(NeboReestr.NeboReestrTypes.Saldo))
                    {
                        foreach (var point in prefixs)
                        {
                            ExecSQL(conn_db, "drop table tmp_nebo", false);
                            sql.Remove(0, sql.Length);
                            sql.Append(" select kv.nzp_kvar, kv.nzp_dom from " + point.pref + "_data:kvar kv ");
                            sql.Append(" where kv.nzp_area=" + nzp_area + " into temp tmp_nebo with no log;  ");
                            if (!ExecSQL(conn_db, sql.ToString(), true).result)
                            {
                                MonitorLog.WriteLog("InsertSaldoHouse: Ошибка записи данных в tmp_nebo, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);
                                ret.result = false;
                                return ret;
                            }

                            ExecSQL(conn_db, "create index ix_1_tmp_nebo on tmp_nebo(nzp_kvar,nzp_dom);", false);
                            ExecSQL(conn_db, "update statistics for table tmp_nebo; ", false);

                            //пишем в nebo_dom
                            sql.Remove(0, sql.Length);
                            sql.Append(" insert into " + Points.Pref + "_fin_" + yy + ":nebo_dom ");
                            sql.Append(" (nzp_nebo_reestr, nzp_area, nzp_dom, pref) ");
                            sql.Append(" select " + nzp_nebo_reestr + ", " + nzp_area + ", t.nzp_dom, '" + point.pref + "' from  tmp_nebo t ");
                            sql.Append(" group by t.nzp_dom ");
                            if (!ExecSQL(conn_db, sql.ToString(), true).result)
                            {
                                MonitorLog.WriteLog("InsertSaldoHouse: Ошибка записи данных в nebo_dom, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);
                                ret.result = false;
                                return ret;
                            }
                        }

                        ret = InsertSaldoHouse(conn_db, nzp_nebo_reestr, nzp_area);
                        ret = InsertSaldoArend(conn_db, nzp_nebo_reestr, nzp_area);
                    }
                    if(nzp_type==Convert.ToInt32(NeboReestr.NeboReestrTypes.Income))
                    {
                         FillSumOplForNebo(conn_db, nzp_nebo_reestr, nzp_area, dat_s, dat_po, out ret);
                    }
                    if (!ret.result)
                    {
                        // не успешно
                        sql.Remove(0, sql.Length);
                        sql.Append("update " + Points.Pref + "_data:nebo_reestr set (is_prepare)=(-1) where nzp_nebo_reestr =" + nzp_nebo_reestr + "");
                        if (!ExecSQL(conn_db, sql.ToString(), true).result)
                        {
                            MonitorLog.WriteLog("CreateReestrNebo: Ошибка обновления статуса реестра в nebo_reestr, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);
                            ret.result = false;
                            return ret;
                        }
                    }
                    else
                    {
                        //успешно
                        sql.Remove(0, sql.Length);
                        sql.Append("update " + Points.Pref + "_data:nebo_reestr set (is_prepare,dat_prepare,is_notify)=(1, '" + DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss") + "',0) ");
                        sql.Append("where nzp_nebo_reestr =" + nzp_nebo_reestr + "");
                        if (!ExecSQL(conn_db, sql.ToString(), true).result)
                        {
                            MonitorLog.WriteLog("CreateReestrNebo: Ошибка обновления статуса реестра в nebo_reestr, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);
                            ret.result = false;
                            return ret;
                        }

                        //временно
                        if (nzp_type == Convert.ToInt32(NeboReestr.NeboReestrTypes.Saldo))
                        {
                            ret = NumRows(conn_db, "nzp_nebo_rsaldo", 500, Points.Pref + "_fin_" + yy + ":nebo_rsaldo", "nzp_nebo_reestr=" + nzp_nebo_reestr + "");
                        }
                        if (nzp_type == Convert.ToInt32(NeboReestr.NeboReestrTypes.Income))
                        {
                            ret = NumRows(conn_db, "nzp_nebo_rfnsupp", 500, Points.Pref + "_fin_" + yy + ":nebo_rfnsupp", "nzp_nebo_reestr=" + nzp_nebo_reestr + "");
                        }
                    }
               

            #endregion

                
            }
            return ret;
        }
        /// <summary>
        /// Запись сальдо в реестр по населению
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="nzp_nebo_reestr"></param>
        /// <returns></returns>
        public Returns InsertSaldoHouse(IDbConnection conn_db, int nzp_nebo_reestr, int nzp_area)
        {
            Returns ret = Utils.InitReturns();
            StringBuilder sql = new StringBuilder();
            DateTime pred_month_calc = new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1).AddMonths(-1); //предыдущий рассчетный месяц
            List<_Point> prefixs = new List<_Point>();
            int pred_year = pred_month_calc.Year;
            string yy = (pred_year - 2000).ToString("00");
            string mm = pred_month_calc.Month.ToString("00");
            prefixs = Points.PointList;

            try
            {
                //для населения typek=1
                foreach (var point in prefixs)
                {
                    ExecSQL(conn_db, "drop table tmp_nebo_saldo", false);

                    sql.Remove(0, sql.Length);
                    sql.Append(" select kv.nzp_kvar, kv.nzp_dom from " + point.pref + "_data:kvar kv ");
                    sql.Append(" where kv.typek=1 and kv.nzp_area="+nzp_area+" into temp  tmp_nebo_saldo with no log;  ");
                    if (!ExecSQL(conn_db, sql.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("InsertSaldoHouse: Ошибка записи данных в nebo_rsaldo, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);
                        ret.result = false;
                        return ret;
                    }
                    
                    ExecSQL(conn_db, "create index ix_1_nebo_saldo on tmp_nebo_saldo(nzp_kvar,nzp_dom);", false);
                    ExecSQL(conn_db, "update statistics for table tmp_nebo_saldo; ", false);                                       

                    sql.Remove(0, sql.Length);
                    sql.Append(" insert into " + Points.Pref + "_fin_" + yy + ":nebo_rsaldo (nzp_nebo_reestr, dat_charge,nzp_area,typek,  ");
                    sql.Append(" nzp_dom, nzp_serv, nzp_supp, sum_insaldo, sum_real, reval, izm_saldo, sum_money, sum_charge, sum_outsaldo)  ");
                    sql.Append(" select  " + nzp_nebo_reestr + ", ");
                    sql.Append(" '" + pred_month_calc.ToString("dd.MM.yyyy") + "',"+nzp_area+", 1, t.nzp_dom, ch.nzp_serv, ch.nzp_supp, sum(ch.sum_insaldo), ");
                    sql.Append(" sum(ch.sum_real), sum(ch.reval), sum(ch.izm_saldo), sum(ch.sum_money),  ");
                    sql.Append(" sum(ch.sum_charge), sum(ch.sum_outsaldo) ");
                    sql.Append(" from  tmp_nebo_saldo t , " + point.pref + "_charge_" + yy + ":charge_" + mm + " ch  ");
                    sql.Append(" where  ch.nzp_kvar=t.nzp_kvar and ch.nzp_serv>1 and ch.dat_charge is null ");
                    //sql.Append(" and abs(ch.sum_insaldo+ch.sum_real+ ch.reval+ch.izm_saldo+ch.sum_money+ch.sum_charge+ch.sum_outsaldo)>0  ");
                    sql.Append(" group by  t.nzp_dom, ch.nzp_serv, ch.nzp_supp  ");
                    if (!ExecSQL(conn_db, sql.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("InsertSaldoHouse: Ошибка записи данных в nebo_rsaldo, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);
                        ret.result = false;
                        return ret;
                    }
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("InsertSaldoHouse:  " + ex.Message, MonitorLog.typelog.Error, true);
                ret.result = false;
                return ret;
            }

            return ret;
        }

        /// <summary>
        /// Запись сальдо в реестр по арендаторам
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="nzp_nebo_reestr"></param>
        /// <returns></returns>
        public Returns InsertSaldoArend(IDbConnection conn_db, int nzp_nebo_reestr, int nzp_area)
        {
            Returns ret = Utils.InitReturns();
            StringBuilder sql = new StringBuilder();
            DateTime pred_month_calc = new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1).AddMonths(-1); //предыдущий рассчетный месяц
            List<_Point> prefixs = new List<_Point>();
            int pred_year = pred_month_calc.Year;
            string yy = (pred_year - 2000).ToString("00");
            string mm = pred_month_calc.Month.ToString("00");
            prefixs = Points.PointList;

            try
            {
                //для арендаторов typek!=1
                foreach (var point in prefixs)
                {
                    ExecSQL(conn_db, "drop table tmp_nebo_saldo_ar", false);

                    sql.Remove(0, sql.Length);
                    sql.Append(" select kv.nzp_kvar, kv.nzp_dom,kv.typek, kv.num_ls, kv.pkod from " + point.pref + "_data:kvar kv ");
                    sql.Append(" where  kv.typek<>1 and kv.nzp_area="+nzp_area+" into temp  tmp_nebo_saldo_ar with no log;  ");                  
                    if (!ExecSQL(conn_db, sql.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("InsertSaldoArend: Ошибка записи данных в nebo_rsaldo, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);
                        ret.result = false;
                        return ret;
                    }

                    ExecSQL(conn_db, "create index ix_2_nebo_saldo_ar on tmp_nebo_saldo_ar(nzp_kvar, nzp_dom,typek, kv.num_ls, pkod);", false);
                    ExecSQL(conn_db, "update statistics for table tmp_nebo_saldo_ar; ", false);

                 

                    sql.Remove(0, sql.Length);
                    sql.Append(" insert into " + Points.Pref + "_fin_" + yy + ":nebo_rsaldo (nzp_nebo_reestr, dat_charge,nzp_area, typek,num_ls,pkod,  ");
                    sql.Append(" nzp_dom, nzp_serv, nzp_supp, sum_insaldo, sum_real, reval, izm_saldo, sum_money, sum_charge, sum_outsaldo)  ");
                    sql.Append(" select  " + nzp_nebo_reestr + ", ");
                    sql.Append(" '" + pred_month_calc.ToString("dd.MM.yyyy") + "',"+nzp_area+", t.typek, t.num_ls,t.pkod, t.nzp_dom, ch.nzp_serv, ch.nzp_supp, sum(ch.sum_insaldo), ");
                    sql.Append(" sum(ch.sum_real), sum(ch.reval), sum(ch.izm_saldo), sum(ch.sum_money),  ");
                    sql.Append(" sum(ch.sum_charge), sum(ch.sum_outsaldo) ");
                    sql.Append(" from  tmp_nebo_saldo_ar t , " + point.pref + "_charge_" + yy + ":charge_" + mm + " ch  ");
                    sql.Append(" where  ch.nzp_kvar=t.nzp_kvar and ch.nzp_serv>1 and ch.dat_charge is null  ");
                    //sql.Append(" and abs(ch.sum_insaldo+ch.sum_real+ ch.reval+ch.izm_saldo+ch.sum_money+ch.sum_charge+ch.sum_outsaldo)>0  ");
                    sql.Append(" group by  t.typek, t.num_ls,t.pkod,t.nzp_dom, ch.nzp_serv, ch.nzp_supp  ");
                    if (!ExecSQL(conn_db, sql.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("InsertSaldoArend: Ошибка записи данных в nebo_rsaldo, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);
                        ret.result = false;
                        return ret;
                    }
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("InsertSaldoArend:  " + ex.Message, MonitorLog.typelog.Error, true);
                ret.result = false;
                return ret;
            }

            return ret;
        }

        /// <summary>
        /// Нумерация строк таблицы по страницам 
        /// </summary>
        /// <param name="conn_db">IDbConnection</param>
        /// <param name="name_serial">наименование сериального поля в </param>
        /// <param name="size_page">Размер страниц</param>
        /// <param name="name_table">Имя таблицы вместе с префиксами</param>
        /// <param name="dop_param">Доп.параметры, nzp_nebo_reestr в том числе</param>
        /// <returns>ret.tag = общее кол-во страниц</returns>
        public Returns NumRows(IDbConnection conn_db, string name_serial, int size_page, string name_table, string dop_param)
        {
            Returns ret = Utils.InitReturns();
            StringBuilder sql = new StringBuilder();

            //дополнительный параметрыы
            string extraWhere = (String.IsNullOrEmpty(dop_param)) ? "" : " WHERE " + dop_param.Trim();
            string extraAnd = (String.IsNullOrEmpty(dop_param)) ? "" : " AND " + dop_param.Trim();

            //достаем минимум и максимум serial in table
            sql.Append(" SELECT MIN(" + name_serial + ") AS _min, MAX(" + name_serial + ") AS _max ");
            sql.Append(" FROM  " + name_table + extraWhere);
            var DT = ClassDBUtils.OpenSQL(sql.ToString(), conn_db);
            if (DT.resultCode == -1)
            {
                MonitorLog.WriteLog("NumRows: Ошибка получения верхнего и нижнего значений:" + sql.ToString(), MonitorLog.typelog.Error, true);
                ret.result = false;
                return ret;
            }
            if(DT.resultData.Rows[0]["_min"]==DBNull.Value || DT.resultData.Rows[0]["_max"]==DBNull.Value)
            {
                //кол-во записанных строк для данного  реестра =0
                return ret;
            }
            int min = Convert.ToInt32(DT.resultData.Rows[0]["_min"]);
            int max = Convert.ToInt32(DT.resultData.Rows[0]["_max"]);

            int cur_val = min;
            int count_page = 0;


            while (cur_val <= max)
            {
                count_page++;
                sql.Remove(0, sql.Length);
                sql.Append(" UPDATE " + name_table + " SET page_number = " + count_page + " where page_number IS NULL and " + name_serial + " >=" + cur_val + " ");
                sql.Append(" and " + name_serial + " < " + (cur_val + size_page) + extraAnd);
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("NumRows: Ошибка нумерации, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);
                    ret.result = false;
                    return ret;
                }
                cur_val += size_page;
            }

            ret.tag = count_page;
            return ret;
        }

        public IntfResultObjectType<List<NeboReestr>> GetReestrInfo(IntfRequestType request, IDbConnection conn_db)
        {
            DateTime pred_month_calc = new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1).AddMonths(-1); //предыдущий рассчетный месяц            
            int pred_year = pred_month_calc.Year;
            string yy = (pred_year - 2000).ToString("00");
            string mm = pred_month_calc.Month.ToString("00");
            string sql = "";
            int nzp_nebo_reestr = Convert.ToInt32(request.keyID);

            ExecSQL(conn_db, "drop table tmp_reestr_info", false);

            sql = " CREATE temp TABLE tmp_reestr_info( " +
               " nzp_nebo_reestr int, " +
               " type_reestr INTEGER, " +
               " dat_charge DATE, " +
               " dat_oper DATE, " +
               " nzp_area INTEGER, "+
               " is_prepare INTEGER, " +
               " dat_prepare DATETIME YEAR to SECOND, " +
               " is_notify INTEGER, " +
               " dat_notify DATETIME YEAR to SECOND, " +
               " dat_nebocall DATETIME YEAR to SECOND, " +
               " count_rows integer, " +
               " count_pages integer, " +
               " kontr_sum_prih decimal(14,2), " +
               " kontr_sum_insaldo decimal(14,2), " +
               " kontr_sum_money decimal(14,2)) with no log; ";
            if (!ExecSQL(conn_db, sql.ToString(), true).result)
            {
                MonitorLog.WriteLog("GetReestrInfo: Ошибка создании темповой таблицы, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);

                return new IntfResultObjectType<List<NeboReestr>>();
            }


            if (nzp_nebo_reestr == 0)
            {
                
                sql = "  insert into  tmp_reestr_info (nzp_nebo_reestr, type_reestr, dat_charge, dat_oper,nzp_area, " +
              "  is_prepare, dat_prepare, is_notify, dat_notify, dat_nebocall) " +
              "  select * from "+Points.Pref+"_data:nebo_reestr where is_notify="
                      + Convert.ToInt32(NeboReestr.NeboReestrNotifyStatuses.OnNotify) + " and is_prepare="
                      + Convert.ToInt32(NeboReestr.NeboReestrStatuses.Success) + "  ";
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("GetReestrInfo: Ошибка создании темповой таблицы, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);

                    return new IntfResultObjectType<List<NeboReestr>>();
                }

                #region Число строк
                sql = "  update tmp_reestr_info set (count_rows) =((select count(*) from " + Points.Pref + "_fin_" + yy + ":nebo_rsaldo r " +
                " where tmp_reestr_info.nzp_nebo_reestr=r.nzp_nebo_reestr )) where type_reestr="+Convert.ToInt32(NeboReestr.NeboReestrTypes.Saldo)+"; ";
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("GetReestrInfo: Ошибка обновления числа строк, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);

                    return new IntfResultObjectType<List<NeboReestr>>();
                }

                sql = "  update tmp_reestr_info set (count_rows) =((select count(*) from " + Points.Pref + "_fin_" + yy + ":nebo_rfnsupp r " +
                " where tmp_reestr_info.nzp_nebo_reestr=r.nzp_nebo_reestr )) where type_reestr=" + Convert.ToInt32(NeboReestr.NeboReestrTypes.Income) + "; ";
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("GetReestrInfo: Ошибка обновления числа строк, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);

                    return new IntfResultObjectType<List<NeboReestr>>();
                }
                #endregion
                #region число страниц
                sql = " update tmp_reestr_info set (count_pages) =((select count(distinct page_number) from " + Points.Pref + "_fin_" + yy + ":nebo_rsaldo r " +
               " where tmp_reestr_info.nzp_nebo_reestr=r.nzp_nebo_reestr )) where type_reestr=" + Convert.ToInt32(NeboReestr.NeboReestrTypes.Saldo) + " ";
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("GetReestrInfo: Ошибка обновления числа страниц, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);
                    return new IntfResultObjectType<List<NeboReestr>>();
                }

                sql = " update tmp_reestr_info set (count_pages) =((select count(distinct page_number) from " + Points.Pref + "_fin_" + yy + ":nebo_rfnsupp r " +
              " where tmp_reestr_info.nzp_nebo_reestr=r.nzp_nebo_reestr )) where type_reestr=" + Convert.ToInt32(NeboReestr.NeboReestrTypes.Income) + "; ";
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("GetReestrInfo: Ошибка обновления числа страниц, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);
                    return new IntfResultObjectType<List<NeboReestr>>();
                }
                #endregion
                #region Контрольные суммы
                //по сальдо
                sql = "  update tmp_reestr_info set (kontr_sum_insaldo,kontr_sum_money) =((select sum(sum_insaldo),sum(sum_money) " +
                " from " + Points.Pref + "_fin_" + yy + ":nebo_rsaldo r " +
                " where tmp_reestr_info.nzp_nebo_reestr=r.nzp_nebo_reestr )) where type_reestr=" + Convert.ToInt32(NeboReestr.NeboReestrTypes.Saldo) + " ";
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("GetReestrInfo: Ошибка обновления контрольных сумм, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);
                    return new IntfResultObjectType<List<NeboReestr>>();
                }
                //по посуплениям
                sql = "  update tmp_reestr_info set (kontr_sum_prih) =((select sum(sum_prih) " +
                " from " + Points.Pref + "_fin_" + yy + ":nebo_rfnsupp r " +
                " where tmp_reestr_info.nzp_nebo_reestr=r.nzp_nebo_reestr )) where type_reestr=" + Convert.ToInt32(NeboReestr.NeboReestrTypes.Income) + " ";
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("GetReestrInfo: Ошибка обновления контрольных сумм, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);
                    return new IntfResultObjectType<List<NeboReestr>>();
                }
                #endregion
            }
            else
            {
                sql = "  insert into  tmp_reestr_info (nzp_nebo_reestr, type_reestr, dat_charge, dat_oper,nzp_area, " +
              "  is_prepare, dat_prepare, is_notify, dat_notify, dat_nebocall) " +
              "  select * from "+Points.Pref+"_data:nebo_reestr where is_notify="
                      + Convert.ToInt32(NeboReestr.NeboReestrNotifyStatuses.OnNotify) + " and is_prepare="
                      + Convert.ToInt32(NeboReestr.NeboReestrStatuses.Success) + " and r.nzp_nebo_reestr=" + nzp_nebo_reestr + " ";
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("GetReestrInfo: Ошибка создании темповой таблицы, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);

                    return new IntfResultObjectType<List<NeboReestr>>();
                }
                #region Число строк
                //по сальдо
                sql = "  update tmp_reestr_info set (count_rows) =((select count(*) from " + Points.Pref + "_fin_" + yy + ":nebo_rsaldo r " +
                " where tmp_reestr_info.nzp_nebo_reestr=r.nzp_nebo_reestr and r.nzp_nebo_reestr=" + nzp_nebo_reestr + ")) " +
                " where tmp_reestr_info.nzp_nebo_reestr=" + nzp_nebo_reestr + " and type_reestr="+Convert.ToInt32(NeboReestr.NeboReestrTypes.Saldo)+"; ";
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("GetReestrInfo: Ошибка обновления числа строк, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);

                    return new IntfResultObjectType<List<NeboReestr>>();
                }
                //по поступлениям
                 sql = "  update tmp_reestr_info set (count_rows) =((select count(*) from " + Points.Pref + "_fin_" + yy + ":nebo_rfnsupp r " +
                " where tmp_reestr_info.nzp_nebo_reestr=r.nzp_nebo_reestr and r.nzp_nebo_reestr=" + nzp_nebo_reestr + ")) " +
                " where tmp_reestr_info.nzp_nebo_reestr=" + nzp_nebo_reestr + " and type_reestr=" + Convert.ToInt32(NeboReestr.NeboReestrTypes.Income) + "; ";
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("GetReestrInfo: Ошибка обновления числа строк, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);

                    return new IntfResultObjectType<List<NeboReestr>>();
                }
                #endregion
                #region Число страниц
                sql = " update tmp_reestr_info set (count_pages) =((select count(distinct page_number) from " + Points.Pref + "_fin_" + yy + ":nebo_rsaldo r " +
               " where tmp_reestr_info.nzp_nebo_reestr=r.nzp_nebo_reestr and r.nzp_nebo_reestr=" + nzp_nebo_reestr + ")) )) " +
                " where tmp_reestr_info.nzp_nebo_reestr=" + nzp_nebo_reestr + "  and type_reestr=" + Convert.ToInt32(NeboReestr.NeboReestrTypes.Saldo) + "; ";
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("GetReestrInfo: Ошибка обновления числа страниц, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);
                    return new IntfResultObjectType<List<NeboReestr>>();
                }

                sql = " update tmp_reestr_info set (count_pages) =((select count(distinct page_number) from " + Points.Pref + "_fin_" + yy + ":nebo_rfnsupp r " +
            " where tmp_reestr_info.nzp_nebo_reestr=r.nzp_nebo_reestr and r.nzp_nebo_reestr=" + nzp_nebo_reestr + ")) )) " +
             " where tmp_reestr_info.nzp_nebo_reestr=" + nzp_nebo_reestr + "  and type_reestr=" + Convert.ToInt32(NeboReestr.NeboReestrTypes.Income) + "; ";
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("GetReestrInfo: Ошибка обновления числа страниц, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);
                    return new IntfResultObjectType<List<NeboReestr>>();
                }
                #endregion
                #region Контрольные суммы
                sql = "  update tmp_reestr_info set (kontr_sum_insaldo,kontr_sum_money) =((select sum(sum_insaldo),sum(sum_money) " +
                "  " + Points.Pref + "_fin_" + yy + ":nebo_rsaldo r " +
                " where tmp_reestr_info.nzp_nebo_reestr=r.nzp_nebo_reestr and r.nzp_nebo_reestr=" + nzp_nebo_reestr + ")) " +
                " where tmp_reestr_info.nzp_nebo_reestr=" + nzp_nebo_reestr + "  and type_reestr=" + Convert.ToInt32(NeboReestr.NeboReestrTypes.Saldo) + "; ";
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("GetReestrInfo: Ошибка обновления контрольных сумм, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);
                    return new IntfResultObjectType<List<NeboReestr>>();
                }

                sql = "  update tmp_reestr_info set (kontr_sum_prih) =((select sum(kontr_sum_prih) " +
               "  " + Points.Pref + "_fin_" + yy + ":nebo_rfnsupp r " +
               " where tmp_reestr_info.nzp_nebo_reestr=r.nzp_nebo_reestr and r.nzp_nebo_reestr=" + nzp_nebo_reestr + ")) " +
               " where tmp_reestr_info.nzp_nebo_reestr=" + nzp_nebo_reestr + "  and type_reestr=" + Convert.ToInt32(NeboReestr.NeboReestrTypes.Income) + "; ";
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("GetReestrInfo: Ошибка обновления контрольных сумм, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);
                    return new IntfResultObjectType<List<NeboReestr>>();
                }
                #endregion
            }

            sql = " select * from tmp_reestr_info";
            //Пример заполнение списка с помощью DataReader
            {
                List<NeboReestr> list = ExecRead(conn_db, sql, NeboSaldoDataConverter.ToReestrInfo);
                return new IntfResultObjectType<List<NeboReestr>>(list);
            }
        }

        public IntfResultObjectType<List<NeboSaldo>> GetSaldoReestr(IntfRequestObjectType<NeboSaldo> request, IDbConnection connectionID)
        {
            ResultPaging pagingInfo = new ResultPaging();
            Returns ret = Utils.InitReturns();
            StringBuilder sql = new StringBuilder();
            int nzp_area = request.entity.nzp_area;
            int nzp_nebo_reestr = request.entity.nzp_nebo_reestr;

            DateTime pred_month_calc = new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1).AddMonths(-1); //предыдущий рассчетный месяц            
            int pred_year = pred_month_calc.Year;
            string yy = (pred_year - 2000).ToString("00");
            string mm = pred_month_calc.Month.ToString("00");
            string tableName = Points.Pref + "_fin_" + yy + ": nebo_rsaldo ";

            sql.Append("SELECT nzp_nebo_rsaldo, nzp_nebo_reestr, dat_charge, typek, ");
            sql.Append("num_ls, pkod, nzp_dom, nzp_serv, nzp_area,  nzp_supp, ");
            sql.Append("sum_insaldo, sum_real, reval, izm_saldo, sum_money, ");
            sql.Append("sum_charge, sum_outsaldo, page_number ");
            sql.Append("FROM " + Points.Pref + "_fin_" + yy + ": nebo_rsaldo ");
            sql.Append("WHERE nzp_area = " + nzp_area + " AND nzp_nebo_reestr = " + nzp_nebo_reestr + " AND page_number = " + request.paging.curPageNumber);
            {
                List<NeboSaldo> list = ExecRead(connectionID, sql.ToString(), NeboSaldoDataConverter.ToSaldoReestr);

                ret = new DbNeboUtils().FillResultPaging(connectionID, tableName, "page_number", list.Count, out pagingInfo);
                if (!ret.result)
                    return new IntfResultObjectType<List<NeboSaldo>>() { resultMessage = ret.text, resultCode = -1 };

                return new IntfResultObjectType<List<NeboSaldo>>(list) { paging = pagingInfo };
            }

        }

        public IntfResultObjectType<List<NeboSupp>> GetSuppReestr(IntfRequestObjectType<NeboSupp> neboSupp, IDbConnection conn_db)
        {
            int nzp_area = neboSupp.entity.nzp_area;
            int nzp_nebo_reestr = neboSupp.entity.nzp_nebo_reestr;
            int page_number = neboSupp.entity.page_number;

            
            DateTime pred_month_calc = new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1).AddMonths(-1); //предыдущий рассчетный месяц            
            int pred_year = pred_month_calc.Year;
            string yy = (pred_year - 2000).ToString("00");
            string mm = pred_month_calc.Month.ToString("00");

            string sql =
                " select nzp_nebo_rfnsupp, nzp_nebo_reestr, dat_uchet, typek, " +
                "        num_ls, pkod, nzp_dom, nzp_serv, nzp_area,  nzp_supp,sum_prih, page_number " +
                " from " + Points.Pref + "_fin_" + yy + ":nebo_rfnsupp " +
                " where nzp_area=" + nzp_area + " and nzp_nebo_reestr=" + nzp_nebo_reestr + " and page_number=" + page_number + " ";
            var DT = ClassDBUtils.OpenSQL(sql, conn_db).resultData;
               
            {
                List<NeboSupp> list = ExecRead(conn_db, sql, NeboSaldoDataConverter.ToSuppReestr);
                return new IntfResultObjectType<List<NeboSupp>>(list);
            }

        }

        // Заполнение информации для портала Небо для реестре №  nzp_nebo_reestr за период pDat_uchet_from по DateTime pDat_uchet_to
        public bool FillSumOplForNebo(IDbConnection conn_db, int nzp_nebo_reestr,int nzp_area,
            string pDat_uchet_from, string pDat_uchet_to, out Returns ret)
        {
            ret = Utils.InitReturns();
            string sql_text = "";

            int yy = Points.DateOper.Year;
            int mm = Points.DateOper.Month;

            string table_nebo = Points.Pref + "_fin_" + (yy - 2000).ToString("00") + ":" + "nebo_rfnsupp ";
            sql_text = "delete from " + table_nebo + " where nzp_nebo_reestr = " + nzp_nebo_reestr;
            ret = ExecSQL(conn_db, sql_text, true);
            if (!ret.result)
            {
                return false;
            }


            sql_text = "drop table tmp_nebo_kvar";
            ret = ExecSQL(conn_db, sql_text, true);

            sql_text = "select typek, pkod, num_ls, nzp_dom, nzp_kvar from " + Points.Pref + "_data:kvar where typek=1 and nzp_area="+nzp_area+" into temp tmp_nebo_kvar with no log";
            ret = ExecSQL(conn_db, sql_text, true);

            ExecSQL(conn_db, " create index idx_tmp_nebo_kvar on  tmp_nebo_kvar(num_ls, nzp_dom);", true);
            ExecSQL(conn_db, " Update statistics for table tmp_nebo_kvar ", true);

            sql_text = "drop table tmp_nebo_kvar_3";
            ret = ExecSQL(conn_db, sql_text, true);

            sql_text = "select typek, pkod, num_ls, nzp_dom, nzp_kvar from " + Points.Pref + "_data:kvar where nvl(typek,1)=3 and nzp_area=" + nzp_area + "  into temp tmp_nebo_kvar_3 with no log";
            ret = ExecSQL(conn_db, sql_text, true);

            ExecSQL(conn_db, " create index idx_tmp_nebo_kvar_3 on  tmp_nebo_kvar_3(num_ls, nzp_dom);", true);
            ExecSQL(conn_db, " Update statistics for table tmp_nebo_kvar_3 ", true);

            string tab_supplier = "";
            foreach (_Point zap in Points.PointList)
            {
                tab_supplier = zap.pref + "_charge_" + (Points.DateOper.Year - 2000).ToString("00") + ":fn_supplier" + Points.DateOper.Month.ToString("00");
                sql_text =
                       "insert into " + table_nebo + "( nzp_nebo_reestr, nzp_dom, dat_uchet,  typek, nzp_supp, nzp_serv,nzp_area,sum_prih)  " +
                       "select "+nzp_nebo_reestr+", nzp_dom, dat_uchet, 1, nzp_supp, nzp_serv,"+nzp_area+", sum(sum_prih) " +
                       "from " + tab_supplier + " a, tmp_nebo_kvar b  " +
                       "where a.num_ls = b.num_ls and a.sum_prih <>0 " +
                       "and dat_uchet between  '" + pDat_uchet_from.Trim() + "' and '" + pDat_uchet_to.Trim() + "' " +
                       "group by 1,2,3,4,5,6;  ";
                ret = ExecSQL(conn_db, sql_text, true);
                sql_text =
                       "insert into " + table_nebo + "( nzp_nebo_reestr, nzp_dom, dat_uchet,  typek, nzp_supp, nzp_serv, num_ls,   pkod,nzp_area, sum_prih)  " +
                       "select " + nzp_nebo_reestr + " , b.nzp_dom, a.dat_uchet, '3' as typek, a.nzp_supp, a.nzp_serv, b.num_ls, b.pkod," + nzp_area + ", sum(a.sum_prih) " +
                       "from " + tab_supplier + " a, tmp_nebo_kvar_3 b  " +
                       "where a.num_ls = b.num_ls and a.sum_prih <>0 " +
                       "and dat_uchet between  '" + pDat_uchet_from.Trim() + "' and '" + pDat_uchet_to.Trim() + "' " +
                       "group by 1,2,3,4,5,6,7,8 ";
                ret = ExecSQL(conn_db, sql_text, true);

            }

            sql_text = "drop table tmp_nebo_kvar";
            ret = ExecSQL(conn_db, sql_text, true);

            sql_text = "drop table tmp_nebo_kvar_3";
            ret = ExecSQL(conn_db, sql_text, true);

            if (!ret.result)
            {
                return false;
            }
            else return true;


        }

        /// <summary>
        /// Получение реестра оплат - население
        /// </summary>
        /// <param name="request"></param>
        /// <param name="conn_db"></param>
        /// <returns></returns>
        public IntfResultObjectType<List<NeboPaymentReestr>> GetPaymentReestr(IntfRequestType request, IDbConnection connectionID)
        {
            ResultPaging pagingInfo = new ResultPaging();
            Returns ret = new Returns();
            StringBuilder sql = new StringBuilder();
            int nzp_nebo_reestr = Convert.ToInt32(request.keyID);
            int nzp_area = Convert.ToInt32(request.parentID);
            string tableName = Points.Pref + "_fin_" + (Points.CalcMonth.year_ - 2000).ToString("00") + ": nebo_rfnsupp ";

            string tempTableName = "tmp_payment_reestr_info";
            ExecSQL(connectionID, "DROP TABLE " + tempTableName, false);

            sql.Append(" CREATE TEMP TABLE " + tempTableName);
            sql.Append("(nzp SERIAL NOT NULL, ");
            sql.Append("pkod DECIMAL(10),");
            sql.Append("nzp_dom INTEGER NOT NULL,");
            sql.Append("nzp_serv INTEGER,");
            sql.Append("nzp_supp INTEGER,");
            sql.Append("sum_prih DECIMAL(10),");
            sql.Append("page_number INTEGER");
            sql.Append(") WITH NO LOG;");
            if (!ExecSQL(connectionID, sql.ToString(), true).result)
            {
                MonitorLog.WriteLog("GetPaymentReestr: Ошибка создании временной таблицы, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);
                new IntfResultObjectType<List<NeboPaymentReestr>>() { resultMessage = ret.text, resultCode = -1 };
            }

            sql.Remove(0, sql.Length);
            sql.Remove(0, sql.Length);
            sql.Append("INSERT INTO " + tempTableName);
            sql.Append("(pkod, nzp_dom, nzp_serv, nzp_supp, sum_prih) ");
            sql.Append("SELECT nf.dat_uchet,");
            sql.Append("nf.pkod,");
            sql.Append("nf.nzp_dom,");
            sql.Append("nf.nzp_serv,");
            sql.Append("nf.nzp_supp, ");
            sql.Append("nf.sum_prih ");
            sql.Append("FROM " + Points.Pref + "_data: nebo_reestr ns ");
            sql.Append("LEFT JOIN " + tableName + " nf  "); 
            sql.Append("ON nf.nzp_nebo_reestr = ns.nzp_nebo_reestr  "); 
            sql.Append("WHERE ns.nzp_nebo_reestr = " + nzp_nebo_reestr + " ");
            sql.Append("AND nf.nzp_area = " + nzp_area + " ");
            sql.Append("AND nf.page_number = " + request.paging.curPageNumber + ";");
            
            #region постраничное разбиение
            DbNeboSaldo ns = new DbNeboSaldo();
            ret = ns.NumRows(connectionID, "nzp", 500, tempTableName, "");
            if (!ret.result)
                new IntfResultObjectType<List<NeboDom>>() { resultMessage = ret.text, resultCode = -1 };
            #endregion

            {
                List<NeboPaymentReestr> list = ExecRead(connectionID, sql.ToString(), NeboSaldoDataConverter.ToNeboPaymentReestr);

                ret = new DbNeboUtils().FillResultPaging(connectionID, tempTableName, "page_number", list.Count, out pagingInfo);
                if(!ret.result)
                    return new IntfResultObjectType<List<NeboPaymentReestr>>() { resultMessage = ret.text, resultCode = -1 };

                return new IntfResultObjectType<List<NeboPaymentReestr>>(list) { paging = pagingInfo };
            }
        }
    }
}
