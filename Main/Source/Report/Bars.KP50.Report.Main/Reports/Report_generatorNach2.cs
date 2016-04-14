using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using Bars.KP50.Report.Base;
using Bars.KP50.Report.Main.Properties;
using Bars.KP50.Utils;
using Castle.Core.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using Constants = STCLINE.KP50.Global.Constants;

namespace Bars.KP50.Report.Main.Reports
{
    class GenNach2 : BaseSqlReport
    {
        public override string Name
        {
            get { return "Генератор по начислениям и квартирным параметрам"; }
        }

        public override string Description
        {
            get { return "Генератор по начислениям и квартирным параметрам"; }
        }

        public override IList<ReportGroup> ReportGroups
        {
            get
            {
                var result = new List<ReportGroup> { ReportGroup.Finans };
                return result;
            }
        }

        public override bool IsPreview
        {
            get { return false; }
        }

        protected override byte[] Template
        {
            get { return Resources.Gen_nach2; }
        }

        public override IList<ReportKind> ReportKinds
        {
            get { return new List<ReportKind> { ReportKind.Base }; }
        }

        /// <summary>Месяц</summary>
        private int Month { get; set; }

        /// <summary>Год</summary>
        private int Year { get; set; }

        /// <summary>УК</summary>
        private List<long> Areas { get; set; }

        /// <summary>Поставщики</summary>
        private List<long> Suppliers { get; set; }

        /// <summary>Услуги</summary>
        private List<long> Services { get; set; }

        /// <summary>УК</summary>
        private string AreasHeader { get; set; }

        /// <summary>Поставщики</summary>
        private string SuppliersHeader { get; set; }

        /// <summary>Услуги</summary>
        private string ServicesHeader { get; set; }

        /// <summary>Параметры</summary>
        private List<long> Params { get; set; }

        /// <summary>Банки данных</summary>
        private List<int> Banks { get; set; }

        /// <summary>Превышение </summary>
        private bool RowCount { get; set; }

        /// <summary>Адрес</summary>
        protected AddressParameterValue Address { get; set; }

        /// <summary>Улица</summary>
        private string Raions { get; set; }

        /// <summary>Улица</summary>
        private string Streets { get; set; }

        /// <summary>Дом</summary>
        private string Houses { get; set; }



        public override List<UserParam> GetUserParams()
        {
            return new List<UserParam>
            {
                new MonthParameter {Value = DateTime.Today.Month },
                new YearParameter {Value = DateTime.Today.Year },
                new SupplierAndBankParameter(),
                new AddressParameter(),
                new AreaParameter(),
                new ServiceParameter(),
                new ComboBoxParameter(true) {
                    Name = "Параметры отчета", 
                    Code = "Params",
                    Value = 1,
                    Require = true,
                    StoreData = new List<object> {
                        new { Id = 1, Name = "Территория (УК)"},
                        new { Id = 2, Name = "ЖЭУ"},
                        new { Id = 3, Name = "Участок"},
                        new { Id = 4, Name = "Улица"},
                        new { Id = 5, Name = "Дом"},
                        new { Id = 6, Name = "Квартира"},
                        new { Id = 7, Name = "Лицевой счет"},
                        new { Id = 8, Name = "ФИО"},
                        new { Id = 9, Name = "Количество прописаных"},
                        new { Id = 10, Name = "Количество временно проживающих"},
                        new { Id = 11, Name = "Количество временно выбывших"},
                        new { Id = 12, Name = "Общая площадь"},
                        new { Id = 13, Name = "Отапливаемая площадь"},
                        new { Id = 14, Name = "Жилая площадь"},
                        new { Id = 15, Name = "Количество комнат"},
                        new { Id = 16, Name = "Этаж"},
                        new { Id = 17, Name = "Платежный код"},
                        new { Id = 18, Name = "Поставщик"},
                        new { Id = 19, Name = "Услуга"},
                        new { Id = 20, Name = "Срок долга"},
                        new { Id = 21, Name = "Входящее сальдо"},
                        new { Id = 22, Name = "Тариф"},
                        new { Id = 23, Name = "Начислено"},
                        new { Id = 24, Name = "Недопоставка"},
                        new { Id = 25, Name = "Начислено с учетом недопоставки"},
                        new { Id = 26, Name = "Перерасчет"},
                        new { Id = 27, Name = "Перекидка"},
                        new { Id = 28, Name = "К оплате"},
                        new { Id = 29, Name = "Оплачено"},
                        new { Id = 30, Name = "Исходящее сальдо"}
                    }
                },
            };
        }

        protected override void PrepareReport(FastReport.Report report)
        {
            var months = new[] {"","Январь","Февраль",
                 "Март","Апрель","Май","Июнь","Июль","Август","Сентябрь",
                 "Октябрь","Ноябрь","Декабрь"};
            report.SetParameterValue("dat", DateTime.Now.ToLongDateString());
            report.SetParameterValue("time", DateTime.Now.ToLongTimeString());
            report.SetParameterValue("month", months[Month]);
            report.SetParameterValue("year", Year);
            report.SetParameterValue("ar", AreasHeader);
            report.SetParameterValue("su", SuppliersHeader);
            report.SetParameterValue("se", ServicesHeader);

            report.SetParameterValue("excel",
                RowCount
                    ? "Выборка записей ограничена первыми 70000 строками. Выберите другой формат экспортируемого файла, либо поставьте другие ограничения для отчета"
                    : "");
        }


