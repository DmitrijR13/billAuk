using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System;
using System.Text;
using System.Collections.Generic;

namespace STCLINE.KP50.DataBase
{
    /// <summary>
    /// Серверный класс для работы с формой Сальдо по перечислениям по договорам
    /// </summary>
    public partial class NewDbDistribDomSuppServer : DataBaseHeadServer
    {
        /// <summary>
        /// Подготавливает таблицу с данными для формы Сальдо по перечислениям
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        public void FindDistribDom(MoneyDistrib finder, out Returns ret)
        {
            #region Проверка входных параметров
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Не определен пользователь");
                return;
            }
            if (finder.dat_oper == "")
            {
                ret = new Returns(false, "Не определен период платежей");
                return;
            }

            if (Utils.GetParams(finder.prms, Constants.page_payer_transfer))
            {
                FindPayerTransfer(finder, out ret);
                return;
            }

            DateTime datOper = DateTime.MinValue;
            DateTime datOperPo = DateTime.MinValue;

            if (!DateTime.TryParse(finder.dat_oper, out datOper))
            {
                ret = new Returns(false, "Неверный формат даты начала платежей");
                return;
            }

            if (finder.dat_oper_po != "")
            {
                if (!DateTime.TryParse(finder.dat_oper_po, out datOperPo))
                {
                    ret = new Returns(false, "Неверный формат даты окончания платежей");
                    return;
                }
            }
            else datOperPo = datOper;
            #endregion

            if (finder.pref == "") finder.pref = Points.Pref;

            #region Создание таблицы tXXDistrib в кэше
            bool isTableCreated;
            string tXXDistribDom_full;

            using (var db = new NewDbDistribDomSuppClient(finder.nzp_user))
            {
                isTableCreated = db.CreateTXXDistribDomSupp(out ret);
                tXXDistribDom_full = db.GetTXXDistribFullName();
            }
            #endregion

            string payer = finder.pref + "_kernel" + tableDelimiter + "s_payer p";
            string service = finder.pref + "_kernel" + tableDelimiter + "services s";
            //string bank = finder.pref + "_kernel"+tableDelimiter+"s_bank b";
            string bank = finder.pref + "_kernel" + tableDelimiter + "s_payer b";
            string dom = finder.pref + "_data" + tableDelimiter + "dom dm";

            DbTables tables = GetDbTablesInstance();

            // составные части запроса
            string insert = "Insert into " + tXXDistribDom_full +
                " (pref, year_, month_, nzp_payer, payer, nzp_payer_agent, agent, nzp_payer_princip, princip, nzp_payer_supp, supp,nzp_payer_podr, podr, nzp_serv, service, nzp_dom, adr, nzp_bank, bank, dat_oper, dat_oper_po, sum_in, sum_rasp, sum_ud, sum_naud, sum_reval, sum_charge, sum_send, sum_out)";
            string select = " Select " + Utils.EStrNull(finder.pref) + "{year}{month}{payer}{agent}{princip}{supp}{podr}{service}{dom}{bank}{dat_oper}";

            if (Utils.GetParams(finder.groupby, Constants.act_groupby_date.ToString()))
                select += ", sum(sum_in) as sum_in";
            else
                select += ", sum(case when dat_oper = " + Utils.EStrNull(datOper.ToShortDateString()) + " then sum_in else 0 end) as sum_in";

            select += ", sum(sum_rasp) as sum_rasp" +
                ", sum(sum_ud) as sum_ud" +
                ", sum(sum_naud) as sum_naud" +
                ", sum(sum_reval) as sum_reval" +
                ", sum(sum_charge) as sum_charge" +
                ", sum(sum_send) as sum_send";

            if (Utils.GetParams(finder.groupby, Constants.act_groupby_date.ToString()))
                select += ", sum(sum_out) as sum_out";
            else
                select += ", sum(case when dat_oper = " + Utils.EStrNull(datOperPo.ToShortDateString()) + " then sum_out else 0 end) as sum_out";

