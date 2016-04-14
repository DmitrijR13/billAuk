using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase
{
    public class CheckRewriteParams : DbAdminClient
    {
        #region Разбор: Функция проверки перезаписи параметров
        public Returns Run(IDbConnection conn_db, FilesDisassemble finder)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                StringBuilder err = new StringBuilder();

                #region Квартирные параметры
                CheckOneParam(conn_db, finder, err, "file_kvar", "total_square", "prm_1", "4", "nzp_kvar", "nzp", true);
                CheckOneParam(conn_db, finder, err, "file_kvar", "living_square", "prm_1", "6", "nzp_kvar", "nzp", true);
                CheckOneParam(conn_db, finder, err, "file_kvar", "otapl_square", "prm_1", "133", "nzp_kvar", "nzp", true);
                CheckOneParam(conn_db, finder, err, "file_kvar", "kol_gil", "prm_1", "5", "nzp_kvar", "nzp", true);
                CheckOneParam(conn_db, finder, err, "file_kvar", "kol_vrem_prib", "prm_1", "131", "nzp_kvar", "nzp", true);
                CheckOneParam(conn_db, finder, err, "file_kvar", "kol_vrem_ub", "prm_1", "10", "nzp_kvar", "nzp", true);
                CheckOneParam(conn_db, finder, err, "file_kvar", "room_number", "prm_1", "107", "nzp_kvar", "nzp", true);
                CheckOneParam(conn_db, finder, err, "file_kvar", "is_el_plita", "prm_1", "19", "nzp_kvar", "nzp", true);
                CheckOneParam(conn_db, finder, err, "file_kvar", "is_gas_plita", "prm_1", "551", "nzp_kvar", "nzp", true);
                CheckOneParam(conn_db, finder, err, "file_kvar", "is_gas_colonka", "prm_1", "1", "nzp_kvar", "nzp", true);
                #endregion

                #region Домовые параметры
                CheckOneParam(conn_db, finder, err, "file_dom", "etazh", "prm_2", "37", "nzp_dom", "nzp", true);
                CheckOneParam(conn_db, finder, err, "file_dom", "build_year", "prm_2", "150", "nzp_dom", "nzp", false);
                CheckOneParam(conn_db, finder, err, "file_dom", "total_square", "prm_2", "40", "nzp_dom", "nzp", true);
                CheckOneParam(conn_db, finder, err, "file_dom", "mop_square", "prm_2", "2049", "nzp_dom", "nzp", true);
                CheckOneParam(conn_db, finder, err, "file_dom", "useful_square", "prm_2", "36", "nzp_dom", "nzp", true);
                #endregion


                if (err.Length != 0)
                {
                    MonitorLog.WriteLog("Результат перезаписи квартирных и лицевых параметров при разборе для файла с nzp_file = " + finder.nzp_file + Environment.NewLine + err.ToString(), MonitorLog.typelog.Error, true);

                }
            }
            catch
            {
                MonitorLog.WriteLog("Ошибка при проверке перезаписи одного параметра в функции CheckOneParam " + Environment.NewLine, MonitorLog.typelog.Error, true);
            }
            return ret;
        }
        #endregion Функция проверки перезаписи параметров

        #region Разбор: Функция проверки перезаписи одного параметра
        private Returns CheckOneParam(IDbConnection conn_db, FilesDisassemble finder, StringBuilder err, string file_table, string file_prm_name, string prm_table, string nzp_prm, string file_rel, string prm_rel, bool is_number)
        {
            Returns ret = Utils.InitReturns();
            try
            {
                string sql;


                #region Сама проверка связности
                if (is_number)
                {
                    sql = "select count (id) from " + Points.Pref + DBManager.sUploadAliasRest + file_table + " k where nzp_file = " + finder.nzp_file +
                        " and " + file_prm_name + "> 0  and not exists" +
                        "(select nzp_key from " + finder.bank + "_data" + tableDelimiter + prm_table + " p where k." + file_rel + " = p." + prm_rel + " and p.user_del = " + finder.nzp_file + " and p.nzp_prm = " + nzp_prm +
                        " )";
                }
                else
                {
                    sql = "select count (id) from " + Points.Pref + DBManager.sUploadAliasRest + file_table + " k where nzp_file = " + finder.nzp_file +
                        " and " + file_prm_name + " is not null  and not exists " +
                        "(select nzp_key from " + finder.bank + "_data" + tableDelimiter + prm_table + " p where k." + file_rel + " = p." + prm_rel + " and p.user_del = " + finder.nzp_file + " and p.nzp_prm = " + nzp_prm +
                        " )";
                }
                #endregion

                int count = Convert.ToInt32(ExecScalar(conn_db, null, sql, out ret, true, 3600));

                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка при проверке перезаписи одного параметра в функции CheckOneParam: " + ret.text + Environment.NewLine, MonitorLog.typelog.Error, true);
                    //err.Append("Ошибка при проверке перезаписи одного параметра в функции CheckOneParam: " + ret.text + Environment.NewLine);
                }
                else
                {
                    if (count > 0)
                    {
                        MonitorLog.WriteLog("Ошибка в файле с номером " + finder.nzp_file + " при проверке перезаписи параметра " + nzp_prm + " " + file_prm_name +
                            " в " + prm_table + ", количество незаписанных строк " + count + Environment.NewLine, MonitorLog.typelog.Error, true);
                        //err.Append("Ошибка в файле с номером " + finder.nzp_file + " при проверке перезаписи параметра " + nzp_prm + " " + file_prm_name +
                            //" в " + prm_table + ", количество незаписанных строк " + count + Environment.NewLine);
                    }
                }
            }
            catch
            {
                MonitorLog.WriteLog("Ошибка при проверке перезаписи одного параметра в функции CheckOneParam " + Environment.NewLine, MonitorLog.typelog.Error, true);
                //err.Append("Ошибка при проверке перезаписи одного параметра в функции CheckOneParam " + Environment.NewLine);
            }
            return ret;
        }
        #endregion Функция проверки перезаписи одного параметра
    }
}
