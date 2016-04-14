using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bars.KP50.Utils;
using STCLINE.KP50.Interfaces;
using System.Data;
using STCLINE.KP50.Global;
using STCLINE.KP50.IFMX.Kernel.source.CommonType;

namespace STCLINE.KP50.DataBase
{
    public partial class DBSendedMoneyServer : DataBaseHeadServer
    {
        public ReturnsObjectType<DataTable> LoadSendedMoney(MoneySended finder)
        {
            string sql = "";
            Returns ret = new Returns();

            DataTable table = null;
            DataTable table2 = null;

            try
            {
                if (finder.nzp_user <= 0) throw new Exception("Не задан пользователь");
                if (finder.nzp_supp <= 0 && finder.nzp_payer <= 0) throw new Exception("Не определен договор или поставщик");
                DateTime dat_oper = DateTime.MinValue;
                if (!DateTime.TryParse(finder.dat_oper, out dat_oper)) throw new Exception("Не задана дата операции");

                if (finder.pref.Trim() == "") finder.pref = Points.Pref;

                // Ограничения пользователя
                string sqlRoleFilter = "";
                string strKeys = "";
                if (finder.RolesVal != null)
                {
                    foreach (_RolesVal role in finder.RolesVal)
                    {
                        if (role.tip == Constants.role_sql)
                        {
                            strKeys = role.val.Trim(new char[] { ' ', ',' });
                            if (role.kod == Constants.role_sql_payer)
                                sqlRoleFilter += " and d.nzp_payer in (" + strKeys + ") ";
                        }
                    }
                }

                string sended = finder.pref + "_fin_" + (Convert.ToDateTime(finder.dat_oper).Year % 100).ToString("00") + tableDelimiter + "fn_sended";
                string distrib = Points.Pref + "_fin_" + (Convert.ToDateTime(finder.dat_oper).Year % 100).ToString("00") + tableDelimiter + "fn_distrib_dom_" + (Convert.ToDateTime(finder.dat_oper).Month % 100).ToString("00");
                string dogovor = finder.pref + "_data" + tableDelimiter + "fn_dogovor";
                string osnov = finder.pref + "_data" + tableDelimiter + "fn_osnov";
                string bank = finder.pref + "_data" + tableDelimiter + "fn_bank";
                string supp = Points.Pref + "_kernel" + tableDelimiter + "supplier";
                string services = finder.pref + "_kernel" + tableDelimiter + "services";
                string payer = finder.pref + "_kernel" + tableDelimiter + "s_payer";
                string dogovor_bank = Points.Pref + "_data" + DBManager.tableDelimiter + "fn_dogovor_bank";
                string s_bank = Points.Pref + "_kernel" + DBManager.tableDelimiter + "s_bank";

                // Создать временную таблицу
                string tshutable = "tshu_" + finder.nzp_user.ToString().Trim() + "_fn_sended";
                string tshutable_p = tshutable.Trim() + "_p";
                
                sql = " drop table " + tshutable;
                ExecSQL(sql, false);

                sql = " drop table " + tshutable_p;
                ExecSQL(sql, false);

                sql = " Create temp table " + tshutable_p + " (" +
                    " nzp_supp     integer, " +
                    " nzp_payer    integer, " +
                    " cnt          integer, " +
                    " osn_priznak  integer) " + DBManager.sUnlogTempTable;
                ret = ExecSQL(sql);
                if (!ret.result) throw new Exception("Ошибка создания временной таблицы\n" + ret.text);

                sql = " insert into " + tshutable_p + " (nzp_supp, nzp_payer, cnt, osn_priznak) " + 
                    " select d.nzp_supp, d.nzp_payer, count(*) cnt, " +
                    sNvlWord + "((select " + sNvlWord + "(v.priznak_perechisl, 1) from " + dogovor + " v where v.nzp_fd = (select min(a.nzp_fd) from " + dogovor + " a where a.nzp_payer = d.nzp_payer and a.nzp_supp = d.nzp_supp)), 0) as osn_priznak " +
                    " from " + dogovor + " d where 1=1 " + sqlRoleFilter;
                if (finder.nzp_supp > 0) sql += " and d.nzp_supp = " + finder.nzp_supp.ToString();
                if (finder.nzp_payer > 0) sql += " and d.nzp_payer = " + finder.nzp_payer.ToString();
                if (finder.nzp_fd > 0) sql += " and d.nzp_fd = " + finder.nzp_fd.ToString();
                sql += " group by 1,2 ";

                ret = ExecSQL(sql);
                if (!ret.result) throw new Exception(ret.text);

                sql = " select MAX(cnt) mcnt from " + tshutable_p;
                IntfResultTableType intfResTable = OpenSQL(sql);
                if (intfResTable.resultCode < 0) throw new Exception("Ошибка получения количества договоров" + "\n" + intfResTable.resultMessage);
                
                Int32 cntFd = 0;

                if ((intfResTable.resultData != null) && (intfResTable.resultData.Rows.Count > 0))
                    if (intfResTable.resultData.Rows[0]["mcnt"].ToString().Trim() != "")
                    {
                        cntFd = Convert.ToInt32(intfResTable.resultData.Rows[0]["mcnt"]);
                    }

                sql = " create temp table " + tshutable + "( " +
                    "   ordering SERIAL,           " +
                    "   dat_oper DATE,             " +
                    "   nzp_supp INTEGER,          " +
                    "   name_supp VARCHAR(100),    " +
                    "   nzp_serv INTEGER,          " +
                    "   service VARCHAR(100),      " +
                    "   nzp_payer INTEGER,         " +
                    "   payer VARCHAR(200),        " +
                    "   sum_charge " + DBManager.sDecimalType + "(14,2),    " +
                    "   sum_must " + DBManager.sDecimalType + "(14,2),    " +
                    "   sum_send " + DBManager.sDecimalType + "(14,2),    " +
                    "   cnt_fd INTEGER DEFAULT 0,  " +
                    "   sum_send_fd " + DBManager.sDecimalType + "(14,2), " +
                    "   osn_priznak INTEGER, " +
                    "   sum_bank " + DBManager.sDecimalType + "(14,2) default 0.00, " +
                    "   sum_send_p " + DBManager.sDecimalType + "(14,2) default 0.00, " +
                    "   priznak INTEGER default 0";
                
                for (int iCount = 1; iCount <= cntFd; iCount++)
                {
                    sql += "," +
                        " nzp_fd_" + iCount.ToString() + " INTEGER, " +
                        " sum_send_" + iCount.ToString() + " " + DBManager.sDecimalType + "(13,2) DEFAULT 0.00, " +
                        " s_osnov_" + iCount.ToString() + " VARCHAR(200), " + 
                        " target_" + iCount.ToString() + " VARCHAR(200), " + 
                        " pp_" + iCount.ToString() + " VARCHAR(40), " + 
                        " num_pp_" + iCount.ToString() + " INTEGER, " + 
                        " dat_pp_" + iCount.ToString() + " DATE, " +
                        " max_sum_" + iCount.ToString() + " " + DBManager.sDecimalType + "(14,2), " +
                        " min_sum_" + iCount.ToString() + " " + DBManager.sDecimalType + "(14,2), " +
                        " sum_serv_" + iCount + " " + DBManager.sDecimalType + "(14,2) default 0.00, " +
                        " bank_cnt_" + iCount.ToString() + " integer ";
                }
                sql += ") " + DBManager.sUnlogTempTable;

                ret = ExecSQL(sql);
                if (!ret.result) throw new Exception("Ошибка создания временой таблицы" + "\n" + ret.text);

#if PG
                string ordering_field = "";
                string ordering_value = "";
                string group_by = "1,2,3,4,5,6,7,11,12,13";
#else
                string ordering_field = "ordering,";
                string ordering_value = "0,";
                string group_by = "1,2,3,4,5,6,7,8,12,13,14 ";
#endif

                sql = " insert into " + tshutable + "(" + ordering_field + "dat_oper, nzp_supp, name_supp, nzp_serv, service, nzp_payer, payer, sum_charge, sum_must, sum_send, cnt_fd, sum_send_fd, osn_priznak)  " +
                    " select " + ordering_value + " d.dat_oper, d.nzp_supp, a.name_supp," +
                    // услуга
                    " case when " + sNvlWord + "(pp.osn_priznak, 0) > 1 then d.nzp_serv else 0 end, " +
                    " case when " + sNvlWord + "(pp.osn_priznak, 0) > 1 then s.service else '' end, " +
                        // поставщик
                    " d.nzp_payer, p.payer, " +
                    " sum(" + sNvlWord + "(d.sum_charge, 0) + " + sNvlWord + "(d.sum_reval, 0)), " +
                    " sum(" + sNvlWord + "(d.sum_in, 0) + " + sNvlWord + "(d.sum_charge, 0) + " + sNvlWord + "(d.sum_reval, 0)), " +
                    " sum(d.sum_send), " +// исправил Андрей К. 21.04.2013
                    " pp.cnt, 0, " +
                    sNvlWord + "(pp.osn_priznak, 0) " +
                    " from " + supp + " a, " + services + " s, " + payer + " p," + distrib + " d " +
                        " left outer join " + tshutable.Trim() + "_p pp on pp.nzp_supp = d.nzp_supp and pp.nzp_payer = d.nzp_payer " +
                    " where 1=1 and d.nzp_supp = a.nzp_supp and d.nzp_serv = s.nzp_serv and d.nzp_payer = p.nzp_payer " +
                    " " + sqlRoleFilter + " and d.dat_oper = " + Utils.EStrNull(Convert.ToDateTime(finder.dat_oper).ToShortDateString());
                if (finder.nzp_supp > 0) sql += " and d.nzp_supp = " + finder.nzp_supp.ToString();
                if (finder.nzp_payer > 0) sql += " and d.nzp_payer = " + finder.nzp_payer.ToString();
                if (finder.nzp_serv > 0) sql += " and d.nzp_serv = " + finder.nzp_serv.ToString();
                if (finder.nzp_fd > 0) sql += " and d.nzp_fd = " + finder.nzp_fd.ToString();
                sql += " group by " + group_by;

                ret = ExecSQL(sql);
                if (!ret.result) throw new Exception("Ошибка выбора данных" + "\n" + ret.text);

                #region создать индексы
                sql = " create index tix_" + tshutable + "_01 on " + tshutable + " (dat_oper) ";
                ret = ExecSQL(sql);
                if (!ret.result) throw new Exception(ret.text);
                
                sql = " create index tix_" + tshutable + "_02 on " + tshutable + " (nzp_supp) ";
                ret = ExecSQL(sql);
                if (!ret.result) throw new Exception(ret.text);

                sql = " create index tix_" + tshutable + "_03 on " + tshutable + " (nzp_serv) ";
                ret = ExecSQL(sql);
                if (!ret.result) throw new Exception(ret.text);

                sql = " create index tix_" + tshutable + "_04 on " + tshutable + " (nzp_payer) ";
                ret = ExecSQL(sql);
                if (!ret.result) throw new Exception(ret.text);

                sql = " create index tix_" + tshutable + "_05 on " + tshutable + " (ordering) ";
                ret = ExecSQL(sql);
                if (!ret.result) throw new Exception(ret.text);

                sql = DBManager.sUpdStat + " " + tshutable;
                ret = ExecSQL(sql);
                if (!ret.result) throw new Exception(ret.text);
                #endregion

                sql = " select * from " + tshutable;
                intfResTable = OpenSQL(sql);
                if (intfResTable.resultCode < 0) throw new Exception("Ошибка получения данных" + "\n" + intfResTable.resultMessage);
                
                table = intfResTable.GetData();

                Int32 osn_priznak = 0;
                
                foreach (DataRow dr in table.Rows)
                {
                    osn_priznak = Convert.ToInt32(dr["osn_priznak"]);

                    sql = " select distinct sum(" + DBManager.sNvlWord + "(d.sum_send,0)) sum_send, v.nzp_fd, " +
                        "   trim(" + DBManager.sNvlWord + "(c.osnov,' '))||' № '||trim(" + DBManager.sNvlWord + "(v.num_dog,' '))|| (case when v.dat_dog is null then '' else ' от '|| v.dat_dog end) s_osnov, " +
                        "   v.target, d.num_pp, d.dat_pp, v.max_sum, v.min_sum, " +
                        "   (select count(*) from " + dogovor_bank + " fdb where v.nzp_fd = fdb.nzp_fd) as bank_cnt " +
                        " from " + dogovor + " v " +
                        "   left outer join " + sended + " d on d.nzp_fd = v.nzp_fd and d.dat_oper = " + Utils.EStrNull(Convert.ToDateTime(finder.dat_oper).ToShortDateString()) +
                        "   left outer join " + osnov + " c on v.nzp_osnov = c.nzp_osnov " +
                        " where v.nzp_payer = " + Convert.ToInt32(dr["nzp_payer"]).ToString() +
                        "   and v.nzp_supp = " + Convert.ToInt32(dr["nzp_supp"]).ToString() +              
                        (osn_priznak > 1 ? " and d.nzp_serv = " + Convert.ToInt32(dr["nzp_serv"]) : "") + " "+ sqlRoleFilter +
                        " group by 2,3,4,5,6,7,8 " + 
                        " order by 1 ";

                    intfResTable = OpenSQL(sql);
                    if (intfResTable.resultCode < 0) throw new Exception("Ошибка получения данных" + "\n" + intfResTable.resultMessage);
                    
                    DataTable tableSend = intfResTable.GetData();
                    if (tableSend == null) throw new Exception("Ошибка получения данных");
                    
                    Int32 ordering = Convert.ToInt32(dr["ordering"]);
                    int iNumRow = 0;
                    foreach (DataRow drSend in tableSend.Rows)
                    {
                        iNumRow++;
                        if (iNumRow > cntFd) break;

                        string sqlSend = "";

                        sqlSend = " update  " + tshutable + " set (nzp_fd_" + iNumRow.ToString() + "," + 
                            " sum_send_" + iNumRow.ToString() + "," + 
                            " s_osnov_" + iNumRow.ToString() + ", " +
                            " target_" + iNumRow.ToString() + ", " +
                            " num_pp_" + iNumRow.ToString() + "," + 
                            " dat_pp_" + iNumRow.ToString() + ", " +
                            " max_sum_" + iNumRow.ToString() + "," + 
                            " min_sum_" + iNumRow.ToString() + ", " +
                            " bank_cnt_" + iNumRow.ToString() + ") =  " +
                            " (" + Utils.EStrNull(Convert.ToString(drSend["nzp_fd"])) + ", " +
                            Utils.EStrNull(Convert.ToString(drSend["sum_send"])) + ", " +
                            Utils.EStrNull(Convert.ToString(drSend["s_osnov"])) + ", " +
                            Utils.EStrNull(Convert.ToString(drSend["target"])) + ", " +
                            Utils.EStrNull(Convert.ToString(drSend["num_pp"])) + ", ";
                        // дата платежного поручения: если не заполнена, то выставлять текущий операционный день
                        if (drSend["dat_pp"] == DBNull.Value) sqlSend += Utils.EStrNull(Points.DateOper.ToShortDateString()) + ", ";
                        else sqlSend += Utils.EStrNull(Convert.ToDateTime(drSend["dat_pp"]).ToShortDateString()) + ", ";
                        // лимит
                        if (drSend["max_sum"] == DBNull.Value) sqlSend += " NULL, "; else sqlSend += Convert.ToDecimal(drSend["max_sum"]) + ", ";
                        // минимальная сумма к перечислению
                        if (drSend["min_sum"] == DBNull.Value) sqlSend += " NULL, "; else sqlSend += Convert.ToDecimal(drSend["min_sum"]) + ", ";
                        // количество банков, через которые должны перечисляться деньги по договору
                        if (drSend["bank_cnt"] == DBNull.Value) sqlSend += " NULL) "; else sqlSend += Convert.ToInt32(drSend["bank_cnt"]) + ") ";

                        sqlSend += " where ordering = " + Convert.ToInt32(dr["ordering"]).ToString();

                        ret = ExecSQL(sqlSend);
                        if (!ret.result) throw new Exception("Ошибка обновления данных" + "\n" + ret.text);
                    }
                }

                if (cntFd > 0)
                {
                    sql = " update " + tshutable + " set sum_send_fd = ";
                    for (int iCntSum = 1; iCntSum <= cntFd; iCntSum++)
                    {
                        sql += " sum_send_" + iCntSum.ToString();
                        if (iCntSum < cntFd) sql += " + ";
                    }
                    ret = ExecSQL(sql);
                    if (!ret.result) throw new Exception("Ошибка получения суммы по договорам" + "\n" + ret.text);
                }

                sql = " select count(*) as cnt from " + tshutable;
                intfResTable = OpenSQL(sql);
                
                table = intfResTable.GetData();
                Int32 cntRecord = Convert.ToInt32(table.Rows[0]["cnt"]);

                #region 1. Объединить суммы по услугам для договоров, у которых не установлен признак "Перечислять в разрезе услуг"
                //--------------------------------------------------------------------------------------------------------------------------------------------------------------
                if (cntFd > 0)
                {
                    sql = "drop table tmp_target";
                    ExecSQL(sql, false);

                    sql = "create temp table tmp_target (ordering integer, target VARCHAR(200))" + DBManager.sUnlogTempTable;
                    ret = ExecSQL(sql);
                    if (!ret.result) throw new Exception("1.1." + ret.text);

                    sql = "insert into tmp_target (ordering, target) " +
                        " select ordering, target_1 from " + tshutable +
                        " where osn_priznak = 1 and cnt_fd > 0 ";
                    ret = ExecSQL(sql);
                    if (!ret.result) throw new Exception("1.2." + ret.text);

                    sql = "update " + tshutable + " set service = (select a.target from tmp_target a where a.ordering = " + tshutable + ".ordering), nzp_serv = 0 " +
                        " where ordering in (select a.ordering from tmp_target a)";
                    ret = ExecSQL(sql);
                    if (!ret.result) throw new Exception("1.3." + ret.text);
                }
                //--------------------------------------------------------------------------------------------------------------------------------------------------------------
                #endregion

                #region 2. Распределить суммы (кнопка "Распределить суммы")
                //--------------------------------------------------------------------------------------------------------------------------------------------------------------
                if (finder.sum_send > 0 && cntFd > 0)
                {
                    ret = DistrSumLoadSended(tshutable, "ordering", "sum_must", "sum_send_1", finder.sum_send, " and cnt_fd > 0 ");
                    if (!ret.result) throw new Exception(ret.text);
                }
                //--------------------------------------------------------------------------------------------------------------------------------------------------------------
                #endregion

                #region 3. Зачислить суммы
                //--------------------------------------------------------------------------------------------------------------------------------------------------------------
                
                #region Зачислить суммы за опер. день или зачислить все
                //--------------------------------------------------------------------------------------------------------------------------------------------------------------
                if (cntFd > 0 && finder.copy_id > 0)
                {
                    string sql_sum = "";

                    // зачесть за опер. день
                    if (finder.copy_id == 1) sql_sum = "sum(" + sNvlWord + "(d.sum_charge, 0) + " + sNvlWord + "(d.sum_reval, 0))";
                    // зачесть все
                    else sql_sum = "sum(" + sNvlWord + "(d.sum_in, 0) + " + sNvlWord + "(d.sum_charge, 0) + " + sNvlWord + "(d.sum_reval, 0))";

                    for (int i = 1; i <= cntFd; i++)
                    {
                        // определить суммы по договорам и банкам, которые указаны в договорах
                        sql = "drop table tmp_fd_sum_send";
                        ExecSQL(sql, false);

                        sql = " create temp table tmp_fd_sum_send (ordering integer, fd_sum_send " + DBManager.sDecimalType + " (14,2) ) " + DBManager.sUnlogTempTable;
                        ret = ExecSQL(sql);
                        if (!ret.result) throw new Exception("3.1." + ret.text);

                        sql = " insert into tmp_fd_sum_send (ordering, fd_sum_send) " +
                            " select t.ordering, " + sql_sum +
                            " from " + distrib + " d, " + tshutable + " t, " + dogovor_bank + " fdb, " + s_bank + " b " +
                            " where t.nzp_fd_" + i + " = fdb.nzp_fd " +
                            "   and fdb.nzp_bank = b.nzp_bank " +
                            "   and b.nzp_payer  = d.nzp_bank " +
                            "   and d.dat_oper = " + Utils.EStrNull(Convert.ToDateTime(finder.dat_oper).ToShortDateString()) +
                            "   and d.nzp_supp = t.nzp_supp " +
                            "   and d.nzp_payer = t.nzp_payer " +
                            "   and d.nzp_serv = (case when t.nzp_serv > 0 then t.nzp_serv else d.nzp_serv end) " +
                            "   and t.bank_cnt_" + i + " > 0" +
                            " group by 1";
                        ret = ExecSQL(sql);
                        if (!ret.result) throw new Exception("3.2." + ret.text);

                        sql = " update " + tshutable + " set sum_send_" + i + " = (select a.fd_sum_send from tmp_fd_sum_send a where " + tshutable + ".ordering = a.ordering) " +
                            " where ordering in (select a.ordering from tmp_fd_sum_send a) ";
                        ret = ExecSQL(sql);
                        if (!ret.result) throw new Exception("3.3." + ret.text);
                    }

                    for (int i = 1; i <= cntFd; i++)
                    {
                        // cобрать все суммы по банкам
                        sql = "update " + tshutable + " set sum_bank = sum_bank + sum_send_" + i + " where bank_cnt_" + i + " > 0";
                        ret = ExecSQL(sql);
                        if (!ret.result) throw new Exception("3.4." + ret.text);
                    }

                    // определить сумму, которая не распределяется по банкам
                    sql = "update " + tshutable + " set sum_send_p = " + (finder.copy_id == 1 ? "sum_charge" : "sum_must") + " - sum_bank";
                    ret = ExecSQL(sql);
                    if (!ret.result) throw new Exception("3.5." + ret.text);

                    // положить эту нераспределенную сумму в первый договор, где не указаны банки 
                    // и в который еще не сохранили нераспределенную сумму (priznak = 0)
                    for (int i = 1; i <= cntFd; i++)
                    {
                        sql = "drop table tmp_ordering_sum_send";
                        ExecSQL(sql, false);

                        sql = " create temp table tmp_ordering_sum_send (ordering integer) " + DBManager.sUnlogTempTable;
                        ret = ExecSQL(sql);
                        if (!ret.result) throw new Exception("3.6." + ret.text);

                        // определить нужные договоры 
                        sql = "insert into tmp_ordering_sum_send (ordering) " +
                            "select ordering from " + tshutable + " where bank_cnt_" + i + " = 0 and priznak = 0";
                        ret = ExecSQL(sql);
                        if (!ret.result) throw new Exception("3.7." + ret.text);

                        // сохранить сумму
                        sql = "update " + tshutable + " set sum_send_" + i + " = sum_send_p, priznak = 1 " +
                            " where ordering in (select ordering from tmp_ordering_sum_send) ";
                        ret = ExecSQL(sql);
                        if (!ret.result) throw new Exception("3.8." + ret.text);
                    }
                }
                //--------------------------------------------------------------------------------------------------------------------------------------------------------------
                #endregion

                #region ограничить суммы верхним и нижним потолком
                //--------------------------------------------------------------------------------------------------------------------------------------------------------------
                if (cntFd > 0 && (finder.sum_send > 0 || finder.copy_id > 0))
                {
                    for (int i = 1; i <= cntFd; i++)
                    {
                        // ограничить суммы по договорам, у которых нет признака "Перечислять в разрезе услуг"
                        sql = "update " + tshutable + " set sum_send_" + i + " = (case when sum_send_" + i + " < min_sum_" + i + " and min_sum_" + i + " > 0 then 0.00 " +
                              " else case when sum_send_" + i + " > max_sum_" + i + " and max_sum_" + i + " > 0 then max_sum_" + i + " " +
                              " else sum_send_" + i + " end end) " +
                            " where cnt_fd > 0 and osn_priznak = 1";
                        ret = ExecSQL(sql);
                        if (!ret.result) throw new Exception("3.9." + ret.text);
                        
                        // по договорам, у которых перечисление в разрезе услуг
                        sql = "drop table tmp_sum_serv";
                        ExecSQL(sql, false);

                        // получить коды УК и поставщиков и итоговые суммы по услугам
                        sql = "create temp table tmp_sum_serv (" +
                            " supp_id integer, payer_id integer, serv_sum " + sDecimalType + "(14,2))" + (DBManager.tableDelimiter == ":" ? " with no log" : "");
                        ret = ExecSQL(sql);
                        if (!ret.result) throw new Exception("3.10." + ret.text);
                        
                        sql = " insert into tmp_sum_serv (supp_id, payer_id, serv_sum) " +
                            " select nzp_supp, nzp_payer, sum(" + DBManager.sNvlWord + "(sum_send_" + i + ", 0.00)) from " + tshutable +
                            " where cnt_fd > 0 and osn_priznak = 2 " +
                            " group by 1,2";
                        ret = ExecSQL(sql);
                        if (!ret.result) throw new Exception("3.11." + ret.text);
                        
                        // cохранить итоговые суммы по услугам
                        sql = " update " + tshutable + " set " +
                            " sum_serv_" + i + "  = (select t.serv_sum from tmp_sum_serv t where t.payer_id = " + tshutable + ".nzp_payer and t.supp_id = " + tshutable + ".nzp_supp) " +
                            " where nzp_supp in (select supp_id from tmp_sum_serv) " +
                            "   and nzp_payer in (select payer_id from tmp_sum_serv) ";
                        ret = ExecSQL(sql);
                        if (!ret.result) throw new Exception("3.12." + ret.text);
                        
                        // ограничить суммы нижним потолком
                        sql = " update " + tshutable + " set sum_send_" + i + " = 0 " +
                            " where sum_serv_" + i + "  < min_sum_" + i + " and osn_priznak = 2";
                        ret = ExecSQL(sql);
                        if (!ret.result) throw new Exception("3.13." + ret.text);
                        
                        // получить коды поставщиков и УК и верхние потолки
                        sql = "select distinct nzp_payer, nzp_supp, max_sum_" + i + " from " + tshutable +
                            " where sum_serv_" + i + " > max_sum_" + i + " and osn_priznak = 2";
                        intfResTable = OpenSQL(sql);
                        if (intfResTable.resultCode < 0) throw new Exception("3.14." + intfResTable.resultMessage);
                        
                        table2 = intfResTable.GetData();

                        for (int j = 0; j < table2.Rows.Count; j++)
                        {
                            int nzp_payer = Convert.ToInt32(table2.Rows[j]["nzp_payer"]);
                            int nzp_supp = Convert.ToInt32(table2.Rows[j]["nzp_supp"]);
                            decimal max_sum = Convert.ToDecimal(table2.Rows[j]["max_sum_" + i]);

                            // выполнить перераспределение по услугам, ограничившись верхним потолком
                            ret = DistrSumLoadSended(tshutable, "ordering", "sum_send_" + i, "sum_send_" + i, max_sum, " and cnt_fd > 0 and nzp_payer = " + nzp_payer + " and nzp_supp = " + nzp_supp);
                            if (!ret.result) throw new Exception("3.15." + ret.text);
                        }
                    }
                }
                //--------------------------------------------------------------------------------------------------------------------------------------------------------------
                #endregion

                //--------------------------------------------------------------------------------------------------------------------------------------------------------------
                #endregion

                #region 4. Определить максимальный номер платежного поручения за день
                //-----------------------------------------------------------
                sql = "select max(" + sNvlWord + "(d.num_pp, 0)) as num_pp from " + sended + " d  where d.dat_oper = " + Utils.EStrNull(Convert.ToDateTime(finder.dat_oper).ToShortDateString());
                intfResTable = OpenSQL(sql);
                if (intfResTable.resultCode < 0) throw new Exception("Ошибка определения номера платежного поручения" + "\n" + intfResTable.resultMessage);
                
                table = intfResTable.GetData();

                Int32 num_pp = 0;

                if ((table != null) && (table.Rows.Count > 0))
                    if (table.Rows[0]["num_pp"].ToString().Trim() != "")
                    {
                        num_pp = Convert.ToInt32(table.Rows[0]["num_pp"]);
                    }
                //-----------------------------------------------------------
                #endregion

                sql = " select * from " + tshutable + " order by 2,4,8,6,ordering";
                intfResTable = OpenSQL(sql);
                if (intfResTable.resultCode < 0) throw new Exception("Ошибка получения данных" + "\n" + intfResTable.resultMessage);
                
                table = intfResTable.GetData();
                if (table == null) throw new Exception("Ошибка получения данных");
                
                sql = " drop table " + tshutable;
                ExecSQL(sql, false);

                #region 5. Добавить в основание основного договора максимальную и минимальную ежедневную сумму к перечислению + проставить номера платежных поручений
                //--------------------------------------------------------------------------------------------------------------------------------------------------------------
                string s_sums = "";
                int cnt = 0;

                foreach (DataRow dr in table.Rows)
                {
                    if (Convert.ToString(dr["cnt_fd"]).Trim() != "")
                    {
                        cnt = Convert.ToInt32(dr["cnt_fd"]);
                        if (cnt > 0)
                        {
                            for (int j = 1; j <= cnt; j++)
                            {
                                if (Convert.ToString(dr["num_pp_" + j]).Trim() == "")
                                {
                                    num_pp++;
                                    dr["num_pp_" + j] = num_pp;
                                }

                                s_sums = "";
                                if (Convert.ToString(dr["s_osnov_" + j]).Trim() != "") s_sums = (string)dr["s_osnov_" + j];
                                if (Convert.ToString(dr["max_sum_" + j]).Trim() != "")
                                {
                                    if (s_sums != "") s_sums += ",<br>";
                                    s_sums = "макс. сумма: " + Convert.ToDecimal(dr["max_sum_" + j]).ToString("N2");
                                }

                                if (Convert.ToString(dr["min_sum_" + j]).Trim() != "")
                                {
                                    if (s_sums != "") s_sums += ",<br>";
                                    s_sums = "мин. сумма: " + Convert.ToDecimal(dr["min_sum_" + j]).ToString("N2");
                                }

                                dr["s_osnov_" + j] = s_sums;
                            }
                        }
                    }
                }

                table.AcceptChanges();
                //--------------------------------------------------------------------------------------------------------------------------------------------------------------
                #endregion

                return new ReturnsObjectType<DataTable>() { returnsData = table, tag = cntRecord };
            }
            catch (Exception ex)
            {
                return new ReturnsObjectType<DataTable>() { returnsData = null, tag = -1, text = ex.Message };
            }
        }

