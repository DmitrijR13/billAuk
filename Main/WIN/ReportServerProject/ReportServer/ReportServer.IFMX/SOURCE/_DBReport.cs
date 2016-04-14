using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IBM.Data.Informix;
using System.Data;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Global;
using STCLINE.KP50.Utility;
using System.Diagnostics;


namespace ReportServer
{
    /// <summary>
    /// класс для работы с БД
    /// </summary>
    public partial class _DBReport : DataBaseHead
    {
        /// <summary>
        /// процедура чтения данных о выгружаемых отчетах из БД
        /// </summary>
        /// <returns>список отчетов</returns>
        public ReturnsObjectType<List<ReportParams>> GetReportList(IfxConnection conn_web, IfxConnection conn_db, string exportPath, string templatePath)
        {
            List<ReportParams> res = new List<ReportParams>();
            try
            {
                string connWeb = conn_web.ConnectionString;
                _DBReport db = new _DBReport();
                StringBuilder sql = new StringBuilder();
                //полчаем все необработанные отчеты на выполнение
                sql.Append("SELECT rs.nzp_exc, r.nzp_act, r.name, r.dll_name, rs.dat_in, r.file_name, r.name, rs.nzp_rep, rs.nzp_user ");
                sql.Append("FROM rep_user_reports rs, report r ");
                sql.Append("WHERE status = 0 AND r.nzp_act = rs.nzp_rep;");
                conn_web.Open();
                foreach (DataRow row in ClassDBUtils.OpenSQL(sql.ToString(), conn_web).GetData().Rows)
                {
                    ReportParams report = new ReportParams();
                    report.nzp = row["nzp_exc"] == DBNull.Value ? 0 : Convert.ToInt32((row["nzp_exc"]));
                    report.id = row["nzp_act"] == DBNull.Value ? 0 : Convert.ToInt32((row["nzp_act"]));
                    report.date_in = row["dat_in"] == DBNull.Value ? "" : Convert.ToString((row["dat_in"]));
                    report.ftemplateName = row["file_name"] == DBNull.Value ? "" : Convert.ToString((row["file_name"])).Trim();
                    report.name = row["name"] == DBNull.Value ? "" : Convert.ToString((row["name"]));
                    report.nzp_user = row["nzp_user"] == DBNull.Value ? 0 : Convert.ToInt32((row["nzp_user"]));
                    report.dllName = row["dll_name"] == DBNull.Value ? "" : Convert.ToString((row["dll_name"])).Trim();
                    report.fileName = row["name"] == DBNull.Value ? "" : Convert.ToString((row["name"])).Trim();

                    sql.Remove(0, sql.Length);
                    sql.Append("SELECT nzp_prm, prm_value ");
                    sql.Append("FROM rep_user_reports_params ");
                    sql.Append("WHERE nzp_rep = " + report.nzp + ";");
                    foreach (DataRow r in ClassDBUtils.OpenSQL(sql.ToString(), conn_web).GetData().Rows)
                    {
                        int nzp_prm = 0;
                        string prm_value = "";
                        nzp_prm = r["nzp_prm"] == DBNull.Value ? 0 : Convert.ToInt32((r["nzp_prm"]));
                        prm_value = r["prm_value"] == DBNull.Value ? "" : r["prm_value"].ToString().Trim();
                        if (nzp_prm != 0)
                            report.selectedValues.Add(nzp_prm, prm_value);
                    }

                    report.ftemplatePath = templatePath;
                    report.exportPath = exportPath;
                    report.reportFinder.connKernelString = conn_db.ConnectionString;
                    report.reportFinder.connWebString = connWeb;

                    report.reportFinder.pref = Points.Pref;
                    //экспорт параметров программы Комплат
                    Returns ret = Utils.InitReturns();
                    report.reportFinder.pointList = Points.PointList;
                    report.reportFinder.calcMonth = Points.CalcMonth.month_;
                    report.reportFinder.calcYear = Points.CalcMonth.year_;
                    res.Add(report);
                }

                //обновляем его статус в БД
                if (res.Count > 0)
                {
                    sql.Remove(0, sql.Length);
                    sql.Append("Update rep_user_reports set status = 1 Where nzp_rep in (");
                    List<string> ids = new List<string>();
                    foreach (ReportParams rep in res)
                    {
                        ids.Add(rep.id.ToString());
                    }
                    sql.Append(string.Join(",", ids.ToArray()) + ')');
                    ClassDBUtils.ExecSQL(sql.ToString(), conn_web);
                }

                return new ReturnsObjectType<List<ReportParams>>(res);
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("ReportServerSource", "Ошибка в получении списка отчетов из базы: " + ex.Message, EventLogEntryType.Error);
                return new ReturnsObjectType<List<ReportParams>>(res);
            }
            finally
            {
                if (conn_web != null)
                    conn_web.Close();
            }
        }

