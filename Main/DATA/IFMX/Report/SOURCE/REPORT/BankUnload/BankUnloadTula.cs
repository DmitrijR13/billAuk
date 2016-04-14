using System;
using System.Collections.Generic;
using System.Text;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System.Data;
using System.Linq;
using System.IO;

namespace STCLINE.KP50.DataBase
{
    /// <summary>
    /// ВЫГРУЗКА в Сбербанк для Тулы
    /// </summary>
    public abstract class BankDownloadReestrVersion2 : BaseBankUnloadReestr
    {
        /// <summary>
        /// Создать временную таблицу tmp_reestr для данных реестра
        /// </summary>
        /// <param name="conn_db">Соединение</param>
        /// <returns>Returns</returns>
        override protected Returns CreateReestrTempTable(IDbConnection conn_db)
        {
            Returns ret = new Returns();

            ExecSQL(conn_db, "drop table tmp_reestr", false);

            StringBuilder sql = new StringBuilder();

            sql.Append("create temp table tmp_reestr (");
            sql.Append(" nzp_kvar integer, ");
            sql.Append(" pkod " + DBManager.sDecimalType + "(13), ");
            sql.Append(" adr char(100), ");
            sql.Append(" sum_charge " + DBManager.sDecimalType + "(14,2), ");
            sql.Append(" kod_dop integer ");

            for (int i = 1; i <= 6; i++)
            {
                sql.Append(", cnt" + i + " char(100), " + " val_cnt" + i + " " + DBManager.sDecimalType + " (14,2) ");
            }

            sql.Append(", pref char(10)) ");

            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result) throw new Exception(ret.text);

            return ret;
        }

        /// <summary>
        /// Записать адреса во временную таблицу tmp_reestr реестра
        /// </summary>
        /// <param name="conn_db">Соединение</param>
        /// <returns>Returns</returns>
        override protected Returns SetReestrAddress(IDbConnection conn_db, string pref)
        {
            StringBuilder sql = new StringBuilder();

            sql.Append(" insert into  tmp_reestr (nzp_kvar,pkod, adr,kod_dop,pref) ");
            sql.Append(" select k.nzp_kvar,k.pkod,  ");
            sql.Append(" trim(a.town)||' '||(case when trim(" + sNvlWord + "(g.rajon,''))='-' then ' ' else trim(" + sNvlWord + "(g.rajon,'')) end) ");
            sql.Append("||' '||trim(" + sNvlWord + "(u.ulicareg,''))||' '||trim(" + sNvlWord + "(u.ulica,''))||");
            sql.Append("' д.'||trim(" + sNvlWord + "(d.ndom,''))||' '||(case when trim(" + sNvlWord + "(d.nkor,''))='-' then '' " +
                       " when trim(" + sNvlWord + "(d.nkor,''))='' then ''  else 'корп.'||trim(" + sNvlWord + "(d.nkor,'')) end) ");
            sql.Append("||' кв.' || trim(" + sNvlWord + "(k.nkvar, '')) as adr, " + GetAddCode() + " as kod_dop ");
            sql.Append(" ," + Utils.EStrNull(pref));
            sql.Append(" from  tmp_spis_ls k ");
            sql.Append(" left outer join " + pref + sDataAliasRest + "dom d     on k.nzp_dom = d.nzp_dom ");
            sql.Append(" left outer join " + Points.Pref + sDataAliasRest + "s_ulica u on d.nzp_ul  = u.nzp_ul ");
            sql.Append(" left outer join " + Points.Pref + sDataAliasRest + "s_rajon g on u.nzp_raj  = g.nzp_raj ");
            sql.Append(" left outer join " + Points.Pref + sDataAliasRest + "s_town a  on g.nzp_town = a.nzp_town ");
            sql.Append(" where k.pref = " + Utils.EStrNull(pref));

            Returns ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result) throw new Exception("Ошибка записи в таблице tmp_reestr\n" + ret.text);

