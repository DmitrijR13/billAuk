#region подключаемые пространства
using System;
using System.Linq;
using System.Data;
using System.Text;
using System.Collections.Generic;
using System.Globalization;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
#endregion подключаемые пространства
#region Расчет пени
namespace STCLINE.KP50.DataBase
{
    #region здесь находятся функции для подсчета пени (partial class DbCalc)
    public partial class DbCalc : DbCalcClient
    {

        #region Переменные и константы расчета
        string d85, d99, pdat_s,pdat_po;
       
        //DateTime dd85, dd99;

        #endregion Переменные и константы расчета

        #region Главный цикл расчета пени
        public bool CalcRasPeni(IDbConnection conn_db, ParamCalc paramcalc, out Returns ret)
        //-----------------------------------------------------------------------------
        {
            // Цикл расчета пени 
            try
            {
                ret = Utils.InitReturns();
               // Utility.ClassLog.InitializeLog("c:\\", "CalcRasPeni.log");
                #region Выбрать параметры 85 и 99 из prm_10  и проверить стоит ли расчитывать
                GetParametrPeni(conn_db, paramcalc);   

                #endregion Выбрать параметры 85 и 99 из prm_10  и проверить стоит ли расчитывать

                #region Расчет пени по условиям Дом, квартира , вся база здесь ВЫЗОВ ФУНКЦИИ РАСЧЕТА С ВЫБРАННЫМИ ПАРАМЕТРАМИ (функции участвующие в расчете )

                if ((paramcalc.nzp_dom > 0) || (paramcalc.nzp_kvar > 0))
                #region По домам и квартирам
                {
                    // Найти перечень лицевых счетов у которых есть услуга пени 
                    if (ExistsLsWithPeni(conn_db, paramcalc).GetReturns().result)
                    {

                    }
                    else
                    {
                        // нет лс с услугой пени 

                        return false;
                    }
                    return true;
                }
                #endregion По домам и квартирам
                else
                #region По всей базе пока пустой
                {
                    // Здесь идет расчет по домам и квартирам
                }
                #endregion По всей базе


                #endregion Расчет пени по условиям Дом, квартира , вся база здесь ВЫЗОВ ФУНКЦИИ РАСЧЕТА С ВЫБРАННЫМИ ПАРАМЕТРАМИ (функции участвующие в расчете )

                #region Сохранение расчета пока пустой
                #endregion Сохранение расчета
            }
            catch (Exception ex)
            {

                ret = STCLINE.KP50.Utility.ExceptionUtility.OnException(ex, System.Reflection.MethodBase.GetCurrentMethod().Name);
            }

            //return new ReturnsType() ;
            return ret.result;
        }

        #endregion Главный цикл расчета пени

        #region Функции, участвующие в расчете Пени

        #region Выбрать 85 и 99 параметр расчета пени
        public ReturnsType GetParametrPeni(IDbConnection conn_db, ParamCalc paramcalc)
        //-----------------------------------------------------------------------------
        {
            #region Выборка значений параметров
            d85 = ""; 
            d99 = "";
            pdat_s = "";
            pdat_po = "";
            //double count;

            // Создать таблицу ставок(пока это процент в в день - получается как ставка Х/300/(количество дней в месяце))
            //, когда будут оплаты , пересечем по датам и вставим актуальные ставки рефинансирования
            string sqlString = " drop table t_stavka_ref ";
            ClassDBUtils.ExecSQL(sqlString, conn_db, ClassDBUtils.ExecMode.Log);

            sqlString = " create temp table t_stavka_ref(dat_s date,dat_po date, stavka " + sDecimalType + "(14,4) )";
            ClassDBUtils.ExecSQL(sqlString, conn_db, ClassDBUtils.ExecMode.Exception);

            sqlString = "select nzp_prm , val_prm, dat_s, dat_po from " + paramcalc.data_alias + "prm_10 where nzp in (" +
                        paramcalc.nzp_dom + ",0) and is_actual<>100  " +
                        " and nzp_prm in (85,99) " +
                        " and dat_s <= " + sPublicForMDY + "mdy(" + paramcalc.calc_mm.ToString("00") + "," +
                        System.DateTime.DaysInMonth(paramcalc.calc_yy, paramcalc.calc_mm).ToString("00") + ","
                        + paramcalc.calc_yy.ToString("0000") + ")" +
                        " and dat_po>= " + sPublicForMDY + "mdy(" + paramcalc.calc_mm + ",01," + paramcalc.calc_yy + ") ";
            DataTable dt = ClassDBUtils.OpenSQL(sqlString, conn_db, ClassDBUtils.ExecMode.Exception).GetData();
            


            foreach (DataRow r in dt.Rows)
            {
                if (Convert.ToInt32(r["nzp_prm"]) == 85) 
                {   d85     = Convert.ToString(r["val_prm"]); 
                    pdat_s  = Convert.ToString(r["dat_s"]);
                    pdat_po = Convert.ToString(r["dat_po"]);

                    // Добавим каждое изменение ставки рафинанасирования в таблицу 
                    sqlString = 
                        "insert into t_stavka_ref(dat_s,dat_po,stavka) values(cast('" + pdat_s.Substring(0, 10) + "' as date), cast('" +
                        pdat_po.Substring(0, 10) + "' as date)," + d85.Trim() + ")";
                    ClassDBUtils.ExecSQL(sqlString, conn_db, ClassDBUtils.ExecMode.Exception);
                 //   count = ClassDBUtils.GetAffectedRowsCount(conn_db);
                };

                if (Convert.ToInt32(r["nzp_prm"])== 99) { d99 = Convert.ToString(r["val_prm"]);};
            }
            #region Удалить после отладки
            /*
            // отладка добавить ставку 0.05 c 15/10/2012
            pdat_s="09.10.2012";
            pdat_po = "01.01.3000";
            sqlString = "insert into t_stavka_ref(dat_s,dat_po,stavka) values('" + pdat_s.Substring(0, 10) + "','" + pdat_po.Substring(0, 10) + "'," + "0.05" + ")";
            ClassDBUtils.ExecSQL(sqlString, conn_db, ClassDBUtils.ExecMode.Exception);
            count = ClassDBUtils.GetAffectedRowsCount(conn_db);
            */ 
            #endregion Удалить после отладки


            #endregion Выборка значений параметров

            #region Проверка выбранных значений
            if (d99.Length > 0)
            {
                #region Ограничить время применения пени
                if (DateTime.Parse(d99) > (new DateTime(paramcalc.calc_yy, paramcalc.calc_mm, 01)))
                {
                    // Дата старта расчета пени больше даты текущего сальдового месяца , и незачем дальше расчитывать 
                    return new ReturnsType(false);
                }
                #endregion Ограничить время применения пени
            }


            if (d85.Length > 0)
            {
                #region Проверка на очень маленькое значение
                if (Convert.ToDecimal(d85) < Convert.ToDecimal(0.0001))
                {
                    return new ReturnsType(false);
                }
                else
                {
                    // Значение процента пени есть  идем расчитывать в следующий блок 
                    return new ReturnsType();  
                }
                #endregion Проверка на очень маленькое значение
            }
            else
            {
                #region Нет процента пени, значит незачем дальше расчитывать
                return new ReturnsType(false);
                #endregion Нет процента пени, значит незачем дальше расчитывать
            }
            #endregion Проверка выбранных значений

            #region Если все хорошо то возвращаем истину
           // return new ReturnsType();
            #endregion Если все хорошо то возвращаем истину
        }
        #endregion Выбрать 85 и 99 параметр расчета пени

