using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System;
using System.Data;
using System.Text;


namespace STCLINE.KP50.DataBase
{
    public partial class DbSpravKernel : DbSpravClient
    {
        private const int _payetGetsPayments = 4;
        
        public Returns SavePayer(Payer finder, IDbTransaction transaction, IDbConnection conn_db)
        {
            Returns ret = Utils.InitReturns();
            finder.npayer = (finder.npayer ?? "").Trim();
            finder.payer = (finder.payer ?? "").Trim();
            finder.inn = (finder.inn ?? "").Trim();
            finder.kpp = (finder.kpp ?? "").Trim();
            if (finder.npayer == "" && finder.payer != "") finder.npayer = finder.payer;
            if (finder.payer == "" && finder.npayer != "") finder.payer = finder.npayer;

            #region проверка входных параметров
            if (finder.nzp_user < 1) return new Returns(false, "Не определен пользователь");
            if (finder.nzp_payer < 1 && finder.nzp_supp < 1)
                if (finder.payer == "") return new Returns(false, "Не задано краткое наименование подрядчика", -1);
            if (finder.nzp_payer > 0)
                if (finder.payer == "") return new Returns(false, "Не задано краткое наименование подрядчика", -1);
            #endregion

            DbTables tables = new DbTables(DBManager.getServer(conn_db));

            string sql;
            bool is_new = false;

            string field = "", value = "";
            if (finder.id_bc_type > 0)
            {
                field = ", id_bc_type ";
                value = ", " + finder.id_bc_type + " ";
            }

            if (finder.nzp_payer > 0)
            {

                field = ", id_bc_type = " + finder.id_bc_type + " ";

                sql = "select nzp_payer from " + tables.payer + " where nzp_payer = " + finder.nzp_payer;
                IDataReader reader;
                ret = ExecRead(conn_db, transaction, out reader, sql, true);
                if (!ret.result) return ret;
                if (reader.Read())
                {
                    sql = " update " + tables.payer + " set " +
                            " npayer = " + Utils.EStrNull(finder.npayer) +
                            ", payer = " + Utils.EStrNull(finder.payer) +
                            ", nzp_supp = " + Utils.EStrNull(finder.nzp_supp.ToString()) +
                            ", inn = " + Utils.EStrNull(finder.inn) +
                            ", kpp = " + Utils.EStrNull(finder.kpp) +
                            field +
                            (finder.nzp_type > 0 ? ", nzp_type = " + finder.nzp_type : "") +
                            " where nzp_payer = " + finder.nzp_payer;
                }
                else
                {
                    is_new = true;

                    if (finder.nzp_supp > 0)
                    {
                        finder.nzp_payer = Convert.ToInt32(finder.nzp_supp);
                        ret = Utils.InitReturns();
                        string s = GetNameById(conn_db, transaction, finder.nzp_supp, Constants.getInfo_supp, out ret);
                        if (!ret.result) return ret;
                        finder.payer = finder.npayer = s;
                        sql = " insert into " + tables.payer + "(nzp_payer, payer, npayer, nzp_supp, is_erc, inn, kpp, nzp_type" + field + ") " +
                              " values (" + finder.nzp_supp + ", " + Utils.EStrNull(finder.payer) + ", " + Utils.EStrNull(finder.npayer) + ", " + finder.nzp_supp + ", 1" +
                              "," + Utils.EStrNull(finder.inn) +
                              "," + Utils.EStrNull(finder.kpp) +
                              ", " + Payer.ContragentTypes.ServiceSupplier.GetHashCode() + value + ")";
                    }
                    else
                    {
                        sql = " insert into " + tables.payer + "(nzp_payer, payer, npayer, nzp_supp, is_erc, nzp_type" + field + ") " +
                              " values (" + finder.nzp_payer + ", " + Utils.EStrNull(finder.payer) + ", " + Utils.EStrNull(finder.npayer) + ", 0, 0," + Utils.EStrNull(finder.inn) +
                              "," + Utils.EStrNull(finder.kpp) +
                              ", " + (finder.nzp_type > 0 ? finder.nzp_type.ToString() : "null") + value + ")";
                    }
                }
                CloseReader(ref reader);
            }
            else
            {
                is_new = true;
                if (finder.nzp_supp > 0)
                {
                    string s = GetNameById(conn_db, transaction, finder.nzp_supp, Constants.getInfo_supp, out ret);
                    if (!ret.result) return ret;
                    finder.nzp_payer = Convert.ToInt32(finder.nzp_supp);
                    finder.payer = finder.npayer = s;
                    sql = " insert into " + tables.payer + "(nzp_payer, payer, npayer, nzp_supp, is_erc,inn,kpp, nzp_type" + field + ") " +
                          " values (" + finder.nzp_payer + ", " + Utils.EStrNull(finder.payer) + ", " + Utils.EStrNull(finder.npayer) + ", " + finder.nzp_supp + ", 1" +
                          ", " + Utils.EStrNull(finder.inn) + "," + Utils.EStrNull(finder.kpp) + ", " + Payer.ContragentTypes.ServiceSupplier.GetHashCode() + value + ")";
                }
                else
                {
                    var db = new DbSpravKernel();
                    ret = db.GetNewId(conn_db, transaction, Series.Types.Payer);
                    if (!ret.result)
                    {

                        return ret;
                    }

                    finder.nzp_payer = ret.tag;

                    sql = " insert into " + tables.payer + "(nzp_payer, payer, npayer, nzp_supp, is_erc, inn, kpp, nzp_type" + field + ") " +
                          " values (" + finder.nzp_payer + ", " + Utils.EStrNull(finder.payer) + ", " + Utils.EStrNull(finder.npayer) + ", 0, 0," + Utils.EStrNull(finder.inn) +
                          "," + Utils.EStrNull(finder.kpp) +
                          ", " + (finder.nzp_type > 0 ? finder.nzp_type.ToString() : "null") + value + ")";
                }
            }

            //Проверка, что связь подрядчик-поставщик - 1:1
            if (finder.nzp_supp > 0)
            {
                string sql_supp = " select count(*) cnt from " + tables.payer + " where nzp_supp = " + finder.nzp_supp + " and nzp_payer <> " + finder.nzp_payer;
                object count_supp = ExecScalar(conn_db, transaction, sql_supp, out ret, true);
                if (!ret.result) return ret;
                if (Convert.ToInt32(count_supp) > 0)
                {
                    ret.result = false;
                    ret.text = "Данный поставщик услуг уже определен для другого подрядчика";
                    ret.tag = -1;
                    return ret;
                }
            }

            //проверка уникальности наименование подрядчика
            if (Points.Region != Regions.Region.Samarskaya_obl)
            {
                string sql_payer = " select count(*) cnt from " + tables.payer + " where payer = " + Utils.EStrNull(finder.payer) + " and nzp_payer <> " + finder.nzp_payer;
                object count_payer = ExecScalar(conn_db, transaction, sql_payer, out ret, true);
                if (!ret.result) return ret;
                if (Convert.ToInt32(count_payer) > 0)
                {
                    ret.result = false;
                    ret.text = "Обнаружено дублирование наименований подрядчиков";
                    ret.tag = -1;
                    return ret;
                }
            }

            //проверка дублирования кода подрядчика
            if (is_new && finder.nzp_payer > 0)
            {
                string sql_kod_payer = " select count(*) cnt from " + tables.payer + " where nzp_payer = " + finder.nzp_payer;
                object count_kod_payer = ExecScalar(conn_db, transaction, sql_kod_payer, out ret, true);
                if (!ret.result)
                {
                    ret.tag = 0;
                    return ret;
                }
                if (Convert.ToInt32(count_kod_payer) > 0)
                {
                    ret.result = false;
                    ret.text = "Неверный код - такой код уже определен";
                    ret.tag = -1;
                    return ret;
                }
            }

            ret = ExecSQL(conn_db, transaction, sql, true);
            if (ret.result)
            {
                ret.tag = finder.nzp_payer;
            }

            return ret;
        }