        protected override void PrepareParams()
        {
            Month = UserParamValues["Month"].GetValue<int>();
            Year = UserParamValues["Year"].GetValue<int>();
            Areas = UserParamValues["Areas"].GetValue<List<long>>();
            Services = UserParamValues["Services"].GetValue<List<long>>();
            Params = UserParamValues["Params"].GetValue<List<long>>();
            if (Params.Contains(6))
            {
                if (!Params.Contains(5)) { Params.Add(5); }
                if (!Params.Contains(4)) { Params.Add(4); }
            }
            if (Params.Contains(5))
            {
                if (!Params.Contains(4)) { Params.Add(4); }
            }

            var values = JsonConvert.DeserializeObject<Dictionary<string, object>>(UserParamValues["SupplierAndBank"].GetValue<string>());
            Suppliers = values["Streets"] != null
                ? values["Streets"].To<JArray>().Select(x => x.Value<long>()).ToList()
                : null;
            Banks = values["Raions"] != null
                ? values["Raions"].To<JArray>().Select(x => x.Value<int>()).ToList()
                : null;

            Address = UserParamValues["Address"].GetValue<AddressParameterValue>();
            if (Address.Raions != null)
            {
                Raions = String.Join(",", Address.Raions.Select(x => x.ToString(CultureInfo.InvariantCulture)).ToArray());
                Raions = "and u.nzp_raj in (" + Raions + ") ";
            }
            if (Address.Streets != null)
            {
                Streets = String.Join(",", Address.Streets.Select(x => x.ToString(CultureInfo.InvariantCulture)).ToArray());
                Streets = "and u.nzp_ul in (" + Streets + ") ";
            }
            if (Address.Houses != null)
            {
                string whereStr;
                Houses = !(whereStr = string.Join(", ",
                    (from item in Address.Houses
                    where !item.Trim().IsNullOrEmpty() && !item.Contains("'")
                    select item).ToArray())).IsNullOrEmpty() ? 
                        " and d.nzp_dom in (" + whereStr + ") " :
                        string.Empty;
            }
        }