        /// <summary>
        /// процедура обновления статуса отчета
        /// </summary>
        /// <param name="rep">объект отчета</param>
        /// <param name="status">статус</param>
        /// <returns>bool</returns>
        public bool UpdateReportStatus(ReportParams rep, int status, string connectionString)
        {
            IfxConnection connectionID = null;
            try
            {
                connectionID = new IfxConnection(connectionString);
                connectionID.Open();
                string sqlText = "Update rep_user_reports set status = " + status + " Where nzp_rep = " + rep.id;
                ClassDBUtils.ExecSQL(sqlText, connectionID);
                return true;
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("ReportServerSource", "Ошибка при обновлении статуса отчета: " + rep.name + "(id:" + rep.id + ") " + ex.Message, EventLogEntryType.Error);
                return false;
            }
            finally
            {
                if (connectionID != null)
                    connectionID.Close();
            }
        }

        /// <summary>
        /// процедура обновления статуса отчета в действующей таблице отчетов
        /// </summary>
        /// <param name="rep">объект отчета</param>
        /// <param name="status">статус</param>
        /// <returns>bool</returns>
        public bool UpdateReportStatusOldTable(ReportParams rep, int status, string connectionString, bool start)
        {
            IfxConnection connectionID = null;
            try
            {
                _DBReport dbr = new _DBReport();
                Returns ret = new Returns();
                string inDateTimeString = dbr.IfmxFormatDatetimeToTime(rep.date_in, out ret);
                string DateTimeString = dbr.IfmxFormatDatetimeToTime(DateTime.Now.ToString(), out ret);
                connectionID = new IfxConnection(connectionString);
                connectionID.Open();
                string sqlText = "";
                if (start)
                    sqlText = "update excel_utility set (stats, dat_start) =  " + 
                        " (" + status + ",\'" + DateTimeString + "\')" + " where  nzp_user =" + rep.nzp_user + " and dat_in = \'" + inDateTimeString + "\';";
                else
                    sqlText = "update excel_utility set (stats, dat_out, exc_path) = " + 
                        " (" + status + ",\'" + DateTimeString + "\', \'" + rep.fileName + "\'" + ") where  " + 
                        "nzp_user = " + rep.nzp_user + " and dat_in = \'" + inDateTimeString + "\';";
                ClassDBUtils.ExecSQL(sqlText, connectionID);
                return true;
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("ReportServerSource", "Ошибка при обновлении статуса отчета в старой таблице отчетов: " + rep.name + "(id:" + rep.id + ") " + ex.Message, EventLogEntryType.Error);
                return false;
            }
            finally
            {
                if (connectionID != null)
                    connectionID.Close();
            }
        }

        /// <summary>
        /// привести "дд.мм.гггг чч:мм" к формату "гггг-мм-дд ч:м:с"
        /// </summary>
        /// <param name="datahour"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public string IfmxFormatDatetimeToTime(string datahour, out Returns ret)
        {
            ret = new Returns(false);
            string outs = "";
            if (String.IsNullOrEmpty(datahour))
                return outs;
            datahour = datahour.Trim();
            string[] mas1 = datahour.Split(new string[] { " " }, StringSplitOptions.None);
            string dt = "";
            string hm = "";
            try
            {
                dt = mas1[0].Trim();
                hm = mas1[1].Trim();
                if (String.IsNullOrEmpty(dt) || String.IsNullOrEmpty(hm))
                    return outs;
                string[] mas2 = dt.Split(new string[] { "." }, StringSplitOptions.None);
                outs = mas2[2].Trim() + "-" + mas2[1].Trim() + "-" + mas2[0].Trim() + " " + hm;
                ret.result = true;
            }
            catch
            {
                return outs;
            }
            return outs;
        }

        /// <summary>
        /// получить список dll отчетов
        /// </summary>
        /// <returns></returns>
        public Dictionary<int, string> GetDllNames(string connectionString)
        {
            IfxConnection conn_web = null;
            try
            {
                conn_web = new IfxConnection(connectionString);
                Dictionary<int, string> res = new Dictionary<int, string>();
                StringBuilder sql = new StringBuilder();
                //полчаем все необработанные отчеты на выполнение
                sql.Append("SELECT nzp_act, dll_name ");
                sql.Append("FROM report ");
                sql.Append("WHERE dll_name IS NOT NULL;");
                conn_web.Open();
                foreach (DataRow row in ClassDBUtils.OpenSQL(sql.ToString(), conn_web).GetData().Rows)
                {
                    if (row["dll_name"].ToString().Trim() != string.Empty)
                    {
                        string dll = row["dll_name"] == DBNull.Value ? "" : Convert.ToString((row["dll_name"])).Trim();
                        int nzp_act = row["nzp_act"] == DBNull.Value ? 0 : Convert.ToInt32((row["nzp_act"]));
                        res.Add(nzp_act, dll);
                    }
                }
                return res;
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("ReportServerSource", "Ошибка в получении списка dll файлов: " + ex.Message, EventLogEntryType.Error);
                return null;
            }
            finally
            {
                if(conn_web != null)
                    conn_web.Close();
            }
        }