        private Returns DistrSumLoadSended(string table, string keyField, string sumSmpField, string targetField, decimal sum_in, string where)
        {
            Returns ret = new Returns();

            try
            {
                string sql = "drop table tmp_sum_distr";
                ExecSQL(sql, false);

                sql = "create temp table tmp_sum_distr (" +
                    " id          integer, " +
                    " sum_in      " + sDecimalType + "(14,2), " +
                    " sum_smp     " + sDecimalType + "(14,2), " +
                    " sum_smp_tot " + sDecimalType + "(14,2), " +
                    " coeff float, " +
                    " sum_out " + sDecimalType + "(14,2), " +
                    " sum_out_tot " + sDecimalType + "(14,2) " + ") " + DBManager.sUnlogTempTable;
                ret = ExecSQL(sql);
                if (!ret.result) throw new Exception(ret.text);

                sql = " insert into tmp_sum_distr (id, sum_smp, sum_in) " +
                    " select " + keyField + ", " + sumSmpField + ", " + sum_in + " from " + table + " where " + sumSmpField + " > 0 " + where;
                ret = ExecSQL(sql);
                if (!ret.result) throw new Exception(ret.text);

                sql = "drop table tmp_sum_smp_tot";
                ExecSQL(sql, false);

                sql = "create temp table tmp_sum_smp_tot (sum_smp_tot " + sDecimalType + "(14,2)) " + DBManager.sUnlogTempTable;
                ret = ExecSQL(sql);
                if (!ret.result) throw new Exception(ret.text);

                sql = " insert into tmp_sum_smp_tot (sum_smp_tot) select sum(sum_smp) from tmp_sum_distr ";
                ret = ExecSQL(sql);
                if (!ret.result) throw new Exception(ret.text);

                sql = " update tmp_sum_distr set sum_smp_tot = (select sum_smp_tot from tmp_sum_smp_tot)";
                ret = ExecSQL(sql);
                if (!ret.result) throw new Exception(ret.text);

                sql = " update tmp_sum_distr set coeff = sum_smp / sum_smp_tot where sum_smp_tot <> 0";
                ret = ExecSQL(sql);
                if (!ret.result) throw new Exception(ret.text);

                sql = " update tmp_sum_distr set sum_out = coeff * sum_in";
                ret = ExecSQL(sql);
                if (!ret.result) throw new Exception(ret.text);

                sql = "drop table tmp_sum_out_tot";
                ExecSQL(sql, false);

                sql = "create temp table tmp_sum_out_tot (sum_out_tot " + sDecimalType + "(14,2)) " + DBManager.sUnlogTempTable;
                ret = ExecSQL(sql);
                if (!ret.result) throw new Exception(ret.text);

                sql = " insert into tmp_sum_out_tot (sum_out_tot) select sum(sum_out) from tmp_sum_distr ";
                ret = ExecSQL(sql);
                if (!ret.result) throw new Exception(ret.text);

                sql = " update tmp_sum_distr set sum_out_tot =  (select sum_out_tot from tmp_sum_out_tot)";
                ret = ExecSQL(sql);
                if (!ret.result) throw new Exception(ret.text);

                sql = "drop table tmp_sum_out_max_id";
                ExecSQL(sql, false);

                sql = "create temp table tmp_sum_out_max_id (id integer) " + DBManager.sUnlogTempTable;
                ret = ExecSQL(sql);
                if (!ret.result) throw new Exception(ret.text);

                sql = " insert into tmp_sum_out_max_id (id) " + 
                    " select id from tmp_sum_distr " +
                    " where sum_out = (select max(a.sum_out) from tmp_sum_distr a)";
                ret = ExecSQL(sql);
                if (!ret.result) throw new Exception(ret.text);

                sql = " update tmp_sum_distr set sum_out =  sum_out + sum_in - sum_out_tot " +
                    " where id = (select max(id) from tmp_sum_out_max_id) ";
                ret = ExecSQL(sql);
                if (!ret.result) throw new Exception(ret.text);

                sql = " update " + table + " set " + targetField + " = (select t.sum_out from tmp_sum_distr t where t.id = " + table + "." + keyField + ") " +
                    " where " + keyField + " in (select t.id from tmp_sum_distr t) ";
                ret = ExecSQL(sql);
                if (!ret.result) throw new Exception(ret.text);
            }
            catch (Exception ex)
            {
                return new Returns(false, ex.Message);
            }
            finally
            {
                ExecSQL("drop table tmp_sum_distr", false);
                ExecSQL("drop table tmp_sum_smp_tot", false);
                ExecSQL("drop table tmp_sum_out_tot", false);
                ExecSQL("drop table tmp_sum_out_max_id", false);
            }

            return ret;
        }

