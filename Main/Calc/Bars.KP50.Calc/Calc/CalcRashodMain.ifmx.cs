// Подсчет расходов

using Bars.KP50.Utils;
using STCLINE.KP50.Interfaces;

#region Подключаемые модули

using System;
using System.Data;

using STCLINE.KP50.Global;

#endregion Подключаемые модули

#region здесь производится подсчет расходов

namespace STCLINE.KP50.DataBase
{

    //здась находятся классы для подсчета расходов
    public partial class DbCalcCharge : DataBaseHead
    {
        #region создание ttt_counters_xx - структуры для хранения стека 3
        public bool CreateTempRashodMain(IDbConnection conn_db, Rashod rashod, String p_dat_charge, out Returns ret)
        //--------------------------------------------------------------------------------
        {

            ExecSQL(conn_db, " Drop table ttt_counters_xx ", false);
            // создадим пустышку... структура совпадает с counters_xx!

            ret = ExecSQL(conn_db,
                " Create Temp table ttt_counters_xx" +
                  " (  nzp_cntx    serial        not null, " +
                  "    nzp_dom     integer       not null, " +
                  "    nzp_kvar    integer       default 0 not null , " +
                  "    nzp_type    integer       not null, " +               //1,2,3
                  "    nzp_serv    integer       not null, " +
                  "    dat_charge  date, " +
                  "    cur_zap     integer       default 0 not null, " +     //0-текущее значение, >0 - ссылка на следующее значение (nzp_cntx)
                  "    nzp_counter integer       default 0 , " +             //счетчик или вариатны расходов для stek=3
                  "    cnt_stage   integer       default 0 , " +             //разрядность
                  "    mmnog       " + sDecimalType + "(15,7) default 1.00 , " +             //масшт. множитель
                  "    stek        integer       default 0 not null, " +     //3-итого по лс,дому; 1-счетчик; 2,3,4,5 - стек расходов
                  "    rashod      " + sDecimalType + "(15,7) default 0.00 not null, " +  //общий расход в зависимости от stek
                  "    dat_s       date          not null, " +               //"дата с" - для ПУ, для по-дневного расчета период в месяце (dp)
                  "    val_s       " + sDecimalType + "(15,7) default 0.00 not null, " +  //значение (а также коэф-т)
                  "    dat_po      date not null, " +                        //"дата по"- для ПУ, для по-дневного расчета период в месяце (dp_end)
                  "    val_po      " + sDecimalType + "(15,7) default 0.00 not null, " +  //значение
                  "    ngp_cnt       " + sDecimalType + "(14,7) default 0.0000000, " +  // расход на нежилые помещения
                  "    rash_norm_one " + sDecimalType + "(14,7) default 0.0000000, " +  // норматив на 1 человека
                  "    val1_g      " + sDecimalType + "(15,7) default 0.00 not null, " +  //расход по счетчику nzp_counter или нормативные расходы в расчетном месяце без учета вр.выбывших
                  "    val1        " + sDecimalType + "(15,7) default 0.00 not null, " +  //расход по счетчику nzp_counter или нормативные расходы в расчетном месяце
                  "    val2        " + sDecimalType + "(15,7) default 0.00 not null, " +  //дом: расход КПУ
                  "    val3        " + sDecimalType + "(15,7) default 0.00         , " +  //дом: расход нормативщики
                  "    val4        " + sDecimalType + "(15,7) default 0.00         , " +  //общий расход по счетчику nzp_counter
                  "    rvirt       " + sDecimalType + "(15,7) default 0.00         , " +  //вирт. расход
                  "    squ1        " + sDecimalType + "(15,7) default 0.00         , " +  //площадь лс, дома (по всем лс)
                  "    squ2        " + sDecimalType + "(15,7) default 0.00         , " +  //площадь лс без КПУ (для домовых строк)
                  "    nzp_prm_squ2 integer default 133, " +                              // по умолчанию как squ2 берется nzp_prm=133 отапливаемая площадь
                  "    cls1        integer       default 0 not null   , " +  //количество лс дома по услуге
                  "    cls2        integer       default 0 not null   , " +  //количество лс без КПУ (для домовых строк)
                  "    gil1_g      " + sDecimalType + "(15,7) default 0.00         , " +  //кол-во жильцов в лс без учета вр.выбывших
                  "    gil1        " + sDecimalType + "(15,7) default 0.00         , " +  //кол-во жильцов в лс
                  "    gil2        " + sDecimalType + "(15,7) default 0.00         , " +  //кол-во жильцов в лс
                  "    cnt1_g      integer       default 0 not null, " +     //кол-во жильцов в лс (нормативное) без учета вр.выбывших
                  "    cnt1        integer       default 0 not null, " +     //кол-во жильцов в лс (нормативное)
                  "    cnt2        integer       default 0 not null, " +     //кол-во комнат в лс
                  "    cnt3        integer       default 0, " +              //тип норматива в зависимости от услуги (ссылка на resolution.nzp_res)
                  "    cnt4        integer       default 0, " +              //1-дом не-МКД (0-МКД)
                  "    cnt5        integer       default 0, " +              //резерв
                  "    dop87       " + sDecimalType + "(15,7) default 0.00         , " +  //доп.значение 87 постановления (7кВт или добавок к нормативу  (87 П) )
                  "    pu7kw       " + sDecimalType + "(15,7) default 0.00         , " +  //7 кВт для КПУ (откорректированный множитель)
                  "    gl7kw       " + sDecimalType + "(15,7) default 0.00         , " +  //7 кВт КПУ * gil1 (учитывая корректировку)
                  "    vl210       " + sDecimalType + "(15,7) default 0.00         , " +  //расход 210 для nzp_type = 6
                  "    kf307       " + sDecimalType + "(15,7) default 0.00         , " +  //коэфициент 307 для КПУ или коэфициент 87 для нормативщиков
                  "    kf307n      " + sDecimalType + "(15,7) default 0.00         , " +  //коэфициент 307 для нормативщиков
                  "    kf307f9     " + sDecimalType + "(15,7) default 0.00         , " +  //коэфициент 307 по формуле 9
                  "    kf_dpu_kg   " + sDecimalType + "(15,7) default 0.00         , " +  //коэфициент ДПУ для распределения пропорционально кол-ву жильцов
                  "    kf_dpu_plob " + sDecimalType + "(15,7) default 0.00         , " +  //коэфициент ДПУ для распределения пропорционально сумме общих площадей
                  "    kf_dpu_plot " + sDecimalType + "(15,7) default 0.00         , " +  //коэфициент ДПУ для распределения пропорционально сумме отапливаемых площадей
                  "    kf_dpu_ls   " + sDecimalType + "(15,7) default 0.00         , " +  //коэфициент ДПУ для распределения пропорционально кол-ву л/с
                  "    dlt_in      " + sDecimalType + "(15,7) default 0.00         , " +  //входящии нераспределенный расход (остаток)
                  "    dlt_cur     " + sDecimalType + "(15,7) default 0.00         , " +  //текущая дельта
                  "    dlt_reval   " + sDecimalType + "(15,7) default 0.00         , " +  //перерасчет дельты за прошлые месяцы
                  "    dlt_real_charge " + sDecimalType + "(15,7) default 0.00     , " +  //перерасчет дельты за прошлые месяцы
                  "    dlt_calc    " + sDecimalType + "(15,7) default 0.00         , " +  //распределенный (учтенный) расход
                  "    dlt_out     " + sDecimalType + "(15,7) default 0.00         , " +  //исходящии нераспределенный расход (остаток)
                  "    kod_info    integer default 0," +
                  "    sqgil       " + sDecimalType + "(15,7) default 0.00         ," +  //жилая площадь лс
                  "    is_day_calc integer not null, " +
                  "    is_use_knp integer default 0, " +
                  "    is_use_ctr integer default 0," +//Количество временно выбывших
                  "    nzp_period  integer not null, " +
                //"    dp       " + sDateTimeType + ", " +
                //"    dp_end   " + sDateTimeType + ", " +
                  "    cntd integer," +
                  "    cntd_mn integer, " +
                  "    nzp_measure integer, " + // ед.измерения 
                  "    norm_type_id integer, " + // id типа норматива - для нового режима введения нормативов
                  "    norm_tables_id integer, " + // id норматива - по нему можно получить набор влияющих пар-в и их знач.
                  "    val1_source " + sDecimalType + "(15,7) default 0.00 not null, " +  //val1 без учета повышающего коэффициента
                  "    val4_source " + sDecimalType + "(15,7) default 0.00 not null, " +  //val4 без учета повышающего коэффициента
                  "    up_kf " + sDecimalType + "(15,7) default 1.00 not null " +   //повышающий коэффициент для нормативного расхода
                  " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " Drop table ttt_cnt_uni ", false);
            // только открытые лс и выборка услуг
            ret = ExecSQL(conn_db,
                " Create temp table ttt_cnt_uni " +
                " ( nzp_kvar integer," +
                "   nzp_dom  integer," +
                "   nzp_serv integer, " +
                "   nzp_measure integer, " +
                "   is_day_calc integer not null," +
                "   is_use_knp integer default 0, " +
                "   is_use_ctr integer default 0" +//Количество временно выбывших
                " )  " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // только открытые лс и выборка услуг
            ret = ExecSQL(conn_db,
                " Insert into ttt_cnt_uni (nzp_kvar,nzp_dom,nzp_serv,nzp_measure,is_day_calc,is_use_knp,is_use_ctr) " +
                " Select k.nzp_kvar, k.nzp_dom, t.nzp_serv,nzp_measure,max(k.is_day_calc),max(t.is_use_knp),max(t.is_use_ctr)" +
                " From temp_table_tarif t, t_opn k, " + rashod.paramcalc.kernel_alias + "s_counts s " +
                " Where  " +
                "   k.nzp_kvar = t.nzp_kvar and t.nzp_serv=s.nzp_serv " +
                "   and " + rashod.where_dom + rashod.where_kvarK +
                " Group by 1,2,3,4 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " Create index ix_ttt_cnt_uni on ttt_cnt_uni (nzp_kvar) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_cnt_uni ", true);

            // только открытые лс и выборка услуг
            ret = ExecSQL(conn_db,
                " Insert into ttt_counters_xx" +
                " (nzp_kvar,nzp_dom,nzp_serv,is_day_calc,nzp_period,dat_s,dat_po,cntd,cntd_mn, stek,nzp_type,mmnog,dat_charge,nzp_measure,is_use_knp,is_use_ctr) " +
                " Select t.nzp_kvar, t.nzp_dom, t.nzp_serv, t.is_day_calc," +
                " k.nzp_period,k.dp,k.dp_end,k.cntd,k.cntd_mn," +
                " 10,3,-100, " + p_dat_charge + ",t.nzp_measure,t.is_use_knp,t.is_use_ctr" +
                " From ttt_cnt_uni t,t_gku_periods k " +
                " Where t.nzp_kvar = k.nzp_kvar "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            ExecSQL(conn_db, " Create unique index ix_aid00_sq on ttt_counters_xx (nzp_cntx) ", true);
            ExecSQL(conn_db, " Create        index ix_aid11_sq on ttt_counters_xx (nzp_kvar,nzp_serv,kod_info) ", true);
            ExecSQL(conn_db, " Create        index ix_aid22_sq on ttt_counters_xx (nzp_dom,nzp_serv) ", true);
            ExecSQL(conn_db, " Create        index ix_aid33_sq on ttt_counters_xx (nzp_serv,cnt2) ", true);
            ExecSQL(conn_db, " Create        index ix_aid44_sq on ttt_counters_xx (nzp_kvar,nzp_serv,dat_s,dat_po) ", true);
            ExecSQL(conn_db, " Create        index ix_aid55_sq on ttt_counters_xx (nzp_kvar,nzp_serv,stek,cnt_stage) ", true);
            ExecSQL(conn_db, " Create        index ix_aid66_sq on ttt_counters_xx (nzp_kvar,nzp_prm_squ2) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_counters_xx ", true);

            ViewTbl(conn_db, " select * from ttt_counters_xx order by nzp_kvar,nzp_serv,dat_s ");

            ExecSQL(conn_db, " Drop table ttt_cnt_uni ", false);

            return true;
        }
        #endregion создание ttt_counters_xx - структуры для хранения стека 3

        #region первичное заполнение ttt_counters_xx - заполнение площадей,количества жильцов,признаков ПУ
        public bool SetTempRashodParams(IDbConnection conn_db, Rashod rashod, bool b_next_month7, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            #region Заполнение площадей

            // общая площадь
            ret = ExecSQL(conn_db,
                " Update ttt_counters_xx " +
                " Set squ1 = ( select " + sNvlWord + "(p.val_prm,'0') from ttt_prm_1d p "+
                " where p.nzp=ttt_counters_xx.nzp_kvar and p.nzp_prm=4 and ttt_counters_xx.dat_s>=p.dat_s and ttt_counters_xx.dat_po<=p.dat_po )" + sConvToNum +
                " Where exists ( select 1 from ttt_prm_1d p where p.nzp=ttt_counters_xx.nzp_kvar and p.nzp_prm=4 " +
                "  and ttt_counters_xx.dat_s>=p.dat_s and ttt_counters_xx.dat_po<=p.dat_po) "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // отапливаемая площадь
            ret = ExecSQL(conn_db,
                " Update ttt_counters_xx " +
                " Set nzp_prm_squ2 = ( select max(f.nzp_prm_rash) " +
                    " from temp_table_tarif t," + rashod.paramcalc.kernel_alias + "formuls_opis f" +
                    " where t.nzp_frm=f.nzp_frm and f.nzp_frm_typrs=1814 and f.nzp_prm_rash<>133" +
                    "   and t.nzp_kvar=ttt_counters_xx.nzp_kvar and t.nzp_serv=ttt_counters_xx.nzp_serv )" +
                " Where exists ( select 1 " +
                    " from temp_table_tarif t," + rashod.paramcalc.kernel_alias + "formuls_opis f" +
                    " where t.nzp_frm=f.nzp_frm and f.nzp_frm_typrs=1814 and f.nzp_prm_rash<>133" +
                    "   and t.nzp_kvar=ttt_counters_xx.nzp_kvar and t.nzp_serv=ttt_counters_xx.nzp_serv ) " 
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            

            ret = ExecSQL(conn_db,
                " Update ttt_counters_xx " +
                " Set squ2 = ( select " + sNvlWord + "(p.val_prm,'0') from ttt_prm_1d p " +
                " where p.nzp=ttt_counters_xx.nzp_kvar and p.nzp_prm=ttt_counters_xx.nzp_prm_squ2" +
                " and ttt_counters_xx.dat_s>=p.dat_s and ttt_counters_xx.dat_po<=p.dat_po )" + sConvToNum +
                " Where exists ( select 1 from ttt_prm_1d p" +
                " where p.nzp=ttt_counters_xx.nzp_kvar and p.nzp_prm=ttt_counters_xx.nzp_prm_squ2 " +
                " and ttt_counters_xx.dat_s>=p.dat_s and ttt_counters_xx.dat_po<=p.dat_po ) "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // жилая площадь - нужна для коммуналок - для разделения расхода по Пост.354
            ret = ExecSQL(conn_db,
                " Update ttt_counters_xx t" +
                " Set sqgil = " + sNvlWord + "(p.val_prm,'0')" + sConvToNum + " from ttt_prm_1d p" +
                " where p.nzp=t.nzp_kvar and p.nzp_prm=6 " +
                " and t.dat_s>=p.dat_s and t.dat_po<=p.dat_po"
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            
            #endregion Заполнение площадей

            #region Установка признаков ПУ

            // признак ПУ
            ret = ExecSQL(conn_db,
                " Update ttt_counters_xx " +
                " Set kod_info = 1 " +
                " Where 0 < ( Select count(*) From " + rashod.counters_xx + " k " +
                            " Where ttt_counters_xx.nzp_kvar = k.nzp_kvar " +
                            "   and ttt_counters_xx.nzp_serv = k.nzp_serv " +
                            "   and k." + rashod.where_dom + rashod.where_kvarK +
                            " ) "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // отдельно выделим лс, где вообще нет ПУ по услуге (на всякий случай)
            ret = ExecSQL(conn_db,
                " Update ttt_counters_xx " +
                " Set nzp_counter = -2 " +
                " Where 0 = ( Select count(*) From " + rashod.paramcalc.data_alias + "counters_spis c " +
                            " Where ttt_counters_xx.nzp_kvar = c.nzp " +
                            "   and ttt_counters_xx.nzp_serv = c.nzp_serv " +
                            "   and c.nzp_type = 3 " +
                   " ) "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            #endregion Установка признаков ПУ

            #region Заполнение количества жильцов
            //----------------------------------------------------------------
            //кол-во жильцов по лс
            //----------------------------------------------------------------
            // gil_xx - cnt1 - целое число жильцов без учета правила 15 дней, но с учетом временно выбывших
            // gil_xx - cnt2 - кол-во жильцов с учетом правила 15 дней без учета временно выбывших
            // gil_xx - val1 - дробное число жильцов без учета правила 15 дней, но с учетом временно выбывших
            // gil_xx - val2 - кол-во жильцов цифрой из БД (выбран nzp_prm=5 из prm_1)
            // gil_xx - val3 - кол-во временно прибывших (выбран nzp_prm=131 из prm_1)
            // gil_xx - val5 - временно выбывших (nzp_prm=10 - это всегда из gil_periods)
            // gil_xx - val6 - Количество незарегистврированных проживающих (выбран nzp_prm=1395 из prm_1)

            ExecSQL(conn_db, " Drop table ttt_aid_c2 ", false);

            ret = ExecSQL(conn_db,
                " Create temp table ttt_aid_c2 " +
                " ( nzp_kvar integer, " +
                "   vald   " + sDecimalType + "(12,4)," +
                "   vald_g " + sDecimalType + "(12,4)," +
                "   val6   " + sDecimalType + "(12,4)," +
                "   dat_s  date," +
                "   dat_po date " +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " Insert Into ttt_aid_c2 (nzp_kvar,dat_s,dat_po,vald,vald_g,val6) " +
                " Select t.nzp_kvar," + rashod.paramcalc.dat_s + "," + rashod.paramcalc.dat_po + "," +
                  " max(case when (g.cnt2 - g.val5 + g.val3)>0 then g.cnt2 - g.val5 + g.val3 else 0 end) as vald, max(g.cnt2 + g.val3) as vald_g, max(g.val6) " +
                " from t_opn t," + rashod.gil_xx + " g " +
                " where t.nzp_kvar=g.nzp_kvar and g.stek=3 and t.is_day_calc=0 " +
                " Group by 1 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }


            ret = ExecSQL(conn_db,
                " Insert Into ttt_aid_c2 (nzp_kvar,dat_s,dat_po,vald,vald_g,val6) " +
                " Select t.nzp_kvar,g.dat_s,g.dat_po," +
                  " max(case when (g.cnt2 - g.val5 + g.val3)>0 then g.cnt2 - g.val5 + g.val3 else 0 end) as vald, max(g.cnt2 + g.val3) as vald_g, max(g.val6) " +
                " from t_opn t," + rashod.gil_xx + " g " +
                " where t.nzp_kvar=g.nzp_kvar and g.stek=4 and t.is_day_calc=1 " +
                " Group by 1,2,3 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " Create index ix_ttt_aid_c2 on ttt_aid_c2 (nzp_kvar,dat_s,dat_po) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_aid_c2 ", true);

            //дробное кол-в жильцов (ссылка get_kol_gil)
            ret = ExecSQL(conn_db,
                " Update ttt_counters_xx Set " +
                " gil1   = ( Select max((case when ttt_counters_xx.is_use_ctr = 1 then k.vald else k.vald_g end) + (case when ttt_counters_xx.is_use_knp=1 then k.val6 else 0 end))" +
                           " From ttt_aid_c2 k" +
                           " Where ttt_counters_xx.nzp_kvar = k.nzp_kvar" +
                             " and ttt_counters_xx.dat_s <= k.dat_po and ttt_counters_xx.dat_po >= k.dat_s )," +
                " gil1_g = ( Select max(k.vald_g + (case when ttt_counters_xx.is_use_knp=1 then k.val6 else 0 end))" +
                           " From ttt_aid_c2 k " +
                           " Where ttt_counters_xx.nzp_kvar = k.nzp_kvar" +
                             " and ttt_counters_xx.dat_s <= k.dat_po and ttt_counters_xx.dat_po >= k.dat_s ) " +
                " Where 0 < ( Select count(*) From ttt_aid_c2 k Where ttt_counters_xx.nzp_kvar = k.nzp_kvar ) "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " Update ttt_counters_xx " +
                " Set cnt1 = Round(gil1),cnt1_g = Round(gil1_g) " +
                " Where gil1 > 0 or gil1_g > 0 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            #endregion Заполнение количества жильцов

            #region Заполнение кол-во комнат для Электричества
            //----------------------------------------------------------------
            //кол-во комнат для ЕЕ
            //----------------------------------------------------------------
            ExecSQL(conn_db, " Drop table ttt_aid_c1 ", false);

            ret = ExecSQL(conn_db,
                " Create temp table ttt_aid_c1 " +
                " ( nzp_kvar integer, " +
                "   val3     integer " +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " Insert Into ttt_aid_c1 (nzp_kvar,val3) " +
                " Select nzp as nzp_kvar, max(p.val_prm" + sConvToInt + ") as val3 " +
                " From ttt_counters_xx a, ttt_prm_1 p " +
                " Where a.nzp_kvar = p.nzp " +
                "   and a.nzp_serv in (25,210,410) " +
                "   and p.nzp_prm = 107 " +
                " Group by 1 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " Create unique index ix_ttt_aid_c1 on ttt_aid_c1 (nzp_kvar) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_aid_c1 ", true);

            ret = ExecSQL(conn_db,
                " Update ttt_counters_xx " +
                " Set cnt2 = ( Select val3 From ttt_aid_c1 k Where ttt_counters_xx.nzp_kvar = k.nzp_kvar )" +
                " Where nzp_serv in (25,210,410) " +
                "   and 0 < ( Select count(*) From ttt_aid_c1 k Where ttt_counters_xx.nzp_kvar = k.nzp_kvar ) "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            string sort_schema = rashod.paramcalc.data_alias;
#if PG
            sort_schema = pgDefaultSchema + tableDelimiter;
#endif

            //если кол-во комнат нет, то по выставим опосредованно по площади nzp_res = 44
            ret = ExecSQL(conn_db,
                " Update ttt_counters_xx " +
                " Set cnt2 = ( Select min( case when trim(b.value)" + sConvToInt + " = 5 then 5 else trim(b.value)" + sConvToInt + " - 1 end ) " +
                             " From " + rashod.paramcalc.kernel_alias + "res_values a, " + rashod.paramcalc.kernel_alias + "res_values b  " +
                             " Where a.nzp_res = 44 " +
                             "   and a.nzp_res = b.nzp_res " +
                             "   and a.nzp_y   = b.nzp_y   " +
                             "   and b.nzp_x   > a.nzp_x   " +
                             "   and ttt_counters_xx.squ1 > " + sort_schema + "sortnum( trim(a.value) ) " +
                             " ) " +
                " Where nzp_serv in (25,210,410) " +
                "   and squ1 > 0 " +
                "   and cnt2 = 0 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            #endregion Заполнение кол-во комнат для Электричества

            #region Заполнение тип водоснабжения для ХВС-ГВС
            //----------------------------------------------------------------
            //тип водоснабжения для ХВС-ГВС
            //----------------------------------------------------------------
            ExecSQL(conn_db, " Drop table ttt_aid_c1 ", false);

            ret = ExecSQL(conn_db,
                " Create temp table ttt_aid_c1 " +
                " ( nzp_kvar integer, " +
                "   nzp_serv integer, " +
                "   val3     integer " +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // ХВС
            ret = ExecSQL(conn_db,
                  " insert into ttt_aid_c1 (nzp_kvar, nzp_serv, val3) " +
                  " Select nzp as nzp_kvar, 6, max(replace( " + sNvlWord + "(p.val_prm,'0'), ',', '.')" + sConvToInt + ") as val3 " +
                  " From ttt_counters_xx a, ttt_prm_1 p " +
                  " Where a.nzp_kvar = p.nzp " +
                  "   and a.nzp_serv = 6 " +
                  "   and p.nzp_prm  = 7 " +
                  " Group by 1 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // КАН
            int iNzpRes = 7;
            if (b_next_month7)
            {
                iNzpRes = 2007;
            }
            ret = ExecSQL(conn_db,
                  " insert into ttt_aid_c1 (nzp_kvar, nzp_serv, val3) " +
                  " Select nzp as nzp_kvar, a.nzp_serv, max(replace( " + sNvlWord + "(p.val_prm,'0'), ',', '.')" + sConvToInt + ") as val3 " +
                  " From ttt_counters_xx a, ttt_prm_1 p " +
                  " Where a.nzp_kvar = p.nzp " +
                  "   and a.nzp_serv in (7,324) " +
                  "   and p.nzp_prm = " + iNzpRes +
                  " Group by 1,2 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // ГВС
            // на дом
            ret = ExecSQL(conn_db,
                  " insert into ttt_aid_c1 (nzp_kvar, nzp_serv, val3) " +
                  " Select a.nzp_kvar, a.nzp_serv, max(replace( " + sNvlWord + "(p.val_prm,'0'), ',', '.')" + sConvToInt + ") as val3 " +
                  " From ttt_counters_xx a, ttt_prm_2 p " +
                  " Where a.nzp_dom = p.nzp " +
                  "   and a.nzp_serv in (9,281,323) " +
                  "   and p.nzp_prm = 38 " +
                  " Group by 1,2 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " Create unique index ix_ttt_aid_c1 on ttt_aid_c1 (nzp_kvar, nzp_serv) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_aid_c1 ", true);

            // на квартиру - в первую очередь
            ret = ExecSQL(conn_db,
                  " Update ttt_aid_c1 " +
                  " Set val3 = ( Select max(replace( " + sNvlWord + "(p.val_prm,'0'), ',', '.')" + sConvToInt + ")" +
                  "              From ttt_prm_1 p Where ttt_aid_c1.nzp_kvar = p.nzp and p.nzp_prm = 463 ) " +
                  " Where nzp_serv in (9,281,323) " +
                  "   and 0  < ( Select count(*)  From ttt_prm_1 p Where ttt_aid_c1.nzp_kvar = p.nzp and p.nzp_prm = 463 ) "
                  , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " Drop table ttt_aid_c1x ", false);

            ret = ExecSQL(conn_db,
                " Create temp table ttt_aid_c1x " +
                " ( nzp_kvar integer, " +
                "   nzp_serv integer  " +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " insert into ttt_aid_c1x (nzp_kvar,nzp_serv) " +
                " Select nzp_kvar,nzp_serv " +
                " From ttt_aid_c1 Where nzp_serv in (9,281,323) " +
                " group by 1,2 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " Create unique index ix_ttt_aid_c1x on ttt_aid_c1x (nzp_kvar,nzp_serv) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_aid_c1x ", true);

            ret = ExecSQL(conn_db,
                " insert into ttt_aid_c1 (nzp_kvar, nzp_serv, val3) " +
                " Select a.nzp_kvar,a.nzp_serv, max(replace( " + sNvlWord + "(p.val_prm,'0'), ',', '.')" + sConvToInt + ") " +
                " From ttt_counters_xx a, ttt_prm_1 p " +
                " Where a.nzp_kvar = p.nzp " +
                "   and a.nzp_serv in (9,281,323) " +
                "   and p.nzp_prm = 463 " +
                "   and 0 = ( Select count(*) From ttt_aid_c1x b Where a.nzp_kvar = b.nzp_kvar and a.nzp_serv=b.nzp_serv )" +
                " Group by 1,2 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, sUpdStat + " ttt_aid_c1 ", true);

            ExecSQL(conn_db, " Drop table ttt_aid_c1x ", false);

            ret = ExecSQL(conn_db,
                " Update ttt_counters_xx " +
                " Set cnt2= ( Select val3 From ttt_aid_c1 k" +
                            " Where ttt_counters_xx.nzp_kvar = k.nzp_kvar and ttt_counters_xx.nzp_serv = k.nzp_serv ) " +
                " Where nzp_serv in (6, 9, 7, 281, 323, 324) " +
                  " and 0 < ( Select count(*) From ttt_aid_c1 k" +
                            " Where ttt_counters_xx.nzp_kvar = k.nzp_kvar and ttt_counters_xx.nzp_serv = k.nzp_serv ) "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, sUpdStat + " ttt_counters_xx ", true);

            #endregion Заполнение тип водоснабжения для ХВС-ГВС

            return true;
        }
        #endregion первичное заполнение ttt_counters_xx - заполнение площадей,количества жильцов,признаков ПУ

        #region стек 3 - первоначально сформировать итоговые суммы по стеку (пока без сумм!)
        public bool InsStek3First(IDbConnection conn_db, Rashod rashod, string p_dat_charge, bool b_calc_kvar, out Returns ret)
        {
            ret = Utils.InitReturns();

            #region стек 3 - сформировать итоговые суммы по лс

            ret = ExecSQL(conn_db,
                " Update ttt_counters_xx Set stek=20 Where stek=10 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            string sql =
                " Insert into " + rashod.counters_xx +
                " ( " +
                  " nzp_dom,nzp_kvar,nzp_type,nzp_serv,dat_charge,cur_zap, " +
                  " nzp_counter,cnt_stage,mmnog,stek,rashod,dat_s,val_s,dat_po,val_po,ngp_cnt,rash_norm_one, " +
                  " val1_g,val1,val2,val3,val4,rvirt,squ1,squ2,cls1,cls2,gil1_g,gil1,gil2, " +
                  " cnt1_g,cnt1,cnt2,cnt3,cnt4,cnt5,dop87,pu7kw,gl7kw,vl210,kf307, " +
                  " kf307n,kf307f9,kf_dpu_kg,kf_dpu_plob,kf_dpu_plot,kf_dpu_ls, " +
                  " dlt_in,dlt_cur,dlt_reval,dlt_real_charge,dlt_calc,dlt_out,kod_info,norm_type_id,norm_tables_id," +
                " val1_source,val4_source,up_kf " +
                " ) " +
                " Select " +
                  " nzp_dom,nzp_kvar,nzp_type,nzp_serv,dat_charge,cur_zap, " +
                  " nzp_counter,cnt_stage,mmnog,stek,rashod,dat_s,val_s,dat_po,val_po,ngp_cnt,rash_norm_one, " +
                  " val1_g,val1,val2,val3,val4,rvirt,squ1,squ2,cls1,cls2,gil1_g,gil1,gil2, " +
                  " cnt1_g,cnt1,cnt2,cnt3,cnt4,cnt5,dop87,pu7kw,gl7kw,vl210,kf307, " +
                  " kf307n,kf307f9,kf_dpu_kg,kf_dpu_plob,kf_dpu_plot,kf_dpu_ls, " +
                  " dlt_in,dlt_cur,dlt_reval,dlt_real_charge,dlt_calc,dlt_out,kod_info,norm_type_id,norm_tables_id," +
                " val1_source,val4_source,up_kf " +
                " From ttt_counters_xx Where 1 = 1 ";

            if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, "ttt_counters_xx", "nzp_cntx", sql, 50000, " ", out ret);
            }
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            UpdateStatistics(false, rashod.paramcalc, rashod.counters_tab, out ret);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            #endregion стек 3 - сформировать итоговые суммы по лс

            #region стек 3 - сформировать итоговые строки по дому (пока без сумм!)

            if (!b_calc_kvar)
            {
                //
                ret = ExecSQL(conn_db,
                    " Insert into " + rashod.counters_xx +
                    " ( nzp_dom,nzp_serv, stek,nzp_type,dat_charge,nzp_kvar,nzp_counter,dat_s,dat_po,squ1 ) " +
                    " Select " +
                    " nzp_dom,nzp_serv, 3,1, " + p_dat_charge + " ,0, 0," + rashod.paramcalc.dat_s + "," + rashod.paramcalc.dat_po + ", 0 " +
                    " From ttt_counters_xx " +
                    " Group by 1,2 "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                ExecSQL(conn_db, " Drop table ttt_ans_grpu ", false);
                ret = ExecSQL(conn_db,
                    " Create temp table ttt_ans_grpu " +
                    " ( nzp_dom     integer, " +
                    "   nzp_counter integer, " +
                    "   nzp_serv    integer  " +
                    " ) " + sUnlogTempTable
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                ret = ExecSQL(conn_db,
                    " Insert into ttt_ans_grpu" +
                    " ( nzp_dom,nzp_counter,nzp_serv ) " +
                    " Select nzp_dom,nzp_counter,nzp_serv " +
                    " From " + rashod.counters_xx +
                    " Where nzp_type = 2 and stek not in (9,39) and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                    " Group by 1,2,3 "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                ExecSQL(conn_db, sUpdStat + " ttt_ans_grpu ", true);

                ret = ExecSQL(conn_db,
                    " Insert into " + rashod.counters_xx +
                    " ( nzp_dom,nzp_counter,nzp_serv, stek,nzp_type,dat_charge,nzp_kvar,dat_s,dat_po,squ1 ) " +
                    " Select " +
                    " nzp_dom,nzp_counter,nzp_serv, 3,2," + p_dat_charge + " ,0, " +
                    rashod.paramcalc.dat_s + "," + rashod.paramcalc.dat_po + ", 0 " +
                    " From ttt_ans_grpu Where 1 = 1 "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                ret = ExecSQL(conn_db, " Drop table ttt_ans_grpu ", true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                UpdateStatistics(false, rashod.paramcalc, rashod.counters_tab, out ret);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            }

            #endregion стек 3 - сформировать итоговые строки по дому (пока без сумм!)

            return true;
        }
        #endregion стек 3 - первоначально сформировать итоговые суммы по стеку (пока без сумм!)

        #region проставить расход - stek = 3 и nzp_type = 3
        public bool CalcStek3Rashod(IDbConnection conn_db, Rashod rashod, out Returns ret)
        {
            //--------------------------101 - по-умолчанию, норматив или КПУ
            //для отопления: val1/val2 = расход в ГКал на квартиру, val3/val4 = расход в ГКал на 1 м2, squ2 = отапливаемая площадь
            //для остальных услуг: val1/val2 = расход на квартиру
            string sql =
                    " Update ttt_counters_xx " +
                    " Set rashod = val1 + val2, nzp_counter = 0 " +
                    " Where nzp_type = 3 and stek in (3,10) ";

            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            return true;
        }
        #endregion проставить расход - stek = 3 и nzp_type = 3

        #region коррекция расхода по канализации по ХВС и ГВС
        public bool CalcRashodKANsumHVandGV(IDbConnection conn_db, Rashod rashod, out Returns ret)
        {
            //----------------------------------------------------------------
            //коррекция расхода по канализации по ХВС и ГВС
            //----------------------------------------------------------------
            ExecSQL(conn_db, " Drop table ttt_ans_rsh69 ", false);
            ret = ExecSQL(conn_db,
                " Create temp table ttt_ans_rsh69 (" +
                " nzp_dom  integer," +
                " nzp_kvar integer," +
                " nzp_serv integer," +
                " stek     integer," +
                " dat_s  date not null, " +
                " dat_po date not null, " +
                " pr_rash  integer default 0," +
                " rashod   "  + sDecimalType + "(16,6) default 0," +
                " rash_kv  "  + sDecimalType + "(16,6) default 0," +
                " rash_kv_g " + sDecimalType + "(16,6) default 0," +
                " rash_odn "  + sDecimalType + "(16,6) default 0," +
                " rashod_source " + sDecimalType + "(16,7) default 0," +
                " up_kf "     + sDecimalType + "(16,7) default 0," +
                " kf307 "     + sDecimalType + "(15,7) default 0," +
                " kod_info  integer  default 0," +
                " is_device integer  default 0 " +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); conn_db.Close(); return false; }

            ExecSQL(conn_db, " Drop table ttt_ans_dop69 ", false);
            ret = ExecSQL(conn_db,
                " Create temp table ttt_ans_dop69 (" +
                " nzp_dom  integer," +
                " nzp_kvar integer," +
                " nzp_serv integer," +
                " stek     integer," +
                " dat_s  date not null, " +
                " dat_po date not null, " +
                " pr_rash  integer default 0," +
                " rashod   "  + sDecimalType + "(16,6) default 0," +
                " rash_kv  "  + sDecimalType + "(16,6) default 0," +
                " rash_kv_g " + sDecimalType + "(16,6) default 0," +
                " rash_odn "  + sDecimalType + "(16,6) default 0," +
                " rashod_source " + sDecimalType + "(16,7) default 0," +
                " up_kf "     + sDecimalType + "(16,7) default 0," +
                " kf307 "     + sDecimalType + "(15,7) default 0," +
                " kod_info  integer  default 0," +
                " is_device integer  default 0 " +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); conn_db.Close(); return false; }

            // расход ХВС по ИПУ
            ret = ExecSQL(conn_db,
                " Insert into ttt_ans_dop69 (nzp_dom,nzp_kvar,nzp_serv,stek,dat_s,dat_po,pr_rash,is_device,rashod,kf307,kod_info,rash_odn,rash_kv,rash_kv_g,rashod_source,up_kf) " +
                " Select nzp_dom,nzp_kvar, 6, 3, min(dat_s), max(dat_po), 1, max(cnt_stage), max(rashod), max(kf307), max(kod_info), max(dop87), max(val1+val2), max(val1_g+val2)" +
                " , max(val1+val2), 1 " +
                " from " + rashod.counters_xx +
                " Where nzp_type = 3 and stek=3 and nzp_serv=6 and cnt_stage in (1,9) and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                " group by 1,2 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); conn_db.Close(); return false; }

            // расход ХВС по ОДПУ для нормативных л/с
            ret = ExecSQL(conn_db,
                " Insert into ttt_ans_dop69 (nzp_dom,nzp_kvar,nzp_serv,stek,dat_s,dat_po,pr_rash,is_device,rashod,kf307,kod_info,rash_odn,rash_kv,rash_kv_g,rashod_source,up_kf) " +
                " Select nzp_dom,nzp_kvar, 6, 3, min(dat_s), max(dat_po), 1, 0, max(rashod), max(kf307), max(kod_info), max(dop87), max(val1), max(val1_g)" +
                " , max(val1_source), max(up_kf)" +
                " from " + rashod.counters_xx +
                " Where nzp_type = 3 and stek=3 and nzp_serv=6 and cnt_stage=0 and kod_info>0 and not(kod_info in (21,22,23,26,27)) and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                " group by 1,2 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); conn_db.Close(); return false; }

            ExecSQL(conn_db, " Create index ix_ttt_ans_dop69 on ttt_ans_dop69 (nzp_kvar) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_ans_dop69 ", true);

            ret = ExecSQL(conn_db,
                " Insert into ttt_ans_rsh69 (nzp_dom,nzp_kvar,nzp_serv,stek,dat_s,dat_po,pr_rash,is_device,rashod,kf307,kod_info,rash_odn,rash_kv,rash_kv_g,rashod_source,up_kf) " +
                " Select nzp_dom,nzp_kvar,nzp_serv,stek,dat_s,dat_po,pr_rash,is_device,rashod,kf307,kod_info,rash_odn,rash_kv,rash_kv_g,rashod_source,up_kf " +
                " from ttt_ans_dop69 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); conn_db.Close(); return false; }

            ret = ExecSQL(conn_db,
                " Insert into ttt_ans_rsh69 (nzp_dom,nzp_kvar,nzp_serv,stek,dat_s,dat_po,pr_rash,is_device,rashod,kf307,kod_info,rash_odn,rash_kv,rash_kv_g,rashod_source,up_kf) " +
                " Select nzp_dom,nzp_kvar, 6, 20, dat_s, dat_po, 1, max(cnt_stage), max(rashod), max(kf307), max(kod_info), max(dop87), max(val1+val2), max(val1_g+val2)" +
                " , max(case when cnt_stage in (1,9) then val1+val2 else val1_source + val2 end), max(case when cnt_stage in (1,9) then 1 else up_kf end)" +
                " from " + rashod.counters_xx +
                " Where nzp_type = 3 and stek=20 and nzp_serv=6 " +
                " and exists (select 1 from ttt_ans_dop69 a where a.nzp_kvar=" + rashod.counters_xx + ".nzp_kvar) " +
                " group by 1,2,5,6 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); conn_db.Close(); return false; }

            ExecSQL(conn_db, " Drop table ttt_ans_dop69 ", false);
            ret = ExecSQL(conn_db,
                " Create temp table ttt_ans_dop69 (" +
                " nzp_dom  integer," +
                " nzp_kvar integer," +
                " nzp_serv integer," +
                " stek     integer," +
                " dat_s  date not null, " +
                " dat_po date not null, " +
                " pr_rash  integer default 0," +
                " rashod   "  + sDecimalType + "(16,6) default 0," +
                " rash_kv  "  + sDecimalType + "(16,6) default 0," +
                " rash_kv_g " + sDecimalType + "(16,6) default 0," +
                " rash_odn "  + sDecimalType + "(16,6) default 0," +
                " rashod_source " + sDecimalType + "(16,7) default 0," +
                " up_kf "     + sDecimalType + "(16,7) default 0," +
                " kf307 "     + sDecimalType + "(15,7) default 0," +
                " kod_info  integer  default 0," +
                " is_device integer  default 0 " +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); conn_db.Close(); return false; }

            // расход ГВС по ИПУ
            ret = ExecSQL(conn_db,
                " Insert into ttt_ans_dop69 (nzp_dom,nzp_kvar,nzp_serv,stek,dat_s,dat_po,pr_rash,is_device,rashod,kf307,kod_info,rash_odn,rash_kv,rash_kv_g,rashod_source,up_kf) " +
                " Select nzp_dom,nzp_kvar, 9, 3, max(dat_s), max(dat_po), 3, max(cnt_stage), max(rashod), max(kf307), max(kod_info), max(dop87), max(val1+val2), max(val1_g+val2)" +
                " , max(val1+val2), 1 " +
                " from " + rashod.counters_xx +
                " Where nzp_type = 3 and stek=3 and nzp_serv=9 and cnt_stage in (1,9) and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                " group by 1,2 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); conn_db.Close(); return false; }

            // расход ГВС по ДПУ для нормативных л/с
            ret = ExecSQL(conn_db,
                " Insert into ttt_ans_dop69 (nzp_dom,nzp_kvar,nzp_serv,stek,dat_s,dat_po,pr_rash,is_device,rashod,kf307,kod_info,rash_odn,rash_kv,rash_kv_g,rashod_source,up_kf) " +
                " Select nzp_dom,nzp_kvar, 9, 3, max(dat_s), max(dat_po), 3, 0, max(rashod), max(kf307), max(kod_info), max(dop87), max(val1), max(val1_g)" +
                " , max(val1_source), max(up_kf)" +
                " from " + rashod.counters_xx +
                " Where nzp_type = 3 and stek=3 and nzp_serv=9 and cnt_stage=0 and kod_info>0 and not(kod_info in (21,22,23,26,27)) and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                " group by 1,2 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); conn_db.Close(); return false; }

            ExecSQL(conn_db, " Create index ix_ttt_ans_dop69 on ttt_ans_dop69 (nzp_kvar) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_ans_dop69 ", true);

            ret = ExecSQL(conn_db,
                " Insert into ttt_ans_rsh69 (nzp_dom,nzp_kvar,nzp_serv,stek,dat_s,dat_po,pr_rash,is_device,rashod,kf307,kod_info,rash_odn,rash_kv,rash_kv_g,rashod_source,up_kf) " +
                " Select nzp_dom,nzp_kvar,nzp_serv,stek,dat_s,dat_po,pr_rash,is_device,rashod,kf307,kod_info,rash_odn,rash_kv,rash_kv_g,rashod_source,up_kf " +
                " from ttt_ans_dop69 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); conn_db.Close(); return false; }

            ret = ExecSQL(conn_db,
                " Insert into ttt_ans_rsh69 (nzp_dom,nzp_kvar,nzp_serv,stek,dat_s,dat_po,pr_rash,is_device,rashod,kf307,kod_info,rash_odn,rash_kv,rash_kv_g,rashod_source,up_kf) " +
                " Select nzp_dom,nzp_kvar, 9, 20, dat_s, dat_po, 3, max(cnt_stage), max(rashod), max(kf307), max(kod_info), max(dop87), max(val1+val2), max(val1_g+val2)" +
                " , max(case when cnt_stage in (1,9) then val1+val2 else val1_source + val2 end), max(case when cnt_stage in (1,9) then 1 else up_kf end)" +
                " from " + rashod.counters_xx +
                " Where nzp_type = 3 and stek=20 and nzp_serv=9 " +
                " and exists (select 1 from ttt_ans_dop69 a where a.nzp_kvar=" + rashod.counters_xx + ".nzp_kvar) " +
                " group by 1,2,5,6 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); conn_db.Close(); return false; }

            ExecSQL(conn_db, " Create index ix1_ttt_ans_rsh69 on ttt_ans_rsh69 (nzp_dom,nzp_kvar,stek,dat_s,dat_po) ", true);
            ExecSQL(conn_db, " Create index ix2_ttt_ans_rsh69 on ttt_ans_rsh69 (nzp_serv) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_ans_rsh69 ", true);

            ExecSQL(conn_db, " Drop table ttt_ans_rsh7 ", false);

            ret = ExecSQL(conn_db,
                " Create temp table ttt_ans_rsh7 (" +
                " nzp_dom  integer," +
                " nzp_kvar integer," +
                " stek     integer," +
                " dat_s  date not null, " +
                " dat_po date not null, " +
                " pr_rash  integer default 0," +
                " rsh_hv   " + sDecimalType + "(16,6) default 0," +
                " rsh_gv   " + sDecimalType + "(16,6) default 0," +
                " rsh_kv_hv  " + sDecimalType + "(16,6) default 0," +
                " rsh_kv_gv  " + sDecimalType + "(16,6) default 0," +
                " rsh_kv_hv_g  " + sDecimalType + "(16,6) default 0," +
                " rsh_kv_gv_g  " + sDecimalType + "(16,6) default 0," +
                " rsh_odn_hv " + sDecimalType + "(16,6) default 0," +
                " rsh_odn_gv " + sDecimalType + "(16,6) default 0," +
                " rashod_source " + sDecimalType + "(16,7) default 0," +
                " up_kf " + sDecimalType + "(16,7) default 0," +
                " up_kf_hvs " + sDecimalType + "(16,7) default 1," +
                " up_kf_gvs " + sDecimalType + "(16,7) default 1," +
                " rashod_source_hv " + sDecimalType + "(16,7) default 0," +
                " up_kf_hv " + sDecimalType + "(16,7) default 0," +
                " rashod_source_gv " + sDecimalType + "(16,7) default 0," +
                " up_kf_gv " + sDecimalType + "(16,7) default 0," +
                " kf307hv  " + sDecimalType + "(15,7) default 1," +
                " kf307gv  " + sDecimalType + "(15,7) default 1," +
                " kod_info_hv integer  default 0," +
                " kod_info_gv integer  default 0," +
                " kod_info    integer  default 0," +
                " rsh_odn_minus " + sDecimalType + "(16,6) default 0," +
                " is_device integer  default 0," +
                " is_device_hv integer  default 0," +
                " is_f339   integer  default 0," +
                " is_f391   integer  default 0," +
                " is_odn    integer  default 1" +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); conn_db.Close(); return false; }

            // расход ХВС и ГВС по видам для КАН
            ret = ExecSQL(conn_db,
                " Insert into ttt_ans_rsh7 (nzp_dom,nzp_kvar,stek,dat_s,dat_po,pr_rash,rsh_hv,rsh_odn_hv,kf307hv,rsh_gv,rsh_odn_gv,kf307gv,kod_info_hv,kod_info_gv," +
                                          " is_device,is_device_hv,rsh_kv_hv,rsh_kv_gv,rsh_kv_hv_g,rsh_kv_gv_g,rashod_source_hv,up_kf_hv,rashod_source_gv,up_kf_gv) " +
                " Select nzp_dom, nzp_kvar, stek, dat_s, dat_po, sum(pr_rash)," +
                " sum(case when nzp_serv=6 then rashod else 0 end), sum(case when nzp_serv=6 then rash_odn else 0 end), sum(case when nzp_serv=6 then kf307 else 0 end)," +
                " sum(case when nzp_serv=9 then rashod else 0 end), sum(case when nzp_serv=9 then rash_odn else 0 end), sum(case when nzp_serv=9 then kf307 else 0 end)," +
                " max(case when nzp_serv=6 then kod_info else 0 end),max(case when nzp_serv=9 then kod_info else 0 end)," +
                " max(is_device),max(case when nzp_serv=6 then is_device else 0 end)," +
                " max(case when nzp_serv=6 then rash_kv else 0 end),max(case when nzp_serv=9 then rash_kv else 0 end)" +
                ",max(case when nzp_serv=6 then rash_kv_g else 0 end),max(case when nzp_serv=9 then rash_kv_g else 0 end)" +
                ",sum(case when nzp_serv=6 then rashod_source else 0 end),max(case when nzp_serv=6 then up_kf else 0 end)" +
                ",sum(case when nzp_serv=9 then rashod_source else 0 end),max(case when nzp_serv=9 then up_kf else 0 end)" +
                " from ttt_ans_rsh69" +
                " group by 1,2,3,4,5 "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); conn_db.Close(); return false; }

            ExecSQL(conn_db, " Create index ix0_ttt_ans_rsh7 on ttt_ans_rsh7 (nzp_kvar, stek, dat_s, dat_po) ", true);
            ExecSQL(conn_db, " Create index ix1_ttt_ans_rsh7 on ttt_ans_rsh7 (nzp_dom) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_ans_rsh7 ", true);

            // 1223,'Пост.344 ДОМ-Для канализации отключить отрицательный ОДН ХВС/ГВС','bool', 2
            ret = ExecSQL(conn_db,
                " update ttt_ans_rsh7 set is_odn=0" +
                " where exists (select 1 from ttt_prm_2 p where p.nzp=ttt_ans_rsh7.nzp_dom and p.nzp_prm = 1223) "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            //

            // добавить нормативы ХВ или ГВ если был КПУ по одной из услуг
            ret = ExecSQL(conn_db,
                " update ttt_ans_rsh7 set (rsh_gv,rsh_odn_gv,rsh_kv_gv,kod_info_gv,rsh_kv_gv_g,rashod_source_gv,up_kf_gv)=" +
                "  (( select max(s.rashod) from " + rashod.counters_xx + " s" +
                " where ttt_ans_rsh7.nzp_kvar=s.nzp_kvar and s.nzp_serv=9 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                "   and s.nzp_type=3 and s.stek=ttt_ans_rsh7.stek and s.dat_s<=ttt_ans_rsh7.dat_po and s.dat_po>=ttt_ans_rsh7.dat_s)," +
                "( select max(s.dop87) from " + rashod.counters_xx + " s" +
                " where ttt_ans_rsh7.nzp_kvar=s.nzp_kvar and s.nzp_serv=9 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                "   and s.nzp_type=3 and s.stek=ttt_ans_rsh7.stek and s.dat_s<=ttt_ans_rsh7.dat_po and s.dat_po>=ttt_ans_rsh7.dat_s )," +
                "( select max(case when s.cnt_stage=0 then s.val1 else s.val2 end) from " + rashod.counters_xx + " s" +
                " where ttt_ans_rsh7.nzp_kvar=s.nzp_kvar and s.nzp_serv=9 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                "   and s.nzp_type=3 and s.stek=ttt_ans_rsh7.stek and s.dat_s<=ttt_ans_rsh7.dat_po and s.dat_po>=ttt_ans_rsh7.dat_s )," +
                "( select max(s.kod_info) from " + rashod.counters_xx + " s" +
                " where ttt_ans_rsh7.nzp_kvar=s.nzp_kvar and s.nzp_serv=9 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                "   and s.nzp_type=3 and s.stek=ttt_ans_rsh7.stek and s.dat_s<=ttt_ans_rsh7.dat_po and s.dat_po>=ttt_ans_rsh7.dat_s )," +
                "( select max(case when s.cnt_stage=0 then s.val1_g else s.val2 end) from " + rashod.counters_xx + " s" +
                " where ttt_ans_rsh7.nzp_kvar=s.nzp_kvar and s.nzp_serv=9 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                "   and s.nzp_type=3 and s.stek=ttt_ans_rsh7.stek and s.dat_s<=ttt_ans_rsh7.dat_po and s.dat_po>=ttt_ans_rsh7.dat_s )," +
                "( select max(case when s.cnt_stage=0 then s.val1_source else s.rashod end) from " + rashod.counters_xx + " s" +
                " where ttt_ans_rsh7.nzp_kvar=s.nzp_kvar and s.nzp_serv=9 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                "   and s.nzp_type=3 and s.stek=ttt_ans_rsh7.stek and s.dat_s<=ttt_ans_rsh7.dat_po and s.dat_po>=ttt_ans_rsh7.dat_s )," +
                "( select max(case when s.cnt_stage=0 then s.up_kf else 1 end) from " + rashod.counters_xx + " s" +
                " where ttt_ans_rsh7.nzp_kvar=s.nzp_kvar and s.nzp_serv=9 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                "   and s.nzp_type=3 and s.stek=ttt_ans_rsh7.stek and s.dat_s<=ttt_ans_rsh7.dat_po and s.dat_po>=ttt_ans_rsh7.dat_s ))" +
                " where pr_rash=1" +
                " and exists ( select 1 from " + rashod.counters_xx + " s" +
                " where ttt_ans_rsh7.nzp_kvar=s.nzp_kvar and s.nzp_serv=9 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                "   and s.nzp_type=3 and s.stek=ttt_ans_rsh7.stek and s.dat_s<=ttt_ans_rsh7.dat_po and s.dat_po>=ttt_ans_rsh7.dat_s )"
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); conn_db.Close(); return false; }

            ret = ExecSQL(conn_db,
                " update ttt_ans_rsh7 set (rsh_hv,rsh_odn_hv,rsh_kv_hv,kod_info_hv,rsh_kv_hv_g,rashod_source_hv,up_kf_hv)=" +
                " (( select max(s.rashod) from " + rashod.counters_xx + " s" +
                " where ttt_ans_rsh7.nzp_kvar=s.nzp_kvar and s.nzp_serv=6 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                "   and s.nzp_type=3 and s.stek=ttt_ans_rsh7.stek and s.dat_s<=ttt_ans_rsh7.dat_po and s.dat_po>=ttt_ans_rsh7.dat_s )," +
                "( select max(s.dop87) from "    + rashod.counters_xx + " s" +
                " where ttt_ans_rsh7.nzp_kvar=s.nzp_kvar and s.nzp_serv=6 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                "   and s.nzp_type=3 and s.stek=ttt_ans_rsh7.stek and s.dat_s<=ttt_ans_rsh7.dat_po and s.dat_po>=ttt_ans_rsh7.dat_s )," +
                "( select max(case when s.cnt_stage=0 then s.val1 else s.val2 end) from " + rashod.counters_xx + " s" +
                " where ttt_ans_rsh7.nzp_kvar=s.nzp_kvar and s.nzp_serv=6 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                "   and s.nzp_type=3 and s.stek=ttt_ans_rsh7.stek and s.dat_s<=ttt_ans_rsh7.dat_po and s.dat_po>=ttt_ans_rsh7.dat_s )," +
                "( select max(s.kod_info) from " + rashod.counters_xx + " s" +
                " where ttt_ans_rsh7.nzp_kvar=s.nzp_kvar and s.nzp_serv=6 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                "   and s.nzp_type=3 and s.stek=ttt_ans_rsh7.stek and s.dat_s<=ttt_ans_rsh7.dat_po and s.dat_po>=ttt_ans_rsh7.dat_s )," +
                "( select max(case when s.cnt_stage=0 then s.val1_g else s.val2 end) from " + rashod.counters_xx + " s" +
                " where ttt_ans_rsh7.nzp_kvar=s.nzp_kvar and s.nzp_serv=6 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                "   and s.nzp_type=3 and s.stek=ttt_ans_rsh7.stek and s.dat_s<=ttt_ans_rsh7.dat_po and s.dat_po>=ttt_ans_rsh7.dat_s )," +
                "( select max(case when s.cnt_stage=0 then s.val1_source else s.rashod end) from " + rashod.counters_xx + " s" +
                " where ttt_ans_rsh7.nzp_kvar=s.nzp_kvar and s.nzp_serv=6 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                "   and s.nzp_type=3 and s.stek=ttt_ans_rsh7.stek and s.dat_s<=ttt_ans_rsh7.dat_po and s.dat_po>=ttt_ans_rsh7.dat_s )," +
                "( select max(case when s.cnt_stage=0 then s.up_kf else 1 end) from " + rashod.counters_xx + " s" +
                " where ttt_ans_rsh7.nzp_kvar=s.nzp_kvar and s.nzp_serv=6 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                "   and s.nzp_type=3 and s.stek=ttt_ans_rsh7.stek and s.dat_s<=ttt_ans_rsh7.dat_po and s.dat_po>=ttt_ans_rsh7.dat_s ))" +
                " where pr_rash=3" +
                " and exists ( select 1 from " + rashod.counters_xx + " s" +
                " where ttt_ans_rsh7.nzp_kvar=s.nzp_kvar and s.nzp_serv=6 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                "   and s.nzp_type=3 and s.stek=ttt_ans_rsh7.stek and s.dat_s<=ttt_ans_rsh7.dat_po and s.dat_po>=ttt_ans_rsh7.dat_s )"
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); conn_db.Close(); return false; }

            //КАН только по ХВ
            ret = ExecSQL(conn_db,
                " Update ttt_ans_rsh7" +
                " Set is_f339 = 1" +
                " Where exists (select 1 from t_is339 t where t.nzp_kvar=ttt_ans_rsh7.nzp_kvar) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); conn_db.Close(); return false; }

            //расход КАН оставляем полученнный по нормативу не перетирая расходом ГВС + ХВС 
            ret = ExecSQL(conn_db,
                " Update ttt_ans_rsh7" +
                " Set is_f391 = 1" +
                " Where exists (select 1 from t_is391 t where t.nzp_kvar=ttt_ans_rsh7.nzp_kvar) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); conn_db.Close(); return false; }

            // вывести расход КАН по л/с
            ret = ExecSQL(conn_db,
                " update ttt_ans_rsh7 set" +
                " rsh_odn_minus = " +
                " case when rsh_odn_hv<0 then rsh_odn_hv else 0 end" +
                " + " +
                " case when is_f339 = 1 then 0 else " +
                " case when rsh_odn_gv<0 then rsh_odn_gv else 0 end" +
                " end "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); conn_db.Close(); return false; }

            ret = ExecSQL(conn_db,
                " update ttt_ans_rsh7 set" +
                " rsh_hv    = case when is_odn=0 then rsh_kv_hv else (case when kod_info_hv in (31,32,33,36) and rsh_hv<0 then 0 else rsh_hv end) end," +
                " rsh_odn_hv= case when kod_info_hv in (31,32,33,36) then 0 else rsh_odn_hv end," +
                " rsh_kv_hv = case when is_odn=0 then rsh_kv_hv else (case when kod_info_hv in (31,32,33,36) then (case when rsh_hv<0 then 0 else rsh_hv end) else rsh_kv_hv end) end," +
                " rsh_kv_hv_g = case when is_odn=0 then rsh_kv_hv_g else (case when kod_info_hv in (31,32,33,36) then (case when rsh_kv_hv_g<0 then 0 else rsh_kv_hv_g end) else rsh_kv_hv_g end) end," +
                " kf307hv = case when kf307hv>0 then kf307hv else 0 end," +

                " rsh_gv    = case when is_odn=0 then rsh_kv_gv else (case when kod_info_gv in (31,32,33,36) and rsh_gv<0 then 0 else rsh_gv end) end," +
                " rsh_odn_gv= case when kod_info_gv in (31,32,33,36) then 0 else rsh_odn_gv end," +
                " rsh_kv_gv = case when is_odn=0 then rsh_kv_gv else (case when kod_info_gv in (31,32,33,36) then (case when rsh_gv<0 then 0 else rsh_gv end) else rsh_kv_gv end) end," +
                " rsh_kv_gv_g = case when is_odn=0 then rsh_kv_gv_g else (case when kod_info_gv in (31,32,33,36) then (case when rsh_kv_gv_g<0 then 0 else rsh_kv_gv_g end) else rsh_kv_gv_g end) end, " +
                " kf307gv   = case when kf307gv>0 then kf307gv else 0 end "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); conn_db.Close(); return false; }

            // повышающий коэффициент по ХВС
            ret = ExecSQL(conn_db,
                " update ttt_ans_rsh7 set " +
                " up_kf_hvs = " +
                  " (Select " + sNvlWord + "(p.val_prm,'0')" + sConvToNum + " from ttt_prm_2 p" +
                  "  where p.nzp_prm=1557 and p.nzp=ttt_ans_rsh7.nzp_dom) " +
                " Where pr_rash=3 and exists (Select 1 from ttt_prm_2 p" +
                  "  where p.nzp_prm=1557 and p.nzp=ttt_ans_rsh7.nzp_dom and " + sNvlWord + "(p.val_prm,'0')" + sConvToNum + ">1.00001) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // повышающий коэффициент по ГВС
            ret = ExecSQL(conn_db,
                " update ttt_ans_rsh7 set " +
                " up_kf_gvs = " +
                  " (Select " + sNvlWord + "(p.val_prm,'0')" + sConvToNum + " from ttt_prm_2 p" +
                  "  where p.nzp_prm=1558 and p.nzp=ttt_ans_rsh7.nzp_dom) " +
                " Where pr_rash=1 and exists (Select 1 from ttt_prm_2 p" +
                  "  where p.nzp_prm=1558 and p.nzp=ttt_ans_rsh7.nzp_dom and " + sNvlWord + "(p.val_prm,'0')" + sConvToNum + ">1.00001) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); return false; }

            // записать расход КАН по л/с
            ExecSQL(conn_db, " Drop table ttt_ans_prepared ", false);

            ret = ExecSQL(conn_db,
                " Create temp table ttt_ans_prepared (" +
                " nzp_kvar integer," +
                " stek     integer," +
                " dat_s  date not null, " +
                " dat_po date not null, " +
                " kod_info_p    integer  default 0," +
                " rashod_p  " + sDecimalType + "(16,6) default 0," +
                " dop87_p  " + sDecimalType + "(16,6) default 0," +
                " val1_p  " + sDecimalType + "(16,6) default 0," +
                " val4_p  " + sDecimalType + "(16,6) default 0," +
                " kf307_p  " + sDecimalType + "(15,7) default 1," +
                " pu7kw_p  " + sDecimalType + "(16,6) default 0," +
                " val1_g_p  " + sDecimalType + "(16,6) default 0, " +
                " is_device_p integer  default 0," +
                " rashod_source_p " + sDecimalType + "(16,7) default 0," +
                " up_kf_p " + sDecimalType + "(16,7) default 0 " +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); conn_db.Close(); return false; }


            var sql =
                " insert into ttt_ans_prepared" +
                " (nzp_kvar,stek,dat_s,dat_po,kod_info_p," +
                 " is_device_p, up_kf_p, rashod_source_p, rashod_p, dop87_p," +
                 " val1_p, val4_p, kf307_p, pu7kw_p, val1_g_p)" +
                " select a.nzp_kvar,a.stek,a.dat_s,a.dat_po," +
                // kod_info
                " max(" +
                " case when kod_info_hv in (21,22,23,26)" +
                " then kod_info_hv" +
                " else " +
                " case when is_f339 = 0 and (kod_info_gv in (21,22,23,26))" +
                "      then kod_info_gv else 69" +
                " end " +
                " end), " +

                // is_device
                " max(" +
                "  case when is_f339 = 1" +
                "       then is_device_hv" +
                "       else is_device" +
                "  end), " +
                // up_kf
                " max(" +
                "  case when is_f391 = 1" +
                "       then " +
                "        (case when pr_rash=1 then up_kf_gvs " +//расход по ПУ ХВ + норм. КАН ГВ c учетом ОДН
                "              when pr_rash=3 then up_kf_hvs " +//расход по норм. КАН ХВ + ПУ ГВ c учетом ОДН
                "              else 1 " +
                "         end)" +
                "       else " +
                "        (case when is_f339=1 " +
                "             then (case when is_device_hv=0 and kod_info_hv=0 then up_kf_hvs else 1 end) " +
                "             else (case when up_kf_hv>up_kf_gv then up_kf_hv else up_kf_gv end) " +
                "         end) " +
                "  end), " +
                //,,,
                // rashod_source
                " max(" +
                "  case when is_f391 = 1" +
                "       then " +
                "        (case when pr_rash=1 then rsh_hv+(val1_source-val4_source)+rsh_odn_gv " +//расход по ПУ ХВ + норм. КАН ГВ c учетом ОДН
                "              when pr_rash=3 then val4_source+rsh_gv+rsh_odn_hv " +//расход по норм. КАН ХВ + ПУ ГВ c учетом ОДН
                "              else rsh_hv + rsh_gv " +
                "         end)" +
                "       else " +
                "        (case when is_f339=1 " +
                "             then (case when is_device_hv=0 and kod_info_hv=0 then val4_source else rsh_hv end) " +
                "             else rashod_source_hv + rashod_source_gv " +
                "         end) " +
                "  end), " +
                // rashod
                " max(" +
                "  case when is_f391 = 1" +
                "       then " +
                "        (case when pr_rash=1 then rsh_hv+(val1-val4)*up_kf_gvs+rsh_odn_gv " +//расход по ПУ ХВ + норм. КАН ГВ c учетом ОДН
                "              when pr_rash=3 then val4*up_kf_hvs+rsh_gv+rsh_odn_hv " +//расход по норм. КАН ХВ + ПУ ГВ c учетом ОДН
                "              else rsh_hv + rsh_gv " +
                "         end)" +
                "       else " +
                "        (case when is_f339=1 " +
                "             then (case when is_device_hv=0 and kod_info_hv=0 then val4*up_kf_hvs else rsh_hv end) " +
                "             else rsh_hv + rsh_gv " +
                "         end) " +
                "  end), " +
                //,,,

                // dop87
                " max(" +
                "  case when is_f339 = 1" +
                " then (case when is_device_hv=0 and kod_info_hv=0 then 0 else rsh_odn_hv end) " +
                " else rsh_odn_hv + rsh_odn_gv " +
                "  end), " +
                "  max(" +
                // val1
                "  case when is_f391 = 1" +
                "       then " +
                "        (case when pr_rash=1 then rsh_kv_hv+(val1-val4) " + //расход по ПУ ХВ + норм. КАН ГВ 
                "              when pr_rash=3 then val4 + rsh_kv_gv  " + //расход по норм. КАН ХВ + ПУ ГВ
                "              else rsh_kv_hv + rsh_kv_gv " +
                "         end)" +
                "       else  " +
                "        (case when is_f339=1 " +
                "              then (case when is_device_hv=0 and kod_info_hv=0 then val1 else rsh_kv_hv end) " +
                "              else rsh_kv_hv + rsh_kv_gv " +
                "         end) " +
                " end), " +
                // val4
                " max(" +
                "  case when is_f391 = 1" +
                "       then " +
                "        (case when pr_rash=3 then val4 " +
                "              else rsh_kv_hv " +
                "         end)" +
                "       else  " +
                "        (case when is_f339=1 then (case when is_device_hv=0 and kod_info_hv=0 then rashod else rsh_kv_hv end) " +
                "              else rsh_kv_hv" +
                "         end) " +
                "  end), " +
                // kf307
                "  max(" +
                "  case when is_f339 = 1" +
                "       then (case when is_device_hv=0 and kod_info_hv=0 then 0 else kf307hv end)" +
                "       else kf307hv + kf307gv" +
                "  end), " +
                // pu7kw
                " max(rsh_odn_minus), " +
                // val1_g
                " max(" +
                "  case when is_f391 = 1" +
                "       then " +
                "        (case when pr_rash=3 then rsh_kv_hv_g+(val1_g-val4) " + //расход по ПУ ХВ + норм. КАН ГВ 
                "              when pr_rash=1 then val4 + rsh_kv_gv_g  " + //расход по норм. КАН ХВ + ПУ ГВ
                "              else rsh_kv_hv_g + rsh_kv_gv_g " +
                "         end)" +
                "       else  " +
                "        (case when is_f339=1 then (case when is_device_hv=0 and kod_info_hv=0 then val1_g else rsh_kv_hv_g end) " +
                "              else rsh_kv_hv_g + rsh_kv_gv_g " +
                "         end) " +
                "  end) " +
                " FROM " + rashod.counters_xx + " c, ttt_ans_rsh7 a " +
                " Where c.nzp_kvar = a.nzp_kvar " +
                " and c.stek     = a.stek " +
                " and c.dat_s   <= a.dat_po " +
                " and c.dat_po  >= a.dat_s " +
                " and c.nzp_type = 3 and c.stek in (3,20) and c.nzp_serv = 7 and c.cnt_stage=0 " +
                " and c." + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                " group by 1,2,3,4 ";

            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); conn_db.Close(); return false; }
            ExecSQL(conn_db, " Create index ix0_ttt_ans_prepared on ttt_ans_prepared (nzp_kvar, stek, dat_s, dat_po) ", true);

            sql = " UPDATE " + rashod.counters_xx +
                  " SET kod_info=" + sNvlWord + "(kod_info_p,0)," +
                  " up_kf=" + sNvlWord + "(up_kf_p,0), " +
                  " val1_source=" + sNvlWord + "(rashod_source_p,0), " +
                  " rashod=" + sNvlWord + "(rashod_p,0), " +
                  " dop87=" + sNvlWord + "(dop87_p,0), " +
                  " val1=" + sNvlWord + "(val1_p,0), " +
                  " val4=" + sNvlWord + "(val4_p,0), " +
                  " kf307=" + sNvlWord + "(kf307_p,0), " +
                  " pu7kw=" + sNvlWord + "(pu7kw_p,0), " +
                  " val1_g=" + sNvlWord + "(val1_g_p,0) " +
                  " FROM ttt_ans_prepared a" +
                  " WHERE " + rashod.counters_xx + ".nzp_kvar = a.nzp_kvar " +
                  " and " + rashod.counters_xx + ".stek     = a.stek " +
                  " and " + rashod.counters_xx + ".dat_s   <= a.dat_po " +
                  " and " + rashod.counters_xx + ".dat_po  >= a.dat_s " +
                  " and " + rashod.counters_xx + ".nzp_type = 3" +
                  " and " + rashod.counters_xx + ".nzp_serv = 7 " +
                  " and " + rashod.counters_xx + ".cnt_stage=0 " +
                  " and " + rashod.where_dom + rashod.paramcalc.per_dat_charge;
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); conn_db.Close(); return false; }

