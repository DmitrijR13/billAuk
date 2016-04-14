using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Utility;

namespace Bars.KP50.SzExchange.UnloadForSZ
{
    /// <summary>
    /// Выгрузка начисленных льгот
    /// </summary>
    public class UnlPassport : BaseUnloadClass
    {
       /* public override int Code
        {
            get
            {
                return 6;
            }
        } */

        public void Change(  )
        {

        }


        private StringBuilder _residentInfo = new StringBuilder();

        public override string Name
        {
            get { return "UnlPassport"; }
        }

        public override string NameText
        {
            get { return "Данные о гражданах"; }
        }

        public override void Start()
        {

        }

        public override void Start(FilesImported finder)
        {
            string sql;
            //string str;
            //string sep = "|";
            OpenConnection();
            CreateTempTable();

            WriteInFile w = new WriteInFile();

            try
            {
                WriteInUnlPassport(finder.bank, finder); //запись во временную таблицу

                w.Filing(FillFileHead(finder), finder.saved_name); 

                //выборка данных из временной таблицы 
                sql =
                    " SELECT * FROM " + Name;

                foreach (DataRow r in ExecSQLToTable(sql).Rows)
                {
                    //формирование строки данные о гражданах
                    /*
                    str =
                        "КодПр:" + Convert.ToString(rr["nzp_gil"]).Trim() + "," +
                        Convert.ToString(rr["nzp_kart"]).Trim() + Environment.NewLine +
                        "ФИО:" + Convert.ToString(rr["fam"]).Trim() + "," +
                        Convert.ToString(rr["ima"]).Trim() + "," + Convert.ToString(rr["otch"]).Trim() +
                        Environment.NewLine +
                        "ДатаРожд:" + ConvertToDate(rr["dat_rog"]) + Environment.NewLine +
                        "Пол:" + Convert.ToString(rr["gender"]).Trim() + Environment.NewLine + "ИзмФИО:" +
                        Convert.ToString(rr["fam_c"]).Trim() + "," +
                        Convert.ToString(rr["ima_c"]).Trim() + "," +
                        Convert.ToString(rr["otch_c"]).Trim() + Environment.NewLine + "ДатаРождИзм:" +
                        ConvertToDate(rr["dat_rog_c"]) + Environment.NewLine +
                        "ТипПроп:" + Convert.ToString(rr["tprp"]).Trim() + Environment.NewLine +
                        "ДатаОконПроп:" + ConvertToDate(rr["dat_oprp"]) + Environment.NewLine + "ТипКарт:" +
                        Convert.ToString(rr["nzp_tkrt"]).Trim() + Environment.NewLine +
                        "ЛицСчет:" + Convert.ToString(rr["nzp_kvar"]).Trim() + Environment.NewLine +
                        "Родство:" + Convert.ToString(rr["nzp_rod"]).Trim() + Environment.NewLine + "УдЛичн:" +
                        Convert.ToString(rr["nzp_dok"]).Trim() +
                        "," + Convert.ToString(rr["serij"]).Trim() +
                        " " + Convert.ToString(rr["nomer"]).Trim() + "," + ConvertToDate(rr["vid_data"]) +
                        Environment.NewLine +
                        "МестоРожд:" + Convert.ToString(rr["nzp_lnmr"]).Trim() + "," +
                        Convert.ToString(rr["indecs"]).Trim() + "," +
                        Convert.ToString(rr["nzp_stmr"]).Trim() + "," + Convert.ToString(rr["region_mr"]).Trim() + "," +
                        Convert.ToString(rr["gorod_mr"]).Trim() + "," + Environment.NewLine +
                        Convert.ToString(rr["npunkt_mr"]).Trim() + "," +
                        "ОткудаПриб:" + Convert.ToString(rr["nzp_lnop"]).Trim() + "," +
                        Convert.ToString(rr["indecs_op"]).Trim() + "," +
                        Convert.ToString(rr["nzp_stop"]).Trim() + "," +
                        Convert.ToString(rr["okrug_op"]).Trim() + "," + Convert.ToString(rr["gorod_op"]).Trim() + "," +
                        Convert.ToString(rr["npunkt_op"]).Trim() + Environment.NewLine +
                        "КудаУбыл:" + Convert.ToString(rr["nzp_lnku"]).Trim() + "," +
                        Convert.ToString(rr["indecs_ku"]).Trim() + "," +
                        Convert.ToString(rr["nzp_stku"]).Trim() + "," +
                        Convert.ToString(rr["okrug_ku"]).Trim() + "," +
                        Convert.ToString(rr["gorod_ku"]).Trim() + "," + Convert.ToString(rr["npunkt_ku"]).Trim() +
                        Environment.NewLine +
                        "ДатаСост:" + ConvertToDate(rr["dat_sost"]) + Environment.NewLine +
                        "ДатаОфор:" + ConvertToDate(rr["dat_ofor"]) + Environment.NewLine + "@@@";
                    //w.WriteFile(str);
                    //us.Start(pref, Convert.ToInt32(rr["nzp_kvar"]));
                    */

                    Add("КодПр:", new[] {r["nzp_gil"], r["nzp_kart"]});
                    Add("ФИО:", new[] {r["fam"], r["ima"], r["otch"]});
                    Add("ДатаРожд:", new[] {ConvertToDate(r["dat_rog"])});
                    Add("Пол:", new[] {r["gender"]});
                    Add("ИзмФИО:", new[] {r["fam_c"], r["ima_c"], r["otch_c"]});
                    Add("ИзмДатаРожд:", new[] { ConvertToDate(r["dat_rog_c"]) });
                    Add("ТипПроп:", new[] {r["tprp"]});
                    Add("ДатаОконПроп:", new[] {ConvertToDate(r["dat_oprp"])});
                    Add("ТипКарт:", new[] { r["typkrt"] });
                    Add("АдрПроп:", new[] { r["lsoato"], r["indecs_propiska"], Convert.ToString(r["ssoato"]).Substring(0,2), Convert.ToString(r["rajon"]).Trim(), Convert.ToString(r["town"]).Trim(), r["npunkt_propiska"],
                        Convert.ToString(r["ulica"]).Trim(), Convert.ToString(r["ndom"]).Trim(), Convert.ToString(r["nkor"]).Trim(), Convert.ToString(r["nkvar"]).Trim(), 
                        Convert.ToString(r["nkvar_n"]).Trim() });
                    Add("ЛицСчет:", new[] {r["nzp_kvar"]});
                    Add("Родство:", new[] {r["rodstvo"]});
                    Add("УдЛичн:", new[] { r["nzp_doc_oso"], Convert.ToString(r["serij"].ToString().Trim() + " " + r["nomer"]), ConvertToDate(r["vid_data"]), r["vid_mes"]});
                    Add("Причина:", new[] { r["reason"] });
                    Add("МестоРожд:", new[] { r["nzp_lnmr"], r["indecs"], r["stat"], r["region_mr"], r["gorod_mr"], r["npunkt_mr"] });
                    //Add("МестоРожд:",new[] {r["nzp_lnmr"], r["indecs"], r["nzp_stmr"], r["region_mr"], r["gorod_mr"], r["npunkt_mr"]});
                    Add("ОткудаПриб:", new[] { r["nzp_lnop"], r["indecs_op"], r["stat_op"], r["okrug_op"], r["gorod_op"], r["npunkt_op"] });
                    //Add("ОткудаПриб:",new[]{r["nzp_lnop"], r["indecs_op"], r["nzp_stop"], r["okrug_op"], r["gorod_op"], r["npunkt_op"]});
                    Add("КудаУбыл:", new[] { r["nzp_lnku"], r["indecs_ku"], r["stat_ku"], r["okrug_ku"], r["gorod_ku"], r["npunkt_ku"] });
                    //Add("КудаУбыл:",new[]{r["nzp_lnku"], r["indecs_ku"], r["nzp_stku"], r["okrug_ku"], r["gorod_ku"], r["npunkt_ku"]});
                    Add("ДатаСост:", new[] {ConvertToDate(r["dat_sost"])});
                    Add("ДатаОфор:", new[] {ConvertToDate(r["dat_ofor"])});
                    Add("@@@", null);

                }

                w.Filing(_residentInfo.ToString(), finder.saved_name);
                 w.Filing(GetComment(),finder.saved_name_log);
            }
                
            catch (Exception ex)
            {
                MonitorLog.WriteLog("UnlPassport.Start(pref): Ошибка добавления полей в таблицу.\n: " + ex.Message + ex.StackTrace, MonitorLog.typelog.Error, true);
            }
            finally
            {
                DropTempTable();
                CloseConnection();
            }

        }