        #region Есть ли лицевые счета с услугой пени НАЧИНАЕМ РАСЧЕТ
        public ReturnsType ExistsLsWithPeni(IDbConnection conn_db, ParamCalc paramcalc)
        //-----------------------------------------------------------------------------
        {
            #region инициализация переменных
            bool b_peni = false;
            d85 = "";
            d99 = "";
            #endregion инициализация переменных

            #region Проверка наличия услуги пени в рассматриваемом множестве квартир
#if PG
            string sqlString = " select nzp_kvar from temp_table_tarif where nzp_serv = 500  limit 1 ";
#else
            string sqlString = " select first 1 nzp_kvar from temp_table_tarif where nzp_serv =500  ";                
#endif
            DataTable dt = ClassDBUtils.OpenSQL(sqlString, conn_db, ClassDBUtils.ExecMode.Exception).GetData();

            foreach (DataRow r in dt.Rows)
            {
                if (Convert.ToInt32(r["nzp_kvar"]) > 0) 
                {
                    b_peni = true;
                    break;                    
                }
            }
            #endregion Проверка наличия услуги пени в рассматриваемом множестве квартир

            #region Расчет пени
            if (b_peni)
            {
                 
              #region ЦИКЛ ОБРАБОТКИ РАСЧЕТА ПЕНИ
                #region Создать временную таблицу где будут храниться квартиры в которых надо будет расчитать пени
                CreateTempTableForPeni(conn_db, paramcalc);
                #endregion Создать временную таблицу где будут храниться квартиры в которых надо будет расчитать пени

                #region Выбрать сумму долга за предыдущий период
                GetSumInsaldoForPeni(conn_db, paramcalc);
                #endregion Выбрать сумму долга за предыдущий период

                // подсчитать итоговые суммы
                #region Подсчитать промежуточные итоги
                PutSumPeniForCharge(conn_db, paramcalc );
                #endregion Подсчитать промежуточные итоги

                // Начать цикл расчета пени (выбрать точки деления расчета по датам )
                #region Выбрать все возможные в текущем месяце ставки рефинансирования
                GetStavRefinans(conn_db, paramcalc);
                #endregion Выбрать все возможные в текущем месяце ставки рефинансирования

                #region Выбрать все отрицательные перерасчеты в текущем месяце за период предшествующий 
                GetSumMinusRevalForPeni(conn_db, paramcalc);
                #endregion Выбрать все отрицательные перерасчеты в текущем месяце за период предшествующий

                #region Выбрать все отрицательные перекидки в прошлом и текущем месяце 
                GetSumMinusPerekidkaForPeni(conn_db, paramcalc);
                #endregion Выбрать все отрицательные перекидки в прошлом и текущем месяце

                #region Выбрать все оплаты поступившие в текущем месяце
                GetSumNewOplatForPeni(conn_db, paramcalc);
                #endregion Выбрать все оплаты поступившие в текущем месяце

                #region выстроить путь обхода по датам
                // Подсчитать количество дней для расчета пени  
                PutUchetOplatPeni(conn_db, paramcalc);
                   // Подсчитать сумму пени 
                   #region в простом варианте накопленную сумму не смотрим
                     // Запомнить накопленную сумму  запомнить параметр текущей суммы и параметры суммы
                   // продолжать цикл до конца месяца 
                   #endregion в простом варианте накопленную сумму не смотрим
                #endregion выстроить путь обхода по датам

                //  положить их в таблицу расходов по текущему месяцу 
                #region Сохранить параметры расчета
                SaveGku(conn_db,paramcalc);
                #endregion Сохранить параметры расчета

                return new ReturnsType();           
                #endregion ЦИКЛ ОБРАБОТКИ РАСЧЕТА ПЕНИ
            }
            else  
                #region ПЕНИ не расчитывается потому что их нет в тарифе
                MonitorLog.WriteLog("ПЕНИ не расчитывается потому что нет установленной услуги  ", MonitorLog.typelog.Info, 1, 2, true);
                    return new ReturnsType();           
                #endregion ПЕНИ не расчитывается
            #endregion Расчет пени                 

        }
        #endregion Есть ли лицевые счета с услугой пени НАЧИНАЕМ РАСЧЕТ

