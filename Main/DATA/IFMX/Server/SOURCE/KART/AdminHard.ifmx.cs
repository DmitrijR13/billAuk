using System;
using System.Data;
using System.Linq;
using System.Xml.Linq;
using System.IO;
using System.Text;
using System.Collections.Generic;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;


namespace STCLINE.KP50.DataBase
{
    public class DbAdminHard : DataBaseHead
    {
        private int AddSuppInDb(IDbConnection conn_db, FileSupp fileSupp, FilesImported finder, out Returns ret)
        {
            IDbTransaction transaction = conn_db.BeginTransaction();

            DbSprav db = new DbSprav();
            Supplier supp = new Supplier();
            supp.name_supp = fileSupp.supp_name;
            supp.nzp_user = finder.nzp_user;
            ret = db.SaveSupplier(supp, transaction, conn_db);
            if (!ret.result)
            {
                if (transaction != null) transaction.Rollback();
                UpdateCommentIntoFileSupp(conn_db, fileSupp.id, "Ошибка  при добавлении поставщика");
                return 0;
            }
            fileSupp.nzp_supp = ret.tag;

            if (fileSupp.nzp_supp <= 0)
            {
                if (transaction != null) transaction.Rollback();
                UpdateCommentIntoFileSupp(conn_db, fileSupp.id, "Ошибка  при добавлении поставщика");
                return 0;
            }

            Payer payer = new Payer();
            payer.payer = payer.npayer = fileSupp.supp_name;
            payer.nzp_user = finder.nzp_user;
            payer.inn = fileSupp.inn;
            payer.kpp = fileSupp.kpp;
            payer.nzp_supp = fileSupp.nzp_supp;
            payer.nzp_type = Payer.ContragentTypes.ServiceSupplier.GetHashCode();
            payer.is_erc = 0;
            ret = db.SavePayer(payer, transaction, conn_db);
            if (!ret.result)
            {
                if (transaction != null)
                    transaction.Rollback();
                UpdateCommentIntoFileSupp(conn_db, fileSupp.id, "Ошибка  при добавлении поставщика");
                return 0;
            }

            if (transaction != null)
            {
                transaction.Commit();
            }
            return fileSupp.nzp_supp;
        }

        public int AddLsInDb(IDbConnection conn_db, IDbConnection conn_web, int nzp_dom, FilesImported finder, FileKvar fileKvar, out Returns ret)
        {
            Ls kvar = new Ls();

            kvar.nzp_wp = Points.GetPoint(finder.pref).nzp_wp;
            kvar.pref = finder.pref;
            DbTables tables = new DbTables(conn_db);
            IDataReader reader2;
            #region определить nzp_area
            string sql = "select nzp_area, nzp_geu from " + tables.dom + " where nzp_dom = " + nzp_dom;
            ret = ExecRead(conn_db, out reader2, sql, true);
            if (!ret.result)
            {
                UpdateCommentIntoFileKvar(conn_db, fileKvar.id, "Ошибка при добавлении л/с при определении nzp_area");
                return 0;
            }
            if (reader2.Read())
            {
                if (reader2["nzp_area"] != DBNull.Value) kvar.nzp_area = Convert.ToInt32(reader2["nzp_area"]);
                if (reader2["nzp_geu"] != DBNull.Value && Convert.ToInt32(reader2["nzp_geu"]) > 0) kvar.nzp_geu = Convert.ToInt32(reader2["nzp_geu"]);
            }
            reader2.Close();
            reader2.Dispose();
            #endregion

            kvar.nkvar = fileKvar.nkvar;
            kvar.nkvar_n = fileKvar.nkvar_n;

            kvar.nzp_dom = nzp_dom;
            kvar.nzp_user = finder.nzp_user;
            kvar.fio = fileKvar.fam;
            if (fileKvar.ima != "") kvar.fio += " " + fileKvar.ima;
            if (fileKvar.otch != "") kvar.fio += " " + fileKvar.otch;
            kvar.chekexistls = 0;

            if (fileKvar.ls_type == 1) kvar.typek = 1;
            else if (fileKvar.ls_type == 2) kvar.typek = 3;
            else
            {
                UpdateCommentIntoFileKvar(conn_db, fileKvar.id, "Поле ls_type может быть 1 или 2");
                return 0;
            }
            // kvar.typek
            kvar.stateID = Ls.States.Open.GetHashCode();
            DbAdresHard db = new DbAdresHard();


            // IDbTransaction transaction =  conn_db.BeginTransaction();
            // transaction = conn_db.BeginTransaction();

            fileKvar.nzp_kvar = db.Update(conn_db, null, conn_web, kvar, out ret);
            if (!ret.result)
            {
                UpdateCommentIntoFileDom(conn_db, fileKvar.id, "Ошибка при добавлении л/с");
                return 0;
            }

            UpdateNzpIntoFileKvar(conn_db, fileKvar.id, fileKvar.nzp_kvar, nzp_dom);
            if (fileKvar.ukas > 0) UpdatePrm4Ukas(conn_db, finder, fileKvar);



            return fileKvar.nzp_kvar;
        }


        public int AddAreaInDb(IDbConnection conn_db, IDbTransaction transaction, FileArea fileArea, FilesImported finder, out Returns ret)
        {
            
            Supplier supp = new Supplier();
            supp.name_supp = fileArea.area;
            supp.nzp_user = finder.nzp_user;
            int nzp_supp = 0;
            using (DbSprav db = new DbSprav())
            {
                ret = db.SaveSupplier(supp, transaction, conn_db);

                if (!ret.result) return 0;

                nzp_supp = ret.tag;
                if (nzp_supp <= 0)
                {
                    ret.result = false;
                    return 0;
                }

                Payer payer = new Payer();
                payer.payer = payer.npayer = fileArea.area;
                payer.nzp_user = finder.nzp_user;
                payer.inn = fileArea.inn;
                payer.kpp = fileArea.kpp;
                payer.nzp_supp = nzp_supp;
                payer.nzp_type = Payer.ContragentTypes.UK.GetHashCode();
                payer.is_erc = 1;

                ret = db.SavePayer(payer, transaction, conn_db);
            }
            if (!ret.result) return 0;
            int nzp_payer = ret.tag;
            if (nzp_payer <= 0)
            {
                ret.result = false;
                return 0;
            }

            Area area = new Area();
            area.nzp_supp = nzp_supp;
            area.nzp_user = finder.nzp_user;
            area.area = fileArea.area;
            using (DbAdres dba = new DbAdres())
            {
                ret = dba.SaveArea(area, transaction, conn_db);
            }
            if (!ret.result) return 0;

            return ret.tag;
        }