            ExecSQL(conn_db, " Drop table ttt_ans_prepared ", false);

            #region old update
            /*
            // записать расход КАН по л/с
            ret = ExecSQL(conn_db,
                " Update " + rashod.counters_xx +
                " Set (kod_info, rashod, dop87, val1, val4, kf307, pu7kw, val1_g) = ( " +
#if PG
                " (Select max(" +
                // kod_info
                  " case when kod_info_hv in (21,22,23,26)" +
                  " then kod_info_hv" +
                  " else " +
                    " case when is_f339 = 0 and (kod_info_gv in (21,22,23,26))" +
                    "      then kod_info_gv else 69" +
                    " end " +
                  " end) " +
                " From ttt_ans_rsh7 a " +
                " Where " + rashod.counters_xx + ".nzp_kvar = a.nzp_kvar " +
                  " and " + rashod.counters_xx + ".stek     = a.stek " +
                  " and " + rashod.counters_xx + ".dat_s   <= a.dat_po " +
                  " and " + rashod.counters_xx + ".dat_po  >= a.dat_s " +
                " )," +
                " (Select max(" +
                // rashod
                "  case when is_f391 = 1" +
                "       then " +
                "        (case when pr_rash=1 then rsh_hv+(val1-val4)+rsh_odn_gv " +//расход по ПУ ХВ + норм. КАН ГВ c учетом ОДН
                "              when pr_rash=3 then val4+rsh_gv+rsh_odn_hv " +//расход по норм. КАН ХВ + ПУ ГВ c учетом ОДН
                "              else rsh_hv + rsh_gv " +
                "         end)" +
                "       else " +
                "        (case when is_f339=1 then " +
                "            (case when is_device_hv=0 and kod_info_hv=0 then rashod else rsh_hv end) " +
                "             else rsh_hv + rsh_gv " +
                "         end) " +
                "  end) " +
                " From ttt_ans_rsh7 a " +
                " Where " + rashod.counters_xx + ".nzp_kvar = a.nzp_kvar " +
                  " and " + rashod.counters_xx + ".stek     = a.stek " +
                  " and " + rashod.counters_xx + ".dat_s   <= a.dat_po " +
                  " and " + rashod.counters_xx + ".dat_po  >= a.dat_s " +
                " )," +
                " (Select max(" +
                // dop87
                "  case when is_f339 = 1" +
                      " then (case when is_device_hv=0 and kod_info_hv=0 then 0 else rsh_odn_hv end) " +
                      " else rsh_odn_hv + rsh_odn_gv " +
                "  end) " +
                " From ttt_ans_rsh7 a " +
                " Where " + rashod.counters_xx + ".nzp_kvar = a.nzp_kvar " +
                  " and " + rashod.counters_xx + ".stek     = a.stek " +
                  " and " + rashod.counters_xx + ".dat_s   <= a.dat_po " +
                  " and " + rashod.counters_xx + ".dat_po  >= a.dat_s " +
                " )," +
                " (Select max(" +
                // val1
                "  case when is_f391 = 1" +
                "       then " +
                "        (case when pr_rash=1 then rsh_kv_hv+(val1-val4) " +//расход по ПУ ХВ + норм. КАН ГВ 
                "              when pr_rash=3 then val4 + rsh_kv_gv  " +//расход по норм. КАН ХВ + ПУ ГВ
                "              else rsh_kv_hv + rsh_kv_gv " +
                "         end)" +
                "       else  " +
                "        (case when is_f339=1 " +
                "              then (case when is_device_hv=0 and kod_info_hv=0 then val1 else rsh_kv_hv end) " +
                "              else rsh_kv_hv + rsh_kv_gv " +
                "         end) " +
                " end) " +
                " From ttt_ans_rsh7 a " +
                " Where " + rashod.counters_xx + ".nzp_kvar = a.nzp_kvar " +
                  " and " + rashod.counters_xx + ".stek     = a.stek " +
                  " and " + rashod.counters_xx + ".dat_s   <= a.dat_po " +
                  " and " + rashod.counters_xx + ".dat_po  >= a.dat_s " +
                " )," +
                " (Select max(" +
                // val4
                "  case when is_f391 = 1" +
                "       then " +
                "        (case when pr_rash=3 then val4 " +
                "              else rsh_kv_hv " +
                "         end)" +
                "       else  " +
                "        (case when is_f339=1 then (case when is_device_hv=0 and kod_info_hv=0 then rashod else rsh_kv_hv end) " +
                "              else rsh_kv_hv" +
                "         end) " +
                "  end) " +
                " From ttt_ans_rsh7 a " +
                " Where " + rashod.counters_xx + ".nzp_kvar = a.nzp_kvar " +
                  " and " + rashod.counters_xx + ".stek     = a.stek " +
                  " and " + rashod.counters_xx + ".dat_s   <= a.dat_po " +
                  " and " + rashod.counters_xx + ".dat_po  >= a.dat_s " +
                " )," +
                " (Select max(" +
                //"case when kf307hv>0 then kf307hv else 0 end + case when kf307gv>0 then kf307gv else 0 end" +
                // kf307
                "  case when is_f339 = 1" +
                "       then (case when is_device_hv=0 and kod_info_hv=0 then 0 else kf307hv end)" +
                "       else kf307hv + kf307gv" +
                "  end) " +
                " From ttt_ans_rsh7 a " +
                " Where " + rashod.counters_xx + ".nzp_kvar = a.nzp_kvar " +
                  " and " + rashod.counters_xx + ".stek     = a.stek " +
                  " and " + rashod.counters_xx + ".dat_s   <= a.dat_po " +
                  " and " + rashod.counters_xx + ".dat_po  >= a.dat_s " +
                " )," +
                " (Select " +
                // pu7kw
                  " max(rsh_odn_minus) " +
                " From ttt_ans_rsh7 a " +
                " Where " + rashod.counters_xx + ".nzp_kvar = a.nzp_kvar " +
                  " and " + rashod.counters_xx + ".stek     = a.stek " +
                  " and " + rashod.counters_xx + ".dat_s   <= a.dat_po " +
                  " and " + rashod.counters_xx + ".dat_po  >= a.dat_s " +
                " )," +
                " (Select max(" +
                // val1_g
                "  case when is_f391 = 1" +
                "       then " +
                "        (case when pr_rash=3 then rsh_kv_hv_g+(val1_g-val4) " +//расход по ПУ ХВ + норм. КАН ГВ 
                "              when pr_rash=1 then val4 + rsh_kv_gv_g  " +//расход по норм. КАН ХВ + ПУ ГВ
                "              else rsh_kv_hv_g + rsh_kv_gv_g " +
                "         end)" +
                "       else  " +
                "        (case when is_f339=1 then (case when is_device_hv=0 and kod_info_hv=0 then val1_g else rsh_kv_hv_g end) " +
                "              else rsh_kv_hv_g + rsh_kv_gv_g " +
                "         end) " +
                "  end) " +
                " From ttt_ans_rsh7 a " +
                " Where " + rashod.counters_xx + ".nzp_kvar = a.nzp_kvar " +
                  " and " + rashod.counters_xx + ".stek     = a.stek " +
                  " and " + rashod.counters_xx + ".dat_s   <= a.dat_po " +
                  " and " + rashod.counters_xx + ".dat_po  >= a.dat_s " +
                " )" +
#else
                " (Select" +
                // kod_info
                " case when kod_info_hv in (21,22,23,26)" +
                  " then kod_info_hv" +
                  " else " +
                    " case when is_f339 = 0 and (kod_info_gv in (21,22,23,26))" +
                    " then kod_info_gv else 69" +
                    " end " +
                  " end, " +
                // rashod
                " (case when is_f391 = 1 then " +
                "   (case when pr_rash=1 then rsh_hv+(val1-val4)+rsh_odn_gv" +//расход по ПУ ХВ + норм. КАН ГВ c учетом ОДН
                "         when pr_rash=3 then val4+rsh_gv+rsh_odn_hv " +//расход по норм. КАН ХВ + ПУ ГВ c учетом ОДН
                "      else (rsh_hv + rsh_gv) " +
                "    end)" +
                "     else " +
                "       (case when is_f339=1 then " +
                "           (case when is_device_hv=0 and kod_info_hv=0 then rashod else rsh_hv " +
                "            end) " +
                "           else (rsh_hv + rsh_gv) " +
                "        end) " +
                "  end), " +
                // dop87
                " (case when is_f339=1 then (case when is_device_hv=0 and kod_info_hv=0 then 0 else rsh_odn_hv end) else (rsh_odn_hv + rsh_odn_gv) end)," +
                // val1
                  " (case when is_f391 = 1 then " +
                "   (case when pr_rash=1 then rsh_kv_hv+(val1-val4)" +//расход по ПУ ХВ + норм. КАН ГВ 
                "         when pr_rash=3 then val4 + rsh_kv_gv  " +//расход по норм. КАН ХВ + ПУ ГВ
                "      else (rsh_kv_hv + rsh_kv_gv) " +
                "    end)" +
                "  else  " +
                "   (case when is_f339=1 then " +
                "       (case when is_device_hv=0 and kod_info_hv=0 then val1 else rsh_kv_hv " +
                "        end) " +
                "     else (rsh_kv_hv + rsh_kv_gv) " +
                "   end) " +
                " end), " +
                // val4
              " (case when is_f391 = 1 then " +
                "   (case when pr_rash=3 then val4 " +
                "      else (rsh_kv_hv) " +
                "    end)" +
                "  else  " +
                "   (case when is_f339=1 then " +
                "       (case when is_device_hv=0 and kod_info_hv=0 then rashod else rsh_kv_hv " +
                "        end) " +
                "     else rsh_kv_hv" +
                "   end) " +
                " end), " +
                // kf307
                " (case when is_f339=1 then (case when is_device_hv=0 and kod_info_hv=0 then 0 else kf307hv end) else (kf307hv + kf307gv) end)," +
                // pu7kw
                " rsh_odn_minus," +
                // val1_g
                  " (case when is_f391 = 1 then " +
                "   (case when pr_rash=3 then rsh_kv_hv_g+(val1_g-val4)" +//расход по ПУ ХВ + норм. КАН ГВ 
                "         when pr_rash=1 then val4 + rsh_kv_gv_g  " +//расход по норм. КАН ХВ + ПУ ГВ
                "      else (rsh_kv_hv_g + rsh_kv_gv_g) " +
                "    end)" +
                "  else  " +
                "   (case when is_f339=1 then " +
                "       (case when is_device_hv=0 and kod_info_hv=0 then val1_g else rsh_kv_hv_g " +
                "        end) " +
                "      else (rsh_kv_hv_g + rsh_kv_gv_g) " +
                "    end) " +
                " end) " +
                " From ttt_ans_rsh7 a " +
                " Where " + rashod.counters_xx + ".nzp_kvar = a.nzp_kvar " +
                  " and " + rashod.counters_xx + ".stek     = a.stek " +
                  " and " + rashod.counters_xx + ".dat_s   <= a.dat_po " +
                  " and " + rashod.counters_xx + ".dat_po  >= a.dat_s " +
                " ) " +
#endif
                " ) " +
                " Where nzp_type = 3 and stek in (3,20) and nzp_serv = 7 and cnt_stage=0 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                  " and exists ( Select 1 From ttt_ans_rsh7 a " +
                " Where " + rashod.counters_xx + ".nzp_kvar = a.nzp_kvar " +
                  " and " + rashod.counters_xx + ".stek     = a.stek " +
                  " and " + rashod.counters_xx + ".dat_s   <= a.dat_po " +
                  " and " + rashod.counters_xx + ".dat_po  >= a.dat_s " +
                            " ) "
                , true);
            if (!ret.result) { CalcGkuXX_CloseTmp(conn_db); conn_db.Close(); return false; }
            */
            #endregion old

