using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.IFMX.Report.SOURCE.REPORT.BankUnload
{
    /// <summary>
    /// Выгрузка реестра для ИС СРП (F102)
    /// </summary>
    public class BankDownloadReestrIssrpF102 : BankDownloadReestrIssrpR102
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

create temp table tmp_reestr (
    pkod {2}(13),
    fio {1}(50),
    adr {1}(50),
    kod_upr {1}(11),
    kod_post {1}(11),
    serv {1}(11),
    serv_priority {0},
    payment_type {0},
    kod_poluch {1}(11),
    kod_banking_poluch {1}(11),
    insaldo {2}(9,2),
    nachisl {2}(9,2),
    peni {2}(9,2),
    k_oplate {2}(9,2),
    payment_purpose {1}(92),
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
            return 0;
        }

        /// <summary>
        /// Cформировать строчку для записи в файл
        /// </summary>
        /// <param name="row">DataRow</param>
        /// <returns>string</returns>
        override protected string AssemblyOneString(DataRow row)
        {
            string fullStr = string.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};{10};{11};{12};{13};{14}",
                row["pkod"] != DBNull.Value ? ((Decimal)row["pkod"]).ToString().Trim() : "",
                row["fio"] != DBNull.Value ? ((string)row["fio"]).Trim() : "",
                row["adr"] != DBNull.Value ? ((string)row["adr"]).Trim() : "",
                row["kod_upr"] != DBNull.Value ? ((string)row["kod_upr"]).Trim() : "",
                row["kod_post"] != DBNull.Value ? ((string)row["kod_post"]).Trim() : "",
                row["serv"] != DBNull.Value ? ((string)row["serv"]).Trim() : "",
                row["serv_priority"] != DBNull.Value ? ((Int32)row["serv_priority"]).ToString().Trim() : "",
                row["payment_type"] != DBNull.Value ? ((Int32)row["payment_type"]).ToString().Trim() : "",
                row["kod_poluch"] != DBNull.Value ? ((string)row["kod_poluch"]).Trim():"",
                row["kod_banking_poluch"] != DBNull.Value ? ((string)row["kod_banking_poluch"]).Trim() : "",
                row["insaldo"] != DBNull.Value ? ((Decimal)row["insaldo"]).ToString("0.00") : "0.00",
                row["nachisl"] != DBNull.Value ? ((Decimal)row["nachisl"]).ToString("0.00") : "0.00",
                row["peni"] != DBNull.Value ? ((Decimal)row["peni"]).ToString("0.00") : "0.00",
                row["k_oplate"] != DBNull.Value ? ((Decimal)row["k_oplate"]).ToString("0.00") : "0.00",
                row["payment_purpose"] != DBNull.Value ? ((string)row["serv"]).Trim() : ""
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

            return string.Format("reestr-accruals-{0}{1:00}{2:00}.csv", date.Year, date.Month, date.Day);
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
            var sqlBuilder = new StringBuilder();

            string year = (date.Year % 100).ToString("00");
            string month = date.Month.ToString("00");

            sqlBuilder.AppendFormat(@"
insert into tmp_reestr (pkod, fio, adr, kod_upr, kod_post, serv, serv_priority, payment_type, kod_poluch, kod_banking_poluch, insaldo, nachisl, peni, k_oplate, payment_purpose, close_period, pref)
select
ls.pkod as pkod,
ls.fio as fio,
trim(coalesce(r.rajon,''))||','||trim(coalesce(u.ulica,''))||','||trim(coalesce(d.ndom,''))||','||replace(trim(coalesce(d.nkor,'')),'-','')||','||trim(coalesce(ls.nkvar,'')) as adr,
ls.nzp_area as kod_upr,
sp.nzp_payer_supp as kod_post,
ch.nzp_serv::text as serv,
1 as serv_priority,
1 as payment_type,
ch.nzp_supp as kod_poluch,
coalesce(fb.nzp_fb,0)::text as kod_banking_poluch,
ch.sum_insaldo as insaldo,
ch.sum_tarif as nachisl,
0 as peni,
ch.sum_outsaldo as k_oplate,
'Платежи населения по договору № от ' as payment_purpose,
{5},
{2}

from tmp_spis_ls ls
inner join {0}_charge_{3:00}.charge_{4:0} ch on (ch.nzp_kvar=ls.nzp_kvar and ch.dat_charge is null and ch.nzp_serv > 1)
inner join {0}{1}dom d on d.nzp_dom = ls.nzp_dom
inner join {0}{1}s_ulica u on d.nzp_ul = u.nzp_ul
inner join {0}{1}s_rajon r on u.nzp_raj = r.nzp_raj
inner join {0}_kernel.supplier sp on ch.nzp_supp = sp.nzp_supp
inner join {6}{1}fn_bank fb on sp.nzp_payer_supp = fb.nzp_payer
;
", pref, sDataAliasRest, Utils.EStrNull(pref), year, month, Utils.EStrNull(date.ToString("dd/MM/yyyy")), Points.Pref);

            Returns ret = ExecSQL(connDb, sqlBuilder.ToString(), true);

            if (!ret.result)
            {
                ret.text = "Ошибка обновления начислений в таблице tmp_reestr" + ret.text;
                return ret;
            }

            return ret;
        }

        protected override string[] GetAdditionLines(IDbConnection connDb)
        {
            string[] additionLines = base.GetAdditionLines(connDb);
            
            // Первая строка нам не нужна
            additionLines = additionLines.Where(x => x != additionLines[0]).ToArray();
 
            return additionLines;
        }

        public void RunUploadDebts(string reestrData)
        {
//            var fileData = reestrData;
            var dcId = 111;
            var rDate = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");
            var token = Utils.CreateMD5StringHash(dcId + rDate + "testsecretword");

            try
            {
                using (WebClient client = new WebClient())
                {
                    System.Collections.Specialized.NameValueCollection reqparm = new System.Collections.Specialized.NameValueCollection();
                    reqparm.Add("DC_ID", dcId.ToString());
                    reqparm.Add("FILE_DATA", reestrData);
                    reqparm.Add("R_DATE", rDate);
                    reqparm.Add("TOKEN", token);
                    byte[] responsebytes = client.UploadValues("http://10.2.10.96/webapi/UploadDebts", "POST", reqparm);
                    string responsebody = Encoding.UTF8.GetString(responsebytes);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка в функции RunUploadDebts:\n" + ex.Message, MonitorLog.typelog.Error, true);
            }
        }

        protected override void SendReestr(IDbConnection connDb, string filename)
        {
            string path = Path.Combine(Constants.ExcelDir, filename);
            var fstream = new FileStream(path, FileMode.Open, FileAccess.Read);
            byte[] buffer = new byte[fstream.Length];
            fstream.Position = 0;
            fstream.Read(buffer, 0, buffer.Length);
            fstream.Close();

            string reestrFileString = Encoding.GetEncoding(1251).GetString(buffer);
//            string[] stSplit = { Environment.NewLine };
//            string[] reestrStrings = reestrFileString.Split(stSplit, StringSplitOptions.RemoveEmptyEntries);

            RunUploadDebts(reestrFileString);
        }
    }

}