        public Returns SaveSendedMoney(List<MoneySended> list)
        {
            string sql = "";
            Returns ret = new Returns();

            try
            {
                if ((list == null) || (list != null && list.Count == 0)) throw new Exception("Не заданы суммы перечислений");
                if (list[0].nzp_user <= 0) throw new Exception("Не задан пользователь");

                DateTime dat_oper = DateTime.MinValue;
                
                for (int iCount = 0; iCount < list.Count; iCount++)
                {
                    if (list[iCount].nzp_supp <= 0) return new Returns(false, "Не задан договор УК");
                    if (list[iCount].nzp_payer <= 0) return new Returns(false, "Не задан поставщик");
                    if (list[iCount].nzp_fd <= 0) return new Returns(false, "Не задан договор с поставщиком");
                    if (list[iCount].dat_oper == "") return new Returns(false, "Не задана дата операционного дня");

                    if (!DateTime.TryParse(list[iCount].dat_oper, out dat_oper))
                        return new Returns(false, "Неверный формат даты операционного дня");
                    
                    if (list[iCount].pref.Trim() == "") list[iCount].pref = Points.Pref;
                }

                int nzpUser = list[0].nzp_user;
                /*using (var db = new DbWorkUser())
                {
                    nzpUser = db.GetLocalUser(list[0], out ret);
                    if (!ret.result) throw new Exception(ret.text);
                }*/
                
#if PG
                string nzpSndField = "";
                string zero = "";
#else
                string nzpSndField = "nzp_snd,";
                string zero = "0,";
#endif

                foreach (MoneySended item in list)
                {
                    // получить операционый день
                    dat_oper = DateTime.Parse(item.dat_oper);
                    // названия таблиц
                    string sended = Points.Pref + "_fin_" + (dat_oper.Year % 100).ToString("00") + tableDelimiter + "fn_sended";
                    string sended_dom = Points.Pref + "_fin_" + (dat_oper.Year % 100).ToString("00") + tableDelimiter + "fn_sended_dom";
                    string distrib = Points.Pref + "_fin_" + (dat_oper.Year % 100).ToString("00") + tableDelimiter + "fn_distrib_dom_" + (dat_oper.Month % 100).ToString("00");
                    string tmp_distrib_dom = "tmp_distrib_dom_sum_send";
                        
                    if (item.sum_send > 0)
                    {
                        #region распределить суммы по домам
                        //-----------------------------------------------------------------------------------------------------------
                        sql = "drop table " + tmp_distrib_dom;
                        ExecSQL(sql, false);

                        // создать временную таблицу
                        sql = " create temp table " + tmp_distrib_dom + " (" +
                            " ordering    serial, " +
                            " nzp_dom     integer, " +
                            " nzp_serv    integer, " +
                            " sum_out     " + DBManager.sDecimalType + " (14,2), " +
                            " sum_distr   " + DBManager.sDecimalType + " (14,2)  " + ") " + DBManager.sUnlogTempTable;
                        ret = ExecSQL(sql);
                        if (!ret.result) throw new Exception(ret.text);

                        // сохранить в временную таблицу коды домов, услуг, суммы, по которым будет выполняться распределение
                        sql = " insert into " + tmp_distrib_dom + " (nzp_dom, nzp_serv, sum_out, sum_distr) " +
                            " select nzp_dom, nzp_serv, sum(" + DBManager.sNvlWord + "(sum_out, 0)), " + item.sum_send +
                            " from " + distrib +
                            " where nzp_supp = " + item.nzp_supp +
                            "   and nzp_payer = " + item.nzp_payer;

                        if (item.nzp_serv > 0) sql += " and nzp_serv = " + item.nzp_serv;

                        sql += " and dat_oper = " + Utils.EStrNull(Convert.ToDateTime(item.dat_oper).ToShortDateString()) +
                            " and sum_out > 0 " +
                            " group by 1,2";

                        ret = ExecSQL(sql);
                        if (!ret.result) throw new Exception(ret.text);

                        // выполнить распределение
                        ret = DistrSumLoadSended(tmp_distrib_dom, "ordering", "sum_out", "sum_distr", item.sum_send, "");
                        if (!ret.result) throw new Exception(ret.text);
                        //-----------------------------------------------------------------------------------------------------------
                        #endregion
                    }
                    
                    try
                    {
                        BeginTransaction();
                        
                        // очистка таблиц
                        sql = "delete from " + sended + " where nzp_supp = " + item.nzp_supp + " and nzp_payer = " + item.nzp_payer + " and dat_oper = " + Utils.EStrNull(Convert.ToDateTime(item.dat_oper).ToShortDateString());
                        ret = ExecSQL(sql);
                        if (!ret.result) throw new Exception(ret.text);

                        sql = "delete from " + sended_dom + " where nzp_supp = " + item.nzp_supp + " and nzp_payer = " + item.nzp_payer + " and dat_oper = " + Utils.EStrNull(Convert.ToDateTime(item.dat_oper).ToShortDateString());
                        ret = ExecSQL(sql);
                        if (!ret.result) throw new Exception(ret.text);

                        if (item.sum_send > 0)
                        {
                            #region sended
                            //---------------------------------------------------------------------------------------------
                            Int32 nzp_snd = 0;
                            sql = "insert into " + sended + " (" + nzpSndField + "nzp_area, dat_oper, nzp_supp, nzp_serv, nzp_payer, nzp_fd, sum_send, nzp_user, dat_when, dat_pp, num_pp) values (" +
                                zero +
                                "0, " + // nzp_area 
                                Utils.EStrNull(Convert.ToDateTime(item.dat_oper).ToShortDateString()) + ", " +
                                item.nzp_supp + "," + item.nzp_serv + "," + item.nzp_payer + "," +
                                item.nzp_fd + "," + item.sum_send + "," + nzpUser + "," + DBManager.sCurDateTime + "," +
                                Utils.EStrNull(item.dat_pp) + "," + item.num_pp + ")";

                            ret = ExecSQL(sql);
                            if (!ret.result) throw new Exception(ret.text);

                            // получить ключ
                            sql = "select nzp_snd from " + sended +
                                " where dat_oper = " + Utils.EStrNull(Convert.ToDateTime(item.dat_oper).ToShortDateString()) +
                                "   and nzp_supp = " + item.nzp_supp +
                                (item.nzp_serv > 0 ? " and nzp_serv = " + item.nzp_serv : "") +
                                "   and nzp_payer = " + item.nzp_payer +
                                "   and nzp_fd = " + item.nzp_fd;

                            IntfResultTableType intfResTable = OpenSQL(sql);
                            if (intfResTable.resultCode < 0) throw new Exception(intfResTable.resultMessage);

                            if ((intfResTable.resultData != null) && (intfResTable.resultData.Rows.Count > 0))
                                if (intfResTable.resultData.Rows[0]["nzp_snd"].ToString().Trim() != "")
                                {
                                    nzp_snd = Convert.ToInt32(intfResTable.resultData.Rows[0]["nzp_snd"]);
                                }

                            if (nzp_snd <= 0) throw new Exception("Не удалось получить ключ");
                            //---------------------------------------------------------------------------------------------
                            #endregion

                            #region sended_dom
                            //---------------------------------------------------------------------------------------------
                            sql = "insert into " + sended_dom + " (" + nzpSndField + "nzp_area, nzp_send, " +
                                "   dat_oper, nzp_supp, nzp_payer, nzp_fd, sum_send, nzp_user, dat_when, nzp_serv, nzp_dom)" +
                                " Select " + zero +
                                    "0, " + // nzp_area
                                    nzp_snd + "," + Utils.EStrNull(Convert.ToDateTime(item.dat_oper).ToShortDateString()) + "," +
                                    item.nzp_supp + ", " + item.nzp_payer + "," +
                                    item.nzp_fd + "," +
                                    (item.nzp_serv > 0 ? "sum_distr" : "sum(sum_distr)") + "," +
                                    nzpUser + "," + DBManager.sCurDateTime + ", nzp_serv, nzp_dom " +
                                " from " + tmp_distrib_dom;

                            if (item.nzp_serv <= 0) sql += " group by nzp_serv, nzp_dom";

                            ret = ExecSQL(sql);
                            if (!ret.result) throw new Exception(ret.text);
                            //---------------------------------------------------------------------------------------------
                            #endregion
                        }
                        
                        Commit();
                    }
                    catch (Exception ex)
                    {
                        Rollback();
                        return new Returns(false, ex.Message, -1);
                    }

                    sql = "drop table " + tmp_distrib_dom;
                    ExecSQL(sql, false);
                }
            }
            catch (Exception ex)
            { 
                return new Returns(false, ex.Message, -1);
            }

            
            #region distrib
            //---------------------------------------------------------------------------------------------
            List<string> datOperList = list.Select(o => o.dat_oper).Distinct().ToList();

            try
            {
                DateTime cur_oper_date = DateTime.Today;
                ret = new Returns(true);
                
                for (int i = 0; i < datOperList.Count; i++)
                {
                    cur_oper_date = Convert.ToDateTime(datOperList[i]);

                    using (DbCalcPack dbCalcPack = new DbCalcPack())
                    {
                        ret = dbCalcPack.UpdateSend(this.ServerConnection, cur_oper_date);
                        if (!ret.result) throw new Exception(ret.text);
                    }
                }
                
            }
            catch (Exception ex)
            {
                Rollback();
                return new Returns(false, ex.Message, -1);
            }
            //---------------------------------------------------------------------------------------------
            #endregion

            #region постановка задач на расчет исходящего сальдо
            //----------------------------------------------------------------------------------------------------------------------
            DateTime _dat_oper = Points.DateOper;
            DateTime min_dat_oper = Points.DateOper;

            foreach (MoneySended item in list)
            {
                // получить операционый день
                _dat_oper = DateTime.Parse(item.dat_oper);
                if (min_dat_oper >= _dat_oper) min_dat_oper = _dat_oper;
            }
            
            List<long> payerList = list.Select(o => o.nzp_payer).Distinct().ToList();

            CalcFonTask newCalcFon = new CalcFonTask(Points.GetCalcNum(0));
            newCalcFon.TaskType = CalcFonTask.Types.taskRecalcDistribSumOutSaldo;
            newCalcFon.Status = FonTask.Statuses.New; //на выполнение    

            newCalcFon.txt = "Перерасчет исходящего сальдо за опер.день " + min_dat_oper.ToShortDateString();
            newCalcFon.year_ = DateTime.Today.Year;
            newCalcFon.month_ = 0;
            newCalcFon.parameters = min_dat_oper.ToShortDateString();
            

            for (int i = 0; i < payerList.Count; i++)
            { 
                newCalcFon.nzp = Convert.ToInt32(payerList[i]);

                using (var dbCalc = new DbCalcQueueClient())
                {
                    ret = dbCalc.AddTask(newCalcFon);
                    dbCalc.Close();
                    if (!ret.result) return ret;    
                }
            }
            //----------------------------------------------------------------------------------------------------------------------
            #endregion

            return ret;
        }