        public Returns SavePayer(Payer finder)
        {
            #region подключение к базе
            string conn_kernel = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            Returns ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;
            #endregion

            IDbTransaction transaction = conn_db.BeginTransaction();
            ret = SavePayer(finder, transaction, conn_db);
            if (ret.result) transaction.Commit();
            else transaction.Rollback();
            conn_db.Close();
            return ret;
        }

        public Returns SavePayerContract(Payer finder, IDbTransaction transaction, IDbConnection conn_db)
        {
            Returns ret = Utils.InitReturns();
            finder.npayer = (finder.npayer ?? "").Trim();
            finder.payer = (finder.payer ?? "").Trim();
            finder.inn = (finder.inn ?? "").Trim();
            finder.kpp = (finder.kpp ?? "").Trim();
            if (finder.npayer == "" && finder.payer != "") finder.npayer = finder.payer;
            if (finder.payer == "" && finder.npayer != "") finder.payer = finder.npayer;

            #region проверка входных параметров
            if (finder.nzp_user < 1) return new Returns(false, "Не определен пользователь");
            if (finder.nzp_payer < 1 && finder.nzp_supp < 1)
                if (finder.payer == "") return new Returns(false, "Не задано краткое наименование подрядчика", -1);
            if (finder.nzp_payer > 0)
                if (finder.payer == "") return new Returns(false, "Не задано краткое наименование подрядчика", -1);
            #endregion

            DbTables tables = new DbTables(DBManager.getServer(conn_db));

            string sql;
            bool is_new = false;

            string field = "", value = "";
            if (finder.id_bc_type > 0)
            {
                field = ", id_bc_type ";
                value = ", " + finder.id_bc_type + " ";
            }

            if (finder.nzp_payer > 0)
            {

                field = ", id_bc_type = " + finder.id_bc_type + " ";

                sql = "select nzp_payer from " + tables.payer + " where nzp_payer = " + finder.nzp_payer;
                IDataReader reader;
                ret = ExecRead(conn_db, transaction, out reader, sql, true);
                if (!ret.result) return ret;
                if (reader.Read())
                {
                    sql = " update " + tables.payer + " set " +
                            " npayer = " + Utils.EStrNull(finder.npayer) +
                            ", payer = " + Utils.EStrNull(finder.payer) +
                            ", inn = " + Utils.EStrNull(finder.inn) +
                            ", kpp = " + Utils.EStrNull(finder.kpp) +
                            field +
                            " where nzp_payer = " + finder.nzp_payer;
                }
                else
                {
                    is_new = true;

                    sql = " insert into " + tables.payer + "(nzp_payer, payer, npayer, is_erc, inn, kpp, " + field + ") " +
                            " values (" + finder.nzp_payer + ", " + Utils.EStrNull(finder.payer) + ", " + Utils.EStrNull(finder.npayer) + ", 1" +
                            "," + Utils.EStrNull(finder.inn) +
                            "," + Utils.EStrNull(finder.kpp) +
                            value + ")";
                   
                }
                CloseReader(ref reader);
            }
            else
            {
                is_new = true;
             
                var db = new DbSpravKernel();
                ret = db.GetNewId(conn_db, transaction, Series.Types.Payer);
                if (!ret.result)
                {

                    return ret;
                }
                if (finder.nzp_supp > 0)
                {
                    field += " , nzp_supp ";
                    value += " , " + finder.nzp_supp + " ";
                }
                finder.nzp_payer = ret.tag;

                sql = " insert into " + tables.payer + "(nzp_payer, payer, npayer, is_erc, inn, kpp " + field + ") " +
                        " values (" + finder.nzp_payer + ", " + Utils.EStrNull(finder.payer) + ", " +
                        Utils.EStrNull(finder.npayer) + ", 0, " + Utils.EStrNull(finder.inn) +
                        "," + Utils.EStrNull(finder.kpp) +
                        value + ")";
                
            }

            
            //проверка уникальности наименование подрядчика
            if (Points.Region != Regions.Region.Samarskaya_obl)
            {
                string sql_payer = " select count(*) cnt from " + tables.payer + " where payer = " + Utils.EStrNull(finder.payer) + " and nzp_payer <> " + finder.nzp_payer;
                object count_payer = ExecScalar(conn_db, transaction, sql_payer, out ret, true);
                if (!ret.result) return ret;
                if (Convert.ToInt32(count_payer) > 0)
                {
                    ret.result = false;
                    ret.text = "Обнаружено дублирование наименований подрядчиков";
                    ret.tag = -1;
                    return ret;
                }
            }

            //проверка дублирования кода подрядчика
            if (is_new && finder.nzp_payer > 0)
            {
                string sql_kod_payer = " select count(*) cnt from " + tables.payer + " where nzp_payer = " + finder.nzp_payer;
                object count_kod_payer = ExecScalar(conn_db, transaction, sql_kod_payer, out ret, true);
                if (!ret.result)
                {
                    ret.tag = 0;
                    return ret;
                }
                if (Convert.ToInt32(count_kod_payer) > 0)
                {
                    ret.result = false;
                    ret.text = "Неверный код - такой код уже определен";
                    ret.tag = -1;
                    return ret;
                }
            }

            ret = ExecSQL(conn_db, transaction, sql, true);
            if (ret.result)
            {
                sql = "delete from " + tables.payertypes + " where nzp_payer = " + finder.nzp_payer;
                ret = ExecSQL(conn_db, transaction, sql, true);
                if (!ret.result) return ret;

                if (finder.include_types != null && finder.include_types.Count > 0)
                {
                    #region Определить пользователя
                    int nzpUser = finder.nzp_user; //локальный пользователь  
                    
                    /*finder.pref = Points.Pref;
                    DbWorkUser db = new DbWorkUser();
                    int nzpUser = db.GetLocalUser(conn_db,transaction, finder, out ret); //локальный пользователь      
                    db.Close();
                    if (!ret.result) return ret;*/
                    #endregion

                    foreach (int nzp_payer_types in finder.include_types)
                    {
                        sql = "insert into " + tables.payertypes + "(nzp_payer,nzp_payer_type,changed_on,changed_by) " +
                            "values (" + finder.nzp_payer + "," + nzp_payer_types + "," + sCurDateTime + ", " + nzpUser + ")";
                        ret = ExecSQL(conn_db, transaction, sql, true);
                        if (!ret.result) return ret;
                    }
                }

                ret.tag = finder.nzp_payer;
            }
            
            if (IsSaveBank(finder))
            {
                ret = SaveBank(conn_db, finder.nzp_payer, finder.payer, finder.npayer);
                if (!ret.result) return ret;
            }

            return ret;
        }

