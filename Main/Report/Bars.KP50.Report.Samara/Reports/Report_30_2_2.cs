using System;
using System.Collections.Generic;
using System.Data;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.Samara.Properties;
using Bars.KP50.Utils;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;

namespace Bars.KP50.Report.Samara.Reports
{
    class Report3022 : BaseSqlReport
    {

        public override string Name
        {
            get { return "30.2.2 Справка о составе семьи"; }
        }

        public override string Description
        {
            get { return "30.2.2 Справка о составе семьи"; }
        }

        public override IList<ReportGroup> ReportGroups
        {
            get
            {
                List<ReportGroup> result = new List<ReportGroup> { ReportGroup.Cards };
                return result;
            }
        }

        public override bool IsPreview
        {
            get { return false; }
        }

        protected override byte[] Template
        {
            get { return Resources.Report_30_2_2; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Person }; }
        }


        private static string _totalKom, _numKom, _gpl, _obPl;

        /// <summary>Лицевой счет</summary>
        protected string PersonalAccount { get; set; }

        /// <summary>ФИО</summary>
        protected string FIO { get; set; }

        /// <summary>Дата рождения</summary>
        protected string DatRoj { get; set; }

        /// <summary>Дата регистрации</summary>
        protected string DatReg { get; set; }

        /// <summary>Адрес</summary>
        protected string Address { get; set; }

        /// <summary>Район</summary>
        protected string Rajon { get; set; }

        /// <summary>Общая площадь</summary>
        protected string ObPl {
            get { return _obPl; }
            set { _obPl = string.IsNullOrEmpty(value) ? "___" : value; }
        }

        /// <summary>Жилая площадь</summary>
        protected string GPl {
            get { return _gpl; }
            set { _gpl = string.IsNullOrEmpty(value) ? "___" : value; }
        }

        /// <summary>Статус</summary>
        protected string Status { get; set; }

        /// <summary>Кол-во занимаемых комнат</summary>
        protected string NumKom {
            get { return _numKom; }
            set { _numKom = string.IsNullOrEmpty(value) ? "___" : value; }
        }

        /// <summary>Кол-во комнат всего</summary>
        protected string TotalKom {
            get { return _totalKom; }
            set { _totalKom = string.IsNullOrEmpty(value) ? "___" : value; }
        }

        /// <summary>Примечание</summary>
        protected string Primech { get; set; }


        public override List<UserParam> GetUserParams()
        {
            return new List<UserParam>();
        }

        protected override void PrepareParams() { }

        protected override void PrepareReport(FastReport.Report report)
        {
            var months = new[] {"","Января","Февраля",
                 "Марта","Апреля","Мая","Июня","Июля","Августа","Сентября",
                 "Октября","Ноября","Декабря"};

            report.SetParameterValue("day", DateTime.Now.Day);
            report.SetParameterValue("month",months[DateTime.Now.Month]);
            report.SetParameterValue("year", DateTime.Now.Year);
            report.SetParameterValue("pers_acc", PersonalAccount);
            report.SetParameterValue("fio", FIO);
            report.SetParameterValue("dat_rog", DatRoj);
            report.SetParameterValue("dat_reg", DatReg);
            report.SetParameterValue("address", Address);
            report.SetParameterValue("rajon", Rajon);
            report.SetParameterValue("ob_pl", ObPl);
            report.SetParameterValue("gpl", GPl);
            report.SetParameterValue("status", Status);
            report.SetParameterValue("num_kom", NumKom);
            report.SetParameterValue("total_kom", TotalKom);
            report.SetParameterValue("primech", Primech);
        }

