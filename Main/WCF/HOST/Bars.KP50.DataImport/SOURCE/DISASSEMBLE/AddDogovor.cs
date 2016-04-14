using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Bars.KP50.Utils;
using FastReport.Format;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.DataImport.SOURCE.DISASSEMBLE
{
    public class DBAddDogovor : DataBaseHeadServer
    {
        public decimal getSequence_(FilesDisassemble finder, string seq,  out Returns ret )
        {
            
            string sql = "";
#if PG
            sql = " SELECT nextval('" + seq + "') ";
#else
                    sql = " SELECT " + seq + ".nextval FROM  " + Points.Pref + "_data" + tableDelimiter + "dual";
#endif
            
            decimal num =Convert.ToDecimal(ExecScalar(GetConnection(), sql, out ret, true));
            return num;
        }

        public Returns AddDogovor(FilesDisassemble finder)
        {
            MonitorLog.WriteLog("Старт разбора секции 'Договор' (AddDogovor)", MonitorLog.typelog.Info, true);
            Returns ret = new Returns();
            try
            {
                string sql;
                var table = "t_dog_" + (DateTime.Now.Ticks % 10000);

                InsertDataIntoTemp(finder, table);

                //смотрим, если такая тройка уже есть - ставим nzp_supp
                SetNzpSuppFromSupplier(table);
                
                //добавляем те договора, которых нет
                sql =
                    " SELECT distinct dog_id FROM " + table +
                    " WHERE nzp_supp IS NULL and nzp_file ="+finder.nzp_file ;
                DataTable dt = ClassDBUtils.OpenSQL(sql, GetConnection(), ClassDBUtils.ExecMode.Exception).GetData();

                MonitorLog.WriteLog("Старт добавления договоров в кол-ве:  " + dt.Rows.Count, MonitorLog.typelog.Info, true);
                foreach (DataRow r in dt.Rows)
                {
//                    string seq = Points.Pref + "_kernel" + tableDelimiter + "supplier_nzp_supp_seq";
                    
//#if PG
//                    sql = " SELECT nextval('" + seq + "') ";
//#else
//                    sql = " SELECT " + seq + ".nextval FROM  " + Points.Pref + "_data" + tableDelimiter + "dual";
//#endif
                    
                    //decimal nzp_supp = Convert.ToDecimal(ExecScalar(GetConnection(), sql, out ret, true));
                    decimal nzp_supp = getSequence_(finder, Points.Pref + sKernelAliasRest + "supplier_nzp_supp_seq", out ret );

                    if (nzp_supp <= 0 || !ret.result)
                    {
                        MonitorLog.WriteLog("Ошибка при генерации кода поставщика! Код = " + nzp_supp, MonitorLog.typelog.Info, true);
                        return new Returns(false, "Ошибка при генерации кода поставщика! Код = " + nzp_supp);
                    }

                    var dat_s = "01.01.1900";
                    var dat_po = "01.01.3000";
                    
                    sql = 
                        " SELECT DISTINCT fb.nzp_fb as nzp_fb" +
                        " FROM  " + Points.Pref + sDataAliasRest + "fn_bank fb, " + table + " t " +
                        " WHERE upper(trim(fb.rcount)) = upper(trim(t.rs)) AND dog_id = " + r["dog_id"];
                    DataTable rsdt = ClassDBUtils.OpenSQL(sql, GetConnection(), ClassDBUtils.ExecMode.Exception).GetData();
                    string rs = rsdt.Rows.Count > 0 ? rsdt.Rows[0]["nzp_fb"].ToString() : "-1";

                    if (rs == "-1")
                    {
                        // проверить существует ли заглушка 
                        sql = " select count(*) kol from  " + Points.Pref + sDataAliasRest + "fn_bank  where nzp_fb =-1 ";
                        Int32 kol = Convert.ToInt32(ExecScalar(GetConnection(), sql, out ret, true));
                        if (kol == 0)
                        {
                            sql = " select min(nzp_payer) nzp_payer from  " + Points.Pref + sKernelAliasRest + "s_payer  ";
                            string pnzp_payer = Convert.ToString(ExecScalar(GetConnection(), sql, out ret, true));
                            sql =
                                " insert into " + Points.Pref + sDataAliasRest + "fn_bank (nzp_fb ,nzp_payer, bank_name, rcount, kcount, bik, nzp_user, dat_when, nzp_payer_bank, num_count )" +
                                " values (-1 ,"+pnzp_payer+" , 'Нет сведений ', 0, 0, 0, "+finder.nzp_user+" ,current_date,"+ pnzp_payer+" ,0 ) ";
                            ret = ExecSQL(sql, true);
                        }                       
                        
                    }

                    decimal pnzp_scope = getSequence_(finder, Points.Pref + sDataAliasRest + "fn_scope_nzp_scope_seq", out ret);
                    decimal pnzp_scope_adres = getSequence_(finder, Points.Pref + sDataAliasRest + "fn_scope_adres_nzp_scope_adres_seq",  out ret);
                    
                    sql = "insert into " + Points.Pref + sDataAliasRest +"fn_scope(nzp_scope,changed_by) values ("+pnzp_scope.ToString("")+","+finder.nzp_user+")";                     
                    ret = ExecSQL(sql, true);

                    sql = " insert into " + Points.Pref + sDataAliasRest + "fn_scope_adres(nzp_scope_adres,nzp_scope,nzp_wp,changed_by,changed_on)"+
                          " values (" + pnzp_scope_adres.ToString("") + "," + pnzp_scope.ToString("") +
                          ",coalesce((select nzp_wp from " + Points.Pref + sKernelAliasRest + "s_point where bd_kernel='" + finder.bank + "' ),0) , "+finder.nzp_user+",current_date )";
                    ret = ExecSQL(sql, true);

                    decimal pnzp_fd = getSequence_(finder, Points.Pref + sDataAliasRest + "fn_dogovor_nzp_fd_seq", out ret); 

                    sql =
                            " INSERT INTO " + Points.Pref + sDataAliasRest + "fn_dogovor" +
                            " (nzp_fd,nzp_supp, nzp_payer, num_dog, dat_dog, dat_s, dat_po, dat_when, nzp_area, nzp_payer_ar, nzp_fb, naznplat, nzp_scope,nzp_payer_agent, nzp_payer_princip) " +
                            " SELECT "+pnzp_fd.ToString("")+"," + nzp_supp.ToString("") + ", id_urlic_p_nzp, dog_num, dog_date," +
                            " '" + dat_s + "', '" + dat_po + "'," + sCurDate + "," +
                            DbDisUtils.GetNoAreaKod(ServerConnection, finder) + ", id_urlic_p_nzp, " + rs + "," +
                            " 'Из файла загрузки наследуемой информации'," +pnzp_scope.ToString("")+",id_agent_nzp, id_urlic_p_nzp"+
                            " FROM " + table +
                            " WHERE dog_id = " + r["dog_id"];

                    ret = ExecSQL(sql, true);

                    // nftul_data.fn_dogovor_bank_lnk l

                    decimal pnzp_lnk = getSequence_(finder, Points.Pref + sDataAliasRest + "fn_dogovor_bank_lnk_id_seq", out ret);
                    sql = "insert into " + Points.Pref + sDataAliasRest +
                          "fn_dogovor_bank_lnk(id,nzp_fd, nzp_fb, changed_by, changed_on) " +
                          "values(" + pnzp_lnk.ToString("") + "," + pnzp_fd.ToString("") + "," + rs + "," +
                          finder.nzp_user + ",current_date)";
                    ret = ExecSQL(sql, true);


                    sql =
                        " INSERT INTO " + Points.Pref + DBManager.sKernelAliasRest + "supplier " +
                        " (nzp_supp, nzp_payer_agent, nzp_payer_princip, nzp_payer_supp, name_supp , nzp_scope, fn_dogovor_bank_lnk_id) " +
                        " SELECT " + nzp_supp + ", id_agent_nzp, id_urlic_p_nzp, id_supp_nzp, dog_name " +","+pnzp_scope.ToString("") +","+pnzp_lnk.ToString("")+
                        " FROM " + table +
                        " WHERE dog_id = " + r["dog_id"] + 
                        " AND NOT EXISTS " +
                        " (SELECT 1 FROM " + Points.Pref + DBManager.sKernelAliasRest + "supplier s WHERE " +
                        " s.nzp_payer_agent = id_agent_nzp AND s.nzp_payer_princip = id_urlic_p_nzp AND s.nzp_payer_supp = id_supp_nzp)";
                    ret = ExecSQL(sql, true);

                    sql =
                        " INSERT INTO " + finder.bank + DBManager.sKernelAliasRest + "supplier " +
                        " (nzp_supp, nzp_payer_agent, nzp_payer_princip, nzp_payer_supp, name_supp, nzp_scope, fn_dogovor_bank_lnk_id ) " +
                        " SELECT " + nzp_supp + ", id_agent_nzp, id_urlic_p_nzp, id_supp_nzp, dog_name " +","+pnzp_scope.ToString("") +","+pnzp_lnk.ToString("")+
                        " FROM " + table +
                        " WHERE dog_id = " + r["dog_id"] +
                        " AND NOT EXISTS " +
                        " (SELECT 1 FROM " + finder.bank + DBManager.sKernelAliasRest + "supplier s WHERE " +
                        " s.nzp_payer_agent = id_agent_nzp AND s.nzp_payer_princip = id_urlic_p_nzp AND s.nzp_payer_supp = id_supp_nzp)";
                    ret = ExecSQL(sql, true);


                    sql =
                        " INSERT INTO " + Points.Pref + DBManager.sKernelAliasRest + "supplier_point " +
                        " (nzp_wp, nzp_supp)" +
                        " VALUES" +
                        " ((SELECT nzp_wp FROM " + Points.Pref + DBManager.sKernelAliasRest + "s_point" +
                        " WHERE trim(bd_kernel) = '" + finder.bank + "'), '" + nzp_supp + "') ";
                    ret = ExecSQL(sql, true);
                    
                    sql =
                        " UPDATE " + table + " SET nzp_supp = " + nzp_supp +
                        " WHERE dog_id = " + r["dog_id"];
                    ret = ExecSQL(sql, true);
                }

                MonitorLog.WriteLog("Завершено добавление договоров", MonitorLog.typelog.Info, true);

                sql =
                    " UPDATE " + Points.Pref + DBManager.sUploadAliasRest + "file_dog SET nzp_supp = " +
                    "   (SELECT t.nzp_supp " +
                    "   FROM " + table + " t" +
                    "   WHERE t.dog_id = " + Points.Pref + DBManager.sUploadAliasRest + "file_dog.dog_id)" +
                    " WHERE  nzp_file = " + finder.nzp_file;
                ExecSQL(sql, true);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка разбора договоров " + ex.Message, MonitorLog.typelog.Error, true);
                return new Returns(false, "Ошибка разбора договоров", -1);
            }

            return ret;
        }

        private void SetNzpSuppFromSupplier(string table)
        {
            string sql;
            sql =
                " UPDATE " + table +
                " SET nzp_supp = " +
                " ((SELECT min(s.nzp_supp) " +
                "   FROM " + Points.Pref + sKernelAliasRest + "supplier s" +
                "   WHERE s.nzp_payer_agent = id_agent_nzp " +
                "   AND s.nzp_payer_princip = id_urlic_p_nzp" +
                "   AND s.nzp_payer_supp = id_supp_nzp  and upper(s.name_supp)=upper(dog_name))) where nzp_supp is null ";
            ExecSQL(sql, true);

            sql =
                " UPDATE " + table +
                " SET nzp_supp = " +
                " ((SELECT min(s.nzp_supp) " +
                "   FROM " + Points.Pref + sKernelAliasRest + "supplier s" +
                "   WHERE s.nzp_payer_agent = id_agent_nzp " +
                "   AND s.nzp_payer_princip = id_urlic_p_nzp" +
                "   AND s.nzp_payer_supp = id_supp_nzp  )) where nzp_supp is null";
            ExecSQL(sql, true);
        }

        private void InsertDataIntoTemp(FilesDisassemble finder, string table)
        {
            string sql;
            try
            {
                sql = "DROP TABLE " + table;
                ExecSQL(sql, false);
            }
            catch
            {
            }

            sql =
                " CREATE TEMP TABLE " + table + "(" +
                " nzp_file INTEGER," +
                " id_agent_nzp INTEGER, " +
                " id_urlic_p_nzp INTEGER," +
                " id_supp_nzp INTEGER," +
                " dog_id INTEGER," +
                " dog_name CHAR(60)," +
                " dog_num CHAR(20)," +
                " dog_date DATE," +
                " nzp_supp INTEGER," +
                " rs CHAR(20))";
            ExecSQL(sql, true);

            sql =
                " INSERT INTO " + table +
                "(nzp_file, id_agent_nzp, id_urlic_p_nzp, id_supp_nzp, dog_id, dog_name, dog_num, dog_date, rs)" +
                " SELECT distinct fd.nzp_file, fu1.nzp_payer, fu2.nzp_payer, fu3.nzp_payer," +
                " fd.dog_id, fd.dog_name, fd.dog_num, fd.dog_date, fd.rs " +
                " FROM " + Points.Pref + sUploadAliasRest + "file_dog fd," +
                Points.Pref + sUploadAliasRest + "file_urlic fu1, " +
                Points.Pref + sUploadAliasRest + "file_urlic fu2, " +
                Points.Pref + sUploadAliasRest + "file_urlic fu3" +
                " WHERE fd.id_agent = fu1.urlic_id AND fd.nzp_file = fu1.nzp_file " +
                " AND fd.id_urlic_p = fu2.urlic_id AND fd.nzp_file = fu2.nzp_file " +
                " AND fd.id_supp = fu3.urlic_id AND fd.nzp_file = fu3.nzp_file " +
                " AND fu1.is_bank = 0 " +
                " AND fu2.is_bank = 0 " +
                " AND fu3.is_bank = 0" +
                " AND fd.nzp_file = " + finder.nzp_file;
            ExecSQL(sql, true);
        }
    }
}
