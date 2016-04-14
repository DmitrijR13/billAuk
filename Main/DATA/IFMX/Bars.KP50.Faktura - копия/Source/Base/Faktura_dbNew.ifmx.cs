using System;
using System.Data;
using System.Globalization;
using Bars.KP50.Faktura.Source.FAKTURA;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using Globals.SOURCE.Container;

namespace Bars.KP50.Faktura.Source.Base
{
    //----------------------------------------------------------------------
    public partial class DbFaktura
    //----------------------------------------------------------------------
    {

        /// <summary>
        /// Выбирает список квартир для печати
        /// </summary>
        /// <param name="finder"></param>
        private void GetSelKvar(STCLINE.KP50.Interfaces.Faktura finder)
        {
            IDbConnection connWeb = DBManager.GetConnection(Constants.cons_Webdata);//new IDbConnection(Constants.cons_Webdata);

            string tXxSpls;
            try
            {
                Returns ret = DBManager.OpenDb(connWeb, true);

                if (!ret.result)
                {
                    MonitorLog.WriteLog("Формирование счетов. Ошибка при открытии соединения с БД ", MonitorLog.typelog.Error, true);
                    return;
                }
                tXxSpls = DBManager.GetFullBaseName(connWeb) + DBManager.tableDelimiter + "t" + finder.nzp_user + "_spls";
            }
            finally
            {
                connWeb.Close();
            }



            DBManager.ExecSQL(_conDb, " drop table fsel_kvar", false);


            string s = " Create temp table fsel_kvar ( " +
                       " nzp_kvar integer, " +
                       " num_ls integer, " +
                       " pkod " + DBManager.sDecimalType + "(13,0), " +
                       " nzp_dom integer, " +
                       " nzp_ul integer, " +
                       " typek integer default 0, " +
                       " fio char(100), " +
                       " ulica char(100), " +
                       " ndom char(20), " +
                       " idom integer, " +
                       " is_print integer default 1, " +
                       " ikvar integer, " +
                       " nkvar char(20), " +
                       " nkvar_n char(5), " +
                       " nzp_geu integer default 0," +
                       " nzp_area integer default 0, " +
                       " uch integer default 0, " +
                       " pref char(10) ) " + DBManager.sUnlogTempTable;
            DBManager.ExecSQL(_conDb, s, false);



            if ((finder.workRegim == STCLINE.KP50.Interfaces.Faktura.WorkFakturaRegims.One) ||
                (finder.workRegim == STCLINE.KP50.Interfaces.Faktura.WorkFakturaRegims.Web) ||
                (finder.workRegim == STCLINE.KP50.Interfaces.Faktura.WorkFakturaRegims.Bank))
            {
                s = " INSERT INTO fsel_kvar ( nzp_kvar, num_ls, pkod, nzp_dom, nzp_ul, typek, " +
                    "        fio, ulica, ndom, " +
                    "        idom, ikvar, nkvar,nkvar_n," +
                    "        nzp_geu, nzp_area, uch, pref) " +
                    " SELECT k.nzp_kvar, k.num_ls, k.pkod, k.nzp_dom, d.nzp_ul, k.typek, " +
                    "        k.fio, (trim(" + DBManager.sNvlWord + "(s.ulicareg,''))||' '||s.ulica) as ulica, trim(d.ndom)||' '||trim(" + DBManager.sNvlWord + "(d.nkor,'')) as ndom, " +
                    "        d.idom, k.ikvar, trim(" + DBManager.sNvlWord + "(k.nkvar,'')) as nkvar, trim(" + DBManager.sNvlWord + "(k.nkvar_n,'')) as nkvar_n," +
                    "        k.nzp_geu, k.nzp_area, 0 as uch, k.pref " +
                    " FROM " + Points.Pref + DBManager.sDataAliasRest + "kvar k , " +
                    Points.Pref + DBManager.sDataAliasRest + "dom d, " +
                    Points.Pref + DBManager.sDataAliasRest + "s_ulica s " +
                    " WHERE k.num_ls>0 " +
                    "        AND k.nzp_dom=d.nzp_dom " +
                    "        AND d.nzp_ul=s.nzp_ul ";

                if (finder.workRegim == STCLINE.KP50.Interfaces.Faktura.WorkFakturaRegims.One)
                    s += "        AND k.nzp_kvar = " + finder.nzp_kvar;

                if (finder.nzp_area > 0)
                    s += " AND k.nzp_area = " + finder.nzp_area;
                if (finder.nzp_geu > 0)
                    s += " AND k.nzp_geu = " + finder.nzp_geu;
                if (finder.pref != "")
                    s += " AND k.pref = '" + finder.pref + "'";

                if (!DBManager.ExecSQL(_conDb, s, true).result)
                {
                    _conDb.Close();
                }
            }
            else if (finder.workRegim == STCLINE.KP50.Interfaces.Faktura.WorkFakturaRegims.Group)
            {

                s = " INSERT INTO fsel_kvar ( nzp_kvar, num_ls, pkod, nzp_dom, nzp_ul, typek, " +
                    "        fio, ulica, ndom, " +
                    "        idom, ikvar, nkvar, nkvar_n, " +
                    "        nzp_geu, nzp_area, uch, pref) " +
                    " SELECT k.nzp_kvar, k.num_ls, k.pkod, k.nzp_dom, d.nzp_ul, k.typek, " +
                    "        k.fio, (trim(" + DBManager.sNvlWord + "(s.ulicareg,''))||' '||s.ulica) as ulica, trim(d.ndom)||' '||trim(" + DBManager.sNvlWord + "(d.nkor,'')) as ndom, " +
                    "        d.idom, k.ikvar, trim(" + DBManager.sNvlWord + "(k.nkvar,'')) as nkvar, trim(" + DBManager.sNvlWord + "(k.nkvar_n,'')) as nkvar_n," +
                    "        k.nzp_geu, k.nzp_area, 0 as uch, k.pref " +
                    " FROM " + tXxSpls + " sp, " +
                             Points.Pref + DBManager.sDataAliasRest + "kvar k, " +
                             Points.Pref + DBManager.sDataAliasRest + "dom d, " +
                             Points.Pref + DBManager.sDataAliasRest + "s_ulica s " +
                    " WHERE  k.num_ls>0 " +
                    "        AND k.nzp_dom=d.nzp_dom " +
                    "        AND d.nzp_ul=s.nzp_ul " +
                    "        AND k.nzp_kvar=sp.nzp_kvar ";
                if (finder.nzp_area > 0)
                    s += " AND nzp_area = " + finder.nzp_area;
                if (finder.nzp_geu > 0)
                    s += " AND nzp_geu = " + finder.nzp_geu;
                if (finder.pref != "")
                    s += " AND pref = '" + finder.pref + "'";
                if (!DBManager.ExecSQL(_conDb, s, true).result)
                {
                    _conDb.Close();
                }
            }


            MyDataReader goodReader;

            string datCalc = "01." + finder.month_ + "." + finder.year_;
            if (!DBManager.ExecRead(_conDb, out goodReader, " SELECT pref FROM fsel_kvar GROUP BY 1 ORDER BY 1 ", true).result)
            {
                MonitorLog.WriteLog("Формирование счетов", MonitorLog.typelog.Error, 20, 201, true);
                _conDb.Close();
                return;
            }
            while (goodReader.Read())
            {
                string baseData = goodReader["pref"].ToString().Trim() + "_data" +
                                  DBManager.tableDelimiter;

                if (finder.idFaktura != 101)
                {
                    s = " UPDATE fsel_kvar SET is_print = 1 " +
                        " WHERE 0<(SELECT COUNT(*) " +
                        "        FROM " + baseData + "prm_3 p " +
                        "        WHERE p.nzp_prm=51 " +
                        "               AND p.is_actual=1 " +
                        "               AND p.dat_s<='" + datCalc + "'" +
                        "               AND p.val_prm='1'" +
                        "               AND p.dat_po>='" + datCalc + "'" +
                        "               AND p.nzp = fsel_kvar.nzp_kvar)";
                }
                else
                {
                    s = " UPDATE fsel_kvar SET is_print = 1 ";
                }
                DBManager.ExecSQL(_conDb, s, true);

                if (finder.workRegim != STCLINE.KP50.Interfaces.Faktura.WorkFakturaRegims.One)
                    s = " UPDATE fsel_kvar SET is_print = 0 " +
                        " WHERE 0< (select count(*) " +
                        "        FROM " + baseData + "prm_1 p " +
                        "        WHERE p.nzp_prm=23 " +
                        "               AND p.is_actual=1 " +
                        "               AND p.dat_s<='" + datCalc + "'" +
                        "               AND p.val_prm='1'" +
                        "               AND p.dat_po>='" + datCalc + "'" +
                        "               AND p.nzp=fsel_kvar.nzp_kvar)";
                DBManager.ExecSQL(_conDb, s, true);


            }

        }

        /// <summary>
        /// Определяет количество квитанций в списке
        /// </summary>
        /// <returns></returns>
        private int GetCountKvit()
        {
            MyDataReader goodreader;
            int result = 1;
            string s = " SELECT count(*) as co " +
                       " FROM fsel_kvar a " +
                       " ," + Points.Pref + DBManager.sDataAliasRest + "s_ulica u " +
                       " ," + Points.Pref + DBManager.sDataAliasRest + "s_rajon r " +
                       " WHERE a.nzp_ul = u.nzp_ul AND r.nzp_raj = u.nzp_raj and is_print=1 ";
            if (DBManager.ExecRead(_conDb, out goodreader, s, true).result)
                if (goodreader.Read())
                {
                    if (goodreader["co"] != DBNull.Value)
                        Int32.TryParse(goodreader["co"].ToString().Trim(), out result);
                }
            if (goodreader != null) goodreader.Close();

            return result;
        }

        /// <summary>
        /// Определяет тип счета и алгоритма заполнения
        /// </summary>
        /// <param name="finder"></param>
        /// <returns></returns>
        private BaseFactura2 GetFakturaClass(STCLINE.KP50.Interfaces.Faktura finder)
        {
            //BaseFactura2 bill = null;
            var bill = (BaseFactura2)IocContainer.Current.Resolve<IBaseBill>(finder.idFaktura.ToString(CultureInfo.InvariantCulture));

            if (bill != null) bill.Init(_conDb, finder.year_, finder.month_);
            
            //switch (finder.idFaktura)
            //{

            //    case 10107:
            //    {
            //        bill = new TulaNewFaktura();
            //        bill.Init(_conDb, finder.year_, finder.month_);
            //        }
            //        break;
            //    case 121:
            //    {
            //        bill = new InstalmentFaktura();
            //        bill.Init(_conDb, finder.year_, finder.month_);
            //    }
            //        break;
            //    case 1101:
            //    {
            //        bill = new KznUyutdLiftNewFaktura();
            //        bill.Init(_conDb, finder.year_, finder.month_);

            //    }
            //        break;

            //}

            return bill;
        }

        /// <summary>
        /// Формирует набор данных для счетов
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        private DataSet GetGroupDataSet2(STCLINE.KP50.Interfaces.Faktura finder, out Returns ret)
        {
            ret = STCLINE.KP50.Global.Utils.InitReturns();

            UpdateBillFon(0);

            int countKvit = 0;
            try
            {
                if (finder.idFaktura == 102)
                    finder.idFaktura = 1003;
                BaseFactura2 bill = GetFakturaClass(finder);

                DataSet fDataSet = bill.MakeFewTables();
                if (fDataSet.Tables.Count == 0 || fDataSet.Tables[0] == null)
                {
                    MonitorLog.WriteLog("Ошибка создания счета квитанции, заголовок", MonitorLog.typelog.Error, true);
                    ret.result = false;
                    return fDataSet;
                }
                
                UpdateBillFon(20);


                GetSelKvar(finder);

                int maxCountLs = GetCountKvit();

                string s = " SELECT a.*,geu, rajon,t.town  " +
                           " FROM fsel_kvar a left outer join " + Points.Pref + DBManager.sDataAliasRest + "s_geu sg" +
                           " on a.nzp_geu=sg.nzp_geu " +
                           " ," + Points.Pref + DBManager.sDataAliasRest + "s_ulica u " +
                           " ," + Points.Pref + DBManager.sDataAliasRest + "s_rajon r " +
                           " left outer join " + Points.Pref + DBManager.sDataAliasRest + "s_town t on r.nzp_town=t.nzp_town" +
                           " WHERE a.nzp_ul = u.nzp_ul AND r.nzp_raj = u.nzp_raj and is_print=1" +
                           " ORDER BY geu, ulica, idom, ndom, ikvar, nkvar ";

                IntfResultTableType goodreader = ClassDBUtils.OpenSQL(s, _conDb);
                if (goodreader.resultCode == -1)
                {
                    MonitorLog.WriteLog("Ошибка выборки cписка квартир " + s, MonitorLog.typelog.Error, 20, 201, true);
                    _conDb.Close();
                    return null;
                }

                foreach (DataRow dr in goodreader.resultData.Rows)
                {

                    bill.Month = finder.month_;
                    bill.Year = finder.year_;
                    bill.FullMonthName = finder.YM.name_month;
                    bill.BillRegim = finder.workRegim;
                    if ((countKvit % 100) == 0) UpdateBillFon(20 + (40 * countKvit / maxCountLs));
                    bill.Pref = dr["pref"].ToString().Trim();
                    bill.NzpKvar = Convert.ToInt32(dr["nzp_kvar"]);
                    bill.NzpDom = Convert.ToInt32(dr["nzp_dom"]);
                    bill.NumLs = Convert.ToInt32(dr["num_ls"]);
                    bill.Pkod = dr["pkod"] != DBNull.Value ? Decimal.Parse(dr["pkod"].ToString()).ToString(CultureInfo.InvariantCulture) : "";
                    bill.NzpArea = dr["nzp_area"] != DBNull.Value ? Convert.ToInt32(dr["nzp_area"]) : 0;
                    bill.NzpGeu = dr["nzp_geu"] != DBNull.Value ? Convert.ToInt32(dr["nzp_geu"]) : 0;
                    bill.Rajon = dr["rajon"] != null ? dr["rajon"].ToString().Trim() : String.Empty;
                    bill.Ulica = dr["ulica"] != null ? dr["ulica"].ToString().Trim() : String.Empty;
                    bill.NumberDom = dr["ndom"] != null ? dr["ndom"].ToString().Trim() : String.Empty;
                    bill.NumberFlat = dr["nkvar"] != null ? dr["nkvar"].ToString().Trim() : String.Empty;
                    bill.NumberRoom = dr["nkvar_n"] != null ? dr["nkvar_n"].ToString().Trim() : String.Empty;
                    bill.Ud = dr["uch"] != DBNull.Value ? Convert.ToString(dr["uch"]).Trim() : "";
                    bill.Dom.SetNzpDom(bill.Pref, bill.NzpDom);
                    bill.Area.LoadAreaPrm(bill.Pref, bill.NzpArea);
                    bill.Town = dr["town"] != null ? dr["town"].ToString().Trim() : String.Empty;

                    

                    bill.NumberDom = Convert.ToString(dr["ndom"]).Trim();


                    bill.NumberFlat = Convert.ToString(dr["nkvar"]).Trim();



                    bill.Kvar.LoadKvarPrm(bill.Pref, bill.NzpKvar);
                    if (String.IsNullOrEmpty(bill.Kvar.PayerFio)) bill.Kvar.PayerFio = dr["fio"].ToString().Trim().TrimEnd('-',' ');
                    //todo Добавить тип счета в finder

                    if (IsAvansSchet(finder))
                        bill.Charge.PreLoadNachT(bill.Pref, bill.NzpKvar, bill.NzpArea);
                    else
                        bill.Charge.PreLoadNach(bill.Pref, bill.NzpKvar, bill.NzpArea);
                    countKvit++;
                    try
                    {
                        bill.FillTables(fDataSet);
                        ret.text = bill.Geu.GeuKodErc + "|" + bill.Shtrih;
                        bill.Clear();
                    }
                    catch (Exception ex)
                    {
                        MonitorLog.WriteLog("Ошибка при формировании квитанций(процедура GetGroupDataSet2), 124" +
                                            "pkod = " + bill.Pkod + ", месяц.год: " + finder.month_ + "." + finder.year_ + " " +
                                             ex.Message, MonitorLog.typelog.Error, 20, 201, true);
                    }
                }
                UpdateBillFon(60);
                return fDataSet;
            }
            catch (Exception e)
            {
                MonitorLog.WriteLog("Неожиданная ошибка формирования счетов " + e.Message, MonitorLog.typelog.Error, 20, 201, true);
                ret.result = false;
                return null;
            }


        }


        /// <summary>
        /// Проверка на то, что счет авансовый
        /// Авансовые счета начинаются с 10000
        /// </summary>
        /// <param name="finder">Инфо объект вида квитанции</param>
        /// <returns></returns>
        private static bool IsAvansSchet(STCLINE.KP50.Interfaces.Faktura finder)
        {
            return finder.idFaktura > 10000;
        }
    }

}