            return ret;
        }

        /// <summary>
        /// Получить доп. код из формата
        /// </summary>
        /// <returns>string</returns>
        abstract protected string GetAddCode();

        /// <summary>
        /// Получить имя файла
        /// </summary>
        /// <param name="nzp_reestr">Код реестра</param>
        /// <returns>string</returns>
        override protected string GetFileName(int nzp_reestr)
        {
            return GetAddCode() + "." + nzp_reestr.ToString("000");
        }

        /// <summary>
        /// Записать данные по ПУ во временную таблицу tmp_cnts
        /// </summary>
        /// <param name="conn_db">Соединение</param>
        /// <param name="pref">Префикс</param>
        /// <param name="dat_uchet">Дата учета</param>
        /// <returns>Returns</returns>
        override protected Returns SetCntsData(IDbConnection conn_db, string pref, DateTime calc_month)
        {
            Returns ret = new Returns();

            StringBuilder sql = new StringBuilder();
            sql.Append(" insert into tmp_cnts(nzp, nzp_serv, nzp_counter, cnt, val_cnt) ");
            sql.Append(" select cs.nzp, cs.nzp_serv, cs.nzp_counter, ");//sql.Append(" trim(" + DBManager.sNvlWord + "(cs.comment,'')) || '(' || trim(" + DBManager.sNvlWord + "(cs.num_cnt,'')) || ')' || '@' || cs.nzp_counter as cnt, "); //comment(num_cnt)@nzp_counter  

            //Услуга, номер счетчика@код счетчика
            sql.Append(" case when cs.nzp_serv = 6 then 'х/в' when cs.nzp_serv = 9 then 'г/в' when cs.nzp_serv = 8 then 'Отопл.' " +
                       " when cs.nzp_serv = 25 then 'Эл.снаб.' when cs.nzp_serv = 210 then 'Ноч.Эл.' " +
                       " else (select substring(service_name for 20) from " +
                       Points.Pref + "_kernel" + tableDelimiter + "services where nzp_serv = cs.nzp_serv) end " +
                       " || ', ' " +
                //"|| (select substring(name_y for 40) from " + pref + "_kernel" + tableDelimiter + "res_y " +
                //" where nzp_y::char = (select max(trim(val_prm)) from " + pref + "_data" + tableDelimiter +
                //"prm_17 where nzp_prm = 974 and is_actual<>100 and " +
                //Utils.EStrNull(calc_month.ToShortDateString()) + " between dat_s and dat_po) " +
                //" and nzp_res = 9990) || ', '" +
                       " || trim(coalesce(cs.num_cnt,'')) || '@' || cs.nzp_counter as cnt,");
            sql.Append(" max(c.val_cnt) ");
            sql.Append(" from " + pref + "_data" + DBManager.tableDelimiter + "counters_spis cs, " + pref + "_data" + DBManager.tableDelimiter + "counters c, tmp_reestr tmp ");
            sql.Append(" where c.nzp_counter=cs.nzp_counter and tmp.nzp_kvar = cs.nzp and tmp.pref = " + Utils.EStrNull(pref));
            sql.Append(" and cs.nzp_type in (3,4) and cs.is_actual=1 and cs.dat_close is null and c.is_actual=1 ");
            sql.Append(" and c.dat_uchet=(select max(pv.dat_uchet) ");
            sql.Append(" from " + pref + "_data" + DBManager.tableDelimiter + "counters pv where pv.nzp_counter = cs.nzp_counter  ");
            sql.Append(" and pv.dat_uchet <= " + Utils.EStrNull(calc_month.ToShortDateString()) + " and pv.is_actual = 1)");
            sql.Append(" group by 1, 2, 3, 4  ");

            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result) throw new Exception("Ошибка записи счетчиков в таблицу tmp_cnts" + ret.text);
            
            return ret;
        }

        /// <summary>
        /// Получить количество ПУ в выгрузке
        /// </summary>
        /// <returns>int</returns>
        override protected int GetCounterCount()
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
            string s = (row["pkod"] != DBNull.Value ? ((Decimal)row["pkod"]).ToString("0").Trim() + ";" : "НЕТ;") +
                (row["adr"] != DBNull.Value ? ((string)row["adr"]).Trim().Replace(";", "").Replace("\"", " ").Replace("\'", " ").Replace("  ", " ") + ";" : "НЕТ;") +
                (row["sum_charge"] != DBNull.Value ? ((Decimal)row["sum_charge"]).ToString("0.00") + ";" : "0.00;") +
                (row["kod_dop"] != DBNull.Value ? ((Int32)row["kod_dop"]).ToString("0") : "НЕТ");

            for (int i = 1; i <= 6; i++)
            {
                s += "#" + (row["cnt" + i] != DBNull.Value ? ((string)row["cnt" + i]).Trim().Replace("\"", " ").Replace("\'", " ").Replace("  ", " ") : "НЕТ");
                s += "#" + (row["val_cnt" + i] != DBNull.Value ? ((Decimal)row["val_cnt" + i]).ToString("").Trim() : (row["cnt" + i] != DBNull.Value ? "0.00" : "НЕТ"));
            }

            return s;
        }

        /// <summary>
        /// Обновить начисления в таблице tmp_reestr. Берется sum_charge
        /// </summary>
        /// <param name="conn_db">Соединение</param>
        /// <param name="pref">Префикс</param>
        /// <param name="year">Год</param>
        /// <param name="month">Месяц</param>
        /// <returns>Returns</returns>
        override protected Returns UpdateReestrSumCharge(IDbConnection conn_db, string pref, string year, string month)
        {
            string errorMessage = "Ошибка обновления начислений в таблице tmp_reestr.";
            
            ExecSQL(conn_db, "drop table tmp_charge", false);

            string sql = String.Concat(" create temp table tmp_charge (",
                    " nzp_kvar integer, ",
                    " sum_charge ", DBManager.sDecimalType, "(15,2))");
            Returns ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result) throw new Exception(errorMessage + ret.text);

            sql = " insert into tmp_charge (nzp_kvar, sum_charge) " +
                " select nzp_kvar, sum(ch.sum_charge) from " + pref + "_charge_" + year + DBManager.tableDelimiter + "charge_" + month + " ch " +
                "   where ch.dat_charge is null " +
                "       and ch.nzp_serv > 1 " +
                " group by 1";
            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result) throw new Exception(errorMessage + ret.text);

            sql = "create index ix_tmp_charge_1 on tmp_charge(nzp_kvar)";
            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result) throw new Exception(errorMessage + ret.text);

            sql = DBManager.sUpdStat + " tmp_charge";
            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result) throw new Exception(errorMessage + ret.text);

            sql = " update tmp_reestr a set " +
                " sum_charge = (select b.sum_charge from tmp_charge b where a.nzp_kvar = b.nzp_kvar) " +
                " where a.pref = " + Utils.EStrNull(pref);
            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result) throw new Exception(errorMessage + ret.text);

            return ret;
        }
    }

    public class BankDownloadReestrVersion21 : BankDownloadReestrVersion2
    {
        protected override string GetAddCode()
        {
            return "86040111";
        }
    }

    public class BankDownloadReestrVersion22 : BankDownloadReestrVersion2
    {
        protected override string GetAddCode()
        {
            return "86040167";
        }
    }
}