        public Returns UploadAreaInDb(FilesImported finder, bool add, IDbConnection conn_db)
        {
            Returns ret = Utils.InitReturns();


            string sql = "";
            IDataReader reader = null, reader2;

            int nzp_supp, nzp_area;
            int counter_update = 0, counter_insert = 0, counter = 0;
            DbTables tables = new DbTables(conn_db);

            string where = "";
            if (add)
            {
#if PG
                where += " and coalesce(nzp_area,0) = 0";
#else
                where += " and nvl(nzp_area,0) = 0";
#endif
            }

            try
            {
                sql = "select id, area, jur_address, " +
                      "fact_address, inn, kpp, rs, bank, bik, ks, nzp_area from " + tables.file_area +
                      " where nzp_file = " + finder.nzp_file + where;
                ret = ExecRead(conn_db, out reader, sql, true);
                if (!ret.result)
                {
                    // conn_db.Close();
                    return ret;
                }
                IDbTransaction transaction = null; // conn_db.BeginTransaction();
                while (reader.Read())
                {
                    counter++;
                    FileArea fileArea = new FileArea();
                    if (reader["id"] != DBNull.Value) fileArea.id = Convert.ToInt32(reader["id"]);
                    if (reader["area"] != DBNull.Value) fileArea.area = Convert.ToString(reader["area"]).Trim();
                    if (reader["inn"] != DBNull.Value) fileArea.inn = Convert.ToString(reader["inn"]).Trim();
                    if (reader["kpp"] != DBNull.Value) fileArea.kpp = Convert.ToString(reader["kpp"]).Trim();

                    if (add)
                    {
                        nzp_area = AddAreaInDb(conn_db, transaction, fileArea, finder, out ret);
                        if (!ret.result)
                        {
                            reader.Close();
                            reader.Dispose();
                            //   conn_db.Close();
                            return ret;
                        }
                        counter_insert++;
                    }
                    else
                    {
                        sql = "select nzp_supp from " + tables.payer + " where trim(inn) = '" + fileArea.inn + "'" +
                              " and trim(kpp) = '" + fileArea.kpp + "'";
                        ret = ExecRead(conn_db, transaction, out reader2, sql, true);
                        if (!ret.result)
                        {
                            //if (transaction != null) transaction.Rollback();
                            reader.Close();
                            reader.Dispose();
                            //conn_db.Close();
                            return ret;
                        }

                        nzp_supp = 0;
                        nzp_area = 0;
                        if (reader2.Read())
                            if (reader2["nzp_supp"] != DBNull.Value) nzp_supp = Convert.ToInt32(reader2["nzp_supp"]);
                        reader2.Close();
                        if (nzp_supp > 0)
                        {
                            sql = "select nzp_area from " + tables.area + " where nzp_supp = " + nzp_supp;
                            ret = ExecRead(conn_db, transaction, out reader2, sql, true);
                            if (!ret.result)
                            {
                                //     if (transaction != null) transaction.Rollback();
                                reader.Close();
                                reader.Dispose();
                                //conn_db.Close();
                                return ret;
                            }
                            if (reader2.Read())
                                if (reader2["nzp_area"] != DBNull.Value) nzp_area = Convert.ToInt32(reader2["nzp_area"]);
                            reader2.Close();
                            reader2.Dispose();
                        }
                    }

                    if (nzp_area > 0)
                    {
                        sql = "update " + tables.file_area + " set nzp_area = " + nzp_area + " where id = " + fileArea.id;
                        ret = ExecSQL(conn_db, transaction, sql, true);
                        if (!ret.result)
                        {
                            if (transaction != null) transaction.Rollback();

                            reader.Close();
                            reader.Dispose();
                            // conn_db.Close();
                            return ret;
                        }
                        counter_update++;
                    }

                }
                reader.Close();
                reader.Dispose();

                if (transaction != null) transaction.Commit();

                if (add) ret.text = "Добавлено " + counter_insert + " из " + counter;
                else ret.text = "Обновлено " + counter_update + " из " + counter;
            }
            catch (Exception ex)
            {
                CloseReader(ref reader);
                conn_db.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка загрузки Управляющих организаций UploadAreaInDb " + err, MonitorLog.typelog.Error, 20, 201, true);
                return ret;
            }

            return ret;
        }


        public Returns UploadAreaInDb(FilesImported finder, bool add)
        {
            Returns ret = Utils.InitReturns();
            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;
            ret = UploadAreaInDb(finder, false, conn_db);
            conn_db.Close();
            return ret;

        }


        public Returns UploadAreaInDb(FilesImported finder)
        {
            return UploadAreaInDb(finder, false);
        }

        #region для UploadDomInDb
        private int AddDomInDb(IDbConnection conn_db, int nzp_area, FilesImported finder, FileDom fileDom, out Returns ret)
        {
            IDataReader reader2 = null;
            DbTables tables = new DbTables(conn_db);
            Dom dom = new Dom();
            dom.pref = Points.PointList[0].pref;
            dom.nzp_wp = Points.PointList[0].nzp_wp;

            #region определить nzp_stat
            string sql = "select nzp_stat from " + tables.town + " where nzp_town = " + fileDom.nzp_town;
            ret = ExecRead(conn_db, out reader2, sql, true);
            if (!ret.result)
            {
                UpdateCommentIntoFileDom(conn_db, fileDom.id, "Ошибка при добавлении дома при определении nzp_stat");
                return 0;
            }
            if (reader2.Read()) if (reader2["nzp_stat"] != DBNull.Value) dom.nzp_stat = Convert.ToInt32(reader2["nzp_stat"]);
            reader2.Close();
            #endregion

            #region определить nzp_land
            sql = "select nzp_land from " + tables.stat + " where nzp_stat = " + dom.nzp_stat;
            ret = ExecRead(conn_db, out reader2, sql, true);
            if (!ret.result)
            {
                UpdateCommentIntoFileDom(conn_db, fileDom.id, "Ошибка при добавлении дома при определении nzp_land");
                return 0;
            }
            if (reader2.Read()) if (reader2["nzp_land"] != DBNull.Value) dom.nzp_land = Convert.ToInt32(reader2["nzp_land"]);
            reader2.Close();
            #endregion


            dom.nkor = fileDom.nkor;
            if (fileDom.nkor.Trim() == "") dom.nkor = "-";
            dom.nzp_ul = fileDom.nzp_ul;
            dom.nzp_area = nzp_area;
            dom.ndom = fileDom.ndom;
            dom.nzp_user = finder.nzp_user;
            dom.nzp_raj = fileDom.nzp_raj;
            DbAdresHard db = new DbAdresHard();
            fileDom.nzp_dom = db.Update(conn_db, dom, out ret);
            if (!ret.result)
            {
                if (fileDom.nzp_dom > 0)
                {
                    // все нормально , такой дом существует нужно его использовать 
                }
                else
                {
                    UpdateCommentIntoFileDom(conn_db, fileDom.id, "Ошибка при добавлении дома");
                    return 0;
                }
            }
            UpdateNzpIntoFileDom(conn_db, fileDom.id, fileDom.nzp_ul, fileDom.nzp_dom);
            if (fileDom.ukds > 0) UpdatePrm4Ukds(conn_db, finder, fileDom);

            return fileDom.nzp_dom;
        }

