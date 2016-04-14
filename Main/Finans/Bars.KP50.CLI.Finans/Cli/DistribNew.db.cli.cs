using System;
using System.Data;
using System.Collections.Generic;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase
{
    /// <summary>
    /// Клиентский класс для работы с формой Сальдо по перечислениям по договорам
    /// </summary>
    public partial class NewDbDistribDomSuppClient : DataBaseHeadClient
    {
        private int nzpUser;

        public NewDbDistribDomSuppClient(int nzpUser)
        {
            this.nzpUser = nzpUser;
        }

        public bool CreateTXXDistribDomSupp(out Returns ret)
        {
#if PG
            ExecSQL("set search_path to 'public'", false);
#endif
            string tXXDistribDom = GetTXXDistribShortName();

            ExecSQL(" Drop table " + tXXDistribDom, false);

            bool result;
            string sql;
            if (!TempTableInWebCashe(tXXDistribDom))
            {
                sql = "CREATE TABLE " + tXXDistribDom + "(" +
                    " nzp_dis SERIAL NOT NULL, " +
                    " pref CHAR(20), " +
                    " year_ INTEGER, " +
                    " month_ INTEGER, " +
                    " nzp_payer_agent INTEGER NOT NULL, " +
                    " agent CHAR(200), " +
                    " nzp_payer_princip INTEGER NOT NULL, " +
                    " princip CHAR(200), " +
                    " nzp_payer_supp INTEGER NOT NULL, " +
                    " supp CHAR(200), " +
                    " nzp_payer_podr INTEGER, " +
                    " podr CHAR(200), " +
                    " nzp_payer INTEGER NOT NULL, " +
                    " payer CHAR(200), " +
                    " nzp_serv INTEGER NOT NULL, " +
                    " nzp_dom INTEGER NOT NULL, " +
                    " adr CHAR(250), " +
                    " service CHAR(100), " +
                    " dat_oper DATE, " +
                    " dat_oper_po DATE, " +
                    " sum_in " + sDecimalType + "(14,2) default 0, " +
                    " sum_rasp " + sDecimalType + "(14,2) default 0, " +
                    " sum_ud " + sDecimalType + "(14,2) default 0, " +
                    " sum_naud " + sDecimalType + "(14,2) default 0, " +
                    " sum_reval " + sDecimalType + "(14,2) default 0, " +
                    " sum_charge " + sDecimalType + "(14,2) default 0, " +
                    " sum_send " + sDecimalType + "(14,2) default 0, " +
                    " sum_out " + sDecimalType + "(14,2) default 0, " +
                    " nzp_bank INTEGER default -1, " +
                    " bank CHAR(200) ) ";
                ret = ExecSQL(sql);
                result = ret.result;
            }
            else
            {
                result = false;
                sql = "delete from " + tXXDistribDom;
                ret = ExecSQL(sql);
            }
            return result;
        }

        public void CreateTXXDistribDomSuppIndexes()
        {
#if PG
            ExecSQL("set search_path to 'public'", false);
#endif
            string tn = GetTXXDistribShortName();
            string ix = "ix" + tn;
            ExecSQL(" Create unique index " + ix + "_1 on " + tn + " (nzp_dis) ", false);
            ExecSQL(" Create index " + ix + "_2 on " + tn + " (nzp_payer, nzp_payer_agent, nzp_payer_princip, nzp_payer_supp, nzp_serv, nzp_dom, nzp_bank, dat_oper) ", false);

        }

        public Returns UpdateStatistics()
        {
            return ExecSQL(sUpdStat + " " + GetTXXDistribShortName());
        }

        private string GetTXXDistribShortName()
        {
            return "t" + nzpUser + "_distrib_dom_supp";
        }

        public string GetTXXDistribFullName()
        {
            return GetTableFullName(GetTXXDistribShortName());
        }

        public List<MoneyDistrib> GetDistribDom(MoneyDistrib finder, out Returns ret)
        {
#if PG
            ExecSQL(" set search_path to 'public'", false);
#endif


            #region Проверка входных параметров
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Не определен пользователь");
                return null;
            }

            if (Utils.GetParams(finder.prms, Constants.page_payer_transfer))
            {
                return GetPayerTransfer(finder, out ret);
            }

            if (finder.dat_oper == "")
            {
                ret = new Returns(false, "Не определен период платежей");
                return null;
            }
            DateTime datOper = DateTime.MinValue;
            DateTime datOperPo = DateTime.MinValue;

            if (!DateTime.TryParse(finder.dat_oper, out datOper))
            {
                ret = new Returns(false, "Неверный формат даты начала платежей");
                return null;
            }

            if (finder.dat_oper_po != "")
            {
                if (!DateTime.TryParse(finder.dat_oper_po, out datOperPo))
                {
                    ret = new Returns(false, "Неверный формат даты окончания платежей");
                    return null;
                }
            }
            else datOperPo = datOper;
            #endregion

            if (finder.pref == "") finder.pref = Points.Pref;

            string tXXDistribDom = GetTXXDistribShortName();

            // составные части запроса
            string sqlItogo;
            if (Utils.GetParams(finder.groupby, Constants.act_groupby_date))
            {
                sqlItogo = "Select sum(case when dat_oper = " + Utils.EStrNull(datOper.ToShortDateString()) + " then sum_in else 0 end) as sum_in" +
                    ", sum(case when dat_oper = " + Utils.EStrNull(datOperPo.ToShortDateString()) + " then sum_out else 0 end) as sum_out";
            }
            else
            {
                sqlItogo = "Select sum(sum_in) as sum_in, sum(sum_out) as sum_out";
            }
            sqlItogo += ", sum(sum_rasp) as sum_rasp, sum(sum_ud) as sum_ud, sum(sum_naud) as sum_naud" +
                ", sum(sum_reval) as sum_reval, sum(sum_charge) as sum_charge, sum(sum_send) as sum_send";

#if PG
            string skip = "";
            string offset = " offset " + finder.skip;
#else
            string skip = " skip "+finder.skip;
            string offset = "";
#endif

            string select = "Select " + skip + " sum(sum_in) as sum_in, sum(sum_out) as sum_out" +
                          ", sum(sum_rasp) as sum_rasp, sum(sum_ud) as sum_ud, sum(sum_naud) as sum_naud" +
                          ", sum(sum_reval) as sum_reval, sum(sum_charge) as sum_charge, sum(sum_send) as sum_send" +
                          ", min(nzp_dis) as nzp_dis, nzp_payer, max(payer) payer, nzp_payer_agent, max(agent) agent, nzp_payer_princip, max(princip) princip, nzp_payer_supp, max(supp) supp, nzp_payer_podr, max(podr) podr, nzp_serv, max(service) service, nzp_dom, max(adr) adr, nzp_bank, max(bank) bank, dat_oper";

            string from = " From " + tXXDistribDom;
            string groupBy = " Group by nzp_payer, nzp_payer_agent, nzp_payer_princip, nzp_payer_supp,nzp_payer_podr,nzp_serv, nzp_dom, nzp_bank, dat_oper";
            string orderBy = " {order_by} {order0}{order1}{order2}{order3}{order4}{order5}{order6}{order7}{order8}";
            string having = " Having sum(sum_rasp) <> 0 or sum(sum_ud) <> 0 or sum(sum_naud) <> 0 or sum(sum_reval) <> 0 or sum(sum_charge) <> 0 or sum(sum_send) <> 0";

            if (Utils.GetParams(finder.groupby, Constants.act_groupby_date))
            {
                sqlItogo += from + having + " or sum(case when dat_oper = " + Utils.EStrNull(datOper.ToShortDateString()) + " then sum_in else 0 end) <> 0" +
                    " or sum(case when dat_oper = " + Utils.EStrNull(datOperPo.ToShortDateString()) + " then sum_out else 0 end) <> 0";
            }
            else
            {
                sqlItogo += from + having + " or sum(sum_in) <> 0 or sum(sum_out) <> 0";
            }

            having += " or sum(sum_in) <> 0 or sum(sum_out) <> 0";

            int sequenceNumber;
            if (Utils.GetParams(finder.groupby, Constants.act_groupby_date.ToString(), out sequenceNumber)) orderBy = orderBy.Replace("{order" + sequenceNumber + "}", ", dat_oper");
            if (Utils.GetParams(finder.groupby, Constants.act_groupby_agent.ToString(), out sequenceNumber)) orderBy = orderBy.Replace("{order" + sequenceNumber + "}", ", agent");
            if (Utils.GetParams(finder.groupby, Constants.act_groupby_princip.ToString(), out sequenceNumber)) orderBy = orderBy.Replace("{order" + sequenceNumber + "}", ", princip");
            if (Utils.GetParams(finder.groupby, Constants.act_groupby_supp.ToString(), out sequenceNumber)) orderBy = orderBy.Replace("{order" + sequenceNumber + "}", ", supp");
            if (Utils.GetParams(finder.groupby, Constants.act_groupby_service.ToString(), out sequenceNumber)) orderBy = orderBy.Replace("{order" + sequenceNumber + "}", ", service");
            if (Utils.GetParams(finder.groupby, Constants.act_groupby_dom.ToString(), out sequenceNumber)) orderBy = orderBy.Replace("{order" + sequenceNumber + "}", ", adr");
            if (Utils.GetParams(finder.groupby, Constants.act_groupby_payer.ToString(), out sequenceNumber)) orderBy = orderBy.Replace("{order" + sequenceNumber + "}", ", payer");
            if (Utils.GetParams(finder.groupby, Constants.act_groupby_bank.ToString(), out sequenceNumber)) orderBy = orderBy.Replace("{order" + sequenceNumber + "}", ", bank");
            if (Utils.GetParams(finder.groupby, Constants.act_groupby_podr.ToString(), out sequenceNumber)) orderBy = orderBy.Replace("{order" + sequenceNumber + "}", ", podr");

            orderBy = orderBy.Replace("{order0}", "")
                .Replace("{order1}", "")
                .Replace("{order2}", "")
                .Replace("{order3}", "")
                .Replace("{order4}", "")
                .Replace("{order5}", "")
                .Replace("{order6}", "")
                .Replace("{order7}", "")
                .Replace("{order8}", "")
                .Replace("{order_by} ,", "Order by ")
                .Replace("{order_by}", "");

            #region определить количество записей
            //ExecSQL("drop table tmp_distrib_dom");
            ExecSQL("drop table tmp_distrib_dom", false);

            string sql = "select nzp_payer, nzp_payer_agent, nzp_payer_princip, nzp_payer_podr, nzp_payer_supp, nzp_serv, nzp_dom, nzp_bank, dat_oper " +
#if PG
 " into temp tmp_distrib_dom " + from + groupBy + having;
#else
                from + groupBy + having + " into temp tmp_distrib_dom with no log";
#endif
            ret = ExecSQL(sql);
            if (!ret.result)
            {
                return null;
            }
            sql = "select count(*) from tmp_distrib_dom";
            object count = ExecScalar(sql, out ret);
            int recordsTotalCount;
            try { recordsTotalCount = Convert.ToInt32(count); }
            catch (Exception e)
            {
                ret = new Returns(false, "Ошибка при определении количества записей: " + (Constants.Debug ? e.Message : ""));
                MonitorLog.WriteLog("Ошибка GetMoneyDistribDom " + (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }

            ExecSQL("drop table tmp_distrib_dom", false);
            #endregion

            #region сформировать запрос
            sql = select + from + groupBy + having + orderBy + offset;

            MyDataReader reader;
            ret = ExecRead(out reader, sql.ToString());
            if (!ret.result)
            {
                return null;
            }

            List<MoneyDistrib> list = new List<MoneyDistrib>();

            try
            {
                int i = finder.skip;
                while (reader.Read())
                {
                    i++;
                    MoneyDistrib zap = new MoneyDistrib();

                    zap.num = i.ToString();

                    zap.nzp_user = finder.nzp_user;

                    if (reader["nzp_dis"] != DBNull.Value) zap.nzp_dis = (int)reader["nzp_dis"];

                    if (reader["nzp_payer_agent"] != DBNull.Value) zap.nzp_payer_agent = (int)reader["nzp_payer_agent"];
                    if (reader["agent"] != DBNull.Value) zap.agent = ((string)reader["agent"]).Trim();

                    if (reader["nzp_payer_princip"] != DBNull.Value) zap.nzp_payer_princip = (int)reader["nzp_payer_princip"];
                    if (reader["princip"] != DBNull.Value) zap.princip = ((string)reader["princip"]).Trim();

                    if (reader["nzp_payer_supp"] != DBNull.Value) zap.nzp_payer_supp = (int)reader["nzp_payer_supp"];
                    if (reader["supp"] != DBNull.Value) zap.supp = ((string)reader["supp"]).Trim();

                    if (reader["nzp_payer_podr"] != DBNull.Value) zap.nzp_payer_podr = (int)reader["nzp_payer_podr"];
                    if (reader["podr"] != DBNull.Value) zap.podr = ((string)reader["podr"]).Trim();

                    if (reader["nzp_serv"] != DBNull.Value) zap.nzp_serv = (int)reader["nzp_serv"];
                    if (reader["service"] != DBNull.Value) zap.service = ((string)reader["service"]).Trim();

                    if (reader["nzp_dom"] != DBNull.Value) zap.nzp_dom = (int)reader["nzp_dom"];
                    if (reader["adr"] != DBNull.Value) zap.adr = ((string)reader["adr"]).Trim();

                    if (reader["nzp_payer"] != DBNull.Value) zap.nzp_payer = (int)reader["nzp_payer"];
                    if (reader["payer"] != DBNull.Value) zap.payer = ((string)reader["payer"]).Trim();

                    if (reader["nzp_bank"] != DBNull.Value) zap.nzp_bank = (int)reader["nzp_bank"];
                    if (reader["bank"] != DBNull.Value) zap.bank = ((string)reader["bank"]).Trim();

                    if (reader["dat_oper"] != DBNull.Value) zap.dat_oper = Convert.ToDateTime(reader["dat_oper"]).ToShortDateString();

                    if (reader["sum_in"] != DBNull.Value) zap.sum_in = Convert.ToDecimal(reader["sum_in"]);
                    if (reader["sum_rasp"] != DBNull.Value) zap.sum_rasp = Convert.ToDecimal(reader["sum_rasp"]);
                    if (reader["sum_ud"] != DBNull.Value) zap.sum_ud = Convert.ToDecimal(reader["sum_ud"]);
                    if (reader["sum_naud"] != DBNull.Value) zap.sum_naud = Convert.ToDecimal(reader["sum_naud"]);
                    if (reader["sum_reval"] != DBNull.Value) zap.sum_reval = Convert.ToDecimal(reader["sum_reval"]);
                    if (reader["sum_charge"] != DBNull.Value) zap.sum_charge = Convert.ToDecimal(reader["sum_charge"]);
                    if (reader["sum_send"] != DBNull.Value) zap.sum_send = Convert.ToDecimal(reader["sum_send"]);
                    if (reader["sum_out"] != DBNull.Value) zap.sum_out = Convert.ToDecimal(reader["sum_out"]);

                    list.Add(zap);

                    if (finder.rows > 0 && list.Count >= finder.rows) break;
                }
            }
            catch (Exception e)
            {
                ret = new Returns(false, "Ошибка загрузки сальдо перечислений:" + (Constants.Debug ? e.Message : ""));
                MonitorLog.WriteLog("Ошибка GetMoneyDistribDom " + (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
            finally
            {
                reader.Close();
            }

            #endregion

            #region сформировать строку итого
            ret = ExecRead(out reader, sqlItogo);
            if (!ret.result)
            {
                return null;
            }

            try
            {
                MoneyDistrib zap = new MoneyDistrib();
                zap.num = "Итого";
                zap.nzp_user = finder.nzp_user;
                if (reader.Read())
                {
                    if (reader["sum_in"] != DBNull.Value) zap.sum_in = Convert.ToDecimal(reader["sum_in"]);
                    if (reader["sum_rasp"] != DBNull.Value) zap.sum_rasp = Convert.ToDecimal(reader["sum_rasp"]);
                    if (reader["sum_ud"] != DBNull.Value) zap.sum_ud = Convert.ToDecimal(reader["sum_ud"]);
                    if (reader["sum_naud"] != DBNull.Value) zap.sum_naud = Convert.ToDecimal(reader["sum_naud"]);
                    if (reader["sum_reval"] != DBNull.Value) zap.sum_reval = Convert.ToDecimal(reader["sum_reval"]);
                    if (reader["sum_charge"] != DBNull.Value) zap.sum_charge = Convert.ToDecimal(reader["sum_charge"]);
                    if (reader["sum_send"] != DBNull.Value) zap.sum_send = Convert.ToDecimal(reader["sum_send"]);
                    if (reader["sum_out"] != DBNull.Value) zap.sum_out = Convert.ToDecimal(reader["sum_out"]);
                }
                list.Add(zap);
            }
            catch (Exception e)
            {
                ret = new Returns(false, "Ошибка загрузки сальдо перечислений:" + (Constants.Debug ? e.Message : ""));
                MonitorLog.WriteLog("Ошибка GetMoneyDistribDom " + (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
            finally
            {
                reader.Close();
            }

            #endregion

            ret.tag = recordsTotalCount;
            return list;
        }

        private List<MoneyDistrib> GetPayerTransfer(MoneyDistrib finder, out Returns ret)
        {
            if (finder.pref == "") finder.pref = Points.Pref;

            string tXXDistrib = GetTXXDistribShortName();

            // составные части запроса
            string sql = "Select nzp_supp, supp, nzp_payer, payer, nzp_serv, service, dat_oper, sum(sum_charge) as sum_charge, sum(sum_send) as sum_send" +
                " From " + tXXDistrib +
                " Group by nzp_payer_agent, agent, nzp_payer_princip, princip, nzp_payer_supp, supp, nzp_payer, payer, nzp_serv, service, dat_oper" +
                " Order by agent, princip, supp, payer, service, dat_oper";

            MyDataReader reader;
            ret = ExecRead(out reader, sql.ToString());
            if (!ret.result)
            {
                return null;
            }

            List<MoneyDistrib> list = new List<MoneyDistrib>();

            decimal sum_charge = 0, sum_send = 0;
            MoneyDistrib zap;
            try
            {
                int i = finder.skip;
                while (reader.Read())
                {
                    i++;
                    zap = new MoneyDistrib();

                    zap.num = i.ToString();

                    zap.nzp_user = finder.nzp_user;

                    if (reader["nzp_supp"] != DBNull.Value) zap.nzp_supp = (int)reader["nzp_supp"];
                    if (reader["supp"] != DBNull.Value) zap.supp = ((string)reader["supp"]).Trim();

                    if (reader["nzp_payer"] != DBNull.Value) zap.nzp_payer = (int)reader["nzp_payer"];
                    if (reader["payer"] != DBNull.Value) zap.payer = ((string)reader["payer"]).Trim();

                    if (reader["nzp_serv"] != DBNull.Value) zap.nzp_serv = (int)reader["nzp_serv"];
                    if (reader["service"] != DBNull.Value) zap.service = ((string)reader["service"]).Trim();

                    if (reader["dat_oper"] != DBNull.Value) zap.dat_oper = Convert.ToDateTime(reader["dat_oper"]).ToShortDateString();

                    if (reader["sum_charge"] != DBNull.Value)
                    {
                        zap.sum_charge = Convert.ToDecimal(reader["sum_charge"]);
                        sum_charge += zap.sum_charge;
                    }
                    if (reader["sum_send"] != DBNull.Value)
                    {
                        zap.sum_send = Convert.ToDecimal(reader["sum_send"]);
                        sum_send += zap.sum_send;
                    }

                    list.Add(zap);

                    if (finder.rows > 0 && list.Count >= finder.rows) break;
                }
            }
            catch (Exception e)
            {
                ret = new Returns(false, "Ошибка загрузки перечислений подрядчикам:" + (Constants.Debug ? e.Message : ""));
                MonitorLog.WriteLog("Ошибка GetPayerTransfer " + (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
            finally
            {
                reader.Close();
            }

            if (finder.nzp_dis < 1) list.Add(new MoneyDistrib() { num = "Итого", nzp_user = finder.nzp_user, sum_charge = sum_charge, sum_send = sum_send });

            return list;
        }
    }
}
