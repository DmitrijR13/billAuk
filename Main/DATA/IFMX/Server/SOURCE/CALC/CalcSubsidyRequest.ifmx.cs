using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Globalization;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System.Data;

namespace STCLINE.KP50.DataBase
{
    //здась находятся классы для распределения оплат
    public partial class DbCalc : DbCalcClient
    {

        //распределение или откат пачки через фоновую задачу
        public void RequestFonTasks(int month_, int year_, int nzp_req, FonTaskTypeIds task, string comment, out Returns ret)
        {
            //затем выставим задание на распределение или откат
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result)
            {
                return;
            }

            if (task == FonTaskTypeIds.taskCalcSubsidyRequest)
            {
                CalcFon calcfon = new CalcFon(0);

                calcfon.task = task;
                calcfon.status = FonTaskStatusId.New; //на выполнение 
                calcfon.nzp = nzp_req;
                calcfon.yy = year_;
                calcfon.mm = month_;

                foreach (_Point zap in Points.PointList)
                {
                    calcfon.nzpt = zap.nzp_wp;
                    calcfon.number = Points.GetCalcNum(zap.nzp_wp); //определить номер потока расчета

                    calcfon.txt = "'" + zap.point + " " + comment + "'";

                    CheckCalcAndPutInFon(conn_web, calcfon, out ret);
                    if (!ret.result)
                    {
                        break;
                    }
                }
            }
            else if (task == FonTaskTypeIds.taskCalcSaldoSubsidy)
            {
                CalcFon calcfon = new CalcFon(0);

                calcfon.task = task;
                calcfon.status = FonTaskStatusId.New; //на выполнение 

                calcfon.nzpt = Points.PointList[0].nzp_wp;
                calcfon.nzp = nzp_req;
                calcfon.yy = year_;
                calcfon.mm = month_;
                calcfon.number = Points.GetCalcNum(Points.PointList[0].nzp_wp); //определить номер потока расчета

                calcfon.txt = "'" + Points.PointList[0].point + " " + comment + "'";

                CheckCalcAndPutInFon(conn_web, calcfon, out ret);
            }

            conn_web.Close();
        }

        //распределение или откат пачки через фоновую задачу
        public void RequestFonTasks(int nzp_req, out Returns ret)
        {
            //затем выставим задание на распределение или откат
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result)
            {
                return;
            }

            CalcFon calcfon = new CalcFon(0);
            calcfon.task = FonTaskTypeIds.taskCalcReport;
            calcfon.status = FonTaskStatusId.New; //на выполнение 
            calcfon.nzp = nzp_req;
            calcfon.yy = Points.CalcMonth.year_;
            calcfon.mm = Points.CalcMonth.month_;
            
            foreach (_Point zap in Points.PointList)
            {
                calcfon.nzpt = zap.nzp_wp;
                calcfon.number = Points.GetCalcNum(zap.nzp_wp); //определить номер потока расчета

                calcfon.txt = "'" + zap.point + "Расчет данных для аналитики'";

                CheckCalcAndPutInFon(conn_web, calcfon, out ret);
                if (!ret.result)
                {
                    break;
                }
            }

