using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using STCLINE.KP50.Global;
using FastReport;
using System.IO;
using System.Data.OleDb;
using SevenZip;
using System.Reflection;
using System.Data.Odbc;
using Globals.SOURCE.Utility;

using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Utility;
using Bars.KP50.Utils;

namespace STCLINE.KP50.DataBase
{
    //Класс для получения данных из генератора отчетов
    public partial class ExcelRep : ExcelRepClient
    {
        /// <summary>
        /// Выгрузка файла обмена
        /// </summary>
        /// <returns></returns>
        public Returns GenerateExchange(out Returns ret, SupgFinder finder)
        {
            ret = Utils.InitReturns();

            var month = finder.adr;
            if (finder.adr.Length == 1)
                month = "0" + finder.adr;
            var year = finder.area;

            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                ret.result = false;
                return ret;
            }

            string fn1 = "";
            //путь, по которому скачивается файл
            string path = "";
            //Имя файла отчета
            string fileNameIn = "Exchange_" + DateTime.Now.Ticks;
            ExcelRep excelRepDb = new ExcelRep();
            StringBuilder sql = new StringBuilder();

            //запись в БД о постановки в поток(статус 0)
            ret = excelRepDb.AddMyFile(new ExcelUtility()
            {
                nzp_user = finder.nzp_user,
                status = ExcelUtility.Statuses.InProcess,
                rep_name = "Выгрузка файла обмена"
            });
            if (!ret.result) return ret;

            int nzpExc = ret.tag;

            IDbConnection conn_db = null;
            MyDataReader reader = null;
            int num = 1;
            int servCount = 0;

            DateTime DATDOLG = DateTime.Parse("01.01.1900");

            string secComm = "";
            OleDbCommand Command = new OleDbCommand();

            var dir = "FilesExchange\\files\\";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            var resDir = STCLINE.KP50.Global.Constants.ExcelDir.Replace("/", "\\");
            if (!Directory.Exists(resDir)) Directory.CreateDirectory(resDir);
            var fullPath = AppDomain.CurrentDomain.BaseDirectory;

