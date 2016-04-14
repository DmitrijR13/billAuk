using System;
using System.Data;
using System.Linq;
using System.Xml.Linq;
using System.IO;
using System.Text;
using System.Collections.Generic;
using FastReport;
using Newtonsoft.Json;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;


namespace STCLINE.KP50.DataBase
{
    public partial class DbPack : DbPackClient
    {

        public Returns CheckingReturnOnPrevDay()
        {
            Returns ret;

            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;

            string sql = "select count(*) from " + Points.Pref + "_fin_" + (Points.DateOper.Year % 100).ToString("00") + tableDelimiter + "pack_ls pls " +
                " where dat_uchet = '" + Points.DateOper.ToShortDateString() + "'";
            int record_count = -1;
            object count = ExecScalar(conn_db, sql, out ret, true);
            if (ret.result)
            {
                try
                {
                    record_count = Convert.ToInt32(count);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = ex.Message;
                    conn_db.Close();
                    return ret;
                }
            }
            else
            {
                conn_db.Close();
                return new Returns(false);
            }

            if (record_count == 0)
            {
                return new Returns(true);
            }
            else return new Returns(false, "", -1);
        }

        public List<FnSupplier> FindFnSupplier(FnSupplier finder, out Returns ret)
        {
            if (finder.year_ < 1)
            {
                ret = new Returns(false, "Неверные входные параметры: не задан год", -1);
                return null;
            }
            if (finder.nzp_pack_ls <= 0)
            {
                ret = new Returns(false, "Неверные входные параметры: не задан код оплаты", -1);
                return null;
            }

            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            MyDataReader reader = null;
            var list = new List<FnSupplier>();

            var tables = new DbTables(conn_db);
            StringBuilder sqlText;
            
            try
            {
                DateTime datUchet, 
                         distrMonth,
                         dateDistr; //реальная дата и время распределения

                #region try

                Pack_ls packLs;
                ret = GetPackLsData(conn_db, finder, out packLs);
                if (!ret.result) return null;

                if (!DateTime.TryParse(packLs.dat_uchet, out datUchet)) datUchet = DateTime.MinValue;
                if (!DateTime.TryParse(packLs.distr_month, out distrMonth)) distrMonth = DateTime.MinValue;
                if (!DateTime.TryParse(packLs.date_distr, out dateDistr)) dateDistr = DateTime.Now;


                var isDistributed = true; //Признак, что оплата распределена
                if (datUchet <= new DateTime(1900, 1, 1))
                {
                    isDistributed = false;
                    datUchet = Points.DateOper;
                }

                if (packLs.pref == "")
                {
                    ret = new Returns(false, "Префикс БД не задан", -1);
                    return null;
                }

                if (packLs.nzp_kvar < 1 || packLs.num_ls < 1)
                {
                    ret = new Returns(false, "Лицевой счет не найден", -1);
                    return null;
                }

                decimal totalSumCharge = 0,
                        totalRsumTarif = 0,
                        totalSumOutsaldo = 0,
                        totalSumInsaldo = 0,
                        totalSumPrihPrev = 0; 

                DateTime dt; //месяц начислений
                #region определим месяц начислений
                if (finder.onlyCharge == 1 && !isDistributed)
                {
                    if (DateTime.TryParse(finder.dateCharge, out dt));
                }
                else
                {
                    dt = Points.packDistributionParameters.strategy == PackDistributionParameters.Strategies.Samara ? 
                        new DateTime(datUchet.Year, datUchet.Month, 1) : 
                        new DateTime(datUchet.Year, datUchet.Month, 1).AddMonths(-1);
                    if (isDistributed && distrMonth > DateTime.MinValue) dt = distrMonth;
                    if (!isDistributed)
                        if (packLs.pref != "")
                        {
                            RecordMonth r_m = Points.GetCalcMonth(new CalcMonthParams(packLs.pref));
                            dt = new DateTime(r_m.year_, r_m.month_, 1).AddMonths(-1);
                        }
                }
                #endregion

                bool vip = true;
                var tablecharge = packLs.pref + "_charge_" + (dt.Year % 100).ToString("00") + tableDelimiter + "charge_" + dt.Month.ToString("00");
                var curtablecharge = packLs.pref + "_charge_" + (Points.DateOper.Year % 100).ToString("00") + tableDelimiter + "charge_" + Points.DateOper.Month.ToString("00");
                
                if (packLs.kod_sum == Faktura.Kinds.kind81.GetHashCode())
                {
                    tablecharge = packLs.pref + "_charge_" + (dt.Year % 100).ToString("00") + tableDelimiter + "charge_" + dt.Month.ToString("00") + "_t";
                }

                if (!TempTableInWebCashe(conn_db, tablecharge))
                {
                    MonitorLog.WriteLog("Финансы: Квитанция об оплате. Нет таблицы:" + tablecharge, MonitorLog.typelog.Warn, true);
                    vip = false;
                }

                if (vip)
                if (packLs.kod_sum == Faktura.Kinds.MonthlyGSK.GetHashCode() ||
                    packLs.kod_sum == Faktura.Kinds.MonthlyMunicipal.GetHashCode() ||
                    packLs.kod_sum == Faktura.Kinds.FreePayment.GetHashCode() ||
                    packLs.kod_sum == Faktura.Kinds.OplatiUKiPost.GetHashCode() ||
                    packLs.kod_sum == Faktura.Kinds.OplatiDogovor.GetHashCode() ||
                    packLs.kod_sum == Faktura.Kinds.kod40.GetHashCode() ||
                    packLs.kod_sum == Faktura.Kinds.kod35.GetHashCode() ||
                    (packLs.kod_sum == Faktura.Kinds.kind81.GetHashCode() && vip)) //???
                {

                    #region заполнить список начислений по ЛС
                    var dop = new StringBuilder("");
                    var dopTable = new StringBuilder();
                    if (packLs.kod_sum == Faktura.Kinds.OplatiUKiPost.GetHashCode()) //nzp_payer
                    {
                        dop.Remove(0, dop.Length);
                        dop.Append(" and sp.nzp_supp = ch.nzp_supp and sp.nzp_payer_princip = " + packLs.nzp_payer);
                        dopTable.Append(" , "+Points.Pref+DBManager.sKernelAliasRest+"supplier sp");
                    }
                    else if (packLs.kod_sum == Faktura.Kinds.OplatiDogovor.GetHashCode()) //nzp_supp
                    {
                        dop.Remove(0, dop.Length);
                        dop.AppendFormat(" and nzp_supp = {0}", packLs.nzp_supp);
                    }

                    ExecSQL(conn_db, "drop table tcharge ", false);

                    sqlText = new StringBuilder();
                    sqlText.Append("create temp table tcharge (");
                    sqlText.Append(" nzp_serv integer,");
                    sqlText.Append(" nzp_supp integer,");
                    sqlText.Append(" service character(100),");
                    sqlText.Append(" name_supp character(100),");
                    sqlText.Append(" sum_outsaldo numeric(14,2),");
                    sqlText.Append(" sum_insaldo numeric(14,2),");
                    sqlText.Append(" sum_charge numeric(14,2),");
                    sqlText.Append(" rsum_tarif numeric(14,2),");
                    sqlText.Append(" isdel integer,");
                    sqlText.Append(" ordering integer,");
                    sqlText.Append(" sum_money numeric(14,2)");
                    sqlText.Append(")");
                    ret = ExecSQL(conn_db, sqlText.ToString(), true);
                    if (!ret.result)
                    {
                        conn_db.Close();
                        return null;
                    }

                    sqlText = new StringBuilder();
                    sqlText.Append("Insert into tcharge (nzp_serv, nzp_supp, sum_outsaldo, sum_insaldo, sum_charge, rsum_tarif, isdel)");
                    sqlText.Append("Select ");
                    sqlText.Append(" ch.nzp_serv, ch.nzp_supp, sum_outsaldo, sum_insaldo, sum_charge, rsum_tarif, isdel ");
                    sqlText.AppendFormat(" From {0} ch", tablecharge);
                    sqlText.Append(dopTable);
                    sqlText.AppendFormat(" Where ch.nzp_kvar = {0}", packLs.nzp_kvar);
                    sqlText.Append(" and ch.dat_charge is null");
                    sqlText.Append(" and ch.nzp_serv > 1");
                    sqlText.Append(dop);
                    ret = ExecSQL(conn_db, sqlText.ToString(), true);
                    if (!ret.result)
                    {
                        conn_db.Close();
                        return null;
                    }

                    sqlText = new StringBuilder();
                    sqlText.Append("update tcharge tch set sum_money = ");
                    sqlText.AppendFormat("(select sum_money from {0} where nzp_kvar = {1} ", curtablecharge, packLs.nzp_kvar);
                    sqlText.Append(" and nzp_supp = tch.nzp_supp ");
                    sqlText.Append(" and nzp_serv = tch.nzp_serv ");
                    sqlText.Append(" and dat_charge is null");
                    sqlText.Append(" and nzp_serv > 1");
                    sqlText.Append(")");
                    ret = ExecRead(conn_db, out reader, sqlText.ToString(), true);
                    if (!ret.result)
                    {
                        conn_db.Close();
                        return null;
                    }

                    if (!(dt.Month == Points.DateOper.Month & dt.Year == Points.DateOper.Year))
                    {
                        sqlText = new StringBuilder();
                        sqlText.AppendFormat("Select nzp_serv, nzp_supp from {0}", curtablecharge);
                        sqlText.AppendFormat(" where nzp_kvar = {0}", packLs.nzp_kvar);
                        sqlText.Append(" and dat_charge is null");
                        sqlText.Append(" and nzp_serv > 1");
                        ret = ExecRead(conn_db, out reader, sqlText.ToString(), true);
                        if (!ret.result)
                        {
                            conn_db.Close();
                            return null;
                        }
                        while (reader.Read())
                        {
                            int nzpsupp = 0, nzpserv = 0;
                            if (reader["nzp_supp"] != DBNull.Value) nzpsupp = Convert.ToInt32(reader["nzp_supp"]);
                            if (reader["nzp_serv"] != DBNull.Value) nzpserv = Convert.ToInt32(reader["nzp_serv"]);
                         
                            sqlText = new StringBuilder();
                            sqlText.Append("select ch.nzp_serv, ch.nzp_supp from tcharge ch ");
                            sqlText.Append(dopTable);
                            sqlText.Append(" where ");
                            sqlText.AppendFormat(" ch.nzp_serv = {0}", nzpserv);
                            sqlText.AppendFormat(" and ch.nzp_supp = {0}", nzpsupp);
                            sqlText.Append(dop);
                            MyDataReader reader2 = null;
                            ret = ExecRead(conn_db, out reader2, sqlText.ToString(), true);
                            if (!ret.result)
                            {
                                conn_db.Close();
                                return null;
                            }
                            //if (!reader2.Read())
                            //{
                            //    sqlText = new StringBuilder();
                            //    sqlText.AppendFormat("insert into tcharge (nzp_serv, nzp_supp, sum_outsaldo, sum_insaldo, sum_charge, rsum_tarif, isdel, sum_money)");
                            //    sqlText.Append("Select ");
                            //    sqlText.Append(" nzp_serv, ch.nzp_supp, sum_outsaldo, sum_insaldo, sum_charge, rsum_tarif, isdel, sum_money ");
                            //    sqlText.AppendFormat(" From {0} ch", curtablecharge);
                            //    sqlText.Append(dopTable);
                            //    sqlText.AppendFormat(" Where ch.nzp_kvar = {0}", packLs.nzp_kvar);
                            //    sqlText.Append(" and ch.dat_charge is null");
                            //    sqlText.Append(" and ch.nzp_serv > 1");
                            //    sqlText.AppendFormat(" and nzp_serv = {0}", nzpserv);
                            //    sqlText.AppendFormat(" and ch.nzp_supp = {0}", nzpsupp);
                            //    sqlText.Append(dop);
                            //    ret = ExecSQL(conn_db, sqlText.ToString(), true);
                            //    if (!ret.result)
                            //    {
                            //        conn_db.Close();
                            //        return null;
                            //    }
                            //}
                        }
                    }

                    sqlText = new StringBuilder();
                    sqlText.AppendFormat("update tcharge tch set service = (select service from {0} where nzp_serv = tch.nzp_serv), ", tables.services);
                    sqlText.AppendFormat(" name_supp = (select name_supp from {0} where nzp_supp = tch.nzp_supp),", tables.supplier);
                    sqlText.AppendFormat(" ordering = (select ordering from {0} where nzp_serv = tch.nzp_serv) ", tables.services);
                    ret = ExecSQL(conn_db, sqlText.ToString(), true);
                    if (!ret.result)
                    {
                        conn_db.Close();
                        return null;
                    }

                    sqlText = new StringBuilder();
                    sqlText.Append(" select * from tcharge ");
                    sqlText.Append(" Order by ordering, service, name_supp");
                    ret = ExecRead(conn_db, out reader, sqlText.ToString(), true);
                    if (!ret.result)
                    {
                        conn_db.Close();
                        return null;
                    }

                    FnSupplier zap;
                    while (reader.Read())
                    {
                        zap = new FnSupplier();
                        if (reader["service"] != DBNull.Value) zap.service = Convert.ToString(reader["service"]).Trim();
                        if (reader["nzp_serv"] != DBNull.Value) zap.nzp_serv = Convert.ToInt32(reader["nzp_serv"]);
                        if (reader["name_supp"] != DBNull.Value) zap.name_supp = Convert.ToString(reader["name_supp"]).Trim();
                        if (reader["nzp_supp"] != DBNull.Value) zap.nzp_supp = Convert.ToInt32(reader["nzp_supp"]);

                        zap.show_etalon = 1;
                        if (reader["sum_outsaldo"] != DBNull.Value) zap.sum_outsaldo = Convert.ToDecimal(reader["sum_outsaldo"]);
                        if (reader["sum_money"] != DBNull.Value) zap.sum_prih_prev = Convert.ToDecimal(reader["sum_money"]);
                        if (reader["sum_insaldo"] != DBNull.Value) zap.sum_insaldo = Convert.ToDecimal(reader["sum_insaldo"]);
                        if (reader["sum_charge"] != DBNull.Value) zap.sum_charge = Convert.ToDecimal(reader["sum_charge"]);
                        if (reader["rsum_tarif"] != DBNull.Value) zap.rsum_tarif = Convert.ToDecimal(reader["rsum_tarif"]);
                        if (reader["isdel"] != DBNull.Value) zap.is_del = Convert.ToInt32(reader["isdel"]);

                        totalSumCharge += zap.sum_charge;
                        totalSumPrihPrev += zap.sum_prih_prev;
                        totalRsumTarif += zap.rsum_tarif;
                        totalSumOutsaldo += zap.sum_outsaldo;
                        totalSumInsaldo += zap.sum_insaldo;

                        list.Add(zap);
                    }
                    #endregion
                }

                decimal totalSumPrih = 0,
                    totalSUser = 0,
                    totalSDolg = 0,
                    totalSForw = 0;
                
                if (isDistributed)
                {
                    totalSumPrihPrev = 0;
                    string temptbl = "temptblfn";
                    ExecSQL(conn_db, "drop table " + temptbl, false);

                    sqlText = new StringBuilder();
                    sqlText.AppendFormat("create temp table {0} (nzp_pack_ls integer, nzp_serv integer, ", temptbl);
                    sqlText.Append("service char(100), ordering integer, name_supp  char(100), ");
                    sqlText.Append("nzp_supp integer, sum_prih float, sum_prih_prev float, s_user float, s_dolg float, s_forw float) ");
                    sqlText.Append(sUnlogTempTable);
                    ret = ExecSQL(conn_db, sqlText.ToString(), true);
                    if (!ret.result) return null;

                    var sumPrihPrev = new StringBuilder();
                    sumPrihPrev.AppendFormat(" coalesce((select sum(sum_prih) from {0}_charge_{1}{2}fn_supplier{3} fp, ", packLs.pref, (datUchet.Year % 100).ToString("00"),
                        tableDelimiter, datUchet.Month.ToString("00"));
                    sumPrihPrev.AppendFormat(" {0}_fin_{1}{2}pack_ls pls where fp.num_ls = fs.num_ls and coalesce(pls.date_distr,'{3}') < '{4}' ", Points.Pref, (finder.year_ % 100).ToString("00"),
                        tableDelimiter, DateTime.MaxValue.ToShortDateString(), dateDistr);
                    sumPrihPrev.Append(" and fp.nzp_serv = fs.nzp_serv and fp.nzp_supp = fs.nzp_supp and pls.nzp_pack_ls = fp.nzp_pack_ls), 0) + ");
                    sumPrihPrev.AppendFormat(" coalesce((select sum(sum_prih) from {0}_charge_{1}{2}from_supplier fp, ", packLs.pref, (datUchet.Year % 100).ToString("00"),
                        tableDelimiter);
                    sumPrihPrev.AppendFormat(" {0}_fin_{1}{2}pack_ls pls where fp.num_ls = fs.num_ls and coalesce(pls.date_distr,'{3}') < '{4}' ", Points.Pref, (finder.year_ % 100).ToString("00"),
                        tableDelimiter, DateTime.MaxValue.ToShortDateString(), dateDistr);
                    sumPrihPrev.AppendFormat(" and fp.nzp_serv = fs.nzp_serv and fp.nzp_supp = fs.nzp_supp and pls.nzp_pack_ls = fp.nzp_pack_ls and fp.dat_uchet between '{0}' and '{1}'), 0) sum_prih_prev ",
                        new DateTime(datUchet.Year, datUchet.Month, 1).ToShortDateString(), 
                        new DateTime(datUchet.Year, datUchet.Month, DateTime.DaysInMonth(datUchet.Year, datUchet.Month)).ToShortDateString());

                    if (packLs.pack_type == 20)
                    {
                        sqlText = new StringBuilder();
                        sqlText.AppendFormat(" insert into {0} (nzp_pack_ls, nzp_serv, service", temptbl);
                        sqlText.Append(", name_supp, nzp_supp, sum_prih, sum_prih_prev, s_user, s_dolg, s_forw ) ");
                        sqlText.Append(" Select fs.nzp_pack_ls, fs.nzp_serv, s.service, sp.name_supp, ");
                        sqlText.AppendFormat(" sp.nzp_supp, (case when fs.nzp_pack_ls = {0}", finder.nzp_pack_ls);
                        sqlText.Append(" then fs.sum_prih else 0 end) sum_prih, ");
                        sqlText.Append(sumPrihPrev);
                        sqlText.Append(", 0 as s_user, 0 as s_dolg, 0 as s_forw ");
                        sqlText.Append(" From ");
                        sqlText.AppendFormat("{0}_charge_{1}{2}from_supplier fs", packLs.pref, (datUchet.Year%100).ToString("00"), tableDelimiter);
                        sqlText.AppendFormat(", {0}  s", tables.services);
                        sqlText.AppendFormat(", {0}  sp", tables.supplier);
                        sqlText.Append(" Where");
                        sqlText.AppendFormat(" fs.num_ls = {0}", packLs.num_ls);
                        sqlText.AppendFormat(" and fs.dat_uchet <= '{0}'", datUchet.ToShortDateString());
                        sqlText.Append(" and fs.nzp_serv = s.nzp_serv and fs.nzp_supp = sp.nzp_supp");
                        sqlText.AppendFormat(" and fs.nzp_pack_ls = {0}", finder.nzp_pack_ls);
                    }
                    else
                    {
                        sqlText = new StringBuilder();
                        sqlText.AppendFormat(" insert into {0} (nzp_pack_ls, nzp_serv, service", temptbl);
                        sqlText.Append(", name_supp, nzp_supp, sum_prih, sum_prih_prev, s_user, s_dolg, s_forw ) ");
                        sqlText.Append(" Select fs.nzp_pack_ls, fs.nzp_serv, s.service");
                        sqlText.Append(", sp.name_supp, ");
                        sqlText.AppendFormat(" sp.nzp_supp, (case when fs.nzp_pack_ls = {0}", finder.nzp_pack_ls);
                        sqlText.Append(" then fs.sum_prih else 0 end) sum_prih, ");
                        sqlText.Append(sumPrihPrev);
                        sqlText.Append(", fs.s_user as s_user, fs.s_dolg as s_dolg, fs.s_forw as s_forw ");
                        sqlText.Append(" From ");
                        sqlText.AppendFormat("{0}_charge_{1}{2}fn_supplier{3} fs", packLs.pref, (datUchet.Year % 100).ToString("00"), tableDelimiter,
                            datUchet.Month.ToString("00"));
                        sqlText.AppendFormat(", {0}  s", tables.services);
                        sqlText.AppendFormat(", {0}  sp", tables.supplier);
                        sqlText.Append(" Where");
                        sqlText.AppendFormat(" fs.num_ls = {0}", packLs.num_ls);
                        sqlText.AppendFormat(" and fs.dat_uchet <= '{0}'", datUchet.ToShortDateString());
                        sqlText.Append(" and fs.nzp_serv = s.nzp_serv and fs.nzp_supp = sp.nzp_supp");
                        sqlText.AppendFormat(" and fs.nzp_pack_ls = {0}", finder.nzp_pack_ls);
                    }
                    ret = ExecSQL(conn_db, sqlText.ToString(), true);
                    if (!ret.result) return null;

                    sqlText = new StringBuilder();
                    sqlText.Append("select nzp_serv, service, ordering, name_supp, ");
                    sqlText.Append("nzp_supp, sum(sum_prih) sum_prih, sum(sum_prih_prev) sum_prih_prev, sum(s_user) s_user, ");
                    sqlText.AppendFormat("sum(s_dolg) s_dolg, sum(s_forw)  s_forw  from {0} ", temptbl);
                    sqlText.Append(" group by nzp_serv, service, ordering, name_supp, nzp_supp " );
                    sqlText.Append(" Order by ordering, name_supp");

                    ret = ExecRead(conn_db, out reader, sqlText.ToString(), true);
                    if (!ret.result) return null;

                    FnSupplier zap;
                    while (reader.Read())
                    {
                        zap = null;

                        int nzp_serv = reader["nzp_serv"] != DBNull.Value ? Convert.ToInt32(reader["nzp_serv"]) : 0;//, nzp_pack_ls;
                        int nzp_supp = reader["nzp_supp"] != DBNull.Value ? Convert.ToInt32(reader["nzp_supp"]) : 0;//, nzp_pack_ls;

                        foreach (FnSupplier item in list)
                        {
                            if (item.nzp_serv == nzp_serv && item.nzp_supp == nzp_supp)
                            {
                                zap = item;
                                break;
                            }
                        }

                        if (zap == null) // если не найдены или не заполнены начисления по услуге, то создаем новую запись
                        {
                            zap = new FnSupplier();

                            zap.nzp_serv = nzp_serv;
                            if (reader["service"] != DBNull.Value) zap.service = Convert.ToString(reader["service"]).Trim();
                            if (reader["name_supp"] != DBNull.Value) zap.name_supp = Convert.ToString(reader["name_supp"]).Trim();

                            list.Add(zap);
                        }

                        zap.sum_prih = reader["sum_prih"] != DBNull.Value ? Convert.ToDecimal(reader["sum_prih"]) : 0;

                        if (reader["s_user"] != DBNull.Value) zap.s_user = Convert.ToDecimal(reader["s_user"]);
                        if (reader["s_dolg"] != DBNull.Value) zap.s_dolg = Convert.ToDecimal(reader["s_dolg"]);
                        if (reader["s_forw"] != DBNull.Value) zap.s_forw = Convert.ToDecimal(reader["s_forw"]);

                        totalSumPrih += zap.sum_prih;
                        totalSUser += zap.s_user;
                        totalSDolg += zap.s_dolg;
                        totalSForw += zap.s_forw;

                        if (reader["sum_prih_prev"] != DBNull.Value) zap.sum_prih_prev = Convert.ToDecimal(reader["sum_prih_prev"]);
                        totalSumPrihPrev += zap.sum_prih_prev;

                        zap.sum_outsaldo_with_opl = zap.sum_outsaldo - zap.sum_prih_prev;
                        zap.sum_insaldo_with_opl = zap.sum_insaldo - zap.sum_prih_prev;
                        zap.sum_charge_with_opl = zap.sum_charge - zap.sum_prih_prev;
                    }
                }

                decimal total_sum_outsaldo_with_opl = 0,
                        total_sum_insaldo_with_opl = 0,
                        total_sum_charge_with_opl = 0;
                foreach (FnSupplier item in list)
                {
                    total_sum_outsaldo_with_opl += item.sum_outsaldo_with_opl;
                    total_sum_insaldo_with_opl += item.sum_insaldo_with_opl;
                    total_sum_charge_with_opl += item.sum_charge_with_opl;
                }

                // Итого
                list.Add(new FnSupplier()
                {
                    sum_prih = totalSumPrih,
                    s_user = totalSUser,
                    s_dolg = totalSDolg,
                    s_forw = totalSForw,
                    sum_prih_prev = totalSumPrihPrev,
                    sum_charge = totalSumCharge,
                    rsum_tarif = totalRsumTarif,
                    sum_outsaldo = totalSumOutsaldo,
                    sum_insaldo = totalSumInsaldo,
                    sum_outsaldo_with_opl = total_sum_outsaldo_with_opl,
                    sum_insaldo_with_opl = total_sum_insaldo_with_opl,
                    sum_charge_with_opl = total_sum_charge_with_opl
                });

                ret.text = dt.Month + "." + dt.Year;
                #endregion
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка выполнения функции FindFnSupplier:\n" + ex.Message, MonitorLog.typelog.Error, true);
            }
            finally
            {
                if (reader != null) reader.Close();
                conn_db.Close();
            }
            return list;
        }

