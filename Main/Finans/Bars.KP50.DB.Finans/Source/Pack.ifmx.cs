using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.IO;
using System.Text;
using System.Collections.Generic;
using Bars.KP50.DB.Sprav.Source;
using Microsoft.Office.Interop.Excel;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Utility;
using System.Diagnostics;
using STCLINE.KP50.IFMX.Kernel.source.CommonType;
using Constants = STCLINE.KP50.Global.Constants;
using DataTable = System.Data.DataTable;
using Points = STCLINE.KP50.Interfaces.Points;
using Newtonsoft.Json;


namespace STCLINE.KP50.DataBase
{
    public partial class DbPack : DbPackClient
    {
        /// <summary>
        /// Получить протокол распределения пачек за опердень
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="IDbConnection"></param>
        /// <returns></returns>
        public ReturnsObjectType<DataSet> GetDistribLog(PackFinder finder, IDbConnection IDbConnection)
        {
            #region Проверка входных параметров
            DateTime datUchet = DateTime.MinValue;
            DateTime datUchetPo = DateTime.MinValue;
            if (!(finder.nzp_user > 0))
                return new ReturnsObjectType<DataSet>("Не задан пользователь");
            if ((finder.dat_uchet.Trim() != "") && !DateTime.TryParse(finder.dat_uchet, out datUchet))
                return new ReturnsObjectType<DataSet>("Неверно задана дата начала периода");
            if ((finder.dat_uchet_po.Trim() != "") && !DateTime.TryParse(finder.dat_uchet_po, out datUchetPo))
                return new ReturnsObjectType<DataSet>("Неверно задана дата окончания периода");

            if (datUchet == DateTime.MinValue)
                return new ReturnsObjectType<DataSet>("Не задана дата начала периода");
            else if ((datUchet.ToString("MMyyyy") != Points.DateOper.ToString("MMyyyy"))
                    || (datUchetPo.ToString("MMyyyy") != Points.DateOper.ToString("MMyyyy")))

                return new ReturnsObjectType<DataSet>("Период должен быть в текущем расчетном месяце");
            #endregion

            //==================================================================
            // ИНФОРМАЦИЯ ПО ПАЧКАМ
            //==================================================================

            DataSet ds = new DataSet();
            DataTable dt;
            DataRow row;

            // Фильтр на лицевые счета Управляющей организации
            string strFilterNumLsList = "";
            if ((finder.dopPointList != null) && (finder.dopPointList.Count > 0)) // если на форме выбраны Управляющие организации
            {
                // Строка кодов Управляющей организации фильтра
                string strNzpWpList = "";
                foreach (Int32 nzpWp in finder.dopPointList)
                    strNzpWpList += "," + nzpWp;
                strNzpWpList = strNzpWpList.Substring(1);

                /*     strFilterNumLsList =
                         " AND {t.}num_ls in ( " +
                         "     select lsb2.num_ls from " + Points.Pref + "_data:lsbase lsb2 " +
                         "     where lsb2.nzp_bl in ( " +
                         "         select bl2.nzp_bl from " + Points.Pref + "_kernel:s_baselist bl2 " +
                         "         where bl2.nzp_wp in (" + strNzpWpList + ") " +
                         "     ) " +
                         " ) ";*/

#if PG
                strFilterNumLsList =
                   " AND {t.}num_ls in ( " +
                   "     select lsb2.num_ls from " + Points.Pref + "_data.lsbase lsb2 " +
                   "     where lsb2.nzp_bl in ( " +
                   "         select bl2.nzp_bl from " + Points.Pref + "_kernel.s_baselist bl2 " +
                   "         where bl2.nzp_wp in (" + strNzpWpList + ") " +
                   "     ) " +
                   " ) ";
#else
                strFilterNumLsList =
                    " AND {t.}num_ls in ( " +
                    "     select lsb2.num_ls from " + Points.Pref + "_data:lsbase lsb2 " +
                    "     where lsb2.nzp_bl in ( " +
                    "         select bl2.nzp_bl from " + Points.Pref + "_kernel:s_baselist bl2 " +
                    "         where bl2.nzp_wp in (" + strNzpWpList + ") " +
                    "     ) " +
                    " ) ";
#endif
            }

            string baseFin = Points.Pref + "_fin_" + datUchet.ToString("yyyy").Substring(2, 2);

            //==================================================================
            // Всего распределено в пачках
            //==================================================================

            // В алгоритме Марата была следующая пара условий (см. ниже)
            // Пока заменим ее на условие kod_sum <> 40 // !RS
            //
            //if ((g_fin__nzp_rs > 0) && (iFlgOnLyRS)) 
            //    sqlText += " AND nzp_rs = " + g_fin__nzp_rs;
            //if (_switch('finans_rt',0) = 1)
            //    sqlText += " AND kod_sum <> 40 ";

            // По счёт фактурам
            string sqlText = "";
#if PG
            sqlText =
               " SELECT coalesce(SUM(coalesce(g_sum_ls,0)),0) AS g_sum_ls " +
               " FROM " + baseFin + ".pack_ls " +
               " WHERE inbasket = 0 and (incase = 0 or incase is null) " +
               " AND kod_sum <> 54 " +
               ((datUchet == datUchetPo)
               ? " AND dat_uchet = public.MDY(" + datUchet.ToString("MM,dd,yyyy") + ") "
               : " AND dat_uchet >= public.MDY(" + datUchet.ToString("MM,dd,yyyy") + ") AND dat_uchet <= public.mdy(" + datUchetPo.ToString("MM,dd,yyyy") + ") ") +
               strFilterNumLsList.Replace("{t.}", "") +
               " AND kod_sum <> 40 ";
#else
            sqlText =
                " SELECT NVL(SUM(NVL(g_sum_ls,0)),0) AS g_sum_ls " +
                " FROM " + baseFin + ":pack_ls " +
                " WHERE inbasket = 0 and (incase = 0 or incase is null) " +
                " AND kod_sum <> 54 " +
                ((datUchet == datUchetPo)
                ? " AND dat_uchet = mdy(" + datUchet.ToString("MM,dd,yyyy") + ") "
                : " AND dat_uchet >= mdy(" + datUchet.ToString("MM,dd,yyyy") + ") AND dat_uchet <= mdy(" + datUchetPo.ToString("MM,dd,yyyy") + ") ") +
                strFilterNumLsList.Replace("{t.}", "") +
                " AND kod_sum <> 40 "; // !RS
#endif
            dt = ClassDBUtils.OpenSQL(sqlText, IDbConnection).GetData();
            Decimal lgSumLs = Convert.ToDecimal(dt.Rows[0]["g_sum_ls"]);

#if PG
            sqlText =
                " SELECT coalesce(SUM(coalesce(g_sum_ls,0)),0) AS g_sum_ls, coalesce(SUM(coalesce(sum_peni,0)),0) AS sum_peni " +
                " FROM " + baseFin + ".pack_ls " +
                " WHERE inbasket = 0 and (incase = 0 or incase is null) " +
                " AND kod_sum = 54 " +
                ((datUchet == datUchetPo)
                ? " AND dat_uchet = public.MDY(" + datUchet.ToString("MM,dd,yyyy") + ") "
                : " AND dat_uchet >= public.MDY(" + datUchet.ToString("MM,dd,yyyy") + ") AND dat_uchet <= public.MDY(" + datUchetPo.ToString("MM,dd,yyyy") + ") ") +
                strFilterNumLsList.Replace("{t.}", "") +
                " AND kod_sum <> 40 "; // !RS
#else
            // По ПД - 4
            sqlText =
                " SELECT NVL(SUM(NVL(g_sum_ls,0)),0) AS g_sum_ls, NVL(SUM(NVL(sum_peni,0)),0) AS sum_peni " +
                " FROM " + baseFin + ":pack_ls " +
                " WHERE inbasket = 0 and (incase = 0 or incase is null) " +
                " AND kod_sum = 54 " +
                ((datUchet == datUchetPo)
                ? " AND dat_uchet = mdy(" + datUchet.ToString("MM,dd,yyyy") + ") "
                : " AND dat_uchet >= mdy(" + datUchet.ToString("MM,dd,yyyy") + ") AND dat_uchet <= mdy(" + datUchetPo.ToString("MM,dd,yyyy") + ") ") +
                strFilterNumLsList.Replace("{t.}", "") +
                " AND kod_sum <> 40 "; // !RS
#endif
            dt = ClassDBUtils.OpenSQL(sqlText, IDbConnection).GetData();
            lgSumLs += Convert.ToDecimal(dt.Rows[0]["g_sum_ls"]) + Convert.ToDecimal(dt.Rows[0]["sum_peni"]);

            //==================================================================
            // Всего нераспределено в пачках
            //==================================================================
            // По счёт фактурам
#if PG
            sqlText =
                " SELECT coalesce(SUM(coalesce(g_sum_ls,0)),0) AS g_sum_ls " +
                " FROM " + baseFin + ".pack_ls " +
                " WHERE (inbasket = 1 or incase = 1) " +
                " AND kod_sum <> 54 " +
                ((datUchet == datUchetPo)
                ? " AND dat_uchet =  public.MDY(" + datUchet.ToString("MM,dd,yyyy") + ") "
                : " AND dat_uchet >=  public.MDY(" + datUchet.ToString("MM,dd,yyyy") + ") AND dat_uchet <=  public.MDY(" + datUchetPo.ToString("MM,dd,yyyy") + ") ") +
                strFilterNumLsList.Replace("{t.}", "") +
                " AND kod_sum <> 40 "; // !RS
#else
            sqlText =
                " SELECT NVL(SUM(NVL(g_sum_ls,0)),0) AS g_sum_ls " +
                " FROM " + baseFin + ":pack_ls " +
                " WHERE (inbasket = 1 or incase = 1) " +
                " AND kod_sum <> 54 " +
                ((datUchet == datUchetPo)
                ? " AND dat_uchet = mdy(" + datUchet.ToString("MM,dd,yyyy") + ") "
                : " AND dat_uchet >= mdy(" + datUchet.ToString("MM,dd,yyyy") + ") AND dat_uchet <= mdy(" + datUchetPo.ToString("MM,dd,yyyy") + ") ") +
                strFilterNumLsList.Replace("{t.}", "") +
                " AND kod_sum <> 40 "; // !RS
#endif
            dt = ClassDBUtils.OpenSQL(sqlText, IDbConnection).GetData();
            Decimal lgSumLsBad = Convert.ToDecimal(dt.Rows[0]["g_sum_ls"]);

            // По ПД - 4
#if PG
            sqlText =
                " SELECT coalesce(SUM(coalesce(g_sum_ls,0)),0) AS g_sum_ls, coalesce(SUM(coalesce(sum_peni,0)),0) AS sum_peni " +
                " FROM " + baseFin + ".pack_ls " +
                " WHERE (inbasket = 1 or incase = 1) " +
                " AND kod_sum = 54 " +
                ((datUchet == datUchetPo)
                ? " AND dat_uchet = public.MDY(" + datUchet.ToString("MM,dd,yyyy") + ") "
                : " AND dat_uchet >= public.MDY(" + datUchet.ToString("MM,dd,yyyy") + ") AND dat_uchet <= public.MDY(" + datUchetPo.ToString("MM,dd,yyyy") + ") ") +
                strFilterNumLsList.Replace("{t.}", "") +
                " AND kod_sum <> 40 "; // !RS
#else
            sqlText =
                " SELECT NVL(SUM(NVL(g_sum_ls,0)),0) AS g_sum_ls, NVL(SUM(NVL(sum_peni,0)),0) AS sum_peni " +
                " FROM " + baseFin + ":pack_ls " +
                " WHERE (inbasket = 1 or incase = 1) " +
                " AND kod_sum = 54 " +
                ((datUchet == datUchetPo)
                ? " AND dat_uchet = mdy(" + datUchet.ToString("MM,dd,yyyy") + ") "
                : " AND dat_uchet >= mdy(" + datUchet.ToString("MM,dd,yyyy") + ") AND dat_uchet <= mdy(" + datUchetPo.ToString("MM,dd,yyyy") + ") ") +
                strFilterNumLsList.Replace("{t.}", "") +
                " AND kod_sum <> 40 "; // !RS
#endif
            dt = ClassDBUtils.OpenSQL(sqlText, IDbConnection).GetData();
            lgSumLsBad += Convert.ToDecimal(dt.Rows[0]["g_sum_ls"]) + Convert.ToDecimal(dt.Rows[0]["sum_peni"]);

            //==================================================================
            // Всего необработано в пачках
            //==================================================================
            // По счёт фактурам
#if PG
            sqlText =
               " SELECT coalesce(SUM(coalesce(g_sum_ls,0)),0) AS g_sum_ls " +
               " FROM " + baseFin + ".pack_ls a, " + baseFin + ".pack b " +
               " WHERE (inbasket = 0 or inbasket is null) and (incase = 0 or incase is null )  and a.nzp_pack = b.nzp_pack and a.dat_uchet is null " +
               " AND kod_sum <> 54 " +
               ((datUchet == datUchetPo)
               ? " AND a.dat_uchet = public.MDY(" + datUchet.ToString("MM,dd,yyyy") + ") "
               : " AND a.dat_uchet >= public.MDY(" + datUchet.ToString("MM,dd,yyyy") + ") AND a.dat_uchet <= public.MDY(" + datUchetPo.ToString("MM,dd,yyyy") + ") ") +
               strFilterNumLsList.Replace("{t.}", "a.") +
               " AND kod_sum <> 40 "; // !RS
#else
            sqlText =
                " SELECT NVL(SUM(NVL(g_sum_ls,0)),0) AS g_sum_ls " +
                " FROM " + baseFin + ":pack_ls a, " + baseFin + ":pack b " +
                " WHERE (inbasket = 0 or inbasket is null) and (incase = 0 or incase is null )  and a.nzp_pack = b.nzp_pack and a.dat_uchet is null " +
                " AND kod_sum <> 54 " +
                ((datUchet == datUchetPo)
                ? " AND a.dat_uchet = mdy(" + datUchet.ToString("MM,dd,yyyy") + ") "
                : " AND a.dat_uchet >= mdy(" + datUchet.ToString("MM,dd,yyyy") + ") AND a.dat_uchet <= mdy(" + datUchetPo.ToString("MM,dd,yyyy") + ") ") +
                strFilterNumLsList.Replace("{t.}", "a.") +
                " AND kod_sum <> 40 "; // !RS
#endif
            dt = ClassDBUtils.OpenSQL(sqlText, IDbConnection).GetData();
            Decimal lUnDistrib = Convert.ToDecimal(dt.Rows[0]["g_sum_ls"]);

            // По ПД - 4
#if PG
            sqlText =
               " SELECT coalesce(SUM(coalesce(g_sum_ls,0)),0) AS g_sum_ls, coalesce(SUM(coalesce(sum_peni,0)),0) AS sum_peni " +
               " FROM " + baseFin + ".pack_ls a, " + baseFin + ".pack b " +
               " WHERE (inbasket = 0 or inbasket is null) and (incase = 0 or incase is null )  and a.nzp_pack = b.nzp_pack and a.dat_uchet is null " +
               " AND kod_sum = 54 " +
               ((datUchet == datUchetPo)
               ? " AND a.dat_uchet =  public.MDY(" + datUchet.ToString("MM,dd,yyyy") + ") "
               : " AND a.dat_uchet >=  public.MDY(" + datUchet.ToString("MM,dd,yyyy") + ") AND a.dat_uchet <=  public.MDY(" + datUchetPo.ToString("MM,dd,yyyy") + ") ") +
               strFilterNumLsList.Replace("{t.}", "a.") +
               " AND kod_sum <> 40 "; // !RS
#else
            sqlText =
                " SELECT NVL(SUM(NVL(g_sum_ls,0)),0) AS g_sum_ls, NVL(SUM(NVL(sum_peni,0)),0) AS sum_peni " +
                " FROM " + baseFin + ":pack_ls a, " + baseFin + ":pack b " +
                " WHERE (inbasket = 0 or inbasket is null) and (incase = 0 or incase is null )  and a.nzp_pack = b.nzp_pack and a.dat_uchet is null " +
                " AND kod_sum = 54 " +
                ((datUchet == datUchetPo)
                ? " AND a.dat_uchet = mdy(" + datUchet.ToString("MM,dd,yyyy") + ") "
                : " AND a.dat_uchet >= mdy(" + datUchet.ToString("MM,dd,yyyy") + ") AND a.dat_uchet <= mdy(" + datUchetPo.ToString("MM,dd,yyyy") + ") ") +
                strFilterNumLsList.Replace("{t.}", "a.") +
                " AND kod_sum <> 40 "; // !RS
#endif
            dt = ClassDBUtils.OpenSQL(sqlText, IDbConnection).GetData();
            lUnDistrib += Convert.ToDecimal(dt.Rows[0]["g_sum_ls"]) + Convert.ToDecimal(dt.Rows[0]["sum_peni"]);

            dt = new DataTable("pack_ls"); // ИНФОРМАЦИЯ ПО ПАЧКАМ
            dt.Columns.Add("sum_ls", typeof(Decimal));
            dt.Columns.Add("sum_ls_bad", typeof(Decimal));
            dt.Columns.Add("un_distrib", typeof(Decimal));

            row = dt.NewRow();
            row["sum_ls"] = lgSumLs;
            row["sum_ls_bad"] = lgSumLsBad;
            row["un_distrib"] = lUnDistrib;
            dt.Rows.Add(row);

            ds.Tables.Add(dt);


            //==================================================================
            // ИНФОРМАЦИЯ ПО ЛИЦЕВЫМ СЧЕТАМ
            //==================================================================

            DataTable dtSupp = new DataTable("fn_supplier"); // ИНФОРМАЦИЯ ПО ЛИЦЕВЫМ СЧЕТАМ
            dtSupp.Columns.Add("nzp_wp", typeof(Int32)); // код записи
            dtSupp.Columns.Add("pref", typeof(string)); // префикс БД
            dtSupp.Columns.Add("point", typeof(string)); // Наименование банка данных
            dtSupp.Columns.Add("sum_prih", typeof(Decimal)); // Сумма распределения
            dtSupp.Columns.Add("err_code", typeof(Int32));
            dtSupp.Columns.Add("err_message", typeof(string));

            foreach (_Point point in Points.PointList)
            {
                // Проверить ограничения на Управляющую организацию
                if ((finder.dopPointList != null) && (finder.dopPointList.Count > 0))
                {
                    bool isFind = false;
                    foreach (Int32 nzpWp in finder.dopPointList)
                        if (point.nzp_wp == nzpWp)
                        {
                            isFind = true;
                            continue;
                        }
                    if (!isFind)
                        continue;
                }

#if PG
                string tableSupplier = point.pref + "_charge_" + (datUchet.ToString("yyyy").Substring(2, 2)) + "." + "fn_supplier" + datUchet.Month.ToString("00");
#else
                string tableSupplier = point.pref + "_charge_" + (datUchet.ToString("yyyy").Substring(2, 2)) + ":" + "fn_supplier" + datUchet.Month.ToString("00");
#endif

                row = dtSupp.NewRow();
                row["pref"] = point.pref;
                row["point"] = point.point;
                row["sum_prih"] = 0;
                row["err_code"] = 0;
                row["err_message"] = "";
#if PG
                sqlText =
                    " SELECT coalesce(SUM(coalesce(sum_prih,0)),0) AS sum_prih " +
                    " FROM " + tableSupplier +
                    " WHERE nzp_serv > 0 " +
                    ((datUchet == datUchetPo)
                    ? " AND dat_uchet =  public.MDY(" + datUchet.ToString("MM,dd,yyyy") + ") "
                    : " AND dat_uchet >=  public.MDY(" + datUchet.ToString("MM,dd,yyyy") + ") AND dat_uchet <=  public.MDY(" + datUchetPo.ToString("MM,dd,yyyy") + ") ") +
                    " AND kod_sum <> 40 "; // !RS
#else
                sqlText =
                    " SELECT NVL(SUM(NVL(sum_prih,0)),0) AS sum_prih " +
                    " FROM " + tableSupplier +
                    " WHERE nzp_serv > 0 " +
                    ((datUchet == datUchetPo)
                    ? " AND dat_uchet = mdy(" + datUchet.ToString("MM,dd,yyyy") + ") "
                    : " AND dat_uchet >= mdy(" + datUchet.ToString("MM,dd,yyyy") + ") AND dat_uchet <= mdy(" + datUchetPo.ToString("MM,dd,yyyy") + ") ") +
                    " AND kod_sum <> 40 "; // !RS
#endif
                try
                {
                    dt = ClassDBUtils.OpenSQL(sqlText, IDbConnection).GetData();
                    row["sum_prih"] = Convert.ToDecimal(dt.Rows[0]["sum_prih"]);
                }
                catch (Exception ex)
                {
                    row["err_code"] = -1;
                    row["err_message"] = ex.Message + ((ex.InnerException != null) ? Environment.NewLine + ex.InnerException.Message : "");
                }

                dtSupp.Rows.Add(row);

                #region Если распределение в Елабуге (DELPHI код)
                //if (_switch('elabuga',1) = 1 )   then  // (Если распределение в Елабуге)
                ////or (_switch('zel',1) = 1)
                //begin
                //    uTableName_fn_supplier:='fn_pd_';
                //    to_supp:= get_TableNameForSupplier_Dat_Oper(g_fin.dat_oper)+' ';
                //    s:=':';
                //    if trim(Q_Circle.FieldByName('dbserver').AsString) <> '' then
                //        s:='@'+trim(Q_Circle.FieldByName('dbserver').AsString)+':';

                //    to_supp:=trim(Q_Circle.FieldByName('dbname').AsString)+s+to_supp;

                //    lQuery.Close;
                //    lQuery.SQL.Text:='SELECT SUM(NVL(sum_prih,0)) AS sum_prih FROM '+trim(to_supp)+' WHERE 1=1 ';
                //    if uFlg_MakeProtokol_AllOperDay then
                //            lQuery.SQL.Text:= lQuery.SQL.Text+' AND dat_uchet is not null '
                //    else
                //            lQuery.SQL.Text:= lQuery.SQL.Text+' AND dat_uchet = '''+DateToStr(dateOper)+'''';
                //    if g_fin.nzp_rs > 0 then
                //    begin
                //            if iFlgOnLyRS  then
                //            begin
                //                lQuery.SQL.Text:= lQuery.SQL.Text+' AND nzp_rs = '+intToStr(g_fin.nzp_rs);
                //            end;
                //    end;

                //    try
                //            lQuery.Open;
                //            lSum_prih:=lSum_prih+lQuery.FieldByName('sum_prih').AsFloat;
                //    except on E:exception do
                //        begin
                //                MessageDlg('ОШИБКА ОПРЕДЕЛЕНИЯ РАСПРЕДЕЛЕННЫХ ОПЛАТ !!!'+#10#13+
                //                    lQuery.SQL.Text+#10#13+
                //                    E.Message, mtError, [mbOk],0);
                //                lSum_prih:=0;
                //        end;
                //    end;
                //end;
                #endregion
            }

            ds.Tables.Add(dtSupp);

            //==================================================================
            // РЕЗУЛЬТАТ
            //==================================================================

            ReturnsObjectType<DataSet> ret = new ReturnsObjectType<DataSet>(ds);

            Decimal sumPrih = 0;
            foreach (DataRow rowPrih in ds.Tables["fn_supplier"].Rows)
                sumPrih += Convert.ToDecimal(rowPrih["sum_prih"]);

            if (Math.Abs(lgSumLs - sumPrih) > Convert.ToDecimal(0.001))
            {
                ret.result = false;
                ret.tag = -1;
                ret.text = String.Format("Распределение оплат произведено некорректно. Cумма рассогласования {0} руб.", Math.Abs(lgSumLs - sumPrih).ToString("N2"));
            }
            else
            {
                ret.text = String.Format("Распределение оплат произведено успешно. Распределено {0} руб.", lgSumLs.ToString("N2"));
            }
            ret.text += " Подробности см. в отчёте \"Контроль распределения оплат\"";

            //==================================================================
            // Форматировать суммы в ячейках
            //==================================================================
            ds.Tables["pack_ls"].Rows[0]["sum_ls"] = Math.Round(Convert.ToDecimal(ds.Tables["pack_ls"].Rows[0]["sum_ls"]), 2);
            ds.Tables["pack_ls"].Rows[0]["sum_ls_bad"] = Math.Round(Convert.ToDecimal(ds.Tables["pack_ls"].Rows[0]["sum_ls_bad"]), 2);
            ds.Tables["pack_ls"].Rows[0]["un_distrib"] = Math.Round(Convert.ToDecimal(ds.Tables["pack_ls"].Rows[0]["un_distrib"]), 2);

            foreach (DataRow rowPrih in ds.Tables["fn_supplier"].Rows)
                rowPrih["sum_prih"] = Math.Round(Convert.ToDecimal(rowPrih["sum_prih"]), 2);

            return ret;
        }

        /// <summary>
        /// Поиск необработанных оплат
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="IDbConnection"></param>
        /// <returns></returns>
        public ReturnsType FindUnDistribPackLs(PackFinder finder, IDbConnection IDbConnection)
        {
            #region Проверка входных параметров
            DateTime datUchet = DateTime.MinValue;
            DateTime datUchetPo = DateTime.MinValue;


            if (!(finder.nzp_user > 0))
                return new ReturnsType(false, "Не задан пользователь", -1);
            if ((finder.dat_uchet.Trim() != "") && !DateTime.TryParse(finder.dat_uchet, out datUchet))
                return new ReturnsType(false, "Неверно задана дата начала периода", -1);
            if ((finder.dat_uchet_po.Trim() != "") && !DateTime.TryParse(finder.dat_uchet_po, out datUchetPo))
                return new ReturnsType(false, "Неверно задана дата окончания периода", -1);

            if (datUchet == DateTime.MinValue)
                return new ReturnsType(false, "Не задана дата начала периода", -1);
            else if ((datUchet.ToString("MMyyyy") != Points.DateOper.ToString("MMyyyy"))
                || (datUchetPo.ToString("MMyyyy") != Points.DateOper.ToString("MMyyyy")))
                return new ReturnsType(false, "Период должен быть в текущем расчетном месяце", -1);

            #endregion

            ReturnsType ret = new ReturnsType();

            DataTable dt;

            string sqlText = " drop table t1 "; //!!! - заменить t1
            ClassDBUtils.ExecSQL(sqlText, IDbConnection, ClassDBUtils.ExecMode.Log);

#if PG
            sqlText = " CREATE TEMP TABLE t1 (nzp_pack_ls integer, num_ls numeric(15,0)) ";
#else
            sqlText = " CREATE TEMP TABLE t1 (nzp_pack_ls integer, num_ls decimal(15,0)) ";
#endif
            ClassDBUtils.ExecSQL(sqlText, IDbConnection);

            string baseFin = Points.Pref + "_fin_" + Points.DateOper.ToString("yyyy").Substring(2, 2);

#if PG
            sqlText =
                " insert into t1 (nzp_pack_ls, num_ls) " +
                " select a.nzp_pack_ls, a.num_ls " +
                " from " + baseFin + ".pack_ls a, " + baseFin + ".pack b " +
                " where a.dat_uchet is null and (a.inbasket = 0 or a.inbasket is null) " +
                " and a.nzp_pack = b.nzp_pack and b.dat_uchet is not null and b.flag > 0 and b.flag is not null ";
#else
            sqlText =
                " insert into t1 (nzp_pack_ls, num_ls) " +
                " select a.nzp_pack_ls, a.num_ls " +
                " from " + baseFin + ":pack_ls a, " + baseFin + ":pack b " +
                " where a.dat_uchet is null and (a.inbasket = 0 or a.inbasket is null) " +
                " and a.nzp_pack = b.nzp_pack and b.dat_uchet is not null and b.flag > 0 and b.flag is not null ";
#endif
            dt = ClassDBUtils.OpenSQL(sqlText, IDbConnection).GetData();

            sqlText = " create index ix1_t1 on t1 (nzp_pack_ls) ";
            ClassDBUtils.ExecSQL(sqlText, IDbConnection);

#if PG
            sqlText = " analyze t1 ";
#else
            sqlText = " update statistics for table t1 ";
#endif
            ClassDBUtils.ExecSQL(sqlText, IDbConnection);

            sqlText = " SELECT COUNT(*) as cnt FROM t1 ";
            dt = ClassDBUtils.OpenSQL(sqlText, IDbConnection).GetData();
            Int32 cnt = Convert.ToInt32(dt.Rows[0]["cnt"]);
            if (cnt > 0)
            {
#if PG
                sqlText =
                    " update " + baseFin + ".pack_ls set dat_uchet = public.MDY(" + Points.DateOper.ToString("MM,dd,yyyy") + "), inbasket = 1 " +
                    " where nzp_pack_ls in (select a.nzp_pack_ls from t1 a) ";
#else
                sqlText =
                    " update " + baseFin + ":pack_ls set dat_uchet = mdy(" + Points.DateOper.ToString("MM,dd,yyyy") + "), inbasket = 1 " +
                    " where nzp_pack_ls in (select a.nzp_pack_ls from t1 a) ";
#endif
                ClassDBUtils.ExecSQL(sqlText, IDbConnection);

#if PG
                sqlText =
                                    " insert into " + baseFin + ".pack_ls_err (nzp_pack_ls, nzp_err, nzp_serv, note) " +
                                    " select nzp_pack_ls, 6, 0, 'Отказано в распределении в результате сбоев в программе' " +
                                    " from t1 ";
#else
                sqlText =
                    " insert into " + baseFin + ":pack_ls_err (nzp_pack_ls, nzp_err, nzp_serv, note) " +
                    " select nzp_pack_ls, 6, 0, 'Отказано в распределении в результате сбоев в программе' " +
                    " from t1 ";
#endif
                ClassDBUtils.ExecSQL(sqlText, IDbConnection);

                ret.tag = cnt;
            }

            sqlText = " DROP TABLE t1 ";
            ClassDBUtils.ExecSQL(sqlText, IDbConnection);

#if PG
            sqlText = " update " + baseFin + ".pack_ls set inbasket = 0 where inbasket = 2 ";
#else
            sqlText = " update " + baseFin + ":pack_ls set inbasket = 0 where inbasket = 2 ";
#endif

            ClassDBUtils.ExecSQL(sqlText, IDbConnection);

#if PG
            sqlText = " update " + baseFin + ".pack set islock = NULL where islock = 1 ";
#else
            sqlText = " update " + baseFin + ":pack set islock = NULL where islock = 1 ";
#endif
            ClassDBUtils.ExecSQL(sqlText, IDbConnection);

            return ret;
        }

        //private Int32 uInBasket;
        private string uTableName_fn_supplier;

        // Получить список управляющих компаний
        private List<string> _LoadUk(List<string> listprefixUk, IDbConnection IDbConnection)
        {
            List<string> pListUk = new List<string>();

            string sqlText = "";

#if PG
            sqlText = " select * from " + Points.Pref + "_kernel.s_erc_code where is_now() = 1 ";
#else
            sqlText = " select * from " + Points.Pref + "_kernel:s_erc_code where is_current = 1 ";
#endif
            DataTable dt = ClassDBUtils.OpenSQL(sqlText, IDbConnection).GetData();
            foreach (DataRow row in dt.Rows)
            {
                //!!! pListUk.Add(trim(Copy(lQuery.FieldByname('erc_code').AsString,1,12)));
                pListUk.Add(row["err_code"].ToString().Trim().Substring(0, 12));
            }

            if (listprefixUk != null)
            {
#if PG
                sqlText = " select * from " + Points.Pref + "_kernel.s_erck ";
#else
                sqlText = " select * from " + Points.Pref + "_kernel:s_erck ";
#endif
                dt = ClassDBUtils.OpenSQL(sqlText, IDbConnection).GetData();
                foreach (DataRow row in dt.Rows)
                {
                    listprefixUk.Add(row["kod"].ToString());
                }
            }

            return pListUk;
        }

        public enum TypeBD
        {
            ttCharge,
            ttFinans,
            ttData,
            ttSystem
        }

        public string get_TableNameForSupplier_Dat_Oper(DateTime dat_oper)
        {
            if ((uTableName_fn_supplier == "fn_supplier") || (uTableName_fn_supplier == "fn_pd_"))
                return uTableName_fn_supplier + dat_oper.ToString("MM");
            else
                return uTableName_fn_supplier;
        }

        // Отклонить проводку лицевого счета
        public ReturnsType RollBack_Distrib_Pack_ls(Int32 pNzp_pack_ls, IDbConnection IDbConnection)
        {
            int nzp_user = 0; // !!! Потом определить код пользователя 
            //#region определение локального пользователя
            //DbWorkUser db = new DbWorkUser();
            //Returns ret_user;
            //int nzp_user = db.GetLocalUser(conn_db, paramcalc, out ret_user);
            //db.Close();
            //if (!ret.result)
            //{
            //    //connectionID.Close();
            //    ret.result = ret_user.result;
            //    ret.tag = ret_user.tag;
            //    ret.text = ret_user.text;
            //    return false;
            //}
            //#endregion

            ReturnsType ret = new ReturnsType();

            string baseFin = Points.Pref + "_fin_" + Points.DateOper.ToString("yyyy").Substring(2, 2);

            // Для выбранного лицевого счёта определить суммы распределения
            string sqlText = "";

#if PG
            sqlText = " SELECT * " +
                            "from " + baseFin + ".pack_ls where nzp_pack_ls = " + pNzp_pack_ls;
#else
            sqlText = " SELECT a.nzp_pack,a.nzp_pack_ls, a.num_ls, a.dat_uchet, k.nzp_dom " +
                "from " + baseFin + ":pack_ls a," + Points.Pref + "_data:kvar k  where a.nzp_pack_ls = " + pNzp_pack_ls + " and a.num_ls = k.num_ls ";
#endif

            DataTable Query_Pack_ls = ClassDBUtils.OpenSQL(sqlText, IDbConnection).GetData();
            DataRow rowPackls = Query_Pack_ls.Rows[0];

            Ls kvar = new Ls();
            kvar.num_ls = Convert.ToInt32(rowPackls["num_ls"]);
            DbAdres dbadres = new DbAdres();
            ReturnsObjectType<Ls> pref = dbadres.GetLsLocation(kvar, IDbConnection);
            dbadres.Close();
            pref.ThrowExceptionIfError();

#if PG
            sqlText = " DELETE FROM " +
                           pref.returnsData.pref + "_charge_" + (Convert.ToDateTime(rowPackls["dat_uchet"]).Year - 2000).ToString("00") + "." +
                           uTableName_fn_supplier + (Convert.ToDateTime(rowPackls["dat_uchet"]).Month).ToString("00") +
                           " WHERE " +
                           " nzp_pack_ls = " + rowPackls["nzp_pack_ls"].ToString();
#else
            sqlText = " DELETE FROM " +
                pref.returnsData.pref + "_charge_" + (Convert.ToDateTime(rowPackls["dat_uchet"]).Year - 2000).ToString("00") + ":" +
                uTableName_fn_supplier + (Convert.ToDateTime(rowPackls["dat_uchet"]).Month).ToString("00") +
                " WHERE " +
                " nzp_pack_ls = " + rowPackls["nzp_pack_ls"].ToString();
#endif
            ret = ClassDBUtils.ExecSQL(sqlText, IDbConnection).GetReturnsType();
            if (!ret.result)
            {
#if PG
                sqlText =
                                " Insert into " + baseFin + ".pack_log " +
                                " ( nzp_pack,nzp_pack_ls,dat_oper,dat_log,nzp_wp, txt_log,tip_log ) " +
                                " Values ( " + rowPackls["nzp_pack"].ToString() + "," + rowPackls["nzp_pack_ls"] + ",'" + Convert.ToDateTime(Points.DateOper).ToShortDateString() + "', now(), " +
                                               "0, " + Utils.EStrNull("Ошибка отмены распределения 1.1") + ",0)";
#else
                sqlText =
                " Insert into " + baseFin + ":pack_log " +
                " ( nzp_pack,nzp_pack_ls,dat_oper,dat_log,nzp_wp, txt_log,tip_log ) " +
                " Values ( " + rowPackls["nzp_pack"].ToString() + "," + rowPackls["nzp_pack_ls"] + ",'" + Convert.ToDateTime(Points.DateOper).ToShortDateString() + "', current, " +
                               "0, " + Utils.EStrNull("Ошибка отмены распределения 1.1") + ",0)";
#endif
                ret = ClassDBUtils.ExecSQL(sqlText, IDbConnection).GetReturnsType();
                return ret;
            }

#if PG
            sqlText = " UPDATE " + baseFin + ".pack_ls set alg = 0, dat_uchet = null, inbasket=1, date_rdistr = now(), nzp_user = " + nzp_user +
                           " WHERE " +
                           " nzp_pack_ls = " + rowPackls["nzp_pack_ls"].ToString();
#else
            sqlText = " UPDATE " + baseFin + ":pack_ls set alg = 0, dat_uchet = null, inbasket=1, date_rdistr = current, nzp_user = " + nzp_user +
                " WHERE " +
                " nzp_pack_ls = " + rowPackls["nzp_pack_ls"].ToString();
#endif
            ret = ClassDBUtils.ExecSQL(sqlText, IDbConnection).GetReturnsType();
            if (!ret.result)
            {
#if PG
                sqlText =
                               " Insert into " + baseFin + ".pack_log " +
                               " ( nzp_pack,nzp_pack_ls,dat_oper,dat_log,nzp_wp, txt_log,tip_log ) " +
                               " Values ( " + rowPackls["nzp_pack"].ToString() + "," + rowPackls["nzp_pack_ls"] + ",'" + Convert.ToDateTime(Points.DateOper).ToShortDateString() + "', now(), " +
                                              "0, " + Utils.EStrNull("Ошибка отмены распределения 1.2") + ",0)";
#else
                sqlText =
                " Insert into " + baseFin + ":pack_log " +
                " ( nzp_pack,nzp_pack_ls,dat_oper,dat_log,nzp_wp, txt_log,tip_log ) " +
                " Values ( " + rowPackls["nzp_pack"].ToString() + "," + rowPackls["nzp_pack_ls"] + ",'" + Convert.ToDateTime(Points.DateOper).ToShortDateString() + "', current, " +
                               "0, " + Utils.EStrNull("Ошибка отмены распределения 1.2") + ",0)";
#endif
                ret = ClassDBUtils.ExecSQL(sqlText, IDbConnection).GetReturnsType();
                return ret;
            }

#if PG
            sqlText =
                       " Insert into " + baseFin + ".pack_log " +
                       " ( nzp_pack,nzp_pack_ls,dat_oper,dat_log,nzp_wp, txt_log,tip_log ) " +
                       " Values ( " + rowPackls["nzp_pack"].ToString() + "," + rowPackls["nzp_pack_ls"] + ",'" + Convert.ToDateTime(Points.DateOper).ToShortDateString() + "', now(), " +
                                      "0, " + Utils.EStrNull("Отмена распределения") + ",0)";
#else
            sqlText =
            " Insert into " + baseFin + ":pack_log " +
            " ( nzp_pack,nzp_pack_ls,dat_oper,dat_log,nzp_wp, txt_log,tip_log ) " +
            " Values ( " + rowPackls["nzp_pack"].ToString() + "," + rowPackls["nzp_pack_ls"] + ",'" + Convert.ToDateTime(Points.DateOper).ToShortDateString() + "', current, " +
                           "0, " + Utils.EStrNull("Отмена распределения") + ",0)";
#endif
            ret = ClassDBUtils.ExecSQL(sqlText, IDbConnection).GetReturnsType();

            // Сохранить признак о том, что нужно пересчитать дом в сальдо по перечислениям
            string fn_operday_dom;
#if PG
            fn_operday_dom = Points.Pref + "_fin_" + (Points.DateOper.Year - 2000).ToString("00") + ".fn_operday_dom_mc";
#else
            fn_operday_dom = Points.Pref + "_fin_" + (Points.DateOper.Year - 2000).ToString("00") + ":fn_operday_dom_mc";
#endif

            sqlText = " insert into  " + fn_operday_dom + "(nzp_dom, date_oper) values (" + rowPackls["nzp_dom"] + ",'" + Convert.ToDateTime(rowPackls["dat_uchet"]).ToShortDateString() + "' ) ";
            ret = ClassDBUtils.ExecSQL(sqlText, IDbConnection).GetReturnsType();

            Pack packFinder = new Pack();
            packFinder.nzp_pack = Convert.ToInt32(rowPackls["nzp_pack"]);

            ReturnsType retType = Upd_SUM_RASP_and_SUM_NRASP(packFinder, IDbConnection);

            return ret;
        }

        // Переписать суммы распределено и нераспределено для пачки

        public ReturnsType Upd_SUM_RASP_and_SUM_NRASP(PackFinder finder)
        {
            Returns ret;
            List<Pack> listPack = FindPack(finder, out ret);
            if (!ret.result)
            {
                return new ReturnsType(ret.result, ret.text, ret.tag, ret.sql_error);
            }

            if (listPack == null || listPack.Count == 0)
            {
                return new ReturnsType(false, "Пачка не найдена", -1);
            }

            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return new ReturnsType(ret.result, ret.text, ret.tag, ret.sql_error);

            ReturnsType retType = Upd_SUM_RASP_and_SUM_NRASP(listPack[0], conn_db);

            //if (finder.par_pack > 0)
            if (listPack[0].par_pack > 0)
            {
                finder.nzp_pack = listPack[0].par_pack;
                listPack = FindPack(finder, out ret);
                retType = Upd_SUM_RASP_and_SUM_NRASP(listPack[0], conn_db);
            }

            conn_db.Close();

            return retType;
        }
        public ReturnsType Upd_SUM_RASP_and_SUM_NRASP(DataRow rowPack, IDbConnection dbConnection)
        {
            Pack packFinder = new Pack();
            packFinder.par_pack = Convert.ToInt32(rowPack["par_pack"]);
            packFinder.nzp_pack = Convert.ToInt32(rowPack["nzp_pack"]);
            packFinder.sum_pack = Convert.ToDecimal(rowPack["sum_pack"]);
            return Upd_SUM_RASP_and_SUM_NRASP(packFinder, dbConnection);


        }
        public ReturnsType Upd_SUM_RASP_and_SUM_NRASP(Pack packFinder, IDbConnection dbConnection)
        {
            ReturnsType ret = new ReturnsType();
            var baseFin = Points.Pref + "_fin_" + (packFinder.year_ % 100).ToString("00");
            var wherePack = new StringBuilder();
            if (packFinder.par_pack != packFinder.nzp_pack) wherePack.AppendFormat(" AND nzp_pack = {0}", packFinder.nzp_pack);
            else wherePack.AppendFormat(" AND nzp_pack in (SELECT a.nzp_pack FROM {0}pack a WHERE a.par_pack   = {1})", baseFin + tableDelimiter, packFinder.nzp_pack);

            
            //1. получить сумму распределенных оплат в пачке или суперпачки
            var sqlText = new StringBuilder("SELECT SUM(g_sum_ls) sum_ FROM ");
            sqlText.AppendFormat("{0}{1}pack_ls WHERE ", baseFin, tableDelimiter);
            sqlText.Append(" coalesce(cast(alg as int),0)<>0 and inbasket = 0 and dat_uchet is not null");
            sqlText.Append(wherePack);
            DataTable queryPackLs = ClassDBUtils.OpenSQL(sqlText.ToString(), dbConnection).GetData();
            DataRow rowPackls = queryPackLs.Rows[0];
            decimal distrSum = 0; // сумма распределенных оплат в пачке или суперпачке
            if (rowPackls["sum_"] != DBNull.Value) distrSum = Convert.ToDecimal(rowPackls["sum_"]);

            //2.получить сумму пачки
            sqlText = new StringBuilder("SELECT SUM(g_sum_ls) sum_ FROM ");
            sqlText.AppendFormat("{0}{1}pack_ls WHERE 1=1 ", baseFin, tableDelimiter);
            sqlText.Append(wherePack);
            queryPackLs = ClassDBUtils.OpenSQL(sqlText.ToString(), dbConnection).GetData();
            rowPackls = queryPackLs.Rows[0];
            decimal packSum = 0; // сумма пачки
            if (rowPackls["sum_"] != DBNull.Value) packSum = Convert.ToDecimal(rowPackls["sum_"]);
            
            var uFlag = 23;//не распределена
            if (distrSum == 0)
            {
                if (packSum == 0)
                {
                    //проверить наличие оплат в корзине
                    sqlText = new StringBuilder("SELECT 1 FROM ");
                    sqlText.AppendFormat("{0}pack_ls ", baseFin + tableDelimiter);
                    sqlText.Append(" WHERE inbasket=1 ");
                    sqlText.Append(wherePack);
                    sqlText.Append(" limit 1 ");

                    queryPackLs = ClassDBUtils.OpenSQL(sqlText.ToString(), dbConnection).GetData();
                    if (queryPackLs.Rows.Count > 0) uFlag = 22;// Распределена с ошибками
                    else
                    {
                        //проверить наличие распределенных оплат
                        sqlText = new StringBuilder("SELECT 1 FROM ");
                        sqlText.AppendFormat("{0}pack_ls WHERE ", baseFin + tableDelimiter);
                        sqlText.Append(" inbasket=0 and coalesce(cast(alg as int),0) <>0 and dat_uchet is not null ");
                        sqlText.Append(wherePack);
                        sqlText.Append(" limit 1 ");

                        queryPackLs = ClassDBUtils.OpenSQL(sqlText.ToString(), dbConnection).GetData();
                        uFlag = queryPackLs.Rows.Count > 0 ? 21 /*распределена*/ : 23 /*не распределена*/;
                    }
                }
                else
                {
                    uFlag = 23; // Не распределена

                    sqlText = new StringBuilder("SELECT 1 FROM ");
                    sqlText.AppendFormat(" {0}pack_ls ", baseFin + tableDelimiter);
                    sqlText.Append(" WHERE inbasket=1 ");
                    sqlText.Append(wherePack);
                    sqlText.Append(" limit 1 ");

                    queryPackLs = ClassDBUtils.OpenSQL(sqlText.ToString(), dbConnection).GetData();
                    if (queryPackLs.Rows.Count > 0) uFlag = 22;          // Распределена с ошибками
                }
            }
            else
                if (Math.Abs(packSum - distrSum) > Convert.ToDecimal(0.001))
                    uFlag = 22;          // Распределена с ошибками
                else
                    uFlag = 21;         // Распределена без ошибок

            sqlText = new StringBuilder("UPDATE ");
            sqlText.AppendFormat(" {0}pack ", baseFin + tableDelimiter);
            sqlText.AppendFormat(" SET flag = {0}, ", uFlag);
            sqlText.AppendFormat(" sum_rasp = {0}, ", distrSum);
            sqlText.AppendFormat(" sum_nrasp = {0} ", Convert.ToString(packSum - distrSum));
            sqlText.AppendFormat(" WHERE nzp_pack = {0} ", packFinder.nzp_pack);
            ret = ClassDBUtils.ExecSQL(sqlText.ToString(), dbConnection).GetReturnsType();
            if (ret.result) return ret;

            sqlText = new StringBuilder(" Insert into  ");
            sqlText.AppendFormat(" {0}pack_log ", baseFin + tableDelimiter);
            sqlText.Append(" ( nzp_pack,dat_oper,dat_log,nzp_wp, txt_log,tip_log ) ");
            sqlText.AppendFormat(" Values ( {0},'{1}', now(), ",packFinder.nzp_pack, Convert.ToDateTime(Points.DateOper).ToShortDateString());
            sqlText.AppendFormat(" 0, {0}, 0", Utils.EStrNull("Ошибка подсчёта общей суммы распределения по пачке 1.1"));
            ret = ClassDBUtils.ExecSQL(sqlText.ToString(), dbConnection).GetReturnsType();
            return ret;
        }

        /// <summary>
        /// Поиск ошибок в распределении
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="IDbConnection"></param>
        /// <returns></returns>        
        public ReturnsType FindErrorInPackLs(PackFinder finder, IDbConnection IDbConnection)
        {
            #region Проверка входных параметров
            DateTime datUchet = DateTime.MinValue;
            DateTime datUchetPo = DateTime.MinValue;

            string sqlText = "";

            if (!(finder.nzp_user > 0))
                return new ReturnsType(false, "Не задан пользователь", -1);
            if ((finder.dat_uchet.Trim() != "") && !DateTime.TryParse(finder.dat_uchet, out datUchet))
                return new ReturnsType(false, "Неверно задана дата начала периода", -1);
            if ((finder.dat_uchet_po.Trim() != "") && !DateTime.TryParse(finder.dat_uchet_po, out datUchetPo))
                return new ReturnsType(false, "Неверно задана дата окончания периода", -1);

            if (datUchet == DateTime.MinValue)
                return new ReturnsType(false, "Не задана дата начала периода", -1);
            else if ((datUchet.ToString("MMyyyy") != Points.DateOper.ToString("MMyyyy"))
                || (datUchetPo.ToString("MMyyyy") != Points.DateOper.ToString("MMyyyy")))
                return new ReturnsType(false, "Период должен быть в текущем расчетном месяце", -1);
            #endregion

            ReturnsType ret = new ReturnsType();


            Int32 Count_ls_error = 0;
            Int32 uCount_ls = 0;
            decimal g_sum_ls_total = 0;
            decimal g_sum_ls_err = 0;
            decimal sum_fn_suppl;

            //List<string> selfUkPrefixList = new List<string>();
            //List<string> ListUk = _LoadUk(selfUkPrefixList, IDbConnection); //!!! один раз, добавить в код

            string baseFin = Points.Pref + "_fin_" + Points.DateOper.ToString("yyyy").Substring(2, 2);



#if PG
            sqlText =
                           " SELECT * " +
                           " FROM " + baseFin + ".pack_ls " +
                           " WHERE dat_uchet between '" + Convert.ToDateTime(datUchet).ToShortDateString() + "' and '" + Convert.ToDateTime(datUchetPo).ToShortDateString() + "' AND inbasket = 0 and coalesce(cast(alg as int),0) <> 0 ";
            DataTable dtPackLs = ClassDBUtils.OpenSQL(sqlText, IDbConnection).GetData();
#else
            sqlText =
                " SELECT * " +
                " FROM " + baseFin + ":pack_ls " +
                " WHERE dat_uchet between '" + Convert.ToDateTime(datUchet).ToShortDateString() + "' and '" + Convert.ToDateTime(datUchetPo).ToShortDateString() + "' AND inbasket = 0 and alg <> 0 ";
            DataTable dtPackLs = ClassDBUtils.OpenSQL(sqlText, IDbConnection).GetData();
#endif

            int num_ls;
            var dbCalc = new DbCalcCharge();
            DateTime datUchet2;
            Returns returns;

            foreach (DataRow rowPackLs in dtPackLs.Rows)
            {
                if (rowPackLs["nzp_pack_ls"] != DBNull.Value)
                {
                    num_ls = rowPackLs["num_ls"] != DBNull.Value ? Convert.ToInt32(rowPackLs["num_ls"]) : 0;

                    g_sum_ls_total += Convert.ToDecimal(rowPackLs["g_sum_ls"]);
                    if (Convert.ToInt32(rowPackLs["kod_sum"]) == 54)
                        uTableName_fn_supplier = "fn_pd";
                    else
                        uTableName_fn_supplier = "fn_supplier";

                    if ((Convert.ToInt32(rowPackLs["incase"]) != 1) & (rowPackLs["nzp_pack_ls"] != DBNull.Value))
                    {
                        uCount_ls += 1;

#if PG
                        sqlText = " DELETE FROM " + baseFin + ".pack_ls_err WHERE nzp_pack_ls = " + rowPackLs["nzp_pack_ls"].ToString() + " AND nzp_err = 4 ";
#else
                        sqlText = " DELETE FROM " + baseFin + ":pack_ls_err WHERE nzp_pack_ls = " + rowPackLs["nzp_pack_ls"].ToString() + " AND nzp_err = 4 ";
#endif
                        ClassDBUtils.ExecSQL(sqlText, IDbConnection);



                        if (Convert.ToInt32(rowPackLs["kod_sum"]) == 54)
                            sqlText = " SELECT SUM(sum_prih) as sum_prih ,SUM(sum_prih),0,0 ";
                        else
#if PG
                            sqlText = " SELECT SUM(sum_prih) as sum_prih  ,SUM(coalesce(s_user,0)),SUM(coalesce(s_dolg,0)),SUM(coalesce(s_forw,0)) ";
#else
                            sqlText = " SELECT SUM(sum_prih) as sum_prih  ,SUM(NVL(s_user,0)),SUM(NVL(s_dolg,0)),SUM(NVL(s_forw,0)) ";
#endif
                        Ls kvar = new Ls();
                        kvar.num_ls = Convert.ToInt32(rowPackLs["num_ls"]);
                        DbAdres dbadres = new DbAdres();
                        ReturnsObjectType<Ls> pref = dbadres.GetLsLocation(kvar, IDbConnection);
                        dbadres.Close();
                        pref.ThrowExceptionIfError();

                        datUchet2 = Convert.ToDateTime(rowPackLs["dat_uchet"]);

#if PG
                        sqlText +=
                                    " FROM " +
                                    pref.returnsData.pref + "_charge_" + (datUchet2.Year % 100).ToString("00") + "." + uTableName_fn_supplier + datUchet2.Month.ToString("00") +
                                    " WHERE " +
                                    " nzp_pack_ls = " + rowPackLs["nzp_pack_ls"].ToString();
#else
                        sqlText +=
                            " FROM " +
                            pref.returnsData.pref + "_charge_" + (datUchet2.Year % 100).ToString("00") + ":" + uTableName_fn_supplier + datUchet2.Month.ToString("00") +
                            " WHERE " +
                            " nzp_pack_ls = " + rowPackLs["nzp_pack_ls"].ToString();
#endif
                        DataTable Query_fn_supplier = ClassDBUtils.OpenSQL(sqlText, IDbConnection).GetData();

                        DataRow rowFnSupplier = Query_fn_supplier.Rows[0];

                        if (rowFnSupplier[0] == DBNull.Value)
                        {
                            sum_fn_suppl = 0;
                        }
                        else
                        {
                            sum_fn_suppl = Convert.ToDecimal(rowFnSupplier[0]);


                            if (Math.Abs(Convert.ToDecimal(rowPackLs["g_sum_ls"]) - sum_fn_suppl)
                                    > Convert.ToDecimal(0.001))
                            {
                                Count_ls_error += 1;
                                g_sum_ls_err += Convert.ToDecimal(rowPackLs["g_sum_ls"]);

#if PG
                                sqlText =
                                        " SELECT * " +
                                        " FROM " + baseFin + ".pack " +
                                        " WHERE nzp_pack = " + rowPackLs["nzp_pack"].ToString();
#else
                                sqlText =
                                    " SELECT * " +
                                    " FROM " + baseFin + ":pack " +
                                    " WHERE nzp_pack = " + rowPackLs["nzp_pack"].ToString();
#endif
                                DataTable Query_Pack = ClassDBUtils.OpenSQL(sqlText, IDbConnection).GetData();
                                DataRow rowPack = Query_Pack.Rows[0];

                                if (rowPackLs["nzp_pack_ls"] != DBNull.Value)
                                {
                                    ret = RollBack_Distrib_Pack_ls(Convert.ToInt32(rowPackLs["nzp_pack_ls"]), IDbConnection);

                                    returns = dbCalc.CalcChargeXXUchetOplatForLs(IDbConnection, null, new Charge() { num_ls = num_ls, pref = pref.returnsData.pref, year_ = datUchet2.Year, month_ = datUchet2.Month }, CalcTypes.FunctionType.Payment);
                                    if (!returns.result)
                                    {
                                        dbCalc.Close();
                                        return new ReturnsType(ret.result, ret.text, ret.tag);
                                    }

                                    // Перерисовать список пачек
                                    Upd_SUM_RASP_and_SUM_NRASP(rowPack, IDbConnection);

#if PG
                                    sqlText =
                                        " UPDATE " + baseFin + ".pack_ls SET dat_uchet = null , inbasket = 1, alg = 0 " +
                                        " WHERE nzp_pack_ls = " + rowPackLs["nzp_pack_ls"].ToString();
#else
                                    sqlText =
                                        " UPDATE " + baseFin + ":pack_ls SET dat_uchet = null , inbasket = 1, alg = 0 " +
                                        " WHERE nzp_pack_ls = " + rowPackLs["nzp_pack_ls"].ToString();
#endif
                                    ClassDBUtils.ExecSQL(sqlText, IDbConnection);

#if PG
                                    sqlText =
                                        " insert into " + baseFin + ".pack_ls_err(nzp_err,nzp_pack_ls,note) values(4," + rowPackLs["nzp_pack_ls"].ToString() + ",'Сумма оплаты " + rowPackLs["g_sum_ls"] + " руб не соответствует сумме распределения " + rowFnSupplier[0] + " руб')";
#else
                                    sqlText =
                                        " insert into " + baseFin + ":pack_ls_err(nzp_err,nzp_pack_ls,note) values(4," + rowPackLs["nzp_pack_ls"].ToString() + ",'Сумма оплаты " + rowPackLs["g_sum_ls"] + " руб не соответствует сумме распределения " + rowFnSupplier[0] + " руб')";
#endif
                                    ClassDBUtils.ExecSQL(sqlText, IDbConnection);
                                }
                            }
                        }
                    }
                }
            }

            dbCalc.Close();

            if (Count_ls_error > 0)
            {
                return new ReturnsType(false, "В указанном периоде проверено " + uCount_ls + " шт. оплат на сумму " + g_sum_ls_total.ToString("N2") + ". Обнаружены оплаты с недопустимым распределение (" + Count_ls_error + " шт. на сумму " + g_sum_ls_err.ToString("N2") + "), которые были помещены в корзину с типом ошибки 'Несоответствие суммы распределения по л/с и суммы оплаты'!", -1);
            }
            else
                return new ReturnsType(true, "В указанном периоде проверено " + uCount_ls + " шт. оплат сумму " + g_sum_ls_total.ToString("N2") + ". Ошибок распределения не обнаружено !", -1);
        }

        /// <summary>
        /// Поиск учёта оплат по Управляющим организацим
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="IDbConnection"></param>
        /// <returns></returns>
        public ReturnsType FindErrorInFnSupplier(PackFinder finder, IDbConnection IDbConnection)
        {
            //#region Проверка входных параметров
            DateTime datUchet = DateTime.MinValue;
            DateTime datUchetPo = DateTime.MinValue;
            string sqlText = "";
            DataTable dt;
            DataTable dt2;

            int count_ls_err = 0;
            decimal g_sum_ls_err = 0;
            if (!(finder.nzp_user > 0))
                return new ReturnsType(false, "Не задан пользователь", -1);
            /*
            if ((finder.dat_uchet.Trim() != "") && !DateTime.TryParse(finder.dat_uchet, out datUchet))
                return new ReturnsType(false, "Неверно задана дата начала периода", -1);
            if ((finder.dat_uchet_po.Trim() != "") && !DateTime.TryParse(finder.dat_uchet_po, out datUchetPo))
                return new ReturnsType(false, "Неверно задана дата окончания периода", -1);

            if (datUchet == DateTime.MinValue)
                return new ReturnsType(false, "Не задана дата начала периода", -1);
            else if ((datUchet.ToString("MMyyyy") != Points.DateOper.ToString("MMyyyy"))
                || (datUchetPo.ToString("MMyyyy") != Points.DateOper.ToString("MMyyyy")))
                return new ReturnsType(false, "Период должен быть в текущем расчетном месяце", -1);
            #endregion
            */
            datUchet = new DateTime(Points.DateOper.Year, Points.DateOper.Month, 1);
            datUchetPo = datUchet.AddMonths(1).AddDays(-1);


            DbCalcCharge Calc = new DbCalcCharge();
            Returns ret;
            foreach (_Point point in Points.PointList)
            {
                // Проверить ограничения на Управляющую организацию
                if ((finder.dopPointList != null) && (finder.dopPointList.Count > 0))
                {
                    bool isFind = false;
                    foreach (Int32 nzpWp in finder.dopPointList)
                        if (point.nzp_wp == nzpWp)
                        {
                            isFind = true;
                            continue;
                        }
                    if (!isFind)
                        continue;
                }

                /*
                #if PG
                                string tableSupplier = point.pref + "_charge_" + (datUchet.ToString("yyyy").Substring(2, 2)) + "." + "fn_supplier" + datUchet.Month.ToString("00");
                #else
                                string tableSupplier = point.pref + "_charge_" + (datUchet.ToString("yyyy").Substring(2, 2)) + tableDelimiter + "fn_supplier" + datUchet.Month.ToString("00");
                #endif
                                */
                string tableSupplier = point.pref + "_charge_" + (datUchet.ToString("yyyy").Substring(2, 2)) + tableDelimiter + "fn_supplier" + datUchet.Month.ToString("00");
                string baseFin = Points.Pref + "_fin_" + datUchet.ToString("yyyy").Substring(2, 2);
                DataRow rows2;
#if PG
                sqlText =
                    " SELECT nzp_pack_ls, num_ls, SUM(sum_prih) as sum_prih  " +
                    " FROM " + tableSupplier +
                    " WHERE nzp_pack_ls not in ( select nzp_pack_ls from " + baseFin + ".pack_ls  WHERE dat_uchet between '" + Convert.ToDateTime(datUchet).ToShortDateString() + "' and '" + Convert.ToDateTime(datUchetPo).ToShortDateString() + "' AND inbasket = 0 and coalesce(cast(alg as int),0)<>0) " +
                    " group by 1,2";
#else
                sqlText =
                    " SELECT  nzp_pack_ls, num_ls, SUM(sum_prih) as sum_prih  " +
                    " FROM " + tableSupplier +
                    " WHERE  nzp_pack_ls not in ( select nzp_pack_ls from " + baseFin + ":pack_ls  WHERE dat_uchet between '" + Convert.ToDateTime(datUchet).ToShortDateString() + "' and '" + Convert.ToDateTime(datUchetPo).ToShortDateString() + "' AND inbasket = 0 and NVL(alg,0)<>0) " +
                    " group by 1,2";
#endif
                dt = ClassDBUtils.OpenSQL(sqlText, IDbConnection).GetData();
                foreach (DataRow rows in dt.Rows)
                {
                    if (rows["nzp_pack_ls"] != null)
                    {
                        g_sum_ls_err += Convert.ToDecimal(rows["sum_prih"]);
                        count_ls_err += 1;

                        sqlText = "insert into " + Points.Pref + "_data" + tableDelimiter + "sys_events(date_, nzp_user, nzp_dict_event, nzp, note) values (" + sCurDateTime + "," + finder.nzp_user + "," + 6612 + "," + rows["nzp_pack_ls"] + ",'" + "Анулирование распределения по квитанции по лс  " + rows["num_ls"] + " на сумму " + rows["sum_prih"] + "') ";
                        ClassDBUtils.ExecSQL(sqlText, IDbConnection);

                        sqlText = "DELETE FROM " + tableSupplier + " WHERE nzp_pack_ls = " + rows["nzp_pack_ls"];
                        ClassDBUtils.ExecSQL(sqlText, IDbConnection);

                        sqlText = " SELECT nzp_kvar, nzp_dom from  " + point.pref + "_data" + tableDelimiter + "kvar where num_ls = " + rows["num_ls"];

                        dt2 = ClassDBUtils.OpenSQL(sqlText, IDbConnection).GetData();
                        rows2 = dt2.Rows[0];

                        ret = Calc.CalcChargeXXUchetOplatForLs(IDbConnection, null, new Charge() { nzp_kvar = Convert.ToInt32(rows2["nzp_kvar"]), pref = point.pref, year_ = datUchet.Year, month_ = datUchet.Month }, CalcTypes.FunctionType.Payment);
                        if (!ret.result)
                        {
                            Calc.Close();
                            return new ReturnsType(ret.result, ret.text, ret.tag);
                        }

                        string fn_operday_dom;
                        string first_day = "01." + Points.DateOper.Month.ToString("00") + "." + Points.DateOper.Year;
#if PG
                        fn_operday_dom = Points.Pref + "_fin_" + (Points.DateOper.Year - 2000).ToString("00") + ".fn_operday_dom_mc";
#else
                        fn_operday_dom = Points.Pref + "_fin_" + (Points.DateOper.Year - 2000).ToString("00") + ":fn_operday_dom_mc";
#endif

                        ret = ExecSQL(IDbConnection, " insert into  " + fn_operday_dom + "(nzp_dom, date_oper) values (" + Convert.ToInt32(rows2["nzp_dom"]) + ",'" + first_day + "' ) ", true);

                    }
                }
            }
            Calc.Close();

            if (count_ls_err > 0)
            {
                return new ReturnsType(false, "В указанном периоде обнаружено " + count_ls_err + " шт. недопустимых оплат на сумму " + g_sum_ls_err.ToString("N2") + ". Недопустимые оплаты анулированы !", -1);
            }
            else
                return new ReturnsType(true, "В указанном периоде необнаружены недопустимые оплаты !", -1);


            /*
            DataTable lQuery;
            DataTable Q_Circle;
            string s;    
            string to_supp;
            string to_supp2;    
            string lsum;
            Decimal lsum_total;
            Int32 lCount;
            
            Int32 lsum_total = 0;
            bool CurT_Start__yes_err;
            
            //??? string g_alias__a_finans = TBLPlace.GetTypePlace(ttFinans,get_year(g_fin.dat_oper));
            string g_alias__a_finans = Points.Pref + "_fin_" + Points.DateOper.ToString("yyyy").Substring(2, 2);
            
            // if MessageDlg('НАЧАТЬ ПОИСК ОШИБОК В ТАБЛИЦАХ ОПЛАТ ЗА ОПЕРДЕНЬ : '''+DateToStr(g_fin.dat_oper)+''''

            lQuery:=TQuery.Create(Application);
            lQuery.DataBaseName:=DM_globall.DataBS.DatabaseName;
            lQuery.ParamCheck:=false;


            Q_Circle:=TQuery.Create(Application);
            Q_Circle.DataBaseName:=DM_globall.DataBS.DatabaseName;
            Q_Circle.ParamCheck:=false;

            Q_Circle.Close;
            Q_Circle.SQL.Text:=' Select b.* From '+
                                g_alias.a_kernel+'logtodb lb, '+
                                g_alias.a_kernel+'s_logicdblist l, '+
                                g_alias.a_kernel+'s_baselist b '+
                                ' Where l.ldbname="'+DM_globall.SLogicDbName+'" '+
                                '   and lb.nzp_ldb=l.nzp_ldb'+
                                '   and b.nzp_bl=lb.nzp_bl'+
                                '   and idtype=1 and yearr='+IntToStr(get_year(g_fin.dat_oper));
            Q_Circle.Prepare;
            Q_Circle.Open;

            Application.CreateForm(TRepGaugeForm, RepGaugeForm);
            if Q_Circle.RecordCount > 0 then
            begin
                RepGaugeForm.Gauge.MaxValue:=Q_Circle.RecordCount+1;
            end else
            begin
                RepGaugeForm.Gauge.MaxValue:=2;
            end;
            RepGaugeForm.Show;
            RepGaugeForm.ShowTime:=true;
            lCount:=0;
            while not Q_Circle.Eof do
            begin
                    lCount:=lCount+1;
                    RepGaugeForm.ShowProgress(lCount);
                    Application.ProcessMessages;

                    RepGaugeForm.Show;
                    RepGaugeForm.ShowTime:=true;

                    to_supp:= 'fn_supplier'+Format('%.2d',[get_month(g_fin.dat_oper)]);
                    to_supp2:= 'fn_pd_'+Format('%.2d',[get_month(g_fin.dat_oper)]);

                    s:=':';
                    if trim(Q_Circle.FieldByName('dbserver').AsString) <> '' then
                    s:='@'+trim(Q_Circle.FieldByName('dbserver').AsString)+':';

                    to_supp:=trim(Q_Circle.FieldByName('dbname').AsString)+s+to_supp;
                    to_supp2:=trim(Q_Circle.FieldByName('dbname').AsString)+s+to_supp2;

                    RepGaugeForm.Caption:='Поиск в банке: '+to_supp;
                    lQuery.Close;
                    lQuery.SQL.Text:='SELECT SUM(sum_prih) FROM '+trim(to_supp)+' WHERE dat_uchet = '''+DateToStr(g_fin.dat_oper)+''' '+
                                    'AND nzp_pack_ls not in ( SELECT a.nzp_pack_ls FROM '+
                                    g_alias.a_finans+ 'pack_ls a '+' WHERE a.dat_uchet = '''+DateToStr(g_fin.dat_oper)+''' AND a.inbasket = 0 and a.incase = 0)';
                    try
                        lQuery.Open;
                        lsum:=lQuery.Fields[0].AsString;
                        lsum_total:=lsum_total+lQuery.Fields[0].AsFloat;

                        if lQuery.Fields[0].AsFloat > 0 then
                        begin
                                    lQuery.Close;
                                    lQuery.SQL.Text:='SELECT num_ls,dat_month,nzp_pack_ls,SUM(sum_prih) AS sum_ FROM '+trim(to_supp)+' WHERE dat_uchet = '''+DateToStr(g_fin.dat_oper)+''' '+
                                                    'AND nzp_pack_ls not in ( SELECT a.nzp_pack_ls FROM '+
                                                    g_alias.a_finans+ 'pack_ls a '+' WHERE a.dat_uchet = '''+DateToStr(g_fin.dat_oper)+''' AND a.inbasket = 0 AND a.incase = 0  AND a.kod_sum <> 54 ) '+
                                                    ' GROUP BY 1,2,3 ';
                                    lQuery.Open;

                                    CurT_Start.yes_err:=true;
                                    CurT_Start.WriteToError('',false);
                                    CurT_Start.WriteToError('',false);
                                    CurT_Start.WriteToError(AddCharR(' ','|---------------------------------------------------------------------------'     ,76)+'|',false);
                                    CurT_Start.WriteToError(AddCharR(' ','| ОБНАРУЖЕНЫ ОШИБКИ В ОПЛАТАХ за опердень : '+dateToStr(g_fin.dat_oper)           ,76)+'|',false);
                                    CurT_Start.WriteToError(AddCharR(' ','|                             банк данных : '+to_supp        ,76)+'|',false);
                                    CurT_Start.WriteToError(AddCharR(' ','|---------------------------------------------------------------------------'     ,76)+'|',false);


                                    while not lQuery.eof do
                                    begin
                                        CurT_Start.WriteToError(AddCharR(' ','| Номер лицевого счета: '+lQuery.FieldByName('num_ls').AsString  ,76)+'|',false);
                                        CurT_Start.WriteToError(AddCharR(' ','| Месяц расчёта       : '+lQuery.FieldByName('dat_month').AsString  ,76)+'|',false);
                                        CurT_Start.WriteToError(AddCharR(' ','| Код записи          : '+lQuery.FieldByName('nzp_pack_ls').AsString   ,76)+'|',false);
                                        CurT_Start.WriteToError(AddCharR(' ','| Сумма оплаты        : '+lQuery.FieldByName('sum_').AsString  ,76)+'|',false);
                                        CurT_Start.WriteToError(AddCharR(' ','|---------------------------------------------------------------------------'   ,76)+'|',false);
                                        lQuery.Next;
                                    end;

                            CurT_Start.WriteToError('',false);
                            if MessageDlg('ВИНИМАНИЕ !!! в банке '+to_supp+#10#13+
                                            ' обнаружены неучтённые оплаты на сумму '+lsum+#10#13+
                                            'УДАЛИТЬ ИХ ? ',mtConfirmation, [mbYes,mbNo],0) = mrYes then
                            begin


                                    lQuery.Close;
                                    lQuery.SQL.Text:='DELETE FROM '+trim(to_supp)+' WHERE dat_uchet = '''+DateToStr(g_fin.dat_oper)+''' '+
                                                    'AND nzp_pack_ls not in ( SELECT a.nzp_pack_ls FROM '+
                                                    g_alias.a_finans+ 'pack_ls a '+' WHERE a.dat_uchet = '''+DateToStr(g_fin.dat_oper)+''' AND a.inbasket = 0 )';
                                    lQuery.ExecSQL;
                            end;

                            if _switch('elabuga',1) = 1  then  // (Если распределение в Казани)
                            begin

                            // -- 54 -------------------------------------------------------------------------------------
                                    lQuery.Close;
                                    lQuery.SQL.Text:='SELECT num_ls,dat_month,nzp_pack_ls,SUM(sum_prih) AS sum_ FROM '+trim(to_supp2)+' WHERE dat_uchet = '''+DateToStr(g_fin.dat_oper)+''' '+
                                                    'AND nzp_pack_ls not in ( SELECT a.nzp_pack_ls FROM '+
                                                    g_alias.a_finans+ 'pack_ls a '+' WHERE a.dat_uchet = '''+DateToStr(g_fin.dat_oper)+''' AND a.inbasket = 0 AND a.incase = 0  AND a.kod_sum = 54 ) '+
                                                    ' GROUP BY 1,2,3 ';
                                    lQuery.Open;

                                    if lQuery.Fields[0].AsInteger > 0 then
                                    begin
                                        CurT_Start.yes_err:=true;
                                        CurT_Start.WriteToError('',false);
                                        CurT_Start.WriteToError('',false);
                                        CurT_Start.WriteToError(AddCharR(' ','|---------------------------------------------------------------------------' ,76)+'|',false);
                                        CurT_Start.WriteToError(AddCharR(' ','| ОБНАРУЖЕНЫ ОШИБКИ В ОПЛАТАХ за опердень : '+dateToStr(g_fin.dat_oper)       ,76)+'|',false);
                                        CurT_Start.WriteToError(AddCharR(' ','|                             банк данных : '+to_supp2        ,76)+'|',false);
                                        CurT_Start.WriteToError(AddCharR(' ','|---------------------------------------------------------------------------' ,76)+'|',false);

                                        lsum:=lQuery.Fields[0].AsString;
                                        lsum_total:=0;


                                        while not lQuery.eof do
                                        begin
                                            CurT_Start.WriteToError(AddCharR(' ','| Номер лицевого счета: '+lQuery.FieldByName('num_ls').AsString  ,76)+'|',false);
                                            CurT_Start.WriteToError(AddCharR(' ','| Месяц расчёта       : '+lQuery.FieldByName('dat_month').AsString    ,76)+'|',false);
                                            CurT_Start.WriteToError(AddCharR(' ','| Код записи          : '+lQuery.FieldByName('nzp_pack_ls').AsString  ,76)+'|',false);
                                            CurT_Start.WriteToError(AddCharR(' ','| Сумма оплаты        : '+lQuery.FieldByName('sum_').AsString   ,76)+'|',false);
                                            CurT_Start.WriteToError(AddCharR(' ','|---------------------------------------------------------------------------'   ,76)+'|',false);
                                            lQuery.Next;
                                            lsum_total:=lsum_total+lQuery.FieldByName('sum_').AsFloat;
                                        end;


                                        CurT_Start.WriteToError('',false);
                                        if MessageDlg('ВИНИМАНИЕ !!! в банке '+to_supp2+#10#13+
                                                        ' обнаружены неучтённые оплаты на сумму '+FloatToStr(lsum_total)+#10#13+
                                                        'УДАЛИТЬ ИХ ? ',mtConfirmation, [mbYes,mbNo],0) = mrYes then
                                        begin


                                                lQuery.Close;
                                                lQuery.SQL.Text:='DELETE FROM '+trim(to_supp2)+' WHERE dat_uchet = '''+DateToStr(g_fin.dat_oper)+''' '+
                                                                'AND nzp_pack_ls not in ( SELECT a.nzp_pack_ls FROM '+
                                                                g_alias.a_finans+ 'pack_ls a '+' WHERE a.dat_uchet = '''+DateToStr(g_fin.dat_oper)+''' AND a.inbasket = 0 )';
                                                lQuery.ExecSQL;
                                        end;
                                    end;
                            end;

                        end;
                        Application.ProcessMessages;

                        RepGaugeForm.Show;
                        RepGaugeForm.ShowTime:=true;
                    except
                        ShowMessage('НЕОБНАРУЖЕН БАНК '+to_supp);
                    end;

                    Q_Circle.Next;
                    Application.ProcessMessages;

                    RepGaugeForm.Show;
                    RepGaugeForm.ShowTime:=true;
            //               RepGaugeForm.Free;
            end;
            RepGaugeForm.Free;          
            

            if CurT_Start.yes_err=true then
            begin
                    CurT_Start.WriteToError(AddCharR(' ','|'              ,76)+'|',false);
                    CurT_Start.WriteToError(AddCharR(' ','|---------------------------------------------------------------------------'              ,76)+'|',false);
                    CurT_Start.WriteToError(AddCharR(' ','| ИТОГО Сумма оплаты        : '+floatToStr(lsum_total)  ,76)+'|',false);
                    CurT_Start.WriteToError(AddCharR(' ','|---------------------------------------------------------------------------'              ,76)+'|',false);

                    ShowMessage('ОШИБОЧНЫЕ ЛИЦЕВЫЕ СЧЕТА БЫЛИ УКЗАНЫ В ЖУРНАЛЕ !!!'+ #10#13+ 
                                '(смотри файл ошибок "Error.log")');
            end;
            

            ReturnsType ret = new ReturnsType(true,"",-1);
            return ret;*/
        }

        private Pack LoadPack(Pack finder, out Returns ret, IDbConnection conn_web, IDbTransaction transaction)
        {

#if PG
            ret = ExecSQL(conn_web, " set search_path to 'public'", true);
#else
            ret = ExecSQL(conn_web, " set encryption password '" + BasePwd + "'", true);
#endif
            if (!ret.result)
            {
                return null;
            }

            string sql = "";
#if PG
            sql = "Select p.nzp_pack, p.nzp_bank, p.num_pack, p.dat_pack, p.count_kv, p.sum_pack, p.flag, p.erc_code, p.nzp_user, decrypt_char(u.uname) as uname, p.file_name" +
                             " From pack p left outer join users u on  p.nzp_user = u.nzp_user" +
                             " Where 1=1";
#else
            sql = "Select p.nzp_pack, p.nzp_bank, p.num_pack, p.dat_pack, p.count_kv, p.sum_pack, p.flag, p.erc_code, p.nzp_user, decrypt_char(u.uname) as uname, p.file_name" +
                 " From pack p, outer users u" +
                 " Where p.nzp_user = u.nzp_user";
#endif
            if (finder.nzp_pack > 0) sql += " and nzp_pack = " + finder.nzp_pack;
            else sql += " and p.nzp_user = " + finder.nzp_user + " and (p.flag = " + Pack.Statuses.Open.GetHashCode().ToString() + " or p.flag is null)";

            IDataReader reader;
            ret = ExecRead(conn_web, transaction, out reader, sql, true);
            if (!ret.result)
            {
                return null;
            }

            Pack pack = new Pack();
            try
            {
                if (reader.Read())
                {
                    if (reader["nzp_pack"] != DBNull.Value) pack.nzp_pack = Convert.ToInt32(reader["nzp_pack"]);
                    if (reader["nzp_bank"] != DBNull.Value) pack.nzp_bank = Convert.ToInt32(reader["nzp_bank"]);
                    if (reader["num_pack"] != DBNull.Value) pack.num_pack = Convert.ToInt32(reader["num_pack"]);
                    if (reader["num_pack"] != DBNull.Value) pack.num = pack.num_pack.ToString();
                    if (reader["dat_pack"] != DBNull.Value) pack.dat_pack = Convert.ToDateTime(reader["dat_pack"]).ToShortDateString();
                    if (reader["count_kv"] != DBNull.Value) pack.count_kv = Convert.ToInt32(reader["count_kv"]);
                    if (reader["sum_pack"] != DBNull.Value) pack.sum_pack = Convert.ToDecimal(reader["sum_pack"]);
                    if (reader["flag"] != DBNull.Value) pack.flag = Convert.ToInt32(reader["flag"]);
                    if (reader["erc_code"] != DBNull.Value) pack.erc_code = Convert.ToString(reader["erc_code"]).Trim();
                    if (reader["nzp_user"] != DBNull.Value) pack.nzp_user = Convert.ToInt32(reader["nzp_user"]);
                    if (reader["uname"] != DBNull.Value) pack.webUname = Convert.ToString(reader["uname"]).Trim();
                    if (reader["file_name"] != DBNull.Value) pack.file_name = Convert.ToString(reader["file_name"]).Trim();
                }
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                pack = null;

                MonitorLog.WriteLog("Ошибка LoadPack " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
            }

            if (reader != null) reader.Close();

            return pack;
        }

        public Pack LoadPack(Pack finder, out Returns ret)
        {
            #region Проверка входных параметров
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Пользователь не определен");
                return null;
            }
            #endregion

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;

            Pack pack = LoadPack(finder, out ret, conn_web, null);
            if (!ret.result)
            {
                conn_web.Close();
                return null;
            }

            conn_web.Close();

            if (pack.nzp_bank > 0)
            {
                Bank bank = new Bank();
                bank.nzp_bank = pack.nzp_bank;
                Returns ret2;
                List<Bank> banks = LoadBankForKassa(bank, out ret2);
                if (ret2.result && banks != null && banks.Count > 0)
                    pack.bank = banks[0].bank;
            }

            return pack;
        }

        public Pack_ls LoadKassaPackLs(Pack_ls finder, out Returns ret)
        {
            #region Проверка входных параметров
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Пользователь не определен");
                return null;
            }

            if (finder.nzp_pack_ls < 1)
            {
                ret = new Returns(false, "Неверные входные параметры");
                return null;
            }
            #endregion

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;

#if PG
            ret = ExecSQL(conn_web, " set search_path to 'public'", true);
#else
            ret = ExecSQL(conn_web, " set encryption password '" + BasePwd + "'", true);
#endif
            if (!ret.result)
            {
                conn_web.Close();
                return null;
            }

            string sql = "";
#if PG
            sql = "Select p.nzp_pack_ls, p.nzp_pack, p.prefix_ls, p.nzp_kvar, p.pkod, p.pref, p.g_sum_ls, p.sum_ls, p.sum_peni, p.dat_month, p.kod_sum, p.paysource, p.id_bill" +
                  " , p.dat_vvod, p.info_num, p.unl, p.erc_code, p.nzp_user, decrypt_char(u.uname) as uname From pack_ls p left outer join users u on p.nzp_user = u.nzp_user" +
                  " Where p.nzp_pack_ls = " + finder.nzp_pack_ls;
#else
            sql = "Select p.nzp_pack_ls, p.nzp_pack, p.prefix_ls, p.nzp_kvar, p.pkod, p.pref, p.g_sum_ls, p.sum_ls, p.sum_peni, p.dat_month, p.kod_sum, p.paysource, p.id_bill" +
                 " , p.dat_vvod, p.info_num, p.unl, p.erc_code, p.nzp_user, decrypt_char(u.uname) as uname From pack_ls p, outer users u" +
                 " Where p.nzp_user = u.nzp_user and p.nzp_pack_ls = " + finder.nzp_pack_ls;
#endif

            IDataReader reader;
            ret = ExecRead(conn_web, out reader, sql, true);
            if (!ret.result)
            {
                conn_web.Close();
                return null;
            }

            Pack_ls packls = new Pack_ls();
            try
            {
                if (reader.Read())
                {
                    packls.nzp_pack_ls = finder.nzp_pack_ls;
                    if (reader["nzp_pack"] != DBNull.Value) packls.nzp_pack = Convert.ToInt32(reader["nzp_pack"]);
                    if (reader["prefix_ls"] != DBNull.Value) packls.prefix_ls = Convert.ToInt32(reader["prefix_ls"]);
                    if (reader["nzp_kvar"] != DBNull.Value) packls.nzp_kvar = Convert.ToInt32(reader["nzp_kvar"]);
                    if (reader["pref"] != DBNull.Value) packls.pref = Convert.ToString(reader["pref"]).Trim();
                    if (reader["g_sum_ls"] != DBNull.Value) packls.g_sum_ls = Convert.ToDecimal(reader["g_sum_ls"]);
                    if (reader["sum_ls"] != DBNull.Value) packls.sum_ls = Convert.ToDecimal(reader["sum_ls"]);
                    if (reader["sum_peni"] != DBNull.Value) packls.sum_peni = Convert.ToDecimal(reader["sum_peni"]);
                    if (reader["dat_month"] != DBNull.Value) packls.dat_month = Convert.ToDateTime(reader["dat_month"]).ToShortDateString();
                    if (reader["kod_sum"] != DBNull.Value) packls.kod_sum = Convert.ToInt32(reader["kod_sum"]);
                    if (reader["paysource"] != DBNull.Value) packls.paysource = Convert.ToInt32(reader["paysource"]);
                    if (reader["id_bill"] != DBNull.Value) packls.id_bill = Convert.ToInt32(reader["id_bill"]);
                    if (reader["dat_vvod"] != DBNull.Value) packls.dat_vvod = Convert.ToDateTime(reader["dat_vvod"]).ToShortDateString();
                    if (reader["info_num"] != DBNull.Value) packls.info_num = Convert.ToInt32(reader["info_num"]);
                    if (reader["unl"] != DBNull.Value) packls.unl = Convert.ToInt32(reader["unl"]);
                    if (reader["erc_code"] != DBNull.Value) packls.erc_code = Convert.ToString(reader["erc_code"]).Trim();
                    if (reader["nzp_user"] != DBNull.Value) packls.nzp_user = Convert.ToInt32(reader["nzp_user"]);
                    if (reader["uname"] != DBNull.Value) packls.webUname = Convert.ToString(reader["uname"]).Trim();
                    if (reader["pkod"] != DBNull.Value) packls.pkod = Convert.ToString(reader["pkod"]).Trim();
                }
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                packls = null;

                MonitorLog.WriteLog("Ошибка LoadPackLs " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
            }

            if (reader != null) reader.Close();
            conn_web.Close();

            return packls;
        }

        private string GetErcCode(out Returns ret, IDbConnection conn_db, IDbTransaction transaction)
        {
            string sql = "";
#if PG
            sql = "select erc_code from " + Points.Pref + "_kernel.s_erc_code where is_current = 1";
#else
            sql = "select erc_code from " + Points.Pref + "_kernel:s_erc_code where is_current = 1";
#endif
            IDataReader reader;
            ret = ExecRead(conn_db, transaction, out reader, sql, true);
            if (!ret.result)
            {
                return "";
            }
            string erc_code = "";
            if (reader.Read())
                if (reader["erc_code"] != DBNull.Value) erc_code = Convert.ToString(reader["erc_code"]).Trim();
            reader.Close();
            return erc_code;
        }

        public string GetErcCode(out Returns ret, IDbConnection conn_db)
        {
            return GetErcCode(out ret, conn_db, null);
        }

        private string GetErcCode(out Returns ret)
        {
            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return "";

            string erc_code = GetErcCode(out ret, conn_db);

            conn_db.Close();
            return erc_code;
        }


        private void SavePack(Pack finder, out Returns ret, IDbConnection conn_web, IDbTransaction transaction)
        {
            string sql;
            IDataReader reader;
            if (finder.nzp_pack > 0)
            {
                // проверка, является ли пользователь создателем этой пачки
                sql = "select nzp_user from pack where nzp_pack = " + finder.nzp_pack;
                ret = ExecRead(conn_web, transaction, out reader, sql, true);
                if (!ret.result)
                {
                    return;
                }
                if (reader.Read())
                {
                    if (reader["nzp_user"] != DBNull.Value)
                    {
                        if (Convert.ToInt32(reader["nzp_user"]) != finder.nzp_user)
                        {
                            reader.Close();
                            ret = new Returns(false, "Данная пачка принадлежит другому пользователю, редактирование запрещено", -1);
                            return;
                        }
                    }
                    else
                    {
                        reader.Close();
                        ret = new Returns(false, "Пачка не найдена", -1);
                        return;
                    }
                }
                reader.Close();

                #region Добавление в sys_events события 'Изменение пачки оплат'
                try
                {
                    string connectionString = Points.GetConnByPref(Points.Pref);
                    IDbConnection conn_db = GetConnection(connectionString);
                    OpenDb(conn_db, true);

                    //получение старых значений
                    var changed_fields = "";
                    ret = ExecRead(conn_web, transaction, out reader, "select * from pack where nzp_pack = " + finder.nzp_pack, true);
                    while (reader.Read())
                    {
                        /* нет там поля dat_uchet
                           if (reader["dat_uchet"] != DBNull.Value && Convert.ToDateTime(reader["dat_uchet"]).ToShortDateString() != finder.dat_uchet.ToString())
                             changed_fields += "Дата учета: c " + Convert.ToDateTime(reader["dat_uchet"]).ToShortDateString() + " на " + finder.dat_uchet + ". ";
                        */
                        if (reader["dat_pack"] != DBNull.Value && Convert.ToDateTime(reader["dat_pack"]).ToShortDateString() != finder.dat_pack.ToString())
                            changed_fields += "Дата пачки: c " + Convert.ToDateTime(reader["dat_pack"]).ToShortDateString() + " на " + finder.dat_pack + ". ";
                        if (reader["num_pack"] != DBNull.Value && reader["num_pack"].ToString().Trim() != finder.snum_pack.ToString())
                            changed_fields += "Номер пачки: c " + reader["num_pack"].ToString() + " на " + finder.snum_pack + ". ";
                    }

                    DbAdmin.InsertSysEvent(new SysEvents()
                    {
                        pref = Points.Pref,
                        nzp_user = finder.nzp_user,
                        nzp_dict = 6498,
                        nzp_obj = finder.nzp_pack,
                        note = changed_fields != "" ? "Были изменены следующие поля: " + changed_fields : "Пачка оплат была изменена"
                    }, conn_db);
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка добавления события в sys_events" + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                }
                #endregion
#if PG
                sql = "update pack set count_kv = (select count(*) from pack_ls where nzp_pack = " + finder.nzp_pack + ")" +
                      ", sum_pack = (select coalesce(sum(g_sum_ls),0) from pack_ls where nzp_pack = " + finder.nzp_pack + ")";
#else
                sql = "update pack set count_kv = (select count(*) from pack_ls where nzp_pack = " + finder.nzp_pack + ")" +
                    ", sum_pack = (select nvl(sum(g_sum_ls),0) from pack_ls where nzp_pack = " + finder.nzp_pack + ")";
#endif
                if (finder.dat_pack != "")
                {
                    DateTime d = Convert.ToDateTime(finder.dat_pack);
                    sql += ", dat_pack = " + MDY(d.Month, d.Day, d.Year);
                }
                sql += " where nzp_pack = " + finder.nzp_pack;
                ret = ExecSQL(conn_web, transaction, sql, true);
            }
            else
            {
                // проверить наличие открытых пачек пользователя
                Pack pack = LoadPack(finder, out ret, conn_web, transaction);
                if (!ret.result)
                {
                    return;
                }
                if (pack != null && pack.nzp_pack > 0)
                {
                    ret = new Returns(false, "Имеется открытая пачка №" + pack.num_pack + " от " + pack.dat_pack + ". Перед созданием новой пачки необходимо закрыть открытые пачки", -1);
                    return;
                }

                // определить номер новой пачки
                sql = "select max(num_pack) as num_pack from pack where nzp_bank = " + finder.nzp_bank;
                ret = ExecRead(conn_web, transaction, out reader, sql, true);
                if (!ret.result)
                {
                    return;
                }
                if (reader.Read())
                {
                    if (reader["num_pack"] != DBNull.Value) finder.num_pack = Convert.ToInt32(reader["num_pack"]) + 1;
                }
                reader.Close();

                if (finder.num_pack < 1) finder.num_pack = 1;

                //определить код ЕРЦ
                string erc_code = GetErcCode(out ret);
                if (!ret.result)
                {
                    return;
                }
                if (erc_code.Trim() == "")
                {
                    ret = new Returns(false, "Не удалось определить код управляющей компании", -1);
                    return;
                }
                string type_oplat = "";
                string val = "";
                if (finder.nzp_payer_contragent > 0)
                {
                    type_oplat = ",nzp_payer";
                    val = "," + finder.nzp_payer_contragent;
                }
                if (finder.nzp_supp > 0)
                {
                    type_oplat = ",nzp_supp ";
                    val = "," + finder.nzp_supp;
                }

                DateTime d2 = DateTime.Today;
#if PG
                sql = "insert into pack ( nzp_bank, num_pack, dat_pack, count_kv, sum_pack, flag, erc_code, nzp_user" + type_oplat + ")" +
                                        " values ( " + finder.nzp_bank + ", " + finder.num_pack + ", " + MDY(d2.Month, d2.Day, d2.Year) +
                                        ", 0, 0, " + Pack.Statuses.Open.GetHashCode() + "," + Utils.EStrNull(erc_code, "") + "," + finder.nzp_user + val + ")";
#else
                sql = "insert into pack (nzp_pack, nzp_bank, num_pack, dat_pack, count_kv, sum_pack, flag, erc_code, nzp_user"+type_oplat+")" +
                        " values (0, " + finder.nzp_bank + ", " + finder.num_pack + ", " + Utils.EStrNull(DateTime.Today.ToShortDateString()) +
                        ", 0, 0, " + Pack.Statuses.Open.GetHashCode() + "," + Utils.EStrNull(erc_code, "") + "," + finder.nzp_user + val+")";
#endif
                ret = ExecSQL(conn_web, transaction, sql, true);
                if (!ret.result)
                {
                    return;
                }
                finder.nzp_pack = GetSerialValue(conn_web, transaction);

                #region Добавление в sys_events события 'Добавление пачки оплат'
                try
                {
                    string connectionString = Points.GetConnByPref(Points.Pref);
                    IDbConnection conn_db = GetConnection(connectionString);
                    OpenDb(conn_db, true);

                    DbAdmin.InsertSysEvent(new SysEvents()
                    {
                        pref = Points.Pref,
                        nzp_user = finder.nzp_user,
                        nzp_dict = 6499,
                        nzp_obj = finder.nzp_pack,
                        note = "Номер пачки: " + finder.num_pack
                    }, conn_db);
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                }
                #endregion
            }
        }

        public Pack SavePack(Pack finder, out Returns ret)
        {
            #region Проверка входных параметров
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Пользователь не определен");
                return null;
            }

            if (finder.nzp_pack < 1 && finder.nzp_bank < 1)
            {
                ret = new Returns(false, "Неверные входные параметры");
                return null;
            }
            #endregion

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;

            SavePack(finder, out ret, conn_web, null);
            if (!ret.result)
            {
                conn_web.Close();
                return null;
            }

            Pack pack = LoadPack(finder, out ret, conn_web, null);

            conn_web.Close();

            return pack;
        }

        public Pack_ls SaveKassaPackLs(Pack_ls finder, out Returns ret)
        {
            #region Проверка входных параметров
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Пользователь не определен");
                return null;
            }
            if (finder.nzp_pack_ls < 1 && finder.nzp_pack < 1)
            {
                ret = new Returns(false, "Неверные входные параметры");
                return null;
            }
            if (finder.nzp_kvar < 1)
            {
                ret = new Returns(false, "Не выбран лицевой счет", -1);
                return null;
            }
            if (finder.pref == "")
            {
                ret = new Returns(false, "Не задан префикс базы данных", -1);
                return null;
            }
            /* if (finder.g_sum_ls <= 0)
             {
                 ret = new Returns(false, "Сумма оплаты должна быть положительной", -1);
                 return null;
             }*/
            if (finder.dat_month == "")
            {
                ret = new Returns(false, "Не задан месяц, за который начислена оплата", -1);
                return null;
            }
            #endregion

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return null;


#if PG
            ret = ExecSQL(conn_web, "set search_path to 'public'", true);
#endif
            string sql;
            IDataReader reader;
            if (finder.nzp_pack_ls > 0) // редактирование оплаты
            {
                sql = "select nzp_pack from pack_ls where nzp_pack_ls = " + finder.nzp_pack_ls;
                ret = ExecRead(conn_web, out reader, sql, true);
                if (!ret.result)
                {
                    conn_web.Close();
                    return null;
                }
                if (reader.Read())
                {
                    if (reader["nzp_pack"] != DBNull.Value) finder.nzp_pack = Convert.ToInt32(reader["nzp_pack"]);
                }
                reader.Close();

                if (finder.nzp_pack < 1)
                {
                    conn_web.Close();
                    ret = new Returns(false, "Пачка не найдена.", -1);
                    return null;
                }
            }

            IDbTransaction transaction;
            try { transaction = conn_web.BeginTransaction(); }
            catch { transaction = null; }

            DateTime d = Convert.ToDateTime(finder.dat_month);
            DateTime d2 = Convert.ToDateTime(finder.dat_vvod);

            if (finder.nzp_pack_ls > 0)
            {
                #region Добавление в sys_events события 'Изменение оплаты'
                try
                {
                    string connectionString = Points.GetConnByPref(Points.Pref);
                    IDbConnection conn_db = GetConnection(connectionString);
                    OpenDb(conn_db, true);

                    //получение старых значений
                    var changed_fields = "";
                    ret = ExecRead(conn_web, transaction, out reader, "select * from pack_ls where nzp_pack_ls = " + finder.nzp_pack_ls, true);
                    while (reader.Read())
                    {
                        if (reader["g_sum_ls"] != DBNull.Value && reader["g_sum_ls"].ToString().Trim() != finder.g_sum_ls.ToString())
                            changed_fields += "Сумма оплаты: c " + reader["g_sum_ls"].ToString().Trim() + " на " + finder.g_sum_ls + ". ";
                        if (reader["sum_ls"] != DBNull.Value && reader["sum_ls"].ToString().Trim() != finder.sum_ls.ToString())
                            changed_fields += "Сумма ЛС: c " + reader["sum_ls"].ToString().Trim() + " на " + finder.sum_ls + ". ";
                        if (reader["dat_month"] != DBNull.Value && Convert.ToDateTime(reader["dat_month"]).ToShortDateString() != finder.dat_month.ToString())
                            changed_fields += "Дата: c " + reader["dat_month"].ToString().Trim() + " на " + finder.dat_month + ". ";
                    }

                    DbAdmin.InsertSysEvent(new SysEvents()
                    {
                        pref = Points.Pref,
                        nzp_user = finder.nzp_user,
                        nzp_dict = 6488,
                        nzp_obj = finder.nzp_pack_ls,
                        note = changed_fields != "" ? "Были изменены следующие поля: " + changed_fields : "Оплата была изменена"
                    }, conn_db);
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog("Ошибка добавления события в sys_events" + ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                }
                #endregion

                sql = "update pack_ls set prefix_ls = " + finder.prefix_ls +
                    ", nzp_kvar = " + finder.nzp_kvar +
                    ", pref = " + Utils.EStrNull(finder.pref) +
                    ", g_sum_ls = " + finder.g_sum_ls +
                    ", sum_ls = " + finder.sum_ls +
                    ", dat_month = " + MDY(d.Month, d.Day, d.Year) +
                    ", kod_sum = 33" +
                    (GlobalSettings.NewGeneratePkodMode ? ", pkod = " + finder.pkod : "") +
                    ", unl = -1" +
                    ", dat_vvod = " + MDY(d2.Month, d2.Day, d2.Year) +
                    " where nzp_pack_ls = " + finder.nzp_pack_ls;
            }
            else // добавление новой оплаты
            {
                #region определить код ЕРЦ
                string erc_code = GetErcCode(out ret);
                if (!ret.result)
                {
                    if (transaction != null) transaction.Rollback();
                    conn_web.Close();
                    return null;
                }
                if (erc_code.Trim() == "")
                {
                    ret = new Returns(false, "Не удалось определить код управляющей компании", -1);
                    conn_web.Close();
                    return null;
                }
                #endregion

                #region определить номер новой пачки
                sql = "select max(info_num) as info_num from pack_ls where nzp_pack = " + finder.nzp_pack;
                ret = ExecRead(conn_web, transaction, out reader, sql, true);
                if (!ret.result)
                {
                    if (transaction != null) transaction.Rollback();
                    conn_web.Close();
                    return null;
                }
                if (reader.Read())
                {
                    if (reader["info_num"] != DBNull.Value) finder.info_num = Convert.ToInt32(reader["info_num"]) + 1;
                }
                reader.Close();

                if (finder.info_num < 1) finder.info_num = 1;
                #endregion

#if PG
                sql = "insert into pack_ls (nzp_pack, dat_vvod, info_num, erc_code, nzp_user, prefix_ls, nzp_kvar, pref, g_sum_ls, sum_ls, dat_month, kod_sum, unl" +
                    (GlobalSettings.NewGeneratePkodMode ? ", pkod " : "") + ")" +
                                        " values (" + finder.nzp_pack +
                                        ", " + MDY(d2.Month, d2.Day, d2.Year) +
                                        ", " + finder.info_num +
                                        ", " + Utils.EStrNull(erc_code, "") +
                                        ", " + finder.nzp_user +
                                        ", " + finder.prefix_ls +
                                        ", " + finder.nzp_kvar +
                                        ", " + Utils.EStrNull(finder.pref.Trim()) +
                                        ", " + finder.g_sum_ls +
                                        ", " + finder.sum_ls +
                                        ", " + MDY(d.Month, d.Day, d.Year) +
                                        ", 33" +
                                        ", -1" +
                                        (GlobalSettings.NewGeneratePkodMode ? ", " + finder.pkod : "")
                                        + ")";
#else
                sql = "insert into pack_ls (nzp_pack_ls, nzp_pack, dat_vvod, info_num, erc_code, nzp_user, prefix_ls, nzp_kvar, pref, g_sum_ls, sum_ls, dat_month, kod_sum, unl)" +
                                        " values (0, " + finder.nzp_pack +
                                        ", " + Utils.EStrNull(finder.dat_vvod) +
                                        ", " + finder.info_num +
                                        ", " + Utils.EStrNull(erc_code, "") +
                                        ", " + finder.nzp_user +
                                        ", " + finder.prefix_ls +
                                        ", " + finder.nzp_kvar +
                                        ", " + Utils.EStrNull(finder.pref.Trim()) +
                                        ", " + finder.g_sum_ls +
                                        ", " + finder.sum_ls +
                                        ", " + Utils.EStrNull(finder.dat_month) +
                                        ", 33" +
                                        ", -1)";
#endif
            }

            ret = ExecSQL(conn_web, transaction, sql, true);
            if (!ret.result)
            {
                if (transaction != null) transaction.Rollback();
                conn_web.Close();
                return null;
            }

            if (finder.nzp_pack_ls < 1)
            {
                finder.nzp_pack_ls = GetSerialValue(conn_web, transaction);

                #region Добавление в sys_events события 'Добавление оплаты'
                try
                {
                    string connectionString = Points.GetConnByPref(Points.Pref);
                    IDbConnection conn_db = GetConnection(connectionString);
                    OpenDb(conn_db, true);

                    DbAdmin.InsertSysEvent(new SysEvents()
                    {
                        pref = Points.Pref,
                        nzp_user = finder.nzp_user,
                        nzp_dict = 6486,
                        nzp_obj = finder.nzp_pack_ls,
                        note = "Сумма оплаты " + finder.g_sum_ls
                    }, conn_db);
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                }
                #endregion
            }

            Pack pack = new Pack() { nzp_user = finder.nzp_user, nzp_pack = finder.nzp_pack };
            SavePack(pack, out ret, conn_web, transaction); // обновить пачку

            if (!ret.result)
            {
                if (transaction != null) transaction.Rollback();
                conn_web.Close();
                return null;
            }

            if (transaction != null) transaction.Commit();

            conn_web.Close();

            Pack_ls packls = LoadKassaPackLs(finder, out ret);

            return packls;
        }

        public Returns DeleteKassaPackLs(Pack_ls finder)
        {
            #region Проверка входных параметров
            if (finder.nzp_user < 1) return new Returns(false, "Пользователь не определен");
            if (finder.nzp_pack_ls < 1) return new Returns(false, "Неверные входные параметры");
            #endregion

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            Returns ret = OpenDb(conn_web, true);
            if (!ret.result) return ret;

#if PG
            ret = ExecSQL(conn_web, "set search_path to 'public'", true);
#endif

            // проверим, что пачка не выгружена
            string sql = "select p.nzp_pack, p.flag from pack p, pack_ls pl where p.nzp_pack = pl.nzp_pack and pl.nzp_pack_ls = " + finder.nzp_pack_ls;
            IDataReader reader;
            ret = ExecRead(conn_web, out reader, sql, true);
            if (!ret.result)
            {
                conn_web.Close();
                return ret;
            }
            int flag = 0;
            if (reader.Read())
            {
                if (reader["flag"] != DBNull.Value) flag = Convert.ToInt32(reader["flag"]);
                if (reader["nzp_pack"] != DBNull.Value) finder.nzp_pack = Convert.ToInt32(reader["nzp_pack"]);
            }
            reader.Close();

            if (finder.nzp_pack < 1)
            {
                conn_web.Close();
                return new Returns(false, "Пачка не найдена.", -1);
            }

            if (flag != Pack.Statuses.Open.GetHashCode())
            {
                conn_web.Close();
                return new Returns(false, "Пачка закрыта. Удаление оплаты из закрытой пачки запрещено.", -1);
            }

            IDbTransaction transaction;
            try { transaction = conn_web.BeginTransaction(); }
            catch { transaction = null; }

            sql = "delete from pack_ls where nzp_pack_ls = " + finder.nzp_pack_ls;
            ret = ExecSQL(conn_web, transaction, sql, true);
            if (!ret.result)
            {
                if (transaction != null) transaction.Rollback();
                conn_web.Close();
                return ret;
            }

            Pack pack = new Pack() { nzp_user = finder.nzp_user, nzp_pack = finder.nzp_pack };
            SavePack(pack, out ret, conn_web, transaction); // обновить пачку

            if (!ret.result)
            {
                if (transaction != null) transaction.Rollback();
                conn_web.Close();
                return ret;
            }

            if (transaction != null) transaction.Commit();
            conn_web.Close();

            #region Добавление в sys_events события 'Удаление оплаты'
            try
            {
                string connectionString = Points.GetConnByPref(Points.Pref);
                IDbConnection conn_db = GetConnection(connectionString);
                OpenDb(conn_db, true);

                DbAdmin.InsertSysEvent(new SysEvents()
                {
                    pref = Points.Pref,
                    nzp_user = finder.nzp_user,
                    nzp_dict = 6487,
                    nzp_obj = finder.nzp_pack_ls,
                    note = "Оплата была успешно удалена"
                }, conn_db);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
            }
            #endregion

            return ret;
        }

        public Returns CheckPAckStatus(Pack_ls finder)
        {
            if (finder.nzp_pack <= 0 && finder.nzp_pack_ls <= 0)
            {
                return new Returns(false, "Не указаны ревкизиты пачки", -1);
            }
            if (finder.year_ <= 0)
            {
                return new Returns(false, "Не указан финансовый год пачки", -1);
            }

            var connDb = GetConnection(Constants.cons_Kernel);
            var ret = OpenDb(connDb, true);
            if (!ret.result) return ret;

            var sql = new StringBuilder();
            if (finder.nzp_pack > 0) 
                sql.AppendFormat("({0})", finder.nzp_pack);
            else if (finder.nzp_pack_ls > 0)
                sql.AppendFormat("(select distinct nzp_pack from {0}_fin_{1}{2}pack_ls where nzp_pack_ls  = {3})",
                    Points.Pref, (finder.year_%100).ToString("00"), tableDelimiter, finder.nzp_pack_ls);
            else return new Returns(false, "Не указаны реквизиты пачки", -1);

            var sb = new StringBuilder();
            sb.AppendFormat("select flag from {0}_fin_{1}{2}pack where nzp_pack in {3}", Points.Pref,
                (finder.year_%100).ToString("00"), tableDelimiter, sql);
            var obj = ExecScalar(connDb, sb.ToString(), out ret, true);
            if (!ret.result)
            {
                connDb.Close();
                return ret;
            }
            int flag = 0;
            Int32.TryParse(obj.ToString(), out flag);
            if (flag == Pack.Statuses.WaitingForCancellationOfDistribution.GetHashCode() ||
                flag == Pack.Statuses.WaitingForDistribution.GetHashCode())
            {
                return new Returns(false, "Операции с пачкой невозможны, т.к. пачка в находится режиме ожидания обработки", -1);
            }
            return new Returns(true);
        }

        public Returns DistributeOrCancelPack(Pack_ls finder, Pack_ls.OperationsWithoutGetting oper, bool background)
        {
            Returns ret = CheckPAckStatus(finder);
            if (!ret.result) return ret;

            if (finder.nzp_pack_ls < 1)
            {
                return new Returns(false, "Неверные входные параметры");
            }
            if (finder.year_ < 1)
            {
                return new Returns(false, "Неверные входные параметры");
            }
            if (finder.dat_uchet == "")
            {
                return new Returns(false, "Неверные входные параметры");
            }
            DateTime dat;
            if (!DateTime.TryParse(finder.dat_uchet, out dat))
            {
                return new Returns(false, "Неверные входные параметры");
            }

          
            DbCalcPack db1 = new DbCalcPack();
            bool b = db1.CalcPackLs(finder.nzp_pack_ls, finder.year_, dat, oper == Pack_ls.OperationsWithoutGetting.Distribute, finder.is_manual, out ret, finder.nzp_user, finder.isCurrentMonth);  // Отдаем оплату на распределение

            if (finder.nzp_pack <= 0)
            {
                finder.year_ = Points.DateOper.Year;
                Returns ret2;
                List<Pack_ls> listPack_ls = FindFinancePackLs(finder, out ret2);
                if (ret2.result)
                {
                    if (listPack_ls != null && listPack_ls.Count > 0)
                    {
                        finder.nzp_pack = listPack_ls[0].nzp_pack;
                    }
                }
            }

            if (ret.result)
            {
                if (!b)
                {
                    ret.text = "Оплата не была распределена";
                    ret.tag = -1;
                }
                else
                {
                    if (background)
                    {
                        Returns ret2;
                        db1.PackFonTasks(finder.nzp_pack, finder.year_, CalcFonTask.Types.UpdatePackStatus, out ret2);  // Обновляем статус пачки
                    }
                    else
                    {
                        PackFinder pf = new PackFinder();
                        pf.nzp_user = 1;
                        pf.year_ = dat.Year;
                        pf.nzp_pack = finder.nzp_pack;

                        ReturnsType rt = Upd_SUM_RASP_and_SUM_NRASP(pf);
                    }
                }
            }

            db1.Close();

            return ret;
        }

        public Returns ClosePack(Pack finder)
        {
            #region Проверка входных параметров
            if (finder.nzp_user < 1) return new Returns(false, "Пользователь не определен");
            if (finder.nzp_pack < 1) return new Returns(false, "Неверные входные параметры");
            #endregion
            int countInsertedRows;
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            Returns ret = OpenDb(conn_web, true);
            if (!ret.result) return ret;

#if PG
            ret = ExecSQL(conn_web, "set search_path to 'public'", true);
#endif

            IDbTransaction transaction;
            try { transaction = conn_web.BeginTransaction(); }
            catch { transaction = null; }

            // удалить недоделанные оплаты
            string sql = "delete from pack_ls where nzp_pack = " + finder.nzp_pack + " and (nzp_kvar is null or nzp_kvar < 1)";
            ret = ExecSQL(conn_web, transaction, sql, true);
            if (!ret.result)
            {
                if (transaction != null) transaction.Rollback();
                conn_web.Close();
                return ret;
            }

            sql = "select nzp_pack_ls from pack_ls where nzp_pack = " + finder.nzp_pack;
            IDataReader reader;
            ret = ExecRead(conn_web, transaction, out reader, sql, true);
            if (!ret.result)
            {
                if (transaction != null) transaction.Rollback();
                conn_web.Close();
                return ret;
            }

            if (!reader.Read())
            {
                //удаление пустой пачки оплат
                reader.Close();

                sql = "delete from pack where nzp_pack = " + finder.nzp_pack;
                ret = ExecSQL(conn_web, transaction, sql, true);

                if (!ret.result)
                {
                    if (transaction != null) transaction.Rollback();
                    conn_web.Close();
                    return ret;
                }
                else
                {
                    ret.tag = -1;
                    ret.text = "Пачка не содержала ни одной оплаты и была удалена";
                }

                if (transaction != null) transaction.Commit();
            }
            else
            {
                // загрузка пачки оплат в основную БД

                sql = "update pack set flag = " + Pack.Statuses.ClosedButNotUnloaded.GetHashCode() +
                    ", file_name = 'kp5_'||nzp_bank||'_'||num_pack||'_'||dat_pack" +
                    " where nzp_pack = " + finder.nzp_pack;
                ret = ExecSQL(conn_web, transaction, sql, true);

                if (!ret.result)
                {
                    if (transaction != null) transaction.Rollback();
                    conn_web.Close();
                    return ret;
                }

                if (transaction != null) transaction.Commit();

                Pack pack = LoadPack(finder, out ret, conn_web, null);
                pack.year_ = Points.DateOper.Year;
                if (!ret.result)
                {
                    conn_web.Close();
                    return ret;
                }

                string connectionString = Points.GetConnByPref(Points.Pref);
                IDbConnection conn_db = GetConnection(connectionString);
                ret = OpenDb(conn_db, true);
                if (!ret.result)
                {
                    conn_web.Close();
                    return ret;
                }

                // проверить, что пачка не загружалась ранее
                if (IsPackExists(pack, conn_db, out ret))
                {
                    conn_db.Close();
                    conn_web.Close();
                    return new Returns(false, "Пачка закрылась, но не загрузилась в финансовый банк, т.к. пачка с таким номером, датой и местом формирования уже существует.", -1);
                }

                // получить список оплат пачки
                List<Pack_ls> list = LoadListPackLs(pack, out ret, conn_db, conn_web);
                if (!ret.result)
                {
                    conn_db.Close();
                    conn_web.Close();
                    return ret;
                }

                // загрузить пачки
                pack.num = pack.num_pack.ToString();
                int res = SavePack(pack, list, conn_db, out ret);
                if (res < 1)
                {
                    conn_db.Close();
                    conn_web.Close();
                    return new Returns(false, "Не удалось загрузить пачку оплат в финансовый банк", -1);
                }
                else
                {
                    sql = "update pack set flag = " + Pack.Statuses.NotDistributed.GetHashCode() + " where nzp_pack = " + finder.nzp_pack;
                    ret = ExecSQL(conn_web, sql, true);

                    //сразу распределить пачку
                    if (ret.result)
                    {
                        if (Points.DateOper.Month != Points.CalcMonth.month_ || Points.DateOper.Year != Points.CalcMonth.year_)
                        {
                            ret = new Returns(true, "Пачка оплат закрыта, но не распределена, т.к. операционный день не относится к текущему расчетному месяцу!", -1);
                        }
                        else
                        {
                            DbCalcPack db1 = new DbCalcPack();
                            Returns ret2;
                            db1.PackFonTasks(pack.nzp_pack, pack.nzp_pack, CalcFonTask.Types.DistributePack, out ret2);  // Отдаем пачку на распределение
                            db1.Close();

                            if (!ret2.result)
                            {
                                ret.result = false;
                                ret.tag = -1;
                                ret.text = "Пачка оплат закрыта, но не распределена." + (ret2.tag < 0 ? " " + ret2.text : "");
                            }
                        }
                    }
                }
                conn_db.Close();
            }
            conn_web.Close();
            return ret;
        }

        public Returns FindPackLsInCase(Pack_ls finder)
        {
            if (finder.nzp_user <= 0) return new Returns(false, "Пользователь не определен", -1);

            string tXX_case = "t" + Convert.ToString(finder.nzp_user) + "_case";
            IDataReader reader = null;
            List<int> years = new List<int>();
#if PG
            string tXX_case_full = "public." + tXX_case;
#else
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            Returns ret1 = OpenDb(conn_web, true);
            if (!ret1.result) return ret1;
            string tXX_case_full = DBManager.GetFullBaseName(conn_web) + ":" + tXX_case;
            conn_web.Close();
#endif
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            Returns ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;

            ExecSQL(conn_db, "drop table " + tXX_case_full, false);
            StringBuilder sql = new StringBuilder("create table " + tXX_case_full);
            sql.Append("(nzp serial not null, nzp_pack_ls integer, year_ integer, num_pack character(10), dat_pack date, nzp_pack integer, mark integer, " +
                "g_sum_ls decimal(10,2), sum_ls decimal(10,2), dat_month date, dat_vvod date, dat_uchet date, kod_sum smallint, info_num integer, " +
                "num_ls integer, nkvar character(10), nkvar_n character(10), fio character(50), nzp_dom integer, ndom character(10), nkor character(3), nzp_ul integer, ulica character(40), " +
                "ulicareg character(100), rajon character(30), town character(30), spkod character(20), nzp_kvar integer, pref character(20), alg character(2), inbasket smallint)");
            ret = ExecSQL(conn_db, sql.ToString(), true);
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }

            sql.Remove(0, sql.Length);
            sql.AppendFormat("select yearr from {0}_kernel{1}s_baselist where idtype = 4 order by yearr", Points.Pref, tableDelimiter);
            ret = ExecRead(conn_db, out reader, sql.ToString(), true);
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }
            while (reader.Read()) if (reader["yearr"] != DBNull.Value) years.Add(Convert.ToInt32(reader["yearr"]));

            CloseReader(ref reader);

            foreach (int year in years)
            {
                string table = Points.Pref + "_fin_" + year.ToString("0000").Substring(2, 2) + tableDelimiter + "pack_ls";
                if (!TempTableInWebCashe(conn_db, table)) continue;

                string tablepack = Points.Pref + "_fin_" + year.ToString("0000").Substring(2, 2) + tableDelimiter + "pack";
                if (!TempTableInWebCashe(conn_db, table)) continue;

                sql.Remove(0, sql.Length);
                sql.AppendFormat("INSERT INTO {0} (nzp_pack_ls, nzp_pack, num_pack, dat_pack, year_, mark, " +
                            "g_sum_ls, sum_ls, dat_month, dat_uchet, dat_vvod, kod_sum, info_num, spkod, " +
                            "num_ls, nkvar, nkvar_n, fio, nzp_dom, ndom, nkor, nzp_ul, ulica, ulicareg, rajon, town, nzp_kvar, pref, alg, inbasket) " +
                   " select pls.nzp_pack_ls, pls.nzp_pack, p.num_pack, p.dat_pack, {1},1, pls.g_sum_ls, pls.sum_ls, pls.dat_month, pls.dat_uchet, pls.dat_vvod, pls.kod_sum, " +
                   " pls.info_num, round(k.pkod)||'' as spkod, k.num_ls, k.nkvar, k.nkvar_n, k.fio, d.nzp_dom, d.ndom, d.nkor, u.nzp_ul, u.ulica, " +
                   " trim({2}(u.ulicareg,'улица')) ulicareg, r.rajon, t.town, k.nzp_kvar, k.pref, pls.alg, pls.inbasket " +
                   " From  {3} p , {4} pls " +
#if PG
 " left outer join {5} " +
                   " left outer join {6} " +
                   " left outer join {7} " +
                   " left outer join {8} " +
                   " left outer join {9} " +
                   " on t.nzp_town = r.nzp_town on u.nzp_raj = r.nzp_raj  on d.nzp_ul = u.nzp_ul on k.nzp_dom = d.nzp_dom on pls.num_ls = k.num_ls " +
                   " Where 1=1 " +
#else                                 
                   " , outer ({4}, {5}, {6}, {7}, {8}) " +
                   " Where pls.num_ls = k.num_ls and k.nzp_dom = d.nzp_dom and d.nzp_ul = u.nzp_ul  and r.nzp_town = t.nzp_town and r.nzp_raj = u.nzp_raj " +
#endif
 " and pls.nzp_pack = p.nzp_pack and pls.incase = {10}",
                tXX_case_full, year, sNvlWord, tablepack, table, Points.Pref + "_data" + tableDelimiter + "kvar k ", Points.Pref + "_data" + tableDelimiter + "dom d ",
                Points.Pref + "_data" + tableDelimiter + "s_ulica u ", Points.Pref + "_data" + tableDelimiter + "s_rajon r ",
                Points.Pref + "_data" + tableDelimiter + "s_town t ", finder.incase);

                ret = ExecSQL(conn_db, sql.ToString(), true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return ret;
                }
            }

            conn_db.Close();
            return ret;
        }

        public List<Pack_ls> GetPackLsInCase(Pack_ls finder, out Returns ret)
        {
            if (finder.nzp_user <= 0)
            {
                ret = new Returns(false, "Пользователь не определен", -1);
                return null;
            }

            string tXX_case = "t" + Convert.ToString(finder.nzp_user) + "_case";
            IDataReader reader = null, readerpack = null;
            List<int> years = new List<int>();
#if PG
            string tXX_case_full = "public." + tXX_case;
#else
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result)
            {
                
                return null;
            }
            string tXX_case_full =  DBManager.GetFullBaseName(conn_web) + ":" + tXX_case;
            conn_web.Close();
#endif
            List<Pack_ls> list = new List<Pack_ls>();

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            if (!TempTableInWebCashe(conn_db, tXX_case_full))
            {
                ret = FindPackLsInCase(finder);
                if (!ret.result) return null;
            }

            StringBuilder sql = new StringBuilder();
            sql.AppendFormat("select  year_, dat_pack, num_pack, nzp_pack from {0} t group by  year_, dat_pack, num_pack, nzp_pack " +
                            "Order by  dat_pack, num_pack, nzp_pack", tXX_case_full);
            ret = ExecRead(conn_db, out readerpack, sql.ToString(), true);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }
            while (readerpack.Read())
            {
                Pack_ls packls = new Pack_ls();
                if (readerpack["nzp_pack"] != DBNull.Value) packls.nzp_pack = Convert.ToInt32(readerpack["nzp_pack"]);
                if (readerpack["year_"] != DBNull.Value) packls.year_ = Convert.ToInt32(readerpack["year_"]);
                if (readerpack["num_pack"] != DBNull.Value) packls.snum_pack = Convert.ToString(readerpack["num_pack"]).Trim();
                if (readerpack["dat_pack"] != DBNull.Value) packls.dat_pack = Convert.ToDateTime(readerpack["dat_pack"]).ToShortDateString();
                list.Add(packls);

                sql.Remove(0, sql.Length);
                sql.Append("select * from " + tXX_case_full + " where year_ = " + packls.year_ +
                    " and nzp_pack = " + packls.nzp_pack +

                    " Order by dat_pack, nzp_pack, dat_uchet, info_num, fio");
                ret = ExecRead(conn_db, out reader, sql.ToString(), true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return null;
                }
                try
                {

                    while (reader.Read())
                    {
                        packls = new Pack_ls();
                        if (reader["nzp"] != DBNull.Value) packls.nzp = Convert.ToInt32(reader["nzp"]);
                        if (reader["year_"] != DBNull.Value) packls.year_ = Convert.ToInt32(reader["year_"]);
                        if (reader["mark"] != DBNull.Value) packls.mark = Convert.ToInt32(reader["mark"]);
                        if (reader["nzp_kvar"] != DBNull.Value) packls.nzp_kvar = Convert.ToInt32(reader["nzp_kvar"]);
                        if (reader["num_ls"] != DBNull.Value) packls.num_ls = Convert.ToInt32(reader["num_ls"]);
                        if (reader["pref"] != DBNull.Value) packls.pref = Convert.ToString(reader["pref"]).Trim();

                        if (reader["nzp_pack"] != DBNull.Value) packls.nzp_pack = Convert.ToInt32(reader["nzp_pack"]);
                        if (reader["nzp_pack_ls"] != DBNull.Value) packls.nzp_pack_ls = Convert.ToInt32(reader["nzp_pack_ls"]);
                        //      if (reader["prefix_ls"] != DBNull.Value) packls.prefix_ls = Convert.ToInt32(reader["prefix_ls"]);
                        if (reader["g_sum_ls"] != DBNull.Value) packls.g_sum_ls = Convert.ToDecimal(reader["g_sum_ls"]);
                        if (reader["info_num"] != DBNull.Value) packls.info_num = Convert.ToInt32(reader["info_num"]);
                        if (reader["nzp_dom"] != DBNull.Value) packls.nzp_dom = Convert.ToInt32(reader["nzp_dom"]);
                        if (reader["nzp_ul"] != DBNull.Value) packls.nzp_ul = Convert.ToInt32(reader["nzp_ul"]);
                        if (reader["dat_vvod"] != DBNull.Value) packls.dat_vvod = Convert.ToDateTime(reader["dat_vvod"]).ToShortDateString();
                        if (reader["sum_ls"] != DBNull.Value) packls.sum_ls = Convert.ToDecimal(reader["sum_ls"]);
                        if (reader["kod_sum"] != DBNull.Value) packls.kod_sum = Convert.ToInt32(reader["kod_sum"]);
                        if (reader["nkvar"] != DBNull.Value) packls.nkvar = Convert.ToString(reader["nkvar"]).Trim();
                        if (reader["nkvar_n"] != DBNull.Value) packls.nkvar_n = Convert.ToString(reader["nkvar_n"]).Trim();
                        if (reader["fio"] != DBNull.Value) packls.fio = Convert.ToString(reader["fio"]).Trim();
                        if (reader["ndom"] != DBNull.Value) packls.ndom = Convert.ToString(reader["ndom"]).Trim();
                        if (reader["nkor"] != DBNull.Value) packls.nkor = Convert.ToString(reader["nkor"]).Trim();
                        if (reader["ulica"] != DBNull.Value) packls.ulica = Convert.ToString(reader["ulicareg"]) + " " +
                                                                                Convert.ToString(reader["ulica"]).Trim();
                        if (reader["rajon"] != DBNull.Value) packls.rajon = Convert.ToString(reader["rajon"]).Trim();
                        if (reader["town"] != DBNull.Value) packls.town = Convert.ToString(reader["town"]).Trim();
                        if (reader["alg"] != DBNull.Value) packls.alg = Convert.ToString(reader["alg"]).Trim();
                        if (reader["inbasket"] != DBNull.Value) packls.inbasket = Convert.ToInt32(reader["inbasket"]);
                        packls.adr = packls.ulica;
                        if (packls.adr.Trim() != "") packls.adr += " / ";
                        packls.adr += packls.rajon;
                        if (packls.adr.Trim() != "") packls.adr += " / ";
                        packls.adr += packls.town;
                        if (packls.ndom != "" && packls.ndom != "-") packls.adr += ", д. " + packls.ndom;
                        if (packls.nkor != "" && packls.nkor != "-") packls.adr += ", корп. " + packls.nkor;
                        if (packls.nkvar != "" && packls.nkvar != "-") packls.adr += ", кв. " + packls.nkvar;
                        if (packls.nkvar_n != "" && packls.nkvar_n != "-") packls.adr += ", комн. " + packls.nkvar_n;

                        if (reader["spkod"] != DBNull.Value)
                        {
                            packls.pkod = Convert.ToString(reader["spkod"]).Trim();
                            string prefix = packls.pkod.Substring(0, 3);
                            int iprefix;
                            if (Int32.TryParse(prefix, out iprefix)) packls.prefix_ls = iprefix;
                        }
                        if (reader["dat_month"] != DBNull.Value) packls.dat_month = Convert.ToDateTime(reader["dat_month"]).ToShortDateString();
                        if (packls.inbasket == 1) packls.status = "В корзине";
                        else if (packls.incase == 1) packls.status = "В портфеле";
                        else if (packls.dat_uchet != "" && (packls.alg != "" && packls.alg != "0") && packls.inbasket == 0)
                        {
                            packls.status = "Распределена";
                        }
                        else packls.status = "Не распределена";

                        list.Add(packls);


                    }
                }
                catch (Exception ex)
                {
                    readerpack.Close();
                    ret = new Returns(false, ex.Message);
                    list = null;
                    MonitorLog.WriteLog("Ошибка GetPackLsInCase " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                }

                reader.Close();
            }
            readerpack.Close();
            sql.Remove(0, sql.Length);
            sql.Append("Select count(*) From " + tXX_case_full);

            int count = 0;
            object obj = ExecScalar(conn_db, sql.ToString(), out ret, true);
            if (!ret.result) return null;
            try { count = Convert.ToInt32(obj); }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                return null;
            }

            if (ret.result) ret.tag = count;

            //if (list != null)
            //{
            //    if (finder.skip > 0 && list.Count > finder.skip) list.RemoveRange(0, finder.skip);
            //    if (finder.rows > 0 && list.Count > finder.rows) list.RemoveRange(finder.rows, list.Count - finder.rows);

            //}
            int i = 0;
            List<Pack_ls> listres = new List<Pack_ls>();
            if (list != null)
            {
                Pack_ls plstmp = null;
                foreach (Pack_ls pls in list)
                {
                    i++;
                    if (pls.snum_pack != "")
                    {
                        plstmp = pls;
                        i--;
                    }
                    if (plstmp != null)
                    {
                        if ((i - 1) % finder.rows == 0) if (pls.snum_pack == "" && pls.nzp_pack == plstmp.nzp_pack)
                            {
                                if (finder.skip > 0 && i <= finder.skip) continue;
                                if (i >= finder.skip + finder.rows) continue;
                                listres.Add(plstmp);
                            }
                    }

                    if (finder.skip > 0 && i <= finder.skip) continue;
                    if (i >= finder.skip + finder.rows) continue;
                    if (i != 0)
                        listres.Add(pls);

                }
            }
            if (reader != null) reader.Close();
            conn_db.Close();

            return listres;

        }

        public Returns PackLsInCaseChangeMark(Finder finder, List<Pack_ls> listChecked, List<Pack_ls> listUnchecked)
        {
            if (finder.nzp_user <= 0) return new Returns(false, "Пользователь не определен", -1);

            string tXX_case = "t" + Convert.ToString(finder.nzp_user) + "_case";
           // IDataReader reader = null;
           // List<int> years = new List<int>();
#if PG
            string tXX_case_full = "public." + tXX_case;
#else
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            Returns ret1 = OpenDb(conn_web, true);
            if (!ret1.result) return ret1;
            string tXX_case_full = DBManager.GetFullBaseName(conn_web) + ":" + tXX_case;
            conn_web.Close();
#endif
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            Returns ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;

            if (!TempTableInWebCashe(conn_db, tXX_case_full))
            {
                return new Returns(false, "Нет данных", -1);
            }

            string true_ = "";
            if (listChecked != null)
            {
                for (int i = 0; i < listChecked.Count; i++)
                {
                    if (i == 0) true_ += listChecked[i].nzp;
                    else true_ += "," + listChecked[i].nzp;
                }
                if (true_ != "") true_ = "(" + true_ + ")";
            }

            string false_ = "";
            if (listUnchecked != null)
            {
                for (int i = 0; i < listUnchecked.Count; i++)
                {
                    if (i == 0) false_ += listUnchecked[i].nzp;
                    else false_ += "," + listUnchecked[i].nzp;
                }
                if (false_ != "") false_ = "(" + false_ + ")";
            }

            StringBuilder sql = new StringBuilder();
            string where = "";
            int mark = -1;
            if (finder.dopFind != null && finder.dopFind.Count > 0)
            {
                if (finder.dopFind[0] == "checkall") mark = 1;
                else mark = 0;

                sql.Remove(0, sql.Length);
                sql.Append("update " + tXX_case_full + " set mark = " + mark);
                ret = ExecSQL(conn_db, sql.ToString(), true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return ret;
                }
            }

            if (true_ != "")
            {
                mark = 1;
                where = " and nzp in " + true_;
                sql.Remove(0, sql.Length);
                sql.Append("update " + tXX_case_full + " set mark = " + mark + " where 1=1 " + where);
                ret = ExecSQL(conn_db, sql.ToString(), true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return ret;
                }
            }
            if (false_ != "")
            {
                where = " and nzp in " + false_;
                mark = 0;
                sql.Remove(0, sql.Length);
                sql.Append("update " + tXX_case_full + " set mark = " + mark + " where 1=1 " + where);
                ret = ExecSQL(conn_db, sql.ToString(), true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return ret;
                }
            }


            conn_db.Close();
            return ret;
        }

        private List<Pack_ls> LoadListPackLs(Pack finder, out Returns ret, IDbConnection conn_db, IDbConnection conn_web)
        {
            string tablePackLs = "";
#if PG
            tablePackLs = "public.pack_ls";
#else
            tablePackLs = conn_web.Database + "@" + DBManager.getServer(conn_web) + ":pack_ls";
#endif

            string sql = "";
            string field = "";
            if (GlobalSettings.NewGeneratePkodMode) field = ", p.pkod spkod";
            else field = ", round(k.pkod)||'' as spkod";
            string fields = "";

            if (DBManager.isTableHasColumn(conn_db, "pack_ls", "type_pay", sDefaultSchema.Replace(".", "")))
            {
                fields = ",p.type_pay,p.month_from,p.month_to";
            }

#if PG
            sql = "Select p.nzp_pack_ls, p.nzp_pack, p.prefix_ls, p.nzp_kvar, p.pref, p.g_sum_ls, p.sum_ls, p.sum_peni, p.dat_month, p.kod_sum, p.paysource, p.id_bill" + fields +
                 ", p.dat_vvod, p.info_num, p.unl, p.erc_code, p.nzp_user" +
                 field + ", k.num_ls, k.nkvar, k.nkvar_n, k.fio, d.ndom, d.nkor, u.ulica" +
                 " From " + tablePackLs + " p, " + Points.Pref + "_data.kvar k, " + Points.Pref + "_data.dom d, " + Points.Pref + "_data.s_ulica u" +
                 " Where p.nzp_kvar = k.nzp_kvar and k.nzp_dom = d.nzp_dom and d.nzp_ul = u.nzp_ul and p.nzp_pack = " + finder.nzp_pack +
                 " Order by info_num" + (finder.sortby < 0 ? " desc" : "");
#else
            sql = "Select p.nzp_pack_ls, p.nzp_pack, p.prefix_ls, p.nzp_kvar, p.pref, p.g_sum_ls, p.sum_ls, p.sum_peni, p.dat_month, p.kod_sum, p.paysource, p.id_bill" + fields +
                 ", p.dat_vvod, p.info_num, p.unl, p.erc_code, p.nzp_user" +
                 field + ", k.num_ls, k.nkvar, k.nkvar_n, k.fio, d.ndom, d.nkor, u.ulica" +
                 " From " + tablePackLs + " p, " + Points.Pref + "_data:kvar k, " + Points.Pref + "_data:dom d, " + Points.Pref + "_data:s_ulica u" +
                 " Where p.nzp_kvar = k.nzp_kvar and k.nzp_dom = d.nzp_dom and d.nzp_ul = u.nzp_ul and p.nzp_pack = " + finder.nzp_pack +
                 " Order by info_num" + (finder.sortby < 0 ? " desc" : "");
#endif

            IDataReader reader;
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                return null;
            }

            List<Pack_ls> list = new List<Pack_ls>();

            try
            {
                while (reader.Read())
                {
                    Pack_ls packls = new Pack_ls();
                    if (reader["nzp_pack_ls"] != DBNull.Value) packls.nzp_pack_ls = Convert.ToInt32(reader["nzp_pack_ls"]);
                    if (reader["nzp_pack"] != DBNull.Value) packls.nzp_pack = Convert.ToInt32(reader["nzp_pack"]);

                    if (reader["spkod"] != DBNull.Value)
                    {
                        packls.pkod = Convert.ToString(reader["spkod"]).Trim();
                        string prefix = packls.pkod.Substring(0, 3);
                        int iprefix;
                        if (Int32.TryParse(prefix, out iprefix)) packls.prefix_ls = iprefix;
                    }

                    if (reader["nzp_kvar"] != DBNull.Value) packls.nzp_kvar = Convert.ToInt32(reader["nzp_kvar"]);
                    if (reader["num_ls"] != DBNull.Value) packls.num_ls = Convert.ToInt32(reader["num_ls"]);
                    if (reader["pref"] != DBNull.Value) packls.pref = Convert.ToString(reader["pref"]).Trim();
                    if (reader["g_sum_ls"] != DBNull.Value) packls.g_sum_ls = Convert.ToDecimal(reader["g_sum_ls"]);
                    if (reader["sum_ls"] != DBNull.Value) packls.sum_ls = Convert.ToDecimal(reader["sum_ls"]);
                    if (reader["sum_peni"] != DBNull.Value) packls.sum_peni = Convert.ToDecimal(reader["sum_peni"]);
                    if (reader["dat_month"] != DBNull.Value) packls.dat_month = Convert.ToDateTime(reader["dat_month"]).ToShortDateString();
                    if (reader["kod_sum"] != DBNull.Value) packls.kod_sum = Convert.ToInt32(reader["kod_sum"]);
                    if (reader["paysource"] != DBNull.Value) packls.paysource = Convert.ToInt32(reader["paysource"]);
                    if (reader["id_bill"] != DBNull.Value) packls.id_bill = Convert.ToInt32(reader["id_bill"]);
                    if (reader["dat_vvod"] != DBNull.Value) packls.dat_vvod = Convert.ToDateTime(reader["dat_vvod"]).ToShortDateString();
                    if (reader["info_num"] != DBNull.Value) packls.info_num = Convert.ToInt32(reader["info_num"]);
                    if (reader["unl"] != DBNull.Value) packls.unl = Convert.ToInt32(reader["unl"]);
                    if (reader["erc_code"] != DBNull.Value) packls.erc_code = Convert.ToString(reader["erc_code"]).Trim();
                    if (reader["nzp_user"] != DBNull.Value) packls.nzp_user = Convert.ToInt32(reader["nzp_user"]);

                    if (reader["nkvar"] != DBNull.Value) packls.nkvar = Convert.ToString(reader["nkvar"]).Trim();
                    if (reader["nkvar_n"] != DBNull.Value) packls.nkvar_n = Convert.ToString(reader["nkvar_n"]).Trim();
                    if (reader["fio"] != DBNull.Value) packls.fio = Convert.ToString(reader["fio"]).Trim();
                    if (reader["ndom"] != DBNull.Value) packls.ndom = Convert.ToString(reader["ndom"]).Trim();
                    if (reader["nkor"] != DBNull.Value) packls.nkor = Convert.ToString(reader["nkor"]).Trim();
                    if (reader["ulica"] != DBNull.Value) packls.ulica = Convert.ToString(reader["ulica"]).Trim();

                    if (fields.Length > 0)
                    {
                        if (reader["type_pay"] != DBNull.Value) packls.type_pay = Convert.ToInt32(reader["type_pay"]);
                        if (reader["month_from"] != DBNull.Value)
                            packls.month_from = Convert.ToDateTime(reader["month_from"]);
                        if (reader["month_to"] != DBNull.Value)
                            packls.month_to = Convert.ToDateTime(reader["month_to"]);
                    }

                    packls.adr = packls.ulica;
                    if (packls.ndom != "" && packls.ndom != "-") packls.adr += ", д. " + packls.ndom;
                    if (packls.nkor != "" && packls.nkor != "-") packls.adr += ", корп. " + packls.nkor;
                    if (packls.nkvar != "" && packls.nkvar != "-") packls.adr += ", кв. " + packls.nkvar;
                    if (packls.nkvar_n != "" && packls.nkvar_n != "-") packls.adr += ", комн. " + packls.nkvar_n;

                    list.Add(packls);
                }
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                list = null;

                MonitorLog.WriteLog("Ошибка LoadListPackLs " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
            }
            return list;
        }

        public List<Pack_ls> GetPackLs(Pack_ls finder, out Returns ret)
        {
            #region Проверка входных параметров
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Пользователь не определен");
                return null;
            }
            if (finder.nzp_pack < 1)
            {
                ret = new Returns(false, "Не задана пачка");
                return null;
            }
            #endregion

            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result)
            {
                return null;
            }

            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                conn_web.Close();
                return null;
            }

            List<Pack_ls> list = LoadListPackLs(finder, out ret, conn_db, conn_web);

            conn_db.Close();
            conn_web.Close();
            return list;
        }

        private string _FindPackAddCondition(PackFinder finder, int year)
        {
            string s = "";

            DateTime dat_vvod_s = DateTime.MinValue;
            DateTime dat_vvod_po = DateTime.MinValue;
            DateTime.TryParse(finder.dat_vvod_s, out dat_vvod_s);
            DateTime.TryParse(finder.dat_vvod_po, out dat_vvod_po);

            DateTime pls_dat_uchet = DateTime.MinValue;
            DateTime pls_dat_uchet_po = DateTime.MinValue;
            DateTime.TryParse(finder.pls_dat_uchet, out pls_dat_uchet);
            DateTime.TryParse(finder.pls_dat_uchet_po, out pls_dat_uchet_po);

            // Статус
            if (finder.flag > 0)
                s += " and p.flag = " + finder.flag.ToString();

            // Сумма пачки
            if (finder.sum_pack != Decimal.MinValue)
            {
                if (finder.sum_pack_po == Decimal.MinValue)
                    s += " and p.sum_pack = " + finder.sum_pack.ToString();
                else
                    s += " and p.sum_pack >= " + finder.sum_pack.ToString() + " and p.sum_pack <= " + finder.sum_pack_po.ToString();
            }

            if (finder.nzp_supp > 0)
            {
                s += " and p.nzp_supp = " + finder.nzp_supp;
            }

            if (finder.nzp_bank > 0)
            {
                s += " and p.nzp_bank = " + finder.nzp_bank;
            }

            if ((finder.num_ls > 0) || (dat_vvod_s != DateTime.MinValue) || (pls_dat_uchet != DateTime.MinValue) || (finder.g_sum_ls_s != Decimal.MinValue)
                || (finder.pkod.Trim() != "") || (finder.nzp_kvar > 0) || (finder.nzp_dom > 0) || (finder.nzp_ul > 0) || (finder.nzp_raj > 0) || (finder.nzp_town > 0) || finder.status_for_opl > 0 || finder.inbasket > 0 || finder.kod_sum > 0)
            {
#if PG
                s +=
                    " and exists ( " +
                    "     select 1 " +
                    "     from " + Points.Pref + "_fin_" + year.ToString("0000").Substring(2, 2) + ".pack_ls ls2, " + Points.Pref + "_data. kvar kv2 " +
                    "     where ls2.num_ls = kv2.num_ls and ls2.nzp_pack = P.NZP_PACK ";
#else
                s +=
                    " and 0 < ( " +
                    "     select count(*) " +
                    "     from " + Points.Pref + "_fin_" + year.ToString("0000").Substring(2, 2) + ":pack_ls ls2, " + Points.Pref + "_data: kvar kv2 " +
                    "     where ls2.num_ls = kv2.num_ls and ls2.nzp_pack = P.NZP_PACK ";
#endif

                // Лицевой счет
                if (finder.num_ls > 0)
                {
                    //if (!Points.IsSmr)
                        s += " and ls2.num_ls = " + finder.num_ls.ToString();
                    //else
                    //    s += " and kv2.pkod10 = " + finder.num_ls.ToString();
                }

                if (finder.inbasket > 0)
                {
                    if (finder.inbasket == PriznakBasket.InBasket.GetHashCode()) s += " and ls2.inbasket = 1 ";
                    else if (finder.inbasket == PriznakBasket.NotBasket.GetHashCode()) s += " and ls2.inbasket <> 1 ";
                }



                if (finder.status_for_opl > 0)
                {
                    if (finder.status_for_opl == PackLsOperations.Distribute.GetHashCode()) s += " and ls2.dat_uchet is not null ";
                    else if (finder.status_for_opl == PackLsOperations.NotDistribute.GetHashCode()) s += " and ls2.dat_uchet is null ";
                }

                // Дата оплаты
                if (dat_vvod_s != DateTime.MinValue)
                {
#if PG
                    if (dat_vvod_po == DateTime.MinValue)
                        s += " and ls2.dat_vvod = public.MDY(" + dat_vvod_s.ToString("MM,dd,yyyy") + ") ";
                    else
                        s += " and ls2.dat_vvod >= public.MDY(" + dat_vvod_s.ToString("MM,dd,yyyy") + ") and ls2.dat_vvod <= public.MDY(" + dat_vvod_po.ToString("MM,dd,yyyy") + ") ";
#else
                    if (dat_vvod_po == DateTime.MinValue)
                        s += " and ls2.dat_vvod = mdy(" + dat_vvod_s.ToString("MM,dd,yyyy") + ") ";
                    else
                        s += " and ls2.dat_vvod >= mdy(" + dat_vvod_s.ToString("MM,dd,yyyy") + ") and ls2.dat_vvod <= mdy(" + dat_vvod_po.ToString("MM,dd,yyyy") + ") ";
#endif
                }

                // Операционный день оплаты
                if (pls_dat_uchet != DateTime.MinValue)
                {
#if PG
                    if (pls_dat_uchet_po == DateTime.MinValue)
                        s += " and ls2.dat_uchet = public.MDY(" + pls_dat_uchet.ToString("MM,dd,yyyy") + ") ";
                    else
                        s += " and ls2.dat_uchet >= public.MDY(" + pls_dat_uchet.ToString("MM,dd,yyyy") + ") and ls2.dat_uchet <= public.MDY(" + pls_dat_uchet_po.ToString("MM,dd,yyyy") + ") ";
#else
                    if (pls_dat_uchet_po == DateTime.MinValue)
                        s += " and ls2.dat_uchet = mdy(" + pls_dat_uchet.ToString("MM,dd,yyyy") + ") ";
                    else
                        s += " and ls2.dat_uchet >= mdy(" + pls_dat_uchet.ToString("MM,dd,yyyy") + ") and ls2.dat_uchet <= mdy(" + pls_dat_uchet_po.ToString("MM,dd,yyyy") + ") ";
#endif
                }

                // Сумма оплаты
                if (finder.g_sum_ls_s != Decimal.MinValue)
                {
                    if (finder.g_sum_ls_po == Decimal.MinValue)
                        s += " and ls2.g_sum_ls = " + finder.g_sum_ls_s.ToString();
                    else
                        s += " and ls2.g_sum_ls >= " + finder.g_sum_ls_s.ToString() + " and ls2.g_sum_ls <= " + finder.g_sum_ls_po.ToString();
                }

                //код квитанции
                if (finder.kod_sum > 0)
                {
                    s += " and ls2.kod_sum = " + finder.kod_sum.ToString();
                }



                if ((finder.pkod.Trim() != "") || (finder.nzp_kvar > 0) || (finder.nzp_dom > 0) || (finder.nzp_ul > 0) || (finder.nzp_raj > 0) || (finder.nzp_town > 0))
                {
#if PG
                    s +=
                        " and exists ( " +
                        "     select 1 " +
                        "     from " + Points.Pref + "_data.kvar kv3, " +
                        ((GlobalSettings.NewGeneratePkodMode && finder.pkod.Trim() != "") ? Points.Pref + "_data" + tableDelimiter + "kvar_pkodes kp ," : "") +
                        Points.Pref + "_data.dom d3, " +Points.Pref + sDataAliasRest+"s_town t ,"+Points.Pref + sDataAliasRest+"s_rajon r ,"+
                        Points.Pref + sDataAliasRest+"s_ulica u "+
                        " where kv3.nzp_dom=d3.nzp_dom and kv3.num_ls = LS2.NUM_LS and d3.nzp_ul=u.nzp_ul and r.nzp_raj=u.nzp_raj and r.nzp_town=t.nzp_town ";
#else
                    s +=
                        " and 0 < ( " +
                        "     select count(*) " +
                        "     from " + Points.Pref + "_data:kvar kv3, " + Points.Pref + "_data:dom d3 " +
                        "     where kv3.nzp_dom=d3.nzp_dom and kv3.num_ls = LS2.NUM_LS ";
#endif

                    // Платежный код
                    if (finder.pkod.Trim() != "")
                    {
                        if (GlobalSettings.NewGeneratePkodMode) s += " and kp.nzp_kvar = kv3.nzp_kvar and kp.pkod = " + Utils.ELong0(finder.pkod.Trim());
                        else s += " and kv3.pkod = " + Utils.ELong0(finder.pkod.Trim());
                    }
                    // Квартира
                    if (finder.nzp_kvar > 0)
                        s += " and kv3.nzp_kvar = " + finder.nzp_kvar;
                    // Дом
                    if (finder.nzp_dom > 0)
                        s += " and kv3.nzp_dom = " + finder.nzp_dom;
                    // Улица
                    if (finder.nzp_ul > 0)
                        s += " and u.nzp_ul = " + finder.nzp_ul;
                    // Район
                    if (finder.nzp_raj > 0)
                        s += " and r.nzp_raj = " + finder.nzp_raj;
                    // Город
                    if (finder.nzp_town > 0)
                        s += " and t.nzp_town = " + finder.nzp_town;

                    s += " ) ";
                }

                s += " ) ";
            }

            return s;
        }

        /// <summary>
        /// Загрузить справочник статусов пачки
        /// </summary>
        public List<PackStatus> GetPackStatus(PackStatus finder, out Returns ret)
        {
            #region Проверка входных параметров
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Не определен пользователь");
                return null;
            }
            #endregion

            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            string sql = "";
#if PG
            sql = "select st.nzp_st, st.name_st from " + Points.Pref + "_kernel.s_status st " +
                "where st.kod_st <> 1 and st.nzp_st not in (14, 24) " +
                "order by st.kod_st, upper(st.name_st) ";
#else
            sql = "select st.nzp_st, st.name_st from " + Points.Pref + "_kernel:s_status st " +
                 "where st.kod_st <> 1 and st.nzp_st not in (14, 24) " +
                 "order by st.kod_st, upper(st.name_st) ";
#endif

            IDataReader reader;
            ret = ExecRead(conn_db, out reader, sql.ToString(), true);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }

            List<PackStatus> list = new List<PackStatus>();

            try
            {
                while (reader.Read())
                {
                    PackStatus zap = new PackStatus();
                    if (reader["nzp_st"] != DBNull.Value) zap.nzp_st = Convert.ToInt32(reader["nzp_st"]);
                    if (reader["name_st"] != DBNull.Value) zap.name_st = Convert.ToString(reader["name_st"]).Trim();
                    list.Add(zap);
                }
            }
            finally
            {
                if (reader != null) reader.Close();
                conn_db.Close(); //закрыть соединение с основной базой
            }
            return list;
        }

        public List<Pack> FindPack(PackFinder finder, out Returns ret)
        {
            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            List<Pack> list = FindPack(finder, out ret, conn_db);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }
            return list;
        }

        private List<Pack> FindPack(PackFinder finder, out Returns ret, IDbConnection conn_db)
        {
            #region Проверка входных параметров
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Пользователь не определен");
                return null;
            }
            DateTime dat_pack = DateTime.MinValue;
            DateTime dat_pack_po = DateTime.MinValue;
            DateTime dat_uchet = DateTime.MinValue;
            DateTime dat_uchet_po = DateTime.MinValue;
            DateTime pls_dat_uchet = DateTime.MinValue;
            DateTime pls_dat_uchet_po = DateTime.MinValue;
            DateTime dat_vvod_s = DateTime.MinValue;
            DateTime dat_vvod_po = DateTime.MinValue;

            int y1, y2;
            if (finder.year_ > 0 && (finder.nzp_pack > 0 || finder.par_pack > 0))
            {
                y1 = y2 = finder.year_;
            }
            else
            {
                if (finder.dat_pack != "" && !DateTime.TryParse(finder.dat_pack, out dat_pack))
                {
                    ret = new Returns(false, "Неверно задана дата пачки");
                    return null;
                }

                if (finder.dat_pack_po != "" && !DateTime.TryParse(finder.dat_pack_po, out dat_pack_po))
                {
                    ret = new Returns(false, "Неверно задана дата пачки");
                    return null;
                }

                if (finder.dat_uchet != "" && !DateTime.TryParse(finder.dat_uchet, out dat_uchet))
                {
                    ret = new Returns(false, "Неверно задана дата учета");
                    return null;
                }

                if (finder.dat_uchet_po != "" && !DateTime.TryParse(finder.dat_uchet_po, out dat_uchet_po))
                {
                    ret = new Returns(false, "Неверно задана дата учета");
                    return null;
                }

                if (finder.pls_dat_uchet != "" && !DateTime.TryParse(finder.pls_dat_uchet, out pls_dat_uchet))
                {
                    ret = new Returns(false, "Неверно задана дата учета оплаты");
                    return null;
                }

                if (finder.pls_dat_uchet_po != "" && !DateTime.TryParse(finder.pls_dat_uchet_po, out pls_dat_uchet_po))
                {
                    ret = new Returns(false, "Неверно задана дата учета оплаты");
                    return null;
                }

                if (dat_pack_po == DateTime.MinValue) dat_pack_po = dat_pack;
                if (dat_uchet_po == DateTime.MinValue) dat_uchet_po = dat_uchet;
                if (pls_dat_uchet_po == DateTime.MinValue) pls_dat_uchet_po = pls_dat_uchet;

                // Дата оплаты
                if ((finder.dat_vvod_s != "") && !DateTime.TryParse(finder.dat_vvod_s, out dat_vvod_s))
                {
                    ret = new Returns(false, "Неверно задана дата оплаты");
                    return null;
                }
                if (finder.dat_vvod_po != "" && !DateTime.TryParse(finder.dat_vvod_po, out dat_vvod_po))
                {
                    ret = new Returns(false, "Неверно задана дата оплаты");
                    return null;
                }

                y1 = 0;
                y2 = 0;
                if (dat_uchet != DateTime.MinValue || dat_uchet_po != DateTime.MinValue)
                {
                    y1 = dat_uchet.Year;
                    y2 = dat_uchet_po.Year;
                }
                if (pls_dat_uchet != DateTime.MinValue || pls_dat_uchet_po != DateTime.MinValue)
                {
                    y1 = pls_dat_uchet.Year;
                    y2 = pls_dat_uchet_po.Year;
                }
                else if (dat_pack != DateTime.MinValue || dat_pack_po != DateTime.MinValue)
                {
                    // год из даты пачки может не соответствовать году в имени БД
                    // например, пачка от 30.12.2011 сохранена в базе данных XX_fin_12:pack
                    // поэтому зададим период с запасом в 1 год
                    y1 = dat_pack.Year - 1;
                    y2 = dat_pack_po.Year + 1;
                }

                if (y1 < 1900 || y2 < 1900)
                {
                    ret = new Returns(false, "Для поиска необходимо ввести дату пачки или операционный день пачки или оплаты", -1);
                    return null;
                }
            }
            #endregion

            string sql;
            IDataReader reader = null, reader2 = null;
            List<int> years = new List<int>();

#if PG
            sql = "select yearr from " + Points.Pref + "_kernel.s_baselist where idtype = 4 and (yearr >= " + y1 + " and yearr <= " + y2 + ") order by yearr";
#else
            sql = "select yearr from " + Points.Pref + "_kernel:s_baselist where idtype = 4 and (yearr >= " + y1 + " and yearr <= " + y2 + ") order by yearr";
#endif
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result) return null;
            while (reader.Read())
            {
                if (reader["yearr"] != DBNull.Value) years.Add(Convert.ToInt32(reader["yearr"]));
            }
            CloseReader(ref reader);

            string table, where = "", table_pls = "";

            if (finder.nzp_pack > 0)
            {
                where += " and p.nzp_pack = " + finder.nzp_pack;
            }
            else if (finder.par_pack > 0)
            {
                where += " and p.par_pack = " + finder.par_pack + " and p.nzp_pack <> p.par_pack";
            }
            else
            {
                if (dat_pack != DateTime.MinValue)
                {
                    if (dat_pack == dat_pack_po) where += " and p.dat_pack = date(" + Utils.EStrNull(dat_pack.ToShortDateString()) + ")";
                    else where += " and p.dat_pack >= date(" + Utils.EStrNull(dat_pack.ToShortDateString()) + ") and p.dat_pack <= date(" + Utils.EStrNull(dat_pack_po.ToShortDateString()) + ")";
                }
                if (dat_uchet != DateTime.MinValue)
                {
#if PG
                    if (dat_uchet == dat_uchet_po) where += " and coalesce(p.dat_uchet,date(" + Utils.EStrNull(Points.DateOper.ToShortDateString()) + ")) = date(" + Utils.EStrNull(dat_uchet.ToShortDateString()) + ")";
                    else where += " and coalesce(p.dat_uchet,date(" + Utils.EStrNull(Points.DateOper.ToShortDateString()) + ")) >= date(" + Utils.EStrNull(dat_uchet.ToShortDateString()) + ")" +
                        " and coalesce(p.dat_uchet,date(" + Utils.EStrNull(Points.DateOper.ToShortDateString()) + ")) <= date(" + Utils.EStrNull(dat_uchet_po.ToShortDateString()) + ")";
#else
                    if (dat_uchet == dat_uchet_po) where += " and nvl(p.dat_uchet,date(" + Utils.EStrNull(Points.DateOper.ToShortDateString()) + ")) = date(" + Utils.EStrNull(dat_uchet.ToShortDateString()) + ")";
                    else where += " and nvl(p.dat_uchet,date(" + Utils.EStrNull(Points.DateOper.ToShortDateString()) + ")) >= date(" + Utils.EStrNull(dat_uchet.ToShortDateString()) + ")" +
                        " and nvl(p.dat_uchet,date(" + Utils.EStrNull(Points.DateOper.ToShortDateString()) + ")) <= date(" + Utils.EStrNull(dat_uchet_po.ToShortDateString()) + ")";
#endif
                }

                if (finder.nzp_payer > 0) where += " and b.nzp_payer = " + finder.nzp_payer; //ограничения по банку(источнику)
                if (finder.nzp_payer_contragent > 0) where += " and p.nzp_payer = " + finder.nzp_payer_contragent; //ограничения по контрагенту??
                else where += " and (p.par_pack is null or (p.par_pack is not null and p.par_pack <> p.nzp_pack))";
                if (finder.snum_pack != "") where += " and p.num_pack = " + Utils.EStrNull(finder.snum_pack);
                if (finder.pack_type > 0) where += " and p.pack_type = " + finder.pack_type;
                if (finder.file_name != "") where += " and p.file_name = " + Utils.EStrNull(finder.file_name);
            }

            List<Pack> list = new List<Pack>();
            DbTables tables = new DbTables(DBManager.getServer(conn_db));

            Pack totalPack = new Pack();
            int totalNumber = 0;
            string where2 = "";

            foreach (int year in years)
            {
                ExecSQL(conn_db, " Drop table tmp_nzp_packs ", false);
                ExecSQL(conn_db, " CREATE TEMP TABLE tmp_nzp_packs (nzp_pack integer, par_pack integer) ", false);
                ExecSQL(conn_db, " delete from tmp_nzp_packs ", false);

#if PG
                table = Points.Pref + "_fin_" + year.ToString("0000").Substring(2, 2) + ".pack";
#else
                table = Points.Pref + "_fin_" + year.ToString("0000").Substring(2, 2) + ":pack";
#endif
                if (!TempTableInWebCashe(conn_db, table)) continue;

#if PG
                table_pls = Points.Pref + "_fin_" + year.ToString("0000").Substring(2, 2) + ".pack_ls";
#else
                table_pls = Points.Pref + "_fin_" + year.ToString("0000").Substring(2, 2) + ":pack_ls";
#endif

                if (finder.nzp_pack < 1 && finder.par_pack < 1)
                    where2 = _FindPackAddCondition(finder, year);

#if PG
                sql = "Select p.nzp_pack, p.dat_pack, p.num_pack, p.dat_uchet, p.sum_pack, p.count_kv,p.nzp_supp, p.flag, p.pack_type, st.name_st, p.nzp_bank, p.nzp_payer as nzp_payer_contragent," +
                      " py.nzp_payer, py.payer, p.file_name, p.par_pack, b.bank || coalesce(' / '||py.payer,'') as bank   " +
                    " From " + table + " p left outer join " + Points.Pref + "_kernel.s_status st on p.flag = st.nzp_st , " + Points.Pref + "_kernel.s_bank b " +
                    " left outer join " + Points.Pref + "_kernel.s_payer py on b.nzp_payer = py.nzp_payer " +

                    " Where p.nzp_bank = b.nzp_bank  " +
                    where + where2 +
                    " Order by p.dat_pack desc, p.dat_uchet desc, py.payer asc, p.num_pack desc";
#else
                sql = "Select p.nzp_pack, p.dat_pack, p.num_pack, p.dat_uchet, p.sum_pack, p.count_kv,p.nzp_supp, p.flag, p.pack_type, st.name_st, p.nzp_bank, p.nzp_payer,p.nzp_payer as nzp_payer_contragent, py.payer, p.file_name, p.par_pack " +
                    " From " + table + " p, " + Points.Pref + "_kernel:s_bank b, outer " + Points.Pref + "_kernel:s_payer py, outer " + Points.Pref + "_kernel:s_status st " +
                    " Where p.nzp_bank = b.nzp_bank and p.flag = st.nzp_st and b.nzp_payer = py.nzp_payer" +
                    where + where2 +
                    " Order by p.dat_pack desc, p.dat_uchet desc, py.payer asc, p.num_pack desc";
#endif
                ret = ExecRead(conn_db, out reader, sql, true);
                if (!ret.result) return null;

#if PG
                sql = "insert into tmp_nzp_packs " +
                    "Select p.nzp_pack, p.par_pack " +
                    " From " + table + " p left outer join " + Points.Pref + "_kernel.s_status st on p.flag = st.nzp_st , " + Points.Pref + "_kernel.s_bank b " +
                    " left outer join " + Points.Pref + "_kernel.s_payer py on b.nzp_payer = py.nzp_payer " +

                    " Where p.nzp_bank = b.nzp_bank " +
                    where + where2;
#else
                sql = "insert into tmp_nzp_packs " +
                    "Select p.nzp_pack, p.par_pack " +
                    " From " + table + " p, " + Points.Pref + "_kernel:s_bank b, outer " + Points.Pref + "_kernel:s_payer py, outer " +
                    Points.Pref + "_kernel:s_status st " +
                    " Where p.nzp_bank = b.nzp_bank and p.flag = st.nzp_st and b.nzp_payer = py.nzp_payer" +
                    where + where2;
#endif
                ret = ExecSQL(conn_db, sql, true);

                try
                {
                    int i = 0;
                    while (reader.Read())
                    {
                        i++;
                        if (finder.skip > 0 && finder.skip >= i) continue;

                        Pack pack = new Pack();
                        pack.num = i.ToString();
                        pack.year_ = year;
                        if (reader["nzp_pack"] != DBNull.Value) pack.nzp_pack = Convert.ToInt32(reader["nzp_pack"]);
                        if (reader["par_pack"] != DBNull.Value) pack.par_pack = Convert.ToInt32(reader["par_pack"]);
                        if (reader["dat_pack"] != DBNull.Value) pack.dat_pack = Convert.ToDateTime(reader["dat_pack"]).ToShortDateString();
                        if (reader["dat_uchet"] != DBNull.Value) pack.dat_uchet = Convert.ToDateTime(reader["dat_uchet"]).ToShortDateString();
                        if (reader["pack_type"] != DBNull.Value) pack.pack_type = Convert.ToInt32(reader["pack_type"]);
                        if (reader["num_pack"] != DBNull.Value) pack.snum_pack = Convert.ToString(reader["num_pack"]).Trim();
                        if (reader["sum_pack"] != DBNull.Value) pack.sum_pack = Convert.ToDecimal(reader["sum_pack"]);
                        if (reader["count_kv"] != DBNull.Value) pack.count_kv = Convert.ToInt32(reader["count_kv"]);
                        if (reader["flag"] != DBNull.Value) pack.flag = Convert.ToInt32(reader["flag"]);
                        if (reader["name_st"] != DBNull.Value) pack.status = Convert.ToString(reader["name_st"]).Trim();

                        if (pack.flag == Pack.Statuses.WaitingForCancellationOfDistribution.GetHashCode() ||
                            pack.flag == Pack.Statuses.WaitingForDistribution.GetHashCode())
                        {
                            var sqlText = new StringBuilder();
                            for (int j = 0; j < 5; j++)
                            {
                                var tableFon = pgDefaultSchema + tableDelimiter +  "calc_fon_" + j;
                                if (!TempTableInWebCashe(conn_db, tableFon)) continue;

                                if (sqlText.Length > 0) sqlText.Append(" union ");

                                sqlText.Append(" select task, kod_info, progress, dat_in from ");
                                sqlText.Append(tableFon);
                                sqlText.AppendFormat(" where nzp = {0} and year_ = {1} and task in ({2}, {3}, {4}) and kod_info <> {5}", pack.nzp_pack, pack.year_,
                                     CalcFonTask.Types.DistributePack.GetHashCode(), CalcFonTask.Types.CancelPackDistribution.GetHashCode(),
                                     CalcFonTask.Types.CancelDistributionAndDeletePack.GetHashCode(), FonTask.Statuses.Completed.GetHashCode());
                            }

                            if (sqlText.Length > 0)
                            {
                                sqlText.Append(" order by dat_in desc");
                                Returns rets = ExecRead(conn_db, out reader2, sqlText.ToString(), true);
                                if (!rets.result) return null;
                                var clFonTask = new CalcFonTask();
                                if (reader2.Read())
                                {
                                    if (reader2["kod_info"] != DBNull.Value) clFonTask.KodInfo = Convert.ToInt32(reader2["kod_info"]);
                                    if (reader2["task"] != DBNull.Value) clFonTask.Task = Convert.ToInt32(reader2["task"]);
                                    if (reader2["progress"] != DBNull.Value) clFonTask.progress = Convert.ToDecimal(reader2["progress"]);
                                }
                                reader2.Close();
                                if (clFonTask.KodInfo.GetHashCode() == FonTask.Statuses.InProcess.GetHashCode() ||
                                    clFonTask.KodInfo.GetHashCode() == FonTask.Statuses.InQueue.GetHashCode() ||
                                    clFonTask.KodInfo.GetHashCode() == FonTask.Statuses.Failed.GetHashCode())
                                {
                                    pack.status += ": " + clFonTask.StatusName;
                                    if (clFonTask.KodInfo.GetHashCode() == FonTask.Statuses.Failed.GetHashCode())
                                        pack.status += ". Обновите статус пачки";
                                }
                            }
                        }
                        if (reader["nzp_bank"] != DBNull.Value) pack.nzp_bank = Convert.ToInt32(reader["nzp_bank"]);
                        if (reader["nzp_payer"] != DBNull.Value) pack.nzp_payer = Convert.ToInt32(reader["nzp_payer"]);
                        if (reader["nzp_payer_contragent"] != DBNull.Value) pack.nzp_payer_contragent = Convert.ToInt32(reader["nzp_payer_contragent"]);
                        if (reader["payer"] != DBNull.Value) pack.payer = Convert.ToString(reader["payer"]).Trim();
                        if (reader["file_name"] != DBNull.Value) pack.file_name = Convert.ToString(reader["file_name"]).Trim();
                        if (reader["bank"] != DBNull.Value) pack.bank = Convert.ToString(reader["bank"]).Trim();

                        int nzp_supp = 0;
                        if (reader["nzp_supp"] != DBNull.Value) nzp_supp = Convert.ToInt32(reader["nzp_supp"]);
                        if (nzp_supp > 0)
                        {
                            pack.nzp_supp = nzp_supp;
                            sql = "select name_supp from " + tables.supplier + " where nzp_supp = " + nzp_supp;
                            ret = ExecRead(conn_db, out reader2, sql, true);
                            if (!ret.result) return null;
                            if (reader2.Read()) if (reader2["name_supp"] != DBNull.Value) pack.name_supp = Convert.ToString(reader2["name_supp"]);
                            reader2.Close();
                        }

                        if (pack.nzp_payer_contragent > 0)
                        {
                            sql = "select payer from " + tables.payer + " where nzp_payer = " + pack.nzp_payer_contragent;
                            ret = ExecRead(conn_db, out reader2, sql, true);
                            if (!ret.result) return null;
                            if (reader2.Read()) if (reader2["payer"] != DBNull.Value) pack.name_supp = Convert.ToString(reader2["payer"]).Trim();
                            reader2.Close();
                        }

                        if (table_pls != "")
                        {
                            if (pack.par_pack > 0 && pack.nzp_pack == pack.par_pack)
                            {
                                sql = "select nzp_pack from " + table + " where nzp_pack <> " + pack.par_pack + " and par_pack = " + pack.par_pack;
                                ret = ExecRead(conn_db, out reader2, sql, true);
                                if (!ret.result) return null;
                                int nzp_pack = 0;
                                while (reader2.Read())
                                {
                                    if (reader["nzp_pack"] != DBNull.Value) nzp_pack = Convert.ToInt32(reader["nzp_pack"]);
                                    pack.sum_distr += GetSumDistrForPack(table_pls, nzp_pack, conn_db);
                                }
                                reader2.Close();
                            }
                            else
                            {
                                pack.sum_distr = GetSumDistrForPack(table_pls, pack.nzp_pack, conn_db);
                                pack.sum_not_distr = pack.sum_pack - pack.sum_distr;
                            }
                        }

                        list.Add(pack);

                        if (finder.rows > 0 && i >= finder.skip + finder.rows) break;
                    }
                }
                catch (Exception ex)
                {
                    ret = new Returns(false, ex.Message);
                    list = null;
                    MonitorLog.WriteLog("Ошибка FindPack " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                    break;
                }

                // строка Итого
                if (finder.isCalcItogo)
                {
#if PG
                    sql = "Select sum(sum_pack) as sum_pack, count(*) as count_pack " +
                        " From " + table + " p, " + Points.Pref + "_kernel.s_bank b " +
                        " left outer join " + Points.Pref + "_kernel.s_payer py on b.nzp_payer = py.nzp_payer " +
                        " Where p.nzp_bank = b.nzp_bank " +
                        where + where2;
#else
                    sql = "Select sum(sum_pack) as sum_pack, count(*) as count_pack " +
                        " From " + table + " p, " + Points.Pref + "_kernel:s_bank b, outer " + Points.Pref + "_kernel:s_payer py " +
                        " Where p.nzp_bank = b.nzp_bank and b.nzp_payer = py.nzp_payer " +
                        where + where2;
#endif //_FindPackAddCondition(finder, year);
                    ret = ExecRead(conn_db, out reader, sql, true);
                    if (!ret.result) return null;
                    try
                    {
                        if (reader.Read())
                        {
                            if (reader["sum_pack"] != DBNull.Value) totalPack.sum_pack += Convert.ToDecimal(reader["sum_pack"]);
                            if (reader["count_pack"] != DBNull.Value) totalNumber += Convert.ToInt32(reader["count_pack"]);
                        }
                    }
                    catch (Exception ex)
                    {
                        ret = new Returns(false, ex.Message);
                        list = null;
                        MonitorLog.WriteLog("Ошибка FindPack " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                        break;
                    }
                    finally { CloseReader(ref reader); }

                    sql = "select nzp_pack, par_pack from tmp_nzp_packs where par_pack is not null and par_pack=nzp_pack";
                    ret = ExecRead(conn_db, out reader2, sql, true);
                    if (!ret.result) return null;
                    int parpack = 0;
                    if (reader2.Read())
                    {
                        if (reader2["par_pack"] != DBNull.Value) parpack = Convert.ToInt32(reader2["par_pack"]);
                        sql = "insert into tmp_nzp_packs (nzp_pack) " +
                            " select nzp_pack from " + table + " where par_pack = " + parpack;
                        ret = ExecSQL(conn_db, sql, true);
                    }
                    sql = "delete from tmp_nzp_packs where par_pack is not null and par_pack=nzp_pack";
                    ret = ExecSQL(conn_db, sql, true);

#if PG
                    sql = "select sum(g_sum_ls) as sum from " + table_pls +
                                           " where  nzp_pack in (select nzp_pack from tmp_nzp_packs) and dat_uchet is not null and coalesce(cast(alg as int),0) <> 0";
#else
                    sql = "select sum(g_sum_ls) as sum from " + table_pls +
                                           " where  nzp_pack in (select nzp_pack from tmp_nzp_packs) and dat_uchet is not null and nvl(cast(alg as int),0) <> 0";
#endif
                    ret = ExecRead(conn_db, out reader2, sql, true);
                    if (!ret.result) return null;
                    if (reader2.Read()) if (reader2["sum"] != DBNull.Value) totalPack.sum_distr += Convert.ToDecimal(reader2["sum"]);
                    reader2.Close();
                }
            }

            if (reader != null) reader.Close();

            if (finder.isCalcItogo)
            {
                if (ret.result) ret.tag = totalNumber;
                totalPack.sum_not_distr = totalPack.sum_pack - totalPack.sum_distr;
                list.Add(totalPack);
            }

            return list;
        }

        private decimal GetSumDistrForPack(string table_pls, int nzp_pack, IDbConnection conn_db)
        {
#if PG
            string sql = "select sum(g_sum_ls) as sums from " + table_pls + " where nzp_pack = " + nzp_pack + " and dat_uchet is not null and coalesce(cast(alg as int),0) <> 0";
#else
            string sql = "select sum(g_sum_ls) as sums from " + table_pls + " where nzp_pack = " + nzp_pack + " and dat_uchet is not null and alg <> 0";
#endif
            IDataReader reader2;
            Returns ret = ExecRead(conn_db, out reader2, sql, true);
            if (!ret.result)
            {
                return 0;
            }
            decimal sum = 0;
            if (reader2.Read()) if (reader2["sums"] != DBNull.Value) sum = Convert.ToDecimal(reader2["sums"]);
            reader2.Close();
            return sum;
        }

        public Returns DeletePack(PackFinder finder)
        {
            #region Проверка входных параметров
            if (finder.nzp_user < 1) return new Returns(false, "Пользователь не определен");
            if (finder.nzp_pack < 1) return new Returns(false, "Пачка не определена");
            if (finder.year_ < 1) return new Returns(false, "Не задан год");
            #endregion

            List<Pack> list = null;
            Returns ret;

            if (finder.is_super_pack)
            {
                PackFinder pack = new PackFinder();
                pack.nzp_user = finder.nzp_user;
                pack.RolesVal = finder.RolesVal;
                pack.year_ = finder.year_;
                pack.par_pack = finder.par_pack;
                pack.isCalcItogo = false;
                list = FindPack(pack, out ret);
                if (!ret.result) return ret;
            }

            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;

            if (list == null) list = new List<Pack>();

            list.Add(finder);

            foreach (var pack in list)
            {
                var pls = new Pack_ls { nzp_pack = pack.nzp_pack, year_ = pack.year_ };
                ret = CheckPackLsToDeleting(pls);
                if (!ret.result)
                {
                    return ret;
                }
            }

            IDataReader reader = null;
            DeleteBadPack(list, conn_db);

            if (reader != null) reader.Close();
            conn_db.Close();

            return ret;
        }

        /// <summary>
        /// 1. Сохраняет в базе данных заданный операционный день
        /// 2. Если этот день в новом расчетном месяце, то переводит расчетный месяц центрального банка на следующий месяц
        /// 3. Обновляет информацию о банках данных
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        public Returns SaveOperDay(Pack finder)
        {
            #region Проверка входных параметров
            if (finder.nzp_user < 1) return new Returns(false, "Пользователь не определен");
            if (finder.dat_uchet == "") return new Returns(false, "Операционный день не задан");
            DateTime dat_uchet;
            if (!DateTime.TryParse(finder.dat_uchet, out dat_uchet)) return new Returns(false, "Неверно задан операционный день");
            #endregion

            DateTime datoper_old = Points.DateOper;

            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            Returns ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;

            IDbTransaction transaction = conn_db.BeginTransaction();

            string sql = "select dat_oper from " + Points.Pref + "_data" + tableDelimiter + "fn_curoperday";

            IDataReader reader;
            ret = ExecRead(conn_db, transaction, out reader, sql, true);
            if (!ret.result)
            {
                transaction.Rollback();
                conn_db.Close();
                return ret;
            }
            if (reader.Read())
            {
                sql = "update " + Points.Pref + "_data" + tableDelimiter + "fn_curoperday set dat_oper = " + Utils.EStrNull(dat_uchet.ToShortDateString()) +
                    ", dat_inkas = " + Utils.EStrNull(dat_uchet.ToShortDateString()) +
                    ", flag = 1";
            }
            else
            {
                sql = "insert into " + Points.Pref + "_data" + tableDelimiter + "fn_curoperday (dat_oper, dat_inkas, flag)" +
                    " values (" + Utils.EStrNull(dat_uchet.ToShortDateString()) + ", " + Utils.EStrNull(dat_uchet.ToShortDateString()) + ", 1)";
            }
            reader.Close();
            ret = ExecSQL(conn_db, transaction, sql, true);
            if (!ret.result)
            {
                transaction.Rollback();
                conn_db.Close();
                return ret;
            }

            if (dat_uchet.Month != datoper_old.Month && finder.mode == OperDayFinder.Mode.CloseOperDay.GetHashCode())
            {
                DateTime calcmonth = new DateTime(Points.CalcMonth.year_, Points.CalcMonth.month_, 1);
                if (dat_uchet > calcmonth)
                {
                    #region определение локального пользователя
                    int nzpUser = finder.nzp_user;
                    
                    /*DbWorkUser db = new DbWorkUser();
                    int nzpUser = db.GetLocalUser(conn_db, transaction, finder, out ret);
                    db.Close();
                    if (!ret.result)
                    {
                        transaction.Rollback();
                        conn_db.Close();
                        return ret;
                    }*/
                    #endregion
                    Finder findercm = new Finder();
                    findercm.nzp_user = nzpUser;
                    DbChargeTemp dbc = new DbChargeTemp();
                    ret = dbc.CloseCalcMonth_actions(conn_db, transaction, Points.Pref, findercm).GetReturns();
                    dbc.Close();
                    if (!ret.result)
                    {
                        transaction.Rollback();
                        conn_db.Close();
                        return ret;
                    }
                }
            }

            if (ret.result)
            {
                transaction.Commit();
                
                using (DbCalcPack dbCalcPack = new DbCalcPack())
                {
                   dbCalcPack.UpdateSaldoFndistrib(dat_uchet, 0, 0, out ret);
                   // if (!ret.result) throw new Exception(ret.text);
                }

                using (DbSprav dbs = new DbSprav())
                {
                    dbs.PointLoad(GlobalSettings.WorkOnlyWithCentralBank, out ret);
                }
            }
            else transaction.Rollback();
            conn_db.Close();


            //запись проводок
            if (ret.result)
            {
                Returns ret2;
                finder.DateOper = datoper_old.ToShortDateString();
                finder.listNumber = finder.mode; //костыль, потому как Pack в сериализованном виде в поле calc_fon_x.parameters не влазит :(
                //вызываем получение проводок в фоне                
                AddFonTaskProv(finder, CalcFonTask.Types.taskInsertProvOnClosedOperDay, out ret2);
            }

            return ret;
        }
      
        public DateTime GetOperDay(out Returns ret)
        {
            DateTime datoper = DateTime.MinValue;

            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return datoper;
            
            string sql = "select dat_oper from " + Points.Pref + "_data" + tableDelimiter + "fn_curoperday";

            IDataReader reader;
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return datoper;
            }
            if (reader.Read())
            {
                if (reader["dat_oper"] != DBNull.Value) datoper = Convert.ToDateTime(reader["dat_oper"]);
            }
            reader.Close();

            conn_db.Close();
            return datoper;
        }

        public Returns ChangeOperDay(OperDayFinder finder, out string date_oper, out string filename, out RecordMonth calcmonth)
        {
            filename = "";
            date_oper = "";
            calcmonth = Points.CalcMonth;
            if (finder.nzp_user < 1) return new Returns(false, "Пользователь не определен");

            DateTime newoperday;
            if (finder.mode == OperDayFinder.Mode.CloseOperDay.GetHashCode()) newoperday = Points.DateOper.AddDays(1);
            else newoperday = Points.DateOper.AddDays(-1);

            DateTime mincm, maxcm;
            Returns ret = GetMinMaxCalcMonth(out mincm, out maxcm);
            if (!ret.result)
            {
                return ret;
            }
            //Максимально допустимое значение операционного дня - это последний день наименьшего среди всех локальных банков расчетного месяца
            DateTime max = new DateTime(mincm.Year, mincm.Month, DateTime.DaysInMonth(mincm.Year, mincm.Month));
            maxcm = new DateTime(maxcm.Year, maxcm.Month, DateTime.DaysInMonth(maxcm.Year, maxcm.Month));
            DateTime min;
            if (max == maxcm)
            {
                min = new DateTime(max.Year, max.Month, 1).AddMonths(-1);
            }
            else min = new DateTime(max.Year, max.Month, 1);

            if (newoperday >= min && newoperday <= max)
            {
                ret = new Returns(true);
                if (finder.mode == OperDayFinder.Mode.GoBackOperDay.GetHashCode())
                {
                    ret = CheckingReturnOnPrevDay();
                    if (!ret.result)
                    {
                        if (ret.tag == 0) ret.text = "Ошибка выполнение операции";
                        else ret.text = "Невозможно изменить операционный день, так как в текущем операционном дне имеются операции";
                        ret.tag = -1;
                        return ret;
                    }

                }

                if (finder.mode == OperDayFinder.Mode.CloseOperDay.GetHashCode())
                {
                    Payments pay = new Payments();
                    pay.nzp_user = finder.nzp_user;
                    pay.nzp_role = finder.nzp_role;
                    pay.RolesVal = finder.RolesVal;
                    pay.webLogin = finder.webLogin;
                    pay.webUname = finder.webUname;
                    pay.remoteLogin = finder.remoteLogin;
                    pay.dat_s = (new DateTime(Points.DateOper.Year, Points.DateOper.Month, 1)).ToShortDateString();
                    pay.dat_po = Points.DateOper.ToShortDateString();
                    pay.checkCanChangeOperDay = true;
                    ret = GenConDistrPayments(pay);

                    if (!ret.result)
                    {
                        ret.text = "Ошибка выполнения проверок при смене опердня";
                        return ret;
                    }
                    filename = ret.text;
                    if (ret.tag == -7)
                    {
                        ret.text = "Операционный день менять нельзя, результаты проверок в отчете";
                        return ret;
                    }

                }

                if (ret.result && ret.tag == 0)
                {
                    Pack finderpack = new Pack();
                    finderpack.nzp_user = finder.nzp_user;
                    finderpack.nzp_role = finder.nzp_role;
                    finderpack.RolesVal = finder.RolesVal;
                    finderpack.webLogin = finder.webLogin;
                    finderpack.webUname = finder.webUname;
                    finderpack.remoteLogin = finder.remoteLogin;
                    finderpack.dat_uchet = newoperday.ToShortDateString();
                    finderpack.mode = finder.mode;
                    ret = SaveOperDay(finderpack);
                    if (!ret.result) return ret;
                    date_oper = newoperday.ToShortDateString();
                    calcmonth = Points.CalcMonth;
                }
            }
            else
            {
                if (finder.mode == OperDayFinder.Mode.CloseOperDay.GetHashCode())
                    ret.text =
                    "Невозможно закрыть операционный день. Новый операционный день должен " +
                    " быть больше или равен " + min.ToShortDateString() + "  и меньше или равен " + max.ToShortDateString();
                else ret.text =
                "Невозможно вернуться на предыдущий операционный день. Операционный день должен " +
                " быть больше или равен " + min.ToShortDateString() + "  и меньше или равен " + max.ToShortDateString();
                ret.tag = -1;
            }
            return ret;
        }

        public Returns ChangeOperDay(OperDayFinder finder, out string date_oper, DbCalcQueueClient.SetTaskProgressDelegate setTaskProgress)
        {
            if (setTaskProgress != null) setTaskProgress((decimal)0.01);
            string filename = "";
            date_oper = "";
            RecordMonth calcmonth = Points.CalcMonth;
            if (finder.nzp_user < 1) return new Returns(false, "Пользователь не определен");

            DateTime newoperday;
            if (finder.mode == OperDayFinder.Mode.CloseOperDay.GetHashCode()) newoperday = Points.DateOper.AddDays(1);
            else newoperday = Points.DateOper.AddDays(-1);

            DateTime mincm, maxcm;
            Returns ret = GetMinMaxCalcMonth(out mincm, out maxcm);
            if (!ret.result)
            {
                return ret;
            }
            //Максимально допустимое значение операционного дня - это последний день наименьшего среди всех локальных банков расчетного месяца
            DateTime max = new DateTime(mincm.Year, mincm.Month, DateTime.DaysInMonth(mincm.Year, mincm.Month));
            maxcm = new DateTime(maxcm.Year, maxcm.Month, DateTime.DaysInMonth(maxcm.Year, maxcm.Month));
            DateTime min;
            if (max == maxcm)
            {
                min = new DateTime(max.Year, max.Month, 1).AddMonths(-1);
            }
            else min = new DateTime(max.Year, max.Month, 1);

            if (newoperday >= min && newoperday <= max)
            {
                ret = new Returns(true);
                if (finder.mode == OperDayFinder.Mode.GoBackOperDay.GetHashCode())
                {
                    ret = CheckingReturnOnPrevDay();
                    if (!ret.result)
                    {
                        if (ret.tag == 0) ret.text = "Ошибка выполнение операции";
                        else ret.text = "Невозможно изменить операционный день, так как в текущем операционном дне имеются операции";
                        ret.tag = -1;
                        return ret;
                    }

                }

                if (finder.mode == OperDayFinder.Mode.CloseOperDay.GetHashCode())
                {
                    if (setTaskProgress != null) setTaskProgress((decimal)0.05);
                    Payments pay = new Payments();
                    pay.nzp_user = finder.nzp_user;
                    pay.nzp_role = finder.nzp_role;
                    pay.RolesVal = finder.RolesVal;
                    pay.webLogin = finder.webLogin;
                    pay.webUname = finder.webUname;
                    pay.remoteLogin = finder.remoteLogin;
                    pay.dat_s = (new DateTime(Points.DateOper.Year, Points.DateOper.Month, 1)).ToShortDateString();
                    pay.dat_po = Points.DateOper.ToShortDateString();
                    pay.checkCanChangeOperDay = true;
                    ret = GenConDistrPaymentsPDF(pay, setTaskProgress);

                    if (!ret.result)
                    {
                        ret.text = "Ошибка выполнения проверок при смене опердня";
                        return ret;
                    }
                    filename = ret.text;
                    if (ret.tag == -7)
                    {
                        ret.text = "Операционный день менять нельзя, результаты проверок в отчете в Моих файлах или На странице Контроль распределения оплат";
                        return ret;
                    }

                }
                if (setTaskProgress != null) setTaskProgress((decimal)0.99);
                if (ret.result && ret.tag == 0)
                {
                    Pack finderpack = new Pack();
                    finderpack.nzp_user = finder.nzp_user;
                    finderpack.nzp_role = finder.nzp_role;
                    finderpack.RolesVal = finder.RolesVal;
                    finderpack.webLogin = finder.webLogin;
                    finderpack.webUname = finder.webUname;
                    finderpack.remoteLogin = finder.remoteLogin;
                    finderpack.dat_uchet = newoperday.ToShortDateString();
                    finderpack.mode = finder.mode;
                    ret = SaveOperDay(finderpack);
                    if (!ret.result) return ret;
                    date_oper = newoperday.ToShortDateString();
                    calcmonth = Points.CalcMonth;
                   // Points.DateOper = newoperday;
                }
            }
            else
            {
                if (finder.mode == OperDayFinder.Mode.CloseOperDay.GetHashCode())
                {
                    ret.text =
                        "Невозможно сменить операционный день с "+Points.DateOper.ToShortDateString()+" на "+newoperday.ToShortDateString()+". Новый операционный день должен " +
                        " быть больше или равен " + min.ToShortDateString() + "  и меньше или равен " +
                        max.ToShortDateString();
                    if (newoperday > max) ret.text += ". Не все банки данных переведены на следующий расчетный месяц";
                    else if (newoperday < min) ret.text += ". Ошибка в данных";
                }
                else
                    ret.text =
                        "Невозможно сменить операционный день с " + Points.DateOper.ToShortDateString() + " на " + newoperday.ToShortDateString() + ". Операционный день должен " +
                        " быть больше или равен " + min.ToShortDateString() + "  и меньше или равен " +
                        max.ToShortDateString();
                ret.tag = -1;
            }
            return ret;
        }

        private Returns GetMinMaxCalcMonth(out DateTime mindate, out DateTime maxdate)
        {
            mindate = maxdate = DateTime.MinValue;
            if (Points.PointList == null || Points.PointList.Count == 0)
            {
                return new Returns(false, "Невозможно определить расчетные месяцы для локальных банков");
            }
            mindate = new DateTime(Points.PointList[0].CalcMonth.year_, Points.PointList[0].CalcMonth.month_, 1);
            maxdate = new DateTime(Points.PointList[0].CalcMonth.year_, Points.PointList[0].CalcMonth.month_, 1);
            for (int i = 1; i < Points.PointList.Count; i++)
            {
                DateTime dat = new DateTime(Points.PointList[i].CalcMonth.year_, Points.PointList[i].CalcMonth.month_, 1);
                if (mindate > dat) mindate = dat;
                if (maxdate < dat) maxdate = dat;
            }

            return new Returns(true);
        }

        /*public Pack LoadFinancePack(Pack finder, out Returns ret)
        {
            #region Проверка входных параметров
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Пользователь не определен");
                return null;
            }
            if (finder.nzp_pack < 1)
            {
                ret = new Returns(false, "Пачка не определена");
                return null;
            }
            if (finder.dat_pack == "")
            {
                ret = new Returns(false, "Дата пачки не задана");
                return null;
            }
            DateTime dat_pack;
            if (!DateTime.TryParse(finder.dat_pack, out dat_pack))
            {
                ret = new Returns(false, "Неверно задана дата пачки");
                return null;
            }
            #endregion

            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            Pack pack = LoadFinancePack(finder, out ret, conn_db);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }

            conn_db.Close();

            return pack;
        }

        public Pack LoadFinancePack(Pack finder, out Returns ret, IDbConnection conn_db)
        {
            DateTime datPack = DateTime.Parse(finder.dat_pack);
            
            string table = Points.Pref + "_fin_" + datPack.Year.ToString("0000").Substring(2, 2) + ":pack";
            
            if (!TempTableInWebCashe(conn_db, table))
            {
                ret = new Returns(false, "Пачка не найдена", -1);
                return null;
            }

            string sql = "Select p.nzp_pack, p.dat_pack, p.num_pack, p.dat_uchet, p.sum_pack, p.count_kv, p.flag, st.name_st, p.nzp_bank, b.nzp_payer, py.payer, p.file_name " +
                " From " + table + " p, outer (" + Points.Pref + "_kernel:s_bank b, outer " + Points.Pref + "_kernel:s_payer py), outer " + Points.Pref + "_kernel:s_status st " +
                " Where p.nzp_bank = b.nzp_bank and p.flag = st.nzp_st and b.nzp_payer = py.nzp_payer" +
                " and p.nzp_pack = " + finder.nzp_pack;

            IDataReader reader;
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                return null;
            }
            Pack pack = null;
            try
            {
                if (reader.Read())
                {
                    pack = new Pack();
                    if (reader["nzp_pack"] != DBNull.Value) pack.nzp_pack = Convert.ToInt32(reader["nzp_pack"]);
                    if (reader["dat_pack"] != DBNull.Value) pack.dat_pack = Convert.ToDateTime(reader["dat_pack"]).ToShortDateString();
                    if (reader["dat_uchet"] != DBNull.Value) pack.dat_uchet = Convert.ToDateTime(reader["dat_uchet"]).ToShortDateString();
                    if (reader["num_pack"] != DBNull.Value) pack.snum_pack = Convert.ToString(reader["num_pack"]).Trim();
                    if (reader["sum_pack"] != DBNull.Value) pack.sum_pack = Convert.ToDecimal(reader["sum_pack"]);
                    if (reader["count_kv"] != DBNull.Value) pack.count_kv = Convert.ToInt32(reader["count_kv"]);
                    if (reader["flag"] != DBNull.Value) pack.flag = Convert.ToInt32(reader["flag"]);
                    if (reader["name_st"] != DBNull.Value) pack.status = Convert.ToString(reader["name_st"]).Trim();
                    if (reader["nzp_bank"] != DBNull.Value) pack.nzp_bank = Convert.ToInt32(reader["nzp_bank"]);
                    if (reader["nzp_payer"] != DBNull.Value) pack.nzp_payer = Convert.ToInt32(reader["nzp_payer"]);
                    if (reader["payer"] != DBNull.Value) pack.payer = Convert.ToString(reader["payer"]).Trim();
                    if (reader["file_name"] != DBNull.Value) pack.file_name = Convert.ToString(reader["file_name"]).Trim();
                }
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                pack = null;
                MonitorLog.WriteLog("Ошибка LoadFinancePack " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
            }

            if (reader != null) reader.Close();
            return pack;
        }*/

        public List<Pack_log> FindPackLog(Pack_log finder, out Returns ret)
        {
            #region Проверка входных параметров
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Пользователь не определен");
                return null;
            }
            if (finder.nzp_pack < 1 && finder.nzp_pack_ls < 1)
            {
                ret = new Returns(false, "Пачка или оплата не определена");
                return null;
            }
            if (finder.year_ < 1)
            {
                ret = new Returns(false, "Год не задан");
                return null;
            }
            #endregion

            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            List<Pack_log> list = FindPackLog(finder, out ret, conn_db);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }

            ret.tag = list.Count;
            if (list != null)
            {
                if (finder.skip > 0 && list.Count > finder.skip) list.RemoveRange(0, finder.skip);
                if (finder.rows > 0 && list.Count > finder.rows) list.RemoveRange(finder.rows, list.Count - finder.rows);
            }
            conn_db.Close();

            return list;
        }

        public List<Pack_log> FindPackLog(Pack_log finder, out Returns ret, IDbConnection conn_db)
        {
#if PG
            string table = Points.Pref + "_fin_" + (finder.year_ % 100).ToString("00") + ".pack_log";
#else
            string table = Points.Pref + "_fin_" + (finder.year_ % 100).ToString("00") + ":pack_log";
#endif

            if (!TempTableInWebCashe(conn_db, table))
            {
                ret = new Returns(false, "Данные не найдены", -1);
                return null;
            }

#if PG
            string sql = "Select pl.nzp_plog, pl.nzp_pack, pl.nzp_pack_ls, pl.nzp_wp, p.point, pl.dat_oper, pl.dat_log, pl.txt_log, pl.tip_log " +
                           " From " + table + " pl left outer join " + Points.Pref + "_kernel.s_point p on pl.nzp_wp = p.nzp_wp" +
                           " Where  pl.nzp_pack = " + finder.nzp_pack;
#else
            string sql = "Select pl.nzp_plog, pl.nzp_pack, pl.nzp_pack_ls, pl.nzp_wp, p.point, pl.dat_oper, pl.dat_log, pl.txt_log, pl.tip_log " +
                " From " + table + " pl, outer " + Points.Pref + "_kernel:s_point p " +
                " Where pl.nzp_wp = p.nzp_wp and pl.nzp_pack = " + finder.nzp_pack;
#endif
            if (finder.nzp_pack_ls > 0) sql += " and pl.nzp_pack_ls = " + finder.nzp_pack_ls; else sql += " and pl.nzp_pack_ls = 0";
            sql += " Order by dat_log, nzp_plog";

            IDataReader reader;
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                return null;
            }
            List<Pack_log> list = new List<Pack_log>();
            try
            {
                int i = 0;
                while (reader.Read())
                {
                    i++;
                    Pack_log log = new Pack_log();
                    log.num = i.ToString();
                    if (reader["nzp_plog"] != DBNull.Value) log.nzp_plog = Convert.ToInt32(reader["nzp_plog"]);
                    if (reader["nzp_pack"] != DBNull.Value) log.nzp_pack = Convert.ToInt32(reader["nzp_pack"]);
                    if (reader["nzp_pack_ls"] != DBNull.Value) log.nzp_pack_ls = Convert.ToInt32(reader["nzp_pack_ls"]);
                    if (reader["nzp_wp"] != DBNull.Value) log.nzp_wp = Convert.ToInt32(reader["nzp_wp"]);
                    if (reader["point"] != DBNull.Value) log.point = Convert.ToString(reader["point"]).Trim();
                    if (reader["dat_oper"] != DBNull.Value) log.dat_oper = Convert.ToDateTime(reader["dat_oper"]).ToShortDateString();
                    if (reader["dat_log"] != DBNull.Value) log.dat_log = String.Format("{0:dd.MM.yyyy HH:mm}", reader["dat_log"]);
                    if (reader["txt_log"] != DBNull.Value) log.txt_log = Convert.ToString(reader["txt_log"]).Trim();
                    if (reader["tip_log"] != DBNull.Value) log.tip_log = Convert.ToInt32(reader["tip_log"]);

                    list.Add(log);
                }
            }
            catch (Exception ex)
            {
                ret = new Returns(false, ex.Message);
                list = null;
                MonitorLog.WriteLog("Ошибка FindPackLog " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
            }

            if (reader != null) reader.Close();
            return list;
        }

        public List<Pack_ls> FindFinancePackLs(Pack_ls finder, out Returns ret)
        {
            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            List<Pack_ls> list = FindFinancePackLs(finder, out ret, conn_db);

            conn_db.Close();
            return list;
        }

        public List<Pack_ls> FindFinancePackLs(Pack_ls finder, out Returns ret, IDbConnection conn_db)
        {
            #region Проверка входных параметров
            if (finder.nzp_user < 1)
            {
                ret = new Returns(false, "Пользователь не определен");
                return null;
            }
            if (finder.nzp_pack < 1 && finder.nzp_pack_ls < 1 && finder.nzp_kvar < 1)
            {
                ret = new Returns(false, "Не задана пачка, квитанция об оплате или лицевой счет");
                return null;
            }
            if (finder.year_ < 1)
            {
                ret = new Returns(false, "Год не задан");
                return null;
            }
            #endregion

            List<Pack_ls> list = LoadListFinancePackLs(finder, out ret, conn_db);

            return list;
        }

        public Returns ShowButtonInCase(Pack_ls finder)
        {
            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            Returns ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;

            ret = ShowButtonInCase(finder, conn_db);
            conn_db.Close();
            return ret;
        }

        public Returns ShowButtonInCase(Pack_ls finder, IDbConnection conn_db)
        {
            Returns ret;
            #region Проверка входных параметров
            if (finder.nzp_user < 1)
            {
                return new Returns(false, "Пользователь не определен");
            }
            if (finder.nzp_pack_ls < 1)
            {
                return new Returns(false, "Не задана квитанция об оплате");
            }
            if (finder.year_ < 1)
            {
                return new Returns(false, "Год не задан");
            }
            #endregion

#if PG
            string tablePackLs = Points.Pref + "_fin_" + (finder.year_ % 100).ToString("00") + ".pack_ls";
#else
            string tablePackLs = Points.Pref + "_fin_" + (finder.year_ % 100).ToString("00") + ":pack_ls";
#endif
            if (!TempTableInWebCashe(conn_db, tablePackLs))
            {
                return new Returns(false, "Данные не найдены");
            }
            string dt =// "01." + Points.CalcMonth.month_ + "." + Points.CalcMonth.year_;
                 Points.DateOper.ToShortDateString();
            string where = "";
#if PG
            where = " and dat_uchet is not null and dat_uchet < '" + dt + "' and coalesce(cast(alg as int),0) <> 0 and inbasket <>1 ";
#else
            where = " and dat_uchet is not null and dat_uchet < '" + dt + "' and alg <> 0 and inbasket <>1 ";
#endif
            string sql = "Select count(*) From " + tablePackLs +
                "  Where nzp_pack_ls = " + finder.nzp_pack_ls + where;

            int totalNumber = 0;
            object obj = ExecScalar(conn_db, sql, out ret, true);
            if (!ret.result)
            {
                return ret;
            }
            try { totalNumber = Convert.ToInt32(obj); }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                return ret;
            }
            if (totalNumber == 0)
            {
                // Зачем при  каждом входе в карточку оплаты давать это сообщение ?
                return new Returns(false, "В портфель можно поместить только распределенные оплаты прошлых операционных дней", -1);
            }
            sql = "Select kod_sum From " + tablePackLs +
                "  Where nzp_pack_ls = " + finder.nzp_pack_ls + where;

            object obj2 = ExecScalar(conn_db, sql, out ret, true);
            if (!ret.result)
            {
                return ret;
            }

            try { totalNumber = Convert.ToInt32(obj2); }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                return ret;
            }
            /* if (totalNumber == 50)
             {
                 conn_db.Close();
                 return new Returns(false, "В портфель можно поместить только оплаты принятые на счета РЦ", -1);
             }*/

            return new Returns(true);
        }

        public Returns ShowButtonInCaseForPack(IDbConnection conn_db, PackFinder finder, string tablePackLs, string wherepls)
        {
            Returns ret = Utils.InitReturns();
            #region Проверка входных параметров
            if (finder.nzp_user < 1)
            {
                return new Returns(false, "Пользователь не определен");
            }
            if (finder.nzp_pack < 1)
            {
                return new Returns(false, "Не задана пачка оплат");
            }
            if (finder.year_ < 1)
            {
                return new Returns(false, "Год не задан");
            }
            #endregion

            if (!TempTableInWebCashe(conn_db, tablePackLs))
            {
                return new Returns(false, "Данные не найдены");
            }
            string dt = Points.DateOper.ToShortDateString();
            string where = "";
#if PG
            where = " and dat_uchet is not null and dat_uchet < '" + dt + "' and inbasket <>1 ";
#else
            where = " and dat_uchet is not null and dat_uchet < '" + dt + "' and inbasket <>1 ";
#endif
            string sql = "Select count(*) From " + tablePackLs +
                "  Where nzp_pack = " + finder.nzp_pack + where + wherepls;

            int totalNumber = 0;
            object obj = ExecScalar(conn_db, sql, out ret, true);
            if (!ret.result)
            {
                return ret;
            }
            try { totalNumber = Convert.ToInt32(obj); }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                return ret;
            }
            if (totalNumber == 0)
            {
                return new Returns(false, "В портфель можно поместить только распределенные оплаты прошлых операционных дней", -1);
            }
            /*   sql = "Select kod_sum From " + tablePackLs +
                   "  Where nzp_pack_ls = " + finder.nzp_pack_ls + where;

               object obj2 = ExecScalar(conn_db, sql, out ret, true);
               if (!ret.result)
               {
                   conn_db.Close();
                   return ret;
               }

               try { totalNumber = Convert.ToInt32(obj2); }
               catch (Exception ex)
               {
                   conn_db.Close();
                   ret.result = false;
                   ret.text = ex.Message;
                   return ret;
               }
               if (totalNumber == 50)
               {
                   conn_db.Close();
                   return new Returns(false, "В портфель можно поместить только оплаты принятые на счета РЦ", -1);
               }*/

            return new Returns(true);
        }

        public Returns ChangeCasePackLs(Pack_ls finder)
        {
            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            Returns ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;
            ret = ChangeCasePackLs(finder, conn_db);
            conn_db.Close();
            return ret;
        }

        public Returns ChangeCasePackLs(Pack_ls finder, IDbConnection conn_db)
        {
            Returns ret;
            #region Проверка входных параметров
            if (finder.nzp_user < 1)
            {
                return new Returns(false, "Пользователь не определен");
            }
            if (finder.nzp_pack_ls < 1)
            {
                return new Returns(false, "Не задана квитанция об оплате");
            }
            if (finder.year_ < 1)
            {
                return new Returns(false, "Год не задан");
            }
            #endregion

#if PG
            string tablePackLs = Points.Pref + "_fin_" + (finder.year_ % 100).ToString("00") + ".pack_ls";
#else
            string tablePackLs = Points.Pref + "_fin_" + (finder.year_ % 100).ToString("00") + ":pack_ls";
#endif
            if (!TempTableInWebCashe(conn_db, tablePackLs))
            {
                return new Returns(false, "Данные не найдены");
            }
            string dt =// "01." + Points.CalcMonth.month_ + "." + Points.CalcMonth.year_;
                Points.DateOper.ToShortDateString();

            if (finder.incase == 1)
            {
                ret = ShowButtonInCase(finder, conn_db);
                if (!ret.result)
                {
                    return new Returns(false);
                }
            }

            string sql = "";

            if (finder.incase == 0)
            {
                sql = "update " + tablePackLs + " set incase = " + finder.incase +
                    " Where nzp_pack_ls = " + finder.nzp_pack_ls + " and incase =1 ";

            }
            else
            {

                sql = "update " + tablePackLs + " set incase = " + finder.incase +
                    " Where nzp_pack_ls = " + finder.nzp_pack_ls + " and dat_uchet is not null and dat_uchet < '" + dt + "'";
            }
            string table = Points.Pref + "_fin_" + (finder.year_ % 100).ToString("00") +
#if PG
 "." +
#else
 ":" +
#endif
 "pack_ls";

            ret = ExecSQL(conn_db, sql, true);
            return ret;
        }

        public Returns ChangeCasePack(PackFinder finder, List<Pack_ls> list)
        {
            #region Проверка входных параметров
            if (finder.nzp_user < 1)
            {
                return new Returns(false, "Пользователь не определен");
            }
            if (finder.nzp_pack < 1)
            {
                return new Returns(false, "Не задана пачка оплат");
            }
            if (finder.year_ < 1)
            {
                return new Returns(false, "Год не задан");
            }
            #endregion

            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            Returns ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;

            string where = "";
            if (list != null && list.Count > 0)
            {
                foreach (Pack_ls pls in list)
                {
                    if (where == "") where += pls.nzp_pack_ls;
                    else where += ", " + pls.nzp_pack_ls;
                }
            }
            if (where != "") where = " and nzp_pack_ls in (" + where + ")";

            List<int> listyear = new List<int>();
            listyear.Add(finder.year_ - 1);
            listyear.Add(finder.year_);
            listyear.Add(finder.year_);

            int countcase = 0, countall = 0;

            foreach (int year in listyear)
            {
                string tablePackLs = Points.Pref + "_fin_" + (year % 100).ToString("00") + tableDelimiter + "pack_ls";

                if (!TempTableInWebCashe(conn_db, tablePackLs))
                {
                    conn_db.Close();
                    return new Returns(false, "Данные не найдены");
                }
                string dt = Points.DateOper.ToShortDateString();

                string sql = "";
                if (finder.putincase == 1)
                {
                    ret = ShowButtonInCaseForPack(conn_db, finder, tablePackLs, where);
                    if (ret.result)
                    {
                        sql = "update " + tablePackLs + " set incase = " + finder.putincase +
                            " Where nzp_pack = " + finder.nzp_pack + " and dat_uchet is not null and inbasket = 0 and " +
                            //sNvlWord + "(alg" + sConvToInt + ",0) <> 0 and dat_uchet < '" + dt + "' and kod_sum <> 50";
                            sNvlWord + "(alg" + sConvToInt + ",0) <> 0 and dat_uchet < '" + dt + "' " + where;

                        ret = ExecSQL(conn_db, sql, true);
                        if (!ret.result)
                        {
                            conn_db.Close();
                            return ret;
                        }
                    }
                }
                else
                {
                    sql = "update " + tablePackLs + " set incase = " + finder.putincase +
                        " Where nzp_pack = " + finder.nzp_pack + " and incase = 1 " + where;

                    ret = ExecSQL(conn_db, sql, true);
                    if (!ret.result)
                    {
                        conn_db.Close();
                        return ret;
                    }
                }

                if (finder.putincase == 1)
                {
                    sql = "select count(*) from " + tablePackLs + " where nzp_pack = " + finder.nzp_pack + " and incase = 1 " + where;
                    var obj = ExecScalar(conn_db, sql, out ret, true);
                    if (!ret.result) return ret;
                    countcase = Convert.ToInt32(obj);

                    sql = "select count(*) from " + tablePackLs + " where nzp_pack = " + finder.nzp_pack + " " + where;
                    obj = ExecScalar(conn_db, sql, out ret, true);
                    if (!ret.result) return ret;
                    countall = Convert.ToInt32(obj);
                }
            }

            if (ret.result)
            {
                if (countcase > 0) ret.text = "Оплаты из пачки помещены в портфель (" + countcase + "/" + countall + "). ";
                if (countcase < countall) ret.text += "В портфель можно поместить распределенные оплаты прошлых операционных дней и только оплаты принятые на счета РЦ";
                if (ret.text != "") ret.tag = -1;
            }

            conn_db.Close();
            return ret;
        }

        public Returns ChangeChoosenPlsInCase(Finder finder)
        {
            Returns ret = Utils.InitReturns();
            if (finder.nzp_user < 1) return new Returns(false, "Пользователь не определен");

            string tXX_case = "t" + Convert.ToString(finder.nzp_user) + "_case";

            IDataReader reader = null;
#if PG
            string tXX_case_full = "public." + tXX_case;
#else
            IDbConnection conn_web = GetConnection(Constants.cons_Webdata);
            ret = OpenDb(conn_web, true);
            if (!ret.result) return ret;
            string tXX_case_full =  DBManager.GetFullBaseName(conn_web) + ":" + tXX_case;
            conn_web.Close();
#endif
            StringBuilder sql = new StringBuilder();
            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;

            if (!TempTableInWebCashe(conn_db, tXX_case_full))
            {
                conn_db.Close();
                return new Returns(false, "Данных не найдено", -1);
            }

            sql.Remove(0, sql.Length);
            sql.Append("select count(*) from " + tXX_case_full + " where mark = 1");
            var obj = ExecScalar(conn_db, sql.ToString(), out ret, true);
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }
            if (Convert.ToInt32(obj) == 0)
            {
                conn_db.Close();
                return new Returns(false, "Нет выбранных оплат", -1);
            }

            sql.Remove(0, sql.Length);
            sql.AppendFormat("select yearr from {0}_kernel{1}s_baselist where idtype = 4 order by yearr", Points.Pref, tableDelimiter);
            ret = ExecRead(conn_db, out reader, sql.ToString(), true);
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }
            var years = new List<int>();
            while (reader.Read()) if (reader["yearr"] != DBNull.Value) years.Add(Convert.ToInt32(reader["yearr"]));

            CloseReader(ref reader);

            foreach (int year in years)
            {
                string tablePackLs = Points.Pref + "_fin_" + year.ToString("0000").Substring(2, 2) + tableDelimiter + "pack_ls";
                if (!TempTableInWebCashe(conn_db, tablePackLs)) continue;

                sql.Remove(0, sql.Length);
                sql.Append(" update " + tablePackLs + " set incase = 0 " +
                           " Where incase = 1 and " +
                           " nzp_pack_ls in (select nzp_pack_ls from " + tXX_case_full + " t where mark = 1 and year_ = " + year + ")");

                ret = ExecSQL(conn_db, sql.ToString(), true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return ret;
                }
            }
            conn_db.Close();
            return ret;
        }


        public List<Pack_ls> GetCasePack_ls(Pack_ls finder, out Returns ret)
        {
            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

#if PG
            string sql = "select yearr from " + Points.Pref + "_kernel.s_baselist where idtype = 4 order by yearr";
#else
            string sql = "select yearr from " + Points.Pref + "_kernel:s_baselist where idtype = 4 order by yearr";
#endif
            IDataReader reader = null;
            List<int> years = new List<int>();

            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }
            while (reader.Read())
                if (reader["yearr"] != DBNull.Value) years.Add(Convert.ToInt32(reader["yearr"]));

            CloseReader(ref reader);

            string table;
            List<Pack_ls> list = new List<Pack_ls>();
            int i = 0;
            bool stop = false;
            int count = 0;
            foreach (int year in years)
            {
#if PG
                table = Points.Pref + "_fin_" + year.ToString("0000").Substring(2, 2) + ".pack_ls";
#else
                table = Points.Pref + "_fin_" + year.ToString("0000").Substring(2, 2) + ":pack_ls";
#endif
                if (!TempTableInWebCashe(conn_db, table)) continue;

                var obj = ExecScalar(conn_db, "select count(*) from " + table + " where incase = " + finder.incase, out ret, true);
                if (!ret.result) return null;
                count += Convert.ToInt32(obj);

                if (stop) continue;

#if PG
                sql = " select pls.nzp_pack_ls, pls.nzp_pack, pls.prefix_ls, k.nzp_kvar,k.pref, pls.g_sum_ls, pls.sum_ls, " +
                    " pls.sum_peni, pls.dat_month, pls.dat_uchet, pls.kod_sum, pls.paysource, pls.id_bill, pls.incase, " +
                    " pls.dat_vvod, pls.info_num, pls.unl, pls.erc_code, pls.nzp_user, pls.inbasket, pls.alg , " +
                    " round(k.pkod)||'' as spkod, k.num_ls, k.nkvar, k.nkvar_n, k.fio, d.nzp_dom, d.ndom, d.nkor, u.nzp_ul, u.ulica, " +
                    " trim(" + sNvlWord + "(u.ulicareg,'улица')) ulicareg, r.rajon, t.town " +
                    " From  " + table + " pls " +
                    " left outer join " + Points.Pref + "_data.kvar k " +
                    " left outer join " + Points.Pref + "_data.dom d  " +
                    " left outer join " + Points.Pref + "_data.s_ulica u " +
                    " left outer join " + Points.Pref + "_data.s_rajon r " +
                    " left outer join " + Points.Pref + "_data.s_town t " +
                " on t.nzp_town = r.nzp_town on u.nzp_raj = r.nzp_raj  on d.nzp_ul = u.nzp_ul on k.nzp_dom = d.nzp_dom on pls.num_ls = k.num_ls" +
                    " Where pls.incase = " + finder.incase + " Order by info_num";
#else
                sql = " select pls.nzp_pack_ls, pls.nzp_pack, pls.prefix_ls, k.nzp_kvar,k.pref, pls.g_sum_ls, pls.sum_ls, " +
                      " pls.sum_peni, pls.dat_month, pls.dat_uchet, pls.kod_sum, pls.paysource, pls.id_bill, pls.incase, " +
                      " pls.dat_vvod, pls.info_num, pls.unl, pls.erc_code, pls.nzp_user, pls.inbasket, pls.alg , " +
                      " round(k.pkod)||'' as spkod, k.num_ls, k.nkvar, k.nkvar_n, k.fio, d.nzp_dom, d.ndom, d.nkor, u.nzp_ul, u.ulica, " +
                       " trim(" + sNvlWord + "(u.ulicareg,'улица')) ulicareg, r.rajon, t.town " +
                       " From  " + table + " pls, outer (" + Points.Pref + "_data:kvar k, " + Points.Pref + "_data:dom d," +
                       Points.Pref + "_data:s_ulica u, " + Points.Pref + "_data:s_town t," + Points.Pref + "_data:s_rajon r ) " +
                        "Where pls.num_ls = k.num_ls and k.nzp_dom = d.nzp_dom and d.nzp_ul = u.nzp_ul  and r.nzp_town = t.nzp_town and r.nzp_raj = u.nzp_raj " +
                        " and pls.incase = " + finder.incase + " Order by info_num";
#endif
                ret = ExecRead(conn_db, out reader, sql, true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return null;
                }
                try
                {
                    while (reader.Read())
                    {
                        i++;
                        if (finder.skip > 0 && i <= finder.skip) continue;

                        Pack_ls packls = new Pack_ls();
                        packls.year_ = year;
                        if (reader["nzp_kvar"] != DBNull.Value) packls.nzp_kvar = Convert.ToInt32(reader["nzp_kvar"]);
                        if (reader["num_ls"] != DBNull.Value) packls.num_ls = Convert.ToInt32(reader["num_ls"]);
                        if (reader["pref"] != DBNull.Value) packls.pref = Convert.ToString(reader["pref"]).Trim();
                        if (reader["nzp_pack"] != DBNull.Value) packls.nzp_pack = Convert.ToInt32(reader["nzp_pack"]);
                        if (reader["nzp_pack_ls"] != DBNull.Value) packls.nzp_pack_ls = Convert.ToInt32(reader["nzp_pack_ls"]);
                        if (reader["prefix_ls"] != DBNull.Value) packls.prefix_ls = Convert.ToInt32(reader["prefix_ls"]);
                        if (reader["g_sum_ls"] != DBNull.Value) packls.g_sum_ls = Convert.ToDecimal(reader["g_sum_ls"]);
                        if (reader["info_num"] != DBNull.Value) packls.info_num = Convert.ToInt32(reader["info_num"]);
                        if (reader["nzp_dom"] != DBNull.Value) packls.nzp_dom = Convert.ToInt32(reader["nzp_dom"]);
                        if (reader["nzp_ul"] != DBNull.Value) packls.nzp_ul = Convert.ToInt32(reader["nzp_ul"]);
                        if (reader["dat_vvod"] != DBNull.Value) packls.dat_vvod = Convert.ToString(reader["dat_vvod"]);
                        if (reader["sum_ls"] != DBNull.Value) packls.sum_ls = Convert.ToDecimal(reader["sum_ls"]);
                        if (reader["kod_sum"] != DBNull.Value) packls.kod_sum = Convert.ToInt32(reader["kod_sum"]);
                        if (reader["nkvar"] != DBNull.Value) packls.nkvar = Convert.ToString(reader["nkvar"]).Trim();
                        if (reader["nkvar_n"] != DBNull.Value) packls.nkvar_n = Convert.ToString(reader["nkvar_n"]).Trim();
                        if (reader["fio"] != DBNull.Value) packls.fio = Convert.ToString(reader["fio"]).Trim();
                        if (reader["ndom"] != DBNull.Value) packls.ndom = Convert.ToString(reader["ndom"]).Trim();
                        if (reader["nkor"] != DBNull.Value) packls.nkor = Convert.ToString(reader["nkor"]).Trim();
                        if (reader["ulica"] != DBNull.Value) packls.ulica = Convert.ToString(reader["ulicareg"]) + " " +
                                                                             Convert.ToString(reader["ulica"]).Trim();
                        if (reader["rajon"] != DBNull.Value) packls.rajon = Convert.ToString(reader["rajon"]).Trim();
                        if (reader["town"] != DBNull.Value) packls.town = Convert.ToString(reader["town"]).Trim();
                        if (reader["alg"] != DBNull.Value) packls.alg = Convert.ToString(reader["alg"]).Trim();
                        if (reader["inbasket"] != DBNull.Value) packls.inbasket = Convert.ToInt32(reader["inbasket"]);
                        packls.adr = packls.ulica;
                        if (packls.adr.Trim() != "") packls.adr += " / ";
                        packls.adr += packls.rajon;
                        if (packls.adr.Trim() != "") packls.adr += " / ";
                        packls.adr += packls.town;
                        if (packls.ndom != "" && packls.ndom != "-") packls.adr += ", д. " + packls.ndom;
                        if (packls.nkor != "" && packls.nkor != "-") packls.adr += ", корп. " + packls.nkor;
                        if (packls.nkvar != "" && packls.nkvar != "-") packls.adr += ", кв. " + packls.nkvar;
                        if (packls.nkvar_n != "" && packls.nkvar_n != "-") packls.adr += ", комн. " + packls.nkvar_n;

                        if (reader["spkod"] != DBNull.Value)
                        {
                            packls.pkod = Convert.ToString(reader["spkod"]).Trim();
                            string prefix = packls.pkod.Substring(0, 3);
                            int iprefix;
                            if (Int32.TryParse(prefix, out iprefix)) packls.prefix_ls = iprefix;
                        }
                        if (reader["dat_month"] != DBNull.Value) packls.dat_month = Convert.ToDateTime(reader["dat_month"]).ToShortDateString();
                        if (packls.inbasket == 1) packls.status = "В корзине";
                        else if (packls.incase == 1) packls.status = "В портфеле";
                        else if (packls.dat_uchet != "" && (packls.alg != "" && packls.alg != "0") && packls.inbasket == 0)
                        {
                            packls.status = "Распределена";
                        }
                        else packls.status = "Не распределена";
                        packls.mark = 0;
                        list.Add(packls);

                        if (finder.rows > 0 && i >= finder.skip + finder.rows)
                        {
                            stop = true;
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    ret = new Returns(false, ex.Message);
                    list = null;
                    MonitorLog.WriteLog("Ошибка GetCasePack_ls " + (Constants.Viewerror ? "\n " + ex.Message : ""), MonitorLog.typelog.Error, 20, 201, true);
                    break;
                }

            }

            conn_db.Close();
            reader.Close();

            if (ret.result) ret.tag = count;
            return list;

        }

        private List<Pack_ls> LoadListFinancePackLs(Pack_ls finder, out Returns ret, IDbConnection conn_db)
        {
            ret = Utils.InitReturns();
            //список квитанций
            List<Pack_ls> list = new List<Pack_ls>();
            try
            {
                //таблица с уточнениями по услугам
                string tablegilsums = Points.Pref + "_fin_" + (finder.year_ % 100).ToString("00") + tableDelimiter + "gil_sums";
                //квитанции 
                string tablePackLs = Points.Pref + "_fin_" + (finder.year_ % 100).ToString("00") + tableDelimiter + "pack_ls";
                //пачка
                string tablePack = Points.Pref + "_fin_" + (finder.year_ % 100).ToString("00") + tableDelimiter + "pack";
                //условия поиска
                string where = "";
                //фильтр по дате учета оплат
                if (finder.dat_uchet != "" || finder.dat_uchet_po != "")
                {
                    DateTime
                        dt = DateTime.MinValue,
                        dt2 = DateTime.MinValue;

                    if (finder.dat_uchet != "") DateTime.TryParse(finder.dat_uchet, out dt);
                    if (finder.dat_uchet_po != "") DateTime.TryParse(finder.dat_uchet_po, out dt2);

                    if (finder.dat_uchet != "" && finder.dat_uchet_po == "")
#if PG
                        where += " and pls.dat_uchet = public.MDY(" + dt.Month + "," + dt.Day + "," + dt.Year + ")";
#else
                    where += " and pls.dat_uchet = mdy(" + dt.Month + "," + dt.Day + "," + dt.Year + ")";
#endif
                    else
#if PG
                        where += " and coalesce(pls.dat_uchet, public.MDY(" + Points.DateOper.Month + "," + Points.DateOper.Day + "," + Points.DateOper.Year + "))" +
                                 " between public.MDY(" + dt.Month + "," + dt.Day + "," + dt.Year + ")" +
                                 " and public.MDY(" + dt2.Month + "," + dt2.Day + "," + dt2.Year + ")";
#else
                        where += " and nvl(pls.dat_uchet, mdy(" + Points.DateOper.Month + "," + Points.DateOper.Day + "," + Points.DateOper.Year + "))" +
                                 " between mdy(" + dt.Month + "," + dt.Day + "," + dt.Year + ") and mdy(" + dt2.Month + "," + dt2.Day + "," + dt2.Year + ")";
#endif
                }

                //фильтр по дате ввода оплат
                if (finder.dat_vvod != "" || finder.dat_vvod_po != "")
                {
                    DateTime
                        dt = DateTime.MinValue,
                        dt2 = DateTime.MinValue;

                    if (finder.dat_vvod != "") DateTime.TryParse(finder.dat_vvod, out dt);
                    if (finder.dat_vvod_po != "") DateTime.TryParse(finder.dat_vvod_po, out dt2);

                    if (finder.dat_vvod != "" && finder.dat_vvod_po == "")
#if PG
                        where += " and pls.dat_vvod = public.MDY(" + dt.Month + "," + dt.Day + "," + dt.Year + ")";
#else
                    where += " and pls.dat_vvod = mdy(" + dt.Month + "," + dt.Day + "," + dt.Year + ")";
#endif
                    else
#if PG
                        where += " and pls.dat_vvod between public.MDY(" + dt.Month + "," + dt.Day + "," + dt.Year + ") " +
                                 " and public.MDY(" + dt2.Month + "," + dt2.Day + "," + dt2.Year + ")";
#else
                        where += " and pls.dat_vvod between mdy(" + dt.Month + "," + dt.Day + "," + dt.Year + ") and mdy(" +
                                 dt2.Month + "," + dt2.Day + "," + dt2.Year + ")";
#endif
                }

                //фильтр по сумме оплаты
                if (finder.g_sum_ls > 0 || finder.g_sum_ls_po > 0)
                {
                    if (finder.g_sum_ls_po == 0) where += " and pls.g_sum_ls =" + finder.g_sum_ls;
                    else if (finder.g_sum_ls != 0) where += " and pls.g_sum_ls between " + finder.g_sum_ls + " and " + finder.g_sum_ls_po;
                    else where += " and pls.g_sum_ls =" + finder.g_sum_ls_po;
                }

                //фильтр по ЛС
                if (finder.num_ls > 0)
                {
                    if (Points.IsSmr) where += " and k.pkod10 = " + finder.num_ls;
                    else where += " and pls.num_ls = " + finder.num_ls;
                }

                //фильтр по платежному коду
                if (finder.pkod != "")
                    if (GlobalSettings.NewGeneratePkodMode) where += " and kp.pkod = " + finder.pkod.Trim();
                    else where += " and k.pkod = " + finder.pkod;

                //фильтр по адресу
                if (finder.nzp_town > 0) where += " and t.nzp_town = " + finder.nzp_town;
                if (finder.nzp_raj > 0) where += " and r.nzp_raj = " + finder.nzp_raj;
                if (finder.nzp_ul > 0) where += " and d.nzp_ul = " + finder.nzp_ul;
                if (finder.nzp_dom > 0) where += " and k.nzp_dom = " + finder.nzp_dom;
                if (finder.nzp_kvar > 0) where += " and k.nzp_kvar = " + finder.nzp_kvar;

                //фильтр по корзине
                if (finder.inbasket > 0)
                {
                    if (finder.inbasket == PriznakBasket.InBasket.GetHashCode()) where += " and pls.inbasket = 1 ";
                    else if (finder.inbasket == PriznakBasket.NotBasket.GetHashCode()) where += " and pls.inbasket <> 1 ";
                }

                //фильтр по статусу оплаты
                if (finder.status_for_opl > 0)
                {
                    if (finder.status_for_opl == PackLsOperations.Distribute.GetHashCode()) where += " and pls.dat_uchet is not null ";
                    else if (finder.status_for_opl == PackLsOperations.NotDistribute.GetHashCode()) where += " and pls.dat_uchet is null ";
                }

#if PG
                string sw = " Where pls.nzp_pack = p.nzp_pack ";
                string tkvr =
                    " left outer join " + Points.Pref + "_data.kvar k " +
                    " left outer join " + Points.Pref + "_data.dom d " +
                    " left outer join " + Points.Pref + "_data.s_ulica u " +
                    " left outer join " + Points.Pref + "_data.s_rajon r " +
                    " left outer join " + Points.Pref + "_data.s_town t " +
                    " on  r.nzp_town = t.nzp_town on  r.nzp_raj = u.nzp_raj on d.nzp_ul = u.nzp_ul " +
                    " on k.nzp_dom = d.nzp_dom on pls.num_ls = k.num_ls";

                if (finder.pkod != "" || (finder.num_ls > 0 && Points.IsSmr) || finder.nzp_ul > 0 || finder.nzp_town > 0 || finder.nzp_raj > 0 ||
                    finder.nzp_dom > 0 || finder.nzp_kvar > 0)
                {
                    tkvr = ", " + Points.Pref + "_data.kvar k, " +
                            (GlobalSettings.NewGeneratePkodMode ? Points.Pref + "_data" + tableDelimiter + "kvar_pkodes kp, " : "") +
                            Points.Pref + "_data.dom d, " +
                            Points.Pref + "_data.s_ulica u, " + Points.Pref + "_data.s_rajon r, " + Points.Pref +
                            "_data.s_town t ";
                    sw +=
                        " and pls.num_ls = k.num_ls and k.nzp_dom = d.nzp_dom and d.nzp_ul = u.nzp_ul and r.nzp_raj = u.nzp_raj " +
                        " and r.nzp_town = t.nzp_town ";
                    if (GlobalSettings.NewGeneratePkodMode) sw += " and kp.nzp_kvar = k.nzp_kvar ";
                }

                string sql = "Select count(*) From " + tablePack + " p," + tablePackLs + " pls" + tkvr + sw;
#else
            string tkvr = ", outer (" + Points.Pref + "_data:kvar k, " + Points.Pref + "_data:dom d," +
                       Points.Pref + "_data:s_ulica u, " + Points.Pref + "_data:s_rajon r , " + Points.Pref + "_data:s_town t " + ") ";
            if (finder.pkod != "" || (finder.num_ls > 0 && Points.IsSmr) || finder.nzp_ul > 0 ||
                finder.nzp_dom > 0 || finder.nzp_kvar > 0)
                tkvr = ", " + Points.Pref + "_data:kvar k, " + Points.Pref + "_data:dom d, " + 
                      Points.Pref + "_data:s_ulica u, " + Points.Pref + "_data:s_rajon r, " + Points.Pref + "_data:s_town t ";
            string sql = "Select count(*) From " + tablePackLs + " pls, " + tablePack + " p " + tkvr +
                " Where pls.nzp_pack = p.nzp_pack  and pls.num_ls = k.num_ls and k.nzp_dom = d.nzp_dom and d.nzp_ul = u.nzp_ul "+
                " and r.nzp_raj = u.nzp_raj and r.nzp_town = t.nzp_town ";
#endif

                if (finder.nzp_pack_ls > 0) sql += " and pls.nzp_pack_ls = " + finder.nzp_pack_ls;
                else if (finder.nzp_pack > 0) sql += " and pls.nzp_pack = " + finder.nzp_pack + where;
                else sql += where;

                int totalNumber = 0;
                object obj = ExecScalar(conn_db, sql, out ret, true);
                if (!ret.result)
                {
                    return null;
                }
                try
                {
                    totalNumber = Convert.ToInt32(obj);
                }
                catch (Exception ex)
                {
                    ret.result = false;
                    ret.text = ex.Message;
                    return null;
                }

                IDataReader reader;
                decimal tot_gsum_ls = 0;

#if PG
                sql = "Select sum(pls.g_sum_ls) as g_sum_ls " +
                      " From " + tablePackLs + " pls " + tkvr + ", " +
                      tablePack + " p " +
                      " left outer join " + Points.Pref + "_kernel.s_bank b" +
                      " left outer join " + Points.Pref +
                      "_kernel.s_payer py on b.nzp_payer = py.nzp_payer on p.nzp_bank = b.nzp_bank" +
                      " " + sw;
#else
            sql = "Select sum(pls.g_sum_ls) as g_sum_ls " +
                " From " + tablePackLs + " pls, " + tablePack +
                " p, outer (" + Points.Pref + "_kernel:s_bank b, outer " + Points.Pref + "_kernel:s_payer py)" +
                    " " + tkvr +
                " Where pls.nzp_pack = p.nzp_pack and pls.num_ls = k.num_ls and k.nzp_dom = d.nzp_dom and d.nzp_ul = u.nzp_ul "+
                " and r.nzp_raj = u.nzp_raj and r.nzp_town = t.nzp_town " +
                " and p.nzp_bank = b.nzp_bank and b.nzp_payer = py.nzp_payer";
#endif

                if (finder.nzp_pack_ls > 0) sql += " and pls.nzp_pack_ls = " + finder.nzp_pack_ls;
                else if (finder.nzp_pack > 0) sql += " and pls.nzp_pack = " + finder.nzp_pack + where;
                else sql += where;
                ret = ExecRead(conn_db, out reader, sql, true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return null;
                }
                if (reader.Read())
                {
                    if (reader["g_sum_ls"] != DBNull.Value) tot_gsum_ls = Convert.ToDecimal(reader["g_sum_ls"]);
                }

#if PG
                sql = "Select pls.nzp_pack_ls, pls.nzp_pack, pls.prefix_ls, r.rajon, t.town,  k.nzp_kvar, k.pref," +
                      " pls.g_sum_ls, pls.sum_ls, pls.sum_peni, pls.dat_month, pls.dat_uchet, pls.calc_month," +
                      ", pls.kod_sum, pls.alg, pls.paysource, pls.id_bill,pls.distr_month, pls.incase, pls.dat_vvod, pls.info_num," +
                      " pls.unl, pls.erc_code, pls.nzp_user, pls.inbasket, p.nzp_bank, b.nzp_payer, py.payer" +
                      (GlobalSettings.NewGeneratePkodMode ? ", pls.pkod spkod" : ", round(k.pkod)||'' as spkod") + ", k.num_ls, k.nkvar, k.nkvar_n, k.fio, d.nzp_dom, d.ndom, d.nkor," +
                      " u.nzp_ul, u.ulica, trim(coalesce(ulicareg,'улица')) ulicareg, ks.comment, plsp.payer plspayer, plss.name_supp plssupplier, pls.nzp_payer plsnzp_payer, pls.nzp_supp plsnzp_supp  " +
                      " From " + tablePack + " p  left outer join " + Points.Pref + "_kernel.s_bank b " +
                      " left outer join " + Points.Pref +
                      "_kernel.s_payer py on b.nzp_payer = py.nzp_payer on p.nzp_bank = b.nzp_bank, " +
                      tablePackLs + " pls " + " " + 
                      " left outer join " + Points.Pref + sKernelAliasRest + "kodsum ks on ks.kod = pls.kod_sum " + 
                      " left outer join " + Points.Pref + sKernelAliasRest + "s_payer plsp on plsp.nzp_payer = pls.nzp_payer " +
                      " left outer join " + Points.Pref + sKernelAliasRest + "supplier plss on plss.nzp_supp = pls.nzp_supp " + 
                      tkvr
                      + sw;
                ;
#else
            sql = "Select pls.nzp_pack_ls, pls.nzp_pack, pls.prefix_ls, r.rajon, t.town, k.nzp_kvar, k.pref, pls.g_sum_ls, pls.sum_ls, pls.sum_peni, pls.dat_month, pls.dat_uchet" +
                ", pls.kod_sum, pls.alg, pls.paysource, pls.id_bill, pls.incase, pls.dat_vvod, pls.info_num, pls.unl, pls.erc_code, pls.nzp_user, pls.inbasket, p.nzp_bank, plsp.payer plspayer, plss.name_supp plssupplier, pls.nzp_payer plsnzp_payer, pls.nzp_supp plsnzp_supp, b.nzp_payer, py.payer" +
                (GlobalSettings.NewGeneratePkodMode?", pls.pkod spkod": ", round(k.pkod)||'' as spkod") + ", k.num_ls, k.nkvar, k.nkvar_n, k.fio, d.nzp_dom, d.ndom, d.nkor, u.nzp_ul, u.ulica, trim(nvl(ulicareg,'улица')) ulicareg " +
                " From " + tablePackLs + " pls, " + tablePack + " p, outer (" + Points.Pref + "_kernel:s_bank b, outer " + Points.Pref + "_kernel:s_payer py)" +
                    " " + " outer " + Points.Pref + "_kernel" + tableDelimiter + "s_payer plsp, outer " + Points.Pref + "_kernel:supplier plss " +
                 tkvr +
                " Where pls.nzp_pack = p.nzp_pack and pls.num_ls = k.num_ls and k.nzp_dom = d.nzp_dom and d.nzp_ul = u.nzp_ul "+
                " and r.nzp_raj = u.nzp_raj and r.nzp_town = t.nzp_town " +
                " and p.nzp_bank = b.nzp_bank and b.nzp_payer = py.nzp_payer and plsp.nzp_payer = pls.nzp_payer and plss.nzp_supp = pls.nzp_supp ";
#endif
                if (finder.nzp_pack_ls > 0) sql += " and pls.nzp_pack_ls = " + finder.nzp_pack_ls;
                else if (finder.nzp_pack > 0) sql += " and pls.nzp_pack = " + finder.nzp_pack + where;
                else sql += where;
                sql += " Order by info_num DESC, spkod, g_sum_ls";


                ret = ExecRead(conn_db, out reader, sql, true);
                if (!ret.result)
                {
                    return null;
                }


                try
                {
                    int i = 0;
                    while (reader.Read())
                    {
                        i++;
                        if (i <= finder.skip) continue;
                        Pack_ls packls = new Pack_ls();
                        packls.year_ = finder.year_;
                        packls.num = i.ToString();
                        if (reader["nzp_pack_ls"] != DBNull.Value)
                            packls.nzp_pack_ls = Convert.ToInt32(reader["nzp_pack_ls"]);
                        if (reader["nzp_pack"] != DBNull.Value) packls.nzp_pack = Convert.ToInt32(reader["nzp_pack"]);

                        if (reader["spkod"] != DBNull.Value)
                        {
                            packls.pkod = Convert.ToString(reader["spkod"]).Trim();
                            if (packls.pkod.Length >= 3)
                            {
                                string prefix = packls.pkod.Substring(0, 3);
                                int iprefix;
                                if (Int32.TryParse(prefix, out iprefix)) packls.prefix_ls = iprefix;
                            }
                        }

                        if (reader["nzp_kvar"] != DBNull.Value) packls.nzp_kvar = Convert.ToInt32(reader["nzp_kvar"]);
                        if (reader["num_ls"] != DBNull.Value) packls.num_ls = Convert.ToInt32(reader["num_ls"]);
                        if (reader["pref"] != DBNull.Value) packls.pref = Convert.ToString(reader["pref"]).Trim();
                        if (reader["g_sum_ls"] != DBNull.Value) packls.g_sum_ls = Convert.ToDecimal(reader["g_sum_ls"]);
                        if (reader["sum_ls"] != DBNull.Value) packls.sum_ls = Convert.ToDecimal(reader["sum_ls"]);
                        if (reader["sum_peni"] != DBNull.Value) packls.sum_peni = Convert.ToDecimal(reader["sum_peni"]);
                        if (reader["dat_month"] != DBNull.Value)
                            packls.dat_month = Convert.ToDateTime(reader["dat_month"]).ToShortDateString();
                        if (reader["kod_sum"] != DBNull.Value) packls.kod_sum = Convert.ToInt32(reader["kod_sum"]);
                        if (reader["comment"] != DBNull.Value)
                            packls.kod_sum_name += packls.kod_sum + " " + Convert.ToString(reader["comment"]);
                        if (reader["paysource"] != DBNull.Value)
                            packls.paysource = Convert.ToInt32(reader["paysource"]);
                        if (reader["id_bill"] != DBNull.Value) packls.id_bill = Convert.ToInt32(reader["id_bill"]);
                        if (reader["dat_vvod"] != DBNull.Value)
                            packls.dat_vvod = Convert.ToDateTime(reader["dat_vvod"]).ToShortDateString();
                        if (reader["distr_month"] != DBNull.Value)
                            packls.distr_month = Convert.ToDateTime(reader["distr_month"]).ToShortDateString();
                        if (reader["dat_uchet"] != DBNull.Value)
                            packls.dat_uchet = Convert.ToDateTime(reader["dat_uchet"]).ToShortDateString();
                        if (reader["calc_month"] != DBNull.Value)
                            packls.calc_month = Convert.ToDateTime(reader["calc_month"]).ToShortDateString();
                        if (reader["info_num"] != DBNull.Value) packls.info_num = Convert.ToInt32(reader["info_num"]);
                        if (reader["unl"] != DBNull.Value) packls.unl = Convert.ToInt32(reader["unl"]);
                        if (reader["erc_code"] != DBNull.Value)
                            packls.erc_code = Convert.ToString(reader["erc_code"]).Trim();
                        if (reader["nzp_user"] != DBNull.Value) packls.nzp_user = Convert.ToInt32(reader["nzp_user"]);
                        if (reader["inbasket"] != DBNull.Value) packls.inbasket = Convert.ToInt32(reader["inbasket"]);
                        if (reader["incase"] != DBNull.Value) packls.incase = Convert.ToInt32(reader["incase"]);

                        if (reader["nzp_bank"] != DBNull.Value) packls.nzp_bank = Convert.ToInt32(reader["nzp_bank"]);
                        if (reader["nzp_payer"] != DBNull.Value) packls.nzp_payer = Convert.ToInt32(reader["nzp_payer"]);

                        if (reader["plsnzp_payer"] != DBNull.Value) packls.nzp_payer_contragent = Convert.ToInt32(reader["plsnzp_payer"]);
                        if (reader["plsnzp_supp"] != DBNull.Value) packls.nzp_supp = Convert.ToInt32(reader["plsnzp_supp"]);

                        if (reader["payer"] != DBNull.Value) packls.payer = Convert.ToString(reader["payer"]).Trim();
                        if (reader["alg"] != DBNull.Value) packls.alg = Convert.ToString(reader["alg"]).Trim();

                        if (packls.inbasket == 1) packls.status = "В корзине";
                        else if (packls.incase == 1) packls.status = "В портфеле";
                        else if (packls.dat_uchet != "" && (packls.alg != "" && packls.alg != "0") && packls.inbasket == 0)
                        {
                            packls.status = "Распределена";
                        }
                        else packls.status = "Не распределена";

                        if (reader["nzp_dom"] != DBNull.Value) packls.nzp_dom = Convert.ToInt32(reader["nzp_dom"]);
                        if (reader["nzp_ul"] != DBNull.Value) packls.nzp_ul = Convert.ToInt32(reader["nzp_ul"]);

                        if (reader["nkvar"] != DBNull.Value) packls.nkvar = Convert.ToString(reader["nkvar"]).Trim();
                        if (reader["nkvar_n"] != DBNull.Value)
                            packls.nkvar_n = Convert.ToString(reader["nkvar_n"]).Trim();
                        if (reader["fio"] != DBNull.Value) packls.fio = Convert.ToString(reader["fio"]).Trim();
                        if (reader["ndom"] != DBNull.Value) packls.ndom = Convert.ToString(reader["ndom"]).Trim();
                        if (reader["nkor"] != DBNull.Value) packls.nkor = Convert.ToString(reader["nkor"]).Trim();
                        if (reader["ulica"] != DBNull.Value)
                            packls.ulica = Convert.ToString(reader["ulicareg"]) + " " +
                                           Convert.ToString(reader["ulica"]).Trim();
                        if (reader["rajon"] != DBNull.Value) packls.rajon = Convert.ToString(reader["rajon"]).Trim();
                        if (reader["town"] != DBNull.Value) packls.town = Convert.ToString(reader["town"]).Trim();

                        packls.adr = packls.ulica;
                        if (packls.adr.Trim() != "") packls.adr += " / ";
                        packls.adr += packls.rajon;
                        if (packls.adr.Trim() != "") packls.adr += " / ";
                        packls.adr += packls.town;
                        if (packls.ndom != "" && packls.ndom != "-") packls.adr += ", д. " + packls.ndom;
                        if (packls.nkor != "" && packls.nkor != "-") packls.adr += ", корп. " + packls.nkor;
                        if (packls.nkvar != "" && packls.nkvar != "-") packls.adr += ", кв. " + packls.nkvar;
                        if (packls.nkvar_n != "" && packls.nkvar_n != "-") packls.adr += ", комн. " + packls.nkvar_n;

                        packls.is_manual = false;
                        if (finder.nzp_pack_ls > 0 && TempTableInWebCashe(conn_db, tablegilsums))
                        {
                            sql = "select count(*) from " + tablegilsums + " where nzp_pack_ls = " + packls.nzp_pack_ls +
                                  " and num_ls = " + packls.num_ls;
                            object count = ExecScalar(conn_db, sql, out ret, true);
                            int recordsTotalCount;
                            try
                            {
                                recordsTotalCount = Convert.ToInt32(count);
                            }
                            catch (Exception e)
                            {
                                ret = new Returns(false,
                                    "Ошибка при определении количества записей: " + (Constants.Debug ? e.Message : ""));
                                MonitorLog.WriteLog("Ошибка LoadGeu " + (Constants.Viewerror ? "\n" + e.Message : ""),
                                    MonitorLog.typelog.Error, 20, 201, true);
                                conn_db.Close();
                                return null;
                            }
                            packls.is_manual = recordsTotalCount > 0;
                        }

                        if (reader["plspayer"] != DBNull.Value) packls.plspayer = Convert.ToString(reader["plspayer"]).Trim();
                        if (reader["plssupplier"] != DBNull.Value) packls.plssupplier = Convert.ToString(reader["plssupplier"]).Trim();
                        list.Add(packls);

                        if (finder.rows > 0 && i >= finder.rows + finder.skip) break;
                    }
                }
                catch (Exception ex)
                {
                    ret = new Returns(false, ex.Message);
                    list = null;

                    MonitorLog.WriteLog(
                        "Ошибка LoadListFinancePackLs " + (Constants.Viewerror ? "\n " + ex.Message : ""),
                        MonitorLog.typelog.Error, 20, 201, true);
                }
                if (ret.result) ret.tag = totalNumber;

                Pack_ls pkls = new Pack_ls();
                pkls.g_sum_ls = tot_gsum_ls;

                list.Add(pkls);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка получения списка оплат пачки";
                MonitorLog.WriteLog(
                    "Ошибка глобальная LoadListFinancePackLs " + "\n " + ex.Message + " " + ex.StackTrace,
                    MonitorLog.typelog.Error, true);
            }
            return list;
        }

        public Returns SaveFinancePackLs(Pack_ls finder)
        { 
            DateTime datVvod; 
            DateTime datMonth;
            IDbTransaction transaction = null;

            #region Проверка входных параметров
            if (finder.nzp_user < 1) return new Returns(false, "Пользователь не определен");
            if (finder.nzp_pack_ls < 1) return new Returns(false, "Не задана квитанция об оплате");
            if (finder.nzp_pack < 1) return new Returns(false, "Не задана пачка");
            if (finder.year_ < 1) return new Returns(false, "Год не задан");
            if (finder.nzp_kvar < 1) return new Returns(false, "Не задан лицевой счет");
            if (finder.dat_vvod == "") return new Returns(false, "Дата оплаты не задана");
            if (!DateTime.TryParse(finder.dat_vvod, out datVvod)) return new Returns(false, "Неверно задана дата оплаты");
            if (finder.dat_month == "") return new Returns(false, "Месяц оплаты не задан");
            if (!DateTime.TryParse(finder.dat_month, out datMonth))  return new Returns(false, "Неверно задан месяц оплаты");
            if (finder.g_sum_ls == 0) return new Returns(false, "Сумма оплаты не задана");
            #endregion

            var connectionString = Points.GetConnByPref(Points.Pref);
            var connDb = GetConnection(connectionString);
            var ret = OpenDb(connDb, true);
            if (!ret.result) return ret;

            IDataReader reader;

            var nzpUser = finder.nzp_user;

            var isBlocked = IsBlockedRecord(nzpUser, finder.nzp_pack_ls, finder.year_, connDb, out ret);
            if (!ret.result) return ret;
            if (isBlocked == 1) return new Returns(false, "Редактирование квитанции невозможно, так как запись заблокирована другим пользователем", -1);
            
            var tablePackLs = Points.Pref + "_fin_" + (finder.year_ % 100).ToString("00") + tableDelimiter + "pack_ls";
            var tablePack = Points.Pref + "_fin_" + (finder.year_ % 100).ToString("00") + tableDelimiter + "pack";
            var gilSums = Points.Pref + "_fin_" + (finder.year_ % 100).ToString("00") + tableDelimiter + "gil_sums";

            try
            {
                if (!TempTableInWebCashe(connDb, tablePackLs))  return new Returns(false, "Данные не найдены");

                var sb = new StringBuilder();
                sb.AppendFormat(
                    "select p.pack_type, p.nzp_supp, p.nzp_payer, pls.kod_sum from {0} p, {1} pls where pls.nzp_pack = p.nzp_pack and nzp_pack_ls = {2}",
                    tablePack, tablePackLs, finder.nzp_pack_ls);
                ret = ExecRead(connDb, out reader, sb.ToString(), true);
                if (!ret.result) return ret;
                var pack = new Pack_ls();
                if (reader.Read())
                {
                    if (reader["pack_type"] != DBNull.Value) pack.pack_type = Convert.ToInt32(reader["pack_type"]);
                    if (reader["nzp_supp"] != DBNull.Value) pack.nzp_supp = Convert.ToInt32(reader["nzp_supp"]);
                    if (reader["nzp_payer"] != DBNull.Value) pack.nzp_payer = Convert.ToInt32(reader["nzp_payer"]);
                    if (reader["kod_sum"] != DBNull.Value) pack.kod_sum = Convert.ToInt32(reader["kod_sum"]);
                }

                if (pack.nzp_supp != finder.nzp_supp) return new Returns(false, "Договор оплаты отличается от договора пачки", -1);
                if (pack.nzp_payer != finder.nzp_payer) return new Returns(false, "Принципал оплаты отличается от принципала пачки", -1);   

                string dop = "";
                if (finder.kod_sum > 0) dop += ", kod_sum = " + finder.kod_sum;
                if (finder.nzp_supp > 0 && finder.kod_sum == 50)
                {
                    sb = new StringBuilder();
                    sb.AppendFormat("select count(*) from {0} where nzp_pack_ls = {1} and nzp_supp <> {2}", gilSums, finder.nzp_pack_ls, finder.nzp_supp);

                    var obj = ExecScalar(connDb, sb.ToString(), out ret, true);
                    if (!ret.result) return ret;

                    int cnt;
                    Int32.TryParse(obj.ToString(), out cnt);
                    if (cnt > 0) return new Returns(false, "Нельзя изменить договор, т.к. есть уточненные суммы на другой договор", -121);
                    dop += ", nzp_supp = " + finder.nzp_supp + ", nzp_payer = null";
                }
                else if (finder.nzp_payer > 0 && finder.kod_sum == 49)
                {
                    sb = new StringBuilder();
                    sb.AppendFormat("select count(*) from {0} where nzp_pack_ls = {1} and sum_oplat<>0 and nzp_supp not in (select nzp_supp from {3} where nzp_payer_princip = {2})",
                        gilSums, finder.nzp_pack_ls, finder.nzp_payer, Points.Pref + "_kernel" + tableDelimiter + "supplier");

                    var obj = ExecScalar(connDb, sb.ToString(), out ret, true);
                    if (!ret.result) return ret;

                    int cnt;
                    Int32.TryParse(obj.ToString(), out cnt);
                    if (cnt > 0) return new Returns(false, "Нельзя изменить принципала, т.к. есть уточненные суммы на другого принципала", -121);
                    dop += ", nzp_payer =" + finder.nzp_payer + ", nzp_supp = null";
                }
                else dop += ", nzp_supp = null, nzp_payer = null";

                transaction = connDb.BeginTransaction();
                var fieldpkod = "";
                if (GlobalSettings.NewGeneratePkodMode && finder.pkod != "") fieldpkod = " , pkod = " + finder.pkod;

                var sql = "update " + tablePackLs + " set dat_vvod = " + Utils.EStrNull(finder.dat_vvod) +
                        ", num_ls = (select num_ls from " + Points.Pref + "_data" + tableDelimiter +"kvar where nzp_kvar = " + finder.nzp_kvar + ")" +
                        ", dat_month = " + Utils.EStrNull(finder.dat_month) + fieldpkod +
                        ", g_sum_ls = " + finder.g_sum_ls.ToString("F2") + dop +
                        " where nzp_pack_ls = " + finder.nzp_pack_ls;

                ret = ExecSQL(connDb, transaction, sql, true);
                if (!ret.result) return ret;

                //обновить таблицу
                sql = "select inbasket, dat_uchet from " + tablePackLs + " where nzp_pack_ls = " + finder.nzp_pack_ls;
                if (!ExecRead(connDb, transaction, out reader, sql, true).result) return ret;
                var datUchet = "";
                var inbasket = 0;
                if (reader.Read())
                {
                    if (reader["dat_uchet"] != DBNull.Value) datUchet = Convert.ToDateTime(reader["dat_uchet"]).ToShortDateString();
                    if (reader["inbasket"] != DBNull.Value) inbasket = Convert.ToInt32(reader["inbasket"]);
                }

                if (inbasket == 1 || datUchet == "")
                {
                    sql = "update " + tablePack + " set sum_pack = (select sum(g_sum_ls) " +
                        " from " + tablePackLs + " pls where pls.nzp_pack = " + finder.nzp_pack + " ) where nzp_pack = " + finder.nzp_pack;
                    ret = ExecSQL(connDb, transaction, sql, true);
                    if (!ret.result) return ret;

                    sql = "select par_pack from " + tablePack + " where nzp_pack = " + finder.nzp_pack;
                    if (!ExecRead(connDb, transaction, out reader, sql, true).result) return ret;

                    var parPack = 0;
                    if (reader.Read()) if (reader["par_pack"] != DBNull.Value) parPack = Convert.ToInt32(reader["par_pack"]);
                    if (parPack > 0)
                    {
                        sql = "update " + tablePack + " set sum_pack = (select sum(sum_pack) " +
                        " from " + tablePack + " p where p.par_pack = " + parPack + " and p.nzp_pack <>" + parPack + ") where nzp_pack = " + parPack;
                        ret = ExecSQL(connDb, transaction, sql, true);
                        if (!ret.result) return ret;
                    }
                }

                return ret;
            }
            catch (Exception)
            {
                if (transaction != null) transaction.Rollback();
            }
            finally
            {
                if (transaction != null) transaction.Commit();
                connDb.Close();
            }
            return ret;
        }


        public Returns DeleteBlockFromPackLs(Pack_ls finder)
        {
            #region Проверка входных параметров
            if (finder.nzp_user < 1)
            {
                return new Returns(false, "Пользователь не определен");
            }
            if (finder.nzp_pack_ls < 1)
            {
                return new Returns(false, "Не задана квитанция об оплате");
            }
            if (finder.year_ < 1)
            {
                return new Returns(false, "Год не задан");
            }
            #endregion
            Returns ret;
            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;

            #region Определить пользователя
            finder.pref = Points.Pref;
            int nzpUser = finder.nzp_user; //локальный пользователь      
            
            /*DbWorkUser db = new DbWorkUser();
            int nzpUser = db.GetLocalUser(conn_db, finder, out ret); //локальный пользователь      
            db.Close();
            if (!ret.result) return ret;*/
            #endregion

            #region Удалить все записи блокировки
            ret = ExecSQL(conn_db,
#if PG
 "delete from " + Points.Pref + "_data.pack_ls_block where nzp_pack_ls = " + finder.nzp_pack_ls +
                    " and year_ = " + finder.year_ + " and nzp_user = " + nzpUser
#else
                    "delete from " + Points.Pref + "_data:pack_ls_block where nzp_pack_ls = " + finder.nzp_pack_ls +
                    " and year_ = " + finder.year_ + " and nzp_user = "+nzpUser
#endif
, true);

            if (!ret.result)
            {
                ret.result = false;
                ret.text = "Ошибка удаления из таблицы pack_ls_block";
                return ret;
            }
            #endregion

            return ret;
        }

        /// <summary>
        /// Получить все банковские реквизиты по nzp_payer
        /// </summary>
        /// <param name="finder">nzp_payer</param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public List<BankRequisites> GetBankRequisites(BankRequisites finder, out Returns ret)
        {
            #region Проверка входных параметров
            ret = Utils.InitReturns();
            if (finder.nzp_payer <= 0)
            {
                ret.result = false;
                ret.text = "Не задан подрядчик";
                ret.tag = -1;
                return null;
            }
            #endregion

            IDbConnection conn_db = null;
            IDataReader reader = null;
            StringBuilder sql = new StringBuilder();

            List<BankRequisites> retList = new List<BankRequisites>();

            try
            {
                #region Открываем соединение с базами
                conn_db = GetConnection(Constants.cons_Kernel);

                ret = OpenDb(conn_db, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return null;
                }
                #endregion

                DbTables tables = new DbTables(DBManager.getServer(conn_db));

                sql.Append(" select   fb.nzp_fb, fb.nzp_payer, fb.nzp_payer_bank, p.payer, fb.num_count, fb.bank_name, fb.rcount, fb.kcount, fb.bik, ");
                sql.Append(" fb.npunkt, fb.nzp_user, fb.dat_when, fb.is_default ");

#if PG
                sql.Append(" from " + Points.Pref + "_data" + tableDelimiter + "fn_bank fb left outer join " + tables.payer + " p on p.nzp_payer = fb.nzp_payer_bank  ");
#else
                sql.Append(" from " + Points.Pref + "_data" + tableDelimiter + "fn_bank fb, outer " + tables.payer + " p ");
#endif
                sql.Append(" where 1=1 ");
                if (finder.nzp_payer > 0) sql.Append(" and fb.nzp_payer = " + finder.nzp_payer + " ");
#if PG
                sql.Append(" ");
#else
                sql.Append(" and p.nzp_payer = fb.nzp_payer_bank ");
#endif
                sql.Append(" order by  fb.num_count ASC");

                if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return null;
                }

                if (reader != null)
                {
                    while (reader.Read())
                    {
                        BankRequisites br = new BankRequisites();

                        if (reader["nzp_fb"] != DBNull.Value) br.nzp_fb = Convert.ToInt32(reader["nzp_fb"]);
                        if (reader["nzp_payer"] != DBNull.Value) br.nzp_payer = Convert.ToInt32(reader["nzp_payer"]);
                        if (reader["nzp_payer_bank"] != DBNull.Value) br.nzp_payer_bank = Convert.ToInt32(reader["nzp_payer_bank"]);
                        if (reader["num_count"] != DBNull.Value) br.num_count = Convert.ToInt32(reader["num_count"]);
                        if (reader["payer"] != DBNull.Value) br.bank_name = Convert.ToString(reader["payer"]).Trim();
                        if (reader["rcount"] != DBNull.Value) br.rcount = Convert.ToString(reader["rcount"]).Trim();
                        if (reader["kcount"] != DBNull.Value) br.kcount = Convert.ToString(reader["kcount"]).Trim();
                        if (reader["bik"] != DBNull.Value) br.bik = Convert.ToString(reader["bik"]).Trim();
                        if (reader["npunkt"] != DBNull.Value) br.npunkt = Convert.ToString(reader["npunkt"]).Trim();
                        if (reader["nzp_user"] != DBNull.Value) br.nzp_user = Convert.ToInt32(reader["nzp_user"]);
                        if (reader["dat_when"] != DBNull.Value) br.dat_when = Convert.ToString(reader["dat_when"]).Trim();
                        if (reader["is_default"] != DBNull.Value) br.is_default = Convert.ToInt32(reader["is_default"]);
                        retList.Add(br);
                    }
                }

                return retList;

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetBankRequisites : " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                #region Закрытие соединений

                if (conn_db != null)
                {
                    conn_db.Close();
                }

                if (reader != null)
                {
                    reader.Close();
                }

                sql.Remove(0, sql.Length);

                #endregion
            }
        }

        public List<BankRequisites> NewFdGetBankRequisites(BankRequisites finder, out Returns ret)
        {
            IDbConnection conn_db = null;
            IDataReader reader = null;
            var sql = new StringBuilder();
            var retList = new List<BankRequisites>();
            ret = Utils.InitReturns();
            try
            {
                #region Открываем соединение с базами
                conn_db = GetConnection(Constants.cons_Kernel);

                ret = OpenDb(conn_db, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return null;
                }
                #endregion

                var tables = new DbTables(DBManager.getServer(conn_db));

                sql.Append(" select nzp_fb, fb.nzp_payer, fb.nzp_payer_bank, p.payer bank,   fb.rcount, p.ks, p.bik, p.city , pp.payer poluch ");
                sql.Append(" from " + Points.Pref + "_data" + tableDelimiter + 
                    "fn_bank fb left outer join " + tables.payer + " p on p.nzp_payer = fb.nzp_payer_bank,  ");
                sql.AppendFormat(" {0}_kernel{1}s_payer pp ", Points.Pref, tableDelimiter);
                sql.Append(" where pp.nzp_payer = fb.nzp_payer ");
                if (finder.nzp_payer > 0) sql.Append(" and fb.nzp_payer = " + finder.nzp_payer + " ");
                if (finder.nzp_fb > 0) sql.Append(" and fb.nzp_fb = " + finder.nzp_fb + " ");
                if (finder.nzp_payer_bank > 0) sql.Append(" and fb.nzp_payer_bank = " + finder.nzp_payer_bank + " ");
         
                if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return null;
                }

                if (reader == null) return retList;

                while (reader.Read())
                {
                    var br = new BankRequisites();
                 
                    if (reader["nzp_fb"] != DBNull.Value) br.nzp_fb = Convert.ToInt32(reader["nzp_fb"]);
                    if (reader["nzp_payer"] != DBNull.Value) br.nzp_payer = Convert.ToInt32(reader["nzp_payer"]);
                    if (reader["nzp_payer_bank"] != DBNull.Value) br.nzp_payer_bank = Convert.ToInt32(reader["nzp_payer_bank"]);
                    if (reader["bank"] != DBNull.Value) br.bank_name = Convert.ToString(reader["bank"]).Trim();
                    if (reader["rcount"] != DBNull.Value) br.rcount = Convert.ToString(reader["rcount"]).Trim();
                    if (reader["poluch"] != DBNull.Value) br.poluch = Convert.ToString(reader["poluch"]).Trim();
                    if (reader["ks"] != DBNull.Value) br.kcount = Convert.ToString(reader["ks"]).Trim();
                    if (reader["bik"] != DBNull.Value) br.bik = Convert.ToString(reader["bik"]).Trim();
                    if (reader["city"] != DBNull.Value) br.npunkt = Convert.ToString(reader["city"]).Trim();
                    br.rasch_full = br.rcount + " - " + br.poluch;
                    retList.Add(br);
                }

                return retList;

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetBankRequisites : " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                #region Закрытие соединений

                if (conn_db != null)
                {
                    conn_db.Close();
                }

                if (reader != null)
                {
                    reader.Close();
                }

                sql.Remove(0, sql.Length);

                #endregion
            }
        }

        public List<BankRequisites> GetRsForERCDogovor(BankRequisites finder, out Returns ret)
        {
            if (finder.nzp_fd <= 0)
            {
                ret = new Returns(false, "Не указан договор", -1);
                return null;
            }
            IDbConnection conn_db = null;
            IDataReader reader = null;
            var sql = new StringBuilder();
            var retList = new List<BankRequisites>();
            ret = Utils.InitReturns();
            try
            {
                #region Открываем соединение с базами
                conn_db = GetConnection(Constants.cons_Kernel);

                ret = OpenDb(conn_db, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return null;
                }
                #endregion

                var tables = new DbTables(DBManager.getServer(conn_db));

                sql.Append("select lnkb.id, lnkb.nzp_fb, lnkb.max_sum, lnkb.min_sum, lnkb.naznplat, lnkb.priznak_perechisl, pb.payer bank_name, b.rcount, b.note, pb.bik, pb.ks, pb.city, p.payer poluch, b.nzp_payer, b.nzp_payer_bank ");
                sql.AppendFormat("from {0}_data{1}fn_dogovor_bank_lnk lnkb, ", Points.Pref, tableDelimiter);
                sql.AppendFormat("{0}_data{1}fn_bank b ", Points.Pref, tableDelimiter);
                sql.AppendFormat("left outer join {0}_kernel{1}s_payer pb on b.nzp_payer_bank = pb.nzp_payer ",
                    Points.Pref, tableDelimiter);
                sql.AppendFormat("left outer join {0}_kernel{1}s_payer p on b.nzp_payer = p.nzp_payer ",
                    Points.Pref, tableDelimiter);
                sql.Append(" where lnkb.nzp_fb = b.nzp_fb ");
                if (finder.nzp_fd > 0) sql.AppendFormat(" and lnkb.nzp_fd = {0}", finder.nzp_fd);
                if (finder.id > 0) sql.AppendFormat(" and lnkb.id = {0}", finder.id);
                if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return null;
                }

                if (reader == null) return retList;

                while (reader.Read())
                {
                    var br = new BankRequisites();
                    if (reader["id"] != DBNull.Value) br.id = Convert.ToInt32(reader["id"]);
                    if (reader["nzp_fb"] != DBNull.Value) br.nzp_fb = Convert.ToInt32(reader["nzp_fb"]);
                    if (reader["nzp_payer"] != DBNull.Value) br.nzp_payer = Convert.ToInt32(reader["nzp_payer"]);
                    if (reader["nzp_payer_bank"] != DBNull.Value) br.nzp_payer_bank = Convert.ToInt32(reader["nzp_payer_bank"]);
                    if (reader["bank_name"] != DBNull.Value) br.bank_name = Convert.ToString(reader["bank_name"]).Trim();
                    if (reader["rcount"] != DBNull.Value) br.rcount = Convert.ToString(reader["rcount"]).Trim();
                    if (reader["ks"] != DBNull.Value) br.kcount = Convert.ToString(reader["ks"]).Trim();
                    if (reader["bik"] != DBNull.Value) br.bik = Convert.ToString(reader["bik"]).Trim();
                    if (reader["city"] != DBNull.Value) br.npunkt = Convert.ToString(reader["city"]).Trim();
                    if (reader["poluch"] != DBNull.Value) br.poluch = Convert.ToString(reader["poluch"]).Trim();
                    if (reader["max_sum"] != DBNull.Value) br.max_sum = Convert.ToDecimal(reader["max_sum"]);
                    if (reader["min_sum"] != DBNull.Value) br.min_sum = Convert.ToDecimal(reader["min_sum"]);
                    if (reader["priznak_perechisl"] != DBNull.Value) br.priznak_perechisl = Convert.ToInt32(reader["priznak_perechisl"]);
                    if (reader["naznplat"] != DBNull.Value) br.naznplat =reader["naznplat"].ToString();
                    br.rasch_full = br.rcount + " - " + br.poluch;
                    retList.Add(br);
                }

                return retList;

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetBankRequisites : " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                #region Закрытие соединений

                if (conn_db != null)
                {
                    conn_db.Close();
                }

                if (reader != null)
                {
                    reader.Close();
                }

                sql.Remove(0, sql.Length);

                #endregion
            }
        }

        /// <summary>
        /// Получить все реквизиты договора по nzp_payer
        /// </summary>
        /// <param name="finder">nzp_payer</param>
        /// <param name="finder">nzp_area</param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public List<DogovorRequisites> GetDogovorList(DogovorRequisites finder, out Returns ret)
        {
            #region Проверка входных параметров
            ret = Utils.InitReturns();
            if (finder.nzp_fd < 1)
            {
                if (finder.nzp_payer <= 0)
                {
                    ret = new Returns(false, "Не задан подрядчик");
                    return null;
                }
                /*     if (finder.nzp_area <= 0)
                     {
                         ret = new Returns(false, "Не задана управляющая организация");
                         return null;
                     }*/
            }
            #endregion

            IDbConnection conn_db = null;
            MyDataReader reader = null;
            MyDataReader secReader = null;
            StringBuilder sql = new StringBuilder();

            List<DogovorRequisites> retList = new List<DogovorRequisites>();

            try
            {
                conn_db = GetConnection(Constants.cons_Kernel);
                ret = OpenDb(conn_db, true);
                if (!ret.result) return null;

                sql.Append(" select fd.nzp_fd,fd.naznplat, fd.nzp_area, a.area, fd.nzp_payer_ar, fd.nzp_payer, fd.nzp_fb, fd.nzp_osnov, fd.num_dog, fd.dat_s,fd.dat_po, fd.max_sum,");
                sql.Append(" fd.dat_dog, fd.target, fd.kpp, fd.min_sum, fd.priznak_perechisl, fd.nzp_user, fd.dat_when, fo.osnov, (bn.bank_name || ' р/с ' || bn. rcount) as rschet ");
                sql.Append(" from " + Points.Pref + "_data" + DBManager.tableDelimiter + "fn_dogovor fd, ");
                sql.Append(" " + Points.Pref + "_data" + DBManager.tableDelimiter + "fn_osnov fo, ");
                sql.Append(" " + Points.Pref + "_data" + DBManager.tableDelimiter + "s_area a, ");
                sql.Append(" " + Points.Pref + "_data" + DBManager.tableDelimiter + "fn_bank bn ");
                sql.Append(" where a.nzp_area = fd.nzp_area and fo.nzp_osnov = fd.nzp_osnov ");
                sql.Append(" and bn.nzp_fb = fd.nzp_fb ");
                if (finder.nzp_fd > 0) sql.Append(" and fd.nzp_fd = " + finder.nzp_fd);
                if (finder.nzp_payer > 0) sql.Append(" and fd.nzp_payer = " + finder.nzp_payer);
                if (finder.nzp_area > 0) sql.Append(" and fd.nzp_area = " + finder.nzp_area);

                ret = ExecRead(conn_db, out reader, sql.ToString(), true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return null;
                }

                while (reader.Read())
                {
                    DogovorRequisites dr = new DogovorRequisites();

                    if (reader["nzp_fd"] != DBNull.Value) dr.nzp_fd = Convert.ToInt32(reader["nzp_fd"]);
                    if (reader["nzp_area"] != DBNull.Value) dr.nzp_area = Convert.ToInt32(reader["nzp_area"]);
                    if (reader["nzp_payer_ar"] != DBNull.Value) dr.nzp_payer_ar = Convert.ToInt32(reader["nzp_payer_ar"]);
                    if (reader["nzp_payer"] != DBNull.Value) dr.nzp_payer = Convert.ToInt32(reader["nzp_payer"]);
                    if (reader["nzp_fb"] != DBNull.Value) dr.nzp_fb = Convert.ToInt32(reader["nzp_fb"]);
                    if (reader["nzp_osnov"] != DBNull.Value) dr.nzp_osnov = Convert.ToInt32(reader["nzp_osnov"]);

                    if (reader["naznplat"] != DBNull.Value) dr.naznplat = Convert.ToString(reader["naznplat"]);
                    if (reader["num_dog"] != DBNull.Value) dr.num_dog = Convert.ToString(reader["num_dog"]).Trim();
                    if (reader["area"] != DBNull.Value) dr.area = Convert.ToString(reader["area"]).Trim();
                    if (reader["dat_dog"] != DBNull.Value)
                    {
                        DateTime datDog;
                        DateTime.TryParse(Convert.ToString(reader["dat_dog"]).Trim(), out datDog);
                        dr.dat_dog = datDog != DateTime.MinValue ? datDog.ToShortDateString() : Convert.ToString(reader["dat_dog"]).Trim();
                    }
                    if (reader["dat_s"] != DBNull.Value)
                    {
                        DateTime dats;
                        DateTime.TryParse(Convert.ToString(reader["dat_s"]).Trim(), out dats);
                        dr.period_deistv = dats != DateTime.MinValue ? dats.ToShortDateString() : Convert.ToString(reader["dat_s"]).Trim();
                        dr.dat_s = dats != DateTime.MinValue ? dats.ToShortDateString() : Convert.ToString(reader["dat_s"]).Trim();
                    }
                    if (reader["dat_po"] != DBNull.Value)
                    {
                        DateTime datpo;
                        DateTime.TryParse(Convert.ToString(reader["dat_po"]).Trim(), out datpo);
                        if (dr.period_deistv != "") dr.period_deistv += " - ";
                        dr.period_deistv += datpo != DateTime.MinValue ? datpo.ToShortDateString() : Convert.ToString(reader["dat_po"]).Trim();
                        dr.dat_po = datpo != DateTime.MinValue ? datpo.ToShortDateString() : Convert.ToString(reader["dat_po"]).Trim();
                    }
                    if (reader["max_sum"] != DBNull.Value) dr.max_sum = Convert.ToDecimal(reader["max_sum"]);
                    if (reader["min_sum"] != DBNull.Value) dr.min_sum = Convert.ToDecimal(reader["min_sum"]);
                    if (reader["target"] != DBNull.Value) dr.target = Convert.ToString(reader["target"]).Trim();
                    if (reader["kpp"] != DBNull.Value) dr.kpp = Convert.ToString(reader["kpp"]).Trim();
                    if (reader["nzp_user"] != DBNull.Value) dr.nzp_user = Convert.ToInt32(reader["nzp_user"]);
                    if (reader["priznak_perechisl"] != DBNull.Value) dr.priznak_perechisl = Convert.ToInt32(reader["priznak_perechisl"]);
                    if (dr.priznak_perechisl == 2) dr.vrazrserv = "Да";
                    else dr.vrazrserv = "";
                    if (reader["dat_when"] != DBNull.Value) dr.dat_when = Convert.ToString(reader["dat_when"]).Trim();
                    if (reader["osnov"] != DBNull.Value) dr.osnov = Convert.ToString(reader["osnov"]).Trim();
                    if (reader["rschet"] != DBNull.Value) dr.rschet = Convert.ToString(reader["rschet"]).Trim();

                    #region получение списка истоников(банков), если берется инфа только по одному договору
                    if (finder.nzp_fd > 0)
                    {
                        string sqlStr = "select nzp_bank from " + Points.Pref + "_data" + tableDelimiter + "fn_dogovor_bank where nzp_fd = " + finder.nzp_fd;
                        ret = ExecRead(conn_db, out secReader, sqlStr, true);
                        if (!ret.result)
                        {
                            conn_db.Close();
                            return null;
                        }
                        dr.bank_list = new System.Collections.ArrayList();
                        while (secReader.Read())
                        {
                            dr.bank_list.Add(Convert.ToInt32(secReader["nzp_bank"]));
                        }
                    }
                    #endregion

                    retList.Add(dr);
                }

                return retList;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetDogovorRequisites : " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                #region Закрытие соединений

                if (reader != null)
                {
                    reader.Close();
                }

                if (conn_db != null && conn_db.State != ConnectionState.Closed)
                {
                    conn_db.Close();
                }

                sql.Remove(0, sql.Length);

                #endregion
            }
        }

        

        public List<DogovorRequisites> GetDogovorERCList(DogovorRequisites finder, out Returns ret)
        {
            #region Проверка входных параметров
            ret = Utils.InitReturns();
            #endregion

            IDbConnection conn_db = null;
            MyDataReader reader = null;
            MyDataReader readerAddressLS = null;
            MyDataReader secReader = null;
            StringBuilder sql = new StringBuilder();
            StringBuilder sql1 = new StringBuilder();

            List<DogovorRequisites> retList = new List<DogovorRequisites>();

            try
            {
                conn_db = GetConnection(Constants.cons_Kernel);
                ret = OpenDb(conn_db, true);
                if (!ret.result) return null;

                sql1.Append(" select fd.nzp_fd, p1.payer payer_agent, p2.payer payer_princip, " +
                           "  fd.num_dog, fd.dat_s,fd.dat_po, fd.nzp_payer_princip, fd.nzp_payer_agent, ");
                sql1.Append(" fd.dat_dog, fd.target, fd.nzp_user, fd.dat_when, fd.nzp_scope ");

                sql.Append(" from " + Points.Pref + "_data" + DBManager.tableDelimiter + "fn_dogovor fd ");
            //    sql.Append(", " + Points.Pref + "_data" + DBManager.tableDelimiter + "fn_osnov fo ");
                sql.Append(", " + Points.Pref + "_kernel" + DBManager.tableDelimiter + "s_payer p1 ");
                sql.Append(", " + Points.Pref + "_kernel" + DBManager.tableDelimiter + "s_payer p2 ");
                sql.Append(" where 1=1 " +
                           //"fo.nzp_osnov = fd.nzp_osnov " +
                           "and p1.nzp_payer = fd.nzp_payer_agent and p2.nzp_payer = fd.nzp_payer_princip ");
                if (finder.nzp_fd > 0) sql.Append(" and fd.nzp_fd = " + finder.nzp_fd);
                if (finder.nzp_kvar > 0)
                {
                    try
                    {
                        string sqlKvar = " Select nzp_kvar, k.nzp_wp, t.nzp_town, r.nzp_raj, u.nzp_ul, d.nzp_dom from " +
                                         Points.Pref + sDataAliasRest + "kvar k " +
                                         "left outer join " + Points.Pref + sDataAliasRest + "dom d on (k.nzp_dom=d.nzp_dom) " +
                                         "left outer join  " + Points.Pref + sDataAliasRest + "s_ulica u on (d.nzp_ul=u.nzp_ul) " +
                                         "left outer join  " + Points.Pref + sDataAliasRest + "s_rajon r on (u.nzp_raj=r.nzp_raj) " +
                                         "left outer join  " + Points.Pref + sDataAliasRest + "s_town t on (r.nzp_town=t.nzp_town) " +
                                         "where nzp_kvar=" + finder.nzp_kvar;
                        ret = ExecRead(conn_db, out readerAddressLS, sqlKvar, true);
                        if (!ret.result)
                        {
                            conn_db.Close();
                            return retList;
                        }
                        Dom addressLS = new Dom();
                        while (readerAddressLS.Read())
                        {
                            if (readerAddressLS["nzp_wp"] != DBNull.Value) addressLS.nzp_wp = Convert.ToInt32(readerAddressLS["nzp_wp"]);
                            if (readerAddressLS["nzp_town"] != DBNull.Value) addressLS.nzp_town = Convert.ToInt32(readerAddressLS["nzp_town"]);
                            if (readerAddressLS["nzp_raj"] != DBNull.Value) addressLS.nzp_raj = Convert.ToInt32(readerAddressLS["nzp_raj"]);
                            if (readerAddressLS["nzp_ul"] != DBNull.Value) addressLS.nzp_ul = Convert.ToInt32(readerAddressLS["nzp_ul"]);
                            if (readerAddressLS["nzp_dom"] != DBNull.Value) addressLS.nzp_dom = Convert.ToInt32(readerAddressLS["nzp_dom"]);
                            break;
                        }
                        sql.Append(" and nzp_scope in (select nzp_scope from " + Points.Pref + sDataAliasRest + "fn_scope_adres fa ");
                        sql.Append("where fa.nzp_wp=" + addressLS.nzp_wp + " and (fa.nzp_town is null OR fa.nzp_town<=0 OR ");
                        sql.Append("(fa.nzp_town=" + addressLS.nzp_town + " and (fa.nzp_raj is null OR fa.nzp_raj<=0 OR ");
                        sql.Append("(fa.nzp_raj=" + addressLS.nzp_raj + " and (fa.nzp_ul is null OR fa.nzp_ul<=0 OR ");
                        sql.Append("fa.nzp_ul=" + addressLS.nzp_ul + " and (fa.nzp_dom is null OR fa.nzp_dom<=0 OR fa.nzp_dom=" + addressLS.nzp_dom + ")))))))");
                        readerAddressLS.Close();
                    }
                    catch (Exception ex)
                    {
                        if (readerAddressLS != null) readerAddressLS.Close();
                        conn_db.Close();
                        MonitorLog.WriteLog("Ошибка выполнения процедуры GetDogovorRequisites : " + ex.Message, MonitorLog.typelog.Error, true);
                        return null;
                    }
                }  
                else
                {
                    if (finder.list_nzp_wp.Count > 0)
                    {
                        sql.Append(" and nzp_scope in (select nzp_scope from " + Points.Pref + sDataAliasRest + "fn_scope_adres where nzp_wp in (" + String.Join(",", finder.list_nzp_wp) + "))");
                    }
                }
                //if (finder.nzp_payer > 0) sql.Append(" and fd.nzp_payer = " + finder.nzp_payer);
                //if (finder.nzp_supp > 0) sql.Append(" and fd.nzp_supp = " + finder.nzp_supp);
                StringBuilder where = new StringBuilder();
                if (finder.nzp_payer_princip > 0) where.AppendFormat(" and fd.nzp_payer_princip = {0} ", finder.nzp_payer_princip);
                if (finder.nzp_payer_agent > 0) where.AppendFormat(" and fd.nzp_payer_agent = {0} ", finder.nzp_payer_agent);
                if (finder.num_dog != "") where.AppendFormat(" and lower(fd.num_dog) like '%{0}%' ", finder.num_dog.Trim().ToLower());
                if (finder.dat_dog != "") where.AppendFormat(" and fd.dat_dog = '{0}' ", finder.dat_dog.Trim());
                if (finder.dat_dog_s != "" && finder.dat_dog_po != "")
                {
                    where.AppendFormat(" and fd.dat_dog between '{0}' and '{1}' ", finder.dat_dog_s.Trim(), finder.dat_dog_po.Trim());
                }
                if (finder.dat_dog_s != "" && finder.dat_dog_po == "")
                {
                    where.AppendFormat(" and fd.dat_dog = '{0}' ", finder.dat_dog_s.Trim());                   
                }
                if (finder.dat_dog_s == "" && finder.dat_dog_po != "")
                {
                    where.AppendFormat(" and fd.dat_dog <= '{0}' ", finder.dat_dog_po.Trim());
                }
                if (finder.dat_s != "") where.AppendFormat(" and fd.dat_s = '{0}' ", finder.dat_s.Trim());
                if (finder.dat_po != "") where.AppendFormat(" and fd.dat_po = '{0}' ", finder.dat_po.Trim());
                sql.Append(where);

                object obj = ExecScalar(conn_db, "select count(*) " + sql, out ret, true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return null;
                }
                int cnt = 0;
                if (obj != null) cnt = Convert.ToInt32(obj);
                string orderby = " order by  payer_princip, payer_agent, fd.num_dog, fd.dat_dog ";
                sql.Append(orderby);
                ret = ExecRead(conn_db, out reader, sql1 + sql.ToString(), true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return null;
                }
                int i = 0;
                while (reader.Read())
                {
                    i++;
                    if (i <= finder.skip) continue;
                    DogovorRequisites dr = new DogovorRequisites();

                    if (reader["nzp_fd"] != DBNull.Value) dr.nzp_fd = Convert.ToInt32(reader["nzp_fd"]);
                    //if (reader["nzp_supp"] != DBNull.Value) dr.nzp_supp = Convert.ToInt32(reader["nzp_supp"]);
                    if (reader["nzp_payer_agent"] != DBNull.Value) dr.nzp_payer_agent = Convert.ToInt32(reader["nzp_payer_agent"]);
                    if (reader["nzp_payer_princip"] != DBNull.Value) dr.nzp_payer_princip = Convert.ToInt32(reader["nzp_payer_princip"]);
                    //if (reader["nzp_fb"] != DBNull.Value) dr.nzp_fb = Convert.ToInt32(reader["nzp_fb"]);
                    //if (reader["nzp_osnov"] != DBNull.Value) dr.nzp_osnov = Convert.ToInt32(reader["nzp_osnov"]);
                 //   if (reader["naznplat"] != DBNull.Value) dr.naznplat = Convert.ToString(reader["naznplat"]);
                    if (reader["payer_agent"] != DBNull.Value) dr.agent = Convert.ToString(reader["payer_agent"]);
                    if (reader["payer_princip"] != DBNull.Value) dr.principal = Convert.ToString(reader["payer_princip"]);
                    if (reader["num_dog"] != DBNull.Value) dr.num_dog = Convert.ToString(reader["num_dog"]).Trim();
                    //if (reader["name_supp"] != DBNull.Value) dr.name_supp = Convert.ToString(reader["name_supp"]).Trim();
                    if (reader["dat_dog"] != DBNull.Value)
                    {
                        DateTime datDog;
                        DateTime.TryParse(Convert.ToString(reader["dat_dog"]).Trim(), out datDog);
                        dr.dat_dog = datDog != DateTime.MinValue ? datDog.ToShortDateString() : Convert.ToString(reader["dat_dog"]).Trim();
                    }
                    if (reader["dat_s"] != DBNull.Value)
                    {
                        DateTime dats;
                        DateTime.TryParse(Convert.ToString(reader["dat_s"]).Trim(), out dats);
                        dr.period_deistv = dats != DateTime.MinValue ? dats.ToShortDateString() : Convert.ToString(reader["dat_s"]).Trim();
                        dr.dat_s = dats != DateTime.MinValue ? dats.ToShortDateString() : Convert.ToString(reader["dat_s"]).Trim();
                    }
                    if (reader["dat_po"] != DBNull.Value)
                    {
                        DateTime datpo;
                        DateTime.TryParse(Convert.ToString(reader["dat_po"]).Trim(), out datpo);
                        if (dr.period_deistv != "") dr.period_deistv += " - ";
                        dr.period_deistv += datpo != DateTime.MinValue ? datpo.ToShortDateString() : Convert.ToString(reader["dat_po"]).Trim();
                        dr.dat_po = datpo != DateTime.MinValue ? datpo.ToShortDateString() : Convert.ToString(reader["dat_po"]).Trim();
                    }
                   if (reader["target"] != DBNull.Value) dr.target = Convert.ToString(reader["target"]).Trim();
                    //if (reader["kpp"] != DBNull.Value) dr.kpp = Convert.ToString(reader["kpp"]).Trim();
                    if (reader["nzp_user"] != DBNull.Value) dr.nzp_user = Convert.ToInt32(reader["nzp_user"]);
                    if (dr.priznak_perechisl == 2) dr.vrazrserv = "Да";
                    else dr.vrazrserv = "";
                    if (reader["dat_when"] != DBNull.Value) dr.dat_when = Convert.ToString(reader["dat_when"]).Trim();
                    //if (reader["osnov"] != DBNull.Value) dr.osnov = Convert.ToString(reader["osnov"]).Trim();
                    //if (reader["rschet"] != DBNull.Value) dr.rschet = Convert.ToString(reader["rschet"]).Trim();
                    if (reader["nzp_scope"] != DBNull.Value) dr.nzp_scope = Convert.ToInt32(reader["nzp_scope"]);

                    //#region получение списка истоников(банков), если берется инфа только по одному договору
                    //if (finder.nzp_fd > 0)
                    //{
                    //    string sqlStr = "select nzp_bank from " + Points.Pref + "_data" + tableDelimiter + "fn_dogovor_bank where nzp_fd = " + finder.nzp_fd;
                    //    ret = ExecRead(conn_db, out secReader, sqlStr, true);
                    //    if (!ret.result)
                    //    {
                    //        conn_db.Close();
                    //        return null;
                    //    }
                    //    dr.bank_list = new System.Collections.ArrayList();
                    //    while (secReader.Read())
                    //    {
                    //        dr.bank_list.Add(Convert.ToInt32(secReader["nzp_bank"]));
                    //    }
                    //}
                    //#endregion

                    retList.Add(dr);
                    if (finder.rows > 0 && i >= finder.skip + finder.rows) break;
                }
                ret.tag = cnt;
                return retList;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetDogovorRequisites : " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                #region Закрытие соединений

                if (reader != null)
                {
                    reader.Close();
                }

                if (conn_db != null && conn_db.State != ConnectionState.Closed)
                {
                    conn_db.Close();
                }

                sql.Remove(0, sql.Length);

                #endregion
            }
        }

        public List<DogovorRequisites> GetDogovorListSupp(DogovorRequisites finder, out Returns ret)
        {
            #region Проверка входных параметров
            ret = Utils.InitReturns();
            if (finder.nzp_fd < 1)
            {
                if (finder.nzp_payer <= 0)
                {
                    ret = new Returns(false, "Не задан подрядчик");
                    return null;
                }
            }
            #endregion

            IDbConnection conn_db = null;
            MyDataReader reader = null;
            MyDataReader secReader = null;
            StringBuilder sql = new StringBuilder();

            List<DogovorRequisites> retList = new List<DogovorRequisites>();

            try
            {
                conn_db = GetConnection(Constants.cons_Kernel);
                ret = OpenDb(conn_db, true);
                if (!ret.result) return null;

                sql.Append(" select fd.nzp_fd,fd.naznplat, fd.nzp_supp, a.name_supp, fd.nzp_payer_ar, fd.nzp_payer, fd.nzp_fb, fd.nzp_osnov, fd.num_dog, fd.dat_s,fd.dat_po, fd.max_sum,");
                sql.Append(" fd.dat_dog, fd.target, fd.kpp, fd.min_sum, fd.priznak_perechisl, fd.nzp_user, fd.dat_when, fo.osnov, (bn.bank_name || ' р/с ' || bn. rcount) as rschet ");

                sql.Append(" from " + Points.Pref + "_data" + DBManager.tableDelimiter + "fn_dogovor fd, ");
                sql.Append(" " + Points.Pref + "_data" + DBManager.tableDelimiter + "fn_osnov fo, ");
                sql.Append(" " + Points.Pref + "_kernel" + DBManager.tableDelimiter + "supplier a, ");
                sql.Append(" " + Points.Pref + "_data" + DBManager.tableDelimiter + "fn_bank bn ");

                sql.Append(" where a.nzp_supp = fd.nzp_supp and fo.nzp_osnov = fd.nzp_osnov ");
                sql.Append(" and bn.nzp_fb = fd.nzp_fb ");
                if (finder.nzp_fd > 0) sql.Append(" and fd.nzp_fd = " + finder.nzp_fd);
                if (finder.nzp_payer > 0) sql.Append(" and fd.nzp_payer = " + finder.nzp_payer);
                if (finder.nzp_supp > 0) sql.Append(" and fd.nzp_supp = " + finder.nzp_supp);

                ret = ExecRead(conn_db, out reader, sql.ToString(), true);
                if (!ret.result)
                {
                    conn_db.Close();
                    return null;
                }

                while (reader.Read())
                {
                    DogovorRequisites dr = new DogovorRequisites();

                    if (reader["nzp_fd"] != DBNull.Value) dr.nzp_fd = Convert.ToInt32(reader["nzp_fd"]);
                    if (reader["nzp_supp"] != DBNull.Value) dr.nzp_supp = Convert.ToInt32(reader["nzp_supp"]);
                    if (reader["nzp_payer_ar"] != DBNull.Value) dr.nzp_payer_ar = Convert.ToInt32(reader["nzp_payer_ar"]);
                    if (reader["nzp_payer"] != DBNull.Value) dr.nzp_payer = Convert.ToInt32(reader["nzp_payer"]);
                    if (reader["nzp_fb"] != DBNull.Value) dr.nzp_fb = Convert.ToInt32(reader["nzp_fb"]);
                    if (reader["nzp_osnov"] != DBNull.Value) dr.nzp_osnov = Convert.ToInt32(reader["nzp_osnov"]);

                    if (reader["naznplat"] != DBNull.Value) dr.naznplat = Convert.ToString(reader["naznplat"]);
                    if (reader["num_dog"] != DBNull.Value) dr.num_dog = Convert.ToString(reader["num_dog"]).Trim();
                    if (reader["name_supp"] != DBNull.Value) dr.name_supp = Convert.ToString(reader["name_supp"]).Trim();
                    if (reader["dat_dog"] != DBNull.Value)
                    {
                        DateTime datDog;
                        DateTime.TryParse(Convert.ToString(reader["dat_dog"]).Trim(), out datDog);
                        dr.dat_dog = datDog != DateTime.MinValue ? datDog.ToShortDateString() : Convert.ToString(reader["dat_dog"]).Trim();
                    }
                    if (reader["dat_s"] != DBNull.Value)
                    {
                        DateTime dats;
                        DateTime.TryParse(Convert.ToString(reader["dat_s"]).Trim(), out dats);
                        dr.period_deistv = dats != DateTime.MinValue ? dats.ToShortDateString() : Convert.ToString(reader["dat_s"]).Trim();
                        dr.dat_s = dats != DateTime.MinValue ? dats.ToShortDateString() : Convert.ToString(reader["dat_s"]).Trim();
                    }
                    if (reader["dat_po"] != DBNull.Value)
                    {
                        DateTime datpo;
                        DateTime.TryParse(Convert.ToString(reader["dat_po"]).Trim(), out datpo);
                        if (dr.period_deistv != "") dr.period_deistv += " - ";
                        dr.period_deistv += datpo != DateTime.MinValue ? datpo.ToShortDateString() : Convert.ToString(reader["dat_po"]).Trim();
                        dr.dat_po = datpo != DateTime.MinValue ? datpo.ToShortDateString() : Convert.ToString(reader["dat_po"]).Trim();
                    }
                    if (reader["max_sum"] != DBNull.Value) dr.max_sum = Convert.ToDecimal(reader["max_sum"]);
                    if (reader["min_sum"] != DBNull.Value) dr.min_sum = Convert.ToDecimal(reader["min_sum"]);
                    if (reader["target"] != DBNull.Value) dr.target = Convert.ToString(reader["target"]).Trim();
                    if (reader["kpp"] != DBNull.Value) dr.kpp = Convert.ToString(reader["kpp"]).Trim();
                    if (reader["nzp_user"] != DBNull.Value) dr.nzp_user = Convert.ToInt32(reader["nzp_user"]);
                    if (reader["priznak_perechisl"] != DBNull.Value) dr.priznak_perechisl = Convert.ToInt32(reader["priznak_perechisl"]);
                    if (dr.priznak_perechisl == 2) dr.vrazrserv = "Да";
                    else dr.vrazrserv = "";
                    if (reader["dat_when"] != DBNull.Value) dr.dat_when = Convert.ToString(reader["dat_when"]).Trim();
                    if (reader["osnov"] != DBNull.Value) dr.osnov = Convert.ToString(reader["osnov"]).Trim();
                    if (reader["rschet"] != DBNull.Value) dr.rschet = Convert.ToString(reader["rschet"]).Trim();
                   

                    #region получение списка истоников(банков), если берется инфа только по одному договору
                    if (finder.nzp_fd > 0)
                    {
                        string sqlStr = "select nzp_bank from " + Points.Pref + "_data" + tableDelimiter + "fn_dogovor_bank where nzp_fd = " + finder.nzp_fd;
                        ret = ExecRead(conn_db, out secReader, sqlStr, true);
                        if (!ret.result)
                        {
                            conn_db.Close();
                            return null;
                        }
                        dr.bank_list = new System.Collections.ArrayList();
                        while (secReader.Read())
                        {
                            dr.bank_list.Add(Convert.ToInt32(secReader["nzp_bank"]));
                        }
                    }
                    #endregion

                    retList.Add(dr);
                }

                return retList;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetDogovorRequisites : " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                #region Закрытие соединений

                if (reader != null)
                {
                    reader.Close();
                }

                if (conn_db != null && conn_db.State != ConnectionState.Closed)
                {
                    conn_db.Close();
                }

                sql.Remove(0, sql.Length);

                #endregion
            }
        }


        /// <summary>
        /// Получить список банков
        /// </summary>
        /// <param name="finder">nzp_payer</param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public List<BankRequisites> GetSourceBankList(BankRequisites finder, out Returns ret)
        {
            IDbConnection conn_db = null;
            MyDataReader reader = new MyDataReader();
            string sql = "";
            ret = Utils.InitReturns();

            List<BankRequisites> retList = new List<BankRequisites>();

            try
            {
                #region Открываем соединение с базами
                conn_db = GetConnection(Constants.cons_Kernel);

                ret = OpenDb(conn_db, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return null;
                }
                #endregion

                sql = " select nzp_bank, bank from " + Points.Pref + "_kernel" + tableDelimiter + "s_bank b, " + Points.Pref + "_kernel" + tableDelimiter + "s_payer p " +
                    " where b.nzp_payer = p.nzp_payer and p.nzp_type = 4 " +
                    " order by b.bank ";

                ExecRead(conn_db, out reader, sql, true);
                if (reader != null)
                    while (reader.Read())
                    {
                        if (reader["nzp_bank"] != DBNull.Value && reader["bank"] != DBNull.Value)
                            retList.Add(new BankRequisites() { nzp_bank = Convert.ToInt32(reader["nzp_bank"]), bank = reader["bank"].ToString().Trim() });
                    }
                return retList;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetSourceBankList : " + ex.Message, MonitorLog.typelog.Error, true);
                return new List<BankRequisites>();
            }
            finally
            {
                #region Закрытие соединений

                if (conn_db != null)
                {
                    conn_db.Close();
                }

                if (reader != null)
                {
                    reader.Close();
                }

                sql.Remove(0, sql.Length);

                #endregion
            }
        }



        /// <summary>
        /// Получить все реквизиты контракта
        /// </summary>
        /// <param name="finder">num_ls</param>
        /// <param name="finder">flag</param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public List<ContractRequisites> GetContractList(ContractRequisites finder, out Returns ret)
        {
            #region Проверка входных параметров

            ret = Utils.InitReturns();

            if (finder.nzp_kvar <= 0)
            {
                ret.text = "Не задан номер лицевого счета";
                return null;
            }

            if (finder.area_flag != 1 && finder.area_flag != 2)
            {
                ret.text = "Не задан флаг";
                return null;
            }

            if (finder.pref.Trim() == "")
            {
                ret.text = "Не задан префикс";
                return null;
            }

            #endregion

            IDbConnection conn_db = null;
            IDataReader reader = null;
            StringBuilder sql = new StringBuilder();
            string table_name = "fn_lsdogovor";


            List<ContractRequisites> retList = new List<ContractRequisites>();

            try
            {
                #region Открываем соединение с базой

                conn_db = GetConnection(Constants.cons_Kernel);

                ret = OpenDb(conn_db, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    ret.text = "Ошибка при получении данных о контрактах";
                    return null;
                }

                #endregion

                #region Создание таблицы

                string conn_kernel = Points.GetConnByPref(finder.pref);
                IDbConnection db = GetConnection(conn_kernel);

                ret = OpenDb(db, true);
                if (!ret.result)
                {
                    return null;
                }

#if PG
                ret = ExecSQL(conn_db, "set search_path to '" + Points.Pref + "_data'", true);
#else
                ret = ExecSQL(conn_db, " Database " + Points.Pref + "_data", true);
#endif

                if (!TempTableInWebCashe(db, table_name))
                {
                    try
                    {
                        ret = ExecSQL(db,
#if PG
 " Create table " + table_name +
#else
 " Create table are." + table_name +
#endif
 " ( nzp_con        serial not null, " +
                                  "   nzp_fb         integer not null, " +
                                  "   nzp_osnov      integer not null, " +
                                  "   nzp_kvar       integer not null, " +
                                  "   num_dog        char(30), " +
                                  "   nzp_payer      integer not null, " +
                                  "   dat_s          date, " +
                                  "   dat_po         date, " +
                                  "   comment        char(100) " +
                                  " ) ", true);
                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog("Ошибка при создании таблицы " + table_name + ex.Message, MonitorLog.typelog.Error, true);
                        ret.text = "Ошибка при получении данных о контрактах";
                        return null;
                    }
                    ret = ExecSQL(db, " Create distinct index " +
#if PG
 "" +
#else
 "are." +
#endif
 "ix_contracts_1 on " + table_name + " (nzp_con) ", true);
                    if (!ret.result)
                    {
                        ret.text = "Ошибка при получении данных о контрактах";
                        conn_db.Close();
                        return null;
                    }
                    ret = ExecSQL(db, " Create index " +
#if PG
 "" +
#else
 "are." +
#endif
 "ix_contracts_2 on " + table_name + " (nzp_kvar) ", true);
                    if (!ret.result)
                    {
                        ret.text = "Ошибка при получении данных о контрактах";
                        conn_db.Close();
                        return null;
                    }
                }
                db.Close();

                #endregion

                sql.Remove(0, sql.Length);

                sql.Append(" Select distinct a.nzp_con, " +
                           " a.nzp_fb, " +
                           " a.num_dog, " +
                           " b.payer, " +
                           " b.nzp_supp, " +
                           " a.dat_s, " +
                           " a.dat_po, " +
                           "'' kod_supp,"+
                           " d.rcount, " +
                           " e.nzp_osnov, " +
                           " e.osnov, " +
                           " d.bank_name, " +
                           " d.bik, " +
                           " d.kcount, " +
                           " d.npunkt, " +
                           " a.comment ");

                sql.AppendFormat(" From {0}_data{1}fn_lsdogovor a, " +
                                "{0}_kernel{1}s_payer b, " +
                                "{0}_data{1}fn_bank d, " +
                                "{0}_data{1}fn_osnov e", Points.Pref, tableDelimiter);

                if (finder.area_flag == 1)
                {
                    sql.AppendFormat(", {0}_data{1}s_area f ", Points.Pref, tableDelimiter);
                    sql.Append(" Where a.nzp_fb = d.nzp_fb ");
                    sql.Append(" AND e.nzp_osnov = a.nzp_osnov ");
                    sql.Append(" AND a.nzp_payer = b.nzp_payer ");
                    sql.Append(" and f.nzp_payer = a.nzp_payer ");
                    sql.Append(" AND a.nzp_kvar = " + finder.nzp_kvar);

                }
                if (finder.area_flag == 2)
                {
                    sql.Append(" Where a.nzp_fb = d.nzp_fb ");
                    sql.Append(" AND e.nzp_osnov = a.nzp_osnov ");
                    sql.Append(" AND a.nzp_payer = b.nzp_payer ");
                    sql.Append(" AND exists (select 1 from "+Points.Pref+"_kernel"+tableDelimiter+"supplier c where c.nzp_payer_supp = a.nzp_payer) ");
                    sql.Append(" AND a.nzp_kvar = " + finder.nzp_kvar);
                }

                if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
                {
                    ret.text = "Ошибка при получении данных о контрактах";
                    MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return null;
                }

                if (reader != null)
                {
                    while (reader.Read())
                    {
                        ContractRequisites dr = new ContractRequisites();

                        if (reader["nzp_con"] != DBNull.Value) dr.nzp_con = Convert.ToInt32(reader["nzp_con"]);
                        if (reader["nzp_fb"] != DBNull.Value) dr.nzp_fb = Convert.ToInt32(reader["nzp_fb"]);
                        if (reader["num_dog"] != DBNull.Value) dr.num_dog = Convert.ToString(reader["num_dog"]).Trim();
                        if (reader["payer"] != DBNull.Value) dr.payer = Convert.ToString(reader["payer"]).Trim();
                        if (reader["nzp_supp"] != DBNull.Value) dr.nzp_supp = Convert.ToInt32(reader["nzp_supp"]);

                        #region dat_s

                        if (reader["dat_s"] != DBNull.Value) dr.dat_s = Convert.ToString(reader["dat_s"]).Trim();
                        DateTime ds = new DateTime();
                        bool d = DateTime.TryParse(dr.dat_s, out ds);
                        if (d)
                            dr.dat_s = ds.ToString("dd.MM.yyyy");
                        else
                        {
                            MonitorLog.WriteLog("Ошибка формата datetime в GetContractList " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                            ret.result = false;
                            return null;
                        }

                        #endregion

                        #region dat_po

                        if (reader["dat_po"] != DBNull.Value) dr.dat_po = Convert.ToString(reader["dat_po"]).Trim();
                        DateTime dpo = new DateTime();
                        bool p = DateTime.TryParse(dr.dat_po, out dpo);
                        if (p)
                            dr.dat_po = dpo.ToString("dd.MM.yyyy");
                        else
                        {
                            MonitorLog.WriteLog("Ошибка формата datetime в GetContractList " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                            ret.result = false;
                            return null;
                        }

                        #endregion

                        if (reader["kod_supp"] != DBNull.Value) dr.kod_supp = Convert.ToString(reader["kod_supp"]).Trim();
                        if (reader["rcount"] != DBNull.Value) dr.rcount = Convert.ToString(reader["rcount"]).Trim();
                        if (reader["nzp_osnov"] != DBNull.Value) dr.nzp_osnov = Convert.ToInt32(reader["nzp_osnov"]);
                        if (reader["osnov"] != DBNull.Value) dr.osnov = Convert.ToString(reader["osnov"]);
                        if (reader["bank_name"] != DBNull.Value) dr.bank_name = Convert.ToString(reader["bank_name"]).Trim();
                        if (reader["bik"] != DBNull.Value) dr.bik = Convert.ToString(reader["bik"]).Trim();
                        if (reader["kcount"] != DBNull.Value) dr.kcount = Convert.ToString(reader["kcount"]).Trim();
                        if (reader["npunkt"] != DBNull.Value) dr.npunkt = Convert.ToString(reader["npunkt"]).Trim();
                        if (reader["comment"] != DBNull.Value) dr.comment = Convert.ToString(reader["comment"]).Trim();

                        retList.Add(dr);
                    }
                }

                return retList;

            }
            catch (Exception ex)
            {
                ret.text = "Ошибка при получении данных о контрактах";
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetContractList : " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                #region Закрытие соединений

                if (conn_db != null)
                {
                    conn_db.Close();
                }

                if (reader != null)
                {
                    reader.Close();
                }

                sql.Remove(0, sql.Length);

                #endregion
            }
        }

        /// <summary>
        /// Получить список всех оснований
        /// </summary>       
        public List<DogovorRequisites> GetOsnovList(DogovorRequisites finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            IDbConnection conn_db = null;
            IDataReader reader = null;
            StringBuilder sql = new StringBuilder();

            List<DogovorRequisites> retList = new List<DogovorRequisites>();

            try
            {
                #region Открываем соединение с базами

                conn_db = GetConnection(Constants.cons_Kernel);

                ret = OpenDb(conn_db, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return null;
                }

                #endregion

                sql.Append(" select  nzp_osnov, osnov from " + Points.Pref + "_data" + DBManager.tableDelimiter + "fn_osnov ");

                if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return null;
                }

                if (reader != null)
                {
                    while (reader.Read())
                    {
                        DogovorRequisites dr = new DogovorRequisites();
                        if (reader["nzp_osnov"] != DBNull.Value) dr.nzp_osnov = Convert.ToInt32(reader["nzp_osnov"]);
                        if (reader["osnov"] != DBNull.Value) dr.osnov = Convert.ToString(reader["osnov"]);
                        retList.Add(dr);
                    }
                }

                return retList;

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры GetOsnovList : " + ex.Message, MonitorLog.typelog.Error, true);
                return null;
            }
            finally
            {
                #region Закрытие соединений

                if (conn_db != null)
                {
                    conn_db.Close();
                }

                if (reader != null)
                {
                    reader.Close();
                }

                sql.Remove(0, sql.Length);

                #endregion
            }
        }


        /// <summary>
        /// Добавить банковские реквизиты поставщику
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public bool AddBankRequisites(BankRequisites finder, out Returns ret)
        {
            #region Проверка входных параметров
            ret = Utils.InitReturns();
            if (finder.nzp_payer <= 0)
            {
                ret.text = "Не задан подрядчик";
                return false;
            }
            #endregion

            IDbConnection conn_db = null;
            IDataReader reader = null;
            StringBuilder sql = new StringBuilder();

            List<BankRequisites> retList = new List<BankRequisites>();

            try
            {
                #region Открываем соединение с базами
                conn_db = GetConnection(Constants.cons_Kernel);

                ret = OpenDb(conn_db, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog(" Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    ret.tag = 0;
                    return false;
                }
                #endregion


                #region Получение локального юзера
                int local_user = finder.nzp_user;
                
                /*DbWorkUser dbU = new DbWorkUser();
                int local_user = dbU.GetLocalUser(conn_db, new Finder() { nzp_user = finder.nzp_user, pref = Points.Pref }, out ret);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка определения локального пользователя :" + ret.text, MonitorLog.typelog.Error, true);
                    ret.tag = 0;
                    return false;
                }
                dbU.Close();*/
                #endregion

#if PG
                sql.Append("Insert into " + Points.Pref + "_data.fn_bank (nzp_payer, nzp_payer_bank, num_count, bank_name, rcount, kcount, bik, npunkt, nzp_user, is_default");
                if (finder.dat_when != null && finder.dat_when != "") sql.Append(", dat_when");

                sql.Append(") values (" + finder.nzp_payer +
                    "," + finder.nzp_payer_bank +
                    "," + finder.num_count +
                    "," + Utils.EStrNull(finder.bank_name, "") +
                    "," + Utils.EStrNull(finder.rcount, ""));
                sql.Append("," + Utils.EStrNull(finder.kcount, "") +
                    "," + Utils.EStrNull(finder.bik, "") +
                    "," + Utils.EStrNull(finder.npunkt, "") +
                    ",'" + local_user + "', " + finder.is_default);

                if (finder.dat_when != null && finder.dat_when != "") sql.Append("," + Utils.EStrNull(finder.dat_when, ""));
                sql.Append(")");
#else
                sql.Append(" Insert into " + Points.Pref + "_data:fn_bank ( nzp_fb, nzp_payer,nzp_payer_bank, num_count, bank_name, rcount, kcount, bik, npunkt, nzp_user, dat_when)  ");
                sql.Append(" values (0," + finder.nzp_payer + "," + finder.nzp_payer_bank + "," + finder.num_count + ",'" + finder.bank_name + "', '" + finder.rcount + "','");
                sql.Append(" " + finder.kcount + "','" + finder.bik + "','" + finder.npunkt + "','" + local_user + "','" + finder.dat_when + "'); ");
#endif

                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка вставки данных " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    ret.tag = 0;
                    return false;
                }

                //получение nzp_fb
                ret.tag = GetSerialValue(conn_db);

                return true;

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры AddBankRequisites : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.tag = 0;
                return false;
            }
            finally
            {
                #region Закрытие соединений

                if (conn_db != null)
                {
                    conn_db.Close();
                }

                if (reader != null)
                {
                    reader.Close();
                }

                sql.Remove(0, sql.Length);

                #endregion
            }
        }

        public bool AddBankRequisitesContr(BankRequisites finder, out Returns ret)
        {
            #region Проверка входных параметров
            ret = Utils.InitReturns();
            if (finder.nzp_payer <= 0)
            {
                ret.text = "Не задан контрагент";
                return false;
            }
            #endregion

            IDbConnection conn_db = null;
            IDataReader reader = null;
            StringBuilder sql = new StringBuilder();

            List<BankRequisites> retList = new List<BankRequisites>();

            try
            {
                #region Открываем соединение с базами
                conn_db = GetConnection(Constants.cons_Kernel);

                ret = OpenDb(conn_db, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog(" Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    ret.tag = 0;
                    return false;
                }
                #endregion


                #region Получение локального юзера
                int local_user = finder.nzp_user;
                
                /*DbWorkUser dbU = new DbWorkUser();
                int local_user = dbU.GetLocalUser(conn_db, new Finder() { nzp_user = finder.nzp_user, pref = Points.Pref }, out ret);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка определения локального пользователя :" + ret.text, MonitorLog.typelog.Error, true);
                    ret.tag = 0;
                    return false;
                }
                dbU.Close();*/
                #endregion

                //sql = new StringBuilder();
                //sql.Append("update " + Points.Pref + "_kernel" + tableDelimiter + "s_payer set ");
                //sql.AppendFormat(" ks = '{0}', ", finder.kcount.Trim());
                //sql.AppendFormat(" bik = '{0}', ", finder.bik.Trim());
                //sql.AppendFormat(" city = '{0}' ", finder.npunkt.Trim());
                //sql.AppendFormat(" where nzp_payer = {0}", finder.nzp_payer);
                //if (!ExecSQL(conn_db, sql.ToString(), true).result)
                //{
                //    MonitorLog.WriteLog("Ошибка обновления данных " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                //    ret.result = false;
                //    ret.tag = 0;
                //    return false;
                //}

                sql = new StringBuilder();
                sql.Append("Insert into " + Points.Pref + "_data"+tableDelimiter+"fn_bank (nzp_payer, nzp_payer_bank, rcount, nzp_user, dat_when)");
                sql.Append("values (" + finder.nzp_payer +
                    "," + finder.nzp_payer_bank +
                    "," + Utils.EStrNull(finder.rcount, "")+
                    ",'" + local_user + "', now())");

                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка вставки данных " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    ret.tag = 0;
                    return false;
                }

                //получение nzp_fb
                ret.tag = GetSerialValue(conn_db);

                return true;

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры AddBankRequisites : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.tag = 0;
                return false;
            }
            finally
            {
                #region Закрытие соединений

                if (conn_db != null)
                {
                    conn_db.Close();
                }

                if (reader != null)
                {
                    reader.Close();
                }

                sql.Remove(0, sql.Length);

                #endregion
            }
        }

        public bool NewFdAddBankRequisites(BankRequisites finder, out Returns ret)
        {
            #region Проверка входных параметров
            ret = Utils.InitReturns();
            if (finder.nzp_fd <= 0)
            {
                ret.text = "Не задан договор";
                ret.tag = -1;
                ret.result = false;
                return false;
            }
            #endregion

            IDbConnection conn_db = null;
            IDataReader reader = null;
            StringBuilder sql = new StringBuilder();

            List<BankRequisites> retList = new List<BankRequisites>();

            try
            {
                #region Открываем соединение с базами
                conn_db = GetConnection(Constants.cons_Kernel);
                ret = OpenDb(conn_db, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog(" Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    ret.tag = 0;
                    return false;
                }
                #endregion
                int local_user = finder.nzp_user;
                Dictionary<string, string> colsValues= new Dictionary<string, string> 
                {{"nzp_fb", finder.nzp_fb.ToString()}, 
                {"nzp_fd", finder.nzp_fd.ToString()}, 
                {"changed_by", local_user.ToString()},
                {"changed_on", "now()"}, 
                {"priznak_perechisl", finder.priznak_perechisl.ToString()}};
                if (finder.max_sum > 0)
                {
                    colsValues.Add("max_sum", finder.max_sum.ToString()); 
                }
                if (finder.min_sum > 0)
                {
                    colsValues.Add("min_sum", finder.min_sum.ToString());
                }
                if (!String.IsNullOrWhiteSpace(finder.naznplat))
                {
                    colsValues.Add("naznplat", "'"+finder.naznplat+"'");
                }

                #region Получение локального юзера
                
                
                /*DbWorkUser dbU = new DbWorkUser();
                int local_user = dbU.GetLocalUser(conn_db, new Finder() { nzp_user = finder.nzp_user, pref = Points.Pref }, out ret);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка определения локального пользователя :" + ret.text, MonitorLog.typelog.Error, true);
                    ret.tag = 0;
                    return false;
                }
                dbU.Close();*/
                #endregion

                string columns = String.Join(",", colsValues.Keys.ToArray());
                string values = String.Join(",", colsValues.Values.ToArray());
                sql.Append("Insert into " + Points.Pref + "_data" + tableDelimiter +
                           "fn_dogovor_bank_lnk (" + columns + ") values (" + values + ")");
                //sql.Append("Insert into " + Points.Pref + "_data" + tableDelimiter +
                //           "fn_dogovor_bank_lnk ( nzp_fb, nzp_fd, changed_by, changed_on) values (" +
                //           "" + finder.nzp_fb +
                //           ", " + finder.nzp_fd +
                //           ", " + local_user);
                //sql.Append(", now()");
                //sql.Append(")");


                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка вставки данных " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    ret.tag = 0;
                    return false;
                }

                //получение id
                ret.tag = GetSerialValue(conn_db);

                return true;

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры AddBankRequisites : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.tag = 0;
                return false;
            }
            finally
            {
                #region Закрытие соединений

                if (conn_db != null)
                {
                    conn_db.Close();
                }

                if (reader != null)
                {
                    reader.Close();
                }

                sql.Remove(0, sql.Length);

                #endregion
            }
        }

        public bool DelBankRequisites(BankRequisites finder, out Returns ret)
        {
            #region Проверка входных параметров
            ret = Utils.InitReturns();
            if (finder.nzp_fb <= 0)
            {
                ret.text = "Не задана строка удаления";
                return false;
            }
            #endregion

            IDbConnection conn_db = null;
            IDataReader reader = null;
            StringBuilder sql = new StringBuilder();

            List<BankRequisites> retList = new List<BankRequisites>();

            try
            {
                #region Открываем соединение с базами
                conn_db = GetConnection(Constants.cons_Kernel);

                ret = OpenDb(conn_db, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return false;
                }
                #endregion

#if PG
                sql.Append("delete from " + Points.Pref + "_data.fn_bank where nzp_fb = " + finder.nzp_fb + " ");
#else
                sql.Append("delete from " + Points.Pref + "_data:fn_bank where nzp_fb = " + finder.nzp_fb + " ");
#endif

                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка удаления данных " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return false;
                }


                return true;

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры DelBankRequisites : " + ex.Message, MonitorLog.typelog.Error, true);
                return false;
            }
            finally
            {
                #region Закрытие соединений

                if (conn_db != null)
                {
                    conn_db.Close();
                }

                if (reader != null)
                {
                    reader.Close();
                }

                sql.Remove(0, sql.Length);

                #endregion
            }
        }

        public bool DelBankRequisitesNewFd(BankRequisites finder, out Returns ret)
        {
            #region Проверка входных параметров
            ret = Utils.InitReturns();
            if (finder.id <= 0)
            {
                ret.text = "Не задана строка удаления";
                return false;
            }
            #endregion

            IDbConnection conn_db = null;
            IDataReader reader = null;
            StringBuilder sql = new StringBuilder();

            List<BankRequisites> retList = new List<BankRequisites>();

            try
            {
                #region Открываем соединение с базами
                conn_db = GetConnection(Constants.cons_Kernel);

                ret = OpenDb(conn_db, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return false;
                }
                #endregion

                sql = new StringBuilder();
                sql.AppendFormat("select count(*) from {0}_kernel{1}supplier where fn_dogovor_bank_lnk_id = {2}", Points.Pref, tableDelimiter, finder.id);
                object obj = ExecScalar(conn_db, sql.ToString(), out ret, true);
                int totalNumber = 0;
                if (!ret.result)
                {
                    conn_db.Close();
                    return ret.result;
                }
                try { totalNumber = Convert.ToInt32(obj); }
                catch (Exception ex)
                {
                    conn_db.Close();
                    ret.result = false;
                    ret.text = ex.Message;
                    return ret.result;
                }
                if (totalNumber > 0)
                {
                    ret =  new Returns(false, "Удалить р/с нельзя, так на него ссылаются Договоры ЖКУ", -1);
                    return false;
                }

                sql = new StringBuilder();
                sql.Append("delete from " + Points.Pref + "_data"+tableDelimiter+"fn_dogovor_bank_lnk where id = " + finder.id + " ");


                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка удаления данных " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return false;
                }


                return true;

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры DelBankRequisites : " + ex.Message, MonitorLog.typelog.Error, true);
                return false;
            }
            finally
            {
                #region Закрытие соединений

                if (conn_db != null)
                {
                    conn_db.Close();
                }

                if (reader != null)
                {
                    reader.Close();
                }

                sql.Remove(0, sql.Length);

                #endregion
            }
        }

        public bool UpdateBankRequisites(BankRequisites finder, out Returns ret)
        {
            #region Проверка входных параметров
            ret = Utils.InitReturns();
            if (finder.nzp_fb <= 0)
            {
                ret.text = "Не задана строка удаления";
                return false;
            }
            #endregion

            IDbConnection conn_db = null;
            IDataReader reader = null;
            StringBuilder sql = new StringBuilder();

            List<BankRequisites> retList = new List<BankRequisites>();

            try
            {
                #region Открываем соединение с базами

                conn_db = GetConnection(Constants.cons_Kernel);

                ret = OpenDb(conn_db, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return false;
                }

                #endregion

                #region Получение локального юзера
                
                int local_user = finder.nzp_user;

                /*DbWorkUser dbU = new DbWorkUser();
                int local_user = dbU.GetLocalUser(conn_db, new Finder() {nzp_user = finder.nzp_user, pref = Points.Pref},
                    out ret);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка определения локального пользователя :" + ret.text,
                        MonitorLog.typelog.Error, true);
                    return false;
                }
                dbU.Close();*/

                #endregion

                sql.Append(" update " + Points.Pref + "_data" + tableDelimiter + "fn_bank set  num_count = " +
                           finder.num_count +
                           ",  bank_name = " + Utils.EStrNull(finder.bank_name, ""));
                sql.Append(", rcount = " + Utils.EStrNull(finder.rcount, "") +
                           ", kcount = " + Utils.EStrNull(finder.kcount, "") +
                           ", bik = " + Utils.EStrNull(finder.bik, "") +
                           ", npunkt = " + Utils.EStrNull(finder.npunkt, ""));
                sql.Append(", nzp_user = '" + local_user + "', nzp_payer_bank =  " + finder.nzp_payer_bank + ", is_default =  " + finder.is_default +
#if PG
                    ",  dat_when = current_date ");
#else
 ",  dat_when = today ");
#endif
                sql.Append(" where  nzp_fb = " + finder.nzp_fb + " ");

                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка обновления данных " + sql.ToString(), MonitorLog.typelog.Error, 20, 201,
                        true);
                    ret.result = false;
                    return false;
                }


                return true;
           
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры UpdateBankRequisites : " + ex.Message, MonitorLog.typelog.Error, true);
                return false;
            }
            finally
            {
                #region Закрытие соединений

                if (conn_db != null)
                {
                    conn_db.Close();
                }

                if (reader != null)
                {
                    reader.Close();
                }

                sql.Remove(0, sql.Length);

                #endregion
            }
        }

        public bool NewFdUpdateBankRequisites(BankRequisites finder, out Returns ret)
        {
            #region Проверка входных параметров
            ret = Utils.InitReturns();
            if (finder.nzp_fb <= 0)
            {
                ret.text = "Не выбран р/с";
                return false;
            }
            #endregion

            IDbConnection conn_db = null;
            IDataReader reader = null;
            StringBuilder sql = new StringBuilder();

            List<BankRequisites> retList = new List<BankRequisites>();

            try
            {
                #region Открываем соединение с базами

                conn_db = GetConnection(Constants.cons_Kernel);

                ret = OpenDb(conn_db, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return false;
                }

                #endregion

                #region Получение локального юзера

                int local_user = finder.nzp_user;

                /*DbWorkUser dbU = new DbWorkUser();
                int local_user = dbU.GetLocalUser(conn_db, new Finder() { nzp_user = finder.nzp_user, pref = Points.Pref },
                    out ret);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка определения локального пользователя :" + ret.text,
                        MonitorLog.typelog.Error, true);
                    return false;
                }
                dbU.Close();*/

                #endregion

                sql.Append(" update " + Points.Pref + "_data" + tableDelimiter + "fn_dogovor_bank_lnk ");
                      
                sql.Append(" set  changed_by = " + local_user + ", changed_on = " + sCurDate);
                sql.Append(", max_sum="+finder.max_sum+", min_sum="+finder.min_sum);
                sql.Append(", priznak_perechisl=" + finder.priznak_perechisl + ", naznplat='" + finder.naznplat+"'");
                sql.Append(" where  id = " + finder.id + " ");

                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка обновления данных " + sql.ToString(), MonitorLog.typelog.Error, 20, 201,
                        true);
                    ret.result = false;
                    return false;
                }


                return true;

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры NewFdUpdateBankRequisites : " + ex.Message, MonitorLog.typelog.Error, true);
                return false;
            }
            finally
            {
                #region Закрытие соединений

                if (conn_db != null)
                {
                    conn_db.Close();
                }

                if (reader != null)
                {
                    reader.Close();
                }

                sql.Remove(0, sql.Length);

                #endregion
            }
        }

        public bool UpdateBankRequisitesContr(BankRequisites finder, out Returns ret)
        {
            #region Проверка входных параметров
            ret = Utils.InitReturns();
            if (finder.nzp_fb <= 0)
            {
                ret.text = "Не задана строка для редактирования";
                return false;
            }
            #endregion

            IDbConnection conn_db = null;
            IDataReader reader = null;
            StringBuilder sql = new StringBuilder();

            List<BankRequisites> retList = new List<BankRequisites>();

            try
            {
                #region Открываем соединение с базами

                conn_db = GetConnection(Constants.cons_Kernel);

                ret = OpenDb(conn_db, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return false;
                }

                #endregion

                #region Получение локального юзера
                int local_user = finder.nzp_user;

                /*DbWorkUser dbU = new DbWorkUser();
                int local_user = dbU.GetLocalUser(conn_db, new Finder() { nzp_user = finder.nzp_user, pref = Points.Pref },
                    out ret);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка определения локального пользователя :" + ret.text,
                        MonitorLog.typelog.Error, true);
                    return false;
                }
                dbU.Close();*/

                #endregion

                //sql = new StringBuilder();
                //sql.Append("update " + Points.Pref + "_kernel" + tableDelimiter + "s_payer set ");
                //sql.AppendFormat(" ks = '{0}', ", finder.kcount.Trim());
                //sql.AppendFormat(" bik = '{0}', ", finder.bik.Trim());
                //sql.AppendFormat(" city = '{0}' ", finder.npunkt.Trim());
                //sql.AppendFormat(" where nzp_payer = {0}", finder.nzp_payer);
                //if (!ExecSQL(conn_db, sql.ToString(), true).result)
                //{
                //    MonitorLog.WriteLog("Ошибка обновления данных " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                //    ret.result = false;
                //    ret.tag = 0;
                //    return false;
                //}

                sql = new StringBuilder();
                sql.Append(" update " + Points.Pref + "_data" + tableDelimiter + "fn_bank set  ");
                sql.Append(" rcount = " + Utils.EStrNull(finder.rcount, ""));
                sql.Append(", nzp_user = '" + local_user + "', nzp_payer_bank =  " + finder.nzp_payer_bank +
                           ",  dat_when = " +sCurDate);
                sql.Append(" where  nzp_fb = " + finder.nzp_fb + " ");

                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка обновления данных " + sql.ToString(), MonitorLog.typelog.Error, 20, 201,
                        true);
                    ret.result = false;
                    return false;
                }


                return true;

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры UpdateBankRequisitesContr : " + ex.Message, MonitorLog.typelog.Error, true);
                return false;
            }
            finally
            {
                #region Закрытие соединений

                if (conn_db != null)
                {
                    conn_db.Close();
                }

                if (reader != null)
                {
                    reader.Close();
                }

                sql.Remove(0, sql.Length);

                #endregion
            }
        }

        /// <summary>
        /// Добавить реквизиты договора поставщику
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public bool AddDogovorRequisites(DogovorRequisites finder, out Returns ret)
        {

            #region Проверка входных параметров

            ret = Utils.InitReturns();
            if (finder.nzp_payer <= 0)
            {
                ret.text = "Не задан подрядчик";
                return false;
            }

            #endregion

            IDbConnection conn_db = null;
            StringBuilder sql = new StringBuilder();

            try
            {
                #region Открываем соединение с базами

                conn_db = GetConnection(Constants.cons_Kernel);

                ret = OpenDb(conn_db, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog(" Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return false;
                }

                #endregion

                #region Получение локального юзера
                int local_user = finder.nzp_user;

                /*DbWorkUser dbU = new DbWorkUser();
                int local_user = dbU.GetLocalUser(conn_db, new Finder() { nzp_user = finder.nzp_user, pref = Points.Pref }, out ret);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка определения локального пользователя :" + ret.text, MonitorLog.typelog.Error, true);
                    return false;
                }
                dbU.Close();*/

                #endregion

                #region определение kpp
                finder.kpp = GetKppFromArea(conn_db, finder, out ret);
                /*  if (!ret.result)
                  {
                      MonitorLog.WriteLog("Ошибка определения кпп " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                      ret.result = false;
                      return false;
                  }*/
                #endregion
#if PG
                sql.Append(" Insert into " + Points.Pref + "_data.fn_dogovor ( nzp_fd, nzp_area, nzp_payer_ar, nzp_payer, nzp_fb, nzp_osnov, num_dog, dat_dog, dat_s, dat_po, max_sum, target, kpp, nzp_user, dat_when, priznak_perechisl, min_sum, naznplat)  ");
#else
                sql.Append(" Insert into " + Points.Pref + "_data:fn_dogovor ( nzp_fd, nzp_area, nzp_payer_ar, nzp_payer, nzp_fb, nzp_osnov, num_dog, dat_dog, dat_s, dat_po, max_sum, target, kpp, nzp_user, dat_when, priznak_perechisl, min_sum, naznplat)  ");
#endif
                sql.Append(" values (" +
#if PG
 "default,"
#else
 "0,"
#endif
 + finder.nzp_area + "," + finder.nzp_payer_ar + "," + finder.nzp_payer + "," + finder.nzp_fb + ",");
                sql.Append(finder.nzp_osnov + ",'" + finder.num_dog + "','" + finder.dat_dog + "'," + (finder.dat_s == "" ? "null," : "'" + finder.dat_s + "',") +
                    (finder.dat_po == "" ? "null," : "'" + finder.dat_po + "',") + finder.max_sum + "," + "'" + finder.target + "','" +
                    finder.kpp + "'," + local_user + ", '" + finder.dat_when + "'," + finder.priznak_perechisl + "," + finder.min_sum + ",'" + finder.naznplat + "'" + "); ");

                ret = ExecSQL(conn_db, sql.ToString(), true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка вставки данных " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return false;
                }
                else
                    ret.tag = GetSerialValue(conn_db);

                #region сохранение источников(банков)
                string sqlStr = "";
                foreach (var b in finder.bank_list)
                {
                    sqlStr += " insert into " + Points.Pref + "_data" + tableDelimiter + "fn_dogovor_bank (nzp_fd, nzp_bank, dat_when, nzp_user) VALUES " +
                        " (" + ret.tag + ", " + b + ", " + sCurDate + ", " + finder.nzp_user + "); ";
                }
                ExecSQL(conn_db, sqlStr);
                #endregion

                return true;

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры AddDogovorRequisites : " + ex.Message, MonitorLog.typelog.Error, true);
                return false;
            }
            finally
            {
                #region Закрытие соединений

                if (conn_db != null)
                {
                    conn_db.Close();
                }

                sql.Remove(0, sql.Length);

                #endregion
            }
        }

        public bool AddDogovorRequisitesSupp(DogovorRequisites finder, out Returns ret)
        {

            #region Проверка входных параметров

            ret = Utils.InitReturns();
            if (finder.nzp_payer <= 0)
            {
                ret.text = "Не задан подрядчик";
                return false;
            }

            #endregion

            IDbConnection conn_db = null;
            StringBuilder sql = new StringBuilder();

            try
            {
                #region Открываем соединение с базами

                conn_db = GetConnection(Constants.cons_Kernel);

                ret = OpenDb(conn_db, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog(" Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return false;
                }

                #endregion

                #region Получение локального юзера

                int local_user = finder.nzp_user;

                /*DbWorkUser dbU = new DbWorkUser();
                int local_user = dbU.GetLocalUser(conn_db, new Finder() { nzp_user = finder.nzp_user, pref = Points.Pref }, out ret);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка определения локального пользователя :" + ret.text, MonitorLog.typelog.Error, true);
                    return false;
                }
                dbU.Close();*/

                #endregion

                #region определение kpp
                finder.kpp = GetKppFromArea(conn_db, finder, out ret);
                #endregion

#if PG
                string _default = "default";
#else
                string _default = "0";
#endif


                sql.Append(" Insert into " + Points.Pref + "_data" + DBManager.tableDelimiter + "fn_dogovor ( nzp_fd, nzp_area, nzp_supp, nzp_payer_ar, nzp_payer, nzp_fb, nzp_osnov, num_dog, dat_dog, dat_s, dat_po, max_sum, target, kpp, nzp_user, dat_when, priznak_perechisl, min_sum, naznplat)  ");
                sql.Append(" values (" + _default + ", 0," +
                finder.nzp_supp + "," + finder.nzp_payer_ar + "," + finder.nzp_payer + "," + finder.nzp_fb + ",");
                sql.Append(finder.nzp_osnov + ",'" + finder.num_dog + "','" + finder.dat_dog + "'," + (finder.dat_s == "" ? "null," : "'" + finder.dat_s + "',") +
                    (finder.dat_po == "" ? "null," : "'" + finder.dat_po + "',") + finder.max_sum + "," + "'" + finder.target + "','" +
                    finder.kpp + "'," + local_user + ", '" + finder.dat_when + "'," + finder.priznak_perechisl + "," + finder.min_sum + ",'" + finder.naznplat + "'" + "); ");

                ret = ExecSQL(conn_db, sql.ToString(), true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка вставки данных " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return false;
                }
                else
                    ret.tag = GetSerialValue(conn_db);

                #region сохранение источников(банков)
                string sqlStr = "";
                foreach (var b in finder.bank_list)
                {
                    sqlStr += " insert into " + Points.Pref + "_data" + tableDelimiter + "fn_dogovor_bank (nzp_fd, nzp_bank, dat_when, nzp_user) VALUES " +
                        " (" + ret.tag + ", " + b + ", " + sCurDate + ", " + finder.nzp_user + "); ";
                }
                ExecSQL(conn_db, sqlStr);
                #endregion

                return true;

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры AddDogovorRequisites : " + ex.Message, MonitorLog.typelog.Error, true);
                return false;
            }
            finally
            {
                #region Закрытие соединений

                if (conn_db != null)
                {
                    conn_db.Close();
                }

                sql.Remove(0, sql.Length);

                #endregion
            }
        }

        public bool AddERCDogovorRequisites(DogovorRequisites finder, out Returns ret)
        {

            #region Проверка входных параметров

            ret = Utils.InitReturns();
            if (finder.nzp_payer_agent <= 0)
            {
                ret.text = "Не задан агент";
                return false;
            }
            if (finder.nzp_payer_princip <= 0)
            {
                ret.text = "Не задан принципал";
                return false;
            }
            #endregion

            IDbConnection conn_db = null;
            StringBuilder sql = new StringBuilder();

            try
            {
                #region Открываем соединение с базами

                conn_db = GetConnection(Constants.cons_Kernel);

                ret = OpenDb(conn_db, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog(" Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return false;
                }

                #endregion

                #region Получение локального юзера
                int local_user = finder.nzp_user;

                /*DbWorkUser dbU = new DbWorkUser();
                int local_user = dbU.GetLocalUser(conn_db, new Finder() { nzp_user = finder.nzp_user, pref = Points.Pref }, out ret);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка определения локального пользователя :" + ret.text, MonitorLog.typelog.Error, true);
                    return false;
                }
                dbU.Close();*/

                #endregion

                //#region определение kpp
                //finder.kpp = GetKppFromArea(conn_db, finder, out ret);
                //#endregion

#if PG
                string _default = "default";
#else
                string _default = "0";
#endif
                using (var db = new DbScopeAddress())
                {
                    ret = db.GenerateNzpScope(finder);
                }
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка вставки данных " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return false;
                }
                int nzp_scope = ret.tag;

                sql.Append(" Insert into " + Points.Pref + "_data" + DBManager.tableDelimiter + "fn_dogovor ( nzp_fd, nzp_payer_agent, nzp_payer_princip,  num_dog," +
                           " dat_dog, dat_s, dat_po, target, nzp_user, dat_when, nzp_scope)  ");
                sql.Append(" values (" + _default + "," +
                finder.nzp_payer_agent + "," + finder.nzp_payer_princip + ",");
                sql.Append("'" + finder.num_dog + "','" + finder.dat_dog + "'," + (finder.dat_s == "" ? "null," : "'" + finder.dat_s + "',") +
                    (finder.dat_po == "" ? "null," : "'" + finder.dat_po + "',") + "'" + finder.target + "'," +
                    local_user + ", now()" +  "" +
                    ","+nzp_scope+"); ");

                ret = ExecSQL(conn_db, sql.ToString(), true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка вставки данных " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return false;
                }
                else
                    ret.tag = GetSerialValue(conn_db);


                //#region сохранение источников(банков)
                //string sqlStr = "";
                //foreach (var b in finder.bank_list)
                //{
                //    sqlStr += " insert into " + Points.Pref + "_data" + tableDelimiter + "fn_dogovor_bank (nzp_fd, nzp_bank, dat_when, nzp_user) VALUES " +
                //        " (" + ret.tag + ", " + b + ", " + sCurDate + ", " + finder.nzp_user + "); ";
                //}
                //ExecSQL(conn_db, sqlStr);
                //#endregion

                return true;

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры AddERCDogovorRequisites : " + ex.Message, MonitorLog.typelog.Error, true);
                return false;
            }
            finally
            {
                #region Закрытие соединений

                if (conn_db != null)
                {
                    conn_db.Close();
                }

                sql.Remove(0, sql.Length);

                #endregion
            }
        }

        private string GetKppFromArea(IDbConnection conn_db, DogovorRequisites finder, out Returns ret)
        {
            DbTables tables = new DbTables(DBManager.getServer(conn_db));
            MyDataReader reader;
            ret = Utils.InitReturns();
            string sqlstr = "select kpp from " + tables.payer + " p where p.nzp_supp = (select " +
                " a.nzp_supp from " + tables.area + " a where nzp_area = " + finder.nzp_area + ")";
            if (!ExecRead(conn_db, out reader, sqlstr, true).result)
            {
                MonitorLog.WriteLog("Ошибка выборки AddDogovorRequisites" + sqlstr.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                ret = new Returns(false);
                return "";
            }
            if (reader != null)
            {
                if (reader.Read())
                {
                    if (reader["kpp"] != DBNull.Value) finder.kpp = Convert.ToString(reader["kpp"]);
                }
                reader.Close();
                return finder.kpp;
            }
            return "";
        }

        public bool DelDogovorRequisites(DogovorRequisites finder, out Returns ret)
        {

            #region Проверка входных параметров

            ret = Utils.InitReturns();
            if (finder.nzp_fd <= 0)
            {
                ret.text = "Не задана строка удаления";
                return false;
            }

            #endregion

            IDbConnection conn_db = null;
            IDataReader reader = null;
            StringBuilder sql = new StringBuilder();

            List<DogovorRequisites> retList = new List<DogovorRequisites>();

            try
            {

                #region Открываем соединение с базами

                conn_db = GetConnection(Constants.cons_Kernel);

                ret = OpenDb(conn_db, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return false;
                }

                #endregion

                //проверка наличия ссылок на договор
                bool nolink = true;
                int year = 0;
                sql.Remove(0, sql.Length);
                sql.Append("select yearr from " + Points.Pref + "_kernel" + tableDelimiter +
                           "s_baselist where idtype = 4 order by yearr");
                ret = ExecRead(conn_db, out reader, sql.ToString(), true);
                if (!ret.result) return ret.result;
                while (reader.Read())
                {
                    year = 0;
                    if (reader["yearr"] != DBNull.Value) year = Convert.ToInt32(reader["yearr"]);

                    string fn_sended = Points.Pref + "_fin_" + year.ToString("0000").Substring(2, 2) + tableDelimiter + "fn_sended";
                    string fn_sended_dom = Points.Pref + "_fin_" + year.ToString("0000").Substring(2, 2) + tableDelimiter + "fn_sended_dom";
                    if (!TempTableInWebCashe(conn_db, fn_sended)) continue;
                    sql.Remove(0, sql.Length);
                    sql.Append("select count (*) from " + fn_sended + " where " + sNvlWord + "(nzp_fd,0) =" + finder.nzp_fd);
                    object obj = ExecScalar(conn_db, sql.ToString(), out ret, true);
                    int totalNumber = 0;
                    if (!ret.result)
                    {
                        conn_db.Close();
                        return ret.result;
                    }
                    try { totalNumber = Convert.ToInt32(obj); }
                    catch (Exception ex)
                    {
                        conn_db.Close();
                        ret.result = false;
                        ret.text = ex.Message;
                        return ret.result;
                    }
                    if (totalNumber > 0)
                    {
                        nolink = false;
                        break;
                    }
                    if (!TempTableInWebCashe(conn_db, fn_sended_dom)) continue;
                    sql.Remove(0, sql.Length);
                    sql.Append("select count (*) from " + fn_sended_dom + " where " + sNvlWord + "(nzp_fd,0) =" + finder.nzp_fd);
                    obj = ExecScalar(conn_db, sql.ToString(), out ret, true);
                    totalNumber = 0;
                    if (!ret.result)
                    {
                        conn_db.Close();
                        return ret.result;
                    }
                    try { totalNumber = Convert.ToInt32(obj); }
                    catch (Exception ex)
                    {
                        conn_db.Close();
                        ret.result = false;
                        ret.text = ex.Message;
                        return ret.result;
                    }
                    if (totalNumber > 0)
                    {
                        nolink = false;
                        break;
                    }
                }
                CloseReader(ref reader);

                if (!nolink)
                {
                    ret.text = "Удаление договора не возможно, так как есть ссылки на этот договор";
                    return false;
                }

                sql.Remove(0, sql.Length);
#if PG
                sql.Append("delete from " + Points.Pref + "_data.fn_dogovor where nzp_fd = " + finder.nzp_fd + " ");
#else
                sql.Append("delete from " + Points.Pref + "_data:fn_dogovor where nzp_fd = " + finder.nzp_fd + " ");
#endif

                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка удаления данных " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return false;
                }

                //удаление из fn_dogovot_bank
                string sqlStr = " delete from " + Points.Pref + "_data" + tableDelimiter + "fn_dogovor_bank where nzp_fd = " + finder.nzp_fd;
                ExecSQL(conn_db, sqlStr);

                return true;

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры DelDogovorRequisites : " + ex.Message, MonitorLog.typelog.Error, true);
                return false;
            }
            finally
            {
                #region Закрытие соединений

                if (conn_db != null)
                {
                    conn_db.Close();
                }

                if (reader != null)
                {
                    reader.Close();
                }

                sql.Remove(0, sql.Length);

                #endregion
            }
        }

        public bool DelDogovorERCRequisites(DogovorRequisites finder, out Returns ret)
        {

            #region Проверка входных параметров

            ret = Utils.InitReturns();
            if (finder.nzp_fd <= 0)
            {
                ret.text = "Не задана строка удаления";
                return false;
            }

            #endregion

            IDbConnection conn_db = null;
            IDataReader reader = null;
            StringBuilder sql = new StringBuilder();

            List<DogovorRequisites> retList = new List<DogovorRequisites>();

            try
            {

                #region Открываем соединение с базами

                conn_db = GetConnection(Constants.cons_Kernel);

                ret = OpenDb(conn_db, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return false;
                }

                #endregion

                //проверка наличия ссылок на договор
                bool nolink = true;
                int year = 0;
                sql.Remove(0, sql.Length);
                sql.Append("select yearr from " + Points.Pref + "_kernel" + tableDelimiter +
                           "s_baselist where idtype = 4 order by yearr");
                ret = ExecRead(conn_db, out reader, sql.ToString(), true);
                if (!ret.result) return ret.result;
                while (reader.Read())
                {
                    year = 0;
                    if (reader["yearr"] != DBNull.Value) year = Convert.ToInt32(reader["yearr"]);

                    string fn_sended = Points.Pref + "_fin_" + year.ToString("0000").Substring(2, 2) + tableDelimiter + "fn_sended";
                    string fn_sended_dom = Points.Pref + "_fin_" + year.ToString("0000").Substring(2, 2) + tableDelimiter + "fn_sended_dom";
                    if (!TempTableInWebCashe(conn_db, fn_sended)) continue;
                    sql.Remove(0, sql.Length);
                    sql.Append("select count (*) from " + fn_sended + " where " + sNvlWord + "(nzp_fd,0) =" + finder.nzp_fd);
                    object obj = ExecScalar(conn_db, sql.ToString(), out ret, true);
                    int totalNumber = 0;
                    if (!ret.result)
                    {
                        conn_db.Close();
                        return ret.result;
                    }
                    try { totalNumber = Convert.ToInt32(obj); }
                    catch (Exception ex)
                    {
                        conn_db.Close();
                        ret.result = false;
                        ret.text = ex.Message;
                        return ret.result;
                    }
                    if (totalNumber > 0)
                    {
                        nolink = false;
                        break;
                    }
                    if (!TempTableInWebCashe(conn_db, fn_sended_dom)) continue;
                    sql.Remove(0, sql.Length);
                    sql.Append("select count (*) from " + fn_sended_dom + " where " + sNvlWord + "(nzp_fd,0) =" + finder.nzp_fd);
                    obj = ExecScalar(conn_db, sql.ToString(), out ret, true);
                    totalNumber = 0;
                    if (!ret.result)
                    {
                        conn_db.Close();
                        return ret.result;
                    }
                    try { totalNumber = Convert.ToInt32(obj); }
                    catch (Exception ex)
                    {
                        conn_db.Close();
                        ret.result = false;
                        ret.text = ex.Message;
                        return ret.result;
                    }
                    if (totalNumber > 0)
                    {
                        nolink = false;
                        break;
                    }
                }
                CloseReader(ref reader);

                sql.Remove(0, sql.Length);
                sql.Append("select count (*) from " + Points.Pref+"_data"+tableDelimiter + "fn_dogovor_bank_lnk"+
                    " where " + sNvlWord + "(nzp_fd,0) =" + finder.nzp_fd);
                object obj2 = ExecScalar(conn_db, sql.ToString(), out ret, true);
                int totalNumber1 = 0;
                if (!ret.result)
                {
                    conn_db.Close();
                    return ret.result;
                }
                try { totalNumber1 = Convert.ToInt32(obj2); }
                catch (Exception ex)
                {
                    conn_db.Close();
                    ret.result = false;
                    ret.text = ex.Message;
                    return ret.result;
                }
                if (totalNumber1 > 0) nolink = false;

                sql.Remove(0, sql.Length);
                sql.Append("select count (*) from " + Points.Pref + "_kernel" + tableDelimiter + "supplier" +
                    " where " + sNvlWord + "(nzp_scope,0) =  (select nzp_scope from " +
                    Points.Pref + "_data" + tableDelimiter + "fn_dogovor where " + sNvlWord + "(nzp_fd,0) =" + finder.nzp_fd + ")");
                obj2 = ExecScalar(conn_db, sql.ToString(), out ret, true);
                totalNumber1 = 0;
                if (!ret.result)
                {
                    conn_db.Close();
                    return ret.result;
                }
                try { totalNumber1 = Convert.ToInt32(obj2); }
                catch (Exception ex)
                {
                    conn_db.Close();
                    ret.result = false;
                    ret.text = ex.Message;
                    return ret.result;
                }
                if (totalNumber1 > 0) nolink = false;

                if (!nolink)
                {
                    ret.text = "Удаление договора не возможно, так как есть ссылки на этот договор";
                    ret.tag = -1;
                    return false;
                }
                sql.Remove(0, sql.Length);
                sql.Append("delete from " + Points.Pref + "_data" + tableDelimiter + "fn_scope_adres where nzp_scope = " +
                                  "  (select nzp_scope from " +
                    Points.Pref + "_data" + tableDelimiter + "fn_dogovor where " + sNvlWord + "(nzp_fd,0) =" + finder.nzp_fd + ")");
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка удаления данных " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return false;
                }

               

                sql.Remove(0, sql.Length);
#if PG
                sql.Append("delete from " + Points.Pref + "_data.fn_dogovor where nzp_fd = " + finder.nzp_fd + " ");
#else
                sql.Append("delete from " + Points.Pref + "_data:fn_dogovor where nzp_fd = " + finder.nzp_fd + " ");
#endif

                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка удаления данных " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return false;
                }
 sql.Remove(0, sql.Length);
                sql.Append("delete from " + Points.Pref + "_data" + tableDelimiter + "fn_scope where nzp_scope = " +
                                  "  (select nzp_scope from " +
                    Points.Pref + "_data" + tableDelimiter + "fn_dogovor where " + sNvlWord + "(nzp_fd,0) =" + finder.nzp_fd + ")");
                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка удаления данных " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return false;
                }
                ////удаление из fn_dogovot_bank
                //string sqlStr = " delete from " + Points.Pref + "_data" + tableDelimiter + "fn_dogovor_bank where nzp_fd = " + finder.nzp_fd;
                //ExecSQL(conn_db, sqlStr);

                return true;

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры DelDogovorRequisites : " + ex.Message, MonitorLog.typelog.Error, true);
                return false;
            }
            finally
            {
                #region Закрытие соединений

                if (conn_db != null)
                {
                    conn_db.Close();
                }

                if (reader != null)
                {
                    reader.Close();
                }

                sql.Remove(0, sql.Length);

                #endregion
            }
        }

        public bool UpdateDogovorRequisites(DogovorRequisites finder, out Returns ret)
        {

            #region Проверка входных параметров

            ret = Utils.InitReturns();
            if (finder.nzp_fb <= 0)
            {
                ret.text = "Не задана строка удаления";
                return false;
            }

            #endregion

            IDbConnection conn_db = null;
            IDataReader reader = null;
            StringBuilder sql = new StringBuilder();

            List<DogovorRequisites> retList = new List<DogovorRequisites>();

            try
            {

                #region Открываем соединение с базами

                conn_db = GetConnection(Constants.cons_Kernel);

                ret = OpenDb(conn_db, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return false;
                }

                #endregion

                #region Получение локального юзера
                int local_user = finder.nzp_user;

                /*DbWorkUser dbU = new DbWorkUser();
                int local_user = dbU.GetLocalUser(conn_db, new Finder() { nzp_user = finder.nzp_user, pref = Points.Pref }, out ret);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка определения локального пользователя :" + ret.text, MonitorLog.typelog.Error, true);
                    return false;
                }
                dbU.Close();*/

                #endregion

#if PG
                sql.Append(" update " + Points.Pref + "_data.fn_dogovor set  nzp_area = " + finder.nzp_area + ",  nzp_payer_ar = " + finder.nzp_payer_ar + ", ");
#else
                sql.Append(" update " + Points.Pref + "_data:fn_dogovor set  nzp_area = " + finder.nzp_area + ",  nzp_payer_ar = " + finder.nzp_payer_ar + ", ");
#endif
                sql.Append(" nzp_payer = " + finder.nzp_payer + ",  nzp_fb = " + finder.nzp_fb + ",  nzp_osnov = " + finder.nzp_osnov + ", num_dog = '" + finder.num_dog + "', ");
                sql.Append(" dat_dog = '" + finder.dat_dog + "'," + " dat_s = " + (finder.dat_s == "" ? "null, " : " '" + finder.dat_s + "',") +
                    " dat_po = " + (finder.dat_po == "" ? "null, " : "'" + finder.dat_po + "',") + " max_sum = " + finder.max_sum + ", " +
                    " target = '" + finder.target + "', kpp = '" + finder.kpp + "', nzp_user = '" + local_user + "'" +
                    ", priznak_perechisl = " + finder.priznak_perechisl + ", min_sum = " + finder.min_sum +
                    ", naznplat = '" + finder.naznplat + "'" +
#if PG
 ",  dat_when = current_date ");
#else
 ",  dat_when = today ");
#endif
                sql.Append(" where  nzp_fd = " + finder.nzp_fd + " ");

                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка обновления данных " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return false;
                }
                else ret.tag = finder.nzp_fd;

                #region сохранение источников(банков)
                string sqlStr = " delete from " + Points.Pref + "_data" + tableDelimiter + "fn_dogovor_bank where nzp_fd = " + finder.nzp_fd + "; ";
                foreach (var b in finder.bank_list)
                {
                    sqlStr += " insert into " + Points.Pref + "_data" + tableDelimiter + "fn_dogovor_bank (nzp_fd, nzp_bank, dat_when, nzp_user) VALUES " +
                        " (" + finder.nzp_fd + ", " + b + ", " + sCurDate + ", " + finder.nzp_user + "); ";
                }
                ExecSQL(conn_db, sqlStr);
                #endregion

                return true;

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры UpdateDogovorRequisites : " + ex.Message, MonitorLog.typelog.Error, true);
                return false;
            }
            finally
            {
                #region Закрытие соединений

                if (conn_db != null)
                {
                    conn_db.Close();
                }

                if (reader != null)
                {
                    reader.Close();
                }

                sql.Remove(0, sql.Length);

                #endregion
            }
        }

        public bool UpdateDogovorRequisitesSupp(DogovorRequisites finder, out Returns ret)
        {

            #region Проверка входных параметров

            ret = Utils.InitReturns();
            if (finder.nzp_fb <= 0)
            {
                ret.text = "Не задана строка удаления";
                return false;
            }

            #endregion

            IDbConnection conn_db = null;
            IDataReader reader = null;
            StringBuilder sql = new StringBuilder();

            List<DogovorRequisites> retList = new List<DogovorRequisites>();

            try
            {
                #region Открываем соединение с базами
                conn_db = GetConnection(Constants.cons_Kernel);

                ret = OpenDb(conn_db, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return false;
                }

                #endregion

                #region Получение локального юзера
                int local_user = finder.nzp_user;
                
                /*DbWorkUser dbU = new DbWorkUser();
                int local_user = dbU.GetLocalUser(conn_db, new Finder() { nzp_user = finder.nzp_user, pref = Points.Pref }, out ret);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка определения локального пользователя :" + ret.text, MonitorLog.typelog.Error, true);
                    return false;
                }
                dbU.Close();*/

                #endregion

                sql.Append(" update " + Points.Pref + "_data" + DBManager.tableDelimiter + "fn_dogovor set  nzp_supp = " + finder.nzp_supp + ",  nzp_payer_ar = " + finder.nzp_payer_ar + ", ");
                sql.Append(" nzp_payer = " + finder.nzp_payer + ",  nzp_fb = " + finder.nzp_fb + ",  nzp_osnov = " + finder.nzp_osnov + ", num_dog = '" + finder.num_dog + "', ");
                sql.Append(" dat_dog = '" + finder.dat_dog + "'," + " dat_s = " + (finder.dat_s == "" ? "null, " : " '" + finder.dat_s + "',") +
                    " dat_po = " + (finder.dat_po == "" ? "null, " : "'" + finder.dat_po + "',") + " max_sum = " + finder.max_sum + ", " +
                    " target = '" + finder.target + "', kpp = '" + finder.kpp + "', nzp_user = '" + local_user + "'" +
                    ", priznak_perechisl = " + finder.priznak_perechisl + ", min_sum = " + finder.min_sum +
                    ", naznplat = '" + finder.naznplat + "'");
                sql.Append(" where  nzp_fd = " + finder.nzp_fd + " ");

                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка обновления данных " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return false;
                }
                else ret.tag = finder.nzp_fd;

                #region сохранение источников(банков)
                string sqlStr = " delete from " + Points.Pref + "_data" + tableDelimiter + "fn_dogovor_bank where nzp_fd = " + finder.nzp_fd + "; ";
                foreach (var b in finder.bank_list)
                {
                    sqlStr += " insert into " + Points.Pref + "_data" + tableDelimiter + "fn_dogovor_bank (nzp_fd, nzp_bank, dat_when, nzp_user) VALUES " +
                        " (" + finder.nzp_fd + ", " + b + ", " + sCurDate + ", " + finder.nzp_user + "); ";
                }
                ExecSQL(conn_db, sqlStr);
                #endregion

                return true;

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры UpdateDogovorRequisites : " + ex.Message, MonitorLog.typelog.Error, true);
                return false;
            }
            finally
            {
                #region Закрытие соединений
                if (conn_db != null) conn_db.Close();
                if (reader != null) reader.Close();
                sql.Remove(0, sql.Length);
                #endregion
            }
        }

        public bool UpdateERCDogovorRequisites(DogovorRequisites finder, out Returns ret)
        {

            #region Проверка входных параметров

            ret = Utils.InitReturns();
            if (finder.nzp_fd <= 0)
            {
                ret.text = "Не задана строка";
                return false;
            }

            #endregion

            IDbConnection conn_db = null;
            IDataReader reader = null;
            StringBuilder sql = new StringBuilder();

            List<DogovorRequisites> retList = new List<DogovorRequisites>();

            try
            {
                #region Открываем соединение с базами
                conn_db = GetConnection(Constants.cons_Kernel);

                ret = OpenDb(conn_db, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return false;
                }

                #endregion

                #region Получение локального юзера
                int local_user = finder.nzp_user;

                /*DbWorkUser dbU = new DbWorkUser();
                int local_user = dbU.GetLocalUser(conn_db, new Finder() { nzp_user = finder.nzp_user, pref = Points.Pref }, out ret);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка определения локального пользователя :" + ret.text, MonitorLog.typelog.Error, true);
                    return false;
                }
                dbU.Close();*/

                #endregion

                sql.Append(" update " + Points.Pref + "_data" + DBManager.tableDelimiter + "fn_dogovor set  nzp_payer_agent = " + finder.nzp_payer_agent + ",  nzp_payer_princip = " + finder.nzp_payer_princip + ", ");
                sql.Append(" " + " num_dog = '" + finder.num_dog + "', ");
                sql.Append(" dat_dog = '" + finder.dat_dog + "'," + " dat_s = " + (finder.dat_s == "" ? "null, " : " '" + finder.dat_s + "',") +
                    " dat_po = " + (finder.dat_po == "" ? "null, " : "'" + finder.dat_po + "',")  +
                    " target = '" + finder.target + "', nzp_user = '" + local_user + "'");
                sql.Append(" where  nzp_fd = " + finder.nzp_fd + " ");

                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка обновления данных " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return false;
                }
                else ret.tag = finder.nzp_fd;

                //#region сохранение источников(банков)
                //string sqlStr = " delete from " + Points.Pref + "_data" + tableDelimiter + "fn_dogovor_bank where nzp_fd = " + finder.nzp_fd + "; ";
                //foreach (var b in finder.bank_list)
                //{
                //    sqlStr += " insert into " + Points.Pref + "_data" + tableDelimiter + "fn_dogovor_bank (nzp_fd, nzp_bank, dat_when, nzp_user) VALUES " +
                //        " (" + finder.nzp_fd + ", " + b + ", " + sCurDate + ", " + finder.nzp_user + "); ";
                //}
                //ExecSQL(conn_db, sqlStr);
                //#endregion

                return true;

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры UpdateDogovorRequisites : " + ex.Message, MonitorLog.typelog.Error, true);
                return false;
            }
            finally
            {
                #region Закрытие соединений
                if (conn_db != null) conn_db.Close();
                if (reader != null) reader.Close();
                sql.Remove(0, sql.Length);
                #endregion
            }
        }

        /// <summary>
        /// Добавить реквизиты контракта
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public bool AddContractRequisites(ContractRequisites finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            int counter = 0;
            string table_name = "fn_lsdogovor";

            IDbConnection conn_db = null;
            StringBuilder sql = new StringBuilder();
            IDataReader reader = null;

            try
            {
                #region Открываем соединение с базой

                conn_db = GetConnection(Constants.cons_Kernel);

                ret = OpenDb(conn_db, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog(" Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    ret.text = "Ошибка при добавлении нового контракта";
                    return false;
                }

                #endregion

                //#region get nzp_fb

                //sql.Remove(0, sql.Length);
                //sql.Append(" Select fb.nzp_payer");
                //sql.Append(" From " + Points.Pref + "_kernel: s_payer sp, " + Points.Pref + "_data: fn_bank fb" );
                //sql.Append(" Where fb.nzp_payer = sp.nzp_payer and sp.nzp_supp = " + finder.nzp_supp);

                //if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
                //{
                //    MonitorLog.WriteLog("Ошибка выборки AddContractRequisites" + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                //    ret.result = false;
                //    return false;
                //}
                //if (reader != null)
                //{
                //    while (reader.Read())
                //    {
                //        if (reader["nzp_fb"] != DBNull.Value) finder.nzp_fb = Convert.ToInt32(reader["nzp_fb"]);
                //        if (reader["nzp_payer"] != DBNull.Value) finder.nzp_payer = Convert.ToInt32(reader["nzp_payer"]);
                //    }
                //}

                //#endregion

                #region Проверка на уникальность записи в таблице

                sql.Remove(0, sql.Length);
                sql.Append(" Select count(nzp_payer) as count");
#if PG
                sql.Append(" From " + Points.Pref + "_data." + table_name);
#else
                sql.Append(" From " + Points.Pref + "_data:" + table_name);
#endif
                sql.Append(" Where nzp_con <> " + finder.nzp_con);
                sql.Append(" And nzp_payer = " + finder.nzp_payer);
                sql.Append(" And nzp_kvar = " + finder.nzp_kvar);


                if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки AddContractRequisites" + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    ret.text = "Ошибка при добавлении нового контракта";
                    return false;
                }
                if (reader != null)
                {
                    while (reader.Read())
                    {
                        if (reader["count"] != DBNull.Value) counter = Convert.ToInt32(reader["count"]);
                    }
                }

                if (counter != 0)
                {
                    ret.text = "Данная запись уже существует в базе";
                    return false;
                }

                #endregion

                #region Добавление данных

                sql.Remove(0, sql.Length);

#if PG
                sql.Append(" Insert into " + Points.Pref + "_data." + table_name);
#else
                sql.Append(" Insert into " + Points.Pref + "_data:" + table_name);
#endif
                sql.Append(" ( nzp_con, nzp_fb, nzp_osnov, nzp_kvar, num_dog, nzp_payer, dat_s, dat_po, comment)");
                sql.Append(" values (" +
#if PG
 "default"
#else
 "0"
#endif
 + ", " + finder.nzp_fb + ", " + finder.nzp_osnov + ", " + finder.nzp_kvar + ",");
                sql.Append(" '" + finder.num_dog.Trim() + "'," + finder.nzp_payer + ",'");
                sql.Append(finder.dat_s + "', '" + finder.dat_po + "', '" + finder.comment.Trim() + "')");

                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка вставки данных в AddContractRequisites " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.text = "Ошибка при добавлении нового контракта";
                    ret.result = false;
                    return false;
                }

                #endregion

                #region Добавление в sys_events события 'Формирование договора с квартиросъёмщиком'
                try
                {
                    var nzp_con = GetSerialValue(conn_db);
                    DbAdmin.InsertSysEvent(new SysEvents()
                    {
                        pref = Points.Pref,
                        nzp_user = finder.nzp_user,
                        nzp_dict = 7429,
                        nzp_obj = nzp_con,
                        note = "Договор №" + finder.num_dog
                    }, conn_db);
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                }
                #endregion

                return true;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры AddContractRequisites : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = "Ошибка при добавлении нового контракта";
                return false;
            }
            finally
            {
                #region Закрытие соединений

                if (conn_db != null)
                {
                    conn_db.Close();
                }

                if (reader != null)
                {
                    reader.Close();
                }

                sql.Remove(0, sql.Length);

                #endregion
            }
        }

        public bool DelContractRequisites(ContractRequisites finder, out Returns ret)
        {
            IDbConnection conn_db = null;
            StringBuilder sql = new StringBuilder();
            string table_name = "fn_lsdogovor";

            #region Проверка входных параметров

            ret = Utils.InitReturns();
            if (finder.nzp_con <= 0)
            {
                ret.text = "Не заполнен серийный номер для " + table_name;
                return false;
            }

            #endregion

            try
            {
                #region Открываем соединение с базами

                conn_db = GetConnection(Constants.cons_Kernel);

                ret = OpenDb(conn_db, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return false;
                }

                #endregion

                #region Добавление в sys_events события 'Удаление договора с квартиросъёмщиком'
                try
                {
                    var num = ExecScalar(conn_db, "select num_dog from " + Points.Pref + "_data" + tableDelimiter + table_name + " where nzp_con = " + finder.nzp_con, out ret, true);

                    DbAdmin.InsertSysEvent(new SysEvents()
                    {
                        pref = Points.Pref,
                        nzp_user = finder.nzp_user,
                        nzp_dict = 7431,
                        nzp_obj = finder.nzp_con,
                        note = num != null ? "Договор №" + num.ToString() : "Договор был успешно удален"
                    }, conn_db);
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteLog(ex.Message, MonitorLog.typelog.Error, true);
                }
                #endregion
#if PG
                sql.Append(" Delete from " + Points.Pref + "_data." + table_name);
#else
                sql.Append(" Delete from " + Points.Pref + "_data:" + table_name);
#endif
                sql.Append(" Where nzp_con = " + finder.nzp_con + " ");

                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка удаления данных " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    return false;
                }

                return true;

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры DelContractRequisites : " + ex.Message, MonitorLog.typelog.Error, true);
                return false;
            }
            finally
            {
                #region Закрытие соединений

                if (conn_db != null)
                {
                    conn_db.Close();
                }

                sql.Remove(0, sql.Length);

                #endregion
            }
        }

        public bool UpdateContractRequisites(ContractRequisites finder, out Returns ret)
        {
            ret = Utils.InitReturns();
            string table_name = "fn_lsdogovor";
            IDbConnection conn_db = null;
            IDataReader reader = null;
            StringBuilder sql = new StringBuilder();
            int counter = 0;

            try
            {
                #region Открываем соединение с базами

                conn_db = GetConnection(Constants.cons_Kernel);

                ret = OpenDb(conn_db, true);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    ret.text = "Ошибка при обновлении контракта";
                    return false;
                }

                #endregion

                //sql.Remove(0, sql.Length);
                //sql.Append(" Select fb.nzp_fb, fb.nzp_payer ");
                //sql.Append(" From " + Points.Pref + "_kernel: s_payer sp, " + Points.Pref + "_data: fn_bank fb"); 
                //sql.Append(" Where fb.nzp_payer = sp.nzp_payer and sp.nzp_supp = " + finder.nzp_supp);

                //if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
                //{
                //    MonitorLog.WriteLog("Ошибка выборки UpdateContractRequisites" + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                //    ret.result = false;
                //    return false;
                //}
                //if (reader != null)
                //{
                //    while (reader.Read())
                //    {
                //        if (reader["nzp_fb"] != DBNull.Value) finder.nzp_fb = Convert.ToInt32(reader["nzp_fb"]);
                //        if (reader["nzp_payer"] != DBNull.Value) finder.nzp_payer = Convert.ToInt32(reader["nzp_payer"]);
                //    }
                //}

                #region Проверка на уникальность записи в таблице

                sql.Remove(0, sql.Length);
                sql.Append(" Select count(nzp_payer) as count");
#if PG
                sql.Append(" From " + Points.Pref + "_data." + table_name);
#else
                sql.Append(" From " + Points.Pref + "_data:" + table_name);
#endif
                sql.Append(" Where nzp_con <> " + finder.nzp_con);
                sql.Append(" And nzp_payer = " + finder.nzp_payer);
                sql.Append(" And nzp_kvar = " + finder.nzp_kvar);


                if (!ExecRead(conn_db, out reader, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка выборки AddContractRequisites" + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.text = "Ошибка при обновлении контракта";
                    ret.result = false;
                    return false;
                }
                if (reader != null)
                {
                    while (reader.Read())
                    {
                        if (reader["count"] != DBNull.Value) counter = Convert.ToInt32(reader["count"]);
                    }
                }

                if (counter != 0)
                {
                    ret.text = " Данная запись уже существует в базе";
                    return false;
                }

                #endregion


                sql.Remove(0, sql.Length);
#if PG
                sql.Append(" Update " + Points.Pref + "_data." + table_name);
#else
                sql.Append(" Update " + Points.Pref + "_data:" + table_name);
#endif
                sql.Append(" Set nzp_fb = " + finder.nzp_fb + ",");
                sql.Append(" nzp_osnov = " + finder.nzp_osnov + ",");
                sql.Append(" num_dog = '" + finder.num_dog + "',");
                sql.Append(" nzp_payer = " + finder.nzp_payer + ",");
                sql.Append(" dat_s = '" + finder.dat_s + "',");
                sql.Append(" dat_po = '" + finder.dat_po + "',");
                sql.Append(" comment = '" + finder.comment + "'");
                sql.Append(" Where nzp_con = " + finder.nzp_con + " ");

                if (!ExecSQL(conn_db, sql.ToString(), true).result)
                {
                    MonitorLog.WriteLog("Ошибка обновления данных UpdateContractRequisites " + sql.ToString(), MonitorLog.typelog.Error, 20, 201, true);
                    ret.result = false;
                    ret.text = "Ошибка при обновлении контракта";
                    return false;
                }
                return true;

            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка выполнения процедуры UpdateContractRequisites : " + ex.Message, MonitorLog.typelog.Error, true);
                ret.text = "Ошибка при обновлении контракта";
                return false;
            }
            finally
            {
                #region Закрытие соединений

                if (conn_db != null)
                {
                    conn_db.Close();
                }

                if (reader != null)
                {
                    reader.Close();
                }

                sql.Remove(0, sql.Length);

                #endregion
            }
        }


        public Pack GetOperDaySettings(Finder packFinder, out Returns ret)
        {
            Pack pack = new Pack();

            // проверка наличия пользователя
            if (packFinder.nzp_user < 1)
            {
                ret = new Returns(false, "Не определен пользователь", -1);
                return null;
            }

            IDbConnection conn = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn, true);
            if (!ret.result) return null;

            DbParameters db = new DbParameters();
            Prm prmFinder = new Prm()
            {
                nzp_user = packFinder.nzp_user,
                pref = Points.Pref,
                prm_num = 10,
                year_ = Points.DateOper.Year,
                month_ = Points.DateOper.Month
            };

            Prm finder;

            try
            {
                #region получить режим смены операционного дня
                //-----------------------------------------------------------------------
                prmFinder.nzp_prm = 1277;
                finder = db.FindSimplePrmValue(conn, prmFinder, out ret);
                if (!ret.result) throw new Exception(ret.text);

                if (finder.val_prm == "1") pack.oper_day_change_mode = 1;
                else pack.oper_day_change_mode = 0;
                //-----------------------------------------------------------------------
                #endregion

                #region получить время смены операционного дня
                //-----------------------------------------------------------------------
                prmFinder.nzp_prm = 1278;
                finder = db.FindSimplePrmValue(conn, prmFinder, out ret);
                if (!ret.result) throw new Exception(ret.text);

                TimeSpan ts;
                if (TimeSpan.TryParse(finder.val_prm, out ts)) pack.oper_day_change_time = finder.val_prm;
                else pack.oper_day_change_time = "00:00";
                //-----------------------------------------------------------------------
                #endregion

                return pack;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка получения настроек смены операционного дня " + ex.Message, MonitorLog.typelog.Error, 20, 401, true);
                return null;
            }
        }

        public Returns SaveOperDaySettings(Pack finder)
        {
            Returns ret = new Returns();

            #region Проверка входных параметров
            if (finder.nzp_user < 1) return new Returns(false, "Пользователь не определен");

            TimeSpan ts;
            if (!TimeSpan.TryParse(finder.oper_day_change_time, out ts)) return new Returns(false, "Неверно задано время смены операционного дня");
            #endregion

            #region Установка подключения
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;
            #endregion

            #region Сохранение параметров
            DbParameters db = new DbParameters();

            // общие поля
            Param prm = new Param();
            prm.dat_s = "01.01.1900";
            prm.dat_po = "01.01.3000";

            prm.nzp_user = finder.nzp_user;
            prm.webLogin = finder.webLogin;
            prm.webUname = finder.webUname;
            prm.pref = Points.Pref;
            prm.nzp = 0;
            prm.prm_num = 10;

            try
            {
                // режим смены операционного дня
                prm.nzp_prm = 1277;
                prm.val_prm = finder.oper_day_change_mode.ToString();

                ret = db.SavePrm(conn_db, null, prm);
                if (!ret.result) throw new Exception(ret.text);

                // время смены операционного дня
                prm.nzp_prm = 1278;
                prm.val_prm = finder.oper_day_change_time;

                ret = db.SavePrm(conn_db, null, prm);
                if (!ret.result) throw new Exception(ret.text);

                return new Returns(true, "Настройки успешно сохранены");
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                MonitorLog.WriteLog("Ошибка сохранения настроек смены операционного дня " + ex.Message, MonitorLog.typelog.Error, 20, 401, true);
                return ret;
            }
            #endregion
        }

#warning Сомнительно перенесено
        [Obsolete("Устарело. Перечисление денег в разрезе УК")]
        public Returns SaveMoneySended(List<MoneySended> list)
        {
            if ((list == null) || (list != null && list.Count == 0)) return new Returns(false, "Не заданы суммы перечислений", -1);

            if (list[0].nzp_area < 1) return new Returns(false, "Не определена Управляющая организация", -1);
            if (list[0].nzp_payer < 1) return new Returns(false, "Не задан подрядчик", -1);
            if (!(list[0].nzp_user > 0)) return new Returns(false, "Не задан пользователь", -1);

            DateTime dat_oper = DateTime.MinValue;
            DateTime min_dat_oper = Points.DateOper;
            for (int iCount = 0; iCount < list.Count; iCount++)
            {
                if (list[iCount].nzp_area < 1 || list[iCount].nzp_payer < 1 /*|| list[iCount].nzp_serv < 1*/ || list[iCount].dat_oper == "")
                {
                    return new Returns(false, "Неверные входные параметры");
                }

                if (!DateTime.TryParse(list[iCount].dat_oper, out dat_oper))
                {
                    return new Returns(false, "Неверный формат даты операционного дня");
                }

                // разрешить перечислять деньги задним числом
                //if (dat_oper != Points.DateOper)
                //    return new Returns(false, "Можно редактировать только перечисления текущего операционного дня");

                if (list[iCount].pref.Trim() == "") list[iCount].pref = Points.Pref;
            }

            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            Returns ret = OpenDb(conn_db, true);
            if (!ret.result) return ret;

            #region определение локального пользователя
            int nzpUser = list[0].nzp_user;
            
            /*DbWorkUser db = new DbWorkUser();
            int nzpUser = db.GetLocalUser(conn_db, list[0], out ret);
            db.Close();
            if (!ret.result)
            {
                conn_db.Close();
                return ret;
            }*/
            #endregion

            IDbTransaction transaction;
            try { transaction = conn_db.BeginTransaction(); }
            catch { transaction = null; }
            string sql = "";
            IDataReader reader = null;

            #region очистка таблиц sended и sended_dom
            //--------------------------------------------------------------------------------------------------------------------
            foreach (MoneySended item in list)
            {
                // получить операционый день
                dat_oper = DateTime.Parse(item.dat_oper);
                // названия таблиц
                string sended = Points.Pref + "_fin_" + (dat_oper.Year % 100).ToString("00") + tableDelimiter + "fn_sended";
                string sended_dom = Points.Pref + "_fin_" + (dat_oper.Year % 100).ToString("00") + tableDelimiter + "fn_sended_dom";

                // очистка таблиц
                sql = "delete from " + sended + " where nzp_area = " + item.nzp_area + " and nzp_payer = " + item.nzp_payer + " and dat_oper = " + Utils.EStrNull(Convert.ToDateTime(item.dat_oper).ToShortDateString());
                ret = ExecSQL(conn_db, transaction, sql, true);
                if (!ret.result)
                {
                    if (transaction != null) transaction.Rollback();
                    conn_db.Close();
                    return ret;
                }

                sql = "delete from " + sended_dom + " where nzp_area = " + item.nzp_area + " and nzp_payer = " + item.nzp_payer + " and dat_oper = " + Utils.EStrNull(Convert.ToDateTime(item.dat_oper).ToShortDateString());
                ret = ExecSQL(conn_db, transaction, sql, true);
                if (!ret.result)
                {
                    if (transaction != null) transaction.Rollback();
                    conn_db.Close();
                    return ret;
                }
            }
            //--------------------------------------------------------------------------------------------------------------------
            #endregion

            Int32 nzp_dom = 0;
            decimal sum_send = 0;
            int nzp_serv = 0;
            List<MoneySended> tmp = new List<MoneySended>();
            MoneySended tmpItem;
            List<decimal> sumList = new List<decimal>();

            string zero = "0";
            if (tableDelimiter == ".") zero = "default";


            #region вставка данных
            //---------------------------------------------------------------------------------------------------------------------------------------------------
            foreach (MoneySended item in list)
            {
                // получить операционый день
                dat_oper = DateTime.Parse(item.dat_oper);
                // названия таблиц
                string sended = Points.Pref + "_fin_" + (dat_oper.Year % 100).ToString("00") + tableDelimiter + "fn_sended";
                string sended_dom = Points.Pref + "_fin_" + (dat_oper.Year % 100).ToString("00") + tableDelimiter + "fn_sended_dom";
                string distrib = Points.Pref + "_fin_" + (dat_oper.Year % 100).ToString("00") + tableDelimiter + "fn_distrib_dom_" + (dat_oper.Month % 100).ToString("00");

                if (item.nzp_fd < 1)
                {
                    if (transaction != null) transaction.Rollback();
                    conn_db.Close();
                    return new Returns(false, "Неверные входные параметры");
                }

                Int32 nzp_snd = 0;
                if (item.sum_send > 0)
                {
                    sql = "insert into " + sended + " (nzp_snd, dat_oper, nzp_area, nzp_serv, nzp_payer, nzp_fd, sum_send, nzp_user, dat_when, dat_pp, num_pp)" +
                        " values (" + zero + ", " + Utils.EStrNull(Convert.ToDateTime(item.dat_oper).ToShortDateString()) + ", " + item.nzp_area + ", " + item.nzp_serv + ", " + item.nzp_payer +
                        ", " + item.nzp_fd + ", " + item.sum_send + ", " + nzpUser + ", " + sCurDateTime + ", " +
                        /*Points.DateOper.ToShortDateString()*/
                    Utils.EStrNull(item.dat_pp) +
                    "," + item.num_pp + ")";

                    ret = ExecSQL(conn_db, transaction, sql, true);
                    if (!ret.result)
                    {
                        if (transaction != null) transaction.Rollback();
                        conn_db.Close();
                        return ret;
                    }

                    // получить ключ
                    sql = "select nzp_snd from " + sended +
                        " where dat_oper = " + Utils.EStrNull(Convert.ToDateTime(item.dat_oper).ToShortDateString()) +
                        "   and nzp_area = " + item.nzp_area +
                        (item.nzp_serv > 0 ? " and nzp_serv = " + item.nzp_serv : "") +
                        "   and nzp_payer = " + item.nzp_payer +
                        "   and nzp_fd = " + item.nzp_fd;

                    ret = ExecRead(conn_db, transaction, out reader, sql, true);
                    if (!ret.result)
                    {
                        conn_db.Close();
                        reader.Close();
                        return new Returns(false, "Не удалось выполнить запрос");
                    }


                    if (reader.Read())
                    {
                        if (reader["nzp_snd"] != DBNull.Value) nzp_snd = Convert.ToInt32(reader["nzp_snd"]);
                    }
                }

                if (nzp_snd <= 0) continue;

                #region получить суммы для распределения по домам
                //-----------------------------------------------------------------------------------------------------------
                sql = "select nzp_dom, nzp_serv, sum (" + sNvlWord + "(sum_out, 0)) as sum_ " +
                " from " + distrib +
                " where nzp_area = " + item.nzp_area +
                "   and nzp_payer = " + item.nzp_payer +
                (item.nzp_serv > 0 ? " and nzp_serv = " + item.nzp_serv : "") +
                "   and dat_oper = " + Utils.EStrNull(Convert.ToDateTime(item.dat_oper).ToShortDateString()) +
                " group by 1, 2";

                ret = ExecRead(conn_db, transaction, out reader, sql, true);
                if (!ret.result)
                {
                    conn_db.Close();
                    reader.Close();
                    return new Returns(false, "Не удалось выполнить запрос");
                }

                tmp.Clear();
                sumList.Clear();

                while (reader.Read())
                {
                    nzp_dom = 0;
                    sum_send = 0;
                    if (reader["nzp_dom"] != DBNull.Value) nzp_dom = Convert.ToInt32(reader["nzp_dom"]);
                    if (reader["sum_"] != DBNull.Value) sum_send = Convert.ToDecimal(reader["sum_"]);
                    if (reader["nzp_serv"] != DBNull.Value) nzp_serv = Convert.ToInt32(reader["nzp_serv"]);

                    if (sum_send <= 0) continue;

                    tmpItem = new MoneySended();
                    tmpItem.sum_send = sum_send;
                    tmpItem.nzp_dom = nzp_dom;
                    tmpItem.nzp_serv = nzp_serv;

                    tmp.Add(tmpItem);
                }

                for (int i = 0; i < tmp.Count; i++) sumList.Add(tmp[i].sum_send);

                sumList = MathUtility.DistributeSum(item.sum_send, sumList);

                for (int i = 0; i < tmp.Count; i++) tmp[i].sum_send = sumList[i];
                //-----------------------------------------------------------------------------------------------------------
                #endregion


                for (int i = 0; i < tmp.Count; i++)
                {
                    if (sumList[i] > 0)
                    {
                        sql = "insert into " + sended_dom + " (nzp_snd, nzp_send, dat_oper, nzp_area, nzp_serv, nzp_payer, nzp_fd, sum_send, nzp_user, dat_when, nzp_dom)" +
                            " values (" + zero + ", " + nzp_snd + ", " + Utils.EStrNull(Convert.ToDateTime(item.dat_oper).ToShortDateString()) + ", " + item.nzp_area + ", " + item.nzp_serv + ", " + item.nzp_payer +
                            ", " + item.nzp_fd + ", " + sumList[i] + ", " + nzpUser + ", " + sCurDateTime + ", " + tmp[i].nzp_dom + ")";

                        ret = ExecSQL(conn_db, transaction, sql, true);
                        if (!ret.result)
                        {
                            if (transaction != null) transaction.Rollback();
                            conn_db.Close();
                            return ret;
                        }
                    }
                }

                // очистка таблицы
                ret = ExecSQL(conn_db, transaction,
                        " delete from  " + distrib + " where  sum_send > 0" +
                                " and nzp_area = " + item.nzp_area +
                                " and nzp_payer = " + item.nzp_payer +
                                (item.nzp_serv > 0 ? " and nzp_serv = " + item.nzp_serv : "") +
                                " and nzp_bank = -1 " +
                                " and dat_oper = " + Utils.EStrNull(Convert.ToDateTime(item.dat_oper).ToShortDateString()), true);
                if (!ret.result)
                {
                    if (transaction != null) transaction.Rollback();
                    conn_db.Close();
                    return ret;
                }

                for (int i = 0; i < tmp.Count; i++)
                {
                    sql = "insert into " + distrib + " ( sum_send,nzp_area,nzp_serv, nzp_payer, nzp_bank, dat_oper, nzp_dom) values(" + tmp[i].sum_send +
                          " , " + item.nzp_area +
                            ", " + tmp[i].nzp_serv +
                            " , " + item.nzp_payer +
                            " , -1 " +
                            " , " + Utils.EStrNull(Convert.ToDateTime(item.dat_oper).ToShortDateString()) +
                            " , " + tmp[i].nzp_dom + ")";
                    ret = ExecSQL(conn_db, transaction, sql, true);
                    if (!ret.result)
                    {
                        if (transaction != null) transaction.Rollback();
                        conn_db.Close();
                        return ret;
                    }
                }
            }
            //---------------------------------------------------------------------------------------------------------------------------------------------------
            #endregion

            // delete
            foreach (MoneySended item in list)
            {
                // получить операционый день
                dat_oper = DateTime.Parse(item.dat_oper);
                if (min_dat_oper >= dat_oper)
                {
                    min_dat_oper = dat_oper;
                }
                // названия таблиц
                string sended = Points.Pref + "_fin_" + (dat_oper.Year % 100).ToString("00") + tableDelimiter + "fn_sended";
                string distrib = Points.Pref + "_fin_" + (dat_oper.Year % 100).ToString("00") + tableDelimiter + "fn_distrib_dom_" + (dat_oper.Month % 100).ToString("00");

                sql = " update " + distrib + " set sum_send = 0 " +
                  " where (select count(*) from " + sended + " where nzp_area = " + distrib + ".nzp_area " +
                  " and nzp_payer = " + distrib + ".nzp_payer " +
                  " and nzp_serv = (case when nzp_serv <> 0 then " + distrib + ".nzp_serv else nzp_serv end) " +
                  " and nzp_bank = -1 " +
                  " and dat_oper = " + distrib + ".dat_oper) = 0 " +
                  " and nzp_area = " + item.nzp_area +
                  " and nzp_payer = " + item.nzp_payer +
                  " and nzp_bank = -1 " +
                  " and dat_oper = " + Utils.EStrNull(Convert.ToDateTime(item.dat_oper).ToShortDateString());
                //ret = ExecSQL(conn_db, transaction, sql, true);
                if (!ret.result)
                {
                    if (transaction != null) transaction.Rollback();
                    conn_db.Close();
                    return ret;
                }
                /*

                ret = ExecSQL(conn_db,
                    " Update " + distrib +
                    " Set sum_send = ( " +
                                " Select sum(sum_send) From ttt_paxx a " +
                                " Where a.nzp_payer= " + distrib + ".nzp_payer " +
                                "   and a.nzp_area = " + distrib + ".nzp_area " +
                                "   and a.nzp_dom = " + distrib + ".nzp_dom " +
                                "   and a.nzp_serv = " + distrib + ".nzp_serv " +
                                "   and a.nzp_bank = " + distrib + ".nzp_bank " +
                                " ) " +
                    " Where dat_oper = " + dat_oper + sConvToDate + " " +
                    "   and 0 < ( Select count(*) From ttt_paxx a " +
                                " Where a.nzp_payer= " + distrib + ".nzp_payer " +
                                "   and a.nzp_area = " + distrib + ".nzp_area " +
                                "   and a.nzp_dom = " + distrib + ".nzp_dom " +
                                "   and a.nzp_serv = " + distrib + ".nzp_serv " +
                                "   and a.nzp_bank = " + distrib + ".nzp_bank " +
                                " ) "
                    , true);
                if (!ret.result)
                {
                    return;
                }
                */
                //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                //расчет итогового сальдо
                //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                ret = ExecSQL(conn_db, transaction,
                    " Update " + distrib +
                    " Set sum_out = sum_in + sum_rasp - sum_ud + sum_naud + sum_reval - sum_send " +
                    "   ,sum_charge = sum_rasp - sum_ud + sum_naud + sum_reval " +
                    " Where dat_oper = " + Utils.EStrNull(Convert.ToDateTime(item.dat_oper).ToShortDateString()) + "  " +
                    " and nzp_payer = " + item.nzp_payer +
                    " and nzp_area = " + item.nzp_area +
                    " and nzp_serv = " + item.nzp_serv +
                    " and nzp_bank = -1 ", true);
                if (!ret.result)
                {
                    if (transaction != null) transaction.Rollback();
                    conn_db.Close();
                    return ret;
                }
            }

            if (transaction != null)
            {
                transaction.Commit();
            }

            foreach (MoneySended item in list)
            {

                //int yy =  Convert.ToDateTime(item.dat_oper).Year;
                //int mm =  Convert.ToDateTime(item.dat_oper).Month;
                //int yy = min_dat_oper.Year;
                //int mm = min_dat_oper.Month;
                //Convert.CalcTypes.ParamCalc paramcalc = new CalcTypes.ParamCalc(-1, -1, "", yy, mm, yy, mm);
                DbCalcPack db2 = new DbCalcPack();
                //paramcalc.dat_oper = Convert.ToDateTime(min_dat_oper.ToShortDateString());
                //paramcalc.dat_oper = Convert.ToDateTime(min_dat_oper.ToShortDateString());
                db2.UpdateSaldoFndistrib(min_dat_oper, Convert.ToInt32(item.nzp_payer), Convert.ToInt32(item.nzp_area), out ret);
                db2.Close();
            }

            //DbCalc dbc = new DbCalc();
            //dbc.DistribPaXX_1(dat_oper, dat_oper, out ret);
            //dbc.Close();

            conn_db.Close();
            return ret;
        }


        public decimal GetSumFromCharge(Saldo finder, GetLsSumOperations operation, out Returns ret)
        {
            string connectionString = Points.GetConnByPref(finder.pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return 0;

#if PG
            string table = finder.pref + "_charge_" + finder.year_.ToString("0000").Substring(2, 2) + ".charge_" + finder.month_.ToString("00");
#else
            string table = finder.pref + "_charge_" + finder.year_.ToString("0000").Substring(2, 2) + ":charge_" + finder.month_.ToString("00");
#endif
            if (!TempTableInWebCashe(conn_db, table))
            {
                ret = new Returns(false, "Начислений не найдено", -1);
                conn_db.Close();
                return 0;
            }
            decimal sum = 0;
            string field_name = "";
            if (operation == GetLsSumOperations.GetSumOutSaldo)
            {
                field_name = "sum_outsaldo";
            }

            if (field_name == "")
            {
                conn_db.Close();
                return sum;
            }

            string sql = "select  sum(" + field_name + ") as sum from " + table + " where dat_charge is null and nzp_serv > 1 and nzp_kvar = " + finder.nzp_kvar;
            IDataReader reader;
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result)
            {
                conn_db.Close();
                return 0;
            }

            if (reader.Read())
                if (reader["sum"] != DBNull.Value) sum = Convert.ToDecimal(reader["sum"]);

            reader.Close();
            conn_db.Close();
            return sum;
        }



        public List<string> GetRS(Pack_ls finder, out Returns ret)
        {
            string connectionString = Points.GetConnByPref(Points.Pref);
            IDbConnection conn_db = GetConnection(connectionString);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return null;

            StringBuilder sql = new StringBuilder("select yearr from " + Points.Pref + "_kernel" + tableDelimiter + "s_baselist where idtype = 4 order by yearr");
            IDataReader reader = null;
            ret = ExecRead(conn_db, out reader, sql.ToString(), true);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }
            sql.Remove(0, sql.Length);
            sql.AppendFormat("select " + sUniqueWord + "(" + sNvlWord + "(substr(" +
#if PG
 "CAST(pkod as varchar),1,3),'0'))"
#else
            "pkod,1,3),0))"
#endif
 + " as rs from {0}_data{1}kvar", Points.Pref, tableDelimiter);
            sql.Append(" union ");
            sql.AppendFormat("select " + sUniqueWord + "(" + sNvlWord + "(substr(" +
#if PG
 "CAST(pkod_supp as varchar),1,3),'0'))"
#else
            "pkod_supp,1,3),0))"
#endif
 + " as rs from {0}_data{1}supplier_codes", Points.Pref, tableDelimiter);
            while (reader.Read())
            {
                int year = 0;
                if (reader["yearr"] != DBNull.Value) year = Convert.ToInt32(reader["yearr"]);
                string table = Points.Pref + "_fin_" + year.ToString("0000").Substring(2, 2) + tableDelimiter + "pack_ls";
                if (!TempTableInWebCashe(conn_db, table)) continue;
                sql.Append(" union ");
                sql.AppendFormat("select " + sUniqueWord + "(" + sNvlWord + "(substr(" +
#if PG
 "CAST(pkod as varchar),1,3),'0'))"
#else
            "pkod,1,3),0))"
#endif
 + " as rs from {0}", table);
            }
            CloseReader(ref reader);

            ret = ExecRead(conn_db, out reader, sql.ToString(), true);
            if (!ret.result)
            {
                conn_db.Close();
                return null;
            }
            List<string> list = new List<string>();
            int i = 0;
            while (reader.Read())
            {
                string rs = "";
                if (reader["rs"] != DBNull.Value) rs = Convert.ToString(reader["rs"]);
                decimal rsd = Convert.ToDecimal(rs);
                if (rsd == 0) if (list.Contains("0")) continue;
                i++;
                if (finder.skip > 0 && finder.skip >= i) continue;
                list.Add(rs);
                if (finder.rows > 0 && i >= finder.skip + finder.rows) break;
            }
            CloseReader(ref reader);
            conn_db.Close();
            return list;
        }
        public List<Pack_ls> GetKodSumList(Pack_ls finder, out Returns ret)
        {
            var list = new List<Pack_ls>();
            if (finder.nzp_pack_ls > 0 && finder.year_ <= 0)
            {
                ret = new Returns(false, "Не заданы реквизиты оплаты", -1);
                return list;
            }
         
            var connDb = GetConnection(Points.GetConnByPref(Points.Pref));
            ret = OpenDb(connDb, true);
            if (!ret.result) return list;

            var packType = 0;
            var kodSums = "33, 41, 49, 50, 55, 57, 40, 35";

            try
            {
                if (finder.nzp_pack_ls > 0 && finder.year_ > 0)
                {
                    var sb = new StringBuilder();
                    sb.AppendFormat("select pack_type from {0}_fin_{1}{2}pack where nzp_pack = ", Points.Pref,
                        (finder.year_%100).ToString("00"), tableDelimiter);
                    sb.AppendFormat(" (select distinct nzp_pack from {0}_fin_{1}{2}pack_ls where nzp_pack_ls  = {3}) ", Points.Pref, (finder.year_%100).ToString("00"), tableDelimiter, finder.nzp_pack_ls);
                    object obj = ExecScalar(connDb, sb.ToString(), out ret, true);
                    if (!ret.result) return null;
                    Int32.TryParse(obj.ToString(), out packType);
                }
                switch(packType)
                {
                    case 20:
                        kodSums = "40, 50, 49, 35";
                        break;
                    case 10:
                        kodSums = "33";
                        break;
                }
                string sql = " SELECT kod, comment " +
                             " FROM " + Points.Pref + sKernelAliasRest + "kodsum" +
                             " WHERE kod in ("+kodSums+")";//???
                DataTable dt = ClassDBUtils.OpenSQL(sql, connDb).resultData;
                foreach (DataRow r in dt.Rows)
                {
                    Pack_ls p = new Pack_ls();
                    if (r["kod"] != DBNull.Value) p.kod_sum = Convert.ToInt32(r["kod"]);
                    if (r["comment"] != DBNull.Value) p.kod_sum_name = p.kod_sum + " " + r["comment"];
                    list.Add(p);
                }
            }
            catch (Exception ex)
            {
                ret.result = false;
                MonitorLog.WriteLog("Ошибка GetKodSumList \n  " + ex.Message + " " + ex.StackTrace, MonitorLog.typelog.Error, true);
            }
            finally
            {
                connDb.Close();
            }

            return list;
        }



        internal Pack_ls AutoSavePackLs(Pack_ls finder, out Returns ret)
        {
            #region информация о ЛС
            List<Ls> list = new List<Ls>();
            using (DbAdres db = new DbAdres())
            {
                list = db.LoadLsData(finder, out ret);
            }
            if (list == null || list.Count < 1 || list[0].nzp_kvar < 1)
            {
                ret = new Returns(false, "Не найден лицевой счет", -1);
                return null;
            }
            #endregion

            finder.nzp_kvar = list[0].nzp_kvar;
            finder.pref = list[0].pref;
            
            int month = Points.GetCalcMonth(new CalcMonthParams { pref = list[0].pref }).month_;
            int year = Points.GetCalcMonth(new CalcMonthParams { pref = list[0].pref }).year_;
            int previousMonth = month == 1 ? 12 : month - 1;
            int previousYear = month == 1 ? year - 1 : year;

            finder.dat_month = "01." + previousMonth.ToString("00") + "." + previousYear.ToString("0000");

            Pack_ls pack_ls;
            using (DbPack dbpack = new DbPack())
            {
                pack_ls = dbpack.SavePackLs(finder, out ret);
            }

            return pack_ls;
        }

        internal Returns ReplacePackLs(List<Pack_ls> finder)
        {
            Returns ret = Utils.InitReturns(); IDbConnection conn_db = GetConnection(Points.GetConnByPref(Points.Pref));
            ret = OpenDb(conn_db, true);
            if (!ret.result)
            {
                return ret;
            }
            int success_replaced_pack_ls_count = 0;
            try
            {
                string sql;
                foreach (var pack_ls in finder)
                {
                    string prev_fin = Points.Pref + "_fin_" + pack_ls.year_.ToString().Substring(2, 2) + tableDelimiter;
                    string cur_fin = Points.Pref + "_fin_" + Points.DateOper.Year.ToString().Substring(2, 2) +
                                     tableDelimiter;

                    #region данные по оплате

                    sql =
                        " SELECT pl.dat_month, pl.dat_vvod, pl.date_distr, pl.g_sum_ls, p.pack_type," +
                        " p.dat_pack, pl.info_num, pl.num_ls, p.sum_pack, p.num_pack " +
                        " FROM " + prev_fin + "pack_ls pl," +
                        prev_fin + "pack p " +
                        " WHERE p.nzp_pack = pl.nzp_pack AND pl.nzp_pack_ls = " + pack_ls.nzp_pack_ls;
                    DataTable dt = ClassDBUtils.OpenSQL(sql, conn_db).resultData;
                    if (dt.Rows.Count == 0)
                    {
                        ret.result = false;
                        ret.text += "Оплата ЛС " + pack_ls.num_ls + " не найдена в базе данных" + Environment.NewLine;
                        continue;
                    }
                    else if (dt.Rows.Count > 1)
                    {
                        ret.result = false;
                        ret.text += "Оплата ЛС " + pack_ls.num_ls + " имеет задвоение в базе данных" + Environment.NewLine;
                        continue;
                    }
                    if (dt.Rows[0]["pack_type"] != null)
                        pack_ls.pack_type = Convert.ToInt32(dt.Rows[0]["pack_type"].ToString());
                    if (dt.Rows[0]["dat_vvod"] != null) pack_ls.dat_vvod = dt.Rows[0]["dat_vvod"].ToString().Substring(0, 10);
                    if (dt.Rows[0]["dat_month"] != null) pack_ls.dat_month = dt.Rows[0]["dat_month"].ToString().Substring(0, 10);
                    if (dt.Rows[0]["date_distr"] != null) pack_ls.date_distr = dt.Rows[0]["date_distr"].ToString();
                    if (dt.Rows[0]["info_num"] != null)
                        pack_ls.info_num = Convert.ToInt32(dt.Rows[0]["info_num"].ToString());
                    else pack_ls.info_num = 0;
                    pack_ls.g_sum_ls = Convert.ToDecimal(dt.Rows[0]["g_sum_ls"].ToString());
                    pack_ls.dat_pack = dt.Rows[0]["dat_pack"].ToString().Substring(0, 10);
                    pack_ls.num_ls = Convert.ToInt32(dt.Rows[0]["num_ls"].ToString());
                    pack_ls.sum_pack = Convert.ToDecimal(dt.Rows[0]["sum_pack"].ToString());
                    pack_ls.snum_pack = dt.Rows[0]["num_pack"].ToString();
                    pack_ls.remark = " Оплата № квит " + pack_ls.info_num +
                        " от " + pack_ls.dat_vvod + " на сумму " + pack_ls.g_sum_ls + 
                        " (пачка № " + pack_ls.snum_pack + " от " + pack_ls.dat_pack + " сумма " + pack_ls.sum_pack + ")";

                    #endregion

                    #region префикс ЛС и код дома

                    //получаем код дома данного ЛС
                    sql =
                        " SELECT nzp_dom, trim(pref) as pref" +
                        " FROM " + Points.Pref + sDataAliasRest + "kvar" +
                        " WHERE num_ls = " + pack_ls.num_ls;
                    DataTable dtNzpDom = ClassDBUtils.OpenSQL(sql, conn_db).resultData;
                    if (dtNzpDom.Rows.Count == 0)
                    {
                        ret.result = false;
                        ret.text += pack_ls.remark + ": не найден код дома для ЛС " + pack_ls.num_ls +
                                    ", одна из проверок не может быть осуществлена" + Environment.NewLine;
                        continue;
                    }
                    string nzp_dom = dtNzpDom.Rows[0]["nzp_dom"].ToString();
                    string pref = dtNzpDom.Rows[0]["pref"].ToString();
                    pack_ls.pref = pref;

                    
                    #endregion

                    #region проверка на распределенность

                    Returns ret1 = CheckOnePackLsInOneMonth(conn_db, pack_ls);
                    if (!ret1.result)
                    {
                        ret.result = false;
                        ret.text += ret1.text;
                        continue;
                    }
                    #endregion

                    #region проверка на совпадение сумм

                    if(!CheckSums(conn_db, pack_ls, nzp_dom, ref ret)) continue;
                    
                    #endregion

                    #region перенос

                    #region переносим пачку

                    sql = 
                        " SELECT cp.nzp_pack" +
                        " FROM " + cur_fin + "pack cp, " +
                        prev_fin + "pack pp " +
                        " WHERE cp.nzp_pack = pp.nzp_pack and cp.num_pack = pp.num_pack AND cp.nzp_bank = pp.nzp_bank AND cp.file_name = pp.file_name " +
                        " AND pp.nzp_pack = " + pack_ls.nzp_pack;
                    DataTable dt_cur_pack = ClassDBUtils.OpenSQL(sql, conn_db).resultData;
                    if (dt_cur_pack.Rows.Count == 0)
                    {
                        //создаем пачку
                        sql =
                            " SELECT 1" +
                            " FROM " + cur_fin + "pack cp " +
                            " WHERE cp.nzp_pack = " + pack_ls.nzp_pack;
                        DataTable dt_nzp_pack_exists = ClassDBUtils.OpenSQL(sql, conn_db).resultData;
                        if (dt_nzp_pack_exists.Rows.Count > 0)
                        {
                            ret.result = false;
                            ret.text += pack_ls.remark + ": существует пачка в текущем периоде с таким же кодом," +
                                        " но другими характеристиками, оплата не перенесена " + Environment.NewLine;
                            continue;
                        }
                        sql =
                            " INSERT INTO " + cur_fin + "pack (nzp_pack, par_pack, pack_type, nzp_bank, num_pack," +
                            " dat_pack, dat_vvod, dat_inp, time_inp, file_name)" +
                            " SELECT nzp_pack, par_pack, pack_type, nzp_bank, num_pack," +
                            " dat_pack, dat_vvod, dat_inp, time_inp, file_name" +
                            " FROM " + prev_fin + "pack " +
                            " WHERE nzp_pack = " + pack_ls.nzp_pack;
                        ret1 = ExecSQL(conn_db, sql);
                        if (!ret1.result)
                        {
                            ret.result = false;
                            ret.text += pack_ls.remark + ": пачка не может быть перезаписана в текущий фин год," +
                                        " оплата не перенесена " + Environment.NewLine;
                            continue;
                        }
                    }
                    else if (dt_cur_pack.Rows.Count > 1)
                    {
                        ret.result = false;
                        ret.text += pack_ls.remark + ": найдено больше одной пачки, оплата не перенесена " + Environment.NewLine;
                        continue;
                    }
                    #endregion

                    #region перекидываем оплату
                    bool use_prev_nzp_pack_ls = false;
                    sql =
                        " SELECT 1 FROM " + cur_fin + "pack_ls" +
                        " WHERE nzp_pack_ls = " + pack_ls.nzp_pack_ls;
                    DataTable dt_nzp_pack_ls = ClassDBUtils.OpenSQL(sql, conn_db).resultData;
                    use_prev_nzp_pack_ls = dt_nzp_pack_ls.Rows.Count == 0;
                    if (use_prev_nzp_pack_ls)
                    {
                        sql =
                            " INSERT INTO " + cur_fin + "pack_ls" +
                            " SELECT * FROM " + prev_fin + "pack_ls" +
                            " WHERE nzp_pack_ls = " + pack_ls.nzp_pack_ls;
                        ret1 = ExecSQL(conn_db, sql);
                        if (!ret1.result)
                        {
                            ret.result = false;
                            ret.text += pack_ls.remark + ": не может быть перезаписана в текущий фин год," +
                                        " оплата не перенесена " + Environment.NewLine;
                            continue;
                        }
                        sql =
                            " DELETE FROM " + prev_fin + "pack_ls" +
                            " WHERE nzp_pack_ls = " + pack_ls.nzp_pack_ls;
                        ret1 = ExecSQL(conn_db, sql);

                    }
                    else
                    {
                        ret.result = false;
                        ret.text += pack_ls.remark + ":  найдена оплата с таким же кодом" +
                                    " в текущем фин периоде, оплата не перенесена " + Environment.NewLine;
                        continue;
                    }

                    #endregion

                    #region перекидываем pu_vals
                    sql =
                        " INSERT INTO " + cur_fin + "pu_vals" +
                        " (nzp_pack_ls, num_ls, nzp_ck, val_cnt, dat_month, pu_order, cur_unl, nzp_counter, nzp_serv, num_cnt)" +
                        " SELECT nzp_pack_ls, num_ls, nzp_ck, val_cnt, dat_month, pu_order, cur_unl, nzp_counter, nzp_serv, num_cnt" +
                        " FROM " + prev_fin + "pu_vals" +
                        " WHERE nzp_pack_ls = " + pack_ls.nzp_pack_ls;
                    ret1 = ExecSQL(conn_db, sql);
                    if (!ret1.result)
                    {
                        ret.result = false;
                        ret.text += pack_ls.remark + ": Не перенесены показания ИПУ " + Environment.NewLine;
                    }
                    #endregion

                    #region перекидываем gil_sums
                    sql =
                        " INSERT INTO " + cur_fin + "gil_sums" +
                        " (nzp_pack_ls, num_ls, nzp_serv, days_nedo, sum_nach, sum_oplat, dat_month, ordering, dat_uchet, is_union, nzp_supp)" +
                        " SELECT nzp_pack_ls, num_ls, nzp_serv, days_nedo, sum_nach, sum_oplat, dat_month, ordering, dat_uchet, is_union, nzp_supp" +
                        " FROM " + prev_fin + "gil_sums" +
                        " WHERE nzp_pack_ls = " + pack_ls.nzp_pack_ls;
                    ret1 = ExecSQL(conn_db, sql);
                    if (!ret1.result)
                    {
                        ret.result = false;
                        ret.text += pack_ls.remark + ": Не перенесены суммы, уточненные жильцом " + Environment.NewLine;
                    }

                    #endregion

                    #region update pack
                    sql =
                        " UPDATE " + prev_fin + "pack SET" +
                        " flag = 21," +
                        " count_kv = (select count(*) from " + prev_fin + "pack_ls where nzp_pack = " + pack_ls.nzp_pack + ")," +
                        " sum_pack = (select sum(g_sum_ls) from " + prev_fin + "pack_ls where nzp_pack = " + pack_ls.nzp_pack + ")," +
                        " sum_rasp = (select sum(g_sum_ls) from " + prev_fin + "pack_ls where nzp_pack = " + pack_ls.nzp_pack + ")," +
                        " sum_nrasp = 0" +
                        " WHERE nzp_pack = " + pack_ls.nzp_pack;
                    ret1 = ExecSQL(conn_db, sql);
                    if (!ret1.result)
                    {
                        ret.result = false;
                        ret.text += pack_ls.remark + ": Не обновлены данные для пачки в прошлом фин периоде " + Environment.NewLine;
                    }
                    sql =
                        " UPDATE " + cur_fin + "pack SET" +
                        " flag = 23," +
                        " dat_uchet = '" + Points.DateOper.ToShortDateString() + "'," +
                        " count_kv = (select count(*) from " + cur_fin + ".pack_ls where nzp_pack = " + pack_ls.nzp_pack + ")," +
                        " sum_pack = (select sum(g_sum_ls) from " + cur_fin + ".pack_ls where nzp_pack = " + pack_ls.nzp_pack + ")," +
                        " sum_nrasp = (select sum(g_sum_ls) from " + cur_fin + ".pack_ls where nzp_pack = " + pack_ls.nzp_pack + ")," +
                        " sum_rasp = 0" +
                        " WHERE nzp_pack = " + pack_ls.nzp_pack;
                    ret1 = ExecSQL(conn_db, sql);
                    if (!ret1.result)
                    {
                        ret.result = false;
                        ret.text += pack_ls.remark + ": Не обновлены данные для пачки в текущем фин пероиде " + Environment.NewLine;
                    }
                    #endregion

                    #endregion

                    success_replaced_pack_ls_count ++;
                }
            }
            catch (Exception ex)
            {
                ret.text += "Ошибка переноса оплат из корзины " + Environment.NewLine;
                ret.result = false;
                MonitorLog.WriteLog("Ошибка ReplacePackLs \n  " + ex.Message + " " + ex.StackTrace, MonitorLog.typelog.Error, true);
            }
            finally
            {
                conn_db.Close();
            }
            if (success_replaced_pack_ls_count < finder.Count)
                ret.text = "Количество успешно перенесенных оплат: " + success_replaced_pack_ls_count + " из " +
                    finder.Count + Environment.NewLine + ret.text;
            if (ret.text != "") ret.tag = -1; 
            return ret;
        }

        private static bool CheckSums(IDbConnection conn_db, Pack_ls pack_ls, string nzp_dom, ref Returns ret)
        {
            string sql;
            string prev_charge = pack_ls.pref + "_charge_" + pack_ls.year_.ToString().Substring(2, 2) + tableDelimiter;
            string prev_fin = Points.Pref + "_fin_" + pack_ls.year_.ToString().Substring(2, 2) + tableDelimiter;
            string[] months = {"", "январь","февраль", "март", "апрель", "май", "июнь", "июль", 
                "август", "сентябрь", "октябрь", "ноябрь", "декабрь"};
            bool passed = true;
            bool equal_sum = false;
            DateTime forMonth;
            List<int> monthList = new List<int>();
            if (pack_ls.dat_month != null && DateTime.TryParse(pack_ls.dat_month, out forMonth)) monthList.Add(forMonth.Month);
            if (pack_ls.dat_vvod != null && DateTime.TryParse(pack_ls.dat_vvod, out forMonth)) monthList.Add(forMonth.Month);
            if (pack_ls.date_distr != null && DateTime.TryParse(pack_ls.date_distr, out forMonth)) monthList.Add(forMonth.Month);
            
            decimal eps = 0.0001M;
            try
            {
                foreach (var i in monthList.Distinct())
                {
                    #region тип пачки 10
                    if (pack_ls.pack_type == 10)
                    {
                        sql =
                            " SELECT " + sNvlWord + "(sum(sum_rasp),0) as sum_rasp" +
                            " FROM " + prev_fin + "fn_distrib_dom_" + i.ToString("00") +
                            " WHERE nzp_dom = " + nzp_dom;
                        DataTable dt_distrib_dom = ClassDBUtils.OpenSQL(sql, conn_db).resultData;
                        if (dt_distrib_dom.Rows.Count == 0)
                        {
                            ret.result = false;
                            ret.text += pack_ls.remark + ": не найдена запись в учете перечислений по дому," +
                                        " одна из проверок не может быть осуществлена" + Environment.NewLine;
                            passed = false;
                            continue;
                        }
                        decimal distrib_dom = Convert.ToDecimal(dt_distrib_dom.Rows[0]["sum_rasp"]);

                        sql =
                            " SELECT " + sNvlWord + "(sum(sum_prih),0) as sum_prih" +
                            " FROM " + prev_charge + "fn_supplier" + i.ToString("00") +
                            " WHERE num_ls in" +
                            " (SELECT num_ls FROM " + Points.Pref + sDataAliasRest + "kvar where nzp_dom = " + nzp_dom +
                            ")";
                        DataTable dt_fn_supplier = ClassDBUtils.OpenSQL(sql, conn_db).resultData;
                        if (dt_fn_supplier.Rows.Count == 0)
                        {
                            ret.result = false;
                            ret.text += pack_ls.remark + ": не найдена запись в учете перечислений по поставщику" +
                                        ", одна из проверок не может быть осуществлена" + Environment.NewLine;
                            passed = false;
                            continue;
                        }
                        decimal fn_supplier = Convert.ToDecimal(dt_fn_supplier.Rows[0]["sum_prih"]);

                        sql =
                            " SELECT " + sNvlWord + "(sum(money_to),0) as money_to" +
                            " FROM " + prev_charge + "charge_" + i.ToString("00") +
                            " WHERE dat_charge is null AND nzp_serv > 1 AND num_ls IN" +
                            " (SELECT num_ls FROM " + Points.Pref + sDataAliasRest + "kvar where nzp_dom = " + nzp_dom +
                            ")";
                        DataTable dt_charge = ClassDBUtils.OpenSQL(sql, conn_db).resultData;
                        if (dt_charge.Rows.Count == 0)
                        {
                            ret.result = false;
                            ret.text += pack_ls.remark + ": не найдена запись в начислениях," +
                                        " одна из проверок не может быть осуществлена" + Environment.NewLine;
                            passed = false;
                            continue;
                        }
                        decimal charge = Convert.ToDecimal(dt_charge.Rows[0]["money_to"]);
                        equal_sum = ((Math.Abs(distrib_dom - fn_supplier) < eps) &&
                                     (Math.Abs(fn_supplier - charge) < eps));
                    }
                    #endregion
                    #region тип пачки 20
                    else if (pack_ls.pack_type == 20)
                    {
                        sql =
                            " SELECT " + sNvlWord + "(sum(sum_prih),0) as sum_prih" +
                            " FROM " + prev_charge + "from_supplier" +
                            " WHERE num_ls in" +
                            " (SELECT num_ls FROM " + Points.Pref + sDataAliasRest + "kvar where nzp_dom = " + nzp_dom +
                            ")" +
                            " AND dat_uchet >= '01." + i.ToString("00") + "." + pack_ls.year_ + "'" +
                            " AND dat_uchet <= '" +
                            DateTime.DaysInMonth(pack_ls.year_, i) + "." + i.ToString("00") + "." + pack_ls.year_ + "'";
                        DataTable dt_from_supplier = ClassDBUtils.OpenSQL(sql, conn_db).resultData;
                        if (dt_from_supplier.Rows.Count == 0)
                        {
                            ret.result = false;
                            ret.text += pack_ls.remark + ": не найдена запись в учете перечислений по поставщику" +
                                        ", одна из проверок не может быть осуществлена" + Environment.NewLine;
                            passed = false;
                            continue;
                        }
                        decimal from_supplier = Convert.ToDecimal(dt_from_supplier.Rows[0]["sum_prih"]);

                        sql =
                            " SELECT " + sNvlWord + "(sum(money_from),0) as money_from" +
                            " FROM " + prev_charge + "charge_" + i.ToString("00") +
                            " WHERE dat_charge is null AND nzp_serv > 1 AND num_ls IN" +
                            " (SELECT num_ls FROM " + Points.Pref + sDataAliasRest + "kvar where nzp_dom = " + nzp_dom +
                            ")";
                        DataTable dt_charge = ClassDBUtils.OpenSQL(sql, conn_db).resultData;
                        if (dt_charge.Rows.Count == 0)
                        {
                            ret.result = false;
                            ret.text += pack_ls.remark + ": не найдена запись в начислениях," +
                                        "одна из проверок не может быть осуществлена" + Environment.NewLine;
                            passed = false;
                            continue;
                        }
                        decimal charge = Convert.ToDecimal(dt_charge.Rows[0]["money_from"]);
                        equal_sum = Math.Abs(from_supplier - charge) < eps;
                    }
                    #endregion
                    else
                    {
                        ret.result = false;
                        ret.text += pack_ls.remark + ": тип пачки не опознан," +
                                    " одна из проверок не может быть осуществлена" + Environment.NewLine;
                        passed = false;
                    }
                    if (!equal_sum)
                    {
                        ret.result = false;
                        ret.text += pack_ls.remark + ": не прошла проверка совпадения сумм за " + months[i] + Environment.NewLine;
                        passed = false;
                    }
                }
            }
            catch (Exception ex)
            {
                ret.text += pack_ls.remark + ": ошибка проверки сумм" + Environment.NewLine;
                ret.result = false;
                MonitorLog.WriteLog("Ошибка CheckSums при переносе оплат корзины \n  " + ex.Message + " " + ex.StackTrace, MonitorLog.typelog.Error, true);
            }
            return passed;
        }

        /// <summary>
        /// проверка оплаты на распределенность в одном месяце
        /// </summary>
        /// <param name="finder">Оплата</param>
        /// <param name="month">Месяц</param>
        /// <returns></returns>
        private Returns CheckOnePackLsInOneMonth(IDbConnection conn_db, Pack_ls finder)
        {
            Returns ret = Utils.InitReturns();
            string sql;
            string[] months = new string[]{"", "январь","февраль", "март", "апрель", "май", "июнь", "июль", 
                "август", "сентябрь", "октябрь", "ноябрь", "декабрь"};

            string prev_charge = finder.pref + "_charge_" + finder.year_.ToString().Substring(2, 2) + tableDelimiter;
            sql =
                " SELECT * FROM " + prev_charge + "from_supplier" +
                " WHERE nzp_pack_ls = " + finder.nzp_pack_ls;
            DataTable dt = ClassDBUtils.OpenSQL(sql, conn_db).resultData;
            if (dt.Rows.Count > 0)
            {
                return new Returns(false,
                    finder.remark + ": уже распределена как оплата сторонних поставщиков" + Environment.NewLine);
            }

            for (int i = 1; i <= 12; i++)
            {
                sql =
                    " SELECT * FROM " + prev_charge + "fn_supplier" + i.ToString("00") +
                    " WHERE nzp_pack_ls = " + finder.nzp_pack_ls;
                dt = ClassDBUtils.OpenSQL(sql, conn_db).resultData;
                if (dt.Rows.Count > 0)
                {
                    return new Returns(false,
                        finder.remark + ": уже распределена как оплата поставщиков в месяце " + months[i] + Environment.NewLine);
                }
            }
            return ret;
        }

        /// <summary>
        /// Запись проводок в фоновой задаче
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        public void AddFonTaskProv(Pack finder, CalcFonTask.Types type, out Returns ret)
        {
            CalcFonTask calcfon = new CalcFonTask(Points.GetCalcNum(0));
            calcfon.TaskType = type;
            calcfon.Status = FonTask.Statuses.New; //на выполнение                         
            calcfon.nzp_user = finder.nzp_user;
            calcfon.nzp = finder.nzp_wp;
            calcfon.nzpt = finder.nzp_wp;
            calcfon.pref = finder.pref;
            calcfon.txt = calcfon.processName;
            var fn = new Finder();
            finder.CopyTo(fn);
            fn.listNumber = finder.mode;
            fn.DateOper = finder.DateOper;
            calcfon.parameters = JsonConvert.SerializeObject(fn);

            using (var db = new DbCalcQueueClient())
            {
                ret = db.AddTask(calcfon);
            }
        }

    } //end class

} //end namespace