using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Bars.KP50.Report.Samara
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Text;
    using Report;
    using Base;
    using Properties;
    using Utils;

    using FastReport;

    using STCLINE.KP50.DataBase;
    using STCLINE.KP50.Global;
    class Vipis_counters : BaseSqlReport
    {
        public override string Name
        {
            get { return "30.2.1 Выписка из лицевого счета о поданых показаниях ПУ"; }
        }

        public override string Description
        {
            get { return "Выписка из лицевого счета о поданых показаниях приборов учета потребления коммунальных услуг"; }
        }

        public override IList<ReportGroup> ReportGroups
        {
            get
            {
                var result = new List<ReportGroup> { ReportGroup.Cards };
                return result;
            }
        }

        public override bool IsPreview
        {
            get { return false; }
        }

        protected override byte[] Template
        {
            get { return Resources.Vipis_counters; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }

        /// <summary>дата с</summary>
        protected DateTime DatS { get; set; }
        
        /// <summary>дата по</summary>
        protected DateTime DatPo { get; set; }

        /// <summary>Услуга</summary>
        protected int Service { get; set; }

        /// <summary>Поставщики</summary>
        protected List<long> Suppliers { get; set; }

        /// <summary>Банки данных</summary>
        protected List<int> Banks { get; set; }


        public override List<UserParam> GetUserParams()
        {

            DateTime datS =  DateTime.Now;
            DateTime datPo = DateTime.Now;
            return new List<UserParam>
            {
                new PeriodParameter(datS, datPo),
                new SupplierAndBankParameter(),
                new ServiceParameter(false){ Require = true}
            };
        }

        protected override void PrepareReport(Report report)
        {
            report.SetParameterValue("town", "г.о. Жигулевск");
            // report.SetParameterValue("printDate", DateTime.Now.ToLongDateString());
            // report.SetParameterValue("printTime", DateTime.Now.ToLongTimeString());
        }


        protected override void PrepareParams()
        {
            var period = UserParamValues["Period"].GetValue<string>();
            DateTime d1;
            DateTime d2;
            PeriodParameter.GetValues(period, out d1, out d2);
            DatS = d1;
            DatPo = d2;
            Service = UserParamValues["Services"].GetValue<int>();

            var values = JsonConvert.DeserializeObject<Dictionary<string, object>>(UserParamValues["SupplierAndBank"].GetValue<string>());
            Suppliers = values["Streets"] != null
                ? values["Streets"].To<JArray>().Select(x => x.Value<long>()).ToList()
                : null;
            Banks = values["Raions"] != null
                ? values["Raions"].To<JArray>().Select(x => x.Value<int>()).ToList()
                : null;
        }

        public override DataSet GetData()
        {
            var sql = new StringBuilder();
            MyDataReader reader;

            sql.Remove(0, sql.Length);
            sql.Append(" select bd_kernel as pref ");
            sql.AppendFormat(" from {0}{1}s_point ", DBManager.GetFullBaseName(Connection), DBManager.tableDelimiter);
            sql.Append(" where nzp_wp>1 " + GetwhereWp());


            ExecRead(out reader, sql.ToString());
            while (reader.Read())
            {
                string pref = reader["pref"].ToStr().Trim();

                //Добавляем показания приборов учета
                sql.Remove(0, sql.Length);
                sql.Append("INSERT into t_counterss(month_, year_,nzp_cnttype,nzp_counter, " +
                           "       num_cnt, dat_uchet, val_cnt) " +
                           " SELECT " +
#if PG
                           " date_part('month',dat_uchet - integer '1'), date_part('year',dat_uchet - integer '1'), " +
#else
                           " month(dat_uchet - 1 units day ), year(dat_uchet - 1 units day), " +
#endif
                           "        nzp_cnttype,nzp_counter, num_cnt, dat_uchet, val_cnt " +
                           " FROM " + pref + DBManager.sDataAliasRest + "counters  " +
                           " WHERE nzp_kvar=" + ReportParams.NzpObject +
                           "       AND nzp_serv= " + Service +
                           "       AND is_actual=1 ");
                ExecSQL(sql.ToString());

                ExecSQL(" INSERT INTO t1_counters SELECT * FROM t_counterss ");



                sql.Remove(0, sql.Length);
                sql.Append(" DELETE FROM t_counterss " +
                           " WHERE DAT_UCHET<'" + DatS.ToShortDateString() + "'");
                ExecSQL(sql.ToString());

                //Проставляем масштабный множитель
                sql.Remove(0, sql.Length);
                sql.Append(" UPDATE t_counterss set (mmnog, cnt_stage, pu_type)=((" +
                           " SELECT mmnog, cnt_stage, name_type " +
                           " FROM " + pref + DBManager.sKernelAliasRest + "s_counttypes a " +
                           " WHERE t_counterss.nzp_cnttype=a.nzp_cnttype))");
                ExecSQL(sql.ToString());


                sql.Remove(0, sql.Length);
                sql.Append(" UPDATE  t_counterss SET dat_begin=( " +
                           "         SELECT MIN(dat_uchet) " +
                           "         FROM t1_counters " +
                           "         WHERE t_counterss.nzp_counter = t1_counters.nzp_counter)");
                ExecSQL(sql.ToString());

                sql.Remove(0, sql.Length);
                sql.Append(" UPDATE  t_counterss SET val_begin=( " +
                           "         SELECT val_cnt " +
                           "         FROM t1_counters " +
                           "         WHERE t_counterss.nzp_counter = t1_counters.nzp_counter " +
                           "               AND t_counterss.dat_begin = t1_counters.dat_uchet )");
                ExecSQL(sql.ToString());


                //Проставляем дату предыдущего показания
                sql.Remove(0, sql.Length);
                sql.Append(" UPDATE  t_counterss SET dat_uchet_pred=( " +
                           "         SELECT MAX(dat_uchet) " +
                           "         FROM t1_counters " +
                           "         WHERE t_counterss.nzp_counter = t1_counters.nzp_counter" +
                           "               AND t_counterss.dat_uchet > t1_counters.dat_uchet)");
                ExecSQL(sql.ToString());

                //Проставляем предыдущее показание
                sql.Remove(0, sql.Length);
                sql.Append(" UPDATE  t_counterss SET val_cnt_pred=( " +
                           "         SELECT MAX(val_cnt) " +
                           "         FROM t1_counters " +
                           "         WHERE t_counterss.nzp_counter = t1_counters.nzp_counter" +
                           "               AND t_counterss.dat_uchet_pred = t1_counters.dat_uchet)");
                ExecSQL(sql.ToString());

                //Расчитываем расход по счетчикам
                sql.Remove(0, sql.Length);
                sql.Append(" UPDATE  t_counterss SET rashod=(CASE WHEN val_cnt>=nvl(val_cnt_pred,0) THEN val_cnt-nvl(val_cnt_pred,0) " +
                           " ELSE val_cnt-nvl(val_cnt_pred,0) + POW(10,cnt_stage) END) ");
                ExecSQL(sql.ToString());

                //Проставляем дату закрытия счетчика
                sql.Remove(0, sql.Length);
                sql.Append(" UPDATE  t_counterss set dat_close=( " +
                           "        SELECT a.dat_close " +
                           "        FROM " + pref + DBManager.sDataAliasRest + "counters_spis a" +
                           "        WHERE t_counterss.nzp_counter = a.nzp_counter) ");
                ExecSQL(sql.ToString());

                //Проставляем дату открытия счетчика
                sql.Remove(0, sql.Length);
                sql.Append(" UPDATE  t_counterss SET dat_open = ( " +
                           "        SELECT val_prm " +
                           "        FROM " + pref + DBManager.sDataAliasRest + "prm_17 " +
                           "        WHERE t_counterss.nzp_counter=nzp " +
                           "               AND nzp_prm=2025 " +
                           "               AND is_actual=1 " +
                           "               AND dat_s<= '" + DatS.ToShortDateString() + "' " +
                           "               AND dat_po>= '" + DatPo.ToShortDateString() + "' )");
                ExecSQL(sql.ToString());



                for (int i = DatS.Year * 12 + DatS.Month; i <
                   DatPo.Year * 12 + DatPo.Month + 1; i++)
                {
                    int year = i / 12;
                    int month = i % 12;
                    if (month == 0)
                    {
                        year--;
                        month = 1;
                    }

                    if (year >= 2013)
                    {

                        //Добавляем месяца по которым не было показаний счетчиков
                        sql.Remove(0, sql.Length);
                        sql.Append(" INSERT into t_counterss(month_, year_) " +
                                   " VALUES(" + month + "," + year + ")");
                        ExecSQL(sql.ToString());

                        string sChargeTable = pref + "_charge_" + (year - 2000).ToString("00") +
                                DBManager.tableDelimiter + "calc_gku_" + month.ToString("00");

                        sql.Remove(0, sql.Length);
                        //Если до начала действия нашей системы, то по данным сервера
                        if (month + year * 12 < 10 + 2013 * 12)
                        {
                            sql.Append(" UPDATE  t_counterss SET (rashod_sr, rashod_nr, Vnach, Vodn, Vitogo )=(( " +
                                       "         SELECT (CASE WHEN type_rashod =3  THEN rashod ELSE 0 END), " +
                                       "                (CASE WHEN type_rashod IN (0,1) THEN rashod ELSE 0 END), " +
                                       "                (rashod), (rashod_odn), (rashod + rashod_odn) " +
                                       "         FROM " + ReportParams.Pref + DBManager.sDataAliasRest + "a_serverlsserv" + month +
                                       "         WHERE nzp_serv=" + Service +
                                       " AND num_ls=" + ReportParams.NzpObject + "  )) " +
                                       " WHERE month_=" + month + " AND year_=" + year);
                        }
                        else
                        {
                            //С начала действия нашей системы по нашим данным
                            sql.Append(" UPDATE  t_counterss SET (rashod_sr, rashod_nr, Vnach,Vodn,Vitogo )=((" +
                                       "         SELECT (CASE WHEN is_device = 9 THEN valm ELSE 0 END), " +
                                       "                (CASE WHEN is_device = 0 THEN valm ELSE 0 END), " +
                                       "                (valm + dlt_reval), " +
                                       "                (CASE WHEN dop87<0 AND dop87<-valm-dlt_reval AND valm+dlt_reval>=0 then -valm-dlt_reval " +
                                       "                      WHEN valm+dlt_reval<0 THEN 0 ELSE dop87 END), (rashod) " +
                                       "         FROM  " + sChargeTable +
                                       "         WHERE nzp_kvar=" + ReportParams.NzpObject + GetWhereSupp() + " AND nzp_serv=" +
                                       Service + ")) " +
                                       " WHERE month_=" + month + " AND year_=" + year);
                        }
                        ExecSQL(sql.ToString());

                    }
                }

                //Убираем дублирующие записи
                sql.Remove(0, sql.Length);
                sql.Append(" DELETE  t_counterss  " +
                            " WHERE nzp_counter = 0 AND 0<(" +
                            "        SELECT count(*) " +
                            "        FROM t1_counters " +
                            "        WHERE t_counterss.year_=t1_counters.year_ " +
                            "                AND t_counterss.month_=t1_counters.month_) ");
                ExecSQL(sql.ToString());

            }
            reader.Close();
            #region Выборка на экран
            sql.Remove(0, sql.Length);
            sql.Append(" SELECT year_, month_, num_cnt, ");
            sql.Append("        " + DBManager.sNvlWord + "(dat_open" + DBManager.sConvToChar + ",'') as dat_open,");
            sql.Append("        " + DBManager.sNvlWord + "(dat_close" + DBManager.sConvToChar + ",'') as dat_close, ");
            sql.Append("        " + DBManager.sNvlWord + "(val_begin,0) as val_begin, ");
            sql.Append("        " + DBManager.sNvlWord + "(pu_type" + DBManager.sConvToChar + ",'') as pu_type,");
            sql.Append("        " + DBManager.sNvlWord + "(mmnog,1) as mmnog, ");
            sql.Append("        " + DBManager.sNvlWord + "(val_cnt,0) as val_cnt, ");
            sql.Append("        " + DBManager.sNvlWord + "(val_cnt_pred,0) as val_cnt_pred,  ");
            sql.Append("        " + DBManager.sNvlWord + "(rashod,0) as rashod, ");
            sql.Append("        " + DBManager.sNvlWord + "(rashod_nr,0) + ");
            sql.Append("        " + DBManager.sNvlWord + "(rashod_sr,0) as rashod_nrsr, ");
            sql.Append("        " + DBManager.sNvlWord + "(Vnach,0) as Vnach, ");
            sql.Append("        " + DBManager.sNvlWord + "(Vodn,0) as Vodn, ");
            sql.Append("        " + DBManager.sNvlWord + "(VItogo,0) as VItogo ");
            sql.Append(" FROM t_counterss t ");
            sql.Append(" ORDER BY 1,2,3 ");
            #endregion
            DataTable dt = ExecSQLToTable(sql.ToString());
            dt.TableName = "Q_master";
            DataSet ds = new DataSet();
            ds.Tables.Add(dt);
            return ds;
            
        }

        /// <summary>
        /// Получить условия органичения по поставщикам
        /// </summary>
        /// <returns></returns>
        private string GetWhereSupp()
        {
            string whereSupp = String.Empty;
            if (Suppliers != null)
            {
                whereSupp = Suppliers.Aggregate(whereSupp, (current, nzpSupp) => current + (nzpSupp + ","));
            }
            else
            {
                whereSupp = ReportParams.GetRolesCondition(Constants.role_sql_supp);
            }
            whereSupp = whereSupp.TrimEnd(',');
            whereSupp = !String.IsNullOrEmpty(whereSupp) ? " AND nzp_supp in (" + whereSupp + ")" : String.Empty;
            return whereSupp;
        }

        /// <summary>
        /// Получить условия органичения по банкам данных
        /// </summary>
        /// <returns></returns>
        private string GetwhereWp()
        {
            string whereWp = String.Empty;
            if (Banks != null)
            {
                whereWp = Banks.Aggregate(whereWp, (current, nzpWp) => current + (nzpWp + ","));
            }
            else
            {
                whereWp = ReportParams.GetRolesCondition(Constants.role_sql_wp);
            }
            whereWp = whereWp.TrimEnd(',');
            whereWp = !String.IsNullOrEmpty(whereWp) ? " AND nzp_wp in (" + whereWp + ")" : String.Empty;
            return whereWp;
        }

        protected override void CreateTempTable()
        {
            var sql = new StringBuilder();
            sql.Remove(0, sql.Length);
            sql.Append(" create temp table t_counterss (     ");
            sql.Append(" month_ integer default 0,");
            sql.Append(" year_ integer default 0,");
            sql.Append(" nzp_counter integer default 0,");
            sql.Append(" cnt_stage integer default 0,");
            sql.Append(" nzp_cnttype integer default 0,");
            sql.Append(" num_cnt char(20),");
            sql.Append(" mmnog integer default 0,");
            sql.Append(" pu_type char(40),");
            sql.Append(" dat_open char(10),");
            sql.Append(" dat_close char(10),");
            sql.Append(" dat_begin Date,");
            sql.Append(" dat_uchet Date,");
            sql.Append(" dat_uchet_pred Date,");
            sql.Append(" val_begin " + DBManager.sDecimalType + "(14,2) default 0.00, "); //Первое показание счетчика            
            sql.Append(" val_cnt " + DBManager.sDecimalType + "(14,2) default 0.00, "); //Показания счетчика
            sql.Append(" val_cnt_pred " + DBManager.sDecimalType + "(14,2) default 0.00, "); //Предыдущие показания счетчика
            sql.Append(" rashod " + DBManager.sDecimalType + "(14,2) default 0.00, "); //Расход по ПУ
            sql.Append(" rashod_sr " + DBManager.sDecimalType + "(14,2) default 0.00, "); //Расход по среднему
            sql.Append(" rashod_nr " + DBManager.sDecimalType + "(14,2) default 0.00, "); //Расход по нормативу
            sql.Append(" Vnach " + DBManager.sDecimalType + "(14,2) default 0.00, "); //Объем начислено
            sql.Append(" Vodn " + DBManager.sDecimalType + "(14,2) default 0.00, "); //Объем ОДН
            sql.Append(" VItogo " + DBManager.sDecimalType + "(14,2) default 0.00 "); //Итого к начислению
            sql.Append(" ) " + DBManager.sUnlogTempTable);
            ExecSQL(sql.ToString());
            ExecSQL(sql.ToString().Replace("t_counterss","t1_counters"));
        }

        protected override void DropTempTable()
        {
            ExecSQL(" drop table t_counterss; drop table t1_counters ");
        }

    }
}