        private Returns GetPackLsData(IDbConnection conn_db, FnSupplier finder, out Pack_ls packLs)
        {
            packLs = null;
            var tables = new DbTables(conn_db);
            string pack_ls = Points.Pref + "_fin_" + (finder.year_ % 100).ToString("00") + tableDelimiter + "pack_ls";
            string pack = Points.Pref + "_fin_" + (finder.year_ % 100).ToString("00") + tableDelimiter + "pack";
            MyDataReader reader;

            var sqlText = new StringBuilder();
            sqlText.Append("select pls.dat_uchet, k.pref, pls.kod_sum, k.nzp_kvar, k.num_ls, pls.distr_month, p.pack_type, pls.nzp_supp, pls.nzp_payer, pls.date_distr from ");
            sqlText.AppendFormat(" {0} pls, ", pack_ls);
            sqlText.AppendFormat(" {0} p, ", pack);
            sqlText.AppendFormat(" {0} k ", tables.kvar);
            sqlText.AppendFormat(" where pls.nzp_pack_ls = {0} and k.num_ls = pls.num_ls  and pls.nzp_pack = p.nzp_pack", finder.nzp_pack_ls);
            Returns ret = ExecRead(conn_db, out reader, sqlText.ToString(), true);
            if (!ret.result) return ret;

            packLs = new Pack_ls { kod_sum = Faktura.Kinds.None.GetHashCode() };

            if (!reader.Read()) return new Returns(false, "Не найден лицевой счет, связанный с оплатой", -1);

            if (reader["dat_uchet"] != DBNull.Value) packLs.dat_uchet = Convert.ToDateTime(reader["dat_uchet"]).ToShortDateString();
            if (reader["pref"] != DBNull.Value) packLs.pref = Convert.ToString(reader["pref"]).Trim();
            if (reader["kod_sum"] != DBNull.Value) packLs.kod_sum = Faktura.GetKindById(Convert.ToInt16(reader["kod_sum"])).GetHashCode();
            if (reader["nzp_kvar"] != DBNull.Value) packLs.nzp_kvar = Convert.ToInt32(reader["nzp_kvar"]);
            if (reader["nzp_supp"] != DBNull.Value) packLs.nzp_supp = Convert.ToInt32(reader["nzp_supp"]);
            if (reader["nzp_payer"] != DBNull.Value) packLs.nzp_payer = Convert.ToInt32(reader["nzp_payer"]);
            if (reader["num_ls"] != DBNull.Value) packLs.num_ls = Convert.ToInt32(reader["num_ls"]);
            if (reader["pack_type"] != DBNull.Value) packLs.pack_type = Convert.ToInt32(reader["pack_type"]);
            if (reader["distr_month"] != DBNull.Value) packLs.distr_month = Convert.ToDateTime(reader["distr_month"]).ToShortDateString();
            if (reader["date_distr"] != DBNull.Value) packLs.date_distr = Convert.ToDateTime(reader["date_distr"]).ToShortDateString();

            reader.Close();

            return ret;
        }

        /// <summary>
        /// Получить список поставщиков по ЛС (ПУ)
        /// </summary>       
        /// <returns>список поставщиков</returns>
        public List<ContractRequisites> GetSupp(ContractRequisites finder, out Returns ret)
        {
            #region Проверка входных параметров

            ret = Utils.InitReturns();
            if (finder.nzp_kvar <= 0)
            {
                ret.text = "Не задан ЛС";
                return null;
            }

            #endregion

            IDbConnection conn_db = null;
            IDataReader reader = null;
            StringBuilder sql = new StringBuilder();

            List<ContractRequisites> retList = new List<ContractRequisites>();

            try
            {

                #region Открываем соединение с базами

                conn_db = GetConnection(Constants.cons_Kernel);

                ret = OpenDb(conn_db, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return null;
                }

                #endregion

//#if PG
//                sql.Append(" select distinct p.nzp_payer, p.payer  ");
//                sql.Append(" from " + finder.pref + "_data.tarif t, ");
//                //sql.Append(" " + Points.Pref + "_kernel. supplier s, ");
//                sql.Append(" " + Points.Pref + "_kernel.s_payer p ");
//                //sql.Append(" where t.nzp_supp = s.nzp_supp ");
//                sql.Append(" where t.nzp_supp = p.nzp_supp ");
//                sql.Append(" and t. is_actual <> 100 ");
//                sql.Append(" and extract(year from current_date) >= extract(year from t.dat_s) ");
//                sql.Append(" and extract(year from current_date) <= extract(year from t.dat_po) ");
//                sql.Append(" and t.nzp_kvar = " + finder.nzp_kvar + " ");
//                //11.07.2014 убрано так как УК может оказывать услуги не по своему ЖФ                
//                //sql.Append(" and  t.nzp_supp  not in (select nzp_supp from " + Points.Pref + "_data.s_area where nzp_supp is not null) ");

//#else
//sql.Append(" select unique p.nzp_payer, p.payer  ");
//                sql.Append(" from " + finder.pref + "_data:tarif t, ");
//                //sql.Append(" " + Points.Pref + "_kernel: supplier s, ");
//                sql.Append(" " + Points.Pref + "_kernel : s_payer p ");
//                //sql.Append(" where t.nzp_supp = s.nzp_supp ");
//                sql.Append(" where t.nzp_supp = p.nzp_supp ");
//                sql.Append(" and t. is_actual <> 100 ");
//                sql.Append(" and year(today) >= year(t.dat_s) ");
//                sql.Append(" and year(today) <= year(t.dat_po) ");
//                sql.Append(" and t.nzp_kvar = " + finder.nzp_kvar + " ");
////                sql.Append(" and  t.nzp_supp  not in (select nzp_supp from " + Points.Pref + "_data:s_area where nzp_supp is not null) ");
//#endif
                string get_year_from_curdate = "year(today)";
                string get_year_from_dat_s = "year(t.dat_s)";
                string get_year_from_dat_po = "year(t.dat_po)";
#if PG
                get_year_from_curdate = "extract(year from current_date)";
                get_year_from_dat_s = "extract(year from t.dat_s)";
                get_year_from_dat_po = "extract(year from t.dat_po)";
#endif

                sql.Append("select distinct p.nzp_payer, p.payer ");  
                sql.AppendFormat("from {0}_data{1}tarif t, ", finder.pref, tableDelimiter);
                sql.AppendFormat("{0}_kernel{1}supplier supp, ", Points.Pref, tableDelimiter);
                sql.AppendFormat("{0}_kernel{1}s_payer p ", Points.Pref, tableDelimiter);
                sql.Append("where t.nzp_supp = supp.nzp_supp ");
                sql.Append("and t.is_actual <> 100 ");
                sql.Append("and p.nzp_payer = supp.nzp_payer_supp ");
                sql.AppendFormat("and {0} >= {1} ", get_year_from_curdate, get_year_from_dat_s);
                sql.AppendFormat("and {0} <= {1} ", get_year_from_curdate, get_year_from_dat_po);
                sql.AppendFormat("and t.nzp_kvar = {0} ", finder.nzp_kvar);

                if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка получения списка поставщиков " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return null;
                }

                while (reader.Read())
                {
                    ContractRequisites con = new ContractRequisites();

                    //if (reader["nzp_kvar"] != DBNull.Value) con.nzp_kvar = Convert.ToInt32(reader["nzp_kvar"]);
                    if (reader["nzp_payer"] != DBNull.Value) con.nzp_payer = Convert.ToInt32(reader["nzp_payer"]);
                    if (reader["payer"] != DBNull.Value) con.payer = Convert.ToString(reader["payer"]);

                    retList.Add(con);
                }


                return retList;

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetSupp : " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                #region Закрытие соединений

                if (conn_db != null)
                {
                    conn_db.Close();
                }

                if (reader != null)
                {
                    reader.Close();
                }

                sql.Remove(0, sql.Length);

                #endregion
            }
        }


