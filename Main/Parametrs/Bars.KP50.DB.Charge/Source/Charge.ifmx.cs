using System;
using System.Collections.Generic;
using System.Data;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using Bars.KP50.DB.Faktura;


namespace STCLINE.KP50.DataBase
{
    public class DbChargeTemp : DbChargeClient
    {
      
     
        public List<ServReval> ListReval; //Список оснований перерасчета
        public BaseServ SummaryServ; //Итого в счете, в т.ч. к оплате
        public List<BaseServ> ListServ; //Список услуг счета
        public CUnionServ CUnionServ; //Правила объединения услуг
        public List<BaseServ> ListKommServ; //Коммунальные услуги
   
   
        public decimal GetSumKOplate(Saldo finder, out Returns ret)
        {
            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return 0;

            string table = finder.pref + "_charge_" + finder.year_.ToString("0000").Substring(2, 2) +
                           tableDelimiter + "charge_" + finder.month_.ToString("00");
            if (!TempTableInWebCashe(conn_db, table))
            {
                ret = new Returns(false, "Начислений не найдено", -1);
                conn_db.Close();
                return 0;
            }
            string where = "";
            if (finder.nzp_supp > 0) where += " and nzp_supp = " + finder.nzp_supp;
            else if (finder.nzp_payer > 0)
                where += " and nzp_supp in (select nzp_supp from  " + Points.Pref + sKernelAliasRest +
                         "supplier where nzp_payer_princip = " + finder.nzp_payer + ")";
            string sql = "select  sum(sum_charge) as sum_charge from " + table + " where dat_charge is null and nzp_serv > 1 and nzp_kvar = " + finder.nzp_kvar + where;
            IDataReader reader;
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return 0;
            }
            decimal sumKOplate = 0;
            if (reader.Read())
                if (reader["sum_charge"] != DBNull.Value) sumKOplate = Convert.ToDecimal(reader["sum_charge"]);

            reader.Close();
            conn_db.Close();
            return sumKOplate;
        }


            public struct ParamCalcP
            {
                public string pref;
                public int calc_yy;
                public int calc_mm;
                public int prev_calc_yy;
                public int prev_calc_mm;
                public string sdat_s;
                public string sdat_po;

            }
            //int GEvent_nzp_user=0;

      

            public ParamCalcP getParamMonth(string pref)
            {
                ParamCalcP paramClose;
                paramClose.pref = pref;
                paramClose.calc_yy = Points.GetCalcMonth(new CalcMonthParams(pref)).year_;
                paramClose.calc_mm = Points.GetCalcMonth(new CalcMonthParams(pref)).month_;
                paramClose.prev_calc_mm = Points.GetCalcMonth(new CalcMonthParams(pref)).month_ - 1;
                paramClose.prev_calc_yy = Points.GetCalcMonth(new CalcMonthParams(pref)).year_;

                if (paramClose.prev_calc_mm == 0)
                {
                    --paramClose.prev_calc_yy;
                    paramClose.prev_calc_mm = 12;
                }
                paramClose.sdat_s = "01." + paramClose.calc_mm.ToString("00") + "." + paramClose.calc_yy.ToString("0000");
                paramClose.sdat_po = "28." + paramClose.calc_mm.ToString("00") + "." + paramClose.calc_yy.ToString("0000");
                return paramClose;
            }


            #region Перечень действий по закрытию месяца
            public ReturnsType CloseCalcMonth_actions(IDbConnection conn_db, IDbTransaction transaction, string pref, Finder finder)
            {
                return CloseCalcMonth_actions(conn_db, transaction, getParamMonth(pref), finder);
            }


