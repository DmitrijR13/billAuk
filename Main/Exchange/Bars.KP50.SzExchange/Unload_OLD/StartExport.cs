using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;

namespace Bars.KP50.SzExchange.Unload
{
    public class StartExport : DataBaseHeadServer
    {
        /// <summary>
        /// Функция начала процесса выгрузки в СЗ
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="conn_db"></param>
        /// <returns></returns>
        public Returns ExportProcess(FilesImported finder, IDbConnection conn_db)
        {
            Returns ret = Utils.InitReturns();
            ret = OpenDb(conn_db, true);

            //string dats = DateTime.Now.ToString();        // Первый день месяца выгрузки 
            //string dat_po = DateTime.Now.ToString();      // Последний день месяца выгрузки
            string sql = "";
            string year_ = "";
            string month_ = "";
            //string file_name = "";                        // Наименование файла

            //вначале по паскаль коду идет выполнение процедуры TFrm_exp.FormCreate(Sender: TObject), она выполняется при старте формы модуля паскаль, т.е. мы должны выполнять ее при начале выгрузки.(?)
            //сюда входит выполнение процедуры получения названия банка set_bank_name, получение месяца и года из saldo_date и заполнение справочника с территориями loadarea.

            #region Получаем месяц и год из saldo_date

            sql = " SELECT month_, yearr " +
                  " FROM " + finder.bank + DBManager.sDataAliasRest + "saldo_date " +
                  " WHERE iscurrent = 0 ";
            DataTable dt = ClassDBUtils.OpenSQL(sql, conn_db, ClassDBUtils.ExecMode.Exception).GetData();
            foreach (DataRow rr in dt.Rows)
            {
                year_ = rr["yearr"].ToString();
                month_ = rr["month_"].ToString();
            }


            #endregion Получаем месяц и год из saldo_date

            //Первый день месяца выгрузки 
            //dats = "01." + Convert.ToDateTime(month_).ToString() + "." + Convert.ToDateTime(year_).ToString();  

            // Последний день месяца выгрузки
            //dat_po = DateTime.DaysInMonth(year, month).ToString();

            MonitorLog.WriteLog("Старт процесса выгрузки в СЗ", MonitorLog.typelog.Info, true);

            // !функция, которая возвращает имя БД начислений

            // !обработка функции test_bd

            // Получаем наименование файла
            // GetFileName();


            #region Comment
            //    string sqlTemp = "";

            //    // ограничение выгрузки 
            //    string dats = DateTime.Now.ToString();
            //    //finder.date_begin;
            //    // приходит пустота,а затем мы не можем обрезать пустоту
            //    string datpo = DateTime.Now.ToString();
            //    //finder.date.ToString();
            //    string sChargeAlias = "_charge_" + dats.Substring(8, 2);

            //    #region выбираем наименование улицы, района, города

            //    //выбираем наименование улицы, района, города
            //    sqlAdres = "DROP TABLE t_raj";

            //    ret = ExecSQL(conn_db, sqlAdres, false);


            //    sqlAdres = " SELECT ulica, ulicareg, rajon, town, nzp_ul, r.nzp_raj, t.nzp_town " +
            //               " INTO TEMP t_raj " +
            //               " FROM " + currPref + DBManager.sDataAliasRest
            //               + "s_ulica s, " + currPref + DBManager.sDataAliasRest + "s_town t, " +
            //               currPref + DBManager.sDataAliasRest + "s_rajon r " +
            //               " WHERE s.nzp_raj = r.nzp_raj " +
            //               " AND r.nzp_town = t.nzp_town ";

            //    ret = ExecSQL(conn_db, sqlAdres, true);
            //    if (!ret.result)
            //    {
            //        return ret;
            //    }


            //    #endregion выбираем наименование улицы, района, города

            //    #region Выбираем домохозяйства

            //    //Выбираем домохозяйства
            //    sqlAdres = "DROP TABLE t_adres1 ";
            //    ret = ExecSQL(conn_db, sqlAdres, false);

            //    sqlAdres = "DROP TABLE t_area ";
            //    ret = ExecSQL(conn_db, sqlAdres, false);

            //    sqlAdres = " CREATE TEMP TABLE t_area (" +
            //               " nzp_area INTEGER, " +
            //               " area CHAR(40), " +
            //               " typehos INTEGER " +
            //               ")";
            //    ret = ExecSQL(conn_db, sqlAdres, true);
            //    // дописать insert!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!


            //    sqlAdres = " SELECT fio, num_ls, nkvar, nkvar_n, ndom, nkor, ulica, ulicareg, rajon, town, " +
            //               " nzp_kvar, d.nzp_dom, s.nzp_ul, s.nzp_raj, s.nzp_town, area, typehos, pkod " +
            //               " INTO TEMP t_adres1 " +
            //               " FROM " + currPref + DBManager.sDataAliasRest + "kvar k, " + currPref + DBManager.sDataAliasRest +
            //               "dom d, t_raj s, " +
            //               " t_area sa " +
            //               " WHERE k.nzp_dom = d.nzp_dom " +
            //               " AND d.nzp_ul = s.nzp_ul " +
            //               " AND num_ls > 0 " +
            //               " AND k.nzp_area = sa.nzp_area " +
            //               " AND k.typek = 1 "; // для postgres
            //    //if nzp_group>0 then sqlAdres=sqlAdres+" and k.nzp_kvar in ( "+
            //    //                  " select nzp as nzp_kvar from "+ currPref +DBManager.sDataAliasRest+"link_group "+
            //    //                " where nzp_group="+inttostr(nzp_group)+")";
            //    //if is_dop_unl then sqlAdres=sqlAdres+" and k.num_ls in ( "+
            //    //                  " select num_ls from "+ currPref +DBManager.sDataAliasRest+"sz_must_unl"+
            //    //                  " where dat_calc='"+dats+"' and dat_charge='"+BaseDatS+"')";


            //    //sqlAdres = sqlAdres + " INTO TEMP t_adres1 ";// для informix
            //    // переделать под postgres!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

            //    ret = ExecSQL(conn_db, sqlAdres, true);
            //    if (!ret.result)
            //    {
            //        return ret;
            //    }


            //    #endregion Выбираем домохозяйства


            //    #region Загрузка параметров
            //    MonitorLog.WriteLog("Старт загрузки параметров", MonitorLog.typelog.Info, true );

            //    LoadParamsSz(finder, conn_db, -1, datpo);  //2ой параметр - nzp_group(откуда брать?)

            //    MonitorLog.WriteLog("Окончание загрузки параметров", MonitorLog.typelog.Info, true);
            //    ////Загрузка параметров
            //    //timelist.add(formatdatetime("hh:nn:ss.zzz", now)+ " Загрузка параметров ");
            //    //preparePrmTmp;!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! необходимо создать список
            //    //timelist.add(formatdatetime("hh:nn:ss.zzz", now)+ " Окончание загрузки параметров ");

            //    //          is_kzn:=GetFieldSum_sn_tarif;

            //    //          get_lgota_string;

            //    //timelist.add(formatdatetime("hh:nn:ss.zzz", now)+ " Загрузка начислений");

            //    #endregion Загрузка параметров

            //    #region Выбираем только открытые лицевые счета

            //    //Выбираем только открытые лицевые счета

            //    sqlAdres = "DROP TABLE t_pere";

            //    ret = ExecSQL(conn_db, sqlAdres, true);



            //    sqlAdres = "CREATE TEMP TABLE t_pere (" +
            //               " nzp_kvar integer, " +
            //               " num_ls integer, " +
            //               " nzp_serv integer, " +
            //               " nzp_measure integer, " +
            //               " nzp_supp integer," +
            //               " tarif Numeric(14,3), " +
            //               " tarif_f Numeric(14,3), " +
            //               " isdel integer, " +
            //               " dat_uchet Date, " +
            //               " sum_subsidy Numeric(14,2)," +
            //               " sum_subsidy_p Numeric(14,2)," +
            //               " sum_tarif_sn_f Numeric(14,2)," +
            //               " sum_tarif_sn_f_p Numeric(14,2)," +
            //               " sum_tarif_eot Numeric(14,2)," +
            //               " sum_lgota Numeric(14,2)," +
            //               " sum_lgota_p Numeric(14,2)," +
            //               " sum_smo Numeric(14,2)," +
            //               " sum_smo_p Numeric(14,2)," +
            //               " sum_tarif_eot_p Numeric(14,2)," +
            //               " is_device integer)";
            //    ret = ExecSQL(conn_db, sqlAdres, true);
            //    if (!ret.result)
            //    {
            //        return ret;
            //    }

            //    sqlAdres = "DROP TABLE tmp_lgcharge2";
            //    ret = ExecSQL(conn_db, sqlAdres, true);


            //    sqlTemp = "CREATE TEMP TABLE tmp_lgcharge2 (" +
            //              " nzp_kvar  integer, " +
            //              " nzp_serv  INTEGER," +
            //              " nzp_supp  integer," +
            //              " nzp_gilec integer," +
            //              " is_family integer," +
            //              " nzp_lgota integer," +
            //              " kod_cz Numeric(13,0)," +
            //              " nzp_law integer, " +
            //              " nzp_bud integer, " +
            //              " cz_law Numeric(13,0)," +
            //              " cz_lgota Numeric(13,0)," +
            //              " delta_lgota Numeric(14,2)," +
            //              " dat_charge DATE" +
            //              " ) ";
            //    ret = ExecSQL(conn_db, sqlAdres, true);
            //    if (!ret.result)
            //    {
            //        return ret;
            //    }


            //    sqlAdres = "DROP TABLE t_adres";
            //    ret = ExecSQL(conn_db, sqlAdres, true);


            //    // string sChargeAlias = "";


            //    sqlAdres =
            //        " SELECT * INTO TEMP t_adres FROM t_adres1 " +
            //        " WHERE nzp_kvar IN ( " +
            //        "  SELECT nzp_kvar " +
            //        "  FROM " + finder.bank + sChargeAlias + ".charge_" + dats.Substring(3, 2) +
            //        "  WHERE nzp_serv > 1" +
            //        "  AND dat_charge is null group by 1) " +
            //        "  AND nzp_kvar NOT IN " +
            //        "   (SELECT nzp " +
            //        "   FROM " + currPref + DBManager.sDataAliasRest + "prm_3 " +
            //        "   WHERE nzp_prm = 51 " +
            //        "   AND val_prm = '3' " +
            //        "   AND is_actual <> 100 " +
            //        "   AND dat_s <='" + datpo + "'  " +
            //        "   AND dat_po>='" + dats + "')";
            //    // например, ntul01_charge_14.charge_09!!!!!!!!!!!!!!!!!!!!!!!!!!!! исправила
            //    // нужно переделать запись currentPref, пока все работает только с верхним банком!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! 
            //    //string sql =
            //    //  " SELECT COUNT (*) as cnt " +
            //    //" FROM " + currPref + DBManager.sDataAliasRest + "prefer" +
            //    //" WHERE p_name = 'nzp_town_rg' " +
            //    // " AND p_value = '16587'";
            //    //var dt = ClassDBUtils.OpenSQL(sql, conn_db);
            //    // if (dt.resultData.Rows.Count > 0)
            //    // {

            //    // }
            //    // else
            //    // {

            //    //}
            //    //ret = ExecSQL(conn_db, sql, true); вынести это все в отдельную функцию,т.к.нам пока нужна только Осетия
            //    //if is_nch
            //    //{
            //    //  sqlAdres=sqlAdres + " and nzp_kvar not in (select nzp + "into temp t_adres" + from "+DBManager.sDataAliasRest+"prm_3 "+
            //    //      " where nzp_prm=51 and val_prm='2' and is_actual<>100 "+
            //    //    " and dat_s<='01.01.2008' and dat_po>='"+dats+"')";
            //    // }       

            //    ret = ExecSQL(conn_db, sqlAdres, true);
            //    sqlAdres = "DROP TABLE t_adres1";
            //    ret = ExecSQL(conn_db, sqlAdres, false);

            //    if (!ret.result)
            //    {
            //        return ret;
            //    }

            //    sqlAdres = " CREATE INDEX ix_tm_01 ON t_adres(num_ls) ";
            //    ret = ExecSQL(conn_db, sqlAdres, true);
            //    if (!ret.result)
            //    {
            //        return ret;
            //    }

            //    sqlAdres = " CREATE INDEX ix_tm_0212 ON t_adres(nzp_kvar) ";
            //    ret = ExecSQL(conn_db, sqlAdres, true);
            //    if (!ret.result)
            //    {
            //        return ret;
            //    }

            //    sqlAdres = " UPDATE statistics FOR TABLE t_adres "; // посмотреть
            //    ret = ExecSQL(conn_db, sqlAdres, true);
            //    if (!ret.result)
            //    {
            //        return ret;
            //    }



            //    sqlAdres = "DROP TABLE t_gil";
            //    ret = ExecSQL(conn_db, sqlAdres, false);

            //sqlAdres =
            //        " SELECT nzp_kvar," + currPref + DBManager.sDataAliasRest + "get_kol_gil('" + dats + "', '" + datpo +
            //        "', 15, nzp_kvar) as kol_gil " +
            //        " INTO TEMP t_gil" +
            //        " FROM t_adres ";
            //ret = ExecSQL(conn_db, sqlAdres, true);
            //    if (!ret.result)
            //    {
            //        return ret;
            //    }

            //sqlAdres = " CREATE INDEX ix_tm_06 ON t_gil(nzp_kvar) ";
            //ret = ExecSQL(conn_db, sqlAdres, true);

            //sqlAdres = " UPDATE statistics FOR TABLE t_gil ";
            //ret = ExecSQL(conn_db, sqlAdres, true);


            //    #endregion

            //    #region Для нижнекамска заменяем услугу
            //    /*
            //    //Для нижнекамска заменяем услугу
            //    //is_serv266=false;
            //    sqlTemp = 
            //            " SELECT * " +
            //            " FROM  " + currPref + sChargeAlias + "charge_" + dats.Substring(4, 2) +
            //            " WHERE nzp_serv IN (266, 284, 285)  " +
            //            " AND dat_charge IS NULL ";
            //    //sqlTemp.open;
            //    //if not sqlTemp.isEmpty then begin
            //    //  sqlTemp.close;
            //    //  is_serv266=true;



            //    sqlAdres = "DROP TABLE t_charge266";
            //    ret = ExecSQL(conn_db, sqlAdres, true);

            //    sqlTemp = 
            //                " SELECT a.nzp_kvar, a.num_ls, a.nzp_serv, a.nzp_supp, a.nzp_frm, a.isdel, a.is_device, " +
            //                " (CASE WHEN a.sum_tarif_eot > 0 THEN a.tarif ELSE 0 END) as tarif, " +
            //                " (CASE WHEN a.sum_tarif_eot > 0 THEN a.tarif_f ELSE 0 END) as tarif_f, a.sum_insaldo, " +
            //                " a.sum_tarif_sn_f, a.sum_tarif_eot, a.sum_subsidy, a.sum_lgota, a.sum_smo, a.reval_lgota," +
            //                " a.sum_money, a.c_sn " + 
            //                " INTO TEMP t_charge266" +
            //                " FROM " + currPref + sChargeAlias + "charge_" + dats.Substring(4, 2) + " a, t_adres b " +
            //                " WHERE a.nzp_serv IN (16, 266, 284, 285) " +
            //                " AND nzp_supp>-999 " +
            //                " AND dat_charge IS NULL " +
            //                " AND a.nzp_kvar = b.nzp_kvar";
            //    ret = ExecSQL(conn_db, sqlAdres, true);
            //    if (!ret.result)
            //    {
            //        return ret;
            //    }


            //    sqlTemp = "DROP TABLE t_cor266";
            //    ret = ExecSQL(conn_db, sqlTemp, true);

            //    sqlTemp = 
            //                    " SELECT nzp_kvar, num_ls, isdel, max(nzp_frm) as nzp_frm, max(nzp_supp) as nzp_supp " + 
            //                    " INTO TEMP t_cor266" +
            //                    " FROM t_charge266 a" +
            //                    " WHERE nzp_serv=16 " +
            //                    " GROUP BY 1,2,3";
            //    ret = ExecSQL(conn_db, sqlTemp, true);
            //    if (!ret.result)
            //    {
            //        return ret;
            //    }

            //    sqlTemp = "DROP TABLE t_charge266";
            //    ret = ExecSQL(conn_db, sqlTemp, true);
            //    sqlTemp = 
            //                " UPDATE t_charge266 " +
            //                " SET (nzp_supp, nzp_frm, nzp_serv) = ((SELECT nzp_supp, nzp_frm, 16  " +
            //                " FROM t_cor266 b " +
            //                " WHERE t_charge266.nzp_kvar = b.nzp_kvar " +
            //                " AND t_charge266.isdel = b.isdel)) " +
            //                " WHERE nzp_serv IN (266, 284, 285) ";
            //    ret = ExecSQL(conn_db, sqlTemp, true);
            //    if (!ret.result)
            //    {
            //        return ret;
            //    }

            //    sqlTemp = "DROP TABLE t_charge266";
            //    ret = ExecSQL(conn_db, sqlTemp, true);

            //    sqlTemp = " CREATE INDEX ix_tmpr0192 ON t_charge266(nzp_kvar, nzp_serv,nzp_frm)";
            //    ret = ExecSQL(conn_db, sqlTemp, true);
            //    if (!ret.result)
            //    {
            //        return ret;
            //    }

            //    sqlTemp = "DROP TABLE t_charge266";
            //    ret = ExecSQL(conn_db, sqlAdres, true);
            //    sqlTemp = " UPDATE statistics FOR TABLE t_charge266";
            //    ret = ExecSQL(conn_db, sqlTemp, true);
            //    if (!ret.result)
            //    {
            //        return ret;
            //    }


            //    // end
            //    //    else sqlTemp.close;


            //    //  st_bar_progress.Panels[0].Text="Выборка перерасчетов...";
            //    //  Application.ProcessMessages;
            //    //  if stop_flag=true then exit;
            //    sqlTemp = "DROP TABLE t_pere1";
            //    ret = ExecSQL(conn_db, sqlTemp, true);
            //    sqlAdres = 
            //                " CREATE TEMP TABLE t_pere1 (" +
            //                " nzp_kvar integer, " +
            //                " num_ls integer, " +
            //                " nzp_serv integer, " +
            //                " nzp_supp integer," +
            //                " tarif Numeric(14,3), " +
            //                " tarif_f Numeric(14,3), " +
            //                " nzp_frm integer, " +
            //                " is_device integer, " +
            //                " isdel integer, " +
            //                " dat_uchet Date, " +
            //                " sum_subsidy Numeric(14,2)," +
            //                " sum_subsidy_p Numeric(14,2)," +
            //                " sum_tarif_sn_f_p Numeric(14,2)," +
            //                " sum_tarif_sn_f Numeric(14,2)," +
            //                " sum_tarif_eot Numeric(14,2)," +
            //                " sum_lgota Numeric(14,2)," +
            //                " sum_lgota_p Numeric(14,2)," +
            //                " sum_smo Numeric(14,2)," +
            //                " sum_smo_p Numeric(14,2)," +
            //                " sum_tarif_eot_p Numeric(14,2))";
            //    ret = ExecSQL(conn_db, sqlTemp, true);
            //    if (!ret.result)
            //    {
            //        return ret;
            //    }


            //    //try
            //    //sqlTemp=" select month_, dbname,dbserver,b.year_ "+
            //    //                 " from "+ currPref +sChargeAlias+"lnk_charge_"+dats.Substring(4,2)+" b, s_baselist a, "+
            //    //                 " t_adres c, logtodb ld, s_logicdblist sl "+
            //    //                 " where a.yearr=b.year_ and b.nzp_kvar=c.nzp_kvar and yearr>=2005"+
            //    //                 " and ld.nzp_bl=a.nzp_bl and sl.nzp_ldb=ld.nzp_ldb and sl.ldbname='"
            //    //                 +LogicDbName+"' and idtype=1 ";
            //    //  if is_nch then sqlTemp=sqlTemp+" and yearr>=2010";
            //    //  sqlTemp=sqlTemp+"  group by 1,2,3,4 order by year_,month_ ";

            //    //                   //При открытии перерасчетов ранее 2005 года сообщить Оллегу
            //    //                   //чтоб проверить PutSubsidyToAnketaPodr в ОЕ
            //    //                   //При открытии перерасчетов ранее 2010 года сообщить Оллегу

            //    //  sqlTemp.open;
            //    //  if not sqlTemp.IsEmpty then
            //    //  while not sqlTemp.Eof do
            //    //    begin
            //    //      if sqlTemp.FieldByName("dbserver").asstring<>"" then
            //    //      sAliasm=sqlTemp.FieldByName("dbname").asstring+"@"+
            //    //      sqlTemp.FieldByName("dbserver").asstring
            //    //      else sAliasm=sqlTemp.FieldByName("dbname").asstring;
            //    //      sAliasm=sAliasm+":";
            //    //      month_=formatfloat("00",sqlTemp.fieldbyname("month_").asinteger);
            //    //      st_bar_progress.Panels[0].Text="Выборка перерасчета за "+month_+"."+
            //    //      sqlTemp.fieldbyname("year_").asstring+"г...";
            //    //      Application.ProcessMessages;
            //    //      if stop_flag=true then exit;

            //    //      if is_kzn then
            //    //sqlAdres=" insert into t_pere1 "+
            //    //                  " select b.nzp_kvar,b.num_ls,a.nzp_serv,nzp_supp,tarif,tarif as tarif_f,nzp_frm,a.is_device,isdel,'01."+month_+"."+
            //    //                   sqlTemp.fieldbyname("year_").asstring+
            //    //                  "',sum(0) as sum_subsidy"+
            //    //                  " ,sum(0) as sum_subsidy_p,"+
            //    //                  " sum(0) as sum_tarif_sn_f_p,"+
            //    //                  " sum(0) as sum_tarif_sn, "+
            //    //                  " sum(sum_tarif) as sum_tarif_eot, "+
            //    //                  " sum(sum_lgota) as sum_lgota, "+
            //    //                  " sum(sum_lgota_p) as sum_lgota_p, "+
            //    //                  " sum(0) as sum_smo, "+
            //    //                  " sum(0) as sum_smo_p, "+
            //    //                  " sum(sum_tarif_p) as sum_tarif_eot_p "+
            //    //                  " from "+ currPref +DBManager.sAliasm+"charge_"+month_+" a, t_adres b "+
            //    //                  " where a.num_ls=b.num_ls and a.nzp_serv>1 "+
            //    //                  " and dat_charge='28."+dats.Substring(4,7)+"' group by 1,2,3,4,5,6,7,8,9"
            //    //      else
            //    //sqlAdres=" insert into t_pere1 "+
            //    //                  " select b.nzp_kvar,b.num_ls,a.nzp_serv,nzp_supp,tarif,tarif_f,nzp_frm,a.is_device,isdel,'01."+month_+"."+
            //    //                   sqlTemp.fieldbyname("year_").asstring+
            //    //                  "',sum(sum_subsidy) as sum_subsidy"+
            //    //                  " ,sum(sum_subsidy_p) as sum_subsidy_p,"+
            //    //                  " sum(sum_tarif_sn_f_p) as sum_tarif_sn_f_p,"+
            //    //                  " sum(sum_tarif_sn_f) as sum_tarif_sn, sum(sum_tarif_eot) as sum_tarif_eot, "+
            //    //                  " sum(sum_lgota) as sum_lgota, sum(sum_lgota_p) as sum_lgota_p, "+
            //    //                  " sum(sum_smo) as sum_smo, sum(sum_smo_p) as sum_smo_p, "+
            //    //                  " sum(sum_tarif_eot_p) as sum_tarif_eot_p "+
            //    //                  " from "+ currPref +DBManager.sAliasm+"charge_"+month_+" a, t_adres b "+
            //    //                  " where a.num_ls=b.num_ls and a.nzp_serv>1 "+
            //    //                  " and dat_charge='28."+dats.Substring(4,7)+"' group by 1,2,3,4,5,6,7,8,9";
            //    //      ExecSQL(sqlAdres);
            //    //      if (strtoint(dats.Substring(4,2))<5) and (strtoint(dats.Substring(7,4))<=2003) then
            //    //      begin

            //    //                     //выделяем перерасчитаные счета (c учетом выбранной группы)

            //    //{                sqlAdres="select r.nzp_kvar,r.nzp_supp,r.nzp_serv,r.nzp_lgota, "
            //    //                         +" r.dat_charge as dat_charge1, "
            //    //                         +" c.dat_charge as dat_charge2, "
            //    //                         +" min(r.dat_charge-nvl(c.dat_charge,DATE('01.01.1999'))) as delta "+"into temp tmp_rdatcl"+
            //    //                         +" from "
            //    //                         +sAliasm+"lgcharge_"+month_+" r, "
            //    //                         +sAliasm+"lgcharge_"+month_+" c "
            //    //                         +" where r.dat_charge='28."+dats.Substring(4,7)+"' and "
            //    //                         +" r.nzp_serv=c.nzp_serv and r.nzp_supp=c.nzp_supp "
            //    //                         +"and  c.nzp_kvar=r.nzp_kvar  and c.nzp_kvar in (select nzp_kvar from "
            //    //                         +" t_adres) and c.nzp_serv>1 and c.nzp_lgota=r.nzp_lgota "
            //    //                         +" group by 1,2,3,4,5,6 "
            //    //                         +" having min(r.dat_charge-nvl(c.dat_charge,DATE('01.01.1999')))>0 ";
            //    //                ExecSQL(sqlAdres);


            //    //достаем текущий перерасчет со знаком "+"

            //    //{
            //    //    sqlAdres =
            //    //             " SELECT c.nzp_kvar,c.nzp_serv,c.nzp_supp,c.nzp_lgota,c.dat_charge, SUM(sum_lgota) sum_lgota" + "into temp tmp_lgcharge1" +
            //    //             "  FROM " + currPref + DBManager.sAliasm + "lgcharge_" + month_ + " c,  tmp_rdatcl k " +
            //    //             " WHERE  k.nzp_kvar=c.nzp_kvar" +
            //    //             "   AND k.nzp_serv=c.nzp_serv" +
            //    //             "   AND k.nzp_supp=c.nzp_supp" +
            //    //             "   AND k.nzp_lgota=c.nzp_lgota" +
            //    //             "   AND nvl(c.dat_charge,Date('01.01.1999'))=k.dat_charge1" +
            //    //             " GROUP BY 1,2,3,4,5";
            //        //          ExecSQL(sqlAdres);


            //        //                      //достаем последний перерасчет/расчет со знаком "-"

            //        //{        sqlAdres="INSERT INTO tmp_lgcharge1(nzp_kvar,nzp_serv,nzp_supp,nzp_lgota,dat_charge,sum_lgota)"+
            //        //                  " SELECT c.nzp_kvar,c.nzp_serv,c.nzp_supp, c.nzp_lgota, c.dat_charge, -SUM(sum_lgota) sum_lgota"+
            //        //                  " FROM " + currPref +DBManager.sAliasm+"lgcharge_"+month_+" c,  tmp_rdatcl k "+
            //        //                  " WHERE k.nzp_kvar=c.nzp_kvar"+
            //        //                  "   AND k.nzp_serv=c.nzp_serv"+
            //        //                  "   AND k.nzp_supp=c.nzp_supp"+
            //        //                   "   AND k.nzp_lgota=c.nzp_lgota"+
            //        //                  "   AND nvl(c.dat_charge,"01.01.1999")=nvl(k.dat_charge2,DATE("01.01.1999"))"+
            //        //                  " GROUP BY 1,2,3,4,5 ";
            //        //               ExecSQL(sqlAdres);
            //        //               sqlAdres="DROP TABLE tmp_rdatcl";
            //        //               ExecSQL(sqlAdres);
            //        //{               sqlAdres=" insert into tmp_lgcharge2(nzp_kvar,nzp_serv, nzp_supp  ,"+
            //        //                   " nzp_gilec , is_family , nzp_lgota , nzp_bud,nzp_law,cz_law, delta_lgota ,dat_charge) "+
            //        //                   " select a.nzp_kvar,nzp_serv,nzp_supp,0,0,a.nzp_lgota, "+
            //        //                   " 0,0,0,  "+
            //        //                   " sum(sum_lgota),"28."+month_+"."+sqlTemp.fieldbyname("year_").asstring+"""+
            //        //                   " from tmp_lgcharge1 a, s_lgota s "+
            //        //                   " where a.nzp_lgota=s.nzp_lgota  group by 1,2,3,4,5,6,7,8,9 ";
            //        //               ExecSQL(sqlAdres);}
            //        // {              sqlAdres="DROP TABLE tmp_lgcharge1";
            //        //               ExecSQL(sqlAdres);}

            //        //      end
            //        //      else
            //        //      begin
            //        //sqlAdres=" insert into tmp_lgcharge2(nzp_kvar,nzp_serv, nzp_supp  ,"+
            //        //             " nzp_gilec , is_family , nzp_lgota , nzp_bud,nzp_law,cz_law,cz_lgota,"+
            //        //             "  delta_lgota ,dat_charge) "+
            //        //             " select a.nzp_kvar,nzp_serv,nzp_supp,nzp_gilec,is_family,a.nzp_lgota, "+
            //        //             " nzp_bud,a.nzp_law,sl.kod_cz as cz_law,s.kod_cz as cz_lgota,  "+
            //        //             " sum(sum_lgota)-sum(sum_lgota_p),"28."+month_+"."+
            //        //                   sqlTemp.fieldbyname("year_").asstring+"""+
            //        //             " from "+ currPref +sAliasm+"lgcharge_"+month_+" a,  t_adres b,s_law sl, s_lgota s"+
            //        //             " where a.nzp_kvar=b.nzp_kvar  and a.nzp_law=sl.nzp_law and a.nzp_lgota=s.nzp_lgota"+
            //        //             " and dat_charge="28."+dats.Substring(4,7)+"" group by 1,2,3,4,5,6,7,8,9,10";
            //        //ExecSQL(sqlAdres);
            //        //      end;

            //        //      sqlTemp.Next;
            //        //    end;
            //        //    sqlTemp.close;
            //        //    st_bar_progress.Panels[0].Text="Группировка перерасчетов... ";
            //        //    Application.ProcessMessages;
            //        //    if stop_flag=true then exit;

            //    #endregion */

            //       /* #region Для нижнекамска заменяем услугу

            //        // Для нижнекамска заменяем услугу
            //        sqlTemp = "DROP TABLE t_pere1";
            //        ret = ExecSQL(conn_db, sqlTemp, true);
            //        sqlTemp = 
            //                    " SELECT * " +
            //                    " FROM t_pere1 a" +
            //                    " WHERE nzp_serv in (266, 284, 285) ";
            //        ret = ExecSQL(conn_db, sqlTemp, true);
            //        if (!ret.result)
            //        {
            //            return ret;
            //        }

            //        //sqlTemp.open;
            //        //  if not sqlTemp.isEmpty then begin
            //        //    sqlTemp.close;
            //        sqlTemp = "DROP TABLE t_cor266";
            //        ret = ExecSQL(conn_db, sqlTemp, true);
            //        sqlTemp = 
            //                    " SELECT nzp_kvar, dat_uchet, isdel, max(nzp_frm) as nzp_frm, max(nzp_supp) as nzp_supp " + 
            //                    " INTO TEMP t_cor266" +
            //                    " FROM t_pere1 a" +
            //                    " WHERE nzp_serv = 16 " +
            //                    " GROUP BY 1,2,3";
            //        ret = ExecSQL(conn_db, sqlTemp, true);
            //        if (!ret.result)
            //        {
            //            return ret;
            //        }


            //        sqlTemp = "DROP TABLE t_pere1";
            //        ret = ExecSQL(conn_db, sqlTemp, true);
            //        sqlTemp = 
            //                    " UPDATE t_pere1 " +
            //                    " SET (nzp_supp, nzp_frm, nzp_serv) = ((SELECT nzp_supp, nzp_frm, 16  " +
            //                    " FROM t_cor266 b " +
            //                    " WHERE t_pere1.nzp_kvar = b.nzp_kvar " +
            //                    " AND t_pere1.dat_uchet = b.dat_uchet " +
            //                    " AND t_pere1.isdel = b.isdel)) " +
            //                    " WHERE nzp_serv IN (266, 284, 285) ";
            //        ret = ExecSQL(conn_db, sqlTemp, true);
            //        if (!ret.result)
            //        {
            //            return ret;
            //        }




            //        sqlTemp = "DROP TABLE t_cor266";
            //        ret = ExecSQL(conn_db, sqlTemp, true);
            //        sqlTemp = 
            //                    " SELECT nzp_kvar, num_ls, a.nzp_serv, a.nzp_frm, nzp_supp, " +
            //                    " isdel, dat_uchet, a.is_device, sum(tarif) as tarif, sum(tarif_f) as tarif_f,  " +
            //                    " sum(sum_subsidy) as sum_subsidy, " +
            //                    " sum(sum_subsidy_p) as sum_subsidy_p, " +
            //                    " sum(sum_tarif_sn_f) as sum_tarif_sn_f, " +
            //                    " sum(sum_tarif_sn_f_p) as sum_tarif_sn_f_p," +
            //                    " sum(sum_tarif_eot) as sum_tarif_eot, " +
            //                    " sum(sum_lgota) as sum_lgota, " +
            //                    " sum(sum_lgota_p) as sum_lgota_p, " +
            //                    " sum(sum_smo) as sum_smo, " +
            //                    " sum(sum_smo_p) as sum_smo_p, " +
            //                    " sum(sum_tarif_eot_p) as sum_tarif_eot_p " + "into temp t_cor266" +
            //                    " FROM t_pere1 a " +
            //                    " WHERE a.nzp_serv = 16  " +
            //                    " GROUP BY 1,2,3,4,5,6,7,8";
            //        ret = ExecSQL(conn_db, sqlTemp, true);
            //        if (!ret.result)
            //        {
            //            return ret;
            //        }



            //        sqlTemp = " DELETE FROM  t_pere1 " +
            //                  " WHERE nzp_serv = 16 ";
            //        ret = ExecSQL(conn_db, sqlTemp, true);
            //        if (!ret.result)
            //        {
            //            return ret;
            //        }


            //        sqlTemp = "DROP TABLE t_pere1";
            //        ret = ExecSQL(conn_db, sqlTemp, true);

            //        sqlTemp = 
            //                    " INSERT INTO t_pere1(nzp_kvar, num_ls, nzp_serv, nzp_frm, nzp_supp, " +
            //                    " isdel, dat_uchet, is_device, tarif, tarif_f,  " +
            //                    " sum_subsidy, sum_subsidy_p, sum_tarif_sn_f, " +
            //                    " sum_tarif_sn_f_p, sum_tarif_eot, sum_lgota, " +
            //                    " sum_lgota_p, sum_smo, sum_smo_p, sum_tarif_eot_p ) " +

            //                    " SELECT nzp_kvar, num_ls, nzp_serv, nzp_frm, nzp_supp, " +
            //                    " isdel, dat_uchet, is_device, tarif, tarif_f,  " +
            //                    " sum_subsidy, sum_subsidy_p, sum_tarif_sn_f, sum_tarif_sn_f_p," +
            //                    " sum_tarif_eot, sum_lgota, sum_lgota_p, sum_smo, " +
            //                    " sum_smo_p, sum_tarif_eot_p " +
            //                    " FROM t_cor266 ";
            //        ret = ExecSQL(conn_db, sqlTemp, true);
            //        if (!ret.result)
            //        {
            //            return ret;
            //        }


            //        sqlTemp = " DROP TABLE t_cor266 ";
            //        ret = ExecSQL(conn_db, sqlTemp, true);
            //        if (!ret.result)
            //        {
            //            return ret;
            //        }


            //        //  end
            //        //  else sqlTemp.close;



            //        sqlTemp = "DROP TABLE  t_pere";
            //        ret = ExecSQL(conn_db, sqlTemp, true);
            //        sqlTemp = 
            //                    " INSERT INTO t_pere (nzp_kvar, num_ls, nzp_serv, nzp_measure, nzp_supp, tarif, tarif_f, isdel," +
            //                    " dat_uchet, sum_subsidy, sum_subsidy_p, sum_tarif_sn_f, sum_tarif_sn_f_p," +
            //                    " sum_tarif_eot, sum_lgota, sum_lgota_p, sum_smo, sum_smo_p, sum_tarif_eot_p, is_device) " +
            //                    " SELECT a.nzp_kvar, a.num_ls, a.nzp_serv, nvl(b.nzp_measure,7) as nzp_measure, a.nzp_supp, a.tarif, a.tarif_f, a.isdel, a.dat_uchet," +
            //                    " sum_subsidy, sum_subsidy_p, sum_tarif_sn_f, sum_tarif_sn_f_p," +
            //                    " a.sum_tarif_eot, a.sum_lgota, a.sum_lgota_p, a.sum_smo, a.sum_smo_p, a.sum_tarif_eot_p, a.is_device " +
            //                    " FROM t_pere1 a, outer formuls b, sz_serv c" +
            //                    " WHERE a.nzp_serv = c.nzp_serv " +
            //                    " AND a.nzp_frm = b.nzp_frm ";
            //        ret = ExecSQL(conn_db, sqlTemp, true);
            //        if (!ret.result)
            //        {
            //            return ret;
            //        }


            //        sqlTemp = "DROP TABLE t_pere1";
            //        ret = ExecSQL(conn_db, sqlTemp, true);
            //        if (!ret.result)
            //        {
            //            return ret;
            //        }

            //        sqlTemp = " CREATE INDEX ix_tm_03 ON t_pere(num_ls, nzp_serv, nzp_supp)";
            //        ret = ExecSQL(conn_db, sqlTemp, true);
            //        if (!ret.result)
            //        {
            //            return ret;
            //        }

            //        sqlTemp = " UPDATE statistics FOR TABLE t_pere";
            //        ret = ExecSQL(conn_db, sqlTemp, true);
            //        if (!ret.result)
            //        {
            //            return ret;
            //        }



            //        sqlTemp = 
            //                    " CREATE INDEX ix_tm_114 ON tmp_lgcharge2(nzp_kvar,nzp_serv," +
            //                    " nzp_supp, nzp_gilec, is_family, nzp_lgota, nzp_bud, nzp_law)";
            //        ret = ExecSQL(conn_db, sqlTemp, true);
            //        if (!ret.result)
            //        {
            //            return ret;
            //        }

            //        //except
            //        //on E=Exception do begin
            //        //messagedlg("Ошибка выборки перерасчетов."+#13#10+
            //        //"Подробности смотри в файле Error.log",mtwarning,[mbOk],0);
            //        //CurT_Start.WriteToError(e.message,true);
            //        //Errortype=3;
            //        //exit;
            //        //end;
            //        //end;
            //        //try
            //        //sqlTemp.close;
            //        ////Отрубаем сумму прерасчитанной льготы (sum_lgota>0)

            //        sqlTemp = "DROP TABLE t_selflgot";
            //        ret = ExecSQL(conn_db, sqlTemp, true);

            //        sqlTemp = 
            //                    " SELECT a.nzp_kvar, nzp_serv, nzp_supp, a.nzp_lgota, s.kod_cz, a.nzp_gilec," +
            //                    " is_family, nzp_bud, sl.kod_cz as nzp_law, a.nzp_law as km_law," +
            //                    " sum(sum_lgota) as sum_lgota, sum(sum_lgota_p) as sum_lgota_p" + 
            //                    " INTO TEMP t_selflgot" +
            //                    " FROM " + currPref + sChargeAlias + "lgcharge_" + dats.Substring(4, 2) + " a, t_adres b, s_lgota s, s_law sl " +
            //                    " WHERE a.nzp_kvar = b.nzp_kvar " +
            //                    " AND dat_charge IS NULL " +
            //                    " AND a.nzp_lgota = s.nzp_lgota " +
            //                    " AND a.nzp_law = sl.nzp_law" +
            //                    " AND s.kod_cz IS NOT NULL " +
            //                    " AND sl.kod_cz IS NOT NULL " +
            //                    " GROUP BY 1,2,3,4,5,6,7,8,9,10";
            //        ret = ExecSQL(conn_db, sqlTemp, true);
            //        if (!ret.result)
            //        {
            //            return ret;
            //        }

            //        //st_bar_progress.Panels[0].Text="Выборка льготников... ";
            //        //Application.ProcessMessages;
            //        //ExecSQL(sqlTemp);
            //        //Application.ProcessMessages;
            //        ////Старые льготы

            //        sqlTemp = "DROP TABLE t_badlgot";
            //        ret = ExecSQL(conn_db, sqlTemp, true);
            //        sqlTemp = 
            //                    " SELECT a.nzp_kvar, nzp_serv, nzp_supp, " +
            //                    " sum(sum_lgota) as sum_lgota, sum(sum_lgota_p) as sum_lgota_p" + 
            //                    " INTO TEMP t_badlgot" +
            //                    " FROM " + currPref + sChargeAlias + "lgcharge_" + dats.Substring(4, 2) + " a, t_adres b, s_lgota s " +
            //                    " WHERE a.nzp_kvar = b.nzp_kvar " +
            //                    " AND dat_charge IS NULL " +
            //                    " AND a.nzp_lgota = s.nzp_lgota " +
            //                    " AND s.kod_cz IS NULL  " +
            //                    " GROUP BY 1,2,3";
            //        ret = ExecSQL(conn_db, sqlTemp, true);
            //        if (!ret.result)
            //        {
            //            return ret;
            //        }

            //        //st_bar_progress.Panels[0].Text = "Выборка льготников... ";
            //        //Application.ProcessMessages;
            //        //ExecSQL(sqlTemp);


            //        //sqlTemp=" select count(*)  as co from t_badlgot ";
            //        //sqlTemp.Open;
            //        //if sqlTemp.fieldbyname("co").asInteger > 0 then hasbadLgot=true
            //        //else hasbadLgot=false;
            //        //sqlTemp.Close;



            //        // if (strtoint(dats.Substring(4,2))<5) and (strtoint(dats.Substring(7,5))<=2003) then
            //        //    begin
            //        sqlTemp = "DROP TABLE t_selflgot";
            //        ret = ExecSQL(conn_db, sqlTemp, true);
            //        sqlTemp = 
            //                    " INSERT INTO t_selflgot (nzp_kvar, nzp_serv, nzp_supp, nzp_lgota, kod_cz, nzp_gilec," +
            //                    " is_family, nzp_bud, nzp_law, km_law, sum_lgota, sum_lgota_p) " +
            //                    " SELECT nzp_kvar, nzp_serv, nzp_supp, nzp_lgota, kod_cz, 0, 0, 0, 0, 0, sum(delta_lgota), 0 " +
            //                    " FROM tmp_lgcharge2 " +
            //                    " GROUP BY 1,2,3,4,5 " +
            //                    " HAVING sum(delta_lgota) <> 0 ";
            //        ret = ExecSQL(conn_db, sqlTemp, true);
            //        if (!ret.result)
            //        {
            //            return ret;
            //        }


            //        //    getreallg;
            //        sqlTemp = "DROP TABLE t_selflgot1";
            //        ret = ExecSQL(conn_db, sqlTemp, true);
            //        sqlTemp = 
            //                    " SELECT nzp_kvar, nzp_serv, nzp_supp, nzp_lgota, kod_cz, nzp_gilec," +
            //                    " is_family, nzp_bud, nzp_law, km_law," +
            //                    " sum(sum_lgota) as sum_lgota, sum(sum_lgota_p) as sum_lgota_p" + 
            //                    " INTO TEMP t_selflgot1" +
            //                    " FROM t_selflgot " +
            //                    " GROUP BY 1,2,3,4,5,6,7,8,9,10";
            //        ret = ExecSQL(conn_db, sqlTemp, true);
            //        if (!ret.result)
            //        {
            //            return ret;
            //        }


            //        sqlTemp = "DROP TABLE t_selflgot";
            //        ret = ExecSQL(conn_db, sqlTemp, true);
            //        sqlTemp = 
            //                " SELECT *" + 
            //                " FROM t_selflgot1 into temp t_selflgot ";
            //        ret = ExecSQL(conn_db, sqlTemp, true);
            //        if (!ret.result)
            //        {
            //            return ret;
            //        }


            //        //    end;


            //        //if stop_flag=true then exit;


            //        //  if is_serv266 then begin
            //        //Основные льготы

            //        sqlTemp = "DROP TABLE t_cor266l";
            //        ret = ExecSQL(conn_db, sqlTemp, true);
            //        sqlTemp = 
            //                  " SELECT nzp_kvar, max(nzp_supp) as nzp_supp" + 
            //                  " INTO TEMP t_cor266l" +
            //                  " FROM t_selflgot " +
            //                  " WHERE nzp_serv = 16 " +
            //                  " GROUP BY 1";
            //        ret = ExecSQL(conn_db, sqlTemp, true);
            //        if (!ret.result)
            //        {
            //            return ret;
            //        }

            //        sqlTemp = 
            //                  " UPDATE t_selflgot SET (nzp_serv, nzp_supp) = ((SELECT 16, nzp_supp" +
            //                  " FROM t_cor266l a " +
            //                  " WHERE t_selflgot.nzp_kvar = a.nzp_kvar)) " +
            //                  " WHERE nzp_serv IN (266, 284, 285) ";
            //        ret = ExecSQL(conn_db, sqlTemp, true);
            //        if (!ret.result)
            //        {
            //            return ret;
            //        }

            //        sqlTemp = " DROP TABLE t_cor266l";
            //        ret = ExecSQL(conn_db, sqlTemp, true);
            //        if (!ret.result)
            //        {
            //            return ret;
            //        }

            //        sqlTemp = 
            //                  " SELECT nzp_kvar, nzp_serv, nzp_supp, nzp_lgota, kod_cz, nzp_gilec," +
            //                  " is_family, nzp_bud, nzp_law, km_law," +
            //                  " sum(sum_lgota) as sum_lgota, sum(sum_lgota_p) as sum_lgota_p" + 
            //                  " INTO TEMP t_cor266l" +
            //                  " FROM t_selflgot " +
            //                  " WHERE nzp_serv = 16 " +
            //                  " GROUP BY 1,2,3,4,5,6,7,8,9,10";
            //        ret = ExecSQL(conn_db, sqlTemp, true);
            //        if (!ret.result)
            //        {
            //            return ret;
            //        }
            //        sqlTemp = 
            //                  " DELETE " +
            //                  " FROM t_selflgot " +
            //                  " WHERE nzp_serv = 16 ";
            //        ret = ExecSQL(conn_db, sqlTemp, true);
            //        if (!ret.result)
            //        {
            //            return ret;
            //        }
            //        sqlTemp = 
            //                  " INSERT INTO t_selflgot (nzp_kvar, nzp_serv, nzp_supp, " +
            //                  " nzp_lgota, kod_cz, nzp_gilec," +
            //                  " is_family, nzp_bud, nzp_law, km_law, sum_lgota, sum_lgota_p) " +
            //                  " SELECT nzp_kvar, nzp_serv, nzp_supp, nzp_lgota, kod_cz, nzp_gilec," +
            //                  " is_family, nzp_bud, nzp_law, km_law," +
            //                  " sum_lgota, sum_lgota_p" +
            //                  " FROM t_cor266l ";
            //        ret = ExecSQL(conn_db, sqlTemp, true);
            //        if (!ret.result)
            //        {
            //            return ret;
            //        }
            //        sqlTemp = " DROP TABLE t_cor266l";
            //        ret = ExecSQL(conn_db, sqlTemp, true);
            //        if (!ret.result)
            //        {
            //            return ret;
            //        }

            //        //Перерасчеты льгот

            //        sqlTemp = 
            //                  " SELECT nzp_kvar, max(nzp_supp) as nzp_supp" + 
            //                  " INTO TEMP t_cor266l" +
            //                  " FROM tmp_lgcharge2 " +
            //                  " WHERE nzp_serv = 16 " +
            //                  " GROUP BY 1";
            //        ret = ExecSQL(conn_db, sqlTemp, true);
            //        if (!ret.result)
            //        {
            //            return ret;
            //        }

            //        sqlTemp = 
            //                  " UPDATE tmp_lgcharge2 " +
            //                  " SET (nzp_serv, nzp_supp) = ((SELECT 16, nzp_supp" +
            //                  " FROM t_cor266l a " +
            //                  " WHERE tmp_lgcharge2.nzp_kvar = a.nzp_kvar)) " +
            //                  " WHERE nzp_serv IN (266, 284, 285) ";
            //        ret = ExecSQL(conn_db, sqlTemp, true);
            //        if (!ret.result)
            //        {
            //            return ret;
            //        }

            //        sqlTemp = " DROP TABLE t_cor266l";
            //        ret = ExecSQL(conn_db, sqlTemp, true);
            //        if (!ret.result)
            //        {
            //            return ret;
            //        }

            //        sqlTemp = 
            //                  " SELECT nzp_kvar, nzp_serv, nzp_supp  ," +
            //                  " nzp_gilec , is_family , nzp_lgota , nzp_bud, nzp_law, cz_law, cz_lgota," +
            //                  " dat_charge, sum(delta_lgota) as delta_lgota " + "into temp t_cor266l" +
            //                  " FROM tmp_lgcharge2 " +
            //                  " WHERE nzp_serv = 16 " +
            //                  " GROUP BY 1,2,3,4,5,6,7,8,9,10,11";
            //        ret = ExecSQL(conn_db, sqlTemp, true);
            //        if (!ret.result)
            //        {
            //            return ret;
            //        }

            //        sqlTemp = " DELETE " +
            //                  " FROM tmp_lgcharge2 " +
            //                  " WHERE nzp_serv = 16 ";
            //        ret = ExecSQL(conn_db, sqlTemp, true);
            //        if (!ret.result)
            //        {
            //            return ret;
            //        }

            //        sqlTemp = 
            //                  " INSERT INTO tmp_lgcharge2 (nzp_kvar, nzp_serv, nzp_supp  ," +
            //                  " nzp_gilec , is_family , nzp_lgota , nzp_bud, nzp_law, cz_law, cz_lgota," +
            //                  " dat_charge, delta_lgota) " +
            //                  " SELECT nzp_kvar, nzp_serv, nzp_supp  ," +
            //                  " nzp_gilec , is_family , nzp_lgota , nzp_bud, nzp_law, cz_law, cz_lgota," +
            //                  " dat_charge, delta_lgota" +
            //                  " FROM t_cor266l ";
            //        ret = ExecSQL(conn_db, sqlTemp, true);
            //        if (!ret.result)
            //        {
            //            return ret;
            //        }

            //        sqlTemp = " DROP TABLE t_cor266l";
            //        ret = ExecSQL(conn_db, sqlTemp, true);
            //        if (!ret.result)
            //        {
            //            return ret;
            //        }

            //        sqlTemp = " CREATE INDEX ix_tm_04 ON t_selflgot(nzp_kvar, nzp_serv, nzp_supp)";
            //        ret = ExecSQL(conn_db, sqlTemp, true);
            //        if (!ret.result)
            //        {
            //            return ret;
            //        }

            //        sqlTemp = " CREATE INDEX ix_tm_041 ON t_badlgot(nzp_kvar, nzp_serv, nzp_supp)";
            //        ret = ExecSQL(conn_db, sqlTemp, true);
            //        if (!ret.result)
            //        {
            //            return ret;
            //        }

            //        //except
            //        //on E=Exception do begin messagedlg("Ошибка выборки льготников."+#13#10+
            //        //"Подробности смотри в файле Error.log",mtwarning,[mbOk],0);
            //        //CurT_Start.WriteToError(e.message,true);
            //        //Errortype=4;
            //        //exit;
            //        //end;
            //        //end;
            //        //  GetErrTable;
            //    */
            //        #endregion 

            //    #region Оптимизация выборки начислений

            //        // Оптимизация выборки начислений
            //        // try
            //        sqlTemp = " DROP TABLE tt_charge";
            //        ret = ExecSQL(conn_db, sqlTemp, false);
            //        if (!ret.result)
            //        {
            //            return ret;
            //        }
            //        // except

            //        // end;
            //        //// timelist.add(formatdatetime("hh:nn:ss.zzz", now)+ " загрузка начислений 2");
            //        // if is_kzn then функция для Казани

            //        //sqlTemp = 
            //        //            " SELECT num_ls, a.nzp_serv, a.nzp_supp, a.sum_insaldo, a.tarif, a.tarif as tarif_f, 7 as nzp_measure, nzp_frm, " +
            //        //            " a.nzp_frm, a.sum_dlt_tarif_p as sum_tarif_sn_f, a.sum_tarif as sum_tarif_eot," +
            //        //            " 0 as sum_subsidy, a.sum_lgota,0 as sum_smo,0 as reval_lgota," +
            //        //            " sum_money,a.is_device,isdel,c_sn, 0 as has_reval " +
            //        //            " FROM " + finder.bank + sChargeAlias + ".charge_" + dats.Substring(3, 2) + " a, " + finder.bank + sKernelAlias + "sz_serv c" + // необходимо выбирать нижний и верхний банк
            //        //            " WHERE nzp_supp>-999 " +
            //        //            " AND dat_charge IS NULL " +
            //        //            " AND a.nzp_serv = c.nzp_serv ";
            //        //ret = ExecSQL(conn_db, sqlTemp, true);
            //        //if (!ret.result)
            //        //{
            //        //    return ret;
            //        //} функция для Казани
            //        //    else begin
            //        sqlTemp = 
            //                    " SELECT num_ls, a.nzp_serv, a.nzp_supp, 7 as nzp_measure, nzp_frm,  a.isdel, a.is_device, " +
            //                    " a.tarif, a.tarif_f, a.sum_insaldo, " +
            //                    " sum_tarif_sn_f, sum_tarif_eot, sum_subsidy, sum_lgota, sum_smo, reval_lgota," +
            //                    " sum_money, c_sn, 0 as has_reval INTO TEMP tt_charge " +
            //                    " FROM " + finder.bank + sChargeAlias + ".charge_" + dats.Substring(3, 2) + " a, " + finder.bank + DBManager.sKernelAliasRest + "sz_serv c" +
            //                    " WHERE nzp_supp >-999 " +
            //                    " AND dat_charge IS NULL " +
            //                    " AND a.nzp_serv = c.nzp_serv";
            //        ret = ExecSQL(conn_db, sqlTemp, true);
            //        if (!ret.result)
            //        {
            //            return ret;
            //        }

            //        //      if is_serv266  then
            //        //         sqlTemp=sqlTemp+" and a.nzp_serv not in (16, 266, 284, 285) "+
            //        //                       " union all "+
            //        //                       " select num_ls, a.nzp_serv,"+
            //        //                       " a.nzp_supp, "+
            //        //                       " 7 as nzp_measure, max(nzp_frm) as nzp_frm, "+
            //        //                       " min(a.isdel) as isdel,"+
            //        //                       " max(a.is_device) as is_device, "+
            //        //                       " sum(tarif) as tarif, "+
            //        //                       " sum(tarif_f) as tarif_f, "+
            //        //                       " sum(a.sum_insaldo) as sum_insaldo,"+
            //        //                       " sum(a.sum_tarif_sn_f) as sum_tarif_sn_f, "+
            //        //                       " sum(a.sum_tarif_eot) as sum_tarif_eot, "+
            //        //                       " sum(a.sum_subsidy) as sum_subsidy, "+
            //        //                       " sum(a.sum_lgota) as sum_lgota, "+
            //        //                       " sum(a.sum_smo) as sum_smo, "+
            //        //                       " sum(a.reval_lgota) as reval_lgota,"+
            //        //                       " sum(a.sum_money) as sum_money, "+
            //        //                       " sum(a.c_sn) as c_sn, sum(0) as has_reval "+
            //        //                       " from t_charge266 a, sz_serv c"+
            //        //                       " where a.nzp_serv=c.nzp_serv"+
            //        //                       " group by 1,2,3,4 ";
            //        //      end;
            //        //sqlTemp = sqlTemp + " INTO TEMP tt_charge"; для informix
            //        //ret = ExecSQL(conn_db, sqlTemp, true);
            //        //if (!ret.result)
            //        //{
            //        //    return ret;
            //        //}

            //        sqlTemp = 
            //                  " UPDATE tt_charge " +
            //                  " SET nzp_measure = (SELECT max(nzp_measure) FROM " + finder.bank + DBManager.sKernelAliasRest + "formuls " +
            //                  "  WHERE tt_charge.nzp_frm = formuls.nzp_frm) " +
            //                  " WHERE nzp_frm IN (SELECT nzp_frm FROM " + finder.bank + DBManager.sKernelAliasRest + " formuls) ";
            //        ret = ExecSQL(conn_db, sqlTemp, true);
            //        if (!ret.result)
            //        {
            //            return ret;
            //        }
            //        sqlTemp = " CREATE INDEX ixttt_ch01 ON tt_charge(num_ls)";
            //        ret = ExecSQL(conn_db, sqlTemp, true);
            //        if (!ret.result)
            //        {
            //            return ret;
            //        }
            //        //sqlTemp = " UPDATE statistics FOR TABLE tt_charge"; informix
            //        sqlTemp = " ANALYZE tt_charge";
            //        ret = ExecSQL(conn_db, sqlTemp, true);
            //        if (!ret.result)
            //        {
            //            return ret;
            //        }

            //        sqlTemp = 
            //                  " UPDATE tt_charge SET has_reval = (SELECT max(1) " +
            //                  "  FROM t_pere" +
            //                  "  WHERE tt_charge.num_ls = t_pere.num_ls " +
            //                  "  AND tt_charge.nzp_serv = t_pere.nzp_serv " +
            //                  "  AND tt_charge.nzp_supp = t_pere.nzp_supp) " +
            //                  " WHERE 0 < (SELECT count(*) " +
            //                  "   FROM t_pere " +
            //                  "   WHERE tt_charge.num_ls = t_pere.num_ls  " +
            //                  "   AND tt_charge.nzp_serv = t_pere.nzp_serv " +
            //                  "   AND tt_charge.nzp_supp = t_pere.nzp_supp)";
            //        ret = ExecSQL(conn_db, sqlTemp, true);
            //        if (!ret.result)
            //        {
            //            return ret;
            //        }


            //        //         HasLgotPere=false;
            //        //         sqlTemp=" select sum(sum_lgota) as sum_lgota,sum(sum_lgota_p) as sum_lgota_p "+
            //        //                           " from t_pere ";
            //        //         sqlTemp.open;
            //        //         if not sqlTemp.isEmpty then
            //        //          if (sqlTemp.fieldbyName("sum_lgota").asFloat>0.001 )or(sqlTemp.fieldbyName("sum_lgota_p").asFloat>0.001)
            //        //          then  HasLgotPere=true;
            //        //         sqlTemp.close;

            //        //sqlTemp=" select count(*)  as co from t_selflgot ";
            //        //sqlTemp.Open;
            //        //if sqlTemp.fieldbyname("co").asInteger > 0 then hasSelfLgot=true
            //        //else hasSelfLgot=false;
            //        //sqlTemp.Close;


            //        // // timelist.add(formatdatetime("hh:nn:ss.zzz", now)+ " окончание загрузка начислений");
            //        //end;

            //        //procedure TFrm_exp.write_service;
            //        //var service string;
            //        //    eot,pt string;
            //        //    soc_rash string;
            //        //    fact_rash string;
            //        //    sum_subs Real;
            //        //    sum_subs_r Real;
            //        //begin
            //        ////  timelist.add(formatdatetime("hh:nn:ss.zzz", now)+ " Начало записи услуги "+ Q_charge.fieldbyname("nzp_serv").asString);


            //        //  if (abs(Q_charge.fieldbyname("tarif").asfloat)+
            //        //     abs(Q_charge.fieldbyname("sum_insaldo").asfloat)+
            //        //     abs(Q_charge.fieldbyname("sum_money").asfloat)+
            //        //     abs(Q_charge.fieldbyname("has_reval").asInteger)
            //        //     <0.001)and(Q_charge.fieldbyname("isdel").asInteger=1) then exit;


            //        //  if Q_charge.fieldbyname("tarif").asfloat<Q_charge.fieldbyname("tarif_f").asfloat
            //        //  then begin
            //        //        //bad_file=true;
            //        //        isBadLs=true;
            //        //        CurT_Start.WriteToError("ЭОТ тариф должен быть больше или равен ПТ "
            //        //        +"  Лицевой счет №"+sqlAdres.fieldbyName("num_ls").asstring,true);
            //        //  end;
            //        //  if Q_charge.fieldbyname("tarif_f").asfloat<0
            //        //  then begin
            //        //        //bad_file=true;
            //        //        isBadLs=true;
            //        //        CurT_Start.WriteToError("ПТ тариф должен быть больше или равен 0 "
            //        //        +"  Лицевой счет №"+sqlAdres.fieldbyName("num_ls").asstring,true);
            //        //  end;

            //        //  eot=formatfloat("0.00#",Q_charge.fieldbyname("tarif").asfloat);
            //        //  pt=formatfloat("0.00#",Q_charge.fieldbyname("tarif_f").asfloat);
            //        //  soc_rash=get_soc_ras(0);
            //        //  fact_rash=get_fact_ras(0);
            //        //  sum_subs=Q_charge.fieldbyname("sum_subsidy").asfloat;

            //        //  sum_subs_r=(strtofloat(eot)-strtofloat(pt))*min(strtofloat(soc_rash),strtofloat(fact_rash));
            //        //  if abs(sum_subs-sum_subs_r)>1
            //        //  then begin
            //        //        //bad_file=true;
            //        //        isBadLs=true;
            //        //        CurT_Start.WriteToError("Субсидия по ЛС должна быть (ЭОТ тариф-ПТ тариф)*расход "
            //        //        +"  Лицевой счет №"+sqlAdres.fieldbyName("num_ls").asstring,true);
            //        //  end;

            //        //  if (Q_charge.fieldbyname("sum_lgota").asfloat<0)or(Q_charge.fieldbyname("sum_lgota").asfloat>3000)
            //        //  then begin
            //        //        //bad_file=true;
            //        //        isBadLs=true;
            //        //        CurT_Start.WriteToError("Сумма льготы должна быть больше или равена 0 и менее 3000 "
            //        //        +"  Лицевой счет №"+sqlAdres.fieldbyName("num_ls").asstring,true);
            //        //  end;

            //        //  if Q_charge.fieldbyname("sum_smo").asfloat<0
            //        //  then begin
            //        //        ///bad_file=true;
            //        //        isBadLs=true;
            //        //        CurT_Start.WriteToError("Сумма СМО должна быть больше или равена 0 "
            //        //        +"  Лицевой счет №"+sqlAdres.fieldbyName("num_ls").asstring,true);
            //        //  end;
            //        //  sum_lgota_k=Q_charge.fieldbyname("sum_lgota").asfloat;
            //        //service="4|"+Q_charge.fieldbyname("nzp_supp").asstring+"|"+
            //        //         Q_charge.fieldbyname("nzp_serv").asstring+"|"+
            //        //         formatfloat("0.00",Q_charge.fieldbyname("sum_insaldo").asfloat)+"|"+
            //        //         formatfloat("0.00#",Q_charge.fieldbyname("tarif").asfloat)+"|"+
            //        //         formatfloat("0.00#",Q_charge.fieldbyname("tarif_f").asfloat)+"|"+
            //        //         Q_charge.fieldbyname("nzp_measure").asstring+"|"+
            //        //         fact_rash+"|"+soc_rash+"|"+get_pribor_uch(0)+"|";

            //        //  if (Q_charge.fieldbyname("nzp_serv").asinteger=11)and (Q_charge.fieldbyname("sum_tarif_sn_f").asCurrency<0) then
            //        //  service=service+"0"+"|"
            //        //  else service=service+formatfloat("0.00",Q_charge.fieldbyname("sum_tarif_sn_f").asfloat)+"|";
            //        //           service=service+get_soc_pere+"|"+formatfloat("0.00",sum_subs)+"|"+
            //        //           get_dot_pere+"|";

            //        //  if (Q_charge.fieldbyname("nzp_serv").asinteger=11)and (Q_charge.fieldbyname("sum_lgota").asCurrency<0) then
            //        //  service=service+"0"+"|"
            //        //  else service=service+formatfloat("0.00",Q_charge.fieldbyname("sum_lgota").asfloat)+"|";

            //        //  service=service+get_lgot_pere+"|";

            //        //  if (Q_charge.fieldbyname("nzp_serv").asinteger=11)and (Q_charge.fieldbyname("sum_smo").asCurrency<0) then
            //        //  service=service+"0"+"|"
            //        //  else service=service+formatfloat("0.00",Q_charge.fieldbyname("sum_smo").asfloat)+"|";


            //        //          service=service+get_smo_pere+"|"+
            //        ////Пока не считаем           formatfloat("0.00",Q_charge.fieldbyname("reval_lgota").asfloat)+"|"+
            //        //           formatfloat("0.00",Q_charge.fieldbyname("sum_money").asfloat)+"|"+
            //        //           Q_charge.fieldbyname("isdel").asstring+"|0|0|";
            //        //           service=Replacestr(service,",",".");
            //        ////  writeln(f,strtooem(service));
            //        //  StringDomo=StringDomo+#13#10+strtooem(service);
            //        ////  timelist.add(formatdatetime("hh:nn:ss.zzz", now)+ " Запись перерасчетов "+ Q_charge.fieldbyname("nzp_serv").asString);
            //        //  write_perer;
            //        ////  timelist.add(formatdatetime("hh:nn:ss.zzz", now)+ " Запись льгот "+ Q_charge.fieldbyname("nzp_serv").asString);
            //        //  write_lg;

            //        //  if ((abs(sum_lgota_k)>0.01) or(abs(sum_lgota_pk)>0.01))and
            //        //  (Q_charge.fieldbyname("nzp_supp").asinteger>0) then
            //        //  begin
            //        //        if ((abs(sum_lgota_k)<0.01)and(abs(sum_lgota_pk)>0.01))then  begin end//bad_file:=false
            //        //        else
            //        //        //bad_file=true;
            //        //        isBadLs=true;
            //        //        CurT_Start.WriteToError("Несовпадение сумм льгот по льготникам и по начислениям"+
            //        //        #13#10+" код услуги "+Q_charge.fieldbyname("nzp_serv").asstring+
            //        //        #13#10+" разница по льготам "+formatfloat("0.00",sum_lgota_k)+
            //        //        " разница по перерасчетам "+formatfloat("0.00",sum_lgota_pk)+
            //        //        "  Лицевой счет №"+sqlAdres.fieldbyName("num_ls").asstring+#13#10+
            //        //        " перерассчитайте счет ",true);
            //        //  end;
            //        //  sum_lgota_k=0;
            //        //  sum_lgota_pk=0;
            //        ////  timelist.add(formatdatetime("hh:nn:ss.zzz", now)+ " Окончание записи услуги "+ Q_charge.fieldbyname("nzp_serv").asString);
            //        //end;

            //        //function TFrm_exp.get_pribor_uch(Regim integer) string;
            //        //begin
            //        //  Result="0";
            //        //  if Regim=0 then
            //        //  Result=inttostr(Q_charge.fieldbyname("is_device").asinteger);
            //        //  if Regim=1 then
            //        //  Result=inttostr(sqlTemp.fieldbyname("is_device").asinteger);
            //        //end;

            //        //function TFrm_exp.get_soc_ras(Regim integer) string;
            //        //var Soc_ras real;
            //        //begin
            //        //  Soc_ras=0;
            //        //  if Regim=0 then  begin
            //        //      if ParamCount>1 then begin
            //        //       Soc_Ras=Q_charge.fieldbyname("c_sn").asfloat;
            //        //      end
            //        //      else begin
            //        //      if Q_charge.fieldbyname("tarif_f").asfloat<>0 then
            //        //      Soc_Ras=Q_charge.fieldbyname("sum_tarif_sn_f").asfloat/
            //        //      Q_charge.fieldbyname("tarif_f").asfloat
            //        //      end;
            //        //      if (Soc_ras<0)and (Q_charge.fieldbyname("nzp_serv").asinteger<>11)
            //        //      then begin
            //        //            //bad_file=true;
            //        //            isBadLs=true;
            //        //            CurT_Start.WriteToError("Расход по соцнормативу должен быть больше 0 "+
            //        //            formatfloat("0.######",Soc_ras)+"  Лицевой счет №"+sqlAdres.fieldbyName("num_ls").asstring,true);
            //        //      end;
            //        //  end;
            //        //  if Regim=1 then begin
            //        //      if sqlTemp.fieldbyname("tarif_f").asfloat<>0 then
            //        //      Soc_Ras=sqlTemp.fieldbyname("sum_tarif_sn_f").asfloat/
            //        //      sqlTemp.fieldbyname("tarif_f").asfloat;
            //        //      if (Soc_ras<0)and (sqlTemp.fieldbyname("nzp_serv").asinteger<>11)
            //        //      then begin
            //        //            //bad_file=true;
            //        //            isBadLs=true;
            //        //            CurT_Start.WriteToError("Расход по соцнормативу должен быть больше 0 "+
            //        //            formatfloat("0.######",Soc_ras)+"  Лицевой счет №"+sqlAdres.fieldbyName("num_ls").asstring,true);
            //        //      end;
            //        //  end;
            //        // if Soc_ras<0 then Result=formatfloat("0.######",0)
            //        // else
            //        //  Result=formatfloat("0.######",Soc_Ras);
            //        //end;

            //        //function TFrm_exp.get_soc_pere string;
            //        //begin
            //        //  sqlTemp=" select sum(sum_tarif_sn_f)-sum(sum_tarif_sn_f_p) as sum_tarif "+
            //        //                   " from t_pere "+
            //        //                   " where num_ls="+sqlAdres.fieldbyname("num_ls").asstring+
            //        //                   " and nzp_serv="+Q_charge.fieldbyname("nzp_serv").asstring+
            //        //                   " and nzp_supp="+Q_charge.fieldbyname("nzp_supp").asstring;
            //        //  sqlTemp.open;
            //        //  if sqlTemp.isEmpty then
            //        //   begin
            //        //     Result="0.00";
            //        //     sqlTemp.close;
            //        //   end;
            //        //  Result=formatfloat("0.00",sqlTemp.fieldbyname("sum_tarif").asfloat);
            //        //  sqlTemp.close;
            //        //end;

            //        //function TFrm_exp.get_dot_pere string;
            //        //begin
            //        //  Result="0.00";
            //        //  exit;
            //        //  sqlTemp=" select sum(sum_subsidy)-sum(sum_subsidy_p) as sum_tarif "+
            //        //                   " from t_pere "+
            //        //                   " where num_ls="+sqlAdres.fieldbyname("num_ls").asstring+
            //        //                   " and nzp_serv="+Q_charge.fieldbyname("nzp_serv").asstring+
            //        //                   " and nzp_supp="+Q_charge.fieldbyname("nzp_supp").asstring;
            //        //  sqlTemp.open;
            //        //  if sqlTemp.isEmpty then
            //        //   begin
            //        //     Result="0.00";
            //        //     sqlTemp.close;
            //        //   end;
            //        //  Result=formatfloat("0.00",sqlTemp.fieldbyname("sum_tarif").asfloat);
            //        //  sqlTemp.close;
            //        //end;

            //        //procedure TFrm_exp.write_perer;
            //        //var pere string;
            //        //begin
            //        //sqlTemp=" select nzp_serv,nzp_supp,tarif,tarif_f,dat_uchet,nzp_measure, "+
            //        //                 " sum_tarif_eot,sum_tarif_sn_f-sum_tarif_sn_f_p as sum_tarif_sn, "+
            //        //                 " sum_subsidy-sum_subsidy_p as sum_subsidy, sum_lgota-sum_lgota_p "+
            //        //                 " as sum_lgota,sum_smo-sum_smo_p as sum_smo, sum_tarif_sn_f,is_device,isdel "+
            //        //                 " from t_pere "+
            //        //                 " where num_ls="+sqlAdres.fieldbyname("num_ls").asstring+
            //        //                 " and nzp_serv="+Q_charge.fieldbyname("nzp_serv").asstring+
            //        //                 " and nzp_supp="+Q_charge.fieldbyname("nzp_supp").asstring;
            //        //  sqlTemp.open;
            //        //  if sqlTemp.isEmpty then
            //        //  begin
            //        //    sqlTemp.close;
            //        //    exit;
            //        //  end;
            //        //  while not sqlTemp.Eof do
            //        //  begin
            //        //  if sqlTemp.fieldbyname("tarif").asfloat<sqlTemp.fieldbyname("tarif_f").asfloat
            //        //  then begin
            //        //        //bad_file=true;
            //        //        isBadLs=true;
            //        //        CurT_Start.WriteToError("ЭОТ тариф должен быть больше или равен ПТ "
            //        //        +"  Лицевой счет №"+sqlAdres.fieldbyName("num_ls").asstring,true);
            //        //  end;
            //        //  if sqlTemp.fieldbyname("tarif_f").asfloat<0
            //        //  then begin
            //        //        //bad_file=true;
            //        //        isBadLs=true;
            //        //        CurT_Start.WriteToError("ПТ тариф должен быть больше или равен 0 "
            //        //        +"  Лицевой счет №"+sqlAdres.fieldbyName("num_ls").asstring,true);
            //        //  end;

            //        //  if (sqlTemp.fieldbyname("tarif").asfloat>3000) and (sqlTemp.fieldbyname("nzp_serv").asinteger<>239) then begin
            //        //     //bad_file=true;
            //        //     isBadLs=true;
            //        //     CurT_Start.WriteToError("Тариф должен быть <3000, текущее значение "
            //        //     +formatfloat("0.00#",sqlTemp.fieldbyname("tarif").asfloat)+"  Лицевой счет №"+sqlAdres.fieldbyName("num_ls").asstring,true);
            //        //  end;

            //        //    pere="5|"+sqlTemp.FieldByName("dat_uchet").asstring+"|"+
            //        //    formatfloat("0.00#",sqlTemp.FieldByName("tarif").asFloat)+"|"+
            //        //    formatfloat("0.00#",sqlTemp.FieldByName("tarif_f").asFloat)+"|"+
            //        //    sqlTemp.FieldByName("nzp_measure").asstring+"|"+
            //        //    get_fact_ras(1)+"|"+get_soc_ras(1)+"|"+
            //        //    get_pribor_uch(1)+"|"+
            //        //    formatfloat("0.00",sqlTemp.FieldByName("sum_tarif_sn").asFloat)+"|"+
            //        //    formatfloat("0.00",sqlTemp.FieldByName("sum_subsidy").asFloat)+"|"+
            //        //    formatfloat("0.00",sqlTemp.FieldByName("sum_lgota").asFloat-get_bad_lgotap(sqlTemp.FieldByName("dat_uchet").asstring))+"|"+
            //        //    formatfloat("0.00",sqlTemp.FieldByName("sum_smo").asFloat)+"|"+
            //        //    sqlTemp.FieldByName("isdel").asstring+"|";
            //        //    pere=Replacestr(pere,",",".");
            //        ////    writeln(f,strtooem(pere));
            //        //    StringDomo=StringDomo+#13#10+strtooem(pere);
            //        //    sqlTemp.Next;
            //        //  end;
            //        //  sqlTemp.close;
            //        //end;


            //        //procedure TFrm_exp.write_lg;
            //        //begin
            //        //  {написать выгрузку по льготам}
            //        //  if hasSelfLgot then begin
            //        //Q_pere.sql.text=" select nzp_gilec,nzp_lgota,kod_cz,sum_lgota,sum_lgota_p,nzp_bud"+
            //        //                 ",nzp_law,is_family,km_law "+
            //        //                 " from t_selflgot "+
            //        //                 " where nzp_kvar="+sqlAdres.fieldbyname("nzp_kvar").asstring+
            //        //                 " and nzp_serv="+Q_charge.fieldbyname("nzp_serv").asstring+
            //        //                 " and nzp_supp="+Q_charge.fieldbyname("nzp_supp").asstring;
            //        //      Q_pere.open;
            //        //      if Q_pere.IsEmpty then
            //        //       begin
            //        //         Q_pere.close;
            //        //         exit;
            //        //       end;
            //        //      while not Q_pere.Eof do
            //        //       begin
            //        //        if GetPereGilec(Q_pere.fieldbyname("nzp_gilec").asstring,Q_pere.fieldbyname("sum_lgota").asfloat)<>"" then begin
            //        //          write_lgota;
            //        //          write_lgota_pere;
            //        //        end;
            //        //        Q_pere.Next;
            //        //       end;
            //        //       Q_pere.Close;
            //        //   end;
            //        //end;

            //        //procedure TFrm_exp.write_lgota;
            //        //var lgota string;
            //        //begin
            //        //  if (Q_pere.fieldbyname("nzp_bud").asstring="0")and((CBox_month.ItemIndex+1)+12*spEd_year.Value>24040)  then
            //        //  begin
            //        //     CurT_Start.WriteToError("Неопределенный бюджет в лицевом счете"+
            //        //     sqlAdres.fieldbyname("num_ls").asstring+ " "+
            //        //     " код услуги "+Q_charge.fieldbyname("nzp_serv").asstring+" "+
            //        //     " код поставщика "+Q_charge.fieldbyname("nzp_supp").asstring+
            //        //     " код льготы "+Q_pere.fieldbyname("nzp_lgota").asstring
            //        //     ,true);
            //        //   //bad_file=true;
            //        //   isBadLs=true;
            //        //   exit;
            //        //  end;

            //        //  lgota="6|"+Q_pere.fieldbyname("kod_cz").asstring+"|"+
            //        //         Q_pere.fieldbyname("nzp_gilec").asstring;

            //        //  if Q_pere.fieldbyname("sum_lgota").asfloat<0
            //        //  then begin
            //        //        //bad_file=true;
            //        //        isBadLs=true;
            //        //        CurT_Start.WriteToError("Сумма льготы должна быть больше или равена 0  "
            //        //        +"  Лицевой счет №"+sqlAdres.fieldbyName("num_ls").asstring,true);
            //        //  end;

            //        //  if (Q_pere.fieldbyname("sum_lgota").asfloat>0) and (SpEd_year.Value>2004) and
            //        //     (pos("|"+Q_pere.fieldbyname("kod_cz").asstring+"|",LgotKatSpis)=0) then begin
            //        ////        bad_file=true;
            //        //        isBadLs=true;
            //        //        CurT_Start.WriteToError("Льгота в 2005 году должна быть равна 0 "
            //        //        +" текущее значение "+formatfloat("0.00",sum_lgota_k)
            //        //        +" код категории СЗ "+Q_pere.fieldbyname("kod_cz").asstring
            //        //        +"  Лицевой счет №"+sqlAdres.fieldbyName("num_ls").asstring,true);
            //        //  end;

            //        //  sum_lgota_k=sum_lgota_k-Q_pere.fieldbyname("sum_lgota").asfloat;



            //        //  if Q_pere.fieldbyname("is_family").asinteger=0 then
            //        //  begin
            //        //  //Кол-во носителей льгот
            //        //  lgota=lgota+"|1|0|";
            //        //  //Сумма по носителю льготы
            //        //  lgota=lgota+formatfloat("0.00",Q_pere.fieldbyname("sum_lgota").asfloat)+"|";
            //        //  //Суммма по получающим льготы
            //        //  lgota=lgota+"0.00|";
            //        //  //Сумма перерасчета по носителю льгот
            //        //  lgota=lgota+get_sum_lgota_pere+"|0.00|";
            //        //  end
            //        //  else
            //        //  begin
            //        //  //Кол-во получающих льготы
            //        //  lgota=lgota+"|0|1|";
            //        //  //Сумма по носителю льготы
            //        //  lgota=lgota+"0.00|";
            //        //  //Суммма по получающим льготы
            //        //  lgota=lgota+formatfloat("0.00",Q_pere.fieldbyname("sum_lgota").asfloat);
            //        //  //Сумма перерасчета по получающему льготы
            //        //  lgota=lgota+"|0.00|"+get_sum_lgota_pere+"|";
            //        //  end;
            //        //  //Признаки бюджета
            //        //  lgota=lgota+Q_pere.fieldbyname("nzp_bud").asstring+"|"+
            //        //  Q_pere.fieldbyname("nzp_law").asstring+"|0|";
            //        //  //Вместо перерасчетов
            //        //  lgota=Replacestr(lgota,",",".");
            //        ////  writeln(f,strtooem(lgota));
            //        //  StringDomo=StringDomo+#13#10+strtooem(lgota);
            //        //end;


            //        #endregion

            //        // Returns XXX;
            //        // XXX.result = true;
            //        //return XXX;

            #endregion Comment

            return ret;

        }
    }
}