        /// <summary>
        /// Получить список УК по ЛС
        /// </summary>        
        /// <returns>Список УК</returns>
        public List<ContractRequisites> GetAreaLS(ContractRequisites finder, out Returns ret)
        {
            #region Проверка входных параметров

            ret = Utils.InitReturns();
            if (finder.nzp_kvar <= 0)
            {
                ret.text = "Не задан ЛС";
                return null;
            }

            #endregion

            IDbConnection conn_db = null;
            IDataReader reader = null;
            StringBuilder sql = new StringBuilder();

            List<ContractRequisites> retList = new List<ContractRequisites>();

            try
            {

                #region Открываем соединение с базами

                conn_db = GetConnection(Constants.cons_Kernel);

                ret = OpenDb(conn_db, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return null;
                }

                #endregion

//#if PG
//                sql.Append(" select p.nzp_payer, p.payer ");
//                sql.Append(" from " + finder.pref + "_data. kvar kv, ");
//                sql.Append("  " + Points.Pref + "_data. s_area a, ");
//                sql.Append("  " + Points.Pref + "_kernel. s_payer p");
//                //sql.Append("  " + Points.Pref + "_kernel. supplier s ");
//                sql.Append(" where kv.nzp_area = a.nzp_area ");
//                sql.Append(" and a.nzp_supp = p.nzp_supp ");
//                //sql.Append(" and s.nzp_supp = a.nzp_supp ");
//                sql.Append(" and kv.nzp_kvar = " + finder.nzp_kvar + " ");
//#else
//                sql.Append(" select p.nzp_payer, p.payer ");
//                sql.Append(" from " + finder.pref + "_data: kvar kv, ");
//                sql.Append("  " + Points.Pref + "_data: s_area a, ");
//                sql.Append("  " + Points.Pref + "_kernel: s_payer p");
//                //sql.Append("  " + Points.Pref + "_kernel: supplier s ");
//                sql.Append(" where kv.nzp_area = a.nzp_area ");
//                sql.Append(" and a.nzp_supp = p.nzp_supp ");
//                //sql.Append(" and s.nzp_supp = a.nzp_supp ");
//                sql.Append(" and kv.nzp_kvar = " + finder.nzp_kvar + " ");
//#endif

                sql.Append("select p.nzp_payer, p.payer ");
                sql.AppendFormat("from {0}_data{1}kvar kv, ", finder.pref, tableDelimiter);
                sql.AppendFormat("{0}_data{1}s_area a, ", Points.Pref, tableDelimiter);
                sql.AppendFormat("{0}_kernel{1}s_payer p ", Points.Pref, tableDelimiter);
                sql.Append("where kv.nzp_area = a.nzp_area ");
                sql.Append("and a.nzp_payer = p.nzp_payer  ");
                sql.AppendFormat("and kv.nzp_kvar = {0} ", finder.nzp_kvar);

                if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка получения списка УК " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return null;
                }

                while (reader.Read())
                {
                    ContractRequisites con = new ContractRequisites();

                    if (reader["nzp_payer"] != DBNull.Value) con.nzp_payer = Convert.ToInt32(reader["nzp_payer"]);
                    if (reader["payer"] != DBNull.Value) con.payer = Convert.ToString(reader["payer"]);

                    retList.Add(con);
                }


                return retList;

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetAreaLS : " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                #region Закрытие соединений

                if (conn_db != null)
                {
                    conn_db.Close();
                }

                if (reader != null)
                {
                    reader.Close();
                }

                sql.Remove(0, sql.Length);

                #endregion
            }
        }


        /// <summary>
        /// Получить список банковских реквизитов по nzp_payer
        /// </summary>        
        public List<ContractRequisites> GetBanks(ContractRequisites finder, out Returns ret)
        {
            #region Проверка входных параметров

            ret = Utils.InitReturns();
            if (finder.nzp_payer <= 0)
            {
                ret.text = "Не задано юр.лицо";
                return null;
            }

            #endregion

            IDbConnection conn_db = null;
            IDataReader reader = null;
            StringBuilder sql = new StringBuilder();

            List<ContractRequisites> retList = new List<ContractRequisites>();

            try
            {

                #region Открываем соединение с базами

                conn_db = GetConnection(Constants.cons_Kernel);

                ret = OpenDb(conn_db, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return null;
                }

                #endregion

                sql.Append(" select b.nzp_fb, b.bank_name, b.rcount ");
#if PG
                sql.Append(" from " + Points.Pref + "_data.fn_bank b where nzp_payer =" + finder.nzp_payer + " ");
#else
                sql.Append(" from " + Points.Pref + "_data:fn_bank b where nzp_payer =" + finder.nzp_payer + " ");
#endif

                if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка получения списка банковских реквизитов " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return null;
                }

                while (reader.Read())
                {
                    ContractRequisites con = new ContractRequisites();

                    if (reader["nzp_fb"] != DBNull.Value) con.nzp_fb = Convert.ToInt32(reader["nzp_fb"]);
                    if (reader["bank_name"] != DBNull.Value) con.bank_name = Convert.ToString(reader["bank_name"]);
                    if (reader["rcount"] != DBNull.Value) con.rcount = Convert.ToString(reader["rcount"]);

                    retList.Add(con);
                }


                return retList;

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetBanksRequisites : " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                #region Закрытие соединений

                if (conn_db != null)
                {
                    conn_db.Close();
                }

                if (reader != null)
                {
                    reader.Close();
                }

                sql.Remove(0, sql.Length);

                #endregion
            }
        }

        public Returns CancelPlat(Finder finder)
        {
            Returns ret = new Returns();
            ret = CancelPlatXX(10, finder); // Откат оплат РЦ
            ret = CancelPlatXX(20, finder); // Откат оплат УК и ПУ
            return ret;
        }

        /// <summary>
        /// Обработка портфеля. Откат ранее учтённых оплат
        /// </summary>
        /// <param name="dt">Список оплат по которым необходимо выполнить формирование новой пачки</param>
        /// <param name="ret">информация о пользователе</param>
        /// <returns></returns>
        public Returns CancelPlatXX(int pack_type, Finder finder)
        {

            //return new Returns();         
            //if (list == null) list = new List<Pack_ls>();

            string sql_text = "";

            string baseName = "";
            string lnum_pack = "";
            string lnzp_supp = "null";
            string lnzp_payer = "null";
            string lfilename = "";

            //int lnzp_bank = 1999;

            decimal gsum_ls_total = 0;
            float count_kv_total = 0;
            string pref = "";
            DateTime dat_uchet;

            string baseName_pref = "";
            string baseName_1 = "";
            baseName = Points.Pref + "_fin_" + (Points.DateOper.Year % 100).ToString("00");
            baseName_1 = Points.Pref + "_fin_" + (Points.DateOper.Year - 2000 - 1 % 100).ToString("00");

            string tXX_case = "t" + Convert.ToString(finder.nzp_user) + "_case";            
#if PG
            string tXX_case_full = "public." + tXX_case;
#else
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            Returns ret1 = OpenDb(conn_web, true);
            if (!ret1.result) return ret1;
            string tXX_case_full = DBManager.GetFullBaseName(conn_web) + ":" + tXX_case;
            conn_web.Close();
#endif
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            IDataReader reader = null;
            IDataReader reader2 = null;
            Returns ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;

            if (!TempTableInWebCashe(conn_db, tXX_case_full))
            {
                conn_db.Close();
                return new Returns(false, "Данных не найдено", -1);
            }
            StringBuilder sql = new StringBuilder();
            sql.Remove(0, sql.Length);
            sql.Append("select count(*) from " + tXX_case_full + " where mark = 1");
            var obj = ExecScalar(conn_db, sql.ToString(), out ret, true);
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }
            if (Convert.ToInt32(obj) == 0)
            {
                conn_db.Close();
                return new Returns(false, "Нет выбранных оплат", -1);
            }

            string oper_;
            if (pack_type == 10)
            {
                oper_ = " not in ";
            }
            else
            {
                oper_ = " in ";
            }
            
            #region Подготовка к откату 
            int nzp_user = finder.nzp_user;
            Pack pack = new Pack();
            Pack_ls pack_ls = new Pack_ls();

            IDbTransaction tranzaction;
            try
            {
                tranzaction = conn_db.BeginTransaction();
            }
            catch
            {
                tranzaction = null;
            }
            
            // 1. Определить номер пачки для отрицательного реестра
            //try
            {
                sql.Remove(0, sql.Length);
#if PG
                sql.Append("SELECT COUNT(*) as co FROM " + baseName + ".pack  WHERE sum_rasp < 0 and nzp_bank = 1999 AND par_pack = nzp_pack ");
#else
                sql.Append("SELECT COUNT(*) as co FROM " + baseName + ":pack  WHERE sum_rasp < 0 and nzp_bank = 1999 AND par_pack = nzp_pack ");
#endif
                ret = ExecRead(conn_db, tranzaction, out reader, sql.ToString(), true);
                if (!ret.result)
                {

                    MonitorLog.WriteLog("Ошибка определения номера отрицательного реестра " + (Constants.Viewerror ? "\n" +
                        sql.ToString() + "(" + ret.sql_error + ")" : ""),
                        MonitorLog.typelog.Error, 20, 201, true);
                    return ret;
                }
                try
                {
                    if (reader.Read())
                        if (reader["co"] != DBNull.Value)
                            lnum_pack = ((-1) * (System.Convert.ToInt32(reader["co"]) + 1)).ToString();
                }
                finally
                {
                    reader.Close();
                }
                #region Добавление суперпачки для оплат портфела
                lnzp_supp = "null";
                lnzp_payer = "null";
                lfilename = "";

                sql_text = "INSERT INTO  " + baseName + tableDelimiter + "pack (";
                sql_text +=
                  "pack_type," +
                  "nzp_supp, " +
                  "nzp_payer, " +
                  "file_name, " +
                  "nzp_bank, " +
                  "num_pack, " +
                  "dat_uchet," +
                  "dat_pack," +
                  "count_kv," +
                  "sum_pack," +
                  "geton_pack," +
                  "real_sum," +
                  "flag," +
                  "dat_vvod," +
                  "islock," +
                  "operday_payer," +
                  "peni_pack," +
                  "sum_rasp," +
                  "sum_nrasp" +
                  ") " +
                  " VALUES ( " +
                  pack_type + "," +                                   // pack_type
                  Convert.ToString(lnzp_supp) + "," +       // nzp_supp
                  Convert.ToString(lnzp_payer) + "," +       // nzp_payer
                  "'" + Convert.ToString(lfilename) + "'," +  // num_pac
                  "1000," +                                 // nzp_bank
                  "'" + lnum_pack.ToString() + "'," +        // num_pack
                  "'" + Points.DateOper.ToShortDateString() + "'," +     // dat_uchet
                  "'" + Points.DateOper.ToShortDateString() + "'," + // dat_pack
                  "0," +                                    // count_kv
                  "0," +                                    // sum_pack
                  "0," +                                    // geton_pack
                  "0," +                                    // real_count
                  Pack.Statuses.DistributedWithErrors.GetHashCode() + "," +  // flag  распределена с ошибками
                  "'" + Points.DateOper.ToShortDateString() + "'," +  // dat_vvod
                  "NULL, " +                                // islock
                  "'" + Points.DateOper.ToShortDateString() + "'," +         // operday_payer
                  "0," +                                    // peni_pack
                  "0," +                                    // sum_rasp
                  "0" +                                    // sum_nrasp                                
                  ") ";

                sql.Remove(0, sql.Length);
                sql.Append(sql_text);
                ret = ExecRead(conn_db, tranzaction, out reader, sql.ToString(), true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка добавления пачки " + (Constants.Viewerror ? "\n" +
                        sql.ToString() + "(" + ret.sql_error + ")" : ""),
                        MonitorLog.typelog.Error, 20, 201, true);
                    if (tranzaction != null)
                    {
                        tranzaction.Rollback();
                    }
                    return ret;
                }
                sql.Remove(0, sql.Length);

#if PG
                sql.Append("SELECT lastval() as co");
#else
                sql.Append("SELECT first 1 dbinfo('sqlca.sqlerrd1') as co from systables");
#endif
                ret = ExecRead(conn_db, tranzaction, out reader, sql.ToString(), true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка добавления пачки " + (Constants.Viewerror ? "\n" +
                    sql.ToString() + "(" + ret.sql_error + ")" : ""),
                    MonitorLog.typelog.Error, 20, 201, true);
                    if (tranzaction != null)
                    {
                        tranzaction.Rollback();
                    }
                    return ret;
                }
                else
                {
                    if (reader.Read())
                        if (reader["co"] != DBNull.Value)
                            pack.par_pack = System.Convert.ToInt32(reader["co"]);
                    reader.Close();
                }

                sql.Remove(0, sql.Length);
                sql_text = "update " + baseName + tableDelimiter + "pack set par_pack = nzp_pack  where nzp_pack = " + Convert.ToString(pack.par_pack);
                sql.Append(sql_text);

                ret = ExecRead(conn_db, tranzaction, out reader, sql.ToString(), true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка обновления суперпачки для отката " + (Constants.Viewerror ? "\n" +
                    sql.ToString() + "(" + ret.sql_error + ")" : ""),
                    MonitorLog.typelog.Error, 20, 201, true);
                    if (tranzaction != null)
                    {
                        tranzaction.Rollback();
                    }
                    return ret;
                }
                #endregion Добавление суперпачки для оплат портфела
                                
                sql.Remove(0, sql.Length);
              
                sql_text = " select *, " + Points.DateOper.Year+ " as year_ from " + baseName + tableDelimiter + "pack_ls where incase = 1 and dat_uchet is not null and kod_sum " + oper_ + " (50,49) " +//???
                           " and nzp_pack_ls in (select nzp_pack_ls from " + tXX_case_full + " t where mark = 1 and year_ = " + Points.DateOper.Year + ")"+      
                           " union all " +
                           " select *,  " + (Points.DateOper.Year-1).ToString()+ " as year_ from " + baseName_1 + tableDelimiter + "pack_ls where incase = 1 and dat_uchet is not null and kod_sum " + oper_ + " (50,49) "+//???
                           " and nzp_pack_ls in (select nzp_pack_ls from " + tXX_case_full + " t where mark = 1 and year_ = " + (Points.DateOper.Year-1).ToString() + ")";


                sql_text += " order by dat_uchet, nzp_pack_ls";
                sql.Append(sql_text);

                ret = ExecRead(conn_db, tranzaction, out reader, sql.ToString(), true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка получения списка оплат к откату " + (Constants.Viewerror ? "\n" +
                    sql.ToString() + "(" + ret.sql_error + ")" : ""),
                    MonitorLog.typelog.Error, 20, 201, true);
                    if (tranzaction != null)
                    {
                        tranzaction.Rollback();
                    }
                    return ret;
                }
            #endregion Подготовка к откату
                List<PackForCase> list_packs = new List<PackForCase>(); //список обрабатываемых пачек
                while (reader.Read())
                {
                    #region Добавление пачки
                    PackForCase ipack = new PackForCase();

                    if (reader["year_"] != DBNull.Value) ipack.year_ = Convert.ToInt32(reader["year_"]);
                    sql.Remove(0, sql.Length);
                    if (ipack.year_ == Points.DateOper.Year)
                    {
                        sql_text = " select nzp_pack, nzp_bank, num_pack, nzp_supp, nzp_payer, file_name  " +
                                   " from " + baseName + tableDelimiter + "pack where nzp_pack =  " +
                                   Convert.ToString(reader["nzp_pack"]);
                    }
                    else
                    {
                        sql_text = " select nzp_pack, nzp_bank, num_pack, nzp_supp, nzp_payer, file_name  " +
                               " from " + baseName_1 + tableDelimiter + "pack where nzp_pack =  " + Convert.ToString(reader["nzp_pack"]);
                            
                    }

                    sql.Append(sql_text);
                    ret = ExecRead(conn_db, tranzaction, out reader2, sql.ToString(), true);
                    if (reader2.Read())
                    {
                       
                        if (reader2["nzp_pack"] != DBNull.Value) ipack.nzp_pack = Convert.ToInt32(reader2["nzp_pack"]);

                        ipack.nzp_bank = Convert.ToInt32(reader2["nzp_bank"]);
                        if (ipack.nzp_bank == 0) ipack.nzp_bank = 1999;
                        ipack.num_pack = Convert.ToString(reader2["num_pack"]);

                        if (reader2["nzp_supp"] != DBNull.Value) ipack.nzp_supp = Convert.ToString(reader2["nzp_supp"]);
                        if (ipack.nzp_supp.Trim() == "") ipack.nzp_supp = "null";
                        if (reader2["nzp_payer"] != DBNull.Value) ipack.nzp_payer = Convert.ToString(reader2["nzp_payer"]);
                        if (ipack.nzp_payer.Trim() == "") ipack.nzp_payer = "null";
                        ipack.file_name = Convert.ToString(reader2["file_name"]);
                        ipack.par_pack = pack.par_pack;
                    }
                    else
                    {
                        continue;
                    }
                    bool contains = false;
                    if (list_packs != null)
                    {
                        foreach(PackForCase it in list_packs)
                        {
                            if (it.nzp_pack == ipack.nzp_pack && it.year_ == ipack.year_)
                            {
                                contains = true;
                                break;
                            }
                        }
                            
                    }

                    if (!contains)
                    {
                        list_packs.Add(ipack);
                        sql_text = "INSERT INTO " + baseName + tableDelimiter + "pack (" +
                             "par_pack, " +
                             "pack_type," +
                             "nzp_supp, " +
                             "nzp_payer, " +
                             "file_name, " +
                             "nzp_bank, " +
                             "num_pack, " +
                             "dat_uchet," +
                             "dat_pack," +
                             "count_kv," +
                             "sum_pack," +
                             "geton_pack," +
                             "real_sum," +
                             "flag," +
                             "dat_vvod," +
                             "islock," +
                             "operday_payer," +
                             "peni_pack," +
                             "sum_rasp," +
                             "sum_nrasp" +
                             ") " +
                             " VALUES ( " +
                             Convert.ToString(ipack.par_pack) + "," +   // par_pack
                             "" + pack_type + "," +                                   // pack_type
                             Convert.ToString(ipack.nzp_supp) + "," +       // nzp_supp
                             Convert.ToString(ipack.nzp_payer) + "," +       // nzp_payer
                             "'" + Convert.ToString(ipack.file_name) + "'," +  // num_pac
                             Convert.ToString(ipack.nzp_bank) + "," +       // nzp_bank
                             "'" + Convert.ToString(ipack.num_pack) + "'," +  // num_pack
                             "'" + Points.DateOper.ToShortDateString() + "'," +  // dat_uchet
                             "'" + Points.DateOper.ToShortDateString() + "'," +  // dat_pack
                             "1," +                                    // count_kv
                             Convert.ToString((-1) * Convert.ToDecimal(reader["g_sum_ls"])) + "," +  // sum_pack
                             "0," +                                    // geton_pack
                             "0," +                                    // real_count
                             Pack.Statuses.Distributed.GetHashCode() + "," + // flag  // Распределена
                             "'" + Convert.ToDateTime(reader["dat_vvod"]).ToShortDateString() + "'," +  // dat_vvod
                             "NULL, " +                                // islock
                             "'" + Points.DateOper.ToShortDateString() + "'," + // operday_payer
                             "0," +                                    // peni_pack
                             "0," +                                    // sum_rasp
                             "0 " +                                   // sum_nrasp                         
                             ") ";

                        sql.Remove(0, sql.Length);
                        sql.Append(sql_text);
                        ret = ExecRead(conn_db, tranzaction, out reader2, sql.ToString(), true);
                        if (!ret.result)
                        {
                            MonitorLog.WriteLog("Ошибка добавления пачки с отрицательной оплатой" + (Constants.Viewerror ? "\n" +
                            sql.ToString() + "(" + ret.sql_error + ")" : ""),
                            MonitorLog.typelog.Error, 20, 201, true);
                            if (tranzaction != null)
                            {
                                tranzaction.Rollback();
                            }
                            return ret;
                        }

                        sql.Remove(0, sql.Length);

#if PG
                        sql.Append("SELECT lastval() as co");
#else
                        sql.Append("SELECT first 1 dbinfo('sqlca.sqlerrd1') as co from systables");
#endif
                        ret = ExecRead(conn_db, tranzaction, out reader2, sql.ToString(), true);

                        if (reader2.Read())
                            if (reader2["co"] != DBNull.Value) ipack.new_nzp_pack = System.Convert.ToInt32(reader2["co"]);
                        if (!ret.result)
                        {
                            MonitorLog.WriteLog("Ошибка добавления пачки с отрицательной оплатой" + (Constants.Viewerror ? "\n" +
                            sql.ToString() + "(" + ret.sql_error + ")" : ""),
                            MonitorLog.typelog.Error, 20, 201, true);
                            if (tranzaction != null)
                            {
                                tranzaction.Rollback();
                            }
                            return ret;
                        }
                        reader2.Close();
                    }
                    #endregion Добавление пачки
                    #region Если добавление в созданную пачку
                    else
                    {
                        foreach (var packForCase in list_packs)
                        {
                            if (packForCase.year_ == ipack.year_ && packForCase.nzp_pack == ipack.nzp_pack)
                            {
                                ipack.new_nzp_pack = packForCase.new_nzp_pack;
                                break;
                            }
                        }
                    }
                    #endregion


                    #region Добавление оплаты с отрицательной суммой
                    // Добавить отрицательную оплату
                    sql_text = "INSERT INTO " + baseName + tableDelimiter + "pack_ls ";
                    sql_text += "(" +
                                "nzp_pack,   " +
                                "num_ls,     " +
                                "prefix_ls,  " +
                                "g_sum_ls,   " +
                                "sum_ls,     " +
                                "geton_ls,   " +
                                "sum_peni,   " +
                                "dat_month,  " +
                                "kod_sum,    " +
                                "nzp_supp,   " +
                                "nzp_payer,   " +
                                "id_bill,    " +
                                "dat_vvod,   " +
                                "dat_uchet,   " +
                                "anketa,     " +
                               
                                "info_num,   " +
                                "inbasket,   " +
                                "alg,        " +
                        //                                "date_distr, " +
                                "erc_code," +
                        //                              "nzp_rs," +
                                "nzp_user ,   " +
                                 "distr_month    " +
                                ") " +
                                "VALUES (" +
                                Convert.ToString(ipack.new_nzp_pack) + ",";

                    if (reader["num_ls"] != DBNull.Value)
                    {
                        sql_text = sql_text +
                                Convert.ToString(reader["num_ls"]) + ",";
                    }
                    else
                        sql_text = sql_text + "NULL,";
                    if (reader["prefix_ls"] != DBNull.Value)
                    {
                        sql_text = sql_text +
                                Convert.ToString(reader["prefix_ls"]) + ",";
                    }
                    else
                        sql_text = sql_text + "NULL,";

                    if (reader["g_sum_ls"] != DBNull.Value)
                    {
                        sql_text = sql_text +
                                Convert.ToString((-1) * Convert.ToDecimal(reader["g_sum_ls"])) + ",";
                    }
                    else
                        sql_text = sql_text + "0,";

                    if (reader["sum_ls"] != DBNull.Value)
                    {
                        sql_text = sql_text +
                                Convert.ToString(reader["sum_ls"]) + ",";
                    }
                    else
                        sql_text = sql_text + "0,";


                    if (reader["geton_ls"] != DBNull.Value)
                    {
                        sql_text = sql_text +
                                Convert.ToString((-1) * Convert.ToDecimal(reader["geton_ls"])) + ",";
                    }
                    else
                        sql_text = sql_text + "0,";

                    if (reader["sum_peni"] != DBNull.Value)
                    {
                        sql_text = sql_text +
                                Convert.ToString((-1) * Convert.ToDecimal(reader["sum_peni"])) + ",";
                    }
                    else
                        sql_text = sql_text + "0,";

                    if (reader["dat_month"] != DBNull.Value)
                    {
                        sql_text = sql_text +
                                "'" + Convert.ToDateTime(reader["dat_month"]).ToShortDateString() + "',";
                    }
                    else
                        sql_text = sql_text + "null,";



                    if (reader["kod_sum"] != DBNull.Value)
                    {
                        sql_text = sql_text +
                                Convert.ToString(reader["kod_sum"]) + ",";
                    }
                    else
                        sql_text = sql_text + "NULL,";

                    if (reader["nzp_supp"] != DBNull.Value)
                    {
                        sql_text = sql_text +
                                Convert.ToString(reader["nzp_supp"]) + ",";
                    }
                    else
                        sql_text = sql_text + "NULL,";

                    if (reader["nzp_payer"] != DBNull.Value)
                    {
                        sql_text = sql_text +
                                Convert.ToString(reader["nzp_payer"]) + ",";
                    }
                    else
                        sql_text = sql_text + "NULL,";

                    if (reader["id_bill"] != DBNull.Value)
                    {
                        sql_text = sql_text +
                                Convert.ToString(reader["id_bill"]) + ",";
                    }
                    else
                        sql_text = sql_text + "NULL,";

                    if (reader["dat_vvod"] != DBNull.Value)
                    {
                        sql_text = sql_text +
                                "'" + Convert.ToDateTime(reader["dat_vvod"]).ToShortDateString() + "',";
                    }
                    else
                        sql_text = sql_text + "NULL,";

                    sql_text = sql_text + "'" + Points.DateOper.ToShortDateString() + "',";  // dat_uchet

                    if (reader["anketa"] != DBNull.Value)
                    {
                        sql_text = sql_text + "'" +
                                Convert.ToString(reader["anketa"]) + "',";
                    }
                    else
                        sql_text = sql_text + "0,";

                    if (reader["info_num"] != DBNull.Value)
                    {
                        sql_text = sql_text + "'" +
                                Convert.ToString(reader["info_num"]) + "',";
                    }
                    else
                        sql_text = sql_text + "NULL,";

                    // inbasket
                    sql_text = sql_text + "0,";

                    if (reader["alg"] != DBNull.Value)
                    {
                        sql_text = sql_text + "'" +
                                Convert.ToString(reader["alg"]) + "',";
                    }
                    else
                        sql_text = sql_text + "0,";

                    if (reader["erc_code"] != DBNull.Value)
                    {
                        if (Convert.ToString(reader["erc_code"]).Trim() != "")
                        {
                            sql_text = sql_text +
                                    Convert.ToString(reader["erc_code"]) + ",";
                        }
                        else
                        {
                            sql_text = sql_text +
                                     "NULL,";
                        }
                    }
                    else
                        sql_text = sql_text + "NULL,";

                    sql_text = sql_text + Convert.ToString(nzp_user) + ",";
                    if (reader["distr_month"] != DBNull.Value)
                    {
                        sql_text += "'" + Convert.ToDateTime(reader["distr_month"]).ToShortDateString() + "'";
                    }
                    else sql_text += "NULL ";
                    sql_text += ") ";
                    reader2.Close();
                    sql.Remove(0, sql.Length);
                    sql.Append(sql_text);
                    ret = ExecRead(conn_db, tranzaction, out reader2, sql.ToString(), true);
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка добавления пачки с отрицательной оплатой" + (Constants.Viewerror ? "\n" +
                        sql.ToString() + "(" + ret.sql_error + ")" : ""),
                        MonitorLog.typelog.Error, 20, 201, true);
                        if (tranzaction != null)
                        {
                            tranzaction.Rollback();
                        }
                        return ret;
                    }

                    reader2.Close();

                    sql.Remove(0, sql.Length);

