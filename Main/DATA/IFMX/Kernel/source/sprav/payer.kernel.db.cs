using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System;
using System.Data;


namespace STCLINE.KP50.DataBase
{
    public partial class DbSpravKernel : DbSpravClient
    {
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
    }
}