        public override DataSet GetData()
        {
            IDataReader reader;

            #region выборка в temp таблицу

            string sql = " SELECT bd_kernel as pref " +
                         " FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point " +
                         " WHERE nzp_wp>1 " + GetwhereWp();
            ExecRead(out reader, sql);
            while (reader.Read())
            {
                string pref = reader["pref"].ToStr().Trim();

                string prefData = pref + DBManager.sDataAliasRest;
                string dateNow = DateTime.Now.ToShortDateString();

                sql = " INSERT INTO t_sprav_sost_sem_kv(fio, dat_rog, dat_ofor, num_ls, town, rajon, ulica, ndom, nkor, nkvar, nzp_kvar) " +
                      " SELECT TRIM(fam) ||' '||TRIM(ima)||' '||TRIM(otch) AS fio, " +
                             " dat_rog," +
                             " dat_ofor," +
                             " num_ls, " +
                             " town, " +
                             " rajon, " +
                             " ulica, " +
                             " ndom, " +
                             " nkor, " +
                             " nkvar," +
                             " k.nzp_kvar " +
                      " FROM  " + prefData + "kvar kv INNER JOIN " + prefData + "dom d ON kv.nzp_dom = d.nzp_dom " +
                                                    " INNER JOIN " + prefData + "s_ulica u ON d.nzp_ul = u.nzp_ul " +
                                                    " INNER JOIN " + prefData + "s_rajon r ON u.nzp_raj = r.nzp_raj " +
                                                    " INNER JOIN " + prefData + "s_town t ON r.nzp_town = t.nzp_town " +
                                                    " INNER JOIN " + prefData + "kart k ON k.nzp_kvar = kv.nzp_kvar " +
                      " WHERE nzp_kart = " + ReportParams.NzpObject + " ";
                ExecSQL(sql);

                sql = " INSERT INTO t_sprav_sost_sem_kart(nzp_kart, nzp_kvar, fam, ima, otch, town_rog, dat_rog, type_rod, dat_ofor, type_reg) " +
                       " SELECT k.nzp_kart, " +
                              " k.nzp_kvar, " +
                              " k.fam, " +
                              " k.ima, " +
                              " k.otch, " +
                              " t.town AS town_rog, " +
                              " k.dat_rog, " +
                              " k.rodstvo AS type_rod, " +
                              " k.dat_ofor, " +
                              " k.tprp AS type_reg " +
                       " FROM " + prefData + "kart k LEFT OUTER JOIN " + prefData + "s_town t ON k.nzp_tnmr=t.nzp_town " +
                                                   " INNER JOIN t_sprav_sost_sem_kv tt ON tt.nzp_kvar = k.nzp_kvar " +
                       " WHERE isactual = 1 ";
                ExecSQL(sql);



                sql = " UPDATE t_sprav_sost_sem_kv " +
                      " SET (gpl) = ((SELECT val_prm " +
                                    " FROM  " + prefData + "prm_1 " +
                                    " WHERE nzp_prm = 4 " +
                                      " AND is_actual<>100  " +
                                      " AND nzp = t_sprav_sost_sem_kv.nzp_kvar " +
                                      " AND dat_s <='" + dateNow + "' " +
                                      " AND dat_po>='" + dateNow + "')) ";
                ExecSQL(sql);

                sql = " UPDATE t_sprav_sost_sem_kv " +
                      " SET (obpl) = ((SELECT val_prm " +
                                     " FROM  " + prefData + "prm_1 " +
                                     " WHERE nzp_prm = 6 " +
                                       " AND is_actual<>100  " +
                                       " AND nzp = t_sprav_sost_sem_kv.nzp_kvar " +
                                       " AND dat_s <='" + dateNow + "' " +
                                       " AND dat_po>='" + dateNow + "')) ";
                ExecSQL(sql);

                sql = " UPDATE t_sprav_sost_sem_kv " +
                      " SET (sum_kom) = ((SELECT val_prm " +
                                        " FROM  " + prefData + "prm_1 " +
                                        " WHERE nzp_prm = 107 " +
                                          " AND is_actual<>100 " +
                                          " AND nzp = t_sprav_sost_sem_kv.nzp_kvar " +
                                          " AND dat_s <='" + dateNow + "' " +
                                          " AND dat_po>='" + dateNow + "')) ";
                ExecSQL(sql);

                sql = " UPDATE t_sprav_sost_sem_kv " +
                      " SET (total_kom) = ((SELECT SUM(CAST (val_prm AS INTEGER)) " +
                                          " FROM  " + prefData + "prm_1 " +
                                          " WHERE nzp_prm = 107 " +
                                            " AND is_actual<>100 " +
                                            " AND nzp IN (SELECT nzp_kvar " +
                                                        " FROM " + prefData + "kvar " +
                                                        " WHERE nzp_dom = t_sprav_sost_sem_kv.nzp_dom " +
                                                          " AND nkvar = t_sprav_sost_sem_kv.nkvar " +
                                                          " AND nkvar <> '-') " +
                                            " AND dat_s <='" + dateNow + "' " +
                                            " AND dat_po>='" + dateNow + "'))";
                ExecSQL(sql);

                sql = " UPDATE t_sprav_sost_sem_kv " +
                      " SET (privat) = ((SELECT val_prm " +
                                       " FROM  " + prefData + "prm_1 " +
                                       " WHERE nzp_prm = 8 " +
                                         " AND is_actual<>100 " +
                                         " AND nzp = t_sprav_sost_sem_kv.nzp_kvar " +
                                         " AND dat_s <='" + dateNow + "' " +
                                         " AND dat_po>='" + dateNow + "'))";
                ExecSQL(sql);

                sql = " UPDATE t_sprav_sost_sem_kv " +
                      " SET (primech) = ((SELECT val_prm " +
                                        " FROM  " + prefData + "prm_18 " +
                                        " WHERE nzp_prm = 2012 " +
                                          " AND is_actual<>100 " +
                                          " AND nzp = t_sprav_sost_sem_kv.nzp_kvar " +
                                          " AND dat_s <='" + dateNow + "' " +
                                          " AND dat_po>='" + dateNow + "')) ";
                ExecSQL(sql);

            }
            reader.Close();

            #endregion

            sql = " SELECT num_ls, TRIM(fio) AS fio, " +
                         " dat_rog," +
                         " dat_ofor ," +
                         " (CASE WHEN TRIM(rajon)='-' THEN TRIM(town) ELSE TRIM(rajon) END) || ', ул.' || TRIM(ulica) || ', д.' || TRIM(ndom) || " +
                         " (CASE WHEN TRIM(nkor)='-' THEN '' ELSE ', корп.' || TRIM(nkor) END) || " +
                         " (CASE WHEN TRIM(nkvar)='-' THEN '' ELSE ', кв.' || TRIM(nkvar) END) AS address, " +
                         " TRIM(rajon) AS rajon, privat, sum_kom, total_kom, gpl, obpl, primech " +
                  " FROM t_sprav_sost_sem_kv ";
            DataTable head = ExecSQLToTable(sql);
            if (head.Rows.Count > 0)
            {
                PersonalAccount = head.Rows[0]["num_ls"].ToString();
                FIO = head.Rows[0]["fio"].ToString();
                DatRoj = !string.IsNullOrEmpty(head.Rows[0]["dat_rog"].ToString())
                                                ? head.Rows[0]["dat_rog"].ToDateTime().ToShortDateString()
                                                : string.Empty;
                DatReg = !string.IsNullOrEmpty(head.Rows[0]["dat_ofor"].ToString())
                                                ? head.Rows[0]["dat_ofor"].ToDateTime().ToShortDateString()
                                                : string.Empty;
                Address = head.Rows[0]["address"].ToString();
                Rajon = head.Rows[0]["rajon"].ToString();
                Status = head.Rows[0]["privat"].ToString()=="1" ? "приватизированa" : "не приватизированa";
                NumKom = head.Rows[0]["sum_kom"].ToString();
                TotalKom = head.Rows[0]["total_kom"].ToString();
                GPl = head.Rows[0]["gpl"].ToString();
                ObPl = head.Rows[0]["obpl"].ToString();
                Primech = head.Rows[0]["primech"].ToString();
            }
            else
            {
                ObPl = string.Empty;
                GPl = string.Empty;
                NumKom = string.Empty;
                TotalKom = string.Empty;
            }

            sql = " SELECT TRIM(fam) || ' ' || TRIM(ima) || ' ' || TRIM(otch) AS fio, " +
                         " dat_rog, " +
                         " TRIM(town_rog) AS town_rog, " +
                         " TRIM(type_rod) AS type_rod, " +
                         " (CASE type_reg WHEN 'П' THEN 'Постоянная' " +
                                        " WHEN 'В' THEN 'Временная' END) AS type_reg, " +
                         " dat_ofor " +
                  " FROM t_sprav_sost_sem_kart ";

            DataTable dt = ExecSQLToTable(sql);
            dt.TableName = "Q_master";
            var ds = new DataSet();
            ds.Tables.Add(dt);
            return ds;
        }

