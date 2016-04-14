using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using Bars.KP50.Load.Obninsk.CountersUnload.Interfaces;
using Bars.KP50.Load.Obninsk.Progress.Interfaces;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.Load.Obninsk
{
    public class UnloadLastValueCounters : ConnectionToDB, IUnloadCounters
    {
        private IBaseLoadProtokol protokol;
        private IProgressWork<ProgressEventArgs> progress;
        private string unloadedFileName;
        private StreamWriter writer;
        private string lastCountersValsTempTable = "temp_cnt";
        private string domAdresTableTemp;
        private int nzp_exc;
        public string Name
        {
            get { return "Файл выгрузки последних показаний ПУ"; }
        }

        public string Description
        {
            get { return "Файл выгрузки последних показаний ПУ"; }
        }
        public UnloadLastValueCounters(IProgressWork<ProgressEventArgs> progress)
        {
            this.progress = progress;
        }

        public void Init(IBaseLoadProtokol protokol, IDbConnection connection, StreamWriter writer, int nzp_exc)
        {
            Connection = connection;
            this.protokol = protokol;
            this.nzp_exc = nzp_exc;
            progress.RaiseProgressEvent += progressEventHandler;
            this.writer = writer;
            createTempTables();
        }

        private void createTempTables()
        {
            string sql = "create temp table " + lastCountersValsTempTable + " ( " +
                         "pkod numeric (13,0), " +
                         "nzp_kvar integer, " +
                         "nzp_serv integer, " +
                         "nzp_counter integer, " +
                         "num_cnt char(40), " +
                         "val_cnt numeric(10,2), " +
                         "uk varchar(100), " +
                         "adres varchar(1000), " +
                         "last_date_uchet date )" + DBManager.sUnlogTempTable;
            ;
            ExecSQL(sql);
        }

        /// <summary>
        /// Главный метод выгрузки
        /// </summary>
        public void Unload(List<int> list_nzp_wp)
        {
            foreach (int nzp_wp in list_nzp_wp)
            {
                string pref = Points.GetPref(nzp_wp);
                string localCounters = pref + DBManager.sDataAliasRest + "counters";
                string sql = "insert into " + lastCountersValsTempTable + "(pkod, nzp_kvar, nzp_serv, nzp_counter, uk, adres, last_date_uchet) " +
                             "select k.pkod, c.nzp_kvar, c.nzp_serv,c.nzp_counter, a.area," +
                             " t.town||' '||r.rajon||' '||u.ulica||' '||d.ndom||" + DBManager.sNvlWord + "(' кв.'||nkvar,''),  max(dat_uchet)  from " +
                             Points.Pref + DBManager.sDataAliasRest + "dom d, " + Points.Pref + DBManager.sDataAliasRest + "s_ulica u, " +
                             Points.Pref + DBManager.sDataAliasRest + "s_rajon r, " + Points.Pref + DBManager.sDataAliasRest + "s_town t, " +
                             localCounters + " c, " + Points.Pref + DBManager.sDataAliasRest + "kvar k, "
                             + Points.Pref + DBManager.sDataAliasRest + "s_area a " +
                             " where k.nzp_kvar=c.nzp_kvar and c.is_actual<>100  and k.nzp_area=a.nzp_area " +
                             " and k.nzp_dom=d.nzp_dom  and k.is_open='1' and  d.nzp_ul=u.nzp_ul and u.nzp_raj=r.nzp_raj and r.nzp_town=t.nzp_town " +
                             "group by 1,2,3,4,5,6 order by 1,2,3,4,5,6";
                ExecSQL(sql);
                // обновить зав №, и показание счетчиков
                sql = "update " + lastCountersValsTempTable + " l  set num_cnt= c.num_cnt, val_cnt=c.val_cnt from " + localCounters + " c " +
                      " where l.last_date_uchet=c.dat_uchet and l.nzp_kvar=c.nzp_kvar and l.nzp_serv=c.nzp_serv " +
                      " and l.nzp_counter=c.nzp_counter and c.is_actual<>100 ";
                ExecSQL(sql);
            }
            // перенести данные в streamwriter
            passDataToWriter();
        }

        /// <summary>
        /// Передает данные в streamwriter
        /// </summary>
        private void passDataToWriter()
        {
            string sql = "select uk, nzp_kvar, nzp_serv, pkod, adres, nzp_counter, num_cnt, last_date_uchet, val_cnt  from " + lastCountersValsTempTable;
            IDataReader reader;
            ExecRead(out reader, sql);
            DataTable dt = new DataTable();
            dt.Load(reader);
            progress.Init(dt.Rows.Count);
            try
            {
                StringBuilder builder = new StringBuilder();
                foreach (DataRow row in dt.Rows)
                {
                    // УК
                    builder.Remove(0, builder.Length);
                    if (row["uk"] == DBNull.Value || String.IsNullOrWhiteSpace(row["uk"].ToString()))
                    {
                        protokol.AddUnrecognisedRow("ЛС " + row["nzp_kvar"] + ", не указано наименование УК");
                        continue;
                    }
                    builder.Append(row["uk"] + ";");
                    // Платежный код
                    if (row["pkod"] == DBNull.Value)
                    {
                        protokol.AddUnrecognisedRow("У ЛС " + row["nzp_kvar"] + " не указан платежный код ");
                        continue;
                    }
                    Decimal parsedPkod;
                    Decimal.TryParse(row["pkod"].ToString(), out parsedPkod);
                    if (parsedPkod <= 0)
                    {
                        protokol.AddUnrecognisedRow("ЛС " + row["nzp_kvar"] + " имеет не корректный платежный код ");
                        continue;
                    }
                    builder.Append(row["pkod"] + ";");

                    //Адрес
                    if (row["adres"] == DBNull.Value || String.IsNullOrWhiteSpace(row["adres"].ToString()))
                    {
                        protokol.AddUnrecognisedRow("ЛС " + row["nzp_kvar"] + " имеет не корректный адрес");
                        continue;
                    }
                    builder.Append(row["adres"] + ";");
                    // Код счетчика nzp_counter
                    if (row["nzp_counter"] == DBNull.Value)
                    {
                        protokol.AddUnrecognisedRow("ЛС " + row["nzp_kvar"] + ", не определен код счетчика по услуге " + row["nzp_serv"]);
                        continue;
                    }
                    Int32 parsedNzpCounter;
                    Int32.TryParse(row["nzp_counter"].ToString(), out parsedNzpCounter);
                    if (parsedNzpCounter <= 0)
                    {
                        protokol.AddUnrecognisedRow("ЛС " + row["nzp_kvar"] + ", услуга " + row["nzp_serv"] + ", не корректный код счетчика ");
                        continue;
                    }
                    builder.Append(row["nzp_counter"] + ";");
                    // зав. номер счетчика
                    if (row["num_cnt"] == DBNull.Value || String.IsNullOrWhiteSpace(row["num_cnt"].ToString()))
                    {
                        protokol.AddUnrecognisedRow("ЛС " + row["nzp_kvar"] + ", услуга " + row["nzp_serv"] + ", код счетчика " + row["nzp_counter"]
                                                    + " не корректный зав. № счетчика");
                        continue;
                    }
                    builder.Append(row["num_cnt"].ToString().Trim() + ";");
                    // дата последнего показания
                    if (row["last_date_uchet"] == DBNull.Value)
                    {
                        protokol.AddUnrecognisedRow("ЛС " + row["last_date_uchet"] + ", услуга " + row["nzp_serv"] + ", код счетчика " + row["nzp_counter"] + " зав. № " + row["num_cnt"] +
                                                    " не определена дата ввода последнего показания");
                        continue;
                    }
                    DateTime parsed_last_date_uchet;
                    if (!DateTime.TryParse(row["last_date_uchet"].ToString(), out parsed_last_date_uchet))
                    {
                        protokol.AddUnrecognisedRow("ЛС " + row["last_date_uchet"] + ", услуга " + row["nzp_serv"] + ", код счетчика " + row["nzp_counter"] + " зав. № " + row["num_cnt"] +
                                                    " не корректная дата ввода последнего показания");
                        continue;
                    }
                    builder.Append(parsed_last_date_uchet.ToShortDateString() + ";");
                    // показание счетчика
                    if (row["val_cnt"] == DBNull.Value)
                    {
                        protokol.AddUnrecognisedRow("ЛС " + row["nzp_kvar"] + ", услуга " + row["nzp_serv"] + ", код счетчика " + row["nzp_counter"]
                                                    + ", зав № " + row["num_cnt"] + " не корректное показание счетчика");
                        continue;
                    }
                    builder.Append(row["val_cnt"]);
                    writer.WriteLine(builder.ToString());
                    protokol.CountInsertedRows++;
                    progress.IncrementProgress(protokol.CountInsertedRows);
                }
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                }
            }
        }

        /// <summary>
        /// Обработчик события обновления прогресса
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void progressEventHandler(object sender, ProgressEventArgs e)
        {

            string sqlStr = " Update  " + DBManager.sDefaultSchema + "excel_utility " +
                            " set progress =" + e.Progress + "," +
                            " stats = " + (int) ExcelUtility.Statuses.InProcess +
                            " where nzp_exc=" + nzp_exc;
            ExecSQL(sqlStr);
        }

        public void Dispose()
        {
            ExecSQL("drop table " + lastCountersValsTempTable, false);
        }
    }
}