        private void Add(string name, IEnumerable<object> data)
        {
            if (data == null)
            {
                _residentInfo.AppendLine(name);
                return;
            }

            //_residentInfo.AppendLine(
            //    data.Aggregate
            //    (
            //    name,
            //    (current, t) => t.ToString() == "-1" ? current + "" : current + (t.ToString().Trim() + "%,")
            //    ).TrimEnd(',').Replace("%",""));

            string result = name;

            foreach (var x in data)
            {
                if (x.ToString() == "-1")
                {
                    result += ",";
                    continue;
                }
                
                result += x.ToString().Trim() + "%,";
            }

            //result+=result.TrimEnd(',').Replace("%","");
            _residentInfo.AppendLine((result).TrimEnd(',').Replace("%",""));
        }

        private string ConvertToDate(object obj)
        {
            if (String.IsNullOrEmpty(Convert.ToString(obj)))
            {
                return "";
            }
            else
            {
                DateTime date = Convert.ToDateTime(obj);
                return date.ToString("dd.MM.yyyy");
            }
        }

        private string FillFileHead(FilesImported finder)
        {

            DateTime dats = new DateTime(Convert.ToInt32(finder.year), Convert.ToInt32(finder.month), 1); //1й день месяца выгрузки
            DateTime datpo = new DateTime(Convert.ToInt32(finder.year), Convert.ToInt32(finder.month),
                DateTime.DaysInMonth(Convert.ToInt32(finder.year), Convert.ToInt32(finder.month)));      //последний день месяца выгрузки

            string sql =
                " SELECT COUNT(*)  AS count " +
                " FROM " + Name;
            int notesCount = ExecSQLToTable(sql).Rows.Count;

            sql =
                " DROP TABLE " + Name + "_Head";
            ExecSQL(sql, false);


            sql =
                " CREATE TEMP TABLE " + Name + "_Head (" +
                " name_org CHAR (40), " +
                " phone_number CHAR (40) ) ";
            ExecSQL(sql);

            StringBuilder fileHead = new StringBuilder();
            fileHead.AppendLine("ИдФайл:0020*****************20050216165725;");
            fileHead.AppendFormat("НомФайл:{0};{1}", finder.nzp_exc, Environment.NewLine);
            fileHead.AppendLine("ТипИнф:ПТЖХ_Проживающие;");
            fileHead.AppendLine("КодПТЖХ:0020;");


            /*sql = " UPDATE " + Name + "_Head SET name_org = " +
                  " (SELECT val_prm as name_org FROM " + finder.bank + DBManager.sDataAliasRest + " prm_10 " +
                  " WHERE nzp_prm = 80 " +
                  " AND is_actual <> 100 " +
                  " AND dat_s <= '" + datpo + "'" +
                  " AND dat_po >= '" + dats + "')";
            ExecSQL(sql);

            sql = " UPDATE " + Name + "_Head SET phone_number = " +
                  " (SELECT val_prm as phone_number FROM " + finder.bank + DBManager.sDataAliasRest + " prm_10 " +
                  " WHERE nzp_prm = 96 " +
                  " AND is_actual <> 100 " +
                  " AND dat_s <= '" + datpo + "'" +
                  " AND dat_po >= '" + dats + "')";
            ExecSQL(sql);*/

            sql =
                " SELECT * FROM " + Name + "_Head";


            sql = "SELECT p1.val_prm as name_org , p2.val_prm as phone_number " +
                  "FROM " + finder.bank + DBManager.sDataAliasRest + " prm_10  p1, " +
                  finder.bank + DBManager.sDataAliasRest + " prm_10  p2 " +
                  "WHERE p1.nzp_prm = 80   " +
                  "AND p1.is_actual <> 100   " +
                  "AND p1.dat_s <= '" + datpo + "'  " + // '01.01.2000 0:00:00' 
                  "AND p1.dat_po >= '" + dats + "' " + //  '01.01.2020 0:00:00'
                  "ANd p2.nzp_prm = 96   " +
                  "AND p2.is_actual <> 100   " +
                  "AND p2.dat_s <= '" + datpo + "'  " + // '01.01.2005 0:00:00' 
                  "AND p2.dat_po >= '" + dats + "'";   // '01.01.2020 0:00:00'

            DataTable dt = ExecSQLToTable(sql);
            if (dt.Rows.Count == 0)
            {
                throw new Exception("Не заполнены обязательные поля: НаимОтпр и ТелОтпр");
            }
            

            /*DataRow rr = ExecSQLToTable(sql).Rows[0];
            if (rr == null)
            {
                throw new Exception("Ололо");
            }*/

            fileHead.AppendLine("НаимОтпр:" + Convert.ToString(dt.Rows[0]["name_org"]).Trim() + ";");
            fileHead.AppendLine("ТелОтпр:" + Convert.ToString(dt.Rows[0]["phone_number"]).Trim() + ";");
            //fileHead.AppendLine("НаимОтпр:ООО Заинский единый расчетный центр;");
            //fileHead.AppendLine("ТелОтпр:3-18-65;");
            fileHead.AppendLine("ФИООтпр:Микалина Ирина;");
            fileHead.AppendLine(String.Format("КолИнф:{0};", notesCount));
            fileHead.AppendLine(String.Format("ДатНач:01.{0}.{1};",finder.month,finder.year));
            fileHead.AppendLine(String.Format("ДатКон:{0}.{1}.{2};",
                DateTime.DaysInMonth(Convert.ToInt32(finder.year), Convert.ToInt32(finder.month)), 
                finder.month,
                finder.year));
            fileHead.AppendLine("###");

            CheckColumnOnEmptiness("name_org", "null", "null-ы в строке " + NameText + ",в поле - 'НаимОтпр'");
            CheckColumnOnEmptiness("phone_number", "null", "null-ы в строке " + NameText + ",в поле - 'ТелОтпр'");

            return fileHead.ToString();
        }

        public override void CreateTempTable()
        {
            string sql;

            sql =
                " DROP TABLE " + Name;
            ExecSQL(sql, false);


            sql =
                " CREATE TEMP TABLE " + Name + "(" +
                " nzp_gil INTEGER, " +
                " nzp_kart INTEGER, " +
                " fam CHAR (40), " +
                " ima CHAR (40), " +
                " otch CHAR (40), " +
                " dat_rog DATE , " +
                " gender CHAR (10), " +
                " fam_c CHAR (40), " +
                " ima_c CHAR (40), " +
                " otch_c CHAR (40), " +
                " dat_rog_c DATE, " +
                " tprp CHAR (10), " +
                " dat_oprp DATE, " +
                " nzp_tkrt INTEGER, " +
                " typkrt CHAR (30), " +
                " nzp_kvar INTEGER, " +
                " nzp_rod INTEGER, " +
                " rodstvo CHAR (30), " +
                " nzp_dok INTEGER, " +
                " nzp_doc_oso CHAR (10), " +
                " serij CHAR (10), " +
                " nomer CHAR (7), " +
                " vid_data DATE, " +
                " vid_mes CHAR (130), " +
                " reason CHAR (40), " +
                " nzp_lnmr INTEGER," +
                " nzp_tnmr INTEGER, " +
                " nzp_rnmr INTEGER, " +
                " stat CHAR (30), " +
                " indecs CHAR(6), " +
                " nzp_stmr INTEGER, " +
                " region_mr CHAR (30), " +
                " gorod_mr CHAR (30), " +
                " npunkt_mr CHAR (30), " +
                " nzp_lnop INTEGER, " +
                " nzp_tnop INTEGER, " +
                " nzp_rnop INTEGER, " +
                " stat_op CHAR (30)," +
                " indecs_op CHAR (6), " +
                " nzp_stop INTEGER, " +
                " okrug_op CHAR (30), " +
                " gorod_op CHAR (30), " +
                " npunkt_op CHAR (30), " +
                " nzp_celp INTEGER, " +
                " nzp_lnku INTEGER, " +
                " nzp_tnku INTEGER, " +
                " nzp_rnku INTEGER, " +
                " stat_ku CHAR (30)," +
                " indecs_ku CHAR (6), " +
                " nzp_stku INTEGER, " +
                " okrug_ku CHAR (30), " +
                " gorod_ku CHAR (30), " +
                " npunkt_ku CHAR (30), " +
                " nzp_celu INTEGER, " +
                " nzp_land INTEGER, " +
                " lsoato INTEGER, " +
                " indecs_propiska CHAR (6), " +
                " nzp_stat INTEGER, " +
                " ssoato BIGINT, " +
                " ssoato_mr_op_ku BIGINT, " +
                " rajon CHAR (30), " +
                " town CHAR (30), " +
                " npunkt_propiska CHAR (30), " +
                " ulica CHAR (40), " +
                " ndom CHAR (10), " +
                " nkor CHAR (3), " +
                " nkvar CHAR (10), " +
                " nkvar_n CHAR (10), " +
                " name_org CHAR (40), " +
                " phone_number CHAR (40), " +
                " dat_sost DATE, " +
                " dat_ofor DATE " +
                ")";
            ExecSQL(sql);
            
            sql =
                " CREATE INDEX " + Name + "_nzp_kvar_idx " + " ON " + Name + "(nzp_kvar)";
            ExecSQL(sql);

            sql = " ANALYZE " + Name;
            ExecSQL(sql);
        }