        #region Создать временные таблицы расчета пени
        public ReturnsType CreateTempTableForPeni(IDbConnection conn_db, ParamCalc paramcalc)
        {
            // Потом использовать для протокола расчета 
            /*
            string sSql;
            #region Удалить таблицу если уже такая есть (t_peni_link)
            sSql=" drop table peni_link ";
            ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Log );
            #endregion Удалить таблицу если уже такая есть (t_peni_link)

            #region Создать временную таблицу без журнала (t_peni_link)
            sSql=   " Create temp table peni_link " +
                    " ( nzp_kvar   integer, " +
                    "   nzp_dom    integer, " +
                    "   sstep      integer, " +
                    "   sum_pere   decimal(16,4), " +
                    "   sum_reval  decimal(16,4), " +
                    "   sum_dolg   decimal(16,4), " +
                    "   sum_dolg_t decimal(16,4), " +
                    "   procent    decimal(16,4), " +
                    "   kol_day    decimal(16,4), " +
                    "   dat_s      date, " +
                    "   dat_po     date, " +
                    "   dat_when   date " +
                    " ) With no log ";
                    
            ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Log);
            #endregion Создать временную таблицу без журнала (t_peni_link)
            */
            return new ReturnsType();                          
        }
        #endregion Создать временные таблицы расчета пени

        #region Выбрать суммы на основании которых будет расчет (текущий месяц -1 )
        public ReturnsType GetSumInsaldoForPeni(IDbConnection conn_db, ParamCalc paramcalc)
        {
            Gku gku = new Gku(paramcalc);            
            string sSql;
            Int32 count;
            #region  Создание временных таблиц
            sSql = "  drop table t_prev_charge ";
            ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Log);
            sSql =  
                " create temp table t_prev_charge( nzp_kvar integer,num_ls integer, nzp_dom integer, "+
                "sum_insaldo " + sDecimalType + "(16,6), real_charge " + sDecimalType + "(16,6), reval " + sDecimalType + "(16,6), sum_money " + sDecimalType + "(14,2), " +
                " nzp_supp integer, nzp_frm integer,dat_s date,dat_po date, sum_prih " + sDecimalType + "(14,4) )" + sUnlogTempTable;
            ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception);

            sSql = "  drop table tt_prev_charge ";
            ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Log);
            sSql = 
                " create temp table tt_prev_charge( nzp_kvar integer,num_ls integer, nzp_dom integer, "+
                "sum_insaldo " + sDecimalType + "(16,6), real_charge " + sDecimalType + "(16,6), reval " + sDecimalType + "(16,6), sum_money " + sDecimalType + "(14,2), " +
                "nzp_supp integer, nzp_frm integer, rashod " + sDecimalType + "(16,6), rashodP " + sDecimalType + "(16,6),dat_s date,dat_po date," +
                "sref " + sDecimalType + "(14,4) , sum_prih " + sDecimalType + "(14,4),note char(250) )" + sUnlogTempTable;
            ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception);
            #endregion  Создание временных таблиц

            sSql =
                 " insert into t_prev_charge( nzp_kvar , num_ls, nzp_dom , sum_insaldo, real_charge, reval, sum_money , nzp_supp, nzp_frm, dat_s,dat_po ) "+
                 " SELECT a.nzp_kvar ,num_ls, 0 nzp_dom , a.sum_insaldo,case when a.real_charge<0 then "+
                 " a.real_charge else 0 end as real_charge, a.reval, a.sum_money , 0  nzp_supp, 0 nzp_frm , '"+
                     System.DateTime.DaysInMonth(paramcalc.prev_calc_yy, paramcalc.prev_calc_mm).ToString("00")+"."  + paramcalc.prev_calc_mm.ToString("00") + "." + paramcalc.prev_calc_yy.ToString("0000")+"' as dat_s,"+
                     "'"+System.DateTime.DaysInMonth(paramcalc.calc_yy , paramcalc.calc_mm).ToString("00")+"."  + paramcalc.calc_mm.ToString("00")      + "." + paramcalc.calc_yy.ToString("0000")     +"' as dat_po" +
                 " FROM "+gku.prevSaldoMon_charge+" a   "+
                 " WHERE a.dat_charge is null and a.nzp_serv>1  "+
                 " and a.nzp_serv<>500 " +
                 " and a.nzp_kvar in (select c.nzp_kvar from temp_table_tarif c where c.nzp_serv = 500)  ";

#if PG            
            count = ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception).resultAffectedRows;
#else
            ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception);
            count = ClassDBUtils.GetAffectedRowsCount(conn_db);
#endif
            sSql = " create index i_t_prev_charge on t_prev_charge (nzp_kvar,nzp_dom ) ";               
            ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Log);
            sSql =
                 " insert into tt_prev_charge( nzp_kvar ,  num_ls,   nzp_dom , nzp_supp, nzp_frm,      rashod,    dat_s,dat_po,     sum_insaldo,       real_charge,       reval,       sum_money)  " +
                                      " SELECT nzp_kvar ,  num_ls, 0 nzp_dom , 0 nzp_supp, 0 nzp_frm, 0 as rashod,dat_s,dat_po, sum(sum_insaldo) , sum(real_charge) , sum(reval) , sum(sum_money)  " +
                 " FROM t_prev_charge  "+
                 " group by 1,2,3,4,5,6,7,8 ";
#if PG
            count = 0;
            count = ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception).resultAffectedRows;
#else
            ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception);
            count = 0;
            count = ClassDBUtils.GetAffectedRowsCount(conn_db);
