using System;
using System.Data;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections.Generic;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;


// ReSharper disable CheckNamespace
namespace STCLINE.KP50.DataBase
// ReSharper restore CheckNamespace
{
	public partial class DbPack
	{
		//ОПЕРАЦИИ- выгрузка банк-клиент

        /// <summary>Создает выгрузку</summary>
        /// <param name="finder">
        /// Фильтр содержит:
        /// IdUser - Идентификатор пользователя;
        /// DogovorRequisiteses - Список Договоров ЖКУ (nzp_supp);
        /// Payees - Список получателей (nzp_payer)
        /// </param>
        public Returns СreateUploading(FilterForBC finder)
        {
            Returns ret;
            string directory = InputOutput.GetOutputDir(); // директория с файлами

            #region Проверка входных параметров

            if (finder.IdUser <= 0)
            {
                ret = new Returns(false, "Пользователь не определен");
                return ret;
            }

            if (finder.DogovorRequisiteses.Count == 0 && finder.Payees.Count == 0)
            {
                ret = new Returns(false, "Для формирования выгрузки не выбраны необходимые значения. " +
                                         "Необходимо выбрать либо договоры из списка договоров, " +
                                         "либо получателей из списка получателей");
                return ret;
            }

            #endregion

            #region Открываем соединение

            IDbConnection connDb = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(connDb, true);
            if (!ret.result)
            {
                ret.text = "Ошибка при открытии соединения с БД";
                MonitorLog.WriteLog("СreateUploading : " + ret.text, MonitorLog.typelog.Error, true);
                return ret;
            }

            #endregion

			try
            {
                #region Собираем список дат, существующих fn_sended

#warning заменить. получить даты с веба
                var listDate = new List<int>();
                int beginYear = Convert.ToInt32(Points.BeginWork.year_ - 2000),
                        endYear = Convert.ToInt32(DateTime.Now.Year - 2000);
                for (var i = beginYear; i <= endYear; i++)
                {
                    string finTable = Points.Pref + "_fin_" + i + DBManager.tableDelimiter + "fn_sended";
                    if (TempTableInWebCashe(connDb, finTable))
                    {
                        listDate.Add(i);
                    }
                }

                #endregion

                string gPrefKernel = Points.Pref + DBManager.sKernelAliasRest,
                        gPrefData = Points.Pref + DBManager.sDataAliasRest;

                #region Список договоров

                string whereSupp = finder.DogovorRequisiteses
                                         .Aggregate(string.Empty, (current, item) => current + ("," + item.nzp_supp))
                                         .TrimStart(',');

                string wherePayee = finder.Payees
                         .Aggregate(string.Empty, (current, item) => current + ("," + item.nzp_payer))
                         .TrimStart(',');

                whereSupp = whereSupp != string.Empty ? " AND s.nzp_supp IN (" + whereSupp + ") " : string.Empty;
                wherePayee = wherePayee != string.Empty ? " AND b.nzp_payer IN (" + wherePayee + ") " : string.Empty;

                ExecSQL(connDb, "DROP TABLE t_bc_supplier", true);
                string sql = "CREATE TEMP TABLE t_bc_supplier( " +
                             " nzp_supp INTEGER, " +
                             " nzp_payer_pl INTEGER, " +
                             " nzp_payer_pol INTEGER, " +
                             " nzp_fd INTEGER, " +
                             " nzp_fb INTEGER) " + DBManager.sUnlogTempTable;
                ret = ExecSQL(connDb, sql, true);
                if (!ret.result)
                {
                    string error = ret.text;
                    ret.text = string.Empty;
                    throw new Exception(error);
                }

                sql = " INSERT INTO t_bc_supplier(nzp_supp, nzp_payer_pl, nzp_payer_pol, nzp_fd, nzp_fb) " +
                      " SELECT s.nzp_supp, s.nzp_payer_agent, b.nzp_payer, l.nzp_fd, l.nzp_fb " +
                      " FROM " + gPrefKernel + "supplier s INNER JOIN " + gPrefData + "fn_dogovor_bank_lnk l ON l.id = s.fn_dogovor_bank_lnk_id " +
                                                         " INNER JOIN " + gPrefData + "fn_bank b ON b.nzp_fb = l.nzp_fb " +
                                                         " INNER JOIN " + gPrefKernel + "s_payer pb ON pb.nzp_payer = b.nzp_payer_bank " +
                      " WHERE 1 = 1 " + whereSupp + wherePayee;
                ret = ExecSQL(connDb, sql, true);
                if (!ret.result)
                {
                    string error = ret.text;
                    ret.text = string.Empty;
                    throw new Exception(error);
                }

                ExecSQL(connDb, "CREATE INDEX ix_t_bc_supplier_1 ON t_bc_supplier(nzp_supp)", true);
                ExecSQL(connDb, "CREATE INDEX ix_t_bc_supplier_2 ON t_bc_supplier(nzp_payer_pl)", true);
                ExecSQL(connDb, "CREATE INDEX ix_t_bc_supplier_3 ON t_bc_supplier(nzp_payer_pol)", true);

                #endregion

                #region Получатели

                ExecSQL(connDb, "DROP TABLE t_bc_pol", true);
                sql = "CREATE TEMP TABLE t_bc_pol( " +
                         " nzp_payer INTEGER, " +
                         " pol CHARACTER(64), " +
                         " city CHARACTER(40), " +
                         " inn CHARACTER(40), " +
                         " kpp CHARACTER(9), " +
                         " rcount CHARACTER(30), " +
                         " nzp_bank_pol INTEGER, " +
                         " bank_pol CHARACTER(64), " +
                         " id_bc_type INTEGER, " +
                         " city_b CHARACTER(40), " +
                         " ks CHARACTER(20), " +
                         " bik CHARACTER(9)) " + DBManager.sUnlogTempTable;
                ret = ExecSQL(connDb, sql, true);
                if (!ret.result)
                {
                    string error = ret.text;
                    ret.text = string.Empty;
                    throw new Exception(error);
                }

                sql = " INSERT INTO t_bc_pol(nzp_payer, pol, city, inn, kpp, rcount, nzp_bank_pol, bank_pol, id_bc_type, city_b, ks, bik) " +
                      " SELECT p.nzp_payer, p.payer AS pol, p.city, p.inn, p.kpp, b.rcount, pb.nzp_payer AS nzp_bank_pol, " +
                             " pb.payer AS bank_pol, pb.id_bc_type, pb.city, pb.ks, pb.bik " +
                      " FROM " + gPrefKernel + "s_payer p INNER JOIN " + gPrefData + "fn_bank b ON b.nzp_payer = p.nzp_payer " +
                                                        " INNER JOIN " + gPrefKernel + "s_payer pb ON pb.nzp_payer = b.nzp_payer_bank " +
                      " WHERE EXISTS(SELECT 1 FROM t_bc_supplier t WHERE t.nzp_payer_pol = p.nzp_payer) ";
                ret = ExecSQL(connDb, sql, true);
                if (!ret.result)
                {
                    string error = ret.text;
                    ret.text = string.Empty;
                    throw new Exception(error);
                }

                ExecSQL(connDb, "CREATE INDEX ix_t_bc_pol_1 ON t_bc_pol(nzp_bank_pol)", true);
                ExecSQL(connDb, "CREATE INDEX ix_t_bc_pol_2 ON t_bc_pol(id_bc_type)", true);
                ExecSQL(connDb, "CREATE INDEX ix_t_bc_pol_3 ON t_bc_pol(nzp_payer)", true);

                #region Проверка роли и наличие формата у банка

                sql = " SELECT DISTINCT bank_pol  " +
                      " FROM t_bc_pol p " +
                      " WHERE NOT EXISTS(SELECT 1 " +
                                       " FROM " + gPrefKernel + "payer_types t " +
                                       " WHERE t.nzp_payer = p.nzp_bank_pol" +
                                         " AND nzp_payer_type = " + Convert.ToInt32(Payer.ContragentTypes.PayingAgent) + ") " +
                      " ORDER BY 1 ";
                DataTable dt = DBManager.ExecSQLToTable(connDb, sql);
                if (dt.Rows.Count > 0)
                {
                    string payer = dt.Rows.Cast<DataRow>()
                                          .Aggregate(string.Empty, (current, row) => current + (row["bank_pol"] != DBNull.Value
                                                                                                    ? row["bank_pol"].ToString().Trim() + ","
                                                                                                    : string.Empty))
                                          .TrimEnd(',');

                    ret.text = "У банка(-ов) не указана выполняемая роль 'Организация, осуществляющая прием платежей'.";
                    string error = "У банка(-ов) " + payer + " не указана выполняемая роль " +
                                   "'Организация, осуществляющая прием платежей'";
                    ret.result = false;
                    throw new Exception(error);
                }

                sql = " SELECT DISTINCT bank_pol  " +
                      " FROM t_bc_pol " +
                      " WHERE NOT EXISTS(SELECT 1 FROM " + gPrefKernel + "bc_types t WHERE t.id = t_bc_pol.id_bc_type) " +
                      " ORDER BY 1 ";
                dt = DBManager.ExecSQLToTable(connDb, sql);
                if (dt.Rows.Count > 0)
                {
                    string payer = dt.Rows.Cast<DataRow>()
                                          .Aggregate(string.Empty, (current, row) => current + (row["bank_pol"] != DBNull.Value
                                                                                                    ? row["bank_pol"].ToString().Trim() + ","
                                                                                                    : string.Empty))
                                          .TrimEnd(',');

                    ret.text = "У банка(-ов) не указан тип формата Банк-клиент.";
                    string error = "У банка(-ов) " + payer + " не указан тип формата Банк-клиент. ";
                    ret.result = false;
                    throw new Exception(error);
                }

                #endregion

                #endregion

                #region Плательщики

                ExecSQL(connDb, "DROP TABLE t_bc_pl", true);
                sql = "CREATE TEMP TABLE t_bc_pl( " +
                         " nzp_payer INTEGER, " +
                         " pl CHARACTER(64), " +
                         " city CHARACTER(40), " +
                         " inn CHARACTER(40), " +
                         " kpp CHARACTER(9), " +
                         " rcount CHARACTER(30), " +
                         " bank_pl CHARACTER(64), " +
                         " city_b CHARACTER(40), " +
                         " ks CHARACTER(20), " +
                         " bik CHARACTER(9)) " + DBManager.sUnlogTempTable;
                ret = ExecSQL(connDb, sql, true);
                if (!ret.result)
                {
                    string error = ret.text;
                    ret.text = string.Empty;
                    throw new Exception(error);
                }

                sql = " INSERT INTO t_bc_pl(nzp_payer, pl, city, inn, kpp, rcount, " +
                                    " bank_pl, city_b, ks, bik) " +
                      " SELECT p.nzp_payer, p.payer AS pl, p.city, p.inn, p.kpp, b.rcount, " +
                             " pb.payer AS bank_pl, pb.city, pb.ks, pb.bik " +
                      " FROM " + gPrefKernel + "s_payer p INNER JOIN " + gPrefData + "fn_bank b ON b.nzp_payer = p.nzp_payer " +
                                                        " INNER JOIN " + gPrefKernel + "s_payer pb ON pb.nzp_payer = b.nzp_payer_bank " +
                      " WHERE EXISTS(SELECT 1 FROM t_bc_supplier t WHERE t.nzp_payer_pl = p.nzp_payer) " +
                      " ORDER BY b.nzp_fb " + DBManager.Limit1;
#warning DBManager.Limit1 костыль
                ret = ExecSQL(connDb, sql, true);
                if (!ret.result)
                {
                    string error = ret.text;
                    ret.text = string.Empty;
                    throw new Exception(error);
                }

                ExecSQL(connDb, "CREATE INDEX ix_t_bc_pl_1 ON t_bc_pl(nzp_payer)", true);

                #endregion

                #region Перечисления

                ExecSQL(connDb, "DROP TABLE t_bc_transfer");
				sql = " CREATE TEMP TABLE t_bc_transfer ( " +
							 " nzp_supp INTEGER, " +
							 " nzp_serv INTEGER, " +
							 " num_pp INTEGER, " +
							 " dat_pp DATE, " +
							 " sum_send " + DBManager.sDecimalType + "(13,2), " +
							 " dat_when DATE) " + DBManager.sUnlogTempTable;
				ret = ExecSQL(connDb, sql, true);
                if (!ret.result)
                {
                    string error = ret.text;
                    ret.text = string.Empty;
                    throw new Exception(error);
                }

				foreach (var i in listDate)
				{
				    string prefFinYY = Points.Pref + "_fin_" + i + DBManager.tableDelimiter;

                    sql = " INSERT INTO t_bc_transfer(nzp_supp, nzp_serv, num_pp, dat_pp, sum_send, dat_when) " +
                          " SELECT f.nzp_supp, nzp_serv, num_pp, dat_pp, sum_send, dat_when " +
                          " FROM " + prefFinYY + "fn_sended f INNER JOIN t_bc_supplier t ON t.nzp_supp = f.nzp_supp " +
                          " WHERE id_bc_file IS NULL ";
                    ret = ExecSQL(connDb, sql, true);
                    if (!ret.result)
                    {
                        string error = ret.text;
                        ret.text = string.Empty;
                        throw new Exception(error);
                    }

                    sql = " INSERT INTO t_bc_transfer(nzp_supp, nzp_serv, num_pp, dat_pp, sum_send, dat_when) " +
                          " SELECT fn.nzp_supp, nzp_serv, num_pp, dat_pp, sum_send, dat_when " +
                          " FROM " + prefFinYY + "fn_sended fn INNER JOIN " + gPrefData + "bc_reestr_files bc ON bc.id = fn.id_bc_file " +
                                                             " INNER JOIN t_bc_supplier t ON t.nzp_supp = fn.nzp_supp " +
                          " WHERE fn.id_bc_file IS NOT NULL " +
                            " AND bc.is_treaster = 1 ";
                    ret = ExecSQL(connDb, sql, true);
                    if (!ret.result)
                    {
                        string error = ret.text;
                        ret.text = string.Empty;
                        throw new Exception(error);
                    }
				}
                ExecSQL(connDb, "CREATE INDEX ix_t_bc_transfer_1 ON t_bc_transfer(nzp_supp)", true);

				sql = " SELECT COUNT(*) FROM t_bc_transfer ";
				string counts = ExecScalar(connDb, sql, out ret, true).ToString();
                if (!ret.result)
                {
                    string error = ret.text;
                    ret.text = string.Empty;
                    throw new Exception(error);
                }

				if (string.IsNullOrEmpty(counts) || counts == "0")
				{
					ret.text = "Нет перечислений контрагенту по договору.";
					ret.tag = 1;
					ret.result = false;
                    throw new Exception(ret.text);
                }

                #endregion

				string prefData = Points.Pref + DBManager.sDataAliasRest,
						prefKernel = Points.Pref + DBManager.sKernelAliasRest;

                //формирование итоговой таблицы
                sql = " SELECT pol.id_bc_type, " +
                             " t.sum_send, " +
                             " t.dat_when, " +
                             " (CASE WHEN t.nzp_serv = 0 THEN TRIM(d.target) ELSE TRIM(serv.service) END) AS service, " +
                             " t.dat_pp, " +
                             " t.num_pp, " +
                             " d.num_dog, " +
                             " d.dat_s, " +
                             " TRIM(pl.pl) AS name_pl, " +
                             " TRIM(pl.city) AS city_pl, " +
                             " pl.inn AS inn_pl, " +
                             " pl.kpp AS kpp_pl, " +
                             " pl.rcount AS rcount_pl, " +
                             " TRIM(pl.bank_pl) AS bank_pl, " +
                             " TRIM(pl.city_b) AS city_pl_b, " +
                             " pl.ks AS ks_pl_b, " +
                             " pl.bik AS bik_pl_b, " +
                             " TRIM(pol.pol) AS name_pol, " +
                             " TRIM(pol.city) AS city_pol, " +
                             " pol.inn AS inn_pol, " +
                             " pol.kpp AS kpp_pol, " +
                             " pol.rcount AS rcount_pol, " +
                             " TRIM(pol.bank_pol) AS bank_pol, " +
                             " TRIM(pol.city_b) AS city_pol_b, " +
                             " pol.ks AS ks_pol_b, " +
                             " pol.bik AS bik_pol_b " +
                    " FROM t_bc_transfer t INNER JOIN t_bc_supplier s ON s.nzp_supp = t.nzp_supp " +
                                         " INNER JOIN t_bc_pol pol ON pol.nzp_payer = s.nzp_payer_pol " +
                                         " INNER JOIN t_bc_pl pl ON pl.nzp_payer = s.nzp_payer_pl " +
                                         " INNER JOIN " + prefData + "fn_dogovor d ON d.nzp_fd = s.nzp_fd  " +
                                         " LEFT OUTER JOIN " + prefKernel + "services serv ON serv.nzp_serv = t.nzp_serv ";
                DataTable table = DBManager.ExecSQLToTable(connDb, sql);

                IDbTransaction transaction = connDb.BeginTransaction();
                var files = new List<FilesUploadingBC>();
                try
                {
                    #region Добавляем запись в таблицу выгрузок - bc_reestr

                    string nowDate = DateTime.Now.ToString("dd.MM.yyyy HH:mm");

                    sql = " INSERT INTO " + gPrefData + "bc_reestr(date_reestr, nzp_user) " +
                          " VALUES ('" + nowDate + "', " + finder.IdUser + ") RETURNING id ";

                    var value = ExecScalar(connDb, transaction, sql, out ret, true);
                    if (!ret.result)
                    {
                        string error = ret.text;
                        ret.text = string.Empty;
                        throw new Exception(error);
                    }
                    int idUploading = value != null ? Convert.ToInt32(value) : 0;
                    if (idUploading == 0)
                    {
                        ret.result = false;
                        ret.text = "Ошибка при добавлении записи в таблицу выгрузок";
                        throw new Exception("Значение Id добавленной строчки в bc_reestr равен " + value);
                    }

                    sql = "UPDATE " + gPrefData + "bc_reestr SET num_reestr = id WHERE id = " + idUploading;
                    ret = ExecSQL(connDb, transaction, sql, true);
                    if (!ret.result)
                    {
                        string error = ret.text;
                        ret.text = string.Empty;
                        throw new Exception(error);
                    }

                    #endregion

                    #region Формирование экземпляра объекта файлов выгрузки

                    sql = " SELECT DISTINCT pol.nzp_bank_pol, pol.bank_pol, pol.id_bc_type  " +
                          " FROM t_bc_transfer t INNER JOIN t_bc_supplier s ON s.nzp_supp = t.nzp_supp " +
                                               " INNER JOIN t_bc_pol pol ON pol.nzp_payer = s.nzp_payer_pol " +
                          " WHERE pol.id_bc_type IS NOT NULL ";
                    DataTable dtBanks = DBManager.ExecSQLToTable(connDb, sql);

                    foreach (DataRow row in dtBanks.Rows)
                    {
                        int nzpBank = row["nzp_bank_pol"] != DBNull.Value ? Convert.ToInt32(row["nzp_bank_pol"]) : 0;
                        string nameBank = row["bank_pol"] != DBNull.Value ? row["bank_pol"].ToString() : string.Empty;
                        int idFormat = row["id_bc_type"] != DBNull.Value ? Convert.ToInt32(row["id_bc_type"]) : 0;

                        var format = GetFormat(finder.IdUser, idFormat, out ret);
                        if (!ret.result)
                        {
                            string error = ret.text;
                            ret.text = string.Empty;
                            throw new Exception(error);
                        }

                        files.Add(new FilesUploadingBC
                        {
                            Uploading = new UploadingBC {Id = idUploading},
                            Format = format,
                            Bank = new Bank
                            {
                                nzp_bank = nzpBank,
                                bank = nameBank
                            }
                        });
                    }

                    if (files.Count == 0)
                    {
                        ret.result = false;
                        ret.text = "У выбранных контрагентов не имеютя перечисления, либо у банка не указан формат";
                        throw new Exception(ret.text);
                    }

                    foreach (var file in files)
                    {
                        //Указываем список тегов
                        var tags = GetTags(finder.IdUser, out ret, file.Format.Id) ?? new List<TagBC>();
                        if (!ret.result)
                        {
                            string error = ret.text;
                            ret.text = string.Empty;
                            throw new Exception(error);
                        }
                        file.Format.Tags = tags;

                        CreateFileBC(file, directory);

                        #region Запись в таблицу файлов

                        using (var excelRep = new ExcelRepClient())
                        {
                            ret = excelRep.AddMyFile(new ExcelUtility
                            {
                                nzp_user = finder.IdUser,
                                status = ExcelUtility.Statuses.InProcess,
                                rep_name =
                                    "Выгрузка платёжного поручения в формате " + file.Format.Name + " банка " +
                                    file.Bank.bank
                            });
                        }
                        file.NzpExc = ret.tag;

                        #endregion

                        // Добавляем запись в таблицу файлов выгрузки
                        sql = " INSERT INTO " + prefData + "bc_reestr_files(id_bc_reestr, id_bc_type, " +
                              " id_payer_bank, file_name, nzp_exc, is_treaster) " +
                              " VALUES (" + file.Uploading.Id + "," + file.Format.Id + "," + file.Bank.nzp_bank + ", " +
                              " '" + file.FileName + "'," + file.NzpExc + "," + Convert.ToInt32(file.IsTreaster) +
                              ") RETURNING id ";
                        value = ExecScalar(connDb, transaction, sql, out ret, true);
                        if (!ret.result)
                        {
                            string error = ret.text;
                            ret.text = string.Empty;
                            throw new Exception(error);
                        }
                        int idFileUploading = value != null ? Convert.ToInt32(value) : 0;
                        if (idFileUploading == 0)
                        {
                            ret.result = false;
                            ret.text = "Ошибка при добавлении записи в таблицу файлов выгрузки";
                            throw new Exception("Значение Id добавленной строчки в bc_reestr_files равен " + value);
                        }
                        file.Id = idFileUploading;
                    }

                    #endregion

                    #region Указать ссылки на файлы в таблицу fn_sended

                    foreach (var yy in listDate)
                    {
                        string prefFinYY = Points.Pref + "_fin_" + yy + DBManager.tableDelimiter;

                        foreach (var file in files)
                        {
                            sql = " UPDATE " + prefFinYY + "fn_sended fn SET id_bc_file = " + file.Id +
                                  " FROM t_bc_supplier s INNER JOIN t_bc_pol p ON p.nzp_payer = s.nzp_payer_pol " +
                                  " WHERE s.nzp_supp = fn.nzp_supp " +
                                    " AND fn.id_bc_file IS NULL " +
                                    " AND p.nzp_bank_pol = " + file.Bank.nzp_bank +
                                    " AND p.id_bc_type = " + file.Format.Id;
                            ret = ExecSQL(connDb, transaction, sql, true);
                            if (!ret.result)
                            {
                                string error = ret.text;
                                ret.text = string.Empty;
                                throw new Exception(error);
                            }

                            sql = " UPDATE " + prefFinYY + "fn_sended fn SET id_bc_file = " + file.Id +
                                  " FROM t_bc_supplier s INNER JOIN t_bc_pol p ON p.nzp_payer = s.nzp_payer_pol " +
                                                                            " AND p.nzp_bank_pol = " + file.Bank.nzp_bank +
                                                                            " AND p.id_bc_type = " + file.Format.Id +
                                                       " INNER JOIN " + gPrefData + "bc_reestr_files bc ON bc.is_treaster = 1" +
                                  " WHERE s.nzp_supp = fn.nzp_supp " +
                                    " AND bc.id = fn.id_bc_file " +
                                    " AND fn.id_bc_file IS NOT NULL ";
                            ret = ExecSQL(connDb, transaction, sql, true);
                            if (!ret.result)
                            {
                                string error = ret.text;
                                ret.text = string.Empty;
                                throw new Exception(error);
                            }
                        }
                    }

                    #endregion

                    ToUnloadInFile(table, files, directory);

                    #region Обновление таблицы файлов

                    using (var excelRep = new ExcelRepClient())
                    {
                        foreach (var file in files)
                        {
                            string fileName = directory + file.FileName;
                            if (InputOutput.useFtp) fileName = InputOutput.SaveOutputFile(fileName);

                            excelRep.SetMyFileState(new ExcelUtility
                            {
                                nzp_exc = file.NzpExc,
                                status = ExcelUtility.Statuses.Success,
                                exc_path = fileName
                            });
                        }
                    }

                    #endregion

                    transaction.Commit();

                }
                catch (Exception ex)
                {
                    transaction.Rollback();

                    #region Обновление таблицы файлов

                    using (var excelRep = new ExcelRepClient())
                    {
                        foreach (var file in files)
                        {
                            string fileName = directory + file.FileName;
                            if (InputOutput.useFtp) fileName = InputOutput.SaveOutputFile(fileName);

                            excelRep.SetMyFileState(new ExcelUtility
                            {
                                nzp_exc = file.NzpExc,
                                status = ExcelUtility.Statuses.Failed,
                                exc_path = fileName
                            });
                        }
                    }

                    #endregion

                    throw new Exception(ex.Message);
                }
                finally
                {
                    if (transaction != null) transaction.Dispose();
                }
            }
			catch (Exception ex)
			{
				ret.result = false;
                ret.text = string.IsNullOrEmpty(ret.text) ? "Ошибка добавления новой выгрузки" : ret.text;
				MonitorLog.WriteLog("Ошибка в функции СreateUploading:\n" + ex.Message, MonitorLog.typelog.Error, true);
			}
			finally
			{
                connDb.Close();
			}
			return ret;
		}

        /// <summary>Записть данных о перечислениях в файл</summary>
        /// <param name="table">Таблица с данными о перечислениях</param>
        /// <param name="files">Экземпляр с файлами выгрузки</param>
        /// <param name="directory">Директория сохранненных файлов</param>
        private void ToUnloadInFile(DataTable table, IEnumerable<FilesUploadingBC> files, string directory)
        {
            var text = new StringBuilder();
            foreach (var file in files)
            {
                text.Clear();

                var tagHeader = file.Format.Tags.FindAll(x => x.TypeTag.Id == Convert.ToInt32(TypeTagBcEnum.Header))
                                                .OrderBy(x => x.Num);
                FillConstantField(tagHeader, text);

                var rowsTransfer = table.Select("id_bc_type = " + file.Format.Id);

                foreach (var row in rowsTransfer)
                {
                    #region Заполнение переменных

                    decimal sumSend = row["sum_send"] != DBNull.Value ? Convert.ToDecimal(row["sum_send"]) : -1m;
                    DateTime datWhen = row["dat_when"] != DBNull.Value
                        ? Convert.ToDateTime(row["dat_when"])
                        : default(DateTime);
                    string service = row["service"] != DBNull.Value ? row["service"].ToString().Trim() : string.Empty;
                    DateTime datePP = row["dat_pp"] != DBNull.Value
                        ? Convert.ToDateTime(row["dat_pp"])
                        : default(DateTime);
                    int numPP = row["num_pp"] != DBNull.Value ? Convert.ToInt32(row["num_pp"]) : -1;
                    string numDog = row["num_dog"] != DBNull.Value ? row["num_dog"].ToString().Trim() : string.Empty;
                    DateTime datS = row["dat_s"] != DBNull.Value
                        ? Convert.ToDateTime(row["dat_s"])
                        : default(DateTime);
                    string namePl = row["name_pl"] != DBNull.Value ? row["name_pl"].ToString().Trim() : string.Empty;
                    string cityPl = row["city_pl"] != DBNull.Value ? row["city_pl"].ToString().Trim() : string.Empty;
                    string innPl = row["inn_pl"] != DBNull.Value ? row["inn_pl"].ToString().Trim() : string.Empty;
                    string kppPl = row["kpp_pl"] != DBNull.Value ? row["kpp_pl"].ToString().Trim() : string.Empty;
                    string rcountPl = row["rcount_pl"] != DBNull.Value ? row["rcount_pl"].ToString().Trim() : string.Empty;
                    string bankPl = row["bank_pl"] != DBNull.Value ? row["bank_pl"].ToString().Trim() : string.Empty;
                    string cityPlB = row["city_pl_b"] != DBNull.Value ? row["city_pl_b"].ToString().Trim() : string.Empty;
                    string ksPlB = row["ks_pl_b"] != DBNull.Value ? row["ks_pl_b"].ToString().Trim() : string.Empty;
                    string bikPlB = row["bik_pl_b"] != DBNull.Value ? row["bik_pl_b"].ToString().Trim() : string.Empty;
                    string namePol = row["name_pol"] != DBNull.Value ? row["name_pol"].ToString().Trim() : string.Empty;
                    string cityPol = row["city_pol"] != DBNull.Value ? row["city_pol"].ToString().Trim() : string.Empty;
                    string innPol = row["inn_pol"] != DBNull.Value ? row["inn_pol"].ToString().Trim() : string.Empty;
                    string kppPol = row["kpp_pol"] != DBNull.Value ? row["kpp_pol"].ToString().Trim() : string.Empty;
                    string rcountPol = row["rcount_pol"] != DBNull.Value ? row["rcount_pol"].ToString().Trim() : string.Empty;
                    string bankPol = row["bank_pol"] != DBNull.Value ? row["bank_pol"].ToString().Trim() : string.Empty;
                    string cityPolB = row["city_pol_b"] != DBNull.Value ? row["city_pol_b"].ToString().Trim() : string.Empty;
                    string ksPolB = row["ks_pol_b"] != DBNull.Value ? row["ks_pol_b"].ToString().Trim() : string.Empty;
                    string bikPolB = row["bik_pol_b"] != DBNull.Value ? row["bik_pol_b"].ToString().Trim() : string.Empty;

                    var tagBody = file.Format.Tags.FindAll(x => x.TypeTag.Id == Convert.ToInt32(TypeTagBcEnum.Body))
                                        .OrderBy(x => x.Num);

                    #endregion

                    #region Заполнение тела файла выгрузки

                    foreach (var tag in tagBody)
                    {
                        switch (tag.Value.Id)
                        {
                            case 1: //SumSend - Сумма платеж
                                if (sumSend >= 0m) text.AppendLine(tag.Name + sumSend.ToString("N"));
                                else if (tag.IsShowEmpty) text.AppendLine(tag.Name);
                                break;
                            case 2: //DatWhen - Срок платежа
                                if (datWhen != default(DateTime)) text.AppendLine(tag.Name + datWhen.ToShortDateString());
                                else if (tag.IsShowEmpty) text.AppendLine(tag.Name);
                                break;
                            case 3: //Area - Наименование управляющей компании
                                if (tag.IsShowEmpty) text.AppendLine(tag.Name);
                                break;
                            case 4: //Service - Наименование услуги
                                if (service != string.Empty) text.AppendLine(tag.Name + service);
                                else if (tag.IsShowEmpty) text.AppendLine(tag.Name);
                                break;
                            case 5: //Dat_pp - Дата платёжного поручения
                                if (datePP != default(DateTime)) text.AppendLine(tag.Name + datePP.ToShortDateString());
                                else if (tag.IsShowEmpty) text.AppendLine(tag.Name);
                                break;
                            case 6: //Num_pp - Номер платёжного поручения
                                if (numPP >= 0) text.AppendLine(tag.Name + numPP);
                                else if (tag.IsShowEmpty) text.AppendLine(tag.Name);
                                break;
                            case 7: //NamePl - Наименование плательщика
                                if (namePl != string.Empty) text.AppendLine(tag.Name + namePl);
                                else if (tag.IsShowEmpty) text.AppendLine(tag.Name);
                                break;
                            case 8: //NamePl_T - Наименование плательщика(имя + город)
                            {
                                string value = namePl;
                                value += cityPl != string.Empty ? ", г. " + cityPl : string.Empty;
                                value = value.TrimStart(',', ' ');

                                if (value != string.Empty) text.AppendLine(tag.Name + value);
                                else if (tag.IsShowEmpty) text.AppendLine(tag.Name);
                                break;
                            }
                            case 9: //NamePl_INN - Наименование плательщика(ИНН + имя)
                            {
                                string value = innPl != string.Empty ? "ИНН " + innPl : string.Empty;
                                value += namePl != string.Empty ? " " + namePl : string.Empty;
                                value = value.TrimStart();

                                if (value != string.Empty) text.AppendLine(tag.Name + value);
                                else if (tag.IsShowEmpty) text.AppendLine(tag.Name);
                                break;
                            }
                            case 10: //INN_pl - ИНН плательщика
                                if (innPl != string.Empty) text.AppendLine(tag.Name + innPl);
                                else if (tag.IsShowEmpty) text.AppendLine(tag.Name);
                                break;
                            case 11: //KPP_pl - КПП плательщика
                                if (kppPl != string.Empty) text.AppendLine(tag.Name + kppPl);
                                else if (tag.IsShowEmpty) text.AppendLine(tag.Name);
                                break;
                            case 12: //NBankCountPl - Счёт плательщика
                                if (rcountPl != string.Empty) text.AppendLine(tag.Name + rcountPl);
                                else if (tag.IsShowEmpty) text.AppendLine(tag.Name);
                                break;
                            case 13: //BankNamePl - Наименование банка плательщика
                                if (bankPl != string.Empty) text.AppendLine(tag.Name + bankPl);
                                else if (tag.IsShowEmpty) text.AppendLine(tag.Name);
                                break;
                            case 14: //BankNamePl_T - Наименование банка плательщика(имя + город)
                            {
                                string value = bankPl;
                                value += cityPlB != string.Empty ? ", г. " + cityPlB : string.Empty;
                                value = value.TrimStart(',', ' ');

                                if (value != string.Empty) text.AppendLine(tag.Name + value);
                                else if (tag.IsShowEmpty) text.AppendLine(tag.Name);
                                break;
                            }
                            case 15: //Town_Pl - Город банка плательщика
                                if (cityPlB != string.Empty) text.AppendLine(tag.Name + cityPlB);
                                else if (tag.IsShowEmpty) text.AppendLine(tag.Name);
                                break;
                            case 16: //KS_BankPl - Корсчёт банка плательщика
                                if (ksPlB != string.Empty) text.AppendLine(tag.Name + ksPlB);
                                else if (tag.IsShowEmpty) text.AppendLine(tag.Name);
                                break;
                            case 17: //BIK_BankPl - БИК банка плательщика
                                if (bikPlB != string.Empty) text.AppendLine(tag.Name + bikPlB);
                                else if (tag.IsShowEmpty) text.AppendLine(tag.Name);
                                break;
                            case 18: //NamePol - Наименование получателя
                                if (namePol != string.Empty) text.AppendLine(tag.Name + namePol);
                                else if (tag.IsShowEmpty) text.AppendLine(tag.Name);
                                break;
                            case 19: //NamePol_T - Наименование получателя(имя + город)
                                {
                                    string value = namePol;
                                    value += cityPol != string.Empty ? ", г. " + cityPol : string.Empty;
                                    value = value.TrimStart(',', ' ');

                                    if (value != string.Empty) text.AppendLine(tag.Name + value);
                                    else if (tag.IsShowEmpty) text.AppendLine(tag.Name);
                                    break;
                                }
                            case 20: //NamePol_INN - Наименование получателя(ИНН + имя)
                                {
                                    string value = innPol != string.Empty ? "ИНН " + innPol : string.Empty;
                                    value += namePol != string.Empty ? " " + namePol : string.Empty;
                                    value = value.TrimStart();

                                    if (value != string.Empty) text.AppendLine(tag.Name + value);
                                    else if (tag.IsShowEmpty) text.AppendLine(tag.Name);
                                    break;
                                }
                            case 21: //INN_pol - ИНН получателя
                                if (innPol != string.Empty) text.AppendLine(tag.Name + innPol);
                                else if (tag.IsShowEmpty) text.AppendLine(tag.Name);
                                break;
                            case 22: //KPP_pol - КПП получателя
                                if (kppPol != string.Empty) text.AppendLine(tag.Name + kppPol);
                                else if (tag.IsShowEmpty) text.AppendLine(tag.Name);
                                break;
                            case 23: //NBankCountPol - Счёт получателя
                                if (rcountPol != string.Empty) text.AppendLine(tag.Name + rcountPol);
                                else if (tag.IsShowEmpty) text.AppendLine(tag.Name);
                                break;
                            case 24: //BankNamePol - Наименование банка получателя
                                if (bankPol != string.Empty) text.AppendLine(tag.Name + bankPol);
                                else if (tag.IsShowEmpty) text.AppendLine(tag.Name);
                                break;
                            case 25: //BankNamePol_T - Наименование банка получателя(имя + город)
                                {
                                    string value = bankPol;
                                    value += cityPolB != string.Empty ? ", г. " + cityPolB : string.Empty;
                                    value = value.TrimStart(',', ' ');

                                    if (value != string.Empty) text.AppendLine(tag.Name + value);
                                    else if (tag.IsShowEmpty) text.AppendLine(tag.Name);
                                    break;
                                }
                            case 26: //Town_Pol - Город получателя
                                if (cityPolB != string.Empty) text.AppendLine(tag.Name + cityPolB);
                                else if (tag.IsShowEmpty) text.AppendLine(tag.Name);
                                break;
                            case 27: //KS_BankPol - Корсчёт банка получателя
                                if (ksPolB != string.Empty) text.AppendLine(tag.Name + ksPolB);
                                else if (tag.IsShowEmpty) text.AppendLine(tag.Name);
                                break;
                            case 28: //BIK_BankPol - БИК банка получателя
                                if (bikPolB != string.Empty) text.AppendLine(tag.Name + bikPolB);
                                else if (tag.IsShowEmpty) text.AppendLine(tag.Name);
                                break;
                            case 29: //PaymentDetails - Назначение платежа
                            {
                                string value = numDog != string.Empty ? "по аген.дог. " + numDog : string.Empty;
                                value += datS != default(DateTime) ? " от " + datS.ToShortDateString() : string.Empty;
                                value += service != string.Empty ? " платежи населения за " + service : string.Empty;
                                value = value.TrimStart();

                                if (value != string.Empty) text.AppendLine(tag.Name + value);
                                else if (tag.IsShowEmpty) text.AppendLine(tag.Name);
                                break;
                            }
                            default:
                                if (tag.IsShowEmpty) text.AppendLine(tag.Name);
                                break;
                        }
                    }

                    #endregion
                }

                var tagFooter = file.Format.Tags.FindAll(x => x.TypeTag.Id == Convert.ToInt32(TypeTagBcEnum.Footer))
                                .OrderBy(x => x.Num);
                FillConstantField(tagFooter, text);

                #region Запись в файл
                var fileInfo = new FileInfo(directory + file.FileName);
                if (!fileInfo.Exists) throw new Exception("Файл созданный раннее не существует:" +
                                                directory + file.FileName);

                using (var fileStream = fileInfo.Open(FileMode.Append, FileAccess.Write))
                    using (var streamWriter = new StreamWriter(fileStream))
                        streamWriter.Write(text.ToString());

                #endregion

            }
	    }

        /// <summary>Заполнение постоянных полей</summary>
        /// <param name="tags">Теги</param>
        /// <param name="text">Текст</param>
        private void FillConstantField(IEnumerable<TagBC> tags, StringBuilder text)
	    {
            foreach (var tag in tags)
            {
                switch (tag.Value.Id)
                {
                    case 30: //TypeOper - Тип операции
                        text.AppendLine(tag.Name + "01");
                        break;
                    case 31: //DocType - Тип документа
                        text.AppendLine(tag.Name + "0");
                        break;
                    case 32: //SendType - Тип передачи
                        text.AppendLine(tag.Name + "2");
                        break;
                    case 33: //Payment - Платёжное поручение
                        text.AppendLine(tag.Name + "Платежное поручение");
                        break;
                    case 34: //PaymentType - Вид платежа
                        text.AppendLine(tag.Name + "Электронно");
                        break;
                    case 35: //Date_cd - Дата создания файла
                        text.AppendLine(tag.Name + DateTime.Now.ToShortDateString());
                        break;
                    case 36: //Time_cd - Время создания файла
                        text.AppendLine(tag.Name + DateTime.Now.ToString("HH:mm"));
                        break;
                    default:
                        if (tag.IsShowEmpty) text.AppendLine(tag.Name);
                        break;
                }
            }
	    }

	    /// <summary>Создает файл выгрузки</summary>
	    /// <param name="file">Экземпрял файла Банк-клиент</param>
	    /// <param name="directory">Директория с сохраннеными файлами</param>
	    private void CreateFileBC(FilesUploadingBC file, string directory)
	    {
	        string nameFormat = Path.GetInvalidFileNameChars()
                .Aggregate(file.Format.Name, (current, @char) => current.Replace(Convert.ToString(@char), string.Empty));
            string fileName = nameFormat.Trim().Replace(" ", "_") + "_" + file.Uploading.Id;
            fileName = Utility.FileUtility.GetFileName(directory, fileName) + ".txt";
            var fileStream = File.Create(directory + fileName);
            fileStream.Close();
            file.FileName = fileName;
	    }

	    /// <summary>Возвращает список перечислений контрагенту</summary>
        /// <param name="finder">Вх. параметры</param>
        /// <param name="ret">Результат функции</param>
        public List<InfoPayerBankClient> GetTransfersPayer(FilterForBC finder, out Returns ret) {

            #region Проверка входных параметров

            if (finder.IdUser <= 0)
            {
                ret = new Returns(false, "Не задан пользователь");
                return new List<InfoPayerBankClient>();
            }

            #endregion

            #region Открываем соединение

            IDbConnection connDb = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(connDb, true);
            if (!ret.result)
            {
                ret.text = "Ошибка при открытии соединения с БД";
                MonitorLog.WriteLog("GetTransfersPayer : " + ret.text, MonitorLog.typelog.Error, true);
                return new List<InfoPayerBankClient>();
            }

            #endregion

            var payers = new List<InfoPayerBankClient>();
            try
            {
                string prefKernel = Points.Pref + DBManager.sKernelAliasRest,
                        prefData = Points.Pref + DBManager.sDataAliasRest;

                string whereBanks = GetWhereBank(finder.Banks),
                       whereAgent = GetWhereAgent(finder.Agents),
                       wherePrincipal = GetWherePrincipal(finder.Principals),
                       whereSupplier = GetWhereSupplier(finder.Suppliers),
                       whereServises = GetWhereService(finder.Services);

                var listYear = GetExistsFnSended(connDb);

                #region Создание временных таблиц

                ExecSQL(connDb, "DROP TABLE t_transfers_payer");
                string sql = " CREATE TEMP TABLE t_transfers_payer(" +
                             " nzp_supp INTEGER, " +
                             " nzp_payer INTEGER, " +
                             " nzp_payer_bank INTEGER, " +
                             " sum_send " + DBManager.sDecimalType + "(14,2)) " + DBManager.sUnlogTempTable;
                ret = ExecSQL(connDb, sql);
                if (!ret.result) throw new Exception(ret.text);

                ExecSQL(connDb, "DROP TABLE t_transfers_payer_send");
                sql = " CREATE TEMP TABLE t_transfers_payer_send(" +
                      " nzp_supp INTEGER, " +
                      " sum_send " + DBManager.sDecimalType + "(14,2)) " + DBManager.sUnlogTempTable;
                ret = ExecSQL(connDb, sql);
                if (!ret.result) throw new Exception(ret.text);

                #endregion

                sql = " INSERT INTO t_transfers_payer(nzp_supp, nzp_payer, nzp_payer_bank) " +
                      " SELECT DISTINCT s.nzp_supp, fb.nzp_payer, fb.nzp_payer_bank " +
                      " FROM " + prefData + "fn_bank fb INNER JOIN " + prefData + "fn_dogovor_bank_lnk fnl ON fnl.nzp_fb = fb.nzp_fb " +
                                                      " INNER JOIN " + prefKernel + "supplier s ON s.fn_dogovor_bank_lnk_id = fnl.id " +
                      " WHERE s.nzp_supp > 0 " + whereBanks + whereAgent + wherePrincipal + whereSupplier;
                ret = ExecSQL(connDb, sql);
                if (!ret.result) throw new Exception(ret.text);

                ExecSQL(connDb, " CREATE INDEX ix_info_payers_1 ON t_transfers_payer(nzp_supp) ");
                ExecSQL(connDb, " CREATE INDEX ix_info_payers_2 ON t_transfers_payer(nzp_payer, nzp_payer_bank) ");

                #region Определение суммы перечислений

                foreach (var year in listYear)
                {
                    string finYY = Points.Pref + "_fin_" + year + DBManager.tableDelimiter;
                    sql = " INSERT INTO t_transfers_payer_send(nzp_supp, sum_send) " +
                          " SELECT fn.nzp_supp, SUM(fn.sum_send) " +
                          " FROM " + finYY + "fn_sended fn INNER JOIN t_transfers_payer t ON t.nzp_supp = fn.nzp_supp " +
                          " WHERE id_bc_file IS NULL " + whereServises +
                          " GROUP BY fn.nzp_supp ";
                    ret = ExecSQL(connDb, sql);
                    if (!ret.result) throw new Exception(ret.text);

                    sql = " INSERT INTO t_transfers_payer_send(nzp_supp, sum_send) " +
                          " SELECT fn.nzp_supp, SUM(fn.sum_send) " +
                          " FROM " + finYY + "fn_sended fn INNER JOIN " + prefData + "bc_reestr_files b ON b.id = fn.id_bc_file " +
                                                         " INNER JOIN t_transfers_payer t ON t.nzp_supp = fn.nzp_supp " +
                          " WHERE fn.id_bc_file > 0 " + whereServises +
                            " AND b.is_treaster = 1 " +
                          " GROUP BY fn.nzp_supp ";
                    ret = ExecSQL(connDb, sql);
                    if (!ret.result) throw new Exception(ret.text);
                }
                ExecSQL(connDb, " CREATE INDEX ix_info_payers_send_1 ON t_transfers_payer_send(nzp_supp) ");

                sql = " UPDATE t_transfers_payer SET sum_send = " +
                      " (SELECT SUM(i.sum_send) " +
                       " FROM t_transfers_payer_send i " +
                       " WHERE i.nzp_supp = t_transfers_payer.nzp_supp) ";
                ret = ExecSQL(connDb, sql);
                if (!ret.result) throw new Exception(ret.text);

                #endregion

                sql = " SELECT t.nzp_payer, TRIM(p.payer) AS payer, TRIM(b.payer) AS bank, SUM(sum_send) AS sum_send " +
                      " FROM t_transfers_payer t INNER JOIN " + prefKernel + "s_payer p ON p.nzp_payer = t.nzp_payer " +
                                               " INNER JOIN " + prefKernel + "s_payer b ON b.nzp_payer = t.nzp_payer_bank " +
                                               " INNER JOIN " + prefKernel + "payer_types pt ON pt.nzp_payer = b.nzp_payer " +
                      " WHERE sum_send > 0 " +
                        " AND pt.nzp_payer_type = " + Convert.ToInt32(Payer.ContragentTypes.PayingAgent) +
                      " GROUP BY 1,2,3 " +
                      " ORDER BY 2,3 ";

                MyDataReader reader;
                ret = ExecRead(connDb, out reader, sql, true);
                if (!ret.result) throw new Exception(ret.text);

                while (reader.Read())
                {
                    int nzpPayer = reader["nzp_payer"] != DBNull.Value ? Convert.ToInt32(reader["nzp_payer"]) : 0;
                    string payer = reader["payer"] != DBNull.Value ? reader["payer"].ToString().Trim() : string.Empty;
                    string bank = reader["bank"] != DBNull.Value ? reader["bank"].ToString().Trim() : string.Empty;
                    decimal sumSend = reader["sum_send"] != DBNull.Value ? Convert.ToDecimal(reader["sum_send"]) : 0m;

                    payers.Add(new InfoPayerBankClient
                    {
                        nzp_payer = nzpPayer,
                        payer = payer,
                        payer_bank = bank,
                        sum_send = sumSend
                    });
                }
                reader.Close();

                ret.tag = payers.Count;
                if (finder.Limit > 0 && finder.OffSet >= 0 && payers.Count > (finder.Limit + finder.OffSet))
                    payers = payers.GetRange(finder.OffSet, finder.Limit);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetTransfersPayer : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = "Ошибка при получении списка контрагентов с перечислениями";
                ret.result = false;
            }
            finally
            {
                if (connDb != null) connDb.Close();
            }
            return payers;
        }

        /// <summary>Возвращает список договоров с перечислениями</summary>
        /// <param name="finder">Вх. параметры</param>
        /// <param name="ret">Результат функции</param>
        public List<InfoPayerBankClient> GetDogovorsWithTransfers(FilterForBC finder, out Returns ret) {
            #region Проверка входных параметров

            if (finder.IdUser <= 0)
            {
                ret = new Returns(false, "Не задан пользователь");
                return new List<InfoPayerBankClient>();
            }

            #endregion

            #region Открываем соединение

            IDbConnection connDb = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(connDb, true);
            if (!ret.result)
            {
                ret.text = "Ошибка при открытии соединения с БД";
                MonitorLog.WriteLog("GetDogovorsWithTransfers : " + ret.text, MonitorLog.typelog.Error, true);
                return new List<InfoPayerBankClient>();
            }

            #endregion

            var dogovors = new List<InfoPayerBankClient>();
            try
            {
                string prefKernel = Points.Pref + DBManager.sKernelAliasRest,
                        prefData = Points.Pref + DBManager.sDataAliasRest;

                string whereBanks = GetWhereBank(finder.Banks),
                       whereAgent = GetWhereAgent(finder.Agents),
                       wherePrincipal = GetWherePrincipal(finder.Principals),
                       whereSupplier = GetWhereSupplier(finder.Suppliers),
                       whereServises = GetWhereService(finder.Services);

                var listYear = GetExistsFnSended(connDb);

                #region Создание временных таблиц

                ExecSQL(connDb, "DROP TABLE t_dogov_transf");
                string sql = " CREATE TEMP TABLE t_dogov_transf(" +
                             " nzp_supp INTEGER, " +
                             " supplier CHARACTER(100)," +
                             " num_dog CHARACTER(20), " +
                             " nzp_payer INTEGER, " +
                             " nzp_payer_bank INTEGER, " +
                             " sum_send " + DBManager.sDecimalType + "(14,2)) " + DBManager.sUnlogTempTable;
                ret = ExecSQL(connDb, sql);
                if (!ret.result) throw new Exception(ret.text);

                ExecSQL(connDb, "DROP TABLE t_dogov_transf_send");
                sql = " CREATE TEMP TABLE t_dogov_transf_send(" +
                      " nzp_supp INTEGER, " +
                      " sum_send " + DBManager.sDecimalType + "(14,2)) " + DBManager.sUnlogTempTable;
                ret = ExecSQL(connDb, sql);
                if (!ret.result) throw new Exception(ret.text);

                #endregion

                sql = " INSERT INTO t_dogov_transf(nzp_supp, supplier, num_dog, nzp_payer, nzp_payer_bank) " +
                      " SELECT s.nzp_supp, s.name_supp, fn.num_dog, fb.nzp_payer, fb.nzp_payer_bank " +
                      " FROM " + prefData + "fn_dogovor fn INNER JOIN " + prefData + "fn_dogovor_bank_lnk fnl ON fnl.nzp_fd = fn.nzp_fd " +
                                                         " INNER JOIN " + prefKernel + "supplier s ON s.fn_dogovor_bank_lnk_id = fnl.id " +
                                                         " INNER JOIN " + prefData + "fn_bank fb ON fb.nzp_fb = fnl.nzp_fb " +
                      " WHERE s.nzp_supp > 0 " + whereBanks + whereAgent + wherePrincipal + whereSupplier;
                ret = ExecSQL(connDb, sql);
                if (!ret.result) throw new Exception(ret.text);

                ExecSQL(connDb, "CREATE INDEX ix_t_dogov_transf_1 ON t_dogov_transf(nzp_supp, nzp_payer, nzp_payer_bank)");

                #region Определение суммы перечислений

                foreach (var year in listYear)
                {
                    string finYY = Points.Pref + "_fin_" + year + DBManager.tableDelimiter;
                    sql = " INSERT INTO t_dogov_transf_send(nzp_supp, sum_send) " +
                          " SELECT nzp_supp, SUM(sum_send) " +
                          " FROM " + finYY + "fn_sended " +
                          " WHERE id_bc_file IS NULL " +
                            " AND nzp_supp > 0 " + whereServises +
                          " GROUP BY nzp_supp ";
                    ret = ExecSQL(connDb, sql);
                    if (!ret.result) throw new Exception(ret.text);

                    sql = " INSERT INTO t_dogov_transf_send(nzp_supp, sum_send) " +
                          " SELECT nzp_supp, SUM(sum_send) " +
                          " FROM " + finYY + "fn_sended f INNER JOIN " + prefData + "bc_reestr_files b ON b.id = f.id_bc_file " +
                          " WHERE nzp_supp > 0 " + whereServises +
                            " AND b.is_treaster = 1 " +
                          " GROUP BY nzp_supp ";
                    ret = ExecSQL(connDb, sql);
                    if (!ret.result) throw new Exception(ret.text);
                }

                ExecSQL(connDb, "CREATE INDEX ix_t_dogov_transf_send_1 ON t_dogov_transf_send(nzp_supp, nzp_payer)");

                sql = " UPDATE t_dogov_transf SET sum_send = " +
                      " (SELECT SUM(i.sum_send) " +
                       " FROM t_dogov_transf_send i " +
                       " WHERE i.nzp_supp = t_dogov_transf.nzp_supp) ";
                ret = ExecSQL(connDb, sql);
                if (!ret.result) throw new Exception(ret.text);

                #endregion

                sql = " SELECT d.nzp_supp, " +
                             " TRIM(d.supplier) AS supplier, " +
                             " TRIM(d.num_dog) AS num_dog, " +
                             " TRIM(p.payer) AS payer, " +
                             " TRIM(b.payer) AS bank, " +
                             " TRIM(t.name_) AS name_type, " +
                             " SUM(sum_send) AS sum_send " +
                      " FROM t_dogov_transf d INNER JOIN " + prefKernel + "s_payer p ON p.nzp_payer = d.nzp_payer " +
                                            " INNER JOIN " + prefKernel + "s_payer b ON b.nzp_payer = d.nzp_payer_bank " +
                                            " LEFT OUTER JOIN " + prefKernel + "bc_types t ON t.id = b.id_bc_type " +
                      " GROUP BY 1,2,3,4,5,6 " +
                      " ORDER BY 2,4 ";

                MyDataReader reader;
                ret = ExecRead(connDb, out reader, sql, true);
                if (!ret.result) throw new Exception(ret.text);

                while (reader.Read())
                {
                    dogovors.Add(new InfoPayerBankClient
                    {
                        nzp_supp = reader["nzp_supp"] != DBNull.Value ? Convert.ToInt32(reader["nzp_supp"]) : 0,
                        name_supp = reader["supplier"] != DBNull.Value ? reader["supplier"].ToString().Trim() : string.Empty,
                        num_dog = reader["num_dog"] != DBNull.Value ? reader["num_dog"].ToString().Trim() : string.Empty,
                        payer = reader["payer"] != DBNull.Value ? reader["payer"].ToString().Trim() : string.Empty,
                        payer_bank = reader["bank"] != DBNull.Value ? reader["bank"].ToString().Trim() : string.Empty,
                        bc_type = reader["name_type"] != DBNull.Value ? reader["name_type"].ToString().Trim() : string.Empty,
                        sum_send = reader["sum_send"] != DBNull.Value ? Convert.ToDecimal(reader["sum_send"]) : 0m,
                    });
                }
                reader.Close();

                ret.tag = dogovors.Count;
                if (finder.Limit > 0 && finder.OffSet >= 0 && dogovors.Count > (finder.Limit + finder.OffSet))
                    dogovors = dogovors.GetRange(finder.OffSet, finder.Limit);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetDogovorsWithTransfers : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = "Ошибка при получении списка договоров ЖКУ с перечислениями";
                ret.result = false;
            }
            finally
            {
                if (connDb != null) connDb.Close();
            }
            return dogovors;
        }

        /// <summary>Возвращает фильтр по банкам для запроса</summary>
        /// <param name="banks">Список банков</param>
        private string GetWhereBank(List<Bank> banks)
        {
            if (banks == null || banks.Count == 0) return string.Empty;
            string where = banks.Aggregate(string.Empty, (current, item) => current + (item.nzp_bank + ",")).TrimEnd(',');
            return where == string.Empty ? string.Empty : " AND fb.nzp_payer_bank IN (" + where + ") ";
	    }

        /// <summary>Возвращает фильтр по агентам для запроса</summary>
        /// <param name="agents">Список банков</param>
        private string GetWhereAgent(List<Payer> agents) {
            if (agents == null || agents.Count == 0) return string.Empty;
            string where = agents.Aggregate(string.Empty, (current, item) => current + (item.nzp_payer + ",")).TrimEnd(',');
            return where == string.Empty ? string.Empty : " AND s.nzp_payer_agent IN (" + where + ") ";
        }

        /// <summary>Возвращает фильтр по принципалам для запроса</summary>
        /// <param name="principals">Список банков</param>
        private string GetWherePrincipal(List<Payer> principals) {
            if (principals == null || principals.Count == 0) return string.Empty;
            string where = principals.Aggregate(string.Empty, (current, item) => current + (item.nzp_payer + ",")).TrimEnd(',');
            return where == string.Empty ? string.Empty : " AND s.nzp_payer_princip IN (" + where + ") ";
        }

        /// <summary>Возвращает фильтр по поставщикам для запроса</summary>
        /// <param name="suppliers">Список банков</param>
        private string GetWhereSupplier(List<Payer> suppliers) {
            if (suppliers == null || suppliers.Count == 0) return string.Empty;
            string where = suppliers.Aggregate(string.Empty, (current, item) => current + (item.nzp_payer + ",")).TrimEnd(',');
            return where == string.Empty ? string.Empty : " AND s.nzp_payer_supp IN (" + where + ") ";
        }

        /// <summary>Возвращает фильтр по услугам для запроса</summary>
        /// <param name="services">Список банков</param>
        private string GetWhereService(List<_Service> services) {
            if (services == null || services.Count == 0) return string.Empty;
            string where = services.Aggregate(string.Empty, (current, item) => current + (item.nzp_serv + ",")).TrimEnd(',');
            return where == string.Empty ? string.Empty : " AND nzp_serv IN (" + where + ") ";
        }

        /// <summary>Список дат(год), в которых существует fn_sended</summary>
        /// <param name="connDb">Подключение к БД</param>
        /// <returns>Список дат(год) в формате 00</returns>
        private IEnumerable<int> GetExistsFnSended(IDbConnection connDb)
        {
            var list = new List<int>();
            for (var i = Points.BeginWork.year_; i <= DateTime.Now.Year; i++)
            {
                string finTable = Points.Pref + "_fin_" + (i - 2000).ToString("00") + DBManager.tableDelimiter + "fn_sended";
                if (TempTableInWebCashe(connDb, finTable))
                {
                    list.Add(i - 2000); // список существующих *central*_fin_YY
                }
            }
            return list;
        }

	    // Получить список контрагентов
        public List<InfoPayerBankClient> GetInfoPayers(FilterForBC finder, out Returns ret) {
            if (finder.IdUser <= 0)
            {
                ret = new Returns(false, "Не задан пользователь");
                return null;
            }

            string whereAgent = string.Empty,
                    wherePrincipal = string.Empty,
                     whereSupplier = string.Empty,
                      whereServises = string.Empty,
                       whereBanks = string.Empty;

            whereBanks = finder.Banks.Aggregate(whereBanks, (current, item) => current + (item.nzp_payer + ",")).TrimEnd(',');
            whereServises = finder.Services.Aggregate(whereServises, (current, item) => current + (item.nzp_serv + ",")).TrimEnd(',');
            whereAgent = finder.Agents.Aggregate(whereAgent, (current, item) => current + (item.nzp_payer + ",")).TrimEnd(',');
            wherePrincipal = finder.Principals.Aggregate(wherePrincipal, (current, item) => current + (item.nzp_payer + ",")).TrimEnd(',');
            whereSupplier = finder.Suppliers.Aggregate(whereSupplier, (current, item) => current + (item.nzp_payer + ",")).TrimEnd(',');

            var reader = new MyDataReader();
            var listPayers = new List<InfoPayerBankClient>();
            string prefKernel = Points.Pref + DBManager.sKernelAliasRest,
                    prefData = Points.Pref + DBManager.sDataAliasRest;

            IDbConnection connDB = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(connDB, true);
            if (!ret.result) return null;

            try
            {
                var listYear = new List<byte>();
                byte beginYear = Convert.ToByte(Points.BeginWork.year_ - 2000),
                    endYear = Convert.ToByte(DateTime.Now.Year - 2000);
                for (var i = beginYear; i <= endYear; i++)
                {
                    string finTable = Points.Pref + "_fin_" + i + DBManager.tableDelimiter + "fn_sended";
                    if (TempTableInWebCashe(connDB, finTable))
                    {
                        listYear.Add(i); // список существующих *central*_fin_YY
                    }
                }

                #region заполнение временных таблиц

                string sql = " CREATE TEMP TABLE info_payers(" +
                             " nzp_supp INTEGER, " +
                             " fn_dogovor_bank_lnk_id INTEGER, " +
                             " supplier CHARACTER(100)," +
                             " num_dog CHARACTER(20), " +
                             " nzp_payer INTEGER, " +
                             " nzp_payer_bank INTEGER, " +
                             " payer CHARACTER(40), " +
                             " payer_bank CHARACTER(40), " +
                             " nzp_bc_type INTEGER, " +
                             " bc_type CHARACTER(100), " +
                             " sum_send " + DBManager.sDecimalType + "(14,2)) " + DBManager.sUnlogTempTable;
                ret = ExecSQL(connDB, sql);
                if (!ret.result) throw new Exception();

                sql = " CREATE TEMP TABLE info_payers_send(" +
                      " nzp_supp INTEGER, " +
                      " nzp_payer INTEGER, " +
                      " nzp_serv INTEGER, " +
                      " fn_dogovor_bank_lnk_id INTEGER, " +
                      " sum_send " + DBManager.sDecimalType + "(14,2)) " + DBManager.sUnlogTempTable;
                ret = ExecSQL(connDB, sql);
                if (!ret.result) throw new Exception();

                sql = " INSERT INTO info_payers(nzp_supp, fn_dogovor_bank_lnk_id, supplier, nzp_payer, nzp_payer_bank, num_dog) " +
                      " SELECT s.nzp_supp, s.fn_dogovor_bank_lnk_id, s.name_supp, fb.nzp_payer, fb.nzp_payer_bank, fn.num_dog " +
                      " FROM " + prefKernel + "supplier s INNER JOIN " + prefData + "fn_dogovor_bank_lnk fnl ON fnl.id = s.fn_dogovor_bank_lnk_id " +
                                                        " INNER JOIN " + prefData + "fn_dogovor fn ON fn.nzp_fd = fnl.nzp_fd " +
                                                        " INNER JOIN " + prefData + "fn_bank fb ON fb.nzp_fb = fnl.nzp_fb " +
                      " WHERE s.nzp_supp > 0 " +
                      (whereBanks  != string.Empty ? " AND nzp_payer_bank IN (" + whereBanks + ")" : string.Empty) +
                      (whereAgent != string.Empty ? " AND s.nzp_payer_agent IN (" + whereAgent + ")" : string.Empty) +
                      (wherePrincipal != string.Empty ? " AND s.nzp_payer_princip IN (" + wherePrincipal + ")" : string.Empty) +
                      (whereSupplier != string.Empty ? " AND s.nzp_payer_supp IN (" + whereSupplier + ")" : string.Empty);
                ret = ExecSQL(connDB, sql);
                if (!ret.result) throw new Exception();


                sql = " UPDATE info_payers SET payer = " +
                      " (SELECT payer FROM " + prefKernel + "s_payer p WHERE p.nzp_payer = info_payers.nzp_payer) ";
                ret = ExecSQL(connDB, sql);
                if (!ret.result) throw new Exception();

                sql = " UPDATE info_payers SET payer_bank = " +
                      " (SELECT payer FROM " + prefKernel + "s_payer p WHERE p.nzp_payer = info_payers.nzp_payer_bank) ";
                ret = ExecSQL(connDB, sql);
                if (!ret.result) throw new Exception();

                sql = " UPDATE info_payers SET nzp_bc_type = " +
                      " (SELECT id_bc_type FROM " + prefKernel + "s_payer p WHERE p.nzp_payer = info_payers.nzp_payer_bank) ";
                ret = ExecSQL(connDB, sql);
                if (!ret.result) throw new Exception();

                sql = " UPDATE info_payers SET bc_type = " +
                      " (SELECT name_ FROM " + prefKernel + "bc_types t WHERE t.id = info_payers.nzp_bc_type) ";
                ret = ExecSQL(connDB, sql);
                if (!ret.result) throw new Exception();

                foreach (byte year in listYear)
                {
                    string finYY = Points.Pref + "_fin_" + year + DBManager.tableDelimiter;
                    sql = " INSERT INTO info_payers_send(nzp_supp, nzp_serv, nzp_payer, fn_dogovor_bank_lnk_id, sum_send) " +
                          " SELECT nzp_supp, nzp_serv, nzp_payer, fn_dogovor_bank_lnk_id, SUM(sum_send) " +
                          " FROM " + finYY + "fn_sended " +
                          " WHERE id_bc_file IS NULL " +
                            " AND fn_dogovor_bank_lnk_id IS NOT NULL " +
                            " AND nzp_supp > 0 " +
                            (whereServises != string.Empty ? " AND nzp_serv IN (" + whereServises + ")" : string.Empty) +
                          " GROUP BY nzp_supp, nzp_serv, fn_dogovor_bank_lnk_id, nzp_payer ";
                    ret = ExecSQL(connDB, sql);
                    if (!ret.result) throw new Exception();

                    sql = " INSERT INTO info_payers_send(nzp_supp, nzp_serv, nzp_payer, fn_dogovor_bank_lnk_id, sum_send) " +
                          " SELECT nzp_supp, nzp_serv, nzp_payer, fn_dogovor_bank_lnk_id, SUM(sum_send) " +
                          " FROM " + finYY + "fn_sended f INNER JOIN " + prefData + "bc_reestr_files b ON b.id = f.id_bc_file " +
                          " WHERE f.id_bc_file IS NOT NULL " +
                            " AND fn_dogovor_bank_lnk_id IS NOT NULL " +
                            " AND nzp_supp > 0 " +
                            (whereServises != string.Empty ? " AND nzp_serv IN (" + whereServises + ")" : string.Empty) +
                            " AND b.is_treaster = 1 " +
                          " GROUP BY nzp_supp, nzp_serv, fn_dogovor_bank_lnk_id, nzp_payer ";
                    ret = ExecSQL(connDB, sql);
                    if (!ret.result) throw new Exception();
                }

                sql = " UPDATE info_payers SET sum_send = " +
                      " (SELECT SUM(sum_send) " +
                       " FROM info_payers_send i " +
                       " WHERE i.nzp_supp = info_payers.nzp_supp " +
                         " AND i.nzp_payer = info_payers.nzp_payer " +
                         " AND i.fn_dogovor_bank_lnk_id  = info_payers.fn_dogovor_bank_lnk_id) ";
                ret = ExecSQL(connDB, sql);
                if (!ret.result) throw new Exception();

                #endregion

                sql = " SELECT * FROM info_payers ORDER BY supplier, payer ";

                ret = ExecRead(connDB, out reader, sql, true);
                if (!ret.result) throw new Exception();

                Int32 n = 0;
                while (reader.Read())
                {
                    Int32 nzpFd, nzpPayer;
                    Decimal sumSend;
                    n++;
                    listPayers.Add(new InfoPayerBankClient
                    {
                        OrderNumber = n,
                        nzp_supp = reader["nzp_supp"] != DBNull.Value
                            ? Int32.TryParse(reader["nzp_supp"].ToString(), out nzpFd)
                                ? nzpFd
                                : 0
                            : 0,
                        name_supp =
                            reader["supplier"] != DBNull.Value ? reader["supplier"].ToString().Trim() : string.Empty,
                        num_dog = reader["num_dog"] != DBNull.Value ? reader["num_dog"].ToString().Trim() : string.Empty,
                        nzp_payer = reader["nzp_payer"] != DBNull.Value
                            ? Int32.TryParse(reader["nzp_payer"].ToString(), out nzpPayer)
                                ? nzpPayer
                                : 0
                            : 0,
                        payer = reader["payer"] != DBNull.Value ? reader["payer"].ToString().Trim() : string.Empty,
                        payer_bank =
                            reader["payer_bank"] != DBNull.Value ? reader["payer_bank"].ToString().Trim() : string.Empty,
                        bc_type = reader["bc_type"] != DBNull.Value ? reader["bc_type"].ToString().Trim() : string.Empty,
                        sum_send = reader["sum_send"] != DBNull.Value
                            ? Decimal.TryParse(reader["sum_send"].ToString(), out sumSend)
                                ? sumSend
                                : 0m
                            : 0m,
                    });
                }
            }
            catch (Exception ex)
            {
                if (ret.text == string.Empty) ret.text = ex.Message;
                ret = new Returns(false);
                return null;
            }
            finally
            {
                if (TempTableInWebCashe(connDB, "info_payers")) ExecSQL(connDB, "DROP TABLE info_payers");
                if (TempTableInWebCashe(connDB, "info_payers_send")) ExecSQL(connDB, "DROP TABLE info_payers_send");
                reader.Close();
                connDB.Close();
            }
            return listPayers;
        }

		// замена правой части тега--------------------------------------------------
		public Returns SearchTag(MyDataReader myread1, MyDataReader myread2, ref string nameF) {
			Returns ret = Utils.InitReturns();
			string format = myread2["name_"].ToString();
			string tagDescr = myread2["tag_descr"].ToString();
			string checkExcept = "";
			try
			{
				nameF = Convert.ToString(myread2["name_field"]);
				checkExcept = "Обязательность заполнения";
				if (Convert.ToBoolean(myread2["is_requared"]))
				{
					if (nameF != "") if (myread1[nameF] != null) nameF = myread2["TAG_NAME"] + myread1[nameF].ToString() + "\r\n";
						else
						{
							ret.result = false;
							ret.text = "Ошибка: Нет необходимого значения в формате " + format + "  - " + tagDescr + ".";
							MonitorLog.WriteLog("Ошибка в функции SearchTag: Есть не заполненные поля", MonitorLog.typelog.Error, true);
						}
					else
					{
						ret.result = false;
						ret.text = "Ошибка: Нет необходимого значения в формате " + format + "  - Заполняемое поле.";
						MonitorLog.WriteLog("Ошибка в функции SearchTag: Есть не заполненные поля", MonitorLog.typelog.Error, true);
					}
				}
				else
				{
					checkExcept = "Отображаемость*";
					if (Convert.ToBoolean(myread2["is_show_empty"]))
					{
						if (nameF != "") if (myread1[nameF] != null) nameF = myread2["TAG_NAME"] + myread1[nameF].ToString() + "\r\n";
							else nameF = myread2["TAG_NAME"] + "\r\n";
						else nameF = myread2["TAG_NAME"] + "\r\n";
					}
					else
						if (nameF != "") if (myread1[nameF] != null) nameF = myread2["TAG_NAME"] + myread1[nameF].ToString() + "\r\n";
							else nameF = "";
				}
			}
			catch (InvalidCastException)
			{
				ret.result = false;
				ret.text = "Ошибка: Не заполнено поле в формате " + format + " - " + checkExcept + ".";
				MonitorLog.WriteLog("Ошибка в функции SearchTag: Есть не заполненные поля", MonitorLog.typelog.Error, true);
			}
			catch (Exception ex)
			{
				ret.result = false;
				ret.text = ex.Message;
				MonitorLog.WriteLog("Ошибка в функции SearchTag:\n" + (ex.Message != "" ? ex.Message : ret.text), MonitorLog.typelog.Error, true);
			}
			return ret;
		}

	    /// <summary>Сохранение состояния обработки файла банком</summary>
	    /// <param name="nzpUser">Идентификатор пользователя</param>
	    /// <param name="files">Файлы с изменённым файлом</param>
	    public Returns SaveCheckSend(int nzpUser, List<FilesUploadingBC> files) {
			Returns ret;

            #region Проверка входных параметров

            if (nzpUser <= 0)
            {
                ret = new Returns(false, "Пользователь не определен");
                return ret;
            }

            if (files == null || files.Count == 0)
            {
                ret = new Returns(false, "Нет файлов с изменённым состоянием");
                return ret;
            }

            #endregion

            #region Открываем соединение

            IDbConnection connDb = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(connDb, true);
            if (!ret.result)
            {
                ret.text = "Ошибка при открытии соединения с БД";
                MonitorLog.WriteLog("GetBanksExecutingPayments : " + ret.text, MonitorLog.typelog.Error, true);
                return ret;
            }

            #endregion

			try
			{
			    string prefData = Points.Pref + DBManager.sDataAliasRest;
			    var whereIdTrue = string.Join(",", files.Where(x => x.IsTreaster).Select(x => x.Id)).Trim(',');
                var whereIdFalse = string.Join(",", files.Where(x => !x.IsTreaster).Select(x => x.Id)).Trim(',');

			    string sql;
			    if (whereIdTrue != string.Empty)
			    {
			        sql = " UPDATE " + prefData + "bc_reestr_files SET is_treaster = 1 " +
			              " WHERE id IN (" + whereIdTrue + ")";
                    ret = ExecSQL(connDb, sql, true);
                    if (!ret.result) throw new Exception(ret.text);
			    }

                if (whereIdFalse != string.Empty)
                {
                    sql = " UPDATE " + prefData + "bc_reestr_files SET is_treaster = 0 " +
                          " WHERE id IN (" + whereIdFalse + ")";
                    ret = ExecSQL(connDb, sql, true);
                    if (!ret.result) throw new Exception(ret.text);
                }
			}
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры SaveCheckSend : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = "Ошибка при сохранении состояния обработки банком";
                ret.result = false;
            }
            finally
            {
                connDb.Close();
            }
			return ret;
		}

        #region Справочник - Форматы выгрузки в системы банк-клиент

        /// <summary>Возвращает список форматов</summary>
	    /// <param name="nzpUser">Идентификатор пользователя</param>
	    /// <param name="ret">Состояние работы функции</param>
	    /// <param name="idFormats">Идентификатор формата</param>
	    public List<FormatBC> GetFormats(int nzpUser, out Returns ret, List<int> idFormats) {
            #region Проверка входных параметров

            if (nzpUser <= 0)
            {
                ret = new Returns(false, "Пользователь не определен");
                return new List<FormatBC>();
            }

            #endregion

            #region Открываем соединение

            IDbConnection connDb = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(connDb, true);
            if (!ret.result)
            {
                ret.text = "Ошибка при открытии соединения с БД";
                MonitorLog.WriteLog("GetFormats : " + ret.text, MonitorLog.typelog.Error, true);
                return new List<FormatBC>();
            }

            #endregion

            var types = new List<FormatBC>();
            try
            {
			    MyDataReader myread;
                string whereFormat = idFormats != null && idFormats.Count > 0
                    ? " AND id IN (" + string.Join(",", idFormats).Trim(',', ' ') + ")"
                    : string.Empty;
			    string sql = " SELECT id, name_, is_active " +
			                 " FROM " + Points.Pref + DBManager.sKernelAliasRest + "bc_types " +
			                 " WHERE 1 = 1 " + whereFormat +
			                 " ORDER BY 1 ";
                ret = ExecRead(connDb, out myread, sql, true);
                if (!ret.result)
                {
                    string error = ret.text;
                    ret.text = string.Empty;
                    throw new Exception(error);
                }

				while (myread.Read())
				{
					var typ = new FormatBC
					{
						Id = Convert.ToInt32(myread["id"]),
						Name = myread["name_"].ToString().Trim(),
						IsActive = Convert.ToBoolean(myread["is_active"])
					};
					types.Add(typ);
				}
			}
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetFormats : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = string.IsNullOrEmpty(ret.text) ? "Ошибка при загрузке форматов" : ret.text;
                ret.result = false;
            }
            finally
            {
                if (connDb != null)
                    connDb.Close();
            }

			return types;
		}

        /// <summary>Возвращает формат</summary>
        /// <param name="nzpUser">Идентификатор пользователя</param>
        /// <param name="idFormat">Идентификатор формата</param>
        /// <param name="ret">Состояние работы функции</param>
        public FormatBC GetFormat(int nzpUser, int idFormat, out Returns ret)
	    {
            #region Проверка входных параметров

            if (nzpUser <= 0)
            {
                ret = new Returns(false, "Пользователь не определен");
                return new FormatBC();
            }

            if (idFormat <= 0)
            {
                ret = new Returns(false, "Неверный идентификатор формата. Обратитесь к разработчику.");
                MonitorLog.WriteLog("GetFormat. Неверный идентификатор формата. idFormat = " + idFormat, MonitorLog.typelog.Error, true);
                return new FormatBC();
            }

            #endregion

            ret = Utils.InitReturns();
            var format = new FormatBC();
            try
            {
                List<FormatBC> formats = GetFormats(nzpUser, out ret, new List<int>{ idFormat } );
                if (!ret.result)
                {
                    string error = ret.text;
                    ret.text = string.Empty;
                    throw new Exception(error);
                }
                format = formats.Find(x => x.Id == idFormat) ?? new FormatBC();
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetFormat : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = string.IsNullOrEmpty(ret.text) ? "Ошибка при загрузке формата" : ret.text;
                ret.result = false;
            }
            return format;
	    }

	    /// <summary>Добавление нового формата</summary>
	    /// <param name="nzpUser">Идентификатор пользователя</param>
	    /// <param name="nameFormat">Наименование формата</param>
	    /// <param name="ret">Состояние выполнение функции</param>
	    /// <returns>Идентификатор добавленного формата</returns>
	    public int AddFormat(int nzpUser, string nameFormat, out Returns ret)
		{
            int idFromat = 0;

            #region Проверка входных параметров

            if (nzpUser <= 0)
            {
                ret = new Returns(false, "Пользователь не определен");
                return idFromat;
            }

            if (string.IsNullOrEmpty(nameFormat))
            {
                ret = new Returns(false, "Не корректное наименование формата");
                return idFromat;
            }

            if (nameFormat.Length > 100)
            {
                ret = new Returns(false, "Наименование формата превышает допустипую длину в 100 символов");
                return idFromat;
            }

            #endregion

            #region Открываем соединение

            IDbConnection connDb = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(connDb, true);
            if (!ret.result)
            {
                ret.text = "Ошибка при открытии соединения с БД";
                MonitorLog.WriteLog("AddFormat : " + ret.text, MonitorLog.typelog.Error, true);
                return idFromat;
            }

            #endregion

		    try
		    {
#if PG
		        string prefKernel = Points.Pref + DBManager.sKernelAliasRest;

		        string sql = " SELECT id FROM " + prefKernel + "bc_types " +
                             " WHERE TRIM(name_) = '" + nameFormat.Trim() + "' ";
                DataTable dt = DBManager.ExecSQLToTable(connDb, sql);
		        bool isDefaultNameFormat = dt.Rows != null && dt.Rows.Count > 0;

		        sql = " INSERT INTO " + prefKernel + "bc_types (name_, is_active) " +
                      " VALUES ('" + nameFormat.Trim() + "',0) RETURNING id ";
		        dt = DBManager.ExecSQLToTable(connDb, sql);
		        foreach (DataRow row in dt.Rows)
		        {
                    if (row["id"] != DBNull.Value)
                        int.TryParse(row["id"].ToString(), out idFromat);
                }

                #region Проставить наименование формата по умолчанию

                if (idFromat > 0 && isDefaultNameFormat)
                {
                    sql = " UPDATE " + prefKernel + " bc_types SET name_ = 'Новый_формат_' || " + idFromat +
                          " WHERE id = " + idFromat;
                    ret = ExecSQL(connDb, sql);
                    if (!ret.result)
                    {
                        string error = ret.text;
                        ret.text = string.Empty;
                        throw new Exception(error);
                    }
                }

                #endregion
#else
		        string sql = " INSERT INTO " + Points.Pref + DBManager.sKernelAliasRest +
		                     " bc_types (name_, is_active) VALUES ('Новый формат',0) ";
                ret = ExecSQL(connDb, sql);
                if (!ret.result)
                {
                    string error = ret.text;
                    ret.text = string.Empty;
                    throw new Exception(error);
                }
#endif

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры AddFormat : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = string.IsNullOrEmpty(ret.text) ? "Ошибка при добавлении формата" : ret.text;
                ret.result = false;
            }
            finally
            {
                if (connDb != null)
                    connDb.Close();
            }
            return idFromat;
		}

        /// <summary>Сохраняет изменения формата</summary>
		/// <param name="nzpUser">Идентификатор пользователь</param>
		/// <param name="format">Формат</param>
		public Returns SaveFormat(int nzpUser, FormatBC format) {
            Returns ret;

            #region Проверка входных параметров

            if (nzpUser <= 0)
            {
                ret = new Returns(false, "Пользователь не определен");
                return ret;
            }

            if (format.Id <= 0)
            {
                ret = new Returns(false, "Формат не выбран");
                return ret;
            }

            if (string.IsNullOrEmpty(format.Name))
            {
                ret = new Returns(false, "Не корректное наименование формата");
                return ret;
            }

            if (format.Name.Length > 100)
            {
                ret = new Returns(false, "Наименование формата превышает допустипую длину в 100 символов");
                return ret;
            }

            #endregion

            #region Открываем соединение

            IDbConnection connDb = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(connDb, true);
            if (!ret.result)
            {
                ret.text = "Ошибка при открытии соединения с БД";
                MonitorLog.WriteLog("SaveFormat : " + ret.text, MonitorLog.typelog.Error, true);
                return ret;
            }

            #endregion

		    try
		    {
                string sql = " UPDATE " + Points.Pref + DBManager.sKernelAliasRest + "bc_types SET " +
                             " (name_, is_active) = " +
                             " ('" + format.Name + "'," + Convert.ToByte(format.IsActive) + ") " +
                             " WHERE id = " + format.Id;
                ret = ExecSQL(connDb, sql, true);
                if (!ret.result)
                {
                    string error = ret.text;
                    ret.text = string.Empty;
                    throw new Exception(error);
                }
		    }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры SaveFormat : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = string.IsNullOrEmpty(ret.text) ? "Ошибка при сохранении изменений формата" : ret.text;
                ret.result = false;
            }
            finally
            {
                if (connDb != null)
                    connDb.Close();
            }
			return ret;
		}

		/// <summary>Удаление формата</summary>
		/// <param name="nzpUser">Идентификатор пользователя</param>
		/// <param name="idFormat">Индентификатор формата</param>
		public Returns DeleteFormat(int nzpUser, int idFormat)
		{
		    Returns ret;

            #region Проверка входных параметров

            if (nzpUser <= 0)
            {
                ret = new Returns(false, "Пользователь не определен");
                return ret;
            }

            if (idFormat <= 0)
            {
                ret = new Returns(false, "Формат не выбран");
                return ret;
            }

            #endregion

            #region Открываем соединение

            IDbConnection connDb = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(connDb, true);
            if (!ret.result)
            {
                ret.text = "Ошибка при открытии соединения с БД";
                MonitorLog.WriteLog("DeleteFormat : " + ret.text, MonitorLog.typelog.Error, true);
                return ret;
            }

            #endregion


		    try
            {
                #region Проверка на имеющие ссылки в других таблицах

                string prefData = Points.Pref + DBManager.sDataAliasRest,
                        prefKernel = Points.Pref + DBManager.sKernelAliasRest;

                string sql = " SELECT COUNT(*) " +
                             " FROM " + prefKernel + "bc_types " +
                             " WHERE id = " + idFormat + 
                               " AND is_active = 1 ";
                int countFormat = Convert.ToInt32(ExecScalar(connDb, sql, out ret, true));
                if (!ret.result)
                {
                    string error = ret.text;
                    ret.text = string.Empty;
                    throw new Exception(error);
                }
                if (countFormat > 0)
                {
                    ret.text = "Удаление не возможно. Данный формат является действующим.";
                    throw new Exception(ret.text);
                }

		        sql = " SELECT COUNT(*) " +
                      " FROM " + prefData + "bc_reestr_files " +
                      " WHERE id_bc_type = " + idFormat;
                countFormat = Convert.ToInt32(ExecScalar(connDb, sql, out ret, true));
                if (!ret.result)
                {
                    string error = ret.text;
                    ret.text = string.Empty;
                    throw new Exception(error);
                }
                if (countFormat > 0)
                {
                    ret.text = "Удаление не возможно. Имеются файлы из реестра файлов 'Банк-клиент', которые ссылаются на данный формат.";
                    throw new Exception(ret.text);
                }

                sql = " SELECT COUNT(*) " +
                      " FROM " + prefKernel + "s_payer " +
                      " WHERE id_bc_type = " + idFormat;
                countFormat = Convert.ToInt32(ExecScalar(connDb, sql, out ret, true));
                if (!ret.result)
                {
                    string error = ret.text;
                    ret.text = string.Empty;
                    throw new Exception(error);
                }
		        if (countFormat > 0)
		        {
                    ret.text = "Удаление не возможно. Имеются контрагенты, которые ссылаются на данный формат.";
                    throw new Exception(ret.text);
                }

                #endregion

                //удаление формата
                sql = "DELETE FROM " + prefKernel + "bc_types WHERE id = " + idFormat;
                ret = ExecSQL(connDb, sql, true);
                if (!ret.result)
                {
                    string error = ret.text;
                    ret.text = string.Empty;
                    throw new Exception(error);
                }

                //удаление тегов формата
                sql = "DELETE FROM " + prefKernel + "bc_schema WHERE id_bc_type = " + idFormat;
                ret = ExecSQL(connDb, sql, true);
                if (!ret.result)
                {
                    string error = ret.text;
                    ret.text = string.Empty;
                    throw new Exception(error);
                }
		    }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры DeleteFormat : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = string.IsNullOrEmpty(ret.text) ? "Ошибка при удалении формата" : ret.text;
                ret.result = false;
            }
            finally
            {
                if (connDb != null)
                    connDb.Close();
            }
			return ret;
		}

	    /// <summary>Возвращает список тегов</summary>
	    /// <param name="nzpUser">Идентификатор пользователя</param>
	    /// <param name="ret">Результат выполнение функции</param>
	    /// <param name="idFormat">Иденитификатор формата</param>
	    /// <param name="idTag">Идентификатор тега</param>
	    public List<TagBC> GetTags(int nzpUser, out Returns ret, int idFormat = -1, int idTag = -1) {

            #region Проверка входных параметров

            if (nzpUser <= 0)
            {
                ret = new Returns(false, "Пользователь не определен");
                return new List<TagBC>();
            }

            if (idFormat <= 0 && idTag <= 0)
            {
                ret = new Returns(false, "Неверный идентификатор формата. Обратитесь к разработчику.");
                MonitorLog.WriteLog("GetTags. Неверный идентификатор формата. idFormat = " + idFormat, MonitorLog.typelog.Error, true);
                return new List<TagBC>();
            }

            #endregion

            #region Открываем соединение

            IDbConnection connDb = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(connDb, true);
            if (!ret.result)
            {
                ret.text = "Ошибка при открытии соединения с БД";
                MonitorLog.WriteLog("GetTags : " + ret.text, MonitorLog.typelog.Error, true);
                return new List<TagBC>();
            }

            #endregion

            var listTags = new List<TagBC>();
            try
            {
			    MyDataReader myread;
                string prefKernel = Points.Pref + DBManager.sKernelAliasRest;
                string whereIdTag = idTag > 0 ? " AND bcs.id = " + idTag : string.Empty;
                string whereTypeFormat = idFormat > 0 ? " AND bcs.id_bc_type = " + idFormat : string.Empty;
			    string sql = " SELECT bcs.id, " +
			    					" bcs.id_bc_row_type, " +
			    					" bcr.name_, " +
			    					" bcs.num, " +
			    					" bcs.tag_name, " +
			    					" bcs.tag_descr,  " +
			    					" bcs.is_requared, " +
			    					" bcs.is_show_empty, " +
			    					" bcrt.id as idField, " +
			    					" bcrt.note_ " +
                             " FROM " + prefKernel + "bc_schema bcs LEFT OUTER JOIN " + prefKernel + "bc_row_type bcr ON bcr.id = bcs.id_bc_row_type " +
                                                                  " LEFT OUTER JOIN " + prefKernel + "bc_fields bcrt ON bcrt.id = bcs.id_bc_field " +
                             " WHERE 1 = 1 " + whereTypeFormat + whereIdTag +
			    			 " ORDER BY  bcs.id_bc_row_type,  bcs.num";
			    ret = ExecRead(connDb, out myread, sql, true);
			    if (!ret.result)
			    {
			        string error = ret.text;
			        ret.text = string.Empty;
			        throw new Exception(error);
			    }

				while (myread.Read())
				{
					var rowTag = new TagBC
					{
						Num = (myread["num"] != DBNull.Value) ? Convert.ToInt16(myread["num"]) : 0,
						Id = (myread["id"] != DBNull.Value) ? Convert.ToByte(myread["id"]) : 0,
                        TypeTag = new TypeTagBC
                        {
                            Id = (myread["id_bc_row_type"] != DBNull.Value)
                                ? Convert.ToByte(myread["id_bc_row_type"])
                                : 0,
                            Name = (myread["name_"] != DBNull.Value) ? myread["name_"].ToString().Trim() : ""
                        },
                        Name = (myread["tag_name"] != DBNull.Value) ? myread["tag_name"].ToString().Trim() : "",
                        Description = (myread["tag_descr"] != DBNull.Value) ? myread["tag_descr"].ToString().Trim() : "",
                        Value = new ValueTagBC
                        {
                            Id = (myread["idField"] != DBNull.Value) ? Convert.ToInt16(myread["idField"]) : 0,
                            Name = (myread["note_"] != DBNull.Value) ? myread["note_"].ToString().Trim() : ""
                        }
					};
				    string isRequared = (myread["is_requared"] != DBNull.Value && myread["is_requared"].ToString() != String.Empty)
				        ? myread["is_requared"].ToString()
				        : "0";
					rowTag.IsRequared = isRequared.Trim() == "1";

				    string isShowEmpty = (myread["is_show_empty"] != DBNull.Value &&
				                          Convert.ToString(myread["is_show_empty"]) != String.Empty)
				        ? myread["is_show_empty"].ToString().Trim()
				        : "0";
				    rowTag.IsShowEmpty = isShowEmpty == "1";

					listTags.Add(rowTag);
				}
			}
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetTags : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = string.IsNullOrEmpty(ret.text) ? "Ошибка при загрузке тегов" : ret.text;
                ret.result = false;
            }
            finally
            {
                if (connDb != null)
                    connDb.Close();
            }

			return listTags;
		}

        /// <summary>Возвращает тег</summary>
        /// <param name="nzpUser">Идентификатор пользователя</param>
        /// <param name="idTag">Идентификатор тега</param>
        /// <param name="ret">Результат выполнение функции</param>
        public TagBC GetTag(int nzpUser, int idTag, out Returns ret)
        {
            #region Проверка входных параметров

            if (nzpUser <= 0)
            {
                ret = new Returns(false, "Пользователь не определен");
                return new TagBC();
            }

            if (idTag <= 0)
            {
                ret = new Returns(false, "Неверный идентификатор тега. Обратитесь к разработчику.");
                MonitorLog.WriteLog("GetTag. Неверный идентификатор тега. idTag = " + idTag, MonitorLog.typelog.Error, true);
                return new TagBC();
            }

            #endregion

            ret = Utils.InitReturns();
            var tag = new TagBC();
            try
            {
                List<TagBC> tags = GetTags(nzpUser, out ret, idTag : idTag);
                if (!ret.result)
                {
                    string error = ret.text;
                    ret.text = string.Empty;
                    throw new Exception(error);
                }
                tag = tags.Find(x => x.Id == idTag) ?? new TagBC();
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetTag : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = string.IsNullOrEmpty(ret.text) ? "Ошибка при загрузке тега" : ret.text;
                ret.result = false;
            }
            return tag;
        }

	    /// <summary>Добавляет тег</summary>
	    /// <param name="nzpUser">Идентификтор пользователя</param>
	    /// <param name="tag">Тег</param>
	    /// <param name="ret">Результат выполнение функции</param>
	    /// <returns>Идентификатор тега</returns>
        public int AddTag(int nzpUser, TagBC tag, out Returns ret) {
	        int idTag = 0;

            #region Проверка входных параметров

            if (nzpUser <= 0)
            {
                ret = new Returns(false, "Пользователь не определен");
                return idTag;
            }
            if ((tag.Name ?? string.Empty).Length > 50)
            {
                ret = new Returns(false, "Наименование тега должно быть не более 50 символов.");
                return idTag;
            }
            if ((tag.Description ?? string.Empty).Length > 250)
            {
                ret = new Returns(false, "Описание тега должно быть не более 250 символов.");
                return idTag;
            }
            #endregion

            #region Открываем соединение

            IDbConnection connDb = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(connDb, true);
            if (!ret.result)
            {
                ret.text = "Ошибка при открытии соединения с БД";
                MonitorLog.WriteLog("AddTag : " + ret.text, MonitorLog.typelog.Error, true);
                return 0;
            }

            #endregion

            try
            {
                string prefKernel = Points.Pref + DBManager.sKernelAliasRest;
                int num = GetNumForTag(connDb, tag.Format.Id, tag.TypeTag.Id);

                string sql = " INSERT INTO " + prefKernel + "bc_schema " +
                            " (id_bc_type, id_bc_row_type, num, tag_name, tag_descr, id_bc_field, is_requared, is_show_empty ) " +
                      " VALUES(" + tag.Format.Id + "," + tag.TypeTag.Id + "," + num + ",'" + tag.Name + "','" +
                      tag.Description + "'," + tag.Value.Id + "," + (tag.IsRequared ? 1 : 0) + "," + (tag.IsShowEmpty ? 1 : 0) + ") ";
                ret = ExecSQL(connDb, sql);
                if (!ret.result)
                {
                    string error = ret.text;
                    ret.text = string.Empty;
                    throw new Exception(error);
                }

                sql = " SELECT id " +
                      " FROM " + prefKernel + "bc_schema " +
                      " WHERE id_bc_type = " + tag.Format.Id +
                        " AND id_bc_row_type = " + tag.TypeTag.Id +
                        " AND num = " + num;
                DataTable dt = DBManager.ExecSQLToTable(connDb, sql);
                if (dt.Rows == null || 
                    dt.Rows.Count != 1 ||
                    dt.Rows[0]["id"] == DBNull.Value ||
                    !int.TryParse(dt.Rows[0]["id"].ToString(), out idTag))
                    throw new Exception("Произошла ошибка при определении идентификатора тега");

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры AddTag : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = string.IsNullOrEmpty(ret.text) ? "Ошибка при добавлении тега" : ret.text;
                ret.result = false;
            }
            finally
            {
                if (connDb != null)
                    connDb.Close();
            }
            return idTag;
		}

	    /// <summary>Возвращает свободный порядковый номер для тега</summary>
	    /// <param name="connDb">Подключение к БД</param>
	    /// <param name="idFormat">Идентификатор формата</param>
	    /// <param name="idTypeTag">Идентификатор тега</param>
	    /// <returns>Порядковый номер</returns>
	    private int GetNumForTag(IDbConnection connDb, int idFormat, int idTypeTag)
	    {
            int num;
            string prefKernel = Points.Pref + DBManager.sKernelAliasRest;
            string sql = " SELECT MAX(num) AS num " +
                         " FROM " + prefKernel + "bc_schema " +
                         " WHERE id_bc_type = " + idFormat +
                           " AND id_bc_row_type = " + idTypeTag;
            DataTable dt = DBManager.ExecSQLToTable(connDb, sql);
            if (dt.Rows != null && dt.Rows.Count > 0)
            {
                int maxNum = dt.Rows[0]["num"] != DBNull.Value ? Convert.ToInt32(dt.Rows[0]["num"]) : 0;
                num = maxNum + 1;
            }
            else num = 1;
	        return num;
	    }

	    /// <summary>Удаляет тег</summary>
        /// <param name="nzpUser">Идентификатор пользователя</param>
        /// <param name="idTag">Идентификатор тега</param>
        public Returns DeleteTag(int nzpUser, int idTag) {
            Returns ret;

            #region Проверка входных параметров

            if (nzpUser <= 0)
            {
                ret = new Returns(false, "Пользователь не определен");
                return ret;
            }
            if (idTag <= 0)
            {
                ret = new Returns(false, "Неверный идентификатор тега. Обратитесь к разработчику.");
                return ret;
            }

            #endregion

            #region Открываем соединение

            IDbConnection connDb = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(connDb, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры DeleteTag : " + ret.text, MonitorLog.typelog.Error, true);
                ret.text = "Ошибка при открытии соединения с БД";
                return ret;
            }

            #endregion

            string prefKernel = Points.Pref + DBManager.sKernelAliasRest;

            try
            {
                RefreshNumTag(connDb, idTag, out ret);

                string sql = "DELETE FROM " + prefKernel + "bc_schema WHERE  id = " + idTag;
                ret = ExecSQL(connDb,sql);
                if (!ret.result)
                {
                    string erroe = ret.text;
                    ret.text = string.Empty;
                    throw new Exception(erroe);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры DeleteTag : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = string.IsNullOrEmpty(ret.text) ? "Ошибка при удалении тега" : ret.text;
                ret.result = false;
            }
            finally
            {
                if (connDb != null)
                    connDb.Close();
            }

            return ret;
		}

        /// <summary>Обновляет порядковые номера выше по номеру указанного тега</summary>
        /// <param name="connDb">Подключение к БД</param>
        /// <param name="idTag">Идентификатор тега</param>
        /// <param name="ret">Рельзультат выболнение тега</param>
        private void RefreshNumTag(IDbConnection connDb, int idTag, out Returns ret)
        {
            string prefKernel = Points.Pref + DBManager.sKernelAliasRest;
            ExecSQL(connDb, "DROP TABLE t_bc_refresh_tag_" + idTag);
            string sql = " SELECT id INTO TEMP t_bc_refresh_tag_" + idTag +
                         " FROM " + prefKernel + "bc_schema g " +
                         " WHERE num > (SELECT num " +
                                      " FROM " + prefKernel + "bc_schema l " +
                                      " WHERE l.id = " + idTag + " " +
                                        " AND l.id_bc_type = g.id_bc_type " +
                                        " AND l.id_bc_row_type = g.id_bc_row_type) ";
            ret = ExecSQL(connDb, sql);
            if (!ret.result)
            {
                string erroe = ret.text;
                ret.text = string.Empty;
                throw new Exception(erroe);
            }

            sql = " UPDATE " + prefKernel + "bc_schema g SET num = g.num - 1 " +
                  " FROM t_bc_refresh_tag_" + idTag + " l " +
                  " WHERE l.id = g.id ";
            ret = ExecSQL(connDb, sql);
            if (!ret.result)
            {
                string erroe = ret.text;
                ret.text = string.Empty;
                throw new Exception(erroe);
            }
	    }

	    /// <summary>Сохраняет тег</summary>
	    /// <param name="tag">Тег</param>
	    /// <param name="nzpUser">Идентификатор пользователя</param>
	    public Returns SaveTag(int nzpUser, TagBC tag )
		{
		    Returns ret;

            #region Проверка входных параметров

            if (nzpUser <= 0)
            {
                ret = new Returns(false, "Пользователь не определен");
                return ret;
            }
            if (tag.Id <= 0)
            {
                ret = new Returns(false, "Неверный идентификатор тега. Обратитесь к разработчику.");
                return ret;
            }
            if ((tag.Name ?? string.Empty).Length > 50)
            {
                ret = new Returns(false, "Наименование тега должно быть не более 50 символов.");
                return ret;
            }
            if ((tag.Description ?? string.Empty).Length > 250) 
            {
                ret = new Returns(false, "Описание тега должно быть не более 250 символов.");
                return ret;
            }
            #endregion

		    #region Открываем соединение

            IDbConnection connDb = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(connDb, true);
            if (!ret.result)
            {
                ret.text = "Ошибка при открытии соединения с БД";
                MonitorLog.WriteLog("SaveTag : " + ret.text, MonitorLog.typelog.Error, true);
                return ret;
            }

            #endregion

			try
			{
			    string prefKernel = Points.Pref + DBManager.sKernelAliasRest;
			    string updateNum = string.Empty;

			    string sql = " SELECT id " +
                             " FROM " + prefKernel + "bc_schema " +
			                 " WHERE id = " + tag.Id +
                               " AND id_bc_row_type <> " + tag.TypeTag.Id;
			    DataTable dt = DBManager.ExecSQLToTable(connDb, sql);
			    if (dt.Rows != null && dt.Rows.Count > 0)
			    {
                    RefreshNumTag(connDb, tag.Id, out ret);
                    int num = GetNumForTag(connDb, tag.Format.Id, tag.TypeTag.Id);
                    updateNum = " num = " + num + ", ";
			    }

			    sql = " UPDATE " + prefKernel + "bc_schema SET " +
                        " id_bc_row_type = " + tag.TypeTag.Id + ", " + updateNum +
                        " tag_name = '" + tag.Name + "', " +
                        " tag_descr = '" + tag.Description + "', " +
                        " id_bc_field = " + tag.Value.Id + ", " +
                        " is_requared = " + (tag.IsRequared ? 1 : 0) + ", " +
                        " is_show_empty = " + (tag.IsShowEmpty ? 1 : 0) +
                      " WHERE id =" + tag.Id;
                ret = ExecSQL(connDb, sql);
			    if (!ret.result)
			    {
			        string error = ret.text;
			        ret.text = string.Empty;
                    throw new Exception(error);
			    }
			}
			catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры SaveTag : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = string.IsNullOrEmpty(ret.text) ? "Ошибка при сохранении тега" : ret.text;
                ret.result = false;
            }
            finally
            {
                if (connDb != null)
                    connDb.Close();
            }
			return ret;
		}

        /// <summary>Увеличивает порядковый номер тега на 1</summary>
        /// <param name="nzpUser">Идентификатор пользователя</param>
        /// <param name="idTag">Идентификатор тега</param>
        public Returns UpTag(int nzpUser, int idTag) 
        {
            Returns ret;

            #region Проверка входных параметров

            if (nzpUser <= 0)
            {
                ret = new Returns(false, "Пользователь не определен");
                return ret;
            }
            if (idTag <= 0)
            {
                ret = new Returns(false, "Неверный идентификатор тега.");
                return ret;
            }

            #endregion

            #region Открываем соединение

            IDbConnection connDb = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(connDb, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры UpTag : " + ret.text, MonitorLog.typelog.Error, true);
                ret.text = "Ошибка при открытии соединения с БД";
                return ret;
            }

            #endregion

            string prefKernel = Points.Pref + DBManager.sKernelAliasRest;

            try
            {
                string sql = " SELECT id, id_bc_type, id_bc_row_type, num " +
                             " FROM " + prefKernel + "bc_schema g " +
                             " WHERE EXISTS(SELECT * " +
                                          " FROM " + prefKernel + "bc_schema l " +
                                          " WHERE l.id = " + idTag + " " +
                                            " AND l.id_bc_row_type = g.id_bc_row_type " +
                                            " AND l.id_bc_type = g.id_bc_type " +
                                            " AND (l.num - 1) = g.num) ";
                DataTable dt = DBManager.ExecSQLToTable(connDb, sql);
                if (dt.Rows.Count > 0)
                {
                    #region Проверка тега находящийся до тега указанного во вх. параметре

                    int num = dt.Rows[0]["num"] != DBNull.Value ? Convert.ToInt32(dt.Rows[0]["num"]) : -1;
                    if (num < 1)
                        throw new Exception("Тег, находящийся до тега с идетификатором " + idTag + ", " +
                                            "имеет не верный порядковый номер: " + num + " .");

                    int idTagBefore = dt.Rows[0]["id"] != DBNull.Value ? Convert.ToInt32(dt.Rows[0]["id"]) : -1;
                    if (idTagBefore < 1)
                        throw new Exception("Тег, находящийся до тега с идетификатором " + idTag + ", " +
                                            "имеет не верный идентификатор: " + idTagBefore + " .");

                    #endregion

                    sql = " UPDATE " + prefKernel + "bc_schema t SET num = " + num +
                          " WHERE id = " + idTag;
                    ret = ExecSQL(connDb, sql);
                    if (!ret.result)
                    {
                        string erroe = ret.text;
                        ret.text = string.Empty;
                        throw new Exception(erroe);
                    }

                    sql = " UPDATE " + prefKernel + "bc_schema t SET num = " + (num + 1) +
                          " WHERE id = " + idTagBefore;
                    ret = ExecSQL(connDb, sql);
                    if (!ret.result)
                    {
                        string erroe = ret.text;
                        ret.text = string.Empty;
                        throw new Exception(erroe);
                    }
                }
                else
                {
                    ret.tag = 1;
                    ret.text = "Переместить тег невозможно. Достигнута верхняя граница данного типа тега.";
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры UpTag : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = string.IsNullOrEmpty(ret.text) ? "Ошибка при перемещении тега" : ret.text;
                ret.result = false;
            }
            finally
            {
                if (connDb != null)
                    connDb.Close();
            }

            return ret;
		}

        /// <summary>Уменьшает порядковый номер тега на 1</summary>
        /// <param name="nzpUser">Идентификатор пользователя</param>
        /// <param name="idTag">Идентификатор тега</param>
	    public Returns DownTag(int nzpUser, int idTag)
		{
		    Returns ret;

            #region Проверка входных параметров

            if (nzpUser <= 0)
            {
                ret = new Returns(false, "Пользователь не определен");
                return ret;
            }
            if (idTag <= 0)
		    {
                ret = new Returns(false, "Неверный идентификатор тега.");
                return ret;
		    }

            #endregion

            #region Открываем соединение

            IDbConnection connDb = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(connDb, true);
		    if (!ret.result)
		    {
                MonitorLog.WriteLog("Ошибка выполнения процедуры DownTag : " + ret.text, MonitorLog.typelog.Error, true);
                ret.text = "Ошибка при открытии соединения с БД";
                return ret;
		    }

            #endregion

		    string prefKernel = Points.Pref + DBManager.sKernelAliasRest;

			try
			{
			    string sql = " SELECT id, id_bc_type, id_bc_row_type, num " +
                             " FROM " + prefKernel + "bc_schema g " +
                             " WHERE EXISTS(SELECT * " +
                                          " FROM " + prefKernel + "bc_schema l " +
                                          " WHERE l.id = " + idTag + " " +
                                            " AND l.id_bc_row_type = g.id_bc_row_type " +
                                            " AND l.id_bc_type = g.id_bc_type " +
                                            " AND (l.num + 1) = g.num) ";
			    DataTable dt = DBManager.ExecSQLToTable(connDb, sql);
			    if (dt.Rows.Count > 0)
			    {

                    #region Проверка тега находящийся до тега указанного во вх. параметре

                    int num = dt.Rows[0]["num"] != DBNull.Value ? Convert.ToInt32(dt.Rows[0]["num"]) : -1;
                    if (num < 1)
                        throw new Exception("Тег, находящийся до тега с идетификатором " + idTag + ", " +
                                            "имеет не верный порядковый номер: " + num + " .");

                    int idTagАfter = dt.Rows[0]["id"] != DBNull.Value ? Convert.ToInt32(dt.Rows[0]["id"]) : -1;
                    if (idTagАfter < 1)
                        throw new Exception("Тег, находящийся до тега с идетификатором " + idTag + ", " +
                                            "имеет не верный идентификатор: " + idTagАfter + " .");

                    #endregion

			        sql = " UPDATE " + prefKernel + "bc_schema t SET num = " + num +
                          " WHERE id = " + idTag;
			        ret = ExecSQL(connDb, sql);
			        if (!ret.result)
			        {
			            string erroe = ret.text;
			            ret.text = string.Empty;
                        throw new Exception(erroe);
			        }

			        sql = " UPDATE " + prefKernel + "bc_schema t SET num = " + (num - 1) +
                          " WHERE id = " + idTagАfter;
			        ret = ExecSQL(connDb, sql);
                    if (!ret.result)
                    {
                        string erroe = ret.text;
                        ret.text = string.Empty;
                        throw new Exception(erroe);
                    }
			    }
			    else
			    {
			        ret.tag = 1;
                    ret.text = "Переместить тег невозможно. Достигнута нижняя граница данного типа тега.";
			    }
			}
			catch (Exception ex)
			{
				MonitorLog.WriteLog("Ошибка выполнения процедуры DownTag : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = string.IsNullOrEmpty(ret.text) ? "Ошибка при перемещении тега" : ret.text;
				ret.result = false;
			}
			finally
			{
				if (connDb != null)
					connDb.Close();
			}

			return ret;
		}

        /// <summary>Возвращает значения тега</summary>
        /// <param name="nzpUser">Идентификатор пользователя</param>
        /// <param name="ret">Результат выполнение функции</param>
        public List<ValueTagBC> GetTagValues(int nzpUser, out Returns ret) {
            #region Проверка входных параметров

            if (nzpUser <= 0)
			{
				ret = new Returns(false, "Пользователь не определен");
				return null;
            }

            #endregion

            #region Открываем соединение

            IDbConnection connDb = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(connDb, true);
            if (!ret.result)
            {
                ret.text = "Ошибка при открытии соединения с БД";
                MonitorLog.WriteLog("GetTagValues : " + ret.text, MonitorLog.typelog.Error, true);
                return new List<ValueTagBC>();
            }

            #endregion

            var tagValues = new List<ValueTagBC>();
            try
			{
				MyDataReader reader;
				string sql = " SELECT id, note_ FROM " + Points.Pref + DBManager.sKernelAliasRest + "bc_fields ORDER BY 2";
			    ret = ExecRead(connDb, out reader, sql, true);
                if (!ret.result)
                {
                    string error = ret.text;
                    ret.text = string.Empty;
                    throw new Exception(error);
				}
				while (reader.Read())
				{
                    var bcField = new ValueTagBC
					{
						Id = Convert.ToInt32(reader["id"]),
						Name = reader["note_"].ToString().Trim()
					};
                    tagValues.Add(bcField);
				}

			}
			catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetTagValues : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = string.IsNullOrEmpty(ret.text) ? "Ошибка при загрузке значений тега" : ret.text;
                ret.result = false;
            }
            finally
            {
                if (connDb != null)
                    connDb.Close();
            }

            return tagValues;
		}

        /// <summary>Возвращает список типов тега</summary>
        /// <param name="nzpUser">Идентификатор пользователя</param>
        /// <param name="ret">Результат выполнение функции</param>
        public List<TypeTagBC> GetTagTypes(int nzpUser, out Returns ret) {
            #region Проверка входных параметров

            if (nzpUser <= 0)
            {
                ret = new Returns(false, "Пользователь не определен");
                return null;
            }

            #endregion

            #region Открываем соединение

            IDbConnection connDb = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(connDb, true);
            if (!ret.result)
            {
                ret.text = "Ошибка при открытии соединения с БД";
                MonitorLog.WriteLog("GetTagTypes : " + ret.text, MonitorLog.typelog.Error, true);
                return new List<TypeTagBC>();
            }

            #endregion

            var tagTypes = new List<TypeTagBC>();
			try
			{
				MyDataReader reader;
				string sql = " SELECT id, name_ FROM " + Points.Pref + DBManager.sKernelAliasRest + "bc_row_type ORDER BY 1 ";
                ret = ExecRead(connDb, out reader, sql, true);
                if (!ret.result)
                {
                    string error = ret.text;
                    ret.text = string.Empty;
                    throw new Exception(error);
                }

				while (reader.Read())
				{
                    var bcRowType = new TypeTagBC
					{
						Id = Convert.ToInt32(reader["id"]),
						Name = reader["name_"].ToString().Trim()
					};
                    tagTypes.Add(bcRowType);
				}

			}
			catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetTagTypes : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = string.IsNullOrEmpty(ret.text) ? "Ошибка при загрузке типов тега" : ret.text;
                ret.result = false;
            }
            finally
            {
                if (connDb != null)
                    connDb.Close();
            }

            return tagTypes;
		}

        /// <summary>Возвращает информацию о выгрузок</summary>
        public List<UploadingOnWebBC> GetUploading(int nzpUser, int skip, int rows, int idReestr, out Returns ret) {
            #region Проверка входных параметров

            if (nzpUser <= 0)
            {
                ret = new Returns(false, "Пользователь не определен");
                return new List<UploadingOnWebBC>();
            }

            #endregion

            #region Открываем соединение

            IDbConnection connDb = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(connDb, true);
            if (!ret.result)
            {
                ret.text = "Ошибка при открытии соединения с БД";
                MonitorLog.WriteLog("GetUploading : " + ret.text, MonitorLog.typelog.Error, true);
                return new List<UploadingOnWebBC>();
            }

            #endregion

            string prefData = Points.Pref + DBManager.sDataAliasRest;
            var list = new List<UploadingOnWebBC>();

            try
            {
                int beginYear = Points.BeginWork.year_ - 2000,
                            endYear = DateTime.Now.Year - 2000;

                string whereReestr = idReestr > 0 ? " AND id = " + idReestr : string.Empty;

                string sql = " SELECT id, b.nzp_user, num_reestr, date_reestr, " +
                                    " TRIM(name) AS login, TRIM(comment) AS name " +
                             " FROM " + prefData + "bc_reestr b LEFT OUTER JOIN " + prefData + "users u ON u.nzp_user = b.nzp_user " +
                             " WHERE id > 0 " + whereReestr +
                             " ORDER BY date_reestr ";

                MyDataReader reader;
                ret = ExecRead(connDb, out reader, sql, true);
                if (!ret.result)
                {
                    string error = ret.text;
                    ret.text = string.Empty;
                    throw new Exception(error);
                }
                while (reader.Read())
                {
                    int numReestr = reader["num_reestr"] != DBNull.Value ? Convert.ToInt32(reader["num_reestr"]) : 0;
                    DateTime dateReestr = reader["date_reestr"] != DBNull.Value
                        ? Convert.ToDateTime(reader["date_reestr"])
                        : default(DateTime);

                    int id = reader["id"] != DBNull.Value ? Convert.ToInt32(reader["id"]) : 0;
                    int idUser = reader["nzp_user"] != DBNull.Value ? Convert.ToInt32(reader["nzp_user"]) : 0;
                    string login = reader["login"] != DBNull.Value ? Convert.ToString(reader["login"]).Trim() : string.Empty;
                    string name = reader["name"] != DBNull.Value ? Convert.ToString(reader["name"]).Trim() : string.Empty;
                    string userName = login + (name != string.Empty ? " (" + name + ")" : string.Empty);
                    decimal totalMoney = 0m;

                    for (int j = beginYear; j <= endYear; j++)
                    {
                        sql = " SELECT SUM(sum_send) " +
                              " FROM " + Points.Pref + "_fin_" + j + DBManager.tableDelimiter + "fn_sended," +
                                         prefData + "bc_reestr_files " +
                              " WHERE id_bc_file = id " +
                                " AND id_bc_reestr = " + id;

                        var value = ExecScalar(connDb, sql, out ret, true);
                        decimal sumSend = value != DBNull.Value ? Convert.ToDecimal(value) : 0m;
                        totalMoney = sumSend;
                    }
                    ret.result = true;
                    list.Add(new UploadingOnWebBC
                    {
                        Id = id,
                        User = new User { nzp_user = idUser, uname = userName },
                        NumReestr = numReestr,
                        DateReestr = dateReestr,
                        TotalSumTransfer = totalMoney
                    });
                }
                reader.Close();

                ret.tag = list.Count;
                if (rows != 0) list = list.Skip(skip).Take(rows).ToList();
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetUploading : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = string.IsNullOrEmpty(ret.text) ? "Ошибка при получении списка реестра выгрузок" : ret.text;
                ret.result = false;
            }
            finally
            {
                if (connDb != null)
                    connDb.Close();
            }
            return list;
        }

        public List<FilesUploadingOnWebBC> GetFilesUploading(int nzpUser, int idReestr, int skip, int rows, out Returns ret) {
            #region Проверка входных параметров

            if (nzpUser <= 0)
            {
                ret = new Returns(false, "Пользователь не определен");
                return new List<FilesUploadingOnWebBC>();
            }

            #endregion

            #region Открываем соединение

            IDbConnection connDb = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(connDb, true);
            if (!ret.result)
            {
                ret.text = "Ошибка при открытии соединения с БД";
                MonitorLog.WriteLog("GetFilesUploading : " + ret.text, MonitorLog.typelog.Error, true);
                return new List<FilesUploadingOnWebBC>();
            }

            #endregion

            var filesUploading = new List<FilesUploadingOnWebBC>();
            try
            {
                MyDataReader reader;
                var listDate = new List<int>();

                string prefData = Points.Pref + DBManager.sDataAliasRest,
                        prefKernel = Points.Pref + DBManager.sKernelAliasRest;

                string sql = " SELECT rf.id, " +
                                    " rf.nzp_exc, " +
                                    " p.payer AS bank, " +
                                    " rf.file_name, " +
                                    " t.name_ AS type_format, " +
                                    " rf.is_treaster " +
                             " FROM " + prefData + "bc_reestr_files rf INNER JOIN " + prefKernel + "s_payer p ON p.nzp_payer = rf.id_payer_bank " +
                                                                     " INNER JOIN " + prefKernel + "bc_types t ON t.id = rf.id_bc_type " +
                             " WHERE rf.id_bc_reestr = " + idReestr;
                ret = ExecRead(connDb, out reader, sql, true);
                if (!ret.result) throw new Exception(ret.text);

                //Список существующих таблиц fsmr_fin_YY:fn_sended
                int beginYear = Points.BeginWork.year_ - 2000,
                        endYear = DateTime.Now.Year - 2000;
                for (var j = beginYear; j <= endYear; j++)
                {
                    string finTable = Points.Pref + "_fin_" + j + DBManager.tableDelimiter + "fn_sended";
                    if (TempTableInWebCashe(connDb, finTable))
                        listDate.Add(j);
                }

                while (reader.Read())
                {
                    int id = reader["id"] != DBNull.Value ? Convert.ToInt32(reader["id"]) : 0;
                    int nzpExc = reader["nzp_exc"] != DBNull.Value ? Convert.ToInt32(reader["nzp_exc"]) : 0;
                    string bankName = reader["bank"] != DBNull.Value ? reader["bank"].ToString().Trim() : string.Empty;
                    string fileName = reader["file_name"] != DBNull.Value ? reader["file_name"].ToString().Trim() : string.Empty;
                    string typeFormat = reader["type_format"] != DBNull.Value ? reader["type_format"].ToString().Trim() : string.Empty;
                    Boolean noProcessedBank = reader["is_treaster"] != DBNull.Value &&
                                         Convert.ToInt32(reader["is_treaster"]) == 1;

                    Decimal sum = 0m;
                    foreach (var yy in listDate)
                    {
                        sql = " SELECT SUM(sum_send) " +
                              " FROM " + Points.Pref + "_fin_" + yy + tableDelimiter + "fn_sended " +
                              " WHERE id_bc_file = " + id;
                        var value = ExecScalar(connDb, sql, out ret, true);
                        sum += value != null && value != DBNull.Value ? Convert.ToDecimal(value) : 0m;
                    }

                    filesUploading.Add(new FilesUploadingOnWebBC
                    {
                        Id = id,
                        Format = new FormatBC { Name = typeFormat },
                        Bank = new Bank { bank = bankName },
                        FileName = fileName,
                        NzpExc = nzpExc,
                        IsTreaster = noProcessedBank,
                        SumTransfer = sum
                    });

                }
                reader.Close();

                //AddNumeration(filesUploading);
                ret.tag = filesUploading.Count;
                if (rows != 0) filesUploading = filesUploading.Skip(skip).Take(rows).ToList();
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetFilesUploading : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = string.IsNullOrEmpty(ret.text) ? "Ошибка при получении списка файлов выгрузки" : ret.text;
                ret.result = false;
            }
            finally
            {
                if (connDb != null)
                    connDb.Close();
            }
            return filesUploading;
        }

        #endregion

		public Returns UploadChangesServSupp(ReestrChangesServSupp finder) {
			if (finder.dat_month == "") return new Returns(false, "Не задан расчетный месяц", -1);
			DateTime datmonth;
			if (!DateTime.TryParse(finder.dat_month, out datmonth)) return new Returns(false, "Не задан расчетный месяц", -1);

			string table_reestr_changes_serv_supp = Points.Pref + "_data" + tableDelimiter + "reestr_changes_serv_supp";

			#region подключение к базе
			string conn_kernel = Points.GetConnByPref(Points.Pref);
			IDbConnection conn_db = GetConnection(conn_kernel);
			Returns ret = OpenDb(conn_db, true);
			if (!ret.result) return ret;
			#endregion

			#region определение локального пользователя
            int nzpUser = finder.nzp_user;
            
            /*DbWorkUser db = new DbWorkUser();
			int nzpUser = db.GetLocalUser(conn_db, finder, out ret);
			db.Close();
			if (!ret.result)
			{
				conn_db.Close();
				return ret;
			}*/
			#endregion

			//запись в реестр о формировании файла
			string sql = "insert into " + table_reestr_changes_serv_supp + " (dat_month,status,uploaded_on,uploaded_by) " +
				"values ('" + finder.dat_month + "'," + ExcelUtility.Statuses.InProcess.GetHashCode() +
#if PG
 ",now(),"
#else
				",current,"
#endif
 + nzpUser + ")";
			ret = ExecSQL(conn_db, sql, true);
			if (!ret.result)
			{
				conn_db.Close();
				return ret;
			}
			int nzp_reestr = GetSerialValue(conn_db);

			ExcelRepClient dbRep = new ExcelRepClient();
			ret = dbRep.AddMyFile(new ExcelUtility()
			{
				nzp_user = finder.nzp_user,
				status = ExcelUtility.Statuses.InProcess,
				is_shared = 1,
				rep_name = "Протокол загрузки ежедневных файлов сверки"
			});
			dbRep.Close();
			if (!ret.result)
			{
				return ret;
			}
			int nzpExc = ret.tag;


			string table_upd_changes_serv_supp = Points.Pref + "_fin_" + (datmonth.Year - 2000) + tableDelimiter + "upd_changes_serv_supp";
			DateTime datmonth_pred = datmonth.AddMonths(-1);
			string table_upd_changes_serv_supp_prev = Points.Pref + "_fin_" + (datmonth_pred.Year - 2000) + tableDelimiter + "upd_changes_serv_supp";

			string temp_upd_changes_serv_supp_cur = "temp_upd_changes_serv_supp_" + datmonth.Month;
			string temp_upd_changes_serv_supp_pred = "temp_upd_changes_serv_supp_" + datmonth_pred.Month;

			sql = "delete from " + table_upd_changes_serv_supp + " where month_=" + datmonth.Month;
			ret = ExecSQL(conn_db, sql, true);
			if (!ret.result)
			{
				sql = "update " + table_reestr_changes_serv_supp + " set status= " + ExcelUtility.Statuses.Failed.GetHashCode() +
						", comment = " + Utils.EStrNull("Ошибка в процессе выполнения") +
						" where nzp_reestr = " + nzp_reestr;
				ret = ExecSQL(conn_db, sql, true);
				conn_db.Close();
				return ret;
			}

			string temp_changes_serv_supp = "changes_serv_supp";
			ExecSQL(conn_db, " Drop table " + temp_changes_serv_supp, false);
			if (!TableInWebCashe(conn_db, temp_changes_serv_supp))
			{
				#region создать таблицу webdata:temp_changes_serv_supp
				sql = " CREATE temp TABLE " + temp_changes_serv_supp + " ( " +
				"   nzp_serv INTEGER, " +
				"   service CHAR(100), " +
				"   nzp_supp INTEGER, " +
				"   name_supp CHAR(100), " +
				"   inn CHAR(12), " +
				"   kpp CHAR(9), " +
				"   rchet CHAR(20), " +
				"   bik CHAR(9), " +
				"   month_ INTEGER " +
#if PG
 " )";
#else
	" ) with no log";
#endif
				#endregion
				ret = ExecSQL(conn_db, sql, true);
				if (!ret.result)
				{
					sql = "update " + table_reestr_changes_serv_supp + " set status= " + ExcelUtility.Statuses.Failed.GetHashCode() +
						", comment = " + Utils.EStrNull("Ошибка в процессе выполнения") +
						" where nzp_reestr = " + nzp_reestr;
					ret = ExecSQL(conn_db, sql, true);
					conn_db.Close();
					return ret;
				}
			}

			IDataReader reader;
			DbTables tables = new DbTables(conn_db);
			string table_charge = "";
			try
			{
				string sql2 = "";
				foreach (_Point zap in Points.PointList)
				{
					table_charge = zap.pref + "_charge_" + (datmonth.Year - 2000).ToString() +
#if PG

#else
   "@" + DBManager.getServer(conn_db) +
#endif
 tableDelimiter + "charge_" + datmonth.Month.ToString("00");
					if (!TempTableInWebCashe(conn_db, table_charge)) continue;
#if PG
					if (sql2 == "") sql2 += " select distinct nzp_serv, nzp_supp," + datmonth.Month + " from " + table_charge + " where dat_charge is null and nzp_serv <> 1";
					else sql2 += " union select distinct nzp_serv, nzp_supp, " + datmonth.Month + " from " + table_charge + " where dat_charge is null and  nzp_serv <> 1";
#else
					if (sql2 == "") sql2 += " select unique nzp_serv, nzp_supp," + datmonth.Month + " from " + table_charge + " where dat_charge is null and  nzp_serv <> 1";
					else sql2 += " union select unique nzp_serv, nzp_supp, " + datmonth.Month + " from " + table_charge + " where dat_charge is null and  nzp_serv <> 1";                   
#endif
				}

#if PG
				sql = " insert into " + temp_changes_serv_supp + " (nzp_serv, nzp_supp, month_) " + sql2;
#else
				sql = " insert into " + temp_changes_serv_supp + " (nzp_serv, nzp_supp, month_) select * from( " + sql2 + ")";
#endif
				ret = ExecSQL(conn_db, sql, true);
				if (!ret.result)
				{
					sql = "update " + table_reestr_changes_serv_supp + " set status= " + ExcelUtility.Statuses.Failed.GetHashCode() +
						", comment = " + Utils.EStrNull("Ошибка в процессе выполнения") +
						" where nzp_reestr = " + nzp_reestr;
					ret = ExecSQL(conn_db, sql, true);
					conn_db.Close();
					return ret;
				}

#if PG
				sql = "update " + temp_changes_serv_supp + " set " +
					"service = (select s.service " +
									" from " + tables.services + " s, " + tables.supplier + " sp, " + tables.payer + " p " +
									" left outer join " + tables.fn_bank + " fn_b on fn_b.nzp_payer = p.nzp_payer and fn_b.is_main = 1 " +
									" where sp.nzp_supp = p.nzp_supp " +
									" and s.nzp_serv= " + temp_changes_serv_supp + ".nzp_serv and sp.nzp_supp = " + temp_changes_serv_supp + ".nzp_supp)," +
					"name_supp = (select sp.name_supp " +
									" from " + tables.services + " s, " + tables.supplier + " sp, " + tables.payer + " p" +
									" left outer join " + tables.fn_bank + " fn_b on fn_b.nzp_payer = p.nzp_payer and fn_b.is_main = 1 " +
									" where sp.nzp_supp = p.nzp_supp and fn_b.nzp_payer = p.nzp_payer and fn_b.is_main = 1" +
									" and s.nzp_serv= " + temp_changes_serv_supp + ".nzp_serv and sp.nzp_supp = " + temp_changes_serv_supp + ".nzp_supp)," +
					"inn = (select  p.inn " +
									" from " + tables.services + " s, " + tables.supplier + " sp, " + tables.payer + " p" +
									" left outer join " + tables.fn_bank + " fn_b on fn_b.nzp_payer = p.nzp_payer and fn_b.is_main = 1 " +
									" where sp.nzp_supp = p.nzp_supp and fn_b.nzp_payer = p.nzp_payer and fn_b.is_main = 1" +
									" and s.nzp_serv= " + temp_changes_serv_supp + ".nzp_serv and sp.nzp_supp = " + temp_changes_serv_supp + ".nzp_supp)," +
					"kpp = (select p.kpp " +
									" from " + tables.services + " s, " + tables.supplier + " sp, " + tables.payer + " p" +
									" left outer join " + tables.fn_bank + " fn_b on fn_b.nzp_payer = p.nzp_payer and fn_b.is_main = 1 " +
									" where sp.nzp_supp = p.nzp_supp and fn_b.nzp_payer = p.nzp_payer and fn_b.is_main = 1" +
									" and s.nzp_serv= " + temp_changes_serv_supp + ".nzp_serv and sp.nzp_supp = " + temp_changes_serv_supp + ".nzp_supp)," +
					"bik = (select  fn_b.bik " +
									" from " + tables.services + " s, " + tables.supplier + " sp, " + tables.payer + " p" +
									" left outer join " + tables.fn_bank + " fn_b on fn_b.nzp_payer = p.nzp_payer and fn_b.is_main = 1 " +
									" where sp.nzp_supp = p.nzp_supp and fn_b.nzp_payer = p.nzp_payer and fn_b.is_main = 1" +
									" and s.nzp_serv= " + temp_changes_serv_supp + ".nzp_serv and sp.nzp_supp = " + temp_changes_serv_supp + ".nzp_supp), " +
					"rchet = (select fn_b.rcount " +
									" from " + tables.services + " s, " + tables.supplier + " sp, " + tables.payer + " p" +
									" left outer join " + tables.fn_bank + " fn_b on fn_b.nzp_payer = p.nzp_payer and fn_b.is_main = 1 " +
									" where sp.nzp_supp = p.nzp_supp and fn_b.nzp_payer = p.nzp_payer and fn_b.is_main = 1" +
									" and s.nzp_serv= " + temp_changes_serv_supp + ".nzp_serv and sp.nzp_supp = " + temp_changes_serv_supp + ".nzp_supp)";
#else
				sql = "update "+ temp_changes_serv_supp + " set (service,name_supp,inn,kpp,bik, rchet) = "+             
					" ((select s.service, sp.name_supp, p.inn, p.kpp, fn_b.bik, fn_b.rcount "+ 
					 " from "+tables.services+" s, "+tables.supplier+" sp, "+tables.payer+" p, "+ 
					"  outer "+tables.fn_bank+" fn_b "+ 
					 " where sp.nzp_supp = p.nzp_supp and fn_b.nzp_payer = p.nzp_payer and fn_b.is_main = 1"+ 
					 " and s.nzp_serv= "+ temp_changes_serv_supp + ".nzp_serv and sp.nzp_supp = "+ temp_changes_serv_supp + ".nzp_supp))";
#endif
				ret = ExecSQL(conn_db, sql, true);
				if (!ret.result)
				{
					sql = "update " + table_reestr_changes_serv_supp + " set status= " + ExcelUtility.Statuses.Failed.GetHashCode() +
						", comment = " + Utils.EStrNull("Ошибка в процессе выполнения") +
						" where nzp_reestr = " + nzp_reestr;
					ret = ExecSQL(conn_db, sql, true);
					conn_db.Close();
					return ret;
				}

				sql = "insert into  " + table_upd_changes_serv_supp + " (nzp_reestr, nzp_serv, nzp_supp, service, name_supp, inn, kpp, bik, rchet, month_) " +
					" select  " + nzp_reestr + ", nzp_serv, nzp_supp, service, name_supp, inn, kpp, bik, rchet, month_ from " + temp_changes_serv_supp;
				ret = ExecSQL(conn_db, sql, true);
				if (!ret.result)
				{
					sql = "update " + table_reestr_changes_serv_supp + " set status= " + ExcelUtility.Statuses.Failed.GetHashCode() +
						", comment = " + Utils.EStrNull("Ошибка в процессе выполнения") +
						" where nzp_reestr = " + nzp_reestr;
					ret = ExecSQL(conn_db, sql, true);
					conn_db.Close();
					return ret;
				}

				DropTempTables(temp_changes_serv_supp, conn_db);
				DropTempTables(temp_upd_changes_serv_supp_cur, conn_db);
				DropTempTables(temp_upd_changes_serv_supp_pred, conn_db);

#if PG
				sql = " select * into temp " + temp_upd_changes_serv_supp_cur + " from " + table_upd_changes_serv_supp +
					" where month_=" + datmonth.Month;
#else
				sql = " select * from "+table_upd_changes_serv_supp+" where month_="+datmonth.Month+
					  " into temp "+temp_upd_changes_serv_supp_cur+" with no log";
#endif
				ret = ExecSQL(conn_db, sql, true);
				if (!ret.result)
				{
					sql = "update " + table_reestr_changes_serv_supp + " set status= " + ExcelUtility.Statuses.Failed.GetHashCode() +
						", comment = " + Utils.EStrNull("Ошибка в процессе выполнения") +
						" where nzp_reestr = " + nzp_reestr;
					ret = ExecSQL(conn_db, sql, true);
					conn_db.Close();
					return ret;
				}

#if PG
				sql = "select *  into temp " + temp_upd_changes_serv_supp_pred + " from " + table_upd_changes_serv_supp_prev +
								   " where month_=" + datmonth_pred.Month + " and coalesce(status,0)<>" +
								   UpdChangesServSupp.ChangedStatuses.Deleted.GetHashCode();
#else
				sql = "select * from " + table_upd_changes_serv_supp_prev + 
					" where month_="+datmonth_pred.Month+" and nvl(status,0)<>"+
					UpdChangesServSupp.ChangedStatuses.Deleted.GetHashCode()+
					" into temp "+temp_upd_changes_serv_supp_pred +" with no log";
#endif
				ret = ExecSQL(conn_db, sql, true);
				if (!ret.result)
				{
					sql = "update " + table_reestr_changes_serv_supp + " set status= " + ExcelUtility.Statuses.Failed.GetHashCode() +
						", comment = " + Utils.EStrNull("Ошибка в процессе выполнения") +
						" where nzp_reestr = " + nzp_reestr;
					ret = ExecSQL(conn_db, sql, true);
					conn_db.Close();
					return ret;
				}

				//  неизмененный
#if PG
				sql = "update " + table_upd_changes_serv_supp + " set status = " + UpdChangesServSupp.ChangedStatuses.NotChanges.GetHashCode() +
									 " where ( " +
									 " select count(*) from " + temp_upd_changes_serv_supp_cur + " a," + temp_upd_changes_serv_supp_pred + " b " +
									 " where a.nzp_serv = b.nzp_serv and a.nzp_supp = b.nzp_supp   " +
									 " and trim(upper(coalesce(a.name_supp,''))) = trim(upper(coalesce(b.name_supp,'')))  " +
									 " and trim(upper(coalesce(a.inn,''))) = trim(upper(coalesce(b.inn,'')))  " +
									 " and trim(upper(coalesce(a.kpp,''))) = trim(upper(coalesce(b.kpp,''))) and trim(upper(coalesce(a.rchet,''))) = trim(upper(coalesce(b.rchet,''))) " +
									 " and trim(upper(coalesce(a.bik,''))) = trim(upper(coalesce(b.bik,''))) and trim(upper(coalesce(a.service,''))) = trim(upper(coalesce(b.service,''))) " +
									 " and a.nzp_serv = " + table_upd_changes_serv_supp + ".nzp_serv and a.nzp_supp=" + table_upd_changes_serv_supp + ".nzp_supp  " +
									  " )=1 and month_=" + datmonth.Month;
#else
				sql = "update "+table_upd_changes_serv_supp+" set status = " + UpdChangesServSupp.ChangedStatuses.NotChanges.GetHashCode() + 
					 " where ( "+
					 " select count(*) from "+temp_upd_changes_serv_supp_cur+" a,"+temp_upd_changes_serv_supp_pred+" b "+
					 " where a.nzp_serv = b.nzp_serv and a.nzp_supp = b.nzp_supp   "+
					 " and trim(upper(nvl(a.name_supp,''))) = trim(upper(nvl(b.name_supp,'')))  "+
					 " and trim(upper(nvl(a.inn,''))) = trim(upper(nvl(b.inn,'')))  "+
					 " and trim(upper(nvl(a.kpp,''))) = trim(upper(nvl(b.kpp,''))) and trim(upper(nvl(a.rchet,''))) = trim(upper(nvl(b.rchet,''))) "+
					 " and trim(upper(nvl(a.bik,''))) = trim(upper(nvl(b.bik,''))) and trim(upper(nvl(a.service,''))) = trim(upper(nvl(b.service,''))) "+
					 " and a.nzp_serv = " + table_upd_changes_serv_supp + ".nzp_serv and a.nzp_supp=" + table_upd_changes_serv_supp + ".nzp_supp  " +
					  " )=1 and month_="+datmonth.Month;
#endif
				ret = ExecSQL(conn_db, sql, true);
				if (!ret.result)
				{
					sql = "update " + table_reestr_changes_serv_supp + " set status= " + ExcelUtility.Statuses.Failed.GetHashCode() +
						", comment = " + Utils.EStrNull("Ошибка в процессе выполнения") +
						" where nzp_reestr = " + nzp_reestr;
					ret = ExecSQL(conn_db, sql, true);
					conn_db.Close();
					return ret;
				}

				DropTempTables(temp_upd_changes_serv_supp_cur, conn_db);
				DropTempTables(temp_upd_changes_serv_supp_pred, conn_db);

				//если статус не определен и точно -  неизмененные
#if PG
				sql = "select * into temp " + temp_upd_changes_serv_supp_cur + " from " + table_upd_changes_serv_supp + " where coalesce(status,0)<>" + UpdChangesServSupp.ChangedStatuses.NotChanges.GetHashCode() +
									" and month_=" + datmonth.Month;
#else
				sql = "select * from "+table_upd_changes_serv_supp+" where nvl(status,0)<>"+UpdChangesServSupp.ChangedStatuses.NotChanges.GetHashCode()+
					" and month_="+datmonth.Month+" into temp "+temp_upd_changes_serv_supp_cur+" with no log"; 
#endif
				ret = ExecSQL(conn_db, sql, true);
				if (!ret.result)
				{
					sql = "update " + table_reestr_changes_serv_supp + " set status= " + ExcelUtility.Statuses.Failed.GetHashCode() +
						", comment = " + Utils.EStrNull("Ошибка в процессе выполнения") +
						" where nzp_reestr = " + nzp_reestr;
					ret = ExecSQL(conn_db, sql, true);
					conn_db.Close();
					return ret;
				}

#if PG
				sql = "select * into temp " + temp_upd_changes_serv_supp_pred + " from " +
					table_upd_changes_serv_supp_prev + " where  month_=" + datmonth_pred.Month +
					" and coalesce(status,0)<>" + UpdChangesServSupp.ChangedStatuses.Deleted.GetHashCode();
#else
				sql = "select * from "+table_upd_changes_serv_supp_prev+" where  month_="+datmonth_pred.Month+" and nvl(status,0)<>"+UpdChangesServSupp.ChangedStatuses.Deleted.GetHashCode()+
					 " into temp "+temp_upd_changes_serv_supp_pred+" with no log";
#endif
				ret = ExecSQL(conn_db, sql, true);
				if (!ret.result)
				{
					sql = "update " + table_reestr_changes_serv_supp + " set status= " + ExcelUtility.Statuses.Failed.GetHashCode() +
						", comment = " + Utils.EStrNull("Ошибка в процессе выполнения") +
						" where nzp_reestr = " + nzp_reestr;
					ret = ExecSQL(conn_db, sql, true);
					conn_db.Close();
					return ret;
				}

				// новый
#if PG
				sql = "update " + table_upd_changes_serv_supp + " set status = " + UpdChangesServSupp.ChangedStatuses.New.GetHashCode() +
								  " where ( " +
								  " select count(*) from " + temp_upd_changes_serv_supp_cur + " a " +
								  " where a.nzp_serv||'_'||a.nzp_supp not in (select b.nzp_serv||'_'||b.nzp_supp from " + temp_upd_changes_serv_supp_pred + " b)   " +
								  " and a.nzp_serv = " + table_upd_changes_serv_supp + ".nzp_serv and a.nzp_supp=" + table_upd_changes_serv_supp + ".nzp_supp )=1  " +
								  " and coalesce(" + table_upd_changes_serv_supp + ".status,0) <>" + UpdChangesServSupp.ChangedStatuses.NotChanges.GetHashCode() + "  and month_=" + datmonth.Month;
#else
  sql = "update "+table_upd_changes_serv_supp+" set status = "+UpdChangesServSupp.ChangedStatuses.New.GetHashCode() +
					" where ( "+
					" select count(*) from "+temp_upd_changes_serv_supp_cur+" a "+
					" where a.nzp_serv||'_'||a.nzp_supp not in (select b.nzp_serv||'_'||b.nzp_supp from "+temp_upd_changes_serv_supp_pred+" b)   "+  
					" and a.nzp_serv = "+table_upd_changes_serv_supp+".nzp_serv and a.nzp_supp="+table_upd_changes_serv_supp+".nzp_supp )=1  "+
					" and nvl("+table_upd_changes_serv_supp+".status,0) <>"+UpdChangesServSupp.ChangedStatuses.NotChanges.GetHashCode()+"  and month_="+datmonth.Month;
#endif
				ret = ExecSQL(conn_db, sql, true);
				if (!ret.result)
				{
					sql = "update " + table_reestr_changes_serv_supp + " set status= " + ExcelUtility.Statuses.Failed.GetHashCode() +
						", comment = " + Utils.EStrNull("Ошибка в процессе выполнения") +
						" where nzp_reestr = " + nzp_reestr;
					ret = ExecSQL(conn_db, sql, true);
					conn_db.Close();
					return ret;
				}

				DropTempTables(temp_upd_changes_serv_supp_cur, conn_db);
				DropTempTables(temp_upd_changes_serv_supp_pred, conn_db);

#if PG
				sql = "select * into temp " + temp_upd_changes_serv_supp_cur + " from " + table_upd_changes_serv_supp +
					" where coalesce(status,0) =0 and month_=" + datmonth.Month;
#else
				sql = "select * from "+table_upd_changes_serv_supp+" where nvl(status,0) =0 and month_="+datmonth.Month +
					" into temp "+temp_upd_changes_serv_supp_cur+" with no log";
#endif
				ret = ExecSQL(conn_db, sql, true);
				if (!ret.result)
				{
					sql = "update " + table_reestr_changes_serv_supp + " set status= " + ExcelUtility.Statuses.Failed.GetHashCode() +
						", comment = " + Utils.EStrNull("Ошибка в процессе выполнения") +
						" where nzp_reestr = " + nzp_reestr;
					ret = ExecSQL(conn_db, sql, true);
					conn_db.Close();
					return ret;
				}

#if PG
				sql = "select * into temp " + temp_upd_changes_serv_supp_pred + " from " + table_upd_changes_serv_supp_prev + " where month_=" + datmonth_pred.Month +
					" and coalesce(status,0)<>" + UpdChangesServSupp.ChangedStatuses.Deleted.GetHashCode();
#else
				sql = "select * from "+table_upd_changes_serv_supp_prev+" where month_="+datmonth_pred.Month+" and nvl(status,0)<>"+UpdChangesServSupp.ChangedStatuses.Deleted.GetHashCode()+
					" into temp "+temp_upd_changes_serv_supp_pred+" with no log ";
#endif
				ret = ExecSQL(conn_db, sql, true);
				if (!ret.result)
				{
					sql = "update " + table_reestr_changes_serv_supp + " set status= " + ExcelUtility.Statuses.Failed.GetHashCode() +
						", comment = " + Utils.EStrNull("Ошибка в процессе выполнения") +
						" where nzp_reestr = " + nzp_reestr;
					ret = ExecSQL(conn_db, sql, true);
					conn_db.Close();
					return ret;
				}

				//изменен
#if PG
				sql = "update " + table_upd_changes_serv_supp + " set status = " + UpdChangesServSupp.ChangedStatuses.Changes.GetHashCode() +
								   " where month_=" + datmonth.Month + " and " +
								   "(select count(*) from " + temp_upd_changes_serv_supp_cur + " a," + temp_upd_changes_serv_supp_pred + " b " +
								   " where a.nzp_serv = b.nzp_serv and a.nzp_supp = b.nzp_supp  and a.nzp_serv = " + table_upd_changes_serv_supp + ".nzp_serv and a.nzp_supp=" + table_upd_changes_serv_supp + ".nzp_supp  " +
								   " and ( " +
								   " trim(upper(coalesce(a.name_supp,''))) <> trim(upper(coalesce(b.name_supp,'')))  " +
								   " or trim(upper(coalesce(a.inn,''))) <> trim(upper(coalesce(b.inn,'')))  " +
								   " or trim(upper(coalesce(a.kpp,''))) <> trim(upper(coalesce(b.kpp,''))) or trim(upper(coalesce(a.rchet,''))) <> trim(upper(coalesce(b.rchet,''))) " +
								   " or trim(upper(coalesce(a.bik,''))) <> trim(upper(coalesce(b.bik,''))) or trim(upper(coalesce(a.service,''))) <> trim(upper(coalesce(b.service,'')))   " +
								   " )) =1 ";
#else
 sql = "update "+table_upd_changes_serv_supp+" set status = "+UpdChangesServSupp.ChangedStatuses.Changes.GetHashCode() +
					" where month_="+datmonth.Month+" and "+
					"(select count(*) from "+temp_upd_changes_serv_supp_cur+" a,"+temp_upd_changes_serv_supp_pred+" b "+
					" where a.nzp_serv = b.nzp_serv and a.nzp_supp = b.nzp_supp  and a.nzp_serv = " + table_upd_changes_serv_supp + ".nzp_serv and a.nzp_supp=" + table_upd_changes_serv_supp + ".nzp_supp  " +
					" and ( "+
					" trim(upper(nvl(a.name_supp,''))) <> trim(upper(nvl(b.name_supp,'')))  "+
					" or trim(upper(nvl(a.inn,''))) <> trim(upper(nvl(b.inn,'')))  "+
					" or trim(upper(nvl(a.kpp,''))) <> trim(upper(nvl(b.kpp,''))) or trim(upper(nvl(a.rchet,''))) <> trim(upper(nvl(b.rchet,''))) "+
					" or trim(upper(nvl(a.bik,''))) <> trim(upper(nvl(b.bik,''))) or trim(upper(nvl(a.service,''))) <> trim(upper(nvl(b.service,'')))   "+
					" )) =1 ";
#endif
				ret = ExecSQL(conn_db, sql, true);
				if (!ret.result)
				{
					sql = "update " + table_reestr_changes_serv_supp + " set status= " + ExcelUtility.Statuses.Failed.GetHashCode() +
						", comment = " + Utils.EStrNull("Ошибка в процессе выполнения") +
						" where nzp_reestr = " + nzp_reestr;
					ret = ExecSQL(conn_db, sql, true);
					conn_db.Close();
					return ret;
				}

				DropTempTables(temp_upd_changes_serv_supp_cur, conn_db);
				DropTempTables(temp_upd_changes_serv_supp_pred, conn_db);

#if PG
				sql = " select * into temp " + temp_upd_changes_serv_supp_cur + " from " + table_upd_changes_serv_supp +
					" where month_=" + datmonth.Month;
#else
sql = " select * from "+table_upd_changes_serv_supp+" where month_="+datmonth.Month+
					" into temp "+temp_upd_changes_serv_supp_cur+" with no log"; 
#endif
				ret = ExecSQL(conn_db, sql, true);
				if (!ret.result)
				{
					sql = "update " + table_reestr_changes_serv_supp + " set status= " + ExcelUtility.Statuses.Failed.GetHashCode() +
						", comment = " + Utils.EStrNull("Ошибка в процессе выполнения") +
						" where nzp_reestr = " + nzp_reestr;
					ret = ExecSQL(conn_db, sql, true);
					conn_db.Close();
					return ret;
				}

#if PG
				sql = "select * into temp " + temp_upd_changes_serv_supp_pred + " from " + table_upd_changes_serv_supp_prev + " where month_=" + datmonth_pred.Month +
								   " and coalesce(status,0)<>" + UpdChangesServSupp.ChangedStatuses.Deleted.GetHashCode();
#else
				sql = "select * from "+table_upd_changes_serv_supp_prev+" where month_="+datmonth_pred.Month+
					" and nvl(status,0)<>"+UpdChangesServSupp.ChangedStatuses.Deleted.GetHashCode()+
					" into temp "+temp_upd_changes_serv_supp_pred+" with no log"; 
#endif
				ret = ExecSQL(conn_db, sql, true);
				if (!ret.result)
				{
					sql = "update " + table_reestr_changes_serv_supp + " set status= " + ExcelUtility.Statuses.Failed.GetHashCode() +
						", comment = " + Utils.EStrNull("Ошибка в процессе выполнения") +
						" where nzp_reestr = " + nzp_reestr;
					ret = ExecSQL(conn_db, sql, true);
					conn_db.Close();
					return ret;
				}

				//УДАЛЕН
#if PG
				sql = "insert into " + table_upd_changes_serv_supp +
								  " (nzp_reestr, nzp_serv, nzp_supp, service, name_supp, inn, kpp, bik, rchet, month_,status) " +
								  " select " + nzp_reestr + ", nzp_serv, nzp_supp, service, name_supp, inn, kpp, bik, rchet," + datmonth.Month + "," +
								  UpdChangesServSupp.ChangedStatuses.Deleted.GetHashCode() + " from  " + table_upd_changes_serv_supp +
								  " where ( " +
								  " select count(*) from " + temp_upd_changes_serv_supp_pred + " a " +
								  " where a.nzp_serv||'_'||a.nzp_supp not in (select b.nzp_serv||'_'||b.nzp_supp from " + temp_upd_changes_serv_supp_cur + " b) " +
								  " and a.nzp_serv = " + table_upd_changes_serv_supp + ".nzp_serv and a.nzp_supp=" + table_upd_changes_serv_supp + ".nzp_supp " +
								  " and coalesce(a.status,0) not in (" + UpdChangesServSupp.ChangedStatuses.Deleted.GetHashCode() + ") " +
								  " )=1 ";
#else
  sql = "insert into "+table_upd_changes_serv_supp+
					" (nzp_reestr, nzp_serv, nzp_supp, service, name_supp, inn, kpp, bik, rchet, month_,status) "+
					" select "+nzp_reestr+", nzp_serv, nzp_supp, service, name_supp, inn, kpp, bik, rchet,"+datmonth.Month+","+
					UpdChangesServSupp.ChangedStatuses.Deleted.GetHashCode() +" from  "+table_upd_changes_serv_supp+
					" where ( " +
					" select count(*) from "+temp_upd_changes_serv_supp_pred+" a " +
					" where a.nzp_serv||'_'||a.nzp_supp not in (select b.nzp_serv||'_'||b.nzp_supp from "+temp_upd_changes_serv_supp_cur+" b) " +
					" and a.nzp_serv = "+table_upd_changes_serv_supp+".nzp_serv and a.nzp_supp="+table_upd_changes_serv_supp+".nzp_supp " +
					" and nvl(a.status,0) not in ("+UpdChangesServSupp.ChangedStatuses.Deleted.GetHashCode()+") " +
					" )=1 ";
#endif
				ret = ExecSQL(conn_db, sql, true);
				if (!ret.result)
				{
					sql = "update " + table_reestr_changes_serv_supp + " set status= " + ExcelUtility.Statuses.Failed.GetHashCode() +
						", comment = " + Utils.EStrNull("Ошибка в процессе выполнения") +
						" where nzp_reestr = " + nzp_reestr;
					ret = ExecSQL(conn_db, sql, true);
					conn_db.Close();
					return ret;
				}

				DropTempTables(temp_upd_changes_serv_supp_cur, conn_db);
				DropTempTables(temp_upd_changes_serv_supp_pred, conn_db);

				//измененным записям добавляем со статусом удалены, а измененныи проставляем статус новые 
#if PG
				sql = " insert into " + table_upd_changes_serv_supp + " (nzp_reestr, nzp_serv, nzp_supp, service, name_supp, inn, kpp, bik, rchet, month_,status) " +
									" select " + nzp_reestr + ", nzp_serv, nzp_supp, service, name_supp, inn, kpp, bik, rchet," + datmonth.Month + "," + UpdChangesServSupp.ChangedStatuses.Deleted.GetHashCode() +
									" from  " + table_upd_changes_serv_supp_prev + " a where " +
									" a.month_=" + datmonth_pred.Month + " and a.nzp_serv = (select b.nzp_serv from " + table_upd_changes_serv_supp +
									" b where b.month_=" + datmonth.Month + "  and  coalesce(b.status,0)=" + UpdChangesServSupp.ChangedStatuses.Changes.GetHashCode() + " and a.nzp_serv = b.nzp_serv) " +
									" and a.nzp_supp = (select b.nzp_supp from " + table_upd_changes_serv_supp + " b where b.month_=" + datmonth.Month + "  and  coalesce(b.status,0)=" + UpdChangesServSupp.ChangedStatuses.Changes.GetHashCode() +
									" and a.nzp_supp = b.nzp_supp)";
#else
  sql = " insert into "+table_upd_changes_serv_supp+" (nzp_reestr, nzp_serv, nzp_supp, service, name_supp, inn, kpp, bik, rchet, month_,status) "+ 
					  " select "+nzp_reestr+", nzp_serv, nzp_supp, service, name_supp, inn, kpp, bik, rchet,"+datmonth.Month+","+UpdChangesServSupp.ChangedStatuses.Deleted.GetHashCode()+
					  " from  "+table_upd_changes_serv_supp_prev+" a where "+
					  " a.month_="+datmonth_pred.Month+" and a.nzp_serv = (select b.nzp_serv from "+table_upd_changes_serv_supp+
					  " b where b.month_="+datmonth.Month+"  and  nvl(b.status,0)="+UpdChangesServSupp.ChangedStatuses.Changes.GetHashCode()+" and a.nzp_serv = b.nzp_serv) "+
					  " and a.nzp_supp = (select b.nzp_supp from "+table_upd_changes_serv_supp+" b where b.month_="+datmonth.Month+"  and  nvl(b.status,0)="+UpdChangesServSupp.ChangedStatuses.Changes.GetHashCode()+
					  " and a.nzp_supp = b.nzp_supp)";
#endif
				ret = ExecSQL(conn_db, sql, true);
				if (!ret.result)
				{
					sql = "update " + table_reestr_changes_serv_supp + " set status= " + ExcelUtility.Statuses.Failed.GetHashCode() +
						", comment = " + Utils.EStrNull("Ошибка в процессе выполнения") +
						" where nzp_reestr = " + nzp_reestr;
					ret = ExecSQL(conn_db, sql, true);
					conn_db.Close();
					return ret;
				}

#if PG
				sql = " update " + table_upd_changes_serv_supp + " set status = " + UpdChangesServSupp.ChangedStatuses.New.GetHashCode() +
									 " where month_=" + datmonth.Month + " and coalesce(status,0)="
									 + UpdChangesServSupp.ChangedStatuses.Changes.GetHashCode();
#else
 sql = " update " + table_upd_changes_serv_supp + " set status = " + UpdChangesServSupp.ChangedStatuses.New.GetHashCode() +
					  " where month_=" + datmonth.Month + " and nvl(status,0)=" + UpdChangesServSupp.ChangedStatuses.Changes.GetHashCode();
#endif
				ret = ExecSQL(conn_db, sql, true);
				if (!ret.result)
				{
					sql = "update " + table_reestr_changes_serv_supp + " set status= " + ExcelUtility.Statuses.Failed.GetHashCode() +
						", comment = " + Utils.EStrNull("Ошибка в процессе выполнения") +
						" where nzp_reestr = " + nzp_reestr;
					ret = ExecSQL(conn_db, sql, true);
					conn_db.Close();
					return ret;
				}

				//всего неизмененных
				sql = "select count(*) from " +
					table_upd_changes_serv_supp + " where month_ = " + datmonth.Month +
#if PG
 " and coalesce(status,0) = " + UpdChangesServSupp.ChangedStatuses.NotChanges.GetHashCode();
#else
" and nvl(status,0) = "+UpdChangesServSupp.ChangedStatuses.NotChanges.GetHashCode();
#endif
				object count = ExecScalar(conn_db, sql, out ret, true);
				int notchanges;
				try { notchanges = Convert.ToInt32(count); }
				catch (Exception e)
				{
					sql = "update " + table_reestr_changes_serv_supp + " set status= " + ExcelUtility.Statuses.Failed.GetHashCode() +
						", comment = " + Utils.EStrNull("Ошибка в процессе выполнения") +
						" where nzp_reestr = " + nzp_reestr;
					ret = ExecSQL(conn_db, sql, true);
					ret = new Returns(false, "Ошибка при определении количества записей: " + (Constants.Debug ? e.Message : ""));
					MonitorLog.WriteLog("Ошибка UploadChangesServSupp " + (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
					conn_db.Close();
					return ret;
				}

				//статус не установился
				sql = "select count(*) from " +
					table_upd_changes_serv_supp + " where month_ = " + datmonth.Month +
#if PG
 " and coalesce(status,0) not in (" +
#else
  " and nvl(status,0) not in (" +
#endif
 UpdChangesServSupp.ChangedStatuses.NotChanges.GetHashCode() + "," +
					UpdChangesServSupp.ChangedStatuses.New.GetHashCode() + "," + UpdChangesServSupp.ChangedStatuses.Deleted.GetHashCode() + ")";
				object count2 = ExecScalar(conn_db, sql, out ret, true);
				int undefined;
				try { undefined = Convert.ToInt32(count2); }
				catch (Exception e)
				{
					sql = "update " + table_reestr_changes_serv_supp + " set status= " + ExcelUtility.Statuses.Failed.GetHashCode() +
						", comment = " + Utils.EStrNull("Ошибка в процессе выполнения") +
						" where nzp_reestr = " + nzp_reestr;
					ret = ExecSQL(conn_db, sql, true);
					ret = new Returns(false, "Ошибка при определении количества записей: " + (Constants.Debug ? e.Message : ""));
					MonitorLog.WriteLog("Ошибка UploadChangesServSupp " + (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
					conn_db.Close();
					return ret;
				}

				//всего записей
				sql = "select count(*) from " +
					table_upd_changes_serv_supp + " where month_ = " + datmonth.Month;
				object count3 = ExecScalar(conn_db, sql, out ret, true);
				int allrecords;
				try { allrecords = Convert.ToInt32(count3); }
				catch (Exception e)
				{
					sql = "update " + table_reestr_changes_serv_supp + " set status= " + ExcelUtility.Statuses.Failed.GetHashCode() +
						", comment = " + Utils.EStrNull("Ошибка в процессе выполнения") +
						" where nzp_reestr = " + nzp_reestr;
					ret = ExecSQL(conn_db, sql, true);
					ret = new Returns(false, "Ошибка при определении количества записей: " + (Constants.Debug ? e.Message : ""));
					MonitorLog.WriteLog("Ошибка UploadChangesServSupp " + (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
					conn_db.Close();
					return ret;
				}

				//статус удалено
				sql = "select count(*) from " +
					table_upd_changes_serv_supp + " where month_ = " + datmonth.Month +
#if PG
 " and coalesce(status,0) = " +
#else
   " and nvl(status,0) = " + 
#endif
 UpdChangesServSupp.ChangedStatuses.Deleted.GetHashCode();
				object count4 = ExecScalar(conn_db, sql, out ret, true);
				int deleted;
				try { deleted = Convert.ToInt32(count4); }
				catch (Exception e)
				{
					sql = "update " + table_reestr_changes_serv_supp + " set status= " + ExcelUtility.Statuses.Failed.GetHashCode() +
						", comment = " + Utils.EStrNull("Ошибка в процессе выполнения") +
						" where nzp_reestr = " + nzp_reestr;
					ret = ExecSQL(conn_db, sql, true);
					ret = new Returns(false, "Ошибка при определении количества записей: " + (Constants.Debug ? e.Message : ""));
					MonitorLog.WriteLog("Ошибка UploadChangesServSupp " + (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
					conn_db.Close();
					return ret;
				}

				//статус добавлено
				sql = "select count(*) from " +
					table_upd_changes_serv_supp + " where month_ = " + datmonth.Month +
#if PG
 " and coalesce(status,0) = "
#else
 " and nvl(status,0) = " 
#endif
 + UpdChangesServSupp.ChangedStatuses.New.GetHashCode();
				object count5 = ExecScalar(conn_db, sql, out ret, true);
				int added;
				try { added = Convert.ToInt32(count5); }
				catch (Exception e)
				{
					sql = "update " + table_reestr_changes_serv_supp + " set status= " + ExcelUtility.Statuses.Failed.GetHashCode() +
						", comment = " + Utils.EStrNull("Ошибка в процессе выполнения") +
						" where nzp_reestr = " + nzp_reestr;
					ret = ExecSQL(conn_db, sql, true);
					ret = new Returns(false, "Ошибка при определении количества записей: " + (Constants.Debug ? e.Message : ""));
					MonitorLog.WriteLog("Ошибка UploadChangesServSupp " + (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
					conn_db.Close();
					return ret;
				}

				List<UpdChangesServSupp> list = new List<UpdChangesServSupp>();

				sql = "select nzp_reestr, nzp_serv, nzp_supp, service, name_supp, inn, kpp, bik, rchet, month_,status from " +
					table_upd_changes_serv_supp + " where month_ = " + datmonth.Month +
#if PG
 " and coalesce(status,0) in (1,2)";
#else
" and nvl(status,0) in (1,2)";
#endif
				ret = ExecRead(conn_db, out reader, sql, true);
				if (!ret.result)
				{
					sql = "update " + table_reestr_changes_serv_supp + " set status= " + ExcelUtility.Statuses.Failed.GetHashCode() +
						", comment = " + Utils.EStrNull("Ошибка в процессе выполнения") +
						" where nzp_reestr = " + nzp_reestr;
					ret = ExecSQL(conn_db, sql, true);
					conn_db.Close();
					return ret;
				}
				while (reader.Read())
				{
					UpdChangesServSupp zap = new UpdChangesServSupp();
					if (reader["nzp_reestr"] != DBNull.Value) zap.nzp_reestr = Convert.ToInt32(reader["nzp_reestr"]);
					if (reader["nzp_serv"] != DBNull.Value) zap.nzp_serv = Convert.ToInt32(reader["nzp_serv"]);
					if (reader["nzp_supp"] != DBNull.Value) zap.nzp_supp = Convert.ToInt32(reader["nzp_supp"]);
					if (reader["service"] != DBNull.Value) zap.service = Convert.ToString(reader["service"]).Trim();
					if (reader["name_supp"] != DBNull.Value) zap.name_supp = Convert.ToString(reader["name_supp"]).Trim();
					if (reader["inn"] != DBNull.Value) zap.inn = Convert.ToString(reader["inn"]).Trim();
					if (reader["kpp"] != DBNull.Value) zap.kpp = Convert.ToString(reader["kpp"]).Trim();
					if (reader["bik"] != DBNull.Value) zap.bik = Convert.ToString(reader["bik"]).Trim();
					if (reader["rchet"] != DBNull.Value) zap.rchet = Convert.ToString(reader["rchet"]).Trim();
					if (reader["month_"] != DBNull.Value) zap.month_ = Convert.ToInt32(reader["month_"]);
					if (reader["status"] != DBNull.Value) zap.status = Convert.ToInt32(reader["status"]);

					list.Add(zap);
				}

				//сохранение результатов в файл
				int k = 1;
				string filename = "sbchange" + DateTime.Now.Year + DateTime.Now.Month.ToString("00") + DateTime.Now.Day.ToString("00");

				while (System.IO.File.Exists(Constants.ExcelDir + filename + k + ".txt")) k++;

				filename = filename + k + ".txt";
				string fullfilename = Constants.ExcelDir + filename;
				using (System.IO.StreamWriter file = new System.IO.StreamWriter(Constants.ExcelDir + filename, true, Encoding.GetEncoding(1251)))
				{
					file.WriteLine("1|1.0|Услуги и поставщики|" + k + "|" + DateTime.Now.ToString("dd.MM.yyyy") + "|" + datmonth.Month.ToString("00") + "." + datmonth.Year + "|" + (list.Count + 1).ToString() + "|");

					if (list.Count > 0)
					{
						foreach (UpdChangesServSupp val in list)
						{
							file.WriteLine("2|" + val.nzp_serv + "_" + val.nzp_supp + "|" + val.service.Replace("|", "/") + "|" + val.name_supp.Replace("|", "/") + "|" + val.inn + "|" +
								val.kpp + "|" + val.rchet + "|" + val.bik + "|" + val.status + "|");
						}
					}
				}

				#region Aрхивация файла
				List<string> arch = new List<string>();
				arch.Add(fullfilename);

				string arch_name = Constants.ExcelDir + "sbchange" + DateTime.Now.Year +
					DateTime.Now.Month.ToString("00") + DateTime.Now.Day.ToString("00");
				string file_name = "sbchange" + DateTime.Now.Year +
					 DateTime.Now.Month.ToString("00") + DateTime.Now.Day.ToString("00");
				if (System.IO.File.Exists(arch_name + ".zip"))
				{
					int k1 = 1;
					while (System.IO.File.Exists(arch_name + "_" + k1 + ".zip")) k1++;
					arch_name += "_" + k1 + ".zip";
					file_name += "_" + k1 + ".zip";
				}
				else
				{
					arch_name += ".zip";
					file_name += ".zip";
				}
				bool res = Utility.Archive.GetInstance().Compress(arch_name, arch.ToArray(), true);

				if (!res)
				{
					return new Returns(false, "Не удалось заархивировать файл", -1);
				}
				#endregion
				if (InputOutput.useFtp) arch_name = InputOutput.SaveOutputFile(arch_name);

				if (ret.result)
				{
					string cmmt = "Выгружено:" + list.Count + ". Всего:" + allrecords + ". Без изменений:" + notchanges +
						". Новые:" + added + ". Удаленные:" + deleted + ". Статус не определен:" + undefined;

					sql = "update " + table_reestr_changes_serv_supp + " set status= " + ExcelUtility.Statuses.Success.GetHashCode() +
						", comment = " + Utils.EStrNull(cmmt) + ", file_name = " + Utils.EStrNull(file_name) + ", nzp_exc = " + nzpExc +
						" where nzp_reestr = " + nzp_reestr;
					ret = ExecSQL(conn_db, sql, true);
					if (!ret.result)
					{
						conn_db.Close();
						return ret;
					}
				}
				else
				{
					sql = "update " + table_reestr_changes_serv_supp + " set status= " + ExcelUtility.Statuses.Failed.GetHashCode() +
						   ", comment = " + Utils.EStrNull("Не удалось заархивировать файл") +
						   " where nzp_reestr = " + nzp_reestr;
					ret = ExecSQL(conn_db, sql, true);
					if (!ret.result)
					{
						conn_db.Close();
						return ret;
					}
				}
				ExcelRepClient dbRep2 = new ExcelRepClient();
				dbRep2.SetMyFileProgress(new ExcelUtility() { nzp_exc = nzpExc, progress = 1 });
				dbRep2.SetMyFileState(new ExcelUtility() { nzp_exc = nzpExc, status = ExcelUtility.Statuses.Success, exc_path = arch_name });
				dbRep2.Close();

			}
			catch (Exception ex)
			{
				sql = "update " + table_reestr_changes_serv_supp + " set status= " + ExcelUtility.Statuses.Failed.GetHashCode() +
						   ", comment = " + Utils.EStrNull("Не удалось заархивировать файл") +
						   " where nzp_reestr = " + nzp_reestr;
				ret = ExecSQL(conn_db, sql, true);
				conn_db.Close();
				ret = new Returns(false, ex.Message);
				MonitorLog.WriteLog("Ошибка выгрузки в банк изменений по поставщикам и услугам " + (Constants.Viewerror ? " \n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
				return ret;
			}
			conn_db.Close();
			return Utils.InitReturns();
		}



		private void DropTempTables(string table_name, IDbConnection conn_db) {
			string sql = "  drop table " + table_name;
			ExecSQL(conn_db, sql, false);
		}

		public List<ReestrChangesServSupp> GetReestrChangesServSupp(ReestrChangesServSupp finder, out Returns ret) {
			if (finder.nzp_user < 1)
			{
				ret = new Returns(false, "Не задан пользователь");
				return null;
			}

			string conn_kernel = Points.GetConnByPref(Points.Pref);
			IDbConnection conn_db = GetConnection(conn_kernel);
			ret = OpenDb(conn_db, true);
			if (!ret.result) return null;

			string where = "";
			if (finder.nzp_reestr > 0) where += " and nzp_reestr = " + finder.nzp_reestr;

			DbTables tables = new DbTables(conn_db);

			//Определить общее количество записей
			string sql = "select count(*) from " + Points.Pref + "_data" + tableDelimiter + "reestr_changes_serv_supp r ," + tables.user + " u " +
				" where r.uploaded_by=u.nzp_user " + where;
			object count = ExecScalar(conn_db, sql, out ret, true);
			int recordsTotalCount;
			try { recordsTotalCount = Convert.ToInt32(count); }
			catch (Exception e)
			{
				ret = new Returns(false, "Ошибка при определении количества записей: " + (Constants.Debug ? e.Message : ""));
				MonitorLog.WriteLog("Ошибка GetReestrChangesServSupp " + (Constants.Viewerror ? "\n" + e.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
				conn_db.Close();
				return null;
			}

			List<ReestrChangesServSupp> spis = new List<ReestrChangesServSupp>();
			sql = " select r.nzp_reestr, r.file_name, r.dat_month, r.nzp_exc, r.status, r.uploaded_on, r.comment, u.comment as user_name from " +
				Points.Pref + "_data" + tableDelimiter + "reestr_changes_serv_supp r ," + tables.user + " u " +
				" where r.uploaded_by=u.nzp_user " + where + " order by nzp_reestr desc";
			IDataReader reader;
			ret = ExecRead(conn_db, out reader, sql, true);
			if (!ret.result)
			{
				conn_db.Close();
				return null;
			}
			try
			{
				int i = 0;
				while (reader.Read())
				{
					i++;
					if (i <= finder.skip) continue;
					ReestrChangesServSupp zap = new ReestrChangesServSupp();
					if (reader["nzp_reestr"] != DBNull.Value) zap.nzp_reestr = Convert.ToInt32(reader["nzp_reestr"]);
					if (reader["nzp_exc"] != DBNull.Value) zap.nzp_exc = Convert.ToInt32(reader["nzp_exc"]);
					if (reader["file_name"] != DBNull.Value) zap.file_name = Convert.ToString(reader["file_name"]);
					if (reader["comment"] != DBNull.Value) zap.comment = Convert.ToString(reader["comment"]).Trim();
					if (reader["user_name"] != DBNull.Value) zap.uploaded = Convert.ToString(reader["user_name"]).Trim();
					if (reader["uploaded_on"] != DBNull.Value) zap.uploaded += " (" + Convert.ToDateTime(reader["uploaded_on"]).ToShortDateString() + ")";
					if (reader["dat_month"] != DBNull.Value) zap.dat_month = Convert.ToDateTime(reader["dat_month"]).Year + "-" +
						Convert.ToDateTime(reader["dat_month"]).Month.ToString("00");
					if (reader["status"] != DBNull.Value) zap.status = Convert.ToInt32(reader["status"]);

					if (zap.status == -1) zap.status_name = "Выгрузка в файл прошла неудачно";
					else if (zap.status == -2)
					{
						zap.status_name = "Выгрузка в файл прошла неудачно(функционал не поддерживается)";
					}
					else if (zap.status == 0) zap.status_name = "В очереди";
					else if (zap.status == 1) zap.status_name = "Файл в процессе формирования";
					else if (zap.status == 2) zap.status_name = "Файл успешно сформирован";
					spis.Add(zap);
					if (finder.rows > 0 && i >= finder.skip + finder.rows) break;
				}

				ret.tag = recordsTotalCount;

				reader.Close();
				conn_db.Close();
				return spis;
			}
			catch (Exception ex)
			{
				reader.Close();
				conn_db.Close();
				ret = new Returns(false, ex.Message);
				MonitorLog.WriteLog("Ошибка заполнения списка " + (Constants.Viewerror ? " \n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
				return null;
			}
		}
        // извлекает имена файлов для быстрого поиска на странице Пачки оплат
	    public List<Pack> GetFilesName(Pack finder, out Returns ret)
	    {
	        ret = Utils.InitReturns();
            if (finder.nzp_user < 1)
            {
                return new List<Pack>();
            }
            // Если поля с датами пустые, выйти
	        if (finder.dat_uchet == "" && finder.dat_uchet_po == "")
	        {
	            return new List<Pack>();
	        }
            #region соединение
            string conn_kernel = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return new List<Pack>(); ;
            # endregion
            
            DateTime parsedDatUchet= new DateTime();
            DateTime parsedDatUchetPo= new DateTime();
            List<Pack> listFilesName = new List<Pack>();
            // ключ- наименование таблицы, значение- условие where
            Dictionary<string, string> tablesWhere = new Dictionary<string, string>();

            // преобразуем даты Операционного дня в тип DateTime, если они заполнены
	        if (finder.dat_uchet != "")
	        {
	            if (!DateTime.TryParse(finder.dat_uchet, out parsedDatUchet))
	            {
	                MonitorLog.WriteLog(
	                    "Ошибка преобразования строки finder.dat_uchet в тип DateTime в методе GetFilesName()",
	                    MonitorLog.typelog.Error, true);
	                return new List<Pack>();
	            }
	        }

            if (finder.dat_uchet_po != "")
            {
                if (!DateTime.TryParse(finder.dat_uchet_po, out parsedDatUchetPo))
                {
                    MonitorLog.WriteLog(
                        "Ошибка преобразования строки finder.dat_uchet_po в тип DateTime в методе GetFilesName()",
                        MonitorLog.typelog.Error, true);
                    return new List<Pack>();
                }
            }
            // Если обе даты заполнены
	        if (finder.dat_uchet != "" && finder.dat_uchet_po != "")
	        {
                // выясняем разницу в годах
	            int diffyear = parsedDatUchetPo.Year - parsedDatUchet.Year;
                // если разница есть
	            if (diffyear > 0)
	            {
                    // например начальная дата 01.01.2012, а конечная 01.01.2015
                    // цикл позволяет учесть все таблицы nftul_fin_год за указанный период
	                for (int i = 0; i <= diffyear; i++)
	                {
                        // Для каждого года формируем наименование таблицы, из которой будет вытягиваться наименование файла
	                    int year = parsedDatUchet.Year + i;
                        string table = Points.Pref + "_fin_" + year.ToString("0000").Substring(2, 2) +
	                                   ".pack";
                        // проверяем существует ли эта таблица
	                    if (!TempTableInWebCashe(conn_db, table)) continue;
                        // Если год начальной даты операционного дня совпал с годом цикла 
	                    if (year == parsedDatUchet.Year)
	                    {
                            // добавляем таблицу и услови
	                        tablesWhere[table] = " and dat_uchet>='" + parsedDatUchet.ToShortDateString() + "'";
	                        continue;
	                    }
                        // Если год конечной даты операционного дня совпал с годом цикла 
	                    if (year == parsedDatUchetPo.Year)
	                    {
                            // добавляем таблицу и условия
                            tablesWhere[table] = " and dat_uchet<= '" + parsedDatUchetPo.ToShortDateString() + "'";    
                            continue;
	                    }
                        // если нет совпадений, то добавляем только таблицу, без условий
	                    tablesWhere[table] = "";
	                }
	            }
                    // если же конечная и начальная даты операционного дня одного года
	            else
	            {
                    // добавляем таблицу и условия
	                string table = Points.Pref + "_fin_" + parsedDatUchet.Year.ToString("0000").Substring(2, 2) + ".pack";
	                if (TempTableInWebCashe(conn_db, table))
	                {
                        tablesWhere[table] = " and dat_uchet>=" + "'" + parsedDatUchet.ToShortDateString() + "'" +
                                              " and dat_uchet<=" + "'" + parsedDatUchetPo.ToShortDateString() + "'";
	                }
	            }
	        }
	        else
	        {
                // Если зполнено только поле с начальной датой Операционного дня
                if (finder.dat_uchet != "")
                {
                    int yearS = parsedDatUchet.Year;
                    do
                    {
                        // проходим все таблицы от nftul_fin_yearS до nftul_fin_99 
                        string table = Points.Pref + "_fin_" + yearS.ToString("0000").Substring(2, 2) + ".pack";
                        // Если таблица не существует , продолжаем
                        if (!TempTableInWebCashe(conn_db, table)) continue;
                        if (yearS == parsedDatUchet.Year)
                        {
                            // добавляем условие если parsedDatUchet.Year и год цикла совпадают
                            tablesWhere[table] = " and dat_uchet>=" + "'" + parsedDatUchet.ToShortDateString() + "'";
                            continue;
                        }
                        tablesWhere[table] = "";
                    } while (++yearS < 2050);
                }
                // Если зполнено только поле с конечной датой Операционного дня
                else if (finder.dat_uchet_po != "")
                {
                    int yearPo = parsedDatUchetPo.Year;
                    do
                    {
                        string table = Points.Pref + "_fin_" + yearPo.ToString("0000").Substring(2, 2) + ".pack";
                        if (!TempTableInWebCashe(conn_db, table)) continue;
                        if (yearPo == parsedDatUchetPo.Year)
                        {
                            tablesWhere[table] = " and dat_uchet<=" + "'" + parsedDatUchetPo.ToShortDateString() + "'";
                            continue;
                        }
                        tablesWhere[table] = "";
                    } while (--yearPo > 2008);
                }
	        }
            // Если после всего этого таблицы не появились, выйти
            if (tablesWhere.Count == 0) return new List<Pack>();
            string where = "";
            if (finder.file_name != "")
            {
                where = " and lower(file_name) LIKE lower('%" + finder.file_name.ToLower() + "%') ";
            }
            foreach (KeyValuePair<string, string> kvp in tablesWhere)
            {
                string sql = "select distinct file_name from " + kvp.Key + " where 1=1 " + kvp.Value + where + " Order by file_name";
                IDataReader reader = null;
                try
                {
                    ret = ExecRead(conn_db, out reader, sql, true);
                    if (!ret.result)
                    {
                        return new List<Pack>();
                    }
                    int i = 0;
                    while (reader.Read())
                    {
                        i++;
                        if (reader["file_name"] == DBNull.Value || reader["file_name"].ToString().Trim() == string.Empty) continue;
                        Pack pack = new Pack();
                        pack.file_name = reader["file_name"].ToString().Trim();
                        listFilesName.Add(pack);
                        if (finder.rows > 0 && i >= finder.rows) return listFilesName;
                    }
                }
                catch (Exception ex)
                {

                    ret.tag = -1;
                    ret = new Returns(false, ex.Message);
                    MonitorLog.WriteLog("Ошибка в функции GetFilesName() " + (Constants.Viewerror ? " \n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                    return null;
                }
                finally
                {
                    if (reader != null) reader.Close();
                    conn_db.Close();
                }
            }

            return listFilesName;
	    }

	    public List<DogovorRequisites> GetListDogERCByAgentAndPrincip(DogovorRequisites finder, out Returns ret)
	    {
            var list = new List<DogovorRequisites>();
            ret = Utils.InitReturns();
            if (finder.nzp_user <= 0)
            {
                ret = new Returns(false, "Пользователь не определен");
                return null;
            }
            #region соединение
            string conn_kernel = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return list; 
            # endregion

	        string where = "";
            if (finder.nzp_supp>0) where+=" and s.nzp_supp=" + finder.nzp_supp;
	        string sql = "select distinct d.nzp_fd, d.num_dog, d.dat_dog, a.payer as agent, pr.payer as princip from "+Points.Pref+sKernelAliasRest+"supplier s " +
                         " left outer join " + Points.Pref + sDataAliasRest + "fn_dogovor d  on s.nzp_payer_agent=d.nzp_payer_agent and s.nzp_payer_princip=d.nzp_payer_princip " +
                         " left outer join " + Points.Pref + sKernelAliasRest + "s_payer a on d.nzp_payer_agent = a.nzp_payer  " +
                         " left outer join " + Points.Pref + sKernelAliasRest + "s_payer  pr on d.nzp_payer_princip=pr.nzp_payer where 1=1" + where;
            try
            {
                MyDataReader reader;
                ret = ExecRead(conn_db, out reader, sql, true);
                if (!ret.result)
                {
                    return list;
                }
                while (reader.Read())
                {
                    DogovorRequisites dr= new DogovorRequisites();
                    if (reader["nzp_fd"] != DBNull.Value) dr.nzp_fd = Convert.ToInt32(reader["nzp_fd"]);
                    if (reader["num_dog"] != DBNull.Value) dr.num_dog = (reader["num_dog"]).ToString();
                    if (reader["agent"] != DBNull.Value) dr.agent = Convert.ToString(reader["agent"]);
                    if (reader["princip"] != DBNull.Value) dr.principal = Convert.ToString(reader["princip"]);
                    if (reader["dat_dog"] != DBNull.Value) dr.dat_dog = Convert.ToString(reader["dat_dog"]);
                    list.Add(dr);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения  GetListDogERCByAgentAndPrincip() : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = "Ошибка сохранения данных";
                ret.result = false;
                return null;
            }
            finally
            {
                if (conn_db != null)
                {
                    conn_db.Close();
                }
            }
            return list;
	    }
	}
}