            public ReturnsType CloseCalcMonth_actions(IDbConnection conn_db, IDbTransaction transaction, ParamCalcP paramcalc, Finder finder)
            {
                // не забыть создать таблицу сохранения изменений сальдового месяца   (в идеале создать триггер который фиксирует вмешательство в сальдовый месяц)
#if PG
                string sSQL_Text = "CREATE TABLE SALDO_DATE_LOG (nzp_ch serial not null ,SALDO_OLD  DATE, SALDO_NEW  DATE, TIME_WHEN timestamp default now() ,  USER_NAME CHAR(60)) ";
#else
            string sSQL_Text = "CREATE TABLE SALDO_DATE_LOG (nzp_ch serial not null ,SALDO_OLD  DATE, SALDO_NEW  DATE, TIME_WHEN DateTime year to second default current year to second ,  USER_NAME CHAR(60)) ";
#endif
                //ClassDBUtils.ExecSQL(sSQL_Text, conn_db, ClassDBUtils.ExecMode.Log);
                // Добавить запись в журнал 

                // Добавить новый сальдовый месяц пока с признаком -1 
                sSQL_Text = " insert into " + paramcalc.pref + "_data" + tableDelimiter + "saldo_date(month_,yearr,iscurrent,dat_saldo,prev_date) " +

#if PG
 " select distinct case when a.month_+1=13 then 1 else month_+1 end, case when a.month_+1=13 then yearr+1 else yearr end, -1,  " +
                           " date((dat_saldo +1)+interval '1  MONTH' - interval '1  DAY'),dat_saldo from " + paramcalc.pref + "_data" + tableDelimiter +
                           "saldo_date a where iscurrent=0   ";
#else
 " select distinct case when a.month_+1=13 then 1 else month_+1 end, case when a.month_+1=13 then yearr+1 else yearr end, -1,  " +
                       " date((dat_saldo +1)+1 units month -1 units day),dat_saldo from " + paramcalc.pref + "_data" + tableDelimiter +
            "saldo_date a where iscurrent=0   ";

#endif
                ClassDBUtils.ExecSQL(sSQL_Text, conn_db, transaction, ClassDBUtils.ExecMode.Log);

                // перевести старый месяц в разряд архивных 
                sSQL_Text = " update " + paramcalc.pref + "_data" + tableDelimiter + "saldo_date set iscurrent=1 where iscurrent=0 ";
                ClassDBUtils.ExecSQL(sSQL_Text, conn_db, transaction, ClassDBUtils.ExecMode.Log);

                // Активировать только что вставленный период 
                sSQL_Text = " update " + paramcalc.pref + "_data" + tableDelimiter + "saldo_date set iscurrent=0 where iscurrent=-1 ";
                ClassDBUtils.ExecSQL(sSQL_Text, conn_db, transaction, ClassDBUtils.ExecMode.Log);

                // Считать параметры нового месяца во все структуры 
                sSQL_Text = "select month_,yearr,iscurrent,dat_saldo,prev_date from " + paramcalc.pref + "_data" + tableDelimiter + "saldo_date where iscurrent<=0 ";

                int cur_month_ = 0;
                int cur_yearr = 0;
                string cur_dat_saldo;
                string cur_prev_date;
                int cur_iscurrent = 1;

                DataTable dtnote = ClassDBUtils.OpenSQL(sSQL_Text, conn_db, transaction, ClassDBUtils.ExecMode.Log).GetData();
                foreach (DataRow rr in dtnote.Rows)
                {
                    cur_month_ = Convert.ToInt32(rr["month_"]);
                    cur_yearr = Convert.ToInt32(rr["yearr"]);
                    cur_dat_saldo = Convert.ToString(rr["dat_saldo"]);
                    cur_prev_date = Convert.ToString(rr["prev_date"]);
                    cur_iscurrent = Convert.ToInt32(rr["iscurrent"]);
                    paramcalc.prev_calc_yy = paramcalc.calc_yy;
                    paramcalc.prev_calc_mm = paramcalc.calc_mm;
                    paramcalc.calc_yy = cur_yearr;
                    paramcalc.calc_mm = cur_month_;

                    MonitorLog.WriteLog("Текущий расчетный месяц: " + cur_month_.ToString("00") + "." + cur_yearr.ToString("0000"), MonitorLog.typelog.Info, 1, 2, true);
                    break;
                }
                if (cur_iscurrent == 0)
                {
                    insertInftocheckChMon(cur_month_.ToString("00"), cur_yearr.ToString("0000"), "Месяц успешно изменен на " + cur_month_.ToString("00") + "." + cur_yearr.ToString("0000"),
          "0", paramcalc.pref, "Месяц изменен", "1", conn_db, transaction, "1");
                    InsertSysEvent(conn_db, transaction, paramcalc.pref, 0, 0, 7428, "Месяц успешно изменен на " + cur_month_.ToString("00") + "." + cur_yearr.ToString("0000"));
                    return new ReturnsType(true, "Месяц успешно изменен на " + cur_month_.ToString("00") + "." + cur_yearr.ToString("0000"), -1);
                }
                else
                {
                    insertInftocheckChMon(cur_month_.ToString("00"), cur_yearr.ToString("0000"), "Месяц не изменен , текущий месяц остался " + cur_month_.ToString("00") + "." + cur_yearr.ToString("0000"),
                         "0", paramcalc.pref, "Ошибка изменения месяца", "2", conn_db, transaction, "0");
                    InsertSysEvent(conn_db, transaction, paramcalc.pref, 0, 0, 7428, "Месяц не изменен , текущий месяц остался " + cur_month_.ToString("00") + "." + cur_yearr.ToString("0000"));
                    return new ReturnsType(false, "Ошибка изменения месяца ", -1);
                };
                
            }
            #endregion Перечень действий по закрытию месяца

      
            public int insertInftocheckChMon(string month_, string yearr, string note, string nzp_grp, string pref, string name_prov, string status_, IDbConnection conn_db, IDbTransaction transaction, string is_critical)
            {
                string sSQL_Text;
                double count;

#if PG
                sSQL_Text = " delete from  " + Points.Pref + "_data.checkChMon where  month_='" + month_ + "'  and yearr='" + yearr + "' and (nzp_grp='" + nzp_grp + "' or nzp_grp='0' ) and pref='" + pref + "'";
#else
            sSQL_Text = " delete from  " + Points.Pref + "_data" + tableDelimiter + "checkChMon where  month_='" + month_ + "'  and yearr='" + yearr + "' and (nzp_grp='" + nzp_grp + "' or nzp_grp='0' ) and pref='" + pref + "'";
#endif
                ClassDBUtils.ExecSQL(sSQL_Text, conn_db, transaction, ClassDBUtils.ExecMode.Log);

#if PG
                sSQL_Text = " insert into " + Points.Pref + "_data.checkChMon (dat_check,month_,yearr,note,nzp_grp,pref,name_prov, status_, is_critical ) values ( now() ,'" +
#else
            sSQL_Text = " insert into " + Points.Pref + "_data" + tableDelimiter + "checkChMon (dat_check,month_,yearr,note,nzp_grp,pref,name_prov, status_, is_critical ) values ( current year to day ,'" +
#endif
 month_ + "','" + yearr + "','" + note + "'," + nzp_grp + ",'" + pref + "','" + name_prov + "','" + status_ + "','" + is_critical + "' )";
                ClassDBUtils.ExecSQL(sSQL_Text, conn_db, transaction, ClassDBUtils.ExecMode.Log);


                // Сформировать группу
#if PG
                sSQL_Text = " delete from " + pref + "_data.s_group where nzp_group= " + nzp_grp;
#else
            sSQL_Text = " delete from " + pref + "_data" + tableDelimiter + "s_group where nzp_group= " + nzp_grp;
#endif
                ClassDBUtils.ExecSQL(sSQL_Text, conn_db, transaction, ClassDBUtils.ExecMode.Log);

#if PG
                sSQL_Text = " insert into " + pref + "_data.s_group(nzp_group,ngroup) values( " + nzp_grp + ",'" + name_prov + "' ) ";
#else
            sSQL_Text = " insert into " + pref + "_data" + tableDelimiter + "s_group(nzp_group,ngroup) values( " + nzp_grp + ",'" + name_prov + "' ) ";
#endif
                ClassDBUtils.ExecSQL(sSQL_Text, conn_db, transaction, ClassDBUtils.ExecMode.Log);

                // почистить группу в данном банке
#if PG
                sSQL_Text = " delete from " + pref + "_data.link_group where nzp_group=" + nzp_grp + " ";
#else
            sSQL_Text = " delete from " + pref + "_data" + tableDelimiter + "link_group where nzp_group=" + nzp_grp + " ";
#endif
                ClassDBUtils.ExecSQL(sSQL_Text, conn_db, transaction, ClassDBUtils.ExecMode.Log);

                if (status_ == "2")
                {
                    // Заполнить группу если были ошибки
                    if (nzp_grp == "10006")
                    {
                        // проверка сальдо
#if PG
                        sSQL_Text = " insert into " + pref + "_data.link_group (nzp,nzp_group) select nzp_kvar," + nzp_grp + " from tmp_saldo";
#else
                    sSQL_Text = " insert into " + pref + "_data" + tableDelimiter + "link_group (nzp,nzp_group) select nzp_kvar," + nzp_grp + " from tmp_saldo";
#endif
                    }
                    else
                    {
                        // Проверка оплат
                        if (nzp_grp == "10000")
                        {
#if PG
                            sSQL_Text = " insert into " + pref + "_data.link_group (nzp,nzp_group) select distinct  nzp_kvar," + nzp_grp +
#else
                        sSQL_Text = " insert into " + pref + "_data" + tableDelimiter + "link_group (nzp,nzp_group) select distinct  nzp_kvar," + nzp_grp +
#endif
 " from ccc where sum_money>0 group by 1 having abs(sum(sum_money)-sum(money_del))>0.0001  ";
                        }
                        else
                            if (nzp_grp == "10003")
                            {
                                // проверка больших начислений
#if PG
                                sSQL_Text = " insert into " + pref + "_data.link_group (nzp,nzp_group) select nzp_kvar," + nzp_grp + " from bbb";
#else
                            sSQL_Text = " insert into " + pref + "_data" + tableDelimiter + "link_group (nzp,nzp_group) select nzp_kvar," + nzp_grp + " from bbb";
#endif
                            }
                            else if (nzp_grp == "10111")
                            {
#if PG
                                sSQL_Text = " insert into " + pref + "_data.link_group (nzp,nzp_group) select nzp_kvar," + nzp_grp + " from bbb";
#else
                            sSQL_Text = " insert into " + pref + "_data" + tableDelimiter + "link_group (nzp,nzp_group) select nzp_kvar," + nzp_grp + " from bbb";
#endif
                            }
                    }
#if PG
                    count = ClassDBUtils.ExecSQL(sSQL_Text, conn_db, ClassDBUtils.ExecMode.Log).resultAffectedRows;
#else
                ClassDBUtils.ExecSQL(sSQL_Text, conn_db, transaction, ClassDBUtils.ExecMode.Log);
                count = ClassDBUtils.GetAffectedRowsCount(conn_db, transaction);
#endif
                }
                return 0;
            }
     

            public bool InsertSysEvent(IDbConnection conn_db, IDbTransaction transaction, string pref, int nzp_obj, int nzp_user, int nzp_dict, string note)
            {
                // nzp_dict =7428
#if PG
                string sSQL_Text = " insert into " + pref + "_data.sys_events(DATE_,NZP_USER,NZP_DICT_EVENT,NZP,NOTE) " +
                                   " values( now(), " + nzp_user.ToString("") + ", " + nzp_dict.ToString("") + "," + nzp_obj.ToString("") + ",'" + note + "' ) ";
#else
            string sSQL_Text = " insert into " + pref + "_data" + tableDelimiter + "sys_events(DATE_,NZP_USER,NZP_DICT_EVENT,NZP,NOTE) " +
                               " values( sysdate year to fraction, " + nzp_user.ToString("") + ", " + nzp_dict.ToString("") + "," + nzp_obj.ToString("") + ",'" + note + "' ) ";
#endif
                ClassDBUtils.ExecSQL(sSQL_Text, conn_db, transaction, ClassDBUtils.ExecMode.Log);
                return true;
            }





          
        

    }
}