#endif
            sSql = " create index i_tt_prev_charge on tt_prev_charge (nzp_kvar ) ";
            ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Log);     
            sSql = " create index i_5tt_prev_charge on tt_prev_charge (num_ls ) ";
            ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Log);     

            return new ReturnsType();
        }
        #endregion Выбрать суммы на основании которых будет расчет (текущий месяц -1 )

        #region  Выбрать оплаты поступившие в текущем месяце
        public ReturnsType GetSumNewOplatForPeni(IDbConnection conn_db, ParamCalc paramcalc)
        {           
            Gku gku = new Gku(paramcalc);    
            string sSql,pstavka,pdat_s;
            Int32 count;
            #region Удаление временных таблиц

            sSql = " drop table t_tekOplP " ;
            ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Log);
            sSql = " drop table tt_tekOplP ";
            ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Log);

            #endregion Удаление временных таблиц

            #region Создание временных таблиц
            
            sSql = " create temp table t_tekOplP "+
                   " ( num_ls integer,sum_prih " + sDecimalType + "(14,2),dat_prih date, sref " + sDecimalType + "(16,6), "+
                   " num smallint, dat_s date ,dat_po date ) "+sUnlogTempTable;
            ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception);
            sSql = 
                " create temp table tt_tekOplP " +
                " ( num_ls integer,sum_prih " + sDecimalType + "(14,2),dat_prih date, sref " + sDecimalType + "(16,6), "+
                " num smallint, dat_s date ,dat_po date )"+sUnlogTempTable;
            ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception);
            #endregion Создание временных таблиц

            #region выбрать оплаты 
           sSql = " insert into t_tekOplP(num_ls,sum_prih,dat_prih,sref,num )" +
                " select a.num_ls, a.sum_prih ,  a.dat_prih,0 sref,1  num   "+
                " from " + gku.calc_tosupplXX + " a " +
                " where a.num_ls in (select nzp_kvar from tt_prev_charge) and dat_prih >='01." + paramcalc.cur_mm.ToString("00") + "." + paramcalc.cur_yy.ToString("0000")+"'";

#if PG
           count = ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception).resultAffectedRows;
#else
            ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception);
            count = ClassDBUtils.GetAffectedRowsCount(conn_db);
#endif
            sSql = " insert into tt_tekOplP(num_ls,dat_prih,sref,num,sum_prih )" +
                " select a.num_ls,  a.dat_prih,0 sref,1 num ,sum( a.sum_prih)  from t_tekOplP a  group by 1,2,3,4 "    ;

#if PG
            count = ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception).resultAffectedRows;
#else
            ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception);
            count = ClassDBUtils.GetAffectedRowsCount(conn_db);
#endif

            #endregion выбрать оплаты

            #region Добавка дат ставок рефинансирования
            //вставка строк касающихся ставки рефинансирования
            sSql = 
                " insert into tt_tekOplP(num_ls,dat_prih,sref,num,sum_prih ) " +            
                " select distinct  0, case when dat_s < "+sPublicForMDY+"mdy("+paramcalc.calc_mm+ ",1,"+paramcalc.calc_yy+               
                ")  then "+sPublicForMDY+"mdy("+paramcalc.calc_mm+ ",1,"+paramcalc.calc_yy+") else dat_s end  as dat_s " +            
                " , 0 , 0, 0 from t_stavka_ref " +            
                " where  dat_s <= '" + System.DateTime.DaysInMonth(paramcalc.calc_yy, paramcalc.calc_mm).ToString("00") + "." + paramcalc.calc_mm.ToString("00") + "." + paramcalc.calc_yy.ToString("0000") + "'" +            
                " and dat_po>= '" + "01." + paramcalc.calc_mm + "." + paramcalc.calc_yy + "' ";

            //" order by dat_s desc ";  // очень важно в обратном порядке дат начал ставок рефинансирования 
#if PG
            count = ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception).resultAffectedRows;
#else
            ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception);
            count = ClassDBUtils.GetAffectedRowsCount(conn_db);
#endif
            // Вставка последней строки в таблицу оплат 
            sSql = " insert into tt_tekOplP(dat_prih,sref,num,sum_prih )" +
                   " values('"+System.DateTime.DaysInMonth(paramcalc.calc_yy, paramcalc.calc_mm).ToString("00")+"."+paramcalc.calc_mm.ToString("00")+ "." + paramcalc.calc_yy.ToString("0000") + "',0,0,0) ";
            ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception);
            count = ClassDBUtils.GetAffectedRowsCount(conn_db);
            #endregion Добавка дат ставок рефинансирования

            #region Проставить ставку рефинансирования в оплатах 
            sSql = "select dat_s , stavka from t_stavka_ref order by dat_s desc ";
            DataTable dtS = ClassDBUtils.OpenSQL(sSql, conn_db).GetData();
            foreach (DataRow r in dtS.Rows)
            {
                    pstavka = Convert.ToString(r["stavka"]);
                    pdat_s  = Convert.ToString(r["dat_s"]);

                    // Добавим каждое изменение ставки рафинанасирования в таблицу 
                sSql = "update tt_tekOplP set sref=" + pstavka + " where dat_prih>='" + pdat_s.Substring(0, 10) + "' and sref=0 ";
#if PG
                count = ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception).resultAffectedRows;
#else
                ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception);
                count = ClassDBUtils.GetAffectedRowsCount(conn_db);
