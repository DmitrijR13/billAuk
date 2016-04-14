using System;
using System.Data;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase
{
    public class DbClearBase : DbAdminClient
    {
        private readonly IDbConnection _conDb;

        public DbClearBase(IDbConnection con_db)
        {
            _conDb = con_db;
        }

        public Returns ClearBase(FilesDisassemble finder)
        {
            Returns ret = Utils.InitReturns();
            string sql;

            try
            {
                #region fXXX_data
                sql = "delete from " + Points.Pref + DBManager.sDataAliasRest + " kvar where nzp_kvar in" +
                      " (select nzp_kvar from " + finder.bank + DBManager.sDataAliasRest + " kvar);";
                ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                sql = "delete from " + Points.Pref + DBManager.sDataAliasRest + " dom where nzp_dom in" +
                      " (select nzp_dom from " + finder.bank + DBManager.sDataAliasRest + " dom);";
                ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                sql = "delete from " + Points.Pref + DBManager.sDataAliasRest + " prm_1 where nzp in" +
                      " (select nzp_kvar from " + finder.bank + DBManager.sDataAliasRest + " kvar); ";
                ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                sql = "delete from " + Points.Pref + DBManager.sDataAliasRest + " prm_2 where nzp in" +
                      " (select nzp_dom from " + finder.bank + DBManager.sDataAliasRest + " dom); ";
                ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                sql = "DELETE FROM " + Points.Pref + DBManager.sDataAliasRest + " prm_3 where nzp in" +
                      " (select nzp_kvar from " + finder.bank + DBManager.sDataAliasRest + " kvar); ";
                ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                sql = "DELETE FROM " + Points.Pref + DBManager.sDataAliasRest + " prm_11 where nzp in" +
                      " (select nzp_supp from  " + finder.bank + DBManager.sKernelAliasRest + "supplier);";
                ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                sql = "DELETE FROM " + Points.Pref + DBManager.sDataAliasRest + " counters_spis where nzp_counter in" +
                      " (select nzp_counter from " + finder.bank + DBManager.sDataAliasRest + " counters_spis);";
                ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                sql = "DELETE FROM " + Points.Pref + DBManager.sDataAliasRest + " counters_dom where nzp_counter in" +
                      " (select nzp_counter from " + finder.bank + DBManager.sDataAliasRest + " counters_dom);";
                ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                sql = "DELETE FROM " + Points.Pref + DBManager.sDataAliasRest + " counters where nzp_counter in" +
                      " (select nzp_counter from " + finder.bank + DBManager.sDataAliasRest + " counters_spis);";
                ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                sql = "DELETE FROM " + Points.Pref + DBManager.sDataAliasRest + " counters where nzp_counter in" +
                      " (select nzp_counter from " + finder.bank + DBManager.sDataAliasRest + " counters_dom);";
                ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                //sql = "DELETE FROM " + Points.Pref + DBManager.sDataAliasRest + " s_area where nzp_area in" +
                //   " (select nzp_area from " + finder.bank + DBManager.sDataAliasRest + " s_area);";
                //ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                //sql = "DELETE FROM " + Points.Pref + DBManager.sDataAliasRest + " s_geu where nzp_geu in" +
                //   " (select nzp_geu from " + finder.bank + DBManager.sDataAliasRest + " s_geu);";
                //ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                #endregion

                #region fXXX_kernel
                //sql = " DELETE FROM  " + Points.Pref + DBManager.sKernelAliasRest + "supplier where nzp_supp in" +
                //" (select nzp_supp from  " + finder.bank + DBManager.sKernelAliasRest + "supplier);";
                //ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                #endregion

                #region fXXX_fin_XX
                sql =
                    " SELECT yearr FROM  " + Points.Pref + DBManager.sKernelAliasRest + "s_baselist" +
                    " WHERE idtype = 4";
                DataTable dtYear = ClassDBUtils.OpenSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception).GetData();
                foreach (DataRow rr in dtYear.Rows)
                {
                    try
                    {
                        string year = rr["yearr"].ToString().Substring(2, 2);
                        sql = 
                            " DELETE FROM " + Points.Pref + "_fin_" + year + tableDelimiter +
                            "gil_sums WHERE nzp_pack_ls IN " +
                                " (SELECT nzp_pack_ls" +
                                " FROM  " + Points.Pref + "_fin_" + year + tableDelimiter +
                                "pack_ls WHERE num_ls in" +
                                    " (SELECT nzp_kvar FROM " + finder.bank + DBManager.sDataAliasRest + " kvar))";
                        ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                        sql =
                            " DELETE FROM " + Points.Pref + "_fin_" + year + tableDelimiter +
                            "pack_ls WHERE num_ls in" +
                              " (SELECT nzp_kvar FROM " + finder.bank + DBManager.sDataAliasRest + " kvar);";
                        ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                        sql =
                            " DELETE FROM " + Points.Pref + "_fin_" + year + tableDelimiter +
                            "pack WHERE nzp_pack not in" +
                              " (SELECT nzp_pack FROM " + Points.Pref + "_fin_" + year + tableDelimiter +
                              " pack_ls); ";
                        ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                    }
                    catch { }
                }
                #endregion

                #region fXXX_upload 
                //удаляем файлы
                //sql = "select nzp_file from " + Points.Pref + DBManager.sUploadAliasRest + "files_imported " +
                //      " where pref  = '" + finder.bank + "'";
                //DataTable dtnote5 = ClassDBUtils.OpenSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception).GetData();
                //foreach (DataRow rr5 in dtnote5.Rows)
                //{
                //    int nzp_file = Convert.ToInt32(rr5["nzp_file"]);

                //    finder.nzp_file = nzp_file;
                //    DbDeleteImportedFile del_file = new DbDeleteImportedFile();
                //    del_file.DeleteImportedFile(finder);
                //};

                //чистим nzp_kvar, ukas, nzp_dom из kvar
                sql = 
                    " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar" +
                    " SET (ukas, nzp_dom, nzp_kvar) = (null,null,null) " +
                    " WHERE nzp_file IN" + 
                        " (SELECT nzp_file" +
                        " FROM " + Points.Pref + DBManager.sUploadAliasRest + "files_imported" +
                        " WHERE pref='" + finder.bank + "')";
                ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);


                //чистим nzp_dom из dom
                sql = 
                    " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "file_dom" +
                    " SET nzp_dom = null " +
                    " WHERE nzp_file IN" + 
                        " (SELECT nzp_file" +
                        " FROM " + Points.Pref + DBManager.sUploadAliasRest + "files_imported" +
                        " WHERE pref='" + finder.bank + "')";
                ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);


                sql =
                    " DELETE FROM " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
                    " WHERE nzp_file IN" +
                        " (SELECT nzp_file" +
                        " FROM " + Points.Pref + DBManager.sUploadAliasRest + "files_imported" +
                        " WHERE pref='" + finder.bank + "')";
                ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);

                sql =
                    " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "file_supp" +
                    " SET nzp_supp = null " +
                    " WHERE nzp_file IN" +
                        " (SELECT nzp_file" +
                        " FROM " + Points.Pref + DBManager.sUploadAliasRest + "files_imported" +
                        " WHERE pref='" + finder.bank + "')";
                ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);


                sql =
                    " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "file_area" +
                    " SET nzp_area = null " +
                    " WHERE nzp_file IN" +
                        " (SELECT nzp_file" +
                        " FROM " + Points.Pref + DBManager.sUploadAliasRest + "files_imported" +
                        " WHERE pref='" + finder.bank + "')";
                ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);

                //sql =
                //    " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "file_services" +
                //    " SET nzp_serv = null " +
                //    " WHERE nzp_file IN" +
                //        " (SELECT nzp_file" +
                //        " FROM " + Points.Pref + DBManager.sUploadAliasRest + "files_imported" +
                //        " WHERE pref='" + finder.bank + "')";
                //ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                #endregion

                #region pref_charge_XX
                sql = 
                    " SELECT yearr FROM  " + Points.Pref + DBManager.sKernelAliasRest + "s_baselist" + 
                    " WHERE idtype = 1";
                DataTable dtYear1 = ClassDBUtils.OpenSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception).GetData();
                foreach (DataRow rr in dtYear1.Rows)
                {
                    try
                    {
                        string year = rr["yearr"].ToString().Substring(2, 2);
                        for(int i = 1; i< 13; i++)
                        {
                            #region
                            sql =
                                " DELETE FROM " + finder.bank + "_charge_" + year + tableDelimiter +
                                "charge_" + i.ToString("00");
                            ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                            try
                            {
                                sql =
                                    " DELETE FROM " + finder.bank + "_charge_" + year + tableDelimiter +
                                    "charge_" + i.ToString("00") + "_t";
                                ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                            }
                            catch { }
                            //sql =
                            //    " DELETE FROM " + finder.bank + "_charge_" + year + tableDelimiter +
                            //    "lnk_charge_" + i.ToString("00");
                            //ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                            //sql =
                            //    " DELETE FROM " + finder.bank + "_charge_" + year + tableDelimiter +
                            //    "to_supplier" + i.ToString("00");
                            //ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                            sql =
                                " DELETE FROM " + finder.bank + "_charge_" + year + tableDelimiter +
                                "fn_supplier" + i.ToString("00");
                            ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                            //sql =
                            //    " DELETE FROM " + finder.bank + "_charge_" + year + tableDelimiter +
                            //    "fr_charge_" + i.ToString("00");
                            //ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                            //sql =
                            //    " DELETE FROM " + finder.bank + "_charge_" + year + tableDelimiter +
                            //    "fn_charge_" + i.ToString("00");
                            //ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                            //sql =
                            //    " DELETE FROM " + finder.bank + "_charge_" + year + tableDelimiter +
                            //    "fn_fin_" + i.ToString("00");
                            //ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                            //sql =
                            //    " DELETE FROM " + finder.bank + "_charge_" + year + tableDelimiter +
                            //    "kvar_" + i.ToString("00");
                            //ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                            //sql =
                            //    " DELETE FROM " + finder.bank + "_charge_" + year + tableDelimiter +
                            //    "calc_sz_gil_" + i.ToString("00");
                            //ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                            //sql =
                            //    " DELETE FROM " + finder.bank + "_charge_" + year + tableDelimiter +
                            //    "calc_sz_" + i.ToString("00");
                            //ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                            //sql =
                            //    " DELETE FROM " + finder.bank + "_charge_" + year + tableDelimiter +
                            //    "calc_sz_fin_" + i.ToString("00");
                            //ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                            //sql =
                            //    " DELETE FROM " + finder.bank + "_charge_" + year + tableDelimiter +
                            //    "kvar_calc_" + i.ToString("00");
                            //ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                            //sql =
                            //    " DELETE FROM " + finder.bank + "_charge_" + year + tableDelimiter +
                            //    "lgcharge_" + i.ToString("00");
                            //ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                            //sql =
                            //    " DELETE FROM " + finder.bank + "_charge_" + year + tableDelimiter +
                            //    "counters_" + i.ToString("00");
                            //ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                            //sql =
                            //    " DELETE FROM " + finder.bank + "_charge_" + year + tableDelimiter +
                            //    "calc_gku_" + i.ToString("00");
                            //ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                            //sql =
                            //    " DELETE FROM " + finder.bank + "_charge_" + year + tableDelimiter +
                            //    "nedo_" + i.ToString("00");
                            //ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                            //sql =
                            //    " DELETE FROM " + finder.bank + "_charge_" + year + tableDelimiter +
                            //    "gil_" + i.ToString("00");
                            //ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                            //sql =
                            //    " DELETE FROM " + finder.bank + "_charge_" + year + tableDelimiter +
                            //    "delta_" + i.ToString("00");
                            //ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                            //try
                            //{
                            //    sql =
                            //        " DELETE FROM " + finder.bank + "_charge_" + year + tableDelimiter +
                            //        "reval_" + i.ToString("00");
                            //    ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                            //}
                            //catch {}
                            #endregion
                        }
                        #region
                        sql =
                            " DELETE FROM " + finder.bank + "_charge_" + year + tableDelimiter +
                            "from_supplier";
                        ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                        //sql =
                        //    " DELETE FROM " + finder.bank + "_charge_" + year + tableDelimiter +
                        //    "del_supplier";
                        //ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                        //sql =
                        //    " DELETE FROM " + finder.bank + "_charge_" + year + tableDelimiter +
                        //    "jrnl_charge";
                        //ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                        //sql =
                        //    " DELETE FROM " + finder.bank + "_charge_" + year + tableDelimiter +
                        //    "jrnl_print";
                        //ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                        //sql =
                        //    " DELETE FROM " + finder.bank + "_charge_" + year + tableDelimiter +
                        //    "sz_charge";
                        //ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                        //sql =
                        //    " DELETE FROM " + finder.bank + "_charge_" + year + tableDelimiter +
                        //    "counters_minus";
                        //ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                        //sql =
                        //    " DELETE FROM " + finder.bank + "_charge_" + year + tableDelimiter +
                        //    "lnk_counters_mns";
                        //ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                        //sql =
                        //    " DELETE FROM " + finder.bank + "_charge_" + year + tableDelimiter +
                        //    "charge_cnts";
                        //ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                        //sql =
                        //    " DELETE FROM " + finder.bank + "_charge_" + year + tableDelimiter +
                        //    "charge_nedo";
                        //ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                        //sql =
                        //    " DELETE FROM " + finder.bank + "_charge_" + year + tableDelimiter +
                        //    "sz_unl";
                        //ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                        //sql =
                        //    " DELETE FROM " + finder.bank + "_charge_" + year + tableDelimiter +
                        //    "charge_t";
                        //ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                        //sql =
                        //    " DELETE FROM " + finder.bank + "_charge_" + year + tableDelimiter +
                        //    "charge_d";
                        //ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                        //sql =
                        //    " DELETE FROM " + finder.bank + "_charge_" + year + tableDelimiter +
                        //    "charge_g";
                        //ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                        //sql =
                        //    " DELETE FROM " + finder.bank + "_charge_" + year + tableDelimiter +
                        //    "counters_ord";
                        //ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                        //sql =
                        //    " DELETE FROM " + finder.bank + "_charge_" + year + tableDelimiter +
                        //    "counters_vals";
                        //ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                        //sql =
                        //    " DELETE FROM " + finder.bank + "_charge_" + year + tableDelimiter +
                        //    "c_pgu";
                        //ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                        sql =
                            " DELETE FROM " + finder.bank + "_charge_" + year + tableDelimiter +
                            "perekidka";
                        ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                        //sql =
                        //    " DELETE FROM " + finder.bank + "_charge_" + year + tableDelimiter +
                        //    "counters_dop";
                        //ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                        #endregion
                    }
                    catch{ }
                }
                #endregion

                #region pref_data
                ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                sql = " DELETE FROM " + finder.bank + DBManager.sDataAliasRest + " kvar;";
                ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                sql = " DELETE FROM " + finder.bank + DBManager.sDataAliasRest + " dom;";
                ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                sql = " DELETE FROM " + finder.bank + DBManager.sDataAliasRest + " prm_1; ";
                ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                sql = " DELETE FROM " + finder.bank + DBManager.sDataAliasRest + " prm_2; ";
                ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                sql = " DELETE FROM " + finder.bank + DBManager.sDataAliasRest + " prm_3; ";
                ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                sql = " DELETE FROM " + finder.bank + DBManager.sDataAliasRest + " prm_11;";
                ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                sql = " DELETE FROM " + finder.bank + DBManager.sDataAliasRest + " counters_spis;";
                ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                sql = " DELETE FROM " + finder.bank + DBManager.sDataAliasRest + " counters_dom;";
                ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                sql = " DELETE FROM " + finder.bank + DBManager.sDataAliasRest + " counters;";
                ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                sql = " DELETE FROM " + finder.bank + DBManager.sDataAliasRest + " kart;";
                ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                //sql = " DELETE FROM " + finder.bank + DBManager.sDataAliasRest + " s_area;";
                //ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                //sql = " DELETE FROM " + finder.bank + DBManager.sDataAliasRest + " s_geu;";
                //ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                sql = " DELETE FROM " + finder.bank + DBManager.sDataAliasRest + " tarif;";
                ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                sql = " DELETE FROM " + finder.bank + DBManager.sDataAliasRest + " kart;";
                ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                sql = " DELETE FROM " + finder.bank + DBManager.sDataAliasRest + " gilec";
                ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                #endregion

                #region pref_kernel
                //sql = " DELETE FROM " + finder.bank + DBManager.sKernelAliasRest + "supplier;";
                //ClassDBUtils.ExecSQL(sql, _conDb, ClassDBUtils.ExecMode.Exception);
                #endregion

                //sql = "update statistics";
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка очистки банка!";
                ret.tag = -1;
                MonitorLog.WriteLog("Ошибка очистки банка! " + ex.Message, MonitorLog.typelog.Error, true);
                return ret;
            }

            return ret;
        }

    }
}