        public Returns SavePayerContractNewFd(Payer finder, IDbTransaction transaction, IDbConnection conn_db)
        {
            Returns ret = Utils.InitReturns();
            finder.npayer = (finder.npayer ?? "").Trim();
            finder.payer = (finder.payer ?? "").Trim();
            finder.inn = (finder.inn ?? "").Trim();
            finder.kpp = (finder.kpp ?? "").Trim();
            finder.bik = (finder.bik ?? "").Trim();
            finder.ks = (finder.ks ?? "").Trim();
            finder.city = (finder.city ?? "").Trim();
            if (finder.npayer == "" && finder.payer != "") finder.npayer = finder.payer;
            if (finder.payer == "" && finder.npayer != "") finder.payer = finder.npayer;

            #region проверка входных параметров
            if (finder.nzp_user < 1) return new Returns(false, "Не определен пользователь");
            if (finder.nzp_payer < 1 && finder.nzp_supp < 1)
                if (finder.payer == "") return new Returns(false, "Не задано краткое наименование подрядчика", -1);
            if (finder.nzp_payer > 0)
                if (finder.payer == "") return new Returns(false, "Не задано краткое наименование подрядчика", -1);
            #endregion

            DbTables tables = new DbTables(DBManager.getServer(conn_db));

            string sql;
            bool is_new = false;

            string field = "", value = "";
            if (finder.id_bc_type > 0)
            {
                field = ", id_bc_type ";
                value = ", " + finder.id_bc_type + " ";
            }



            if (finder.nzp_payer > 0)
            {

                field = ", id_bc_type = " + finder.id_bc_type + " ";

                sql = "select nzp_payer from " + tables.payer + " where nzp_payer = " + finder.nzp_payer;
                IDataReader reader;
                ret = ExecRead(conn_db, transaction, out reader, sql, true);
                if (!ret.result) return ret;
                if (reader.Read())
                {
                    sql = " update " + tables.payer + " set " +
                            " npayer = " + Utils.EStrNull(finder.npayer) +
                            ", payer = " + Utils.EStrNull(finder.payer) +
                            ", inn = " + Utils.EStrNull(finder.inn) +
                            ", kpp = " + Utils.EStrNull(finder.kpp) +
                            ", bik = " + Utils.EStrNull(finder.bik) +
                            ", ks = " + Utils.EStrNull(finder.ks) +
                            ", city = " + Utils.EStrNull(finder.city) +
                            ", changed_by = " + finder.nzp_user +
                            ", changed_on = " + sCurDateTime + "  " +
                            field +
                            " where nzp_payer = " + finder.nzp_payer;
                }
                else
                {
                    is_new = true;

                    sql = " insert into " + tables.payer + "(nzp_payer, payer, npayer, is_erc, inn, kpp," +
                          " bik, ks, city, changed_by, changed_on " + field + ") " +
                            " values (" + finder.nzp_payer + ", " + Utils.EStrNull(finder.payer) + ", " + Utils.EStrNull(finder.npayer) + ", 1" +
                            "," + Utils.EStrNull(finder.inn) +
                            "," + Utils.EStrNull(finder.kpp) +
                            "," + Utils.EStrNull(finder.bik) +
                            "," + Utils.EStrNull(finder.ks) +
                            "," + Utils.EStrNull(finder.city) +
                            "," + finder.nzp_user +
                            ", " + sCurDateTime + " " +
                            value + ")";

                }
                CloseReader(ref reader);
            }
            else
            {
                is_new = true;

                var db = new DbSpravKernel();
                ret = db.GetNewId(conn_db, transaction, Series.Types.Payer);
                if (!ret.result)
                {

                    return ret;
                }
                if (finder.nzp_supp > 0)
                {
                    field += " , nzp_supp ";
                    value += " , " + finder.nzp_supp + " ";
                }
                finder.nzp_payer = ret.tag;

                sql = " insert into " + tables.payer + "(nzp_payer, payer, npayer, is_erc, inn, kpp, bik, ks, city, changed_by, changed_on " + field + ") " +
                        " values (" + finder.nzp_payer + ", " + Utils.EStrNull(finder.payer) + ", " +
                        Utils.EStrNull(finder.npayer) + ", 0, " + Utils.EStrNull(finder.inn) +
                        "," + Utils.EStrNull(finder.kpp) +
                        "," + Utils.EStrNull(finder.bik) +
                        "," + Utils.EStrNull(finder.ks) +
                        "," + Utils.EStrNull(finder.city) +
                        "," + finder.nzp_user +
                        ", " + sCurDateTime + " " +
                        value + ")";

            }


            //проверка уникальности наименование подрядчика
            if (Points.Region != Regions.Region.Samarskaya_obl)
            {
                string sql_payer = " select count(*) cnt from " + tables.payer + " where payer = " + Utils.EStrNull(finder.payer) + " and nzp_payer <> " + finder.nzp_payer;
                object count_payer = ExecScalar(conn_db, transaction, sql_payer, out ret, true);
                if (!ret.result) return ret;
                if (Convert.ToInt32(count_payer) > 0)
                {
                    ret.result = false;
                    ret.text = "Обнаружено дублирование наименований подрядчиков";
                    ret.tag = -1;
                    return ret;
                }
            }

            //проверка дублирования кода подрядчика
            if (is_new && finder.nzp_payer > 0)
            {
                string sql_kod_payer = " select count(*) cnt from " + tables.payer + " where nzp_payer = " + finder.nzp_payer;
                object count_kod_payer = ExecScalar(conn_db, transaction, sql_kod_payer, out ret, true);
                if (!ret.result)
                {
                    ret.tag = 0;
                    return ret;
                }
                if (Convert.ToInt32(count_kod_payer) > 0)
                {
                    ret.result = false;
                    ret.text = "Неверный код - такой код уже определен";
                    ret.tag = -1;
                    return ret;
                }
            }

            ret = ExecSQL(conn_db, transaction, sql, true);
            if (ret.result)
            {
                sql = "delete from " + tables.payertypes + " where nzp_payer = " + finder.nzp_payer;
                ret = ExecSQL(conn_db, transaction, sql, true);
                if (!ret.result) return ret;

                if (finder.include_types != null && finder.include_types.Count > 0)
                {
                    #region Определить пользователя
                    int nzpUser = finder.nzp_user;
                    
                    /*finder.pref = Points.Pref;
                    DbWorkUser db = new DbWorkUser();
                    int nzpUser = db.GetLocalUser(conn_db, transaction, finder, out ret); //локальный пользователь      
                    db.Close();
                    if (!ret.result) return ret;*/
                    #endregion

                    foreach (int nzp_payer_types in finder.include_types)
                    {
                        sql = "insert into " + tables.payertypes + "(nzp_payer,nzp_payer_type,changed_on,changed_by) " +
                            "values (" + finder.nzp_payer + "," + nzp_payer_types + "," + sCurDateTime + ", " + nzpUser + ")";
                        ret = ExecSQL(conn_db, transaction, sql, true);
                        if (!ret.result) return ret;
                    }
                }

                ret.tag = finder.nzp_payer;
            }

            if (IsSaveBank(finder))
            {
                ret = SaveBank(conn_db, finder.nzp_payer, finder.payer, finder.npayer);
                if (!ret.result) return ret;
            }

            return ret;
        }