#endif                   
            }            

            #endregion Проставить ставку рефинансирования в оплатах

            return new ReturnsType();
        }
        #endregion  Выбрать оплаты поступившие в текущем месяце

        #region  Выбрать отрицательные перерасчеты в текущем месяце за период раньше чем sum_insaldo
        public ReturnsType GetSumMinusRevalForPeni(IDbConnection conn_db, ParamCalc paramcalc)
        {
            Gku gku = new Gku(paramcalc);

            string sSql;
            Int32 count;

            #region Создание удаление временных таблиц
            sSql = " drop table t_maxp_rev ";                             
            ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Log);

            sSql = " create temp table t_maxp_rev " +
                   " (num_ls integer, month_ integer,year_ integer,dat_charge date)" + sUnlogTempTable;
            ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception);

            sSql = " drop table t_prev_revals ";
            ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Log);
            sSql = " create temp table t_prev_revals " +
                   " (nzp_kvar integer,num_ls integer,reval "+sDecimalType+"(14,2),dat_charge date, month_ integer,year_ integer)" + sUnlogTempTable;
            ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception);

            sSql = " drop table tt_prev_revals ";
            ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Log);

            sSql = " create temp table tt_prev_revals " +
                   " (nzp_kvar integer,num_ls integer,reval "+sDecimalType+"(14,2),dat_charge date, month_ integer,year_ integer)"+sUnlogTempTable;
            ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception);
            #endregion Создание удаление временных таблиц
            
            string sqlString = 
                " select distinct  month_,year_ , substr(year_,3,2) as year_2  from " + gku.calc_lnkchargeXX + " a " +
           // string sqlString = " select distinct  month_,year_ , substr(year_,3,2) as year_2 from smr36_charge_12:lnk_charge_09 a " +
           " where a.nzp_kvar in (select nzp_kvar from tt_prev_charge) and month_<= " + Convert.ToString(paramcalc.cur_mm) + " and year_<=" + Convert.ToString(paramcalc.cur_yy);
            DataTable dtLnk = ClassDBUtils.OpenSQL(sqlString, conn_db, ClassDBUtils.ExecMode.Exception).GetData();

            Int32 perMon;
            Int32 perYear;
            foreach (DataRow r in dtLnk.Rows)
            {
                // Перебор месяцев перерасчета 
                perMon = Convert.ToInt32(r["month_"]); perYear = Convert.ToInt32(r["year_2"]);
                sSql = 
                    " insert into t_prev_revals( nzp_kvar,num_ls,reval,dat_charge , month_, year_ )" +
                    " select a.nzp_kvar ,  a.num_ls ,  a.reval  , a.dat_charge, "+perMon.ToString("00")+","+perYear.ToString("00")+
                    " from "+paramcalc.pref+ "_charge_"+perYear.ToString("00")+tableDelimiter+"charge_"+perMon.ToString("00") +" a "+
                    " where a.num_ls in (select b.num_ls from tt_prev_charge b  where b.rashodp>0) and a.reval<0  and a.nzp_serv >1 and a.nzp_serv <>500  "+
                    " and a.dat_charge >='28." + paramcalc.prev_calc_mm.ToString("00")+"." +paramcalc.prev_calc_yy.ToString("0000")+"'";

#if PG
                count = ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception).resultAffectedRows;
#else
                ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception);
                count = ClassDBUtils.GetAffectedRowsCount(conn_db);
#endif
                
            }
            sSql = " create index i1_t_prev_revals on t_prev_revals(dat_charge,num_ls,month_,year_) ";
            ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Log);           
            sSql = 
                " insert into t_maxp_rev(      num_ls ,   month_,    year_,     dat_charge  )" +
                " select  a.num_ls , a.month_,  a.year_, max(a.dat_charge ) "+
                " from t_prev_revals a group by 1,2,3 ";

#if PG
            count = ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception).resultAffectedRows;
#else
            ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception);
            count = ClassDBUtils.GetAffectedRowsCount(conn_db);
#endif
            sSql = 
                " insert into tt_prev_revals( nzp_kvar  ,    num_ls ,   month_,    year_, reval  )" +
                " select a.nzp_kvar  ,  a.num_ls , a.month_,  a.year_, sum(a.reval) "+
                " from t_prev_revals a , t_maxp_rev b where "+
                " a.num_ls=b.num_ls and a.month_=b.month_ and a.year_=b.year_ and a.dat_charge=b.dat_charge "+ " group by 1,2,3,4 ";


#if PG
            count = ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception).resultAffectedRows;
#else
            ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception);
            count = ClassDBUtils.GetAffectedRowsCount(conn_db);
#endif

            // Уменьшить суммы перерасчетов за все периоды в которых был перерасчет 
            sSql = 
                " update tt_prev_charge set note=' '||"+sNvlWord+"(note,'')||' вх.сальдо: '||rashodp||'+'|| "+sNvlWord+"((select sum(a.reval) "+
                " from tt_prev_revals a  "+
                " where a.num_ls=tt_prev_charge.num_ls ),0) ," +
                " rashodp="+sNvlWord+"(rashodp,0)+"+sNvlWord+"((select sum(a.reval) "+
                " from tt_prev_revals a  where a.num_ls=tt_prev_charge.num_ls ),0) ";
#if PG
            count = ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception).resultAffectedRows;
#else
            ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception);
            count = ClassDBUtils.GetAffectedRowsCount(conn_db);
#endif
            // Удалить из расмотрения все суммы <=0
            sSql = " delete from tt_prev_charge where rashodp<=0 " ;
#if PG
            count = ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Log).resultAffectedRows;
#else
            ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception);
            count = ClassDBUtils.GetAffectedRowsCount(conn_db);