        private void UpdatePrm4Ukds(IDbConnection conn_db, FilesImported file, FileDom dom)
        {
            DbParameters db = new DbParameters();
            Param finder = new Param();
            finder.nzp_user = file.nzp_user;
            finder.pref = file.pref;
            finder.webLogin = file.webLogin;
            finder.webUname = file.webUname;
            finder.dat_s = "1." + DateTime.Now.Month.ToString() + "." + DateTime.Now.Year.ToString();
            finder.nzp_prm = 866;
            finder.val_prm = dom.ukds.ToString();
            finder.prm_num = 4;
            finder.nzp = dom.nzp_dom;

            Returns ret = db.SavePrm(conn_db, null, finder);
            if (!ret.result)
            {
                UpdateCommentIntoFileDom(conn_db, dom.id, "Ошибка при добавлении параметра ukds");
            }
        }

        private void UpdateCommentIntoFileDom(IDbConnection conn_db, decimal id, string text)
        {
#if PG
            string pref_data = Points.Pref + "_data.";
#else
            string pref_data = Points.Pref + "_data@" + DBManager.getServer(conn_db) + ":";
#endif
            string sql = "update " + pref_data + "file_dom set comment = '" + text + "' where id = " + id;
            ExecSQL(conn_db, sql, true);
        }

        private void UpdateNzpIntoFileDom(IDbConnection conn_db, decimal id, int nzp_ul, int nzp_dom)
        {
#if PG
            string pref_data = Points.Pref + "_data.";
#else
            string pref_data = Points.Pref + "_data@" + DBManager.getServer(conn_db) + ":";
#endif
            string sql = "update " + pref_data + "file_dom set nzp_ul = " + nzp_ul + " , nzp_dom = " + nzp_dom + ", comment='' where id = " + id;
            ExecSQL(conn_db, sql, true);
        }

        private int FindUl(IDbConnection conn_db, ref FileDom finder, out Returns ret)
        {
#if PG
            string pref_data = Points.Pref + "_data.";
#else
            string pref_data = Points.Pref + "_data@" + DBManager.getServer(conn_db) + ":";
#endif
            string sql = "select nzp_town from " + pref_data + "s_town where upper(trim(town)) = " + Utils.EStrNull(finder.town.ToUpper().Trim().Replace(".", ""));

            IDataReader reader1;
            ret = ExecRead(conn_db, out reader1, sql, true);
            if (!ret.result)
            {
                // проверить на менее жесткие условия 
                reader1.Close();
                sql = "select nzp_town from " + pref_data + "s_town where upper(trim(replace(town,' Г','')) = " + Utils.EStrNull(finder.town.ToUpper().Trim().Replace(" Г", ""));
                ret = ExecRead(conn_db, out reader1, sql, true);
                return 0;
            }
            if (reader1.Read()) if (reader1["nzp_town"] != DBNull.Value) finder.nzp_town = Convert.ToInt32(reader1["nzp_town"]);
            reader1.Close();
            if (finder.nzp_town <= 0) return 0;

            string rr;
            if (Utils.EStrNull(finder.rajon.ToUpper().Trim()) == " NULL ") { rr = "'-'"; }
            else { rr = Utils.EStrNull(finder.rajon.ToUpper().Trim()); };
            sql = "select nzp_raj from " + pref_data + "s_rajon where nzp_town = " + finder.nzp_town + " and upper(trim(rajon)) = " + rr;
            ret = ExecRead(conn_db, out reader1, sql, true);
            if (!ret.result) return 0;
            if (reader1.Read()) if (reader1["nzp_raj"] != DBNull.Value) finder.nzp_raj = Convert.ToInt32(reader1["nzp_raj"]);
            reader1.Close();
            if (finder.nzp_raj <= 0) return 0;

            sql = "select nzp_ul from " + pref_data + "s_ulica where nzp_raj = " + finder.nzp_raj + " and upper(trim(ulica)) = " + Utils.EStrNull(finder.ulica.ToUpper().Trim());
            ret = ExecRead(conn_db, out reader1, sql, true);
            if (!ret.result) return 0;
            if (reader1.Read()) if (reader1["nzp_ul"] != DBNull.Value) finder.nzp_ul = Convert.ToInt32(reader1["nzp_ul"]);
            reader1.Close();

            if (finder.nzp_ul <= 0)
            {
                sql = "select nzp_ul from " + pref_data + "s_ulica where nzp_raj = " + finder.nzp_raj + " and replace(replace(upper(trim(ulica)),' ПР-КТ',' ПР'),' (П.ТРОИЦКИЙ)','') = " +
                   "replace(replace(" + Utils.EStrNull(finder.ulica.ToUpper().Trim()) + ",' (П.ТРОИЦКИЙ)',''),'ПАРКОВАЯ УЛ' ,'ПАРКОВАЯ УЛ') ";
                ret = ExecRead(conn_db, out reader1, sql, true);
                if (!ret.result) return 0;
                if (reader1.Read()) if (reader1["nzp_ul"] != DBNull.Value) finder.nzp_ul = Convert.ToInt32(reader1["nzp_ul"]);
                reader1.Close();
                if (finder.nzp_ul <= 0)
                {
                    sql = "select nzp_ul from " + pref_data + "s_ulica where nzp_raj = " + finder.nzp_raj +
                        " and replace(replace(upper(trim(ulica)),' УЛ','') ,' (П.ТРОИЦКИЙ)','')=replace(replace( "
                    + Utils.EStrNull(finder.ulica.ToUpper().Trim()) + ",' УЛ',''),' (П.ТРОИЦКИЙ)','')";
                    ret = ExecRead(conn_db, out reader1, sql, true);
                    if (!ret.result) return 0;
                    if (reader1.Read()) if (reader1["nzp_ul"] != DBNull.Value) finder.nzp_ul = Convert.ToInt32(reader1["nzp_ul"]);
                    reader1.Close();
                }
            }

            return finder.nzp_ul;
        }