#if PG
                    sql.Append("SELECT lastval() as co");
#else
                    sql.Append("SELECT first 1 dbinfo('sqlca.sqlerrd1') as co from systables");
#endif
                    ret = ExecRead(conn_db, tranzaction, out reader2, sql.ToString(), true);


                    if (reader2.Read())
                        if (reader2["co"] != DBNull.Value)
                            pack_ls.nzp_pack_ls = System.Convert.ToInt32(reader2["co"]);


                    gsum_ls_total = gsum_ls_total + Convert.ToDecimal(reader["g_sum_ls"]);
                    count_kv_total = count_kv_total + 1;

                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка добавления пачки с отрицательной оплатой" + (Constants.Viewerror ? "\n" +
                        sql.ToString() + "(" + ret.sql_error + ")" : ""),
                        MonitorLog.typelog.Error, 20, 201, true);
                        if (tranzaction != null)
                        {
                            tranzaction.Rollback();
                        }
                        return ret;
                    }

                    #endregion Добавление оплаты с отрицательной суммой

                    #region Определение префикса для определения банка
                    sql_text = "select pref from " + Points.Pref + "_data" + tableDelimiter + "kvar  " +
                                            " where num_ls = " + Convert.ToString(reader["num_ls"]);

                    ret = ExecRead(conn_db, tranzaction, out reader2, sql_text, true);
                    if (!ret.result)
                    {
                        reader.Close();
                        reader2.Close();
                        ret = new Returns(false, "Не найдена оплата");
                        if (tranzaction != null)
                        {
                            tranzaction.Rollback();
                        }
                        return ret;
                    }



                    dat_uchet = Points.DateOper;
                    if (reader2.Read())
                    {
                        if (reader["dat_uchet"] != DBNull.Value) dat_uchet = Convert.ToDateTime(reader["dat_uchet"]);
                        if (reader2["pref"] != DBNull.Value) pref = Convert.ToString(reader2["pref"]).Trim();
                    }
                    else
                    {
                        reader.Close();
                        reader2.Close();
                        ret = new Returns(false, "Не найдена оплата");
                        if (tranzaction != null)
                        {
                            tranzaction.Rollback();
                        }
                        return ret;
                    }

                    reader2.Close();

                    if (pref == "")
                    {
                        ret = new Returns(false, "Префикс БД не задан");
                        if (tranzaction != null)
                        {
                            tranzaction.Rollback();
                        }
                        return ret;
                    }
                    string fn_supplier_pref;
                    string fn_supplier_new;
                    if (pack_type == 10)
                    {
                        fn_supplier_pref = pref + "_charge_" + (dat_uchet.Year % 100).ToString("00") + tableDelimiter + "fn_supplier" + dat_uchet.Month.ToString("00");
                        fn_supplier_new = pref + "_charge_" + (Points.DateOper.Year % 100).ToString("00") + tableDelimiter + "fn_supplier" + Points.DateOper.Month.ToString("00");
                    }
                    else
                    {
                        fn_supplier_pref = pref + "_charge_" + (dat_uchet.Year % 100).ToString("00") + tableDelimiter + "from_supplier";
                        fn_supplier_new = pref + "_charge_" + (Points.DateOper.Year % 100).ToString("00") + tableDelimiter + "from_supplier";
                    }
                    #endregion Определение префикса для определения банка
                    var rM = Points.GetCalcMonth(new CalcMonthParams(pref));
                    #region Перенос распределений
                    if (pack_type == 10)
                    {
                        sql_text = " INSERT INTO " + fn_supplier_new +
                                          " ( " +
                                          "nzp_serv ," +
                                          "nzp_supp ," +
                                          "nzp_pack_ls," +
                                          "nzp_charge ," +
                                          "num_charge ," +
                                          "num_ls ," +
                                          "sum_prih ," +
                                          "kod_sum ," +
                                          "dat_month ," +
                                          "dat_prih ," +
                                          "dat_uchet ," +
                                          "calc_month, " +
                                          "dat_plat ," +
                                          "s_user ," +
                                          "s_dolg ," +
                                          "s_forw ) " +
                                          " select " +
                                          "nzp_serv ," +
                                          "nzp_supp ," +
                                          Convert.ToString(pack_ls.nzp_pack_ls) + "," +
                                          "nzp_charge ," +
                                          "num_charge ," +
                                          "num_ls ," +
                                          "(-1)*sum_prih ," +
                                          "kod_sum ," +
                                          "dat_month ," +
                                          "dat_prih ," +
                                          "'" + Points.DateOper.ToShortDateString() + "'," +  // dat_uchet
                                          "'" + new DateTime(rM.year_, rM.month_, 1).ToShortDateString() + "', " +
                                          "dat_plat, " +
                                          "(-1)*s_user ," +
                                          "(-1)*s_dolg ," +
                                          "(-1)*s_forw  " +
                                          " from  " + fn_supplier_pref +
                                          " where  nzp_pack_ls =  " + Convert.ToString(reader["nzp_pack_ls"]);
                    }
                    else
                    {
                        sql_text = " INSERT INTO " + fn_supplier_new +
                                                              " ( " +
                                                              "nzp_serv ," +
                                                              "nzp_supp ," +
                                                              "nzp_pack_ls," +
                                                              "nzp_charge ," +
                                                              "num_charge ," +
                                                              "num_ls ," +
                                                              "sum_prih ," +
                                                              "kod_sum ," +
                                                              "dat_month ," +
                                                              "dat_prih ," +
                                                              "dat_uchet ," +
                                                              "calc_month, "+
                                                              "dat_plat ) " +
                                                              " select " +
                                                              "nzp_serv ," +
                                                              "nzp_supp ," +
                                                              Convert.ToString(pack_ls.nzp_pack_ls) + "," +
                                                              "nzp_charge ," +
                                                              "num_charge ," +
                                                              "num_ls ," +
                                                              "(-1)*sum_prih ," +
                                                              "kod_sum ," +
                                                              "dat_month ," +
                                                              "dat_prih ," +
                                                              "'" + Points.DateOper.ToShortDateString() + "'," +  // dat_uchet
                                                              "'" + new DateTime(rM.year_, rM.month_, 1).ToShortDateString() + "', " +
                                                              "dat_plat " +
                                                              " from  " + fn_supplier_pref +
                                                              " where  nzp_pack_ls =  " + Convert.ToString(reader["nzp_pack_ls"]);

                    }
                    ret = ExecRead(conn_db, tranzaction, out reader2, sql_text, true);
                    if (!ret.result)
                    {
                        if (tranzaction != null)
                        {
                            tranzaction.Rollback();
                        }
                        return ret;
                    }

                    sql_text = "update " + baseName + tableDelimiter + "pack_ls set calc_month = '" + new DateTime(rM.year_, rM.month_, 1).ToShortDateString() + "' "+
                        " where nzp_pack_ls = " + pack_ls.nzp_pack_ls;
                    ret = ExecRead(conn_db, tranzaction, out reader2, sql_text, true);
                    if (!ret.result)
                    {
                        if (tranzaction != null)
                        {
                            tranzaction.Rollback();
                        }
                        return ret;
                    }

                    sql_text = "update " + baseName + tableDelimiter + "pack_ls set incase = 0 where nzp_pack_ls = " + Convert.ToString(reader["nzp_pack_ls"] + " and incase = 1");
                    ret = ExecRead(conn_db, tranzaction, out reader2, sql_text, true);
                    if (!ret.result)
                    {
                        if (tranzaction != null)
                        {
                            tranzaction.Rollback();
                        }
                        return ret;
                    }
                    sql_text = "update " + baseName_1 + tableDelimiter + "pack_ls set incase = 0 where nzp_pack_ls = " + Convert.ToString(reader["nzp_pack_ls"] + " and incase = 1");
                    ret = ExecRead(conn_db, tranzaction, out reader2, sql_text, true);
                    if (!ret.result)
                    {
                        if (tranzaction != null)
                        {
                            tranzaction.Rollback();
                        }
                        return ret;
                    }

                    #endregion Перенос распределений с отрицательным знаком

                    bool addpositiveoplat = false;
                    if (finder.dopFind != null && finder.dopFind.Count > 0 && finder.dopFind[0] == "add")
                    {
                        addpositiveoplat = true;
                    }
                    //else
                    //{
                    //    foreach (Pack_ls pls in list)
                    //    {
                    //        if (pls.nzp_pack_ls == Convert.ToInt32(reader["nzp_pack_ls"]) &&
                    //            pls.nzp_pack == Convert.ToInt32(reader["nzp_pack"]) &&
                    //            pls.num_ls == Convert.ToInt32(reader["num_ls"]) &&
                    //            (pls.year_ == Points.DateOper.Year || pls.year_ == Points.DateOper.Year - 1))
                    //            addpositiveoplat = true;

                    //        if (pack_type == 10 && (pls.kod_sum == 50 || pls.kod_sum == 49))//???
                    //        {
                    //            addpositiveoplat = false;
                    //        }
                    //        if (pack_type == 20 && pls.kod_sum != 50 && pls.kod_sum != 49)//???
                    //        {
                    //            addpositiveoplat = false;
                    //        }

                    //    }
                    //}
                    int status = Pack.Statuses.Distributed.GetHashCode();
                    if (addpositiveoplat)
                    {
                        status = Pack.Statuses.DistributedWithErrors.GetHashCode();
                        #region Добавление положительной оплаты
                        // Добавить положительную оплату
                        baseName_pref = Points.Pref + "_fin_" + (Convert.ToDateTime(reader["dat_uchet"]).Year % 100).ToString("00");
                        // Добавить положительную оплату
                        sql_text = "INSERT INTO " + baseName + tableDelimiter + "pack_ls ";
                        sql_text += "(" +
                                    "nzp_pack,   " +
                                    "num_ls,     " +
                                    "prefix_ls,  " +
                                    "g_sum_ls,   " +
                                    "sum_ls,     " +
                                    "geton_ls,   " +
                                    "sum_peni,   " +
                                    "dat_month,  " +
                                    "kod_sum,    " +
                                    "nzp_supp,   " +
                                    "nzp_payer,   " +
                                    "id_bill,    " +
                                    "dat_vvod,   " +
                                    "anketa,     " +
                                    "info_num,   " +
                                    "inbasket,   " +
                                    "alg,        " +
                                    "date_distr, " +
                                    "erc_code," +
                                    "nzp_user    " +
                                    ") " +
                                    " select " +
                                    Convert.ToString(ipack.new_nzp_pack) + "," +
                                    "num_ls,     " +
                                    "prefix_ls,  " +
                                    "g_sum_ls,   " +
                                    "sum_ls,     " +
                                    "geton_ls,   " +
                                    "sum_peni,   " +
                                    "dat_month,  " +
                                    "kod_sum,    " +
                                    "nzp_supp,   " +
                                    "nzp_payer,   " +
                                    "id_bill,    " +
                                    "dat_vvod,   " +
                                    "anketa,     " +
                                    "info_num,   " +
                                    "inbasket,   " +
                                    "0,        " +
                                    "date_distr, " +
                                    "erc_code," +
                                    Convert.ToString(nzp_user) +
                                    " from ";
                        sql_text +=
                                    baseName_pref + tableDelimiter + "pack_ls " +
                                    " where nzp_pack_ls = " + Convert.ToString(reader["nzp_pack_ls"]);

                        reader2.Close();
                        sql.Remove(0, sql.Length);
                        sql.Append(sql_text);
                        ret = ExecRead(conn_db, tranzaction, out reader2, sql.ToString(), true);
                        if (!ret.result)
                        {
                            MonitorLog.WriteLog("Ошибка добавления пачки с положительной оплатой" + (Constants.Viewerror ? "\n" +
                            sql.ToString() + "(" + ret.sql_error + ")" : ""),
                            MonitorLog.typelog.Error, 20, 201, true);
                            if (tranzaction != null)
                            {
                                tranzaction.Rollback();
                            }
                            return ret;
                        }
                        gsum_ls_total = gsum_ls_total - Convert.ToDecimal(reader["g_sum_ls"]);
                        count_kv_total = count_kv_total + 1;
                        #endregion Добавление положительной оплаты
                    }

                    #region обновление информации по пачке
                    sql_text = "update " + baseName + tableDelimiter + "pack set sum_pack = (select sum(g_sum_ls) from " +
                        baseName + tableDelimiter + "pack_ls where nzp_pack = " + Convert.ToString(ipack.new_nzp_pack) + "), " +
                        " count_kv = (select count(*) from " + baseName + tableDelimiter + "pack_ls where nzp_pack = " + Convert.ToString(ipack.new_nzp_pack) + ") " +
                        " where nzp_pack = " + Convert.ToString(ipack.new_nzp_pack);
                    ret = ExecRead(conn_db, tranzaction, out reader2, sql_text, true);
                    if (!ret.result)
                    {
                        if (tranzaction != null)
                        {
                            tranzaction.Rollback();
                        }
                        return ret;
                    }
                    sql_text = "update " + baseName + tableDelimiter + "pack set flag = " + status + ", sum_nrasp = (select sum(g_sum_ls) from " +
                        baseName + tableDelimiter + "pack_ls where nzp_pack = " + Convert.ToString(ipack.new_nzp_pack) + " and dat_uchet is null ) where nzp_pack = " + Convert.ToString(ipack.new_nzp_pack);
                    ret = ExecRead(conn_db, tranzaction, out reader2, sql_text, true);
                    if (!ret.result)
                    {
                        if (tranzaction != null)
                        {
                            tranzaction.Rollback();
                        }
                        return ret;
                    }
                    #endregion обновление информации по пачке

                }
            }

            #region Обновление сводной информации по общей сумме оплаты и количеству квитанций по суперпачке
            /*     sql_text = "update " + baseName + tableDelimiter + "pack set sum_pack = (-1)*" + Convert.ToString(gsum_ls_total) + ", count_kv =" + Convert.ToString(count_kv_total) + " where nzp_pack = " + Convert.ToString(pack.par_pack);
            ret = ExecRead(conn_db, tranzaction, out reader2, sql_text, true);
            if (!ret.result)
            {
                if (tranzaction != null)
                {
                    tranzaction.Rollback();
                }
                return ret;
            }*/
            #endregion Обновление сводной информации по общей сумме оплаты и количеству квитанций по суперпачке

            if (tranzaction != null)
            {
                tranzaction.Commit();
            }


            #region Обновление сводной информации по общей сумме оплаты и количеству квитанций по суперпачке
            PackFinder fndr = new PackFinder();
            fndr.nzp_user = nzp_user;
            fndr.year_ = Points.DateOper.Year;
            fndr.nzp_pack = pack.par_pack;

            ReturnsType ret2 = Upd_SUM_RASP_and_SUM_NRASP(fndr);
            #endregion Обновление сводной информации по общей сумме оплаты и количеству квитанций по суперпачке

            #region постановка задания в очередь для учета оплат суперпачки
            UchetOplatForListNzpPacks(finder, pack.par_pack, conn_db);
            #endregion постановка задания в очередь для учета оплат суперпачки
            /*
                catch (Exception ex)
                {
                    MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                    if (tranzaction != null) tranzaction.Rollback();
                    throw new Exception(ex.Message, ex);
                }
                finally
                {
                    conn_db.Close();
                }

                return ret;
            }
             */
            return ret;
        }

        public Returns UchetOplatForListNzpPacks(Finder finder, int par_pack, IDbConnection conn_db)
        {
            StringBuilder sql = new StringBuilder();
            sql.AppendFormat(
                " select distinct pref from {0}_fin_{1}{2}pack_ls pls, {0}_fin_{1}{2}pack p, {0}_data{2}kvar k " +
                " where par_pack = {3} and p.nzp_pack <> par_pack and p.nzp_pack = pls.nzp_pack and k.num_ls=pls.num_ls",
                Points.Pref, (Points.DateOper.Year%100).ToString("00"), tableDelimiter, par_pack);
            MyDataReader reader;
            Returns ret = ExecRead(conn_db, out reader, sql.ToString(), true);
            if (!ret.result) return ret;
           
            while (reader.Read())
            {
                var calcfon = new CalcFonTask(Points.GetCalcNum(0));
                calcfon.TaskType = CalcFonTask.Types.uchetOplatBank;
                calcfon.Status = FonTask.Statuses.New; //на выполнение                
                calcfon.nzp_user = finder.nzp_user;
                string pref = "";
                if (reader["pref"] != DBNull.Value) pref = Convert.ToString(reader["pref"]);
                calcfon.txt = "Процедура учета оплат по оплатам суперпачки (" + Points.GetPoint(pref.Trim()).point + ") ";

                var dbCalc = new DbCalcQueueClient();
                calcfon.nzpt = Points.GetPoint(pref.Trim()).nzp_wp;
                calcfon.nzp = par_pack;
                ret = dbCalc.AddTask(calcfon);

                if (ret.result)
                if (ret.text == "" || ret.text == "0") ret.text = "Задача поставлена в очередь на выполнение";
                dbCalc.Close();
            }
            return ret;
        }

        //public Returns CancelPlat2(Finder finder, List<Pack_ls> list)
        //{
        //    Returns ret = new Returns();
        //    ret = CancelPlatXX2(10, finder, list); // Откат оплат РЦ
        //    ret = CancelPlatXX2(20, finder, list); // Откат оплат УК и ПУ
        //    return ret;
        //}