            StringBuilder from = new StringBuilder(" From " + tables.supplier + " supp, {distrib}");
            //string where = " Where d.nzp_serv not in (2,21,22)"; // эти три услуги (2, 21, 22) заменяет услуга 98
            StringBuilder where = new StringBuilder(" Where d.nzp_supp = supp.nzp_supp");
            StringBuilder groupBy = new StringBuilder(" Group by 1");
            StringBuilder having = new StringBuilder(" Having sum(sum_in) <> 0 or sum(sum_rasp) <> 0 or sum(sum_ud) <> 0 or sum(sum_naud) <> 0 or sum(sum_reval) <> 0 or sum(sum_charge) <> 0 or sum(sum_send) <> 0 or sum(sum_out) <> 0");

            if (finder.RolesVal != null)
                foreach (_RolesVal rv in finder.RolesVal)
                {
                    if (rv.tip == Constants.role_sql && rv.val.Trim() != "")
                    {
                        switch (rv.kod)
                        {
                            case Constants.role_sql_supp:
                                where.Append(" and d.nzp_supp in (" + (rv.val[rv.val.Length - 1] == ',' ? rv.val + "0" : rv.val) + ")");
                                break;
                            case Constants.role_sql_serv:
                                where.Append(" and d.nzp_serv in (" + (rv.val[rv.val.Length - 1] == ',' ? rv.val + "0" : rv.val) + ")");
                                break;
                            case Constants.role_sql_payer:
                                where.Append(" and d.nzp_payer in (" + (rv.val[rv.val.Length - 1] == ',' ? rv.val + "0" : rv.val) + ")");
                                break;
                            case Constants.role_sql_bank:
                                if (finder.bank_not_choosen == 0)
                                    where.Append(" and d.nzp_bank in (" + (rv.val[rv.val.Length - 1] == ',' ? rv.val + "0" : rv.val) + ")");
                                break;
                            //case Constants.role_sql_supp:
                            //    where.Append(" and exists (select * from " + payer + "1 where p1.nzp_payer = d.nzp_payer and p1.nzp_supp in (" + (rv.val[rv.val.Length - 1] == ',' ? rv.val + "0" : rv.val) + "))");
                            //    break;
                        }
                    }
                }

            if (finder.agent != null && finder.agent != "") where.Append(" and supp.nzp_payer_agent in (" + finder.agent + ")");
            if (finder.princip != null && finder.princip != "") where.Append(" and supp.nzp_payer_princip in (" + finder.princip + ")");
            if (finder.supp != null && finder.supp != "") where.Append(" and supp.nzp_payer_supp in (" + finder.supp + ")");
            if (finder.podr != null && finder.podr != "") where.Append(" and supp.nzp_payer_podr in (" + finder.podr + ")");


            if (finder.bank_not_choosen == 1) where.Append(" and (d.nzp_bank is null or d.nzp_bank = -1) ");

            int sequenceNumber;
            if (Utils.GetParams(finder.groupby, Constants.act_groupby_date.ToString(), out sequenceNumber))
            {
                select = select.Replace("{dat_oper}", ", dat_oper, dat_oper");
                groupBy.Append(", dat_oper");
            }
            else select = select.Replace("{dat_oper}", ", " + Utils.EStrNull(datOper.ToShortDateString()) + ", " + Utils.EStrNull(datOperPo.ToShortDateString()));

            if (Utils.GetParams(finder.groupby, Constants.act_groupby_agent.ToString(), out sequenceNumber))
            {
                select = select.Replace("{agent}", ", supp.nzp_payer_agent, ''");
                groupBy.Append(", supp.nzp_payer_agent");
            }
            else select = select.Replace("{agent}", ", 0 as nzp_payer_agent, '' as agent");

            if (Utils.GetParams(finder.groupby, Constants.act_groupby_princip.ToString(), out sequenceNumber))
            {
                select = select.Replace("{princip}", ", supp.nzp_payer_princip, ''");
                groupBy.Append(", supp.nzp_payer_princip");
            }
            else select = select.Replace("{princip}", ", 0 as nzp_payer_princip, '' as princip");

            if (Utils.GetParams(finder.groupby, Constants.act_groupby_podr.ToString(), out sequenceNumber))
            {
                select = select.Replace("{podr}", ", supp.nzp_payer_podr, ''");
                groupBy.Append(", supp.nzp_payer_podr");
            }
            else select = select.Replace("{podr}", ", 0 as nzp_payer_podr, '' as princip");