        private Dom FindDom(IDbConnection conn_db, FileDom finder, int nzp_area, out Returns ret)
        {
            Dom d = new Dom();
#if PG
            string pref_data = Points.Pref + "_data.";
#else
            string pref_data = Points.Pref + "_data@" + DBManager.getServer(conn_db) + ":";
#endif
            string nkor = finder.nkor.Trim().ToUpper();
            if (finder.nkor.Trim() == "-") nkor = "";

            string sql = "select nzp_dom, pref from " + pref_data + "dom where nzp_ul = " + finder.nzp_ul +
                         " and nzp_area = " + nzp_area + " and trim(upper(ndom)) = '" + finder.ndom.Trim().ToUpper() + "'" +
#if PG
                            " and case when coalesce(nkor,'0') = '-' then '' else trim(upper(nkor)) end = '" + nkor + "'";
#else
 " and case when nvl(nkor,'0') = '-' then '' else trim(upper(nkor)) end = '" + nkor + "'";
#endif
            IDataReader reader1;
            ret = ExecRead(conn_db, out reader1, sql, true);
            if (!ret.result) return d;

            if (reader1.Read())
            {
                if (reader1["nzp_dom"] != DBNull.Value) d.nzp_dom = Convert.ToInt32(reader1["nzp_dom"]);
                if (reader1["pref"] != DBNull.Value) d.pref = Convert.ToString(reader1["pref"]);
            }
            reader1.Close();
            return d;
        }

        private Dom FindDom(IDbConnection conn_db, FileDom finder, int nzp_area, out Returns ret, int pmode)
        {
            if (pmode == 0) { ret.result = true; };
            Dom d = new Dom();
#if PG
            string pref_data = Points.Pref + "_data.";
#else
            string pref_data = Points.Pref + "_data@" + DBManager.getServer(conn_db) + ":";
#endif
            string nkor = finder.nkor.Trim().ToUpper();
            //if (finder.nkor.Trim() == "-") nkor = "";
            if (nzp_area > 0)
            {
                string sql = "select nzp_dom, pref from " + pref_data + "dom where nzp_ul = " + finder.nzp_ul +
                 " and trim(upper(ndom)) = '" + finder.ndom.Trim().ToUpper() + "'" +
#if PG
                " and coalesce(trim(upper(nkor)),'-')=coalesce( '" + nkor + "','-') and nzp_area =" + nzp_area.ToString();
#else
 " and nvl(trim(upper(nkor)),'-')=nvl( '" + nkor + "','-') and nzp_area =" + nzp_area.ToString();
#endif
                IDataReader reader1;
                ret = ExecRead(conn_db, out reader1, sql, true);
                if (!ret.result) return d;

                if (reader1.Read())
                {
                    if (reader1["nzp_dom"] != DBNull.Value) d.nzp_dom = Convert.ToInt32(reader1["nzp_dom"]);
                    if (reader1["pref"] != DBNull.Value) d.pref = Convert.ToString(reader1["pref"]);
                }
                reader1.Close();
                if (d.nzp_dom > 0) { return d; };
            }
            if (nkor == "") { nkor = "-"; };
            string sql1 = "select nzp_dom, pref from " + pref_data + "dom where nzp_ul = " + finder.nzp_ul +
                         " and trim(upper(ndom)) = '" + finder.ndom.Trim().ToUpper() + "'" +
#if PG
                            " and coalesce(trim(upper(nkor)),'-')=coalesce( '" + nkor + "','-')";
#else
 " and nvl(trim(upper(nkor)),'-')=nvl( '" + nkor + "','-')";
#endif
            IDataReader reader11;
            ret = ExecRead(conn_db, out reader11, sql1, true);
            if (!ret.result) return d;

            if (reader11.Read())
            {
                if (reader11["nzp_dom"] != DBNull.Value) d.nzp_dom = Convert.ToInt32(reader11["nzp_dom"]);
                if (reader11["pref"] != DBNull.Value) d.pref = Convert.ToString(reader11["pref"]);
            }
            reader11.Close();
            return d;

        }
        #endregion



        public Returns UploadDomInDb(FilesImported finder)
        {
            return UploadDomInDb(finder, true);
        }

        public Returns UploadDomInDb(FilesImported finder, bool add)
        {
            Returns ret = Utils.InitReturns();

            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;
            UploadDomInDb(finder, true, conn_db);
            return ret;
        }


        public Returns UploadDomInDb(FilesImported finder, bool add, IDbConnection conn_db)
        {
            Returns ret = Utils.InitReturns();

            // string connectionString = Points.GetConnByPref(Points.Pref);
            // IDbConnection conn_db = GetConnection(connectionString);
            // ret = OpenDb(conn_db, true);
            // if (!ret.result) return ret;

            DbTables tables = new DbTables(conn_db);
            IDataReader reader = null, reader2, reader3;

            if (Points.PointList.Count > 0)
            {
                finder.pref = Points.PointList[0].pref;
                finder.nzp_wp = Points.PointList[0].nzp_wp;
            }
            string where = "";
            //  if (add)
            //  {
            //      where += " and nvl(nzp_dom,0) = 0";
            //  }
#if PG
            where += " and coalesce(nzp_dom,0) = 0";
#else
            where += " and nvl(nzp_dom,0) = 0";
#endif

            string sql = "select id, ukds, town, rajon, ulica, ndom, nkor, area_id, " +
                         "cat_blago, etazh, build_year, total_square, mop_square, useful_square, mo_id, " +
                         "params, ls_row_number, odpu_row_number, nzp_ul, nzp_dom, nzp_raj, nzp_town " +
                         "from " + tables.file_dom + " where nzp_file = " + finder.nzp_file + where;
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                //   conn_db.Close();
                return ret;
            }