        public override DataSet GetData()
        {
            var sql = new StringBuilder();
            MyDataReader reader;

            string whereSupp = GetWhereSupp();
            string whereArea = GetWhereArea();
            string whereServ = GetWhereServ();


            CreateTSvod();

            var datS = new DateTime(Year, Month, 1);
            var datPo = datS.AddMonths(1).AddDays(-1);

            sql.Remove(0, sql.Length);
            sql.Append(" select bd_kernel as pref " +
                  " FROM " + ReportParams.Pref + DBManager.sKernelAliasRest + "s_point " +
                  " where nzp_wp>1 " + GetwhereWp());
            ExecRead(out reader, sql.ToString());
            while (reader.Read())
            {
                string pref = reader["pref"].ToStr().Trim();
                string kvarTable = pref + DBManager.sDataAliasRest + "kvar ";
                string domTable = pref + DBManager.sDataAliasRest + "dom ";
                string ulTable = pref + DBManager.sDataAliasRest + "s_ulica ";
                string chargeTable = pref + "_charge_" + (Year - 2000).ToString("00") + DBManager.tableDelimiter + "charge_" + Month.ToString("00");
                string prmTable = pref + DBManager.sDataAliasRest + "prm_1 ";
                if (TempTableInWebCashe(kvarTable) && TempTableInWebCashe(domTable) && TempTableInWebCashe(ulTable) &&
                    TempTableInWebCashe(chargeTable) && TempTableInWebCashe(prmTable))
                {
                    sql.Remove(0, sql.Length);
                    sql.Append(" insert into t_svod_mini( ");
                    if (Params.Contains(1)){sql.Append(" nzp_area, ");}
                    if (Params.Contains(2)){sql.Append(" nzp_geu, ");}
                    if (Params.Contains(3)){sql.Append(" uch, ");}
                    if (Params.Contains(4)){sql.Append(" nzp_raj, nzp_ul, ");}
                    if (Params.Contains(5)){sql.Append(" nzp_dom, ");}
                    sql.Append(" nzp_kvar, ");
                    if (Params.Contains(7)){sql.Append(" num_ls, ");}
                    if (Params.Contains(8)){sql.Append(" fio, ");}
                    if (Params.Contains(17)){sql.Append(" pkod, ");}
                    if (Params.Contains(18)){sql.Append(" nzp_supp, ");}
                    if (Params.Contains(19)){sql.Append(" nzp_serv, ");}
                    if (Params.Contains(20)){sql.Append(" srok_dolg, ");}
                    if (Params.Contains(21)){sql.Append(" sum_insaldo, ");}
                    if (Params.Contains(22)){sql.Append(" tarif, ");}
                    if (Params.Contains(23)){sql.Append(" rsum_tarif, ");}
                    if (Params.Contains(24)){sql.Append(" sum_nedop, ");}
                    if (Params.Contains(25)){sql.Append(" sum_tarif, ");}
                    if (Params.Contains(26)){sql.Append(" reval, ");}
                    if (Params.Contains(27)){sql.Append(" real_charge, ");}
                    if (Params.Contains(28)){sql.Append(" sum_charge, ");}
                    if (Params.Contains(29)){sql.Append(" sum_money, ");}
                    if (Params.Contains(30)){sql.Append(" sum_outsaldo, ");}
                    sql.Remove(sql.Length - 2, 2);
                    sql.Append(") ");
                    sql.Append(" select ");
                    if (Params.Contains(1)){sql.Append(" k.nzp_area, ");}
                    if (Params.Contains(2)){sql.Append(" k.nzp_geu, ");}
                    if (Params.Contains(3)){sql.Append(" uch, ");}
                    if (Params.Contains(4)){sql.Append(" u.nzp_raj, u.nzp_ul, ");}
                    if (Params.Contains(5)){sql.Append(" d.nzp_dom, ");}
                    sql.Append(" k.nzp_kvar, ");
                    if (Params.Contains(7)){sql.Append(" k.num_ls, ");}
                    if (Params.Contains(8)){sql.Append(" k.fio, ");}
                    if (Params.Contains(17)){sql.Append(" pkod, ");}
                    if (Params.Contains(18)){sql.Append(" nzp_supp, ");}
                    if (Params.Contains(19)){sql.Append(" nzp_serv, ");}
                    if (Params.Contains(20)){sql.Append(" (CASE WHEN sum_tarif = 0 THEN 0 ELSE (sum_insaldo - sum_money)/sum_tarif END) AS srok_dolg,");}
                    if (Params.Contains(21)){sql.Append(" sum_insaldo, ");}
                    if (Params.Contains(22)){sql.Append(" tarif, ");}
                    if (Params.Contains(23)){sql.Append(" rsum_tarif, ");}
                    if (Params.Contains(24)){sql.Append(" sum_nedop, ");}
                    if (Params.Contains(25)){sql.Append(" sum_tarif, ");}
                    if (Params.Contains(26)){sql.Append(" reval, ");}
                    if (Params.Contains(27)){sql.Append(" real_charge, ");}
                    if (Params.Contains(28)){sql.Append(" sum_charge, ");}
                    if (Params.Contains(29)){sql.Append(" sum_money, ");}
                    if (Params.Contains(30)){sql.Append(" sum_outsaldo, ");}
                    sql.Remove(sql.Length - 2, 2);

                    sql.Append(" from " + kvarTable + " k, ");
                    sql.Append(chargeTable + " c, ");
                    if (Params.Contains(4)) //если выбрана улица
                    {
                        sql.Append(ulTable + " u, " + domTable + " d, ");
                    }
                    else if (Params.Contains(5)) //если выбран дом
                    {
                        sql.Append(domTable + " d, ");
                    }
                    sql.Remove(sql.Length - 2, 2);
                    sql.Append(" where k.nzp_kvar = c.nzp_kvar and  c.dat_charge is null  ");
                    sql.Append(" and c.nzp_serv > 1 ");
                    sql.Append(whereArea + whereSupp + whereServ);
                    if (Params.Contains(4))
                    {
                        sql.Append(" and k.nzp_dom = d.nzp_dom and d.nzp_ul = u.nzp_ul " + Raions + Streets + Houses);
                    }
                    else if (Params.Contains(5))
                    {
                        sql.Append(" and k.nzp_dom = d.nzp_dom " + Houses);
                    }
                    ExecSQL(sql.ToString());

                    if (Params.Contains(9) || Params.Contains(10) || Params.Contains(11))
                    {
                        sql.Remove(0, sql.Length);
                        sql.Append("  insert into t_g (nzp_kvar, propis_count, gil_prib, gil_ub) " +
                                    "  select nzp as nzp_kvar, " +
                                    "  case when nzp_prm = 5 then val_prm" + DBManager.sConvToInt + " end as propis_count, " +
                                    "  case when nzp_prm = 131 then val_prm" + DBManager.sConvToInt + " end as gil_prib, " +
                                    "  case when nzp_prm = 10 then val_prm" + DBManager.sConvToInt + " end as gil_ub " +
                                    "  from " + prmTable +
                                    "  where nzp_prm in (5,131,10) and is_actual = 1 " +
                                    "  and dat_s <= '" + datPo.ToShortDateString() + "' " +
                                    "  and dat_po >= '" + datS.ToShortDateString() + "' ");
                        ExecSQL(sql.ToString());
                    }

                    if (Params.Contains(11) || Params.Contains(12) || Params.Contains(13) || Params.Contains(14) || Params.Contains(15))
                    {
                        sql.Remove(0, sql.Length);
                        sql.Append("  insert into t_s (nzp_kvar, ob_s, otop_s, gil_s, rooms, floor) " +
                                   "  select nzp as nzp_kvar, " +
                                   "  case when nzp_prm = 4 then p.val_prm"+DBManager.sConvToNum+" end as ob_s, " +
                                   "  case when nzp_prm = 133 then p.val_prm" + DBManager.sConvToNum + " end as otop_s, " +
                                   "  case when nzp_prm = 6 then p.val_prm" + DBManager.sConvToNum + " end as gil_s, " +
                                   "  case when nzp_prm = 107 then p.val_prm" + DBManager.sConvToInt + " end as rooms, " +
                                   "  case when nzp_prm = 2 then p.val_prm" + DBManager.sConvToInt + " end as floor " +
                                   "  from " + prmTable + " p " +
                                   "  where nzp_prm in (4,133,6,107,2) and is_actual = 1 " +
                                   "  and dat_s <= '" + datPo.ToShortDateString() + "' " +
                                   "  and dat_po >= '" + datS.ToShortDateString() + "' ");
                        ExecSQL(sql.ToString());
                    }
                }
            }
            reader.Close();

            ExecSQL(" insert into t_gil_kvar (nzp_kvar, propis_count, gil_prib, gil_ub) " +
                    " select nzp_kvar, max(propis_count), max(gil_prib), max(gil_ub) " +
                    " from t_g group by 1 ");

            ExecSQL(" insert into t_prms (nzp_kvar, ob_s, otop_s, gil_s, rooms, floor) " +
                    " select nzp_kvar, max(ob_s), max(otop_s), max(gil_s), max(rooms), max(floor) " +
                    " from t_s group by 1 ");
            

            string order = FillSvodTable();

            DataTable dt;
            try
            {
                dt = ExecSQLToTable(" select area, geu, uch, rajon, ulica, ndom, nkor, nkvar, num_ls, fio, " +
                    " propis_count, gil_prib, gil_ub, ob_s, otop_s, gil_s, rooms, floor, pkod, name_supp, service, srok_dolg, " +  
                    " sum_insaldo, tarif, rsum_tarif, sum_nedop, sum_tarif, reval, real_charge, sum_charge, sum_money, sum_outsaldo " +
                    " from t_svod_all ");
                var dv = new DataView(dt) { Sort = order };
                dt = dv.ToTable();
                RowCount = false;
            }
            catch (Exception)
            {
                dt = ExecSQLToTable(DBManager.SetLimitOffset(" select area, geu, uch, rajon, ulica, ndom, nkor, nkvar, num_ls, fio, " +
                    " propis_count, gil_prib, gil_ub, ob_s, otop_s, gil_s, rooms, floor, pkod, name_supp, service, srok_dolg, " +  
                    " sum_insaldo, tarif, rsum_tarif, sum_nedop, sum_tarif, reval, real_charge, sum_charge, sum_money, sum_outsaldo " +
                    " from t_svod_all ", 100000, 0));
                var dv = new DataView(dt) { Sort = order };
                dt = dv.ToTable();
                RowCount = true;
            }
            dt.TableName = "Q_master";

            if (dt.Rows.Count >= 100000)
            {
                if (ReportParams.ExportFormat == ExportFormat.Excel2007)
                {
                    var dtr = dt.Rows.Cast<DataRow>().Skip(40000).ToArray();
                    EnumerableExtension.ForEach(dtr, dt.Rows.Remove);
                }
                else
                {
                    var dtr = dt.Rows.Cast<DataRow>().Skip(100000).ToArray();
                    EnumerableExtension.ForEach(dtr, dt.Rows.Remove);
                }
                RowCount = true;
            }
            else
            {
                RowCount = false;
            }

            sql.Remove(0, sql.Length);
            sql.Append(" insert into t_title values( ");
            sql.Append(Params.Contains(1) ? " 'УК' , " : " '' , ");
            sql.Append(Params.Contains(2) ? " 'ЖЭУ' , " : " '' , ");
            sql.Append(Params.Contains(3) ? " 'Участок' , " : " '' , ");
            sql.Append(Params.Contains(4) ? " 'Улица' , " : " '' , ");
            sql.Append(Params.Contains(5) ? " 'Дом' , " : " '' , ");
            sql.Append(Params.Contains(6) ? " 'Квартира' , " : " '' , ");
            sql.Append(Params.Contains(7) ? " 'ЛС' , " : " '' , ");
            sql.Append(Params.Contains(8) ? " 'ФИО' , " : " '' , ");
            sql.Append(Params.Contains(9) ? " 'Жильцов' , " : " '' , ");
            sql.Append(Params.Contains(10) ? " 'Проживающих' , " : " '' , ");
            sql.Append(Params.Contains(11) ? " 'Выбывших' , " : " '' , ");
            sql.Append(Params.Contains(12) ? " 'Общая площадь' , " : " '' , ");
            sql.Append(Params.Contains(13) ? " 'Отапливаемая площадь' , " : " '' , ");
            sql.Append(Params.Contains(14) ? " 'Жилая площадь' , " : " '' , ");
            sql.Append(Params.Contains(15) ? " 'Комнат' , " : " '' , ");
            sql.Append(Params.Contains(16) ? " 'Этаж' , " : " '' , ");
            sql.Append(Params.Contains(17) ? " 'Пл. код' , " : " '' , ");
            sql.Append(Params.Contains(18) ? " 'Поставщик' , " : " '' , ");
            sql.Append(Params.Contains(19) ? " 'Услуга' , " : " '' , ");
            sql.Append(Params.Contains(20) ? " 'Срок долга' , " : " '' , ");
            sql.Append(Params.Contains(21) ? " 'Вх. сальдо' , " : " '' , ");
            sql.Append(Params.Contains(22) ? " 'Тариф' , " : " '' , ");
            sql.Append(Params.Contains(23) ? " 'Начислено' , " : " '' , ");
            sql.Append(Params.Contains(24) ? " 'Недопоставка' , " : " '' , ");
            sql.Append(Params.Contains(25) ? " 'Начислено с учетом недоп-ки' , " : " '' , ");
            sql.Append(Params.Contains(26) ? " 'Перерасчет' , " : " '' , ");
            sql.Append(Params.Contains(27) ? " 'Перекидка' , " : " '' , ");
            sql.Append(Params.Contains(28) ? " 'К оплате' , " : " '' , ");
            sql.Append(Params.Contains(29) ? " 'Оплачено' , " : " '' , ");
            sql.Append(Params.Contains(30) ? " 'Исх. сальдо' , " : " '' , ");
            sql.Remove(sql.Length - 2, 2);
            sql.Append(") ");
            ExecSQL(sql.ToString());
            DataTable dt1 = ExecSQLToTable(" select * from t_title ");
            dt1.TableName = "Q_master1";
            var ds = new DataSet();
            ds.Tables.Add(dt);
            ds.Tables.Add(dt1);
            return ds;

        }