            conn_web.Close();
            return;
        }

        /// <summary>
        /// Расчет заявки на субсидию
        /// </summary>
        /// <param name="calcfon">Атрибуты задачи calcfon</param>
        /// <param name="ret"></param>
        public void CalcSubsidyRequest(CalcFon calcfon, out Returns ret)
        {
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            IDataReader reader = null;
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                return;
            }

            StringBuilder sql = new StringBuilder();

            #region Расчет заявки
            ExecRead(conn_db, out reader, "drop table t_subsrequest", false);
            string pref = Points.GetPref(calcfon.nzpt);
            sql.Remove(0, sql.Length);
            sql.Append(" create temp table t_subsrequest (");
            sql.Append(" nzp_town integer, ");
            sql.Append(" nzp_serv integer, ");
            sql.Append(" nzp_payer integer, ");
            sql.Append(" sum_charge Decimal(14,2) default 0.00, ");
            sql.Append(" sum_pere Decimal(14,2) default 0.00, ");
            sql.Append(" sum_plat Decimal(14,2) default 0.00) with no log ");
            if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
            {
                ret.text = "Ошибка сохранения списка к перечислению";
                MonitorLog.WriteLog(ret.text + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                ret.result = false;
                return;
            }

            sql.Remove(0, sql.Length);
            sql.Append(" insert into t_subsrequest(nzp_payer, nzp_serv, nzp_town, sum_charge) ");
            sql.Append(" select su.nzp_payer, 1, sr.nzp_town, sum(b.sum_charge) as sum_charge ");
            sql.Append(" from " + Points.Pref + "_data:kvar k, " + Points.Pref + "_data:dom d, ");
            sql.Append(Points.Pref + "_data:s_ulica s, " + Points.Pref + "_data:s_rajon sr, " + pref + "_charge_");
            sql.Append((calcfon.yy - 2000).ToString("00"));
            sql.Append(" : charge_"+calcfon.mm.ToString("00")+" b, "+Points.Pref+"_kernel:s_payer su");
            sql.Append(" where k.nzp_kvar=b.nzp_kvar ");
            sql.Append(" and k.nzp_dom=d.nzp_dom ");
            sql.Append(" and d.nzp_ul=s.nzp_ul ");
            sql.Append(" and s.nzp_raj=sr.nzp_raj ");
            sql.Append(" and b.nzp_supp=su.nzp_supp ");
            sql.Append(" and b.nzp_serv>1 ");
            sql.Append(" and dat_charge is null");
            sql.Append(" and abs(sum_charge)>0.001");
            sql.Append(" group by 1,2,3 ");
            if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
            {
                ret.text = "Ошибка сохранения списка к перечислению ";
                MonitorLog.WriteLog(ret.text + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                ret.result = false;
                return;
            }

            #region Сбор невыплат из предыдущего месяца
            if (calcfon.mm > 1)
            {
                sql.Remove(0, sql.Length);
                sql.Append(" insert into t_subsrequest(nzp_payer, nzp_serv, nzp_town, sum_pere) ");
                sql.Append(" select nzp_payer, nzp_serv, nzp_town, sum(sum_outsaldo) as sum_pere ");
                sql.Append(" from " + Points.Pref + "_fin_"+(calcfon.yy-2000).ToString("00")+ ":subs_saldo ");
                sql.Append(" where date_month ='01." + (calcfon.mm - 1).ToString("00") + "." + calcfon.yy + "'");
                sql.Append(" group by 1,2,3 ");
                if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
                {
                    ret.text = "Ошибка сохранения списка к перечислению ";
                    MonitorLog.WriteLog(ret.text + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return;
                }
            }
            if (reader != null) reader.Close();
            #endregion

            sql.Remove(0, sql.Length);
            sql.Append(" insert into  " + Points.Pref + "_fin_" + (calcfon.yy - 2000).ToString("00"));
            sql.Append(": subs_req_details");
            sql.Append(" (nzp_req, nzp_town, nzp_payer, nzp_serv, sum_charge, ");
            sql.Append(" sum_pere,  sum_request) ");
            sql.Append(" select " + calcfon.nzp + ", nzp_town, nzp_payer, nzp_serv, ");
            sql.Append(" sum(sum_charge), sum(sum_pere) , sum(sum_charge) ");
            sql.Append(" from t_subsrequest group by 1,2,3,4 ");
            if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
            {
                ret.text = "Ошибка сохранения списка к перечислению";
                MonitorLog.WriteLog(ret.text + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                ret.result = false;
                return;
            }
            if (reader != null) reader.Close();

            sql.Remove(0, sql.Length);
            sql.Append("  update  " + Points.Pref + "_fin_" + (calcfon.yy - 2000).ToString("00"));
            sql.Append(": subs_req_details");
            sql.Append(" set sum_request = 0 where nzp_req=" + calcfon.nzp);
            sql.Append(" and sum_request<0.001 ");
            if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
            {
                ret.text = "Ошибка сохранения списка к перечислению";
                MonitorLog.WriteLog(ret.text + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                ret.result = false;
                return;
            }
            if (reader != null) reader.Close();


            sql.Remove(0, sql.Length);
            sql.Append(" update  " + Points.Pref + "_fin_" + (calcfon.yy - 2000).ToString("00"));
            sql.Append(": subs_req set sum_request = (select sum(sum_request) ");
            sql.Append("  from " + Points.Pref + "_fin_" + (calcfon.yy - 2000).ToString("00"));
            sql.Append(": subs_req_details where nzp_req = " + calcfon.nzp + ")");
            sql.Append("  where nzp_req = " + calcfon.nzp);
            if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
            {
                ret.text = "Ошибка сохранения списка к перечислению";
                MonitorLog.WriteLog(ret.text + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                ret.result = false;
                return;
            }

            ExecRead(conn_db, out reader, "drop table t_subsrequest", true);
            if (reader != null) reader.Close();
            if (reader != null) reader.Dispose();


            #endregion

            #region Расчет сальдо
            if (ret.result)
              CalcSaldoSubsidy( calcfon, out ret);
            #endregion

            return;
        }

   
        /// <summary>
        /// Проверяет выполнено ли задание, если выполнено, то меняет статус заявки
        /// </summary>
        /// <param name="conn_web"></param>
        /// <param name="calcfon"></param>
        /// <param name="ret"></param>
        public void CheckSubsidyRequestTask(CalcFon calcfon, out Returns ret)
        {
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result)
            {
                return;
            }

            CheckSubsidyRequestTask(conn_web, calcfon, out ret);

            conn_web.Close();
            //conn_web.Dispose();
        }

        public void CheckSubsidyRequestTask(IDbConnection conn_web, CalcFon calcfon, out Returns ret)
        {
            ret = Utils.InitReturns();
            IDataReader reader;


            #region обход calc_fonXX и проверка задания по nzp_req
            for (int i = 0; i < CalcThreads.maxCalcThreads; i++)
            {
                string tab = "calc_fon_" + i;
                if (!TempTableInWebCashe(conn_web, tab)) continue;

                ret = ExecRead(conn_web, out reader,
                    " Select kod_info From " + tab +
                    " Where nzp   = " + calcfon.nzp +
                      " and year_ = " + calcfon.yy +
                      " and month_= " + calcfon.mm +
                      " and task  = " + calcfon.task
                    , true);
                if (!ret.result)
                {
                    conn_web.Close();
                    return;
                }

                try
                {
                    while (reader.Read())
                    {
                        if (reader["kod_info"] != DBNull.Value)
                        {
                            int k = (int)reader["kod_info"];
                            if (k == -1)
                            {
                                //Задание завершено с ошибками
                                break;
                            }
                            if (k == 0 || k == 3)
                            {
                                //задание  еще не обработано
                                return;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = ex.Message;
                    MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Error, 1, 222, true);
                    return;
                }

            }
            #endregion

           

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                return;
            }

            
            #region Изменение статуса заявки после расчета на сформировано
            StringBuilder sql = new StringBuilder();
            sql.Remove(0, sql.Length);
            sql.Append(" update " + Points.Pref + "_fin_" + (calcfon.yy-2000).ToString());
            sql.Append(" :subs_req ");
            sql.Append(" set ");
            sql.Append(" nzp_status =  " + (int)SubsidyRequestStates.Placed);
            sql.Append(" where nzp_req = " + calcfon.nzp);
            if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
            {
                ret.text = "Ошибка изменения атрибутов заявки ";
                MonitorLog.WriteLog(ret.text + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                ret.result = false;
                return;
            }
            #endregion
            conn_db.Close();
        }


        /// <summary>
        /// Расчет subs_saldo
        /// </summary>
        /// <param name="calcfon"></param>
        /// <param name="ret"></param>
        public void CalcSaldoSubsidy(CalcFon calcfon, out Returns ret)
        {
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            IDataReader reader = null;
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                return;
            }

            #region Расчет заявки
            StringBuilder sql = new StringBuilder();
            string pref = Points.GetPref(calcfon.nzpt);
            string finAlias = Points.Pref + "_fin_" + (calcfon.yy-2000).ToString("00");
            ExecRead(conn_db, out reader, "drop table t_subssaldo", false);
            sql.Remove(0, sql.Length);
            sql.Append(" create temp table t_subssaldo (");
            sql.Append(" nzp_town integer, ");
            sql.Append(" nzp_serv integer, ");
            sql.Append(" nzp_payer integer, ");
            sql.Append(" sum_insaldo Decimal(14,2) default 0.00, ");
            sql.Append(" sum_outsaldo Decimal(14,2) default 0.00, ");
            sql.Append(" sum_outsaldo_c Decimal(14,2) default 0.00, ");
            sql.Append(" sum_charge Decimal(14,2) default 0.00, ");
            sql.Append(" sum_request Decimal(14,2) default 0.00, ");
            sql.Append(" sum_mismatch Decimal(14,2) default 0.00, ");
            sql.Append(" sum_order Decimal(14,2) default 0.00) with no log ");
            if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
            {
                ret.text = "Ошибка сохранения списка к перечислению";
                MonitorLog.WriteLog(ret.text + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                ret.result = false;
                return;
            }
            #region Добавляем Исходящее сальдо предыдущего месяца
            //На начало года сальдо по умолчанию равно 0
            if (calcfon.mm > 1)
            {
                sql.Remove(0, sql.Length);
                sql.Append(" insert into t_subssaldo(nzp_payer, nzp_serv, nzp_town, sum_outsaldo) ");
                sql.Append(" select nzp_payer, nzp_serv, nzp_town, sum(sum_outsaldo) as sum_outsaldo ");
                sql.Append(" from " + finAlias + ":subs_saldo ");
                sql.Append(" where date_month='01." + (calcfon.mm - 1).ToString("00") + "." + calcfon.yy.ToString() + "'");
                sql.Append(" group by 1,2,3 ");
                if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
                {
                    ret.text = "Ошибка сохранения списка к перечислению ";
                    MonitorLog.WriteLog(ret.text + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return;
                }
            }
            #endregion

            #region Добавляем сумму по заявкам и перечислено
            //На начало года сальдо по умолчанию равно 0
            sql.Remove(0, sql.Length);
            sql.Append(" insert into t_subssaldo(nzp_payer, nzp_serv, nzp_town, sum_charge, sum_request, sum_order) ");
            sql.Append(" select nzp_payer, nzp_serv, nzp_town, sum(sum_charge), sum(a.sum_request),   ");
            sql.Append(" sum(case when nzp_order is null then 0 else a.sum_request end) as sum_order   ");
            sql.Append(" from " + finAlias + ":subs_req_details a,  " + finAlias + ":subs_req b ");
            sql.Append(" where date_month='01." + calcfon.mm.ToString("00") + "." + calcfon.yy.ToString() + "'");
            sql.Append(" and a.nzp_req=b.nzp_req and nzp_status <> " + ((int)SubsidyRequestStates.Deleted).ToString() );
            sql.Append(" group by 1,2,3 ");
            if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
            {
                ret.text = "Ошибка сохранения списка к перечислению ";
                MonitorLog.WriteLog(ret.text + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                ret.result = false;
                return;
            }

            #endregion

            #region Удаляем старую информацию
            sql.Remove(0, sql.Length);
            sql.Append(" delete from " + finAlias + ":subs_saldo ");
            sql.Append(" where date_month = '01." + calcfon.mm.ToString() + "." + calcfon.yy.ToString() + "'");
            if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
            {
                ret.text = "Ошибка сохранения списка к перечислению ";
                MonitorLog.WriteLog(ret.text + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                ret.result = false;
                return;
            }
            #endregion

            #region Добавляем новую информацию
            sql.Remove(0, sql.Length);
            sql.Append(" insert into " + finAlias + ":subs_saldo(nzp_payer, nzp_serv, nzp_town,date_month, ");
            sql.Append(" sum_insaldo, sum_charge, sum_request, sum_order, sum_outsaldo, sum_mismatch) ");
            sql.Append(" select nzp_payer, nzp_serv, nzp_town, '01."+ calcfon.mm.ToString()+"."+calcfon.yy.ToString()+"',");
            sql.Append(" sum(sum_insaldo) as sum_insaldo, ");
            sql.Append(" sum(sum_charge) as sum_charge, sum(sum_request) as sum_request,");
            sql.Append(" sum(sum_order) as sum_order, sum(nvl(sum_insaldo,0)+nvl(sum_request,0)+");
            sql.Append(" -nvl(sum_order,0)) as sum_outsaldo, sum(0) ");
            sql.Append(" from  t_subssaldo ");
            sql.Append(" group by 1,2,3 ");
            if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
            {
                ret.text = "Ошибка сохранения списка к перечислению ";
                MonitorLog.WriteLog(ret.text + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                ret.result = false;
                return;
            }
            #endregion

            if (reader != null) reader.Close();
            ExecRead(conn_db, out reader, "drop table t_subssaldo", true);

            if (reader != null) reader.Close();
            if (reader != null) reader.Dispose();




            #endregion

            return;
        }


    }
}