        /// <summary>
        /// Запись во временную таблицу
        /// </summary>
        /// <param name="pref"></param>
        public void WriteInUnlPassport(string pref, FilesImported finder)
        {
            string sql;

            DateTime dats = new DateTime(Convert.ToInt32(finder.year), Convert.ToInt32(finder.month), 1); //1й день месяца выгрузки
            DateTime datpo = new DateTime(Convert.ToInt32(finder.year), Convert.ToInt32(finder.month),
                DateTime.DaysInMonth(Convert.ToInt32(finder.year), Convert.ToInt32(finder.month)));      //последний день месяца выгрузки

            // max cur_unl для того, чтобы брать только измененные данные
            sql = " SELECT max(cur_unl) FROM " +
                  pref + DBManager.sDataAliasRest + " w_cur_unl ";
            var dt = ExecSQLToTable(sql);

            // max nzp_prot для того, чтобы брать только измененные данные
            sql = " SELECT max(nzp_prot) FROM " +
                  pref + DBManager.sDataAliasRest + "in_prot " +
                  " WHERE nzp_reg = 1 ";
            var dt1 = ExecSQLToTable(sql);

            var cur_unl = dt.Rows[0][0].ToString();

            var nzp_prot = dt1.Rows[0][0].ToString();

            sql =
                " SELECT COUNT(*)  AS count " +
                " FROM " + Name;
            int notesCount = ExecSQLToTable(sql).Rows.Count;
            
            //sql =
            //    "INSERT INTO " + Name +
            //    "   (nzp_gil,	" +
            //    "   nzp_kart,  " +
            //    "   fam,       " +
            //    "   ima,       " +
            //    "   otch,      " +
            //    "   dat_rog,   " +
            //    "   gender,    " +
            //    "   fam_c,     " +
            //    "   ima_c,     " +
            //    "   otch_c,    " +
            //    "   dat_rog_c, " +
            //    "   tprp,      " +
            //    "   dat_oprp,  " +
            //    "   nzp_tkrt,  " +
            //    "   nzp_kvar,  " +
            //    "   nzp_rod,   " +
            //    "   rodstvo,   " +
            //    "   nzp_dok,   " +
            //    "   serij,     " +
            //    "   nomer,     " +
            //    "   vid_data,  " +
            //    "   vid_mes,   " +
            //    "   nzp_lnmr,  " +
            //    "   nzp_tnmr,  " +
            //    "   nzp_rnmr,  " + 
            //    "   nzp_stmr,  " +
            //    "   region_mr, " +
            //    "   gorod_mr,  " +
            //    "   npunkt_mr, " +
            //    "   nzp_lnop,  " +
            //    "   nzp_tnop,  " +
            //    "   nzp_rnop,  " +
            //    "   nzp_stop,  " +
            //    "   okrug_op,  " +
            //    "   gorod_op,  " +
            //    "   npunkt_op, " +
            //    "   nzp_celp,  " +
            //    "   nzp_lnku,  " +
            //    "   nzp_tnku,  " +
            //    "   nzp_rnku,  " +
            //    "   nzp_stku,  " +
            //    "   okrug_ku,  " +
            //    "   gorod_ku,  " +
            //    "   npunkt_ku, " +
            //    "   nzp_celu,  " +
            //    "   dat_sost,  " +
            //    "   dat_ofor)  " +
            //    " SELECT " +
            //    "    nzp_gil,				 " +
            //    "    nzp_kart,               " +
            //    "    TRIM(fam),              " +
            //    "    TRIM(ima),              " +
            //    "    TRIM(otch),             " +
            //    "    cast(dat_rog as DATE),  " +
            //    "    gender,                 " +
            //    "    TRIM(fam_c),            " +
            //    "    TRIM(ima_c),            " +
            //    "    TRIM(otch_c),           " +
            //    "    cast(dat_rog_c as DATE)," +
            //    "    tprp,                   " +
            //    "    cast(dat_oprp as DATE), " +
            //    "    nzp_tkrt,               " +
            //    "    nzp_kvar,               " +
            //    "    nzp_rod,                " +
            //    "    rodstvo,                " +
            //    "    nzp_dok,                " +
            //    "    serij,                  " +
            //    "    nomer,                  " +
            //    "    cast(vid_dat as DATE),  " +
            //    "    cast(Replace(vid_mes,',','') as CHAR (80))," +
            //    "    nzp_lnmr,               " +
            //    "    nzp_tnmr,               " +
            //    "    nzp_rnmr,               " +
            //    "    nzp_stmr,               " +
            //    "    TRIM(region_mr),        " +
            //    "    TRIM(gorod_mr),         " +
            //    "    TRIM(npunkt_mr),        " +
            //    "    nzp_lnop,               " +
            //    "    nzp_tnop,               " +
            //    "    nzp_rnop,               " +
            //    "    nzp_stop,               " +
            //    "    TRIM(okrug_op),         " +
            //    "    TRIM(gorod_op),         " +
            //    "    TRIM(npunkt_op),        " +
            //    "    nzp_celp,               " +
            //    "    nzp_lnku,               " +
            //    "    nzp_tnku,               " +
            //    "    nzp_rnku,               " +
            //    "    nzp_stku,               " +
            //    "    TRIM(okrug_ku),         " +
            //    "    TRIM(gorod_ku),         " +
            //    "    TRIM(npunkt_ku),        " +
            //    "    nzp_celu,               " +
            //    "    cast(dat_sost as DATE), " +
            //    "    cast(dat_ofor as DATE)  " +
            //    " FROM " + pref + DBManager.sDataAliasRest + "kart";
            //if (!string.IsNullOrEmpty(cur_unl) && !string.IsNullOrEmpty(nzp_prot))
            //{
            //    sql += " WHERE cur_unl <= " + cur_unl +
            //        " AND cur_unl >=" + (Convert.ToInt32(nzp_prot) + 1);
            //}
            //ExecSQL(sql);

            // w_cur_unl

            if (!string.IsNullOrEmpty(nzp_prot) && !string.IsNullOrEmpty(cur_unl))
            {
                // Вставка (обновление) в in_prot nzp_prot и данные о выгрузке 
                sql = " INSERT INTO " + pref + DBManager.sDataAliasRest +
                      "in_prot (is_loaded, nzp_reg, id_file, file_name, nzp_user, dat_save)" + // nzp_prot,
                      " VALUES (0, 1,  " + finder.nzp_exc + ", '" + finder.saved_name + "', 1,'" +
                      DateTime.Now.ToString("yyyy-MM-dd") + "')";
                    // date time now " + (Convert.ToInt32(nzp_prot) + 1) + ",
                ExecSQL(sql);

                sql = " SELECT max(nzp_prot) FROM " +
                      pref + DBManager.sDataAliasRest + "in_prot " +
                      " WHERE nzp_reg = 1 ";
                var dt2 = ExecSQLToTable(sql);

                var nzp_prot_1 = dt2.Rows[0][0].ToString();

                // ищем есть ли данные с nzp_prot меньше, чем при insert данных о нововй выгрузки
                sql = " SELECT * FROM " + pref + DBManager.sDataAliasRest + " in_prot " +
                      " WHERE nzp_prot < " + nzp_prot_1 +
                      " AND nzp_reg = 1 ";
                var dt3 = ExecSQLToTable(sql);

                //sql = " SELECT max(nzp_prot) FROM " + pref + DBManager.sDataAliasRest + " in_prot " +
                //      " WHERE nzp_reg = 1 ";
                //// возможно нужно выбирать max значение nzp_prot
                //var dt4 = ExecSQLToTable(sql);

                //var nzp_prot_2 = dt4.Rows[0][0].ToString();

                if (dt3.Rows.Count != 0)
                {
                    int cur_unl_1 = (Convert.ToInt32(cur_unl)-1);// (Convert.ToInt32(cur_unl) + 1)
                    //int nzp_prot_3 = (Convert.ToInt32(nzp_prot) + 1);// скорее всего не нужно прибалять 1 к nzp_prot можно просто брать nzp_prot_1

                    // insert 
                    sql =
                        "INSERT INTO " + Name +
                        "   (nzp_gil,	" +
                        "   nzp_kart,  " +
                        "   fam,       " +
                        "   ima,       " +
                        "   otch,      " +
                        "   dat_rog,   " +
                        "   gender,    " +
                        "   fam_c,     " +
                        "   ima_c,     " +
                        "   otch_c,    " +
                        "   dat_rog_c, " +
                        "   tprp,      " +
                        "   dat_oprp,  " +
                        "   nzp_tkrt,  " +
                        "   nzp_kvar,  " +
                        "   nzp_rod,   " +
                        "   rodstvo,   " +
                        "   nzp_dok,   " +
                        "   serij,     " +
                        "   nomer,     " +
                        "   vid_data,  " +
                        "   vid_mes,   " +
                        "   nzp_lnmr,  " +
                        "   nzp_tnmr,  " +
                        "   nzp_rnmr,  " +
                        "   nzp_stmr,  " +
                        "   region_mr, " +
                        "   gorod_mr,  " +
                        "   npunkt_mr, " +
                        "   nzp_lnop,  " +
                        "   nzp_tnop,  " +
                        "   nzp_rnop,  " +
                        "   nzp_stop,  " +
                        "   okrug_op,  " +
                        "   gorod_op,  " +
                        "   npunkt_op, " +
                        "   nzp_celp,  " +
                        "   nzp_lnku,  " +
                        "   nzp_tnku,  " +
                        "   nzp_rnku,  " +
                        "   nzp_stku,  " +
                        "   okrug_ku,  " +
                        "   gorod_ku,  " +
                        "   npunkt_ku, " +
                        "   nzp_celu,  " +
                        "   dat_sost,  " +
                        "   dat_ofor)  " +
                        " SELECT " +
                        "    nzp_gil,				 " +
                        "    nzp_kart,               " +
                        "    TRIM(fam),              " +
                        "    TRIM(ima),              " +
                        "    TRIM(otch),             " +
                        "    cast(dat_rog as DATE),  " +
                        "    gender,                 " +
                        "    TRIM(fam_c),            " +
                        "    TRIM(ima_c),            " +
                        "    TRIM(otch_c),           " +
                        "    cast(dat_rog_c as DATE)," +
                        "    tprp,                   " +
                        "    cast(dat_oprp as DATE), " +
                        "    nzp_tkrt,               " +
                        "    nzp_kvar,               " +
                        "    nzp_rod,                " +
                        "    rodstvo,                " +
                        "    nzp_dok,                " +
                        "    serij,                  " +
                        "    nomer,                  " +
                        "    cast(vid_dat as DATE),  " +
                        "    cast(Replace(vid_mes,',','') as CHAR (80))," +
                        "    nzp_lnmr,               " +
                        "    nzp_tnmr,               " +
                        "    nzp_rnmr,               " +
                        "    nzp_stmr,               " +
                        "    TRIM(region_mr),        " +
                        "    TRIM(gorod_mr),         " +
                        "    TRIM(npunkt_mr),        " +
                        "    nzp_lnop,               " +
                        "    nzp_tnop,               " +
                        "    nzp_rnop,               " +
                        "    nzp_stop,               " +
                        "    TRIM(okrug_op),         " +
                        "    TRIM(gorod_op),         " +
                        "    TRIM(npunkt_op),        " +
                        "    nzp_celp,               " +
                        "    nzp_lnku,               " +
                        "    nzp_tnku,               " +
                        "    nzp_rnku,               " +
                        "    nzp_stku,               " +
                        "    TRIM(okrug_ku),         " +
                        "    TRIM(gorod_ku),         " +
                        "    TRIM(npunkt_ku),        " +
                        "    nzp_celu,               " +
                        "    cast(dat_sost as DATE), " +
                        "    cast(dat_ofor as DATE)  " +
                        " FROM " + pref + DBManager.sDataAliasRest + "kart k" +
                        " WHERE k.cur_unl > " + cur_unl_1 +
                        " AND k.cur_unl <= " + cur_unl;
                   ExecSQL(sql);

                    // обновляем данные в w_cur_unl  для следующей выгрузки
                    sql = " UPDATE " + pref + DBManager.sDataAliasRest + "w_cur_unl SET cur_unl = " +
                          (Convert.ToInt32(cur_unl) + 1); //(Convert.ToInt32(nzp_prot) + 2)
                    ExecSQL(sql);

                    // заполнение таблицы in_prot_prm
                    sql = " INSERT INTO " + pref + DBManager.sDataAliasRest + "in_prot " +
                          " (nzp_prot, nzp_param, val_param) " +
                          " VALUES (" +
                          // номер файла
                          " (SELECT nzp_prot FROM " + pref + DBManager.sDataAliasRest + "in_prot " +
                          " WHERE nzp_reg = 1 " +
                          " AND id_file = '" + finder.nzp_exc +
                          " '), " +
                          " 1, '" + finder.nzp_exc + "' )," +
                          // дата начала
                          " (( SELECT nzp_prot FROM " + pref + DBManager.sDataAliasRest + "in_prot " +
                          " WHERE nzp_reg = 1 " +
                          " AND id_file = '" + finder.nzp_exc +
                          " '), " +
                          " 2, '" + dats + "' ), " +
                          // дата окончания
                          " (( SELECT nzp_prot FROM " + pref + DBManager.sDataAliasRest + "in_prot " +
                          " WHERE nzp_reg = 1 " +
                          " AND id_file = '" + finder.nzp_exc +
                          " '), " +
                          " 3, '" + datpo + "' ), " +
                          // количество информации
                          " ((SELECT nzp_prot FROM " + pref + DBManager.sDataAliasRest + "in_prot " +
                          " WHERE nzp_reg = 1 " +
                          " AND id_file = '" + finder.nzp_exc +
                          " '), " +
                          " 4, '" + notesCount + "' )," +
                          // код ПТЖХ
                          " ((SELECT nzp_prot FROM " + pref + DBManager.sDataAliasRest + "in_prot " +
                          " WHERE nzp_reg = 1 " +
                          " AND id_file = '" + finder.nzp_exc +
                          " '), " +
                          " 5, '0020')," +
                          // наименование отправителя
                          " ((SELECT nzp_prot FROM " + pref + DBManager.sDataAliasRest + "in_prot " +
                          " WHERE nzp_reg = 1 " +
                          " AND id_file = '" + finder.nzp_exc +
                          " '), " +
                          " 6, (SELECT val_prm as name_org FROM " + finder.bank + DBManager.sDataAliasRest + " prm_10 " +
                          " WHERE nzp_prm = 80 " +
                          " AND is_actual <> 100 " +
                          " AND dat_s <= '" + datpo + "'" +
                          " AND dat_po >= '" + dats + "'))," +
                          // телефон отправителя
                          " ((SELECT nzp_prot FROM " + pref + DBManager.sDataAliasRest + "in_prot " +
                          " WHERE nzp_reg = 1 " +
                          " AND id_file = '" + finder.nzp_exc +
                          " '), " +
                          " 7, (SELECT val_prm as phone_number FROM " + finder.bank + DBManager.sDataAliasRest +
                          " prm_10 " +
                          " WHERE nzp_prm = 96 " +
                          " AND is_actual <> 100 " +
                          " AND dat_s <= '" + datpo + "'" +
                          " AND dat_po >= '" + dats + "'))," +
                          // ФИО отправителя
                          " ((SELECT nzp_prot FROM " + pref + DBManager.sDataAliasRest + "in_prot " +
                          " WHERE nzp_reg = 1 " +
                          " AND id_file = '" + finder.nzp_exc +
                          " '), " +
                          " 8, 'Микалина Ирина')," +
                          // номер конечного тика
                          " ((SELECT nzp_prot FROM " + pref + DBManager.sDataAliasRest + "in_prot " +
                          " WHERE nzp_reg = 1 " +
                          " AND id_file = '" + finder.nzp_exc +
                          " '), " +
                          " 13, '" + cur_unl + "' ), " +
                          // номер начального тика
                          " ((SELECT nzp_prot FROM " + pref + DBManager.sDataAliasRest + "in_prot " +
                          " WHERE nzp_reg = 1 " +
                          " AND id_file = '" + finder.nzp_exc +
                          " '), " +
                          " 14, '" + cur_unl_1 + "')";
                    ExecSQL(sql);

                }

                // #region делать только тогда, когда нет тригера для обновления w_cur_unl при insert в in_prot
                // обновялется ли cur_unl

                //if (!string.IsNullOrEmpty(cur_unl))
                //{
                    //sql = " INSERT INTO " +
                    //  pref + DBManager.sDataAliasRest + "w_cur_unl (cur_unl, nzp_wp) VALUES (" +
                    //  " (SELECT max(cur_unl) FROM " + pref + DBManager.sDataAliasRest + "kart)" + ",1)";
                    //ExecSQL(sql);

                    // Вставка (обновление) в w_cur_unl для следующей выгрузки 
                    //sql = " UPDATE " + pref + DBManager.sDataAliasRest + "w_cur_unl SET cur_unl = " +
                    //      (Convert.ToInt32(cur_unl) + 1); //(Convert.ToInt32(nzp_prot) + 2)
                    //ExecSQL(sql);
                //}
                //else
                //{
                //    //sql = " INSERT INTO " +
                //    //  pref + DBManager.sDataAliasRest + "w_cur_unl (cur_unl, nzp_wp) VALUES (" +
                //    //  (Convert.ToInt32(nzp_prot) + 2) + ",1)";
                //    //ExecSQL(sql);

                //    sql = " INSERT INTO " +
                //          pref + DBManager.sDataAliasRest + "w_cur_unl (cur_unl, nzp_wp) VALUES (" +
                //          (Convert.ToInt32(cur_unl) + 1) + ",1)";
                //    ExecSQL(sql);
                //}
                //#endregion
            }
            else
            {
                if (string.IsNullOrEmpty(nzp_prot))
                {
                    sql = " INSERT INTO " + pref + DBManager.sDataAliasRest +
                          "in_prot (is_loaded, nzp_reg, id_file, file_name, nzp_user, dat_save)" + // nzp_prot,
                          " VALUES (0, 1,  " + finder.nzp_exc + ", '" + finder.saved_name + "', 1,'" +
                          DateTime.Now.ToString("yyyy-MM-dd") + "')";
                        // " + (Convert.ToInt32(nzp_prot) + 1) + ", nzp_prot
                    ExecSQL(sql);

                    // выбор max значения nzp_prot для проверки
                    sql = " SELECT max(nzp_prot) FROM " +
                          pref + DBManager.sDataAliasRest + "in_prot " +
                          " WHERE nzp_reg = 1 ";
                    var dt2 = ExecSQLToTable(sql);

                    var nzp_prot_1 = dt2.Rows[0][0].ToString();

                    // ищем есть ли данные с nzp_prot меньше, чем при insert данных о нововй выгрузки
                    sql = " SELECT * FROM " + pref + DBManager.sDataAliasRest + " in_prot " +
                          " WHERE nzp_prot < " + nzp_prot_1 +
                          " AND nzp_reg = 1 ";
                    var dt4 = ExecSQLToTable(sql);

                    //sql = " SELECT nzp_prot FROM " + pref + DBManager.sDataAliasRest + " in_prot " +
                    //      " WHERE nzp_reg = 1 ";
                    //// возможно нужно выбирать max значение nzp_prot
                    //var dt3 = ExecSQLToTable(sql);

                    //var nzp_prot_2 = dt3.Rows[0][0].ToString();

                    if (dt4.Rows.Count == 0)
                        //(nzp_prot_2 == nzp_prot_1)
                    {
                        int cur_unl_1 = 1; // с какого начинаем выгружать

                        if (string.IsNullOrEmpty(cur_unl))
                        {
                            sql = " INSERT INTO " +
                                  pref + DBManager.sDataAliasRest + "w_cur_unl (cur_unl, nzp_wp) VALUES (" +
                                  " 1, 1)";
                            ExecSQL(sql);
                        }
                        
                        sql =
                            "INSERT INTO " + Name +
                            "   (nzp_gil,	" +
                            "   nzp_kart,  " +
                            "   fam,       " +
                            "   ima,       " +
                            "   otch,      " +
                            "   dat_rog,   " +
                            "   gender,    " +
                            "   fam_c,     " +
                            "   ima_c,     " +
                            "   otch_c,    " +
                            "   dat_rog_c, " +
                            "   tprp,      " +
                            "   dat_oprp,  " +
                            "   nzp_tkrt,  " +
                            "   nzp_kvar,  " +
                            "   nzp_rod,   " +
                            "   rodstvo,   " +
                            "   nzp_dok,   " +
                            "   serij,     " +
                            "   nomer,     " +
                            "   vid_data,  " +
                            "   vid_mes,   " +
                            "   nzp_lnmr,  " +
                            "   nzp_tnmr,  " +
                            "   nzp_rnmr,  " +
                            "   nzp_stmr,  " +
                            "   region_mr, " +
                            "   gorod_mr,  " +
                            "   npunkt_mr, " +
                            "   nzp_lnop,  " +
                            "   nzp_tnop,  " +
                            "   nzp_rnop,  " +
                            "   nzp_stop,  " +
                            "   okrug_op,  " +
                            "   gorod_op,  " +
                            "   npunkt_op, " +
                            "   nzp_celp,  " +
                            "   nzp_lnku,  " +
                            "   nzp_tnku,  " +
                            "   nzp_rnku,  " +
                            "   nzp_stku,  " +
                            "   okrug_ku,  " +
                            "   gorod_ku,  " +
                            "   npunkt_ku, " +
                            "   nzp_celu,  " +
                            "   dat_sost,  " +
                            "   dat_ofor)  " +
                            " SELECT " +
                            "    nzp_gil,				 " +
                            "    nzp_kart,               " +
                            "    TRIM(fam),              " +
                            "    TRIM(ima),              " +
                            "    TRIM(otch),             " +
                            "    cast(dat_rog as DATE),  " +
                            "    gender,                 " +
                            "    TRIM(fam_c),            " +
                            "    TRIM(ima_c),            " +
                            "    TRIM(otch_c),           " +
                            "    cast(dat_rog_c as DATE)," +
                            "    tprp,                   " +
                            "    cast(dat_oprp as DATE), " +
                            "    nzp_tkrt,               " +
                            "    nzp_kvar,               " +
                            "    nzp_rod,                " +
                            "    rodstvo,                " +
                            "    nzp_dok,                " +
                            "    serij,                  " +
                            "    nomer,                  " +
                            "    cast(vid_dat as DATE),  " +
                            "    cast(Replace(vid_mes,',','') as CHAR (80))," +
                            "    nzp_lnmr,               " +
                            "    nzp_tnmr,               " +
                            "    nzp_rnmr,               " +
                            "    nzp_stmr,               " +
                            "    TRIM(region_mr),        " +
                            "    TRIM(gorod_mr),         " +
                            "    TRIM(npunkt_mr),        " +
                            "    nzp_lnop,               " +
                            "    nzp_tnop,               " +
                            "    nzp_rnop,               " +
                            "    nzp_stop,               " +
                            "    TRIM(okrug_op),         " +
                            "    TRIM(gorod_op),         " +
                            "    TRIM(npunkt_op),        " +
                            "    nzp_celp,               " +
                            "    nzp_lnku,               " +
                            "    nzp_tnku,               " +
                            "    nzp_rnku,               " +
                            "    nzp_stku,               " +
                            "    TRIM(okrug_ku),         " +
                            "    TRIM(gorod_ku),         " +
                            "    TRIM(npunkt_ku),        " +
                            "    nzp_celu,               " +
                            "    cast(dat_sost as DATE), " +
                            "    cast(dat_ofor as DATE)  " +
                            " FROM " + pref + DBManager.sDataAliasRest + "kart k" +
                            " WHERE k.cur_unl >= " + cur_unl_1 +
                            " AND k.cur_unl <=" + cur_unl;
                        //if (!string.IsNullOrEmpty(cur_unl) && !string.IsNullOrEmpty(nzp_prot))
                        //{
                        //    sql += " WHERE cur_unl <= " + cur_unl_1 +
                        //           " AND cur_unl >=" + cur_unl; //(Convert.ToInt32(nzp_prot) + 1);
                        //}
                        ExecSQL(sql);

                        // обновляем данные в w_cur_unl  для следующей выгрузки
                        sql = " UPDATE " + pref + DBManager.sDataAliasRest + "w_cur_unl SET cur_unl = " +
                              (Convert.ToInt32(cur_unl) + 1); //(Convert.ToInt32(nzp_prot) + 2)
                        ExecSQL(sql);

                        // заполнение таблицы in_prot_prm
                        sql = " INSERT INTO " + pref + DBManager.sDataAliasRest + "in_prot " +
                              " (nzp_prot, nzp_param, val_param) " +
                              " VALUES (" +
                            // номер файла
                              " (SELECT nzp_prot FROM " + pref + DBManager.sDataAliasRest + "in_prot " +
                              " WHERE nzp_reg = 1 " +
                              " AND id_file = '" + finder.nzp_exc +
                              " '), " +
                              " 1, '" + finder.nzp_exc + "' )," +
                            // дата начала
                              " (( SELECT nzp_prot FROM " + pref + DBManager.sDataAliasRest + "in_prot " +
                              " WHERE nzp_reg = 1 " +
                              " AND id_file = '" + finder.nzp_exc +
                              " '), " +
                              " 2, '" + dats + "' ), " +
                            // дата окончания
                              " (( SELECT nzp_prot FROM " + pref + DBManager.sDataAliasRest + "in_prot " +
                              " WHERE nzp_reg = 1 " +
                              " AND id_file = '" + finder.nzp_exc +
                              " '), " +
                              " 3, '" + datpo + "' ), " +
                            // количество информации
                              " ((SELECT nzp_prot FROM " + pref + DBManager.sDataAliasRest + "in_prot " +
                              " WHERE nzp_reg = 1 " +
                              " AND id_file = '" + finder.nzp_exc +
                              " '), " +
                              " 4, '" + notesCount + "' )," +
                            // код ПТЖХ
                              " ((SELECT nzp_prot FROM " + pref + DBManager.sDataAliasRest + "in_prot " +
                              " WHERE nzp_reg = 1 " +
                              " AND id_file = '" + finder.nzp_exc +
                              " '), " +
                              " 5, '0020')," +
                            // наименование отправителя
                              " ((SELECT nzp_prot FROM " + pref + DBManager.sDataAliasRest + "in_prot " +
                              " WHERE nzp_reg = 1 " +
                              " AND id_file = '" + finder.nzp_exc +
                              " '), " +
                              " 6, (SELECT val_prm as name_org FROM " + finder.bank + DBManager.sDataAliasRest + " prm_10 " +
                              " WHERE nzp_prm = 80 " +
                              " AND is_actual <> 100 " +
                              " AND dat_s <= '" + datpo + "'" +
                              " AND dat_po >= '" + dats + "'))," +
                            // телефон отправителя
                              " ((SELECT nzp_prot FROM " + pref + DBManager.sDataAliasRest + "in_prot " +
                              " WHERE nzp_reg = 1 " +
                              " AND id_file = '" + finder.nzp_exc +
                              " '), " +
                              " 7, (SELECT val_prm as phone_number FROM " + finder.bank + DBManager.sDataAliasRest +
                              " prm_10 " +
                              " WHERE nzp_prm = 96 " +
                              " AND is_actual <> 100 " +
                              " AND dat_s <= '" + datpo + "'" +
                              " AND dat_po >= '" + dats + "'))," +
                            // ФИО отправителя
                              " ((SELECT nzp_prot FROM " + pref + DBManager.sDataAliasRest + "in_prot " +
                              " WHERE nzp_reg = 1 " +
                              " AND id_file = '" + finder.nzp_exc +
                              " '), " +
                              " 8, 'Микалина Ирина')," +
                            // номер конечного тика
                              " ((SELECT nzp_prot FROM " + pref + DBManager.sDataAliasRest + "in_prot " +
                              " WHERE nzp_reg = 1 " +
                              " AND id_file = '" + finder.nzp_exc +
                              " '), " +
                              " 13, '" + cur_unl + "' ), " +
                            // номер начального тика
                              " ((SELECT nzp_prot FROM " + pref + DBManager.sDataAliasRest + "in_prot " +
                              " WHERE nzp_reg = 1 " +
                              " AND id_file = '" + finder.nzp_exc +
                              " '), " +
                              " 14, '" + cur_unl_1 + "')";
                        ExecSQL(sql);

                    }

                }
            }




            sql = " UPDATE " + Name + " SET nzp_doc_oso = " +
                  " (SELECT nzp_oso FROM " + pref + DBManager.sDataAliasRest + " s_dok d " +
                  " WHERE d.nzp_dok = " + Name + ".nzp_dok)";
            ExecSQL(sql);

            sql =
                " UPDATE " + Name + " SET tprp = " +
                " (SELECT " +
                " CASE " +
                " WHEN tprp = 'П' THEN 'ПОСТОЯННАЯ' " +
                " WHEN tprp = 'В' THEN 'ВРЕМЕННАЯ' " +
                " END " +
                " FROM " + pref + DBManager.sDataAliasRest + " kart " +
                " WHERE nzp_kart = UnlPassport.nzp_kart) ";
            ExecSQL(sql);

            sql =
                " UPDATE " + Name + " set typkrt = " +
                " (SELECT typkrt FROM " + pref + DBManager.sDataAliasRest + " s_typkrt WHERE nzp_tkrt = UnlPassport.nzp_tkrt) ";
            ExecSQL(sql);

            sql =
                " UPDATE UnlPassport SET nzp_lnmr =  (SELECT " + 
                " CAST " +
                " ( " + 
                " (case when trim(soato) = '' then null else  soato end) " + 
                " AS integer " +
                " ) " +
                " FROM " + pref + DBManager.sDataAliasRest + " s_land l WHERE l.nzp_land = nzp_lnmr)  "; //кривый данные в базе

                //" UPDATE " + Name + " SET nzp_lnmr = " +
                //" (SELECT CAST(soato AS integer) FROM " + pref + DBManager.sDataAliasRest + " s_land l WHERE l.nzp_land = nzp_lnmr) ";
            ExecSQL(sql);

            sql =
                " UPDATE " + Name + " SET stat = " +
                " (SELECT stat FROM " + pref + DBManager.sDataAliasRest +
                " s_stat l WHERE l.nzp_stat = nzp_stmr) ";
            ExecSQL(sql);

            sql =
                " UPDATE " + Name + " SET gorod_mr = " +
                " (SELECT town FROM " + pref + DBManager.sDataAliasRest +
                " s_town l WHERE l.nzp_town = nzp_tnmr) ";
            ExecSQL(sql);

            sql =
                " UPDATE " + Name + " SET region_mr = " +
                " (SELECT rajon FROM " + pref + DBManager.sDataAliasRest +
                " s_rajon l WHERE l.nzp_raj = nzp_rnmr) ";
            ExecSQL(sql);

            sql =
                " UPDATE UnlPassport SET nzp_lnmr =  (SELECT " +
                " CAST " +
                " ( " +
                " (case when trim(soato) = '' then null else  soato end) " +
                " AS integer " +
                " ) " +
                " FROM " + pref + DBManager.sDataAliasRest + " s_land l WHERE l.nzp_land = nzp_lnop)  "; //кривый данные в базе
                //" UPDATE " + Name + " SET nzp_lnop = " +
                //" (SELECT cast(soato as integer) FROM " + pref + DBManager.sDataAliasRest +
                //" s_land l WHERE l.nzp_land = nzp_lnop) ";
            ExecSQL(sql);

            sql =
                " UPDATE " + Name + " SET stat_op = " +
                " (SELECT stat FROM " + pref + DBManager.sDataAliasRest +
                " s_stat l WHERE l.nzp_stat = nzp_stop) ";
            ExecSQL(sql);

            sql =
                " UPDATE " + Name + " SET gorod_op = " +
                " (SELECT town FROM " + pref + DBManager.sDataAliasRest +
                " s_town l WHERE l.nzp_town = nzp_tnop) ";
            ExecSQL(sql);

            sql =
                " UPDATE " + Name + " SET okrug_op = " +
                " (SELECT rajon FROM " + pref + DBManager.sDataAliasRest +
                " s_rajon l WHERE l.nzp_raj = nzp_rnop) ";
            ExecSQL(sql);

            sql =
                " UPDATE " + Name + " SET nzp_lnku = " +
                " (SELECT cast(soato as integer) FROM " + pref + DBManager.sDataAliasRest +
                " s_land l WHERE l.nzp_land = nzp_lnku) ";
            ExecSQL(sql);

            sql =
                " UPDATE " + Name + " SET gorod_ku = " +
                " (SELECT town FROM " + pref + DBManager.sDataAliasRest +
                " s_town l WHERE l.nzp_town = nzp_tnku) ";
            ExecSQL(sql);

            sql =
                " UPDATE " + Name + " SET okrug_ku = " +
                " (SELECT rajon FROM " + pref + DBManager.sDataAliasRest +
                " s_rajon l WHERE l.nzp_raj = nzp_rnku) ";
            ExecSQL(sql);

            sql =
                " UPDATE " + Name + " SET stat_ku = " +
                " (SELECT stat FROM " + pref + DBManager.sDataAliasRest +
                " s_stat l WHERE l.nzp_stat = nzp_stku) ";
            ExecSQL(sql);

            sql =
                " UPDATE " + Name + " SET rodstvo = " +
                " (SELECT rod FROM " + pref + DBManager.sDataAliasRest +
                " s_rod l WHERE l.nzp_rod = " + Name + " .nzp_rod) ";
            ExecSQL(sql);

            sql =
                " UPDATE " + Name + " SET nzp_kvar = " +
                " (SELECT nzp_kvar FROM " + pref + DBManager.sDataAliasRest +
                " kvar l WHERE l.nzp_kvar = " + Name + " .nzp_kvar) ";
            ExecSQL(sql);

            /*sql = " UPDATE " + Name + " SET vid_mes = " +
                  " (SELECT vid_mes FROM " + pref + DBManager.sDataAliasRest + " kart k " +
                  " WHERE k.vid_mes = " + Name + ".vid_mes)";
            ExecSQL(sql);*/

            sql = " UPDATE " + Name + " SET reason = " +
                  " (SELECT cel FROM " + pref + DBManager.sDataAliasRest + " s_cel c " +
                  " WHERE c.nzp_cel = " + Name + ".nzp_celp" +
                  " AND c.nzp_tkrt = " + Name + ".nzp_tkrt" +
                  " AND " + Name + ".nzp_tkrt = 1)" +
                  " WHERE EXISTS (" +
                  " SELECT 1 FROM " + pref + DBManager.sDataAliasRest + " s_cel c " +
                  " WHERE c.nzp_cel = " + Name + ".nzp_celp" +
                  " AND c.nzp_tkrt = " + Name + ".nzp_tkrt" +
                  " AND " + Name + ".nzp_tkrt = 1)";
            ExecSQL(sql);

            sql = " UPDATE " + Name + " SET reason = " +
                  " (SELECT cel FROM " + pref + DBManager.sDataAliasRest + " s_cel c " +
                  " WHERE c.nzp_cel = " + Name + ".nzp_celu" +
                  " AND c.nzp_tkrt = " + Name + ".nzp_tkrt" +
                  " AND " + Name + ".nzp_tkrt = 2)" +
                  " WHERE EXISTS (" +
                  " SELECT 1 FROM " + pref + DBManager.sDataAliasRest + " s_cel c " +
                  " WHERE c.nzp_cel = " + Name + ".nzp_celu" +
                  " AND c.nzp_tkrt = " + Name + ".nzp_tkrt" +
                  " AND " + Name + ".nzp_tkrt = 2)";
            ExecSQL(sql);

            sql = " UPDATE " + Name + " SET nzp_land = " +
                  " (SELECT l.nzp_land FROM " + pref + DBManager.sDataAliasRest + " s_land l, " +
                  pref + DBManager.sDataAliasRest + " s_stat s, " +
                  pref + DBManager.sDataAliasRest + " s_town t, " +
                  pref + DBManager.sDataAliasRest + " s_rajon r, " +
                  pref + DBManager.sDataAliasRest + " s_ulica u, " +
                  pref + DBManager.sDataAliasRest + " dom d, " +
                  pref + DBManager.sDataAliasRest + " kvar k " +
                  " WHERE k.nzp_kvar = " + Name + ".nzp_kvar " +
                  " AND k.nzp_dom = d.nzp_dom " +
                  " AND d.nzp_ul = u.nzp_ul " +
                  " AND u.nzp_raj = r.nzp_raj " +
                  " AND r.nzp_town = t.nzp_town " +
                  " AND t.nzp_stat = s.nzp_stat " +
                  " AND s.nzp_land = l.nzp_land) ";
                  ExecSQL(sql);

            sql = " UPDATE " + Name + " SET lsoato = " +
                  " (SELECT cast(l.soato as integer) FROM " + pref + DBManager.sDataAliasRest + " s_land l, " +
                  pref + DBManager.sDataAliasRest + " s_stat s, " +
                  pref + DBManager.sDataAliasRest + " s_town t, " +
                  pref + DBManager.sDataAliasRest + " s_rajon r, " +
                  pref + DBManager.sDataAliasRest + " s_ulica u, " +
                  pref + DBManager.sDataAliasRest + " dom d, " +
                  pref + DBManager.sDataAliasRest + " kvar k " +
                  " WHERE k.nzp_kvar = " + Name + ".nzp_kvar " +
                  " AND k.nzp_dom = d.nzp_dom " +
                  " AND d.nzp_ul = u.nzp_ul " +
                  " AND u.nzp_raj = r.nzp_raj " +
                  " AND r.nzp_town = t.nzp_town " +
                  " AND t.nzp_stat = s.nzp_stat " +
                  " AND s.nzp_land = l.nzp_land) ";
                  ExecSQL(sql);


            sql = " UPDATE " + Name + " SET nzp_stat = " +
                  " (SELECT s.nzp_stat FROM " + pref + DBManager.sDataAliasRest + " s_land l, " +
                  pref + DBManager.sDataAliasRest + " s_stat s, " +
                  pref + DBManager.sDataAliasRest + " s_town t, " +
                  pref + DBManager.sDataAliasRest + " s_rajon r, " +
                  pref + DBManager.sDataAliasRest + " s_ulica u, " +
                  pref + DBManager.sDataAliasRest + " dom d, " +
                  pref + DBManager.sDataAliasRest + " kvar k " +
                  " WHERE k.nzp_kvar = " + Name + ".nzp_kvar " +
                  " AND k.nzp_dom = d.nzp_dom " +
                  " AND d.nzp_ul = u.nzp_ul " +
                  " AND u.nzp_raj = r.nzp_raj " +
                  " AND r.nzp_town = t.nzp_town " +
                  " AND t.nzp_stat = s.nzp_stat " +
                  " AND s.nzp_land = l.nzp_land) ";
                  ExecSQL(sql);

            sql = " UPDATE " + Name + " SET ssoato = " +
                  " (SELECT cast(s.soato as bigint) FROM " + pref + DBManager.sDataAliasRest + " s_land l, " +
                  pref + DBManager.sDataAliasRest + " s_stat s, " +
                  pref + DBManager.sDataAliasRest + " s_town t, " +
                  pref + DBManager.sDataAliasRest + " s_rajon r, " +
                  pref + DBManager.sDataAliasRest + " s_ulica u, " +
                  pref + DBManager.sDataAliasRest + " dom d, " +
                  pref + DBManager.sDataAliasRest + " kvar k " +
                  " WHERE k.nzp_kvar = " + Name + ".nzp_kvar " +
                  " AND k.nzp_dom = d.nzp_dom " +
                  " AND d.nzp_ul = u.nzp_ul " +
                  " AND u.nzp_raj = r.nzp_raj " +
                  " AND r.nzp_town = t.nzp_town " +
                  " AND t.nzp_stat = s.nzp_stat " +
                  " AND s.nzp_land = l.nzp_land) ";
                  ExecSQL(sql);

                  sql = " UPDATE " + Name + " SET rajon = " +
                  " (SELECT replace(r.rajon,'-','') FROM " + pref + DBManager.sDataAliasRest + " s_land l, " +
                  pref + DBManager.sDataAliasRest + " s_stat s, " +
                  pref + DBManager.sDataAliasRest + " s_town t, " +
                  pref + DBManager.sDataAliasRest + " s_rajon r, " +
                  pref + DBManager.sDataAliasRest + " s_ulica u, " +
                  pref + DBManager.sDataAliasRest + " dom d, " +
                  pref + DBManager.sDataAliasRest + " kvar k " +
                  " WHERE k.nzp_kvar = " + Name + ".nzp_kvar " +
                  " AND k.nzp_dom = d.nzp_dom " +
                  " AND d.nzp_ul = u.nzp_ul " +
                  " AND u.nzp_raj = r.nzp_raj " +
                  " AND r.nzp_town = t.nzp_town " +
                  " AND t.nzp_stat = s.nzp_stat " +
                  " AND s.nzp_land = l.nzp_land) ";
                  ExecSQL(sql);

                  sql = " UPDATE " + Name + " SET town = " +
                  " (SELECT replace(t.town,'-','') FROM " + pref + DBManager.sDataAliasRest + " s_land l, " +
                  pref + DBManager.sDataAliasRest + " s_stat s, " +
                  pref + DBManager.sDataAliasRest + " s_town t, " +
                  pref + DBManager.sDataAliasRest + " s_rajon r, " +
                  pref + DBManager.sDataAliasRest + " s_ulica u, " +
                  pref + DBManager.sDataAliasRest + " dom d, " +
                  pref + DBManager.sDataAliasRest + " kvar k " +
                  " WHERE k.nzp_kvar = " + Name + ".nzp_kvar " +
                  " AND k.nzp_dom = d.nzp_dom " +
                  " AND d.nzp_ul = u.nzp_ul " +
                  " AND u.nzp_raj = r.nzp_raj " +
                  " AND r.nzp_town = t.nzp_town " +
                  " AND t.nzp_stat = s.nzp_stat " +
                  " AND s.nzp_land = l.nzp_land) ";
                  ExecSQL(sql);

            sql = " UPDATE " + Name + " SET ulica = " +
                  " (SELECT replace(u.ulica,'-','') FROM " + pref + DBManager.sDataAliasRest + " s_land l, " +
                  pref + DBManager.sDataAliasRest + " s_stat s, " +
                  pref + DBManager.sDataAliasRest + " s_town t, " +
                  pref + DBManager.sDataAliasRest + " s_rajon r, " +
                  pref + DBManager.sDataAliasRest + " s_ulica u, " +
                  pref + DBManager.sDataAliasRest + " dom d, " +
                  pref + DBManager.sDataAliasRest + " kvar k " +
                  " WHERE k.nzp_kvar = " + Name + ".nzp_kvar " +
                  " AND k.nzp_dom = d.nzp_dom " +
                  " AND d.nzp_ul = u.nzp_ul " +
                  " AND u.nzp_raj = r.nzp_raj " +
                  " AND r.nzp_town = t.nzp_town " +
                  " AND t.nzp_stat = s.nzp_stat " +
                  " AND s.nzp_land = l.nzp_land) ";
                  ExecSQL(sql);

                  sql = " UPDATE " + Name + " SET ndom = " +
                  " (SELECT cast(replace(replace (d.ndom,'-',''),',','|') as CHAR (7)) FROM " + pref + DBManager.sDataAliasRest + " s_land l, " +
                  pref + DBManager.sDataAliasRest + " s_stat s, " +
                  pref + DBManager.sDataAliasRest + " s_town t, " +
                  pref + DBManager.sDataAliasRest + " s_rajon r, " +
                  pref + DBManager.sDataAliasRest + " s_ulica u, " +
                  pref + DBManager.sDataAliasRest + " dom d, " +
                  pref + DBManager.sDataAliasRest + " kvar k " +
                  " WHERE k.nzp_kvar = " + Name + ".nzp_kvar " +
                  " AND k.nzp_dom = d.nzp_dom " +
                  " AND d.nzp_ul = u.nzp_ul " +
                  " AND u.nzp_raj = r.nzp_raj " +
                  " AND r.nzp_town = t.nzp_town " +
                  " AND t.nzp_stat = s.nzp_stat " +
                  " AND s.nzp_land = l.nzp_land) ";
                  ExecSQL(sql);

            sql = " UPDATE " + Name + " SET nkor = " +
                  " (SELECT cast(replace(replace (d.nkor,'-',''),',','|') as CHAR (2)) FROM " + pref + DBManager.sDataAliasRest + " s_land l, " +
                  pref + DBManager.sDataAliasRest + " s_stat s, " +
                  pref + DBManager.sDataAliasRest + " s_town t, " +
                  pref + DBManager.sDataAliasRest + " s_rajon r, " +
                  pref + DBManager.sDataAliasRest + " s_ulica u, " +
                  pref + DBManager.sDataAliasRest + " dom d, " +
                  pref + DBManager.sDataAliasRest + " kvar k " +
                  " WHERE k.nzp_kvar = " + Name + ".nzp_kvar " +
                  " AND k.nzp_dom = d.nzp_dom " +
                  " AND d.nzp_ul = u.nzp_ul " +
                  " AND u.nzp_raj = r.nzp_raj " +
                  " AND r.nzp_town = t.nzp_town " +
                  " AND t.nzp_stat = s.nzp_stat " +
                  " AND s.nzp_land = l.nzp_land) ";
                  ExecSQL(sql);

            sql = " UPDATE " + Name + " SET nkvar = " +
                  " (SELECT cast(replace(replace (k.nkvar,'-',''),',','|') as CHAR (10)) FROM " + pref + DBManager.sDataAliasRest + " s_land l, " +
                  pref + DBManager.sDataAliasRest + " s_stat s, " +
                  pref + DBManager.sDataAliasRest + " s_town t, " +
                  pref + DBManager.sDataAliasRest + " s_rajon r, " +
                  pref + DBManager.sDataAliasRest + " s_ulica u, " +
                  pref + DBManager.sDataAliasRest + " dom d, " +
                  pref + DBManager.sDataAliasRest + " kvar k " +
                  " WHERE k.nzp_kvar = " + Name + ".nzp_kvar " +
                  " AND k.nzp_dom = d.nzp_dom " +
                  " AND d.nzp_ul = u.nzp_ul " +
                  " AND u.nzp_raj = r.nzp_raj " +
                  " AND r.nzp_town = t.nzp_town " +
                  " AND t.nzp_stat = s.nzp_stat " +
                  " AND s.nzp_land = l.nzp_land) ";
                  ExecSQL(sql);

            sql = " UPDATE " + Name + " SET nkvar_n = " +
                  " (SELECT cast(replace(replace (k.nkvar_n,'-',''),',','|') as CHAR (10)) FROM " + pref + DBManager.sDataAliasRest + " s_land l, " +
                  pref + DBManager.sDataAliasRest + " s_stat s, " +
                  pref + DBManager.sDataAliasRest + " s_town t, " +
                  pref + DBManager.sDataAliasRest + " s_rajon r, " +
                  pref + DBManager.sDataAliasRest + " s_ulica u, " +
                  pref + DBManager.sDataAliasRest + " dom d, " +
                  pref + DBManager.sDataAliasRest + " kvar k " +
                  " WHERE k.nzp_kvar = " + Name + ".nzp_kvar " +
                  " AND k.nzp_dom = d.nzp_dom " +
                  " AND d.nzp_ul = u.nzp_ul " +
                  " AND u.nzp_raj = r.nzp_raj " +
                  " AND r.nzp_town = t.nzp_town " +
                  " AND t.nzp_stat = s.nzp_stat " +
                  " AND s.nzp_land = l.nzp_land) ";
                  ExecSQL(sql);






            CheckColumnOnEmptiness("nzp_lnmr", "negative", "отрицательные значения в строке " + NameText + ",в поле - 'Код страны места рождения'");
            CheckColumnOnEmptiness("nzp_stmr", "negative", "отрицательные значения в строке " + NameText + ",в поле - 'Код региона места рождения'");
            CheckColumnOnEmptiness("nzp_lnop", "negative", "отрицательные значения в строке " + NameText + ",в поле - 'Код страны откуда прибыл'");
            CheckColumnOnEmptiness("nzp_stop", "negative", "отрицательные значения в строке " + NameText + ",в поле - 'Код региона откуда прибыл'");
            CheckColumnOnEmptiness("nzp_lnku", "negative", "отрицательные значения в строке " + NameText + ",в поле - 'Код страны куда убыл'");
            CheckColumnOnEmptiness("nzp_stku", "negative", "отрицательные значения в строке " + NameText + ",в поле - 'Код региона куда убыл'");
            CheckColumnOnEmptiness("nzp_tkrt", "negative", "отрицательные значения в строке " + NameText + ",в поле - 'Тип (1 - прибытие, 2 - убытие)'");
            //CheckColumnOnEmptiness("rodstvo", "negative", "отрицательные значения в строке " + NameText + ",в поле - 'Код родственного отношения'");
            CheckColumnOnEmptiness("nzp_dok", "negative", "отрицательные значения в строке " + NameText + ",в поле - 'Код удостоверения личности'");
            CheckColumnOnEmptiness("vid_mes", "null", "null-ы в строке " + NameText + ",в поле - 'Место удостоверения личности'");
            CheckColumnOnEmptiness("nzp_doc_oso", "null", "null-ы в строке " + NameText + ",в поле - 'Код удостоверения личности'", true, "-1");
            CheckColumnOnEmptiness("serij", "null", "null-ы в строке " + NameText + ",в поле - 'Серия удостоверения личности'");
            CheckColumnOnEmptiness("nomer", "null", "null-ы в строке " + NameText + ",в поле - 'Номер удостоверения личности'");
            CheckColumnOnEmptiness("fam", "null", "null-ы в строке " + NameText + ",в поле - 'Фамилия'");
            CheckColumnOnEmptiness("ima", "null", "null-ы в строке " + NameText + ",в поле - 'Имя'");
            CheckColumnOnEmptiness("otch", "null", "null-ы в строке " + NameText + ",в поле - 'Отчество'");

        


            /*
             * нет у нас индексов, и не было :)
            sql = 
                " UPDATE " + Name + " set indecs = " +
                " ( " +
                " SELECT indecs " +
                " FROM " + pref + DBManager.sDataAliasRest + " dom d, " + 
                  pref + DBManager.sDataAliasRest + "kart k " +
                " WHERE d.nzp_raj = k.nzp_rnmr " +
                " AND " +
                " ) ";
            ExecSQL(sql);

            sql =
                " UPDATE " + Name + " set indecs_op = " +
                " (SELECT indecs FROM " + pref + DBManager.sDataAliasRest +
                " dom d, " + pref + DBManager.sDataAliasRest + "kart k " +
                " WHERE d.nzp_raj = k.nzp_lnop) ";
            ExecSQL(sql);

            sql =
                " UPDATE " + Name + " set indecs_ku = " +
                " (SELECT indecs FROM " + pref + DBManager.sDataAliasRest +
                " dom d, " + pref + DBManager.sDataAliasRest + "kart k " +
                " WHERE d.nzp_raj = k.nzp_lnku) ";
            ExecSQL(sql);
            */
        }

        /// <summary>
        /// Запись во временную таблицу
        /// </summary>
        public void WriteInUnlPassport()
        {

        }


    }

}