            int nzp_area = 0;
            int counter_update = 0, counter_insert = 0, counter = 0;
            try
            {
                while (reader.Read())
                {
                    bool seachUlica = false;

                    counter++;
                    FileDom fileDom = new FileDom();
                    if (reader["id"] != DBNull.Value) fileDom.id = Convert.ToDecimal(reader["id"]);
                    if (reader["ukds"] != DBNull.Value) fileDom.ukds = Convert.ToInt32(reader["ukds"]);
                    if (reader["town"] != DBNull.Value) fileDom.town = Convert.ToString(reader["town"]).Trim();
                    if (reader["rajon"] != DBNull.Value) fileDom.rajon = Convert.ToString(reader["rajon"]).Trim();
                    if (reader["ulica"] != DBNull.Value) fileDom.ulica = Convert.ToString(reader["ulica"]).Trim();
                    if (reader["ndom"] != DBNull.Value) fileDom.ndom = Convert.ToString(reader["ndom"]).Trim();
                    if (reader["nkor"] != DBNull.Value) fileDom.nkor = Convert.ToString(reader["nkor"]).Trim();
                    if (reader["area_id"] != DBNull.Value) fileDom.area_id = Convert.ToDecimal(reader["area_id"]);
                    if (reader["cat_blago"] != DBNull.Value) fileDom.cat_blago = Convert.ToString(reader["cat_blago"]).Trim();
                    if (reader["etazh"] != DBNull.Value) fileDom.etazh = Convert.ToInt32(reader["etazh"]);
                    if (reader["build_year"] != DBNull.Value) fileDom.build_year = Convert.ToDateTime(reader["build_year"]).ToShortDateString();
                    if (reader["total_square"] != DBNull.Value) fileDom.total_square = Convert.ToDecimal(reader["total_square"]);
                    if (reader["mop_square"] != DBNull.Value) fileDom.mop_square = Convert.ToDecimal(reader["mop_square"]);
                    if (reader["useful_square"] != DBNull.Value) fileDom.useful_square = Convert.ToDecimal(reader["useful_square"]);
                    if (reader["mo_id"] != DBNull.Value) fileDom.mo_id = Convert.ToDecimal(reader["mo_id"]);
                    if (reader["params"] != DBNull.Value) fileDom.params_ = Convert.ToString(reader["params"]).Trim();
                    if (reader["ls_row_number"] != DBNull.Value) fileDom.ls_row_number = Convert.ToInt32(reader["ls_row_number"]);
                    if (reader["odpu_row_number"] != DBNull.Value) fileDom.odpu_row_number = Convert.ToInt32(reader["odpu_row_number"]);
                    if (reader["nzp_ul"] != DBNull.Value) fileDom.nzp_ul = Convert.ToInt32(reader["nzp_ul"]);
                    if (reader["nzp_dom"] != DBNull.Value) fileDom.nzp_dom = Convert.ToInt32(reader["nzp_dom"]);
                    if (reader["nzp_raj"] != DBNull.Value) fileDom.nzp_raj = Convert.ToInt32(reader["nzp_raj"]);
                    if (reader["nzp_town"] != DBNull.Value) fileDom.nzp_raj = Convert.ToInt32(reader["nzp_town"]);

                    if (fileDom.area_id <= 0)
                    {
                        UpdateCommentIntoFileDom(conn_db, fileDom.id, "Поле area_id должно быть заполнено");
                        continue;
                    }

                    #region определить nzp_area
                    sql = "select nzp_area from " + tables.file_area + " where id = " + fileDom.area_id;
                    ret = ExecRead(conn_db, out reader2, sql, true);
                    if (!ret.result)
                    {
                        UpdateCommentIntoFileDom(conn_db, fileDom.id, "Не удалось получить nzp_area из таблицы file_area");
                        continue;
                    }
                    nzp_area = 0;
                    if (reader2.Read()) if (reader2["nzp_area"] != DBNull.Value) nzp_area = Convert.ToInt32(reader2["nzp_area"]);
                    reader2.Close();
                    if (nzp_area <= 0)
                    {
                        UpdateCommentIntoFileDom(conn_db, fileDom.id, "Не удалось получить nzp_area из таблицы file_area");
                        continue;
                    }
                    #endregion

                    if (add)
                    {
                        AddDomInDb(conn_db, nzp_area, finder, fileDom, out ret);
                        counter_insert++;
                        continue;
                    }
                    else
                    {
                        if (fileDom.ukds > 0)
                        {
                            bool is_continue = false;
                            foreach (_Point point in Points.PointList)
                            {
                                #region найти nzp_dom (и nzp_area для него - loc_nzp_area) по ukds
#if PG
                                string pref = point.pref + "_data.";
#else
                                string pref = point.pref + "_data@" + DBManager.getServer(conn_db) + ":";
#endif
                                sql = "select nzp from " + pref + "prm_4 where nzp_prm = 866 and trim(val_prm) = " + fileDom.ukds + " " +
#if PG
                                     "and now() " +
#else
 "and current " +
#endif
 " between dat_s and dat_po and is_actual != 100";
                                ret = ExecRead(conn_db, out reader2, sql, true);
                                if (!ret.result)
                                {
                                    UpdateCommentIntoFileDom(conn_db, fileDom.id, "Ошибка поиска дома по ukds");
                                    is_continue = true;
                                    break;
                                }

                                int loc_nzp_area = 0;
                                while (reader2.Read())
                                {
                                    fileDom.nzp_dom = 0;
                                    if (reader2["nzp"] != DBNull.Value) fileDom.nzp_dom = Convert.ToInt32(reader2["nzp"]);

                                    #region определить nzp_area для дома loc_nzp_area
                                    sql = "select nzp_area from " + tables.dom + " where nzp_dom =" + fileDom.nzp_dom;
                                    ret = ExecRead(conn_db, out reader3, sql, true);
                                    if (!ret.result)
                                    {
                                        UpdateCommentIntoFileDom(conn_db, fileDom.id, "Ошибка при получении nzp_area для дома nzp_dom = " + fileDom.nzp_dom);
                                        is_continue = true;
                                        break;
                                    }
                                    loc_nzp_area = 0;
                                    int i = 0;
                                    while (reader3.Read())
                                    {
                                        if (i > 0)
                                        {
                                            is_continue = true;
                                            UpdateCommentIntoFileDom(conn_db, fileDom.id, "Для ukds = " + fileDom.ukds + " несколько домов");
                                            break;
                                        }
                                        if (reader3["nzp_area"] != DBNull.Value) loc_nzp_area = Convert.ToInt32(reader3["nzp_area"]);
                                        i++;
                                    }
                                    reader3.Close();
                                    #endregion

                                    if (is_continue || loc_nzp_area == nzp_area) break;
                                    else fileDom.nzp_dom = 0;
                                }
                                reader2.Close();
                                #endregion

                                if (fileDom.nzp_dom > 0 && loc_nzp_area == nzp_area)
                                {
                                    finder.pref = point.pref;
                                    break;
                                }
                                else seachUlica = true;
                            }
                            if (is_continue) continue;

                            fileDom.nzp_ul = 0;
                            if (fileDom.nzp_dom > 0)
                            {
                                #region найти улицу для nzp_dom и nzp_area
#if PG
                                string pref = finder.pref + "_data.";
#else
                                string pref = finder.pref + "_data@" + DBManager.getServer(conn_db) + ":";
#endif
                                sql = "select nzp_ul from " + pref + "dom where nzp_dom = " + fileDom.nzp_dom + " and nzp_area = " + nzp_area;
                                ret = ExecRead(conn_db, out reader3, sql, true);
                                if (!ret.result)
                                {
                                    UpdateCommentIntoFileDom(conn_db, fileDom.id, "Ошибка при проверке существования дома nzp_dom = " + fileDom.nzp_dom);
                                    continue;
                                }

                                if (reader3.Read()) if (reader3["nzp_ul"] != DBNull.Value) fileDom.nzp_ul = Convert.ToInt32(reader3["nzp_ul"]);
                                reader3.Close();
                                #endregion
                            }

                            if (fileDom.nzp_ul > 0)
                            {
                                counter_update++;
                                UpdateNzpIntoFileDom(conn_db, fileDom.id, fileDom.nzp_ul, fileDom.nzp_dom);
                                continue;
                            }
                            else seachUlica = true;
                        }
                        else seachUlica = true;//если ukds не заполнен                   

                        if (seachUlica)
                        {

                            #region найти улицу
                            fileDom.nzp_ul = FindUl(conn_db, ref fileDom, out ret);
                            if (!ret.result)
                            {
                                UpdateCommentIntoFileDom(conn_db, fileDom.id, "Ошибка в запросе при поиске улицы " + fileDom.ulica);
                                continue;
                            }
                            #endregion

                            if (fileDom.nzp_ul > 0)
                            {
                                #region найти дом
                                Dom d = FindDom(conn_db, fileDom, nzp_area, out ret, 1);
                                fileDom.nzp_dom = d.nzp_dom;
                                finder.pref = d.pref;
                                if (!ret.result)
                                {
                                    UpdateCommentIntoFileDom(conn_db, fileDom.id, "Ошибка в запросе при поиске дома №" + fileDom.ndom);
                                    continue;
                                }
                                #endregion

                                if (fileDom.nzp_dom > 0)
                                {
                                    counter_update++;
                                    UpdateNzpIntoFileDom(conn_db, fileDom.id, fileDom.nzp_ul, fileDom.nzp_dom);
                                    if (fileDom.ukds > 0) UpdatePrm4Ukds(conn_db, finder, fileDom);
                                    continue;
                                }
                            }
                            else
                            {
                                UpdateCommentIntoFileDom(conn_db, fileDom.id, "Не найдена улица " + fileDom.ulica);
                                continue;
                            }

                        }
                    }
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                CloseReader(ref reader);
                //   conn_db.Close();

                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка загрузки домов UploadDomInDb " + err, MonitorLog.typelog.Error, 20, 201, true);
                return ret;
            }
            // conn_db.Close();
            if (add) ret.text = "Добавлено " + counter_insert + " из " + counter;
            else ret.text = "Обновлено " + counter_update + " из " + counter;
            return ret;
        }