        public ReturnsObjectType<DataTable> LoadSendedMoneyNew(MoneySended finder)
        {
            StringBuilder sqlText;
            Returns ret = new Returns();

            DataTable table = null;

            try
            {
                if (finder.nzp_user <= 0) throw new Exception("Не задан пользователь");
                if (finder.nzp_supp <= 0) throw new Exception("Не определен договор");
                DateTime dat_oper = DateTime.MinValue;
                if (!DateTime.TryParse(finder.dat_oper, out dat_oper)) throw new Exception("Не задана дата операции");

                if (finder.pref.Trim() == "") finder.pref = Points.Pref;

                // Ограничения пользователя
                string sqlRoleFilter = "";
                string strKeys = "";
                if (finder.RolesVal != null)
                {
                    foreach (_RolesVal role in finder.RolesVal)
                    {
                        if (role.tip == Constants.role_sql)
                        {
                            strKeys = role.val.Trim(new char[] { ' ', ',' });
                            if (role.kod == Constants.role_sql_payer)
                                sqlRoleFilter += " and d.nzp_payer in (" + strKeys + ") ";
                        }
                    }
                }

                string sended = finder.pref + "_fin_" + (Convert.ToDateTime(finder.dat_oper).Year % 100).ToString("00") + tableDelimiter + "fn_sended";
                string distrib = Points.Pref + "_fin_" + (Convert.ToDateTime(finder.dat_oper).Year % 100).ToString("00") + tableDelimiter + "fn_distrib_dom_" + (Convert.ToDateTime(finder.dat_oper).Month % 100).ToString("00");
                string dogovor = finder.pref + "_data" + tableDelimiter + "fn_dogovor";
                string bank = finder.pref + "_data" + tableDelimiter + "fn_bank";
                string supp = Points.Pref + "_kernel" + tableDelimiter + "supplier";
                string services = finder.pref + "_kernel" + tableDelimiter + "services";
                string payer = finder.pref + "_kernel" + tableDelimiter + "s_payer";
                string payer_types = finder.pref + "_kernel" + tableDelimiter + "payer_types";
                string dogovor_bank = Points.Pref + "_data" + DBManager.tableDelimiter + "fn_dogovor_bank";

                // Создать временную таблицу
                string tshutable = "tshu_" + finder.nzp_user.ToString().Trim() + "_fn_sended";
                string tshutable_p = tshutable.Trim() + "_p";

                sqlText = new StringBuilder(" drop table " + tshutable);
                ExecSQL(sqlText.ToString(), false);

                sqlText = new StringBuilder(" drop table " + tshutable_p);
                ExecSQL(sqlText.ToString(), false);

                sqlText = new StringBuilder(" Create temp table " + tshutable_p);
                sqlText.Append(" ( nzp_supp     integer, ");
                sqlText.Append(" nzp_payer    integer, ");
                sqlText.Append(" cnt          integer, ");
                sqlText.Append(" osn_priznak  integer) ");
                sqlText.Append(DBManager.sUnlogTempTable);
                ret = ExecSQL(sqlText.ToString());
                if (!ret.result) throw new Exception("Ошибка создания временной таблицы\n" + ret.text);
              
                sqlText = new StringBuilder();
                sqlText.AppendFormat(" insert into {0} (nzp_supp, nzp_payer, cnt, osn_priznak) ", tshutable_p);
                sqlText.Append(" select s1.nzp_supp, b.nzp_payer, count(*), max(db.priznak_perechisl) ");
                sqlText.AppendFormat(" from {0}_data{1}fn_dogovor_bank_lnk db, {0}_data{1}fn_bank b, {0}_kernel{1}supplier s1 ", Points.Pref, tableDelimiter);
                sqlText.AppendFormat(" where db.nzp_fd = (select nzp_fd from {0}_data{1}fn_dogovor_bank_lnk db, {0}_kernel{1}supplier s where ", Points.Pref, tableDelimiter);
                sqlText.AppendFormat(" s.fn_dogovor_bank_lnk_id = db.id and s.nzp_supp = {0}) ", finder.nzp_supp);
                sqlText.AppendFormat(" and b.nzp_fb = db.nzp_fb and s1.nzp_supp = {0} ", finder.nzp_supp);
                sqlText.Append(" group by 1, 2 ");
                ret = ExecSQL(sqlText.ToString());
                if (!ret.result) throw new Exception(ret.text);

                sqlText = new StringBuilder(" select MAX(cnt) mcnt from " + tshutable_p);
                IntfResultTableType intfResTable = OpenSQL(sqlText.ToString());
                if (intfResTable.resultCode < 0) throw new Exception("Ошибка получения количества договоров" + "\n" + intfResTable.resultMessage);

                Int32 cntFd = 0;

                if ((intfResTable.resultData != null) && (intfResTable.resultData.Rows.Count > 0))
                    if (intfResTable.resultData.Rows[0]["mcnt"].ToString().Trim() != "")
                    {
                        cntFd = Convert.ToInt32(intfResTable.resultData.Rows[0]["mcnt"]);
                    }

                sqlText = new StringBuilder(" create temp table " + tshutable + "( ");
                sqlText.Append("   ordering SERIAL,           ");
                sqlText.Append("   dat_oper DATE,             "); 
                sqlText.Append("   nzp_supp INTEGER,          "); 
                sqlText.Append("   name_supp VARCHAR(100),    "); 
                sqlText.Append("   nzp_serv INTEGER,          "); 
                sqlText.Append("   service VARCHAR(100),      "); 
                sqlText.Append("   nzp_payer INTEGER,         "); 
                sqlText.Append("   payer VARCHAR(200),        ");
                sqlText.AppendFormat("   sum_charge {0}(14,2),    ", DBManager.sDecimalType);
                sqlText.AppendFormat("   sum_must {0}(14,2),    ", DBManager.sDecimalType);
                sqlText.AppendFormat("   sum_send {0}(14,2),    ", DBManager.sDecimalType);
                sqlText.Append("   cnt_fd INTEGER DEFAULT 0,  ");
                sqlText.AppendFormat("   sum_send_fd {0}(14,2), ", DBManager.sDecimalType); 
                sqlText.Append("   osn_priznak INTEGER, ");
                sqlText.AppendFormat("   sum_bank {0}(14,2) default 0.00, ", DBManager.sDecimalType);
                sqlText.AppendFormat("   sum_send_p {0}(14,2) default 0.00, ", DBManager.sDecimalType);
                sqlText.Append("   priznak INTEGER default 0"); 

                for (int iCount = 1; iCount <= cntFd; iCount++)
                {
                    sqlText.Append(",");
                    sqlText.AppendFormat(" fn_dogovor_bank_lnk_id_{0} INTEGER, ", iCount);
                    sqlText.AppendFormat(" sum_send_{0} {1}(13,2) DEFAULT 0.00, ", iCount, DBManager.sDecimalType);
                    sqlText.AppendFormat(" s_osnov_{0} VARCHAR(200), ", iCount); 
                    sqlText.AppendFormat(" target_{0} VARCHAR(200), ", iCount);
                    sqlText.AppendFormat(" pp_{0} VARCHAR(40), ", iCount);
                    sqlText.AppendFormat(" num_pp_{0} INTEGER, ", iCount);
                    sqlText.AppendFormat(" dat_pp_{0} DATE, ", iCount);
                    sqlText.AppendFormat(" max_sum_{0} {1}(14,2), ", iCount, DBManager.sDecimalType);
                    sqlText.AppendFormat(" min_sum_{0} {1}(14,2), ", iCount, DBManager.sDecimalType);
                    sqlText.AppendFormat(" sum_serv_{0} {1}(14,2) default 0.00, ", iCount, DBManager.sDecimalType);
                    sqlText.AppendFormat(" bank_cnt_{0} integer ", iCount);
                }
                sqlText.AppendFormat(") {0}", DBManager.sUnlogTempTable);

                ret = ExecSQL(sqlText.ToString());
                if (!ret.result) throw new Exception("Ошибка создания временой таблицы" + "\n" + ret.text);

#if PG
                string ordering_field = "";
                string ordering_value = "";
                string group_by = "1,2,3,4,5,6,7,11,12,13";
#else
                string ordering_field = "ordering,";
                string ordering_value = "0,";
                string group_by = "1,2,3,4,5,6,7,8,12,13,14 ";
#endif

                sqlText = new StringBuilder(" insert into ");
                sqlText.AppendFormat("{0} (", tshutable);
                sqlText.Append(ordering_field);
                sqlText.Append("dat_oper, nzp_supp, name_supp, nzp_serv, service, nzp_payer, payer, sum_charge, sum_must, sum_send, cnt_fd, sum_send_fd, osn_priznak)  ");
                sqlText.AppendFormat(" select {0} d.dat_oper, d.nzp_supp, a.name_supp,", ordering_value);
                // услуга
                sqlText.AppendFormat(" case when {0}(pp.osn_priznak, 0) > 1 then d.nzp_serv else 0 end, ", sNvlWord);
                sqlText.AppendFormat(" case when {0}(pp.osn_priznak, 0) > 1 then s.service else '' end, ", sNvlWord);
                // поставщик
                sqlText.Append(" d.nzp_payer, p.payer, ");
                sqlText.AppendFormat(" sum({0}(d.sum_charge, 0) + {0}(d.sum_reval, 0)), ", sNvlWord);
                sqlText.AppendFormat(" sum({0}(d.sum_in, 0) + {0}(d.sum_charge, 0) + {0}(d.sum_reval, 0)), ", sNvlWord);
                sqlText.Append(" sum(d.sum_send), ");
                
                sqlText.Append(" pp.cnt, 0, ");
                sqlText.AppendFormat("{0}(pp.osn_priznak, 0) ", sNvlWord);
                sqlText.AppendFormat(" from {0} a, {1} s, {2} p, {3} d ", supp, services, payer, distrib);
                sqlText.AppendFormat(" left outer join {0}_p pp on pp.nzp_supp = d.nzp_supp and pp.nzp_payer = d.nzp_payer ", tshutable.Trim());
                sqlText.Append(" where 1=1 and d.nzp_supp = a.nzp_supp and d.nzp_serv = s.nzp_serv and d.nzp_payer = p.nzp_payer and d.nzp_payer not in ");
                sqlText.AppendFormat("(select pt1.nzp_payer from {0} pt1, {1} p1 where pt1.nzp_payer_type=5 and pt1.nzp_payer = p1.nzp_payer) ", payer_types, payer);
                sqlText.AppendFormat(" {0} and d.dat_oper = {1} ", sqlRoleFilter, Utils.EStrNull(Convert.ToDateTime(finder.dat_oper).ToShortDateString()));
                if (finder.nzp_supp > 0) sqlText.AppendFormat(" and d.nzp_supp = {0} ", finder.nzp_supp);
                sqlText.AppendFormat(" group by {0}", group_by);

                ret = ExecSQL(sqlText.ToString());
                if (!ret.result) throw new Exception("Ошибка выбора данных" + "\n" + ret.text);

                #region создать индексы
                sqlText = new StringBuilder(" create index tix_" + tshutable + "_01 on " + tshutable + " (dat_oper) ");
                ret = ExecSQL(sqlText.ToString());
                if (!ret.result) throw new Exception(ret.text);

                sqlText = new StringBuilder(" create index tix_" + tshutable + "_02 on " + tshutable + " (nzp_supp) ");
                ret = ExecSQL(sqlText.ToString());
                if (!ret.result) throw new Exception(ret.text);

                sqlText = new StringBuilder(" create index tix_" + tshutable + "_03 on " + tshutable + " (nzp_serv) ");
                ret = ExecSQL(sqlText.ToString());
                if (!ret.result) throw new Exception(ret.text);

                sqlText = new StringBuilder(" create index tix_" + tshutable + "_04 on " + tshutable + " (nzp_payer) ");
                ret = ExecSQL(sqlText.ToString());
                if (!ret.result) throw new Exception(ret.text);

                sqlText = new StringBuilder(" create index tix_" + tshutable + "_05 on " + tshutable + " (ordering) ");
                ret = ExecSQL(sqlText.ToString());
                if (!ret.result) throw new Exception(ret.text);

                sqlText = new StringBuilder(DBManager.sUpdStat + " " + tshutable);
                ret = ExecSQL(sqlText.ToString());
                if (!ret.result) throw new Exception(ret.text);
                #endregion

                sqlText = new StringBuilder(" select * from " + tshutable);
                intfResTable = OpenSQL(sqlText.ToString());
                if (intfResTable.resultCode < 0) throw new Exception("Ошибка получения данных" + "\n" + intfResTable.resultMessage);

                table = intfResTable.GetData();

                Int32 osn_priznak = 0;

                foreach (DataRow dr in table.Rows)
                {
                    osn_priznak = Convert.ToInt32(dr["osn_priznak"]);
                    sqlText = new StringBuilder();
                    //sqlText.AppendFormat(" distinct sum({0}(d.sum_send,0)) sum_send, v.nzp_fd,   ", sNvlWord);
                    //      "' № '||trim(" + sNvlWord + "(v.num_dog,' '))||" +
                    //      "(case when v.dat_dog is null then '' else ' от '|| v.dat_dog end) s_osnov, " +
                    //      "v.target, d.num_pp, d.dat_pp, fb.max_sum, fb.min_sum,    " +
                    //      "(select count(*) from "+dogovor_bank+" fdb where v.nzp_fd = fdb.nzp_fd) as bank_cnt " +
                    //      "from " + bank + " fb," + supp + " s, " + dogovor + " v    left outer join " + sended + " d on d.nzp_fd = v.nzp_fd and d.dat_oper = " + Utils.EStrNull(Convert.ToDateTime(finder.dat_oper).ToShortDateString()) +
                    //      "where fb.nzp_fb = s.nzp_fb and s.nzp_supp=" + Convert.ToInt32(dr["nzp_supp"]) +
                    //      (osn_priznak > 1 ? " and d.nzp_serv = " + Convert.ToInt32(dr["nzp_serv"]) : "") + " " + sqlRoleFilter +
                    //      "and fb.nzp_fd=v.nzp_fd  group by 2,3,4,5,6,7,8  order by 1 ";

                    sqlText.Append(" select s.fn_dogovor_bank_lnk_id, d.target, db2.max_sum, db2.min_sum, num_pp, dat_pp, ");
                    sqlText.AppendFormat(
                        " 'р/с ' || b.rcount || ' в ' || (select payer from {0}_kernel{1}s_payer where nzp_payer = b.nzp_payer_bank) s_osnov,",
                        Points.Pref, tableDelimiter);
                    sqlText.Append(" sum(coalesce((sum_send),0)) sum_send, 0 bank_cnt ");
                    sqlText.AppendFormat(" from {0}_data{1}fn_dogovor_bank_lnk db ", Points.Pref, tableDelimiter);
                    sqlText.AppendFormat(" inner join {0}_data{1}fn_dogovor_bank_lnk db2 on db2.nzp_fd = db.nzp_fd ", Points.Pref, tableDelimiter);
                    sqlText.AppendFormat(" left outer join {0}_data{1}fn_dogovor d on d.nzp_fd = db.nzp_fd ", Points.Pref, tableDelimiter);
                    sqlText.AppendFormat(" inner join {0}_data{1}fn_bank b on  b.nzp_fb = db2.nzp_fb and b.nzp_payer =  {2}", Points.Pref, tableDelimiter, Convert.ToInt32(dr["nzp_payer"]));
                    sqlText.AppendFormat(" left outer join {0} sd on sd.fn_dogovor_bank_lnk_id = db.id ", sended);
                    sqlText.AppendFormat(" and  sd.dat_oper = {0} ",  Utils.EStrNull(Convert.ToDateTime(finder.dat_oper).ToShortDateString()));
                    sqlText.AppendFormat(" and sd.nzp_supp = {0} and b.nzp_payer = sd.nzp_payer ", Convert.ToInt32(dr["nzp_supp"]));
                    sqlText.Append((osn_priznak > 1 ? " and sd.nzp_serv = " + Convert.ToInt32(dr["nzp_serv"]) : "") + " " + sqlRoleFilter);
                    sqlText.AppendFormat(" , {0}_kernel{1}supplier s ", Points.Pref, tableDelimiter);
                    sqlText.AppendFormat(" where s.nzp_supp = {0} and s.fn_dogovor_bank_lnk_id = db.id ", Convert.ToInt32(dr["nzp_supp"]));
                    sqlText.Append(" group by 1, 2,3, 4, 5, 6, 7");

                    intfResTable = OpenSQL(sqlText.ToString());
                    if (intfResTable.resultCode < 0) throw new Exception("Ошибка получения данных" + "\n" + intfResTable.resultMessage);

                    DataTable tableSend = intfResTable.GetData();
                    if (tableSend == null) throw new Exception("Ошибка получения данных");

                    Int32 ordering = Convert.ToInt32(dr["ordering"]);
                    int iNumRow = 0;
                    foreach (DataRow drSend in tableSend.Rows)
                    {
                        iNumRow++;
                        if (iNumRow > cntFd) break;

                        string sqlSend = "";

                        sqlSend = " update  " + tshutable + " set (fn_dogovor_bank_lnk_id_" + iNumRow.ToString() + "," +
                            " sum_send_" + iNumRow.ToString() + "," +
                            " s_osnov_" + iNumRow.ToString() + ", " +
                            " target_" + iNumRow.ToString() + ", " +
                            " num_pp_" + iNumRow.ToString() + "," +
                            " dat_pp_" + iNumRow.ToString() + ", " +
                            " max_sum_" + iNumRow.ToString() + "," +
                            " min_sum_" + iNumRow.ToString() + ", " +
                            " bank_cnt_" + iNumRow.ToString() + ") =  " +
                            " (" + Utils.EStrNull(Convert.ToString(drSend["fn_dogovor_bank_lnk_id"])) + ", " +
                            Utils.EStrNull(Convert.ToString(drSend["sum_send"])) + ", " +
                            Utils.EStrNull(Convert.ToString(drSend["s_osnov"])) + ", " +
                            Utils.EStrNull(Convert.ToString(drSend["target"])) + ", " +
                            Utils.EStrNull(Convert.ToString(drSend["num_pp"])) + ", ";
                        // дата платежного поручения: если не заполнена, то выставлять текущий операционный день
                        if (drSend["dat_pp"] == DBNull.Value) sqlSend += Utils.EStrNull(Points.DateOper.ToShortDateString()) + ", ";
                        else sqlSend += Utils.EStrNull(Convert.ToDateTime(drSend["dat_pp"]).ToShortDateString()) + ", ";
                        // лимит
                        if (drSend["max_sum"] == DBNull.Value) sqlSend += " NULL, "; else sqlSend += Convert.ToDecimal(drSend["max_sum"]) + ", ";
                        // минимальная сумма к перечислению
                        if (drSend["min_sum"] == DBNull.Value) sqlSend += " NULL, "; else sqlSend += Convert.ToDecimal(drSend["min_sum"]) + ", ";
                        // количество банков, через которые должны перечисляться деньги по договору
                        if (drSend["bank_cnt"] == DBNull.Value) sqlSend += " NULL) "; else sqlSend += Convert.ToInt32(drSend["bank_cnt"]) + ") ";

                        sqlSend += " where ordering = " + Convert.ToInt32(dr["ordering"]).ToString();

                        ret = ExecSQL(sqlSend);
                        if (!ret.result) throw new Exception("Ошибка обновления данных" + "\n" + ret.text);
                    }
                }

                if (cntFd > 0)
                {
                    sqlText = new StringBuilder(" update " + tshutable + " set sum_send_fd = ");
                    for (int iCntSum = 1; iCntSum <= cntFd; iCntSum++)
                    {
                        sqlText.AppendFormat(" sum_send_{0}", iCntSum.ToString());
                        if (iCntSum < cntFd) sqlText.Append(" + ");
                    }
                    ret = ExecSQL(sqlText.ToString());
                    if (!ret.result) throw new Exception("Ошибка получения суммы по договорам" + "\n" + ret.text);
                }

                sqlText = new StringBuilder(" select count(*) as cnt from " + tshutable);
                intfResTable = OpenSQL(sqlText.ToString());

                table = intfResTable.GetData();
                Int32 cntRecord = Convert.ToInt32(table.Rows[0]["cnt"]);

                #region 1. Объединить суммы по услугам для договоров, у которых не установлен признак "Перечислять в разрезе услуг"
                //--------------------------------------------------------------------------------------------------------------------------------------------------------------
                if (cntFd > 0)
                {
                    sqlText = new StringBuilder("drop table tmp_target");
                    ExecSQL(sqlText.ToString(), false);

                    sqlText = new StringBuilder("create temp table tmp_target (ordering integer, target VARCHAR(200))" + DBManager.sUnlogTempTable);
                    ret = ExecSQL(sqlText.ToString());
                    if (!ret.result) throw new Exception("1.1." + ret.text);

                    sqlText = new StringBuilder("insert into tmp_target (ordering, target) ");
                    sqlText.AppendFormat(" select ordering, target_1 from {0}", tshutable);
                    sqlText.Append(" where osn_priznak = 1 and cnt_fd > 0 ");
                    ret = ExecSQL(sqlText.ToString());
                    if (!ret.result) throw new Exception("1.2." + ret.text);

                    sqlText = new StringBuilder("update ");
                    sqlText.AppendFormat(" {0} set service = (select a.target from tmp_target a where a.ordering = {0}", tshutable);
                    sqlText.Append(".ordering), nzp_serv = 0 ");
                    sqlText.Append(" where ordering in (select a.ordering from tmp_target a)");
                    ret = ExecSQL(sqlText.ToString());
                    if (!ret.result) throw new Exception("1.3." + ret.text);
                }
                //--------------------------------------------------------------------------------------------------------------------------------------------------------------
                #endregion

                #region 2. Распределить суммы (кнопка "Распределить суммы")
                //--------------------------------------------------------------------------------------------------------------------------------------------------------------
                if (finder.sum_send > 0 && cntFd > 0)
                {
                    ret = DistrSumLoadSended(tshutable, "ordering", "sum_must", "sum_send_1", finder.sum_send, " and cnt_fd > 0 ");
                    if (!ret.result) throw new Exception(ret.text);
                }
                //--------------------------------------------------------------------------------------------------------------------------------------------------------------
                #endregion

                #region 3. Зачислить суммы
                //--------------------------------------------------------------------------------------------------------------------------------------------------------------

                #region Зачислить суммы за опер. день или зачислить все
                //--------------------------------------------------------------------------------------------------------------------------------------------------------------
                if (cntFd > 0 && finder.copy_id > 0)
                {
                    string sql_sum = "";

                    // зачесть за опер. день
                    if (finder.copy_id == 1) sql_sum = "sum(" + sNvlWord + "(d.sum_charge, 0) + " + sNvlWord + "(d.sum_reval, 0))";
                    // зачесть все
                    else sql_sum = "sum(" + sNvlWord + "(d.sum_in, 0) + " + sNvlWord + "(d.sum_charge, 0) + " + sNvlWord + "(d.sum_reval, 0))";

                    //for (int i = 1; i <= cntFd; i++)
                    //{
                    //    // определить суммы по договорам и банкам, которые указаны в договорах
                    //    sql = "drop table tmp_fd_sum_send";
                    //    ExecSQL(sql, false);

                    //    sql = " create temp table tmp_fd_sum_send (ordering integer, fd_sum_send " + DBManager.sDecimalType + " (14,2) ) " + DBManager.sUnlogTempTable;
                    //    ret = ExecSQL(sql);
                    //    if (!ret.result) throw new Exception("3.1." + ret.text);

                    //    sql = " insert into tmp_fd_sum_send (ordering, fd_sum_send) " +
                    //        " select t.ordering, " + sql_sum +
                    //        " from " + distrib + " d, " + tshutable + " t, " + dogovor_bank + " fdb, " + s_bank + " b " +
                    //        " where t.nzp_fd_" + i + " = fdb.nzp_fd " +
                    //        "   and fdb.nzp_bank = b.nzp_bank " +
                    //        "   and b.nzp_payer  = d.nzp_bank " +
                    //        "   and d.dat_oper = " + Utils.EStrNull(Convert.ToDateTime(finder.dat_oper).ToShortDateString()) +
                    //        "   and d.nzp_supp = t.nzp_supp " +
                    //        "   and d.nzp_payer = t.nzp_payer " +
                    //        "   and d.nzp_serv = (case when t.nzp_serv > 0 then t.nzp_serv else d.nzp_serv end) " +
                    //        "   and t.bank_cnt_" + i + " > 0" +
                    //        " group by 1";
                    //    ret = ExecSQL(sql);
                    //    if (!ret.result) throw new Exception("3.2." + ret.text);

                    //    sql = " update " + tshutable + " set sum_send_" + i + " = (select a.fd_sum_send from tmp_fd_sum_send a where " + tshutable + ".ordering = a.ordering) " +
                    //        " where ordering in (select a.ordering from tmp_fd_sum_send a) ";
                    //    ret = ExecSQL(sql);
                    //    if (!ret.result) throw new Exception("3.3." + ret.text);
                    //}

                    //for (int i = 1; i <= cntFd; i++)
                    //{
                    //    // cобрать все суммы по банкам
                    //    sql = "update " + tshutable + " set sum_bank = sum_bank + sum_send_" + i + " where bank_cnt_" + i + " > 0";
                    //    ret = ExecSQL(sql);
                    //    if (!ret.result) throw new Exception("3.4." + ret.text);
                    //}

                    //// определить сумму, которая не распределяется по банкам
                    sqlText = new StringBuilder("update " + tshutable + " set sum_send_p = " + (finder.copy_id == 1 ? "sum_charge" : "sum_must") + " - sum_bank");
                    ret = ExecSQL(sqlText.ToString());
                    if (!ret.result) throw new Exception("3.5." + ret.text);

                    //// положить эту нераспределенную сумму в первый договор, где не указаны банки 
                    //// и в который еще не сохранили нераспределенную сумму (priznak = 0)
                    for (int i = 1; i <= cntFd; i++)
                    {
                        sqlText = new StringBuilder("drop table tmp_ordering_sum_send");
                        ExecSQL(sqlText.ToString(), false);

                        sqlText = new StringBuilder(" create temp table tmp_ordering_sum_send (ordering integer) " + DBManager.sUnlogTempTable);
                        ret = ExecSQL(sqlText.ToString());
                        if (!ret.result) throw new Exception("3.6." + ret.text);

                        // определить нужные договоры 
                        sqlText = new StringBuilder("insert into tmp_ordering_sum_send (ordering) ");
                        sqlText.Append("select ordering from " + tshutable + " where bank_cnt_" + i + " = 0 and priznak = 0");
                        ret = ExecSQL(sqlText.ToString());
                        if (!ret.result) throw new Exception("3.7." + ret.text);

                        // сохранить сумму
                        sqlText =
                            new StringBuilder("update " + tshutable + " set sum_send_" + i +
                                              " = sum_send_p, priznak = 1 ");
                            sqlText.Append(" where ordering in (select ordering from tmp_ordering_sum_send) ");
                        ret = ExecSQL(sqlText.ToString());
                        if (!ret.result) throw new Exception("3.8." + ret.text);
                    }
                }
                //--------------------------------------------------------------------------------------------------------------------------------------------------------------
                #endregion

                //#region ограничить суммы верхним и нижним потолком
                ////--------------------------------------------------------------------------------------------------------------------------------------------------------------
                //if (cntFd > 0 && (finder.sum_send > 0 || finder.copy_id > 0))
                //{
                //    for (int i = 1; i <= cntFd; i++)
                //    {
                //        // ограничить суммы по договорам, у которых нет признака "Перечислять в разрезе услуг"
                //        sql = "update " + tshutable + " set sum_send_" + i + " = (case when sum_send_" + i + " < min_sum_" + i + " and min_sum_" + i + " > 0 then 0.00 " +
                //              " else case when sum_send_" + i + " > max_sum_" + i + " and max_sum_" + i + " > 0 then max_sum_" + i + " " +
                //              " else sum_send_" + i + " end end) " +
                //            " where cnt_fd > 0 and osn_priznak = 1";
                //        ret = ExecSQL(sql);
                //        if (!ret.result) throw new Exception("3.9." + ret.text);

                //        // по договорам, у которых перечисление в разрезе услуг
                //        sql = "drop table tmp_sum_serv";
                //        ExecSQL(sql, false);

                //        // получить коды УК и поставщиков и итоговые суммы по услугам
                //        sql = "create temp table tmp_sum_serv (" +
                //            " supp_id integer, payer_id integer, serv_sum " + sDecimalType + "(14,2))" + (DBManager.tableDelimiter == ":" ? " with no log" : "");
                //        ret = ExecSQL(sql);
                //        if (!ret.result) throw new Exception("3.10." + ret.text);

                //        sql = " insert into tmp_sum_serv (supp_id, payer_id, serv_sum) " +
                //            " select nzp_supp, nzp_payer, sum(" + DBManager.sNvlWord + "(sum_send_" + i + ", 0.00)) from " + tshutable +
                //            " where cnt_fd > 0 and osn_priznak = 2 " +
                //            " group by 1,2";
                //        ret = ExecSQL(sql);
                //        if (!ret.result) throw new Exception("3.11." + ret.text);

                //        // cохранить итоговые суммы по услугам
                //        sql = " update " + tshutable + " set " +
                //            " sum_serv_" + i + "  = (select t.serv_sum from tmp_sum_serv t where t.payer_id = " + tshutable + ".nzp_payer and t.supp_id = " + tshutable + ".nzp_supp) " +
                //            " where nzp_supp in (select supp_id from tmp_sum_serv) " +
                //            "   and nzp_payer in (select payer_id from tmp_sum_serv) ";
                //        ret = ExecSQL(sql);
                //        if (!ret.result) throw new Exception("3.12." + ret.text);

                //        // ограничить суммы нижним потолком
                //        sql = " update " + tshutable + " set sum_send_" + i + " = 0 " +
                //            " where sum_serv_" + i + "  < min_sum_" + i + " and osn_priznak = 2";
                //        ret = ExecSQL(sql);
                //        if (!ret.result) throw new Exception("3.13." + ret.text);

                //        // получить коды поставщиков и УК и верхние потолки
                //        sql = "select distinct nzp_payer, nzp_supp, max_sum_" + i + " from " + tshutable +
                //            " where sum_serv_" + i + " > max_sum_" + i + " and osn_priznak = 2";
                //        intfResTable = OpenSQL(sql);
                //        if (intfResTable.resultCode < 0) throw new Exception("3.14." + intfResTable.resultMessage);

                //        table2 = intfResTable.GetData();

                //        for (int j = 0; j < table2.Rows.Count; j++)
                //        {
                //            int nzp_payer = Convert.ToInt32(table2.Rows[j]["nzp_payer"]);
                //            int nzp_supp = Convert.ToInt32(table2.Rows[j]["nzp_supp"]);
                //            decimal max_sum = Convert.ToDecimal(table2.Rows[j]["max_sum_" + i]);

                //            // выполнить перераспределение по услугам, ограничившись верхним потолком
                //            ret = DistrSumLoadSended(tshutable, "ordering", "sum_send_" + i, "sum_send_" + i, max_sum, " and cnt_fd > 0 and nzp_payer = " + nzp_payer + " and nzp_supp = " + nzp_supp);
                //            if (!ret.result) throw new Exception("3.15." + ret.text);
                //        }
                //    }
                //}
                ////--------------------------------------------------------------------------------------------------------------------------------------------------------------
                //#endregion

                //--------------------------------------------------------------------------------------------------------------------------------------------------------------
                #endregion

                #region 4. Определить максимальный номер платежного поручения за день
                //-----------------------------------------------------------
                sqlText = new StringBuilder("select max(" + sNvlWord + "(d.num_pp, 0)) as num_pp from " + sended + " d  where d.dat_oper = " + Utils.EStrNull(Convert.ToDateTime(finder.dat_oper).ToShortDateString()));
                intfResTable = OpenSQL(sqlText.ToString());
                if (intfResTable.resultCode < 0) throw new Exception("Ошибка определения номера платежного поручения" + "\n" + intfResTable.resultMessage);

                table = intfResTable.GetData();

                Int32 num_pp = 0;

                if ((table != null) && (table.Rows.Count > 0))
                    if (table.Rows[0]["num_pp"].ToString().Trim() != "")
                    {
                        num_pp = Convert.ToInt32(table.Rows[0]["num_pp"]);
                    }
                //-----------------------------------------------------------
                #endregion

                sqlText = new StringBuilder(" select * from " + tshutable + " order by 2,4,8,6,ordering");
                intfResTable = OpenSQL(sqlText.ToString());
                if (intfResTable.resultCode < 0) throw new Exception("Ошибка получения данных" + "\n" + intfResTable.resultMessage);

                table = intfResTable.GetData();
                if (table == null) throw new Exception("Ошибка получения данных");

                sqlText = new StringBuilder(" drop table " + tshutable);
                ExecSQL(sqlText.ToString(), false);

                #region 5. Добавить в основание основного договора максимальную и минимальную ежедневную сумму к перечислению + проставить номера платежных поручений
                //--------------------------------------------------------------------------------------------------------------------------------------------------------------
                string s_sums = "";
                int cnt = 0;

                foreach (DataRow dr in table.Rows)
                {
                    if (Convert.ToString(dr["cnt_fd"]).Trim() != "")
                    {
                        cnt = Convert.ToInt32(dr["cnt_fd"]);
                        if (cnt > 0)
                        {
                            for (int j = 1; j <= cnt; j++)
                            {
                                if (Convert.ToString(dr["num_pp_" + j]).Trim() == "")
                                {
                                    num_pp++;
                                    dr["num_pp_" + j] = num_pp;
                                }

                                s_sums = "";
                                if (Convert.ToString(dr["s_osnov_" + j]).Trim() != "") s_sums = (string)dr["s_osnov_" + j];
                                //if (Convert.ToString(dr["max_sum_" + j]).Trim() != "")
                                //{
                                //    if (s_sums != "") s_sums += ",<br>";
                                //    s_sums = "макс. сумма: " + Convert.ToDecimal(dr["max_sum_" + j]).ToString("N2");
                                //}

                                //if (Convert.ToString(dr["min_sum_" + j]).Trim() != "")
                                //{
                                //    if (s_sums != "") s_sums += ",<br>";
                                //    s_sums = "мин. сумма: " + Convert.ToDecimal(dr["min_sum_" + j]).ToString("N2");
                                //}

                                dr["s_osnov_" + j] = s_sums;
                            }
                        }
                    }
                }

                table.AcceptChanges();
                //--------------------------------------------------------------------------------------------------------------------------------------------------------------
                #endregion

                return new ReturnsObjectType<DataTable>(table) { tag = cntRecord };

            }
            catch (Exception ex)
            {
                return new ReturnsObjectType<DataTable>(null, false, ex.Message, -1);
            }
        }

