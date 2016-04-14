using System;
using System.Linq;
using System.Text;
using System.Data;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.IFMX.Kernel.source.CommonType;
using STCLINE.KP50.Utility;
using STCLINE.KP50.DataBase;

namespace STCLINE.KP50.DataBase
{

    public class DbCalcKredit : DataBaseHead
    {
        private IDbConnection _connDb = null;
        private string _pref = "";
        private string tmp_kvar = "tmp_kredit_kvar";
        private string tmp_service = "tmp_kredit_service";
        private string tmp_percent = "tmp_kredit_percent";
        private string tmp_charge = "tmp_kredit_charge";

        private DateTime calcMonth;
        private string dat_s = "";
        private string dat_po = "";

        private string prevYearChargeXX = "";
        private string chargeXX = "";

        private StringBuilder log;

        private Returns MyExecSQL(IDbConnection conn_db, string sql)
        {
            log.Append(sql + ";" + Environment.NewLine);
            return ExecSQLWE(conn_db, sql);
        }


        public Returns CalcKredit(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc)
        {
            _connDb = conn_db;
            _pref = paramcalc.pref;
            dat_s = paramcalc.dat_s;
            dat_po = paramcalc.dat_po;
            calcMonth = new DateTime(paramcalc.calc_yy, paramcalc.calc_mm, 1);

            log = new StringBuilder();
            
            try
            {
                prevYearChargeXX = _pref + "_charge_" + ((calcMonth.Year - 1) % 100).ToString("00") + DBManager.tableDelimiter + "charge_" + calcMonth.Month.ToString("00");
                chargeXX = _pref + "_charge_" + (calcMonth.Year % 100).ToString("00") + DBManager.tableDelimiter + "charge_" + calcMonth.Month.ToString("00");

                CreateKredit(conn_db);
                
                // получить список ЛС
                GetKvarList();

                // почистить данные
                ClearKredit();
                                
                // Получить действующие процентные услуги из tarif
                GetActualPercentServicesFromTarif();

                // проставить проценты
                SetPercent();

                // вставить данные о рассрочке
                InsertKredit();

                DropTempTablesKredit(conn_db);
            }
            catch (Exception ex)
            {
                DropTempTablesKredit(conn_db);
                MonitorLog.WriteLog("Ошибка в CalcKredit" + (Constants.Viewerror ? ":\n" + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                return new Returns(false, "Ошибка при расчете рассрочки");
            }

            return new Returns(true);
        }

        /// <summary>
        /// Создать таблицу kredit
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="kreditXX"></param>
        /// <returns></returns>
        private void CreateKredit(IDbConnection conn_db)
        {
            Returns ret = new Returns(true);

            if (TempTableInWebCashe(conn_db, Points.Pref + DBManager.sDebtAliasRest + "kredit")) return;
            
            string sql = "";
#if PG
            sql = " set search_path to " + Points.Pref + DBManager.tableDelimiter + "_debt";
#else
            sql = " Database " + Points.Pref + DBManager.tableDelimiter + "_debt";
#endif
            MyExecSQL(conn_db, sql);

            sql = " Create table kredit" +
                " (nzp_kredit     serial not null, " +
                "  nzp_kvar       integer not null, " +
                "  nzp_serv       integer not null, " +
                "  dat_month      date not null, " +              //расчетный месяц, когда возникла рассрочка
                "  dat_s          date not null, " +              //период действия = расчетный месяц + 1 год (+11 месяцев)
                "  dat_po         date not null, " +
                "  valid          integer not null, " +           //0-новый, 1-подтвержденный жителем, 2 - завершен, ....
                "  dog_num        varchar(20), " +
                "  dog_dat        date, " +
                "  sum_dolg       " + DBManager.sDecimalType + "(14,2) default 0.00, " + //зафиксированный кредит
                "  perc           " + DBManager.sDecimalType + "(5,2)  default 0.00, " +
                "  sum_real_p     " + DBManager.sDecimalType + "(14,2) default 0.00 " + //зафиксированный кредит
                ") ";   //зафиксированный процент
            MyExecSQL(conn_db, sql);

            sql = " Create unique index ix_kredit_nzp_kredit on kredit (nzp_kredit)";
            MyExecSQL(conn_db, sql);

            sql = " Create index ix_kredit_1 on kredit (nzp_kvar, dat_month)";
            MyExecSQL(conn_db, sql);
        }

        /// <summary>
        /// получить список ЛС
        /// </summary>
        /// <returns></returns>
        private void GetKvarList()
        {
            ExecSQL(_connDb, "drop table " + tmp_kvar, false);

            string sql = " create temp table " + tmp_kvar + 
                "(nzp_kvar integer, " +
                " nzp_serv integer, " +
                " curr_kolg  " + DBManager.sDecimalType + "(11,7) default 0.0000000, " +
                " prev_kolg  " + DBManager.sDecimalType + "(11,7) default 0.0000000 " + ")";
            MyExecSQL(_connDb, sql);
            
            string mmnog = "1.25";

            //  получить ЛС, для которых расчет ведется и велся по нормативу (is_device = 0) 
            //  и тариф в текущем расчетном месяце больше на 25% по сравнению с прошлым годом
            sql = " insert into " + tmp_kvar + " (nzp_kvar, nzp_serv) " +
                  " select distinct cc.nzp_kvar, cc.nzp_serv " +
                  " from " + chargeXX + " cc, " + prevYearChargeXX + " pc, t_selkvar sk " +
                  " where cc.nzp_kvar = sk.nzp_kvar " +
                  "   and cc.nzp_kvar = pc.nzp_kvar " +
                  "   and cc.nzp_serv = pc.nzp_serv " +
                  "   and " + mmnog + " * cc.rsum_tarif > pc.rsum_tarif ";
              //  "   and cc.is_device = 0 " +
                //"   and " + DBManager.sNvlWord + "(cc.isdel, 0) = 0 " +
               // "   and pc.is_device = 0 ";
            MyExecSQL(_connDb, sql);

            // создать индекс
            sql = " create index ix_" + tmp_kvar + "_1 on " + tmp_kvar + " (nzp_kvar) ";
            MyExecSQL(_connDb, sql);

            // обновить статистику
            sql = DBManager.sUpdStat + " " + tmp_kvar;
            MyExecSQL(_connDb, sql);

            // получить количество жильцов в текущем расчетном месяце и за прошлый год
            string prevYearGilXX = _pref + "_charge_" + ((calcMonth.Year - 1) % 100).ToString("00") + DBManager.tableDelimiter + "gil_" + calcMonth.Month.ToString("00");
            string gilXX = _pref + "_charge_" + (calcMonth.Year % 100).ToString("00") + DBManager.tableDelimiter + "gil_" + calcMonth.Month.ToString("00");

            sql = "update " + tmp_kvar + " t set " + 
                " curr_kolg = " + DBManager.sNvlWord + "((select sum(c.val2 + c.val3 - c.val5) from " + gilXX + " c where c.nzp_kvar = t.nzp_kvar and c.stek = 3), 0) ";
            MyExecSQL(_connDb, sql);

            sql = "update " + tmp_kvar + " t set " +
                " prev_kolg = " + DBManager.sNvlWord + "((select sum(c.val2 + c.val3 - c.val5) from " + prevYearGilXX + " c where c.nzp_kvar = t.nzp_kvar and c.stek = 3), 0) ";
            MyExecSQL(_connDb, sql);
           
            // удалить ЛС, для которых количество жильцов в текущем месяце больше, чем год назад
            sql = " delete from " + tmp_kvar + " where curr_kolg > prev_kolg ";
            MyExecSQL(_connDb, sql);
        }

        /// <summary>
        /// Почистить кредитные истории
        /// </summary>
        private void ClearKredit()
        {
            string sql = "delete from " + Points.Pref + DBManager.sDebtAliasRest + "kredit_pay " +
                " where nzp_kredit in (select a.nzp_kredit from " + Points.Pref + DBManager.sDebtAliasRest + "kredit a, " + tmp_kvar + " t " +
                "   where a.nzp_kvar = t.nzp_kvar " + 
                "   and a.dat_month = " + Utils.EStrNull(calcMonth.ToShortDateString()) + ") ";
            MyExecSQL(_connDb, sql);

            sql = "delete from " + Points.Pref + DBManager.sDebtAliasRest + "kredit where nzp_kvar in (select nzp_kvar from " + tmp_kvar + ") " +
                " and dat_month = " + Utils.EStrNull(calcMonth.ToShortDateString());
            MyExecSQL(_connDb, sql);
        }

        /// <summary>
        /// Получить действующие процентные услуги из tarif
        /// </summary>
        private void GetActualPercentServicesFromTarif()
        {
            ExecSQL(_connDb, "drop table " + tmp_service, false);

            string sql = " create temp table " + tmp_service + " (" +
                " nzp_kvar       integer, " +
                " nzp_serv_perc  integer, " + // коммунальные услуги
                " nzp_serv_cmnl  integer, " + // процентные   услуги
                " perc " + DBManager.sDecimalType + " (5,2) default 0.00, " +
                " nzp_supp integer " +
                ")";
            MyExecSQL(_connDb, sql);

            sql = " insert into " + tmp_service + " (nzp_kvar, nzp_serv_perc, nzp_serv_cmnl, nzp_supp) " +
                " Select distinct p.nzp_kvar, " +
                // процентные услуги
                "   p.nzp_serv as nzp_serv_perc, so.nzp_serv_link as nzp_serv_cmnl, " +  //заодно сопоставим с коммунальными услугами
                // код поставщика
                "    max(p.nzp_supp) as nzp_supp " +
                " From " + _pref + DBManager.sDataAliasRest + "tarif p,  " + tmp_kvar + " t, " + _pref + DBManager.sKernelAliasRest + "serv_odn so " +
                " Where p.nzp_kvar = t.nzp_kvar " +
                "   and t.nzp_serv = so.nzp_serv_link " +
                "   and p.nzp_serv = so.nzp_serv_repay " +
                "   and p.is_actual <> 100 " +
                "   and p.dat_po >= " + dat_s +
                "   and p.dat_s <= " + dat_po +
                " Group by 1,2,3 ";
            MyExecSQL(_connDb, sql);

            ExecSQL(_connDb, " Create index ix1_" + tmp_service + " on " + tmp_service + " (nzp_kvar, nzp_serv_perc, nzp_supp) ", true);
            ExecSQL(_connDb, " Create index ix2_" + tmp_service + " on " + tmp_service + " (nzp_kvar, nzp_serv_cmnl) ", true);
            ExecSQL(_connDb, DBManager.sUpdStat + " " + tmp_service, true);
        }

        /// <summary>
        /// Проставить проценты по услугам
        /// </summary>
        private void SetPercent()
        { 
            // Выбрать проценты рассрочки по услугам на лс
            string sql = "drop table " + tmp_percent;
            ExecSQL(_connDb, sql, false);

            sql = " create temp table " + tmp_percent + " (" +
                " nzp_kvar       integer, " +
                " nzp_serv_perc  integer, " +
                " perc           " + DBManager.sDecimalType + " (5,2) " +
                ")";
            MyExecSQL(_connDb, sql);

            sql = " insert into " + tmp_percent + " (nzp_kvar, nzp_serv_perc, perc) " +
                " Select distinct p.nzp as nzp_kvar, so.nzp_serv_repay as nzp_serv_perc, " +
                    " max(replace(" + DBManager.sNvlWord + "(p.val_prm,'0'), ',', '.')" + DBManager.sConvToNum + ") as perc " +
                " From " + _pref + DBManager.sDataAliasRest + "prm_1 p,  " + tmp_service + " t, " + _pref + DBManager.sKernelAliasRest + "serv_odn so " +
                " Where p.nzp = t.nzp_kvar " +
                "   and p.nzp_prm = so.nzp_prm_repay " +
                "   and p.is_actual <> 100 " +
                "   and p.dat_po >= " + dat_s +
                "   and p.dat_s <= " + dat_po +
                " Group by 1,2 ";
            MyExecSQL(_connDb, sql);

            sql = " Create index ix1_" + tmp_percent + "_1 on " + tmp_percent + " (nzp_kvar, nzp_serv_perc) ";
            MyExecSQL(_connDb, sql);

            sql = DBManager.sUpdStat + " " + tmp_percent;
            MyExecSQL(_connDb, sql);
            
            //сначала проставить проценты по-умолчанию
            sql = " Update " + tmp_service + " Set " +
                " perc = (Select max(replace(" + DBManager.sNvlWord + "(val_prm, '0'), ',', '.') " + DBManager.sConvToNum + ") From " + _pref + DBManager.sDataAliasRest + "prm_5 p " +
                " Where p.nzp_prm = 1122 " +
                "   and p.is_actual <> 100 " +
                "   and p.dat_po >= " + dat_s +
                "   and p.dat_s <= " + dat_po + ") " +
                " Where exists (Select 1 From " + _pref + DBManager.sDataAliasRest + "prm_5 p " +
                    " Where p.nzp_prm = 1122 " +
                    "   and p.is_actual <> 100 " +
                    "   and p.dat_po >= " + dat_s +
                    "   and p.dat_s <= " + dat_po + DBManager.Limit1 + ") ";
            MyExecSQL(_connDb, sql);

            //проставить проценты по услугам ЛС, которые перекроют проценты по умолчанию
            sql = "Update " + tmp_service + " t Set " +
                " perc = (Select max(perc) From " + tmp_percent + " p " +
                    " Where p.nzp_kvar      = t.nzp_kvar " +
                    "   and p.nzp_serv_perc = t.nzp_serv_perc) " +
                " Where exists (Select 1 From " + tmp_percent + " p " +
                    " Where p.nzp_kvar      = t.nzp_kvar " +
                    "   and p.nzp_serv_perc = t.nzp_serv_perc " + DBManager.Limit1 + ") ";
            MyExecSQL(_connDb, sql);
        }

        /// <summary>
        /// Вставка данных о рассрочке
        /// </summary>
        private void InsertKredit()
        { 
            string sql = "drop table " + tmp_charge;
            ExecSQL(_connDb, sql, false);

            sql = " create temp table " + tmp_charge +
                " (" +
                " nzp_kvar       integer, " +
                " nzp_serv       integer, " +
                " nzp_serv_perc  integer, " +
                " nzp_supp        integer, " +
                " perc " + DBManager.sDecimalType + "(5,2), " +
                " sum_real " + DBManager.sDecimalType + "(14,2), " +
                " sum_real_p " + DBManager.sDecimalType + "(14,2) " +
                ") ";
            MyExecSQL(_connDb, sql);

            sql = "insert into " + tmp_charge + " (nzp_kvar, nzp_serv_perc, nzp_supp, perc, nzp_serv, sum_real) " +
                " Select a.nzp_kvar, a.nzp_serv_perc, a.nzp_supp, a.perc, b.nzp_serv, sum(b.sum_real) as sum_real " +
                " From " + tmp_service + " a, " + 
                chargeXX  + " b " +
                " Where a.nzp_kvar = b.nzp_kvar " +
                "   and a.nzp_serv_cmnl = b.nzp_serv " +
                "   and b.dat_charge is null " +
                "   and b.sum_real > 0 " +
                " Group by 1,2,3,4,5 ";
            MyExecSQL(_connDb, sql);

            sql = " Create index ix_" + tmp_charge + "_1 on " + tmp_charge + " (nzp_kvar, nzp_serv) ";
            MyExecSQL(_connDb, sql);

            sql = DBManager.sUpdStat + " " + tmp_charge;
            MyExecSQL(_connDb, sql);

            sql = "update " + tmp_charge + " t set " +
                " sum_real_p = " + sNvlWord + "((select sum(c.sum_real) " +
                    " from " + prevYearChargeXX + " c " + 
                    " where t.nzp_kvar = c.nzp_kvar " + 
                    "   and t.nzp_serv = c.nzp_serv " +
                    "   and c.dat_charge is null), 0) ";
            MyExecSQL(_connDb, sql);

            sql = " Insert into " + Points.Pref + DBManager.sDebtAliasRest + "kredit (nzp_kvar, nzp_serv, dat_month, dat_s, dat_po, valid, sum_dolg, perc, sum_real_p) " +
                " Select nzp_kvar, nzp_serv_perc, " + dat_s + ", " + dat_s + "," +
                " (" + dat_s + " + " + (DBManager.tableDelimiter == ":" ? " 12 units month - 1 units day " : "INTERVAL '12 months' MONTH - INTERVAL '1 days' ") + "), " +
                "   0, sum_real - sum_real_p, perc, sum_real_p " +
                " From " + tmp_charge+ " where sum_real > sum_real_p";
            MyExecSQL(_connDb, sql);
        }

        private void DropTempTablesKredit(IDbConnection conn_db)
        {
            ExecSQL(_connDb, " Drop table " + tmp_kvar, false);
            ExecSQL(_connDb, " Drop table " + tmp_service, false);
            ExecSQL(_connDb, " Drop table " + tmp_percent, false);
            ExecSQL(_connDb, " Drop table " + tmp_charge, false);
        }
    }
    
    public partial class DbCalcCharge : DataBaseHead
    {
        //-----------------------------------------------------------------------------
        public Returns CalcKreditData(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc)
        //-----------------------------------------------------------------------------
        {
            string prevYearChargeSchema = paramcalc.pref + "_charge_" + ((paramcalc.calc_yy - 1) % 100).ToString("00") + DBManager.tableDelimiter + "charge_" + paramcalc.calc_mm.ToString("00");
            Returns ret = new Returns(true);

            if (!TempTableInWebCashe(conn_db, prevYearChargeSchema))
            {
                return new Returns(false, "Нет данных о расчетах в прошлом году");
            }

            if (!TempTableInWebCashe(conn_db, "t_selkvar"))
            {
                ChoiseTempKvar(conn_db, ref paramcalc, out ret);
                if (!ret.result) throw new Exception(ret.text);
            }

            using (DbCalcKredit db = new DbCalcKredit())
            {
                return db.CalcKredit(conn_db, paramcalc);
            }
        }
    }
}