        /// <summary>
        /// Признак, что нужно сохранить контрагента в таблицу s_bank, если контрагент принимает платежи
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        private bool IsSaveBank(Payer finder)
        {
            bool saveBank = false;
            for (int i = 0; i < finder.include_types.Count; i++)
            {
                if (finder.include_types[i] == _payetGetsPayments)
                {
                    saveBank = true;
                    break;
                }
            }

            return saveBank;
        }

        /// <summary>
        /// Если контрагент принимает платежи, то скопировать его в таблицу s_bank
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        private Returns SaveBank(IDbConnection conn_db, int nzp_payer, string payer, string npayer)
        {
            Returns ret = new Returns(true);
            
            string sql = "select count(*) from " + Points.Pref + "_kernel" + DBManager.tableDelimiter + "s_bank where nzp_payer = " + nzp_payer;
            Object obj = ExecScalar(conn_db, sql, out ret, true);
            if (!ret.result) return ret;

            if (Convert.ToInt32(obj) == 0)
            {
                string bank_name = npayer;
                if (bank_name.Trim() == "") bank_name = payer;
                if (bank_name.Length > 40) bank_name = bank_name.Substring(0, 40);

                sql = "insert into " + Points.Pref + "_kernel" + DBManager.tableDelimiter + "s_bank (nzp_payer, bank) values (" + nzp_payer + ", " + Utils.EStrNull(bank_name) + ")";
                ret = ExecSQL(conn_db, sql);
                if (!ret.result) return ret;
            }

            return ret;
        }