        public Returns SaveSendedMoneyNew(List<MoneySended> list)
        {
            string sql = "";
            Returns ret = new Returns();

            try
            {
                if ((list == null) || (list != null && list.Count == 0)) throw new Exception("Не заданы суммы перечислений");
                if (list[0].nzp_user <= 0) throw new Exception("Не задан пользователь");

                DateTime dat_oper = DateTime.MinValue;

                for (int iCount = 0; iCount < list.Count; iCount++)
                {
                    if (list[iCount].nzp_supp <= 0) return new Returns(false, "Не задан договор ЖКУ");
                    if (list[iCount].nzp_payer <= 0) return new Returns(false, "Не задан получатель");
                    if (list[iCount].fn_dogovor_bank_lnk_id <= 0) return new Returns(false, "Не задан расчетный счет получателя");
                    if (list[iCount].dat_oper == "") return new Returns(false, "Не задана дата операционного дня");

                    if (!DateTime.TryParse(list[iCount].dat_oper, out dat_oper))
                        return new Returns(false, "Неверный формат даты операционного дня");

                    if (list[iCount].pref.Trim() == "") list[iCount].pref = Points.Pref;
                }

                int nzpUser = list[0].nzp_user;
                /*using (var db = new DbWorkUser())
                {
                    nzpUser = db.GetLocalUser(list[0], out ret);
                    if (!ret.result) throw new Exception(ret.text);
                }*/

#if PG
                string nzpSndField = "";
                string zero = "";
#else
                string nzpSndField = "nzp_snd,";
                string zero = "0,";
#endif

                foreach (MoneySended item in list)
                {
                    // получить операционый день
                    dat_oper = DateTime.Parse(item.dat_oper);
                    // названия таблиц
                    string sended = Points.Pref + "_fin_" + (dat_oper.Year % 100).ToString("00") + tableDelimiter + "fn_sended";
                    string fn_dogovor_bank_lnk = Points.Pref + "_data"+ tableDelimiter + "fn_dogovor_bank_lnk";
                    string sended_dom = Points.Pref + "_fin_" + (dat_oper.Year % 100).ToString("00") + tableDelimiter + "fn_sended_dom";
                    string distrib = Points.Pref + "_fin_" + (dat_oper.Year % 100).ToString("00") + tableDelimiter + "fn_distrib_dom_" + (dat_oper.Month % 100).ToString("00");
                    string tmp_distrib_dom = "tmp_distrib_dom_sum_send";

                    if (item.sum_send > 0)
                    {
                        #region распределить суммы по домам
                        //-----------------------------------------------------------------------------------------------------------
                        sql = "drop table " + tmp_distrib_dom;
                        ExecSQL(sql, false);

                        // создать временную таблицу
                        sql = " create temp table " + tmp_distrib_dom + " (" +
                            " ordering    serial, " +
                            " nzp_dom     integer, " +
                            " nzp_serv    integer, " +
                            " sum_out     " + DBManager.sDecimalType + " (14,2), " +
                            " sum_distr   " + DBManager.sDecimalType + " (14,2)  " + ") " + DBManager.sUnlogTempTable;
                        ret = ExecSQL(sql);
                        if (!ret.result) throw new Exception(ret.text);

                        // сохранить в временную таблицу коды домов, услуг, суммы, по которым будет выполняться распределение
                        sql = " insert into " + tmp_distrib_dom + " (nzp_dom, nzp_serv, sum_out, sum_distr) " +
                            " select nzp_dom, nzp_serv, sum(" + DBManager.sNvlWord + "(sum_out, 0)), " + item.sum_send +
                            " from " + distrib +
                            " where nzp_supp = " + item.nzp_supp +
                            "   and nzp_payer = " + item.nzp_payer;

                        if (item.nzp_serv > 0) sql += " and nzp_serv = " + item.nzp_serv;

                        sql += " and dat_oper = " + Utils.EStrNull(Convert.ToDateTime(item.dat_oper).ToShortDateString()) +
                            " and sum_out > 0 " +
                            " group by 1,2";

                        ret = ExecSQL(sql);
                        if (!ret.result) throw new Exception(ret.text);

                        // выполнить распределение
                        ret = DistrSumLoadSended(tmp_distrib_dom, "ordering", "sum_out", "sum_distr", item.sum_send, "");
                        if (!ret.result) throw new Exception(ret.text);
                        //-----------------------------------------------------------------------------------------------------------
                        #endregion
                    }

                    try
                    {
                        BeginTransaction();

                        // очистка таблиц
                        sql = "delete from " + sended + " where nzp_supp = " + item.nzp_supp + " and nzp_payer = " + item.nzp_payer + " and dat_oper = " + Utils.EStrNull(Convert.ToDateTime(item.dat_oper).ToShortDateString());
                        ret = ExecSQL(sql);
                        if (!ret.result) throw new Exception(ret.text);

                        sql = "delete from " + sended_dom + " where nzp_supp = " + item.nzp_supp + " and nzp_payer = " + item.nzp_payer + " and dat_oper = " + Utils.EStrNull(Convert.ToDateTime(item.dat_oper).ToShortDateString());
                        ret = ExecSQL(sql);
                        if (!ret.result) throw new Exception(ret.text);

                        if (item.sum_send > 0)
                        {
                            #region sended
                            //---------------------------------------------------------------------------------------------
                            Int32 nzp_snd = 0;
                            sql = "insert into " + sended + " (" + nzpSndField +
                                  "nzp_area, dat_oper, nzp_supp, nzp_serv, nzp_payer, fn_dogovor_bank_lnk_id, sum_send, nzp_user, dat_when, dat_pp, num_pp, naznplat) values (" +
                                  zero +
                                  "0, " + // nzp_area 
                                  Utils.EStrNull(Convert.ToDateTime(item.dat_oper).ToShortDateString()) + ", " +
                                  item.nzp_supp + "," + item.nzp_serv + "," + item.nzp_payer + "," +
                                  item.fn_dogovor_bank_lnk_id + "," + item.sum_send + "," + nzpUser + "," +
                                  DBManager.sCurDateTime + "," +
                                  Utils.EStrNull(item.dat_pp) + "," + item.num_pp + "," +
                                  "(select naznplat from " + fn_dogovor_bank_lnk + " where id = " +
                                  item.fn_dogovor_bank_lnk_id + ")" +
                                  ")";

                            ret = ExecSQL(sql);
                            if (!ret.result) throw new Exception(ret.text);

                            // получить ключ
                            sql = "select nzp_snd from " + sended +
                                " where dat_oper = " + Utils.EStrNull(Convert.ToDateTime(item.dat_oper).ToShortDateString()) +
                                "   and nzp_supp = " + item.nzp_supp +
                                (item.nzp_serv > 0 ? " and nzp_serv = " + item.nzp_serv : "") +
                                "   and nzp_payer = " + item.nzp_payer +
                                "   and fn_dogovor_bank_lnk_id = " + item.fn_dogovor_bank_lnk_id;

                            IntfResultTableType intfResTable = OpenSQL(sql);
                            if (intfResTable.resultCode < 0) throw new Exception(intfResTable.resultMessage);

                            if ((intfResTable.resultData != null) && (intfResTable.resultData.Rows.Count > 0))
                                if (intfResTable.resultData.Rows[0]["nzp_snd"].ToString().Trim() != "")
                                {
                                    nzp_snd = Convert.ToInt32(intfResTable.resultData.Rows[0]["nzp_snd"]);
                                }

                            if (nzp_snd <= 0) throw new Exception("Не удалось получить ключ");
                            //---------------------------------------------------------------------------------------------
                            #endregion

                            #region sended_dom
                            //---------------------------------------------------------------------------------------------
                            sql = "insert into " + sended_dom + " (" + nzpSndField + "nzp_area, nzp_send, " +
                                "   dat_oper, nzp_supp, nzp_payer, fn_dogovor_bank_lnk_id, sum_send, nzp_user, dat_when, nzp_serv, nzp_dom)" +
                                " Select " + zero +
                                    "0, " + // nzp_area
                                    nzp_snd + "," + Utils.EStrNull(Convert.ToDateTime(item.dat_oper).ToShortDateString()) + "," +
                                    item.nzp_supp + ", " + item.nzp_payer + "," +
                                    item.fn_dogovor_bank_lnk_id + "," +
                                    (item.nzp_serv > 0 ? "sum_distr" : "sum(sum_distr)") + "," +
                                    nzpUser + "," + DBManager.sCurDateTime + ", nzp_serv, nzp_dom " +
                                " from " + tmp_distrib_dom;

                            if (item.nzp_serv <= 0) sql += " group by nzp_serv, nzp_dom";

                            ret = ExecSQL(sql);
                            if (!ret.result) throw new Exception(ret.text);
                            //---------------------------------------------------------------------------------------------
                            #endregion
                        }

                        Commit();
                    }
                    catch (Exception ex)
                    {
                        Rollback();
                        return new Returns(false, ex.Message, -1);
                    }

                    sql = "drop table " + tmp_distrib_dom;
                    ExecSQL(sql, false);
                }
            }
            catch (Exception ex)
            {
                return new Returns(false, ex.Message, -1);
            }


            #region distrib
            //---------------------------------------------------------------------------------------------
            List<string> datOperList = list.Select(o => o.dat_oper).Distinct().ToList();

            try
            {
                DateTime cur_oper_date = DateTime.Today;
                ret = new Returns(true);

                for (int i = 0; i < datOperList.Count; i++)
                {
                    cur_oper_date = Convert.ToDateTime(datOperList[i]);

                    using (DbCalcPack dbCalcPack = new DbCalcPack())
                    {
                        ret = dbCalcPack.UpdateSend(this.ServerConnection, cur_oper_date);
                        if (!ret.result) throw new Exception(ret.text);
                    }
                }

            }
            catch (Exception ex)
            {
                Rollback();
                return new Returns(false, ex.Message, -1);
            }
            //---------------------------------------------------------------------------------------------
            #endregion

            #region постановка задач на расчет исходящего сальдо
            //----------------------------------------------------------------------------------------------------------------------
            DateTime _dat_oper = Points.DateOper;
            DateTime min_dat_oper = Points.DateOper;

            foreach (MoneySended item in list)
            {
                // получить операционый день
                _dat_oper = DateTime.Parse(item.dat_oper);
                if (min_dat_oper >= _dat_oper) min_dat_oper = _dat_oper;
            }

            List<long> payerList = list.Select(o => o.nzp_payer).Distinct().ToList();

            CalcFonTask newCalcFon = new CalcFonTask(Points.GetCalcNum(0));
            newCalcFon.TaskType = CalcFonTask.Types.taskRecalcDistribSumOutSaldo;
            newCalcFon.Status = FonTask.Statuses.New; //на выполнение    

            newCalcFon.txt = "Перерасчет исходящего сальдо за опер.день " + min_dat_oper.ToShortDateString();
            newCalcFon.year_ = DateTime.Today.Year;
            newCalcFon.month_ = 0;
            newCalcFon.parameters = min_dat_oper.ToShortDateString();


            for (int i = 0; i < payerList.Count; i++)
            {
                newCalcFon.nzp = Convert.ToInt32(payerList[i]);

                using (var dbCalc = new DbCalcQueueClient())
                {
                    ret = dbCalc.AddTask(newCalcFon);
                    dbCalc.Close();
                    if (!ret.result) return ret;
                }
            }
            //----------------------------------------------------------------------------------------------------------------------
            #endregion

            return ret;
        }


    }
}