        public Returns UploadLsInDb(FilesImported finder)
        {
            return UploadLsInDb(finder, true);
        }

        public Returns UploadLsInDb(FilesImported finder, bool add)
        {
            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            Returns ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;

            ret = UploadLsInDb(conn_db, finder, add);

            conn_db.Close();

            return ret;
        }
        public Returns UploadLsInDb(IDbConnection conn_db, FilesImported finder, bool add)
        {
            Returns ret = Utils.InitReturns();

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result)
            {
                return ret;
            }
            ret = UploadLsInDb(conn_db, conn_web, finder, add);
            conn_web.Close();
            return ret;
        }

        public Returns UploadLsInDb(IDbConnection conn_db, IDbConnection conn_web, FilesImported finder, bool add)
        {
            Returns ret = Utils.InitReturns();
            /*
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result)
            {
                return ret;
            }
            */
#if PG
            string pref_data = Points.Pref + "_data.";
#else
            string pref_data = Points.Pref + "_data@" + DBManager.getServer(conn_db) + ":";
#endif
            IDataReader reader = null, reader2;
            string where = "";
            if (add)
            {
#if PG
                where += " and coalesce(nzp_kvar,0) = 0";
#else
                where += " and nvl(nzp_kvar,0) = 0";
#endif
            }
            string sql = "select id, ukas, dom_id, nkvar, nkvar_n, ls_type, fam, ima, otch from " +
#if PG
                        pref_data + "file_kvar  where nzp_kvar is null and length(trim(coalesce(comment,' ')))=0 and nzp_file = " + finder.nzp_file + where;
#else
 pref_data + "file_kvar  where nzp_kvar is null and length(trim(nvl(comment,' ')))=0 and nzp_file = " + finder.nzp_file + where;
#endif
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                return ret;
            }