        private string FillSvodTable()
        {
            var sql = new StringBuilder();
            var grouper = new StringBuilder();
            var order = new StringBuilder();

            ExecSQL(" create index mini_index on t_svod_mini(nzp_kvar) ");
            ExecSQL(DBManager.sUpdStat + " t_svod_mini ");

            sql.Remove(0, sql.Length);
            sql.Append(" insert into t_svod_all( ");
            if (Params.Contains(1)) { sql.Append(" nzp_area, "); }
            if (Params.Contains(2)) { sql.Append(" nzp_geu, "); }
            if (Params.Contains(3)) { sql.Append(" uch, "); }
            if (Params.Contains(4)) { sql.Append(" nzp_raj, nzp_ul, "); }
            if (Params.Contains(5)) { sql.Append(" nzp_dom, "); }
            sql.Append(" nzp_kvar, ");
            if (Params.Contains(7)) { sql.Append(" num_ls, "); }
            if (Params.Contains(8)) { sql.Append(" fio, "); }
            if (Params.Contains(17)) { sql.Append(" pkod, "); }
            if (Params.Contains(18)) { sql.Append(" nzp_supp, "); }
            if (Params.Contains(19)) { sql.Append(" nzp_serv, "); }
            if (Params.Contains(20)) { sql.Append(" srok_dolg, "); }
            if (Params.Contains(21)) { sql.Append(" sum_insaldo, "); }
            if (Params.Contains(22)) { sql.Append(" tarif, "); }
            if (Params.Contains(23)) { sql.Append(" rsum_tarif, "); }
            if (Params.Contains(24)) { sql.Append(" sum_nedop, "); }
            if (Params.Contains(25)) { sql.Append(" sum_tarif, "); }
            if (Params.Contains(26)) { sql.Append(" reval, "); }
            if (Params.Contains(27)) { sql.Append(" real_charge, "); }
            if (Params.Contains(28)) { sql.Append(" sum_charge, "); }
            if (Params.Contains(29)) { sql.Append(" sum_money, "); }
            if (Params.Contains(30)) { sql.Append(" sum_outsaldo, "); }
            sql.Remove(sql.Length - 2, 2);
            sql.Append(") ");
            sql.Append(" select ");
            if (Params.Contains(1)) { sql.Append(" nzp_area, "); grouper.Append(" nzp_area, "); order.Append(" area, "); }
            if (Params.Contains(2)) { sql.Append(" nzp_geu, "); grouper.Append(" nzp_geu, "); order.Append(" geu, "); }
            if (Params.Contains(3)) { sql.Append(" uch, "); grouper.Append(" uch, "); order.Append(" uch, "); }
            if (Params.Contains(4)) { sql.Append(" nzp_raj, nzp_ul, "); grouper.Append(" nzp_raj, nzp_ul, "); order.Append(" rajon, ulica, "); }
            if (Params.Contains(5)) { sql.Append(" nzp_dom, "); grouper.Append(" nzp_dom, "); order.Append(" ndom, nkor, "); }
            sql.Append(" m.nzp_kvar, "); grouper.Append(" nzp_kvar, "); order.Append(" nkvar, ");
            if (Params.Contains(7)) { sql.Append(" num_ls, "); grouper.Append(" num_ls, "); order.Append(" num_ls, "); }
            if (Params.Contains(8)) { sql.Append(" fio, "); grouper.Append(" fio, "); order.Append(" fio, "); }
            if (Params.Contains(17)) { sql.Append(" pkod, "); grouper.Append(" pkod, "); order.Append(" pkod, "); }
            if (Params.Contains(18)) { sql.Append(" nzp_supp, "); grouper.Append(" nzp_supp, "); order.Append(" name_supp, "); }
            if (Params.Contains(19)) { sql.Append(" nzp_serv, "); grouper.Append(" nzp_serv, "); order.Append(" service, "); }
            if (Params.Contains(20)) { sql.Append(" srok_dolg, "); grouper.Append(" srok_dolg, "); order.Append(" srok_dolg, "); }
            if (Params.Contains(21)) { sql.Append(" sum(sum_insaldo) as sum_insaldo, "); }
            if (Params.Contains(22)) { sql.Append(" sum(tarif) as tarif, "); }
            if (Params.Contains(23)) { sql.Append(" sum(rsum_tarif) as rsum_tarif, "); }
            if (Params.Contains(24)) { sql.Append(" sum(sum_nedop) as sum_nedop, "); }
            if (Params.Contains(25)) { sql.Append(" sum(sum_tarif) as sum_tarif, "); }
            if (Params.Contains(26)) { sql.Append(" sum(reval) as reval, "); }
            if (Params.Contains(27)) { sql.Append(" sum(real_charge) as real_charge, "); }
            if (Params.Contains(28)) { sql.Append(" sum(sum_charge) as sum_charge, "); }
            if (Params.Contains(29)) { sql.Append(" sum(sum_money) as sum_money, "); }
            if (Params.Contains(30)) { sql.Append(" sum(sum_outsaldo) as sum_outsaldo, "); }
            if (grouper.Length > 0) grouper.Remove(grouper.Length - 2, 2);
            if (order.Length > 0) order.Remove(order.Length - 2, 2);
            sql.Remove(sql.Length - 2, 2);
            sql.Append(" from t_svod_mini m ");
            sql.Append(" group by " + grouper);
            ExecSQL(sql.ToString());

            if (Params.Contains(1))
            {
                sql.Remove(0, sql.Length);
                sql.Append(" update t_svod_all set area = (" +
                           " select area from " + ReportParams.Pref + DBManager.sDataAliasRest + "s_area a " +
                           " where a.nzp_area = t_svod_all.nzp_area) ");
                ExecSQL(sql.ToString());
            }
            if (Params.Contains(2))
            {
                sql.Remove(0, sql.Length);
                sql.Append(" update t_svod_all set geu = (" +
                           " select geu from " + ReportParams.Pref + DBManager.sDataAliasRest + "s_geu a " +
                           " where a.nzp_geu = t_svod_all.nzp_geu) ");
                ExecSQL(sql.ToString());
            }
            if (Params.Contains(4))
            {
                sql.Remove(0, sql.Length);
                sql.Append(" update t_svod_all set " +
                           " rajon = (select rajon from " + ReportParams.Pref + DBManager.sDataAliasRest + "s_rajon a " +
                           " where a.nzp_raj = t_svod_all.nzp_raj), " +
                           " ulica = (select ulica from " + ReportParams.Pref + DBManager.sDataAliasRest + "s_ulica a " +
                           " where a.nzp_ul = t_svod_all.nzp_ul) ");
                ExecSQL(sql.ToString());
            }
            if (Params.Contains(5))
            {
                sql.Remove(0, sql.Length);
                sql.Append(" update t_svod_all set " +
                           " ndom = (select ndom from " + ReportParams.Pref + DBManager.sDataAliasRest + "dom a " +
                           " where a.nzp_dom = t_svod_all.nzp_dom), " +
                           " nkor = (select nkor from " + ReportParams.Pref + DBManager.sDataAliasRest + "dom a " +
                           " where a.nzp_dom = t_svod_all.nzp_dom and nkor<>'-') ");
                ExecSQL(sql.ToString());
            }
            if (Params.Contains(6))
            {
                sql.Remove(0, sql.Length);
                sql.Append(" update t_svod_all set nkvar = (" +
                           " select nkvar from " + ReportParams.Pref + DBManager.sDataAliasRest + "kvar a " +
                           " where a.nzp_kvar = t_svod_all.nzp_kvar) ");
                ExecSQL(sql.ToString());
            }
            if (Params.Contains(18))
            {
                sql.Remove(0, sql.Length);
                sql.Append(" update t_svod_all set name_supp = (" +
                           " select name_supp from " + ReportParams.Pref + DBManager.sKernelAliasRest + "supplier a " +
                           " where a.nzp_supp = t_svod_all.nzp_supp) ");
                ExecSQL(sql.ToString());
            }
            if (Params.Contains(19))
            {
                sql.Remove(0, sql.Length);
                sql.Append(" update t_svod_all set service = (" +
                           " select service from " + ReportParams.Pref + DBManager.sKernelAliasRest + "services a " +
                           " where a.nzp_serv = t_svod_all.nzp_serv) ");
                ExecSQL(sql.ToString());
            }

            ExecSQL(" create index all_index on t_svod_all(nzp_kvar) ");
            ExecSQL(DBManager.sUpdStat + " t_svod_all ");
            ExecSQL(" create index gil_index on t_gil_kvar(nzp_kvar) ");
            ExecSQL(DBManager.sUpdStat + " t_gil_kvar ");
            ExecSQL(" create index prms_index on t_prms(nzp_kvar) ");
            ExecSQL(DBManager.sUpdStat + " t_prms ");

            if (Params.Contains(9))
            {
                sql.Remove(0, sql.Length);
                sql.Append(" update t_svod_all set propis_count = (" +
                           " select propis_count from t_gil_kvar a " +
                           " where a.nzp_kvar = t_svod_all.nzp_kvar) ");
                ExecSQL(sql.ToString());
            }
            if (Params.Contains(10))
            {
                sql.Remove(0, sql.Length);
                sql.Append(" update t_svod_all set gil_prib = (" +
                           " select gil_prib from t_gil_kvar a " +
                           " where a.nzp_kvar = t_svod_all.nzp_kvar) ");
                ExecSQL(sql.ToString());
            }
            if (Params.Contains(11))
            {
                sql.Remove(0, sql.Length);
                sql.Append(" update t_svod_all set gil_ub = (" +
                           " select gil_ub from t_gil_kvar a " +
                           " where a.nzp_kvar = t_svod_all.nzp_kvar) ");
                ExecSQL(sql.ToString());
            }
            if (Params.Contains(12))
            {
                sql.Remove(0, sql.Length);
                sql.Append(" update t_svod_all set ob_s = (" +
                           " select ob_s from t_prms a " +
                           " where a.nzp_kvar = t_svod_all.nzp_kvar) ");
                ExecSQL(sql.ToString());
            }
            if (Params.Contains(13))
            {
                sql.Remove(0, sql.Length);
                sql.Append(" update t_svod_all set otop_s = (" +
                           " select otop_s from t_prms a " +
                           " where a.nzp_kvar = t_svod_all.nzp_kvar) ");
                ExecSQL(sql.ToString());
            }
            if (Params.Contains(14))
            {
                sql.Remove(0, sql.Length);
                sql.Append(" update t_svod_all set gil_s = (" +
                           " select gil_s from t_prms a " +
                           " where a.nzp_kvar = t_svod_all.nzp_kvar) ");
                ExecSQL(sql.ToString());
            }
            if (Params.Contains(15))
            {
                sql.Remove(0, sql.Length);
                sql.Append(" update t_svod_all set rooms = (" +
                           " select rooms from t_prms a " +
                           " where a.nzp_kvar = t_svod_all.nzp_kvar) ");
                ExecSQL(sql.ToString());
            }
            if (Params.Contains(16))
            {
                sql.Remove(0, sql.Length);
                sql.Append(" update t_svod_all set floor = (" +
                           " select floor from t_prms a " +
                           " where a.nzp_kvar = t_svod_all.nzp_kvar) ");
                ExecSQL(sql.ToString());
            }

            return order.ToString();
        }