#endif
            // Осталось учесть отрицательные перекидки см в следующей функции

            return new ReturnsType();
        }
        #endregion  Выбрать отрицательные перерасчеты в текущем месяце за период раньше чем sum_insaldo

        #region Выбрать ставки рефинансирования в текущем месяце
        public ReturnsType GetStavRefinans(IDbConnection conn_db, ParamCalc paramcalc)
        {
            return new ReturnsType();
        }
        #endregion Выбрать ставки рефинансирования в текущем месяце

        #region Выбрать отрицательные перекидки текущего месяца (предыдущий учтен при выборке данных )
        public ReturnsType GetSumMinusPerekidkaForPeni(IDbConnection conn_db, ParamCalc paramcalc)
        {
            Gku gku = new Gku(paramcalc);            
            string sSql;
            Int32 count;

            #region Создание удаление временных таблиц
            sSql = " drop table t_prev_realcharge ";
            ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Log);            
            sSql =  " create temp table t_prev_realcharge( nzp_kvar integer,num_ls integer,  real_charge "+sDecimalType+"(16,6) ) "+ sUnlogTempTable;
            ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception);
            sSql = " drop table tt_prev_realcharge ";
            ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Log);
            sSql = " create temp table tt_prev_realcharge( nzp_kvar integer,num_ls integer,  real_charge "+sDecimalType+"(16,6) ) "+sUnlogTempTable;
            ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception);
            #endregion Создание удаление временных таблиц

            sSql =  " insert into t_prev_realcharge(nzp_kvar , num_ls , real_charge ) "+
                    " select a.nzp_kvar , a.num_ls , case when a.real_charge<0 then a.real_charge else 0 end as real_charge "+
                    " from "+gku.curSaldoMon_charge + " a  where a.nzp_kvar in (select b.nzp_kvar from tt_prev_charge  b ) and a.nzp_serv >1 and a.nzp_serv <>500 ";

#if PG
            count = ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception).resultAffectedRows;
#else
            ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception);
            count = ClassDBUtils.GetAffectedRowsCount(conn_db);
#endif
            sSql = 
                " insert into tt_prev_realcharge(nzp_kvar , num_ls , real_charge ) " +
                " select a.nzp_kvar , a.num_ls , sum(a.real_charge) as real_charge  from t_prev_realcharge  a group by 1,2 ";
#if PG
            count = ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception).resultAffectedRows;
#else
            ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception);
            count = ClassDBUtils.GetAffectedRowsCount(conn_db);
#endif
            // Уменьшить суммы перерасчетов за все периоды в которых был перерасчет 
            sSql = 
                " update tt_prev_charge set rashodp="+sNvlWord+"(rashodp,0)+"+sNvlWord+"((select sum(a.real_charge) from tt_prev_realcharge a  "+
                " where a.num_ls=tt_prev_charge.num_ls ),0) "+
                ", note=trim(note) ||' вхс-пер:'|| "+sNvlWord+"(rashodp,0)||'-'||   "+ sNvlWord+ " ((select sum(a.real_charge) from tt_prev_realcharge a where a.num_ls=tt_prev_charge.num_ls ),0) ";

#if PG
            count = ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception).resultAffectedRows;
#else
            ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception);
            count = ClassDBUtils.GetAffectedRowsCount(conn_db);
#endif
            // Удалить из расмотрения все суммы <=0
            sSql = " delete from tt_prev_charge where rashodp<=0 ";

            return new ReturnsType();
        }

        #endregion Выбрать отрицательные перекидки текущего месяца (предыдущий учтен при выборке данных )

        #region Положить суммы в таблицы расходов
        public ReturnsType SaveGku(IDbConnection conn_db, ParamCalc paramcalc)
        {
            string sSql;
            Int32 count;
            Gku gku = new Gku(paramcalc);

            #region Удалить нулевые строки которые добавил Анэс
            sSql = 
                " delete from  " + gku.calc_gku_xx +
                " where nzp_serv =500 " +
                " and nzp_kvar in (select nzp_kvar from tt_prev_charge where rashod>0 ) ";
#if PG
            count = ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception).resultAffectedRows;
#else
            ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception);
            count = ClassDBUtils.GetAffectedRowsCount(conn_db);
#endif
            sSql = 
                " delete from  " + gku.curSaldoMon_charge +
                " where nzp_serv =500 " +
                " and num_ls in (select num_ls from tt_prev_charge where rashod>0 ) ";

#if PG
            count = ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception).resultAffectedRows;
#else
            ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception);
            count = ClassDBUtils.GetAffectedRowsCount(conn_db);
#endif
            #endregion Удалить нулевые строки которые добавил Анэс

            sSql = 
                " insert into " + gku.calc_gku_xx + " (nzp_dom,     nzp_kvar ,nzp_serv,   nzp_supp,  nzp_frm , nzp_prm_tarif, nzp_prm_rashod, rashod , tarif) " +
                " select  t.nzp_dom,   t.nzp_kvar,   500  ,   t.nzp_supp ,  t.nzp_frm , 500          , 500       ,   1    , " +
                " sum(t.rashod) " +
                " from tt_prev_charge t where t.rashod>0 " +
                " group by 1,2,3,4,5,6,7,8 ";
#if PG
            count = ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception).resultAffectedRows;
#else
            ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception);
            count = ClassDBUtils.GetAffectedRowsCount(conn_db);
#endif
            return new ReturnsType();
        }
        #endregion Положить суммы в таблицы расходов 

        #region Обработать суммы положенные во временные таблицы
        public ReturnsType PutSumPeniForCharge(IDbConnection conn_db, ParamCalc paramcalc)
        {
            string sSql;
            Gku gku = new Gku(paramcalc);
            sSql =  
                " update tt_prev_charge "+
                " set  rashodp = ( case when (("+sNvlWord+"(tt_prev_charge.sum_insaldo,0)+"+sNvlWord+"(case when tt_prev_charge.reval<0 then tt_prev_charge.reval else 0 end,0)  " +
                " + "+sNvlWord+"(case when tt_prev_charge.real_charge<0 then tt_prev_charge.real_charge else 0 end,0) - "+sNvlWord+"(tt_prev_charge.sum_money,0))  )>0   "+
                " then   (("+sNvlWord+"(tt_prev_charge.sum_insaldo,0)+"+sNvlWord+"(case when tt_prev_charge.reval<0 then tt_prev_charge.reval else 0 end,0)  "+
                " + "+sNvlWord+"(case when tt_prev_charge.real_charge<0 then tt_prev_charge.real_charge else 0 end,0) - "+sNvlWord+"(tt_prev_charge.sum_money,0)))   else 0 end) ";
#if PG
            Int32 count = ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception).resultAffectedRows;