            int counter_update = 0, counter_insert = 0, counter = 0;
            int nzp_dom = 0;
            bool searchLs;
            try
            {
                while (reader.Read())
                {
                    counter++;

                    FileKvar fileKvar = new FileKvar();
                    if (reader["id"] != DBNull.Value) fileKvar.id = Convert.ToDecimal(reader["id"]);
                    if (reader["ukas"] != DBNull.Value) fileKvar.ukas = Convert.ToInt32(reader["ukas"]);
                    if (reader["dom_id"] != DBNull.Value) fileKvar.dom_id = Convert.ToDecimal(reader["dom_id"]);
                    if (reader["nkvar"] != DBNull.Value) fileKvar.nkvar = Convert.ToString(reader["nkvar"]).Trim();
                    if (reader["nkvar_n"] != DBNull.Value) fileKvar.nkvar_n = Convert.ToString(reader["nkvar_n"]).Trim();
                    if (reader["ls_type"] != DBNull.Value) fileKvar.ls_type = Convert.ToInt32(reader["ls_type"]);
                    if (reader["fam"] != DBNull.Value) fileKvar.fam = Convert.ToString(reader["fam"]).Trim();
                    if (reader["ima"] != DBNull.Value) fileKvar.ima = Convert.ToString(reader["ima"]).Trim();
                    if (reader["otch"] != DBNull.Value) fileKvar.otch = Convert.ToString(reader["otch"]).Trim();

                    if (fileKvar.dom_id <= 0)
                    {
                        UpdateCommentIntoFileKvar(conn_db, fileKvar.id, "Поле dom_id должно быть заполнено");

                        continue;
                    }

                    #region определить nzp_dom
                    sql = "select nzp_dom from " + pref_data + "file_dom where id = " + fileKvar.dom_id + " and nzp_file = " + finder.nzp_file;
                    ret = ExecRead(conn_db, out reader2, sql, true);
                    if (!ret.result)
                    {
                        sql = "select nzp_dom from " + pref_data + "file_dom where id = " + fileKvar.dom_id + " and nzp_dom>0 ";
                        ret = ExecRead(conn_db, out reader2, sql, true);
                        if (!ret.result)
                        {
                            UpdateCommentIntoFileKvar(conn_db, fileKvar.id, "Не удалось получить nzp_dom из таблицы file_dom");
                            continue;
                        }
                    }
                    nzp_dom = 0;
                    if (reader2.Read()) if (reader2["nzp_dom"] != DBNull.Value) nzp_dom = Convert.ToInt32(reader2["nzp_dom"]);
                    reader2.Close();
                    reader2.Dispose();
                    if (nzp_dom <= 0)
                    {
                        UpdateCommentIntoFileKvar(conn_db, fileKvar.id, "Не удалось получить nzp_dom из таблицы file_dom");

                        continue;
                    }
                    #endregion

                    #region определить pref дома finder.pref
                    sql = "select pref from " + pref_data + "dom where nzp_dom = " + nzp_dom;
                    ret = ExecRead(conn_db, out reader2, sql, true);
                    if (!ret.result)
                    {
                        UpdateCommentIntoFileKvar(conn_db, fileKvar.id, "Не удалось определить pref для nzp_dom = " + nzp_dom);

                        continue;
                    }
                    finder.pref = "";
                    if (reader2.Read()) if (reader2["pref"] != DBNull.Value) finder.pref = Convert.ToString(reader2["pref"]).Trim();
                    reader2.Close();
                    reader2.Dispose();
                    if (finder.pref == "")
                    {
                        UpdateCommentIntoFileKvar(conn_db, fileKvar.id, "Не удалось определить pref для nzp_dom = " + nzp_dom);

                        continue;
                    }
                    #endregion

                    if (add)
                    {
                        AddLsInDb(conn_db, conn_web, nzp_dom, finder, fileKvar, out ret);
                        continue;
                    }
                    else
                    {
                        searchLs = false;

                        if (fileKvar.ukas > 0)
                        {
                            bool is_continue = false;
                            searchLs = true;
                            #region найти nzp_kvar по ukas
#if PG
                            string pref = finder.pref + "_data.";
#else
                            string pref = finder.pref + "_data@" + DBManager.getServer(conn_db) + ":";
#endif
                            sql = "select nzp from " + pref + "prm_15 where nzp_prm = 162 and trim(val_prm) = " + fileKvar.ukas + " " +
#if PG
                                     "and now() between dat_s and dat_po and is_actual != 100";
#else
 "and current between dat_s and dat_po and is_actual != 100";
#endif
                            ret = ExecRead(conn_db, out reader2, sql, true);
                            if (!ret.result)
                            {
                                UpdateCommentIntoFileKvar(conn_db, fileKvar.id, "Ошибка поиска л/с по ukas");
                                is_continue = true;
                            }
                            else
                            {
                                while (reader2.Read())
                                {
                                    fileKvar.nzp_kvar = 0;
                                    if (reader2["nzp"] != DBNull.Value) fileKvar.nzp_kvar = Convert.ToInt32(reader2["nzp"]);

                                    #region проверить, что nzp соостветствует дому
                                    sql = "select count(*) from " + pref_data + "kvar where nzp_dom = " + nzp_dom + " and nzp_kvar =" + fileKvar.nzp_kvar;
                                    object count = ExecScalar(conn_db, sql, out ret, true);
                                    int records;
                                    try { records = Convert.ToInt32(count); }
                                    catch (Exception e)
                                    {
                                        UpdateCommentIntoFileKvar(conn_db, fileKvar.id, "Ошибка при определении количества записей: " + (Constants.Debug ? e.Message : ""));
                                        is_continue = true;
                                        break;
                                    }
                                    if (records > 0)
                                    {
                                        counter_update++;
                                        UpdateNzpIntoFileKvar(conn_db, fileKvar.id, fileKvar.nzp_kvar, nzp_dom);
                                        is_continue = true;
                                        searchLs = false;
                                    }
                                    #endregion
                                }
                                reader2.Close();
                                reader2.Dispose();
                            }
                            #endregion

                            if (is_continue)
                            {

                                continue;
                            }
                        }
                        else searchLs = true;//если ukas не заполнен

                        if (searchLs)
                        {
                            #region найти л/с
                            Ls ls = FindKvar(conn_db, fileKvar, nzp_dom, pref_data, out ret);
                            fileKvar.nzp_kvar = ls.nzp_kvar;
                            if (!ret.result)
                            {
                                UpdateCommentIntoFileKvar(conn_db, fileKvar.id, "Ошибка в запросе при поиске л/с кв." + fileKvar.nkvar);

                                continue;
                            }
                            #endregion

                            if (fileKvar.nzp_kvar > 0)
                            {
                                counter_update++;
                                UpdateNzpIntoFileKvar(conn_db, fileKvar.id, fileKvar.nzp_kvar, nzp_dom);

                                if (fileKvar.ukas > 0) UpdatePrm4Ukas(conn_db, finder, fileKvar);

                                continue;
                            }
                        }
                    }

                }
            }
            catch (Exception ex)
            {

                CloseReader(ref reader);
                //conn_web.Close();
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка загрузки домов UploadLsInDb " + err, MonitorLog.typelog.Error, 20, 201, true);
                return ret;
            }

