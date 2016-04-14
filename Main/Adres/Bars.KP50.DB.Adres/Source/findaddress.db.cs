using System;
using System.Data;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System.Text;

namespace STCLINE.KP50.DataBase
{
    public partial class DbFindAddress : DataBaseHeadServer
    {
        public Returns FindLsForServ(Dom finder, Service servfinder)
        {            
            MyDataReader reader;         
            string tXXSpTable = String.Empty;
            #region tXXSpTable - таблица содержащая результат поиска ЛС
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            Returns ret = OpenDb(conn_web, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка открытия соединения с БД.",MonitorLog.typelog.Error,true);
                return ret;
            }
            string nzp = "";
            if (Utils.GetParams(finder.prms, Constants.page_spisls))          
            {//таблица выбранных ЛС
                nzp="nzp_kvar";
                tXXSpTable = DBManager.GetFullBaseName(conn_web, conn_web.Database, "t" + finder.nzp_user + "_spls");
            }
            else 
            {
                nzp = "nzp_dom";
                tXXSpTable = DBManager.GetFullBaseName(conn_web, conn_web.Database, "t" + finder.nzp_user + "_spdom");  
            }
            conn_web.Close();
            #endregion
            
            string table_supplier = "", where_supp = "";
            #region supplier
            if (servfinder.nzp_serv == 0 && 
                servfinder.nzp_payer_agent == 0 && 
                servfinder.nzp_payer_princip == 0 &&  
                servfinder.nzp_payer_supp == 0 &&  
                servfinder.nzp_frm == 0)
            {
                table_supplier = "";
                where_supp = "";
            }
            else 
            {
                table_supplier = ", " + Points.Pref + "_kernel"+tableDelimiter + "supplier s";
                where_supp = " and s.nzp_supp = t.nzp_supp";
            }
            #endregion

            DateTime ds = DateTime.MinValue, dpo = DateTime.MaxValue;
            #region проверка периода
            if (servfinder.dat_s != "" && !DateTime.TryParse(servfinder.dat_s, out ds))
                return new Returns(false, "Неправильно введена дата начала периода", -1);
            if (servfinder.dat_po != "" && !DateTime.TryParse(servfinder.dat_po, out dpo))
                return new Returns(false, "Неправильно введена дата окончания периода", -1);
            #endregion

            string where_for_incl = "", where_for_exclude = "", where = "";
            if (ds != DateTime.MinValue || dpo != DateTime.MaxValue)
            {
                if (ds == DateTime.MinValue) ds = dpo;
                else if (dpo == DateTime.MaxValue) dpo = ds;

                where += " and t.dat_s <= " + Utils.EStrNull(dpo.ToShortDateString());
                where += " and t.dat_po >= " + Utils.EStrNull(ds.ToShortDateString());
            }

            if (servfinder.nzp_serv > 0) where_for_incl += " and t.nzp_serv = " + servfinder.nzp_serv;
            else if (servfinder.nzp_serv < 0)
                if (where_for_exclude == "") where_for_exclude += "( " + " t.nzp_serv = " + -servfinder.nzp_serv;
                else where_for_exclude += " or t.nzp_serv = " + -servfinder.nzp_serv;

            if (servfinder.nzp_payer_agent > 0) where_for_incl += " and s.nzp_payer_agent =" + servfinder.nzp_payer_agent;
            else if (servfinder.nzp_payer_agent < 0)
                if (where_for_exclude == "") where_for_exclude += "( " + "s.nzp_payer_agent =" + -servfinder.nzp_payer_agent;
                else where_for_exclude += " or s.nzp_payer_agent =" + -servfinder.nzp_payer_agent;

            if (servfinder.nzp_payer_princip > 0) where_for_incl += " and s.nzp_payer_princip =" + servfinder.nzp_payer_princip;
            else if (servfinder.nzp_payer_princip < 0)
                if (where_for_exclude == "") where_for_exclude += "( " + " s.nzp_payer_princip =" + -servfinder.nzp_payer_princip;
                else where_for_exclude += " or s.nzp_payer_princip =" + -servfinder.nzp_payer_princip;

            if (servfinder.nzp_payer_supp > 0) where_for_incl += " and s.nzp_payer_supp =" + servfinder.nzp_payer_supp;
            else if (servfinder.nzp_payer_supp < 0)
                if (where_for_exclude == "") where_for_exclude += "( " + "s.nzp_payer_supp =" + -servfinder.nzp_payer_supp;
                else where_for_exclude += " or s.nzp_payer_supp =" + -servfinder.nzp_payer_supp;

            if (servfinder.nzp_frm > 0) where_for_incl += " and t.nzp_frm = " + servfinder.nzp_frm;
            else if (servfinder.nzp_frm < 0)
                if (where_for_exclude == "") where_for_exclude += "( "+ "t.nzp_frm = " + -servfinder.nzp_frm;
                else where_for_exclude += " or t.nzp_frm = " + -servfinder.nzp_frm;

            if (where_for_exclude != "") where_for_exclude = " and " + where_for_exclude + ")";
             
            ExecSQL("drop table temptblnzp", false);
            ret = ExecSQL("create temp table temptblnzp (nzp integer)"
#if PG
#else
                +" with no log"
#endif
                );
            if (!ret.result) return ret;


            string sql = string.Format("select distinct pref from {0}", tXXSpTable);
            ret = ExecRead(out reader, sql.ToString());
            if (!ret.result) return ret;
            while (reader.Read())
            {
                string pref = "";
                if (reader["pref"] != DBNull.Value) pref = Convert.ToString(reader["pref"]).Trim();

                ExecSQL("drop table temptblnzp2", false);
                ret = ExecSQL("create temp table temptblnzp2 (nzp integer)"
#if PG
#else
                +" with no log"
#endif
                );
                if (!ret.result) return ret;

                sql = "insert into temptblnzp2 (nzp) ";
                if (Utils.GetParams(finder.prms, Constants.page_spisls))
                {                    
                    sql += string.Format("select distinct nzp_kvar from {0}_data{1}tarif t {2} where t.is_actual <> 100 {3} {4}",
                            pref, tableDelimiter, table_supplier, where_supp, where + where_for_incl);                                       
                }
                else if (Utils.GetParams(finder.prms, Constants.page_spisdom))
                {                    
                    sql += string.Format("Select distinct nzp_dom From {0}_data{1}kvar k1, {0}_data{1}tarif t {2} Where t.is_actual <> 100 and t.nzp_kvar = k1.nzp_kvar {3} {4}",
                        pref, tableDelimiter, table_supplier, where_supp, where + where_for_incl);                  
                }
                ret = ExecSQL(sql);
                if (!ret.result) return ret;

                if (where_for_exclude != "")
                {
                    if (Utils.GetParams(finder.prms, Constants.page_spisls))
                    {
                        sql = "delete from temptblnzp2 where nzp in (" +
                              string.Format("select distinct nzp_kvar from {0}_data{1}tarif t {2} where t.is_actual <> 100 {3} {4}",
                                pref, tableDelimiter, table_supplier, where_supp, where + where_for_exclude) + ")";
                    }
                    else if (Utils.GetParams(finder.prms, Constants.page_spisdom))
                    {
                        sql = "delete from temptblnzp2 where nzp in (" + 
                            string.Format("Select distinct nzp_dom From {0}_data{1}kvar k1, {0}_data{1}tarif t {2} Where t.nzp_kvar = k1.nzp_kvar {3} {4}",
                            pref, tableDelimiter, table_supplier, where_supp, where + where_for_exclude) + ")";
                    }
                    ret = ExecSQL(sql);
                    if (!ret.result)
                    {
                        ExecSQL("drop table temptblnzp", false);
                        ExecSQL("drop table temptblnzp2", false);
                        return ret;
                    }
                }

                sql = "insert into temptblnzp (nzp) select nzp from temptblnzp2";
                ret = ExecSQL(sql);
                if (!ret.result)
                {
                    ExecSQL("drop table temptblnzp", false);
                    ExecSQL("drop table temptblnzp2", false);
                    return ret;
                }

                ExecSQL("drop table temptblnzp2", false);
            }
            sql = string.Format("delete from {0} txx where txx.{1} not in (select nzp from temptblnzp)", tXXSpTable, nzp);
            ret = ExecSQL(sql);
            if (!ret.result) return ret;
            ExecSQL("drop table temptblnzp");
            if (Utils.GetParams(finder.prms, Constants.page_spisls))
            {
               nzp = "nzp_dom";
               string tXXspdom = DBManager.GetFullBaseName(conn_web, conn_web.Database, "t" + finder.nzp_user + "_spdom");
               sql = string.Format("delete from {0} d where not exists (select {2} from {1} ls where d.{2}=ls.{2})", tXXspdom, tXXSpTable, nzp);
               ret = ExecSQL(sql);
               if (!ret.result) return ret;
            }
            return ret;
        }
    }
}