#else
            ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception);
            Int32 count = ClassDBUtils.GetAffectedRowsCount(conn_db);
#endif
            sSql =  
                " update tt_prev_charge "+
                " set  note =' '|| trim(note)|| '2. Вх.С-Перекид: '|| rashodp ";
#if PG
            count = ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception).resultAffectedRows;
#else
            ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception);
            count = ClassDBUtils.GetAffectedRowsCount(conn_db);
#endif
            sSql = " create index i3_tt_prev_charge on tt_prev_charge (rashodP) ";
            ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Log);
            sSql = " delete from tt_prev_charge where rashodP <=0 ";
#if PG
            count = ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception).resultAffectedRows;
#else
            ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception);
            count = ClassDBUtils.GetAffectedRowsCount(conn_db);
#endif

            // Восполнить недостающие поля
            // поставщик и формула
            sSql = 
                " update tt_prev_charge set nzp_supp = (select a.nzp_supp from temp_table_tarif a where a.nzp_kvar=tt_prev_charge.nzp_kvar "+
                " and a.nzp_serv=500 ) ";
#if PG
            count = ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception).resultAffectedRows;
#else
            ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception);
            count = ClassDBUtils.GetAffectedRowsCount(conn_db);
#endif

            sSql = " update tt_prev_charge set nzp_frm = (select a.nzp_frm from temp_table_tarif a where a.nzp_kvar=tt_prev_charge.nzp_kvar " +
                                                                       " and a.nzp_serv=500 ) ";
#if PG
            count = ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception).resultAffectedRows;
#else
            ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception);
            count = ClassDBUtils.GetAffectedRowsCount(conn_db);
#endif

            // дома
            sSql = " update tt_prev_charge set nzp_dom = (select a.nzp_dom from t_selkvar a where a.nzp_kvar=tt_prev_charge.nzp_kvar ) ";
#if PG
            count = ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception).resultAffectedRows;
#else
            ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception);
            count = ClassDBUtils.GetAffectedRowsCount(conn_db);
#endif

            sSql = " create index i2_tt_prev_charge on tt_prev_charge (nzp_kvar,nzp_dom,sum_insaldo,real_charge,reval, sum_money ) ";
            ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Log);
            sSql = " delete from " + gku.calc_gku_xx + " where nzp_serv =500 and nzp_kvar in (select b.nzp_kvar from tt_prev_charge b )";
#if PG
            count = ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception).resultAffectedRows;
#else
            ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception);
            count = ClassDBUtils.GetAffectedRowsCount(conn_db);
