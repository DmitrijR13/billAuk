using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase
{
    public class DbUsePreviousLinks : DbAdminClient
    {
        private int source_nzp_file = 0;
        private int receiver_nzp_file = 0;
        private IDbConnection _connDb = null;
        
        /// <summary>
        /// Функция использование предыдущих сопоставлений
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        public Returns UsePreviousLinks(FilesImported finder)
        {
            Returns ret = new Returns(true);

            try
            {
                using (IDbConnection conn_db = DBManager.GetConnection(Global.Constants.cons_Kernel))
                {
                    ret = DBManager.OpenDb(conn_db, true);
                    if (!ret.result) throw new Exception(ret.text);

                    _connDb = conn_db;

                    //if (finder.nzp_file_1 <= 0 && finder.nzp_file_2 <= 0)
                    //{
                    //    ret = GetSelectedFiles(finder.nzp_user, ref finder);
                    //}
                    //else
                    {
                        // проверка данных
                        ret = CheckData(finder);
                    }

                    if (ret.result)
                    {
                        // определить источник и приемник
                        source_nzp_file = Math.Min(finder.nzp_file_1, finder.nzp_file_2);
                        receiver_nzp_file = Math.Max(finder.nzp_file_1, finder.nzp_file_2);
                        
                        // копировать связки
                        CopyLinkArea();
                        CopyLinkSupp();
                        CopyLinkDogovor();
                        CopyLinkUrLic();
                        CopyLinkVill();
                        CopyLinkServ();
                        CopyLinkMeasure();
                        CopyLinkPar("file_typeparams");
                        CopyLinkPar("file_blag");
                        CopyLinkPar("file_gaz");
                        CopyLinkPar("file_voda");
                        CopyLinkDom();
                        CopyLinkKvar();
                    }
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры UsePreviousLinks : " + ex.Message, MonitorLog.typelog.Error, true);
                ret = new Returns(false, "Ошибка при сопоставлении!");
            }

            return ret;
        }

        /// <summary>
        /// Получить выбранные файлы
        /// </summary>
        /// <param name="nzp_user"></param>
        /// <param name="finder"></param>
        /// <returns></returns>
        //private Returns GetSelectedFiles(int nzp_user, ref FilesImported finder)
        //{
        //    Returns ret = new Returns(true);
            
        //    //выбираем все файлы с галочками
        //    string sql =
        //        " SELECT fs.nzp_file, fi.loaded_name||'('||p.point||')' as file_name " +
        //        " FROM " + Points.Pref + DBManager.sUploadAliasRest + "files_selected fs,"
        //        + Points.Pref + DBManager.sUploadAliasRest + "files_imported fi, "
        //        + Points.Pref + DBManager.sKernelAliasRest + "s_point p" +
        //        " WHERE fs.nzp_user = " + nzp_user +
        //        " AND fs.nzp_file = fi.nzp_file AND trim(p.bd_kernel) = trim(fi.pref) ";
            
        //    DataTable dt = ClassDBUtils.OpenSQL(sql, _connDb, ClassDBUtils.ExecMode.Exception).GetData();

        //    if (dt.Rows.Count != 2)
        //    {
        //        string selected_files_name = "";
        //        foreach (DataRow row in dt.Rows)
        //        {
        //            if (selected_files_name != "") selected_files_name += ", ";
        //            selected_files_name += row["file_name"].ToString();
        //        }

        //        ret = new Returns(false, "Необходимо выбрать ровно два файла! Выбрано: " + dt.Rows.Count + ", файлы:" + selected_files_name, -1);
        //    }
        //    else
        //    {
        //        finder.nzp_file_1 = (int)dt.Rows[0]["nzp_file"];
        //        finder.nzp_file_2 = (int)dt.Rows[1]["nzp_file"];
        //    }

        //    return ret;
        //}

        /// <summary>
        /// Проверка данных
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        private Returns CheckData(FilesImported finder)
        {
            if (finder.nzp_file_1 <= 0) return new Returns(false, "Не задан первый файл");
            if (finder.nzp_file_2 <= 0) return new Returns(false, "Не задан второй файл");

            return new Returns(true);
        }

        /// <summary>
        /// УК
        /// </summary>
        private void CopyLinkArea()
        {
            string tmp_table = "tmp_file_area";

            ExecSQL(_connDb, "Drop table " + tmp_table, false);

            string sql = "create temp table " + tmp_table + " (" +
                " nzp_area integer, " +
                " id       integer )";
            ExecSQLWE(_connDb, sql);

            sql = " insert into " + tmp_table + " (nzp_area, id) " +
                "select id, nzp_area " +
                " from " + Points.Pref + DBManager.sUploadAliasRest + "file_area where nzp_file = " + source_nzp_file;
            ExecSQLWE(_connDb, sql);

            ExecSQLWE(_connDb, "create index ix_" + tmp_table + "_id       on " + tmp_table + " (id)");
            ExecSQLWE(_connDb, "create index ix_" + tmp_table + "_nzp_area on " + tmp_table + " (nzp_area)");
            ExecSQLWE(_connDb, DBManager.sUpdStat + " " + tmp_table);

            sql = "update " + Points.Pref + DBManager.sUploadAliasRest + "file_area fa set " +
                " nzp_area = (select max(t.nzp_area) from  " + tmp_table + " t where t.id = fa.id) " +
                " where fa.nzp_file = " + receiver_nzp_file +
                "   and fa.nzp_area is null";
            ExecSQLWE(_connDb, sql);

            ExecSQL(_connDb, "Drop table " + tmp_table, false);
        }

        /// <summary>
        /// Поставщики
        /// </summary>
        private void CopyLinkSupp()
        {
            string tmp_table = "tmp_file_supp";

            ExecSQL(_connDb, "Drop table " + tmp_table, false);

            string sql = "create temp table " + tmp_table + " (" +
                " nzp_supp integer, " +
                " supp_id  " + DBManager.sDecimalType + "(18,0) )";
            ExecSQLWE(_connDb, sql);

            sql = " insert into " + tmp_table + " (nzp_supp, supp_id) " +
                " select nzp_supp, supp_id " +
                " from " + Points.Pref + DBManager.sUploadAliasRest + "file_supp where nzp_file = " + source_nzp_file;
            ExecSQLWE(_connDb, sql);

            ExecSQLWE(_connDb, "create index ix_" + tmp_table + "_supp_id  on " + tmp_table + " (supp_id)");
            ExecSQLWE(_connDb, "create index ix_" + tmp_table + "_nzp_supp on " + tmp_table + " (nzp_supp)");
            ExecSQLWE(_connDb, DBManager.sUpdStat + " " + tmp_table);

            sql = "update " + Points.Pref + DBManager.sUploadAliasRest + "file_supp s set " +
                " nzp_supp = (select max(t.nzp_supp) from " + tmp_table + " t where t.supp_id = s.supp_id) " +
                " where s.nzp_file = " + receiver_nzp_file + 
                "   and s.nzp_supp is null";
            ExecSQLWE(_connDb, sql);

            ExecSQL(_connDb, "Drop table " + tmp_table, false);
        }

        /// <summary>
        /// Договора
        /// </summary>
        private void CopyLinkDogovor()
        {
            string tmp_table = "tmp_file_dog";

            ExecSQL(_connDb, "Drop table " + tmp_table, false);

            string sql = "create temp table " + tmp_table + " (" +
                " nzp_supp   integer, " +
                " id_agent   integer, " +
                " id_urlic_p integer, " +
                " id_supp    integer, " +
                " dog_id     integer " + " )";
            ExecSQLWE(_connDb, sql);

            sql = " insert into " + tmp_table + " (nzp_supp, id_agent, id_urlic_p, id_supp, dog_id) " +
                " select nzp_supp, id_agent, id_urlic_p, id_supp, dog_id " +
                " from " + Points.Pref + DBManager.sUploadAliasRest + "file_dog where nzp_file = " + source_nzp_file;
            ExecSQLWE(_connDb, sql);

            ExecSQLWE(_connDb, "create index ix_" + tmp_table + "_nzp_supp on " + tmp_table + " (nzp_supp)");
            ExecSQLWE(_connDb, "create index ix_" + tmp_table + "_1        on " + tmp_table + " (id_agent, id_urlic_p, id_supp, dog_id)");
            ExecSQLWE(_connDb, DBManager.sUpdStat + " " + tmp_table);

            sql = "update " + Points.Pref + DBManager.sUploadAliasRest + "file_dog d set " +
                " nzp_supp = (select max(t.nzp_supp) from " + tmp_table + " t " + 
                " where t.id_supp    = d.id_supp " +
                "   and t.id_agent   = d.id_agent " +
                "   and t.id_urlic_p = d.id_urlic_p " +
                "   and t.dog_id     = d.dog_id) " +
                " where d.nzp_file = " + receiver_nzp_file + 
                "   and d.nzp_supp is null";
            ExecSQLWE(_connDb, sql);

            ExecSQL(_connDb, "Drop table " + tmp_table, false);
        }

        /// <summary>
        /// Юр. лица
        /// </summary>
        private void CopyLinkUrLic()
        {
            string temp_table = "tmp_file_urlic";

            ExecSQL(_connDb, "Drop table " + temp_table, false);

            string sql = "create temp table " + temp_table + " (" +
                " nzp_payer    integer, " +
                " urlic_id     " + DBManager.sDecimalType + "(18,0), " +
                " urlic_name   varchar(200), " + 
                " inn          varchar(40), " + 
                " kpp          varchar(40) " + " )";
            ExecSQLWE(_connDb, sql);

            sql = " insert into " + temp_table + " (nzp_payer, urlic_id, urlic_name, inn, kpp) " +
                " select nzp_payer, urlic_id, urlic_name, inn, kpp " +
                " from " + Points.Pref + DBManager.sUploadAliasRest + "file_urlic " + 
                " where nzp_payer > 0 and nzp_file = " + source_nzp_file;
            ExecSQLWE(_connDb, sql);

            ExecSQLWE(_connDb, "create index ix_" + temp_table + "_nzp_payer on " + temp_table + " (nzp_payer)");
            ExecSQLWE(_connDb, "create index ix_" + temp_table + "_1         on " + temp_table + " (urlic_id, urlic_name, inn, kpp)");
            ExecSQLWE(_connDb, DBManager.sUpdStat + " " + temp_table);

            sql = "update " + Points.Pref + DBManager.sUploadAliasRest + "file_urlic u set  " + 
                " nzp_payer = (select max(t.nzp_payer) from " + temp_table + " t " + 
                " where t.urlic_id = u.urlic_id " +
                "   and upper(t.urlic_name) = upper(u.urlic_name) " +
                "   and t.inn = u.inn " +
                "   and t.kpp = u.kpp) " +
                " where u.nzp_file = " + receiver_nzp_file + 
                "   and u.nzp_payer is null ";
            ExecSQLWE(_connDb, sql);

            ExecSQL(_connDb, "Drop table " + temp_table, false);
        }

        /// <summary>
        /// Муниципальные образования
        /// </summary>
        private void CopyLinkVill()
        {
            string temp_table = "tmp_file_mo";

            ExecSQL(_connDb, "Drop table " + temp_table, false);

            string sql = "create temp table " + temp_table + " (" +
                " nzp_vill " + DBManager.sDecimalType + "(18,0), " +
                " id_mo    integer)";
            ExecSQLWE(_connDb, sql);

            sql = " insert into " + temp_table + " (nzp_vill, id_mo) " +
                " select nzp_vill, id_mo " +
                " from " + Points.Pref + DBManager.sUploadAliasRest + "file_mo " +
                " where nzp_file = " + source_nzp_file;
            ExecSQLWE(_connDb, sql);

            ExecSQLWE(_connDb, "create index ix_" + temp_table + "_nzp_vill on " + temp_table + " (nzp_vill)");
            ExecSQLWE(_connDb, "create index ix_" + temp_table + "_id_mo    on " + temp_table + " (id_mo)");
            ExecSQLWE(_connDb, DBManager.sUpdStat + " " + temp_table);

            sql = "update " + Points.Pref + DBManager.sUploadAliasRest + "file_mo m set " +
                " nzp_vill = (select max(t.nzp_vill) from " + temp_table + " t where t.id_mo = m.id_mo) " +
                " where nzp_file =" + receiver_nzp_file + 
                "   and m.nzp_vill is null";
            ExecSQLWE(_connDb, sql);

            ExecSQL(_connDb, "Drop table " + temp_table, false);
        }

        /// <summary>
        /// Услуги
        /// </summary>
        private void CopyLinkServ()
        {
            string temp_table = "tmp_file_services";

            ExecSQL(_connDb, "Drop table " + temp_table, false);

            string sql = "create temp table " + temp_table + " (" +
                " nzp_measure integer, " + 
                " nzp_serv    integer, " +
                " id_serv     integer, " +
                " service     varchar(200) )";
            ExecSQLWE(_connDb, sql);

            sql = " insert into " + temp_table + " (nzp_measure, nzp_serv, id_serv, service) " +
                " select nzp_measure, nzp_serv, id_serv, service " +
                " from " + Points.Pref + DBManager.sUploadAliasRest + "file_services " +
                " where nzp_file = " + source_nzp_file;
            ExecSQLWE(_connDb, sql);

            ExecSQLWE(_connDb, "create index ix_" + temp_table + "_nzp_measure on " + temp_table + " (nzp_measure)");
            ExecSQLWE(_connDb, "create index ix_" + temp_table + "_nzp_serv    on " + temp_table + " (nzp_serv)");
            ExecSQLWE(_connDb, "create index ix_" + temp_table + "_1           on " + temp_table + " (id_serv, service)");
            ExecSQLWE(_connDb, DBManager.sUpdStat + " " + temp_table);

            string where = " where t.id_serv = s.id_serv and t.service = s.service";

            sql = "update " + Points.Pref + DBManager.sUploadAliasRest + "file_services s set " +
                " nzp_measure = (select max(t.nzp_measure) from " + temp_table + " t " + where + ") " +
                " where s.nzp_file = " + receiver_nzp_file;
            ExecSQLWE(_connDb, sql);

            sql = "update " + Points.Pref + DBManager.sUploadAliasRest + "file_services s set " +
                " nzp_serv = (select max(t.nzp_serv) from " + temp_table + " t " + where + ") " +
                " where s.nzp_file = " + receiver_nzp_file;
            ExecSQLWE(_connDb, sql);

            ExecSQL(_connDb, "Drop table " + temp_table, false);
        }

        /// <summary>
        /// Единицы измерения
        /// </summary>
        private void CopyLinkMeasure()
        {
            string temp_table = "tmp_file_measures";

            ExecSQL(_connDb, "Drop table " + temp_table, false);

            string sql = "create temp table " + temp_table + " (" +
                " nzp_measure integer, " +
                " id_measure    integer )";
            ExecSQLWE(_connDb, sql);

            sql = " insert into " + temp_table + " (nzp_measure, id_measure) " +
                " select nzp_measure, id_measure " +
                " from " + Points.Pref + DBManager.sUploadAliasRest + "file_measures " +
                " where nzp_file = " + source_nzp_file;
            ExecSQLWE(_connDb, sql);

            ExecSQLWE(_connDb, "create index ix_" + temp_table + "_nzp_measure on " + temp_table + " (nzp_measure)");
            ExecSQLWE(_connDb, "create index ix_" + temp_table + "_id_measure  on " + temp_table + " (id_measure)");
            ExecSQLWE(_connDb, DBManager.sUpdStat + " " + temp_table);

            sql = "update " + Points.Pref + DBManager.sUploadAliasRest + "file_measures m set " +
                " nzp_measure = (select max(t.nzp_measure) from " + temp_table + " t where t.id_measure = m.id_measure) " +
                " where m.nzp_file =" + receiver_nzp_file + 
                "   and m.nzp_measure is null";
            ExecSQLWE(_connDb, sql);

            ExecSQL(_connDb, "Drop table " + temp_table, false);
        }

        /// <summary>
        /// Параметры
        /// </summary>
        /// <param name="parTableName">таблица параметров</param>
        private void CopyLinkPar(string parTableName)
        {
            parTableName = parTableName.Trim();
            string temp_table = "tmp_" + parTableName;

            ExecSQL(_connDb, "Drop table " + temp_table, false);

            string sql = "create temp table " + temp_table + " (" +
                " nzp_prm integer, " +
                " id_prm  integer )";
            ExecSQLWE(_connDb, sql);

            sql = " insert into " + temp_table + " (nzp_prm, id_prm) " +
                " select nzp_prm, id_prm " +
                " from " + Points.Pref + DBManager.sUploadAliasRest + parTableName +
                " where nzp_file = " + source_nzp_file;
            ExecSQLWE(_connDb, sql);

            ExecSQLWE(_connDb, "create index ix_" + temp_table + "_nzp_prm on " + temp_table + " (nzp_prm)");
            ExecSQLWE(_connDb, "create index ix_" + temp_table + "_id_prm  on " + temp_table + " (id_prm)");
            ExecSQLWE(_connDb, DBManager.sUpdStat + " " + temp_table);

            sql = "update " + Points.Pref + DBManager.sUploadAliasRest + parTableName + " p set " +
                " nzp_prm = (select max(t.nzp_prm) from " + temp_table + " t where t.id_prm = p.id_prm) " +
                " where p.nzp_file = " + receiver_nzp_file;
            ExecSQLWE(_connDb, sql);

            ExecSQL(_connDb, "Drop table " + temp_table, false);
        }

        /// <summary>
        /// Дома
        /// </summary>
        private void CopyLinkDom()
        {
            string temp_table = "tmp_file_dom";

            ExecSQL(_connDb, "Drop table " + temp_table, false);

            string sql = "create temp table " + temp_table + " (" +
                " id  " + DBManager.sDecimalType + "(18,0), " +
                " local_id varchar(20), " +
                " nzp_dom  integer, " +
                " nzp_ul   integer, " +
                " nzp_raj  integer, " +
                " nzp_town integer, " +
                " ulica    varchar(200), " +
                " rajon    varchar(200), " +
                " town     varchar(200) )";
            ExecSQLWE(_connDb, sql);

            sql = " insert into " + temp_table + " (local_id, nzp_dom, nzp_ul, nzp_raj, nzp_town, ulica, rajon, town) " +
                " select local_id, nzp_dom, nzp_ul, nzp_raj, nzp_town, ulica, rajon, town " +
                " from " + Points.Pref + DBManager.sUploadAliasRest + "file_dom " +
                " where nzp_file = " + source_nzp_file;
            ExecSQLWE(_connDb, sql);

            ExecSQLWE(_connDb, "create index ix_" + temp_table + "_nzp_town on " + temp_table + " (nzp_town)");
            ExecSQLWE(_connDb, "create index ix_" + temp_table + "_nzp_raj  on " + temp_table + " (nzp_raj)");
            ExecSQLWE(_connDb, "create index ix_" + temp_table + "_1        on " + temp_table + " (local_id, ulica, rajon, town)");
            ExecSQLWE(_connDb, DBManager.sUpdStat + " " + temp_table);

            string where =
                " where t.local_id = d.local_id " +
                "   and t.ulica    = d.ulica " + 
                "   and t.rajon    = d.rajon " + 
                "   and t.town     = d.town";

            // ... nzp_town
            sql = "update " + Points.Pref + DBManager.sUploadAliasRest + "file_dom d set " +
                " nzp_raj = (select max(t.nzp_raj) from " + temp_table + " t " + where + ")" +
                " where d.nzp_file =" + receiver_nzp_file;
            ExecSQLWE(_connDb, sql);

            // ... nzp_raj
            sql = "update " + Points.Pref + DBManager.sUploadAliasRest + "file_dom d set " +
                " nzp_town = (select max(t.nzp_town) from " + temp_table + " t " + where + ")" + 
                " where d.nzp_file =" + receiver_nzp_file;
            ExecSQLWE(_connDb, sql);

            where += " and t.nzp_ul is not null ";
            // ... nzp_dom
            sql = "update " + Points.Pref + DBManager.sUploadAliasRest + "file_dom d set " + 
                " nzp_dom = (select max(t.nzp_dom) from " + temp_table + " t " +  where + ")" +
                " where d.nzp_file =" + receiver_nzp_file;
            ExecSQLWE(_connDb, sql);
            
            // ... nzp_ul
            sql = "update " + Points.Pref + DBManager.sUploadAliasRest + "file_dom d set " +
                " nzp_ul = (select max(t.nzp_ul) from " + temp_table + " t " + where + ")" +
                " where d.nzp_file =" + receiver_nzp_file;
            ExecSQLWE(_connDb, sql);

            ExecSQL(_connDb, "Drop table " + temp_table, false);
        }

        /// <summary>
        /// Квартиры
        /// </summary>
        private void CopyLinkKvar()
        {
            string temp_table = "tmp_file_kvar";

            ExecSQL(_connDb, "Drop table " + temp_table, false);

            string sql = "create temp table " + temp_table + " (" +
                " id          varchar(20), " +
                " dom_id_char varchar(20), " +
                " nzp_kvar integer, " +
                " nzp_dom  integer )";
            ExecSQLWE(_connDb, sql);

            sql = " insert into " + temp_table + " (id, dom_id_char, nzp_kvar, nzp_dom) " +
                " select id, dom_id_char, nzp_kvar, nzp_dom " +
                " from " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar " +
                " where nzp_file = " + source_nzp_file;
            ExecSQLWE(_connDb, sql);

            ExecSQLWE(_connDb, "create index ix_" + temp_table + "_nzp_dom  on " + temp_table + " (nzp_dom)");
            ExecSQLWE(_connDb, "create index ix_" + temp_table + "_nzp_kvar on " + temp_table + " (nzp_kvar)");
            ExecSQLWE(_connDb, "create index ix_" + temp_table + "_1 on " + temp_table + " (id, nzp_dom, dom_id_char)");
            ExecSQLWE(_connDb, "create index ix_" + temp_table + "_2 on " + temp_table + " (id, nzp_dom, nzp_kvar)");
            ExecSQLWE(_connDb, DBManager.sUpdStat + " " + temp_table);

            // ... nzp_dom
            sql = "update " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar fk set " + 
                " nzp_dom = (select max(t.nzp_dom) from " + temp_table + " t " +
                " where t.id          = fk.id " +
                "   and t.dom_id_char =  fk.dom_id_char " +
                "   and t.nzp_dom is not null) " +
                " where fk.nzp_file =" + receiver_nzp_file; ;
            ExecSQLWE(_connDb, sql);

            // ... nzp_kvar
            sql = "update " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar fk set " + 
                " nzp_kvar = (select max(t.nzp_kvar) from " + temp_table + " t " +
                " where t.id      =  fk.id " +
                "   and t.nzp_dom =  fk.nzp_dom " +
                "   and t.nzp_kvar is not null) " +
                " where nzp_file =" + receiver_nzp_file;
            ExecSQLWE(_connDb, sql);

            ExecSQL(_connDb, "Drop table " + temp_table, false);
        }
    }
}