        /// <summary>
        /// получить список отчетов для обновления реализации
        /// </summary>
        /// <param name="idReport">идентификатор отчета</param>
        /// <param name="connectionString">строка подключения к веб базе</param>
        /// <returns></returns>
        public string CheckReportForUpdate(int idReport, string connectionString)
        {
            IfxConnection conn_web = null;
            try
            {
                conn_web = new IfxConnection(connectionString);
                string res = "";
                StringBuilder sql = new StringBuilder();
                //полчаем все необработанные отчеты на выполнение
                sql.Append("SELECT nzp_act, dll_name ");
                sql.Append("FROM rep_user_reports_updates ");
                sql.Append("WHERE status <> 0;");
                conn_web.Open();
                foreach (DataRow row in ClassDBUtils.OpenSQL(sql.ToString(), conn_web).GetData().Rows)
                {
                    res = row["dll_name"] == DBNull.Value ? "" : Convert.ToString((row["dll_name"])).Trim();
                }
                return res;
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("ReportServerSource", "Ошибка в получении dll файла для обновления отчета: " + ex.Message, EventLogEntryType.Error);
                return null;
            }
            finally
            {
                if (conn_web != null)
                    conn_web.Close();
            }
        }

        /// <summary>
        /// процедура обновления статуса записи об обновлении отчета
        /// </summary>
        /// <param name="rep">объект отчета</param>
        /// <param name="status">статус</param>
        /// <param name="connectionString">строка подключения к веб базе</param>
        /// <returns>bool</returns>
        public bool UpdateReportUpdateStatus(ReportParams rep, int status, string connectionString)
        {
            IfxConnection connectionID = null;
            try
            {
                connectionID = new IfxConnection(connectionString);
                connectionID.Open();
                string sqlText = "Update rep_user_reports set status = " + status + " Where nzp_rep = " + rep.id;
                ClassDBUtils.ExecSQL(sqlText, connectionID);
                return true;
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("ReportServerSource", "Ошибка при обновлении статуса отчета: " + rep.name + "(id:" + rep.id + ") " + ex.Message, EventLogEntryType.Error);
                return false;
            }
            finally
            {
                if (connectionID != null)
                    connectionID.Close();
            }
        }

        #region для получения префиксов БД

        /// <summary>
        /// получение префикса БД
        /// </summary>
        /// <param name="kernel"></param>
        /// <returns></returns>
        public string IfmxGetPref(string kernel)
        {
            if (kernel == null) return "";
            int k, l;
            k = kernel.LastIndexOf("_kernel");
            if ((k - 9) > 0)
            {
                string s;
                k = kernel.LastIndexOf("_kernel");
                s = kernel.Substring(k - 9, 9);
                l = s.Length;
                k = s.LastIndexOf("=");
                return (s.Substring(k + 1, l - k - 1)).Trim();
            }
            else
            {
                return (kernel.Substring(0, k)).Trim();
            }
        }