        /// <summary>
        /// Получить условия органичения по УК
        /// </summary>
        /// <returns></returns>
        private string GetWhereArea()
        {
            string whereArea = String.Empty;
            whereArea = Areas != null ? Areas.Aggregate(whereArea, (current, nzpArea) => current + (nzpArea + ",")) : ReportParams.GetRolesCondition(Constants.role_sql_area);
            whereArea = whereArea.TrimEnd(',');
            whereArea = !String.IsNullOrEmpty(whereArea) ? " AND k.nzp_area in (" + whereArea + ")" : String.Empty;
            if (!String.IsNullOrEmpty(whereArea))
            {
                string sql = " SELECT area from " + ReportParams.Pref + DBManager.sDataAliasRest + "s_area k  WHERE k.nzp_area > 0 " + whereArea;
                DataTable area = ExecSQLToTable(sql);
                foreach (DataRow dr in area.Rows)
                {
                    AreasHeader += dr["area"].ToString().Trim() + ", ";
                }
                AreasHeader = AreasHeader.TrimEnd(',', ' ');
            }
            return whereArea;
        }

        /// <summary>
        /// Получить условия органичения по поставщикам
        /// </summary>
        /// <returns></returns>
        private string GetWhereSupp()
        {
            string whereSupp = String.Empty;
            whereSupp = Suppliers != null ? Suppliers.Aggregate(whereSupp, (current, nzpSupp) => current + (nzpSupp + ",")) : ReportParams.GetRolesCondition(Constants.role_sql_supp);
            whereSupp = whereSupp.TrimEnd(',');
            whereSupp = !String.IsNullOrEmpty(whereSupp) ? " AND nzp_supp in (" + whereSupp + ")" : String.Empty;
            if (!String.IsNullOrEmpty(whereSupp))
            {
                string sql = " SELECT name_supp from " + ReportParams.Pref + DBManager.sKernelAliasRest + "supplier  WHERE nzp_supp > 0 " + whereSupp;
                DataTable supp = ExecSQLToTable(sql);
                foreach (DataRow dr in supp.Rows)
                {
                    SuppliersHeader += dr["name_supp"].ToString().Trim() + ", ";
                }
                SuppliersHeader = SuppliersHeader.TrimEnd(',', ' ');
            }
            return whereSupp;
        }