        public Returns SavePayerContract(Payer finder)
        {
            #region подключение к базе
            string conn_kernel = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            Returns ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;
            #endregion

            IDbTransaction transaction = conn_db.BeginTransaction();
            ret = SavePayerContract(finder, transaction, conn_db);
            if (ret.result) transaction.Commit();
            else transaction.Rollback();
            conn_db.Close();
            return ret;
        }

        public Returns SavePayerContractNewFd(Payer finder)
        {
            #region подключение к базе
            string conn_kernel = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            Returns ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;
            #endregion

            IDbTransaction transaction = conn_db.BeginTransaction();
            ret = SavePayerContractNewFd(finder, transaction, conn_db);
            if (ret.result) transaction.Commit();
            else transaction.Rollback();
            conn_db.Close();
            return ret;
        }

        public Returns DeletePayerContract(Payer finder)
        {
            #region подключение к базе
            string conn_kernel = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(conn_kernel);
            Returns ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;
            #endregion
            string tnames = "tnames";
            ret = PrepareNamesTable(conn_db, tnames);
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }
            MyDataReader reader, reader2;
            StringBuilder sql = new StringBuilder("select table_name, table_schema, field_name from " + tnames);
            ret = ExecRead(conn_db, out reader, sql.ToString(), true);
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }

            while (reader.Read())
            {
                if (!TempTableInWebCashe(conn_db, reader["table_schema"].ToString() + tableDelimiter +
                    reader["table_name"].ToString())) continue;
                sql.Remove(0, sql.Length);
                sql.AppendFormat("select 1 from {0}{1}{2} where {3} = {4}  limit 1",
                    reader["table_schema"].ToString(), tableDelimiter, 
                    reader["table_name"].ToString(), reader["field_name"], finder.nzp_payer);
                ret = ExecRead(conn_db, out reader2,sql.ToString(), true);
                if (!ret.result)
                {
                    reader.Close();
                    conn_db.Close();
                    return ret;
                }
                if (reader2.Read())
                {
                    reader.Close();
                    conn_db.Close();
                    return new Returns(false, "Удалить контрагента нельзя, т.к. на него имеются ссылки", -1);
                }
                if (reader2 != null) reader2.Close();
            }
            reader.Close();

            //проверка в prm
            sql.Remove(0, sql.Length);
            sql.AppendFormat("select 1 from {0}_data{1}prm_9 where nzp_prm in (505, 1269) and nzp ={2} limit 1", Points.Pref, tableDelimiter, finder.nzp_payer);
            if (TempTableInWebCashe(conn_db, Points.Pref + "_data" + tableDelimiter + "prm_9"))
            {
                ret = ExecRead(conn_db, out reader2, sql.ToString(), true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return ret;
                }
                if (reader2.Read())
                {
                    reader2.Close();
                    conn_db.Close();
                    return new Returns(false, "Удалить контрагента нельзя, т.к. на него имеются ссылки", -1);
                }
            }
            foreach(_Point p in Points.PointList)
            {
                if (TempTableInWebCashe(conn_db, p.pref +"_data"+ tableDelimiter + "prm_9"))
                {
                    sql.Remove(0, sql.Length);
                    sql.AppendFormat("select 1 from {0}_data{1}prm_9 where nzp_prm in (505, 1269) and nzp ={2}  limit 1", p.pref, tableDelimiter, finder.nzp_payer);
                    ret = ExecRead(conn_db, out reader2, sql.ToString(), true);
                    if (!ret.result)
                    {
                        conn_db.Close();
                        return ret;
                    }
                    if (reader2.Read())
                    {
                        reader2.Close();
                        conn_db.Close();
                        return new Returns(false, "Удалить контрагента нельзя, т.к. на него имеются ссылки", -1);
                    }
                }
            }

