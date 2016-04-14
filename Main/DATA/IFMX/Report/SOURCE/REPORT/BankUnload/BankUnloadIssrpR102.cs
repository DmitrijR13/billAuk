using System;
using System.Data;
using System.Text;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;

namespace STCLINE.KP50.IFMX.Report.SOURCE.REPORT.BankUnload
{
    /// <summary>
    /// Выгрузка реестра для ИС СРП (R102)
    /// </summary>
    public class BankDownloadReestrIssrpR102 : BankDownloadReestrBaikalskVstkb
    {
        /// <summary>
        /// Создать временную таблицу tmp_reestr для данных реестра
        /// </summary>
        /// <param name="connDb">Соединение</param>
        /// <returns>Returns</returns>
        protected override Returns CreateReestrTempTable(IDbConnection connDb)
        {
            var sqlBuilder = new StringBuilder();

            sqlBuilder.AppendFormat(@"
drop table tmp_reestr;
drop table tmp_counters;

create temp table tmp_counters (
	nzp_kvar {0},
	nzp_serv {0},
	serv_name {1}(250),
	number {1}(250),
    rate {0},
    accuracy {0},
    last_date date,
    val {2}(10)
);

create temp table tmp_reestr (
    pkod {2}(13),
    fio {1}(50),
    adr {1}(50),
    kod_upr {1}(11),
    name_upr {1}(516),
    serv {1}(6000),
    kod_poluch {1}(11),
    insaldo {2}(9,2),
    nachisl {2}(9,2),
    peni {2}(9,2),
    k_oplate {2}(9,2),
    counters {1}(1615),
    close_period {1}(10),
    pref {1}(10)
);
", "integer", DBManager.sCharType, DBManager.sDecimalType);

            var ret = ExecSQL(connDb, sqlBuilder.ToString(), true);
            if (!ret.result)
            {
                ret.text = "Ошибка создания таблицы tmp_reestr\n" + ret.text;
            }

            return ret;
        }

        protected override Returns SetReestrAddress(IDbConnection conn_db, string pref)
        {
            throw new NotImplementedException();
        }

        protected override Returns SetCntsData(IDbConnection conn_db, string pref, DateTime calc_month)
        {
            throw new NotImplementedException();
        }

        protected override Returns UpdateReestrSumCharge(IDbConnection conn_db, string pref, string year, string month)
        {
            throw new NotImplementedException();
        }

        protected override int GetCounterCount()
        {
            return 6;
        }

        /// <summary>
        /// Cформировать строчку для записи в файл
        /// </summary>
        /// <param name="row">DataRow</param>
        /// <returns>string</returns>
        override protected string AssemblyOneString(DataRow row)
        {
            string fullStr = string.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};{10};{11}",
                row["pkod"] != DBNull.Value ? ((Decimal)row["pkod"]).ToString().Trim() : "",
                row["fio"] != DBNull.Value ? ((string)row["fio"]).Trim() : "",
                row["adr"] != DBNull.Value ? ((string)row["adr"]).Trim() : "",
                row["kod_upr"] != DBNull.Value ? ((string)row["kod_upr"]).Trim() : "",
                row["name_upr"] != DBNull.Value ? ((string)row["name_upr"]).Trim() : "",
                row["serv"] != DBNull.Value ? ((string)row["serv"]).Trim() : "",
                row["kod_poluch"] != DBNull.Value ? ((string)row["kod_poluch"]).Trim():"",
                row["insaldo"] != DBNull.Value ? ((Decimal)row["insaldo"]).ToString("0.00") : "0.00",
                row["nachisl"] != DBNull.Value ? ((Decimal)row["nachisl"]).ToString("0.00") : "0.00",
                row["peni"] != DBNull.Value ? ((Decimal)row["peni"]).ToString("0.00") : "0.00",
                row["k_oplate"] != DBNull.Value ? ((Decimal)row["k_oplate"]).ToString("0.00") : "0.00",
                row["counters"] != DBNull.Value ? ((string)row["serv"]).Trim() : ""
            );

            return fullStr;
        }