//        /// <summary>
//        /// Обработка портфеля. Откат ранее учтённых оплат
//        /// </summary>
//        /// <param name="dt">Список оплат по которым необходимо выполнить формирование новой пачки</param>
//        /// <param name="ret">информация о пользователе</param>
//        /// <returns></returns>
//        public Returns CancelPlatXX2(int pack_type, Finder finder, List<Pack_ls> list)
//        {

//            //return new Returns();         
//            if (list == null) list = new List<Pack_ls>();

//            string sql_text = "";

//            string baseName = "";
//            string lnum_pack = "";
//            string lnzp_supp = "null";
//            string lfilename = "";

//            int lnzp_bank = 1999;

//            decimal gsum_ls_total = 0;
//            float count_kv_total = 0;
//            string pref = "";
//            DateTime dat_uchet;

//            string baseName_pref = "";
//            string baseName_1 = "";

//#if PG
//            baseName = Points.Pref + "_fin_" + (Points.DateOper.Year % 100).ToString("00");
//#else
//            baseName = Points.Pref + "_fin_" + (Points.DateOper.Year % 100).ToString("00");
//#endif

//#if PG
//            baseName_1 = Points.Pref + "_fin_" + (Points.DateOper.Year - 2000 - 1 % 100).ToString("00");
//#else
//            baseName_1 = Points.Pref + "_fin_" + (Points.DateOper.Year-2000-1  % 100).ToString("00");
//#endif
//            #region Подготовка к откату
//            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
//            IDataReader reader = null;
//            IDataReader reader2 = null;
//            Returns ret = OpenDb(conn_db, true);
//            if (!ret.result) return ret;

//            #region определение локального пользователя
//            int nzp_user = finder.nzp_user;
            
//            /*DbWorkUser db = new DbWorkUser();
//            Returns ret_user;
//            int nzp_user = db.GetLocalUser(conn_db, finder, out ret_user);
//            db.Close();
//            if (!ret.result)
//            {
//                //connectionID.Close();
//                ret.result = ret_user.result;
//                ret.tag = ret_user.tag;
//                ret.text = ret_user.text;
//                return ret;
//            }*/
//            #endregion

//            Pack pack = new Pack();
//            Pack_ls pack_ls = new Pack_ls();

//            IDbTransaction tranzaction;
//            try
//            {
//                tranzaction = conn_db.BeginTransaction();
//            }
//            catch
//            {
//                tranzaction = null;
//            }

//            int count_ls = 0;
//            int kod_sum = 0;
//            foreach (Pack_ls pls in list)
//            {
//                sql_text = "select kod_sum from " + baseName + tableDelimiter + "pack_ls  " +
//                                        " where nzp_pack_ls = " + pls.nzp_pack_ls;

//                ret = ExecRead(conn_db, tranzaction, out reader2, sql_text, true);
//                if (!ret.result)
//                {
//                    reader.Close();
//                    reader2.Close();
//                    ret = new Returns(false, "Не найдена оплата");
//                    if (tranzaction != null)
//                    {
//                        tranzaction.Rollback();
//                    }
//                    return ret;
//                }




//                if (reader2.Read())
//                {
//                    if (reader2["kod_sum"] != DBNull.Value)
//                    {
//                        kod_sum = Convert.ToInt32(reader2["kod_sum"]);
//                        if (pack_type == 10 && kod_sum != 50)
//                        {
//                            count_ls = count_ls + 1;
//                        }
//                        if (pack_type == 20 && kod_sum == 50)
//                        {
//                            count_ls = count_ls + 1;
//                        }

//                    }
//                }
//                reader2.Close();
//            }



//            if (count_ls == 0)
//            {
//                ret = new Returns(false, "Не найдены оплаты для отменты");

//            }


//            StringBuilder sql = new StringBuilder();

//            // 1. Определить номер пачки для отрицательного реестра
//            //try
//            {
//                sql.Remove(0, sql.Length);
//#if PG
//                sql.Append("SELECT COUNT(*) as co FROM " + baseName + ".pack  WHERE sum_rasp < 0 and nzp_bank = 1999 AND par_pack = nzp_pack ");
//#else
//                sql.Append("SELECT COUNT(*) as co FROM " + baseName + ":pack  WHERE sum_rasp < 0 and nzp_bank = 1999 AND par_pack = nzp_pack ");
//#endif
//                ret = ExecRead(conn_db, tranzaction, out reader, sql.ToString(), true);
//                if (!ret.result)
//                {

//                    MonitorLog.WriteLog("Ошибка определения номера отрицательного реестра " + (Constants.Viewerror ? "\n" +
//                        sql.ToString() + "(" + ret.sql_error + ")" : ""),
//                        MonitorLog.typelog.Error, 20, 201, true);
//                    return ret;
//                }
//                try
//                {
//                    if (reader.Read())
//                        if (reader["co"] != DBNull.Value)
//                            lnum_pack = ((-1) * (System.Convert.ToInt32(reader["co"]) + 1)).ToString();
//                }
//                finally
//                {
//                    reader.Close();
//                }
//                #region Добавление суперпачки для оплат портфела
//                lnzp_supp = "null";
//                lfilename = "";

//                sql_text = "INSERT INTO  " + baseName + tableDelimiter + "pack (";
//                sql_text +=
//                  "pack_type," +
//                  "nzp_supp, " +
//                  "file_name, " +
//                  "nzp_bank, " +
//                  "num_pack, " +
//                  "dat_uchet," +
//                  "dat_pack," +
//                  "count_kv," +
//                  "sum_pack," +
//                  "geton_pack," +
//                  "real_sum," +
//                  "flag," +
//                  "dat_vvod," +
//                  "islock," +
//                  "operday_payer," +
//                  "peni_pack," +
//                  "sum_rasp," +
//                  "sum_nrasp" +
//                  ") " +
//                  " VALUES ( " +
//                  pack_type + "," +                                   // pack_type
//                  Convert.ToString(lnzp_supp) + "," +       // nzp_supp
//                  "'" + Convert.ToString(lfilename) + "'," +  // num_pac
//                  "1000," +                                 // nzp_bank
//                  "'" + lnum_pack.ToString() + "'," +        // num_pack
//                  "'" + Points.DateOper.ToShortDateString() + "'," +     // dat_uchet
//                  "'" + Points.DateOper.ToShortDateString() + "'," + // dat_pack
//                  "0," +                                    // count_kv
//                  "0," +                                    // sum_pack
//                  "0," +                                    // geton_pack
//                  "0," +                                    // real_count
//                  Pack.Statuses.DistributedWithErrors.GetHashCode() + "," +  // flag  распределена с ошибками
//                  "'" + Points.DateOper.ToShortDateString() + "'," +  // dat_vvod
//                  "NULL, " +                                // islock
//                  "'" + Points.DateOper.ToShortDateString() + "'," +         // operday_payer
//                  "0," +                                    // peni_pack
//                  "0," +                                    // sum_rasp
//                  "0" +                                    // sum_nrasp                                
//                  ") ";

//                sql.Remove(0, sql.Length);
//                sql.Append(sql_text);
//                ret = ExecRead(conn_db, tranzaction, out reader, sql.ToString(), true);
//                if (!ret.result)
//                {
//                    MonitorLog.WriteLog("Ошибка добавления пачки " + (Constants.Viewerror ? "\n" +
//                        sql.ToString() + "(" + ret.sql_error + ")" : ""),
//                        MonitorLog.typelog.Error, 20, 201, true);
//                    if (tranzaction != null)
//                    {
//                        tranzaction.Rollback();
//                    }
//                    return ret;
//                }
//                sql.Remove(0, sql.Length);

//#if PG
//                sql.Append("SELECT lastval() as co");
//#else
//                sql.Append("SELECT first 1 dbinfo('sqlca.sqlerrd1') as co from systables");
//#endif
//                ret = ExecRead(conn_db, tranzaction, out reader, sql.ToString(), true);
//                if (!ret.result)
//                {
//                    MonitorLog.WriteLog("Ошибка добавления пачки " + (Constants.Viewerror ? "\n" +
//                    sql.ToString() + "(" + ret.sql_error + ")" : ""),
//                    MonitorLog.typelog.Error, 20, 201, true);
//                    if (tranzaction != null)
//                    {
//                        tranzaction.Rollback();
//                    }
//                    return ret;
//                }
//                else
//                {
//                    if (reader.Read())
//                        if (reader["co"] != DBNull.Value)
//                            pack.par_pack = System.Convert.ToInt32(reader["co"]);
//                    reader.Close();
//                }

//                sql.Remove(0, sql.Length);
//                sql_text = "update " + baseName + tableDelimiter + "pack set par_pack = nzp_pack  where nzp_pack = " + Convert.ToString(pack.par_pack);
//                sql.Append(sql_text);

//                ret = ExecRead(conn_db, tranzaction, out reader, sql.ToString(), true);
//                if (!ret.result)
//                {
//                    MonitorLog.WriteLog("Ошибка обновления суперпачки для отката " + (Constants.Viewerror ? "\n" +
//                    sql.ToString() + "(" + ret.sql_error + ")" : ""),
//                    MonitorLog.typelog.Error, 20, 201, true);
//                    if (tranzaction != null)
//                    {
//                        tranzaction.Rollback();
//                    }
//                    return ret;
//                }
//                #endregion Добавление суперпачки для оплат портфела

//                sql.Remove(0, sql.Length);
//                string oper_;
//                if (pack_type == 10)
//                {
//                    oper_ = "<>";
//                }
//                else
//                {
//                    oper_ = "=";
//                }
//                sql_text = "select * from " + baseName + tableDelimiter + "pack_ls where incase = 1 and dat_uchet is not null and kod_sum " + oper_ + " 50 " +
//                            " union all " +
//                            "select * from " + baseName_1 + tableDelimiter + "pack_ls where incase = 1 and dat_uchet is not null and kod_sum " + oper_ + " 50 ";


//                sql_text += " order by dat_uchet, nzp_pack_ls";
//                sql.Append(sql_text);

//                ret = ExecRead(conn_db, tranzaction, out reader, sql.ToString(), true);
//                if (!ret.result)
//                {
//                    MonitorLog.WriteLog("Ошибка получения списка оплат к откату " + (Constants.Viewerror ? "\n" +
//                    sql.ToString() + "(" + ret.sql_error + ")" : ""),
//                    MonitorLog.typelog.Error, 20, 201, true);
//                    if (tranzaction != null)
//                    {
//                        tranzaction.Rollback();
//                    }
//                    return ret;
//                }
//            #endregion Подготовка к откату
//                List<string> list_num_packs = new List<string>();
//                while (reader.Read())
//                {
//                    #region Добавление пачки
//                    string num_pack = "";
//                    sql.Remove(0, sql.Length);
//                    sql_text = "select nzp_bank, num_pack, nzp_supp, file_name from " + baseName + tableDelimiter + "pack where nzp_pack =  " + Convert.ToString(reader["nzp_pack"]);
//                    sql.Append(sql_text);
//                    ret = ExecRead(conn_db, tranzaction, out reader2, sql.ToString(), true);
//                    if (reader2.Read())
//                    {
//                        lnzp_bank = Convert.ToInt32(reader2["nzp_bank"]);

//                        if (lnzp_bank == 0)
//                        {
//                            lnzp_bank = 1999;
//                        }

//                        num_pack = Convert.ToString(reader2["num_pack"]);
//                        lnzp_supp = Convert.ToString(reader2["nzp_supp"]);
//                        if (lnzp_supp.Trim() == "")
//                            lnzp_supp = "null";
//                        lfilename = Convert.ToString(reader2["file_name"]);
//                    }

//                    lnum_pack = num_pack;