        /// <summary>
        /// Получить условия органичения по услугам
        /// </summary>
        /// <returns></returns>
        private string GetWhereServ()
        {
            string whereServ = String.Empty;
            whereServ = Services != null ? Services.Aggregate(whereServ, (current, nzpServ) => current + (nzpServ + ",")) : ReportParams.GetRolesCondition(Constants.role_sql_serv);
            whereServ = whereServ.TrimEnd(',');
            whereServ = !String.IsNullOrEmpty(whereServ) ? " AND nzp_serv in (" + whereServ + ")" : String.Empty;
            if (!String.IsNullOrEmpty(whereServ))
            {
                string sql = " SELECT service from " + ReportParams.Pref + DBManager.sKernelAliasRest + "services  WHERE nzp_serv > 0 " + whereServ;
                DataTable serv = ExecSQLToTable(sql);
                foreach (DataRow dr in serv.Rows)
                {
                    ServicesHeader += dr["service"].ToString().Trim() + ", ";
                }
                ServicesHeader = ServicesHeader.TrimEnd(',', ' ');

            }
            return whereServ;
        }

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

        }

        private void CreateTSvod()
        {
            var sql = new StringBuilder();
            sql.Append(" create temp table t_svod_all( ");
            sql.Append(" nzp_area integer, area char(40), "); 
            sql.Append(" nzp_geu integer, geu char(60), "); 
            sql.Append(" uch integer, "); 
            sql.Append(" nzp_raj integer, nzp_ul integer, rajon char(30), ulica char(40), "); 
            sql.Append(" nzp_dom integer, ndom char(15), nkor char(15), "); 
            sql.Append(" nzp_kvar integer, nkvar char(40), "); 
            sql.Append(" num_ls integer, "); 
            sql.Append(" fio character(40), "); 
            sql.Append(" propis_count integer, "); 
            sql.Append(" gil_prib integer, "); 
            sql.Append(" gil_ub integer, "); 
            sql.Append(" ob_s " + DBManager.sDecimalType + "(14,2), "); 
            sql.Append(" otop_s " + DBManager.sDecimalType + "(14,2), "); 
            sql.Append(" gil_s " + DBManager.sDecimalType + "(14,2), "); 
            sql.Append(" rooms integer, "); 
            sql.Append(" floor integer, "); 
            sql.Append(" pkod " + DBManager.sDecimalType + "(13,0), "); 
            sql.Append(" nzp_supp integer, name_supp char(100), "); 
            sql.Append(" nzp_serv integer, service char(100), srok_dolg char(100), "); 
            sql.Append(" sum_insaldo " + DBManager.sDecimalType + "(14,2), "); 
            sql.Append(" tarif " + DBManager.sDecimalType + "(14,2), "); 
            sql.Append(" rsum_tarif " + DBManager.sDecimalType + "(14,2), "); 
            sql.Append(" sum_nedop " + DBManager.sDecimalType + "(14,2), "); 
            sql.Append(" sum_tarif " + DBManager.sDecimalType + "(14,2), "); 
            sql.Append(" reval " + DBManager.sDecimalType + "(14,2), "); 
            sql.Append(" real_charge " + DBManager.sDecimalType + "(14,2), "); 
            sql.Append(" sum_charge " + DBManager.sDecimalType + "(14,2), "); 
            sql.Append(" sum_money " + DBManager.sDecimalType + "(14,2), "); 
            sql.Append(" sum_outsaldo " + DBManager.sDecimalType + "(14,2), "); 
            sql.Remove(sql.Length - 2, 2);
            sql.Append(") " + DBManager.sUnlogTempTable);
            ExecSQL(sql.ToString());

            sql.Remove(0, sql.Length);
            sql.Append(" create temp table t_svod_mini( ");
            if (Params.Contains(1)) { sql.Append(" nzp_area integer, "); }
            if (Params.Contains(2)) { sql.Append(" nzp_geu integer, "); }
            if (Params.Contains(3)) { sql.Append(" uch integer, "); }
            if (Params.Contains(4)) { sql.Append(" nzp_raj integer, nzp_ul integer, "); }
            if (Params.Contains(5)) { sql.Append(" nzp_dom integer, "); }
            sql.Append(" nzp_kvar integer, "); 
            if (Params.Contains(7)) { sql.Append(" num_ls integer, "); }
            if (Params.Contains(8)) { sql.Append(" fio character(40), "); }
            if (Params.Contains(9)) { sql.Append(" propis_count integer, "); }
            if (Params.Contains(10)) { sql.Append(" gil_prib integer, "); }
            if (Params.Contains(11)) { sql.Append(" gil_ub integer, "); }
            if (Params.Contains(12)) { sql.Append(" ob_s " + DBManager.sDecimalType + "(14,2), "); }
            if (Params.Contains(13)) { sql.Append(" otop_s " + DBManager.sDecimalType + "(14,2), "); }
            if (Params.Contains(14)) { sql.Append(" gil_s " + DBManager.sDecimalType + "(14,2), "); }
            if (Params.Contains(15)) { sql.Append(" rooms integer, "); }
            if (Params.Contains(16)) { sql.Append(" floor integer, "); }
            if (Params.Contains(17)) { sql.Append(" pkod " + DBManager.sDecimalType + "(13,0), "); }
            if (Params.Contains(18)) { sql.Append(" nzp_supp integer, "); }
            if (Params.Contains(19)) { sql.Append(" nzp_serv integer, "); }
            if (Params.Contains(20)) { sql.Append(" srok_dolg integer, "); }
            if (Params.Contains(21)) { sql.Append(" sum_insaldo " + DBManager.sDecimalType + "(14,2), "); }
            if (Params.Contains(22)) { sql.Append(" tarif " + DBManager.sDecimalType + "(14,2), "); }
            if (Params.Contains(23)) { sql.Append(" rsum_tarif " + DBManager.sDecimalType + "(14,2), "); }
            if (Params.Contains(24)) { sql.Append(" sum_nedop " + DBManager.sDecimalType + "(14,2), "); }
            if (Params.Contains(25)) { sql.Append(" sum_tarif " + DBManager.sDecimalType + "(14,2), "); }
            if (Params.Contains(26)) { sql.Append(" reval " + DBManager.sDecimalType + "(14,2), "); }
            if (Params.Contains(27)) { sql.Append(" real_charge " + DBManager.sDecimalType + "(14,2), "); }
            if (Params.Contains(28)) { sql.Append(" sum_charge " + DBManager.sDecimalType + "(14,2), "); }
            if (Params.Contains(29)) { sql.Append(" sum_money " + DBManager.sDecimalType + "(14,2), "); }
            if (Params.Contains(30)) { sql.Append(" sum_outsaldo " + DBManager.sDecimalType + "(14,2), "); }
            sql.Remove(sql.Length - 2, 2);
            sql.Append(") " + DBManager.sUnlogTempTable);
            ExecSQL(sql.ToString());


            sql.Remove(0, sql.Length);
            sql.Append(" create temp table t_title( " +
                        " area char(50), " +
                        " geu char(50), " +
                        " uch char(50), " +
                        " rajon char(50), " +
                        " ndom char(50), " +
                        " nkvar char(50), " +
                        " num_ls char(50), " +
                        " fio char(50), " +
                        " propis_count char(50), " +
                        " gil_prib char(50), " +
                        " gil_ub char(50), " +
                        " ob_s char(50), " +
                        " otop_s char(50), " +
                        " gil_s char(50), " +
                        " rooms char(50), " +
                        " floor char(50), " +
                        " pkod char(50), " +
                        " name_supp char(100), " +
                        " service char(50), " +
                        " srok_dolg char(50), " +
                        " sum_insaldo char(50), " +
                        " tarif char(50), " +
                        " rsum_tarif char(50), " +
                        " sum_nedop char(50), " +
                        " sum_tarif char(50), " +
                        " reval char(50), " +
                        " real_charge char(50), " +
                        " sum_charge char(50), " +
                        " sum_money char(50), " +
                        " sum_outsaldo char(50)) " + DBManager.sUnlogTempTable);
            ExecSQL(sql.ToString());


            sql.Remove(0, sql.Length);
            sql.Append(" create temp table t_g (nzp_kvar integer, propis_count integer, gil_prib integer, gil_ub integer)" + DBManager.sUnlogTempTable);
            ExecSQL(sql.ToString());

            sql.Remove(0, sql.Length);
            sql.Append(" create temp table t_gil_kvar (nzp_kvar integer, propis_count integer, gil_prib integer, gil_ub integer)" + DBManager.sUnlogTempTable);
            ExecSQL(sql.ToString());

            sql.Remove(0, sql.Length);
            sql.Append(" create temp table t_s (nzp_kvar integer, " +
                       " ob_s " + DBManager.sDecimalType + "(14,2), " +
                       " otop_s " + DBManager.sDecimalType + "(14,2), " +
                       " gil_s " + DBManager.sDecimalType + "(14,2), " +
                       " rooms " + DBManager.sDecimalType + "(14,2), " +
                       " floor " + DBManager.sDecimalType + "(14,2))" + DBManager.sUnlogTempTable);
            ExecSQL(sql.ToString());

            sql.Remove(0, sql.Length);
            sql.Append(" create temp table t_prms (nzp_kvar integer, " +
                       " ob_s " + DBManager.sDecimalType + "(14,2), " +
                       " otop_s " + DBManager.sDecimalType + "(14,2), " +
                       " gil_s " + DBManager.sDecimalType + "(14,2), " +
                       " rooms " + DBManager.sDecimalType + "(14,2), " +
                       " floor " + DBManager.sDecimalType + "(14,2))" + DBManager.sUnlogTempTable);
            ExecSQL(sql.ToString());
        }

        protected override void DropTempTable()
        {
            try { ExecSQL(" drop table t_svod_all; drop table t_title; drop table t_g; drop table t_gil_kvar; drop table t_s; drop table t_prms; drop table t_svod_mini; "); }
            catch (Exception e)
            {
                MonitorLog.WriteLog("Отчет 'Генератор по начислениям' " + e.Message, MonitorLog.typelog.Error, false);
            }
        }

    }
}