            try
            {
                IDbConnection conn_web = DBManager.newDbConnection(Constants.cons_Webdata);
                ret = OpenDb(conn_web, true);
                if (!ret.result)
                {
                    return ret;
                }

                conn_db = DBManager.newDbConnection(Constants.cons_Kernel);
                ret = OpenDb(conn_db, true);
                if (!ret.result)
                {
                    return ret;
                }

                DataTable resTable = new DataTable(), tmpHeaderTable = new DataTable(), tmpServTable = new DataTable();
                string strComm = "";
                string sqlStr = "";


                //определение максимального кол-ва услуг
                foreach (var bank in Points.PointList)
                {
                    var charge = bank.pref + "_charge_" + year.Substring(2, 2) + tableDelimiter+"charge_" + month;
                    //для нормальной базы
                    sqlStr = "SELECT COUNT(*) AS cnt FROM " + charge +
                                    " where nzp_serv <> 1 GROUP BY nzp_kvar ORDER BY cnt DESC";

                    var tmpMax = Convert.ToInt32(ClassDBUtils.OpenSQL(sqlStr, conn_db).resultData.Rows[0][0]);
                    if (tmpMax > servCount)
                        servCount = tmpMax;

                    servCount = 13;
                }

                #region Создание DBF файла
                //Заголовок
                //strComm = "CREATE TABLE [new.DBF] (ID double,PKU char(11), FAMIL char(100), IMJA char(100),OTCH char(100),SNILS char(20),DROG datetime, " +
                //            " DATN datetime,PRED char(100),FRA_REG_ID double,POSEL char(100),NASP char(100), YLIC char(100),NDOM char(100),NKORP char(100),NKW char(100),NKOMN char(50),ILCHET char(100), " +
                //            " FAMIL_LCH char(100),IMJA_LCH char(100),OTCH_LCH char(100),SNILS_LCH char(100),DROG_LCH datetime) ";
                //Command = new OleDbCommand(strComm, Connection);
                //Command.ExecuteNonQuery();
                //Command.Dispose();

                exDBF eDBF = new exDBF("new");
                eDBF.AddColumn("ID", typeof(decimal), 38, 0);
                eDBF.AddColumn("PKU", typeof(string), 11, 0);
                eDBF.AddColumn("FAMIL", typeof(string), 100, 0);
                eDBF.AddColumn("IMJA", typeof(string), 100, 0);
                eDBF.AddColumn("OTCH", typeof(string), 100, 0);
                eDBF.AddColumn("SNILS", typeof(string), 20, 0);
                eDBF.AddColumn("DROG", typeof(DateTime), 0, 0);
                eDBF.AddColumn("DATN", typeof(DateTime), 0, 0);
                eDBF.AddColumn("PRED", typeof(string), 100, 0);
                eDBF.AddColumn("FRA_REG_ID", typeof(double), 38, 5);
                eDBF.AddColumn("POSEL", typeof(string), 100, 0);
                eDBF.AddColumn("NASP", typeof(string), 100, 0);
                eDBF.AddColumn("YLIC", typeof(string), 100, 0);
                eDBF.AddColumn("NDOM", typeof(string), 100, 0);
                eDBF.AddColumn("NKORP", typeof(string), 100, 0);
                eDBF.AddColumn("NKW", typeof(string), 100, 0);
                eDBF.AddColumn("NKOMN", typeof(string), 50, 0);
                eDBF.AddColumn("ILCHET", typeof(string), 100, 0);
                eDBF.AddColumn("FAMIL_LCH", typeof(string), 100, 0);
                eDBF.AddColumn("IMJA_LCH", typeof(string), 100, 0);
                eDBF.AddColumn("OTCH_LCH", typeof(string), 100, 0);
                eDBF.AddColumn("SNILS_LCH", typeof(string), 100, 0);
                eDBF.AddColumn("DROG_LCH", typeof(string), 100, 0);

                //Услуги
                for (int j = 1; j <= servCount; j++)
                {
                    //strComm = "ALTER TABLE [new] ADD COLUMN KGKYSL_" + j + " double";
                    //Command = new OleDbCommand(strComm, Connection);
                    //Command.ExecuteNonQuery();
                    //Command.Dispose();
                    eDBF.AddColumn("KGKYSL_" + j, typeof(decimal), 38, 5);

                    //strComm = "ALTER TABLE [new] ADD COLUMN GKYSL_" + j + " char(100)";
                    //Command = new OleDbCommand(strComm, Connection);
                    //Command.ExecuteNonQuery();
                    //Command.Dispose();
                    eDBF.AddColumn("GKYSL_" + j, typeof(string), 100, 0);

                    //strComm = "ALTER TABLE [new] ADD COLUMN NGKYSL1_" + j + " char(100)";
                    //Command = new OleDbCommand(strComm, Connection);
                    //Command.ExecuteNonQuery();
                    //Command.Dispose();
                    eDBF.AddColumn("NGKYSL1_" + j, typeof(string), 100, 0);

                    //strComm = "ALTER TABLE [new] ADD COLUMN NGKYSL2_" + j + " char(100)";
                    //Command = new OleDbCommand(strComm, Connection);
                    //Command.ExecuteNonQuery();
                    //Command.Dispose();
                    eDBF.AddColumn("NGKYSL2_" + j, typeof(string), 100, 0);

                    //strComm = "ALTER TABLE [new] ADD COLUMN LCHET_" + j + " char(50)";
                    //Command = new OleDbCommand(strComm, Connection);
                    //Command.ExecuteNonQuery();
                    //Command.Dispose();
                    eDBF.AddColumn("LCHET_" + j, typeof(string), 100, 0);

                    //strComm = "ALTER TABLE [new] ADD COLUMN TARIF1_" + j + " double";
                    //Command = new OleDbCommand(strComm, Connection);
                    //Command.ExecuteNonQuery();
                    //Command.Dispose();
                    eDBF.AddColumn("TARIF1_" + j, typeof(decimal), 38, 5);

                    //strComm = "ALTER TABLE [new] ADD COLUMN TARIF2_" + j + " double";
                    //Command = new OleDbCommand(strComm, Connection);
                    //Command.ExecuteNonQuery();
                    //Command.Dispose();
                    eDBF.AddColumn("TARIF2_" + j, typeof(decimal), 38, 5);

                    //strComm = "ALTER TABLE [new] ADD COLUMN FAKT_" + j + " double";
                    //Command = new OleDbCommand(strComm, Connection);
                    //Command.ExecuteNonQuery();
                    //Command.Dispose();
                    eDBF.AddColumn("FAKT_" + j, typeof(decimal), 38, 5);

                    //strComm = "ALTER TABLE [new] ADD COLUMN SUMTAR_" + j + " double";
                    //Command = new OleDbCommand(strComm, Connection);
                    //Command.ExecuteNonQuery();
                    //Command.Dispose();
                    eDBF.AddColumn("SUMTAR_" + j, typeof(decimal), 38, 5);

                    //strComm = "ALTER TABLE [new] ADD COLUMN SUMOPL_" + j + " double";
                    //Command = new OleDbCommand(strComm, Connection);
                    //Command.ExecuteNonQuery();
                    //Command.Dispose();
                    eDBF.AddColumn("SUMOPL_" + j, typeof(decimal), 38, 5);

                    //strComm = "ALTER TABLE [new] ADD COLUMN SUMLGT_" + j + " double";
                    //Command = new OleDbCommand(strComm, Connection);
                    //Command.ExecuteNonQuery();
                    //Command.Dispose();
                    eDBF.AddColumn("SUMLGT_" + j, typeof(decimal), 38, 5);

                    //strComm = "ALTER TABLE [new] ADD COLUMN SUMDOLG_" + j + " double ";
                    //Command = new OleDbCommand(strComm, Connection);
                    //Command.ExecuteNonQuery();
                    //Command.Dispose();
                    eDBF.AddColumn("SUMDOLG_" + j, typeof(decimal), 38, 5);

                    //strComm = "ALTER TABLE [new] ADD COLUMN OPLDOLG_" + j + " double";
                    //Command = new OleDbCommand(strComm, Connection);
                    //Command.ExecuteNonQuery();
                    //Command.Dispose();
                    eDBF.AddColumn("OPLDOLG_" + j, typeof(decimal), 38, 5);

                    //strComm = "ALTER TABLE [new] ADD COLUMN DATDOLG_" + j + " datetime";
                    //Command = new OleDbCommand(strComm, Connection);
                    //Command.ExecuteNonQuery();
                    //Command.Dispose();
                    eDBF.AddColumn("DATDOLG_" + j, typeof(DateTime), 8, 0);

                    //strComm = "ALTER TABLE [new] ADD COLUMN KOLDOLG_" + j + " double";
                    //Command = new OleDbCommand(strComm, Connection);
                    //Command.ExecuteNonQuery();
                    //Command.Dispose();
                    eDBF.AddColumn("KOLDOLG_" + j, typeof(decimal), 38, 5);

                    //strComm = "ALTER TABLE [new] ADD COLUMN PRIZN_" + j + " double";
                    //Command = new OleDbCommand(strComm, Connection);
                    //Command.ExecuteNonQuery();
                    //Command.Dispose();
                    eDBF.AddColumn("PRIZN_" + j, typeof(decimal), 38, 5);

                    //strComm = "ALTER TABLE [new] ADD COLUMN KOLLGTP_" + j + " double";
                    //Command = new OleDbCommand(strComm, Connection);
                    //Command.ExecuteNonQuery();
                    //Command.Dispose();
                    eDBF.AddColumn("KOLLGTP_" + j, typeof(decimal), 38, 5);

                    //strComm = "ALTER TABLE [new] ADD COLUMN KOLLGT_" + j + " double";
                    //Command = new OleDbCommand(strComm, Connection);
                    //Command.ExecuteNonQuery();
                    //Command.Dispose();
                    eDBF.AddColumn("KOLLGT_" + j, typeof(decimal), 38, 5);

                    //strComm = "ALTER TABLE [new]  ADD COLUMN KOLZR_" + j + " double";
                    //Command = new OleDbCommand(strComm, Connection);
                    //Command.ExecuteNonQuery();
                    //Command.Dispose();
                    eDBF.AddColumn("KOLZR_" + j, typeof(decimal), 38, 5);
                }

                eDBF.Save(dir, 866);

                #endregion

                OleDbConnection Connection = new OleDbConnection();
                var myConnectionString = @"Provider=Microsoft.ACE.OLEDB.12.0;" +
                             "Data Source=" + fullPath + dir + ";Extended Properties=dBASE III;";
                Connection.ConnectionString = myConnectionString;
                Connection.Open();

                List<int> servList = new List<int>();

                #region Получение данных
                foreach (var bank in Points.PointList)
                {
                    //получение заголовка
                    sqlStr = " SELECT "+
#if PG
                        
#else
                        " first 1000 "+
#endif
                        "distinct kt.nzp_gil as ID, kt.fam as FAMIL, kt.ima as IMJA, kt.otch as OTCH, kt.dat_rog as DROG, town.town as POSEl, ulica.ulica as YLIC, " +
                            " dom.ndom as NDOM, dom.nkor as NKORP, kv.nkvar as NKW, kt.dat_rog as DROG_LCH, sup.name_supp as PRED, kv.nzp_kvar as FRA_reg_id, " +
                            " (case when dom.nzp_raj <> -1 then raj.rajon else '' end) as NASP, kt.fam as FAMIL_LCH, kt.ima as IMJA_LCH, kt.otch as OTCH_LCH " +
                        " FROM " + bank.pref + "_data"+tableDelimiter + "kart kt, " + bank.pref + "_data"+tableDelimiter + "kvar kv, " + bank.pref + "_data"+tableDelimiter + "dom dom, " + bank.pref + "_data"+tableDelimiter + "s_rajon raj, " +
                                bank.pref + "_data" + tableDelimiter + "s_town town, " + bank.pref + "_data" + tableDelimiter + "s_ulica ulica, " + bank.pref + "_kernel" + tableDelimiter + "supplier sup " +
                        " WHERE kt.nzp_tkrt = 1 and kt.isactual = '1' and "
#if PG
                         + "EXTRACT (MONTH FROM kt.dat_ofor) = " + month +
#else
                        +" MONTH(kt.dat_ofor) = " + month + 
#endif
                        " and kt.nzp_kvar = kv.nzp_kvar and " +
                       " kv.nzp_dom = dom.nzp_dom and dom.nzp_town = town.nzp_town and dom.nzp_ul = ulica.nzp_ul and sup.nzp_supp = 1 " +
                        " and dom.nzp_raj = (case when dom.nzp_raj <> -1 then raj.nzp_raj else dom.nzp_raj end)" +
#if PG
                        " limit 1000 " +
#else                        
#endif  
                    "";

                    tmpHeaderTable = ClassDBUtils.OpenSQL(sqlStr, conn_db).resultData;
                    //resTable.Merge(tmpHeaderTable);
                    num += resTable.Rows.Count;

                    MonitorLog.WriteLog(sqlStr, MonitorLog.typelog.Warn, true);

                    //получение услуг
                    for (int i = 0; i < tmpHeaderTable.Rows.Count; i++)
                    {
                        sqlStr = " select distinct p.point as GKYSL, srv.service_name as NGKYSL1, service_small as NGKYSL2, ch.num_ls as LCHET, max(ch.tarif) as TARIF1, " +
                                 " max(ch.tarif_f) as TARIF2, max(ch.sum_tarif) as SUMTAR, max(ch.c_calc) as FAKT, max(ch.sum_money) as SUMOPL, max(ch.rsum_lgota) as SUMLGT, ch.nzp_serv, " +
                                 " max((" + Points.Pref + "_data"+tableDelimiter + "get_kol_gil('01." + month + "." + year + "','" +
                                 DateTime.DaysInMonth(Convert.ToInt32(year), Convert.ToInt32(month)) +
                                 "." + month + "." + year + "', 15, " + tmpHeaderTable.Rows[i]["fra_reg_id"] + "))) as KOLZR, max(ch.sum_insaldo) as SUMDOLG, max(ch.sum_money) as OPLDOLG " +
                                 " from " + bank.pref + "_charge_" + year.Substring(2, 2) +tableDelimiter+ "charge_" + month + " ch, " +
                                 " " + Points.Pref + "_kernel" + tableDelimiter + "s_point p, " + Points.Pref + "_kernel" + tableDelimiter + "services srv " +
                                 " where nzp_kvar = " + tmpHeaderTable.Rows[i]["fra_reg_id"] + " and p.bd_kernel = '" + bank.pref + "' and srv.nzp_serv = ch.nzp_serv and ch.nzp_serv <> 1 " +
                                 " group by 1, 2, 3, 4, ch.nzp_serv ";
                        tmpServTable = ClassDBUtils.OpenSQL(sqlStr, conn_db).resultData;



                        // Заполнения листа с номерами услуг                       
                        if (servList.Count == 0)
                        {
                            foreach (DataRow row in tmpServTable.Rows)
                            {
                                servList.Add(Convert.ToInt32(row["nzp_serv"]));
                            }
                        }

                        for (int j = 0; j < tmpServTable.Rows.Count; j++)
                        {
                            var index = servList.IndexOf(Convert.ToInt32(tmpServTable.Rows[j]["nzp_serv"]));
                            if (index == -1)
                            {
                                servList.Add(Convert.ToInt32(tmpServTable.Rows[j]["nzp_serv"]));
                            }
                        }

                        #region формирование инсерта в DBF файл
                        //Заголовок
                        strComm = "insert into new.dbf (ID, PKU , FAMIL, IMJA, OTCH,SNILS, DROG, DATN, PRED, FRA_REG_ID, POSEL, NASP, YLIC, NDOM, NKORP, NKW, NKOMN, ILCHET, " +
                            " FAMIL_LCH, IMJA_LCH, OTCH_LCH, SNILS_LCH, DROG_LCH ";

                        #region услуги
                        secComm = "";

                        //#region исключение лс с 13 услугами
                        //bool cont_bool = false;
                        //if (servList.Count > 12)
                        //{
                        //    foreach (DataRow item in tmpServTable.Rows)
                        //    {
                        //        if (Convert.ToInt32(item["nzp_serv"]) == servList[11])
                        //        {
                        //            cont_bool = true;
                        //            break;
                        //        }
                        //    }
                        //    if (cont_bool)
                        //        continue;
                        //}

                        //if (tmpServTable.Rows.Count > 3)
                        //    continue;
                        //#endregion


                        for (int j = 0; j < tmpServTable.Rows.Count; j++)
                        {
                            var b = -2;
                            var index = servList.IndexOf(Convert.ToInt32(tmpServTable.Rows[j]["nzp_serv"]));
                            if (index != -1)
                            {
                                b = index;
                            }
                            else
                            {
                                servList.Add(Convert.ToInt32(tmpServTable.Rows[j]["nzp_serv"]));
                                b = servList.IndexOf(Convert.ToInt32(tmpServTable.Rows[j]["nzp_serv"]));
                            }

                            #region нормальный вариант
                            //strComm += ", KGKYSL_" + (b + 1) + ", GKYSL_" + (b + 1) + ", NGKYSL1_" + (b + 1) + ", NGKYSL2_" + (b + 1) + ", LCHET_" + (b + 1) + ", TARIF1_" + (b + 1) + ", TARIF2_" + (b + 1) +
                            //        ", FAKT_" + (b + 1) + ", SUMTAR_" + (b + 1) + " , SUMOPL_" + (b + 1) + ", SUMLGT_" + (b + 1) +// ", SUMDOLG_" + (b + 1) + ", OPLDOLG_" + (b + 1) + ", DATDOLG_" + (b + 1) +
                            //    /*", KOLDOLG_" + (b + 1) +*/ ", PRIZN_" + (b + 1) + ", KOLLGTP_" + (b + 1) + ", KOLLGT_" + (b + 1) + ", KOLZR_" + (b + 1);
                            //secComm += ", 0, " +
                            //    " '" + ((string)tmpServTable.Rows[j]["GKYSL"]).Trim() + "', " +
                            //    " '" + ((string)tmpServTable.Rows[j]["NGKYSL1"]).Trim() + "', " +
                            //    " '" + ((string)tmpServTable.Rows[j]["NGKYSL2"]).Trim() + "', " +
                            //    " '" + (tmpServTable.Rows[j]["LCHET"]).ToString() + "', " +
                            //    " " + (Convert.ToDecimal(tmpServTable.Rows[j]["TARIF1"])) + ", " +
                            //    " " + (Convert.ToDecimal(tmpServTable.Rows[j]["TARIF2"])) + ", " +
                            //    " " + (Convert.ToDecimal(tmpServTable.Rows[j]["FAKT"])) + ", " +
                            //    " " + (Convert.ToDecimal(tmpServTable.Rows[j]["SUMTAR"])) + ", " +
                            //    " " + (Convert.ToDecimal(tmpServTable.Rows[j]["SUMOPL"])) + ", " +
                            //    " " + (Convert.ToDecimal(tmpServTable.Rows[j]["SUMLGT"])) + ", " +
                            //    // " " + (Convert.ToDecimal(tmpServTable.Rows[j]["SUMDOLG"])) + ", " +
                            //    //" " + (Convert.ToDecimal(tmpServTable.Rows[j]["OPLDOLG"])) + ", " +
                            //    //" DATE(" + year + "," + finder.adr + ",1), " +
                            //    //" 1, " +
                            //    " 0, " +
                            //    " 0, " +
                            //    " 0, " +
                            //    " " + (Convert.ToDecimal(tmpServTable.Rows[j]["KOLZR"])) + " ";
                            #endregion

                            #region подгонка
                            decimal fakt = 0;
                            if ((Convert.ToDecimal(tmpServTable.Rows[j]["TARIF1"])) != 0)
                            {
                                fakt = (Convert.ToDecimal(tmpServTable.Rows[j]["SUMTAR"])) / (Convert.ToDecimal(tmpServTable.Rows[j]["TARIF1"]));
                            }
                            strComm += ", KGKYSL_" + (b + 1) + ", GKYSL_" + (b + 1) + ", NGKYSL1_" + (b + 1) + ", NGKYSL2_" + (b + 1) + ", LCHET_" + (b + 1) + ", TARIF1_" + (b + 1) + ", TARIF2_" + (b + 1) +
                                    ", FAKT_" + (b + 1) + ", SUMTAR_" + (b + 1) + " , SUMOPL_" + (b + 1) + ", SUMLGT_" + (b + 1) +// ", SUMDOLG_" + (b + 1) + ", OPLDOLG_" + (b + 1) + ", DATDOLG_" + (b + 1) +
                                /*", KOLDOLG_" + (b + 1) +*/ ", PRIZN_" + (b + 1) + ", KOLLGTP_" + (b + 1) + ", KOLLGT_" + (b + 1) + ", KOLZR_" + (b + 1);
                            secComm += ", 0, " +
                                " '" + ((string)tmpServTable.Rows[j]["GKYSL"]).Trim() + "', " +
                                " '" + ((string)tmpServTable.Rows[j]["NGKYSL1"]).Trim() + "', " +
                                " '" + ((string)tmpServTable.Rows[j]["NGKYSL2"]).Trim() + "', " +
                                " '" + (tmpServTable.Rows[j]["LCHET"]).ToString() + "', " +
                                " " + (Convert.ToDecimal(tmpServTable.Rows[j]["TARIF1"])) + ", " +
                                " " + (Convert.ToDecimal(tmpServTable.Rows[j]["TARIF2"])) + ", " +
                                " " + fakt + ", " +
                                " " + (Convert.ToDecimal(tmpServTable.Rows[j]["SUMTAR"])) + ", " +
                                " " + (Convert.ToDecimal(tmpServTable.Rows[j]["SUMTAR"])) + ", " +
                                //" " + (Convert.ToDecimal(tmpServTable.Rows[j]["SUMOPL"])) + ", " +
                                " " + (Convert.ToDecimal(tmpServTable.Rows[j]["SUMLGT"])) + ", " +
                                // " " + (Convert.ToDecimal(tmpServTable.Rows[j]["SUMDOLG"])) + ", " +
                                //" " + (Convert.ToDecimal(tmpServTable.Rows[j]["OPLDOLG"])) + ", " +
                                //" DATE(" + year + "," + finder.adr + ",1), " +
                                //" 1, " +
                                " 0, " +
                                " 0, " +
                                " 0, " +
                                " " + (Convert.ToDecimal(tmpServTable.Rows[j]["KOLZR"])) + " ";
                            #endregion
                        }

                        #endregion
                        strComm += ") values(" +
                                " " + (tmpHeaderTable.Rows[i]["ID"] != DBNull.Value ? (Convert.ToDecimal(tmpHeaderTable.Rows[i]["ID"])) : 0) + ", " +
                                " '0', " +
                                " '" + (tmpHeaderTable.Rows[i]["FAMIL"] != DBNull.Value ? ((string)tmpHeaderTable.Rows[i]["FAMIL"]).Trim() : "") + "', " +
                                " '" + (tmpHeaderTable.Rows[i]["IMJA"] != DBNull.Value ? ((string)tmpHeaderTable.Rows[i]["IMJA"]).Trim() : "") + "', " +
                                " '" + (tmpHeaderTable.Rows[i]["OTCH"] != DBNull.Value ? ((string)tmpHeaderTable.Rows[i]["OTCH"]).Trim() : "") + "', " +
                                " '0', " +
                                " '" + Convert.ToDateTime(tmpHeaderTable.Rows[i]["DROG"]) + "', " +
                                " '01." + finder.adr + "." + year + "', " +
                                " '" + (tmpHeaderTable.Rows[i]["PRED"] != DBNull.Value ? ((string)tmpHeaderTable.Rows[i]["PRED"]).Trim() : "") + "', " +
                                " " + (tmpHeaderTable.Rows[i]["FRA_REG_ID"] != DBNull.Value ? (Convert.ToDecimal(tmpHeaderTable.Rows[i]["FRA_REG_ID"])) : 0) + ", " +
                                " '" + (tmpHeaderTable.Rows[i]["POSEL"] != DBNull.Value ? ((string)tmpHeaderTable.Rows[i]["POSEL"]).Trim() : "") + "', " +
                                " '" + (tmpHeaderTable.Rows[i]["NASP"] != DBNull.Value ? ((string)tmpHeaderTable.Rows[i]["NASP"]).Trim() : "") + "', " +
                                " '" + (tmpHeaderTable.Rows[i]["YLIC"] != DBNull.Value ? ((string)tmpHeaderTable.Rows[i]["YLIC"]).Trim() : "") + "', " +
                                " '" + (tmpHeaderTable.Rows[i]["NDOM"] != DBNull.Value ? ((string)tmpHeaderTable.Rows[i]["NDOM"]).Trim() : "") + "', " +
                                " '" + (tmpHeaderTable.Rows[i]["NKORP"] != DBNull.Value ? ((string)tmpHeaderTable.Rows[i]["NKORP"]).Trim() : "") + "', " +
                                " '" + (tmpHeaderTable.Rows[i]["NKW"] != DBNull.Value ? ((string)tmpHeaderTable.Rows[i]["NKW"]).Trim() : "") + "', " +
                                " '1', " +
                                " ' ', " +
                                " '" + (tmpHeaderTable.Rows[i]["FAMIL_LCH"] != DBNull.Value ? ((string)tmpHeaderTable.Rows[i]["FAMIL_LCH"]).Trim() : "") + "', " +
                                " '" + (tmpHeaderTable.Rows[i]["IMJA_LCH"] != DBNull.Value ? ((string)tmpHeaderTable.Rows[i]["IMJA_LCH"]).Trim() : "") + "', " +
                                " '" + (tmpHeaderTable.Rows[i]["OTCH_LCH"] != DBNull.Value ? ((string)tmpHeaderTable.Rows[i]["OTCH_LCH"]).Trim() : "") + "', " +
                                " ' ', " +
                                " '" + Convert.ToDateTime(tmpHeaderTable.Rows[i]["DROG_LCH"]) + "' " + secComm + ")";

                        OleDbCommand cmd_insert = new OleDbCommand(strComm, Connection);

                        cmd_insert.ExecuteNonQuery(); //выполняем запрос
                        cmd_insert.Dispose();

                        //if (i % 100 == 0) excelRepDb.SetMyFileProgress(new ExcelUtility() { nzp_exc = nzpExc, progress = ((decimal)i) / num });
                        #endregion
                    }
                }
                #endregion
                Connection.Close();

                //перенос файла на клиент                
                File.Copy(dir + "new.dbf", resDir + fileNameIn + ".dbf");
                if (InputOutput.useFtp) fn1 = InputOutput.SaveInputFile(fullPath + dir +  "new.dbf");
               
               
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка в функции GenerateExchange:\n" + ex.Message, MonitorLog.typelog.Error, true);
            }
            finally
            {
                File.Delete(dir + "new.dbf");
                if (ret.result)
                {
                    excelRepDb.SetMyFileProgress(new ExcelUtility() { nzp_exc = nzpExc, progress = 1 });
                    excelRepDb.SetMyFileState(new ExcelUtility() { nzp_exc = nzpExc, status = ExcelUtility.Statuses.Success, exc_path = InputOutput.useFtp ? fn1 : path + fileNameIn + ".DBF" });
                }
                else
                {
                    excelRepDb.SetMyFileState(new ExcelUtility() { nzp_exc = nzpExc, status = ExcelUtility.Statuses.Failed });
                }
                excelRepDb.Close();

                if (reader != null) reader.Close();
                if (conn_db != null) conn_db.Close();
            }