//                    if (!list_num_packs.Contains(num_pack))
//                    {
//                        list_num_packs.Add(num_pack);
//                        sql_text = "INSERT INTO " + baseName + tableDelimiter + "pack (" +
//                             "par_pack, " +
//                             "pack_type," +
//                             "nzp_supp, " +
//                             "file_name, " +
//                             "nzp_bank, " +
//                             "num_pack, " +
//                             "dat_uchet," +
//                             "dat_pack," +
//                             "count_kv," +
//                             "sum_pack," +
//                             "geton_pack," +
//                             "real_sum," +
//                             "flag," +
//                             "dat_vvod," +
//                             "islock," +
//                             "operday_payer," +
//                             "peni_pack," +
//                             "sum_rasp," +
//                             "sum_nrasp" +
//                             ") " +
//                             " VALUES ( " +
//                             Convert.ToString(pack.par_pack) + "," +   // par_pack
//                             "" + pack_type + "," +                                   // pack_type
//                             Convert.ToString(lnzp_supp) + "," +       // nzp_supp
//                             "'" + Convert.ToString(lfilename) + "'," +  // num_pac
//                             Convert.ToString(lnzp_bank) + "," +       // nzp_bank
//                             "'" + Convert.ToString(num_pack) + "'," +  // num_pack
//                             "'" + Points.DateOper.ToShortDateString() + "'," +  // dat_uchet
//                             "'" + Points.DateOper.ToShortDateString() + "'," +  // dat_pack
//                             "1," +                                    // count_kv
//                             Convert.ToString((-1) * Convert.ToDecimal(reader["g_sum_ls"])) + "," +  // sum_pack
//                             "0," +                                    // geton_pack
//                             "0," +                                    // real_count
//                             Pack.Statuses.Distributed.GetHashCode() + "," + // flag  // Распределена
//                             "'" + Convert.ToDateTime(reader["dat_vvod"]).ToShortDateString() + "'," +  // dat_vvod
//                             "NULL, " +                                // islock
//                             "'" + Points.DateOper.ToShortDateString() + "'," + // operday_payer
//                             "0," +                                    // peni_pack
//                             "0," +                                    // sum_rasp
//                             "0 " +                                   // sum_nrasp                         
//                             ") ";

//                        sql.Remove(0, sql.Length);
//                        sql.Append(sql_text);
//                        ret = ExecRead(conn_db, tranzaction, out reader2, sql.ToString(), true);
//                        if (!ret.result)
//                        {
//                            MonitorLog.WriteLog("Ошибка добавления пачки с отрицательной оплатой" + (Constants.Viewerror ? "\n" +
//                            sql.ToString() + "(" + ret.sql_error + ")" : ""),
//                            MonitorLog.typelog.Error, 20, 201, true);
//                            if (tranzaction != null)
//                            {
//                                tranzaction.Rollback();
//                            }
//                            return ret;
//                        }

//                        sql.Remove(0, sql.Length);

//#if PG
//                        sql.Append("SELECT lastval() as co");
//#else
//                        sql.Append("SELECT first 1 dbinfo('sqlca.sqlerrd1') as co from systables");
//#endif
//                        ret = ExecRead(conn_db, tranzaction, out reader2, sql.ToString(), true);

//                        if (reader2.Read())
//                            if (reader2["co"] != DBNull.Value)
//                                pack.nzp_pack = System.Convert.ToInt32(reader2["co"]);
//                        if (!ret.result)
//                        {
//                            MonitorLog.WriteLog("Ошибка добавления пачки с отрицательной оплатой" + (Constants.Viewerror ? "\n" +
//                            sql.ToString() + "(" + ret.sql_error + ")" : ""),
//                            MonitorLog.typelog.Error, 20, 201, true);
//                            if (tranzaction != null)
//                            {
//                                tranzaction.Rollback();
//                            }
//                            return ret;
//                        }
//                        reader2.Close();
//                    }
//                    #endregion Добавление пачки

//                    #region Добавление оплаты с отрицательной суммой
//                    // Добавить отрицательную оплату
//                    sql_text = "INSERT INTO " + baseName + tableDelimiter + "pack_ls ";
//                    sql_text += "(" +
//                                "nzp_pack,   " +
//                                "num_ls,     " +
//                                "prefix_ls,  " +
//                                "g_sum_ls,   " +
//                                "sum_ls,     " +
//                                "geton_ls,   " +
//                                "sum_peni,   " +
//                                "dat_month,  " +
//                                "kod_sum,    " +
//                                "nzp_supp,   " +
//                                "id_bill,    " +
//                                "dat_vvod,   " +
//                                "dat_uchet,   " +
//                                "anketa,     " +
//                                "info_num,   " +
//                                "inbasket,   " +
//                                "alg,        " +
//                        //                                "date_distr, " +
//                                "erc_code," +
//                        //                              "nzp_rs," +
//                                "nzp_user    " +
//                                ") " +
//                                "VALUES (" +
//                                Convert.ToString(pack.nzp_pack) + ",";

//                    if (reader["num_ls"] != DBNull.Value)
//                    {
//                        sql_text = sql_text +
//                                Convert.ToString(reader["num_ls"]) + ",";
//                    }
//                    else
//                        sql_text = sql_text + "NULL,";
//                    if (reader["prefix_ls"] != DBNull.Value)
//                    {
//                        sql_text = sql_text +
//                                Convert.ToString(reader["prefix_ls"]) + ",";
//                    }
//                    else
//                        sql_text = sql_text + "NULL,";

//                    if (reader["g_sum_ls"] != DBNull.Value)
//                    {
//                        sql_text = sql_text +
//                                Convert.ToString((-1) * Convert.ToDecimal(reader["g_sum_ls"])) + ",";
//                    }
//                    else
//                        sql_text = sql_text + "0,";

//                    if (reader["sum_ls"] != DBNull.Value)
//                    {
//                        sql_text = sql_text +
//                                Convert.ToString(reader["sum_ls"]) + ",";
//                    }
//                    else
//                        sql_text = sql_text + "0,";


//                    if (reader["geton_ls"] != DBNull.Value)
//                    {
//                        sql_text = sql_text +
//                                Convert.ToString((-1) * Convert.ToDecimal(reader["geton_ls"])) + ",";
//                    }
//                    else
//                        sql_text = sql_text + "0,";

//                    if (reader["sum_peni"] != DBNull.Value)
//                    {
//                        sql_text = sql_text +
//                                Convert.ToString((-1) * Convert.ToDecimal(reader["sum_peni"])) + ",";
//                    }
//                    else
//                        sql_text = sql_text + "0,";

//                    if (reader["dat_month"] != DBNull.Value)
//                    {
//                        sql_text = sql_text +
//                                "'" + Convert.ToDateTime(reader["dat_month"]).ToShortDateString() + "',";
//                    }
//                    else
//                        sql_text = sql_text + "null,";



//                    if (reader["kod_sum"] != DBNull.Value)
//                    {
//                        sql_text = sql_text +
//                                Convert.ToString(reader["kod_sum"]) + ",";
//                    }
//                    else
//                        sql_text = sql_text + "NULL,";

//                    if (reader["nzp_supp"] != DBNull.Value)
//                    {
//                        sql_text = sql_text +
//                                Convert.ToString(reader["nzp_supp"]) + ",";
//                    }
//                    else
//                        sql_text = sql_text + "NULL,";

//                    if (reader["id_bill"] != DBNull.Value)
//                    {
//                        sql_text = sql_text +
//                                Convert.ToString(reader["id_bill"]) + ",";
//                    }
//                    else
//                        sql_text = sql_text + "NULL,";

//                    if (reader["dat_vvod"] != DBNull.Value)
//                    {
//                        sql_text = sql_text +
//                                "'" + Convert.ToDateTime(reader["dat_vvod"]).ToShortDateString() + "',";
//                    }
//                    else
//                        sql_text = sql_text + "NULL,";

//                    sql_text = sql_text + "'" + Points.DateOper.ToShortDateString() + "',";  // dat_uchet

//                    if (reader["anketa"] != DBNull.Value)
//                    {
//                        sql_text = sql_text + "'" +
//                                Convert.ToString(reader["anketa"]) + "',";
//                    }
//                    else
//                        sql_text = sql_text + "0,";

//                    if (reader["info_num"] != DBNull.Value)
//                    {
//                        sql_text = sql_text + "'" +
//                                Convert.ToString(reader["info_num"]) + "',";
//                    }
//                    else
//                        sql_text = sql_text + "NULL,";

//                    // inbasket
//                    sql_text = sql_text + "0,";

//                    if (reader["alg"] != DBNull.Value)
//                    {
//                        sql_text = sql_text + "'" +
//                                Convert.ToString(reader["alg"]) + "',";
//                    }
//                    else
//                        sql_text = sql_text + "0,";

//                    if (reader["erc_code"] != DBNull.Value)
//                    {
//                        if (Convert.ToString(reader["erc_code"]).Trim() != "")
//                        {
//                            sql_text = sql_text +
//                                    Convert.ToString(reader["erc_code"]) + ",";
//                        }
//                        else
//                        {
//                            sql_text = sql_text +
//                                     "NULL,";
//                        }
//                    }
//                    else
//                        sql_text = sql_text + "NULL,";

//                    sql_text = sql_text + Convert.ToString(nzp_user) + ") ";
//                    reader2.Close();
//                    sql.Remove(0, sql.Length);
//                    sql.Append(sql_text);
//                    ret = ExecRead(conn_db, tranzaction, out reader2, sql.ToString(), true);
//                    if (!ret.result)
//                    {
//                        MonitorLog.WriteLog("Ошибка добавления пачки с отрицательной оплатой" + (Constants.Viewerror ? "\n" +
//                        sql.ToString() + "(" + ret.sql_error + ")" : ""),
//                        MonitorLog.typelog.Error, 20, 201, true);
//                        if (tranzaction != null)
//                        {
//                            tranzaction.Rollback();
//                        }
//                        return ret;
//                    }

//                    reader2.Close();

//                    sql.Remove(0, sql.Length);

//#if PG
//                    sql.Append("SELECT lastval() as co");
//#else
//                    sql.Append("SELECT first 1 dbinfo('sqlca.sqlerrd1') as co from systables");
//#endif
//                    ret = ExecRead(conn_db, tranzaction, out reader2, sql.ToString(), true);


//                    if (reader2.Read())
//                        if (reader2["co"] != DBNull.Value)
//                            pack_ls.nzp_pack_ls = System.Convert.ToInt32(reader2["co"]);


//                    gsum_ls_total = gsum_ls_total + Convert.ToDecimal(reader["g_sum_ls"]);
//                    count_kv_total = count_kv_total + 1;

//                    if (!ret.result)
//                    {
//                        MonitorLog.WriteLog("Ошибка добавления пачки с отрицательной оплатой" + (Constants.Viewerror ? "\n" +
//                        sql.ToString() + "(" + ret.sql_error + ")" : ""),
//                        MonitorLog.typelog.Error, 20, 201, true);
//                        if (tranzaction != null)
//                        {
//                            tranzaction.Rollback();
//                        }
//                        return ret;
//                    }

//                    #endregion Добавление оплаты с отрицательной суммой

//                    #region Определение префикса для определения банка
//                    sql_text = "select pref from " + Points.Pref + "_data" + tableDelimiter + "kvar  " +
//                                            " where num_ls = " + Convert.ToString(reader["num_ls"]);

//                    ret = ExecRead(conn_db, tranzaction, out reader2, sql_text, true);
//                    if (!ret.result)
//                    {
//                        reader.Close();
//                        reader2.Close();
//                        ret = new Returns(false, "Не найдена оплата");
//                        if (tranzaction != null)
//                        {
//                            tranzaction.Rollback();
//                        }
//                        return ret;
//                    }



//                    dat_uchet = Points.DateOper;
//                    if (reader2.Read())
//                    {
//                        if (reader["dat_uchet"] != DBNull.Value) dat_uchet = Convert.ToDateTime(reader["dat_uchet"]);
//                        if (reader2["pref"] != DBNull.Value) pref = Convert.ToString(reader2["pref"]).Trim();
//                    }
//                    else
//                    {
//                        reader.Close();
//                        reader2.Close();
//                        ret = new Returns(false, "Не найдена оплата");
//                        if (tranzaction != null)
//                        {
//                            tranzaction.Rollback();
//                        }
//                        return ret;
//                    }

//                    reader2.Close();

//                    if (pref == "")
//                    {
//                        ret = new Returns(false, "Префикс БД не задан");
//                        if (tranzaction != null)
//                        {
//                            tranzaction.Rollback();
//                        }
//                        return ret;
//                    }
//                    string fn_supplier_pref;
//                    string fn_supplier_new;
//                    if (pack_type == 10)
//                    {
//                        fn_supplier_pref = pref + "_charge_" + (dat_uchet.Year % 100).ToString("00") + tableDelimiter + "fn_supplier" + dat_uchet.Month.ToString("00");
//                        fn_supplier_new = pref + "_charge_" + (Points.DateOper.Year % 100).ToString("00") + tableDelimiter + "fn_supplier" + Points.DateOper.Month.ToString("00");
//                    }
//                    else
//                    {
//                        fn_supplier_pref = pref + "_charge_" + (dat_uchet.Year % 100).ToString("00") + tableDelimiter + "from_supplier";
//                        fn_supplier_new = pref + "_charge_" + (Points.DateOper.Year % 100).ToString("00") + tableDelimiter + "from_supplier";
//                    }
//                    #endregion Определение префикса для определения банка

//                    #region Перенос распределений
//                    if (pack_type == 10)
//                    {
//                        sql_text = " INSERT INTO " + fn_supplier_new +
//                                          " ( " +
//                                          "nzp_serv ," +
//                                          "nzp_supp ," +
//                                          "nzp_pack_ls," +
//                                          "nzp_charge ," +
//                                          "num_charge ," +
//                                          "num_ls ," +
//                                          "sum_prih ," +
//                                          "kod_sum ," +
//                                          "dat_month ," +
//                                          "dat_prih ," +
//                                          "dat_uchet ," +
//                                          "dat_plat ," +
//                                          "s_user ," +
//                                          "s_dolg ," +
//                                          "s_forw ) " +
//                                          " select " +
//                                          "nzp_serv ," +
//                                          "nzp_supp ," +
//                                          Convert.ToString(pack_ls.nzp_pack_ls) + "," +
//                                          "nzp_charge ," +
//                                          "num_charge ," +
//                                          "num_ls ," +
//                                          "(-1)*sum_prih ," +
//                                          "kod_sum ," +
//                                          "dat_month ," +
//                                          "dat_prih ," +
//                                          "'" + Points.DateOper.ToShortDateString() + "'," +  // dat_uchet
//                                          "dat_plat, " +
//                                          "s_user ," +
//                                          "s_dolg ," +
//                                          "s_forw  " +
//                                          " from  " + fn_supplier_pref +
//                                          " where  nzp_pack_ls =  " + Convert.ToString(reader["nzp_pack_ls"]);
//                    }
//                    else
//                    {
//                        sql_text = " INSERT INTO " + fn_supplier_new +
//                                                              " ( " +
//                                                              "nzp_serv ," +
//                                                              "nzp_supp ," +
//                                                              "nzp_pack_ls," +
//                                                              "nzp_charge ," +
//                                                              "num_charge ," +
//                                                              "num_ls ," +
//                                                              "sum_prih ," +
//                                                              "kod_sum ," +
//                                                              "dat_month ," +
//                                                              "dat_prih ," +
//                                                              "dat_uchet ," +
//                                                              "dat_plat ) " +
//                                                              " select " +
//                                                              "nzp_serv ," +
//                                                              "nzp_supp ," +
//                                                              Convert.ToString(pack_ls.nzp_pack_ls) + "," +
//                                                              "nzp_charge ," +
//                                                              "num_charge ," +
//                                                              "num_ls ," +
//                                                              "(-1)*sum_prih ," +
//                                                              "kod_sum ," +
//                                                              "dat_month ," +
//                                                              "dat_prih ," +
//                                                              "'" + Points.DateOper.ToShortDateString() + "'," +  // dat_uchet
//                                                              "dat_plat " +
//                                                              " from  " + fn_supplier_pref +
//                                                              " where  nzp_pack_ls =  " + Convert.ToString(reader["nzp_pack_ls"]);

//                    }
//                    ret = ExecRead(conn_db, tranzaction, out reader2, sql_text, true);
//                    if (!ret.result)
//                    {
//                        if (tranzaction != null)
//                        {
//                            tranzaction.Rollback();
//                        }
//                        return ret;
//                    }
//                    sql_text = "update " + baseName + tableDelimiter + "pack_ls set incase = 0 where nzp_pack_ls = " + Convert.ToString(reader["nzp_pack_ls"] + " and incase = 1");
//                    ret = ExecRead(conn_db, tranzaction, out reader2, sql_text, true);
//                    if (!ret.result)
//                    {
//                        if (tranzaction != null)
//                        {
//                            tranzaction.Rollback();
//                        }
//                        return ret;
//                    }
//                    sql_text = "update " + baseName_1 + tableDelimiter + "pack_ls set incase = 0 where nzp_pack_ls = " + Convert.ToString(reader["nzp_pack_ls"] + " and incase = 1");
//                    ret = ExecRead(conn_db, tranzaction, out reader2, sql_text, true);
//                    if (!ret.result)
//                    {
//                        if (tranzaction != null)
//                        {
//                            tranzaction.Rollback();
//                        }
//                        return ret;
//                    }

//                    #endregion Перенос распределений с отрицательным знаком

//                    bool addpositiveoplat = false;
//                    if (finder.dopFind != null && finder.dopFind.Count > 0 && finder.dopFind[0] == "all")
//                    {
//                        addpositiveoplat = true;
//                    }
//                    else
//                    {
//                        foreach (Pack_ls pls in list)
//                        {
//                            if (pls.nzp_pack_ls == Convert.ToInt32(reader["nzp_pack_ls"]) &&
//                                pls.nzp_pack == Convert.ToInt32(reader["nzp_pack"]) &&
//                                pls.num_ls == Convert.ToInt32(reader["num_ls"]) &&
//                                (pls.year_ == Points.DateOper.Year || pls.year_ == Points.DateOper.Year - 1))
//                                addpositiveoplat = true;

//                            if (pack_type == 10 && pls.kod_sum == 50)
//                            {
//                                addpositiveoplat = false;
//                            }
//                            if (pack_type == 20 && pls.kod_sum != 50)
//                            {
//                                addpositiveoplat = false;
//                            }