            //conn_web.Close();
            if (add) ret.text = "Добавлено " + counter_insert + " из " + counter;
            else ret.text = "Обновлено " + counter_update + " из " + counter;
            return ret;
        }



        #region Функции для UploadLsInDb
        private void UpdateCommentIntoFileKvar(IDbConnection conn_db, decimal id, string text)
        {
#if PG
            string pref_data = Points.Pref + "_data.";
#else
            string pref_data = Points.Pref + "_data@" + DBManager.getServer(conn_db) + ":";
#endif
            string sql = "update " + pref_data + "file_kvar set comment = '" + text + "' where id = " + id;
            ExecSQL(conn_db, sql, true);
        }

        private void UpdateNzpIntoFileKvar(IDbConnection conn_db, decimal id, int nzp_kvar, int nzp_dom)
        {
#if PG
            string pref_data = Points.Pref + "_data.";
#else
            string pref_data = Points.Pref + "_data@" + DBManager.getServer(conn_db) + ":";
#endif
            string sql = "update " + pref_data + "file_kvar set nzp_kvar = " + nzp_kvar +
                ", nzp_dom = " + nzp_dom + ", comment='' where id = " + id;
            ExecSQL(conn_db, sql, true);
        }

        private Ls FindKvar(IDbConnection conn_db, FileKvar finder, int nzp_dom, string pref, out Returns ret)
        {
            Ls ls = new Ls();

            string nkvar_n = finder.nkvar_n.Trim().ToUpper();
            if (finder.nkvar_n.Trim() == "-") nkvar_n = "";

            string sql = "select nzp_kvar from " + pref + "kvar where nzp_dom = " + nzp_dom +
                         " and trim(upper(nkvar)) = '" + finder.nkvar.Trim().ToUpper() + "'" +
#if PG
                         " and case when coalesce(nkvar_n,'0') = '-' then '' else trim(upper(nkvar_n)) end = '" + nkvar_n + "'";
#else
 " and case when nvl(nkvar_n,'0') = '-' then '' else trim(upper(nkvar_n)) end = '" + nkvar_n + "'";
#endif
            IDataReader reader1;
            ret = ExecRead(conn_db, out reader1, sql, true);
            if (!ret.result) return ls;

            if (reader1.Read())
                if (reader1["nzp_kvar"] != DBNull.Value) ls.nzp_kvar = Convert.ToInt32(reader1["nzp_kvar"]);
            reader1.Close();
            reader1.Dispose();
            return ls;
        }

        private void UpdatePrm4Ukas(IDbConnection conn_db, FilesImported file, FileKvar kvar)
        {
            DbParameters db = new DbParameters();
            Param finder = new Param();
            finder.nzp_user = file.nzp_user;
            finder.pref = file.pref;
            finder.webLogin = file.webLogin;
            finder.webUname = file.webUname;
            finder.dat_s = "1." + DateTime.Now.Month.ToString() + "." + DateTime.Now.Year.ToString();
            finder.nzp_prm = 162;
            finder.val_prm = kvar.ukas.ToString();
            finder.prm_num = 15;
            finder.nzp = kvar.nzp_kvar;

            Returns ret = db.SavePrm(conn_db, null, finder);
            if (!ret.result)
            {
                UpdateCommentIntoFileKvar(conn_db, kvar.id, "Ошибка при добавлении параметра ukas");
            }
        }
        #endregion


        public Returns UploadSuppInDb(FilesImported finder)
        {
            return UploadSuppInDb(finder, false);
        }

        public Returns UploadSuppInDb(FilesImported finder, bool add)
        {
            Returns ret = Utils.InitReturns();

            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;

            DbTables tables = new DbTables(conn_db);
            string where = "";
            if (add)
            {
#if PG
                where += " and coalesce(nzp_supp,0) = 0";
#else
                where += " and nvl(nzp_supp,0) = 0";
#endif
            }
            string sql = "select * from " + tables.file_supp + " where nzp_file = " + finder.nzp_file + where;
            IDataReader reader = null, reader2;
            int counter_update = 0, counter_insert = 0, counter = 0;
            try
            {
                ret = ExecRead(conn_db, out reader, sql, true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return ret;
                }

                while (reader.Read())
                {
                    counter++;
                    FileSupp fileSupp = new FileSupp();
                    if (reader["id"] != DBNull.Value) fileSupp.id = Convert.ToInt32(reader["id"]);
                    if (reader["supp_name"] != DBNull.Value) fileSupp.supp_name = Convert.ToString(reader["supp_name"]).Trim();
                    if (reader["inn"] != DBNull.Value) fileSupp.inn = Convert.ToString(reader["inn"]).Trim();
                    if (reader["kpp"] != DBNull.Value) fileSupp.kpp = Convert.ToString(reader["kpp"]).Trim();

                    if (add)
                    {
                        fileSupp.nzp_supp = AddSuppInDb(conn_db, fileSupp, finder, out ret);
                        counter_insert++;
                    }
                    else
                    {
                        sql = "select nzp_supp from " + tables.payer + " where trim(inn) = " + fileSupp.inn + " and trim(kpp) = " + fileSupp.kpp;
                        ret = ExecRead(conn_db, out reader2, sql, true);
                        if (!ret.result)
                        {
                            UpdateCommentIntoFileSupp(conn_db, fileSupp.id, "Ошибка  при определении кода поставщика по инн и кпп");
                            continue;
                        }
                        if (reader2.Read()) if (reader2["nzp_supp"] != DBNull.Value) fileSupp.nzp_supp = Convert.ToInt32(reader2["nzp_supp"]);
                        reader2.Close();
                        reader2.Dispose();
                    }

                    if (fileSupp.nzp_supp > 0)
                    {
                        sql = "update " + tables.file_supp + " set nzp_supp = " + fileSupp.nzp_supp + " where id = " + fileSupp.id;
                        ret = ExecSQL(conn_db, sql, true);
                        if (!ret.result)
                        {
                            UpdateCommentIntoFileSupp(conn_db, fileSupp.id, "Ошибка  при обновлении поля nzp_supp = " + fileSupp.nzp_supp);
                            continue;
                        }
                        counter_update++;
                    }
                }
                reader.Close();
                reader.Dispose();
            }
            catch (Exception ex)
            {
                CloseReader(ref reader);
                conn_db.Close();
                ret.result = false;
                ret.text = ex.Message;

                string err = "";
                if (Constants.Viewerror) err = " \n " + ex.Message;

                MonitorLog.WriteLog("Ошибка загрузки домов UploadSuppInDb " + err, MonitorLog.typelog.Error, 20, 201, true);
                return ret;
            }
            if (add) ret.text = "Добавлено " + counter_insert + " из " + counter;
            else ret.text = "Обновлено " + counter_update + " из " + counter;
            return ret;
        }

        private void UpdateCommentIntoFileSupp(IDbConnection conn_db, decimal id, string text)
        {
            DbTables tables = new DbTables(conn_db);
            string sql = "update " + tables.file_kvar + " set comment = '" + text + "' where id = " + id;
            ExecSQL(conn_db, sql, true);
        }


        /// <summary>
        /// Функция подготовки данных для печати ЛС
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        public Returns PreparePrintInvoices(List<PointForPrepare> finder)
        {
            Returns ret = Utils.InitReturns();



            foreach (PointForPrepare pointForPrepare in finder)
            {
                //поставить фоновую задачу для каждого лок банка                
                CalcFonTask calcfon = new CalcFonTask(Points.GetCalcNum(0));
                calcfon.TaskType = CalcFonTask.Types.taskPreparePrintInvoices;
                calcfon.Status = FonTask.Statuses.New; //на выполнение    
                calcfon.nzp = pointForPrepare.mark ? 1 : 0;
                calcfon.nzpt = pointForPrepare.nzp_wp;
                calcfon.year_ = pointForPrepare.PrepareDate.Year;
                calcfon.month_ = pointForPrepare.PrepareDate.Month;

                calcfon.txt = "Операция закрытия месяца : " + Utils.GetMonthName(pointForPrepare.PrepareDate.Month) + pointForPrepare.PrepareDate.Year + "г., " + Points.GetPoint(pointForPrepare.nzp_wp).point+".";
                calcfon.nzp_user = pointForPrepare.nzp_user;
                DbCalcQueueClient dbCalc = new DbCalcQueueClient();
                ret = dbCalc.AddTask(calcfon);
                dbCalc.Close();
                if (!ret.result)
                {
                    return ret;
                }
            }

            return ret;
        }
    }
}