            return ret;
        }

        /// <summary>
        /// Выгрузка начислений УЭС
        /// </summary>
        /// <returns></returns>
        public Returns GenerateUESVigr(out Returns ret, SupgFinder finder)
        {
            ret = Utils.InitReturns();

            var month = finder.adr;
            if (finder.adr.Length == 1)
                month = "0" + finder.adr;
            var year = finder.area;

            if (finder.nzp_user < 1)
            {
                ret.result = false;
                ret.text = "Не определен пользователь";
                ret.result = false;
                return ret;
            }

            #region Проверка наличия таблиц
            using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            {
                try
                {
                    var t = OpenDb(con_db, true);

                    if (!t.result)
                    {
                        MonitorLog.WriteLog("Ошибка открытия соединения", MonitorLog.typelog.Error, true);
                        ret.result = false;
                        ret.text = " Файл не загружен, отсутствует подключение к базе данных ";
                        ret.tag = -1;
                        return ret;
                    }

                    foreach (var bank in Points.PointList)
                    {
                        string comStr = "select * from " + bank.pref + "_charge_" + finder.year.Substring(2, 2) +  tableDelimiter + "charge_" + finder.month + " where nzp_kvar = -1";
                        var dt = ClassDBUtils.OpenSQL(comStr, con_db);
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteException("Ошибка выгрузки начислений УЭС(GenerateUESVigr)", ex);
                    ret.result = false;
                    ret.text = "Для выбранной даты невозможна загрузка";
                    ret.tag = -1;
                    return ret;
                }
            }
            #endregion

            string fn2 = "";
            //путь, по которому скачивается файл
            string path = "";
            //Имя файла отчета
            string fileNameIn = "UES_vigr_" + DateTime.Now.Ticks;
            ExcelRep excelRepDb = new ExcelRep();
            StringBuilder sql = new StringBuilder();

            //запись в БД о постановки в поток(статус 0)
            ret = excelRepDb.AddMyFile(new ExcelUtility()
            {
                nzp_user = finder.nzp_user,
                status = ExcelUtility.Statuses.InProcess,
                rep_name = "Выгрузка начислений УЭС"
            });
            if (!ret.result) return ret;

            int nzpExc = ret.tag;

            IDbConnection conn_db = null;
            MyDataReader reader = null;
            decimal progress = 0;
            var dir = "FilesExchange\\files\\" + fileNameIn + "\\";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            var resDir = STCLINE.KP50.Global.Constants.ExcelDir.Replace("/", "\\");
            if (!Directory.Exists(resDir)) Directory.CreateDirectory(resDir);

            var fullPath = AppDomain.CurrentDomain.BaseDirectory;
            OleDbCommand Command = new OleDbCommand();

            try
            {
                IDbConnection conn_web = DBManager.newDbConnection(Constants.cons_Webdata);
                ret = OpenDb(conn_web, true);
                if (!ret.result)
                {
                    return ret;
                }

                conn_db = DBManager.newDbConnection(Constants.cons_Kernel);
                ret = OpenDb(conn_db, true);
                if (!ret.result)
                {
                    return ret;
                }

                #region Создание mdb Счета Дотации УЭС
                //создание дубликата пустого mdb файла
                File.Copy("template/blank_table.mdb", dir + "Счета Дотации УЭС.mdb");
                File.Copy("template/blank_table.mdb", dir + "Счета Льготные площади.mdb");
                OleDbConnection Connection = new OleDbConnection();
                var myConnectionString = @"Provider=Microsoft.ACE.OLEDB.12.0;" +
                           "Data Source=" + dir + "Счета Дотации УЭС.mdb;Jet OLEDB:Database Password=password;";
                Connection.ConnectionString = myConnectionString;
                Connection.Open();

                DataTable resTable = new DataTable(), tmpHeaderTable = new DataTable(), tmpServTable = new DataTable();
                string strComm = "";

                //Заголовок
                strComm = "CREATE TABLE [Счета Дотации УЭС] ([Дата расчета] DATETIME,[Счет] DOUBLE, [Код Услуги] INTEGER, [Код льготы] INTEGER, [Вид регистрации] INTEGER, [Сумма] DOUBLE )";
                Command = new OleDbCommand(strComm, Connection);
                Command.ExecuteNonQuery();
                Command.Dispose();

                progress += 20;
                decimal pr = progress / 100;
                excelRepDb.SetMyFileProgress(new ExcelUtility() { nzp_exc = nzpExc, progress = progress / 100 });

                Connection.Close();
                //Connection.Dispose();

                #endregion

                #region Создание mdb Счета Льготные площади УЭС
                //создание дубликата пустого mdb файла
                //File.Copy("template/blank_table.mdb", dir + "Счета Льготные площади.mdb");
                Connection = new OleDbConnection();
                myConnectionString = @"Provider=Microsoft.ACE.OLEDB.12.0;" +
                           "Data Source=" + dir + "Счета Льготные площади.mdb;Jet OLEDB:Database Password=password;";
                Connection.ConnectionString = myConnectionString;
                Connection.Open();

                resTable = new DataTable();
                tmpHeaderTable = new DataTable();
                tmpServTable = new DataTable();
                strComm = "";

                //Заголовок
                strComm = "CREATE TABLE [Счета Льготные площади УЭС] ([Счет] DOUBLE, [Код Услуги] INTEGER, [Код льготы] INTEGER, [Вид регистрации] INTEGER, " +
                          " [Площадь] DOUBLE, [Дата расчета]  DATETIME,[Тариф] DOUBLE )";
                Command = new OleDbCommand(strComm, Connection);
                Command.ExecuteNonQuery();
                Command.Dispose();

                progress += 20;
                excelRepDb.SetMyFileProgress(new ExcelUtility() { nzp_exc = nzpExc, progress = progress / 100 });

                Connection.Close();
                Connection.Dispose();
                #endregion

                #region Создание mdb Счета Корректировки УЭС
                //создание дубликата пустого mdb файла
                File.Copy("template/blank_table.mdb", dir + "Счета Корректировки УЭС.mdb");
                Connection = new OleDbConnection();
                myConnectionString = @"Provider=Microsoft.ACE.OLEDB.12.0;" +
                           "Data Source=" + dir + "Счета Корректировки УЭС.mdb;Jet OLEDB:Database Password=password;";
                Connection.ConnectionString = myConnectionString;
                Connection.Open();

                resTable = new DataTable();
                tmpHeaderTable = new DataTable();
                tmpServTable = new DataTable();
                strComm = "";

                //запись в файл
                strComm = "CREATE TABLE [Счета Корректировки УЭС] ([Счет] DOUBLE, [Код Услуги] INTEGER, [Дата расчета] DATETIME, [Сумма] DOUBLE, [Вид] TEXT(5), [Код льготы] INTEGER, [Вид регистрации] INTEGER)";
                Command = new OleDbCommand(strComm, Connection);
                Command.ExecuteNonQuery();
                Command.Dispose();


                foreach (var bank in Points.PointList)
                {
                    //string sqlStr = " select nzp_serv, real_charge, nzp_kvar from " + bank.pref + "_charge_" + finder.year.Substring(2, 2) +  tableDelimiter + "charge_" + finder.month + " where nzp_serv = 8 or nzp_serv = 9 ";
                    //var result = ClassDBUtils.OpenSQL(sqlStr, conn_db);
                    //if (result.resultData != null)
                    //{
                    //    resTable = result.resultData;

                    //    foreach (DataRow row in resTable.Rows)
                    //    {
                    //        var nzp_serv = 2;
                    //        if (row["nzp_serv"].ToString().Trim() == "9")
                    //            nzp_serv = 3;
                    //        strComm = "insert into [Счета Корректировки УЭС] ([Счет], [Код услуги] , [Дата расчета], [Сумма], [Вид], [Код льготы], [Вид регистрации]) values " +
                    //                  " (" + row["nzp_kvar"] + ", " + nzp_serv + ", '01." + finder.month + "." + finder.year + "', " + row["real_charge"] + ", 'Н', 0, 0 ) ";
                    //        OleDbCommand cmd_insert = new OleDbCommand(strComm, Connection);
                    //        cmd_insert.ExecuteNonQuery();
                    //        cmd_insert.Dispose();
                    //    }
                    //}

                    // из xxx_charge_13:perekidki
                    string sqlStr1 = " select distinct nzp_serv, sum_rcl, nzp_kvar from " + bank.pref + "_charge_" + finder.year.Substring(2, 2) + tableDelimiter+"perekidka where nzp_serv in (8,9)" +
                        " and type_rcl = 1 and month_ = " + finder.month;
                    var result1 = ClassDBUtils.OpenSQL(sqlStr1, conn_db);
                    if (result1.resultData != null)
                    {
                        resTable = result1.resultData;

                        foreach (DataRow row in resTable.Rows)
                        {
                            var nzp_serv = 2;
                            if (row["nzp_serv"].ToString().Trim() == "9")
                                nzp_serv = 3;
                            strComm = "insert into [Счета Корректировки УЭС] ([Счет], [Код услуги] , [Дата расчета], [Сумма], [Вид], [Код льготы], [Вид регистрации]) values " +
                                      " (" + row["nzp_kvar"] + ", " + nzp_serv + ", '01." + finder.month + "." + finder.year + "', " + row["sum_rcl"] + ", 'Н', 0, 0 ) ";
                            OleDbCommand cmd_insert = new OleDbCommand(strComm, Connection);
                            cmd_insert.ExecuteNonQuery();
                            cmd_insert.Dispose();
                        }
                    }

                    progress += 20 / Points.PointList.Count;
                    excelRepDb.SetMyFileProgress(new ExcelUtility() { nzp_exc = nzpExc, progress = progress / 100 });
                }

                Connection.Close();
                Connection.Dispose();
                #endregion

                #region Создание mdb Счета Начисления УЭС
                //создание дубликата пустого mdb файла
                File.Copy("template/blank_table.mdb", dir + "Счета Начисления УЭС.mdb");
                Connection = new OleDbConnection();
                myConnectionString = @"Provider=Microsoft.ACE.OLEDB.12.0;" +
                           "Data Source=" + dir + "Счета Начисления УЭС.mdb;Jet OLEDB:Database Password=password;";
                Connection.ConnectionString = myConnectionString;
                Connection.Open();

                resTable = new DataTable();
                tmpHeaderTable = new DataTable();
                tmpServTable = new DataTable();
                strComm = "";

                //Заголовок
                strComm = "CREATE TABLE [Счета Начисления УЭС] ([Счет] DOUBLE, [Код Услуги] INTEGER, [Дата расчета] DATETIME, [Сумма] DOUBLE) ";
                Command = new OleDbCommand(strComm, Connection);
                Command.ExecuteNonQuery();
                Command.Dispose();

                foreach (var bank in Points.PointList)
                {
                    string sqlStr = " select nzp_serv, sum_tarif, nzp_kvar from " + bank.pref + "_charge_" + finder.year.Substring(2, 2) +  tableDelimiter + "charge_" + finder.month + " where nzp_serv in(8, 9) and sum_tarif <> 0";
                    var result = ClassDBUtils.OpenSQL(sqlStr, conn_db);
                    if (result.resultData != null)
                    {
                        resTable = result.resultData;

                        foreach (DataRow row in resTable.Rows)
                        {
                            var nzp_serv = 2;
                            if (row["nzp_serv"].ToString().Trim() == "9")
                                nzp_serv = 3;
                            strComm = "insert into [Счета Начисления УЭС] ([Счет], [Код услуги] , [Дата расчета], [Сумма]) values " +
                                      " (" + row["nzp_kvar"] + ", " + nzp_serv + ", '01." + finder.month + "." + finder.year + "', " + row["sum_tarif"] + " ) ";
                            OleDbCommand cmd_insert = new OleDbCommand(strComm, Connection);
                            cmd_insert.ExecuteNonQuery();
                            cmd_insert.Dispose();
                        }
                    }

                    progress += 20 / Points.PointList.Count;
                    excelRepDb.SetMyFileProgress(new ExcelUtility() { nzp_exc = nzpExc, progress = progress / 100 });
                }

                Connection.Close();
                Connection.Dispose();
                #endregion

                #region Создание mdb Счета Превышения УЭС
                //создание дубликата пустого mdb файла
                File.Copy("template/blank_table.mdb", dir + "Счета Превышения УЭС.mdb");
                Connection = new OleDbConnection();
                myConnectionString = @"Provider=Microsoft.ACE.OLEDB.12.0;" +
                           "Data Source=" + dir + "Счета Превышения УЭС.mdb;Jet OLEDB:Database Password=password;";
                Connection.ConnectionString = myConnectionString;
                Connection.Open();

                resTable = new DataTable();
                tmpHeaderTable = new DataTable();
                tmpServTable = new DataTable();
                strComm = "";

                //Заголовок
                strComm = "CREATE TABLE [Счета Превышения УЭС] ([Счет] DOUBLE, [Код Услуги] INTEGER, [Дата расчета] DATETIME, [Сумма] DOUBLE) ";
                Command = new OleDbCommand(strComm, Connection);
                Command.ExecuteNonQuery();
                Command.Dispose();

                foreach (var bank in Points.PointList)
                {
                    //string sqlStr = " select nzp_serv, reval, nzp_kvar from " + bank.pref + "_charge_" + finder.year.Substring(2, 2) +  tableDelimiter + "charge_" + finder.month + " where nzp_serv = 8 or nzp_serv = 9 ";
                    //var result = ClassDBUtils.OpenSQL(sqlStr, conn_db);
                    //if (result.resultData != null)
                    //{
                    //    resTable = result.resultData;

                    //    foreach (DataRow row in resTable.Rows)
                    //    {
                    //        var nzp_serv = 2;
                    //        if (row["nzp_serv"].ToString().Trim() == "9")
                    //            nzp_serv = 3;
                    //        strComm = "insert into [Счета Превышения УЭС] ([Счет], [Код услуги] , [Дата расчета], [Сумма]) values " +
                    //                  " (" + row["nzp_kvar"] + ", " + nzp_serv + ", '01." + finder.month + "." + finder.year + "', " + row["reval"] + " ) ";
                    //        OleDbCommand cmd_insert = new OleDbCommand(strComm, Connection);
                    //        cmd_insert.ExecuteNonQuery();
                    //        cmd_insert.Dispose();
                    //    }
                    //}

                    string sqlStr1 = " select distinct nzp_serv, sum_rcl, nzp_kvar from " + bank.pref + "_charge_" + finder.year.Substring(2, 2) +  tableDelimiter + "perekidka where nzp_serv in (8,9) " +
                        " and type_rcl = 2 and month_ = " + finder.month;
                    var result1 = ClassDBUtils.OpenSQL(sqlStr1, conn_db);
                    if (result1.resultData != null)
                    {
                        resTable = result1.resultData;

                        foreach (DataRow row in resTable.Rows)
                        {
                            var nzp_serv = 2;
                            if (row["nzp_serv"].ToString().Trim() == "9")
                                nzp_serv = 3;
                            strComm = "insert into [Счета Превышения УЭС] ([Счет], [Код услуги] , [Дата расчета], [Сумма]) values " +
                                      " (" + row["nzp_kvar"] + ", " + nzp_serv + ", '01." + finder.month + "." + finder.year + "', " + row["sum_rcl"] + " ) ";
                            OleDbCommand cmd_insert = new OleDbCommand(strComm, Connection);
                            cmd_insert.ExecuteNonQuery();
                            cmd_insert.Dispose();
                        }
                    }

                    progress += 20 / Points.PointList.Count;
                    excelRepDb.SetMyFileProgress(new ExcelUtility() { nzp_exc = nzpExc, progress = progress / 100 });
                }

                Connection.Close();
                Connection.Dispose();
                #endregion

                conn_web.Close();

                #region архивация
                SevenZipCompressor file = new SevenZipCompressor();
                file.EncryptHeaders = true;
                file.CompressionMethod = SevenZip.CompressionMethod.BZip2;
                file.DefaultItemName = fileNameIn;
                file.CompressionLevel = SevenZip.CompressionLevel.Normal;

                file.CompressDirectory(fullPath + dir, fullPath + dir.Substring(0, dir.Length - 1) + ".7z");
                #endregion

                //перенос файла на клиент
                File.Copy(dir.Substring(0, dir.Length - 1) + ".7z", resDir + fileNameIn + ".7z");
                if (InputOutput.useFtp) fn2 = InputOutput.SaveInputFile(fullPath + dir.Substring(0, dir.Length - 1) + ".7z");
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка в функции GenerateUESVigr:\n" + ex.Message, MonitorLog.typelog.Error, true);
            }
            finally
            {
                Directory.Delete(dir, true);

                if (ret.result)
                {
                    excelRepDb.SetMyFileProgress(new ExcelUtility() { nzp_exc = nzpExc, progress = 1 });
                    excelRepDb.SetMyFileState(new ExcelUtility() { nzp_exc = nzpExc, status = ExcelUtility.Statuses.Success, exc_path = InputOutput.useFtp ? fn2 : path + fileNameIn + ".7z" });
                }
                else
                {
                    excelRepDb.SetMyFileState(new ExcelUtility() { nzp_exc = nzpExc, status = ExcelUtility.Statuses.Failed });
                }
                excelRepDb.Close();

                if (reader != null) reader.Close();
                if (conn_db != null) conn_db.Close();
            }

            ret.text = "Файл успешно загружен";
            return ret;
        }

        #region функции для загрузки адресного пространства из КЛАДР

        private decimal SaveRegion(KLADRData obj, IDbConnection conn)
        {
            decimal nzp_stat = 0;
            var upper_bank = Points.Pref;
            Returns retvar = Utils.InitReturns();

            #region сохранение в верхний банк
            string selectedCode = obj.code;
            string selectedFullname = obj.fullname.ToUpper();
            string sqlString = "SELECT * FROM " + upper_bank + "_data"+tableDelimiter + "s_stat where soato = '" + selectedCode + "'";
            var resCount = ClassDBUtils.OpenSQL(sqlString, conn).resultData.Rows.Count;
            if (resCount != 0)
            {
                //обновить
                sqlString = "UPDATE " + upper_bank + "_data"+tableDelimiter + "s_stat SET ( stat, stat_t ) = ( '" + selectedFullname + "' , '" + selectedFullname + "' ) where soato = '" + selectedCode + "'";
                retvar = ExecSQL(conn, sqlString, true);
                if (!retvar.result)
                {
                    MonitorLog.WriteLog("Ошибка в функции SaveRegion:\n" + retvar.text, MonitorLog.typelog.Error, true);
                    return -1;
                }
                sqlString = "SELECT nzp_stat FROM " + upper_bank + "_data"+tableDelimiter + "s_stat WHERE soato = '" + selectedCode + "'";
                nzp_stat = Convert.ToDecimal(ExecScalar(conn, sqlString, out retvar, true));
            }
            else
            {
                //добавить
                IDbTransaction tr_id = null;
                try
                {
                    tr_id = conn.BeginTransaction();
                    sqlString = "INSERT INTO " + upper_bank + "_data"+tableDelimiter + "s_stat ( stat, stat_t, nzp_land, soato ) VALUES ( '" + selectedFullname + "', '" + selectedFullname + "' , '1' , '" + selectedCode + "')";
                    retvar = ExecSQL(conn, tr_id, sqlString, true);
                    if (!retvar.result)
                    {
                        MonitorLog.WriteLog("Ошибка в функции SaveRegion:\n" + retvar.text, MonitorLog.typelog.Error, true);
                        return -1;
                    }
                    nzp_stat = ClassDBUtils.GetSerialKey(conn, tr_id);
                    tr_id.Commit();
                }
                catch (Exception ex)
                {
                    tr_id.Rollback();
                    MonitorLog.WriteLog("Ошибка в функции SaveRegion:\n" + ex.Message, MonitorLog.typelog.Error, true);
                    return -1;
                }
            }
            #endregion

            #region сохранение в нижние банки
            foreach (var bank in Points.PointList)
            {
                sqlString = "SELECT * FROM " + bank.pref + "_data"+tableDelimiter + "s_stat where soato = '" + selectedCode + "'";
                resCount = ClassDBUtils.OpenSQL(sqlString, conn).resultData.Rows.Count;
                if (resCount != 0)
                {
                    //обновить
                    sqlString = "UPDATE " + bank.pref + "_data"+tableDelimiter + "s_stat SET ( stat, stat_t ) = ( '" + selectedFullname + "' , '" + selectedFullname + "' ) where soato = '" + selectedCode + "'";
                    retvar = ExecSQL(conn, sqlString, true);
                    if (!retvar.result)
                    {
                        MonitorLog.WriteLog("Ошибка в функции SaveRegion:\n" + retvar.text, MonitorLog.typelog.Error, true);
                        return -1;
                    }
                }
                else
                {
                    //добавить
                    sqlString = "INSERT INTO " + bank.pref + "_data"+tableDelimiter + "s_stat ( stat, stat_t, nzp_land, soato, nzp_stat ) VALUES " +
                              " ( '" + selectedFullname + "', '" + selectedFullname + "' , '1' , '" + selectedCode + "', '" + nzp_stat + "')";
                    retvar = ExecSQL(conn, sqlString, true);
                    if (!retvar.result)
                    {
                        MonitorLog.WriteLog("Ошибка в функции SaveRegion:\n" + retvar.text, MonitorLog.typelog.Error, true);
                        return -1;
                    }
                }
            }
            #endregion

            return nzp_stat;
        }

        private decimal SaveDistricrt(KLADRData obj, IDbConnection conn, decimal nzp_stat)
        {
            decimal nzp_town = 0;
            var upper_bank = Points.Pref;
            Returns retvar = Utils.InitReturns();

            #region сохранение в верхний банк
            string selectedCode = obj.code;
            string selectedFullname = obj.fullname.ToUpper();
            string sqlString = "SELECT * FROM " + upper_bank + "_data"+tableDelimiter + "s_town where soato = '" + selectedCode + "'";
            var resCount = ClassDBUtils.OpenSQL(sqlString, conn).resultData.Rows.Count;
            if (resCount != 0)
            {
                //обновить
                sqlString = "UPDATE " + upper_bank + "_data"+tableDelimiter + "s_town SET ( town, town_t, nzp_stat ) = " +
                    "( '" + selectedFullname + "' , '" + selectedFullname + "', '" + nzp_stat.ToString() + "' ) where soato = '" + selectedCode + "'";
                retvar = ExecSQL(conn, sqlString, true);
                if (!retvar.result)
                {
                    MonitorLog.WriteLog("Ошибка в функции SaveDistricrt:\n" + retvar.text, MonitorLog.typelog.Error, true);
                    return -1;
                }
                sqlString = "SELECT nzp_town FROM " + upper_bank + "_data"+tableDelimiter + "s_town WHERE soato = '" + selectedCode + "'";
                nzp_town = Convert.ToDecimal(ExecScalar(conn, sqlString, out retvar, true));
            }
            else
            {
                //добавить
                IDbTransaction tr_id = null;
                try
                {
                    tr_id = conn.BeginTransaction();
                    sqlString = "INSERT INTO " + upper_bank + "_data"+tableDelimiter + "s_town ( town, town_t, nzp_stat, soato ) VALUES " +
                        "( '" + selectedFullname + "', '" + selectedFullname + "' , '" + nzp_stat.ToString() + "' , '" + selectedCode + "')";
                    retvar = ExecSQL(conn, tr_id, sqlString, true);
                    if (!retvar.result)
                    {
                        MonitorLog.WriteLog("Ошибка в функции SaveDistricrt:\n" + retvar.text, MonitorLog.typelog.Error, true);
                        return -1;
                    }
                    nzp_town = ClassDBUtils.GetSerialKey(conn, tr_id);
                    tr_id.Commit();
                }
                catch (Exception ex)
                {
                    tr_id.Rollback();
                    MonitorLog.WriteLog("Ошибка в функции SaveDistricrt:\n" + ex.Message, MonitorLog.typelog.Error, true);
                    return -1;
                }
            }
            #endregion

            #region сохранение в нижние банки
            foreach (var bank in Points.PointList)
            {
                sqlString = "SELECT * FROM " + bank.pref + "_data"+tableDelimiter + "s_town where soato = '" + selectedCode + "'";
                resCount = ClassDBUtils.OpenSQL(sqlString, conn).resultData.Rows.Count;
                if (resCount != 0)
                {
                    //обновить
                    sqlString = "UPDATE " + bank.pref + "_data"+tableDelimiter + "s_town SET ( town, town_t ) = ( '" + selectedFullname + "' , '" + selectedFullname + "' ) where soato = '" + selectedCode + "'";
                    retvar = ExecSQL(conn, sqlString, true);
                    if (!retvar.result)
                    {
                        MonitorLog.WriteLog("Ошибка в функции SaveDistricrt:\n" + retvar.text, MonitorLog.typelog.Error, true);
                        return -1;
                    }
                }
                else
                {
                    //добавить
                    sqlString = "INSERT INTO " + bank.pref + "_data"+tableDelimiter + "s_town ( town, town_t, nzp_stat, soato, nzp_town ) VALUES " +
                               " ( '" + selectedFullname + "', '" + selectedFullname + "' , '" + nzp_stat + "' , '" + selectedCode + "', '" + nzp_town + "')";
                    retvar = ExecSQL(conn, sqlString, true);
                    if (!retvar.result)
                    {
                        MonitorLog.WriteLog("Ошибка в функции SaveDistricrt:\n" + retvar.text, MonitorLog.typelog.Error, true);
                        return -1;
                    }
                }
            }
            #endregion

            return nzp_town;
        }

        private decimal SaveCity(KLADRData obj, IDbConnection conn, decimal nzp_stat)
        {
            decimal nzp_town = 0;
            var upper_bank = Points.Pref;
            Returns retvar = Utils.InitReturns();

            #region сохранение в верхний банк
            string selectedCode = obj.code;
            string selectedFullname = obj.fullname.ToUpper();
            string sqlString = "SELECT * FROM " + upper_bank + "_data"+tableDelimiter + "s_town where soato = '" + selectedCode + "'";
            var resCount = ClassDBUtils.OpenSQL(sqlString, conn).resultData.Rows.Count;
            if (resCount != 0)
            {
                //обновить
                sqlString = "UPDATE " + upper_bank + "_data"+tableDelimiter + "s_town SET ( town, town_t, nzp_stat ) = " +
                            " ( '" + selectedFullname + "' , '" + selectedFullname + "', '" + nzp_stat.ToString() + "' ) where soato = '" + selectedCode + "'";
                retvar = ExecSQL(conn, sqlString, true);
                if (!retvar.result)
                {
                    MonitorLog.WriteLog("Ошибка в функции SaveCity:\n" + retvar.text, MonitorLog.typelog.Error, true);
                    return -1;
                }
                sqlString = "SELECT nzp_town FROM " + upper_bank + "_data"+tableDelimiter + "s_town WHERE soato = '" + selectedCode + "'";
                nzp_town = Convert.ToDecimal(ExecScalar(conn, sqlString, out retvar, true));
            }
            else
            {
                //добавить
                IDbTransaction tr_id = null;
                try
                {
                    tr_id = conn.BeginTransaction();
                    sqlString = "INSERT INTO " + upper_bank + "_data"+tableDelimiter + "s_town ( town, town_t, nzp_stat, soato ) VALUES " +
                                " ( '" + selectedFullname + "', '" + selectedFullname + "' , '" + nzp_stat.ToString() + "' , '" + selectedCode + "')";
                    retvar = ExecSQL(conn, tr_id, sqlString, true);
                    if (!retvar.result)
                    {
                        MonitorLog.WriteLog("Ошибка в функции SaveCity:\n" + retvar.text, MonitorLog.typelog.Error, true);
                        return -1;
                    }
                    nzp_town = ClassDBUtils.GetSerialKey(conn, tr_id);
                    tr_id.Commit();
                }
                catch (Exception ex)
                {
                    tr_id.Rollback();
                    MonitorLog.WriteLog("Ошибка в функции SaveCity:\n" + ex.Message, MonitorLog.typelog.Error, true);
                    return -1;
                }
            }
            #endregion

            #region сохранение в нижние банки
            foreach (var bank in Points.PointList)
            {
                sqlString = "SELECT * FROM " + bank.pref + "_data"+tableDelimiter + "s_town where soato = '" + selectedCode + "'";
                resCount = ClassDBUtils.OpenSQL(sqlString, conn).resultData.Rows.Count;
                if (resCount != 0)
                {
                    //обновить
                    sqlString = "UPDATE " + bank.pref + "_data"+tableDelimiter + "s_town SET ( town, town_t ) = ( '" + selectedFullname + "' , '" + selectedFullname + "' ) where soato = '" + selectedCode + "'";
                    retvar = ExecSQL(conn, sqlString, true);
                    if (!retvar.result)
                    {
                        MonitorLog.WriteLog("Ошибка в функции SaveCity:\n" + retvar.text, MonitorLog.typelog.Error, true);
                        return -1;
                    }
                }
                else
                {
                    //добавить
                    sqlString = "INSERT INTO " + bank.pref + "_data"+tableDelimiter + "s_town ( town, town_t, nzp_stat, soato, nzp_town ) VALUES " +
                               " ( '" + selectedFullname + "', '" + selectedFullname + "' , '" + nzp_stat + "' , '" + selectedCode + "', '" + nzp_town + "')";
                    retvar = ExecSQL(conn, sqlString, true);
                    if (!retvar.result)
                    {
                        MonitorLog.WriteLog("Ошибка в функции SaveCity:\n" + retvar.text, MonitorLog.typelog.Error, true);
                        return -1;
                    }
                }
            }
            #endregion

            return nzp_town;
        }

        private decimal SaveSettlement(KLADRData obj, IDbConnection conn, decimal nzp_town)
        {
            decimal nzp_raj = 0;
            var upper_bank = Points.Pref;
            Returns retvar = Utils.InitReturns();

            #region сохранение в верхний банк
            string selectedCode = obj.code;
            string selectedFullname = obj.fullname.ToUpper();
            string sqlString = "SELECT * FROM " + upper_bank + "_data"+tableDelimiter + "s_rajon where soato = '" + selectedCode + "' and nzp_town = " + nzp_town.ToString();
            var resCount = ClassDBUtils.OpenSQL(sqlString, conn).resultData.Rows.Count;
            if (resCount != 0)
            {
                //обновить
                sqlString = "UPDATE " + upper_bank + "_data"+tableDelimiter + "s_rajon SET ( rajon, rajon_t ) = " +
#if PG
 "( '" + selectedFullname + "'::character(30), '" + selectedFullname + "'::character(30) ) where soato = '" + selectedCode + "' and nzp_town = " + nzp_town.ToString();
#else
                    "( '" + selectedFullname + "', '" + selectedFullname + "' ) where soato = '" + selectedCode + "' and nzp_town = " + nzp_town.ToString();
#endif
                retvar = ExecSQL(conn, sqlString, true);
                if (!retvar.result)
                {
                    MonitorLog.WriteLog("Ошибка в функции SaveSettlement:\n" + retvar.text, MonitorLog.typelog.Error, true);
                    return -1;
                }
                sqlString = "SELECT nzp_raj FROM " + upper_bank + "_data"+tableDelimiter + "s_rajon WHERE soato = '" + selectedCode + "' and nzp_town = " + nzp_town.ToString();
                nzp_raj = Convert.ToDecimal(ExecScalar(conn, sqlString, out retvar, true));
            }
            else
            {
                //добавить
                IDbTransaction tr_id = null;
                try
                {
                    tr_id = conn.BeginTransaction();
                     sqlString = "INSERT INTO " + upper_bank + "_data"+tableDelimiter + "s_rajon ( nzp_town, rajon, rajon_t, soato ) VALUES " +
#if PG
 "(  '" + nzp_town.ToString() + "' ,'" + selectedFullname + "'::character(30), '" + selectedFullname + "'::character(30) , '" + selectedCode + "')";
#else
                        "(  '" + nzp_town.ToString() + "' ,'" + selectedFullname + "', '" + selectedFullname + "' , '" + selectedCode + "')";
#endif
                    retvar = ExecSQL(conn, tr_id, sqlString, true);
                    if (!retvar.result)
                    {
                        MonitorLog.WriteLog("Ошибка в функции SaveSettlement:\n" + retvar.text, MonitorLog.typelog.Error, true);
                        return -1;
                    }
                    nzp_raj = ClassDBUtils.GetSerialKey(conn, tr_id);
                    tr_id.Commit();
                }
                catch (Exception ex)
                {
                    tr_id.Rollback();
                    MonitorLog.WriteLog("Ошибка в функции SaveSettlement:\n" + ex.Message, MonitorLog.typelog.Error, true);
                    return -1;
                }
            }
            #endregion

            #region сохранение в нижние банки
            foreach (var bank in Points.PointList)
            {
                sqlString = "SELECT * FROM " + bank.pref + "_data"+tableDelimiter + "s_rajon where soato = '" + selectedCode + "'";
                resCount = ClassDBUtils.OpenSQL(sqlString, conn).resultData.Rows.Count;
                if (resCount != 0)
                {
                    //обновить
                    sqlString = "UPDATE " + bank.pref + "_data"+tableDelimiter + "s_rajon SET ( rajon, rajon_t ) = " +
#if PG
 "( '" + selectedFullname + "'::character(30) , '" + selectedFullname + "'::character(30) ) where soato = '" + selectedCode + "'";
#else
                        "( '" + selectedFullname + "' , '" + selectedFullname + "' ) where soato = '" + selectedCode + "'"; 
#endif
                    retvar = ExecSQL(conn, sqlString, true);
                    if (!retvar.result)
                    {
                        MonitorLog.WriteLog("Ошибка в функции SaveSettlement:\n" + retvar.text, MonitorLog.typelog.Error, true);
                        return -1;
                    }
                }
                else
                {
                    //добавить
                    sqlString = "INSERT INTO " + bank.pref + "_data"+tableDelimiter + "s_rajon ( rajon, rajon_t, nzp_town, soato, nzp_raj ) VALUES " +
#if PG
 " ( '" + selectedFullname + "'::character(30), '" + selectedFullname + "'::character(30) , '" + nzp_town + "' , '" + selectedCode + "', '" + nzp_raj + "')"; retvar = ExecSQL(conn, sqlString, true);
#else
                        " ( '" + selectedFullname + "', '" + selectedFullname + "' , '" + nzp_town + "' , '" + selectedCode + "', '" + nzp_raj + "')"; retvar = ExecSQL(conn, sqlString, true);
#endif
                    if (!retvar.result)
                    {
                        MonitorLog.WriteLog("Ошибка в функции SaveSettlement:\n" + retvar.text, MonitorLog.typelog.Error, true);
                        return -1;
                    }
                }
            }
            #endregion

            return nzp_raj;
        }

        private decimal SaveStreet(KLADRData obj, IDbConnection conn, decimal nzp_raj)
        {
            decimal nzp_ul = 0;
            var upper_bank = Points.Pref;
            Returns retvar = Utils.InitReturns();

            #region сохранение в верхний банк
            string selectedCode = obj.code;
            string selectedFullname = obj.fullname.ToUpper();
            string selectedName = obj.name.ToUpper();
            string selectedSocr = obj.socr.ToUpper();
            string sqlString = "SELECT * FROM " + upper_bank + "_data"+tableDelimiter + "s_ulica where soato = '" + selectedCode + "'";
            var resCount = ClassDBUtils.OpenSQL(sqlString, conn).resultData.Rows.Count;
            if (resCount != 0)
            {
                //обновить
                sqlString = "UPDATE " + upper_bank + "_data"+tableDelimiter + "s_ulica SET ( ulica, ulicareg, nzp_raj ) = " +
                    "( '" + selectedName + "' , '" + selectedSocr + "', '" + nzp_raj + "') where soato = '" + selectedCode + "'";
                retvar = ExecSQL(conn, sqlString, true);
                if (!retvar.result)
                {
                    MonitorLog.WriteLog("Ошибка в функции SaveStreet:\n" + retvar.text, MonitorLog.typelog.Error, true);
                    return -1;
                }
                sqlString = "SELECT nzp_ul FROM " + upper_bank + "_data"+tableDelimiter + "s_ulica WHERE soato = '" + selectedCode + "' and nzp_raj = " + nzp_raj.ToString();
                nzp_ul = Convert.ToDecimal(ExecScalar(conn, sqlString, out retvar, true));
            }
            else
            {
                //добавить
                IDbTransaction tr_id = null;
                try
                {
                    tr_id = conn.BeginTransaction();
                    sqlString = "INSERT INTO " + upper_bank + "_data"+tableDelimiter + "s_ulica ( ulica, nzp_raj, soato, ulicareg ) VALUES " +
                        "( '" + selectedName + "', '" + nzp_raj.ToString() + "' , '" + selectedCode + "' , '" + selectedSocr + "')";
                    retvar = ExecSQL(conn, tr_id, sqlString, true);
                    if (!retvar.result)
                    {
                        MonitorLog.WriteLog("Ошибка в функции SaveStreet:\n" + retvar.text, MonitorLog.typelog.Error, true);
                        return -1;
                    }
                    nzp_ul = ClassDBUtils.GetSerialKey(conn, tr_id);
                    tr_id.Commit();
                }
                catch (Exception ex)
                {
                    tr_id.Rollback();
                    MonitorLog.WriteLog("Ошибка в функции SaveStreet:\n" + ex.Message, MonitorLog.typelog.Error, true);
                    return -1;
                }
            }
            #endregion

            #region сохранение в нижние банки
            foreach (var bank in Points.PointList)
            {
                sqlString = "SELECT * FROM " + bank.pref + "_data"+tableDelimiter + "s_ulica where soato = '" + selectedCode + "'";
                resCount = ClassDBUtils.OpenSQL(sqlString, conn).resultData.Rows.Count;
                if (resCount != 0)
                {
                    //обновить
                    sqlString = "UPDATE " + bank.pref + "_data"+tableDelimiter + "s_ulica SET ( ulica, ulicareg ) = ( '" + selectedName + "' , '" + selectedSocr + "' ) where soato = '" + selectedCode + "'";
                    retvar = ExecSQL(conn, sqlString, true);
                    if (!retvar.result)
                    {
                        MonitorLog.WriteLog("Ошибка в функции SaveStreet:\n" + retvar.text, MonitorLog.typelog.Error, true);
                        return -1;
                    }
                }
                else
                {
                    //добавить
                    sqlString = "INSERT INTO " + bank.pref + "_data"+tableDelimiter + "s_ulica ( ulica, ulicareg, nzp_raj, soato, nzp_ul ) VALUES " +
                            " ( '" + selectedName + "', '" + selectedSocr + "' , '" + nzp_raj + "' , '" + selectedCode + "', '" + nzp_ul + "')";
                    retvar = ExecSQL(conn, sqlString, true);
                    if (!retvar.result)
                    {
                        MonitorLog.WriteLog("Ошибка в функции SaveStreet:\n" + retvar.text, MonitorLog.typelog.Error, true);
                        return -1;
                    }
                }
            }
            #endregion

            return nzp_ul;
        }

        /// <summary>
        /// Загрузка информации из КЛАДР
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsObjectType<List<KLADRData>> LoadDataFromKLADR(KLADRFinder finder)
        {
            var soatoLength = 11;
            if (finder.level == "street")
                soatoLength = 15;

            #region проверка на наличие файлов КЛАДР
            if (!File.Exists("Source\\KLADR\\KLADR.DBF") || !File.Exists("Source\\KLADR\\STREET.DBF"))
                return new ReturnsObjectType<List<KLADRData>>() { tag = -2, result = false };
            #endregion

            ReturnsObjectType<List<KLADRData>> ret = new ReturnsObjectType<List<KLADRData>>();
            var resList = new List<KLADRData>();
            try
            {
                //получение списка регионов
                #region Считывание dbf
                // Создать объект подключения
                OleDbConnection oDbCon = new OleDbConnection();
                var myConnectionString = @"Provider=Microsoft.ACE.OLEDB.12.0;" +
                                "Data Source=Source\\KLADR;Extended Properties=dBASE III;";
                oDbCon.ConnectionString = myConnectionString;
                oDbCon.Open();

                OleDbCommand cmd = new OleDbCommand();
                cmd.CommandText = finder.query;
                cmd.Connection = oDbCon;
                // Адаптер данных
                OleDbDataAdapter da = new OleDbDataAdapter();
                da.SelectCommand = cmd;
                // Заполняем объект данными
                DataTable tbl = new DataTable();
                da.Fill(tbl);
                cmd.Dispose();
                da.Dispose();
                tbl.Columns.Add("FULLNAME", typeof(String));
                for (int i = 0; i < tbl.Rows.Count; i++)
                {
                    //склеить название и сокращение
                    tbl.Rows[i]["CODE"] = tbl.Rows[i]["CODE"].ToString().Substring(0, soatoLength);
                    string socr = "";
                    if (tbl.Rows[i]["SOCR"] != null)
                    {
                        socr = tbl.Rows[i]["SOCR"].ToString().Trim();
                        tbl.Rows[i]["SOCR"] = socr;
                    }
                    string name = "";
                    if (tbl.Rows[i]["NAME"] != null)
                    {
                        name = tbl.Rows[i]["NAME"].ToString().Trim();
                        tbl.Rows[i]["NAME"] = name;
                    }
                    tbl.Rows[i]["FULLNAME"] = name + " " + socr;
                }
                #endregion

                foreach (DataRow dr in tbl.Rows)
                {
                    resList.Add(new KLADRData() { fullname = dr["FULLNAME"].ToString(), code = dr["CODE"].ToString(), name = dr["NAME"].ToString(), socr = dr["SOCR"].ToString() });
                }
                ret.returnsData = resList;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры LoadRegionKLADR : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.result = false;
                return ret;
            }

            ret.result = true;
            ret.text = "Выполнено.";
            ret.tag = -1;

            return ret;
        }

        #endregion функции для загрузки адресного пространства из КЛАДР

        public Returns UploadKLADRAddrSpace(out Returns ret, KLADRFinder finder)
        {
            ret = Utils.InitReturns();
            Returns retvar = Utils.InitReturns();

            var upper_bank = Points.Pref;
            int id = 0;
            var sqlStr = "";

            decimal nzp_city = 0;
            decimal nzp_raj = 0;
            decimal nzp_town = 0;
            decimal nzp_ul = 0;

            var region = finder.regionCode;
            var district = finder.districtCode;
            var city = finder.cityCode;
            var settlement = finder.settlementCode;

            try
            {
                IDbConnection conn_db = DBManager.newDbConnection(Constants.cons_Kernel);
                ret = OpenDb(conn_db, true);
                if (!ret.result)
                {
                    return ret;
                }

                ////Проверка на возможность загрузки
                //var sqlStr = " select count(*) from " + Points.Pref + "_data"+tableDelimiter + "upload_progress where upload_type = 1 and progress >= 0 and progress <= 1 ";
                //var count = Convert.ToDecimal(ExecScalar(conn_db, sqlStr, out retvar, true));
                //if (!retvar.result) { ret.result = false; return ret; }
                //if (count != 0)
                //{
                //    ret.tag = -2;
                //    ret.result = false;
                //    return ret;
                //}

                OleDbConnection oDbCon = new OleDbConnection();
                var myConnectionString = @"Provider=Microsoft.ACE.OLEDB.12.0;" +
                                "Data Source=Source\\KLADR;Extended Properties=dBASE III;";
                oDbCon.ConnectionString = myConnectionString;
                oDbCon.Open();

                //запись в базу о начале загрузки
                IDbTransaction tr_id = null;
                try
                {
                    tr_id = conn_db.BeginTransaction();
                    sqlStr = " insert into " + Points.Pref + "_data"+tableDelimiter + "upload_progress (date_upload, progress, upload_type) VALUES " +

#if PG
 " (now(), 0, 1 ) ";
#else
" (current, 0, 1 ) ";
#endif
                    retvar = ExecSQL(conn_db, tr_id, sqlStr, true);
                    if (!retvar.result)
                    {
                        ret.result = false;
                        MonitorLog.WriteLog("Ошибка в функции UploadKLADRAddrSpace:\n", MonitorLog.typelog.Error, true);
                    }
                    id = Convert.ToInt32(ClassDBUtils.GetSerialKey(conn_db, tr_id));
                    tr_id.Commit();
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = ex.Message;
                    MonitorLog.WriteLog("Ошибка в функции UploadKLADRAddrSpace:\n" + ex.Message, MonitorLog.typelog.Error, true);
                }

                #region очистка адресного пространства
                if (finder.clearAddrSpace)
                {
                    sqlStr = " delete from " + upper_bank + "_data"+tableDelimiter + "s_stat; delete from " + upper_bank + "_data"+tableDelimiter + "s_town; delete from " + upper_bank + "_data"+tableDelimiter + "s_rajon; " +
                                " delete from " + upper_bank + "_data"+tableDelimiter + "s_ulica; delete from " + upper_bank + "_data"+tableDelimiter + "dom; delete from " + upper_bank + "_data"+tableDelimiter + "kvar;";
                    retvar = ExecSQL(conn_db, sqlStr, true);
                    if (!retvar.result) { ret.result = false; return ret; }
                    foreach (var bank in Points.PointList)
                    {
                        sqlStr = " delete from " + bank.pref + "_data"+tableDelimiter + "s_stat; delete from " + bank.pref + "_data"+tableDelimiter + "s_town; delete from " + bank.pref + "_data"+tableDelimiter + "s_rajon; " +
                                    " delete from " + bank.pref + "_data"+tableDelimiter + "s_ulica; delete from " + bank.pref + "_data"+tableDelimiter + "dom; delete from " + bank.pref + "_data"+tableDelimiter + "kvar;";
                        ClassDBUtils.ExecSQL(sqlStr, conn_db);
                        if (!retvar.result) { ret.result = false; return ret; }
                    }
                }
                #endregion

                #region Для выгрузки выбрана улица
                if (finder.level == "street")
                {
                    decimal nzp_stat = SaveRegion(finder.region, conn_db);
                    if (nzp_stat == -1) { ret.result = false; return ret; }

                    if (finder.district != null)
                    {
                        nzp_town = SaveDistricrt(finder.district, conn_db, nzp_stat);
                        if (nzp_town == -1) { ret.result = false; return ret; }
                    }
                    else if (finder.city == null)
                    {
                        var tmpObj = new KLADRData() { code = "-", fullname = "-" };
                        nzp_town = SaveDistricrt(tmpObj, conn_db, nzp_stat);
                        if (nzp_town == -1) { ret.result = false; return ret; }
                    }

                    if (finder.city != null)
                    {
                        nzp_city = SaveCity(finder.city, conn_db, nzp_stat);
                        if (nzp_city == -1) { ret.result = false; return ret; }
                    }

                    if (finder.settlement != null)
                    {
                        if (nzp_city != 0)
                        {
                            //if (!cbxIgnoreCityDistrict.Checked)
                            nzp_raj = SaveSettlement(finder.settlement, conn_db, nzp_city);
                            if (nzp_raj == -1) { ret.result = false; return ret; }
                        }
                        if (nzp_city == 0)
                        {
                            nzp_raj = SaveSettlement(finder.settlement, conn_db, nzp_town);
                            if (nzp_raj == -1) { ret.result = false; return ret; }
                        }
                    }

                    if (nzp_raj == 0)
                    {
                        var tmpObj = new KLADRData() { code = "-", fullname = "-" };
                        if (nzp_city != 0)
                        {
                            nzp_raj = SaveSettlement(tmpObj, conn_db, nzp_city);
                            if (nzp_raj == -1) { ret.result = false; return ret; }
                        }
                        else
                        {
                            nzp_raj = SaveSettlement(tmpObj, conn_db, nzp_town);
                            if (nzp_raj == -1) { ret.result = false; return ret; }
                        }

                        nzp_ul = SaveStreet(finder.street, conn_db, nzp_raj);
                        if (nzp_raj == -1) { ret.result = false; return ret; }
                    }
                    else
                    {
                        nzp_ul = SaveStreet(finder.street, conn_db, nzp_raj);
                        if (nzp_raj == -1) { ret.result = false; return ret; }
                    }
                }
                #endregion Для выгрузки выбрана улица

                #region Для выгрузки выбран населенный пункт
                if (finder.level == "settlement")
                {
                    decimal nzp_stat = SaveRegion(finder.region, conn_db);

                    if (finder.district != null)
                    {
                        nzp_town = SaveDistricrt(finder.district, conn_db, nzp_stat);
                    }

                    if (finder.city != null)
                    {
                        nzp_city = SaveCity(finder.city, conn_db, nzp_stat);
                    }

                    SetProgress(conn_db, id, 0.7);

                    if (nzp_city != 0)
                        nzp_raj = SaveSettlement(finder.settlement, conn_db, nzp_city);
                    if (nzp_city == 0)
                        nzp_raj = SaveSettlement(finder.settlement, conn_db, nzp_town);

                    if (finder.loadStreets)
                    {
                        foreach (var obj in finder.streetList)
                        {
                            if (nzp_raj == 0)
                            {
                                var tmpObj = new KLADRData() { code = "-", fullname = "-" };
                                if (nzp_city != 0)
                                    nzp_raj = SaveSettlement(tmpObj, conn_db, nzp_city);
                                else
                                    nzp_raj = SaveSettlement(tmpObj, conn_db, nzp_town);

                                SaveStreet(obj, conn_db, nzp_raj);
                            }
                            else
                                SaveStreet(obj, conn_db, nzp_raj);
                        }
                    }

                    SetProgress(conn_db, id, 0.9);
                }
                #endregion Для выгрузки выбран населенный пункт

                #region Для выгрузки выбран город
                if (finder.level == "city")
                {
                    decimal nzp_stat = SaveRegion(finder.region, conn_db);

                    if (finder.district != null)
                    {
                        nzp_town = SaveDistricrt(finder.district, conn_db, nzp_stat);
                    }

                    nzp_city = SaveCity(finder.city, conn_db, nzp_stat);

                    SetProgress(conn_db, id, 0.3);

                    var tmpObj = new KLADRData() { code = "-", fullname = "-" };
                    nzp_raj = SaveSettlement(tmpObj, conn_db, nzp_city);

                    if (finder.loadStreets)
                    {
                        for (int i = 0; i < finder.streetList.Count; i++)
                        {
                            SaveStreet(finder.streetList[i], conn_db, nzp_raj);
                        }
                    }

                    SetProgress(conn_db, id, 0.5);

                    for (int j = 0; j < finder.settlementList.Count; j++)
                    {
                        nzp_raj = SaveSettlement(finder.settlementList[j], conn_db, nzp_city);

                        if (j == finder.settlementList.Count / 4)
                            SetProgress(conn_db, id, 0.6);

                        if (j == finder.settlementList.Count / 2)
                            SetProgress(conn_db, id, 0.7);

                        if (finder.loadStreets)
                        {
                            #region считывание улиц
                            OleDbCommand cmd = new OleDbCommand();
                            cmd.CommandText = "select * from street where mid(CODE, 1 , 11) = '" + finder.settlementList[j].code + "' and mid(CODE, 16 , 2) = '00'";
                            cmd.Connection = oDbCon;
                            // Адаптер данных
                            OleDbDataAdapter da = new OleDbDataAdapter();
                            da.SelectCommand = cmd;
                            // Заполняем объект данными
                            DataTable tbl = new DataTable();
                            da.Fill(tbl);
                            da.Dispose();
                            tbl.Columns.Add("FULLNAME", typeof(String));
                            for (int i = 0; i < tbl.Rows.Count; i++)
                            {
                                //склеить название и сокращение
                                tbl.Rows[i]["CODE"] = tbl.Rows[i]["CODE"].ToString().Substring(0, 15);
                                string socr = "";
                                if (tbl.Rows[i]["SOCR"] != null)
                                {
                                    socr = tbl.Rows[i]["SOCR"].ToString().Trim();
                                    tbl.Rows[i]["SOCR"] = socr;
                                }
                                string name = "";
                                if (tbl.Rows[i]["NAME"] != null)
                                {
                                    name = tbl.Rows[i]["NAME"].ToString().Trim();
                                    tbl.Rows[i]["NAME"] = name;
                                }
                                tbl.Rows[i]["FULLNAME"] = name + " " + socr;
                            }
                            #endregion

                            foreach (DataRow dr in tbl.Rows)
                            {
                                SaveStreet(new KLADRData() { fullname = dr["FULLNAME"].ToString(), code = dr["CODE"].ToString(), name = dr["NAME"].ToString(), socr = dr["SOCR"].ToString() }, conn_db, nzp_raj);
                            }
                        }
                    }
                }
                #endregion Для выгрузки выбран город

                #region Для выгрузки выбран район
                if (finder.level == "district")
                {
                    List<KLADRData> tmpSettlementList = new List<KLADRData>();
                    List<KLADRData> tmpStreetList = new List<KLADRData>();

                    decimal nzp_stat = SaveRegion(finder.region, conn_db);

                    nzp_town = SaveDistricrt(finder.district, conn_db, nzp_stat);

                    SetProgress(conn_db, id, 0.1);

                    if (finder.cityList != null)
                    {
                        for (int c = 0; c < finder.cityList.Count; c++)
                        {
                            if (c == finder.cityList.Count / 4)
                                SetProgress(conn_db, id, 0.3);
                            if (c == finder.cityList.Count / 2)
                                SetProgress(conn_db, id, 0.4);

                            nzp_city = SaveCity(finder.cityList[c], conn_db, nzp_stat);
                            city = finder.cityList[c].code.Substring(5, 3);

                            tmpSettlementList.Clear();

                            tmpSettlementList = LoadDataFromKLADR(new KLADRFinder()
                            {
                                query = "select * from kladr where mid(CODE, 1, 2) = '" + region + "'" +
                                 " AND mid(CODE, 3, 3) = '" + district + "'AND mid(CODE, 6, 3) = '" + city + "' AND mid(CODE, 9, 3) <> '000' and mid(CODE, 12, 2) = '00'"
                            }).returnsData;

                            var tmpObj = new KLADRData() { code = "-", fullname = "-" };
                            nzp_raj = SaveSettlement(tmpObj, conn_db, nzp_city);

                            if (finder.loadStreets)
                            {
                                tmpStreetList.Clear();
                                tmpStreetList = LoadDataFromKLADR(new KLADRFinder()
                                {
                                    query = "select * from street where mid(CODE, 1 , 11) = '" + finder.cityList[c].code + "' and mid(CODE, 16 , 2) = '00'",
                                    level = "street"
                                }).returnsData;
                                for (int i = 0; i < tmpStreetList.Count; i++)
                                {
                                    SaveStreet(tmpStreetList[i], conn_db, nzp_raj);
                                }
                            }

                            if (tmpSettlementList.Count != 0)
                            {
                                for (int s = 0; s < tmpSettlementList.Count; s++)
                                {
                                    nzp_raj = SaveSettlement(tmpSettlementList[s], conn_db, nzp_city);
                                    if (finder.loadStreets)
                                    {
                                        tmpStreetList.Clear();
                                        tmpStreetList = LoadDataFromKLADR(new KLADRFinder()
                                        {
                                            query = "select * from street where mid(CODE, 1 , 11) = '" + tmpSettlementList[s].code + "' and mid(CODE, 16 , 2) = '00'",
                                            level = "street"
                                        }).returnsData;
                                        for (int i = 0; i < tmpStreetList.Count; i++)
                                        {
                                            SaveStreet(tmpStreetList[i], conn_db, nzp_raj);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    SetProgress(conn_db, id, 0.5);

                    tmpSettlementList.Clear();
                    tmpSettlementList = LoadDataFromKLADR(new KLADRFinder()
                    {
                        query = "select * from kladr where mid(CODE, 1, 2) = '" + region + "'" +
                        " AND mid(CODE, 3, 3) = '" + district + "'AND mid(CODE, 6, 3) = '000' AND mid(CODE, 9, 3) <> '000' and mid(CODE, 12, 2) = '00'"
                    }).returnsData;

                    for (int j = 0; j < tmpSettlementList.Count; j++)
                    {
                        nzp_raj = SaveSettlement(tmpSettlementList[j], conn_db, nzp_town);

                        if (j == tmpSettlementList.Count / 4)
                            SetProgress(conn_db, id, 0.7);
                        if (j == tmpSettlementList.Count / 2)
                            SetProgress(conn_db, id, 0.8);

                        if (finder.loadStreets)
                        {
                            tmpStreetList.Clear();
                            tmpStreetList = LoadDataFromKLADR(new KLADRFinder()
                            {
                                query = "select * from street where mid(CODE, 1 , 11) = '" + tmpSettlementList[j].code + "' and mid(CODE, 16 , 2) = '00'",
                                level = "street"
                            }).returnsData;

                            for (int i = 0; i < tmpStreetList.Count; i++)
                            {
                                SaveStreet(tmpStreetList[i], conn_db, nzp_raj);
                            }
                        }
                    }
                }
                #endregion Для выгрузки выбран район

                #region Для выгрузки выбран регион
                if (finder.level == "region")
                {
                    decimal nzp_stat = SaveRegion(finder.region, conn_db);

                    List<KLADRData> tmpCityList = new List<KLADRData>();
                    List<KLADRData> tmpSettlementList = new List<KLADRData>();
                    List<KLADRData> tmpStreetList = new List<KLADRData>();

                    SetProgress(conn_db, id, 0.1);

                    #region сохранение улиц, принадлежащих региону
                    if (finder.loadStreets && finder.streetList != null && finder.streetList.Count != 0)
                    {
                        var tmpObj = new KLADRData() { code = "-", fullname = "-" };

                        nzp_town = SaveDistricrt(tmpObj, conn_db, nzp_stat);
                        nzp_raj = SaveSettlement(tmpObj, conn_db, nzp_town);

                        for (int f = 0; f < finder.streetList.Count; f++)
                        {
                            SaveStreet(finder.streetList[f], conn_db, nzp_raj);
                        }
                    }
                    #endregion сохранение улиц, принадлежащих региону

                    for (int c = 0; c < finder.cityList.Count; c++)
                    {
                        nzp_city = SaveCity(finder.cityList[c], conn_db, nzp_stat);
                        city = finder.cityList[c].code.Substring(5, 3);

                        tmpSettlementList.Clear();
                        tmpSettlementList = LoadDataFromKLADR(new KLADRFinder()
                        {
                            query = "select * from kladr where mid(CODE, 1, 2) = '" + region + "'" +
                            " AND mid(CODE, 3, 3) = '000'AND mid(CODE, 6, 3) = '" + city + "' AND mid(CODE, 9, 3) <> '000' and mid(CODE, 12, 2) = '00'"
                        }).returnsData;

                        var tmpObj = new KLADRData() { code = "-", fullname = "-" };
                        nzp_raj = SaveSettlement(tmpObj, conn_db, nzp_city);

                        if (finder.loadStreets)
                        {
                            tmpStreetList.Clear();
                            tmpStreetList = LoadDataFromKLADR(new KLADRFinder()
                            {
                                query = "select * from street where mid(CODE, 1 , 11) = '" + finder.cityList[c].code + "' and mid(CODE, 16 , 2) = '00'",
                                level = "street"
                            }).returnsData;
                            for (int i = 0; i < tmpStreetList.Count; i++)
                            {
                                SaveStreet(tmpStreetList[i], conn_db, nzp_raj);
                            }
                        }

                        if (tmpSettlementList.Count != 0)
                        {
                            for (int s = 0; s < tmpSettlementList.Count; s++)
                            {
                                nzp_raj = SaveSettlement(tmpSettlementList[s], conn_db, nzp_city);
                                if (finder.loadStreets)
                                {
                                    tmpStreetList.Clear();
                                    tmpStreetList = LoadDataFromKLADR(new KLADRFinder()
                                    {
                                        query = "select * from street where mid(CODE, 1 , 11) = '" + tmpSettlementList[s].code + "' and mid(CODE, 16 , 2) = '00'",
                                        level = "street"
                                    }).returnsData;
                                    for (int i = 0; i < tmpStreetList.Count; i++)
                                    {
                                        SaveStreet(tmpStreetList[i], conn_db, nzp_raj);
                                    }
                                }
                            }
                        }
                    }
                    
                    SetProgress(conn_db, id, 0.3);

                    for (int d = 0; d < finder.districtList.Count; d++)
                    {
                        if (d == finder.districtList.Count / 8)
                            SetProgress(conn_db, id, 0.4);
                        if (d == finder.districtList.Count / 4)
                            SetProgress(conn_db, id, 0.6);
                        if (d == finder.districtList.Count / 2)
                            SetProgress(conn_db, id, 0.7);
                        if (d == finder.districtList.Count * 3 / 4)
                            SetProgress(conn_db, id, 0.8);


                        nzp_town = SaveDistricrt(finder.districtList[d], conn_db, nzp_stat);

                        district = finder.districtList[d].code.Substring(2, 3);
                        tmpCityList.Clear();
                        tmpCityList = LoadDataFromKLADR(new KLADRFinder()
                        {
                            query = "select * from kladr where mid(CODE, 1, 2) = '" + region + "'" +
                            " AND mid(CODE, 3, 3) = '" + district + "'AND mid(CODE, 9, 3) = '000' AND mid(CODE, 6, 3) <> '000' and mid(CODE, 12, 2) = '00'"
                        }).returnsData;

                        for (int c = 0; c < tmpCityList.Count; c++)
                        {
                            nzp_city = SaveCity(tmpCityList[c], conn_db, nzp_stat);
                            city = tmpCityList[c].code.Substring(5, 3);

                            var tmpObj = new KLADRData() { code = "-", fullname = "-" };
                            nzp_raj = SaveSettlement(tmpObj, conn_db, nzp_city);

                            if (finder.loadStreets)
                            {
                                tmpStreetList.Clear();
                                tmpStreetList = LoadDataFromKLADR(new KLADRFinder()
                                {
                                    query = "select * from street where mid(CODE, 1 , 11) = '" + tmpCityList[c].code + "' and mid(CODE, 16 , 2) = '00'",
                                    level = "street"
                                }).returnsData;
                                for (int i = 0; i < tmpStreetList.Count; i++)
                                {
                                    SaveStreet(tmpStreetList[i], conn_db, nzp_raj);
                                }
                            }

                            tmpSettlementList.Clear();
                            tmpSettlementList = LoadDataFromKLADR(new KLADRFinder()
                            {
                                query = "select * from kladr where mid(CODE, 1, 2) = '" + region + "'" +
                                " AND mid(CODE, 3, 3) = '" + district + "'AND mid(CODE, 6, 3) = '" + city + "' AND mid(CODE, 9, 3) <> '000' and mid(CODE, 12, 2) = '00'"
                            }).returnsData;

                            if (tmpSettlementList.Count != 0)
                            {
                                for (int s = 0; s < tmpSettlementList.Count; s++)
                                {
                                    nzp_raj = SaveSettlement(tmpSettlementList[s], conn_db, nzp_city);
                                    if (finder.loadStreets)
                                    {
                                        tmpStreetList.Clear();
                                        tmpStreetList = LoadDataFromKLADR(new KLADRFinder()
                                        {
                                            query = "select * from street where mid(CODE, 1 , 11) = '" + tmpSettlementList[s].code + "' and mid(CODE, 16 , 2) = '00'",
                                            level = "street"
                                        }).returnsData;
                                        for (int i = 0; i < tmpStreetList.Count; i++)
                                        {
                                            SaveStreet(tmpStreetList[i], conn_db, nzp_raj);
                                        }
                                    }
                                }
                            }
                        }

                        tmpSettlementList.Clear();
                        tmpSettlementList = LoadDataFromKLADR(new KLADRFinder()
                        {
                            query = "select * from kladr where mid(CODE, 1, 2) = '" + region + "'" +
                            " AND mid(CODE, 3, 3) = '" + district + "'AND mid(CODE, 6, 3) = '000' AND mid(CODE, 9, 3) <> '000' and mid(CODE, 12, 2) = '00'"
                        }).returnsData;

                        for (int j = 0; j < tmpSettlementList.Count; j++)
                        {
                            nzp_raj = SaveSettlement(tmpSettlementList[j], conn_db, nzp_town);

                            if (finder.loadStreets)
                            {
                                tmpStreetList.Clear();
                                tmpStreetList = LoadDataFromKLADR(new KLADRFinder()
                                {
                                    query = "select * from street where mid(CODE, 1 , 11) = '" + tmpSettlementList[j].code + "' and mid(CODE, 16 , 2) = '00'",
                                    level = "street"
                                }).returnsData;

                                for (int i = 0; i < tmpStreetList.Count; i++)
                                {
                                    SaveStreet(tmpStreetList[i], conn_db, nzp_raj);
                                }
                            }
                        }
                    }
                }
                #endregion регион

                //запись в базу об окончании загрузки
                SetProgress(conn_db, id, 1);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка в функции UploadKLADRAddrSpace:\n" + ex.Message, MonitorLog.typelog.Error, true);
            }

            return ret;
        }

        //обновление прогресса загрузки
        private void SetProgress(IDbConnection conn, int id, double progress)
        {
            var sqlStr = " update " + Points.Pref + "_data"+tableDelimiter + "upload_progress set progress = " + progress + " where id = " + id;
            ExecSQL(conn, sqlStr, true);
        }
       
    }
}

