using System;
using System.Data;
using System.Text;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase 
{
    public class CheckRelation : DbAdminClient
    {

        #region Функция проверки связность адресного пространства в нижнем банке
        //связность адресного пространства в нижнем банке
        public Returns Run(IDbConnection conn_db, FilesDisassemble finder)
        {
            StringBuilder err = new StringBuilder();
            Returns ret = Utils.InitReturns();
            try
            {
                #region регион без страны
                CheckOneRelationBank(conn_db,  err, "s_stat", "nzp_stat", "s_land", "nzp_land", "nzp_land", "nzp_land", "nzp_stat", "Код региона", "Код страны", finder.bank.ToString(), "регионы без страны в нижнем банке");
                #endregion

                #region город/район без региона
                CheckOneRelationBank(conn_db,  err, "s_town", "nzp_town", "s_stat", "nzp_stat", "nzp_stat", "nzp_stat", "nzp_town", "Код города/района", "Код региона", finder.bank.ToString(), "города/районы без региона в нижнем банке");
                #endregion

                #region район без города/района
                CheckOneRelationBank(conn_db,  err, "s_rajon", "nzp_raj", "s_town", "nzp_town", "nzp_town", "nzp_town", "nzp_raj", "Код района", "Код города/района", finder.bank.ToString(), "районы без города/района в нижнем банке");
                #endregion

                #region улицы без района
                CheckOneRelationBank(conn_db,  err, "s_ulica", "nzp_ul", "s_rajon", "nzp_raj", "nzp_raj", "nzp_raj", "nzp_ul", "Код улицы", "Код района", finder.bank.ToString(), "улицы без района в нижнем банке");
                #endregion

                #region дома без страны
                CheckOneRelationBank(conn_db,  err, "dom", "nzp_dom", "s_land", "nzp_land", "nzp_land", "nzp_land", "nzp_dom", "Код дома", "Код страны", finder.bank.ToString(), "дома без страны в нижнем банке");
                #endregion

                #region дома без региона
                CheckOneRelationBank(conn_db,  err, "dom", "nzp_dom", "s_stat", "nzp_stat", "nzp_stat", "nzp_stat", "nzp_dom", "Код дома", "Код региона", finder.bank.ToString(), "дома без региона в нижнем банке");
                #endregion

                #region дома без города/района
                CheckOneRelationBank(conn_db,  err, "dom", "nzp_dom", "s_town", "nzp_town", "nzp_town", "nzp_town", "nzp_dom", "Код дома", "Код города/района", finder.bank.ToString(), "дома без города/района в нижнем банке");
                #endregion

                #region дома без района
                CheckOneRelationBank(conn_db,  err, "dom", "nzp_dom", "s_rajon", "nzp_raj", "nzp_raj", "nzp_raj", "nzp_dom", "Код дома", "Код района", finder.bank.ToString(), "дома без района в нижнем банке");
                #endregion

                #region дома без улицы
                CheckOneRelationBank(conn_db,  err, "dom", "nzp_dom", "s_ulica", "nzp_ul", "nzp_ul", "nzp_ul", "nzp_dom", "Код дома", "Код улицы", finder.bank.ToString(), "дома без улицы в нижнем банке");
                #endregion

                #region квартиры без домов
                CheckOneRelationBank(conn_db,  err, "kvar", "nzp_kvar", "dom", "nzp_dom", "nzp_dom", "nzp_dom", "nzp_kvar", "Код квартиры", "Код дома", finder.bank.ToString(), "квартиры без дома в нижнем банке");
                #endregion

                if (err.Length != 0)
                {
                    MonitorLog.WriteLog("Результат проверки адресного пространства в нижнем банке. " + Environment.NewLine + err.ToString(), MonitorLog.typelog.Error, true);

                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка при проверке несвязности таблиц в функции CheckRelationBankPref " + ex.Message + ex.StackTrace, MonitorLog.typelog.Error, true);
            }
            return ret;
        }
        #endregion Функция проверки связность адресного пространства в нижнем банке

        #region Функция проверки связности
        private void CheckOneRelationBank(IDbConnection conn_db, StringBuilder err, string doch_tbl, string doch_field_for_index, string rodit_tbl, string rodit_field_for_index, string doch_field_relation, string rodit_field_relation, string doch_field_log, string field1_name, string feild2_name, string pref, string errMessage)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                string sql;
#if PG
                ret = ExecSQL(conn_db, " SET search_path TO '" + pref.Trim() + "_data'", false);
#else
                ret = ExecSQL(conn_db, "database " + pref.Trim() + "_data", true);
#endif
                #region создаем индексы
                ret = ExecSQL(conn_db, " Create index ix1_" + doch_tbl.Trim() + " on " + pref.Trim() + "_data" + tableDelimiter + "" + doch_tbl.Trim() + " (" + doch_field_for_index.Trim() + ")", false);


                ExecSQL(conn_db, DBManager.sUpdStat + " " + pref.Trim() + DBManager.sDataAliasRest +" " + doch_tbl.Trim(), true);
                
               
                ret = ExecSQL(conn_db, " Create index ix1_" + rodit_tbl.Trim() + " on " + pref.Trim() + "_data" + tableDelimiter + "" + rodit_tbl.Trim() + " (" + rodit_field_for_index.Trim() + ")", false);

                ExecSQL(conn_db, DBManager.sUpdStat + " " + pref.Trim() + DBManager.sDataAliasRest + " " + rodit_tbl.Trim(), true);
     

                #endregion

                #region Сама проверка связности
                //#if PG
                //                   sql = "select count(" + doch_field_for_index.Trim() + ") as kol from " + Points.Pref + "_data." + doch_tbl.Trim() + " a "+
                //                                    "where a.nzp_file ="+ finder.nzp_file + " and "+
                //                                    " not exists (select b." + rodit_field_relation.Trim() + " from " + Points.Pref + "_data." + rodit_tbl.Trim() + " b"+
                //                                    " where b." + rodit_field_relation.Trim() + " = a." + doch_field_relation.Trim() + ")";
                //#else
                //                sql = "select count(" + doch_field_for_index.Trim() + ") as kol from " + Points.Pref + "_data"+tableDelimiter + "" + doch_tbl.Trim() + " a " +
                //                                 "where " +
                //                                 " not exists (select b." + rodit_field_relation.Trim() + " from " + Points.Pref + "_data"+tableDelimiter + "" + rodit_tbl.Trim() + " b" +
                //                                 " where b." + rodit_field_relation.Trim() + " = a." + doch_field_relation.Trim() + ")";
                //#endif
                //                var dt = ClassDBUtils.OpenSQL(sql, conn_db);
                //                if (Convert.ToInt32(dt.resultData.Rows[0]["kol"]) > 0)
                //                {
                //                    err.Append("Обнаружена несвязность данных. Имеются " + errMessage + " в количестве " + Convert.ToInt32(dt.resultData.Rows[0]["kol"]) + "." + Environment.NewLine);
                //                    //ret.text = "Обнаружена несвязность таблиц. Имеются квартиры без домов.";
                //                    //ret.result = false;
                //                    //return ret;
                //                }
                #endregion

                #region Сама проверка связности
                sql = 
                    " select a." + doch_field_log.Trim() + " as field1, a." + doch_field_relation.Trim() + " as field2 " +
                    " from " + pref.Trim() + "_data" + tableDelimiter + "" + doch_tbl.Trim() + " a " +
                    " where " +
                    " not exists (select b." + rodit_field_relation.Trim() + " from " + pref.Trim() + "_data" + tableDelimiter + "" + rodit_tbl.Trim() + " b" +
                    " where b." + rodit_field_relation.Trim() + " = a." + doch_field_relation.Trim() + ")";
                var dt = ClassDBUtils.OpenSQL(sql, conn_db);
                
                if (dt.resultData.Rows.Count > 0)
                {

                    //MonitorLog.WriteLog("Обнаружена несвязность данных. Имеются " + errMessage + " в количестве " + dt.resultData.Rows.Count + ".", MonitorLog.typelog.Error, true);
                    err.Append("Обнаружена несвязность данных. Имеются " + errMessage + " в количестве " + dt.resultData.Rows.Count + "." + Environment.NewLine);
                    //err.Append(String.Format("{0,30}|{1,30}|{2}", field1_name, feild2_name, Environment.NewLine));

                    //foreach (DataRow rr in dt.GetData().Rows)
                    //{
                    //    string testMePls = String.Format("{0,30}|{1,30}|{2}", rr["field1"].ToString().Trim(), rr["field2"].ToString().Trim(), Environment.NewLine);
                    //    err.Append(testMePls);
                    //}
                }
                #endregion

            }
            catch (Exception ex)
            {
                //err.Append("Ошибка при проверке несвязности таблиц в функции CheckRelation " + Environment.NewLine);
                MonitorLog.WriteLog("Ошибка при проверке несвязности таблиц в функции CheckRelation: "+  ex.Message + Environment.NewLine + ex.StackTrace, MonitorLog.typelog.Error, true);
            }
            return;
        }
        #endregion Функция проверки связности
    }
}