            if (Utils.GetParams(finder.groupby, Constants.act_groupby_supp.ToString(), out sequenceNumber))
            {
                select = select.Replace("{supp}", ", supp.nzp_payer_supp, ''");
                groupBy.Append(", supp.nzp_payer_supp");
            }
            else select = select.Replace("{supp}", ", 0 as nzp_payer_supp, '' as supp");

            if (Utils.GetParams(finder.groupby, Constants.act_groupby_service.ToString(), out sequenceNumber))
            {
                select = select.Replace("{service}", ", d.nzp_serv, s.service");
                from.Append(" left outer join " + service + " ON d.nzp_serv = s.nzp_serv ");
                groupBy.Append(", d.nzp_serv, s.service");
            }
            else select = select.Replace("{service}", ", 0 as nzp_serv, '' as service");

            if (Utils.GetParams(finder.groupby, Constants.act_groupby_dom.ToString(), out sequenceNumber))
            {
                select = select.Replace("{dom}", ", d.nzp_dom, 'ул.'|| u.ulica || ', д.' || dm.ndom");
                from.Append(" left outer join " + dom +
                            " left outer join " + tables.ulica + " u  on u.nzp_ul = dm.nzp_ul " +
                    " ON d.nzp_dom = dm.nzp_dom ");
                if (finder.ndom != "") where.Append(" and d.nzp_dom in (" + finder.ndom + ") ");
                groupBy.Append(", d.nzp_dom, 12, u.ulica, dm.ndom");
            }
            else select = select.Replace("{dom}", ", 0 as nzp_dom, '' as adr");

            if (Utils.GetParams(finder.groupby, Constants.act_groupby_payer.ToString(), out sequenceNumber))
            {
                select = select.Replace("{payer}", ", d.nzp_payer, p.payer");
                from.Append(" left outer join " + payer + " ON d.nzp_payer = p.nzp_payer ");
                groupBy.Append(", d.nzp_payer, p.payer");
            }
            else select = select.Replace("{payer}", ", 0 as nzp_payer, '' as payer");

            if (Utils.GetParams(finder.groupby, Constants.act_groupby_bank.ToString(), out sequenceNumber))
            {
                select = select.Replace("{bank}", ", d.nzp_bank, b.payer as bank");
                from.Append(" left outer join " + bank + " ON d.nzp_bank = b.nzp_payer ");
                groupBy.Append(", d.nzp_bank, b.payer");
            }
            else select = select.Replace("{bank}", ", 0 as nzp_bank, '' as bank");

            /*groupBy = groupBy.Replace("{group_by} ,", "Group by ")
                .Replace("{group_by}", "");*/

            int m1, m2, y1, y2;
            if (datOperPo == DateTime.MinValue)
            {
                m1 = m2 = datOper.Month;
                y1 = y2 = datOper.Year;
                where.Append(" and d.dat_oper = " + Utils.EStrNull(datOper.ToShortDateString()));
                datOperPo = datOper;
            }
            else
            {
                m1 = datOper.Month;
                y1 = datOper.Year;
                m2 = datOperPo.Month;
                y2 = datOperPo.Year;
                where.Append(" and d.dat_oper >= " + Utils.EStrNull(datOper.ToShortDateString()) +
                    " and d.dat_oper <= " + Utils.EStrNull(datOperPo.ToShortDateString()));
            }

            string distrib = "";
            StringBuilder sql;

            for (int y = y1; y <= y2; y++)
                for (int m = 1; m <= 12; m++)
                {
                    if (y == y1 && m < m1) continue;
                    if (y == y2 && m > m2) continue;

                    // проверить наличие таблицы
                    distrib = finder.pref + "_fin_" + (y % 100).ToString("00") + tableDelimiter + "fn_distrib_dom_" + m.ToString("00") + " d";
                    if (!TempTableInWebCashe(distrib)) continue;

                    select = select.Replace("{year}", ", " + y.ToString()).Replace("{month}", ", " + m.ToString());

                    // сформировать запрос
                    sql = new StringBuilder(insert + select + from.Replace("{distrib}", distrib) + where + groupBy + having);

                    ret = ExecSQL(sql.ToString());
                    if (!ret.result)
                    {
                        return;
                    }
                }

