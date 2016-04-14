using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Global;
using STCLINE.KP50.DataBase;
using System.Collections;
using System.Data;
using FastReport;
using System.Globalization;
using SevenZip;

namespace STCLINE.KP50.DataBase
{
    public partial class Debitor : DataBaseHead
    {
        //достает данные по должнику
        public Deal LoadDealInfo(DealFinder finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            if (finder.nzp_user < 0)
            {
                ret = new Returns(false, "Не задан пользователь", -1);
                return null;
            }

            IDbConnection conn_db = null;
            #region Коннект к базе
            conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;
            #endregion
            StringBuilder sql = new StringBuilder();


            sql.Remove(0, sql.Length);
            sql.Append(" select distinct pref from " + Points.Pref + "_debt" + tableDelimiter + "deal ");
            sql.Append(" where nzp_deal=" + finder.nzp_deal + "");
            object obj = ExecScalar(conn_db, sql.ToString(), out ret, true);
            if (obj == null || obj == DBNull.Value)
            {
                ret.result = false;
                ret.text = "\n Не найдены реквизиты Дела";
                return null;
            }
            var pref = Convert.ToString(obj).Trim(); //получили прификс для банка где лежат параметры данного ЛС
            if (pref == "")
            {
                ret.result = false;
                ret.text = "\n Не найдены реквизиты Дела";
                return null;
            }


            sql.Remove(0, sql.Length);
            sql.Append(" select max(sd.name_deal_status) status,max(deal.debt_money) debt_money,max(deal.debt_date) debt_date ");
            sql.Append(" ,max(deal.responsible_name) responsible_name,max(deal.fio) fio,max(deal.children_count) children_count,max(deal.comment) as comment,max(deal.is_priv) is_priv, ");
            sql.Append(" max(deal.dat_roj) dat_rog, max(deal.nzp_area) area, ");
            sql.Append(" max(trim(t.town)||', '||(case when trim(coalesce(g.rajon,''))='-' then ' ' else trim(coalesce(g.rajon,'')) end) ");
            sql.Append(" ||', '||trim(coalesce(u.ulica,''))||', д.'||trim(coalesce(d.ndom,''))||(case when trim(coalesce(d.nkor,''))='-' then '' else ' '|| ");
            sql.Append(" trim(coalesce(d.nkor,'')) end)||', кв.' ||k.nkvar) as adr,  ");
            sql.Append(" max(deal.dat_roj) dat_roj, max(deal.nzp_dok) dok, max(deal.serij) serij, max(deal.nomer) nomer, ");
            sql.Append(" max(deal.vid_mes) vid_mes, max(deal.vid_dat) vid_dat ");
            
            //sql.Append(" LEFT OUTER JOIN " + pref + "_data" + tableDelimiter + "s_dok dok on dok.nzp_dok=deal.nzp_dok, ");
            sql.Append(" from "+Points.Pref + "_debt" + tableDelimiter + "s_deal_statuses sd, ");
            sql.Append(Points.Pref + "_debt" + tableDelimiter + "deal deal ");
            sql.Append(" LEFT OUTER JOIN " + pref + "_data" + tableDelimiter + "kart kart on kart.nzp_kvar = deal.nzp_kvar, ");
            sql.Append(Points.Pref + "_data" + tableDelimiter + "kvar k ");
            sql.Append(" left outer join " + Points.Pref + "_data" + tableDelimiter + "dom d  ");
            sql.Append(" left outer join " + Points.Pref + "_data" + tableDelimiter + "s_ulica u  ");
            sql.Append(" left outer join " + Points.Pref + "_data" + tableDelimiter + "s_rajon g ");
            sql.Append(" left outer join " + Points.Pref + "_data" + tableDelimiter + "s_town t  ");
            sql.Append(" on g.nzp_town = t.nzp_town  ");
            sql.Append(" on u.nzp_raj  = g.nzp_raj ");
            sql.Append(" on d.nzp_ul  = u.nzp_ul  ");
            sql.Append(" on k.nzp_dom = d.nzp_dom  ");
            sql.Append(" where deal.nzp_deal=" + finder.nzp_deal + " and k.nzp_kvar=deal.nzp_kvar and deal.nzp_deal_status=sd.nzp_deal_status");
            sql.Append(" and cast(coalesce(kart.isactual,'0') as char)<>'100' ");
            var DT = ClassDBUtils.OpenSQL(sql.ToString(), conn_db);
            if (DT.resultCode == -1)
            {
                MonitorLog.WriteLog("LoadDealInfo: Ошибка получения данных о деле, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);
                ret.result = false;
                return null;
            }
            Deal ResultData = new Deal();
            try
            {
                if (DT.resultData.Rows[0]["fio"] == DBNull.Value || DT.resultData.Rows[0]["fio"] == null)
                {
                    sql.Remove(0, sql.Length);
                    sql.Append(" select max(sd.name_deal_status) status,max(deal.debt_money) debt_money,max(deal.debt_date) debt_date ");
                    sql.Append(" ,max(deal.responsible_name) responsible_name,max(deal.fio) fio,max(deal.children_count) children_count,max(deal.comment) as comment,max(deal.is_priv) is_priv, ");
                    sql.Append(" max(deal.dat_roj) dat_rog, max(deal.nzp_area) area, ");
                    sql.Append(" max(trim(t.town)||', '||(case when trim(coalesce(g.rajon,''))='-' then ' ' else trim(coalesce(g.rajon,'')) end) ");
                    sql.Append(" ||', '||trim(coalesce(u.ulica,''))||', д.'||trim(coalesce(d.ndom,''))||(case when trim(coalesce(d.nkor,''))='-' then '' else ' '|| ");
                    sql.Append(" trim(coalesce(d.nkor,'')) end)||', кв.' ||k.nkvar) as adr,  ");
                    sql.Append(" max(deal.dat_roj) dat_roj, max(deal.nzp_dok) dok, max(deal.serij) serij, max(deal.nomer) nomer, ");
                    sql.Append(" max(deal.vid_mes) vid_mes, max(deal.vid_dat) vid_dat ");
                    
                    //sql.Append(" LEFT OUTER JOIN " + pref + "_data" + tableDelimiter + "s_dok dok on dok.nzp_dok=deal.nzp_dok, ");
                    sql.Append(" from " + Points.Pref + "_debt" + tableDelimiter + "s_deal_statuses sd, ");
                    sql.Append(Points.Pref + "_debt" + tableDelimiter + "deal deal ");
                    sql.Append(" LEFT OUTER JOIN " + pref + "_data" + tableDelimiter + "kart kart on kart.nzp_kvar = deal.nzp_kvar, ");
                    sql.Append(Points.Pref + "_data" + tableDelimiter + "kvar k ");
                    sql.Append(" left outer join " + Points.Pref + "_data" + tableDelimiter + "dom d  ");
                    sql.Append(" left outer join " + Points.Pref + "_data" + tableDelimiter + "s_ulica u  ");
                    sql.Append(" left outer join " + Points.Pref + "_data" + tableDelimiter + "s_rajon g ");
                    sql.Append(" left outer join " + Points.Pref + "_data" + tableDelimiter + "s_town t  ");
                    sql.Append(" on g.nzp_town = t.nzp_town  ");
                    sql.Append(" on u.nzp_raj  = g.nzp_raj ");
                    sql.Append(" on d.nzp_ul  = u.nzp_ul  ");
                    sql.Append(" on k.nzp_dom = d.nzp_dom  ");
                    sql.Append(" where deal.nzp_deal=" + finder.nzp_deal + " and k.nzp_kvar=deal.nzp_kvar and deal.nzp_deal_status=sd.nzp_deal_status");
                    sql.Append(" and cast(coalesce(kart.isactual,'0') as char)<>'100' ");
                    DT = ClassDBUtils.OpenSQL(sql.ToString(), conn_db);
                    if (DT.resultCode == -1)
                    {
                        MonitorLog.WriteLog("LoadDealInfo: Ошибка получения данных о деле, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);
                        ret.result = false;
                        return null;
                    }
                }
                ResultData.nzp_deal = finder.nzp_deal;
                ResultData.status = (DT.resultData.Rows[0]["status"] != DBNull.Value ? Convert.ToString(DT.resultData.Rows[0]["status"]).Trim() : "");
                ResultData.responsible_name = (DT.resultData.Rows[0]["responsible_name"] != DBNull.Value ? Convert.ToString(DT.resultData.Rows[0]["responsible_name"]) : "");
                ResultData.debt_money = (DT.resultData.Rows[0]["debt_money"] != DBNull.Value ? Convert.ToDecimal(DT.resultData.Rows[0]["debt_money"]) : 0);
                ResultData.debt_fix_date = (DT.resultData.Rows[0]["debt_date"] != DBNull.Value ? Convert.ToDateTime(DT.resultData.Rows[0]["debt_date"]) : DateTime.MinValue);
                ResultData.fio = (DT.resultData.Rows[0]["fio"] != DBNull.Value ? Convert.ToString(DT.resultData.Rows[0]["fio"]) : "");
                ResultData.comment = (DT.resultData.Rows[0]["comment"] != DBNull.Value ? Convert.ToString(DT.resultData.Rows[0]["comment"]) : "");
                ResultData.adr = (DT.resultData.Rows[0]["adr"] != DBNull.Value ? Convert.ToString(DT.resultData.Rows[0]["adr"]) : "");
                ResultData.children_count = (DT.resultData.Rows[0]["children_count"] != DBNull.Value ? Convert.ToInt32(DT.resultData.Rows[0]["children_count"]) : 0);
                ResultData.dat_rog = (DT.resultData.Rows[0]["dat_rog"] != DBNull.Value ? Convert.ToDateTime(DT.resultData.Rows[0]["dat_rog"]) : DateTime.MinValue);
                ResultData.is_priv = (DT.resultData.Rows[0]["is_priv"] != DBNull.Value ? Convert.ToString(DT.resultData.Rows[0]["is_priv"]) : "");
                ResultData.nzp_area = (DT.resultData.Rows[0]["area"] != DBNull.Value ? Convert.ToInt32(DT.resultData.Rows[0]["area"]) : 0);
                //из sobstw
                ResultData.dok = (DT.resultData.Rows[0]["dok"] != DBNull.Value ? Convert.ToInt32(DT.resultData.Rows[0]["dok"]) : 0);
                ResultData.serij = (DT.resultData.Rows[0]["serij"] != DBNull.Value ? Convert.ToString(DT.resultData.Rows[0]["serij"]).Trim() : "");
                ResultData.nomer = (DT.resultData.Rows[0]["nomer"] != DBNull.Value ? Convert.ToString(DT.resultData.Rows[0]["nomer"]).Trim() : "");
                ResultData.vid_mes = (DT.resultData.Rows[0]["vid_mes"] != DBNull.Value ? Convert.ToString(DT.resultData.Rows[0]["vid_mes"]).Trim() : "");
                ResultData.vid_dat = (DT.resultData.Rows[0]["vid_dat"] != DBNull.Value ? Convert.ToDateTime(DT.resultData.Rows[0]["vid_dat"]) : DateTime.MinValue);


                sql.Remove(0, sql.Length);
                sql.Append(" select distinct area from " + Points.Pref + "_data" + tableDelimiter + "s_area where nzp_area =" + ResultData.nzp_area);
                var area = ClassDBUtils.OpenSQL(sql.ToString(), conn_db);
                if (area.resultCode == -1)
                {
                    MonitorLog.WriteLog("LoadDealInfo: Ошибка получения данных о деле, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);
                    ret.result = false;
                    return null;
                }
                if (area.resultData.Rows.Count > 0 && area != null)
                {
                    ResultData.area = (area.resultData.Rows[0]["area"] != DBNull.Value ? Convert.ToString(area.resultData.Rows[0]["area"]).Trim() : "");
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("LoadDealInfo: Ошибка конвертации данных, sql: " + ex.Message, MonitorLog.typelog.Error, true);
                ret.result = false;
                return null;
            }
            return ResultData;
        }

        public List<Agreement> GetAgreements(DealFinder finder, out Returns ret)
        {

            ret = Utils.InitReturns();
            List<Agreement> res = new List<Agreement>();
            if (finder.nzp_user < 0)
            {
                ret = new Returns(false, "Не задан пользователь", -1);
                return null;
            }
            IDbConnection conn_db = null;
            #region Коннект к базе
            conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;
            #endregion
            StringBuilder sql = new StringBuilder();
            sql.Append(" select * from " + Points.Pref + "_debt" + tableDelimiter + "agreement where nzp_deal=" + finder.nzp_deal + "");
            var DT = ClassDBUtils.OpenSQL(sql.ToString(), conn_db);
            if (DT.resultCode == -1)
            {
                MonitorLog.WriteLog("GetAgreements: Ошибка получения соглашений, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);
                ret.result = false;
                return null;
            }
            for (int i = 0; i < DT.resultData.Rows.Count; i++)
            {
                Agreement zap = new Agreement();
                zap.nzp_agr = (DT.resultData.Rows[i]["nzp_agr"] != DBNull.Value ? Convert.ToInt32(DT.resultData.Rows[i]["nzp_agr"]) : 0);
                zap.number = (DT.resultData.Rows[i]["number"] != DBNull.Value ? Convert.ToString(DT.resultData.Rows[i]["number"]) : "");
                zap.agr_dat = (DT.resultData.Rows[i]["agr_date"] != DBNull.Value ? Convert.ToDateTime(DT.resultData.Rows[i]["agr_date"]) : DateTime.MinValue);
                zap.agr_money = (DT.resultData.Rows[i]["agr_money"] != DBNull.Value ? Convert.ToDecimal(DT.resultData.Rows[i]["agr_money"]) : 0);
                zap.agr_month_count = (DT.resultData.Rows[i]["agr_month_count"] != DBNull.Value ? Convert.ToInt32(DT.resultData.Rows[i]["agr_month_count"]) : 0);
                zap.nzp_agr_status = (DT.resultData.Rows[i]["nzp_agr_status"] != DBNull.Value ? Convert.ToInt32(DT.resultData.Rows[i]["nzp_agr_status"]) : 0);
                res.Add(zap);
            }


            return res;
        }


        public List<Deal> GetDealStatuses(DealFinder finder, out Returns ret)
        {

            ret = Utils.InitReturns();
            List<Deal> res = new List<Deal>();
            if (finder.nzp_user < 0)
            {
                ret = new Returns(false, "Не задан пользователь", -1);
                return null;
            }
            IDbConnection conn_db = null;
            #region Коннект к базе
            conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;
            #endregion
            StringBuilder sql = new StringBuilder();
            sql.Append(" select * from " + Points.Pref + "_debt" + tableDelimiter + "s_deal_statuses");
            var DT = ClassDBUtils.OpenSQL(sql.ToString(), conn_db);
            if (DT.resultCode == -1)
            {
                MonitorLog.WriteLog("GetDealStatus: Ошибка получения статусов Дела, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);
                ret.result = false;
                return null;
            }
            for (int i = 0; i < DT.resultData.Rows.Count; i++)
            {
                Deal zap = new Deal();
                zap.nzp_deal_status = (DT.resultData.Rows[i]["nzp_deal_status"] != DBNull.Value ? Convert.ToInt32(DT.resultData.Rows[i]["nzp_deal_status"]) : 0);
                zap.status = (DT.resultData.Rows[i]["name_deal_status"] != DBNull.Value ? Convert.ToString(DT.resultData.Rows[i]["name_deal_status"]) : "");
                res.Add(zap);
            }


            return res;
        }

        public Returns SaveDealChanges(Deal finder)
        {
            Returns ret = Utils.InitReturns();
            IDbConnection conn_db = null;
            #region Коннект к базе
            conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;
            #endregion
            StringBuilder sql = new StringBuilder();

            sql.Append(" update " + Points.Pref + "_debt" + tableDelimiter + "deal set (debt_money,debt_date,responsible_name,comment,children_count,fio,dat_roj,serij,nomer,vid_dat,vid_mes,nzp_dok)=");
            sql.Append("(" + finder.debt_money + ",'" + finder.debt_fix_date.ToString("dd.MM.yyyy") + "', ");
            sql.Append("" + Utils.EStrNull(finder.responsible_name) + "," + Utils.EStrNull(finder.comment) + "," + finder.children_count + "");
            sql.Append("," + Utils.EStrNull(finder.fio) + ",'" + finder.dat_rog + "'," + Utils.EStrNull(finder.serij) + "," + Utils.EStrNull(finder.nomer));
            sql.Append(",'" + finder.vid_dat + "', " + Utils.EStrNull(finder.vid_mes) +  ","+finder.dok);
            sql.Append(") where nzp_deal=" + finder.nzp_deal + "");
            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("SaveDealChanges: " + sql.ToString(), MonitorLog.typelog.Error, true);
                ret.text = "Ошибка при сохранении изменений в Деле";
                ret.result = false;
                return ret;
            }
            return ret;
        }


        public List<deal_states_history> GetDealHistory(Deal finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<deal_states_history> res = new List<deal_states_history>();
            IDbConnection conn_db = null;
            #region Коннект к базе
            conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;
            #endregion
            StringBuilder sql = new StringBuilder();
            string rows = "";
            string skip = "";
            if (finder.skip != 0)
            {
                skip = " OFFSET " + finder.skip;
            }
            if (finder.rows != 0)
            {
                rows = " LIMIT " + finder.rows;
            }

            sql.Append(" SELECT h.*, s.name FROM " + Points.Pref + "_debt" + tableDelimiter + "deal_states_history h, " + Points.Pref + "_debt" + tableDelimiter + "s_opers s ");
            sql.Append(" WHERE h.nzp_deal=" + finder.nzp_deal + " and h.nzp_oper=s.nzp_oper ORDER BY nzp_deal_state" + skip + " " + rows + " ");
            try
            {
                var DT = ClassDBUtils.OpenSQL(sql.ToString(), conn_db);
                if (DT.resultCode == -1)
                {
                    MonitorLog.WriteLog("GetDealHistory: Ошибка получения истории состояний Дела, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);
                    ret.result = false;
                    return null;
                }
                for (int i = 0; i < DT.resultData.Rows.Count; i++)
                {
                    deal_states_history zap = new deal_states_history();
                    zap.nzp_deal_state = (DT.resultData.Rows[i]["nzp_deal_state"] != DBNull.Value ? Convert.ToInt32(DT.resultData.Rows[i]["nzp_deal_state"]) : 0);
                    zap.date_state = (DT.resultData.Rows[i]["date_state"] != DBNull.Value ? Convert.ToDateTime(DT.resultData.Rows[i]["date_state"]) : DateTime.MinValue);
                    zap.debt_money = (DT.resultData.Rows[i]["debt_money"] != DBNull.Value ? Convert.ToDecimal(DT.resultData.Rows[i]["debt_money"]) : 0);
                    zap.plus = (DT.resultData.Rows[i]["plus"] != DBNull.Value ? Convert.ToDecimal(DT.resultData.Rows[i]["plus"]) : 0);
                    zap.minus = (DT.resultData.Rows[i]["minus"] != DBNull.Value ? Convert.ToDecimal(DT.resultData.Rows[i]["minus"]) : 0);
                    zap.oper = (DT.resultData.Rows[i]["name"] != DBNull.Value ? Convert.ToString(DT.resultData.Rows[i]["name"]) : "");
                    res.Add(zap);
                }
                sql.Remove(0, sql.Length);
                sql.Append(" SELECT count(*) FROM " + Points.Pref + "_debt" + tableDelimiter + "deal_states_history h, " + Points.Pref + "_debt" + tableDelimiter + "s_opers s ");
                sql.Append(" WHERE h.nzp_deal=" + finder.nzp_deal + " and h.nzp_oper=s.nzp_oper ");
                object count = ExecScalar(conn_db, sql.ToString(), out ret, true);
                if (ret.result && count != null && count != DBNull.Value)
                {
                    ret.tag = Convert.ToInt32(count);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("GetDealHistory: Ошибка получения истории состояний Дела, sql: " + ex.Message, MonitorLog.typelog.Error, true);
                ret.result = false;
                ret.text = "\n Ошибка получения истории Дела";
            }

            return res;
        }

        public List<DealCharge> GetDealCharges(Deal finder, int yy, int mm, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<DealCharge> res = new List<DealCharge>();
            IDbConnection conn_db = null;
            #region Коннект к базе
            conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;
            #endregion
            StringBuilder sql = new StringBuilder();
            string rows = "";
            string skip = "";
            if (finder.skip != 0)
            {
                skip = " OFFSET " + finder.skip;
            }
            if (finder.rows != 0)
            {
                rows = " LIMIT " + finder.rows;
            }

            sql.Remove(0, sql.Length);
            sql.Append(" SELECT distinct pref FROM " + Points.Pref + "_debt" + tableDelimiter + "deal");
            sql.Append(" WHERE nzp_deal=" + finder.nzp_deal + "");
            object obj = ExecScalar(conn_db, sql.ToString(), out ret, true);
            if (obj == null || obj == DBNull.Value)
            {
                ret.result = false;
                ret.text = "\n Не найдены реквизиты Дела";
                return null;
            }
            var pref = Convert.ToString(obj).Trim(); //получили прификс для банка где лежат параметры данного ЛС
            if (pref == "")
            {
                ret.result = false;
                ret.text = "\n Не найдены реквизиты Дела";
                return null;
            }
            sql.Remove(0, sql.Length);
            sql.Append(" SELECT max(ch.nzp_charge) nzp_charge,max(s.service_name) service_name,max(supp.name_supp) name_supp,");
            sql.Append(" sum(ch.sum_insaldo) sum_insaldo,sum(ch.sum_tarif+ch.reval+ch.real_charge) sum_nach, ");
            sql.Append(" sum(ch.sum_charge) sum_charge, sum(ch.sum_outsaldo) sum_outsaldo ");
            sql.Append(" FROM " + pref + "_charge_" + yy.ToString("00") + "" + tableDelimiter + "charge_" + mm.ToString("00") + " ch, ");
            sql.Append(" " + Points.Pref + "_kernel" + tableDelimiter + "services s," + Points.Pref + "_kernel" + tableDelimiter + "supplier supp, " + Points.Pref + "_debt" + tableDelimiter + "deal d ");
            sql.Append(" WHERE ch.dat_charge is null and ch.nzp_serv>1 and ch.nzp_serv=s.nzp_serv and supp.nzp_supp=ch.nzp_supp ");
            sql.Append(" and ch.nzp_kvar=d.nzp_kvar and d.nzp_deal=" + finder.nzp_deal);
            sql.Append(" GROUP BY ch.nzp_kvar,ch.nzp_serv,ch.nzp_supp ");
            sql.Append(" ORDER BY 1 " + skip + " " + rows);
            try
            {
                var DT = ClassDBUtils.OpenSQL(sql.ToString(), conn_db);
                if (DT.resultCode == -1)
                {
                    MonitorLog.WriteLog("GetDealCharges: Ошибка получения начислений Дела, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);
                    ret.result = false;
                    return null;
                }
                for (int i = 0; i < DT.resultData.Rows.Count; i++)
                {
                    DealCharge zap = new DealCharge();
                    zap.nzp_charge = (DT.resultData.Rows[i]["nzp_charge"] != DBNull.Value ? Convert.ToInt32(DT.resultData.Rows[i]["nzp_charge"]) : 0);
                    zap.service_name = (DT.resultData.Rows[i]["service_name"] != DBNull.Value ? Convert.ToString(DT.resultData.Rows[i]["service_name"]).Trim() : "");
                    zap.name_supp = (DT.resultData.Rows[i]["name_supp"] != DBNull.Value ? Convert.ToString(DT.resultData.Rows[i]["name_supp"]) : "").Trim();
                    zap.sum_insaldo = (DT.resultData.Rows[i]["sum_insaldo"] != DBNull.Value ? Convert.ToDecimal(DT.resultData.Rows[i]["sum_insaldo"]) : 0);
                    zap.sum_outsaldo = (DT.resultData.Rows[i]["sum_outsaldo"] != DBNull.Value ? Convert.ToDecimal(DT.resultData.Rows[i]["sum_outsaldo"]) : 0);
                    zap.sum_nach = (DT.resultData.Rows[i]["sum_nach"] != DBNull.Value ? Convert.ToDecimal(DT.resultData.Rows[i]["sum_nach"]) : 0);
                    zap.sum_charge = (DT.resultData.Rows[i]["sum_charge"] != DBNull.Value ? Convert.ToDecimal(DT.resultData.Rows[i]["sum_charge"]) : 0);
                    res.Add(zap);
                }
                sql.Remove(0, sql.Length);
                sql.Append(" SELECT count(*)  FROM " + pref + "_charge_" + yy.ToString("00") + "" + tableDelimiter + "charge_" + mm.ToString("00") + " ch, " + Points.Pref + "_kernel" + tableDelimiter + "services s,");
                sql.Append(Points.Pref + "_kernel.supplier supp, " + Points.Pref + "_debt.deal d ");
                sql.Append(" WHERE ch.dat_charge is null and ch.nzp_serv>1 and ch.nzp_serv=s.nzp_serv and supp.nzp_supp=ch.nzp_supp ");
                sql.Append(" and ch.nzp_kvar=d.nzp_kvar and d.nzp_deal=" + finder.nzp_deal);


                object count = ExecScalar(conn_db, sql.ToString(), out ret, true);
                if (ret.result && count != null && count != DBNull.Value)
                {
                    ret.tag = Convert.ToInt32(count);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("GetDealCharges: Ошибка получения начислений Дела, sql: " + ex.Message, MonitorLog.typelog.Error, true);
                ret.result = false;
                ret.text = "\n Ошибка получения начислений";
            }


            return res;
        }

        public List<lawsuit_Data> GetLawSuits(Deal finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            List<lawsuit_Data> res = new List<lawsuit_Data>();
            IDbConnection conn_db = null;
            #region Коннект к базе
            conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;
            #endregion
            StringBuilder sql = new StringBuilder();
            string rows = "";
            string skip = "";
            if (finder.skip != 0)
            {
                skip = " OFFSET " + finder.skip;
            }
            if (finder.rows != 0)
            {
                rows = " LIMIT " + finder.rows;
            }

            sql.Remove(0, sql.Length);
            sql.Append(" SELECT l.nzp_lawsuit,l.lawsuit_price, l.tax, l.lawsuit_date,s.name_lawsuit_status, l.number ");
            sql.Append(" FROM " + Points.Pref + "_debt" + tableDelimiter + "lawsuit l," + Points.Pref + "_debt" + tableDelimiter + "s_lawsuit_statuses s ");
            sql.Append(" WHERE l.nzp_lawsuit_status=s.nzp_lawsuit_status and nzp_deal=" + finder.nzp_deal + skip + " " + rows);
            try
            {
                var DT = ClassDBUtils.OpenSQL(sql.ToString(), conn_db);
                if (DT.resultCode == -1)
                {
                    MonitorLog.WriteLog("GetDealCharges: Ошибка получения исков Дела, sql: " + sql.ToString(), MonitorLog.typelog.Error, true);
                    ret.result = false;
                    return null;
                }
                for (int i = 0; i < DT.resultData.Rows.Count; i++)
                {
                    lawsuit_Data zap = new lawsuit_Data();
                    zap.nzp_lawsuit = (DT.resultData.Rows[i]["nzp_lawsuit"] != DBNull.Value ? Convert.ToInt32(DT.resultData.Rows[i]["nzp_lawsuit"]) : 0);
                    zap.lawsuit_price = (DT.resultData.Rows[i]["lawsuit_price"] != DBNull.Value ? Convert.ToDecimal(DT.resultData.Rows[i]["lawsuit_price"]) : 0);
                    zap.tax = (DT.resultData.Rows[i]["tax"] != DBNull.Value ? Convert.ToDecimal(DT.resultData.Rows[i]["tax"]) : 0);
                    zap.lawsuit_date = (DT.resultData.Rows[i]["lawsuit_date"] != DBNull.Value ? Convert.ToDateTime(DT.resultData.Rows[i]["lawsuit_date"]) : DateTime.MinValue);
                    zap.lawsuit_status = (DT.resultData.Rows[i]["name_lawsuit_status"] != DBNull.Value ? Convert.ToString(DT.resultData.Rows[i]["name_lawsuit_status"]).Trim() : "");
                    zap.number = (DT.resultData.Rows[i]["number"] != DBNull.Value ? Convert.ToString(DT.resultData.Rows[i]["number"]).Trim() : "");
                    res.Add(zap);
                }

                sql.Remove(0, sql.Length);
                sql.Append(" SELECT count(*) ");
                sql.Append(" FROM " + Points.Pref + "_debt" + tableDelimiter + "lawsuit l," + Points.Pref + "_debt" + tableDelimiter + "s_lawsuit_statuses s ");
                sql.Append(" WHERE l.nzp_lawsuit_status=s.nzp_lawsuit_status and nzp_deal=" + finder.nzp_deal + " ");
                object count = ExecScalar(conn_db, sql.ToString(), out ret, true);
                if (ret.result && count != null && count != DBNull.Value)
                {
                    ret.tag = Convert.ToInt32(count);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("GetDealCharges: Ошибка получения исков Дела, sql: " + ex.Message, MonitorLog.typelog.Error, true);
                ret.result = false;
                ret.text = "\n Ошибка получения исков";
            }


            return res;
        }

        public Returns CloseDeal(Deal finder)
        {
            Returns ret = Utils.InitReturns();
            IDbConnection conn_db = null;
            #region Коннект к базе
            conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;
            #endregion
            StringBuilder sql = new StringBuilder();
            sql.Append(" update " + Points.Pref + "_debt" + tableDelimiter + "deal set (nzp_deal_status)=(" + Convert.ToInt32(EnumDealStatuses.Close) + ") where nzp_deal=" + finder.nzp_deal);
            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("CloseDeal: " + sql.ToString(), MonitorLog.typelog.Error, true);
                ret.text = "Ошибка при закрытии Дела";
                ret.result = false;
                return ret;
            }
            sql.Remove(0, sql.Length);
            sql.Append(" update " + Points.Pref + "_debt" + tableDelimiter + "agreement set (nzp_agr_status)=(" + (int)s_agr_statuses.DealClosed + ") ");
            sql.Append(" where nzp_deal=" + finder.nzp_deal + " and nzp_agr_status=" + (int)s_agr_statuses.Сelebrates + "");
            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("CloseDeal: " + sql.ToString(), MonitorLog.typelog.Error, true);
                ret.text = "Ошибка при закрытии Соглашения";
                ret.result = false;
                return ret;
            }

            deal_states_history deal = new deal_states_history();
            deal.nzp_user = finder.nzp_user;
            deal.nzp_deal = finder.nzp_deal;
            //закрытие соглашения
            deal.nzp_oper = (int)EnumOpers.CloseAgreement;
            MakeOperOnDeal(deal, out ret);
            deal.nzp_oper = (int)EnumOpers.CloseDeal;
            MakeOperOnDeal(deal, out ret);

            return ret;
        }


        public Deal CreateDeal(Deal finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            Deal new_deal = new Deal();
            IDbConnection conn_db = null;
            #region Коннект к базе
            conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;
            #endregion

            IDbTransaction transaction = conn_db.BeginTransaction();

            #region определение центрального пользователя
            int nzpUser = finder.nzp_user;

            /*
            DbWorkUser db = new DbWorkUser();
            Finder f_user = new Finder();
            f_user.nzp_user = finder.nzp_user;

            int nzpUser = db.GetLocalUser(conn_db, transaction, f_user, out ret);
            db.Close();
            if (!ret.result)
            {
                return null;
            }*/
            #endregion



            StringBuilder sql = new StringBuilder();
            sql.Append("select count(*) from " + finder.pref + "_data" + tableDelimiter + "prm_1 where nzp=" + finder.nzp_kvar + " and (nzp_prm=8 and cast(coalesce(is_actual,'0') as text)<>'100') ");
            object obj = ExecScalar(conn_db, transaction, sql.ToString(), out ret, true);
            if (obj == null || obj == DBNull.Value)
            {
                ret.result = false;
                ret.text = "\n Ошибка получения реквизитов должника";
                return null;
            }
            if (Convert.ToInt32(obj) == 0)
            {
                sql.Remove(0, sql.Length);
                sql.Append(" INSERT INTO " + Points.Pref + "_debt" + tableDelimiter + "deal(nzp_kvar,children_count,nzp_user,debt_date,start_debt_money,debt_money,nzp_deal_status,nzp_area,pref,is_priv,responsible_name) ");
                sql.Append(" SELECT k.nzp_kvar, 0 as children_count ," + nzpUser + " as nzp_user,now()," + finder.debt_money + " as start_debt_money," + finder.debt_money + " as debt_money, ");
                sql.Append(Convert.ToInt32(EnumDealStatuses.Registered) + " as nzp_deal_status, ");
                sql.Append(" k.nzp_area, k.pref,0 as is_priv,u.comment ");
                sql.Append(" FROM " + Points.Pref + "_data" + tableDelimiter + "kvar k ");
                sql.Append(" LEFT OUTER JOIN " + Points.Pref + "_data" + tableDelimiter + "users u ");
                sql.Append(" on u.nzp_user=" + nzpUser + "  ");
                sql.Append(" WHERE k.nzp_kvar=" + finder.nzp_kvar + " ");
            }
            else
            {
                sql.Remove(0, sql.Length);
                sql.Append(" INSERT INTO " + Points.Pref + "_debt" + tableDelimiter + "deal(nzp_kvar,children_count,nzp_user,debt_date,start_debt_money,debt_money,nzp_deal_status,nzp_area,pref,is_priv,responsible_name) ");
                sql.Append(" SELECT k.nzp_kvar, 0 as children_count ," + nzpUser + " as nzp_user,now()," + finder.debt_money + " as start_debt_money," + finder.debt_money + " as debt_money, ");
                sql.Append(Convert.ToInt32(EnumDealStatuses.Registered) + " as nzp_deal_status, ");
                sql.Append(" k.nzp_area, k.pref,CASE WHEN TRIM(COALESCE(p1.val_prm,'0')) = '' THEN 0 ELSE CAST(TRIM(COALESCE(p1.val_prm,'0')) as INTEGER) END as is_priv,u.comment ");
                sql.Append(" FROM " + Points.Pref + "_data" + tableDelimiter + "kvar k ");
                sql.Append(" LEFT OUTER JOIN " + finder.pref + "_data" + tableDelimiter + "prm_1  p1 ");
                sql.Append(" LEFT OUTER JOIN " + Points.Pref + "_data" + tableDelimiter + "users u ");
                sql.Append(" on u.nzp_user=" + nzpUser + "  ");
                sql.Append(" on p1.nzp=k.nzp_kvar ");
                sql.Append(" WHERE (p1.nzp_prm=8 and cast(coalesce(p1.is_actual,'0') as text)<>'100') and k.nzp_kvar=" + finder.nzp_kvar + " ");
            }
            ret = ExecSQL(conn_db, transaction, sql.ToString(), true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("CreateDeal: " + sql.ToString(), MonitorLog.typelog.Error, true);
                ret.text = "Ошибка создании Дела";
                ret.result = false;
                return null;
            }
            int nzp = 0;
            var new_nzp_deal = GetSerialValue(conn_db, transaction);
            //if (new_nzp_deal != null)
            //{
                nzp = new_nzp_deal;
            //}
            new_deal.nzp_deal = nzp;

            #region пока достаем данные из sobstw, т.к. собственников по одному nzp_kvar может быть несколько берем первого(временно!!!)
            sql.Remove(0, sql.Length);
            sql.Append(" SELECT count(nzp_sobstw) from " + finder.pref + "_data" + tableDelimiter + "sobstw   where is_actual<>100 and nzp_kvar =" + finder.nzp_kvar);
            var count = ExecScalar(conn_db, transaction, sql.ToString(), out ret, true);
            if (count == null || obj == DBNull.Value)
            {
                ret.result = false;
                ret.text = "\n Ошибка получения реквизитов должника";
                return null;
            }
            var nzp_sobstw = new object();
            if (Convert.ToInt32(count) > 1)
            {
                sql.Remove(0, sql.Length);
                sql.Append(" SELECT nzp_sobstw from " + finder.pref + "_data" + tableDelimiter + "sobstw   where is_actual<>100 and nzp_kvar =" + finder.nzp_kvar + " LIMIT 1");
                nzp_sobstw = ExecScalar(conn_db, transaction, sql.ToString(), out ret, true);
                if (nzp_sobstw == null || obj == DBNull.Value)
                {
                    ret.result = false;
                    ret.text = "\n Ошибка получения реквизитов должника";
                    return null;
                }
            }


            string dop_param = "";
            if (Convert.ToInt32(count) > 1)
            {
                dop_param = " and nzp_sobstw = " + Convert.ToInt32(nzp_sobstw) + "";
            }

            sql.Remove(0, sql.Length);

            sql.Append(" UPDATE " + Points.Pref + "_debt" + tableDelimiter + "deal SET (dat_roj,fio,nzp_dok,serij,nomer,vid_mes,vid_dat) = ");
            sql.Append(" ((SELECT dat_rog from " + finder.pref + "_data" + tableDelimiter + "sobstw  where is_actual<>100 and nzp_kvar =" + finder.nzp_kvar + dop_param + "), ");
            sql.Append(" (SELECT  (trim(COALESCE(fam,''))||' '||trim(COALESCE(ima,''))||' '||trim(COALESCE(otch,''))) fio from " + finder.pref + "_data" + tableDelimiter + "sobstw ");
            sql.Append(" where is_actual<>100 and nzp_kvar =" + finder.nzp_kvar + dop_param + "), ");
            sql.Append(" (SELECT  nzp_dok from " + finder.pref + "_data" + tableDelimiter + "sobstw   where is_actual<>100 and nzp_kvar =" + finder.nzp_kvar + dop_param + "), ");
            sql.Append(" (SELECT  serij from " + finder.pref + "_data" + tableDelimiter + "sobstw   where is_actual<>100 and nzp_kvar =" + finder.nzp_kvar + dop_param + "), ");
            sql.Append(" (SELECT  nomer from " + finder.pref + "_data" + tableDelimiter + "sobstw  where is_actual<>100 and nzp_kvar =" + finder.nzp_kvar + dop_param + "), ");
            sql.Append(" (SELECT  vid_mes from " + finder.pref + "_data" + tableDelimiter + "sobstw   where is_actual<>100 and nzp_kvar =" + finder.nzp_kvar + dop_param + "), ");
            sql.Append(" (SELECT  vid_dat from " + finder.pref + "_data" + tableDelimiter + "sobstw  where is_actual<>100 and nzp_kvar =" + finder.nzp_kvar + dop_param + ") ");
            sql.Append(" ) WHERE nzp_deal=" + nzp + " ");

            ret = ExecSQL(conn_db, transaction, sql.ToString(), true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("CreateDeal: " + sql.ToString(), MonitorLog.typelog.Error, true);
                ret.text = "Ошибка создании Дела";
                ret.result = false;
                return null;
            }
            #endregion
            deal_states_history deal = new deal_states_history();
            deal.nzp_user = finder.nzp_user;
            deal.nzp_deal = nzp;
            deal.nzp_oper = (int)EnumOpers.CreateDebt;
            MakeOperOnDeal(deal, conn_db, transaction, out ret);
            if (ret.result)
            {
                transaction.Commit();
            }
            else
            {
                MonitorLog.WriteLog("CreateDeal: " + sql.ToString(), MonitorLog.typelog.Error, true);
                ret.text = "Ошибка создании Дела";
                transaction.Rollback();
                return null;
            }

            return new_deal;
        }

        /// <summary>
        /// Проверяет существования активного дела для должника
        /// </summary>
        /// <param name="nzp_kvar"></param>
        /// <param name="ret"></param>
        /// <returns>true-существует, false - нет</returns>
        public bool ExistDeal(int nzp_kvar, out Returns ret)
        {
            ret = Utils.InitReturns();
            Deal new_deal = new Deal();
            IDbConnection conn_db = null;
            #region Коннект к базе
            conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return false;
            #endregion
            StringBuilder sql = new StringBuilder();
            sql.Append(" select count(*) from " + Points.Pref + "_debt" + tableDelimiter + "deal where nzp_kvar=" + nzp_kvar + " and nzp_deal_status<>" + (int)EnumDealStatuses.Close);
            object obj = ExecScalar(conn_db, sql.ToString(), out ret, true);
            if (obj == null || obj == DBNull.Value)
            {
                ret.result = false;
                ret.text = "\n Ошибка получения реквизитов должника";
                return false;
            }
            var count = Convert.ToInt32(obj);
            if (count == 0)
            {
                ret.result = true;
                return false;
            }
            sql.Remove(0, sql.Length);
            sql.Append("select nzp_deal from " + Points.Pref + "_debt" + tableDelimiter + "deal where nzp_kvar=" + nzp_kvar + " and nzp_deal_status<>" + (int)EnumDealStatuses.Close);
            obj = ExecScalar(conn_db, sql.ToString(), out ret, true);
            if (obj == null || obj == DBNull.Value)
            {
                ret.result = false;
                ret.text = "\n Ошибка получения реквизитов должника";
                return false;
            }
            var nzp_deal = Convert.ToInt32(obj);
            ret.tag = nzp_deal;
            ret.result = true;
            return true;
        }
    }
}
