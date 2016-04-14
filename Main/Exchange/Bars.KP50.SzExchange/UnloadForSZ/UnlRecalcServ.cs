using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;

namespace Bars.KP50.SzExchange.UnloadForSZ
{
    /// <summary>
    /// Выгрузка перерасчетов по услуге
    /// </summary>
    public class UnlRecalcServ: BaseUnloadClass
    {
        public DateTime time;
        public override int Code
        {
            get
            {
                return 5;
            }
        }

        public override string Name
        {
            get { return "UnlRecalcServ"; }
        }

        public override string NameText
        {
            get { return "Перерасчет по услуге"; }
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

        public void Start(FilesImported finder, int nzp_kvar, int nzp_supp, int nzp_serv, int num_ls)
        {
            string sql;
            string str;
            string sep = "|";
            DateTime time_finish = DateTime.Now;

            OpenConnection();
            CreateTempTable();

            WriteInFile w = new WriteInFile();
            UnlRecalcServ urs = new UnlRecalcServ();

            try
            {
                WriteInRecalcServ(finder, nzp_kvar, nzp_supp, nzp_serv, urs); //запись во временную таблицу

                //выборка данных из временной таблицы 
                sql =
                    " SELECT * FROM " + Name;
                foreach (DataRow rr in ExecSQLToTable(sql).Rows)
                {
                    str = Code + sep + rr["month_and_year_pere"].ToString().Substring(0,10) + sep + rr["eot"] + sep + rr["reg_tarif"] + sep +
                          rr["nzp_measure"] + sep + rr["fact_rashod"] + sep + rr["norm_rashod"] + sep +
                          rr["is_pu_calc"] + sep + rr["sum_nach_pere"] + sep + rr["sum_subsidi_pere"] + sep +
                          rr["sum_lg_pere"] + sep + rr["sum_smo"] + sep + rr["is_del"] + sep;
                    //запись в файл
                    w.Filing(str, finder.saved_name);


                    time_finish = DateTime.Now;
                    string tinfo = " выгрузка перерасчетов по услуге: " + nzp_serv + ", поставщик: " + nzp_supp + ", время выполнения: " + GetTime(urs.time, time_finish);
                    w.Filing(tinfo, finder.saved_name_log);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("UnlRecalcServ.Start(pref): Ошибка добавления полей в таблицу.\n: " + ex.Message, MonitorLog.typelog.Error, true);
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

           sql =" CREATE TEMP TABLE " + Name + "(" +
                " month_and_year_pere DATE , " +
                " eot DECIMAL (12,2) , " +
                " reg_tarif DECIMAL (12,2) , " +
                " nzp_measure INTEGER , " +
                " fact_rashod DECIMAL (12,2) , " +
                " norm_rashod DECIMAL (12,2) , " +
                " is_pu_calc INTEGER , " +
                " sum_nach_pere DECIMAL (12,2) , " +
                " sum_subsidi_pere DECIMAL (12,2) , " +
                " sum_lg_pere DECIMAL (12,2) , " +
                " sum_smo DECIMAL (12,2) , " +
                " is_del DECIMAL (12,2), " +
                " nzp_frm INTEGER" + ")";
           ExecSQL(sql, false);
           
        }

        /// <summary>
        /// Запись во временную таблицу
        /// </summary>
        /// <param name="pref"></param>
        public void WriteInRecalcServ(FilesImported finder, int nzp_kvar, int nzp_supp, int nzp_serv, UnlRecalcServ urs)
        {
            string pref = finder.bank;
            string year = finder.year.Substring(2, 2);
            string month = finder.month.PadLeft(2, '0');
            string sql;
            string yReval;
            string mReval;

            urs.Time = DateTime.Now;

            //Сохраняем месяц и год перерасчета
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
                    if (ExecSQLToTable(sql).Rows.Count != 0)
                    {
                    //    string mess = "Схемы " + finder.bank + "_charge_" + yReval.Substring(2, 2) + " не существует в базе";
                    //    AddComment(mess);
                    //}
                    //else
                    //{
                        sql =
                            " INSERT INTO " + Name +
                            " SELECT '" + yReval + "-" + mReval + "-28'" + ", tarif, tarif_f," +
                            " 0 as nzp_measure, c_calc, c_sn, " +
                            " is_device, reval, 0," +
                            " 0, 0, isdel, nzp_frm " +
                            " FROM " + pref + "_charge_" + Convert.ToString(yReval).Substring(2, 2) + ".charge_" +
                            mReval +
                            " a " +
                            " WHERE dat_charge = '" + finder.year + "-" + month + "-28'" +
                            " AND nzp_kvar = " + nzp_kvar +
                            " AND nzp_supp = " + nzp_supp +
                            " AND nzp_serv = " + nzp_serv;
                        ExecSQL(sql, true);

                    }
                }
            }

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

            CheckColumnOnEmptiness("nzp_measure", "zero", "0-и в строке " + NameText + ", поле 'Код ед. измерения'", true, "7");
        }

        /// <summary>
        /// Запись во временную таблицу
        /// </summary>
        public void WriteInRecalcServ()
        {

        }

    }
}