            if (Utils.GetParams(finder.groupby, Constants.act_groupby_agent.ToString(), out sequenceNumber))
            {
                ret = ExecSQL("update " + tXXDistribDom_full + " set agent = (select payer from " + tables.payer + " where nzp_payer = " + tXXDistribDom_full + ".nzp_payer_agent)");
                if (!ret.result) return;
            }

            if (Utils.GetParams(finder.groupby, Constants.act_groupby_podr.ToString(), out sequenceNumber))
            {
                ret = ExecSQL("update " + tXXDistribDom_full + " set podr = (select payer from " + tables.payer + " where nzp_payer = " + tXXDistribDom_full + ".nzp_payer_podr)");
                if (!ret.result) return;
            }

            if (Utils.GetParams(finder.groupby, Constants.act_groupby_princip.ToString(), out sequenceNumber))
            {
                ret = ExecSQL("update " + tXXDistribDom_full + " set princip = (select payer from " + tables.payer + " where nzp_payer = " + tXXDistribDom_full + ".nzp_payer_princip)");
                if (!ret.result) return;
            }

            if (Utils.GetParams(finder.groupby, Constants.act_groupby_supp.ToString(), out sequenceNumber))
            {
                ret = ExecSQL("update " + tXXDistribDom_full + " set supp = (select payer from " + tables.payer + " where nzp_payer = " + tXXDistribDom_full + ".nzp_payer_supp)");
                if (!ret.result) return;
            }

            using (NewDbDistribDomSuppClient db = new NewDbDistribDomSuppClient(finder.nzp_user))
            {
                if (isTableCreated) db.CreateTXXDistribDomSuppIndexes();
                db.UpdateStatistics();
            }
        }

        private void FindPayerTransfer(MoneyDistrib finder, out Returns ret)
        {
            #region Проверка входных параметров
            DateTime datOper = DateTime.MinValue;

            if (!DateTime.TryParse(finder.dat_oper, out datOper))
            {
                ret = new Returns(false, "Неверный формат даты начала платежей");
                return;
            }
            #endregion

            if (finder.pref == "") finder.pref = Points.Pref;

            bool isTableCreated;
            string tXXDistribDom_full;

            using (var db = new DbDistribDomSuppClient(finder.nzp_user))
            {
                isTableCreated = db.CreateTXXDistribDomSupp(out ret);
                tXXDistribDom_full = db.GetTXXDistribFullName();
            }

            string distrib = finder.pref + "_fin_" + (datOper.Year % 100).ToString("00") + tableDelimiter + "fn_distrib_" + datOper.Month.ToString("00");

            if (!TempTableInWebCashe(distrib)) return;

            string payer = finder.pref + "_kernel" + tableDelimiter + "s_payer p";
            string supplier = finder.pref + "_data" + tableDelimiter + "supplier supp";
            string service = finder.pref + "_kernel" + tableDelimiter + "services s";
            string bank = finder.pref + "_kernel" + tableDelimiter + "s_payer b";

            // составные части запроса
            string sql = "Insert into " + tXXDistribDom_full +
                " (nzp_dis, pref, year_, month_, nzp_payer_agent, agent, nzp_payer_princip, princip, nzp_payer_supp, supp, nzp_payer, payer, nzp_serv, service, nzp_bank, bank, dat_oper, dat_oper_po, sum_in, sum_rasp, sum_ud, sum_naud, sum_reval, sum_charge, sum_send, sum_out)" +
                " Select nzp_dis, " + Utils.EStrNull(finder.pref) + "," + datOper.Year + "," + datOper.Month +
                ", supp.nzp_payer_agent, null, supp.nzp_payer_princip, null, supp.nzp_payer_supp, null, d.nzp_payer, p.payer, d.nzp_serv, s.service, d.nzp_bank, b.payer as bank, d.dat_oper, d.dat_oper, sum_in, sum_rasp, sum_ud, sum_naud, sum_reval, sum_charge, sum_send, sum_out" +
            " From " + distrib + " d " +
            " inner join " + supplier + " on d.nzp_supp = supp.nzp_supp " +
            " left outer join " + payer + " on d.nzp_payer = p.nzp_payer " +
            " left outer join " + service + " on d.nzp_serv = s.nzp_serv " +
            " left outer join " + bank + " on d.nzp_bank = b.nzp_payer " +
            " Where d.nzp_serv not in (2,21,22)" + // эти три услуги (2, 21, 22) заменяет услуга 98
                " and d.dat_oper = " + Utils.EStrNull(datOper.ToShortDateString()) +
                (finder.nzp_supp > 0 ? " and d.nzp_supp = " + finder.nzp_supp : "") +
                (finder.nzp_payer > 0 ? " and d.nzp_payer = " + finder.nzp_payer : "");

            if (finder.RolesVal != null)
                foreach (_RolesVal rv in finder.RolesVal)
                {
                    if (rv.tip == Constants.role_sql && rv.val.Trim() != "")
                    {
                        switch (rv.kod)
                        {
                            case Constants.role_sql_supp:
                                sql += " and d.nzp_supp in (" + (rv.val[rv.val.Length - 1] == ',' ? rv.val + "0" : rv.val) + ")";
                                break;
                            case Constants.role_sql_serv:
                                sql += " and d.nzp_serv in (" + (rv.val[rv.val.Length - 1] == ',' ? rv.val + "0" : rv.val) + ")";
                                break;
                            case Constants.role_sql_payer:
                                sql += " and d.nzp_payer in (" + (rv.val[rv.val.Length - 1] == ',' ? rv.val + "0" : rv.val) + ")";
                                break;
                            case Constants.role_sql_bank:
                                sql += " and d.nzp_bank in (" + (rv.val[rv.val.Length - 1] == ',' ? rv.val + "0" : rv.val) + ")";
                                break;
                            //case Constants.role_sql_supp:
                            //    sql += " and exists (select * from " + payer + "1 where p1.nzp_payer = d.nzp_payer and p1.nzp_supp in (" + (rv.val[rv.val.Length - 1] == ',' ? rv.val + "0" : rv.val) + "))";
                            //    break;
                        }
                    }
                }

            ret = ExecSQL(sql);
            if (!ret.result)
            {
                return;
            }

            using (var db = new DbDistribDomSuppClient(finder.nzp_user))
            {
                if (isTableCreated) db.CreateTXXDistribDomSuppIndexes();
                db.UpdateStatistics();
            }
        }

