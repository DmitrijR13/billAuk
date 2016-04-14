using STCLINE.KP50.Global;
using STCLINE.KP50.IFMX.Kernel.source.CommonType;
using STCLINE.KP50.Interfaces;
using System;
using System.Text;
using System.Collections.Generic;

namespace STCLINE.KP50.DataBase
{
    /// <summary>
    /// Серверный класс для работы с перекидками
    /// </summary>
    public partial class DbPerekidkaServer : DataBaseHeadServer
    {
        private Returns PrepareSplsPerekidkiItogo(ParamsForGroupPerekidki finder, string tXX_spls_full, string tXX_spls_perekidki_full)
        {
            MyDataReader reader;

            string sql = "select distinct pref from " + tXX_spls_full + " where mark = 1";

            Returns ret = ExecRead(out reader, sql);
            if (!ret.result) return ret;

            string pref_ = "", charge_table = "";
            Ls ls = new Ls();
            try
            {
                while (reader.Read())
                {
                    if (reader["pref"] != DBNull.Value) pref_ = Convert.ToString(reader["pref"]).Trim();

                    ExecSQL("drop table temptspls");

                    string into_ifmx, into_pg;
#if PG
                    into_ifmx = "";
                    into_pg = " into temp temptspls ";
#else
                    into_ifmx = " into temp temptspls with no log";
                    into_pg = "";
#endif

                    string sq = "select nzp_kvar,num_ls,fio,adr,mark " + into_pg + " from " + tXX_spls_full + " where mark = 1 and pref = '" + pref_ + "'" + into_ifmx;
                    ret = ExecSQL(sq);
                    if (!ret.result) return ret;

                    ret = ExecSQL("create unique index ind_temp_nzp_kvar on temptspls(nzp_kvar)");
                    if (!ret.result) return ret;

                    ret = ExecSQL(sUpdStat + " temptspls");
                    if (!ret.result) return ret;

                    RecordMonth r_m;
                    if (finder.oper_perekidri == ParamsForGroupPerekidki.Operations.SpisInSaldo.GetHashCode() ||
                        finder.oper_perekidri == ParamsForGroupPerekidki.Operations.SpisOutSaldo.GetHashCode() ||
                        finder.oper_perekidri == ParamsForGroupPerekidki.Operations.PerenosOutSaldo.GetHashCode())
                    {
                        DateTime dt;
                        if (!DateTime.TryParse(finder.month_etalon, out dt)) return new Returns(false, "Не верно задан Эталонный месяц", -1);
                        r_m = new RecordMonth {month_ = dt.Month, year_ = dt.Year};
                    }
                    else r_m = Points.GetCalcMonth(new CalcMonthParams(pref_));

                    charge_table = pref_ + "_charge_" + (r_m.year_ % 100).ToString("00") + tableDelimiter + "charge_" + r_m.month_.ToString("00");

                    if (!TempTableInWebCashe(charge_table)) continue;
                 
                    sql = "insert into " + tXX_spls_perekidki_full +
                        " (nzp_kvar, num_ls, fio, adr, pref, mark,nzp_serv,nzp_supp,sum_insaldo,sum_outsaldo, sum_real, real_charge, sum_money) " +
                        " select s.nzp_kvar,s.num_ls,s.fio,s.adr,'" + pref_ + "',s.mark,c.nzp_serv,c.nzp_supp, sum(c.sum_insaldo), sum(c.sum_outsaldo), sum(c.sum_real), sum(c.real_charge), sum(c.sum_money) From " +
                        charge_table + " c, temptspls s where s.nzp_kvar=c.nzp_kvar and dat_charge is null and nzp_serv > 1 " +
                        " group by 1,2,3,4,5,6,7,8";
                    ret = ExecSQL(sql);
                    if (!ret.result) return ret;
                }
            }
            finally
            {
                reader.Close();
            }
            return ret;
        }