#endif                 
            return new ReturnsType();
        }
        #endregion Обработать суммы положенные во временные таблицы

        #region Учет оплат перед окончательным расчетом пени
        public ReturnsType PutUchetOplatPeni(IDbConnection conn_db, ParamCalc paramcalc)
        {
            // Организовываем цикл 
            Int32 count;
            string sqlString = " select distinct dat_prih from tt_tekOplP  order by dat_prih ";
            DataTable dtprih = ClassDBUtils.OpenSQL(sqlString, conn_db, ClassDBUtils.ExecMode.Exception).GetData();

            foreach (DataRow r in dtprih.Rows)
            { 
                string date1 = Convert.ToString(r["dat_prih"]);
                // Делаем исправление суммы долга по всем записям с такой даты , максимальное количество update 
                /*
                sqlString = " update tt_prev_charge set " +
                            " rashod=rashod+rashodp* "+sNvlWord+"((select sum((a.dat_prih-dat_s)*a.sref)/100 from  tt_tekOplP a " +
                            " where (tt_prev_charge.num_ls =a.num_ls or a.num_ls=0) and a.dat_prih='" + date1 + "'),0),   " +
                            " rashodp=rashodp-"+sNvlWord+"((select sum(a.sum_prih) from tt_tekOplP a " +
                            " where (a.num_ls=tt_prev_charge.num_ls or a.num_ls=0 ) and a.dat_prih= '" + date1 + "' ,0)," +
                            " dat_s="+sNvlWord+"((select max(a.dat_prih) from tt_tekOplP a where (tt_prev_charge.num_ls =a.num_ls or a.num_ls=0) " +
                            " and a.dat_prih='" + date1 + "'),tt_prev_charge.dat_s) ";
                 sqlString = "update tt_prev_charge set  "+
                            " rashod  ="+sNvlWord+"(rashod,0)+"+sNvlWord+"(rashodp,0)* "+sNvlWord+"((select sum((a.dat_prih-dat_s)*a.sref)/100 from  tt_tekOplP a    where (tt_prev_charge.num_ls =a.num_ls or a.num_ls=0) and a.dat_prih='" + date1.Substring(0,10) + "') ,0), " +
                            " rashodp ="+sNvlWord+"(rashodp,0)-"+sNvlWord+"((select sum(a.sum_prih) from tt_tekOplP a  where (a.num_ls=tt_prev_charge.num_ls or a.num_ls=0 ) and a.dat_prih= '" + date1.Substring(0, 10) + "') ,0) " +
                            ", dat_s="+sNvlWord+"((select max(a.dat_prih) from tt_tekOplP a  where (tt_prev_charge.num_ls =a.num_ls or a.num_ls=0)  and a.dat_prih='" + date1.Substring(0, 10) + "'),tt_prev_charge.dat_s) "; 
 
                
                sqlString = "update tt_prev_charge set  " +
                            " rashod  ="+sNvlWord+"(rashod,0)+"+sNvlWord+"(rashodp,0)* "+sNvlWord+"((select sum((a.dat_prih-dat_s)*a.sref)/100 from  tt_tekOplP a  "+
                            " where (tt_prev_charge.num_ls =a.num_ls or a.num_ls=0) and a.dat_prih='" + date1.Substring(0, 10) + "') ,0), " +
                            " rashodp ="+sNvlWord+"(rashodp,0)-"+sNvlWord+"((select sum(a.sum_prih) from tt_tekOplP a  where (a.num_ls=tt_prev_charge.num_ls or a.num_ls=0 )"+
                            " and a.dat_prih= '" + date1.Substring(0, 10) + "') ,0) " +
                            ", dat_s="+sNvlWord+"((select max(a.dat_prih) from tt_tekOplP a  where (tt_prev_charge.num_ls =a.num_ls or a.num_ls=0) "+
                            "  and a.dat_prih='" + date1.Substring(0, 10) + "'),tt_prev_charge.dat_s) "; 

                */
                sqlString =  " update tt_prev_charge set dat_po=dat_s " ;
#if PG
                count = ClassDBUtils.ExecSQL(sqlString, conn_db, ClassDBUtils.ExecMode.Exception).resultAffectedRows;
#else
                ClassDBUtils.ExecSQL(sqlString, conn_db, ClassDBUtils.ExecMode.Exception);
                count = ClassDBUtils.GetAffectedRowsCount(conn_db);
#endif
               sqlString =  
                   " update tt_prev_charge set "+   
                   " rashod  ="+sNvlWord+"(rashod,0)+"+sNvlWord+"(rashodp,0)* "+sNvlWord+"((select sum((a.dat_prih-dat_s)*a.sref)/100 from  tt_tekOplP a "+   
                   " where (tt_prev_charge.num_ls =a.num_ls  or ( a.num_ls=0 and a.num=0)) and a.dat_prih='" + date1.Substring(0, 10) + "') ,0), "+
                   " rashodp ="+sNvlWord+"(rashodp,0)-"+sNvlWord+"((select sum(a.sum_prih) from tt_tekOplP a  "+
                   " where (a.num_ls=tt_prev_charge.num_ls  or ( a.num_ls=0 and a.num=0) ) and a.dat_prih='" + date1.Substring(0, 10) + "') ,0) , "+
                   " dat_s="+sNvlWord+"((select max(a.dat_prih) from tt_tekOplP a  where (tt_prev_charge.num_ls =a.num_ls  or ( a.num_ls=0 and a.num=0)) "+
                   "  and a.dat_prih='" + date1.Substring(0, 10) + "'),tt_prev_charge.dat_s), " +
                   " sref ="+sNvlWord+"((select a.sref from tt_tekOplP a  where (tt_prev_charge.num_ls =a.num_ls "+
                   " or ( a.num_ls=0 and a.num=0)) and a.dat_prih='" + date1.Substring(0, 10) + "'),0), "+
                   " sum_prih ="+sNvlWord+"((select sum(a.sum_prih) from tt_tekOplP a "+
                   " where (tt_prev_charge.num_ls =a.num_ls  or ( a.num_ls=0 and a.num=0)) and a.dat_prih='" + date1.Substring(0, 10) + "'),0) ";
#if PG
               count = ClassDBUtils.ExecSQL(sqlString, conn_db, ClassDBUtils.ExecMode.Exception).resultAffectedRows;
#else
                ClassDBUtils.ExecSQL(sqlString, conn_db, ClassDBUtils.ExecMode.Exception);
               count = ClassDBUtils.GetAffectedRowsCount(conn_db);
#endif
               sqlString = 
                   " update tt_prev_charge set rashod "+
                   " ="+sNvlWord+"(rashod,0)+("+sNvlWord+"(rashodp,0)-"+sNvlWord+"(sum_prih,0))* (dat_s-dat_po)*sref/100 where dat_s='" + date1.Substring(0, 10) + "' ";
#if PG
               count = ClassDBUtils.ExecSQL(sqlString, conn_db, ClassDBUtils.ExecMode.Exception).resultAffectedRows;
#else
                ClassDBUtils.ExecSQL(sqlString, conn_db, ClassDBUtils.ExecMode.Exception);
               count = ClassDBUtils.GetAffectedRowsCount(conn_db);
#endif

               sqlString = 
                   " update tt_prev_charge set note=' '||trim(note)||';п= '|| " +
                   " trunc(rashod,2) ||'='||trunc(rashodp,2)||' (оп='||trunc(sum_prih,2)||')* '||(dat_s-dat_po)||'* пр.в/д='||sref||'/'||100 ||' '||dat_s||';'";
#if PG
               count = ClassDBUtils.ExecSQL(sqlString, conn_db, ClassDBUtils.ExecMode.Exception).resultAffectedRows;
#else
                ClassDBUtils.ExecSQL(sqlString, conn_db, ClassDBUtils.ExecMode.Exception);
               count = ClassDBUtils.GetAffectedRowsCount(conn_db);
#endif

               if (count == 1)
               {
#if PG
                   sqlString = " select note from tt_prev_charge limit 1";
#else
                   sqlString = " select first 1 note from tt_prev_charge ";
#endif
                   DataTable dtnote = ClassDBUtils.OpenSQL(sqlString, conn_db, ClassDBUtils.ExecMode.Exception).GetData();

                   foreach (DataRow rr in dtnote.Rows)
                   {
                       string note1 = Convert.ToString(rr["note"]);
                       MonitorLog.WriteLog("Протокол расчета пени для л.с: " + note1, MonitorLog.typelog.Info, 1, 2, true);
                       break;
                   }
               }

            }

            return new ReturnsType();
        }
        #endregion Учет оплат перед окончательным расчетом пени

        #endregion Функции, участвующие в расчете Пени


    }
    #endregion здесь находятся функции для подсчета пени (partial class DbCalc)
    
}
#endregion Расчет пени