        public List<MoneyNaud> FindMoneyNaud(MoneyNaud finder, out Returns ret)
        {
            #region Проверка входных параметров
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Не определен пользователь");
                return null;
            }
            if (finder.nzp_dis < 1)
            {
                ret = new Returns(false, "Не определен пользователь");
                return null;
            }
            if (finder.mode == 0)
            {
                ret = new Returns(false, "Не определена колонка");
                return null;
            }
            #endregion

            ret = Utils.InitReturns();

            if (finder.pref == "") finder.pref = Points.Pref;

            string tXXDistrib = "";

            using (var db = new DbDistribDomSuppClient(finder.nzp_user))
            {
                tXXDistrib = db.GetTXXDistribFullName();
            }
            MyDataReader reader;

            ret = ExecRead(out reader, "select nzp_payer, nzp_payer_agent, nzp_payer_princip, nzp_payer_supp, nzp_serv, nzp_dom, nzp_bank, dat_oper, dat_oper_po from " +
                tXXDistrib + " Where nzp_dis = " + finder.nzp_dis);
            if (!ret.result)
            {
                return null;
            }
            int nzp_payer_agent = 0, nzp_payer_princip = 0, nzp_payer_supp = 0, nzp_serv = 0, nzp_payer = 0, nzp_bank = 0, nzp_dom = 0;
            DateTime dat_oper = DateTime.MinValue, dat_oper_po = DateTime.MinValue;
            try
            {
                if (reader.Read())
                {
                    if (reader["nzp_payer_agent"] != DBNull.Value) nzp_payer_agent = (int)reader["nzp_payer_agent"];
                    if (reader["nzp_payer_princip"] != DBNull.Value) nzp_payer_princip = (int)reader["nzp_payer_princip"];
                    if (reader["nzp_payer_supp"] != DBNull.Value) nzp_payer_supp = (int)reader["nzp_payer_supp"];
                    if (reader["nzp_serv"] != DBNull.Value) nzp_serv = (int)reader["nzp_serv"];
                    if (reader["nzp_payer"] != DBNull.Value) nzp_payer = (int)reader["nzp_payer"];
                    if (reader["nzp_bank"] != DBNull.Value) nzp_bank = (int)reader["nzp_bank"];
                    if (finder.distrib == 2) if (reader["nzp_dom"] != DBNull.Value) nzp_dom = (int)reader["nzp_dom"];
                    if (reader["dat_oper"] != DBNull.Value) dat_oper = Convert.ToDateTime(reader["dat_oper"]);
                    if (reader["dat_oper_po"] != DBNull.Value) dat_oper_po = Convert.ToDateTime(reader["dat_oper_po"]);

                    if (dat_oper == DateTime.MinValue || dat_oper_po == DateTime.MinValue)
                    {
                        ret = new ReturnsType("Не задан операционный день").GetReturns();
                        return null;
                    }
                }
                else
                {
                    ret = new ReturnsType("Данных не найдено").GetReturns();
                    return null;
                }
            }
            catch
            {
                return null;
            }
            finally
            {
                reader.Close();
            }