        public List<GroupsPerekidki> PrepareSplsPerekidki(ParamsForGroupPerekidki finder, out Returns ret)
        {
            #region проверка параметров
            if (finder.nzp_user <= 0)
            {
                ret = new Returns(false, "Не задан пользователь", -1);
                return null;
            }

            if (finder.dat_uchet == "")
            {
                ret = new Returns(false, "Не задан операционный день", -1);
                return null;
            }

            if (finder.nzp_serv <= 0)
            {
                ret = new Returns(false, "Не задана услуга", -1);
                return null;
            }

            if (finder.nzp_serv > 1)
                if (finder.nzp_supp <= 0 && finder.oper_perekidri != (int)ParamsForGroupPerekidki.Operations.IzmSaldoRaschSum)
                {
                    ret = new Returns(false, "Не задан договор", -1);
                    return null;
                }
                else if (finder.nzp_payer_supp <= 0 && finder.oper_perekidri == (int)ParamsForGroupPerekidki.Operations.IzmSaldoRaschSum)
                {
                    ret = new Returns(false, "Не задан поставщик", -1);
                    return null;
                }

            if (finder.nzp_user <= 0)
            {
                ret = new Returns(false, "Не задан пользователь", -1);
                return null;
            }

            if (finder.oper_perekidri == (int)ParamsForGroupPerekidki.Operations.PerenosOutSaldo && finder.nzp_serv == finder.on_nzp_serv && finder.nzp_supp == finder.on_nzp_supp)
            {
                ret = new Returns(false, "Недопустимые параметры. Пара услуга-поставщик, с которой снимается исходящее сальдо, должна отличаться от пары услуга-поставщик, на которую переносится сальдо", -1);
                return null;
            }
            #endregion

            string tXX_spls_perekidki_full;
            string tXX_spls_full;
            using (DbPerekidkaClient db = new DbPerekidkaClient(finder.nzp_user))
            {
                tXX_spls_perekidki_full = db.GetTXXSplsPerekidkiFullName();
                tXX_spls_full = db.GetTXXSplsFullName();
            }

            string sql = "";
            MyDataReader reader, reader2;

            #region создание и заполнение таблицы tXX_spls_perekidki_full
            if (finder.find_from_start == 1)
            {
                using (DbPerekidkaClient db = new DbPerekidkaClient(finder.nzp_user))
                {
                    ret = db.CreateTXXSplsPerekidki();
                    if (!ret.result) return null;
                }

                if (finder.nzp_serv == 1)
                {
                    ret = PrepareSplsPerekidkiItogo(finder, tXX_spls_full, tXX_spls_perekidki_full);
                    if (!ret.result) return null;
                }
                else
                {
                    if (finder.oper_perekidri != (int)ParamsForGroupPerekidki.Operations.IzmSaldoRaschSum)
                    {
                        string nzp_supp = "0";
                        if (finder.nzp_supp > 0) nzp_supp = finder.nzp_supp.ToString();
                        sql = " insert into " + tXX_spls_perekidki_full + " (nzp_kvar, num_ls, fio, adr, pref, mark,nzp_serv,nzp_supp)" +
                                  " Select s.nzp_kvar, s.num_ls, s.fio, s.adr, s.pref, s.mark," + finder.nzp_serv + "," + nzp_supp + " From " + tXX_spls_full + " s where mark=1 ";
                        ret = ExecSQL(sql);
                        if (!ret.result)
                        {
                            return null;
                        }

                        if (finder.oper_perekidri == (int)ParamsForGroupPerekidki.Operations.PerenosOutSaldo)
                        {
                            sql = " insert into " + tXX_spls_perekidki_full + " (nzp_kvar, num_ls, fio, adr, pref, mark,nzp_serv,nzp_supp)" +
                                  " Select s.nzp_kvar, s.num_ls, s.fio, s.adr, s.pref, s.mark, " + finder.on_nzp_serv + "," + finder.on_nzp_supp + " From " + tXX_spls_full + " s where mark = 1 ";
                            ret = ExecSQL(sql);
                            if (!ret.result)
                            {
                                return null;
                            }
                        }
                    }

                    using (DbPerekidkaClient db = new DbPerekidkaClient(finder.nzp_user))
                    {
                        db.CreateTXXSplsPerekidkiIndexes();
                    }

                    sql = "select distinct pref from " + tXX_spls_full + " where mark = 1 ";
                    ret = ExecRead(out reader, sql);
                    if (!ret.result)
                    {
                        return null;
                    }

                    string pref_ = "", charge_table = "", gil_table;//, nzp_kvar_ = "", nzp = "";
                    //int nzp_serv = 0;
                    try
                    {
                        while (reader.Read())
                        {
                            if (reader["pref"] != DBNull.Value) pref_ = Convert.ToString(reader["pref"]).Trim();

                            sql = " insert into " + tXX_spls_perekidki_full + " (nzp_kvar, num_ls, fio, adr, pref, mark,nzp_serv,nzp_supp)" +
                                  " select s.nzp_kvar, s.num_ls, s.fio, s.adr, s.pref, s.mark, t.nzp_serv, t.nzp_supp from " +
                                  pref_ + "_data" + tableDelimiter + "tarif t, " + tXX_spls_full + " s, " + Points.Pref + "_kernel" + tableDelimiter + "supplier supp " +
                                  " where t.nzp_kvar=s.nzp_kvar and t.num_ls = s.num_ls and t.nzp_supp =supp.nzp_supp " +
                                  " and t.nzp_serv = " + finder.nzp_serv +
                                  " and mark = 1 and pref = '" + pref_ + "' and nzp_payer_supp = " + finder.nzp_payer_supp + " and t.is_actual<>100 and '" +
                                  Convert.ToDateTime(finder.dat_uchet).ToShortDateString() + "' between dat_s and dat_po ";

                            ret = ExecSQL(sql);
                            if (!ret.result)
                            {
                                return null;
                            }

                            RecordMonth r_m;
                            if (finder.oper_perekidri == ParamsForGroupPerekidki.Operations.SpisInSaldo.GetHashCode() ||
                                finder.oper_perekidri == ParamsForGroupPerekidki.Operations.SpisOutSaldo.GetHashCode() ||
                                finder.oper_perekidri == ParamsForGroupPerekidki.Operations.PerenosOutSaldo.GetHashCode())
                            {
                                DateTime dt1;
                                if (!DateTime.TryParse(finder.month_etalon, out dt1))
                                {
                                    ret = new Returns(false, "Не верно задан Эталонный месяц", -1);
                                    return null;
                                }
                                r_m = new RecordMonth { month_ = dt1.Month, year_ = dt1.Year };
                            }
                            else r_m = Points.GetCalcMonth(new CalcMonthParams(pref_));

                            charge_table = pref_ + "_charge_" + (r_m.year_ % 100).ToString("00") + tableDelimiter + "charge_" + r_m.month_.ToString("00");

                            if (!TempTableInWebCashe(charge_table)) continue;

                            var rec1 = Points.GetCalcMonth(new CalcMonthParams(pref_));
                            var dt = new DateTime(rec1.year_, rec1.month_, 1).ToShortDateString();

                            sql = "select * from " + tXX_spls_perekidki_full + " where pref = " + Utils.EStrNull(pref_);
                            ret = ExecRead(out reader2, sql);
                            if (!ret.result)
                            {
                                return null;
                            }
                            
                            sql = "update " + tXX_spls_perekidki_full +
#if PG
                                    " set sum_insaldo = (select sum(sum_insaldo) From " + charge_table + " where nzp_kvar = " + tXX_spls_perekidki_full + ".nzp_kvar and dat_charge is null and nzp_serv = " + tXX_spls_perekidki_full + ".nzp_serv and nzp_supp = " + tXX_spls_perekidki_full + ".nzp_supp)" +
                                    ", sum_outsaldo = (select sum(sum_outsaldo) From " + charge_table + " where nzp_kvar = " + tXX_spls_perekidki_full + ".nzp_kvar and dat_charge is null and nzp_serv = " + tXX_spls_perekidki_full + ".nzp_serv and nzp_supp = " + tXX_spls_perekidki_full + ".nzp_supp)" +
                                    ", sum_real = (select sum(sum_real) From " + charge_table + " where nzp_kvar = " + tXX_spls_perekidki_full + ".nzp_kvar and dat_charge is null and nzp_serv = " + tXX_spls_perekidki_full + ".nzp_serv and nzp_supp = " + tXX_spls_perekidki_full + ".nzp_supp)" +
                                    ", real_charge = (select sum(real_charge) From " + charge_table + " where nzp_kvar = " + tXX_spls_perekidki_full + ".nzp_kvar and dat_charge is null and nzp_serv = " + tXX_spls_perekidki_full + ".nzp_serv and nzp_supp = " + tXX_spls_perekidki_full + ".nzp_supp)" +
                                    ", sum_money = (select sum(sum_money) From " + charge_table + " where nzp_kvar = " + tXX_spls_perekidki_full + ".nzp_kvar and dat_charge is null and nzp_serv = " + tXX_spls_perekidki_full + ".nzp_serv and nzp_supp = " + tXX_spls_perekidki_full + ".nzp_supp)" +
#else
                                    " set (sum_insaldo, sum_outsaldo, sum_real, real_charge, sum_money) = " +
                                    " ((select sum(sum_insaldo), sum(sum_outsaldo), sum(sum_real), sum(real_charge), sum(sum_money) From " +
                                        charge_table + " where nzp_kvar = " + tXX_spls_perekidki_full+ ".nzp_kvar and dat_charge is null and nzp_serv = " + tXX_spls_perekidki_full + ".nzp_serv" + " and nzp_supp = " + tXX_spls_perekidki_full + ".nzp_supp)) " +
#endif
 " where pref = '" + pref_ + "'";

                            ret = ExecSQL(sql, true);
                            if (!ret.result)
                            {
                                return null;
                            }


                            if (finder.oper_perekidri == (int)ParamsForGroupPerekidki.Operations.IzmSaldoRaschSum)
                            {
                                if (finder.sposob_raspr == (int)SposobRaspr.TotSquare)
                                {
                                    sql = "update " + tXX_spls_perekidki_full + " set tot_square = " + sNvlWord + "(" +
                                        " (select max(p.val_prm" + sConvToNum + ") from " + pref_ + "_data" + tableDelimiter + "prm_1 p " +
                                            " where " + Utils.EStrNull(dt) + " between p.dat_s and p.dat_po " +
                                              " and p.is_actual <> 100" +
                                              " and p.nzp_prm = 4" +
                                              " and p.nzp = " + tXX_spls_perekidki_full + ".nzp_kvar),0)" +
                                        " where pref = " + Utils.EStrNull(pref_);
                                    ret = ExecSQL(sql);
                                    if (!ret.result)
                                    {
                                        return null;
                                    }
                                }
                                else if (finder.sposob_raspr == (int)SposobRaspr.OtoplSquare)
                                {
                                    sql = "update " + tXX_spls_perekidki_full + " set otopl_square = " + sNvlWord + "(" +
                                        " (select max(p.val_prm" + sConvToNum + ") from " + pref_ + "_data" + tableDelimiter + "prm_1 p " +
                                            " where " + Utils.EStrNull(dt) + " between p.dat_s and p.dat_po " +
                                              " and p.is_actual <> 100" +
                                              " and p.nzp_prm = 133" +
                                              " and p.nzp = " + tXX_spls_perekidki_full + ".nzp_kvar),0)" +
                                        " where pref = " + Utils.EStrNull(pref_);
                                    ret = ExecSQL(sql);
                                    if (!ret.result)
                                    {
                                        return null;
                                    }
                                }
                                else if (finder.sposob_raspr == (int)SposobRaspr.CountGil)
                                {
                                    gil_table = pref_ + "_charge_" + (r_m.year_ % 100).ToString("00") + tableDelimiter + "gil_" + r_m.month_.ToString("00");

                                    sql = "update " + tXX_spls_perekidki_full + " set kol_gil = " + sNvlWord + "((select max(cnt2 - val3 + val5) from " + gil_table + " where nzp_kvar = " + tXX_spls_perekidki_full + ".nzp_kvar and stek = 3 and dat_charge is null), 0) where pref = " + Utils.EStrNull(pref_);

                                    ret = ExecSQL(sql);
                                    if (!ret.result)
                                    {
                                        return null;
                                    }
                                }
                            }
                        }
                    }
                    finally
                    {
                        reader.Close();
                    }
                }

                sql = "update " + tXX_spls_perekidki_full +
                    " set sum_insaldo = case when sum_insaldo is null then 0 else sum_insaldo end" +
                    ", sum_outsaldo = case when sum_outsaldo is null then 0 else sum_outsaldo end" +
                    ", sum_real = case when sum_real is null then 0 else sum_real end" +
                    ", real_charge = case when real_charge is null then 0 else real_charge end" +
                    ", sum_money = case when sum_money is null then 0 else sum_money end" +
                    ", tot_square = case when tot_square is null then '0' else tot_square end" +
                    ", otopl_square = case when otopl_square is null then '0' else otopl_square end" +
                    ", kol_gil = case when kol_gil is null then 0 else kol_gil end";
                ret = ExecSQL(sql);
                if (!ret.result) return null;

                using (DbPerekidkaClient db = new DbPerekidkaClient(finder.nzp_user))
                {
                    ret = db.IzmSaldo(finder);
                    if (!ret.result)
                    {
                        return null;
                    }
                }
            }
            #endregion

            List<GroupsPerekidki> list = new List<GroupsPerekidki>();
            if (finder.oper_perekidri == (int)ParamsForGroupPerekidki.Operations.PerenosOutSaldo)
            {
                sql = " Select count(*) From " + tXX_spls_perekidki_full + " spp, " + Points.Pref + "_kernel" + tableDelimiter + "services s" +
                      " where spp.nzp_serv = s.nzp_serv";
            }
            else if (finder.nzp_serv == 1)
            {
                sql = " Select count(distinct nzp_kvar) From " + tXX_spls_perekidki_full;
            }
            else sql = "Select count(*) From " + tXX_spls_perekidki_full;

            object count2 = ExecScalar(sql, out ret);
            int recordsTotalCount;
            try { recordsTotalCount = Convert.ToInt32(count2); }
            catch (Exception e)
            {
                ret = new Returns(false, "Ошибка при определении количества записей: " + (Constants.Debug ? e.Message : ""));
                MonitorLog.WriteLog("Ошибка при определении количества записей " + (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }

            if (finder.oper_perekidri == (int)ParamsForGroupPerekidki.Operations.PerenosOutSaldo)
            {
                sql = " Select * From " + tXX_spls_perekidki_full + " spp, " + Points.Pref + "_kernel" + tableDelimiter + "services s" +
                    " where spp.nzp_serv = s.nzp_serv order by fio, service";
            }
            else if (finder.nzp_serv == 1)
            {
                sql = " Select nzp_kvar,adr,num_ls,fio,tot_square,otopl_square,kol_gil,sum(sum_insaldo) as sum_insaldo, " +
                      " sum(sum_izm) as sum_izm,sum(sum_money) as sum_money,sum(sum_new_outsaldo) as sum_new_outsaldo, " +
                      " sum(sum_outsaldo) as sum_outsaldo, sum(sum_real) as sum_real, sum(real_charge) as real_charge From " + tXX_spls_perekidki_full +
                      " group by nzp_kvar,adr,num_ls,fio,tot_square,otopl_square,kol_gil";
            }
            else sql = "select * from " + tXX_spls_perekidki_full;
            ret = ExecRead(out reader2, sql);
            if (!ret.result)
            {
                return null;
            }
            int i = 0;
            try
            {
                while (reader2.Read())
                {
                    i++;
                    if (i <= finder.skip) continue;
                    GroupsPerekidki gp = new GroupsPerekidki();
                    gp.num = i.ToString();
                    if (reader2["nzp_kvar"] != DBNull.Value) gp.nzp_kvar = Convert.ToInt32(reader2["nzp_kvar"]);
                    if (reader2["adr"] != DBNull.Value) gp.adr = Convert.ToString(reader2["adr"]);
                    if (reader2["num_ls"] != DBNull.Value) gp.num_ls = Convert.ToInt32(reader2["num_ls"]);
                    if (reader2["fio"] != DBNull.Value) gp.fio = Convert.ToString(reader2["fio"]);
                    if (reader2["tot_square"] != DBNull.Value) gp.tot_square = Convert.ToDecimal(reader2["tot_square"]);
                    if (reader2["otopl_square"] != DBNull.Value) gp.otopl_square = Convert.ToDecimal(reader2["otopl_square"]);
                    if (reader2["kol_gil"] != DBNull.Value) gp.kol_gil = Convert.ToDecimal(reader2["kol_gil"]);
                    if (reader2["sum_insaldo"] != DBNull.Value) gp.sum_insaldo = Convert.ToDecimal(reader2["sum_insaldo"]);
                    if (reader2["sum_izm"] != DBNull.Value) gp.sum_izm = Convert.ToDecimal(reader2["sum_izm"]);
                    if (reader2["sum_money"] != DBNull.Value) gp.sum_money = Convert.ToDecimal(reader2["sum_money"]);
                    if (reader2["sum_new_outsaldo"] != DBNull.Value) gp.sum_new_outsaldo = Convert.ToDecimal(reader2["sum_new_outsaldo"]);
                    if (reader2["sum_outsaldo"] != DBNull.Value) gp.sum_outsaldo = Convert.ToDecimal(reader2["sum_outsaldo"]);
                    if (reader2["sum_real"] != DBNull.Value) gp.sum_real = Convert.ToDecimal(reader2["sum_real"]);
                    if (reader2["real_charge"] != DBNull.Value) gp.real_charge = Convert.ToDecimal(reader2["real_charge"]);
                    if (finder.oper_perekidri == (int)ParamsForGroupPerekidki.Operations.PerenosOutSaldo)
                        if (reader2["service"] != DBNull.Value) gp.service = Convert.ToString(reader2["service"]);

                    list.Add(gp);
                    if (finder.rows > 0 && i >= finder.skip + finder.rows) break;
                }
            }
            finally
            {
                reader2.Close();
            }
            ret.tag = recordsTotalCount;
            return list;
        }

        public Returns SavePerekidkiOplatami(DelSupplier finder)
        {
            if (finder.pref == "") return new Returns(false, "Не задан префикс");
            if (finder.dat_uchet == "") return new Returns(false, "Не задан операционный день");

            string table_del_supplier = finder.pref + "_charge_" + (Convert.ToDateTime(finder.dat_uchet).Year % 100).ToString() + tableDelimiter + "del_supplier";

            if (!TempTableInWebCashe(table_del_supplier)) return new Returns(false, "Нет таблицы в БД", -1);

            StringBuilder sql = new StringBuilder();

            DbCharge db = new DbCharge();
            int num_ls = db.GetNumLsFromNzpKvar(finder.nzp_kvar, finder.pref, ServerConnection, null);

            DateTime operday = Convert.ToDateTime(finder.dat_uchet);
            DateTime dat_month = new DateTime(operday.Year, operday.Month, 1);
            DateTime dat_account = new DateTime(operday.Year, operday.Month, 28);
            BeginTransaction();
            Returns ret = db.SaveDocumentBase(ServerConnection, Transaction, finder.doc_base);
            if (!ret.result)
            {
                Rollback();
                return ret;
            }
            finder.doc_base.nzp_doc_base = ret.tag;
            int nzp_dict = 0;
            if (finder.nzp_del > 0)
            {
                nzp_dict = 6599; //изменение перекидки
                sql.AppendFormat(" update {0} set ", table_del_supplier);
                sql.AppendFormat(" nzp_serv = {0}, nzp_supp = {1}, sum_prih = {2}, dat_month = '{3}', dat_prih = '{4}', dat_uchet = '{5}', dat_account = '{6}'", finder.nzp_serv,
                    finder.nzp_supp, finder.sum_prih, dat_month.ToShortDateString(), sCurDateTime, operday.ToShortDateString(), dat_account.ToShortDateString());
                sql.AppendFormat(" where nzp_del = {0}", finder.nzp_del);
            }
            else
            {
                nzp_dict = 6597; //добавление перекидки
                sql.AppendFormat(" insert into {0} (nzp_serv, nzp_supp, num_ls, sum_prih, kod_sum, dat_month, dat_prih, dat_uchet, dat_account, nzp_doc_base, type_rcl)",
                    table_del_supplier);
                sql.AppendFormat(" values ({0}, {1}, {2}, {3}, {4}, '{5}', '{6}', '{7}', '{8}', {9}, {10})", finder.nzp_serv, finder.nzp_supp, num_ls, finder.sum_prih, 51,
                    dat_month.ToShortDateString(), sCurDateTime, operday.ToShortDateString(), dat_account.ToShortDateString(), finder.doc_base.nzp_doc_base, finder.typercl.type_rcl);
            }
            ret = ExecSQL(sql.ToString());
            if (!ret.result)
            {
                Rollback();
                return ret;
            }

            #region Добавление в sys_events события
            DbAdmin.InsertSysEvent(new SysEvents()
            {
                pref = finder.pref,
                nzp_user = finder.nzp_user,
                nzp_dict = nzp_dict,
                nzp_obj = finder.nzp_kvar,
                note = "Изменение сальдо оплатами. Перекидка на сумму " + finder.sum_prih + " была " + (nzp_dict == 6597 ? "добавлена" : "изменена.")
            }, Transaction, ServerConnection);
            #endregion
            Commit();
            if (ret.result)
            {
                var dbCalc = new DbCalcCharge();
                Returns ret2 = dbCalc.CalcChargeXXUchetOplatForLs(ServerConnection, null, new Charge() { num_ls = num_ls, pref = finder.pref, year_ = operday.Year, month_ = operday.Month }, CalcTypes.FunctionType.Perekidki);
                dbCalc.Close();
            }

            db.Close();
            return ret;
        }

        public Returns DeletePerekidkaOplatami(DelSupplier finder)
        {
            Returns ret = Utils.InitReturns();

            if (!(finder.nzp_user > 0))
            {
                ret.text = "Пользователь не задан";
                ret.tag = -1;
                ret.result = false;
                return ret;
            }

            if (finder.pref == "")
            {
                ret.text = "Префикс не задан";
                ret.tag = -1;
                ret.result = false;
                return ret;
            }

            if (!(finder.year_ > 0))
            {
                ret.text = "Год не задан";
                ret.tag = -1;
                ret.result = false;
                return ret;
            }

            StringBuilder sql = new StringBuilder();
            string usl = "";
            usl += " and yearr = " + finder.year_.ToString();
            sql.AppendFormat("select dbname, yearr from {0}_kernel{1}s_baselist where idtype=1 {2}", finder.pref, tableDelimiter, usl);
            MyDataReader reader;
            if (!ExecRead(out reader, sql.ToString()).result)
            {
                return ret;
            }
            DbTables tables = new DbTables(ServerConnection);
            string dbname = "", dat_uchet = "";
            int num_ls = 0;
            try
            {
                if (reader.Read())
                {
                    if (reader["dbname"] != DBNull.Value) dbname = Convert.ToString(reader["dbname"]).Trim();
#if PG
                    ret = ExecSQL("set search_path to '" + dbname + "'", false);
#else
                    ret = ExecSQL("database " + dbname, false);
#endif
                    if (!ret.result) return ret;

                    if (finder.nzp_del > 0)
                    {
                        BeginTransaction();
                        sql.Remove(0, sql.Length);
                        sql.AppendFormat("select nzp_doc_base, dat_uchet, num_ls from {0}{1}del_supplier where nzp_del = {2}", dbname, tableDelimiter, finder.nzp_del);
                        MyDataReader myreader;
                        ret = ExecRead(out myreader, sql.ToString());
                        if (!ret.result)
                        {
                            Rollback();
                            return ret;
                        }
                        int nzp_doc_base = 0;
                        if (myreader.Read())
                        {
                            if (myreader["nzp_doc_base"] != DBNull.Value) nzp_doc_base = Convert.ToInt32(myreader["nzp_doc_base"]);
                            if (myreader["dat_uchet"] != DBNull.Value) dat_uchet = Convert.ToDateTime(myreader["dat_uchet"]).ToShortDateString();
                            if (myreader["num_ls"] != DBNull.Value) num_ls = Convert.ToInt32(myreader["num_ls"]);
                        }

                        sql.Remove(0, sql.Length);
                        sql.AppendFormat("delete from {0}{1}del_supplier where nzp_del = {2}", dbname, tableDelimiter, finder.nzp_del);
                        ret = ExecSQL(sql.ToString(), true);
                        if (!ret.result)
                        {
                            Rollback();
                            return ret;
                        }

                        sql.Remove(0, sql.Length);
                        sql.AppendFormat(" delete from {0} where nzp_doc_base = {1}", tables.document_base, nzp_doc_base);
                        ret = ExecSQL(sql.ToString(), true);
                        if (!ret.result)
                        {
                            Rollback();
                            return ret;
                        }

                        #region Добавление в sys_events события
                        DbAdmin.InsertSysEvent(new SysEvents()
                        {
                            pref = finder.pref,
                            nzp_user = finder.nzp_user,
                            nzp_dict = 6598,
                            nzp_obj = finder.nzp_kvar,
                            note = "Изменение сальдо оплатами. Перекидка была удалена."
                        }, Transaction, ServerConnection);
                        #endregion

                        Commit();

                        if (ret.result)
                        {
                            var dbCalc = new DbCalcCharge();
                            Returns ret2 = dbCalc.CalcChargeXXUchetOplatForLs(ServerConnection, null, new Charge() { num_ls = num_ls, pref = finder.pref, year_ = Convert.ToDateTime(dat_uchet).Year, month_ = Convert.ToDateTime(dat_uchet).Month }, CalcTypes.FunctionType.Perekidki);
                            dbCalc.Close();
                        }
                    }
                }
                reader.Close();

                /* if (ret.result)
                 {
                     var dbCalc = new DbCalcCharge();
                     Returns ret2 = dbCalc.CalcChargeXXUchetOplatForLs(conn_db, null, new Charge() { num_ls = num_ls, pref = finder.pref, year_ = finder.year_, month_ = month });
                     dbCalc.Close();
                 }*/
                return ret;
            }
            catch (Exception ex)
            {
                if (reader != null) reader.Close();
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Изменение сальдо оплатами. Ошибка удаления перекидок " + err, MonitorLog.typelog.Error, 20, 201, true);
                return ret;
            }

            //return ret;
        }

        private Returns UpdateSumReestrPerekidokDelSupp(DelSupplier finder, string charge_dbname)
        {
            Returns ret;
            string reestr_perekidok = Points.Pref + "_fin_" + (finder.year_ % 100).ToString("00") + tableDelimiter + "reestr_perekidok";
            string sql = "update " + reestr_perekidok + " set sum_oper = (" +
                         reestr_perekidok + ".sum_oper - (select sum_prih from " + charge_dbname + tableDelimiter + "del_supplier p where nzp_del = " + finder.nzp_del + ") + " + finder.sum_prih +
                         " ) where nzp_oper = " + (int)ParamsForGroupPerekidki.Operations.PerekidkaLs +
                         " and nzp_reestr = (select nzp_reestr from " + charge_dbname + tableDelimiter + "del_supplier  where nzp_del =   " + finder.nzp_del + ")";
            ret = ExecSQL(sql, true);
            return ret;
        }

        public List<DelSupplier> LoadPerekidkiOplatami(DelSupplier finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            if (finder.nzp_kvar == 0)
            {
                ret = new Returns(false, "Не выбран лицевой счет", -1);
                return null;
            }

            if (finder.pref == "")
            {
                ret = new Returns(false, "Не задан префикс", -1);
                return null;
            }

            StringBuilder sql = new StringBuilder();
            DateTime operday = DateTime.MinValue;
            string usl = "";
            if (finder.dat_uchet != "")
            {
                operday = Convert.ToDateTime(finder.dat_uchet);
                usl += " and yearr = " + operday.Year;
            }

            List<DelSupplier> list = new List<DelSupplier>();
            DelSupplier perekidka;

            List<DelSupplier> lf = new List<DelSupplier>();
            MyDataReader reader, reader2 = null;
            decimal itog_sum = 0;

            sql.AppendFormat("select dbname, yearr from {0}_kernel{1}s_baselist where idtype=1 {2}", finder.pref, tableDelimiter, usl);
            ret = ExecRead(out reader, sql.ToString());
            if (!ret.result) return null;

            try
            {
                string from_where =
#if PG
 "from " +
                       finder.pref + "_kernel.services s, " +
                       finder.pref + "_kernel.supplier sp, " +
                       Points.Pref + "_kernel.s_typercl tr, " +
                       Points.Pref + "_data.kvar k, " +
                       "{BASE}.del_supplier p " +
                       " left outer join " + Points.Pref + "_fin_{YEAR}.reestr_perekidok rp on rp.nzp_reestr = p.nzp_reestr" +
                          " left outer join " + Points.Pref + "_data.document_base db " +
                           "  left outer join " + Points.Pref + "_kernel.s_type_doc td on  td.nzp_type_doc = db.nzp_type_doc " +
                           " on db.nzp_doc_base = p.nzp_doc_base " +
                       " where k.nzp_kvar = " + finder.nzp_kvar + "  and p.nzp_serv = s.nzp_serv and k.num_ls = p.num_ls " +
                       " and p.nzp_supp = sp.nzp_supp and tr.type_rcl = p.type_rcl ";
#else
                          "from {BASE}:del_supplier p, " +
                          finder.pref + "_kernel:services s, " +
                          finder.pref + "_kernel:supplier sp, " +
                          Points.Pref + "_kernel: s_typercl tr, " +
                          Points.Pref + "_data: kvar k, " +
                         "outer "+ Points.Pref + "_data:document_base db, "+
                          " outer " + Points.Pref + "_kernel:s_type_doc td, " +
                          " outer " + Points.Pref + "_fin_{YEAR}:reestr_perekidok rp " +
                          "where k.nzp_kvar = " + finder.nzp_kvar + "  and p.nzp_serv = s.nzp_serv  and k.num_ls = p.num_ls " +
                          " and p.nzp_doc_base = db.nzp_doc_base and td.nzp_type_doc = db.nzp_type_doc and p.nzp_supp = sp.nzp_supp and tr.type_rcl = p.type_rcl " + 
                          " and rp.nzp_reestr = p.nzp_reestr ";
#endif
                int i = 0;
                int totalrecords = 0;
                while (reader.Read())
                {
                    DelSupplier f = new DelSupplier();
                    if (reader["yearr"] != DBNull.Value) f.year_ = Convert.ToInt32(reader["yearr"]);
                    if (reader["dbname"] != DBNull.Value) f.database = Convert.ToString(reader["dbname"]).Trim();
                    if (operday != DateTime.MinValue)
                        if (operday.Year != 0 && operday.Year != f.year_) continue;
                    if (f.year_ > Points.DateOper.Year) continue;
                    lf.Add(f);

                    if (!TempTableInWebCashe(f.database + tableDelimiter + "del_supplier")) continue;

                    //подсчитать итого
                    sql.Remove(0, sql.Length);
                    sql.AppendFormat("select sum(p.sum_prih) as sum_prih {0}", from_where.Replace("{BASE}", f.database).Replace("{YEAR}", (f.year_ % 100).ToString("00")));
                    ret = ExecRead(out reader2, sql.ToString());
                    if (!ret.result) continue;
                    if (reader2.Read())
                        if (reader2["sum_prih"] != DBNull.Value) itog_sum += Convert.ToDecimal(reader2["sum_prih"]);
                    reader2.Close();

                    //определить количество записей
                    sql.Remove(0, sql.Length);
                    sql.AppendFormat("select count(*) as cnt {0}", from_where.Replace("{BASE}", f.database).Replace("{YEAR}", (f.year_ % 100).ToString("00")));
                    ret = ExecRead(out reader2, sql.ToString());
                    if (!ret.result) continue;

                    while (reader2.Read())
                    {
                        if (reader2["cnt"] != DBNull.Value) totalrecords += Convert.ToInt32(reader2["cnt"]);
                    }
                    reader2.Close();

                    //взять данные
                    sql.Remove(0, sql.Length);
                    sql.AppendFormat("select p.nzp_del, k.nzp_kvar, p.nzp_serv, p.nzp_supp, p.dat_month, p.nzp_doc_base, " +
                          "p.sum_prih, p.dat_prih, p.dat_uchet, db.comment, p.dat_account, s.service, sp.name_supp, " +
                          "tr.type_rcl, tr.typename, rp.nzp_reestr, rp.nzp_oper, td.doc_name as type_doc, db.nzp_type_doc, db.num_doc, db.dat_doc {0}",
                          from_where.Replace("{BASE}", f.database).Replace("{YEAR}", (f.year_ % 100).ToString("00")));
                    ret = ExecRead(out reader2, sql.ToString());
                    if (!ret.result) continue;

                    while (reader2.Read())
                    {
                        i++;
                        if (i <= finder.skip) continue;
                        perekidka = new DelSupplier();
                        if (reader2["nzp_del"] != DBNull.Value) perekidka.nzp_del = Convert.ToInt32(reader2["nzp_del"]);
                        if (reader2["nzp_kvar"] != DBNull.Value) perekidka.nzp_kvar = Convert.ToInt32(reader2["nzp_kvar"]);
                        if (reader2["nzp_serv"] != DBNull.Value) perekidka.nzp_serv = Convert.ToInt32(reader2["nzp_serv"]);
                        if (reader2["nzp_supp"] != DBNull.Value) perekidka.nzp_supp = Convert.ToInt32(reader2["nzp_supp"]);
                        if (reader2["dat_uchet"] != DBNull.Value) perekidka.dat_uchet = Convert.ToDateTime(reader2["dat_uchet"]).ToShortDateString();
                        if (reader2["dat_month"] != DBNull.Value) perekidka.dat_month = Convert.ToDateTime(reader2["dat_month"]).ToShortDateString();
                        if (reader2["dat_prih"] != DBNull.Value) perekidka.dat_prih = Convert.ToDateTime(reader2["dat_prih"]).ToShortDateString();
                        if (reader2["dat_account"] != DBNull.Value) perekidka.dat_account = Convert.ToDateTime(reader2["dat_account"]).ToShortDateString();
                        if (reader2["sum_prih"] != DBNull.Value) perekidka.sum_prih = Convert.ToDecimal(reader2["sum_prih"]);
                        if (reader2["comment"] != DBNull.Value) perekidka.doc_base.comment = Convert.ToString(reader2["comment"]);
                        if (reader2["service"] != DBNull.Value) perekidka.service = Convert.ToString(reader2["service"]);
                        if (reader2["name_supp"] != DBNull.Value) perekidka.name_supp = Convert.ToString(reader2["name_supp"]);
                        if (reader2["type_rcl"] != DBNull.Value) perekidka.typercl.type_rcl = Convert.ToInt32(reader2["type_rcl"]);
                        if (reader2["typename"] != DBNull.Value) perekidka.typercl.typename = Convert.ToString(reader2["typename"]);
                        if (reader2["nzp_doc_base"] != DBNull.Value) perekidka.doc_base.nzp_doc_base = Convert.ToInt32(reader2["nzp_doc_base"]);
                        if (reader2["nzp_reestr"] != DBNull.Value) perekidka.nzp_reestr = Convert.ToInt32(reader2["nzp_reestr"]);
                        if (reader2["nzp_oper"] != DBNull.Value)
                        {
                            int oper = Convert.ToInt32(reader2["nzp_oper"]);
                            perekidka.nzp_oper = oper;
                            perekidka.reestr = "№" + perekidka.nzp_reestr + ". " + ParamsForGroupPerekidki.GetOperationNameById(oper);
                        }

                        if (reader2["nzp_type_doc"] != DBNull.Value) perekidka.doc_base.nzp_type_doc = Convert.ToInt32(reader2["nzp_type_doc"]);
                        if (reader2["type_doc"] != DBNull.Value) perekidka.doc_base.type_doc = Convert.ToString(reader2["type_doc"]);

                        if (reader2["num_doc"] != DBNull.Value) perekidka.doc_base.num_doc = Convert.ToString(reader2["num_doc"]);
                        if (reader2["dat_doc"] != DBNull.Value) perekidka.doc_base.dat_doc = Convert.ToDateTime(reader2["dat_doc"]).ToShortDateString();


                        perekidka.year_ = f.year_;
                        list.Add(perekidka);
                        if (finder.rows > 0 && i >= finder.skip + finder.rows) break;
                    }
                    reader2.Close();
                }

                reader.Close();
                DelSupplier p = new DelSupplier();
                p.sum_prih = itog_sum;
                list.Insert(0, p);
                ret.tag = totalrecords;
                return list;
            }
            catch (Exception ex)
            {
                reader.Close();
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка получения перекидок оплатами " + err, MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }
        }
        

        public Returns BackComments()
        {
            Returns ret;
            string sql = "";
            MyDataReader reader = null, reader2 = null;

            List<Finder> ListFinder = new List<Finder>();
            foreach (_Point p in Points.PointList)
            {
                for (int y = 13; y <= 14; y++)
                {
                    Finder finder = new Finder();
                    finder.pref = p.pref;
                    finder.nzp_wp = y;
                    finder.database = p.pref + "_charge_" + y;
                    if (!TempTableInWebCashe(finder.database + tableDelimiter + "perekidka")) continue;
                    ListFinder.Add(finder);
                }
            }

            sql = "select count(*) as cnt from " + Points.Pref + "_data.reestr_perekidok where coalesce(nzp_doc_base, 0) = 0";
            object count2 = ExecScalar(sql, out ret);
            int recordsTotalCount = 0;
            try { recordsTotalCount = Convert.ToInt32(count2); }
            catch (Exception e)
            {
                ret = new Returns(false, "Ошибка при определении количества записей: " + (Constants.Debug ? e.Message : ""));
                MonitorLog.WriteLog("Ошибка при определении количества записей " + (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                return ret;
            }

            if (recordsTotalCount > 0)
            {
                sql = " select nzp_reestr from " + Points.Pref + "_data.reestr_perekidok where coalesce(nzp_doc_base, 0) = 0";
                ret = ExecRead(out reader, sql);
                if (!ret.result) return ret;

                while (reader.Read())
                {
                    int nzp_reestr = 0;
                    if (reader["nzp_reestr"] != DBNull.Value) nzp_reestr = Convert.ToInt32(reader["nzp_reestr"]);

                    if (nzp_reestr == 0) continue;

                    sql = "drop table tperekidki";
                    ret = ExecSQL(sql, false);

                    sql = "create temp table tperekidki (nzp_rcl integer, pref varchar(20), year_ integer, nzp_reestr integer, num_doc varchar(20), dat_doc date, nzp_type_doc integer, comment varchar(100), date_rcl date)";
                    ret = ExecSQL(sql, true);
                    if (!ret.result) return ret;

                    foreach (Finder finder in ListFinder)
                    {
                        string schema = finder.database;
                        string perekidka_table = schema + tableDelimiter + "perekidka";

                        sql = " insert into tperekidki (pref, year_, nzp_rcl, nzp_reestr, comment, date_rcl ) " +
                              " select '" + finder.pref + "', " + finder.nzp_wp + ", nzp_rcl, nzp_reestr, comment, date_rcl from " + perekidka_table +
                              " where coalesce(nzp_doc_base,0) = 0 and coalesce(comment, '') <> '' and nzp_reestr = " + nzp_reestr;
                        ret = ExecSQL(sql, true);
                        if (!ret.result) return ret;
                    }

                    sql = " select count(*) from tperekidki";
                    object count = ExecScalar(sql, out ret);
                    int records;
                    try { records = Convert.ToInt32(count); }
                    catch (Exception e)
                    {
                        ret = new Returns(false, "Ошибка при определении количества записей: " + (Constants.Debug ? e.Message : ""));
                        MonitorLog.WriteLog("Ошибка при определении количества записей " + (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                        return ret;
                    }

                    if (records > 0)
                    {
                        BeginTransaction();
                        sql = " insert into " + Points.Pref + "_data.document_base (num_doc,dat_doc,nzp_type_doc,comment) " +
                              " select coalesce(num_doc,'1'),coalesce(dat_doc, date_rcl),coalesce(nzp_type_doc,6),comment from tperekidki limit 1";
                        ret = ExecSQL(sql, true);
                        if (!ret.result)
                        {
                            Rollback();
                            return ret;
                        }

                        sql = "SELECT lastval() as co";
                        ret = ExecRead(out reader2, sql);
                        if (!ret.result)
                        {
                            Rollback();
                            return ret;
                        }
                        int nzp_doc_base = 0;
                        if (reader2.Read()) if (reader2["co"] != DBNull.Value) nzp_doc_base = Convert.ToInt32(reader2["co"]);

                        sql = "update " + Points.Pref + "_data.reestr_perekidok set nzp_doc_base = " + nzp_doc_base + " where nzp_reestr = " + nzp_reestr;
                        ret = ExecSQL(sql, true);
                        if (!ret.result)
                        {
                            Rollback();
                            return ret;
                        }

                        sql = "select distinct pref, year_ from tperekidki";
                        ret = ExecRead(out reader2, sql);
                        if (!ret.result)
                        {
                            Rollback();
                            return ret;
                        }
                        while (reader2.Read())
                        {
                            int year = 0;
                            string pref = "";
                            if (reader2["pref"] != DBNull.Value) pref = Convert.ToString(reader2["pref"]);
                            if (reader2["year_"] != DBNull.Value) year = Convert.ToInt32(reader2["year_"]);

                            string perekidka = pref + "_charge_" + year + ".perekidka";
                            bool exist = false;
                            foreach (Finder finder in ListFinder)
                            {
                                if (finder.database + tableDelimiter + "perekidka" == perekidka)
                                {
                                    exist = true;
                                    break;
                                }
                            }
                            if (!exist) continue;

                            sql = "update " + perekidka + " set nzp_doc_base = " + nzp_doc_base + " where nzp_reestr = " + nzp_reestr;
                            ret = ExecSQL(sql, true);
                            if (!ret.result)
                            {
                                Rollback();
                                return ret;
                            }
                        }
                        Commit();
                        reader2.Close();
                    }
                }
            }

            sql = "drop table tperekidki";
            ret = ExecSQL(sql, false);

            sql = "create temp table tperekidki (nzp_doc_base integer, nzp_rcl integer, pref varchar(20), year_ integer, nzp_reestr integer, num_doc varchar(20), dat_doc date, nzp_type_doc integer, comment varchar(100), date_rcl date)";
            ret = ExecSQL(sql, true);
            if (!ret.result) return ret;

            sql = "ALTER TABLE " + Points.Pref + "_data.document_base DROP COLUMN nzp_rcl, DROP COLUMN bd_kernel, DROP COLUMN year";
            ret = ExecSQL(sql, false);

            sql = "Alter table " + Points.Pref + "_data.document_base add column nzp_rcl int, add column bd_kernel char(20), add column year int";
            ret = ExecSQL(sql, true);
            //if (!ret.result) return ret;

            foreach (Finder finder in ListFinder)
            {
                string schema = finder.database;
                string perekidka = "perekidka";
                string perekidka_table = schema + tableDelimiter + perekidka;

                sql = " insert into tperekidki (pref, year_, nzp_rcl, nzp_reestr, comment, date_rcl ) " +
                        " select '" + finder.pref + "', " + finder.nzp_wp + ", nzp_rcl, nzp_reestr, comment, date_rcl from " + perekidka_table +
                        " WHERE coalesce(comment,'')<>'' and coalesce(nzp_doc_base,0)=0 and coalesce(nzp_reestr,0)=0";
                ret = ExecSQL(sql, true);
                if (!ret.result) return ret;
            }

            BeginTransaction();
            sql = "INSERT INTO " + Points.Pref + "_data.document_base (num_doc, dat_doc, nzp_type_doc,comment,nzp_rcl,bd_kernel,year) " +
                " SELECT coalesce(a.num_doc,'1'), coalesce(a.dat_doc,a.date_rcl ), coalesce(a.nzp_type_doc,6), a.comment, a.nzp_rcl,a.pref,a.year_ " +
                " FROM tperekidki a ";
            ret = ExecSQL(sql, true);
            if (!ret.result)
            {
                Rollback();
                return ret;
            }

            sql = "select distinct pref, year_ from tperekidki";
            ret = ExecRead(out reader2, sql);
            if (!ret.result)
            {
                Rollback();
                return ret;
            }
            while (reader2.Read())
            {
                int year = 0;
                string pref = "";
                if (reader2["pref"] != DBNull.Value) pref = Convert.ToString(reader2["pref"]);
                if (reader2["year_"] != DBNull.Value) year = Convert.ToInt32(reader2["year_"]);

                string perekidka = pref + "_charge_" + year + ".perekidka";
                bool exist = false;
                foreach (Finder finder in ListFinder)
                {
                    if (finder.database + tableDelimiter + "perekidka" == perekidka)
                    {
                        exist = true;
                        break;
                    }
                }
                if (!exist) continue;

                sql = " UPDATE " + perekidka +
                      " SET nzp_doc_base= (select a.nzp_doc_base FROM " + Points.Pref +
                      "_data.document_base a where a.nzp_rcl= " + perekidka + ".nzp_rcl and a.bd_kernel='" + pref + "' and year=" + year + " ) " +
                      " WHERE coalesce(" + perekidka + ".comment,'')<>'' and coalesce(nzp_doc_base,0)=0 and coalesce(nzp_reestr,0)=0";
                ret = ExecSQL(sql, true);
                if (!ret.result)
                {
                    Rollback();
                    return ret;
                }
            }
            Commit();
            reader2.Close();

            sql = "ALTER TABLE " + Points.Pref + "_data.document_base DROP COLUMN nzp_rcl, DROP COLUMN bd_kernel, DROP COLUMN year";
            ret = ExecSQL(sql, true);
            //  if (!ret.result) return ret;

            return ret;
        }

        public Returns PerenosReestrPerekidok()
        {
            string sql = "";
            if (!TempTableInWebCashe(Points.Pref + "_data.reestr_perekidok")) return Utils.InitReturns();

            List<Finder> ListFinder = new List<Finder>();
            foreach (_Point p in Points.PointList)
            {
                for (int y = 13; y <= 14; y++)
                {
                    Finder finder = new Finder();
                    finder.pref = p.pref;
                    finder.nzp_wp = y;
                    finder.database = p.pref + "_charge_" + y;
                    if (!TempTableInWebCashe(finder.database + tableDelimiter + "perekidka")) continue;
                    ListFinder.Add(finder);
                }
            }

            Returns ret = Utils.InitReturns();

            for (int y = 2013; y <= 2014; y++)
            {
                BeginTransaction();
                sql = " insert into " + Points.Pref + "_fin_" + (y % 100).ToString("00") + ".reestr_perekidok " +
                        " (nzp_reestr,dat_uchet,comment,sposob_raspr,nzp_oper,nzp_serv,nzp_supp,nzp_serv_on,nzp_supp_on,saldo_part, " +
                        " sum_oper,is_actual,changed_by,changed_on,created_by,created_on,nzp_doc_base) " +
                        " select nzp_reestr,public.mdy(month_,year_,1),comment,sposob_raspr,nzp_oper,nzp_serv,nzp_supp,nzp_serv_on,nzp_supp_on,saldo_part, " +
                        " sum,is_actual,changed_by,coalesce(changed_on, now()),created_by,created_on,nzp_doc_base " +
                        " from " + Points.Pref + "_data.reestr_perekidok where year_ = " + y;
                ret = ExecSQL(sql, true);
                if (!ret.result)
                {
                    Rollback();
                    return ret;
                }

                sql = "drop table temptype_rcl_nzp_reestr";
                ret = ExecSQL(sql, false);

                sql = "create temp table temptype_rcl_nzp_reestr(type_rcl integer, nzp_reestr integer)";
                ret = ExecSQL(sql, true);
                if (!ret.result)
                {
                    Rollback();
                    return ret;
                }

                foreach (Finder finder in ListFinder)
                {
                    sql = " insert into temptype_rcl_nzp_reestr(type_rcl, nzp_reestr) " +
                          " select distinct type_rcl, nzp_reestr from " + finder.database + ".perekidka where coalesce(nzp_reestr, 0)>0";
                    ret = ExecSQL(sql, true);
                    if (!ret.result)
                    {
                        Rollback();
                        return ret;
                    }

                    sql = "drop table t";
                    ret = ExecSQL(sql, false);

                    sql = "select distinct type_rcl, nzp_reestr into temp t from temptype_rcl_nzp_reestr";
                    if (!ret.result)
                    {
                        Rollback();
                        return ret;
                    }

                    sql = " update " + Points.Pref + "_fin_" + (y % 100).ToString() + ".reestr_perekidok set type_rcl = (select type_rcl from t where nzp_reestr = " +
                          Points.Pref + "_fin_" + (y % 100).ToString() + ".reestr_perekidok.nzp_reestr)";
                    if (!ret.result)
                    {
                        Rollback();
                        return ret;
                    }
                }
                Commit();
            }

            foreach (Finder finder in ListFinder)
            {
                sql = "ALTER TABLE " + finder.database + ".del_supplier DROP CONSTRAINT fk_perekidka_nzp_reestr";
                ret = ExecSQL(sql, false);
            }

            sql = "drop table " + Points.Pref + "_data.reestr_perekidok;";
            ret = ExecSQL(sql, true);

            for (int y = 2013; y <= 2014; y++)
            {
                sql = "SELECT setval('" + Points.Pref + "_fin_" + (y % 100).ToString("00") + ".reestr_perekidok_nzp_reestr_seq', max(nzp_reestr)) FROM " + Points.Pref + "_fin_" + (y % 100).ToString("00") + ".reestr_perekidok";
                ret = ExecSQL(sql, true);
            }

            return ret;
        }

        public Returns AutoPerekidkiBetweenSupp(AutoPerekidka finder)
        {
            if (finder.nzp_serv == 0 ||
                finder.pref == "" ||
                finder.nzp_supp_1 == 0 ||
                finder.nzp_supp_2 == 0) new Returns(false, "Для выполнения автоматической перекидки между договорами должны быть заданы услуга, банк данных и договоры", -1);

            return new Returns();
        }
    }
}