        private string GetwhereWp()
        {
            var result = ReportParams.GetRolesCondition(Constants.role_sql_wp);
            return !String.IsNullOrEmpty(result) ? " and nzp_wp in (" + result + ") " : "";
        }

        protected override void CreateTempTable()
        {
            string sql = " CREATE TEMP TABLE t_sprav_sost_sem_kart( " +
                         " nzp_kart INTEGER, " +
                         " nzp_kvar INTEGER, " +
                         " fam CHARACTER(40), " +
                         " ima CHARACTER(40), " +
                         " otch CHARACTER(40), " +
                         " dat_rog DATE, " +
                         " town_rog CHARACTER(30), " +
                         " type_rod CHARACTER(30), " +
                         " type_reg CHARACTER(1), " +
                         " dat_ofor DATE) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);

            sql = " CREATE TEMP TABLE t_sprav_sost_sem_kv  ( " +
                  " nzp_kvar INTEGER, " +
                  " nzp_dom INTEGER, " +
                  " num_ls INTEGER, " +
                  " fio CHARACTER(40), " +
                  " dat_rog DATE, " +
                  " dat_ofor DATE, " +
                  " town CHARACTER(30), " +
                  " rajon CHARACTER(30), " +
                  " ulica CHARACTER(40), " +
                  " ndom CHARACTER(10), " +
                  " nkor CHARACTER(3), " +
                  " nkvar CHARACTER(10), " +
                  " gpl CHARACTER(20), " +
                  " obpl CHARACTER(20), " +
                  " privat CHARACTER(20), " +
                  " sum_kom CHARACTER(20), " +
                  " total_kom CHARACTER(20), " +
                  " primech CHARACTER(250)) " + DBManager.sUnlogTempTable;
            ExecSQL(sql);
        }

        protected override void DropTempTable()
        {
            ExecSQL(" DROP TABLE t_sprav_sost_sem_kart ");
            ExecSQL(" DROP TABLE t_sprav_sost_sem_kv ");
        }
    }
}