        /// <summary>
        /// Формирование имени файла реестра
        /// </summary>
        /// <param name="nzpReestr">id реестра</param>
        /// <returns></returns>
        protected override string GetFileName(int nzpReestr)
        {
            var date = DateTime.Now;

            return string.Format("z_{0}_{1}.{2:00}{3}", 0, 0, date.Day, date.Month.ToString("X")); //????????????
        }

        /// <summary>
        /// Добовляет в таблицу tmp_reestr начисления, разделенные по услугам
        /// </summary>
        /// <param name="connDb">Соединение</param>
        /// <param name="pref">Префикс</param>
        /// <param name="date">Дата закрытого месяца</param>
        /// <returns>Returns</returns>
        protected override Returns CreateReestrCharge(IDbConnection connDb, string pref, DateTime date)
        {
            Returns ret = new Returns();
            var sqlBuilder = new StringBuilder();

            string year = (date.Year % 100).ToString("00");
            string month = date.Month.ToString("00");
            var curPeriod = date.AddMonths(1);

            sqlBuilder.AppendFormat(@"
insert into tmp_counters (nzp_kvar, nzp_serv, serv_name, number, rate, accuracy, last_date, val)
select cs.nzp, c.nzp_serv, s.service_small, cs.nzp_counter, ct.cnt_stage, 0, c.dat_uchet, max(c.val_cnt) as val
from {0}{1}counters_spis cs, {0}{1}counters c, tmp_spis_ls tmp, {0}{2}services s, {0}{2}s_counttypes ct
where c.nzp_counter=cs.nzp_counter
and tmp.nzp_kvar = cs.nzp
and cs.nzp_type in (3,4)
and cs.is_actual=1
and cs.dat_close is null
and c.is_actual=1
and c.dat_uchet=(select max(pv.dat_uchet) from {0}{1}counters pv  where pv.nzp_counter = cs.nzp_counter and pv.dat_uchet <= {3} and pv.is_actual = 1)
and c.nzp_serv = s.nzp_serv
and c.nzp_cnttype = ct.nzp_cnttype
group by 1, 2, 3, 4, 5, 6, 7;


insert into tmp_reestr (pkod, fio, adr, kod_upr, name_upr, serv, kod_poluch, insaldo, nachisl, peni, k_oplate, counters, close_period, pref)
select
ls.pkod as pkod,
ls.fio as fio,
trim(coalesce(r.rajon,''))||','||trim(coalesce(u.ulica,''))||','||trim(coalesce(d.ndom,''))||','||replace(trim(coalesce(d.nkor,'')),'-','')||','||trim(coalesce(ls.nkvar,'')) as adr,
ls.nzp_area as kod_upr,
a.area as name_upr,
string_agg(ch.nzp_supp||','||sp.name_supp||','||ch.nzp_serv::text||','||s.service_small, '$') as serv,
ch.nzp_supp as kod_poluch,
sum(ch.sum_insaldo) as insaldo,
sum(ch.sum_tarif) as nachisl,
0 as peni,
sum(ch.sum_outsaldo) as k_oplate,
case
	when count(cnt.*) = 0 then repeat('НЕТ$', ({4}-1)::integer)||'НЕТ'
    else string_agg(cnt.serv_name||','||cnt.number||','||cnt.rate::text||','||cnt.accuracy::text||':'||to_char(cnt.last_date, 'DD/MM/YYYY')||','||cnt.val::text, '$')||repeat('$НЕТ', ({4}-count(cnt.*))::integer)
end,
{8},
{5}

from tmp_spis_ls ls
inner join {0}_charge_{6}.charge_{7} ch on (ch.nzp_kvar=ls.nzp_kvar and ch.dat_charge is null and ch.nzp_serv > 1)
inner join {0}{1}dom d on d.nzp_dom = ls.nzp_dom
inner join {0}{1}s_ulica u on d.nzp_ul = u.nzp_ul
inner join {0}{1}s_rajon r on u.nzp_raj = r.nzp_raj
inner join {0}_kernel.services s on ch.nzp_serv = s.nzp_serv
inner join {0}_kernel.supplier sp on ch.nzp_supp = sp.nzp_supp
inner join {0}{1}s_area a on ls.nzp_area = a.nzp_area
left join tmp_counters cnt on ls.nzp_kvar = cnt.nzp_kvar and ch.nzp_serv = cnt.nzp_serv

group by 1,2,3,4,5,7,10,13,14;
", pref, sDataAliasRest, sKernelAliasRest, Utils.EStrNull(curPeriod.ToShortDateString()), GetCounterCount(), Utils.EStrNull(pref), year, month, Utils.EStrNull(date.ToString("dd/MM/yyyy")));

            ret = ExecSQL(connDb, sqlBuilder.ToString(), true);

            if (!ret.result)
            {
                ret.text = "Ошибка обновления начислений в таблице tmp_reestr" + ret.text;
                return ret;
            }

            return ret;
        }

        protected override string[] GetAdditionLines(IDbConnection connDb)
        {
            MyDataReader goodreader;
            string sumNachisl, sumForPay, sumPeni, closePeriod, countCompany, countAll, countSupp, countLs;
            string sumInsaldo = sumNachisl = sumForPay = sumPeni = closePeriod = countCompany = countAll = countSupp = countLs = "";

            string sql = "select sum(insaldo) as insaldo, sum(nachisl) as nachisl, sum(k_oplate) as k_oplate, sum(peni) as peni from tmp_reestr";
            var ret = DBManager.ExecRead(connDb, out goodreader, sql, true);
            if (ret.result)
            {
                if (goodreader.Read())
                {
                    if (goodreader["insaldo"] != DBNull.Value)
                        sumInsaldo = goodreader["insaldo"].ToString().Trim();
                    if (goodreader["nachisl"] != DBNull.Value)
                        sumNachisl = goodreader["nachisl"].ToString().Trim();
                    if (goodreader["k_oplate"] != DBNull.Value)
                        sumForPay = goodreader["k_oplate"].ToString().Trim();
                    if (goodreader["peni"] != DBNull.Value)
                        sumPeni = goodreader["peni"].ToString().Trim();
                }
            }
            goodreader.Close();

            var data = ClassDBUtils.OpenSQL("select close_period from tmp_reestr limit 1", connDb);
            if (data.resultCode >= 0) 
                closePeriod = (data.resultData.Rows[0]["close_period"] != DBNull.Value ? (data.resultData.Rows[0]["close_period"]).ToString().Trim() : "");

            data = ClassDBUtils.OpenSQL("select kod_upr from tmp_reestr group by 1", connDb);
            if (data.resultCode >= 0)
                countCompany = string.Format("{0}", data.resultData.Rows.Count);

            data = ClassDBUtils.OpenSQL("select count(*) as num from tmp_reestr", connDb);
            if (data.resultCode >= 0) 
                countAll = (data.resultData.Rows[0]["num"] != DBNull.Value ? (data.resultData.Rows[0]["num"]).ToString().Trim() : "");

            data = ClassDBUtils.OpenSQL("select kod_poluch from tmp_reestr group by 1", connDb);
            if (data.resultCode >= 0)
                countSupp = string.Format("{0}", data.resultData.Rows.Count);

            data = ClassDBUtils.OpenSQL("select pkod from tmp_reestr group by 1", connDb);
            if (data.resultCode >= 0)
                countLs = string.Format("{0}", data.resultData.Rows.Count);

            string[] additionLines =
                {
                    string.Format("#{0}", 111), //????? Код ЦН
                    string.Format("#{0}", IdReestr), //Номер реестра
                    string.Format("#{0}", DateTime.Now.ToLongTimeString()), //Дата формирования реестра
                    string.Format("#{0}", closePeriod), //Расчетный период
                    string.Format("#{0}", countAll), //Количество записей
                    string.Format("#{0}", countCompany), //Количество уникальных управляющих компаний
                    string.Format("#{0}", countSupp), //Количество уникальных кодов поставщиков услуг
                    string.Format("#{0}", countLs), //Количество уникальных лицевых счетов
                    string.Format("#{0}", sumInsaldo), //Общая сумма по полю Входящее сальдо расчетного периода
                    string.Format("#{0}", sumNachisl), //Общая сумма по полю Начисления за расчетный период
                    string.Format("#{0}", sumPeni), //Общая сумма по полю Пеня
                    string.Format("#{0}", sumForPay) //Общая сумма по полю Рекомендованная сумма к оплате
                };
            return additionLines;
        }
       
    }

}
