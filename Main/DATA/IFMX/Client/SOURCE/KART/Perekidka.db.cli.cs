using System;
using System.Data;
using System.Collections.Generic;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Utility;

namespace STCLINE.KP50.DataBase
{
    /// <summary>
    /// Клиентский класс для работы с перекидками
    /// </summary>
    public partial class DbPerekidkaClient : DataBaseHeadClient
    {
        private int nzpUser;

        public DbPerekidkaClient(int nzpUser)
        {
            this.nzpUser = nzpUser;
        }

        private string GetTXXSplsPerekidkiShortName()
        {
            return "t" + nzpUser + "_spls_perekidki";
        }

        public string GetTXXSplsPerekidkiFullName()
        {
            return GetTableFullName(GetTXXSplsPerekidkiShortName());
        }

        private string GetTXXSplsShortName()
        {
            return "t" + nzpUser + "_spls";
        }

        public string GetTXXSplsFullName()
        {
            return GetTableFullName(GetTXXSplsShortName());
        }

        public Returns CreateTXXSplsPerekidki()
        {
#if PG
            ExecSQL("set search_path to 'public'");
#endif

            string tXX_spls_perekidki = GetTXXSplsPerekidkiShortName();

            Returns ret = Utils.InitReturns();

            if (TempTableInWebCashe(tXX_spls_perekidki)) ExecSQL("drop table " + tXX_spls_perekidki, false);

            if (!TempTableInWebCashe(tXX_spls_perekidki))
            {
                ret = ExecSQL("CREATE TABLE " + tXX_spls_perekidki + " (" +
                                           " nzp_spls_per SERIAL NOT NULL, " +
                                           " nzp_kvar INTEGER, " +
                                           " num_ls INTEGER, " +
                                           " fio CHAR(60)," +
                                           " adr CHAR(160)," +
                                           " pref CHAR(20)," +
                                           " mark INTEGER," +
                                           " nzp_serv INTEGER," +
                                           " nzp_supp INTEGER," +
                                           " tot_square char(20) default 0," +
                                           " otopl_square char(20) default 0," +
                                           " kol_gil INTEGER default 0," +
                                           " sum_insaldo " + sDecimalType + "(14,2) default 0," +
                                           " sum_outsaldo " + sDecimalType + "(14,2) default 0," +
                                           " sum_real " + sDecimalType + "(14,2) default 0," +
                                           " real_charge " + sDecimalType + "(14,2) default 0," +
                                           " sum_money " + sDecimalType + "(14,2) default 0," +
                                           " sum_izm " + sDecimalType + "(14,2) default 0," +
                                           " sum_new_outsaldo " + sDecimalType + "(14,2) default 0" + ")");
                if (!ret.result)
                {
                    return ret;
                }
            }
            else ret = ExecSQL("delete from " + tXX_spls_perekidki);

            string tXX_spls = GetTXXSplsShortName();

            if (!TempTableInWebCashe(tXX_spls))
            {
                ret = new Returns(false, "Список выбранных лицевых счетов не сформирован", -1);
                return ret;
            }

            string sql = "Select count(*) From " + tXX_spls;
            object count = ExecScalar(sql, out ret);
            int cnt;
            try { cnt = Convert.ToInt32(count); }
            catch (Exception e)
            {
                ret = new Returns(false, "Ошибка при определении количества записей: " + (Constants.Debug ? e.Message : ""));
                MonitorLog.WriteLog("Ошибка PrepareSplsPerekidki " + (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                return ret;
            }
            if (cnt <= 0)
            {
                ret = new Returns(false, "Список выбранных лицевых счетов содержит 0 записей", -1);
            }
            return ret;
        }

        public void CreateTXXSplsPerekidkiIndexes()
        {
#if PG
            ExecSQL("set search_path to 'public'");
#endif
            string tn = GetTXXSplsPerekidkiShortName();

            ExecSQL("create unique index ix_" + tn + "_1 on " + tn + "(nzp_spls_per)");
            ExecSQL("create index ix_" + tn + "_2 on " + tn + "(pref)");
            ExecSQL(sUpdStat + " " + tn);
        }

        public Returns IzmSaldo(ParamsForGroupPerekidki finder)
        {
#if PG
            ExecSQL("set search_path to 'public'");
#endif

            Returns ret = Utils.InitReturns();
            string sql = "";
            string tXX_spls_perekidki = GetTXXSplsPerekidkiShortName();

            if (finder.oper_perekidri == (int)ParamsForGroupPerekidki.Operations.SpisInSaldo)
            {
                if (finder.saldo_part == SaldoPart.PositiveNegative.GetHashCode())
                {
                    sql = " update " + tXX_spls_perekidki + " set sum_izm = sum_insaldo * (-1)" +
                        ", sum_new_outsaldo = sum_outsaldo + sum_insaldo * (-1)";
                }
                else if (finder.saldo_part == SaldoPart.Positive.GetHashCode())
                {
                    sql = " update " + tXX_spls_perekidki + " set sum_izm = (case when sum_insaldo > 0 then sum_insaldo * (-1) else 0 end)" +
                        ", sum_new_outsaldo = sum_outsaldo + (case when sum_insaldo > 0 then sum_insaldo * (-1) else 0 end)";
                }
                else if (finder.saldo_part == SaldoPart.Negative.GetHashCode())
                {
                    sql = " update " + tXX_spls_perekidki + " set sum_izm = (case when sum_insaldo < 0 then sum_insaldo * (-1) else 0 end)" +
                        ", sum_new_outsaldo = sum_outsaldo + (case when sum_insaldo < 0 then sum_insaldo * (-1) else 0 end)";
                }
                ret = ExecSQL(sql);
                if (!ret.result)
                {
                    return ret;
                }
            }
            else if (finder.oper_perekidri == (int)ParamsForGroupPerekidki.Operations.SpisOutSaldo)
            {
                if (finder.saldo_part == SaldoPart.PositiveNegative.GetHashCode())
                {
                    sql = " update " + tXX_spls_perekidki + " set sum_izm = sum_outsaldo * (-1), sum_new_outsaldo = sum_outsaldo + sum_outsaldo * (-1)";
                }
                else if (finder.saldo_part == SaldoPart.Positive.GetHashCode())
                {
                    sql = " update " + tXX_spls_perekidki + " set sum_izm = (case when sum_outsaldo > 0 then sum_outsaldo * (-1) else 0 end), sum_new_outsaldo = sum_outsaldo + (case when sum_outsaldo > 0 then sum_outsaldo * (-1) else 0 end)";
                }
                else if (finder.saldo_part == SaldoPart.Negative.GetHashCode())
                {
                    sql = " update " + tXX_spls_perekidki + " set sum_izm = (case when sum_outsaldo < 0 then sum_outsaldo * (-1) else 0 end), sum_new_outsaldo = sum_outsaldo + (case when sum_outsaldo < 0 then sum_outsaldo * (-1) else 0 end)";
                }
                ret = ExecSQL(sql);
                if (!ret.result)
                {
                    return ret;
                }
            }
            else if (finder.oper_perekidri == (int)ParamsForGroupPerekidki.Operations.IzmSaldoFixSum)
            {
                sql = " update " + tXX_spls_perekidki + " set sum_izm = " + finder.sum_izm + ", sum_new_outsaldo = sum_outsaldo + " + finder.sum_izm;
                ret = ExecSQL(sql);
                if (!ret.result)
                {
                    return ret;
                }
            }
            else if (finder.oper_perekidri == (int)ParamsForGroupPerekidki.Operations.IzmSaldoRaschSum)
            {
                ret = IzmSaldoRaschSum(finder);
            }
            else if (finder.oper_perekidri == (int)ParamsForGroupPerekidki.Operations.PerenosOutSaldo)
            {
                ret = PerenosOutSaldo(finder);
            }
            return ret;
        }

        private Returns IzmSaldoRaschSum(ParamsForGroupPerekidki finder)
        {
            string tXX_spls_perekidki = GetTXXSplsPerekidkiShortName();

            MyDataReader reader = null;
            Returns ret = Utils.InitReturns();

            string sql = "";

            List<GroupsPerekidki> list = new List<GroupsPerekidki>();
            List<decimal> list1 = new List<decimal>();

            sql = "select tot_square, otopl_square, kol_gil, nzp_kvar, pref, nzp_spls_per, sum_outsaldo from " + tXX_spls_perekidki;
            ret = ExecRead(out reader, sql);
            if (!ret.result) return ret;

            try
            {
                try
                {
                    while (reader.Read())
                    {
                        GroupsPerekidki gp = new GroupsPerekidki();
                        if (finder.sposob_raspr == SposobRaspr.TotSquare.GetHashCode())
                        {
                            if (reader["tot_square"] != DBNull.Value) list1.Add(Convert.ToDecimal(reader["tot_square"]));
                            else list1.Add(0);
                        }
                        if (finder.sposob_raspr == SposobRaspr.OtoplSquare.GetHashCode())
                        {
                            if (reader["otopl_square"] != DBNull.Value) list1.Add(Convert.ToDecimal(reader["otopl_square"]));
                            else list1.Add(0);
                        }
                        else if (finder.sposob_raspr == SposobRaspr.CountGil.GetHashCode())
                        {
                            if (reader["kol_gil"] != DBNull.Value) list1.Add(Convert.ToInt32(reader["kol_gil"]));
                            else list1.Add(0);
                        }
                        else list1.Add(1);

                        if (reader["nzp_spls_per"] != DBNull.Value) gp.nzp_spls_per = Convert.ToInt32(reader["nzp_spls_per"]);
                        if (reader["sum_outsaldo"] != DBNull.Value) gp.sum_outsaldo = Convert.ToDecimal(reader["sum_outsaldo"]);

                        list.Add(gp);
                    }
                }
                finally
                {
                    reader.Close();
                }

                List<decimal> listres = MathUtility.DistributeSum(finder.sum_raspr, list1);

                if (list != null)
                {
                    for (int i = 0; i < list.Count;i++ )
                    {
                        list[i].sum_izm = listres[i];
                        list[i].sum_new_outsaldo = list[i].sum_outsaldo + list[i].sum_izm;

                        sql = " update " + tXX_spls_perekidki + " set sum_izm = " + list[i].sum_izm + ", sum_new_outsaldo = " + list[i].sum_new_outsaldo + " where nzp_spls_per = " + list[i].nzp_spls_per;
                        ret = ExecSQL(sql);
                        if (!ret.result) return ret;
                    }
                }
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка в функции DbCharge.SaveSumIzm" + (Constants.Viewerror ? ":\n" + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
            }
            return ret;
        }
        
        private Returns PerenosOutSaldo(ParamsForGroupPerekidki finder)
        {
            string tXX_spls_perekidki = GetTXXSplsPerekidkiShortName();
            
            Returns ret = Utils.InitReturns();

            try
            {
                string sql = " update " + tXX_spls_perekidki + " set sum_izm = sum_outsaldo*(-1) " +
                      " where nzp_serv = " + finder.nzp_serv + " and nzp_supp = " + finder.nzp_supp;
                ret = ExecSQL(sql);
                if (!ret.result) return ret;

                ExecSQL("drop table txxx", false);

                ret = ExecSQL("create temp table txxx (nzp_kvar integer, nzp_serv integer, nzp_supp integer, sum_izm " + sDecimalType + "(14,2)) " + sUnlogTempTable);
                if (!ret.result) return ret;

                ret = ExecSQL("insert into txxx (nzp_kvar, nzp_serv, nzp_supp, sum_izm) select nzp_kvar, nzp_serv, nzp_supp, sum_izm from " + tXX_spls_perekidki + " where nzp_serv = " + finder.nzp_serv + " and nzp_supp = " + finder.nzp_supp);
                if (!ret.result) return ret;

                ret = ExecSQL("update " + tXX_spls_perekidki +
                    " set sum_izm = (select sum(sum_izm)*(-1) from txxx where nzp_serv = " + finder.nzp_serv + " and nzp_supp = " + finder.nzp_supp + " and nzp_kvar = " + tXX_spls_perekidki + ".nzp_kvar)" +
                    " where nzp_serv = " + finder.on_nzp_serv + " and nzp_supp = " + finder.on_nzp_supp);
                if (!ret.result) return ret;

                sql = " update " + tXX_spls_perekidki + " set sum_new_outsaldo = sum_outsaldo + sum_izm";
                ret = ExecSQL(sql);
            }
            catch (Exception ex)
            {
                ExecSQL("drop table txxx", false);

                ret.result = false;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка в функции DbCharge.PerenosOutSaldo" + (Constants.Viewerror ? ":\n" + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
            }
            return ret;
        }

    }
}
