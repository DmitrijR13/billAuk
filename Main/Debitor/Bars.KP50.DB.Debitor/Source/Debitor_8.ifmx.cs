using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FastReport;
using FastReport.Export.OoXML;
using FastReport.Utils;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Global;
using System.Data;

namespace STCLINE.KP50.DataBase
{
    public partial class Debitor : DataBaseHead
    {

        /// <summary>
        ///  Совершить операцию над делом - смена статуса дела
        /// </summary>
        /// <param name="finder">операция, дело</param>
        /// <param name="ret">результат</param>
        public void MakeOperOnDeal(deal_states_history finder, out Returns ret)
        {
            this.MakeOperOnDeal(finder, null, null, out ret);
        }


        /// <summary>
        /// Совершить операцию над делом - смена статуса дела
        /// </summary>
        /// <param name="finder">операция, дело</param>
        /// <param name="trans">Транзакция</param>
        /// <param name="ret">результат</param>
        public void MakeOperOnDeal(deal_states_history finder, IDbConnection conn, IDbTransaction trans, out Returns ret)
        {

            #region Проверка данных
            ret = Utils.InitReturns();

            if (finder.nzp_deal == 0)
            {
                ret = new Returns(false, "Отсутствуют данные по делу", -1);
                ret.result = false;
                return;
            }

            if (finder.nzp_user < 0)
            {
                ret = new Returns(false, "Не задан пользователь", -1);
                ret.result = false;
                return;
            }

            if (finder.nzp_oper <= 0)
            {
                ret = new Returns(false, "Не задана операция", -1);
                ret.result = false;
                return;
            }
            #endregion
            
            StringBuilder sql = new StringBuilder();

            try
            {
                #region Открываем соединение
                if (conn == null)
                {
                    conn = GetConnection(Constants.cons_Kernel);
                    ret = OpenDb(conn, true);
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("MakeOperOnDeal : Ошибка при открытии соединения с БД ",
                            MonitorLog.typelog.Error, true);
                        return;
                    }
                }

                #endregion


                #region Обновление статуса дела
                if (finder.nzp_oper != 0)
                {
                    sql.Append(" UPDATE " + Points.Pref + "_debt.deal ");
                    sql.Append(" set nzp_deal_status = ");
                    sql.Append(" (SELECT nzp_deal_status from " + Points.Pref + "_debt.s_opers where nzp_oper = " + finder.nzp_oper + ") ");
                    sql.Append(" where nzp_deal = " + finder.nzp_deal + "; ");
                    if (!ExecSQL(conn, trans, sql.ToString(), true).result)
                    {
                        MonitorLog.WriteLog("MakeOperOnDeal: Ошибка : " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                        ret.text = "Ошибка смены статуса дела";
                        ret.result = false;
                        return;
                    }
                }
                #endregion

                #region Сохранение в deal_states_history
                sql.Remove(0, sql.Length);
                sql.Append(" INSERT into " + Points.Pref + "_debt.deal_states_history (nzp_deal,date_state,debt_money,plus,minus,nzp_oper) ");
                sql.Append(" VALUES (" + finder.nzp_deal + ",CURRENT_TIMESTAMP, (select debt_money from " + Points.Pref + "_debt.deal where nzp_deal = " + finder.nzp_deal + ")," + finder.plus + "," + finder.minus + "," + (finder.nzp_oper != 0 ? finder.nzp_oper.ToString() : "NULL") + " ); ");                 
                if (!ExecSQL(conn, trans, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("MakeOperOnDeal: Ошибка : " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.text = "Ошибка смены статуса дела";
                    ret.result = false;
                    return;
                }
                #endregion

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetArgDetail : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = "Ошибка сохранения данных";
                ret.result = false;
                return;
            }
        }

        /// <summary>
        /// Изменение количества долга у дела.
        /// </summary>
        /// <param name="finder">дело</param>
        /// <param name="conn">подключение. Открывается автоматически по состоянию</param>
        /// <param name="ret">результат</param>
        public void ChangeMoneyOnDeal(Deal finder, IDbConnection conn, IDbTransaction trans, out Returns ret)
        {

            #region Проверка данных
            ret = Utils.InitReturns();

            if (finder.nzp_deal == 0)
            {
                ret = new Returns(false, "Отсутствуют данные по делу", -1);
                ret.result = false;
                return;
            }

            if (finder.nzp_user < 0)
            {
                ret = new Returns(false, "Не задан пользователь", -1);
                ret.result = false;
                return;
            }          
            #endregion
            
            StringBuilder sql = new StringBuilder();

            try
            {
                #region Открываем соединение
                if (conn == null)
                {
                    conn = GetConnection(Constants.cons_Kernel);
                    ret = OpenDb(conn, true);
                    if (!ret.result)
                    {
                        MonitorLog.WriteLog("MakeOperOnDeal : Ошибка при открытии соединения с БД ",
                            MonitorLog.typelog.Error, true);
                        return;
                    }
                }
                #endregion


                #region Обновление долга дела
                sql.Append(" UPDATE " + Points.Pref + "_debt.deal ");
                sql.Append(" set debt_money = " + finder.debt_money+ " ");
                sql.Append(" where nzp_deal = " + finder.nzp_deal);                
                if (!ExecSQL(conn, trans, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("ChangeMoneyOnDeal: Ошибка : " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.text = "Ошибка обновления количества долга у дела";
                    ret.result = false;
                    return;
                }
                #endregion                

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры ChangeMoneyOnDeal : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = "Ошибка сохранения данных";
                ret.result = false;
                return;
            }
        }

        public string GetDebtorList(ChargeFind finder, out Returns ret)
        {
            //проверка пользователя
            if (finder.nzp_user < 0)
            {
                ret = new Returns(false, "Не задан пользователь", -1);
                return null;
            }

            var excelRepDb = new ExcelRepClient();
            ret = excelRepDb.AddMyFile(new ExcelUtility
            {
                nzp_user = finder.nzp_user,
                status = ExcelUtility.Statuses.InProcess,
                rep_name = "Отчет 'Сведения о должниках'"
            });
            int nzpExc = ret.tag;

            var report = new Report();
            string fileName = string.Empty;

            MyDataReader reader = null;
            string centralData = Points.Pref + DBManager.sDataAliasRest,
                    centralKernel = Points.Pref + DBManager.sKernelAliasRest;

            //подключение к БД
            IDbConnection connDB = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(connDB, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("GetDebtorList : Ошибка при открытии соединения с БД ",
                    MonitorLog.typelog.Error, true);
                return null;
            }

            try
            {
                string tXXDebt = DBManager.GetFullBaseName(connDB) + DBManager.tableDelimiter + "t" + finder.nzp_user + "_debt";

                if (TempTableInWebCashe(connDB, tXXDebt))
                {
                    //получить префик банка данных
                    string sql = " SELECT TRIM(pref) AS pref " +
                                 " FROM " + tXXDebt + " GROUP BY pref ";
                    ret = ExecRead(connDB, out reader, sql, true);
                    if (!ret.result) throw new Exception(ret.text);

                    #region создание временной таблицы
                    ExecSQL(connDB, "DROP TABLE t_dolgnik", false);
                    sql = " CREATE TEMP TABLE t_dolgnik( " +
                                " nzp_kvar INTEGER, " +
                                " num_ls INTEGER, " +
                                " fio CHARACTER(50)," +
                                " nkvar CHARACTER(10)," +
                                " ikvar INTEGER, " +
                                " ndom CHARACTER(10)," +
                                " idom INTEGER, " +
                                " nkor CHARACTER(3), " +
                                " ulica CHARACTER(40), " +
                                " rajon CHARACTER(30), " +
                                " town CHARACTER(30), " +
                                " gil INTEGER, " +
                                " rsum_tarif " + DBManager.sDecimalType + "(14,2), " +
                                " sum_outsaldo " + DBManager.sDecimalType + "(14,2)," +
                                " dolg " + DBManager.sDecimalType + "(14,2)) " + DBManager.sUnlogTempTable;
                    ret = ExecSQL(connDB, sql);
                    if (!ret.result) throw new Exception(ret.text);

                    ExecSQL(connDB, "CREATE INDEX idx_t_dolgnik_01 ON t_dolgnik(nzp_kvar)", false);
                    #endregion

                    #region заполнение временной таблицы
                    //занесение выбранных должников 
                    sql = " INSERT INTO t_dolgnik(nzp_kvar, num_ls, fio, nkvar, ikvar, ndom, idom, nkor, ulica, rajon, town) " +
                          " SELECT nzp_kvar, num_ls, fio, nkvar, ikvar, ndom, idom, nkor, ulica, rajon, town " +
                          " FROM " + centralData + "kvar k INNER JOIN " + centralData + "dom d ON d.nzp_dom = k.nzp_dom " +
                                                         " INNER JOIN " + centralData + "s_ulica u ON u.nzp_ul = d.nzp_ul " +
                                                         " INNER JOIN " + centralData + "s_rajon r ON r.nzp_raj = u.nzp_raj " +
                                                         " INNER JOIN " + centralData + "s_town t ON t.nzp_town = r.nzp_town " +
                          " WHERE k.nzp_kvar IN (SELECT nzp_kvar FROM " + tXXDebt + ") ";
                    ret = ExecSQL(connDB, sql);
                    if (!ret.result) throw new Exception(ret.text);

                    ExecSQL(connDB, DBManager.sUpdStat + " t_dolgnik", false);

                    while (reader.Read())
                    {
                        string pref = reader["pref"].ToString().Trim();
                        string prefData = pref + DBManager.sDataAliasRest;
                        string chargeXX = pref + "_charge_" + (finder.year_ - 2000).ToString("00") +
                                          DBManager.tableDelimiter + "charge_" + finder.month_.ToString("00");

                        if (TempTableInWebCashe(connDB, chargeXX))
                        {
                            //занести информацию о начислениях, исх.садьдо, вх.сальдо, оплате
                            ExecSQL(connDB, " DROP TABLE t_dolg_charge ", false);
                            sql = " SELECT nzp_kvar, " +
                                  " SUM(rsum_tarif) AS rsum_tarif, " +
                                  " SUM(sum_outsaldo) AS sum_outsaldo, " +
                                  " SUM(sum_insaldo)- SUM(sum_money)+ SUM(CASE WHEN reval >= 0 then 0 ELSE reval END)+" +
                                  " SUM(CASE WHEN real_charge >= 0 then 0 ELSE real_charge END) AS sum_dolg " +
                                  " INTO TEMP t_dolg_charge " +
                                  " FROM " + chargeXX +
                                  " WHERE sum_insaldo > 0 " +
                                  " AND nzp_serv > 1 " +
                                  " GROUP BY nzp_kvar ";
                            ret = ExecSQL(connDB, sql);
                            if (!ret.result) throw new Exception(ret.text);

                            ExecSQL(connDB, "CREATE INDEX idx_t_dolg_charge_01 ON t_dolg_charge(nzp_kvar)", false);
                            ExecSQL(connDB, DBManager.sUpdStat + " t_dolg_charge", false);

                            sql = " UPDATE t_dolgnik SET " +
                                    " rsum_tarif = ( SELECT SUM(rsum_tarif) " +
                                                   " FROM t_dolg_charge" +
                                                   " WHERE nzp_kvar = t_dolgnik.nzp_kvar ), " +
                                    " sum_outsaldo = ( SELECT SUM(sum_outsaldo) " +
                                                     " FROM t_dolg_charge" +
                                                     " WHERE nzp_kvar = t_dolgnik.nzp_kvar ), " +
                                    " dolg = ( SELECT SUM(sum_dolg) " +
                                                     " FROM t_dolg_charge" +
                                                     " WHERE nzp_kvar = t_dolgnik.nzp_kvar ) ";
                            ret = ExecSQL(connDB, sql);
                            if (!ret.result) throw new Exception(ret.text);

                            //кол-во жильцов
                            sql = " UPDATE t_dolgnik SET " +
                                    " gil = ( SELECT MAX(val_prm " + DBManager.sConvToInt + ") " +
                                                   " FROM " + prefData + "prm_1 " +
                                                   " WHERE nzp = t_dolgnik.nzp_kvar " +
                                                     " AND nzp_prm = 5 " +
                                                     " AND dat_s <= DATE('" + DateTime.DaysInMonth(finder.year_,finder.month_) + "." + finder.month_ + "." + finder.year_ + "') " +
                                                     " AND dat_po >= DATE('1." + finder.month_ + "." + finder.year_ + "')" +
                                                     " AND is_actual <> 100) ";
                            ret = ExecSQL(connDB, sql);
                            if (!ret.result) throw new Exception(ret.text);
                        }
                    }
                    #endregion

                    sql = " SELECT num_ls, " +
                                 " TRIM(town) AS town, " +
                                 " TRIM(rajon) AS rajon, " +
                                 " TRIM(ulica) AS ulica, " +
                                 " TRIM(ndom) AS ndom," +
                                 " TRIM(nkor) AS nkor, " +
                                 " idom," +
                                 " TRIM(nkvar) AS nkvar, " +
                                 " TRIM(fio) AS fio, " +
                                 " gil, " +
                                 " rsum_tarif, sum_outsaldo, dolg " +
                          " FROM t_dolgnik ORDER BY town, rajon, ulica, idom, nkor, ikvar, nkvar ";

                    DataTable dt = DBManager.ExecSQLToTable(connDB,sql);
                    dt.TableName = "Q_master";

                    //установка таблиц с данными
                    var ds = new DataSet();
                    ds.Tables.Add(dt);
                    report.RegisterData(ds);

                    //загрузка шаблона файла
                    fileName = PathHelper.GetReportTemplatePath("Svedenya_o_dolzhnikah.frx");
                    report.Load(fileName);

                    #region загрузка параметров

                    string territory = string.Empty;
                    string uk = string.Empty;
                    string zheu = string.Empty;
                    string uch = string.Empty;

                    //территория
                    sql = " SELECT TRIM(point) AS point " +
                                 " FROM " + centralKernel + "s_point" +
                                 " WHERE nzp_wp IN ( SELECT nzp_wp FROM " + centralData + "kvar k," +
                                                                            tXXDebt + " t " +
                                                                 " WHERE k.nzp_kvar = t.nzp_kvar GROUP BY nzp_wp ) ";
                    DataTable tt = DBManager.ExecSQLToTable(connDB, sql);
                    territory = tt.Rows.Cast<DataRow>()
                        .Aggregate(territory,
                            (current, value) =>
                                current + (value["point"] != DBNull.Value ? value["point"] + ", " : string.Empty))
                        .TrimEnd(' ', ',');

                    //УК
                    sql = " SELECT TRIM(area) AS area " +
                                 " FROM " + centralData + "kvar k INNER JOIN " + centralData + "s_area a ON a.nzp_area = k.nzp_area " +
                                 " WHERE nzp_kvar IN ( SELECT nzp_kvar " +
                                                     " FROM " + tXXDebt + " t) " +
                          " GROUP BY area ";
                    tt = DBManager.ExecSQLToTable(connDB, sql);
                    uk = tt.Rows.Cast<DataRow>()
                        .Aggregate(uk,
                            (current, value) =>
                                current + (value["area"] != DBNull.Value ? value["area"] + ", " : string.Empty))
                        .TrimEnd(' ', ',');

                    //ЖЭУ
                    sql = " SELECT TRIM(geu) AS geu " +
                                 " FROM " + centralData + "kvar k INNER JOIN " + centralData + "s_geu g ON g.nzp_geu = k.nzp_geu " +
                                 " WHERE nzp_kvar IN ( SELECT nzp_kvar " +
                                                     " FROM " + tXXDebt + " t) " +
                          " GROUP BY geu ";
                    tt = DBManager.ExecSQLToTable(connDB, sql);
                    zheu = tt.Rows.Cast<DataRow>()
                        .Aggregate(zheu,
                            (current, value) =>
                                current + (value["geu"] != DBNull.Value ? value["geu"] + ", " : string.Empty))
                        .TrimEnd(' ', ',');

                    //Учтасток
                    sql = " SELECT uch " +
                          " FROM " + centralData + "kvar k " +
                          " WHERE nzp_kvar IN ( SELECT nzp_kvar " +
                                                     " FROM " + tXXDebt + " t) " +
                          " GROUP BY uch ";
                    tt = DBManager.ExecSQLToTable(connDB, sql);
                    uch = tt.Rows.Cast<DataRow>()
                        .Aggregate(uch,
                            (current, value) =>
                                current + (value["uch"] != DBNull.Value ? value["uch"] + ", " : string.Empty))
                        .TrimEnd(' ', ',');

                    var month = new[] { "", "Январь", "Февраль","Март", 
                                            "Апрель", "Май",    "Июнь", 
                                            "Июль",   "Август", "Сентябрь", 
                                            "Октябрь","Ноябрь", "Декабрь" };
                    report.SetParameterValue("DATE", DateTime.Now.ToShortDateString());
                    report.SetParameterValue("TIME", DateTime.Now.ToShortTimeString());
                    report.SetParameterValue("period", "за " + month[finder.month_] + " " + finder.year_ + " г.");

                    report.SetParameterValue("territory", territory != string.Empty ? territory : "<все территории>");
                    report.SetParameterValue("uk", uk != string.Empty ? uk : "<все УК>");
                    report.SetParameterValue("zheu", zheu != string.Empty ? zheu : "<все ЖЭУ>");
                    report.SetParameterValue("uch", uch != string.Empty ? uch : "<все участки>");
#endregion

                    //сформировать отчет
                    report.Prepare();

                    //сохранение отчета
                    fileName = "VedDol_" + DateTime.Now.ToShortDateString() + 
                                                  DateTime.Now.Hour + 
                                                  DateTime.Now.Minute + 
                                                  DateTime.Now.Second + 
                                                  DateTime.Now.Millisecond ;
                    string fileNameXLS = fileName + ".xls", 
                           fileNameFPX = fileName + ".fpx";

                    var exporter = new Excel2007Export {ShowProgress = false};
                    exporter.Export(report, Constants.FilesDir + fileNameXLS);

                    report.SavePrepared(Constants.FilesDir + fileNameFPX);

                    //сохранение отчета на ftp
                    if (InputOutput.useFtp)
                    {
                        if (File.Exists(Path.Combine(Constants.FilesDir, fileNameXLS)))
                            fileNameXLS = InputOutput.SaveInputFile(Path.Combine(Constants.FilesDir, fileNameXLS));

                        if (File.Exists(Path.Combine(Constants.FilesDir, fileNameFPX)))
                            fileName = InputOutput.SaveInputFile(Path.Combine(Constants.FilesDir, fileNameFPX));
                    }

                    excelRepDb.SetMyFileState(new ExcelUtility
                    {
                        nzp_exc = nzpExc,
                        status = ExcelUtility.Statuses.Success,
                        exc_path = fileNameXLS
                    });
                }
                else
                {
                    ret = new Returns(false, "Данные не были выбраны! Для получения отчета необходимо выполнить поиск должников.", -1);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetDebtorList : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = "Ошибка выполнение отчета";
                ret.result = false;
                return null;
            }
            finally
            {
                if (reader != null) reader.Close();
                connDB.Close();
            }
            return fileName;
        }

    }
}