            ExecSQL("drop table tmp_naud_dom", false);

            int m1, y1, m2, y2;
            m1 = dat_oper.Month;
            y1 = dat_oper.Year;
            m2 = dat_oper_po.Month;
            y2 = dat_oper_po.Year;

            string sql = "";
            for (int y = y1; y <= y2; y++)
            {
                string tn;

                if (finder.mode == 1) //- Следует удержать за обслуживание
                {
                    tn = finder.pref + "_fin_" + (y % 100).ToString("00") + tableDelimiter + "fn_naud_dom";
                    if (!TempTableInWebCashe(tn)) continue;
                    if (sql != "") sql += " union all ";
#if PG
                    if (sql != "")
                        sql += " select s.payer, srv.service, SUM(sum_prih) as sum_prih, " +
                               " perc_ud,SUM(sum_naud) as sum_naud from " +
                               tn + " a, " +
                               Points.Pref + "_kernel" + tableDelimiter + "supplier supp, " +
                               Points.Pref + "_kernel" + tableDelimiter + "s_payer  s," +
                               Points.Pref + "_kernel" + tableDelimiter + "services  srv ";
                    else sql += " select s.payer, srv.service, SUM(sum_prih) as sum_prih, " +
                                " perc_ud,SUM(sum_naud) as sum_naud " +
                                " into temp tmp_naud_dom from " +
                                tn + " a, " +
                                Points.Pref + "_kernel" + tableDelimiter + "supplier supp, " +
                                Points.Pref + "_kernel" + tableDelimiter + "s_payer  s," +
                                Points.Pref + "_kernel" + tableDelimiter + "services  srv ";

#else
                    sql += " select s.payer, srv.service, SUM(sum_prih) as sum_prih, " +
                            " perc_ud,SUM(sum_naud) as sum_naud from " +
                            tn + " a" +
                        "," + Points.Pref + "_kernel" + tableDelimiter + "supplier supp" +
                        "," + Points.Pref + "_kernel" + tableDelimiter + "s_payer  s" +
                        "," + Points.Pref + "_kernel" + tableDelimiter + "services  srv ";
#endif
                    sql += " where a.nzp_supp = supp.nzp_supp" +
                            " and a.nzp_payer = s.nzp_payer and srv.nzp_serv = a.nzp_serv and " +
                            " dat_oper between '" + dat_oper.ToShortDateString() + "' and '" + dat_oper_po.ToShortDateString() + "'";

                    if (nzp_payer_agent > 0) sql += " and supp.nzp_payer_agent = " + nzp_payer_agent;
                    if (nzp_payer_princip > 0) sql += " and supp.nzp_payer_princip = " + nzp_payer_princip;
                    if (nzp_payer_supp > 0) sql += " and supp.nzp_payer_supp = " + nzp_payer_supp;
                    if (nzp_payer > 0) sql += " and a.nzp_payer_2 = " + nzp_payer;
                    if (nzp_serv > 0) sql += " and a.nzp_serv = " + nzp_serv;
                    if (nzp_bank > 0) sql += " and a.nzp_bank = " + nzp_bank;
                    if (nzp_dom > 0) sql += " and a.nzp_dom = " + nzp_dom;

                    sql += " group by 1,2,4";
                }
                else
                {
                    tn = finder.pref + "_fin_" + (y % 100).ToString("00") + tableDelimiter + "fn_perc_dom";
                    if (!TempTableInWebCashe(tn)) continue;
                    if (sql != "") sql += " union all ";

#if PG
                    if (sql != "")
                        sql += " select s.name_supp as payer, srv.service, SUM(sum_prih) as sum_prih,perc_ud,SUM(sum_perc) as sum_naud from " +
                            tn + " a, " +
                            Points.Pref + "_kernel" + tableDelimiter + "supplier  s," +
                            Points.Pref + "_kernel" + tableDelimiter + "services  srv ";
                    else sql += " select s.name_supp as payer, srv.service, SUM(sum_prih) as sum_prih,perc_ud,SUM(sum_perc) as sum_naud " +
                                " into temp tmp_naud_dom from " +
                                tn + " a, " +
                                Points.Pref + "_kernel" + tableDelimiter + "supplier  s," +
                                Points.Pref + "_kernel" + tableDelimiter + "services  srv ";
#else
                    sql += " select s.name_supp as payer, srv.service, SUM(sum_prih) as sum_prih,perc_ud,SUM(sum_perc) as sum_naud from " +
                            tn + " a, " +
                            Points.Pref + "_kernel" + tableDelimiter + "supplier  s," +
                            Points.Pref + "_kernel" + tableDelimiter + "services  srv ";
#endif

                    sql += " where " +
                            "   a.nzp_supp = s.nzp_supp and srv.nzp_serv = a.nzp_serv and " +
                            " dat_oper between '" + dat_oper.ToShortDateString() + "' and '" + dat_oper_po.ToShortDateString() + "'";

                    if (nzp_payer_agent > 0) sql += " and s.nzp_payer_agent = " + nzp_payer_agent;
                    if (nzp_payer_princip > 0) sql += " and s.nzp_payer_princip = " + nzp_payer_princip;
                    if (nzp_payer_supp > 0) sql += " and s.nzp_payer_supp = " + nzp_payer_supp;
                    if (nzp_payer > 0) sql += " and a.nzp_payer = " + nzp_payer;
                    if (nzp_serv > 0) sql += " and a.nzp_serv = " + nzp_serv;
                    if (nzp_bank > 0) sql += " and a.nzp_bank = " + nzp_bank;
                    if (nzp_dom > 0) sql += " and a.nzp_dom = " + nzp_dom;

                    sql += " group by 1,2,4";

                }
            }

