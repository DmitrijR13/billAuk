using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Text;
using Bars.KP50.DataImport.SOURCE.UTILS;
using Bars.KP50.Utils;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.DataImport.SOURCE.DISASSEMBLE
{
    public class DbInsertPayer : DataBaseHeadServer
    {
        public Returns InsertPayer(FilesDisassemble finder)
        {
            Returns ret = STCLINE.KP50.Global.Utils.InitReturns();
            MonitorLog.WriteLog("Старт добавления типов юр. лиц", MonitorLog.typelog.Info, true);
            try
            {
                AddPayerRoles(finder);
                AddPayerParams(finder);
                AddPayerInArea(finder);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка добавления типов юр. лиц" + ex.Message, MonitorLog.typelog.Error, true);
                return new Returns(false, "Ошибка добавления типов юр. лиц", -1);
            }
            return ret;
        }

        private Returns AddPayerRoles(FilesDisassemble finder)
        {
            string sql;
            Returns ret = new Returns();
            MonitorLog.WriteLog("Старт добавления ролей ЮЛ", MonitorLog.typelog.Info, true);

            try
            {
                try
                {
                    sql = "DROP TABLE t_insert_payer_types";
                    ExecSQL(sql, false);
                }
                catch{}

                sql =
                    " CREATE TEMP TABLE t_insert_payer_types(" +
                    " nzp_payer INTEGER," +
                    " role INTEGER," +
                    " nzp_file INTEGER)";
                ret = ExecSQL(sql);

                AddRoleIntoTemp("is_area", 3, finder.nzp_file);
                AddRoleIntoTemp("is_supp", 2, finder.nzp_file);
                AddRoleIntoTemp("is_arendator", 7, finder.nzp_file);
                AddRoleIntoTemp("is_rc", 5, finder.nzp_file);
                AddRoleIntoTemp("is_rso", 6, finder.nzp_file);
                AddRoleIntoTemp("is_agent", 4, finder.nzp_file);
                AddRoleIntoTemp("is_subabonent", 9, finder.nzp_file);
                AddRoleIntoTemp("is_bank", 8, finder.nzp_file);

                sql =
                    " INSERT INTO " + Points.Pref + sKernelAliasRest + "payer_types" +
                    " (nzp_payer, nzp_payer_type, changed_by, changed_on)" +
                    " SELECT DISTINCT nzp_payer, role, " + finder.nzp_user + ", " + sCurDate +
                    " FROM t_insert_payer_types " +
                    " WHERE 0 =" +
                    " (SELECT COUNT(*) " +
                    " FROM " + Points.Pref + sKernelAliasRest + "payer_types t " +
                    " WHERE t.nzp_payer = nzp_payer " +
                    " AND role = t.nzp_payer_type)";
                ret = ExecSQL(sql);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка добавления ролей ЮЛ: " + ex.Message, MonitorLog.typelog.Error, true);
                return new Returns(false);
            }
            return ret;
        }

        private void AddRoleIntoTemp(string field_from_file_urlic, int nzp_role, int nzp_file)
        {
            string sql =
                     " INSERT INTO t_insert_payer_types (nzp_payer, role, nzp_file) " +
                     " SELECT nzp_payer, " + nzp_role + ", nzp_file " +
                     " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_urlic " +
                     " WHERE " + field_from_file_urlic + " = 1" +
                     " AND nzp_file = " + nzp_file;
            ExecSQL(sql);
        }

        private Returns AddPayerParamsOld(FilesDisassemble finder)
        {
            Returns ret = new Returns();
            MonitorLog.WriteLog("Старт добавления параметров ЮЛ", MonitorLog.typelog.Info, true);
            string sql;
            try
            {
                try
                {
                    sql = "DROP TABLE t_for_payer_prm";
                    ExecSQL(sql, false);
                }
                catch { }
                sql = " CREATE TEMP TABLE  t_for_payer_prm (" +
                             " nzp INTEGER," +
                             " nzp_prm INTEGER," +
                             " dat_po DATE," +
                             " dat_s DATE," +
                             " val_prm CHAR(200)," +
                             " is_actual INTEGER, " +
                             " user_del INTEGER," +
                             " cur_unl INTEGER, " +
                             " nzp_user INTEGER)";
                ret = ExecSQL(sql);

                //Фактический адрес
                InsertPrmToTempTable(finder, 1269, "fact_address");

                //Расчетный счет
                InsertPrmToTempTable(finder, 1305, 
                    "(SELECT min(r.rs) FROM " + Points.Pref + sUploadAliasRest + "file_rs r" +
                    " WHERE r.id_urlic = urlic_id and nzp_file = " + finder.nzp_file +
                    " and r.rs= rs and not exists (select 1 from " + finder.bank + sDataAliasRest + ".prm_9 pp where pp.val_prm=r.rs and pp.nzp=r.id_urlic and nzp_prm =1305 ) )");

                //телефон руководителя
                InsertPrmToTempTable(finder, 1306, "tel_chief");

                //телефон бухгалтерии
                InsertPrmToTempTable(finder, 1307, "tel_b");

                //фио руководителя
                InsertPrmToTempTable(finder, 1308, "chief_name");

                //должность руководителя
                InsertPrmToTempTable(finder, 1309, "chief_post");

                //ФИО бухгалтера
                InsertPrmToTempTable(finder, 1310, "b_name");

                //ОКОНХ1
                InsertPrmToTempTable(finder, 506, "okonh1");

                //ОКОНХ2
                InsertPrmToTempTable(finder, 1311, "okonh2");

                //ОКПО
                InsertPrmToTempTable(finder, 507, "okpo");

                //Должность + ФИО руководителя в родит падеже 
                InsertPrmToTempTable(finder, 1312, "post_and_name");

                //ИНН
                InsertPrmToTempTable(finder, 502, "inn");

                //КПП
                InsertPrmToTempTable(finder, 503, "kpp");

                //название
                InsertPrmToTempTable(finder, 504, "urlic_name");

                //Юридический адрес
                InsertPrmToTempTable(finder, 505, "jur_address");

                var lp = new LoadPrm(ServerConnection);
                ret = lp.SetPrm(9, "t_for_payer_prm", finder.bank);

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка добавления типов юр. лиц" + ex.Message, MonitorLog.typelog.Error, true);
                return new Returns(false, "Ошибка добавления типов юр. лиц", -1);
            }
            return  ret;
        }

        private Returns AddPayerParams(FilesDisassemble finder)
        {
            Returns ret = new Returns();
            MonitorLog.WriteLog("Старт добавления параметров ЮЛ", MonitorLog.typelog.Info, true);
            string sql;
            try
            {
                try
                {
                    sql = "DROP TABLE t_for_payer_prm";
                    ExecSQL(sql, false);
                }
                catch { }
                sql = " CREATE TEMP TABLE  t_for_payer_prm (" +
                             " nzp_key serial not null, " +
                             " nzp INTEGER," +
                             " nzp_prm INTEGER," +
                             " dat_po DATE," +
                             " dat_s DATE," +
                             " val_prm CHAR(200)," +
                             " is_actual INTEGER, " +
                             " user_del INTEGER," +
                             " cur_unl INTEGER, " +
                             " nzp_user INTEGER)";
                ret = ExecSQL(sql);

                //Фактический адрес
                InsertPrmToTempTable(finder, 1269, "fact_address");

                //Расчетный счет
                InsertPrmToTempTable(finder, 1305,
                    "(SELECT min(r.rs) FROM " + Points.Pref + sUploadAliasRest + "file_rs r" +
                    " WHERE r.id_urlic = urlic_id and nzp_file = " + finder.nzp_file +
                    " and r.rs= rs and not exists (select 1 from " + finder.bank + sDataAliasRest +
                    ".prm_9 pp where pp.val_prm=r.rs and pp.nzp=r.id_urlic and nzp_prm =1305 ) )");

                //телефон руководителя
                InsertPrmToTempTable(finder, 1306, "tel_chief");

                //телефон бухгалтерии
                InsertPrmToTempTable(finder, 1307, "tel_b");

                //фио руководителя
                InsertPrmToTempTable(finder, 1308, "chief_name");

                //должность руководителя
                InsertPrmToTempTable(finder, 1309, "chief_post");

                //ФИО бухгалтера
                InsertPrmToTempTable(finder, 1310, "b_name");

                //ОКОНХ1
                InsertPrmToTempTable(finder, 506, "okonh1");

                //ОКОНХ2
                InsertPrmToTempTable(finder, 1311, "okonh2");

                //ОКПО
                InsertPrmToTempTable(finder, 507, "okpo");

                //Должность + ФИО руководителя в родит падеже 
                InsertPrmToTempTable(finder, 1312, "post_and_name");

                //ИНН
                InsertPrmToTempTable(finder, 502, "inn");

                //КПП
                InsertPrmToTempTable(finder, 503, "kpp");

                //название
                InsertPrmToTempTable(finder, 504, "urlic_name");

                //Юридический адрес
                InsertPrmToTempTable(finder, 505, "jur_address");

                //почистить пересеуающиеся значения по дат с
                sql = "update " + finder.bank + sDataAliasRest + tableDelimiter + "prm_9 set is_actual=100 " +
                    " where exists (select 1 from t_for_payer_prm a where" +
                    " a.nzp=" + finder.bank + sDataAliasRest + tableDelimiter + "prm_9.nzp and" +
                " a.nzp_prm=" + finder.bank + sDataAliasRest + tableDelimiter + "prm_9.nzp_prm and" +
                " a.dat_s=" + finder.bank + sDataAliasRest + tableDelimiter + "prm_9.dat_s and a.is_actual =1) " +
                " and " + finder.bank + sDataAliasRest + tableDelimiter + "prm_9.is_actual=1 ";
                ret = ExecSQL(sql);
                try
                {
                    sql = "DROP TABLE t_prm_9";
                    ExecSQL(sql, false);
                }
                catch { }

                sql = " select min(nzp_key ) nzp_key ,nzp,nzp_prm,dat_s into temp t_prm_9 from t_for_payer_prm  where is_actual=1 group by 2,3,4 ";
                ret = ExecSQL(sql);

                var lp = new LoadPrm(ServerConnection);
                ret = lp.SetPrm(9, "t_for_payer_prm b where b.nzp_key in  (select nzp_key from t_prm_9) and  not exists (select 1 from " + finder.bank + sDataAliasRest + tableDelimiter + "prm_9 a where a.nzp=b.nzp and a.nzp_prm =b.nzp_prm and a.dat_s=b.dat_s and a.is_actual=1 )", finder.bank);

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка добавления типов юр. лиц" + ex.Message, MonitorLog.typelog.Error, true);
                return new Returns(false, "Ошибка добавления типов юр. лиц", -1);
            }
            return ret;
        }

        private Returns InsertPrmToTempTable(FilesDisassemble finder, int nzp_prm, string field_from_file_urlic)
        {
            Returns ret = new Returns();
            string dat_s = "01." + finder.month.ToString("00") + "." +
                           finder.year.ToString("0000");
            string sql =
                " INSERT INTO t_for_payer_prm" +
                    " ( nzp, nzp_prm, dat_s, dat_po, val_prm, is_actual, cur_unl, user_del, nzp_user) " +
                    " SELECT distinct nzp_payer, " + nzp_prm + ", '" + dat_s + "'::date,'" + finder.dat_po + "'::date," +
                    " trim(" + field_from_file_urlic + "), 1, 1, " + finder.nzp_file + "," + finder.nzp_user +
                    " FROM " + Points.Pref + sUploadAliasRest + "file_urlic " +
                    " WHERE " + sNvlWord + "(" + field_from_file_urlic + ",'') <> '' AND" +
                    " nzp_file = " + finder.nzp_file;
            ret = ExecSQL(sql);

            return ret;
        }

        private Returns AddPayerInArea(FilesDisassemble finder)
        {
            MonitorLog.WriteLog("Старт добавления УК", MonitorLog.typelog.Info, true);

            Returns ret = new Returns(true);
            string seq = Points.Pref + "_data" + tableDelimiter + "s_area_nzp_area_seq";
            decimal strNzp_area = 0;
            
            //добавляем в верхний банк те УК, которых там нет
            string sql;
            sql = 
                " SELECT urlic_name as payer, nzp_payer " +
                " FROM " + Points.Pref + sUploadAliasRest + "file_urlic " +
                " WHERE nzp_file = " + finder.nzp_file +
                " AND is_area = 1 AND nzp_payer NOT IN " +
                " (SELECT a.nzp_payer" +
                " FROM " + Points.Pref + sDataAliasRest + "s_area a  " +
                " WHERE a.nzp_payer IS NOT NULL) ";

            DataTable dt = ClassDBUtils.OpenSQL(sql, ServerConnection).resultData;
            foreach (DataRow r in dt.Rows)
            {
#if PG
                sql = " SELECT nextval('" + seq + "') ";
#else
                    sql = " SELECT " + seq + ".nextval FROM  " + Points.Pref + "_data" + tableDelimiter + "dual";
#endif


                strNzp_area = Convert.ToDecimal(ExecScalar(ServerConnection, sql, out ret, true));

                if (strNzp_area <= 0 || !ret.result)
                {
                    ret.text = "Ошибка при генерации кода поставщика! Код = " + strNzp_area;
                    ret.result = false;
                    return ret;
                }

                var payer = r["payer"] == null ? string.Empty : r["payer"].ToString().Trim();
                if (payer.Length > 40)
                    payer = payer.Substring(0, 40).Trim();

                sql =
                    " insert into " + Points.Pref + sDataAliasRest + "s_area" +
                    " (nzp_area, area, nzp_payer) " +
                    " VALUES" +
                    " (" + strNzp_area + ",'" + payer + "', " + r["nzp_payer"] + ") ";
                ret = ExecSQL(sql);
                sql =
                    " insert into " + finder.bank + sDataAliasRest + "s_area" +
                    " (nzp_area, area, nzp_payer) " +
                    " VALUES" +
                    " (" + strNzp_area + ",'" + payer + "', " + r["nzp_payer"] + ") ";
                ret = ExecSQL(sql);
            
            }
            
            //спускаем те УК, которые есть в этом файле и в верхнем банке, но нет в локальном
            sql =
                " SELECT urlic_name as payer, nzp_payer " +
                " FROM " + Points.Pref + sUploadAliasRest + "file_urlic " +
                " WHERE nzp_file = " + finder.nzp_file +
                " AND is_area = 1 AND nzp_payer NOT IN " +
                " (SELECT a.nzp_payer" +
                " FROM " + finder.bank + sDataAliasRest + "s_area a " +
                " WHERE a.nzp_payer IS NOT NULL) ";

            dt = ClassDBUtils.OpenSQL(sql, ServerConnection).resultData;
            foreach (DataRow r in dt.Rows)
            {
                sql =
                    " INSERT INTO " + finder.bank + sDataAliasRest + "s_area" +
                    " (nzp_area, area, nzp_supp, nzp_payer) " +
                    " SELECT nzp_area, cast(area as CHAR(40)), nzp_supp, nzp_payer " +
                    " FROM " + Points.Pref + DBManager.sDataAliasRest + "s_area " +
                    " WHERE nzp_payer = " + r["nzp_payer"];
                    
                ExecSQL(sql);

            }

            return ret;
        }
    }

    public class DbInsertRS : DataBaseHeadServer
    {
        public Returns InsertRS(FilesDisassemble finder)
        {
            Returns ret = new Returns();
            MonitorLog.WriteLog("Старт добавления РС ЮЛ", MonitorLog.typelog.Info, true);

            string sql = 
                " INSERT INTO " + Points.Pref + sDataAliasRest + "fn_bank" +
                " (nzp_payer, bank_name, rcount, kcount, bik, nzp_user, dat_when, nzp_payer_bank, num_count)" +
                " SELECT ur.nzp_payer, bank.urlic_name, rs.rs, rs.ks, rs.bik, " + 
                finder.nzp_user + "," + sCurDate + ", bank.nzp_payer, 0" +
                " FROM " + Points.Pref + sUploadAliasRest + "file_rs rs," +
                Points.Pref + sUploadAliasRest + "file_urlic ur," +
                Points.Pref + sUploadAliasRest + "file_urlic bank" +
                " WHERE rs.id_bank = bank.urlic_id AND rs.nzp_file = bank.nzp_file AND" +
                " rs.id_urlic = ur.urlic_id  AND rs.nzp_file = ur.nzp_file " +
                " AND rs.nzp_file =" + finder.nzp_file + 
                " AND NOT EXISTS" +
                " (SELECT 1" +
                " FROM " + Points.Pref + sDataAliasRest + "fn_bank" +
                " WHERE nzp_payer = ur.nzp_payer AND rcount = rs.rs)";
            ret = ExecSQL(sql);

            return ret;
        }
    }
}