//                        }
//                    }
//                    int status = Pack.Statuses.Distributed.GetHashCode();
//                    if (addpositiveoplat)
//                    {
//                        status = Pack.Statuses.DistributedWithErrors.GetHashCode();
//                        #region Добавление положительной оплаты
//                        // Добавить положительную оплату
//                        baseName_pref = Points.Pref + "_fin_" + (Convert.ToDateTime(reader["dat_uchet"]).Year % 100).ToString("00");
//                        // Добавить положительную оплату
//                        sql_text = "INSERT INTO " + baseName + tableDelimiter + "pack_ls ";
//                        sql_text += "(" +
//                                    "nzp_pack,   " +
//                                    "num_ls,     " +
//                                    "prefix_ls,  " +
//                                    "g_sum_ls,   " +
//                                    "sum_ls,     " +
//                                    "geton_ls,   " +
//                                    "sum_peni,   " +
//                                    "dat_month,  " +
//                                    "kod_sum,    " +
//                                    "nzp_supp,   " +
//                                    "id_bill,    " +
//                                    "dat_vvod,   " +
//                                    "anketa,     " +
//                                    "info_num,   " +
//                                    "inbasket,   " +
//                                    "alg,        " +
//                                    "date_distr, " +
//                                    "erc_code," +
//                                    "nzp_user    " +
//                                    ") " +
//                                    " select " +
//                                    Convert.ToString(pack.nzp_pack) + "," +
//                                    "num_ls,     " +
//                                    "prefix_ls,  " +
//                                    "g_sum_ls,   " +
//                                    "sum_ls,     " +
//                                    "geton_ls,   " +
//                                    "sum_peni,   " +
//                                    "dat_month,  " +
//                                    "kod_sum,    " +
//                                    "nzp_supp,   " +
//                                    "id_bill,    " +
//                                    "dat_vvod,   " +
//                                    "anketa,     " +
//                                    "info_num,   " +
//                                    "inbasket,   " +
//                                    "0,        " +
//                                    "date_distr, " +
//                                    "erc_code," +
//                                    Convert.ToString(nzp_user) +
//                                    " from ";
//                        sql_text +=
//                                    baseName_pref + tableDelimiter + "pack_ls " +
//                                    " where nzp_pack_ls = " + Convert.ToString(reader["nzp_pack_ls"]);

//                        reader2.Close();
//                        sql.Remove(0, sql.Length);
//                        sql.Append(sql_text);
//                        ret = ExecRead(conn_db, tranzaction, out reader2, sql.ToString(), true);
//                        if (!ret.result)
//                        {
//                            MonitorLog.WriteLog("Ошибка добавления пачки с положительной оплатой" + (Constants.Viewerror ? "\n" +
//                            sql.ToString() + "(" + ret.sql_error + ")" : ""),
//                            MonitorLog.typelog.Error, 20, 201, true);
//                            if (tranzaction != null)
//                            {
//                                tranzaction.Rollback();
//                            }
//                            return ret;
//                        }
//                        gsum_ls_total = gsum_ls_total - Convert.ToDecimal(reader["g_sum_ls"]);
//                        count_kv_total = count_kv_total + 1;
//                        #endregion Добавление положительной оплаты
//                    }

//                    #region обновление информации по пачке
//                    sql_text = "update " + baseName + tableDelimiter + "pack set sum_pack = (select sum(g_sum_ls) from " +
//                        baseName + tableDelimiter + "pack_ls where nzp_pack = " + Convert.ToString(pack.nzp_pack) + "), " +
//                        " count_kv = (select count(*) from " + baseName + tableDelimiter + "pack_ls where nzp_pack = " + Convert.ToString(pack.nzp_pack) + ") " +
//                        " where nzp_pack = " + Convert.ToString(pack.nzp_pack);
//                    ret = ExecRead(conn_db, tranzaction, out reader2, sql_text, true);
//                    if (!ret.result)
//                    {
//                        if (tranzaction != null)
//                        {
//                            tranzaction.Rollback();
//                        }
//                        return ret;
//                    }
//                    sql_text = "update " + baseName + tableDelimiter + "pack set flag = " + status + ", sum_nrasp = (select sum(g_sum_ls) from " +
//                        baseName + tableDelimiter + "pack_ls where nzp_pack = " + Convert.ToString(pack.nzp_pack) + " and dat_uchet is null ) where nzp_pack = " + Convert.ToString(pack.nzp_pack);
//                    ret = ExecRead(conn_db, tranzaction, out reader2, sql_text, true);
//                    if (!ret.result)
//                    {
//                        if (tranzaction != null)
//                        {
//                            tranzaction.Rollback();
//                        }
//                        return ret;
//                    }
//                    #endregion обновление информации по пачке

//                }
//            }

//            #region Обновление сводной информации по общей сумме оплаты и количеству квитанций по суперпачке
//            /*     sql_text = "update " + baseName + tableDelimiter + "pack set sum_pack = (-1)*" + Convert.ToString(gsum_ls_total) + ", count_kv =" + Convert.ToString(count_kv_total) + " where nzp_pack = " + Convert.ToString(pack.par_pack);
//            ret = ExecRead(conn_db, tranzaction, out reader2, sql_text, true);
//            if (!ret.result)
//            {
//                if (tranzaction != null)
//                {
//                    tranzaction.Rollback();
//                }
//                return ret;
//            }*/
//            #endregion Обновление сводной информации по общей сумме оплаты и количеству квитанций по суперпачке

//            if (tranzaction != null)
//            {
//                tranzaction.Commit();
//            }


//            #region Обновление сводной информации по общей сумме оплаты и количеству квитанций по суперпачке
//            PackFinder fndr = new PackFinder();
//            fndr.nzp_user = nzp_user;
//            fndr.year_ = Points.DateOper.Year;
//            fndr.nzp_pack = pack.par_pack;

//            ReturnsType ret2 = Upd_SUM_RASP_and_SUM_NRASP(fndr);
//            #endregion Обновление сводной информации по общей сумме оплаты и количеству квитанций по суперпачке
//            /*
//                catch (Exception ex)
//                {
//                    MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
//                    if (tranzaction != null) tranzaction.Rollback();
//                    throw new Exception(ex.Message, ex);
//                }
//                finally
//                {
//                    conn_db.Close();
//                }

