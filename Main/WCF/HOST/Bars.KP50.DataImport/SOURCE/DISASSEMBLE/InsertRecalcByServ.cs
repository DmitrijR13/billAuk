
using System;
using System.Data;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.DataImport.SOURCE.DISASSEMBLE
{
    class InsertRecalcByServ : DbAdminClient
    {
        /// <summary>
        /// Функция разбора 8ой секции Перерасчетов по начислениям
        /// </summary>
        /// <param name="conDb"></param>
        /// <param name="finder"></param>
        /// <returns></returns>
        public Returns DisassRecalcByServ(IDbConnection conDb, FilesDisassemble finder)
        {
            Returns ret = new Returns(true);
            string sql;
            string calc_date = "";
            string year;
            string month;
           
            MonitorLog.WriteLog("Старт разбора 8й секции: Перерасчеты начислений по услугам", MonitorLog.typelog.Info, true);

            // получаем дату начислений 
            sql =
                " SELECT calc_date as calc_date " +
                " FROM " + Points.Pref + DBManager.sUploadAliasRest + " file_head " +
                " WHERE nzp_file = " + finder.nzp_file;
            DataTable dt = ClassDBUtils.OpenSQL(sql, conDb, ClassDBUtils.ExecMode.Exception).GetData();
            
            foreach (DataRow rr in dt.Rows)
            {
                calc_date = Convert.ToString(rr["calc_date"]);
            }
            
            sql =
                " SELECT fk.nzp_kvar, fs.nzp_serv, fd.nzp_supp, " +
                "fsp.dog_id, fsp.reval_month, fsp.ls_id,  " +
                "fsp.supp_id, fsp.eot, fsp.reg_tarif_percent, "  +
                "fsp.reg_tarif, fsp.nzp_measure, fsp.fact_rashod, " +  
                "fsp.norm_rashod, fsp.is_pu_calc, fsp.sum_reval, " +  
                "fsp.sum_subsidyp, fsp.sum_lgotap, fsp.sum_smop " +
                " FROM " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar fk, " +
                Points.Pref + DBManager.sUploadAliasRest + "file_services fs, " +
                Points.Pref + DBManager.sUploadAliasRest + "file_dog fd, " +
                Points.Pref + DBManager.sUploadAliasRest + "file_servp fsp " +
                " WHERE fsp.ls_id = fk.id " +
                " AND fsp.nzp_serv = fs.id_serv " +
                " AND fsp.dog_id = fd.dog_id " +
                " AND fsp.nzp_file = " + finder.nzp_file +
                " AND fk.nzp_file = " + finder.nzp_file +
                " AND fs.nzp_file = " + finder.nzp_file +
                " AND fd.nzp_file = " + finder.nzp_file;               

            DataTable dtrec = ClassDBUtils.OpenSQL( sql, conDb, ClassDBUtils.ExecMode.Exception).GetData();
            foreach (DataRow rr in dtrec.Rows)
            {
                year = rr["reval_month"].ToString().Substring(8, 2);
                month = rr["reval_month"].ToString().Substring(3, 2);

                //проверяем существует ли charge_XX.charge_xx (где XX - год перерасчета, xx - месяц перерасчета)
                sql =
                    " SELECT * " +
                    " FROM information_schema.tables " +
                    " WHERE table_schema = '" + finder.bank + "_charge_" + year + "'" +
                    " AND  table_name = 'charge_" + month + "'";
                DataTable dt_sch = ClassDBUtils.OpenSQL(sql, conDb, ClassDBUtils.ExecMode.Exception).GetData();
                if (dt_sch.Rows.Count == 0 )
                {
                    MonitorLog.WriteLog("Схемы " + finder.bank + "_charge_" + year + " не существует в базе", MonitorLog.typelog.Error, true);
                    return new Returns(false, "Схемы по данному месяцу перерасчета не существует в базе", -1);
                }

                sql = "INSERT INTO " + finder.bank + "_charge_" + year +                               
                      tableDelimiter + "charge_" + month +
                      "( nzp_kvar, num_ls, nzp_serv," +
                      " nzp_supp, nzp_frm, dat_charge," +
                      " tarif, tarif_p, rsum_tarif," +
                      " gsum_tarif, rsum_lgota, sum_tarif," +
                      "  sum_dlt_tarif, sum_dlt_tarif_p, sum_tarif_p," +
                      " sum_lgota, sum_dlt_lgota, sum_dlt_lgota_p," +
                      " sum_lgota_p, sum_nedop, sum_nedop_p, " +
                      " sum_real, sum_charge, reval," +
                      " real_pere, sum_pere, real_charge," +
                      " sum_money, money_to, money_from," +
                      " money_del, sum_fakt, fakt_to," +
                      " fakt_from, fakt_del, sum_insaldo," +
                      " izm_saldo, sum_outsaldo, isblocked," +
                      " is_device, c_calc, c_sn," +
                      " c_okaz, c_nedop, isdel," +
                      " c_reval, reval_tarif, reval_lgota," +
                      " tarif_f, sum_tarif_eot, sum_tarif_sn_eot," +
                      " sum_tarif_sn_f, rsum_subsidy, sum_subsidy," +
                      " sum_subsidy_p,  sum_subsidy_reval, sum_subsidy_all," +
                      " sum_lgota_eot, sum_lgota_f, sum_smo," +
                      " tarif_f_p, sum_tarif_eot_p, sum_tarif_sn_eot_p," +
                      " sum_tarif_sn_f_p, sum_lgota_eot_p, sum_lgota_f_p," +
                      " sum_smo_p, sum_tarif_f, sum_tarif_f_p, " +
                      " order_print  )" +

                      " VALUES" +
                      " (" + Convert.ToString(rr["nzp_kvar"]) + ", " + Convert.ToString(rr["nzp_kvar"]) + ", " + Convert.ToString(rr["nzp_serv"]) + "," +
                      " " + Convert.ToString(rr["nzp_supp"]) + ", 0, cast('" + calc_date + "'as date), " +
                      " " + Convert.ToString(rr["eot"]) + ", 0, 0, " +
                      " 0, 0, 0, " +
                      " 0, 0, 0, " +
                      " 0, 0, 0, " +
                      " " + Convert.ToString(rr["sum_lgotap"]) + ", 0, 0, " +
                      " 0, 0, 0, " +
                      " 0, 0, " + Convert.ToString(rr["sum_reval"]) + "," +
                      " 0, 0, 0, " +
                      " 0, 0, 0, " +
                      " 0, 0, 0, " +
                      " 0, 0, 0, " +
                      " " + Convert.ToString(rr["is_pu_calc"]) + ", " + Convert.ToString(rr["fact_rashod"]) + ", " + Convert.ToString(rr["norm_rashod"]) + ", " +
                      " 0, 0, 0, " +
                      " 0, 0, 0, " +
                      " " + Convert.ToString(rr["reg_tarif"]) + ", 0, 0, " +
                      " 0, 0, 0, " +
                      " " + Convert.ToString(rr["sum_subsidyp"]) + ", 0, 0, " +
                      " 0, 0, 0, " +
                      " 0, 0, 0, " +
                      " 0, 0, 0, " +
                      " " + Convert.ToString(rr["sum_smop"]) + ", 0, 0, " +
                      finder.nzp_file +" )";

                ret = ExecSQL(conDb, sql, true);
            }
            
            
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка разбора 8й секции 'Перерасчеты начислений по услугам' ", MonitorLog.typelog.Error, true);
                return new Returns(false, "Ошибка разбора 8й секции 'Перерасчеты начислений по услугам'", -1);
            }
            else
            {
                MonitorLog.WriteLog("Успешно завершен разбор перерасчетов начислений (ф-ция DisassRecalcByServ)", MonitorLog.typelog.Info, true);
            }
            return ret;
        }
    }
}