            sql.Remove(0, sql.Length);
            sql.AppendFormat("delete from {0}_kernel{1}s_payer where nzp_payer = {2}", Points.Pref, tableDelimiter, finder.nzp_payer);
            ret = ExecSQL(conn_db, sql.ToString(), true);
            return ret;        
        }

        private Returns PrepareNamesTable(IDbConnection conn_db, string table_name)
        {
            StringBuilder sql = new StringBuilder("drop table "+ table_name);
            Returns ret = ExecSQL (conn_db, sql.ToString(), false);

            sql.Remove(0, sql.Length);
            sql.AppendFormat("create temp table {0} "+
            " ( "+
            " table_name character varying,"+
            " table_schema character varying,"+
            " field_name character varying"+
            " );", table_name);
            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result) return ret;

            sql.Remove(0, sql.Length);
            sql.AppendFormat(" insert into {0} (table_name, table_schema, field_name)", table_name);
            sql.Append(" Select c.table_name, c.table_schema, c.column_name ");
            sql.Append(" From information_schema.columns c ");
            sql.Append(" where c.table_catalog='"+conn_db.Database+"' and c.table_schema not in ('pg_catalog', 'information_schema', 'public') and ");
            sql.Append(" c.table_name not in ('s_payer', '_s_payer_new', '_s_payer', 'a_payer') and lower(table_name) !~ '^[t]\\d' and ");
            sql.Append(" lower(trim(c.column_name)) in ('nzp_payer', 'nzp_payer_princip', 'nzp_payer_agent', 'nzp_payer_supp', 'nzp_payer_2', 'nzp_payer_bank'); ");
            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result) return ret;
                        
            sql.Remove(0, sql.Length);
            sql.AppendFormat(" insert into {0} (table_name, table_schema, field_name)", table_name);
            sql.Append(" Select c.table_name, c.table_schema, c.column_name ");
            sql.Append(" From information_schema.columns c ");
            sql.AppendFormat(" where c.table_catalog='{0}' ", conn_db.Database); 
            sql.Append(" and c.table_schema = 'public' and lower(c.table_name) !~ '^[t]\\d' ");
            sql.Append(" and lower(trim(c.column_name)) in ('nzp_payer', 'nzp_payer_princip', 'nzp_payer_agent', 'nzp_payer_supp', 'nzp_payer_2', 'nzp_payer_bank');");
            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result) return ret;

            sql.Remove(0, sql.Length);
            sql.AppendFormat(" insert into tnames (table_name, table_schema, field_name) ");
            sql.Append(" Select c.table_name, c.table_schema, c.column_name ");
            sql.Append(" From information_schema.columns c ");
            sql.AppendFormat(" where c.table_catalog='{0}' ", conn_db.Database); 
            sql.Append(" and lower(trim(c.column_name)) = 'nzp_bank' and lower(table_schema) like '" + Points.Pref + "_fin_%'  ");
            sql.Append(" and lower(c.table_schema) not in ('pg_catalog', 'information_schema', 'public') ");
            sql.Append(" and lower(table_name) like 'fn_distrib_dom_%';");
            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result) return ret;

            //sql.Remove(0, sql.Length);
            //sql.AppendFormat(" insert into {0} (table_name, table_schema, field_name)", table_name);
            //sql.Append(" values ('prm_9', 'nftul_data', 'nzp');");
            //ret = ExecSQL(conn_db, sql.ToString(), true);
            return ret;
        }
    }
}