            if (sql != "")
            {
#if PG
                //
#else
                sql += " into temp tmp_naud_dom";
#endif
            }
            else
            {
                return new List<MoneyNaud>();
            }

            ret = ExecSQL(sql);
            if (!ret.result)
            {
                return null;
            }

            sql = "Select payer, service, sum_prih, perc_ud, sum_naud " +
                    " From tmp_naud_dom Order by payer";
            ret = ExecRead(out reader, sql);
            if (!ret.result)
            {
                return null;
            }

            List<MoneyNaud> list = new List<MoneyNaud>();
            try
            {
                while (reader.Read())
                {
                    MoneyNaud zap = new MoneyNaud();
                    if (reader["payer"] != DBNull.Value) zap.payer = Convert.ToString(reader["payer"]).Trim();
                    if (reader["service"] != DBNull.Value) zap.service = Convert.ToString(reader["service"]).Trim();
                    if (reader["sum_prih"] != DBNull.Value) zap.sum_prih = Convert.ToDecimal(reader["sum_prih"]);
                    if (reader["perc_ud"] != DBNull.Value) zap.perc_ud = Convert.ToDecimal(reader["perc_ud"]);
                    if (reader["sum_naud"] != DBNull.Value) zap.sum_naud = Convert.ToDecimal(reader["sum_naud"]);

                    list.Add(zap);
                }
            }
            finally
            {
                reader.Close();
            }

            ExecSQL("drop table tmp_naud_dom", false);

            return list;
        }
    }
}
