// Подсчет расходов

#region Подключаемые модули

using System;
using System.Data;

using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

#endregion Подключаемые модули

#region здесь производится подсчет расходов

namespace STCLINE.KP50.DataBase
{
   
    //здась находятся классы для подсчета расходов
    public partial class DbCalcCharge : DataBaseHead
    {
        #region Подготовить перечень коммуналок и их параметров
        public bool CreateTempRashodKommunal(IDbConnection conn_db, Rashod rashod, bool b_calc_kvar, out Returns ret)
        {
            #region отдельно выделим коммунальные лс - заполним в mmnog ikvar?, нет - nzp_kvar!, т.к. ikvar м.б. = 0
            //----------------------------------------------------------------
            //стек 3 - отдельно выделим коммунальные лс - заполним в mmnog ikvar?, нет - nzp_kvar!, т.к. ikvar м.б. = 0
            //----------------------------------------------------------------
            ExecSQL(conn_db, " Drop table ttt_aid_c1 ", false);

            ret = ExecSQL(conn_db,
                " Create temp table ttt_aid_c1 " +
                " ( nzp_kvar integer, " +
                "   nzp_dom  integer, " +
                "   mmnog    integer  " +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // добавить назначенные коммунальные квартиры
            ret = ExecSQL(conn_db,
                  " Insert Into ttt_aid_c1 (nzp_kvar,nzp_dom,mmnog) " +
                  " Select k.nzp_kvar,k.nzp_dom,t.nzp_kvar_base as mmnog " +
                  " From " + rashod.paramcalc.data_alias + "kvar k," + rashod.paramcalc.data_alias + "link_ls_lit t, ttt_prm_1 p  " +
                  " Where k." + rashod.where_dom + rashod.where_kvarK +
                  "   and k.nzp_kvar = t.nzp_kvar " +
                  "   and k.nzp_kvar = p.nzp " +
                  "   and p.nzp_prm = 3 " +
                  "   and p.val_prm = '2' " +
                  " Group by 1,2,3 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // добавить остальные коммунальные квартиры
            ret = ExecSQL(conn_db,
                  " insert into ttt_aid_c1 (nzp_kvar,nzp_dom,mmnog) " +
                  " Select k.nzp_kvar,k.nzp_dom,max(case when k.ikvar > 0 then k.ikvar else k.nzp_kvar end) as mmnog " +
                  " From " + rashod.paramcalc.data_alias + "kvar k, ttt_prm_1 p  " +
                  " Where k." + rashod.where_dom + rashod.where_kvarK +
                  "   and k.nzp_kvar = p.nzp " +
                  "   and p.nzp_prm = 3 " +
                  "   and p.val_prm = '2' " +
                  "   and not exists (select 1 from " + rashod.paramcalc.data_alias + "link_ls_lit a where a.nzp_kvar=k.nzp_kvar)" +
                  " Group by 1,2 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }


            ExecSQL(conn_db, " Create unique index ix_ttt_aid_c1 on ttt_aid_c1 (nzp_kvar) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_aid_c1 ", true);

            ret = ExecSQL(conn_db,
                " Update ttt_counters_xx " +
                " Set mmnog = ( Select mmnog From ttt_aid_c1 k Where ttt_counters_xx.nzp_kvar = k.nzp_kvar ) " +
                " Where exists ( Select 1 From ttt_aid_c1 k Where ttt_counters_xx.nzp_kvar = k.nzp_kvar ) "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // информациию о коммуналках пока не удаляем - ttt_aid_c1 - сейчас используем для стека 29
            #endregion отдельно выделим коммунальные лс - заполним в mmnog ikvar?, нет - nzp_kvar!, т.к. ikvar м.б. = 0

            #region Подготовить перечень коммуналок и их параметров

            // пока приготовим перечень коммуналок
            ExecSQL(conn_db, " Drop table t_ans_kommunal ", false);
            ret = ExecSQL(conn_db,
                " Create temp table t_ans_kommunal (" +
                " nzp_no serial not null," +
                " nzp_dom  integer not null, " +
                " nzp_kvar integer not null, " +
                " nkvar  " + sDecimalType + "(18,7) not null, " +
                " val1   " + sDecimalType + "(14,7) default 0.0000000, " +
                " val2   " + sDecimalType + "(14,7) default 0.0000000, " +
                " val3   " + sDecimalType + "(14,7) default 0.0000000, " +
                " val4   " + sDecimalType + "(14,7) default 0.0000000, " +
                " kf307  " + sDecimalType + "(14,7) default 0.0000000, " +
                " kf307n " + sDecimalType + "(14,7) default 0.0000000, " +
                " kf354  " + sDecimalType + "(14,7) default 0.0000000, " +
                " squ1   " + sDecimalType + "(14,7) default 0.0000000, " +
                " squ2   " + sDecimalType + "(14,7) default 0.0000000, " +
                " gil1   " + sDecimalType + "(14,7) default 0.0000000, " +
                " cnt1 integer default 0, " +
                " rastot " + sDecimalType + "(14,7) default 0.0000000, " +   //rashod
                " rasind " + sDecimalType + "(14,7) default 0.0000000, " +   //val_s 
                " kol_komn integer default 0, " +               //cnt5
                " cnt3 integer     default 0, " +               //nzp_res
                " is2075 integer   default 0  " +               // признак спец коммуналки     
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            if (!b_calc_kvar)
            {
                //----------------------------------------------------------------
                // заполнить стек 29 для сохранения доли коммуналки в площади квартиры для Пост 354 только при расчете дома
                // при расчете по 1 л/с используется чтобы не пересчитывать все коммунальные комнаты квартиры
                //----------------------------------------------------------------

                // список коммуналок с жилыми и общими площадями - по ttt_counters_xx вставятся только открытые ЛС!
                ret = ExecSQL(conn_db,
                    " Insert into t_ans_kommunal " +
                    " (nzp_kvar,nzp_dom,nkvar,squ1,squ2) " +
                    " Select a.nzp_kvar,a.nzp_dom,b.mmnog,max(a.squ1),max(a.sqgil)" +
                    " From ttt_counters_xx a,ttt_aid_c1 b" +
                    " Where a.nzp_kvar=b.nzp_kvar " +
                    " group by 1,2,3 "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // дополнить список коммуналок закрытыми коммуналками с жилыми и общими площадями
                ExecSQL(conn_db, " Drop table t_ans_komm_sq ", false);
                ret = ExecSQL(conn_db,
                    " Create temp table t_ans_komm_sq " +
                    " ( nzp_kvar integer," +
                    "   nzp_dom  integer," +
                    "   mmnog    integer," +
                    "   sq       " + sDecimalType + "(12,4) default 0," +
                    "   sqgil    " + sDecimalType + "(12,4) default 0," +
                    "   sqot     " + sDecimalType + "(12,4) default 0 " +
                    " )  " + sUnlogTempTable
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // только открытые лс
                ret = ExecSQL(conn_db,
                    " Insert into t_ans_komm_sq (nzp_kvar,nzp_dom,mmnog) " +
                    " Select t.nzp_kvar,t.nzp_dom,t.mmnog " +
                    " From ttt_aid_c1 t" +
                    " Where not exists (Select 1 From t_ans_kommunal a where t.nzp_kvar=a.nzp_kvar) "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                ExecSQL(conn_db, " Create unique index ix_ans11_sq on t_ans_komm_sq (nzp_kvar) ", true);
                ExecSQL(conn_db, sUpdStat + " t_ans_komm_sq ", true);

                // общая площадь
                ret = ExecSQL(conn_db, 
                    " Update t_ans_komm_sq " +
                    " Set sq   = ( select p.val_prm from ttt_prm_1 p where p.nzp=t_ans_komm_sq.nzp_kvar and p.nzp_prm=4 )" + sConvToNum + " " +
                    " Where exists ( select 1 from ttt_prm_1 p where p.nzp=t_ans_komm_sq.nzp_kvar and p.nzp_prm=4 ) "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // отапливаемая площадь
                ret = ExecSQL(conn_db, 
                    " Update t_ans_komm_sq " +
                    " Set sqot = ( select p.val_prm from ttt_prm_1 p where p.nzp=t_ans_komm_sq.nzp_kvar and p.nzp_prm=133 ) " + sConvToNum + " " +
                    " Where  0 < ( select count(*)  from ttt_prm_1 p where p.nzp=t_ans_komm_sq.nzp_kvar and p.nzp_prm=133 ) "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // жилая площадь - нужна для коммуналок - для разделения расхода по Пост.354
                ret = ExecSQL(conn_db, 
                    " Update t_ans_komm_sq " +
                    " Set sqgil = ( select p.val_prm from ttt_prm_1 p where p.nzp=t_ans_komm_sq.nzp_kvar and p.nzp_prm=6 ) " + sConvToNum + " " +
                    " Where   0 < ( select count(*)  from ttt_prm_1 p where p.nzp=t_ans_komm_sq.nzp_kvar and p.nzp_prm=6 ) "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                ret = ExecSQL(conn_db,
                    " Insert into t_ans_kommunal " +
                    " (nzp_kvar,nzp_dom,nkvar,squ1,squ2) " +
                    " Select nzp_kvar,nzp_dom,mmnog,max(sq),max(sqgil)" +
                    " From t_ans_komm_sq " +
                    " Where sq > 0 or sqgil > 0 " +
                    " group by 1,2,3 "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                //
                ExecSQL(conn_db, " Drop table t_ans_komm_sq ", false);
                
                #region 29 стек для коммуналок в случае если расчет домовой !b_calc_kvar

                //----------------------------------------------------------------
                // заполнить стек 29 для сохранения доли коммуналки в площади квартиры для Пост 354 только при расчете дома
                // при расчете по 1 л/с используется чтобы не пересчитывать все коммунальные комнаты квартиры
                //----------------------------------------------------------------

                // сохраним кол-во жильцов для коммуналок для стека 29 - поможет при показе распределний по коммуналкам расходов
                ret = ExecSQL(conn_db,
                    " Update t_ans_kommunal " +
                    " Set gil1 = ( Select max(vald) From ttt_aid_c2 k Where t_ans_kommunal.nzp_kvar = k.nzp_kvar ) " +
                    " Where 0 < ( Select count(*) From ttt_aid_c2 k Where t_ans_kommunal.nzp_kvar = k.nzp_kvar ) "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                ret = ExecSQL(conn_db,
                    " Update t_ans_kommunal " +
                    " Set cnt1 = Round(gil1) " +
                    " Where 1=1 "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // расчет суммарных площадей и кол-ва жильцов по квартирам, где есть коммуналки
                ExecSQL(conn_db, " Drop table t_ans_itog ", false);
                ret = ExecSQL(conn_db,
                    " Create temp table t_ans_itog " +
                    " ( nzp_dom  integer not null, " +
                    "   nkvar  " + sDecimalType + "(18,7) not null, " +
                    "   squ1   " + sDecimalType + "(14,7) default 0.0000000, " +
                    "   squ2   " + sDecimalType + "(14,7) default 0.0000000, " +
                    "   gil1   " + sDecimalType + "(14,7) default 0.0000000, " +
                    "   cnt1 integer default 0 " +
                    " ) " + sUnlogTempTable
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                ret = ExecSQL(conn_db,
                    " Insert into t_ans_itog (nzp_dom,nkvar,squ1,squ2,gil1,cnt1) " +
                    " Select nzp_dom,nkvar,sum(squ1) squ1,sum(squ2) squ2,sum(gil1) gil1,sum(cnt1) cnt1 " +
                    " From t_ans_kommunal " +
                    " Group by 1,2 "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                ExecSQL(conn_db, " Create index ix_t_ans_itog on t_ans_itog (nzp_dom,nkvar) ", true);
                ExecSQL(conn_db, sUpdStat + " t_ans_itog ", true);

                ret = ExecSQL(conn_db,
                    " Update t_ans_kommunal Set " +
                    " val1 = ( Select squ1 From t_ans_itog k Where t_ans_kommunal.nzp_dom = k.nzp_dom and t_ans_kommunal.nkvar = k.nkvar )," +
                    " val2 = ( Select squ2 From t_ans_itog k Where t_ans_kommunal.nzp_dom = k.nzp_dom and t_ans_kommunal.nkvar = k.nkvar )," +
                    " val3 = ( Select gil1 From t_ans_itog k Where t_ans_kommunal.nzp_dom = k.nzp_dom and t_ans_kommunal.nkvar = k.nkvar )," +
                    " val4 = ( Select cnt1 From t_ans_itog k Where t_ans_kommunal.nzp_dom = k.nzp_dom and t_ans_kommunal.nkvar = k.nkvar ) " +
                    " Where 0 < ( Select count(*) From t_ans_itog k" +
                    "             Where t_ans_kommunal.nzp_dom = k.nzp_dom and t_ans_kommunal.nkvar = k.nkvar ) "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                string sql;
                if (Points.IsSmr)
                {
                    // для самары коэф-т по жилой площади округлить до 4х знаков
                    sql =
                    " kf307  = case when val2>0.0001 then Round( (squ2/val2) * 10000 )/10000 else 0 end, " +
                    " kf307n = case when val3>0.0001 then gil1/val3 else 0 end  ";

                }
                else
                {
                    sql =
                    " kf307  = case when val2>0.0001 then squ2/val2 else 0 end, " +
                    " kf307n = case when val3>0.0001 then gil1/val3 else 0 end  ";
                }
                ret = ExecSQL(conn_db,
                    " Update t_ans_kommunal " +
                    " Set" + sql +
                    " Where 1=1 "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                #region Признак дома с общагами домовой
                ret = ExecSQL(conn_db,
                    " Update t_ans_kommunal " +
                    " Set is2075 =1 " +
                    " Where 0 < ( Select count(*) From ttt_prm_2 k Where t_ans_kommunal.nzp_dom = k.nzp and k.nzp_prm =2075 and k.val_prm ='1' ) "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                ret = ExecSQL(conn_db,
                    " Update t_ans_kommunal " +
                    " Set is2075 =1 " +
                    " Where 0 < ( Select count(*) From ttt_prm_1 k Where t_ans_kommunal.nzp_kvar = k.nzp and k.nzp_prm =2076 and k.val_prm ='1' ) "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                #endregion Признак дома с общагами домовой

                #endregion 29 стек для коммуналок в случае если расчет домовой !b_calc_kvar
            }
            else
            {
                // для расчета по л/с t_ans_kommunal не создан!
                // nzp_kvar в cur_zap!!! иначе удалится пр расчете 1го л/с! пока ничего другого не придумал!?
                ret = ExecSQL(conn_db,
                    " Insert into t_ans_kommunal" +
                    " (nzp_dom,nzp_kvar,nkvar,val1,val2,val3,val4,squ1,squ2,gil1,cnt1,kf307,kf307n, rastot, rasind,kol_komn, is2075)" +
                    " select nzp_dom,cur_zap,mmnog,val1,val2,val3,val4,squ1,squ2,gil1,cnt1,kf307,kf307n, rashod, val_s, cnt5, cnt4" +
                    " From " + rashod.counters_xx +
                    " Where nzp_type = 3 and stek = 29  " + rashod.where_kvar.Replace("nzp_kvar", "cur_zap") + rashod.paramcalc.per_dat_charge
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            }
            ExecSQL(conn_db, " Create unique index ix1_t_ans_kommunal on t_ans_kommunal (nzp_no) ", true);
            ExecSQL(conn_db, " Create unique index ix2_t_ans_kommunal on t_ans_kommunal (nzp_kvar) ", true);
            ExecSQL(conn_db, sUpdStat + " t_ans_kommunal ", true);

            ExecSQL(conn_db, " Drop table ttt_aid_c1 ", false);
            ExecSQL(conn_db, " Drop table ttt_aid_c2 ", false);

            #endregion Подготовить перечень коммуналок и их параметров

            return true;
        }
        #endregion Подготовить перечень коммуналок и их параметров

        #region Поправки в данные для коммуналок по ЭлЭн и сохранение стека 29 - если расчет дома или БД
        public bool SetAndSaveTempRashodKommunal(IDbConnection conn_db, Rashod rashod, bool b_calc_kvar, 
            bool bIsSaha, string p_dat_charge, out Returns ret)
        {
            ret = Utils.InitReturns();

            if (bIsSaha) { /* В сахе ничего не делаем */ }
            else
            {
                #region Проставить количество комнат для общежитий и признак = дом не-МКД в t_ans_kommunal

                //2075|Применить норматив ЭлЭн для коммуналок в общежитии|||bool||2||||

                ret = ExecSQL(conn_db, 
                    " Update t_ans_kommunal " +
                    " Set" +
                    // Количество комнат для общежитий проставить в t_ans_kommunal.kol_komn
                    " kol_komn =" + sNvlWord +
                    " ((select max(cnt2) from ttt_counters_xx " +
                    "   where nzp_serv in (25,210,410) and t_ans_kommunal.nzp_kvar= ttt_counters_xx.nzp_kvar ),0) " +
                    // Тип из нормативов cnt3 =дом не-МКД (по всем нормативным строкам лс и домам)
                    ",cnt3  =" + sNvlWord +
                    " ((select max(cnt3) from ttt_counters_xx " +
                    "   where nzp_serv in (25,210,410) and t_ans_kommunal.nzp_kvar= ttt_counters_xx.nzp_kvar ),0) " +
                    " Where is2075=1 "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                #endregion Проставить количество комнат для общежитий и признак = дом не-МКД в t_ans_kommunal

                #region Определить общую норму и индивидуальную

                ret = ExecSQL(conn_db, 
                    " Update t_ans_kommunal " +
                    " Set rastot = " + sNvlWord +
                    "(( Select value From " + rashod.paramcalc.kernel_alias + "res_values " +
                                 " Where nzp_res = cnt3 " +
                                 "   and nzp_y = (case when val3 > 6 then 6 else val3 end) " + //кол-во людей
                                 "   and nzp_x = (case when kol_komn > 5 then 5 else kol_komn end) " + //кол-во комнат
                                 " ),'0')" + sConvToNum +
                    " Where cnt3 in ( Select nzp_res From " + rashod.paramcalc.kernel_alias + "res_values ) " +
                      " and is2075=1 "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                #endregion Определить общую норму и индивидуальную

                #region Подсчитать расход (временно убывших не учитываем, возможно потом)

                ret = ExecSQL(conn_db, 
                    " Update ttt_counters_xx " +
                    " Set  " +
                    " val1 =" + sNvlWord +
                    "((select t.rastot*t.gil1 from t_ans_kommunal t where t.nzp_kvar=ttt_counters_xx.nzp_kvar and t.is2075=1),0)  " +
                    ", rash_norm_one =" + sNvlWord +
                    "((select t.rastot        from t_ans_kommunal t where t.nzp_kvar=ttt_counters_xx.nzp_kvar and t.is2075=1),0)  " +
                    ", val1_g =" + sNvlWord +
                    "((select t.rastot*t.gil1 from t_ans_kommunal t where t.nzp_kvar=ttt_counters_xx.nzp_kvar and t.is2075=1),0)  " +
                    ", gil2 =" + sNvlWord +
                    "((select t.val3          from t_ans_kommunal t where t.nzp_kvar=ttt_counters_xx.nzp_kvar and t.is2075=1),0) " +
                    " Where nzp_serv in (25,210) " +
                    "   and cnt1_g > 0 and cnt2 > 0 " +
                    "   and 0< (select count(*) from t_ans_kommunal t where t.nzp_kvar=ttt_counters_xx.nzp_kvar and is2075=1 ) "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                #endregion Подсчитать расход

                #region Сохранить в 29 стеке
                if (!b_calc_kvar)
                {
                    string sql =
                        " Insert into " + rashod.counters_xx +
                        " ( stek,nzp_type,dat_charge,nzp_kvar,cur_zap,nzp_dom,nzp_counter,nzp_serv,mmnog,val1,val2,val3,val4," +
                        "   squ1,squ2,gil1,cnt1,kf307,kf307n,dat_s,dat_po, rashod, val_s , cnt5, cnt4 ) " +
                        " Select 29,3, " + p_dat_charge + ", 0,nzp_kvar,nzp_dom, 0, 0, nkvar," +
                        " val1,val2,val3,val4,squ1,squ2,gil1,cnt1,kf307,kf307n," + MDY(1, 1, 1900) + ", " + MDY(1, 1, 1900) +
                        ", rastot, rasind, kol_komn, is2075 " +
                        " From t_ans_kommunal Where 1=1 ";

                    if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
                    {
                        ret = ExecSQL(conn_db, sql, true);
                    }
                    else
                    {
                        ExecByStep(conn_db, "t_ans_kommunal", "nzp_no", sql, 100000, " ", out ret);
                    }
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                    UpdateStatistics(false, rashod.paramcalc, rashod.counters_tab, out ret);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                }
                #endregion Сохранить в 29 стеке
            }

            return true;
        }
        #endregion Поправки в данные для коммуналок по ЭлЭн и сохранение стека 29 - если расчет дома или БД
    }

}

#endregion здесь производится подсчет расходов
