using System;
using System.Data;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System.Collections.Generic;
using globalsUtils = STCLINE.KP50.Global.Utils;

namespace Bars.KP50.DataImport.SOURCE.COMPARE
{
    
    public class AddNewData : SelectedFiles
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="table">буферная таблица</param>
        /// <param name="keyField">поле ключ</param>
        /// <param name="field">поле значение</param>
        /// <param name="keyValue">значение ключа</param>
        /// <param name="fieldValue"></param>
        /// <param name="selectedFiles"></param>
        /// <param name="conn_db"></param>
        /// <param name="transaction"></param>
        private void UpdateFileLinks(string table, 
            string keyField, string field,
            string keyValue, string fieldValue,
            List<int> selectedFiles,
            IDbConnection conn_db,
            IDbTransaction transaction)
        {
            string sql = " update " + Points.Pref + DBManager.sUploadAliasRest + table + " set " +
                keyField + " = " + keyValue +
                " where upper(trim(" + field + ")) = " + globalsUtils.EStrNull(fieldValue.Trim().ToUpper()) +
                "   and " + keyField + " is null " +
                WhereNzpFile(selectedFiles);

            ExecSQLWE(conn_db, transaction, sql, true);
        }

        /// <summary>
        /// Проверка существования параметра
        /// </summary>
        /// <param name="name_prm">Название параметра</param>
        /// <param name="bank">Банк данных</param>
        /// <param name="conn_db">соединение с БД</param>
        /// <returns></returns>
        private bool CanSaveParam(string name_prm, int prm_num, string bank, IDbConnection conn_db)
        {
            Returns ret = new Returns(true);
            name_prm = name_prm.Trim().ToUpper();

            string sql = " select count(*) from " + bank + DBManager.sKernelAliasRest + "prm_name " +
                        " where UPPER(trim(name_prm)) = " + globalsUtils.EStrNull(name_prm) + 
                        " AND prm_num = " + prm_num;
            object obj = ExecScalar(conn_db, sql, out ret, true);
            if (!ret.result) throw new Exception(ret.text);

            if (Convert.ToInt32(obj) != 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        private void AlterSequence(IDbConnection conn_db, string tableName, string serialField)
        {
            tableName = tableName.Trim().ToLower();
            serialField = serialField.Trim().ToLower();

#if !PG
            ExecSQL(con_db, "database " + Points.Pref + DBManager.sKernelAliasRest, false);
#endif
            string sql = "alter sequence " + Points.Pref + DBManager.sKernelAliasRest + tableName + "_" + serialField + "_seq restart with 1000000";
            ExecSQLWE(conn_db, sql);
        }
        
        /// <summary>
        /// добавление нового участка в базу
        /// </summary>
        /// <returns>результат</returns>
        public ReturnsType AddNewArea(UncomparedAreas finder, List<int> selectedFiles)
        {
            ReturnsType result = new ReturnsType();
            Returns ret = new Returns(true);
            IDataReader reader = null;

            finder.area = finder.area.ToUpper().Trim(); 

            using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            {
                try
                {
                    ret = OpenDb(con_db, true);
                    if (!ret.result) throw new Exception(ret.text);

                    string sql = " select count(*) from " + finder.bank + DBManager.sDataAliasRest + "s_area " +
                        " where UPPER(trim(area)) = " + globalsUtils.EStrNull(finder.area);
                    object obj = ExecScalar(con_db, sql, out ret, true);
                    if (!ret.result) throw new Exception(ret.text);

                    if (Convert.ToInt32(obj) != 0)
                    {
                        result = new ReturnsType(false, "Участок с таким наименованием уже добавлен в базу");
                    }

                    if (result.result)
                    {
                        // поиск УК в supplier
                        sql = "select max(nzp_supp) from " + finder.bank + DBManager.sKernelAliasRest + "supplier " +
                          " where upper(trim(name_supp)) = " + globalsUtils.EStrNull(finder.area);
                        obj = ExecScalar(con_db, sql, out ret, true);
                        if (!ret.result) throw new Exception(ret.text);

                        int nzp_supp = 0;
                        if (obj != DBNull.Value && Convert.ToDecimal(obj) != 0) nzp_supp = Convert.ToInt32(obj);
                        
                        string inn = "";
                        string kod_supp = "";

                        // получить данные для добавления supplier
                        if (nzp_supp <= 0)
                        {
                            sql = "select max(inn) as inn, max(id) as id from " + Points.Pref + DBManager.sUploadAliasRest + "file_area " +
                                " where upper(trim(area)) = " + globalsUtils.EStrNull(finder.area) + 
                                WhereNzpFile(selectedFiles);
                            ret = ExecRead(con_db, out reader, sql, true);
                            if (!ret.result) throw new Exception(ret.text);

                            if (reader.Read())
                            {
                                if (reader["inn"] != DBNull.Value) inn = reader["inn"].ToString().Trim();
                                if (reader["id"] != DBNull.Value) kod_supp = reader["id"].ToString().Trim();
                            }        
                        }
                        
                        using (IDbTransaction transaction = con_db.BeginTransaction())
                        {
                            try
                            {
                                // добавить УК в supplier
                                if (nzp_supp <= 0)
                                {
                                    nzp_supp = GetNextSerialInt(Points.Pref + DBManager.sKernelAliasRest, "supplier", "nzp_supp", con_db, transaction);
                                    
                                    // ... добавить в центральный банк
                                    sql = "insert into " + Points.Pref + DBManager.sKernelAliasRest + "supplier " +
                                        "(nzp_supp, name_supp, adres_supp,  kod_supp) values " + 
                                        "(" + nzp_supp + ", " + globalsUtils.EStrNull(finder.area) + "," + globalsUtils.EStrNull(inn) + "," + kod_supp + ")";
                                    ExecSQLWE(con_db, transaction, sql, true);
                                  
                                    // ... добавить в локальный банк
                                    sql = "insert into " + finder.bank + DBManager.sKernelAliasRest + "supplier (nzp_supp, name_supp, adres_supp, kod_supp) " +
                                        " select nzp_supp, name_supp, adres_supp,  kod_supp from " + Points.Pref + DBManager.sKernelAliasRest + "supplier " + 
                                        " where nzp_supp = " + nzp_supp;
                                    ExecSQLWE(con_db, transaction, sql, true);
                                }

                                // добавить УК в s_area
                                // ... в центральный банк
                                int nzp_area = GetNextSerialInt(Points.Pref + DBManager.sKernelAliasRest, "supplier", "nzp_supp", con_db, transaction); 
                                
                                sql = " insert into " + Points.Pref + DBManager.sDataAliasRest + "s_area " +
                                    " (nzp_area, area, nzp_supp) values " + 
                                    "(" + nzp_area + "," + globalsUtils.EStrNull(finder.area.Substring(0, 40)) + ", " + nzp_supp + ")";
                                ExecSQLWE(con_db, transaction, sql, true);
                                
                                // ... в локальный банк
                                sql = " insert into " + finder.bank + DBManager.sDataAliasRest + "s_area (nzp_area, area, nzp_supp) " +
                                    " select nzp_area, area, nzp_supp from " + Points.Pref + DBManager.sDataAliasRest + "s_area " + 
                                    " where nzp_area = " + nzp_area;
                                ExecSQLWE(con_db, transaction, sql, true);

                                // ccылки в файлах
                                UpdateFileLinks("file_area", "nzp_area", "area", nzp_area.ToString(), finder.area, selectedFiles, con_db, transaction);

                                transaction.Commit();
                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback();
                                throw new Exception(ex.Message);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка выполнения процедуры AddNewArea : " + ex.Message, MonitorLog.typelog.Error, true);
                    result = new ReturnsType(false, "Ошибка выполнения");
                }
                finally
                {
                    if (reader != null)
                    {
                        reader.Close();
                        reader.Dispose();
                    }
                }
            }
            
            return result;
        }

        /// <summary>
        /// добавление нового поставщика в базу
        /// </summary>
        /// <returns>результат</returns>
        public ReturnsType AddNewSupp(UncomparedSupps finder, List<int> selectedFiles)
        {
            ReturnsType result = new ReturnsType();
            Returns ret = new Returns(true);
            finder.supp = finder.supp.Trim().ToUpper();

            using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            {
                try
                {
                    ret = OpenDb(con_db, true);
                    if (!ret.result) throw new Exception(ret.text);

                    string sql = " select count(*) from " + finder.bank + DBManager.sKernelAliasRest + "supplier " +
                            " where UPPER(trim(name_supp)) = " + globalsUtils.EStrNull(finder.supp);
                    object obj = ExecScalar(con_db, sql, out ret, true);
                    if (!ret.result) throw new Exception(ret.text);

                    if (Convert.ToInt32(obj) != 0)
                    {
                        result = new ReturnsType(false, "Поставщик с таким наименованием уже добавлен в базу");
                    }
                    
                    if (result.result)
                    {
                        using (IDbTransaction transaction = con_db.BeginTransaction())
                        {
                            try
                            {
                                // добавить поставщика 
                                // ... в центральный банк
                                int nzp_supp = GetNextSerialInt(Points.Pref + DBManager.sKernelAliasRest, "supplier", "nzp_supp", con_db, transaction);

                                sql = " INSERT INTO " + Points.Pref + DBManager.sKernelAliasRest + "supplier (nzp_supp, name_supp) " +
                                    " VALUES (" + nzp_supp + "," + globalsUtils.EStrNull(finder.supp) + ")";
                                ExecSQLWE(con_db, transaction, sql);

                                // ... в локальный банк
                                sql = " insert into " + finder.bank + DBManager.sKernelAliasRest + "supplier (nzp_supp, name_supp) " +
                                    " select nzp_supp, name_supp from " + Points.Pref + DBManager.sKernelAliasRest + "supplier " +
                                    " where nzp_supp = " + nzp_supp;
                                ExecSQLWE(con_db, transaction, sql, true);

                                // обновить ссылки в файлах
                                UpdateFileLinks("file_supp", "nzp_supp", "supp_name", nzp_supp.ToString(), finder.supp, selectedFiles, con_db, transaction);

                                transaction.Commit();
                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback();
                                throw new Exception(ex.Message);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка выполнения процедуры AddNewSupp : " + ex.Message, MonitorLog.typelog.Error, true);
                    result = new ReturnsType(false, "Ошибка выполнения");
                }
            }

            return result;
        }

        /// <summary>
        /// добавление нового МО в базу
        /// </summary>
        /// <returns>результат</returns>
        public ReturnsType AddNewMO(UncomparedVills finder, List<int> selectedFiles)
        {
            ReturnsType result = new ReturnsType();
            Returns ret = new Returns(true);
            finder.vill = finder.vill.Trim().ToUpper();

            using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            {
                try
                {
                    ret = OpenDb(con_db, true);
                    if (!ret.result) throw new Exception(ret.text);

                    string sql = " select count(*) from " + finder.bank + DBManager.sKernelAliasRest + "s_vill " + 
                        " where UPPER(trim(vill)) = " + globalsUtils.EStrNull(finder.vill);

                    object obj = ExecScalar(con_db, sql, out ret, true);
                    if (!ret.result) throw new Exception(ret.text);

                    if (Convert.ToInt32(obj) != 0)
                    {
                        result = new ReturnsType(false, "МО с таким наименованием уже добавлено в базу");
                    }
                    
                    if (result.result)
                    {
                        using (IDbTransaction transaction = con_db.BeginTransaction())
                        {
                            try
                            {
                                // добавить МО 
                                // ... в центральный банк
                                int no = GetNextSerialInt(Points.Pref + DBManager.sKernelAliasRest, "s_vill", "no", con_db, transaction);
                                decimal nzp_vill = 990140000000 + no;

                                sql = " INSERT INTO " + Points.Pref + DBManager.sKernelAliasRest + "s_vill (nzp_vill, no, vill, nzp_user, dat_when) " +
                                    " VALUES (" + nzp_vill + "," + no + "," + globalsUtils.EStrNull(finder.vill) + "," + finder.nzp_user + "," + DBManager.sCurDate + ")";
                                ExecSQLWE(con_db, transaction, sql);

                                // ... в локальный банк
                                sql = " insert into " + finder.bank + DBManager.sKernelAliasRest + "s_vill (nzp_vill, no, vill, nzp_user, dat_when) " +
                                    " select nzp_vill, no, vill, nzp_user, dat_when from " + Points.Pref + DBManager.sKernelAliasRest + "s_vill " +
                                    " where nzp_vill = " + nzp_vill;
                                ExecSQLWE(con_db, transaction, sql, true);

                                // обновить ссылки в файлах
                                UpdateFileLinks("file_mo", "nzp_vill", "vill", nzp_vill.ToString(), finder.vill, selectedFiles, con_db, transaction);

                                transaction.Commit();
                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback();
                                throw new Exception(ex.Message);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка выполнения процедуры AddNewMO : " + ex.Message, MonitorLog.typelog.Error, true);
                    result = new ReturnsType(false, "Ошибка выполнения");
                }
            }

            return result;
        }

        /// <summary>
        /// добавление новой услуги в базу
        /// </summary>
        /// <returns>результат</returns>
        public ReturnsType AddNewServ(UncomparedServs finder, List<int> selectedFiles)
        {
            ReturnsType result = new ReturnsType();
            Returns ret = new Returns(true);
            finder.serv = finder.serv.Trim().ToUpper();

            using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            {
                try
                {
                    ret = OpenDb(con_db, true);
                    if (!ret.result) throw new Exception(ret.text);

                    string sql = " select count(*) from " + Points.Pref + DBManager.sKernelAliasRest + "services " +
                        " where UPPER(trim(service)) = " + globalsUtils.EStrNull(finder.serv);

                    object obj = ExecScalar(con_db, sql, out ret, true);
                    if (!ret.result) throw new Exception(ret.text);

                    if (Convert.ToInt32(obj) != 0)
                    {
                        result = new ReturnsType(false, "Услуга с таким наименованием уже добавлена в базу");
                    }
                    
                    if (result.result)
                    {
                        int nzp_serv = GetNextSerialInt(Points.Pref + DBManager.sKernelAliasRest, "services", "nzp_serv", con_db, null);
                        if (nzp_serv < 1000000)
                        {
                            AlterSequence(con_db, "services", "nzp_serv");
                            nzp_serv = GetNextSerialInt(Points.Pref + DBManager.sKernelAliasRest, "services", "nzp_serv", con_db, null);
                        }
                        
                        using (IDbTransaction transaction = con_db.BeginTransaction())
                        {
                            try
                            {
                                // добавить услугу 
                                // ... в центральный банк
                                string service_small = finder.serv; 
                                
                                if (finder.serv.Length > 100) finder.serv = finder.serv.Substring(0, 100);
                                if (service_small.Length > 20) service_small = service_small.Substring(0, 20);
                                
                                sql = " INSERT INTO " + Points.Pref + DBManager.sKernelAliasRest + "services (nzp_serv, service, service_small, service_name) " +
                                    " VALUES (" + 
                                    nzp_serv + "," +
                                    globalsUtils.EStrNull(finder.serv) + "," +
                                    globalsUtils.EStrNull(service_small) + "," +
                                    globalsUtils.EStrNull(finder.serv) + 
                                    ")";
                                ExecSQLWE(con_db, transaction, sql);

                                // ... в локальный банк
                                sql = " insert into " + finder.bank + DBManager.sKernelAliasRest + "services (nzp_serv, service, service_small, service_name) " +
                                    " select nzp_serv, service, service_small, service_name " + 
                                    " from " + Points.Pref + DBManager.sKernelAliasRest + "services " +
                                    " where nzp_serv = " + nzp_serv;
                                ExecSQLWE(con_db, transaction, sql, true);

                                // обновить ссылки в файлах
                                UpdateFileLinks("file_services", "nzp_serv", "service2", nzp_serv.ToString(), finder.serv, selectedFiles, con_db, transaction);

                                transaction.Commit();
                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback();
                                throw new Exception(ex.Message);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка выполнения процедуры AddNewServ : " + ex.Message, MonitorLog.typelog.Error, true);
                    result = new ReturnsType(false, "Ошибка выполнения");
                }
            }

            return result;
        }

        /// <summary>
        /// добавление нового типа параметра
        /// </summary>
        /// <returns>результат</returns>
        public ReturnsType AddNewParameter(UncomparedParTypes finder, List<int> selectedFiles, string tableName, IDbConnection con_db)
        {
            ReturnsType result = new ReturnsType();
            Returns ret = new Returns(true);
            finder.name_prm = finder.name_prm.Trim().ToUpper();
            string sql = "";

            try
            {
                if (!CanSaveParam(finder.name_prm, finder.prm_num, finder.bank, con_db))
                {
                    result = new ReturnsType(false, "Параметр с таким наименованием уже добавлен в базу");
                }
                
                if (result.result)
                {
                    string prmNum = finder.prm_num.ToString();
                    string prm_name = "name";
                    string whereStr = "";

                    if (tableName.Trim().ToLower() == "file_typeparams")
                    {
                        prm_name = "prm_name";
                        whereStr = " and prm_num = " + prmNum;
                    }
                    //    sql = "select max(type_prm) from " + Points.Pref + DBManager.sUploadAliasRest + "file_typeparams" +
                    //    " where trim(upper(prm_name)) = " + globalsUtils.EStrNull(finder.name_prm) + WhereNzpFile(selectedFiles) + 
                    //    " AND ";
                        
                    //    prmNum = Convert.ToString(ExecScalar(con_db, sql, out ret, true));
                    //    if (!ret.result) throw new Exception(ret.text);
                        
                    //    if (prmNum == "0" || prmNum == "") prmNum = "1";
                    //}

                    int nzp_prm = GetNextSerialInt(Points.Pref + DBManager.sKernelAliasRest, "prm_name", "nzp_prm", con_db, null);
                    if (nzp_prm < 1000000)
                    {
                        AlterSequence(con_db, "prm_name", "nzp_prm");
                        nzp_prm = GetNextSerialInt(Points.Pref + DBManager.sKernelAliasRest, "prm_name", "nzp_prm", con_db, null);
                    }

                    using (IDbTransaction transaction = con_db.BeginTransaction())
                    {
                        try
                        {
                            // добавить параметры 
                            // ... в центральный банк
                            sql = " insert into " + Points.Pref + DBManager.sKernelAliasRest + "prm_name " +
                                " (nzp_prm, name_prm, prm_num) values " +
                                "(" + nzp_prm + "," + globalsUtils.EStrNull(finder.name_prm) + ", " + prmNum + ")";
                            ExecSQLWE(con_db, transaction, sql);

                            // ... в локальный банк
                            sql = " insert into " + finder.bank + DBManager.sKernelAliasRest + "prm_name (name_prm, nzp_prm, prm_num) " +
                                " select name_prm, nzp_prm, prm_num from " + Points.Pref + DBManager.sKernelAliasRest + "prm_name " +
                                " where nzp_prm = " + nzp_prm;
                            ExecSQLWE(con_db, transaction, sql);

                            // обновить ссылки в файлах
                            //UpdateFileLinks(tableName, "nzp_prm", prm_name, nzp_prm.ToString(), finder.name_prm, selectedFiles, con_db, transaction);

                            sql = " update " + Points.Pref + DBManager.sUploadAliasRest + tableName + " set" + 
                                " nzp_prm = " + nzp_prm.ToString() +
                                " where upper(trim(" + prm_name + ")) = " + globalsUtils.EStrNull(finder.name_prm) +
                                whereStr + "  and nzp_prm is null " +
                                WhereNzpFile(selectedFiles);
                            ExecSQLWE(con_db, transaction, sql, true);

                            transaction.Commit();
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            throw new Exception(ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры AddNewParType : " + ex.Message, MonitorLog.typelog.Error, true);
                result = new ReturnsType(false, "Ошибка выполнения");
            }

            return result;
        }

        /// <summary>
        /// добавление нового типа параметра
        /// </summary>
        /// <returns>результат</returns>
        public ReturnsType AddNewParType(UncomparedParTypes finder, List<int> selectedFiles, IDbConnection con_db)
        {
            return AddNewParameter(finder, selectedFiles, "file_typeparams", con_db);
        }

        /// <summary>
        /// добавление нового типа параметра
        /// </summary>
        /// <returns>результат</returns>
        public ReturnsType AddNewParType(UncomparedParTypes finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType(true);

            using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            {
                try
                {
                    var t = OpenDb(con_db, true);
                    if (!t.result) throw new Exception(t.text);
                    ret = AddNewParType(finder, selectedFiles, con_db);
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка выполнения процедуры AddNewParType : " + ex.Message, MonitorLog.typelog.Error, true);
                    ret.text = "Ошибка выполнения";
                    ret.result = false;
                }
            }

            return ret;
        }

        /// <summary>
        /// добавление нового типа благоустройства в базу
        /// </summary>
        /// <returns>результат</returns>
        public ReturnsType AddNewParBlag(UncomparedParTypes finder, List<int> selectedFiles, IDbConnection con_db)
        {
            return AddNewParameter(finder, selectedFiles, "file_blag", con_db);
        }

        /// <summary>
        /// добавление нового типа благоустройства в базу
        /// </summary>
        /// <returns>результат</returns>
        public ReturnsType AddNewParBlag(UncomparedParTypes finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();

            using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            {
                try
                {
                    var t = OpenDb(con_db, true);
                    if (!t.result) throw new Exception(t.text);
                    ret = AddNewParBlag(finder, selectedFiles, con_db);
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка выполнения процедуры AddNewParBlag : " + ex.Message, MonitorLog.typelog.Error, true);
                    ret.text = "Ошибка выполнения";
                    ret.result = false;
                }
            }

            return ret;
        }

        /// <summary>
        /// добавление нового типа параметров по газу в базу
        /// </summary>
        /// <returns>результат</returns>
        public ReturnsType AddNewParGas(UncomparedParTypes finder, List<int> selectedFiles, IDbConnection con_db)
        {
            return AddNewParameter(finder, selectedFiles, "file_gaz", con_db);
        }

        /// <summary>
        /// добавление нового типа параметров по газу в базу
        /// </summary>
        /// <returns>результат</returns>
        public ReturnsType AddNewParGas(UncomparedParTypes finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType(true);

            using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            {
                try
                {
                    var t = OpenDb(con_db, true);
                    if (!t.result) throw new Exception(t.text);
                    ret = AddNewParGas(finder, selectedFiles, con_db);
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка выполнения процедуры AddNewParGas : " + ex.Message, MonitorLog.typelog.Error, true);
                    ret.text = "Ошибка выполнения";
                    ret.result = false;
                }
            }

            return ret;
        }

        /// <summary>
        /// добавление нового типа параметров по воде в базу
        /// </summary>
        /// <returns>результат</returns>
        public ReturnsType AddNewParWater(UncomparedParTypes finder, List<int> selectedFiles, IDbConnection con_db)
        {
            return AddNewParameter(finder, selectedFiles, "file_voda", con_db);
        }

        /// <summary>
        /// добавление нового типа параметров по воде в базу
        /// </summary>
        /// <returns>результат</returns>
        public ReturnsType AddNewParWater(UncomparedParTypes finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();

            using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            {
                try
                {
                    var t = OpenDb(con_db, true);
                    if (!t.result) throw new Exception(t.text);
                    ret = AddNewParWater(finder, selectedFiles, con_db);
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка выполнения процедуры AddNewParWater : " + ex.Message, MonitorLog.typelog.Error, true);
                    ret.text = "Ошибка выполнения";
                    ret.result = false;
                }
            }

            return ret;
        }

        /// <summary>
        /// добавление новой единицы измерения в базу
        /// </summary>
        /// <returns>результат</returns>
        public ReturnsType AddNewMeasure(UncomparedMeasures finder, List<int> selectedFiles)
        {
            return new ReturnsType();

            //ReturnsType result = new ReturnsType();
            //Returns ret = new Returns();
            //finder.measure = finder.measure.Trim().ToUpper();

            //using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            //{
            //    try
            //    {
            //        ret = OpenDb(con_db, true);
            //        if (!ret.result) throw new Exception(ret.text);

            //        string sql = " select count(*) from " + finder.bank + DBManager.sKernelAliasRest + "s_measure " +
            //            " where UPPER(trim(measure_long)) = " + globalsUtils.EStrNull(finder.measure);

            //        object obj = ExecScalar(con_db, sql, out ret, true);
            //        if (!ret.result) throw new Exception(ret.text);

            //        if (Convert.ToInt32(obj) != 0)
            //        {
            //            result = new ReturnsType(false, "Единица измерения с таким наименованием уже добавлена в базу");
            //        }
            //        else
            //        {
            //            using (IDbTransaction transaction = con_db.BeginTransaction())
            //            {
            //                try
            //                {
            //                    // добавить МО 
            //                    // ... в центральный банк
            //                    sql = " INSERT INTO " + Points.Pref + DBManager.sKernelAliasRest + "s_measure (measure, measure_long) " +
            //                        " VALUES (" + globalsUtils.EStrNull(finder.measure) + "," + globalsUtils.EStrNull(finder.measure) + ")";
            //                    ExecSQLWE(con_db, transaction, sql);

            //                    int nzp_measure = GetCurrentSerialInt(Points.Pref + DBManager.sKernelAliasRest, "s_measure", "nzp_measure", con_db, transaction);

            //                    // ... в локальный банк
            //                    sql = " insert into " + finder.bank + DBManager.sKernelAliasRest + "s_measure (nzp_measure, measure, measure_long) " +
            //                        " select nzp_measure, measure, measure_long from " + Points.Pref + DBManager.sKernelAliasRest + "s_measure " +
            //                        " where nzp_measure = " + nzp_measure;
            //                    ExecSQLWE(con_db, transaction, sql, true);

            //                    // обновить ссылки в файлах
            //                    UpdateFileLinks("file_measures", "nzp_measure", "measure", nzp_measure.ToString(), finder.measure, selectedFiles, con_db, transaction);

            //                    transaction.Commit();
            //                }
            //                catch (Exception ex)
            //                {
            //                    transaction.Rollback();
            //                    throw new Exception(ex.Message);
            //                }
            //            }
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        MonitorLog.WriteLog("Ошибка выполнения процедуры AddNewMeasure : " + ex.Message, MonitorLog.typelog.Error, true);
            //        result = new ReturnsType(false, "Ошибка выполнения");
            //    }
            
            //}

            //return result;
        }

        /// <summary>
        /// Проверка данных перед сохранение нас. пункта
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        private ReturnsType CheckRajonData(UncomparedRajons finder)
        {
            int nzp_town = 0;
            if (!int.TryParse(finder.nzp_town, out nzp_town))
            {
                return new ReturnsType(false, "Не удалось определить город/район");
            }

            if (nzp_town <= 0) return new ReturnsType(false, "Не задан город/район");

            return new ReturnsType(true);
        }

        /// <summary>
        /// добавление нового населенного пункта в базу
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsType AddNewRajon(UncomparedRajons finder, List<int> selectedFiles)
        {
            ReturnsType result = new ReturnsType();
            Returns ret = new Returns();
            finder.rajon = finder.rajon.Trim().ToUpper();

            result = CheckRajonData(finder);
            if (!result.result) return result;

            using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            {
                try
                {
                    ret = OpenDb(con_db, true);
                    if (!ret.result) throw new Exception(ret.text);

                    string sql = " select count(*) from " + finder.bank + DBManager.sDataAliasRest + "s_rajon " + 
                        " where UPPER(trim(rajon)) = " + globalsUtils.EStrNull(finder.rajon) + 
                        "   and nzp_town = " + finder.nzp_town;
                    object obj = ExecScalar(con_db, sql, out ret, true);
                    if (!ret.result) throw new Exception(ret.text);

                    if (Convert.ToInt32(obj) != 0) result = new ReturnsType(false, "Населенный пункт с таким наименованием в данном районе/городе уже добавлена в базу");

                    if (result.result)
                    {
                        sql = " select count(*) from " + Points.Pref + DBManager.sDataAliasRest + "s_town where nzp_town = " + finder.nzp_town;
                        obj = ExecScalar(con_db, sql, out ret, true);
                        if (!ret.result) throw new Exception(ret.text);

                        if (Convert.ToInt32(obj) == 0) result = new ReturnsType(false, "Для данного населенного пункта не задан соответствующий населенный район/город");
                    }


                    if (result.result)
                    {
                        using (IDbTransaction transaction = con_db.BeginTransaction())
                        {
                            try
                            {
                                // добавить населенный пункт 
                                // ... в центральный банк
                                int nzp_raj = GetNextSerialInt(Points.Pref + DBManager.sDataAliasRest, "s_rajon", "nzp_raj", con_db, transaction);

                                sql = " INSERT INTO " + Points.Pref + DBManager.sDataAliasRest + "s_rajon (nzp_raj, rajon, nzp_town) " +
                                    " VALUES (" + nzp_raj + "," + globalsUtils.EStrNull(finder.rajon) + "," + finder.nzp_town + ")";
                                ExecSQLWE(con_db, transaction, sql);

                                // ... в локальный банк
                                sql = " insert into " + finder.bank + DBManager.sDataAliasRest + "s_rajon (nzp_raj, rajon, nzp_town) " +
                                    " select nzp_raj, rajon, nzp_town from " + Points.Pref + DBManager.sDataAliasRest + "s_rajon " +
                                    " where nzp_raj = " + nzp_raj;
                                ExecSQLWE(con_db, transaction, sql, true);

                                // обновить ссылки в файлах
                                sql = "update " + Points.Pref + DBManager.sUploadAliasRest + "file_dom set " +
                                    " nzp_raj = " + nzp_raj +
                                    " where upper(trim(rajon)) = " + globalsUtils.EStrNull(finder.rajon) +
                                    "   and nzp_town = " + finder.nzp_town +
                                    WhereNzpFile(selectedFiles);
                                ExecSQLWE(con_db, transaction, sql);

                                transaction.Commit();
                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback();
                                throw new Exception(ex.Message);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка выполнения процедуры AddNewSupp : " + ex.Message, MonitorLog.typelog.Error, true);
                    result = new ReturnsType(false, "Ошибка выполнения");
                }
            }

            return result;
        }

        /// <summary>
        /// добавление улицы в базу
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsType AddNewStreet(UncomparedStreets finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();

            using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            {
                try
                {
                    Returns t = OpenDb(con_db, true);
                    if (!t.result) throw new Exception(t.text);
                    ret = AddNewStreet(finder, selectedFiles, con_db);
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка выполнения процедуры AddNewStreet 2 : " + ex.Message, MonitorLog.typelog.Error, true);
                    return new ReturnsType(false, " Ошибка выполнения ", -1);
                }
            }

            return ret;
        }

        /// <summary>
        /// Проверка данных перед сохранение нас. пункта
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        private ReturnsType CheckUlicaData(UncomparedStreets finder)
        {
            int nzp_raj = 0;
            if (!int.TryParse(finder.nzp_raj, out nzp_raj))
            {
                return new ReturnsType(false, "Не удалось определить населенный пункт");
            }

            if (nzp_raj <= 0) return new ReturnsType(false, "Не задан населенный пункт");

            return new ReturnsType(true);
        }

        /// <summary>
        /// добавление новой улицы в базу
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsType AddNewStreet(UncomparedStreets finder, List<int> selectedFiles, IDbConnection con_db)
        {
            Returns ret = new Returns(true);
            ReturnsType result = new ReturnsType();
            finder.ulica = finder.ulica.Trim().ToUpper();

            result = CheckUlicaData(finder);
            if (!result.result) return result;

            try
            {
                string sql = " select count(*) from " + finder.bank + DBManager.sDataAliasRest + "s_ulica " +
                    " where UPPER(trim(ulica)) = " + globalsUtils.EStrNull(finder.ulica) +
                    "   and nzp_raj = " + finder.nzp_raj;
                object obj = ExecScalar(con_db, sql, out ret, true);
                if (!ret.result) throw new Exception(ret.text);
                if (Convert.ToInt32(obj) != 0) result = new ReturnsType(false, "Улица с таким наименованием в данном населенном пункте уже добавлена в базу");

                if (result.result)
                {
                    sql = " select count(*) from " + Points.Pref + DBManager.sDataAliasRest + "s_rajon where nzp_raj = " + finder.nzp_raj;
                    obj = ExecScalar(con_db, sql, out ret, true);
                    if (!ret.result) throw new Exception(ret.text);
                    if (Convert.ToInt32(obj) == 0) result = new ReturnsType(false, "Для данной улицы не задан соответствующий населенный пункт");
                }

                if (result.result)
                {
                    using (IDbTransaction transaction = con_db.BeginTransaction())
                    {
                        try
                        {
                            // добавить улицу 
                            // ... в центральный банк
                            int nzp_ul = GetNextSerialInt(Points.Pref + DBManager.sDataAliasRest, "s_ulica", "nzp_ul", con_db, transaction);

                            sql = " INSERT INTO " + Points.Pref + DBManager.sDataAliasRest + "s_ulica (nzp_ul, ulica, nzp_raj) " +
                                " VALUES (" + nzp_ul + "," + globalsUtils.EStrNull(finder.ulica) + "," + finder.nzp_raj + ")";
                            ExecSQLWE(con_db, transaction, sql);
                            
                            // ... в локальный банк
                            sql = " insert into " + finder.bank + DBManager.sDataAliasRest + "s_ulica (nzp_ul, ulica, nzp_raj) " +
                                " select nzp_ul, ulica, nzp_raj from " + Points.Pref + DBManager.sDataAliasRest + "s_ulica " +
                                " where nzp_ul = " + nzp_ul;
                            ExecSQLWE(con_db, transaction, sql, true);

                            // обновить ссылки в файлах
                            sql = "update " + Points.Pref + DBManager.sUploadAliasRest + "file_dom set " +
                                " nzp_ul = " + nzp_ul +
                                " where upper(trim(ulica)) = " + globalsUtils.EStrNull(finder.ulica) +
                                "   and nzp_raj = " + finder.nzp_raj +
                                WhereNzpFile(selectedFiles);
                            ExecSQLWE(con_db, transaction, sql);

                            transaction.Commit();
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            throw new Exception(ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры AddNewSupp : " + ex.Message, MonitorLog.typelog.Error, true);
                result = new ReturnsType(false, "Ошибка выполнения");
            }

            return result;
        }

        /// <summary>
        /// добавление нового дома в базу
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsType AddNewHouse(UncomparedHouses finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();

            #region Запись в БД

            IDbConnection con_db = GetConnection(Constants.cons_Kernel);
            try
            {
                var t = OpenDb(con_db, true);
                if (!t.result)
                {
                    MonitorLog.WriteLog("Ошибка открытия соединения", MonitorLog.typelog.Error, true);
                    return new ReturnsType(false, " Файл не загружен, отсутствует подключение к базе данных ", -1);
                }
                ret = AddNewHouse(finder, selectedFiles, con_db);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры AddNewHouse 2 : " + ex.Message, MonitorLog.typelog.Error,
                    true);
                return new ReturnsType(false, " Ошибка выполнения ", -1);
            }                
            #endregion

            return ret;
        }

        /// <summary>
        /// Проверка данных перед сохранение нас. пункта
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        private ReturnsType CheckDomData(UncomparedHouses finder)
        {
            int nzp_ul = 0;
            if (!int.TryParse(finder.nzp_ul, out nzp_ul))
            {
                return new ReturnsType(false, "Не удалось определить улицу");
            }

            if (nzp_ul <= 0) return new ReturnsType(false, "Не задана улица");

            return new ReturnsType(true);
        }

        /// <summary>
        /// добавление нового дома в базу
        /// </summary>
        /// <param name="finder">файл</param>
        /// <returns>результат</returns>
        public ReturnsType AddNewHouse(UncomparedHouses finder, List<int> selectedFiles, IDbConnection con_db)
        {
            ReturnsType result = new ReturnsType();
            Returns ret = new Returns(true);
            IDataReader reader = null;

            result = CheckDomData(finder);
            if (!result.result) return result;

            try
            {
                var ndom = finder.dom;
                var nkor = "-";
                if (finder.dom.Split('/').Length > 1)
                {
                    ndom = "";
                    for (int i = 0; i < finder.dom.Split('/').Length - 1; i++)
                    {
                        if (i == 0) ndom += finder.dom.Split('/')[i];
                        else ndom += "/" + finder.dom.Split('/')[i];
                    }
                    nkor = finder.dom.Split('/')[finder.dom.Split('/').Length - 1];
                }
                ndom = ndom.Trim().ToUpper();

                string nkor_sql = "";
                if (nkor == "-" || nkor == "" || nkor == null) nkor_sql = "(UPPER(trim(" + DBManager.sNvlWord + "({table_alias}nkor, '-'))) = '-' or upper(trim({table_alias}nkor)) = '')";
                else nkor_sql = "UPPER(nkor) = " + globalsUtils.EStrNull(nkor.ToUpper());

                // проверка существования в базе
                string sql = " select count(*) from " + finder.bank + DBManager.sDataAliasRest + "dom " +
                    " where UPPER(ndom) = " + globalsUtils.EStrNull(ndom) +
                    "   and " + nkor_sql.Replace("{table_alias}", "") +
                    "   and nzp_ul = " + finder.nzp_ul;
                object obj = ExecScalar(con_db, sql, out ret, true);
                if (!ret.result) throw new Exception(ret.text);

                int cnt = Convert.ToInt32(obj);

                // дом есть, обновить ссылки
                if (cnt > 1)
                {
                    result = new ReturnsType(false, "Для данного дома найдено несколько домов в базе данных");
                }
                else if (cnt == 1)
                {
                    sql = " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "file_dom fd SET " +
                        " nzp_dom = (SELECT max(a.nzp_dom) FROM " + finder.bank + sDataAliasRest + "dom a " +
                        "     WHERE a.nzp_ul = fd.nzp_ul" +
                        "     AND trim(upper(a.ndom)) = " + globalsUtils.EStrNull(ndom) +
                        "     and  " + nkor_sql.Replace("{table_alias}", "a.") + " )" +
                        " WHERE fd.nzp_dom is null " +
                        "   and fd.nzp_ul is not null " +
                        "   and fd.nzp_ul = " + finder.nzp_ul +
                        "   and trim(upper(fd.ndom)) = " + globalsUtils.EStrNull(ndom) +
                        "   and  " + nkor_sql.Replace("{table_alias}", "fd.") +
                        WhereNzpFile(selectedFiles, "fd");
                    ExecSQLWE(con_db, sql);
                }
                else
                {
                    sql = " select count(*) from " + finder.bank + DBManager.sDataAliasRest + "s_ulica " +
                        " where nzp_ul = " + finder.nzp_ul;
                    obj = ExecScalar(con_db, sql, out ret, true);
                    if (!ret.result) throw new Exception(ret.text);

                    if (Convert.ToInt32(obj) == 0)
                    {
                        result = new ReturnsType(false, "Для данного дома не задана соответствующая улица", -1);
                    }
                    else
                    {
                        sql = " select s.nzp_land, s.nzp_stat, t.nzp_town, r.nzp_raj " +
                            " from " + Points.Pref + DBManager.sDataAliasRest + "s_stat s, " +
                            Points.Pref + DBManager.sDataAliasRest + "s_town t, " +
                            Points.Pref + DBManager.sDataAliasRest + "s_rajon r, " +
                            Points.Pref + DBManager.sDataAliasRest + "s_ulica u " +
                            " WHERE s.nzp_stat = t.nzp_stat " +
                            "   and t.nzp_town = r.nzp_town " +
                            "   and r.nzp_raj = u.nzp_raj " +
                            "   and u.nzp_ul = " + finder.nzp_ul;

                        ret = ExecRead(con_db, out reader, sql, true);
                        if (!ret.result) throw new Exception(ret.text);

                        int nzp_land = 0;
                        int nzp_stat = 0;
                        int nzp_town = 0;
                        int nzp_raj = 0;

                        if (reader.Read())
                        {
                            if (reader["nzp_land"] != DBNull.Value) nzp_land = Convert.ToInt32(reader["nzp_land"]);
                            if (reader["nzp_stat"] != DBNull.Value) nzp_stat = Convert.ToInt32(reader["nzp_stat"]);
                            if (reader["nzp_town"] != DBNull.Value) nzp_town = Convert.ToInt32(reader["nzp_town"]);
                            if (reader["nzp_raj"] != DBNull.Value) nzp_raj = Convert.ToInt32(reader["nzp_raj"]);
                        }

                        // вставка дома
                        // ... в центральный банк
                        int nzp_dom = GetNextSerialInt(Points.Pref + sDataAliasRest, "dom", "nzp_dom", con_db, null);

                        sql = " INSERT INTO " + Points.Pref + sDataAliasRest + "dom" +
                              " (nzp_dom, nzp_land, nzp_stat, nzp_town, nzp_raj, nzp_ul, idom, ndom, nkor, nzp_wp, pref, nzp_area) VALUES " +
                              " (" + nzp_dom + "," + nzp_land + "," + nzp_stat + "," + nzp_town + "," + nzp_raj + "," + finder.nzp_ul + "," + DataImportUtils.GetIDom(ndom) + "," +
                              globalsUtils.EStrNull(ndom) + "," + globalsUtils.EStrNull(nkor) + ", " +
                              " (SELECT max(nzp_wp) FROM " + Points.Pref + sKernelAliasRest + "s_point WHERE bd_kernel = " + globalsUtils.EStrNull(finder.bank) + "), " +
                              globalsUtils.EStrNull(finder.bank) + ", 1)";
                        ExecSQLWE(con_db, sql);

                        // ... в локальный банк
                        sql = "insert into " + finder.bank + DBManager.sDataAliasRest + "dom (nzp_dom, nzp_land, nzp_stat, nzp_town, nzp_raj, nzp_ul, idom, ndom, nkor, nzp_wp, pref, nzp_area) " +
                            " select nzp_dom, nzp_land, nzp_stat, nzp_town, nzp_raj, nzp_ul, idom, ndom, nkor, nzp_wp, pref, nzp_area " +
                            " from " + Points.Pref + DBManager.sDataAliasRest + "dom " +
                            " where nzp_dom = " + nzp_dom;
                        ExecSQLWE(con_db, sql);

                        // обновление ссылок в файлах
                        string korStr = " upper(nkor) = '" + nkor.ToUpper() + "'";
                        if (nkor == "") korStr = " (upper(nkor) = '' or nkor is null) ";

                        sql = " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "file_dom" +
                            " SET nzp_dom = " + nzp_dom +
                            " WHERE upper(ndom) = " + globalsUtils.EStrNull(ndom) +
                            "   AND " + korStr +
                            "   AND nzp_ul = " + finder.nzp_ul +
                            WhereNzpFile(selectedFiles);
                        ExecSQLWE(con_db, sql);
                    }
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры AddNewHouse : " + ex.Message, MonitorLog.typelog.Error, true);
                result = new ReturnsType(false, "Ошибка выполнения", -1);
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                    reader.Dispose();
                }
            }

            return result;
        }


        /// <summary>
        /// добавление юридического лица в базу
        /// </summary>
        /// <returns>результат</returns>
        public ReturnsType AddNewPayer(UncomparedPayer finder, List<int> selectedFiles, IDbConnection con_db)
        {
            ReturnsType result = new ReturnsType();
            Returns ret = new Returns(true);
            finder.payer = finder.payer.Trim().ToUpper();
            IDataReader reader = null;

            try
            {
                string sql = " select count(*) from " + Points.Pref + DBManager.sKernelAliasRest + "s_payer " +
                    " where UPPER(trim(payer)) = " + globalsUtils.EStrNull(finder.payer);

                object obj = ExecScalar(con_db, sql, out ret, true);
                if (!ret.result) throw new Exception(ret.text);

                if (Convert.ToInt32(obj) != 0)
                {
                    result = new ReturnsType(false, "Юридическое лицо с таким наименованием уже добавлено в базу");
                }

                if (result.result)
                {
                    using (IDbTransaction transaction = con_db.BeginTransaction())
                    {
                        try
                        {
                            sql = " SELECT distinct urlic_name_s, is_supp, inn, kpp, bik, ks " + 
                                " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_urlic" +
                                " WHERE upper(trim(urlic_name)) = " + globalsUtils.EStrNull(finder.payer.ToUpper()) +
                                " AND nzp_payer IS NULL " +
                                WhereNzpFile(selectedFiles);

                            ret = ExecRead(con_db, transaction, out reader, sql, true);
                            if (!ret.result) throw new Exception(ret.text);

                            string urlic_name_s = "";
                            string is_supp = "";
                            string inn = "";
                            string kpp = "";
                            string bik = "";
                            string ks = "";
                            int nzp_payer = 0;

                            while (reader.Read())
                            {
                                urlic_name_s = is_supp = inn = kpp = bik = ks = "";

                                if (reader["urlic_name_s"] != DBNull.Value) urlic_name_s = reader["urlic_name_s"].ToString().Trim();
                                if (reader["is_supp"] != DBNull.Value) is_supp = reader["is_supp"].ToString().Trim();
                                if (reader["inn"] != DBNull.Value) inn = reader["inn"].ToString().Trim();
                                if (reader["kpp"] != DBNull.Value) kpp = reader["kpp"].ToString().Trim();
                                if (reader["bik"] != DBNull.Value) bik = reader["bik"].ToString().Trim();
                                if (reader["ks"] != DBNull.Value) ks = reader["ks"].ToString().Trim();

                                nzp_payer = GetNextSerialInt(Points.Pref + DBManager.sKernelAliasRest, "s_payer", "nzp_payer", con_db, transaction);

                                sql = " INSERT INTO " + Points.Pref + DBManager.sKernelAliasRest + "s_payer (nzp_payer, payer, npayer, is_erc, inn, kpp, bik, ks) VALUES (" +
                                    nzp_payer + "," +
                                    globalsUtils.EStrNull(urlic_name_s) + "," + globalsUtils.EStrNull(finder.payer.ToUpper()) + "," + globalsUtils.EStrNull(is_supp) + "," + 
                                    globalsUtils.EStrNull(inn) + "," + globalsUtils.EStrNull(kpp) + "," + globalsUtils.EStrNull(bik) + "," + globalsUtils.EStrNull(ks) + ")";
                                ExecSQLWE(con_db, transaction, sql);

                                sql = " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "file_urlic SET " + 
                                    " nzp_payer = " + nzp_payer +
                                    " WHERE upper(trim(urlic_name)) = " + globalsUtils.EStrNull(finder.payer, "''") +
                                    "   AND " + DBManager.sNvlWord + "(urlic_name_s, '') = '" + urlic_name_s + "'" +
                                    "   AND is_supp = " + is_supp +
                                    "   AND " + DBManager.sNvlWord + "(inn, '') = '" + inn + "'"+
                                    "   AND " + DBManager.sNvlWord + "(kpp, '') = '" + kpp + "'" +
                                    "   AND " + DBManager.sNvlWord + "(bik, '') = '" + bik + "'" +
                                    "   AND " + DBManager.sNvlWord + "(ks,  '') = '" + ks  + "'" + 
                                    "   AND nzp_payer IS NULL " + 
                                    WhereNzpFile(selectedFiles);
                                ExecSQLWE(con_db, transaction, sql);
                            }

                            transaction.Commit();
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            throw new Exception(ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры AddNewServ : " + ex.Message, MonitorLog.typelog.Error, true);
                result = new ReturnsType(false, "Ошибка выполнения");
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                    reader.Dispose();
                }
            }

            return result;
        }

        /// <summary>
        /// добавление юридического лица в базу
        /// </summary>
        /// <returns>результат</returns>
        public ReturnsType AddNewPayer(UncomparedPayer finder, List<int> selectedFiles)
        {
            ReturnsType ret = new ReturnsType();

            using (IDbConnection con_db = GetConnection(Constants.cons_Kernel))
            {
                try
                {
                    var t = OpenDb(con_db, true);
                    if (!t.result) throw new Exception(ret.text);

                    ret = AddNewPayer(finder, selectedFiles, con_db);
                    if (!ret.result) throw new Exception(ret.text);
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка выполнения процедуры AddNewPayer : " + ex.Message, MonitorLog.typelog.Error, true);
                    ret.text = "Ошибка выполнения";
                    ret.result = false;
                }
            }

            return ret;
        }
    }
}