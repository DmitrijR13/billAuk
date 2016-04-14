using System;
using System.Data;
using System.Linq;
using System.Xml.Linq;
using System.IO;
using System.Text;
using System.Collections.Generic;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Utility;


namespace STCLINE.KP50.DataBase
{


    //----------------------------------------------------------------------
    public partial class DbChargeClient : DataBaseHead
    //----------------------------------------------------------------------
    {

        string first_month, last_month;
        Charge ishZap = null;

        /// <summary> Сформировать условие отбора данных
        /// </summary>
        //----------------------------------------------------------------------
        void WhereStringForGet(ChargeFind finder, ref string whereString, out bool isOneMonth)
        //----------------------------------------------------------------------
        {
            StringBuilder swhere = new StringBuilder();

            if (finder.nzp_kvar > 0) swhere.Append(" and a.nzp_kvar = " + finder.nzp_kvar.ToString());

            // определить месяцы начислений
            first_month = String.Format("01.01.{0}", finder.YM.year_.ToString("0000"));
            last_month = String.Format("31.12.{0}", finder.YM.year_.ToString("0000"));
            if (finder.month_po > 0)
            {
                if (finder.YM.month_ > 0) first_month = String.Format("01.{0}.{1}", finder.YM.month_.ToString("00"), finder.YM.year_.ToString("0000"));
                last_month = new DateTime(finder.YM.year_, finder.month_po, 1).AddMonths(1).AddDays(-1).ToShortDateString();
            }
            else if (finder.YM.month_ > 0)
            {
                first_month = last_month = String.Format("01.{0}.{1}", finder.YM.month_.ToString("00"), finder.YM.year_.ToString("0000"));
            }

            if (last_month != first_month)
            {
                swhere.Append(" and a.dat_month <= '" + last_month + "'");
                swhere.Append(" and a.dat_month >= '" + first_month + "'");
                isOneMonth = false;
            }
            else
            {
                swhere.Append(" and a.dat_month = '" + first_month + "'");
                isOneMonth = true;
            }

            // не загружать перерасчеты, если запрашиваются начисления за несколько месяцев и при этом нет группировки по месяцам
            // или стоит признак "не показывать перерасчеты"
            if ((first_month != last_month && !Utils.GetParams(finder.groupby, Constants.act_groupby_month.ToString())) ||
                    (finder.is_show_reval != 1)
                )
                swhere.Append(" and a.dat_charge is null");

            whereString += swhere.ToString();
        }
        //----------------------------------------------------------------------
        private void AddZapToList(ref Charge zap, ref List<Charge> list, ref int position, ChargeFind finder)
        //----------------------------------------------------------------------
        {
            if (zap == null) return;
            if ((zap.dat_charge == "" && (zap.has_past_reval == 1 || zap.has_future_reval == 1)) ||
                    (zap.tarif != 0 ||
                        zap.tarif_p != 0 ||
                        zap.reval != 0 ||
                        zap.rsum_tarif != 0 ||
                        zap.sum_real != 0 ||
                        zap.sum_tarif != 0 ||
                        zap.sum_tarif_p != 0 ||
                        zap.sum_nedop != 0 ||
                        zap.sum_nedop_p != 0 ||
                        zap.sum_lgota != 0 ||
                        zap.sum_lgota_p != 0 ||
                        zap.sum_dlt_tarif != 0 ||
                        zap.sum_dlt_tarif_p != 0
                    ) ||
                    (
                        (
                            zap.real_charge != 0 ||
                            zap.sum_money != 0 ||
                            zap.sum_insaldo != 0 ||
                            zap.sum_outsaldo != 0 ||
                            zap.sum_charge != 0
                        ) &&
                        Utils.GetParams(finder.groupby, Constants.act_show_saldo.ToString())
                    )
               )
            {
                if (zap.dat_charge == "")
                {
                    list.Insert(position, zap);
                    position = list.Count;
                }
                else
                {
                    if (Convert.ToDateTime(zap.dat_charge) > Convert.ToDateTime(zap.dat_month))
                    {
                        if (ishZap != null) ishZap.has_future_reval = 1;
                    }
                    if (Convert.ToDateTime(zap.dat_charge) < Convert.ToDateTime(zap.dat_month))
                    {
                        if (ishZap != null) ishZap.has_past_reval = 1;
                    }
                    list.Add(zap);
                }
            }

        }

        //----------------------------------------------------------------------
        string Datas(string dat_nam, string dat_s, string dat_po)
        //----------------------------------------------------------------------
        {
            StringBuilder swhere = new StringBuilder();

            if (dat_po != "")
            {
                swhere.Append(" and " + dat_nam + " <= " + dat_po);
                if (dat_s != "") swhere.Append(" and " + dat_nam + " >= " + dat_s);
            }
            else if (dat_s != "") swhere.Append(" and " + dat_nam + " = " + dat_s);

            return swhere.ToString();
        }

        /// <summary> Сформировать условие отбора данных
        /// </summary>
        void WhereStringForFindCommon(ChargeFind finder, string alias, ref string whereString)
        //----------------------------------------------------------------------
        {
            StringBuilder swhere = new StringBuilder();

            if (finder.nzp_kvar > 0)
            {
                swhere.Append(" and " + alias + ".nzp_kvar = " + finder.nzp_kvar.ToString());
            }

            //if (finder.get_koss)
            //    swhere.Append(" and "+alias+".nzp_serv in (25,210,11,242) ");
            if (finder.RolesVal != null)
            {
                if (finder.RolesVal.Count > 0)
                {
                    foreach (_RolesVal role in finder.RolesVal)
                    {
                        if (role.tip == Constants.role_sql)
                        {
                            if (role.kod == Constants.role_sql_serv)
                                swhere.Append(" and " + alias + ".nzp_serv in (" + role.val + ") ");
                            if (role.kod == Constants.role_sql_supp)
                                swhere.Append(" and " + alias + ".nzp_supp in (" + role.val + ") ");
                        }
                    }
                }
            }
            if (finder.is_device != "")
                swhere.Append(" and " + alias + ".is_device = " + finder.is_device);

            whereString += swhere.ToString();
        }

        /// <summary> Сформировать условие отбора данных
        /// </summary>
        void WhereStringForFindServices(ChargeFind finder, string alias, ref string whereString)
        //----------------------------------------------------------------------
        {
            if (finder.nzp_serv <= 0) return;

            StringBuilder swhere = new StringBuilder();

            finder.sum_real = Utils.EFlo0(finder.sum_real.Trim(), "");
            finder.sum_real_po = Utils.EFlo0(finder.sum_real_po.Trim(), "");
            if ((finder.sum_real != "") && (finder.sum_real_po == "" || finder.sum_real == finder.sum_real_po)) swhere.Append(" and " + alias + ".sum_real = " + finder.sum_real);
            else
            {
                if (finder.sum_real != "") swhere.Append(" and " + alias + ".sum_real >= " + finder.sum_real);
                if (finder.sum_real_po != "") swhere.Append(" and " + alias + ".sum_real <= " + finder.sum_real_po);
            }

            finder.sum_charge = Utils.EFlo0(finder.sum_charge.Trim(), "");
            finder.sum_charge_po = Utils.EFlo0(finder.sum_charge_po.Trim(), "");
            if ((finder.sum_charge != "") && (finder.sum_charge_po == "" || finder.sum_charge == finder.sum_charge_po)) swhere.Append(" and " + alias + ".sum_charge = " + finder.sum_charge);
            else
            {
                if (finder.sum_charge != "") swhere.Append(" and " + alias + ".sum_charge >= " + finder.sum_charge);
                if (finder.sum_charge_po != "") swhere.Append(" and " + alias + ".sum_charge <= " + finder.sum_charge_po);
            }

            finder.reval = Utils.EFlo0(finder.reval.Trim(), "");
            finder.reval_po = Utils.EFlo0(finder.reval_po.Trim(), "");
            if ((finder.reval != "") && (finder.reval_po == "" || finder.reval == finder.reval_po)) swhere.Append(" and " + alias + ".reval = " + finder.reval);
            else
            {
                if (finder.reval != "") swhere.Append(" and " + alias + ".reval >= " + finder.reval);
                if (finder.reval_po != "") swhere.Append(" and " + alias + ".reval <= " + finder.reval_po);
            }

            finder.real_charge = Utils.EFlo0(finder.real_charge.Trim(), "");
            finder.real_charge_po = Utils.EFlo0(finder.real_charge_po.Trim(), "");
            if ((finder.real_charge != "") && (finder.real_charge_po == "" || finder.real_charge == finder.real_charge_po)) swhere.Append(" and " + alias + ".real_charge = " + finder.real_charge);
            else
            {
                if (finder.real_charge != "") swhere.Append(" and " + alias + ".real_charge >= " + finder.real_charge);
                if (finder.real_charge_po != "") swhere.Append(" and " + alias + ".real_charge <= " + finder.real_charge_po);
            }

            finder.sum_money = Utils.EFlo0(finder.sum_money.Trim(), "");
            finder.sum_money_po = Utils.EFlo0(finder.sum_money_po.Trim(), "");
            if ((finder.sum_money != "") && (finder.sum_money_po == "" || finder.sum_money == finder.sum_money_po)) swhere.Append(" and " + alias + ".sum_money = " + finder.sum_money);
            else
            {
                if (finder.sum_money != "") swhere.Append(" and " + alias + ".sum_money >= " + finder.sum_money);
                if (finder.sum_money_po != "") swhere.Append(" and " + alias + ".sum_money <= " + finder.sum_money_po);
            }

            finder.sum_insaldo = Utils.EFlo0(finder.sum_insaldo.Trim(), "");
            finder.sum_insaldo_po = Utils.EFlo0(finder.sum_insaldo_po.Trim(), "");
            if ((finder.sum_insaldo != "") && (finder.sum_insaldo_po == "" || finder.sum_insaldo == finder.sum_insaldo_po)) swhere.Append(" and " + alias + ".sum_insaldo = " + finder.sum_insaldo);
            else
            {
                if (finder.sum_insaldo != "") swhere.Append(" and " + alias + ".sum_insaldo >= " + finder.sum_insaldo);
                if (finder.sum_insaldo_po != "") swhere.Append(" and " + alias + ".sum_insaldo <= " + finder.sum_insaldo_po);
            }

            finder.sum_outsaldo = Utils.EFlo0(finder.sum_outsaldo.Trim(), "");
            finder.sum_outsaldo_po = Utils.EFlo0(finder.sum_outsaldo_po.Trim(), "");
            if ((finder.sum_outsaldo != "") && (finder.sum_outsaldo_po == "" || finder.sum_outsaldo == finder.sum_outsaldo_po)) swhere.Append(" and " + alias + ".sum_outsaldo = " + finder.sum_outsaldo);
            else
            {
                if (finder.sum_outsaldo != "") swhere.Append(" and " + alias + ".sum_outsaldo >= " + finder.sum_outsaldo);
                if (finder.sum_outsaldo_po != "") swhere.Append(" and " + alias + ".sum_outsaldo <= " + finder.sum_outsaldo_po);
            }

            finder.tarif = Utils.EFlo0(finder.tarif.Trim(), "");
            finder.tarif_po = Utils.EFlo0(finder.tarif_po.Trim(), "");
            if ((finder.tarif != "") && (finder.tarif_po == "" || finder.tarif == finder.tarif_po)) swhere.Append(" and " + alias + ".tarif = " + finder.tarif);
            else
            {
                if (finder.tarif != "") swhere.Append(" and " + alias + ".tarif >= " + finder.tarif);
                if (finder.tarif_po != "") swhere.Append(" and " + alias + ".tarif <= " + finder.tarif_po);
            }

            finder.rsum_tarif = Utils.EFlo0(finder.rsum_tarif.Trim(), "");
            finder.rsum_tarif_po = Utils.EFlo0(finder.rsum_tarif_po.Trim(), "");
            if ((finder.rsum_tarif != "") && (finder.rsum_tarif_po == "" || finder.rsum_tarif == finder.rsum_tarif_po)) swhere.Append(" and " + alias + ".rsum_tarif = " + finder.rsum_tarif);
            else
            {
                if (finder.rsum_tarif != "") swhere.Append(" and " + alias + ".rsum_tarif >= " + finder.rsum_tarif);
                if (finder.rsum_tarif_po != "") swhere.Append(" and " + alias + ".rsum_tarif <= " + finder.rsum_tarif_po);
            }

            finder.sum_tarif = Utils.EFlo0(finder.sum_tarif.Trim(), "");
            finder.sum_tarif_po = Utils.EFlo0(finder.sum_tarif_po.Trim(), "");
            if ((finder.sum_tarif != "") && (finder.sum_tarif_po == "" || finder.sum_tarif == finder.sum_tarif_po)) swhere.Append(" and " + alias + ".sum_tarif = " + finder.sum_tarif);
            else
            {
                if (finder.sum_tarif != "") swhere.Append(" and " + alias + ".sum_tarif >= " + finder.sum_tarif);
                if (finder.sum_tarif_po != "") swhere.Append(" and " + alias + ".sum_tarif <= " + finder.sum_tarif_po);
            }

            finder.sum_dlt_tarif = Utils.EFlo0(finder.sum_dlt_tarif.Trim(), "");
            finder.sum_dlt_tarif_po = Utils.EFlo0(finder.sum_dlt_tarif_po.Trim(), "");
            if ((finder.sum_dlt_tarif != "") && (finder.sum_dlt_tarif_po == "" || finder.sum_dlt_tarif == finder.sum_dlt_tarif_po)) swhere.Append(" and " + alias + ".sum_dlt_tarif = " + finder.sum_dlt_tarif);
            else
            {
                if (finder.sum_dlt_tarif != "") swhere.Append(" and " + alias + ".sum_dlt_tarif >= " + finder.sum_dlt_tarif);
                if (finder.sum_dlt_tarif_po != "") swhere.Append(" and " + alias + ".sum_dlt_tarif <= " + finder.sum_dlt_tarif_po);
            }

            finder.sum_lgota = Utils.EFlo0(finder.sum_lgota.Trim(), "");
            finder.sum_lgota_po = Utils.EFlo0(finder.sum_lgota_po.Trim(), "");
            if ((finder.sum_lgota != "") && (finder.sum_lgota_po == "" || finder.sum_lgota == finder.sum_lgota_po)) swhere.Append(" and " + alias + ".sum_lgota = " + finder.sum_lgota);
            else
            {
                if (finder.sum_lgota != "") swhere.Append(" and " + alias + ".sum_lgota >= " + finder.sum_lgota);
                if (finder.sum_lgota_po != "") swhere.Append(" and " + alias + ".sum_lgota <= " + finder.sum_lgota_po);
            }

            finder.sum_nedop = Utils.EFlo0(finder.sum_nedop.Trim(), "");
            finder.sum_nedop_po = Utils.EFlo0(finder.sum_nedop_po.Trim(), "");
            if ((finder.sum_nedop != "") && (finder.sum_nedop_po == "" || finder.sum_nedop == finder.sum_nedop_po)) swhere.Append(" and " + alias + ".sum_nedop = " + finder.sum_nedop);
            else
            {
                if (finder.sum_nedop != "") swhere.Append(" and " + alias + ".sum_nedop >= " + finder.sum_nedop);
                if (finder.sum_nedop_po != "") swhere.Append(" and " + alias + ".sum_nedop <= " + finder.sum_nedop_po);
            }

            finder.c_calc = Utils.EFlo0(finder.c_calc.Trim(), "");
            finder.c_calc_po = Utils.EFlo0(finder.c_calc_po.Trim(), "");
            if ((finder.c_calc != "") && (finder.c_calc_po == "" || finder.c_calc == finder.c_calc_po)) swhere.Append(" and " + alias + ".c_calc = " + finder.c_calc);
            else
            {
                if (finder.c_calc != "") swhere.Append(" and " + alias + ".c_calc >= " + finder.c_calc);
                if (finder.c_calc_po != "") swhere.Append(" and " + alias + ".c_calc <= " + finder.c_calc_po);
            }

            if (swhere.Length > 0)
            {
                swhere.Insert(0, " (" + alias + ".nzp_serv = " + finder.nzp_serv.ToString());
                swhere.Append(")");
                if (whereString == "") whereString += swhere.ToString();
                else whereString += " and " + swhere.ToString();
            }
        }

        public class comparerSaldRep : IComparer<SaldoRep>
        {
            public int Compare(SaldoRep x, SaldoRep y)
            {
                if (x == null && y == null)
                    return 0;
                else if (x == null && y != null)
                    return 1;
                else if (x != null && y == null)
                    return -1;
                else
                {
                    int ret = x.adr.CompareTo(y.adr);
                    if (ret == 0) return ret;
                    else
                    {
                        ret = x.fio.CompareTo(y.fio);
                        if (ret == 0) return ret;
                        else if (x.num_ls > y.num_ls) return 1;
                        else if (x.num_ls < y.num_ls) return -1;
                        else return 0;
                    }
                }
            }
        }

        public List<SaldoRep> GetPreparedReport5_20(ChargeFind finder, out Returns ret)
        {
            if (finder.nzp_user <= 0)
            {
                ret = new Returns(false, "Не указан пользователь", -1);
                return null;
            }

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;

            string tXX_rep5_20 = "t" + Convert.ToString(finder.nzp_user) + "_rep5_20";

            if (!TableInWebCashe(conn_web, tXX_rep5_20))
            {
                conn_web.Close();
                ret = new Returns(false, "Данные для отчета не были подготовлены", -1);
                return null;
            }

            List<SaldoRep> list = new List<SaldoRep>();

            string sql = "select count(*) from " + tXX_rep5_20;
            object count = ExecScalar(conn_web, sql, out ret, true);
            int recordsTotalCount;
            try { recordsTotalCount = Convert.ToInt32(count); }
            catch (Exception e)
            {
                MonitorLog.WriteLog("Ошибка GetPreparedReport5_20 " + (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                ret = new Returns(false, "Ошибка при определении количества записей: " + (Constants.Debug ? e.Message : ""));
                conn_web.Close();
                return null;
            }

            IDataReader reader = null;

            sql = "select adr, sum_insaldo, sum_real, sum_money, sum_izm, sum_outsaldo From " + tXX_rep5_20 + " Order by adr";
            ret = ExecRead(conn_web, out reader, sql, true);
            if (!ret.result)
            {
                conn_web.Close();
                return null;
            }

            try
            {
                int i = 0;
                while (reader.Read()) //цикл по ЛС
                {
                    i++;
                    SaldoRep saldoRep = new SaldoRep();
                    if (reader["adr"] != DBNull.Value) saldoRep.adr = Convert.ToString(reader["adr"]).Trim();
                    if (reader["sum_insaldo"] != DBNull.Value) saldoRep.sum_insaldo = Convert.ToDecimal(reader["sum_insaldo"]);
                    if (reader["sum_izm"] != DBNull.Value) saldoRep.reval = Convert.ToDecimal(reader["sum_izm"]);
                    if (reader["sum_real"] != DBNull.Value) saldoRep.sum_real = Convert.ToDecimal(reader["sum_real"]);
                    if (reader["sum_money"] != DBNull.Value) saldoRep.sum_money = Convert.ToDecimal(reader["sum_money"]);
                    if (reader["sum_outsaldo"] != DBNull.Value) saldoRep.sum_outsaldo = Convert.ToDecimal(reader["sum_outsaldo"]);

                    list.Add(saldoRep);

                    if (finder.rows > 0 && i >= finder.rows) break;
                }
                reader.Close();
                conn_web.Close();

                ret.tag = recordsTotalCount;
                return list;
            }
            catch (Exception ex)
            {
                if (reader != null) reader.Close();
                conn_web.Close();

                MonitorLog.WriteLog("Ошибка выполнения отчета 5.20 " + (Constants.Viewerror ? "\n" + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                ret = new Returns(false, "Ошибка выполнения отчета 5.20: " + (Constants.Debug ? ex.Message : ""));
                return null;
            }
        }

        public List<SaldoRep> FillRep(ChargeFind finder, out Returns ret, int num_rep)
        {
            ret = new Returns(false, "", -1);

            if (num_rep == 11)
            {
                //return FillRep_Protokol_odn(finder, out ret);
            }

            return new List<SaldoRep>();

            /*if (finder.nzp_user <= 0)
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = "Не указан пользователь";
                return null;
            }

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;

            string tXX_spls = "t" + Convert.ToString(finder.nzp_user) + "_spls";

            List<SaldoRep> list = new List<SaldoRep>();
            SaldoRep saldoRep;
            string database = "";
            string tablename = "";
            int nzp_kvar;
            string pref = "", new_pref = "";

            string sql = "select count(*) from " + tXX_spls;
            object count = ExecScalar(conn_web, sql, out ret, true);
            int recordsTotalCount;
            try { recordsTotalCount = Convert.ToInt32(count); }
            catch (Exception e)
            {
                ret = new Returns(false, "Ошибка при определении количества записей: " + (Constants.Debug ? e.Message : ""));
                MonitorLog.WriteLog("Ошибка FillRep " + (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                conn_web.Close();
                return null;
            }

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                conn_web.Close();
                return null;
            }

            IDataReader reader = null, reader2 = null;

            sql = "select skip " + finder.skip + " pref, nzp_kvar, adr, fio From " + tXX_spls + " Order by adr, fio";
            ret = ExecRead(conn_web, out reader, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                conn_web.Close();
                return null;
            }

            try
            {
                int i = 0;
                while (reader.Read()) // цикл по ЛС
                {
                     i++;
                    if (reader["pref"] == DBNull.Value) continue;
                    new_pref = (string)reader["pref"].ToString().Trim();
                    if (pref != new_pref) // соединение с БД начислений нового префикса
                    {
                        database = new_pref + "_charge_" + finder.year_.ToString().Substring(2, 2);
                        tablename = "charge_" + ((finder.month_.ToString().Trim().Length == 1) ? "0" + finder.month_.ToString().Trim() : finder.month_.ToString().Trim());
                        ExecSQL(conn_db, "database " + database, true);
                        if (!TableInWebCashe(conn_db, tablename)) continue;
                        pref = new_pref;
                    }

                    saldoRep = new SaldoRep();
                    if (reader["nzp_kvar"] != DBNull.Value) nzp_kvar = (int)reader["nzp_kvar"];
                    else continue;
                    if (reader["adr"] != DBNull.Value) saldoRep.adr = Convert.ToString(reader["adr"]).Trim();
                    if (reader["fio"] != DBNull.Value) saldoRep.fio = Convert.ToString(reader["fio"]).Trim();

                    sql = "Select sum(sum_insaldo) as sum_insaldo" +
                                ", sum(real_charge + reval) as sum_izm" +
                                ", sum(sum_real) as sum_real" +
                                ", sum(sum_money) as sum_money" +
                                ", sum(sum_outsaldo) as sum_outsaldo" +
                            " From " + tablename + " ch" +
                            " Where ch.nzp_kvar = " + nzp_kvar + " and ch.dat_charge is null and ch.nzp_serv > 1";
                        
                    ret = ExecRead(conn_db, out reader2, sql, true);
                    if (!ret.result)
                    {
                        conn_db.Close();
                        conn_web.Close();
                        return null;
                    }
                    
                    if (reader2.Read())
                    {
                        if (reader2["sum_insaldo"] != DBNull.Value) saldoRep.sum_insaldo = Convert.ToDecimal(reader2["sum_insaldo"]);
                        if (reader2["sum_izm"] != DBNull.Value) saldoRep.reval = Convert.ToDecimal(reader2["sum_izm"]);
                        if (reader2["sum_real"] != DBNull.Value) saldoRep.sum_real = Convert.ToDecimal(reader2["sum_real"]);
                        if (reader2["sum_money"] != DBNull.Value) saldoRep.sum_money = Convert.ToDecimal(reader2["sum_money"]);
                        if (reader2["sum_outsaldo"] != DBNull.Value) saldoRep.sum_outsaldo = Convert.ToDecimal(reader2["sum_outsaldo"]);
                        list.Add(saldoRep);
                    }
                    reader2.Close();

                    if (finder.rows > 0 && i >= finder.rows) break;
                }
                reader.Close();
                conn_db.Close();
                conn_web.Close();

                ret.tag = recordsTotalCount;
                return list;
            }
            catch (Exception ex)
            {
                if (reader != null) reader.Close();
                if (reader2 != null) reader2.Close();
                conn_db.Close();
                conn_web.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка выполнения отчета 5.20 " + err, MonitorLog.typelog.Error, 20, 201, true);

                return null;
            }*/
        }//FillRep

        //----------------------------------------------------------------------
        public _RecordSzFin GetCalcSz(ChargeFind finder, out Returns ret) //вытащить cevvs к перечислению из Кэш-БД
        //----------------------------------------------------------------------
        {
            #region инициализация
            ret = Utils.InitReturns();
            _RecordSzFin zap = new _RecordSzFin();

            zap.ls_edv = 0;
            zap.ls_lgota = 0;
            zap.ls_smo = 0;
            zap.ls_teplo = 0;
            #endregion

            return zap;

        }//GetCalcSz


        /// <summary> Сформировать условие отбора записей для поиска ЛС, домов и т.д.
        /// </summary>
        public string MakeWhereString(List<ChargeFind> finder, out Returns ret, enDopFindType tip)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            string whereString = "";
            string alias = "chrg";
            ChargeFind charge;
            for (var i = 0; i < finder.Count; i++)
            {
                charge = finder[i];
                WhereStringForFindServices(charge, alias, ref whereString);
            }

            if (whereString == "") return "";

            whereString = String.Format(" and ({0}) ", whereString);

            if (finder != null && finder.Count > 0)
                WhereStringForFindCommon(finder[0], alias, ref whereString);

            StringBuilder sql = new StringBuilder();
            int year = finder[0].year_ % 100;
            int month = finder[0].month_;

            string y = "", m = "";
            if (year <= 0) y = "CYEAR";
            else y = year.ToString("00");

            if (month <= 0) m = "CMONTH";
            else m = month.ToString("00");

         //   if (year <= 0 || month <= 0) return "";

#if PG
            sql.Append(" Select 1 From PREFX_charge_"+y+".charge_"+m+" " + alias);
#else
            sql.Append(" Select count(*) From PREFX_charge_"+y+":charge_"+m+" " + alias);
#endif
            sql.Append(" Where " + alias + ".dat_charge is null ");

            switch (tip)
            {
                case enDopFindType.dft_CntKvar:
                    sql.Append(" and " + alias + ".nzp_kvar = k.nzp_kvar ");
                    sql.Append(whereString);
                    return sql.ToString();
                case enDopFindType.dft_CntDom:
                    sql.Append(" and " + alias + ".nzp_dom = d.nzp_dom ");
                    sql.Append(whereString);
                    return sql.ToString();
            }
            return "";
        }

        public string MakeWhereString2(List<ChargeFind> finder, out Returns ret, enDopFindType tip)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            string whereString = "";
            string alias = "chrg";

            string services = "";
            for (var i = 0; i < finder.Count; i++)
            {
                if (services != "") services += ", " + finder[i].nzp_serv;
                else services += finder[i].nzp_serv;
            }
       
            StringBuilder sql = new StringBuilder();
            int year = finder[0].year_ % 100;
            int month = finder[0].month_;

            string y = "", m = "";
            if (year <= 0) y = "CYEAR";
            else y = year.ToString("00");

            if (month <= 0) m = "CMONTH";
            else m = month.ToString("00");

            sql.Append(" not exists (select 1 from PREFX_charge_" + y + tableDelimiter + "charge_" + m + " " + alias);
            sql.Append(" where 1=1 ");
            switch (tip)
            {
                case enDopFindType.dft_CntKvar:
                    sql.Append(" and " + alias + ".nzp_kvar = k.nzp_kvar "); break;
                case enDopFindType.dft_CntDom:
                    sql.Append(" and " + alias + ".nzp_dom = d.nzp_dom ");  break;
            }
            sql.Append(" and " + alias + ".dat_charge is null ");
            if (services != "") sql.Append(" and " + alias + ".nzp_serv not in(" + services + ") and " + alias + ".nzp_serv > 1 ");
            sql.Append(" and (" + alias + ".sum_real <> 0 or " + alias + ".reval <>0  or " + alias + ".rsum_tarif <>0 or " + alias + ".tarif <> 0  ");
            sql.Append(" or " + alias + ".sum_nedop<> 0  or " + alias + ".sum_lgota <>0  or " + alias + ".sum_dlt_tarif <> 0  ");
            sql.Append(" or " + alias + ".sum_insaldo <>0  or " + alias + ".real_charge<>0  or " + alias + ".sum_money <> 0 ");
            sql.Append(" or " + alias + ".sum_outsaldo<> 0 or " + alias + ".sum_charge <>0))");

          
            return sql.ToString();
        }

        public List<Charge> GetChargeStatisticsSupp(ChargeFind finder, out Returns ret)
        {
            #region Проверка входных данных
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Не определен пользователь");
                return null;
            }
            if (finder.year_ < 1 || finder.month_ < 1)
            {
                ret = new Returns(false, "Не определен расчетный месяц");
                return null;
            }

            int m1, m2, y1, y2;
            if (finder.year_po < 1 || finder.month_po < 1)
            {
                m1 = m2 = finder.month_;
                y1 = y2 = finder.year_;
            }
            else
            {
                m1 = finder.month_;
                y1 = finder.year_;
                m2 = finder.month_po;
                y2 = finder.year_po;
            }
            #endregion

            if (finder.pref == "") finder.pref = Points.Pref;

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;

#if PG
            string tXXUkRguCharge = "public.t" + Convert.ToString(finder.nzp_user) + "_ukrgucharge";
#else
            string tXXUkRguCharge = "t" + Convert.ToString(finder.nzp_user) + "_ukrgucharge";
#endif

            //проверка на существование таблицы в базе
            if (!TempTableInWebCashe(conn_web, tXXUkRguCharge))
            {
                ret.tag = -1;
                ret.text = "Статистика по начислениям не рассчитана";
                ret.result = false;
                return null;
            }

            // составные части запроса
            string select = "", s = "";
            string sqlItogo = "Select";
            s = " sum(case when year_ = " + y1 + " and month_ = " + m1 + " then sum_insaldo else 0 end) as sum_insaldo" +
                ", sum(case when year_ = " + y1 + " and month_ = " + m1 + " then sum_insaldo_k else 0 end) as sum_insaldo_k" +
                ", sum(case when year_ = " + y1 + " and month_ = " + m1 + " then sum_insaldo_d else 0 end) as sum_insaldo_d" +
                ", sum(case when year_ = " + y2 + " and month_ = " + m2 + " then sum_outsaldo else 0 end) as sum_outsaldo" +
                ", sum(case when year_ = " + y2 + " and month_ = " + m2 + " then sum_outsaldo_k else 0 end) as sum_outsaldo_k" +
                ", sum(case when year_ = " + y2 + " and month_ = " + m2 + " then sum_outsaldo_d else 0 end) as sum_outsaldo_d";
            if (Utils.GetParams(finder.groupby, Constants.act_groupby_month.ToString()))
                select += " sum(sum_insaldo) as sum_insaldo" +
                    ", sum(sum_insaldo_k) as sum_insaldo_k" +
                    ", sum(sum_insaldo_d) as sum_insaldo_d" +
                    ", sum(sum_outsaldo) as sum_outsaldo" +
                    ", sum(sum_outsaldo_k) as sum_outsaldo_k" +
                    ", sum(sum_outsaldo_d) as sum_outsaldo_d";
            else select += s;
            sqlItogo += s;
            s = ", sum(rsum_tarif) as rsum_tarif, sum(sum_nedop) as sum_nedop, sum(sum_tarif) as sum_tarif, sum(real_charge) as real_charge" +
                ", sum(reval) as reval, sum(reval_k) as reval_k, sum(reval_d) as reval_d, sum(sum_money) as sum_money, sum(sum_nach) as sum_nach" +
                ", sum(real_charge_k) as real_charge_k, sum(real_charge_d) as real_charge_d " +
                ", sum(case when year_ = " + y2 + " and month_ = " + m2 + " then sum_outsaldo else 0 end) as sum_outsaldo" +
                ", sum(case when year_ = " + y2 + " and month_ = " + m2 + " then sum_outsaldo_k else 0 end) as sum_outsaldo_k" +
                ", sum(case when year_ = " + y2 + " and month_ = " + m2 + " then sum_outsaldo_d else 0 end) as sum_outsaldo_d";
            select += s;
            sqlItogo += s;
            string fields = ", nzp_area, area, nzp_geu, geu,nzp_payer_agent, max(agent) agent, nzp_payer_princip, max(princip) princip, nzp_payer_supp, max(supp) supp, nzp_serv, service, ordering, nzp_supp, name_supp";
            string from = " From " + tXXUkRguCharge;
            sqlItogo += from;
            string groupBy = " Group by nzp_area, area, nzp_geu, geu,nzp_payer_agent, nzp_payer_princip, nzp_payer_supp, nzp_serv, service, ordering, nzp_supp, name_supp";
            string having = " Having sum(rsum_tarif) <> 0 or sum(sum_nedop) <> 0 or sum(sum_tarif) <> 0 or sum(real_charge) <> 0 or sum(reval) <> 0 or sum(sum_money) <> 0 or sum(sum_nach) <> 0";
            if (Utils.GetParams(finder.groupby, Constants.act_groupby_month.ToString()))
                having += " or sum(sum_insaldo) <> 0 or sum(sum_insaldo_k) <> 0 or sum(sum_insaldo_d) <> 0 or sum(sum_outsaldo) <> 0 or sum(sum_outsaldo_k) <> 0 or sum(sum_outsaldo_d) <> 0";
            else
                having += " or sum(case when year_ = " + y1 + " and month_ = " + m1 + " then sum_insaldo else 0 end) <> 0" +
                    " or sum(case when year_ = " + y1 + " and month_ = " + m1 + " then sum_insaldo_k else 0 end) <> 0" +
                    " or sum(case when year_ = " + y1 + " and month_ = " + m1 + " then sum_insaldo_d else 0 end) <> 0" +
                    " or sum(case when year_ = " + y2 + " and month_ = " + m2 + " then sum_outsaldo else 0 end) <> 0" +
                    " or sum(case when year_ = " + y2 + " and month_ = " + m2 + " then sum_outsaldo_k else 0 end) <> 0" +
                    " or sum(case when year_ = " + y2 + " and month_ = " + m2 + " then sum_outsaldo_d else 0 end) <> 0";
            string orderBy = " {order_by} {order0}{order1}{order2}{order3}{order4}{order5}{order6}{order7}";

            if (Utils.GetParams(finder.groupby, Constants.act_groupby_month.ToString()))
            {
                fields += ", year_, month_";
                groupBy += ", year_, month_";
            }

            int sequenceNumber;
            if (Utils.GetParams(finder.groupby, Constants.act_groupby_month.ToString(), out sequenceNumber)) orderBy = orderBy.Replace("{order" + sequenceNumber + "}", ", year_, month_");
            if (Utils.GetParams(finder.groupby, Constants.act_groupby_area.ToString(), out sequenceNumber)) orderBy = orderBy.Replace("{order" + sequenceNumber + "}", ", area");
            if (Utils.GetParams(finder.groupby, Constants.act_groupby_service.ToString(), out sequenceNumber)) orderBy = orderBy.Replace("{order" + sequenceNumber + "}", ", ordering");
            if (Utils.GetParams(finder.groupby, Constants.act_groupby_supplier.ToString(), out sequenceNumber)) orderBy = orderBy.Replace("{order" + sequenceNumber + "}", ", name_supp");
            if (Utils.GetParams(finder.groupby, Constants.act_groupby_agent.ToString(), out sequenceNumber)) orderBy = orderBy.Replace("{order" + sequenceNumber + "}", ", agent");
            if (Utils.GetParams(finder.groupby, Constants.act_groupby_princip.ToString(), out sequenceNumber)) orderBy = orderBy.Replace("{order" + sequenceNumber + "}", ", princip");
            if (Utils.GetParams(finder.groupby, Constants.act_groupby_supp.ToString(), out sequenceNumber)) orderBy = orderBy.Replace("{order" + sequenceNumber + "}", ", supp");
            if (Utils.GetParams(finder.groupby, Constants.act_groupby_geu.ToString(), out sequenceNumber)) orderBy = orderBy.Replace("{order" + sequenceNumber + "}", ", geu");

            orderBy = orderBy.Replace("{order0}", "")
                .Replace("{order1}", "")
                .Replace("{order2}", "")
                .Replace("{order3}", "")
                .Replace("{order4}", "")
                .Replace("{order5}", "")
                .Replace("{order6}", "")
                .Replace("{order7}", "")
                .Replace("{order_by} ,", "Order by ")
                .Replace("{order_by}", "");

            #region определить количество записей
            ExecSQL(conn_web, "drop table tmp_rgucharge", false);

#if PG
            string sql = "select nzp_area into temp tmp_rgucharge " + from + groupBy + having;
#else
            string sql = "select nzp_area" + from + groupBy + having + " into temp tmp_rgucharge";
#endif
            ret = ExecSQL(conn_web, sql, true);
            if (!ret.result)
            {
                conn_web.Close();
                return null;
            }
            sql = "select count(*) from tmp_rgucharge";
            object count = ExecScalar(conn_web, sql, out ret, true);
            int recordsTotalCount;
            try { recordsTotalCount = Convert.ToInt32(count); }
            catch (Exception e)
            {
                ret = new Returns(false, "Ошибка при определении количества записей: " + (Constants.Debug ? e.Message : ""));
                MonitorLog.WriteLog("Ошибка GetChargeStatistics " + (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                conn_web.Close();
                return null;
            }

            ExecSQL(conn_web, "drop table tmp_rgucharge", false);
            #endregion

            #region сформировать список
#if PG
            sql = "Select " + select + fields + from + groupBy + having + orderBy + " offset " + finder.skip;
#else
            sql = "Select skip " + finder.skip + select + fields + from + groupBy + having + orderBy;
#endif

            IDataReader reader;
            ret = ExecRead(conn_web, out reader, sql.ToString(), true);
            if (!ret.result)
            {
                conn_web.Close();
                return null;
            }

            List<Charge> list = new List<Charge>();

            try
            {
                int i = finder.skip;
                while (reader.Read())
                {
                    i++;
                    Charge zap = new Charge();

                    zap.num = i.ToString();

                    if (Utils.GetParams(finder.groupby, Constants.act_groupby_month.ToString()))
                    {
                        if (reader["year_"] != DBNull.Value) zap.year_ = (int)reader["year_"];
                        if (reader["month_"] != DBNull.Value) zap.month_ = (int)reader["month_"];
                        zap.dat_month = zap.YM.name;
                    }

                    if (reader["nzp_area"] != DBNull.Value) zap.nzp_area = (int)reader["nzp_area"];
                    if (reader["area"] != DBNull.Value) zap.area = ((string)reader["area"]).Trim();
                    if (Utils.GetParams(finder.groupby, Constants.act_groupby_agent.ToString()))
                    {
                        if (reader["nzp_payer_agent"] != DBNull.Value)
                            zap.nzp_payer_agent = (int) reader["nzp_payer_agent"];
                        if (reader["agent"] != DBNull.Value) zap.agent = ((string) reader["agent"]).Trim();
                    }
                    if (Utils.GetParams(finder.groupby, Constants.act_groupby_princip.ToString()))
                    {
                        if (reader["nzp_payer_princip"] != DBNull.Value)
                            zap.nzp_payer_princip = (int) reader["nzp_payer_princip"];
                        if (reader["princip"] != DBNull.Value) zap.princip = ((string) reader["princip"]).Trim();
                    }
                    if (Utils.GetParams(finder.groupby, Constants.act_groupby_supp.ToString()))
                    {
                        if (reader["nzp_payer_supp"] != DBNull.Value)
                            zap.nzp_payer_supp = (int) reader["nzp_payer_supp"];
                        if (reader["supp"] != DBNull.Value) zap.supp = ((string) reader["supp"]).Trim();
                    }
                    if (reader["nzp_serv"] != DBNull.Value) zap.nzp_serv = (int)reader["nzp_serv"];
                    if (reader["service"] != DBNull.Value) zap.service = ((string)reader["service"]).Trim();
                    if (reader["ordering"] != DBNull.Value) zap.ordering = (int)reader["ordering"];

                    if (reader["nzp_supp"] != DBNull.Value) zap.nzp_supp = (int)reader["nzp_supp"];
                    if (reader["name_supp"] != DBNull.Value) zap.supplier = ((string)reader["name_supp"]).Trim();

                    if (reader["nzp_geu"] != DBNull.Value) zap.nzp_geu = (int)reader["nzp_geu"];
                    if (reader["geu"] != DBNull.Value) zap.geu = ((string)reader["geu"]).Trim();

                    if (reader["sum_insaldo"] != DBNull.Value) zap.sum_insaldo = Convert.ToDecimal(reader["sum_insaldo"]);
                    if (reader["sum_insaldo_k"] != DBNull.Value) zap.sum_insaldo_k = Convert.ToDecimal(reader["sum_insaldo_k"]);
                    if (reader["sum_insaldo_d"] != DBNull.Value) zap.sum_insaldo_d = Convert.ToDecimal(reader["sum_insaldo_d"]);
                    if (reader["rsum_tarif"] != DBNull.Value) zap.rsum_tarif = Convert.ToDecimal(reader["rsum_tarif"]);
                    if (reader["sum_nedop"] != DBNull.Value) zap.sum_nedop = Convert.ToDecimal(reader["sum_nedop"]);
                    if (reader["sum_tarif"] != DBNull.Value) zap.sum_tarif = Convert.ToDecimal(reader["sum_tarif"]);
                    if (reader["real_charge"] != DBNull.Value) zap.real_charge = Convert.ToDecimal(reader["real_charge"]);
                    if (reader["reval"] != DBNull.Value) zap.reval = Convert.ToDecimal(reader["reval"]);
                    if (reader["sum_money"] != DBNull.Value) zap.sum_money = Convert.ToDecimal(reader["sum_money"]);
                    if (reader["sum_nach"] != DBNull.Value) zap.sum_charge = Convert.ToDecimal(reader["sum_nach"]);
                    if (reader["sum_outsaldo"] != DBNull.Value) zap.sum_outsaldo = Convert.ToDecimal(reader["sum_outsaldo"]);
                    if (reader["reval_k"] != DBNull.Value) zap.reval_otr = Convert.ToDecimal(reader["reval_k"]);
                    if (reader["reval_d"] != DBNull.Value) zap.reval_pol = Convert.ToDecimal(reader["reval_d"]);
                    if (reader["sum_outsaldo_k"] != DBNull.Value) zap.sum_outsaldo_k = Convert.ToDecimal(reader["sum_outsaldo_k"]);
                    if (reader["sum_outsaldo_d"] != DBNull.Value) zap.sum_outsaldo_d = Convert.ToDecimal(reader["sum_outsaldo_d"]);

                    list.Add(zap);

                    if (finder.rows > 0 && list.Count >= finder.rows) break;
                }
            }
            catch (Exception e)
            {
                ret = new Returns(false, "Ошибка загрузки статистики начислений:" + (Constants.Debug ? e.Message : ""));
                MonitorLog.WriteLog("Ошибка GetChargeStatistics " + (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                reader.Close();
                conn_web.Close();
                return null;
            }

            reader.Close();
            #endregion

            #region сформировать строку итого
            sql = sqlItogo;

            ret = ExecRead(conn_web, out reader, sql.ToString(), true);
            if (!ret.result)
            {
                conn_web.Close();
                return null;
            }

            try
            {
                Charge zap = new Charge();
                zap.num = "Итого";
                if (reader.Read())
                {
                    if (reader["sum_insaldo"] != DBNull.Value) zap.sum_insaldo = Convert.ToDecimal(reader["sum_insaldo"]);
                    if (reader["sum_insaldo_k"] != DBNull.Value) zap.sum_insaldo_k = Convert.ToDecimal(reader["sum_insaldo_k"]);
                    if (reader["sum_insaldo_d"] != DBNull.Value) zap.sum_insaldo_d = Convert.ToDecimal(reader["sum_insaldo_d"]);
                    if (reader["rsum_tarif"] != DBNull.Value) zap.rsum_tarif = Convert.ToDecimal(reader["rsum_tarif"]);
                    if (reader["sum_nedop"] != DBNull.Value) zap.sum_nedop = Convert.ToDecimal(reader["sum_nedop"]);
                    if (reader["sum_tarif"] != DBNull.Value) zap.sum_tarif = Convert.ToDecimal(reader["sum_tarif"]);
                    if (reader["real_charge"] != DBNull.Value) zap.real_charge = Convert.ToDecimal(reader["real_charge"]);
                    if (reader["reval"] != DBNull.Value) zap.reval = Convert.ToDecimal(reader["reval"]);
                    if (reader["sum_money"] != DBNull.Value) zap.sum_money = Convert.ToDecimal(reader["sum_money"]);
                    if (reader["sum_nach"] != DBNull.Value) zap.sum_charge = Convert.ToDecimal(reader["sum_nach"]);
                    if (reader["sum_outsaldo"] != DBNull.Value) zap.sum_outsaldo = Convert.ToDecimal(reader["sum_outsaldo"]);
                    if (reader["sum_outsaldo_k"] != DBNull.Value) zap.sum_outsaldo_k = Convert.ToDecimal(reader["sum_outsaldo_k"]);
                    if (reader["sum_outsaldo_d"] != DBNull.Value) zap.sum_outsaldo_d = Convert.ToDecimal(reader["sum_outsaldo_d"]);
                }
                list.Add(zap);
            }
            catch (Exception e)
            {
                ret = new Returns(false, "Ошибка загрузки статистики начислений:" + (Constants.Debug ? e.Message : ""));
                MonitorLog.WriteLog("Ошибка GetChargeStatistics " + (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                reader.Close();
                conn_web.Close();
                return null;
            }
            reader.Close();
            #endregion

            conn_web.Close();
            ret.tag = recordsTotalCount;
            return list;
        }

        public List<Charge> GetChargeStatistics(ChargeFind finder, out Returns ret)
        {
            #region Проверка входных данных
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Не определен пользователь");
                return null;
            }
            if (finder.year_ < 1 || finder.month_ < 1)
            {
                ret = new Returns(false, "Не определен расчетный месяц");
                return null;
            }

            int m1, m2, y1, y2;
            if (finder.year_po < 1 || finder.month_po < 1)
            {
                m1 = m2 = finder.month_;
                y1 = y2 = finder.year_;
            }
            else
            {
                m1 = finder.month_;
                y1 = finder.year_;
                m2 = finder.month_po;
                y2 = finder.year_po;
            }
            #endregion

            if (finder.pref == "") finder.pref = Points.Pref;

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;

#if PG
            string tXXUkRguCharge = "public.t" + Convert.ToString(finder.nzp_user) + "_ukrgucharge";
#else
            string tXXUkRguCharge = "t" + Convert.ToString(finder.nzp_user) + "_ukrgucharge";
#endif

            //проверка на существование таблицы в базе
            if (!TempTableInWebCashe(conn_web, tXXUkRguCharge))
            {
                ret.tag = -1;
                ret.text = "Статистика по начислениям не рассчитана";
                ret.result = false;
                return null;
            }

            // составные части запроса
            string select = "", s = "";
            string sqlItogo = "Select";
            s = " sum(case when year_ = " + y1 + " and month_ = " + m1 + " then sum_insaldo else 0 end) as sum_insaldo" +
                ", sum(case when year_ = " + y1 + " and month_ = " + m1 + " then sum_insaldo_k else 0 end) as sum_insaldo_k" +
                ", sum(case when year_ = " + y1 + " and month_ = " + m1 + " then sum_insaldo_d else 0 end) as sum_insaldo_d" +
                ", sum(case when year_ = " + y2 + " and month_ = " + m2 + " then sum_outsaldo else 0 end) as sum_outsaldo" +
                ", sum(case when year_ = " + y2 + " and month_ = " + m2 + " then sum_outsaldo_k else 0 end) as sum_outsaldo_k" +
                ", sum(case when year_ = " + y2 + " and month_ = " + m2 + " then sum_outsaldo_d else 0 end) as sum_outsaldo_d";
            if (Utils.GetParams(finder.groupby, Constants.act_groupby_month.ToString()))
                select += " sum(sum_insaldo) as sum_insaldo" +
                    ", sum(sum_insaldo_k) as sum_insaldo_k" +
                    ", sum(sum_insaldo_d) as sum_insaldo_d" +
                    ", sum(sum_outsaldo) as sum_outsaldo" +
                    ", sum(sum_outsaldo_k) as sum_outsaldo_k" +
                    ", sum(sum_outsaldo_d) as sum_outsaldo_d";
            else select += s;
            sqlItogo += s;
            s = ", sum(rsum_tarif) as rsum_tarif, sum(sum_nedop) as sum_nedop, sum(sum_tarif) as sum_tarif, sum(real_charge) as real_charge" +
                ", sum(reval) as reval, sum(reval_k) as reval_k, sum(reval_d) as reval_d, sum(sum_money) as sum_money, sum(sum_nach) as sum_nach" +
                ", sum(real_charge_k) as real_charge_k, sum(real_charge_d) as real_charge_d " +
                ", sum(case when year_ = " + y2 + " and month_ = " + m2 + " then sum_outsaldo else 0 end) as sum_outsaldo" +
                ", sum(case when year_ = " + y2 + " and month_ = " + m2 + " then sum_outsaldo_k else 0 end) as sum_outsaldo_k" +
                ", sum(case when year_ = " + y2 + " and month_ = " + m2 + " then sum_outsaldo_d else 0 end) as sum_outsaldo_d";
            select += s;
            sqlItogo += s;
            string fields = ", nzp_area, area, nzp_geu, geu, nzp_serv, service, ordering, nzp_supp, name_supp";
            string from = " From " + tXXUkRguCharge;
            sqlItogo += from;
            string groupBy = " Group by nzp_area, area, nzp_geu, geu, nzp_serv, service, ordering, nzp_supp, name_supp";
            string having = " Having sum(rsum_tarif) <> 0 or sum(sum_nedop) <> 0 or sum(sum_tarif) <> 0 or sum(real_charge) <> 0 or sum(reval) <> 0 or sum(sum_money) <> 0 or sum(sum_nach) <> 0";
            if (Utils.GetParams(finder.groupby, Constants.act_groupby_month.ToString()))
                having += " or sum(sum_insaldo) <> 0 or sum(sum_insaldo_k) <> 0 or sum(sum_insaldo_d) <> 0 or sum(sum_outsaldo) <> 0 or sum(sum_outsaldo_k) <> 0 or sum(sum_outsaldo_d) <> 0";
            else
                having += " or sum(case when year_ = " + y1 + " and month_ = " + m1 + " then sum_insaldo else 0 end) <> 0" +
                    " or sum(case when year_ = " + y1 + " and month_ = " + m1 + " then sum_insaldo_k else 0 end) <> 0" +
                    " or sum(case when year_ = " + y1 + " and month_ = " + m1 + " then sum_insaldo_d else 0 end) <> 0" +
                    " or sum(case when year_ = " + y2 + " and month_ = " + m2 + " then sum_outsaldo else 0 end) <> 0" +
                    " or sum(case when year_ = " + y2 + " and month_ = " + m2 + " then sum_outsaldo_k else 0 end) <> 0" +
                    " or sum(case when year_ = " + y2 + " and month_ = " + m2 + " then sum_outsaldo_d else 0 end) <> 0";
            string orderBy = " {order_by} {order0}{order1}{order2}{order3}{order4}";

            if (Utils.GetParams(finder.groupby, Constants.act_groupby_month.ToString()))
            {
                fields += ", year_, month_";
                groupBy += ", year_, month_";
            }

            int sequenceNumber;
            if (Utils.GetParams(finder.groupby, Constants.act_groupby_month.ToString(), out sequenceNumber)) orderBy = orderBy.Replace("{order" + sequenceNumber + "}", ", year_, month_");
            if (Utils.GetParams(finder.groupby, Constants.act_groupby_area.ToString(), out sequenceNumber)) orderBy = orderBy.Replace("{order" + sequenceNumber + "}", ", area");
            if (Utils.GetParams(finder.groupby, Constants.act_groupby_service.ToString(), out sequenceNumber)) orderBy = orderBy.Replace("{order" + sequenceNumber + "}", ", ordering");
            if (Utils.GetParams(finder.groupby, Constants.act_groupby_supplier.ToString(), out sequenceNumber)) orderBy = orderBy.Replace("{order" + sequenceNumber + "}", ", name_supp");
            if (Utils.GetParams(finder.groupby, Constants.act_groupby_geu.ToString(), out sequenceNumber)) orderBy = orderBy.Replace("{order" + sequenceNumber + "}", ", geu");

            orderBy = orderBy.Replace("{order0}", "")
                .Replace("{order1}", "")
                .Replace("{order2}", "")
                .Replace("{order3}", "")
                .Replace("{order4}", "")
                .Replace("{order_by} ,", "Order by ")
                .Replace("{order_by}", "");

            #region определить количество записей
            ExecSQL(conn_web, "drop table tmp_rgucharge", false);

#if PG
            string sql = "select nzp_area into temp tmp_rgucharge " + from + groupBy + having;
#else
            string sql = "select nzp_area" + from + groupBy + having + " into temp tmp_rgucharge";
#endif
            ret = ExecSQL(conn_web, sql, true);
            if (!ret.result)
            {
                conn_web.Close();
                return null;
            }
            sql = "select count(*) from tmp_rgucharge";
            object count = ExecScalar(conn_web, sql, out ret, true);
            int recordsTotalCount;
            try { recordsTotalCount = Convert.ToInt32(count); }
            catch (Exception e)
            {
                ret = new Returns(false, "Ошибка при определении количества записей: " + (Constants.Debug ? e.Message : ""));
                MonitorLog.WriteLog("Ошибка GetChargeStatistics " + (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                conn_web.Close();
                return null;
            }

            ExecSQL(conn_web, "drop table tmp_rgucharge", false);
            #endregion

            #region сформировать список
#if PG
            sql = "Select " + select + fields + from + groupBy + having + orderBy + " offset " + finder.skip;
#else
            sql = "Select skip " + finder.skip + select + fields + from + groupBy + having + orderBy;
#endif

            IDataReader reader;
            ret = ExecRead(conn_web, out reader, sql.ToString(), true);
            if (!ret.result)
            {
                conn_web.Close();
                return null;
            }

            List<Charge> list = new List<Charge>();

            try
            {
                int i = finder.skip;
                while (reader.Read())
                {
                    i++;
                    Charge zap = new Charge();

                    zap.num = i.ToString();

                    if (Utils.GetParams(finder.groupby, Constants.act_groupby_month.ToString()))
                    {
                        if (reader["year_"] != DBNull.Value) zap.year_ = (int)reader["year_"];
                        if (reader["month_"] != DBNull.Value) zap.month_ = (int)reader["month_"];
                        zap.dat_month = zap.YM.name;
                    }

                    if (reader["nzp_area"] != DBNull.Value) zap.nzp_area = (int)reader["nzp_area"];
                    if (reader["area"] != DBNull.Value) zap.area = ((string)reader["area"]).Trim();

                    if (reader["nzp_serv"] != DBNull.Value) zap.nzp_serv = (int)reader["nzp_serv"];
                    if (reader["service"] != DBNull.Value) zap.service = ((string)reader["service"]).Trim();
                    if (reader["ordering"] != DBNull.Value) zap.ordering = (int)reader["ordering"];

                    if (reader["nzp_supp"] != DBNull.Value) zap.nzp_supp = (int)reader["nzp_supp"];
                    if (reader["name_supp"] != DBNull.Value) zap.supplier = ((string)reader["name_supp"]).Trim();

                    if (reader["nzp_geu"] != DBNull.Value) zap.nzp_geu = (int)reader["nzp_geu"];
                    if (reader["geu"] != DBNull.Value) zap.geu = ((string)reader["geu"]).Trim();

                    if (reader["sum_insaldo"] != DBNull.Value) zap.sum_insaldo = Convert.ToDecimal(reader["sum_insaldo"]);
                    if (reader["sum_insaldo_k"] != DBNull.Value) zap.sum_insaldo_k = Convert.ToDecimal(reader["sum_insaldo_k"]);
                    if (reader["sum_insaldo_d"] != DBNull.Value) zap.sum_insaldo_d = Convert.ToDecimal(reader["sum_insaldo_d"]);
                    if (reader["rsum_tarif"] != DBNull.Value) zap.rsum_tarif = Convert.ToDecimal(reader["rsum_tarif"]);
                    if (reader["sum_nedop"] != DBNull.Value) zap.sum_nedop = Convert.ToDecimal(reader["sum_nedop"]);
                    if (reader["sum_tarif"] != DBNull.Value) zap.sum_tarif = Convert.ToDecimal(reader["sum_tarif"]);
                    if (reader["real_charge"] != DBNull.Value) zap.real_charge = Convert.ToDecimal(reader["real_charge"]);
                    if (reader["reval"] != DBNull.Value) zap.reval = Convert.ToDecimal(reader["reval"]);
                    if (reader["sum_money"] != DBNull.Value) zap.sum_money = Convert.ToDecimal(reader["sum_money"]);
                    if (reader["sum_nach"] != DBNull.Value) zap.sum_charge = Convert.ToDecimal(reader["sum_nach"]);
                    if (reader["sum_outsaldo"] != DBNull.Value) zap.sum_outsaldo = Convert.ToDecimal(reader["sum_outsaldo"]);
                    if (reader["reval_k"] != DBNull.Value) zap.reval_otr = Convert.ToDecimal(reader["reval_k"]);
                    if (reader["reval_d"] != DBNull.Value) zap.reval_pol = Convert.ToDecimal(reader["reval_d"]);
                    if (reader["sum_outsaldo_k"] != DBNull.Value) zap.sum_outsaldo_k = Convert.ToDecimal(reader["sum_outsaldo_k"]);
                    if (reader["sum_outsaldo_d"] != DBNull.Value) zap.sum_outsaldo_d = Convert.ToDecimal(reader["sum_outsaldo_d"]);

                    list.Add(zap);

                    if (finder.rows > 0 && list.Count >= finder.rows) break;
                }
            }
            catch (Exception e)
            {
                ret = new Returns(false, "Ошибка загрузки статистики начислений:" + (Constants.Debug ? e.Message : ""));
                MonitorLog.WriteLog("Ошибка GetChargeStatistics " + (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                reader.Close();
                conn_web.Close();
                return null;
            }

            reader.Close();
            #endregion

            #region сформировать строку итого
            sql = sqlItogo;

            ret = ExecRead(conn_web, out reader, sql.ToString(), true);
            if (!ret.result)
            {
                conn_web.Close();
                return null;
            }

            try
            {
                Charge zap = new Charge();
                zap.num = "Итого";
                if (reader.Read())
                {
                    if (reader["sum_insaldo"] != DBNull.Value) zap.sum_insaldo = Convert.ToDecimal(reader["sum_insaldo"]);
                    if (reader["sum_insaldo_k"] != DBNull.Value) zap.sum_insaldo_k = Convert.ToDecimal(reader["sum_insaldo_k"]);
                    if (reader["sum_insaldo_d"] != DBNull.Value) zap.sum_insaldo_d = Convert.ToDecimal(reader["sum_insaldo_d"]);
                    if (reader["rsum_tarif"] != DBNull.Value) zap.rsum_tarif = Convert.ToDecimal(reader["rsum_tarif"]);
                    if (reader["sum_nedop"] != DBNull.Value) zap.sum_nedop = Convert.ToDecimal(reader["sum_nedop"]);
                    if (reader["sum_tarif"] != DBNull.Value) zap.sum_tarif = Convert.ToDecimal(reader["sum_tarif"]);
                    if (reader["real_charge"] != DBNull.Value) zap.real_charge = Convert.ToDecimal(reader["real_charge"]);
                    if (reader["reval"] != DBNull.Value) zap.reval = Convert.ToDecimal(reader["reval"]);
                    if (reader["sum_money"] != DBNull.Value) zap.sum_money = Convert.ToDecimal(reader["sum_money"]);
                    if (reader["sum_nach"] != DBNull.Value) zap.sum_charge = Convert.ToDecimal(reader["sum_nach"]);
                    if (reader["sum_outsaldo"] != DBNull.Value) zap.sum_outsaldo = Convert.ToDecimal(reader["sum_outsaldo"]);
                    if (reader["sum_outsaldo_k"] != DBNull.Value) zap.sum_outsaldo_k = Convert.ToDecimal(reader["sum_outsaldo_k"]);
                    if (reader["sum_outsaldo_d"] != DBNull.Value) zap.sum_outsaldo_d = Convert.ToDecimal(reader["sum_outsaldo_d"]);
                }
                list.Add(zap);
            }
            catch (Exception e)
            {
                ret = new Returns(false, "Ошибка загрузки статистики начислений:" + (Constants.Debug ? e.Message : ""));
                MonitorLog.WriteLog("Ошибка GetChargeStatistics " + (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                reader.Close();
                conn_web.Close();
                return null;
            }
            reader.Close();
            #endregion

            conn_web.Close();
            ret.tag = recordsTotalCount;
            return list;
        }

        private bool CreateTXXDistrib(IDbConnection conn_web, string tXXDistrib, out Returns ret)
        {
            ExecSQL(conn_web, " Drop table " + tXXDistrib, false);

            IDataReader reader;
#if PG
            string sql = "select * from information_schema.tables where table_name = '" + tXXDistrib + "' and table_schema = CURRENT_SCHEMA()";
#else
            string sql = "select * from systables where tabname = '" + tXXDistrib + "'";
#endif
            ret = ExecRead(conn_web, out reader, sql, true);
            if (!ret.result)
            {
                return false;
            }
            bool result;
            if (!reader.Read())
            {
#if PG
                sql = "CREATE TABLE " + tXXDistrib + "(" +
                    " nzp_dis SERIAL NOT NULL, " +
                    " pref CHAR(20), " +
                    " year_ INTEGER, " +
                    " month_ INTEGER, " +
                    " nzp_payer INTEGER NOT NULL, " +
                    " payer CHAR(200), " +
                    " nzp_area INTEGER NOT NULL, " +
                    " area CHAR(40), " +
                    " nzp_serv INTEGER NOT NULL, " +
                    " service CHAR(100), " +
                    " dat_oper DATE, " +
                    " dat_oper_po DATE, " +
                    " sum_in NUMERIC(14,2) default 0, " +
                    " sum_rasp NUMERIC(14,2) default 0, " +
                    " sum_ud NUMERIC(14,2) default 0, " +
                    " sum_naud NUMERIC(14,2) default 0, " +
                    " sum_reval NUMERIC(14,2) default 0, " +
                    " sum_charge NUMERIC(14,2) default 0, " +
                    " sum_send NUMERIC(14,2) default 0, " +
                    " sum_out NUMERIC(14,2) default 0, " +
                    " nzp_bank INTEGER default -1, " +
                    " bank CHAR(200) ) ";
#else
                sql = "CREATE TABLE " + tXXDistrib + "(" +
                    " nzp_dis SERIAL NOT NULL, " +
                    " pref CHAR(20), " +
                    " year_ INTEGER, " +
                    " month_ INTEGER, " +
                    " nzp_payer INTEGER NOT NULL, " +
                    " payer CHAR(200), " +
                    " nzp_area INTEGER NOT NULL, " +
                    " area CHAR(40), " +
                    " nzp_serv INTEGER NOT NULL, " +
                    " service CHAR(100), " +
                    " dat_oper DATE, " +
                    " dat_oper_po DATE, " +
                    " sum_in DECIMAL(14,2) default 0, " +
                    " sum_rasp DECIMAL(14,2) default 0, " +
                    " sum_ud DECIMAL(14,2) default 0, " +
                    " sum_naud DECIMAL(14,2) default 0, " +
                    " sum_reval DECIMAL(14,2) default 0, " +
                    " sum_charge DECIMAL(14,2) default 0, " +
                    " sum_send DECIMAL(14,2) default 0, " +
                    " sum_out DECIMAL(14,2) default 0, " +
                    " nzp_bank INTEGER default -1, " +
                    " bank CHAR(200) ) ";
#endif

                ret = ExecSQL(conn_web, sql, false);
                result = ret.result;
            }
            else
            {
                result = false;
                sql = "delete from " + tXXDistrib;
                ret = ExecSQL(conn_web, sql, true);
            }
            reader.Close();
            return result;
        }
        public List<MoneyDistrib> GetMoneyDistribDom(MoneyDistrib finder, IDbConnection conn_web, out Returns ret)
        {
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
#if PG
            ExecSQL(conn_web, "set search_path to 'public'", false);
#endif                      
            string tXXDistribDom = "t" + Convert.ToString(finder.nzp_user) + "_distrib_dom";

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
            string select = "Select sum(sum_in) as sum_in, sum(sum_out) as sum_out" +
                          ", sum(sum_rasp) as sum_rasp, sum(sum_ud) as sum_ud, sum(sum_naud) as sum_naud" +
                          ", sum(sum_reval) as sum_reval, sum(sum_charge) as sum_charge, sum(sum_send) as sum_send" +
                          ", min(nzp_dis) as nzp_dis, nzp_payer, payer, nzp_area, area, nzp_serv, service,nzp_dom, adr,nzp_bank, bank, dat_oper";
#else
            string select = "Select skip " + finder.skip + " sum(sum_in) as sum_in, sum(sum_out) as sum_out" +
                          ", sum(sum_rasp) as sum_rasp, sum(sum_ud) as sum_ud, sum(sum_naud) as sum_naud" +
                          ", sum(sum_reval) as sum_reval, sum(sum_charge) as sum_charge, sum(sum_send) as sum_send" +
                          ", min(nzp_dis) as nzp_dis, nzp_payer, payer, nzp_area, area, nzp_serv, service,nzp_dom, adr, nzp_bank, bank, dat_oper";
#endif

            string from = " From " + tXXDistribDom;
            string groupBy = " Group by nzp_payer, payer, nzp_area, area, nzp_serv, service, nzp_dom,adr, nzp_bank, bank, dat_oper";
            string orderBy = " {order_by} {order0}{order1}{order2}{order3}{order4}{order5}";
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
            if (Utils.GetParams(finder.groupby, Constants.act_groupby_area.ToString(), out sequenceNumber)) orderBy = orderBy.Replace("{order" + sequenceNumber + "}", ", area");
            if (Utils.GetParams(finder.groupby, Constants.act_groupby_service.ToString(), out sequenceNumber)) orderBy = orderBy.Replace("{order" + sequenceNumber + "}", ", service");
            if (Utils.GetParams(finder.groupby, Constants.act_groupby_dom.ToString(), out sequenceNumber)) orderBy = orderBy.Replace("{order" + sequenceNumber + "}", ", adr");
            if (Utils.GetParams(finder.groupby, Constants.act_groupby_payer.ToString(), out sequenceNumber)) orderBy = orderBy.Replace("{order" + sequenceNumber + "}", ", payer");
            if (Utils.GetParams(finder.groupby, Constants.act_groupby_bank.ToString(), out sequenceNumber)) orderBy = orderBy.Replace("{order" + sequenceNumber + "}", ", bank");

            orderBy = orderBy.Replace("{order0}", "")
                .Replace("{order1}", "")
                .Replace("{order2}", "")
                .Replace("{order3}", "")
                .Replace("{order4}", "")
                .Replace("{order5}", "")
                .Replace("{order_by} ,", "Order by ")
                .Replace("{order_by}", "");

            #region определить количество записей
            ExecSQL(conn_web, "drop table tmp_distrib_dom", false);

#if PG
            string sql = "select nzp_payer, nzp_area, nzp_serv, nzp_dom, nzp_bank, dat_oper into temp tmp_distrib_dom " + from + groupBy + having;
#else
            string sql = "select nzp_payer, nzp_area, nzp_serv, nzp_dom, nzp_bank, dat_oper " + from + groupBy + having +
                " into temp tmp_distrib_dom";
#endif
            ret = ExecSQL(conn_web, sql, true);
            if (!ret.result)
            {          
                return null;
            }
            sql = "select count(*) from tmp_distrib_dom";
            object count = ExecScalar(conn_web, sql, out ret, true);
            int recordsTotalCount;
            try { recordsTotalCount = Convert.ToInt32(count); }
            catch (Exception e)
            {
                ret = new Returns(false, "Ошибка при определении количества записей: " + (Constants.Debug ? e.Message : ""));
                MonitorLog.WriteLog("Ошибка GetMoneyDistribDom " + (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                return null;
            }

            ExecSQL(conn_web, "drop table tmp_distrib_dom", false);
            #endregion

            #region сформировать запрос
#if PG
            sql = select + from + groupBy + having + orderBy + " offset "+finder.skip;
#else
            sql = select + from + groupBy + having + orderBy;
#endif

            IDataReader reader;
            ret = ExecRead(conn_web, out reader, sql.ToString(), true);
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

                    if (reader["nzp_area"] != DBNull.Value) zap.nzp_area = (int)reader["nzp_area"];
                    if (reader["area"] != DBNull.Value) zap.area = ((string)reader["area"]).Trim();

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
                reader.Close();
                return null;
            }

            reader.Close();
            #endregion

            #region сформировать строку итого
            ret = ExecRead(conn_web, out reader, sqlItogo, true);
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
                reader.Close();
                return null;
            }

            reader.Close();
            #endregion

            ret.tag = recordsTotalCount;
            return list;
        }
        public List<MoneyDistrib> GetMoneyDistrib(MoneyDistrib finder, out Returns ret)
        {         
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;
            List<MoneyDistrib> list = GetMoneyDistrib(finder, conn_web, out ret);

            conn_web.Close();
            return list;
        }

        public List<MoneyDistrib> GetMoneyDistrib(MoneyDistrib finder, IDbConnection conn_web, out Returns ret)
        {
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
                      

            string tXXDistrib = "t" + Convert.ToString(finder.nzp_user) + "_distrib";

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

            string select = "Select skip " + finder.skip + " sum(sum_in) as sum_in, sum(sum_out) as sum_out" +
                ", sum(sum_rasp) as sum_rasp, sum(sum_ud) as sum_ud, sum(sum_naud) as sum_naud" +
                ", sum(sum_reval) as sum_reval, sum(sum_charge) as sum_charge, sum(sum_send) as sum_send" +
                ", min(nzp_dis) as nzp_dis, nzp_payer, payer, nzp_area, area, nzp_serv, service, nzp_bank, bank, dat_oper";

            string from = " From " + tXXDistrib;
            string groupBy = " Group by nzp_payer, payer, nzp_area, area, nzp_serv, service, nzp_bank, bank, dat_oper";
            string orderBy = " {order_by} {order0}{order1}{order2}{order3}{order4}";
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
            if (Utils.GetParams(finder.groupby, Constants.act_groupby_area.ToString(), out sequenceNumber)) orderBy = orderBy.Replace("{order" + sequenceNumber + "}", ", area");
            if (Utils.GetParams(finder.groupby, Constants.act_groupby_service.ToString(), out sequenceNumber)) orderBy = orderBy.Replace("{order" + sequenceNumber + "}", ", service");
            if (Utils.GetParams(finder.groupby, Constants.act_groupby_payer.ToString(), out sequenceNumber)) orderBy = orderBy.Replace("{order" + sequenceNumber + "}", ", payer");
            if (Utils.GetParams(finder.groupby, Constants.act_groupby_bank.ToString(), out sequenceNumber)) orderBy = orderBy.Replace("{order" + sequenceNumber + "}", ", bank");

            orderBy = orderBy.Replace("{order0}", "")
                .Replace("{order1}", "")
                .Replace("{order2}", "")
                .Replace("{order3}", "")
                .Replace("{order4}", "")
                .Replace("{order_by} ,", "Order by ")
                .Replace("{order_by}", "");

            #region определить количество записей
            ExecSQL(conn_web, "drop table tmp_distrib", false);

#if PG
            string sql = "select nzp_payer, nzp_area, nzp_serv, nzp_bank, dat_oper into temp tmp_distrib " + from + groupBy + having;
#else
            string sql = "select nzp_payer, nzp_area, nzp_serv, nzp_bank, dat_oper " + from + groupBy + having +
                " into temp tmp_distrib";
#endif
            ret = ExecSQL(conn_web, sql, true);
            if (!ret.result)
            {
              
                return null;
            }
            sql = "select count(*) from tmp_distrib";
            object count = ExecScalar(conn_web, sql, out ret, true);
            int recordsTotalCount;
            try { recordsTotalCount = Convert.ToInt32(count); }
            catch (Exception e)
            {
                ret = new Returns(false, "Ошибка при определении количества записей: " + (Constants.Debug ? e.Message : ""));
                MonitorLog.WriteLog("Ошибка GetMoneyDistrib " + (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
             
                return null;
            }

            ExecSQL(conn_web, "drop table tmp_distrib", false);
            #endregion

            #region сформировать запрос
            sql = select + from + groupBy + having + orderBy;

            IDataReader reader;
            ret = ExecRead(conn_web, out reader, sql.ToString(), true);
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

                    if (reader["nzp_area"] != DBNull.Value) zap.nzp_area = (int)reader["nzp_area"];
                    if (reader["area"] != DBNull.Value) zap.area = ((string)reader["area"]).Trim();

                    if (reader["nzp_serv"] != DBNull.Value) zap.nzp_serv = (int)reader["nzp_serv"];
                    if (reader["service"] != DBNull.Value) zap.service = ((string)reader["service"]).Trim();

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
                MonitorLog.WriteLog("Ошибка GetMoneyDistrib " + (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                reader.Close();                
                return null;
            }

            reader.Close();
            #endregion

            #region сформировать строку итого
            ret = ExecRead(conn_web, out reader, sqlItogo, true);
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
                MonitorLog.WriteLog("Ошибка GetMoneyDistrib " + (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                reader.Close();
              
                return null;
            }

            reader.Close();
            #endregion

           
            ret.tag = recordsTotalCount;
            return list;
        }

        public List<MoneyDistrib> GetMoneyDistribDom(MoneyDistrib finder, out Returns ret)
        {
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

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;
#if PG
            ExecSQL(conn_web, "set search_path to 'public'", false);
#endif

#if PG
            string tXXDistribDom = "public.t" + Convert.ToString(finder.nzp_user) + "_distrib_dom";
#else
            string tXXDistribDom = "t" + Convert.ToString(finder.nzp_user) + "_distrib_dom";
#endif

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
            string select = "Select sum(sum_in) as sum_in, sum(sum_out) as sum_out" +
                          ", sum(sum_rasp) as sum_rasp, sum(sum_ud) as sum_ud, sum(sum_naud) as sum_naud" +
                          ", sum(sum_reval) as sum_reval, sum(sum_charge) as sum_charge, sum(sum_send) as sum_send" +
                          ", min(nzp_dis) as nzp_dis, nzp_payer, payer, nzp_area, area, nzp_serv, service,nzp_dom,adr, nzp_bank, bank, dat_oper";
#else
            string select = "Select skip " + finder.skip + " sum(sum_in) as sum_in, sum(sum_out) as sum_out" +
                          ", sum(sum_rasp) as sum_rasp, sum(sum_ud) as sum_ud, sum(sum_naud) as sum_naud" +
                          ", sum(sum_reval) as sum_reval, sum(sum_charge) as sum_charge, sum(sum_send) as sum_send" +
                          ", min(nzp_dis) as nzp_dis, nzp_payer, payer, nzp_area, area, nzp_serv, service,nzp_dom, adr, nzp_bank, bank, dat_oper";
#endif

            string from = " From " + tXXDistribDom;
            string groupBy = " Group by nzp_payer, payer, nzp_area, area, nzp_serv, service, nzp_dom,adr, nzp_bank, bank, dat_oper";
            string orderBy = " {order_by} {order0}{order1}{order2}{order3}{order4}{order5}";
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
            if (Utils.GetParams(finder.groupby, Constants.act_groupby_area.ToString(), out sequenceNumber)) orderBy = orderBy.Replace("{order" + sequenceNumber + "}", ", area");
            if (Utils.GetParams(finder.groupby, Constants.act_groupby_service.ToString(), out sequenceNumber)) orderBy = orderBy.Replace("{order" + sequenceNumber + "}", ", service");
            if (Utils.GetParams(finder.groupby, Constants.act_groupby_dom.ToString(), out sequenceNumber)) orderBy = orderBy.Replace("{order" + sequenceNumber + "}", ", adr");
            if (Utils.GetParams(finder.groupby, Constants.act_groupby_payer.ToString(), out sequenceNumber)) orderBy = orderBy.Replace("{order" + sequenceNumber + "}", ", payer");
            if (Utils.GetParams(finder.groupby, Constants.act_groupby_bank.ToString(), out sequenceNumber)) orderBy = orderBy.Replace("{order" + sequenceNumber + "}", ", bank");

            orderBy = orderBy.Replace("{order0}", "")
                .Replace("{order1}", "")
                .Replace("{order2}", "")
                .Replace("{order3}", "")
                .Replace("{order4}", "")
                .Replace("{order5}", "")
                .Replace("{order_by} ,", "Order by ")
                .Replace("{order_by}", "");

            #region определить количество записей
            ExecSQL(conn_web, "drop table tmp_distrib_dom", false);

#if PG
            string sql = "select nzp_payer, nzp_area, nzp_serv, nzp_dom, nzp_bank, dat_oper into temp tmp_distrib_dom " + from + groupBy + having;
#else
            string sql = "select nzp_payer, nzp_area, nzp_serv, nzp_dom, nzp_bank, dat_oper " + from + groupBy + having +
                " into temp tmp_distrib_dom";
#endif
            ret = ExecSQL(conn_web, sql, true);
            if (!ret.result)
            {
                conn_web.Close();
                return null;
            }
            sql = "select count(*) from tmp_distrib_dom";
            object count = ExecScalar(conn_web, sql, out ret, true);
            int recordsTotalCount;
            try { recordsTotalCount = Convert.ToInt32(count); }
            catch (Exception e)
            {
                ret = new Returns(false, "Ошибка при определении количества записей: " + (Constants.Debug ? e.Message : ""));
                MonitorLog.WriteLog("Ошибка GetMoneyDistribDom " + (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                conn_web.Close();
                return null;
            }

            ExecSQL(conn_web, "drop table tmp_distrib_dom", false);
            #endregion

            #region сформировать запрос
#if PG
            sql = select + from + groupBy + having + orderBy + " offset "+finder.skip;
#else
            sql = select + from + groupBy + having + orderBy;
#endif

            IDataReader reader;
            ret = ExecRead(conn_web, out reader, sql.ToString(), true);
            if (!ret.result)
            {
                conn_web.Close();
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

                    if (reader["nzp_area"] != DBNull.Value) zap.nzp_area = (int)reader["nzp_area"];
                    if (reader["area"] != DBNull.Value) zap.area = ((string)reader["area"]).Trim();

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
                reader.Close();
                conn_web.Close();
                return null;
            }

            reader.Close();
            #endregion

            #region сформировать строку итого
            ret = ExecRead(conn_web, out reader, sqlItogo, true);
            if (!ret.result)
            {
                conn_web.Close();
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
                reader.Close();
                conn_web.Close();
                return null;
            }

            reader.Close();
            #endregion

            conn_web.Close();
            ret.tag = recordsTotalCount;
            return list;
        }

        private List<MoneyDistrib> GetPayerTransfer(MoneyDistrib finder, out Returns ret)
        {
            if (finder.pref == "") finder.pref = Points.Pref;

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;

            string tXXDistrib = "t" + Convert.ToString(finder.nzp_user) + "_distrib";

            // составные части запроса
            string sql = "Select nzp_area, area, nzp_payer, payer, nzp_serv, service, dat_oper, sum(sum_charge) as sum_charge, sum(sum_send) as sum_send" +
                " From " + tXXDistrib +
                " Group by nzp_area, area, nzp_payer, payer, nzp_serv, service, dat_oper" +
                " Order by area, payer, service, dat_oper";

            IDataReader reader;
            ret = ExecRead(conn_web, out reader, sql.ToString(), true);
            if (!ret.result)
            {
                conn_web.Close();
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

                    if (reader["nzp_area"] != DBNull.Value) zap.nzp_area = (int)reader["nzp_area"];
                    if (reader["area"] != DBNull.Value) zap.area = ((string)reader["area"]).Trim();

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
                reader.Close();
                conn_web.Close();
                return null;
            }

            reader.Close();
            conn_web.Close();

            if (finder.nzp_dis < 1) list.Add(new MoneyDistrib() { num = "Итого", nzp_user = finder.nzp_user, sum_charge = sum_charge, sum_send = sum_send });

            return list;
        }

        /// <summary>
        /// Создание таблицы переплат
        /// </summary>
        /// <param name="conn_web">Соедиение к базе</param>
        /// <param name="tXX_spls">Наименование таблицы</param>
        /// <param name="onCreate">Создание новой таблицы</param>
        public Returns CreateTableWebOverPayment(IDbConnection conn_web, string tXX_overpayment, bool onCreate)
        {
            Returns ret = Utils.InitReturns();

            if (onCreate)
            {
                if (TableInWebCashe(conn_web, tXX_overpayment))
                {
                    ret = ExecSQL(conn_web, " Drop table " + tXX_overpayment, false);
                    if (!ret.result) ret = ExecSQL(conn_web, " Delete from " + tXX_overpayment, false);
                }

                if (!TableInWebCashe(conn_web, tXX_overpayment))
                {
                    //создать таблицу webdata:tXX_overpayment
                    ret = ExecSQL(conn_web,
                                " Create table " + tXX_overpayment +
                                " ( nzp_overpay SERIAL NOT NULL, " +
                                " nzp_kvar_from INTEGER, " +
                                " pref_from char(10), " +
                                " adr_from varchar(160), " +
                                " adr_to varchar(160), " +
                                " nzp_geu_from INTEGER, " +
                                " geu_from char(60), " +
                                " nzp_area_from INTEGER, " +
                                " area_from char(40), " +
                                " nzp_area_to INTEGER, " +
                                " area_to char(40), " +
                                " nzp_kvar_to INTEGER, " +
                                " pref_to char(10), " +
                                " nzp_geu_to INTEGER, " +
                                " geu_to char(60), " +
                                " num_ls INTEGER, " +
                                " pkod10 INTEGER, " +
                                " num_ls_to INTEGER, " +
                                " mark INTEGER, " +
                                " litera INTEGER, "+ 
                                " sum_overpay DECIMAL(14,2) " +
                                " ) ", true);
                    if (!ret.result) return ret;
                }
            }
            else
            {
                ret = ExecSQL(conn_web, " Create index ix1_" + tXX_overpayment + " on " + tXX_overpayment + " (nzp_kvar_from,pref_from,nzp_kvar_to,pref_to) ", true);
             
                if (ret.result)
                {
#if PG
                    ret = ExecSQL(conn_web, " analyze  " + tXX_overpayment, true);
#else
                    ret = ExecSQL(conn_web, " Update statistics for table  " + tXX_overpayment, true);

#endif
                }
            }

            return ret;
        }

        /// <summary>
        /// Изменение marks для выбранных списков
        /// </summary>
        /// <param name="list0">список не выбранных</param>
        /// <param name="list1">список выбранных</param>
        /// <param name="finder">nzp_user необходим</param>
        /// <returns></returns>
        public Returns ChangeMarksSpisOverPayment(Finder finder, List<OverPayment> list0, List<OverPayment> list1)
        {
            Returns ret = Utils.InitReturns();
            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                return ret;
            }

            string true_ = "";
            for (int i = 0; i < list1.Count; i++)
            {
                if (i == 0) true_ += list1[i].nzp_overpay;
                else true_ += "," + list1[i].nzp_overpay;
            }
            if (true_ != "") true_ = "(" + true_ + ")";

            string false_ = "";
            for (int i = 0; i < list0.Count; i++)
            {
                if (i == 0) false_ += list0[i].nzp_overpay;
                else false_ += "," + list0[i].nzp_overpay;
            }
            if (false_ != "") false_ = "(" + false_ + ")";

            string tXX_overpayment = "t" + Convert.ToString(finder.nzp_user) + "_overpayment";

            if (tXX_overpayment != "")
            {
                //выбрать общее кол-во
                IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
                ret = OpenDb(conn_web, true);
                if (!ret.result) return ret;

                if (!TableInWebCashe(conn_web, tXX_overpayment))
                {
                    conn_web.Close();
                    ret.tag = -1;
                    ret.result = false;
                    ret.text = "Данные не были выбраны";
                    return ret;
                }

                string sql = "";
                if (true_ != "")
                {
                    sql = "update " + tXX_overpayment + " set mark = 1 where nzp_overpay in " + true_;
                    ret = ExecSQL(conn_web, sql, true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }
                if (false_ != "")
                {
                    sql = "update " + tXX_overpayment + " set mark = 0 where nzp_overpay in " + false_;
                    ret = ExecSQL(conn_web, sql, true);
                    if (!ret.result)
                    {
                        conn_web.Close();
                        return ret;
                    }
                }
                conn_web.Close();
            }
            return ret;
        }
    }
}