//                return ret;
//            }
//             */
//            return ret;
//        }


        public ReturnsType RepairPackLs(FinderObjectType<List<Pack_ls>> request, IDbConnection connectionID)
        {
            ReturnsType ret = new ReturnsType();

            ret.tag = 0;
            ret.result = true;

            #region Исправление типовых ошибок
            string sqlText = "";
#if PG
            string baseName = Points.Pref + "_fin_" + (Points.DateOper.Year % 100).ToString("00");
#else
            string baseName = Points.Pref + "_fin_" + (Points.DateOper.Year % 100).ToString("00") + "@" + DBManager.getServer(connectionID);
#endif

            IDbTransaction tranzaction;

            int nzp_user = request.entity[0].nzp_user;

            int oldYear = 0;
            bool start = false;
            if (request.entity.Count <= 0) return new ReturnsType(false, "Не выбрана оплата для исправления", -1);

            for (int i = 0; i <= request.entity.Count - 1; i++)
            {
                start = true;
                if (Points.DateOper.Year > request.entity[i].year_)
                {
                    oldYear++;
                    continue;
                }
               
                decimal sum_charge;
                DataRow rowPackLs;
                DataRow r3;

                sqlText = "select * from " + baseName + tableDelimiter + "pack_ls_err  where nzp_pack_ls=" + request.entity[i].nzp_pack_ls;
                DataTable dtErrs = ClassDBUtils.OpenSQL(sqlText, connectionID).GetData();

                sqlText = "select * from " + baseName + tableDelimiter + "pack_ls  where nzp_pack_ls=" + request.entity[i].nzp_pack_ls;
                DataTable dtPackLs = ClassDBUtils.OpenSQL(sqlText, connectionID).GetData();
                rowPackLs = dtPackLs.Rows[0];

                DataTable dt3;

                Ls kvar = null;
                try
                {
                    kvar = new Ls {num_ls = Convert.ToInt32(rowPackLs["num_ls"])};
                }
                catch
                {
                    kvar = new Ls();
                }
                var dbadres = new DbAdres();
                ReturnsObjectType<Ls> pref = dbadres.GetLsLocation(kvar, connectionID);
                dbadres.Close();
                pref.ThrowExceptionIfError();

                try
                {
                    tranzaction = connectionID.BeginTransaction();
                }
                catch
                {
                    tranzaction = null;
                }

                foreach (DataRow r in dtErrs.Rows)
                {

                    #region 1. Начислено к оплате в квитанции не совпадает с начислено к оплате в базе
                    if (r["nzp_err"].ToString() == "8") // Если не совпадает начислено к оплате в базе с суммой в квитанции
                    {
                        sqlText = " SELECT SUM(sum_charge) as sum_charge FROM " +
                                    pref.returnsData.pref + "_charge_" + (Convert.ToDateTime(rowPackLs["dat_month"]).Year % 100).ToString("00") +
                                    tableDelimiter + "charge_" + (Convert.ToDateTime(rowPackLs["dat_month"]).Month % 100).ToString("00") +
                                    " WHERE num_ls = " + kvar.num_ls + " AND nzp_serv > 1 ";
                        dt3 = ClassDBUtils.OpenSQL(sqlText, connectionID, tranzaction).GetData();
                        r3 = dt3.Rows[0];
                        sum_charge = Convert.ToDecimal(r3["sum_charge"]);
                        dt3.Clear();
                        sqlText = "update " + baseName + tableDelimiter +
                            "pack_ls set sum_ls = " + sum_charge.ToString() + ", nzp_user = " + nzp_user + " where nzp_pack_ls = " + request.entity[i].nzp_pack_ls;
                        ClassDBUtils.ExecSQL(sqlText, connectionID, tranzaction);

                        sqlText =
                                " Insert into " + baseName + tableDelimiter + "pack_log " +
                                " (nzp_pack_ls,   nzp_pack, dat_oper, dat_log  , txt_log ) " +
                                " select nzp_pack_ls,   nzp_pack,'" + Points.DateOper.ToShortDateString() + "',now(), " +
                                "'Исправление ошибки №8 по несовпадению сумм начисления(с " + Convert.ToString(rowPackLs["sum_ls"]) + " на " + sum_charge.ToString() + ")'" +
                                " from " + baseName + ".pack_ls where nzp_pack_ls = " + request.entity[i].nzp_pack_ls;
                        ClassDBUtils.ExecSQL(sqlText, connectionID, tranzaction);

                        sqlText = "delete from  " + baseName + tableDelimiter + "pack_ls_err where nzp_err=  " + r["nzp_err"].ToString() + " and nzp_pack_ls = " + request.entity[i].nzp_pack_ls;
                        ClassDBUtils.ExecSQL(sqlText, connectionID, tranzaction);
                    }
                    #endregion 1. Начислено к оплате в квитанции не совпадает с начислено к оплате в базе
                    else
                        #region 2. Сумма в графе оплачиваю превосходит сумму оплаты
                        if (r["nzp_err"].ToString() == "3") // Сумма в графе оплачиваю превосходит сумму оплаты
                        {
                            sqlText = "delete from " + baseName + tableDelimiter + "gil_sums where nzp_pack_ls = " + request.entity[i].nzp_pack_ls;
                            ClassDBUtils.ExecSQL(sqlText, connectionID, tranzaction);

                            sqlText =
                            " Insert into " + baseName + tableDelimiter + "pack_log " +
                            " (nzp_pack_ls,   nzp_pack, dat_oper, dat_log  , txt_log ) " +
                            " select nzp_pack_ls,   nzp_pack,'" + Points.DateOper.ToShortDateString() + "',now(), " +
                            "'Исправление ошибки № 3 (Сумма оплаты меньше суммы по графе оплачиваю)' from " + baseName + tableDelimiter + "pack_ls where nzp_pack_ls = " + request.entity[i].nzp_pack_ls;
                            ClassDBUtils.ExecSQL(sqlText, connectionID, tranzaction);

                            sqlText = "delete from  " + baseName + tableDelimiter + "pack_ls_err where nzp_err=  " + r["nzp_err"].ToString() + " and nzp_pack_ls = " + request.entity[i].nzp_pack_ls;
                            ClassDBUtils.ExecSQL(sqlText, connectionID, tranzaction);

                        }
                        #endregion 2. Сумма в графе оплачиваю превосходит сумму оплаты
                        else
                            #region 3. Подозрение на оплату по коду 57
                            if (r["nzp_err"].ToString() == "701") // Подозрение на оплату по коду 57
                            {
                                sqlText = "update " + baseName + tableDelimiter + "pack_ls set kod_sum = 33 where nzp_pack_ls = " + request.entity[i].nzp_pack_ls;
                                ClassDBUtils.ExecSQL(sqlText, connectionID, tranzaction);

                                sqlText =
                                    " Insert into " + baseName + tableDelimiter + "pack_log " +
                                    " (nzp_pack_ls,   nzp_pack, dat_oper, dat_log  , txt_log ) " +
                                    " select nzp_pack_ls,   nzp_pack,'" + Points.DateOper.ToShortDateString() + "',now(), " +
                                    "'Подозрение  № 701 (Подозрение на оплату по коду 57)' from " + baseName + tableDelimiter + "pack_ls where nzp_pack_ls = " + request.entity[i].nzp_pack_ls;
                                ClassDBUtils.ExecSQL(sqlText, connectionID, tranzaction);

                                sqlText = "delete from  " + baseName + tableDelimiter + "pack_ls_err where nzp_err=  " + r["nzp_err"].ToString() + " and nzp_pack_ls = " + request.entity[i].nzp_pack_ls;
                                ClassDBUtils.ExecSQL(sqlText, connectionID, tranzaction);

                            }
                            #endregion 3. Подозрение на оплату по коду 57
                            else
                                #region 4. Подозрение на оплату по коду 55
                                if (r["nzp_err"].ToString() == "703") // Подозрение на оплату по коду 55
                                {
                                    sqlText = "update " + baseName + tableDelimiter + "pack_ls set kod_sum = 33 where nzp_pack_ls = " + request.entity[i].nzp_pack_ls;
                                    ClassDBUtils.ExecSQL(sqlText, connectionID, tranzaction);

                                    sqlText =
                                        " Insert into " + baseName + tableDelimiter + "pack_log " +
                                        " (nzp_pack_ls,   nzp_pack, dat_oper, dat_log  , txt_log ) " +
                                        " select nzp_pack_ls,   nzp_pack,'" + Points.DateOper.ToShortDateString() + "',now(), " +
                                        "'Подозрение  № 703 (Подозрение на оплату по коду 55)' from " + baseName + tableDelimiter + "pack_ls where nzp_pack_ls = " + request.entity[i].nzp_pack_ls;
                                    ClassDBUtils.ExecSQL(sqlText, connectionID, tranzaction);

                                    sqlText = "delete from  " + baseName + tableDelimiter + "pack_ls_err where nzp_err=  " + r["nzp_err"].ToString() + " and nzp_pack_ls = " + request.entity[i].nzp_pack_ls;
                                    ClassDBUtils.ExecSQL(sqlText, connectionID, tranzaction);
                                }
                                #endregion 4. Подозрение на оплату по коду 55
                                else
                                    #region 5. Прочие ошибки
                                    if (r["nzp_err"].ToString() == "99") // Прочие ошибки
                                    {
                                        if (r["note"].ToString().Trim() == "Изменения внесенные жильцом, не определена услуга, по изменениям")
                                        {
                                            sqlText = "delete from " + baseName + tableDelimiter + "gil_sums where nzp_pack_ls = " + request.entity[i].nzp_pack_ls;
                                            ClassDBUtils.ExecSQL(sqlText, connectionID, tranzaction);
                                            sqlText =
                                            " Insert into " + baseName + tableDelimiter + "pack_log " +
                                            " (nzp_pack_ls,   nzp_pack, dat_oper, dat_log  , txt_log ) " +
                                            " select nzp_pack_ls,   nzp_pack,'" + Points.DateOper.ToShortDateString() + "',now(), " +
                                            "'Исправление ошибки № 99 (Изменения внесенные жильцом, не определена услуга, по изменениям)' from " +
                                            baseName + tableDelimiter + "pack_ls where nzp_pack_ls = " + request.entity[i].nzp_pack_ls;
                                            ClassDBUtils.ExecSQL(sqlText, connectionID, tranzaction);

                                            sqlText = "delete from  " + baseName + tableDelimiter + "pack_ls_err where nzp_err=  " + r["nzp_err"].ToString() + " and nzp_pack_ls = " + request.entity[i].nzp_pack_ls;
                                            ClassDBUtils.ExecSQL(sqlText, connectionID, tranzaction);
                                        }
                                    }
                                    #endregion 5. Прочие ошибки
                                    else
                                        #region 6. Отсутсвуют начисления по лицевому счёту
                                        if (r["nzp_err"].ToString() == "1") //  Отсутсвуют начисления по лицевому счёту
                                        {
                                            sqlText =
                                                    " Insert into " + baseName + tableDelimiter + "pack_log " +
                                                    " (nzp_pack_ls,   nzp_pack, dat_oper, dat_log  , txt_log ) " +
                                                    " select nzp_pack_ls,   nzp_pack,'" + Points.DateOper.ToShortDateString() + "',now(), " +
                                                    "'Исправление ошибки № 1 (Отсутствуют начисления по лицевому счёту)' from " +
                                                    baseName + tableDelimiter + "pack_ls where nzp_pack_ls = " + request.entity[i].nzp_pack_ls;
                                            ClassDBUtils.ExecSQL(sqlText, connectionID, tranzaction);

                                            sqlText = "delete from  " + baseName + tableDelimiter + "pack_ls_err where nzp_err=  " + r["nzp_err"].ToString() + " and nzp_pack_ls = " + request.entity[i].nzp_pack_ls;
                                            ClassDBUtils.ExecSQL(sqlText, connectionID, tranzaction);
                                        }
                                        #endregion 6. Отсутствуют начисления
                                        else
                                            #region 7. Отказано в распределении в результате сбоев в программе и подозрение на дублирование оплаты
                                            if (r["nzp_err"].ToString() == "6" || r["nzp_err"].ToString() == "5") // Отказано в распределении в результате сбоев в программе и подозрение на дублирование оплаты
                                            {
                                                sqlText = "delete from " + baseName + tableDelimiter + "pack_ls_err where nzp_err = " + r["nzp_err"].ToString() + " and nzp_pack_ls = " + request.entity[i].nzp_pack_ls;
                                                ClassDBUtils.ExecSQL(sqlText, connectionID, tranzaction);

                                                sqlText =
                                                    " Insert into " + baseName + tableDelimiter + "pack_log " +
                                                    " (nzp_pack_ls,   nzp_pack, dat_oper, dat_log  , txt_log ) " +
                                                    " select nzp_pack_ls,   nzp_pack,'" + Points.DateOper.ToShortDateString() + "',now(), " +
                                                    "'Исправление ошибки № 6 (Отказано в распределении в результате сбоев в программе)' from " +
                                                    baseName + tableDelimiter + "pack_ls where nzp_pack_ls = " + request.entity[i].nzp_pack_ls;
                                                ClassDBUtils.ExecSQL(sqlText, connectionID, tranzaction);

                                                sqlText = "delete from  " + baseName + tableDelimiter + "pack_ls_err where nzp_err=  " + r["nzp_err"].ToString() + " and nzp_pack_ls = " + request.entity[i].nzp_pack_ls;
                                                ClassDBUtils.ExecSQL(sqlText, connectionID, tranzaction);

                                            }
                                            #endregion 7.Отказано в распределении в результате сбоев в программе
                                            else
                                                #region 8. Несоответствие суммы распределения сумме оплаты
                                                if (r["nzp_err"].ToString() == "4")
                                                {
                                                    sqlText = "delete from " + baseName + tableDelimiter + "pack_ls_err where nzp_err = " + r["nzp_err"].ToString() + " and nzp_pack_ls = " + request.entity[i].nzp_pack_ls;
                                                    ClassDBUtils.ExecSQL(sqlText, connectionID, tranzaction);

                                                    sqlText =
                                                        " Insert into " + baseName + tableDelimiter + "pack_log " +
                                                        " (nzp_pack_ls,   nzp_pack, dat_oper, dat_log  , txt_log ) " +
                                                        " select nzp_pack_ls,   nzp_pack,'" + Points.DateOper.ToShortDateString() + "',now(), " +
                                                        "'Исправление ошибки № 4 (Несоответствие суммы оплаты сумме распределения)' from " +
                                                        baseName + tableDelimiter + "pack_ls where nzp_pack_ls = " + request.entity[i].nzp_pack_ls;
                                                    ClassDBUtils.ExecSQL(sqlText, connectionID, tranzaction);

                                                    sqlText = "delete from  " + baseName + tableDelimiter + "pack_ls_err where nzp_err=  " + r["nzp_err"].ToString() + " and nzp_pack_ls = " + request.entity[i].nzp_pack_ls;
                                                    ClassDBUtils.ExecSQL(sqlText, connectionID, tranzaction);
                                                }
                                                #endregion 8. Несоответствие суммы распределения сумме оплаты
                                                else
                                                {
                                                    // Прочие ошибки не обрабатывать
                                                    ret.tag = -1;
                                                    ret.text = "Не все ошибки были исправлены";
                                                    ret.result = false;
                                                }
                }
                #region Если все ошибки исправлены, то снять признак об ошибке с оплаты
                //sqlText = "select count(*) as co from " + baseName + tableDelimiter + "pack_ls_err where   nzp_pack_ls = " + request.entity[i].nzp_pack_ls;
                //dtErrs.Clear();
                //dtErrs = ClassDBUtils.OpenSQL(sqlText, connectionID, tranzaction).GetData();
                //r3 = dtErrs.Rows[0];

                if (tranzaction != null)
                {
                    tranzaction.Commit();
                }

                //if (Convert.ToInt32(r3["co"]) == 0)
                try
                {
                    tranzaction = connectionID.BeginTransaction();
                }
                catch
                {
                    tranzaction = null;
                }

                //sqlText = "update " + baseName + tableDelimiter + "pack_ls set  inbasket = 0, dat_uchet = null, alg = 0 where nzp_pack_ls = " + request.entity[i].nzp_pack_ls;
                //ClassDBUtils.ExecSQL(sqlText, connectionID, tranzaction);

                sqlText = " DELETE FROM " + pref.returnsData.pref + "_charge_" + (Points.DateOper.Year % 100).ToString("00") +
                tableDelimiter + "fn_supplier" + Points.DateOper.Month.ToString("00") +
                " WHERE nzp_pack_ls = " + request.entity[i].nzp_pack_ls;
                ClassDBUtils.ExecSQL(sqlText, connectionID, tranzaction);

                sqlText = " DELETE FROM " + pref.returnsData.pref + "_charge_" + (Points.DateOper.Year % 100).ToString("00") +
                tableDelimiter + "from_supplier WHERE nzp_pack_ls = " + request.entity[i].nzp_pack_ls;
                ClassDBUtils.ExecSQL(sqlText, connectionID, tranzaction);

                if (tranzaction != null)
                {
                    tranzaction.Commit();
                }
                else
                {
                    tranzaction.Rollback();
                }

                // Если все ошибки исправлены, то распределить оплату ещё раз
                #region Распределение оплаты
                DbCalcPack db1 = new DbCalcPack();
                Returns ret2 = new Returns();
                db1.CalcPackLs(request.entity[i].nzp_pack_ls, request.entity[i].year_, Points.DateOper, true, false, out ret2, request.entity[i].nzp_user);  // Отдаем оплату на распределение
                db1.Close();
                #endregion Распределение оплаты

                sqlText = "select count(*) as co from " + baseName + tableDelimiter + "pack_ls where   nzp_pack_ls = " + request.entity[i].nzp_pack_ls + " and dat_uchet is not null and " +
                    sNvlWord + "(alg,'0')<>'0'";
                object obj = ExecScalar(connectionID, sqlText, out ret2, true);
                if (Convert.ToInt32(obj) == 0)
                {
                    ret = new ReturnsType(false, "Автоматически исправить оплату не удалось. Оплата не была распределена.", -1);
                }
                else ret = new ReturnsType(true, "Оплата была успешно распределена.", -1);


                #endregion Если все ошибки исправлены, то снять признак об ошибке с оплаты
            }


            #endregion Исправление типовой ошибки

            if (ret.result)
            {
                if (!start)
                {
                    ret = new ReturnsType(false,
                        "Исправление не было произведено, так как не установлена причина попадания оплаты в корзину.", -1);


                }
                else
                {

                    if (request.entity.Count == oldYear)
                        ret = new ReturnsType(false,
                            "Оплата(ы) находится(ятся) в закрытом финансовом периоде. Исправление невозможно.", -1);
                    else if (oldYear > 0)
                        ret = new ReturnsType(true,
                            oldYear + " из " + request.entity.Count +
                            " оплат находится в закрытом финансовом периоде. Их исправление невозможно.", -1);
                }
            }

            //ret.tag=0;
            return ret;
            #region типовые ошибки в финансах
            //if (r["nzp_err"].ToString() == "1") //  Отсутсвуют начисления по лицевому счёту
            //if (r["nzp_err"].ToString() == "2") // Отсутствует лицевой счёт
            //if (r["nzp_err"].ToString() == "3") // Сумма в графе оплачиваю превосходит сумму оплаты
            //if (r["nzp_err"].ToString() == "4") // Несоответствие суммы распределения по л/с и суммы оплаты
            //if (r["nzp_err"].ToString() == "5") // Подозрение на дублирование оплаты по лицевому счету
            //if (r["nzp_err"].ToString() == "6") // Отказано в распределении в результате сбоев в программе
            //if (r["nzp_err"].ToString() == "8") // Если не совпадает начислено к оплате в базе с суммой в квитанции                                                                             
            //if (r["nzp_err"].ToString() == "700") // Несоотвествие штрих-кода виду оплаты
            //if (r["nzp_err"].ToString() == "701") // Подозрение на оплату по коду 57
            //if (r["nzp_err"].ToString() == "703") // Подозрение на оплату по коду 55
            //if (r["nzp_err"].ToString() == "800") // ПНедопустимый код получателя средств (перый штрих-код)
            #endregion типовые ошибки в финансах

        }

        /// <summary>
        /// Повторное распределение оплат с отсутствием распределений по услугам
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        public Returns ReDistributePackLs(Finder finder)
        {
            #region соединение
            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            Returns ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;
            #endregion

            ExcelRepClient dbRep = new ExcelRepClient();
            ret = dbRep.AddMyFile(new ExcelUtility()
            {
                nzp_user = finder.nzp_user,
                status = ExcelUtility.Statuses.InProcess,
                rep_name = "Повторное распределение оплат с отсутствием распределений по услугам "
            });
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }
            int id = ret.tag;

            DateTime savedOperDay = Points.DateOper;

            try
            {
                DbTables tables = new DbTables(conn_db);
                string t_pack_ls = "temppackls";

                string sql = " drop table " + t_pack_ls;
                ret = ExecSQL(conn_db, sql, false);

                int fin_year = Points.DateOper.Year;

                string pack_ls = Points.Pref + "_fin_" + (fin_year%100).ToString("00");
#if !PG              
                pack_ls += "@" + DBManager.getServer(conn_db);
#endif
                pack_ls += tableDelimiter + "pack_ls";

                DateTime dt = new DateTime(fin_year, Points.DateOper.Month, 1);

                sql = " select nzp_pack_ls, dat_uchet, pref, nzp_user " +
                " from " + pack_ls + " a, " + tables.kvar + " k " +
                " where a.dat_uchet is not null and a.num_ls = k.num_ls and dat_uchet between " + Utils.EStrNull(dt.ToShortDateString()) +
                " and " + Utils.EStrNull(Points.DateOper.ToShortDateString()) + " " +
                " into temp " + t_pack_ls;
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return ret;

                foreach (_Point p in Points.PointList)
                {
                    string fnsupplier = p.pref + "_charge_" + (fin_year % 100).ToString("00") + tableDelimiter + "fn_supplier" + Points.DateOper.Month.ToString("00");

                    sql = " delete from " + t_pack_ls + " where pref = '" + p.pref + "' and nzp_pack_ls in (select nzp_pack_ls from " + fnsupplier + ")";
                    ret = ExecSQL(conn_db, sql, true);
                    if (!ret.result) return ret;
                }
                IDataReader reader;
                List<Pack_ls> list = new List<Pack_ls>();
                sql = " select nzp_pack_ls, dat_uchet, nzp_user from " + t_pack_ls;
                ret = ExecRead(conn_db, out reader, sql, true);
                if (!ret.result) return ret;
                while (reader.Read())
                {
                    Pack_ls pls = new Pack_ls();
                    if (reader["nzp_pack_ls"] != DBNull.Value) pls.nzp_pack_ls = Convert.ToInt32(reader["nzp_pack_ls"]);
                    if (reader["dat_uchet"] != DBNull.Value) pls.dat_uchet = Convert.ToDateTime(reader["dat_uchet"]).ToShortDateString();
                    list.Add(pls);
                }

                if (list.Count > 0)
                {
                    int count_err = 0;
                    int count = 0;


                    #region Распределение выбранных оплат
                    foreach (Pack_ls p in list)
                    {
                        DateTime dat_uchet = savedOperDay;
                        if (!DateTime.TryParse(p.dat_uchet, out dat_uchet)) dat_uchet = savedOperDay;

                        //Points.DateOper = dat_uchet;

                        ret = ExecSQL(conn_db,
                            "update " + pack_ls + " set dat_uchet = null, alg = 0 where nzp_pack_ls = " + p.nzp_pack_ls,
                            true);
                        if (!ret.result) continue;

                        #region Распределение оплаты
                        DbCalcPack db1 = new DbCalcPack();
                        Returns ret2 = new Returns();
                        if (!db1.CalcPackLs2(p.nzp_pack_ls, fin_year, dat_uchet, true, false, out ret2, p.nzp_user))  // Отдаем оплату на распределение
                            count_err += 1;
                        db1.Close();
                        dbRep.SetMyFileProgress(new ExcelUtility()
                        {
                            nzp_exc = id,
                            progress = ((decimal)count) / list.Count
                        });
                        #endregion Распределение оплаты
                        count++;
                    }

                    #endregion Распределение выбранных оплат
                    if (count_err > 0)
                    {
                        ret.tag = -1;
                        ret.text = "Не все оплаты были распределены";// ("+count_err.ToString()+" шт) !";
                        ret.result = false;
                    }
                    return ret;
                }
            }
            catch (Exception ex)
            {
                ret.result = false;
                MonitorLog.WriteLog("Ошибка при повторном распределении оплат с отсутствием распределений по услугам:" +
                    ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                return ret;
            }
            finally
            {
       //         Points.DateOper = savedOperDay;

                if (ret.result) dbRep.SetMyFileState((new ExcelUtility() { nzp_exc = id, status = ExcelUtility.Statuses.Success }));
                dbRep.Close();
                conn_db.Close();
            }
            return ret;
        }

        public ReturnsType DistributePackLs(FinderObjectType<List<Pack_ls>> request, IDbConnection connectionID)
        {
            //return new Returns();

            ReturnsType ret = new ReturnsType();
            ret.tag = 0;
            ret.result = true;
            int count_err = 0;

            #region Распределение выбранных оплат
            //IDbTransaction tranzaction;

            if (request.entity.Count > 0)
            {

                for (int i = 0; i <= request.entity.Count - 1; i++)
                {
                    #region Распределение оплаты
                    DbCalcPack db1 = new DbCalcPack();
                    Returns ret2 = new Returns();
                    if (!db1.CalcPackLs(request.entity[i].nzp_pack_ls, request.entity[i].year_, Points.DateOper, true, false, out ret2, request.entity[i].nzp_user, request.entity[i].isCurrentMonth))  // Отдаем оплату на распределение
                        count_err += 1;
                    db1.Close();
                    #endregion Распределение оплаты
                }

            }
            #endregion Распределение выбранных оплат
            if (count_err > 0)
            {
                ret.tag = -1;
                ret.text = "Не все оплаты были распределены ("+count_err.ToString()+" шт) !";
                ret.result = false;
            }
            return ret;
        }


        public Returns CheckPackLsToDeleting(Pack_ls finder)
        {
            #region соединение
            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            Returns ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;
            #endregion
            
            var sb = new StringBuilder();
            var sb_nzp_pack_ls = new StringBuilder();

            if (finder.nzp_pack > 0)
            {
                sb_nzp_pack_ls = new StringBuilder();
                sb_nzp_pack_ls.AppendFormat(
                    "select count(*) from {0}_fin_{1}{2}pack where nzp_pack = par_pack and nzp_pack = {3}",
                    Points.Pref, (finder.year_%100).ToString("00"), tableDelimiter, finder.nzp_pack);
                object obj1 = ExecScalar(conn_db, sb_nzp_pack_ls.ToString(), out ret, true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return ret;
                }
                int cnt1 = 0;
                Int32.TryParse(obj1.ToString(), out cnt1);
                if (cnt1 > 0)
                {
                    sb_nzp_pack_ls = new StringBuilder();
                    sb_nzp_pack_ls.AppendFormat("(select nzp_pack_ls from {0}_fin_{1}{2}pack_ls where nzp_pack in (select nzp_pack from  {0}_fin_{1}{2}pack where par_pack = {3}))", 
                        Points.Pref, (finder.year_ % 100).ToString("00"), tableDelimiter, finder.nzp_pack);
                }
                else
                {
                    sb_nzp_pack_ls = new StringBuilder();
                    sb_nzp_pack_ls.AppendFormat("(select nzp_pack_ls from {0}_fin_{1}{2}pack_ls where nzp_pack = {3})",
                        Points.Pref, (finder.year_%100).ToString("00"), tableDelimiter, finder.nzp_pack);
                }
            }
            else if (finder.nzp_pack_ls > 0)
            {
                sb_nzp_pack_ls = new StringBuilder();
                sb_nzp_pack_ls.AppendFormat("({0})", finder.nzp_pack_ls);
            }

            foreach (var point in Points.PointList)
            {
                object obj;
                int cnt;
                for (var i = 0; i < Points.DateOper.Month; i++)
                {
                    sb = new StringBuilder();
                    sb.AppendFormat("select count(*) from {0}_charge_{1}{2}fn_supplier{3} where dat_uchet <> '{5}' and nzp_pack_ls in {4}",
                        point.pref, (finder.year_ % 100).ToString("00"), tableDelimiter, (i + 1).ToString("00"), sb_nzp_pack_ls, Points.DateOper.ToShortDateString());
                    obj = ExecScalar(conn_db, sb.ToString(), out ret, true);
                    if (!ret.result)
                    {
                        conn_db.Close();
                        return ret;
                    }
                    cnt = 0;
                    Int32.TryParse(obj.ToString(), out cnt);
                    if (cnt > 0)
                    {
                        var str = finder.nzp_pack > 0 ? " пачку " : " оплату ";
                        ret = new Returns(false, "Удалить " + str + " нельзя, т.к. существует распределение в предыдущем операционном периоде", -1);
                        conn_db.Close();
                        return ret;
                    }
                }
                sb = new StringBuilder();
                sb.AppendFormat("select count(*) from {0}_charge_{1}{2}from_supplier where dat_uchet <> '{4}' and nzp_pack_ls in {3}",
                    point.pref, (finder.year_ % 100).ToString("00"), tableDelimiter, sb_nzp_pack_ls, Points.DateOper.ToShortDateString());
                obj = ExecScalar(conn_db, sb.ToString(), out ret, true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return ret;
                }
                cnt = 0;
                Int32.TryParse(obj.ToString(), out cnt);
                if (cnt > 0)
                {
                    var str = finder.nzp_pack > 0 ? " пачку " : " оплату ";
                    ret = new Returns(false, "Удалить " + str + " нельзя, т.к. существует распределение в предыдущем операционном периоде", -1);
                    conn_db.Close();
                    return ret;
                }
            }
            return ret;
        }

        public Returns PutTaskDistribLs(Dictionary<int, int> listPackLs, int nzp_user)
        {
            Returns ret = Utils.InitReturns();
            var db2 = new DbCalcPack();
            db2.PutTasksDistribOneLs(listPackLs, true, "", out ret);
            db2.Close();
            return ret;
        }

    } //end class

} //end namespace