        public bool PointLoad(string sSource, out Returns ret)
        {
            #region Признак, что работать надо только с центральным банком данных и не трогать локальные
            GlobalSettings.WorkOnlyWithCentralBank = false;

            Setup finder = new Setup();
            finder.nzp_param = WebSetups.WorkOnlyWithCentralBank;

            DbAdmin dbAdmin = new DbAdmin();
            Setup setup = dbAdmin.GetSetup(finder, out ret);
            dbAdmin.Close();

            if (ret.result)
            {
                if (setup.value == "1")
                {
                    GlobalSettings.WorkOnlyWithCentralBank = true;
                }
            }
            else
            {
                return false;
            }
            #endregion

            string z = 0.ToString();
            ret = Utils.InitReturns();
            IDbConnection conn_db = null;
            MyDataReader reader = null;
            try
            {
                #region try
                Points.IsPoint = false;

                z = 1.ToString();

                if (Points.PointList == null) Points.PointList = new List<_Point>();
                else Points.PointList.Clear();

                if (Points.Servers == null) Points.Servers = new List<_Server>();
                else Points.Servers.Clear();

                z = 2.ToString();

                conn_db = GetConnection(Constants.cons_Kernel);

                z = 3.ToString();

                ret = OpenDb(conn_db, true);
                if (!ret.result) return false;

                ret = ExecSQL(conn_db, " Database " + Points.Pref + "_kernel", true);

                if (!ret.result) return false;

                z = 4.ToString();

                DbTables tables = new DbTables(DBManager.getServer(conn_db));

                //скачаем текущий расчетный месяц
                Points.pointWebData.calcMonth = GetCalcMonth(conn_db, null, out ret);
                if (!ret.result) return false;

                z = 5.ToString();

                if (Points.Pref == null) z = "6. Points.Pref is null ";
                else z = "6. Points.Pref = " + Points.Pref;

                //скачаем текущий операционный день
                if (ExecRead(conn_db, out reader,

 "Select dat_oper From " + Points.Pref + "_data:fn_curoperday", true).result)

                {
                    z = "6.1";

                    Points.isFinances = true;

                    if (reader.Read() && reader["dat_oper"] != DBNull.Value)
                    {
                        z = "6.2";

                        Points.DateOper = Convert.ToDateTime(reader["dat_oper"]);
                    }
                    reader.Close();
                }

                z = 7.ToString();

                GetCalcDates(conn_db, Points.Pref, out Points.pointWebData.beginWork, out Points.pointWebData.beginCalc, out Points.pointWebData.calcMonths, 0, out ret);
                if (!ret.result) return false;

                z = 8.ToString();

                z = 9.ToString();

                if (!TableInWebCashe(conn_db, "s_point"))
                {
                    //по умолчанию Татарстан, т.к. только в нем есть банки без s_point
                    Points.Region = Regions.Region.Tatarstan;

                    //заполнить по-умолчанию point = pref (для одиночных баз)
                    _Point zap = new _Point();
                    zap.nzp_wp = Constants.DefaultZap;
                    zap.point = "Локальный банк";
                    zap.pref = Points.Pref; ;

                    zap.nzp_server = -1;

                    GetCalcDates(conn_db, zap.pref, out zap.BeginWork, out zap.BeginCalc, out zap.CalcMonths, 0, out ret);
                    if (!ret.result) return false;

                    Points.isFinances = false; //финансы не подключены
                    Points.PointList.Add(zap);
                    Points.mainPageName = "Биллинговый центр";
                }
                else
                {
                    #region Определение региона
                    Points.Region = Regions.Region.Tatarstan;

                    Returns retRegion = ExecRead(conn_db, out reader, " select substr(bank_number,1,2) region_code from " + Points.Pref + "_kernel:s_point where nzp_wp = 1 ", true);

                    if (retRegion.result)
                    {
                        try
                        {
                            if (reader.Read())
                            {
                                if (reader["region_code"] != DBNull.Value)
                                {
                                    Points.Region = Regions.GetById(Convert.ToInt32((string)reader["region_code"]));
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            throw new Exception("Ошибка при определении кода региона " + ex.Message);
                        }
                    }
                    reader.Close();
                    #endregion

                    List<string> title;
                    if ((title = this.GetMainPageTitle()) != null && title.Count != 0)
                    {
                        Points.mainPageName = title[0];
                    }
                    else
                    {
                        Points.mainPageName = "Биллинговый центр";
                    }

                    bool b_yes_server = isTableHasColumn(conn_db, "s_point", "nzp_server");

                    if (b_yes_server && TableInWebCashe(conn_db, "servers"))
                    {
                        //подключен механизм фабрики серверов, считаем список серверов
                        if (ExecRead(conn_db, out reader,
                             " Select * From servers Order by nzp_server ", false).result)
                        {
                            try
                            {
                                while (reader.Read())
                                {
                                    _Server zap = new _Server();
                                    zap.is_valid = true;

                                    zap.conn = "";
                                    zap.ip_adr = "";
                                    zap.login = "";
                                    zap.pwd = "";
                                    zap.nserver = "";
                                    zap.ol_server = "";

                                    zap.nzp_server = (int)reader["nzp_server"];

                                    if (reader["conn"] != DBNull.Value)
                                        zap.conn = (string)reader["conn"];
                                    if (reader["ip_adr"] != DBNull.Value)
                                        zap.ip_adr = (string)reader["ip_adr"];
                                    if (reader["login"] != DBNull.Value)
                                        zap.login = (string)reader["login"];
                                    if (reader["pwd"] != DBNull.Value)
                                        zap.pwd = (string)reader["pwd"];
                                    if (reader["nserver"] != DBNull.Value)
                                        zap.nserver = (string)reader["nserver"];
                                    if (reader["ol_server"] != DBNull.Value)
                                        zap.ol_server = (string)reader["ol_server"];

                                    zap.conn = zap.conn.Trim();
                                    zap.ip_adr = zap.ip_adr.Trim();
                                    zap.login = zap.login.Trim();
                                    zap.pwd = zap.pwd.Trim();
                                    zap.nserver = zap.nserver.Trim();
                                    zap.ol_server = zap.ol_server.Trim();

                                    Points.Servers.Add(zap);
                                }
                                Points.IsFabric = true;
                            }
                            catch (Exception ex)
                            {
                                EventLog.WriteEntry(sSource, "Ошибка заполнения servers " + ex.Message, EventLogEntryType.Error);
                                Points.IsFabric = false;
                            }
                            reader.Close();
                        }
                    }

                    //заполнить список локальных банков данных
                    if (ExecRead(conn_db, out reader,
                       " Select * From s_point Order by n", false).result)
                    {
                        try
                        {
                            Returns ret2;
                            while (reader.Read())
                            {
                                _Point zap = new _Point();
                                zap.nzp_wp = (int)reader["nzp_wp"];
                                zap.flag = (int)reader["flag"];
                                zap.point = (string)reader["point"];
                                zap.pref = (string)reader["bd_kernel"];

                                zap.point = zap.point.Trim();
                                zap.pref = zap.pref.Trim();
                                zap.ol_server = "";

                                if (b_yes_server)
                                {
                                    zap.nzp_server = (int)reader["nzp_server"];
                                    if (reader["bd_old"] != DBNull.Value)
                                        zap.ol_server = (string)reader["bd_old"];
                                }
                                else
                                    zap.nzp_server = -1;

                                if (zap.nzp_wp == 1)
                                {
                                    //фин.банк
                                    Points.Point = zap;
                                }
                                else
                                {
                                    //список расчетных дат
                                    if (!GlobalSettings.WorkOnlyWithCentralBank)
                                    {
                                        GetCalcDates(conn_db, zap.pref, out zap.BeginWork, out zap.BeginCalc, out zap.CalcMonths, zap.nzp_server, out ret2);
                                    }
                                    else
                                    {
                                        // при запрете работе с локальными банками используем параметры центрального банка
                                        zap.BeginWork = Points.BeginWork;
                                        zap.BeginCalc = Points.BeginCalc;
                                        zap.CalcMonths = Points.CalcMonths;
                                        ret2 = Utils.InitReturns();
                                    }

                                    if (!ret2.result && ret2.tag >= 0)
                                    {
                                        return false;
                                    }

                                    if (ret2.result)
                                    {
                                        Points.PointList.Add(zap);
                                    }
                                    else
                                    {
                                        ret.tag = -1;
                                        ret.text += "Банк данных \"" + zap.point + "\" не доступен\n";
                                    }
                                }

                            }

                            Points.IsPoint = true;
                        }
                        catch (Exception ex)
                        {
                            Points.IsPoint = false;
                            throw new Exception("Ошибка заполнения s_point " + ex.Message);
                        }
                        reader.Close();
                    }
                }

                z = 10.ToString();

                //признак Самары
                Points.IsSmr = false;

                Returns ret3 = ExecRead(conn_db, out reader,
                " Select val_prm From " + Points.Pref + "_data:prm_5 p " +
                " Where p.nzp_prm = 2000 " +
                "   and p.is_actual <> 100 " +
                "   and p.dat_s  <= today and p.dat_po >= today "
                , true);

                if (ret3.result)
                {
                    if (reader.Read())
                    {
                        Points.IsSmr = (((string)reader["val_prm"]).Trim() == "1");
                    }
                }
                reader.Close();

                #region параметры распределения оплат
                Points.packDistributionParameters = new PackDistributionParameters();
                DbParameters dbparam = new DbParameters();
                Prm prm = new Prm()
                {
                    nzp_user = 1,
                    pref = Points.Pref,
                    prm_num = 10,
                    nzp_prm = 1131,
                    nzp = 0,
                    year_ = Points.CalcMonth.year_,
                    month_ = Points.CalcMonth.month_
                };
                Prm val;
                int id;

                //Определение стратегии распределения оплат
                if (Points.IsSmr) Points.packDistributionParameters.strategy = PackDistributionParameters.Strategies.Samara;
                else
                {
                    Points.packDistributionParameters.strategy = PackDistributionParameters.Strategies.InactiveServicesFirst;


                    val = dbparam.FindSimplePrmValue(conn_db, prm, out ret3);
                    if (ret3.result)
                    {
                        if (Int32.TryParse(val.val_prm, out id))
                        {
                            switch (id)
                            {
                                case 1: Points.packDistributionParameters.strategy = PackDistributionParameters.Strategies.InactiveServicesFirst; break;
                                case 2: Points.packDistributionParameters.strategy = PackDistributionParameters.Strategies.ActiveServicesFirst; break;
                                case 3: Points.packDistributionParameters.strategy = PackDistributionParameters.Strategies.NoPriority; break;
                            }
                        }
                    }
                }

                //Распределять пачки сразу после загрузки
                prm.nzp_prm = 1132;
                val = dbparam.FindSimplePrmValue(conn_db, prm, out ret3);
                if (ret3.result && val.val_prm == "1")
                    Points.packDistributionParameters.DistributePackImmediately = true;

                //Выполнять протоколирование процесса распределения оплат
                prm.nzp_prm = 1133;
                val = dbparam.FindSimplePrmValue(conn_db, prm, out ret3);
                if (ret3.result && val.val_prm == "1")
                    Points.packDistributionParameters.EnableLog = true;

                //Первоначальное распределение по полю
                Points.packDistributionParameters.chargeMethod = PackDistributionParameters.ChargeMethods.PositiveOutsaldo;
                prm.nzp_prm = 1134;
                val = dbparam.FindSimplePrmValue(conn_db, prm, out ret3);
                if (ret3.result)
                {
                    if (Int32.TryParse(val.val_prm, out id))
                    {
                        switch (id)
                        {
                            case 1: Points.packDistributionParameters.chargeMethod = PackDistributionParameters.ChargeMethods.Outsaldo; break;
                            case 2: Points.packDistributionParameters.chargeMethod = PackDistributionParameters.ChargeMethods.PositiveOutsaldo; break;
                            case 3: Points.packDistributionParameters.chargeMethod = PackDistributionParameters.ChargeMethods.MonthlyCalculationWithChangesAndOverpayment; break;
                            case 4: Points.packDistributionParameters.chargeMethod = PackDistributionParameters.ChargeMethods.PositiveMonthlyCalculationWithChangesAndOverpayment; break;
                            case 5: Points.packDistributionParameters.chargeMethod = PackDistributionParameters.ChargeMethods.MonthlyCalculationWithChanges; break;
                            case 6: Points.packDistributionParameters.chargeMethod = PackDistributionParameters.ChargeMethods.PositiveMonthlyCalculationWithChanges; break;
                        }
                    }
                }

                //Плательщик заполняет оплату по услугам
                prm.nzp_prm = 1135;
                val = dbparam.FindSimplePrmValue(conn_db, prm, out ret3);
                if (ret3.result && val.val_prm == "1")
                    Points.packDistributionParameters.AllowSelectServicesWhilePaying = true;

                //Список приоритетных услуг
                Points.packDistributionParameters.PriorityServices = new List<Service>();

                ret3 = ExecRead(conn_db, out reader, "select p.nzp_serv, s.service, s.ordering" +
                    " from " + Points.Pref + "_kernel@" + DBManager.getServer(conn_db) + ":servpriority p, " + tables.services + " s" +
                    " where s.nzp_serv = p.nzp_serv and current between p.dat_s and p.dat_po order by ordering", true);

                if (ret3.result)
                {
                    while (reader.Read())
                    {
                        Service zap = new Service();
                        if (reader["nzp_serv"] != DBNull.Value) zap.nzp_serv = (int)reader["nzp_serv"];
                        if (reader["service"] != DBNull.Value) zap.service = ((string)reader["service"]).Trim();
                        if (reader["ordering"] != DBNull.Value) zap.ordering = (int)reader["ordering"];

                        Points.packDistributionParameters.PriorityServices.Add(zap);
                    }
                    reader.Close();
                }
                #endregion

                #region Признак, сохранять ли показания ПУ прямо в основной банк
                Prm prm2 = new Prm()
                {
                    nzp_user = 1,
                    pref = Points.Pref,
                    prm_num = 5,
                    nzp_prm = 1993,
                    nzp = 0,
                    year_ = Points.CalcMonth.year_,
                    month_ = Points.CalcMonth.month_
                };
                val = dbparam.FindSimplePrmValue(conn_db, prm2, out ret3);
                if (ret3.result && val.val_prm == "1")
                    Points.SaveCounterReadingsToRealBank = true;
                else
                    Points.SaveCounterReadingsToRealBank = false;
                #endregion

                //признак Demo
                Points.IsDemo = false;

                ret3 = ExecRead(conn_db, out reader,
                " Select val_prm From " + Points.Pref + "_data:prm_5 p " +
                " Where p.nzp_prm = 1999 " +
                "   and p.is_actual <> 100 " +
                "   and p.dat_s  <= today and p.dat_po >= today "
                , true);

                if (ret3.result)
                {
                    if (reader.Read())
                    {
                        Points.IsDemo = (((string)reader["val_prm"]).Trim() == "1");
                    }
                    reader.Close();
                }
                z = 11.ToString();

                //признак Is50!!!
                Points.Is50 = false;

                ret3 = ExecRead(conn_db, out reader,
                " Select val_prm From " + Points.Pref + "_data:prm_5 p " +
                " Where p.nzp_prm = 1997 " +
                "   and p.is_actual <> 100 " +
                "   and p.dat_s  <= today and p.dat_po >= today "
                , true);

                if (ret3.result)
                {
                    if (reader.Read())
                    {
                        Points.Is50 = (((string)reader["val_prm"]).Trim() == "1");
                    }

                    reader.Close();
                }

                z = 12.ToString();

                //снять зависшие фоновые задания при рестарте хоста
                IDbConnection conn_web = GetConnection(Constants.cons_Webdata);

                z = 13.ToString();

                ret3 = OpenDb(conn_web, true);
                if (ret3.result)
                {

                    ExecSQL(conn_web, " Update saldo_fon Set kod_info = 3, txt = 'повторно!' Where kod_info = 0 ", false);
                }

                //удалить временные образы __anlXX
                DbAnalizClient dba = new DbAnalizClient();
                dba.Drop__AnlTables(conn_web);
                dba.Close();

                conn_web.Close();

                z = 14.ToString();

                //признак клона
                Points.isClone = false;

                ret3 = ExecRead(conn_db, out reader,
                " Select dbname From " + Points.Pref + "_kernel:s_baselist " +
                " Where idtype = " + BaselistTypes.PrimaryBank.GetHashCode(), true);

                if (ret3.result)
                    if (reader.Read())
                    {
                        Points.isClone = (reader["dbname"] != DBNull.Value && ((string)reader["dbname"]).Trim() != "");
                    }
                reader.Close();
                z = 15.ToString();

                //признак IsCalcSubsidy
                Points.IsCalcSubsidy = false;

                ret3 = ExecRead(conn_db, out reader,
                " Select val_prm From " + Points.Pref + "_data:prm_5 p " +
                " Where p.nzp_prm = 1992 " +
                "   and p.is_actual <> 100 " +
                "   and p.dat_s  <= today and p.dat_po >= today "
                , true);

                if (ret3.result)
                    if (reader.Read())
                    {
                        Points.IsCalcSubsidy = (((string)reader["val_prm"]).Trim() == "1");
                    }
                z = 16.ToString();

                #region RecalcMode
                Points.RecalcMode = RecalcModes.Automatic;

                ret3 = ExecRead(conn_db, out reader,
                    " Select val_prm From " + Points.Pref + "_data:prm_5 p " +
                    " Where p.nzp_prm = 1990 " +
                    "   and p.is_actual <> 100 " +
                    "   and p.dat_s  <= today and p.dat_po >= today "
                    , true);

                if (ret3.result)
                {
                    if (reader.Read())
                    {
                        string val_prm = reader["val_prm"] != DBNull.Value ? ((string)reader["val_prm"]).Trim() : "";
                        if (val_prm == RecalcModes.Automatic.GetHashCode().ToString()) Points.RecalcMode = RecalcModes.Automatic;
                        else if (val_prm == RecalcModes.AutomaticWithCancelAbility.GetHashCode().ToString()) Points.RecalcMode = RecalcModes.AutomaticWithCancelAbility;
                        else if (val_prm == RecalcModes.Manual.GetHashCode().ToString()) Points.RecalcMode = RecalcModes.Manual;
                    }
                    reader.Close();
                    if (Constants.Trace) STCLINE.KP50.Utility.ClassLog.WriteLog("Задан режим перерасчета: " + Points.RecalcMode + "(код " + Points.RecalcMode.GetHashCode() + ")");
                }
                else
                {
                    if (Constants.Trace) STCLINE.KP50.Utility.ClassLog.WriteLog("Ошибка при определении режима перерасчета: " + ret3.text);
                }
                z = 17.ToString();
                #endregion

                #region проверить, что в таблице counters всех банков есть поле ngp_cnt
                if (GlobalSettings.WorkOnlyWithCentralBank)
                {
                    Points.IsIpuHasNgpCnt = false;
                }
                else
                {
                    Points.IsIpuHasNgpCnt = true;

                    foreach (_Point p in Points.PointList)
                    {
                        ret3 = ExecSQL(conn_db, " Select c.ngp_cnt From " + p.pref + "_data:counters c Where c.nzp_cr = 0 ", false);
                        if (!ret3.result)
                        {
                            Points.IsIpuHasNgpCnt = false;
                            break;
                        }
                    }
                }
                #endregion

                conn_db.Close();

                return true;
                #endregion
            }
            catch (Exception ex)
            {
                ret = new Returns(false, z + ": " + ex.Message);
                EventLog.WriteEntry(sSource, "Ошибка в функции DbSprav.PointLoad " + z + ": " + ex.Message, EventLogEntryType.Error);

                if (conn_db != null) conn_db.Close();
                return false;
            }
            finally
            {
                if (reader != null) reader.Close();
                if (conn_db != null) conn_db.Close();
            }
        }

        void GetCalcDates(IDbConnection conn_db2, string pref, out RecordMonth bw, out RecordMonth bc, out List<RecordMonth> cm, int nzp_server, out Returns ret)
        {
            //проверим, есть ли данная база на сервере
            ret = Utils.InitReturns();
            bw.month_ = 0;
            bw.year_ = 0;
            bc.month_ = 0;
            bc.year_ = 0;
            cm = new List<RecordMonth>();
            GetCalcDates0(conn_db2, pref, out bw, out bc, out cm, out ret);
        }

        void GetCalcDates0(IDbConnection conn_db, string pref, out RecordMonth bw, out RecordMonth bc, out List<RecordMonth> cm, out Returns ret)
        {
            ret = Utils.InitReturns();
            bw.month_ = 0;
            bw.year_ = 0;
            bc.month_ = 0;
            bc.year_ = 0;
            cm = new List<RecordMonth>();

            DateTime defaultStartDate = new DateTime(2005, 1, 1); //дата начала работы по умолчанию

            if (!TempTableInWebCashe(conn_db, pref + "_data:prm_10"))

            {
                EventLog.WriteEntry("ReportServerSource", "При загрузке банка данных таблица " + pref + "_data:prm_10 оказалась не доступна. Дата начала работы системы и дата начала перерасчетов установлены в значения по умолчанию " + defaultStartDate.ToString("dd.MM.yyyy"), EventLogEntryType.Error);
            }
            else
            {
                IDataReader reader;
                //даты начала работы системы и перерасчетов
                if (!ExecRead(conn_db, out reader,

 " Select nzp_prm,val_prm From " + pref + "_data:prm_10 " +
                    " Where nzp_prm in (82,771) and is_actual <> 100 " +
                    "   and nvl(dat_s, MDY(1,1,2001)) <= MDY(" + Points.CalcMonth.month_.ToString() + ",1," + Points.CalcMonth.year_.ToString() + ") " +
                    "   and nvl(dat_po,MDY(1,1,3000)) >= MDY(" + Points.CalcMonth.month_.ToString() + ",1," + Points.CalcMonth.year_.ToString() + ") " +
                    " Order by nzp_prm "

, true).result
                   )
                {
                    ret.text = "Ошибка чтения (" + pref + ")prm_10";
                    ret.result = false;
                    return;
                }
                try
                {
                    string val_prm;

                    while (reader.Read())
                    {
                        val_prm = (string)reader["val_prm"];
                        DateTime d = Convert.ToDateTime(val_prm.Trim());

                        if ((int)reader["nzp_prm"] == 82)
                        {
                            bw.month_ = d.Month;
                            bw.year_ = d.Year;
                        }
                        else
                        {
                            bc.month_ = d.Month;
                            bc.year_ = d.Year;

                            break;
                        }
                    }
                    reader.Close();
                }
                catch (Exception ex)
                {
                    reader.Close();
                    ret.text = "Ошибка чтения (" + pref + ")prm_10";
                    ret.result = false;
                    EventLog.WriteEntry("ReportServerSource", ex.Message, EventLogEntryType.Error);
                    return;
                }
            }

            if (bw.year_ < 1998)
            {
                //не указана дата начала работы системы
                bw.year_ = defaultStartDate.Year;
                bw.month_ = defaultStartDate.Month;
            }
            if (bc.year_ < 1998)
            {
                //не указана дата пересчетов
                bc.year_ = bw.year_;
                bc.month_ = bw.month_;
            }

            RecordMonth r;

            for (int y = Points.CalcMonth.year_; y >= bw.year_; y--)
            {
                r.year_ = y;
                r.month_ = 0;
                cm.Add(r);

                int mm = 12;
                if (y == Points.CalcMonth.year_) mm = Points.CalcMonth.month_;

                for (int m = mm; m >= 1; m--)
                {
                    r.year_ = y;
                    r.month_ = m;

                    cm.Add(r);
                }
            }
            if (cm.Count == 0)
            {
                EventLog.WriteEntry("ReportServerSource", "Список расчетных месяцев пуст для банка " + pref, EventLogEntryType.Information);
            }
        }

        /// <summary>
        /// Получить текущий расчетный месяц
        /// </summary>
        public RecordMonth GetCalcMonth(IDbConnection connection, IDbTransaction transaction, out Returns ret)
        {
            IDataReader reader;

            RecordMonth rm = new RecordMonth();
            rm.year_ = 0;
            rm.month_ = 0;

            ret = ExecRead(connection, out reader,
                " Select month_,yearr From " + Points.Pref + "_data:saldo_date " +
                " Where iscurrent = 0 ", true);

            if (!ret.result)
            {
                return rm;
            }

            if (!reader.Read())
            {
                ret.text = "Не определен текущий расчетный месяц";
                ret.result = false;
                ret.tag = -1;
                return rm;
            }

            try
            {
                rm.month_ = (int)reader["month_"];
                rm.year_ = (int)reader["yearr"];
            }
            catch
            {
                rm.month_ = 0;
                rm.year_ = 0;

                ret.text = "Ошибка определения текущего расчетного месяца";
                ret.result = false;
            }
            reader.Close();
            reader.Dispose();
            return rm;
        }

        public List<string> GetMainPageTitle()
        {
            Returns ret = new Returns();
            List<string> Spis = new List<string>();
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            //проверка на существование таблицы
            if (!TempTableInWebCashe(conn_db, "s_point"))
            {
                return Spis;
            }

            //выбрать список
            string sql = String.Format(
                " Select s.point From  {0}_kernel:s_point s where s.nzp_wp = 1", Points.Pref);

            IDataReader reader;
            if (!ExecRead(conn_db, out reader, sql, true).result)
            {
                conn_db.Close();
                return null;
            }
            try
            {
                while (reader.Read())
                {
                    if (reader["point"] != DBNull.Value)
                        Spis.Add((string)reader["point"]);
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                reader.Close();
                conn_db.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err;
                if (Constants.Viewerror)
                    err = " \n " + ex.Message;
                else
                    err = "";
                EventLog.WriteEntry("ReportServerSource", "Ошибка заполнения  " + err, EventLogEntryType.Information);
                return null;
            }

            conn_db.Close();
            return Spis;
        }

        #endregion
    }
}