            return true;
        }
        #endregion коррекция расхода по канализации по ХВС и ГВС

        #region распределенный (учтенный) расход
        public bool CalcRashodSaldo(IDbConnection conn_db, Rashod rashod, bool b_calc_kvar, out Returns ret)
        {
            ret = Utils.InitReturns();

            //----------------------------------------------------------------
            //распределенный (учтенный) расход
            //----------------------------------------------------------------
            if (!b_calc_kvar)
            {
                ExecSQL(conn_db, " Drop table ttt_aid_stat ", false);
                ret = ExecSQL(conn_db,
                    " Create temp table ttt_aid_stat " +
                    " ( nzp_dom integer, " +
                    "   nzp_serv integer, " +
                    "   rashod " + sDecimalType + "(15,7) " +
                    " ) " + sUnlogTempTable
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                ret = ExecSQL(conn_db,
                    " Insert into ttt_aid_stat (nzp_dom, nzp_serv, rashod) " +
                    " Select nzp_dom, nzp_serv, sum(rashod) as rashod " +
                    " From " + rashod.counters_xx + " a " +
                    " Where nzp_type = 3 and stek = 3 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                    " Group by 1,2 "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                ExecSQL(conn_db, " Create unique index ix_aid44_st on ttt_aid_stat (nzp_dom,nzp_serv) ", true);
                ExecSQL(conn_db, sUpdStat + " ttt_aid_stat ", true);

                string sql =
                        " Update " + rashod.counters_xx +
                        " Set dlt_calc = dlt_reval + dlt_real_charge + (" +
                                    " Select CASE when sum(rashod)>" + Constants.max_val_for_pu +
                                    " then " + Constants.max_val_for_pu + " else sum(rashod) END" +
                        " From ttt_aid_stat a " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = a.nzp_dom  " +
                                    "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv ) " +
                        " Where nzp_type in (1,2) and stek = 3 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge +
                          " and 0 < ( Select count(*) From ttt_aid_stat a " +
                                    " Where " + rashod.counters_xx + ".nzp_dom  = a.nzp_dom  " +
                                    "   and " + rashod.counters_xx + ".nzp_serv = a.nzp_serv " +
                                    " ) ";

                if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
                {
                    ret = ExecSQL(conn_db, sql, true);
                }
                else
                {
                    ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
                }
                if (!ret.result)
                { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                ExecSQL(conn_db, " Drop table ttt_aid_stat ", false);

                sql =
                        " Update " + rashod.counters_xx +
                        " Set dlt_cur = val3 - dlt_calc " + //текущая дельта
                        " Where nzp_type in (1,2) and stek = 3 and " + rashod.where_dom + rashod.paramcalc.per_dat_charge;

                if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
                {
                    ret = ExecSQL(conn_db, sql, true);
                }
                else
                {
                    ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
                }
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            }

            return true;
        }
        #endregion распределенный (учтенный) расход

        #region в целях учета дельты расходов необходимо, чтобы в counters_xx присутствовали все коммунальные услуги из charge_xx !!!
        public bool UchetRashodRest(IDbConnection conn_db, Rashod rashod, out Returns ret)
        {
            //----------------------------------------------------------------
            //в целях учета дельты расходов необходимо, чтобы в counters_xx присутствовали все коммунальные услуги из charge_xx !!!
            //----------------------------------------------------------------

            ExecSQL(conn_db, " Drop table ttt_aid_stat ", false);
            ret = ExecSQL(conn_db,
                " Create temp table ttt_aid_stat " +
                " ( nzp_dom integer, " +
                "   nzp_kvar integer, " +
                "   nzp_serv integer  " +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            //выбрать отсутствующие услуги из charge_xx, которых нет в counters_xx
            ret = ExecSQL(conn_db,
                " Insert into ttt_aid_stat (nzp_dom, nzp_kvar, nzp_serv) " +
                " Select a.nzp_dom, a.nzp_kvar, b.nzp_serv " +
                " From t_selkvar a, " + rashod.charge_xx + " b " +
                " Where a.nzp_kvar = b.nzp_kvar " +
                "   and b.nzp_serv in ( Select nzp_serv From " + rashod.paramcalc.kernel_alias + "s_counts ) " +
                "   and not exists ( Select 1 From " + rashod.counters_xx + " c " +
                            " Where c.nzp_kvar = b.nzp_kvar " +
                            "   and c.nzp_serv = b.nzp_serv " +
                            " ) " +
                " Group by 1,2,3 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            //вставить с нулями пустую строку расходов
            ret = ExecSQL(conn_db,
                  " Insert into " + rashod.counters_xx + " (nzp_dom, nzp_kvar, nzp_serv, nzp_type, stek, kod_info, dat_s,dat_po ) " +
                  " Select nzp_dom, nzp_kvar, nzp_serv, 3, 3,-961, " + rashod.paramcalc.dat_s + "," + rashod.paramcalc.dat_po +
                  " From ttt_aid_stat "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            return true;
        }
        #endregion в целях учета дельты расходов необходимо, чтобы в counters_xx присутствовали все коммунальные услуги из charge_xx !!!

        /// <summary>
        /// Скопировать нормативные расходы в поля, для сохранения исходных значений,без учета повышающего норматива
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="rashod"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public bool CopySourceVal(IDbConnection conn_db, Rashod rashod, out Returns ret, bool isOdn = false)
        {
            ret = Utils.InitReturns();
            var sql = "";
            if (isOdn)
            {
                sql = "UPDATE ttt_aid_kpu SET val_norm_odn_source=val_norm_odn ";
            }
            else
            {
                sql = "UPDATE ttt_counters_xx SET val1_source=val1, val4_source=val4 ";
            }

            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                ret.text = "Ошибка копирования исходных расходов по нормативам";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Применение повышающего коэф-та по списку услуг и соответствующих параметров
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="rashod"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public bool ApplyUpKoef(IDbConnection conn_db, Rashod rashod, out Returns ret, bool isOdn = false)
        {
            ret = Utils.InitReturns();
            if (isOdn)
            {
                //1556, 'Повышающий коэффициент по услуге отопления'
                ApplyUpKoefForServ(conn_db, rashod, 8, 1556, out ret, isOdn);
                if (!ret.result)
                    return false;
                //1557, 'Повышающий коэффициент по услуге хол.вода'
                ApplyUpKoefForServ(conn_db, rashod, 6, 1557, out ret, isOdn);
                if (!ret.result)
                    return false;
                //1558, 'Повышающий коэффициент по услуге гор.вода'
                ApplyUpKoefForServ(conn_db, rashod, 9, 1558, out ret, isOdn);
                if (!ret.result)
                    return false;
                //1559, 'Повышающий коэффициент по услуге электроснабжение'
                ApplyUpKoefForServ(conn_db, rashod, 25, 1559, out ret, isOdn);
                if (!ret.result)
                    return false;
            }
            else
            {  //1556, 'Повышающий коэффициент по услуге отопления'
                ApplyUpKoefForServ(conn_db, rashod, 8, 1556, out ret, isOdn);
                if (!ret.result)
                    return false;
                //1557, 'Повышающий коэффициент по услуге хол.вода'
                ApplyUpKoefForServ(conn_db, rashod, 6, 1557, out ret, isOdn);
                if (!ret.result)
                    return false;
                //1558, 'Повышающий коэффициент по услуге гор.вода'
                ApplyUpKoefForServ(conn_db, rashod, 9, 1558, out ret, isOdn);
                if (!ret.result)
                    return false;
                //1559, 'Повышающий коэффициент по услуге электроснабжение'
                ApplyUpKoefForServ(conn_db, rashod, 25, 1559, out ret, isOdn);
                if (!ret.result)
                    return false;

            }

            return true;
        }


        /// <summary>
        /// Применить повышающие коэффициенты для нормативов по услугам
        /// </summary>
        /// <param name="conn_db">соединение</param>
        /// <param name="rashod">параметры расчета</param>
        /// <param name="nzp_serv">номер услуги</param>
        /// <param name="nzp_prm">номер параметра для получения коэф.</param>
        /// <param name="ret"></param>
        /// <param name="isOdn">признак ОДН</param>
        /// <param name="defaultVal">значение коэф. по умолчанию - берем при отсутствии коэф.</param>
        /// <returns></returns>
        public bool ApplyUpKoefForServ(IDbConnection conn_db, Rashod rashod, int nzp_serv, int nzp_prm, out Returns ret, bool isOdn, decimal defaultVal = 1)
        {
            ret = Utils.InitReturns();
            //текущий расчетный месяц
            var prm = new CalcMonthParams();
            prm.pref = rashod.paramcalc.pref;
            var sql = "";

            ExecSQL(conn_db, " DROP TABLE up_koef ", false);
            if (isOdn)
            {
                sql =
                    " CREATE TEMP TABLE up_koef " +
                    "(nzp_dom  integer," +
                    " koef " + sDecimalType + "(14,7) default 1 " +
                    " )" + sUnlogTempTable;
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) { ret.text = "Ошибка получения повышающего коэффициента"; return false; }

                //получили список домов с этой услугой
                sql =
                    " INSERT INTO up_koef (nzp_dom) " +
                    " SELECT nzp_dom FROM ttt_aid_kpu " +
                    " WHERE nzp_serv=" + nzp_serv +
                    " GROUP BY 1";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) { ret.text = "Ошибка получения повышающего коэффициента"; return false; }

                ExecSQL(conn_db, " Create index ix_up_koef on up_koef (nzp_dom) ", false);
                ExecSQL(conn_db, sUpdStat + " up_koef ", true);

                //получаем повышающий коэффициент для каждого дома по выбранной услуге
                sql = 
                    " UPDATE up_koef SET" +
                    " koef= (SELECT max(val_prm" + sConvToNum + ")" +
                     " FROM ttt_prm_2 p WHERE p.nzp_prm=" + nzp_prm + " AND up_koef.nzp_dom=p.nzp) " +
                    " Where exists (SELECT 1" +
                     " FROM ttt_prm_2 p WHERE p.nzp_prm=" + nzp_prm + " AND up_koef.nzp_dom=p.nzp)";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) { ret.text = "Ошибка получения повышающего коэффициента"; return false; }

                if ((rashod.paramcalc.calc_yy == 2015) && (rashod.paramcalc.calc_mm == 7))
                {
                    sql =
                        " UPDATE up_koef SET koef = 1 + (koef-1) * 20.00 / 31.00 Where koef>1 ";
                    ret = ExecSQL(conn_db, sql, true);
                    if (!ret.result) { ret.text = "Ошибка получения повышающего коэффициента"; return false; }
                }

                sql = 
                    " UPDATE ttt_aid_kpu" +
                    " SET val_norm_odn=val_norm_odn*koef, up_kf=koef" +
                    " FROM up_koef u" +
                    " WHERE ttt_aid_kpu.nzp_dom=u.nzp_dom AND ttt_aid_kpu.nzp_serv=" + nzp_serv + " and ttt_aid_kpu.cnt_stage=0 ";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) { ret.text = "Ошибка получения повышающего коэффициента"; return false; }
            }
            else
            {
                sql =
                    " CREATE TEMP TABLE up_koef " +
                    "(nzp_dom   integer," +
                    " nzp_kvar  integer," +
                    " stek      integer," +
                    " koef " + sDecimalType + "(14,7) default 1," +
                    " nzp_period integer," +
                    " dat_s    DATE," +
                    " dat_po   DATE " +
                    " )" + sUnlogTempTable;
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) { ret.text = "Ошибка получения повышающего коэффициента"; return false; }

                //получили список домов с этой услугой
                sql =
                    " INSERT INTO up_koef (nzp_dom,nzp_kvar,stek,nzp_period,dat_s,dat_po) " +
                    " SELECT a.nzp_dom,a.nzp_kvar,a.stek,a.nzp_period,a.dat_s,a.dat_po " +
                    " FROM ttt_counters_xx a " +
                    " WHERE a.nzp_serv=" + nzp_serv +
                    "   and exists (SELECT 1 FROM temp_table_tarif t," + Points.Pref +"_kernel.serv_norm_koef s" +
                               " WHERE t.nzp_serv=s.nzp_serv and t.nzp_kvar=a.nzp_kvar and a.nzp_serv=s.nzp_serv_link)" +
                    " GROUP BY 1,2,3,4,5,6 ";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) { ret.text = "Ошибка получения повышающего коэффициента"; return false; }

                ExecSQL(conn_db, " Create index ix1_up_koef on up_koef (nzp_dom,dat_s,dat_po) ", false);
                ExecSQL(conn_db, " Create index ix2_up_koef on up_koef (nzp_kvar,stek,nzp_period) ", false);
                ExecSQL(conn_db, sUpdStat + " up_koef ", true);

                //получаем повышающий коэффициент для каждого дома по выбранной услуге
                sql = 
                    " UPDATE up_koef" +
                    " SET koef= (SELECT max(val_prm" + sConvToNum + ") FROM ttt_prm_2d p" +
                               " WHERE p.nzp_prm=" + nzp_prm + " AND up_koef.nzp_dom=p.nzp" +
                               "  and up_koef.dat_s<=p.dat_po and up_koef.dat_po>=p.dat_s) " +
                    " Where exists (SELECT 1 FROM ttt_prm_2d p" +
                               " WHERE p.nzp_prm=" + nzp_prm + " AND up_koef.nzp_dom=p.nzp" +
                               "  and up_koef.dat_s<=p.dat_po and up_koef.dat_po>=p.dat_s)";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) { ret.text = "Ошибка получения повышающего коэффициента"; return false; }

                sql = 
                    " UPDATE ttt_counters_xx" +
                    " SET val1=val1*koef, val1_g=val1_g*koef, val4=val4*koef, up_kf=koef" +
                    " FROM up_koef u" +
                    " WHERE ttt_counters_xx.nzp_kvar=u.nzp_kvar AND ttt_counters_xx.stek=u.stek" +
                    "   AND ttt_counters_xx.nzp_period=u.nzp_period AND ttt_counters_xx.nzp_serv=" + nzp_serv;
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) { ret.text = "Ошибка получения повышающего коэффициента"; return false; }

                ExecSQL(conn_db, " DROP TABLE ttt_ls_norm_koef ", false);
                sql =
                    " CREATE TEMP TABLE ttt_ls_norm_koef " +
                    "(nzp_kvar  integer," +
                    " up_kf "  + sDecimalType + "(16,7) default 1," +
                    " val1 "   + sDecimalType + "(16,7) default 0.00," +
                    " val1_g " + sDecimalType + "(16,7) default 0.00," +
                    " val4 "   + sDecimalType + "(16,7) default 0.00" +
                    " )" + sUnlogTempTable;
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) { ret.text = "Ошибка получения повышающего коэффициента"; return false; }

