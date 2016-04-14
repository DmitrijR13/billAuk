using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;

namespace Bars.KP50.SzExchange.UnloadForSZ
{
    /// <summary>
    /// Выгрузка услуг
    /// </summary>
    public class UnlServ : BaseUnloadClass
    {
        public DateTime time;
        public override int Code
        {
            get
            {
                return 4;
            }
        }

        public override string Name
        {
            get { return "UnlServ"; }
        }

        public override string NameText
        {
            get { return "'Услуга'"; }
        }

        public override void Start(FilesImported finder)
        {

        }

        public override void Start()
        {

        }

        public DateTime Time
        {
            get
            {
                return time;
            }
            set
            {
                time = value;
            }
        }

        public TimeSpan GetTime(DateTime t_start, DateTime t_finish)
        {
            return (t_finish - t_start);
        }

        public void Start(FilesImported finder, int num_ls, int nzp_kvar)
        {
            string sql;
            string str;
            string sep = "|";
            DateTime time_finish = DateTime.Now;

            OpenConnection();
            CreateTempTable();

            WriteInFile w = new WriteInFile();
            UnlServ us = new UnlServ();
            UnlRecalcServ urs = new UnlRecalcServ();

            try
            {
                WriteInServ(finder, num_ls, nzp_kvar, us); //запись во временную таблицу

                //w.Filing(GetComment(), finder.saved_name_log);

                //выборка данных из временной таблицы 
                sql =
                    " SELECT * FROM " + Name;
                foreach (DataRow rr in ExecSQLToTable(sql).Rows)
                {
                    //формирование строки услуга
                    str = Code + sep + rr["nzp_supp"] + sep + rr["nzp_serv"] + sep + rr["sum_insaldo"] + sep +
                          rr["eot"] + sep + rr["reg_tarif"] + sep + rr["nzp_measure"] + sep +
                          rr["fact_rashod"] + sep + rr["norm_rashod"] + sep + rr["is_pu_calc"] + sep +
                          rr["sum_nach"] + sep + rr["sum_pere_nach"] + sep + rr["sum_subsidy"] + sep +
                          rr["sum_pere_subsidy"] + sep + rr["sum_lg"] + sep + rr["sum_pere_lg"] + sep +
                          rr["sum_smo"] + sep + rr["sum_pere_smo"] + sep + rr["sum_oplat"] + sep +
                          rr["is_del"] + sep + rr["count_string_pere"] + sep + rr["count_string_nach"] + sep;
                    //запись в файл
                    w.Filing(str, finder.saved_name);

                    time_finish = DateTime.Now;
                    string tinfo = " выгрузка услуги: " + rr["nzp_serv"] + ", поставщик: " + rr["nzp_supp"] + ", время выполнения: " + GetTime(us.time, time_finish);
                    w.Filing(tinfo, finder.saved_name_log);

                    urs.Start(finder, nzp_kvar, Convert.ToInt32(rr["nzp_supp"]), Convert.ToInt32(rr["nzp_serv"]), num_ls);

                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("UnlServ.Start(pref): Ошибка добавления полей в таблицу.\n: " + ex.Message,
                    MonitorLog.typelog.Error, true);
            }
            finally
            {
                DropTempTable();
                CloseConnection();
            }
        }

        public override void CreateTempTable()
        {
            string sql;

            sql = "DROP TABLE " + Name;
            ExecSQL(sql, false);

            sql =
                "CREATE TEMP TABLE " + Name + "(" +
                " nzp_supp INTEGER , " +
                " nzp_serv INTEGER , " +
                " nzp_frm INTEGER, " +
                " sum_insaldo DECIMAL (12,2), " +
                " eot DECIMAL (12,2), " +
                " reg_tarif DECIMAL (12,2), " +
                " nzp_measure INTEGER , " +
                " fact_rashod DECIMAL (12,2), " +
                " norm_rashod DECIMAL (12,2), " +
                " is_pu_calc INTEGER , " +
                " sum_nach DECIMAL (12,2), " +
                " sum_pere_nach DECIMAL (12,2), " +
                " sum_subsidy DECIMAL (12,2), " +
                " sum_pere_subsidy DECIMAL (12,2), " +
                " sum_lg DECIMAL (12,2)," +
                " sum_pere_lg DECIMAL (12,2), " +
                " sum_smo DECIMAL (12,2), " +
                " sum_pere_smo DECIMAL (12,2), " +
                " sum_oplat DECIMAL (12,2), " +
                " is_del INTEGER , " +
                " count_string_pere INTEGER , " +
                " count_string_nach INTEGER )";
            ExecSQL(sql, false);

            sql =
                " CREATE INDEX " + Name + "_nzp_supp_idx ON " + Name + " (nzp_supp)";
            ExecSQL(sql);
            sql =
                " CREATE INDEX " + Name + "_nzp_serv_idx ON " + Name + " (nzp_serv)";
            ExecSQL(sql);
            sql =
                " CREATE INDEX " + Name + "_nzp_serv_nzp_supp_idx ON " + Name + " (nzp_serv, nzp_supp)";
            ExecSQL(sql);
            sql =
                " CREATE INDEX " + Name + "_nzp_measure_idx ON " + Name + " (nzp_measure)";
            ExecSQL(sql);
            
        }

        /// <summary>
        /// Запись во временную таблицу
        /// </summary>
        /// <param name="pref"></param>
        public void WriteInServ(FilesImported finder, int num_ls, int nzp_kvar, UnlServ us)
        {
            string sql;

            string pref = finder.bank;
            string year = finder.year.Substring(2, 2);
            string month = finder.month.PadLeft(2, '0');
            int nzp_serv;
            string sum_nach = "";
            bool IsKzn = true;

            us.Time = DateTime.Now;

            sum_nach = IsKzn ? "sum_tarif_sn_f" : "sum_dlt_tarif_p"; //фиксировано - признак Казани 
            //if (num_ls == 250062)
            //{
                //Выборка данных из БД 
                sql =
                    " INSERT INTO " + Name +
                    " SELECT " +
                    " nzp_supp, nzp_serv, nzp_frm, sum_insaldo, " +
                    " tarif, tarif_f, 0, " +
                    " c_calc, c_sn, is_device, " +
                    " " + sum_nach + ", reval, 0, " +
                    " 0, 0, 0, " +
                    " 0, 0, sum_money, " +
                    " isdel, 0, 0 " +
                    " FROM " + pref + "_charge_" + year + ".charge_" + month +
                    " WHERE dat_charge IS NULL" +
                    " AND num_ls = " + num_ls +
                    " AND nzp_serv IN (" +
                    "  SELECT nzp_serv " +
                    "  FROM " + pref + "_kernel.sz_serv)" +
                    " AND nzp_supp > -999";
                ExecSQL(sql);
                
            sql =
                " ANALYZE " + Name;
                ExecSQL(sql);

                //Апдейт перерасчета по соц. нормативу
                sql =
                    " SELECT nzp_serv, nzp_supp " +
                    " FROM " + Name;
                DataTable dt = ExecSQLToTable(sql);
                foreach (DataRow rr in dt.Rows)
                {
                    sql =
                        " UPDATE " + Name +
                        " SET sum_pere_nach = " +
                        CheckReval(finder, num_ls, nzp_kvar, Convert.ToInt32(rr["nzp_serv"]),
                            Convert.ToInt32(rr["nzp_supp"])) +
                        " WHERE nzp_serv = " + rr["nzp_serv"] +
                        " AND nzp_supp = " + rr["nzp_supp"];
                    ExecSQL(sql);
                }
           // }
            // не используется

            #region Выгрузка идиотского кода в качестве nzp_measure

            //Временная таблица для получения идиотского кода
            //sql =
            //    " DROP TABLE t_measure";
            //ExecSQL(sql, false);

            //sql =
            //    " SELECT nzp_frm, " +
            //    " CASE WHEN coalesce(idiotsky_kod,7)=0 " +
            //    " THEN 7 " +
            //    " ELSE coalesce(idiotsky_kod,7) " +
            //    " END " +
            //    " INTO TEMP t_measure " +
            //    " FROM " + pref + "_kernel.formuls a " +
            //    "  LEFT JOIN " + pref + "_kernel.s_measure b " +
            //    "  ON a.nzp_measure = b.nzp_measure " +
            //    " WHERE nzp_frm IN (" +
            //    "   SELECT nzp_frm " +
            //    "   FROM " + pref + "_charge_" + year + ".charge_" + month + ") ";
            //ExecSQL(sql);

            //
            //sql =
            //    " UPDATE " + Name +
            //    " SET nzp_measure = " +
            //    "  (SELECT tm.coalesce as nzp_measure " +
            //    "  FROM t_measure tm " +
            //    "  WHERE " + Name + ".nzp_frm = tm.nzp_frm)" +
            //    " WHERE EXISTS " +
            //    " (SELECT 1 FROM t_measure tm " +
            //    "  WHERE " + Name + ".nzp_frm = tm.nzp_frm)";
            //    //" WHERE " + Name + ".nzp_frm IN ( " +
            //    //"  SELECT nzp_frm" +
            //    //"  FROM " + pref + "_kernel.formuls)";
            //ExecSQL(sql);

            #endregion

            //Апдейтим nzp_measure
            sql =
                " UPDATE " + Name +
                " SET nzp_measure = " +
                "  (SELECT f.nzp_measure " +
                "  FROM " + pref + "_kernel.formuls f " +
                "  WHERE " + Name + ".nzp_frm = f.nzp_frm)" +
                " WHERE EXISTS " +
                " (SELECT 1 " +
                "  FROM " + pref + "_kernel.formuls f " +
                "  WHERE " + Name + ".nzp_frm = f.nzp_frm)";
            ExecSQL(sql);

            #region Проверки на пустоту и отрицательные значения

            // код единицы измерения проставляем 0 в том случае, если он не проапдейтился(когда nzp_frm по этому коду = 0)
            CheckColumnOnEmptiness("nzp_measure", "zero",
                "0-и в строке " + NameText + ", поле 'Код ед. измерения' по ЛС № " + num_ls, true, "7");
            // регулируемый тариф
            CheckColumnOnEmptiness("reg_tarif", "null",
                "null-ы в строке " + NameText + ", поле 'Регулируемый тариф' по ЛС №" + num_ls, true, "0");
            // расход фактический
            CheckColumnOnEmptiness("fact_rashod", "null",
                "null-ы в строке " + NameText + ", поле 'Фактический расход' по ЛС №" + num_ls, true, "0");
            CheckColumnOnEmptiness("fact_rashod", "negative",
                "отрицательные значения в строке " + NameText + ", поле 'Фактический расход' по ЛС №" + num_ls, true,
                "0");
            // расход по соц. нормативу
            CheckColumnOnEmptiness("norm_rashod", "null",
                "null-ы в строке " + NameText + ", поле 'Расход по соц. нормативу' по ЛС №" + num_ls, true, "0");
            CheckColumnOnEmptiness("norm_rashod", "negative",
                "отрицательные значения в строке " + NameText + ", поле 'Расход по соц. нормативу' по ЛС №" + num_ls,
                true, "0");
            // наличие прибора учета
            CheckColumnOnEmptiness("is_pu_calc", "null",
                "null-ы в строке " + NameText + ", поле 'Наличие прибора учета' по ЛС №" + num_ls, true, "0");
            // признак удаления
            CheckColumnOnEmptiness("is_del", "null",
                "null-ы в строке " + NameText + ", поле 'Признак удаления' по ЛС №" + num_ls, true, "0");
            //Сумма начислений по соц. нормативу
            CheckColumnOnEmptiness("sum_nach", "negative",
                "отрицательные значения в строке " + NameText + ", поле 'Сумма начислений по соц. нормативу' по ЛС №" +
                num_ls, true, "0");

            #endregion

            //!!!Поля "Кол-во строк начисленных льгот по категориям" и "Кол-во строк перерасчетов начисления по соц. норативу по регулируему тарифу по месяцам" - не заполняеются!
        }

        /// <summary>
        /// Запись во временную таблицу
        /// </summary>
        public void WriteInServ()
        {

        }

        public double CheckReval(FilesImported finder, int num_ls, int nzp_kvar, int nzp_serv, int nzp_supp)
        {
            string sql;
            string pref = finder.bank;
            string year = finder.year.Substring(2, 2);
            string month = finder.month.PadLeft(2, '0');
            string yReval;
            string mReval;
            double reval = 0;

            sql =
                " SELECT month_, year_" +
                " FROM " + pref + "_charge_" + year + ".lnk_charge_" + month +
                " WHERE nzp_kvar = " + nzp_kvar;
            DataTable dt = ExecSQLToTable(sql);
            if (dt.Rows.Count != 0)
            {
                foreach (DataRow rr in dt.Rows)
                {
                    yReval = rr["year_"].ToString();
                    mReval = rr["month_"].ToString().PadLeft(2, '0');

                    sql =
                        " SELECT DISTINCT 1 " +
                        " FROM information_schema.tables " +
                        " WHERE table_schema = '" + pref + "_charge_" + yReval.Substring(2, 2) + "'" /*+
                    " AND table_name = 'charge_" + mReval + "'"*/;
                    if (ExecSQLToTable(sql).Rows.Count == 0)
                    {
                        string mess = "Схемы " + finder.bank + "_charge_" + yReval.Substring(2, 2) +
                                      " не существует в базе";
                        AddComment(mess);
                    }
                    else
                    {
                        sql =
                            " SELECT reval " +
                            " FROM " + pref + "_charge_" + yReval.Substring(2, 2) + ".charge_" + mReval +
                            " WHERE dat_charge = '" + finder.year + "-" + month + "-28'" +
                            " AND num_ls = " + num_ls + 
                            " AND nzp_serv = " + nzp_serv + 
                            " AND nzp_supp = " + nzp_supp;
                    DataTable dt1 = ExecSQLToTable(sql);
                        foreach (DataRow rr1 in dt1.Rows)
                        {
                            reval += Convert.ToDouble(rr1["reval"]);
                        }
                    }
                }
            }
            return reval;
        }
    }
}
