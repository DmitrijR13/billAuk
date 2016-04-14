using System.Linq;
using Bars.KP50.Faktura.Source.Base;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;


namespace STCLINE.KP50.DataBase
{

    /// <summary>
    /// Распределение одного ЛС
    /// </summary>
    public class FakturaWebTask : BaseFonTask
    {
        /// <summary>
        /// Таблица session_ls с полным путем к ней
        /// </summary>
        private string strWBFDatabase;

        private IDbConnection conDB;

        public override Returns StartTask()
        {
            conDB = GetConnection(Constants.cons_Kernel);
            Returns ret = OpenDb(conDB, true);
            try
            {
                strWBFDatabase = GetStrWbfDatabase();
                
                Faktura finder = GetFakturaFinder();

                var dbFaktura = new DbFaktura();
                List<string> fName = dbFaktura.GetFaktura(finder, out ret);

                SetCodeForWebService(ret.result, ret.text, fName);

                ret.tag = 0;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetFakturaWeb : " + ex,
                    MonitorLog.typelog.Error, true);
                ret.result = false;
                ret.tag = -1;
            }
            finally
            {
                conDB.Close();
            }

            return ret;
        }

        /// <summary>
        /// Установка штрих-кодов для вебсервисов
        /// </summary>
        /// <param name="shtrihCode">Штрих-код из ЕПД</param>
        /// <param name="fName">Имя файла сформированного ЕПД</param>
        /// <param name="result">Результат формирования ЕПД</param>
        /// <returns></returns>
        private void SetCodeForWebService(bool result, string shtrihCode,  List<string> fName)
        {
            string erc12 = result ? shtrihCode.Split('|')[0] : "";
            string erc28 = result ? shtrihCode.Split('|')[1] : "";

            string strSQLQuery =
                "UPDATE " + strWBFDatabase + " " +
                " SET cur_oper = , " + (result ? "34" : "-5") +
                " erc12 = '" + erc12 + "', " +
                " erc28 = '" + erc28 + "', " +
                " pdf_filename = '" + (Path.GetFileName(fName.FirstOrDefault()) ?? "") + "' " +
                " WHERE nzp_session = " + _task.nzpt;
            Returns rets = ExecSQL(conDB, strSQLQuery, true);
            if (!rets.result) throw new Exception();
        }


        /// <summary>
        /// Получить набор начальных условий для формирования счета на оплату ЖКУ
        /// </summary>
        /// <returns></returns>
        private Faktura GetFakturaFinder()
        {
            MyDataReader reader;

            string strSQLQuery = " SELECT a.pref AS wbf_pref, a.id_bill AS wbf_id_bill, a.dat_when AS wbf_dat_when," +
                                 " a.cur_oper AS wbf_cur_oper, k.nzp_kvar AS dat_nzp_kvar," +
                                 " k.nzp_dom AS dat_nzp_dom, k.pkod AS dat_pkod," +
                                 " s.kind AS krn_kind, 301 AS sys_nzp_user " +
                                 " FROM " + strWBFDatabase + " a," +
                                 Points.Pref + sDataAliasRest + "kvar k, " +
                                 Points.Pref + sKernelAliasRest + "s_listfactura s " +
                                 " WHERE a.nzp_session = " + _task.nzpt +
                                 " AND a.num_ls = k.num_ls " +
                                 " AND s.default_ = 1 ";
            Returns ret = ExecRead(conDB, out reader, strSQLQuery, true);
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка выполнения запроса", MonitorLog.typelog.Error, true);
                ret.text = " Ошибка выполнения запроса. ";
                ret.tag = -1;
                throw new Exception();
            }

            string strWBFPref = "";
            int intWBFIdBill = 0;
            DateTime dtWBFDateWhen = new DateTime();
            int intWBFCurOper = 0;
            int intDATNzpKvar = 0;
            int intDATNzpDom = 0;
            long longDATPkod = 0;
            int intKRNKind = 0;
            int intSYSNzpUser = 0;

            Utils.setCulture();
            if (reader.Read())
            {
                if (reader["wbf_pref"] != DBNull.Value) strWBFPref = reader["wbf_pref"].ToString();
                if (reader["wbf_id_bill"] != DBNull.Value)
                    intWBFIdBill = Convert.ToInt32(reader["wbf_id_bill"]);
                if (reader["wbf_dat_when"] != DBNull.Value)
                    dtWBFDateWhen = Convert.ToDateTime(reader["wbf_dat_when"]);
                if (reader["wbf_cur_oper"] != DBNull.Value)
                    intWBFCurOper = Convert.ToInt32(reader["wbf_cur_oper"]);
                if (reader["dat_nzp_kvar"] != DBNull.Value)
                    intDATNzpKvar = Convert.ToInt32(reader["dat_nzp_kvar"]);
                if (reader["dat_nzp_dom"] != DBNull.Value)
                    intDATNzpDom = Convert.ToInt32(reader["dat_nzp_dom"]);
                if (reader["dat_pkod"] != DBNull.Value) longDATPkod = Convert.ToInt64(reader["dat_pkod"]);
                if (reader["krn_kind"] != DBNull.Value) intKRNKind = Convert.ToInt32(reader["krn_kind"]);
                if (reader["sys_nzp_user"] != DBNull.Value)
                    intSYSNzpUser = Convert.ToInt32(reader["sys_nzp_user"]);
            }

            Faktura finder = new Faktura();
            finder.pref = strWBFPref;
            finder.nzp_kvar = intDATNzpKvar;
            finder.nzp_dom = intDATNzpDom;
            finder.idFaktura = intKRNKind;
            finder.nzp_user = intSYSNzpUser;
            finder.workRegim = Faktura.WorkFakturaRegims.Web;
            finder.withDolg = true;
            finder.month_ = _task.month_;
            finder.year_ = _task.year_;
            finder.resultFileType = Faktura.FakturaFileTypes.PDF;
            finder.destFileName = String.Format("{0}_{1}{2}_{3}", longDATPkod,
                _task.year_.ToString("0000"), _task.month_.ToString("00"), intWBFIdBill);
            return finder;
        }


        /// <summary>
        /// Получить путь к таблице session_ls
        /// </summary>
        /// <returns></returns>
        private string GetStrWbfDatabase()
        {
            MyDataReader reader;
            string result = "";
            string strSQLQuery = " SELECT  TRIM (dbname)|| '.session_ls' AS wbf_database  " +
                                 " FROM " + Points.Pref + sKernelAliasRest + "s_baselist " +
                                 " WHERE s_baselist.idtype = 8 ";
            Returns ret = ExecRead(conDB, out reader, strSQLQuery, true);
            if (!ret.result) throw new Exception("Ошибка получения таблицы session_ls");
            while (reader.Read())
                if (reader["wbf_database"] != DBNull.Value)
                {
                    result = reader["wbf_database"].ToString().Replace("@" + tableDelimiter, ".");
                    break;
                }
            reader.Close();
            return result;
        }

        public FakturaWebTask(CalcFonTask task)
            : base(task)
        {

        }

    }
   
}