                sql =
                    " Insert into ttt_ls_norm_koef (nzp_kvar,up_kf,val1,val1_g,val4) " +
                    " Select t.nzp_kvar,max(up_kf),sum(t.val1) val1, sum(t.val1_g) val1_g, sum(t.val4) val4" +
                    " From  ttt_counters_xx t" +
                    " WHERE t.stek=10 AND t.nzp_serv=" + nzp_serv +
                    "   and exists (Select 1 From up_koef u WHERE t.nzp_kvar=u.nzp_kvar)" +
                    " Group by 1";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) { ret.text = "Ошибка получения повышающего коэффициента"; return false; }

                ExecSQL(conn_db, " Create index ix_ttt_ls_norm_koef on ttt_ls_norm_koef (nzp_kvar) ", false);
                ExecSQL(conn_db, sUpdStat + " ttt_ls_norm_koef ", true);

                sql =
                    " UPDATE ttt_counters_xx" +
                    " SET val1=u.val1, val1_g=u.val1_g, val4=u.val4, up_kf=u.up_kf" +
                    " FROM ttt_ls_norm_koef u" +
                    " WHERE ttt_counters_xx.nzp_kvar=u.nzp_kvar AND ttt_counters_xx.stek=3 AND ttt_counters_xx.nzp_serv=" + nzp_serv;
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) { ret.text = "Ошибка получения повышающего коэффициента"; return false; }

                ExecSQL(conn_db, " DROP TABLE ttt_ls_norm_koef ", false);
            }
            ExecSQL(conn_db, " DROP TABLE up_koef ", false);

            return true;
        }


        /// <summary>
        /// Обновляем исходные расходы в соответствии с настоящими значениями 
        /// для записей полученных на основе показаний ПУ или средних значений
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="rashod"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public bool ClearSourceValsForPU(IDbConnection conn_db, Rashod rashod, out Returns ret)
        {
            //для средних расходов по ЛС и расходов по ИПУ ставим val_source=val1
            var sql =
                " UPDATE " + rashod.counters_xx + " SET " +
                " val1_source=val1, up_kf=1,val4_source=val4 " +
                " WHERE exists (SELECT 1 FROM  ttt_counters_xx t" +
                              " WHERE " + rashod.counters_xx + ".nzp_kvar=t.nzp_kvar and " + rashod.counters_xx + ".nzp_serv=t.nzp_serv)" +
                " AND (cnt_stage<>0 OR nzp_counter=-9) AND nzp_type=3 AND stek in (3,20)";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            /*
            //с одн меньше нуля
            sql =
                " UPDATE " + rashod.counters_xx + " SET " +
                " val1_source=rashod, up_kf=1" +
                " WHERE exists (SELECT 1 FROM  ttt_counters_xx t" +
                              " WHERE " + rashod.counters_xx + ".nzp_kvar=t.nzp_kvar and " + rashod.counters_xx + ".nzp_serv=t.nzp_serv)" +
                " AND kod_info in (31,32,33,36) AND cnt_stage=0 AND nzp_counter<>-9 AND nzp_type=3 AND stek in (3,20)";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            //для одпу
            sql =
                " UPDATE " + rashod.counters_xx + " SET " +
                " val1_source=val1, up_kf=1 " +
                " WHERE exists (SELECT 1 FROM  ttt_counters_xx t" +
                              " WHERE " + rashod.counters_xx + ".nzp_kvar=t.nzp_kvar and " + rashod.counters_xx + ".nzp_serv=t.nzp_serv)" +
                " AND kod_info>100 AND cnt_stage=0 AND nzp_counter<>-9 AND nzp_type=3 AND stek in (3,20)";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            */
            //с одн меньше нуля
            sql =
                " UPDATE " + rashod.counters_xx + " SET " +
                " dop87_source=dop87 " +
                " WHERE exists (SELECT 1 FROM  ttt_counters_xx t" +
                              " WHERE " + rashod.counters_xx + ".nzp_kvar=t.nzp_kvar and " + rashod.counters_xx + ".nzp_serv=t.nzp_serv)" +
                " AND kod_info in (31,32,33,36) AND nzp_type=3 AND stek in (3,20)";


            //с одн больше нуля
            ExecSQL(conn_db, "DROP TABLE houses_with_dpu ", false);
            sql = " CREATE TEMP TABLE houses_with_dpu (nzp_kvar integer, nzp_serv integer) " + sUnlogTempTable;
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            sql = " INSERT INTO houses_with_dpu (nzp_kvar,nzp_serv)" +
                  " SELECT t.nzp_kvar, t.nzp_serv" +
                  " FROM " + rashod.counters_xx + " c, ttt_counters_xx t" +
                  " WHERE c.nzp_dom=t.nzp_dom and c.nzp_serv=t.nzp_serv AND c.nzp_type in (1,2) " +
                  " AND c.cnt_stage>0 AND c.stek=3 " +
                  " GROUP BY 1,2";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " CREATE INDEX ix_1_houses_with_dpu on houses_with_dpu (nzp_kvar,nzp_serv)", false);
            ExecSQL(conn_db, sUpdStat + "  houses_with_dpu", false);

            sql =
                " UPDATE " + rashod.counters_xx + " SET " +
                " dop87_source=dop87 " +
                " WHERE exists (SELECT 1 FROM houses_with_dpu t" +
                              " WHERE " + rashod.counters_xx + ".nzp_kvar=t.nzp_kvar and " + rashod.counters_xx + ".nzp_serv=t.nzp_serv)" +
                " AND kod_info in (21,22,23,26,27) AND nzp_type=3 AND stek in (3,20)  ";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            /*
            sql =
                " UPDATE " + rashod.counters_xx + " SET " +
                " val1_source=val1,dop87_source=dop87 " +
                " WHERE exists (SELECT 1 FROM  ttt_counters_xx t" +
                              " WHERE " + rashod.counters_xx + ".nzp_kvar=t.nzp_kvar and " + rashod.counters_xx + ".nzp_serv=t.nzp_serv)" +
                " AND nzp_type=3 AND stek in (3,20) AND nzp_serv=7  ";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            */
            return true;
        }
    }
}

#endregion здесь производится подсчет расходов

