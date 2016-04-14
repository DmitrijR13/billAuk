using System.Diagnostics;

#region подключаемые пространства

using System;
using System.Linq;
using System.Data;
using System.Text;
using System.Collections.Generic;
using System.Globalization;
using STCLINE.KP50.Global;
using Bars.KP50.Utils;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.IFMX.Kernel.source.CommonType;
#endregion подключаемые пространства
#region Расчет пени
namespace STCLINE.KP50.DataBase
{
    #region здесь находятся функции для подсчета пени (partial class DbCalc)
    public partial class DbCalcCharge : DataBaseHead
    {

        #region Переменные и константы расчета
        string d85, d99, pdat_s, pdat_po;

        //DateTime dd85, dd99;

        #endregion Переменные и константы расчета

        #region Главный цикл расчета пени
        [Obsolete]
        public bool CalcRasPeni(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc, out Returns ret)
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

                // Найти перечень лицевых счетов у которых есть услуга пени 
                if (!ExistsLsWithPeni(conn_db, paramcalc).GetReturns().result)
                {
                    // нет лс с услугой пени 
                    return false;
                }
                return true;

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
        public ReturnsType GetParametrPeni(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc)
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
                        " and dat_s <= " + sDefaultSchema + "mdy(" + paramcalc.calc_mm.ToString("00") + "," +
                        System.DateTime.DaysInMonth(paramcalc.calc_yy, paramcalc.calc_mm).ToString("00") + ","
                        + paramcalc.calc_yy.ToString("0000") + ")" +
                        " and dat_po>= " + sDefaultSchema + "mdy(" + paramcalc.calc_mm + ",01," + paramcalc.calc_yy + ") ";
            DataTable dt = ClassDBUtils.OpenSQL(sqlString, conn_db, ClassDBUtils.ExecMode.Exception).GetData();



            foreach (DataRow r in dt.Rows)
            {
                if (Convert.ToInt32(r["nzp_prm"]) == 85)
                {
                    d85 = Convert.ToString(r["val_prm"]);
                    pdat_s = Convert.ToString(r["dat_s"]);
                    pdat_po = Convert.ToString(r["dat_po"]);

                    // Добавим каждое изменение ставки рафинанасирования в таблицу 
                    sqlString =
                        "insert into t_stavka_ref(dat_s,dat_po,stavka) values(cast('" + pdat_s.Substring(0, 10) + "' as date), cast('" +
                        pdat_po.Substring(0, 10) + "' as date)," + d85.Trim() + ")";
                    ClassDBUtils.ExecSQL(sqlString, conn_db, ClassDBUtils.ExecMode.Exception);
                    //   count = ClassDBUtils.GetAffectedRowsCount(conn_db);
                };

                if (Convert.ToInt32(r["nzp_prm"]) == 99) { d99 = Convert.ToString(r["val_prm"]); };
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
        public ReturnsType ExistsLsWithPeni(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc)
        //-----------------------------------------------------------------------------
        {
            #region инициализация переменных
            bool b_peni = true;
            d85 = "";
            d99 = "";
            #endregion инициализация переменных

            #region Проверка наличия услуги пени в рассматриваемом множестве квартир
            //если услуги пени нет, то прекращаем выполнение 
            Returns ret;
            var countLsWithPeni = CastValue<int>(ExecScalar(conn_db, "SELECT count(*) FROM temp_table_tarif WHERE nzp_serv=500 ", out ret, true));
            if (countLsWithPeni == 0)
            {
                b_peni = false;
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
                PutSumPeniForCharge(conn_db, paramcalc);
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
                SaveGku(conn_db, paramcalc);
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
        public ReturnsType CreateTempTableForPeni(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc)
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
        public ReturnsType GetSumInsaldoForPeni(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc)
        {
            Gku gku = new Gku(paramcalc);
            string sSql;
            Int32 count;
            #region  Создание временных таблиц
            sSql = "  drop table t_prev_charge ";
            ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Log);
            sSql =
                " create temp table t_prev_charge( nzp_kvar integer,num_ls integer, nzp_dom integer, " +
                "sum_insaldo " + sDecimalType + "(16,6), real_charge " + sDecimalType + "(16,6), reval " + sDecimalType + "(16,6), sum_money " + sDecimalType + "(14,2), " +
                " nzp_supp integer, nzp_frm integer,dat_s date,dat_po date, sum_prih " + sDecimalType + "(14,4) )" + sUnlogTempTable;
            ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception);

            sSql = "  drop table tt_prev_charge ";
            ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Log);
            sSql =
                " create temp table tt_prev_charge( nzp_kvar integer,num_ls integer, nzp_dom integer, " +
                "sum_insaldo " + sDecimalType + "(16,6), real_charge " + sDecimalType + "(16,6), reval " + sDecimalType + "(16,6), sum_money " + sDecimalType + "(14,2), " +
                "nzp_supp integer, nzp_frm integer, rashod " + sDecimalType + "(16,6), rashodP " + sDecimalType + "(16,6),dat_s date,dat_po date," +
                "sref " + sDecimalType + "(14,4) , sum_prih " + sDecimalType + "(14,4),note char(250) )" + sUnlogTempTable;
            ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception);
            #endregion  Создание временных таблиц

            sSql =
                 " insert into t_prev_charge( nzp_kvar , num_ls, nzp_dom , sum_insaldo, real_charge, reval, sum_money , nzp_supp, nzp_frm, dat_s,dat_po ) " +
                 " SELECT a.nzp_kvar ,num_ls, 0 nzp_dom , a.sum_insaldo,case when a.real_charge<0 then " +
                 " a.real_charge else 0 end as real_charge, a.reval, a.sum_money , 0  nzp_supp, 0 nzp_frm , '" +
                     System.DateTime.DaysInMonth(paramcalc.prev_calc_yy, paramcalc.prev_calc_mm).ToString("00") + "." + paramcalc.prev_calc_mm.ToString("00") + "." + paramcalc.prev_calc_yy.ToString("0000") + "' as dat_s," +
                     "'" + System.DateTime.DaysInMonth(paramcalc.calc_yy, paramcalc.calc_mm).ToString("00") + "." + paramcalc.calc_mm.ToString("00") + "." + paramcalc.calc_yy.ToString("0000") + "' as dat_po" +
                 " FROM " + gku.prevSaldoMon_charge + " a   " +
                 " WHERE a.dat_charge is null and a.nzp_serv>1  " +
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
                 " FROM t_prev_charge  " +
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
        public ReturnsType GetSumNewOplatForPeni(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc)
        {
            Gku gku = new Gku(paramcalc);
            string sSql, pstavka, pdat_s;
            Int32 count;
            #region Удаление временных таблиц

            sSql = " drop table t_tekOplP ";
            ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Log);
            sSql = " drop table tt_tekOplP ";
            ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Log);

            #endregion Удаление временных таблиц

            #region Создание временных таблиц

            sSql = " create temp table t_tekOplP " +
                   " ( num_ls integer,sum_prih " + sDecimalType + "(14,2),dat_prih date, sref " + sDecimalType + "(16,6), " +
                   " num smallint, dat_s date ,dat_po date ) " + sUnlogTempTable;
            ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception);
            sSql =
                " create temp table tt_tekOplP " +
                " ( num_ls integer default 0,sum_prih " + sDecimalType + "(14,2),dat_prih date, sref " + sDecimalType + "(16,6), " +
                " num smallint, dat_s date ,dat_po date )" + sUnlogTempTable;
            ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception);
            #endregion Создание временных таблиц

            #region выбрать оплаты
            sSql = " insert into t_tekOplP(num_ls,sum_prih,dat_prih,sref,num )" +
                 " select a.num_ls, a.sum_prih ,  a.dat_prih,0 sref,1  num   " +
                 " from " + gku.calc_tosupplXX + " a " +
                 " where a.num_ls in (select nzp_kvar from tt_prev_charge) and dat_prih >='01." + paramcalc.cur_mm.ToString("00") + "." + paramcalc.cur_yy.ToString("0000") + "'";

#if PG
            count = ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception).resultAffectedRows;
#else
            ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception);
            count = ClassDBUtils.GetAffectedRowsCount(conn_db);
#endif
            sSql = " insert into tt_tekOplP(num_ls,dat_prih,sref,num,sum_prih )" +
                " select a.num_ls,  a.dat_prih,0 sref,1 num ,sum( a.sum_prih)  from t_tekOplP a  group by 1,2,3,4 ";

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
                " select distinct  0, case when dat_s < " + sDefaultSchema + "mdy(" + paramcalc.calc_mm + ",1," + paramcalc.calc_yy +
                ")  then " + sDefaultSchema + "mdy(" + paramcalc.calc_mm + ",1," + paramcalc.calc_yy + ") else dat_s end  as dat_s " +
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
                   " values('" + System.DateTime.DaysInMonth(paramcalc.calc_yy, paramcalc.calc_mm).ToString("00") + "." + paramcalc.calc_mm.ToString("00") + "." + paramcalc.calc_yy.ToString("0000") + "',0,0,0) ";
#if PG
            count = ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception).resultAffectedRows;
#else
            ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception);
            count = ClassDBUtils.GetAffectedRowsCount(conn_db);
#endif


            #endregion Добавка дат ставок рефинансирования

            #region Проставить ставку рефинансирования в оплатах
            sSql = "select dat_s , stavka from t_stavka_ref order by dat_s desc ";
            DataTable dtS = ClassDBUtils.OpenSQL(sSql, conn_db).GetData();
            foreach (DataRow r in dtS.Rows)
            {
                pstavka = Convert.ToString(r["stavka"]);
                pdat_s = Convert.ToString(r["dat_s"]);

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
        public ReturnsType GetSumMinusRevalForPeni(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc)
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
                   " (nzp_kvar integer,num_ls integer,reval " + sDecimalType + "(14,2),dat_charge date, month_ integer,year_ integer)" + sUnlogTempTable;
            ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception);

            sSql = " drop table tt_prev_revals ";
            ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Log);

            sSql = " create temp table tt_prev_revals " +
                   " (nzp_kvar integer,num_ls integer,reval " + sDecimalType + "(14,2),dat_charge date, month_ integer,year_ integer)" + sUnlogTempTable;
            ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception);
            #endregion Создание удаление временных таблиц

            var size = "";
#if PG
            size = "(10)";
#endif

            string sqlString =
            " select distinct  month_,year_ , substr(year_" + sConvToChar + size + ",3,2) as year_2  from " + gku.calc_lnkchargeXX + " a " +
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
                    " select a.nzp_kvar ,  a.num_ls ,  a.reval  , a.dat_charge, " + perMon.ToString("00") + "," + perYear.ToString("00") +
                    " from " + paramcalc.pref + "_charge_" + perYear.ToString("00") + tableDelimiter + "charge_" + perMon.ToString("00") + " a " +
                    " where a.num_ls in (select b.num_ls from tt_prev_charge b  where b.rashodp>0) and a.reval<0  and a.nzp_serv >1 and a.nzp_serv <>500  " +
                    " and a.dat_charge >='28." + paramcalc.prev_calc_mm.ToString("00") + "." + paramcalc.prev_calc_yy.ToString("0000") + "'";

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
                " select  a.num_ls , a.month_,  a.year_, max(a.dat_charge ) " +
                " from t_prev_revals a group by 1,2,3 ";

#if PG
            count = ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception).resultAffectedRows;
#else
            ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception);
            count = ClassDBUtils.GetAffectedRowsCount(conn_db);
#endif
            sSql =
                " insert into tt_prev_revals( nzp_kvar  ,    num_ls ,   month_,    year_, reval  )" +
                " select a.nzp_kvar  ,  a.num_ls , a.month_,  a.year_, sum(a.reval) " +
                " from t_prev_revals a , t_maxp_rev b where " +
                " a.num_ls=b.num_ls and a.month_=b.month_ and a.year_=b.year_ and a.dat_charge=b.dat_charge " + " group by 1,2,3,4 ";


#if PG
            count = ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception).resultAffectedRows;
#else
            ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception);
            count = ClassDBUtils.GetAffectedRowsCount(conn_db);
#endif

            // Уменьшить суммы перерасчетов за все периоды в которых был перерасчет 
            sSql =
                " update tt_prev_charge set note=' '||" + sNvlWord + "(note,'')||' вх.сальдо: '||rashodp||'+'|| " + sNvlWord + "((select sum(a.reval) " +
                " from tt_prev_revals a  " +
                " where a.num_ls=tt_prev_charge.num_ls ),0) ," +
                " rashodp=" + sNvlWord + "(rashodp,0)+" + sNvlWord + "((select sum(a.reval) " +
                " from tt_prev_revals a  where a.num_ls=tt_prev_charge.num_ls ),0) ";
#if PG
            count = ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception).resultAffectedRows;
#else
            ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception);
            count = ClassDBUtils.GetAffectedRowsCount(conn_db);
#endif
            // Удалить из расмотрения все суммы <=0
            sSql = " delete from tt_prev_charge where rashodp<=0 ";
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
        public ReturnsType GetStavRefinans(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc)
        {
            return new ReturnsType();
        }
        #endregion Выбрать ставки рефинансирования в текущем месяце

        #region Выбрать отрицательные перекидки текущего месяца (предыдущий учтен при выборке данных )
        public ReturnsType GetSumMinusPerekidkaForPeni(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc)
        {
            Gku gku = new Gku(paramcalc);
            string sSql;
            Int32 count;

            #region Создание удаление временных таблиц
            sSql = " drop table t_prev_realcharge ";
            ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Log);
            sSql = " create temp table t_prev_realcharge( nzp_kvar integer,num_ls integer,  real_charge " + sDecimalType + "(16,6) ) " + sUnlogTempTable;
            ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception);
            sSql = " drop table tt_prev_realcharge ";
            ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Log);
            sSql = " create temp table tt_prev_realcharge( nzp_kvar integer,num_ls integer,  real_charge " + sDecimalType + "(16,6) ) " + sUnlogTempTable;
            ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception);
            #endregion Создание удаление временных таблиц

            sSql = " insert into t_prev_realcharge(nzp_kvar , num_ls , real_charge ) " +
                    " select a.nzp_kvar , a.num_ls , case when a.real_charge<0 then a.real_charge else 0 end as real_charge " +
                    " from " + gku.curSaldoMon_charge + " a  where a.nzp_kvar in (select b.nzp_kvar from tt_prev_charge  b ) and a.nzp_serv >1 and a.nzp_serv <>500 ";

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
                " update tt_prev_charge set rashodp=" + sNvlWord + "(rashodp,0)+" + sNvlWord + "((select sum(a.real_charge) from tt_prev_realcharge a  " +
                " where a.num_ls=tt_prev_charge.num_ls ),0) " +
                ", note=trim(note) ||' вхс-пер:'|| " + sNvlWord + "(rashodp,0)||'-'||   " + sNvlWord + " ((select sum(a.real_charge) from tt_prev_realcharge a where a.num_ls=tt_prev_charge.num_ls ),0) ";

#if PG
            count = ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception).resultAffectedRows;
#else
            ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception);
            count = ClassDBUtils.GetAffectedRowsCount(conn_db);
#endif
            // Удалить из расмотрения все суммы <=0
            sSql = " delete from tt_prev_charge where rashodp<=0 ";
            //DbWorkUser db = new DbWorkUser();

            return new ReturnsType();
        }

        #endregion Выбрать отрицательные перекидки текущего месяца (предыдущий учтен при выборке данных )

        #region Положить суммы в таблицы расходов
        public ReturnsType SaveGku(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc)
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
        public ReturnsType PutSumPeniForCharge(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc)
        {
            string sSql;
            Gku gku = new Gku(paramcalc);
            sSql =
                " update tt_prev_charge " +
                " set  rashodp = ( case when ((" + sNvlWord + "(tt_prev_charge.sum_insaldo,0)+" + sNvlWord + "(case when tt_prev_charge.reval<0 then tt_prev_charge.reval else 0 end,0)  " +
                " + " + sNvlWord + "(case when tt_prev_charge.real_charge<0 then tt_prev_charge.real_charge else 0 end,0) - " + sNvlWord + "(tt_prev_charge.sum_money,0))  )>0   " +
                " then   ((" + sNvlWord + "(tt_prev_charge.sum_insaldo,0)+" + sNvlWord + "(case when tt_prev_charge.reval<0 then tt_prev_charge.reval else 0 end,0)  " +
                " + " + sNvlWord + "(case when tt_prev_charge.real_charge<0 then tt_prev_charge.real_charge else 0 end,0) - " + sNvlWord + "(tt_prev_charge.sum_money,0)))   else 0 end) ";
#if PG
            Int32 count = ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception).resultAffectedRows;
#else
            ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception);
            Int32 count = ClassDBUtils.GetAffectedRowsCount(conn_db);
#endif
            sSql =
                " update tt_prev_charge " +
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
                " update tt_prev_charge set nzp_supp = (select max(a.nzp_supp) from temp_table_tarif a where a.nzp_kvar=tt_prev_charge.nzp_kvar " +
                " and a.nzp_serv=500 ) ";
#if PG
            count = ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception).resultAffectedRows;
#else
            ClassDBUtils.ExecSQL(sSql, conn_db, ClassDBUtils.ExecMode.Exception);
            count = ClassDBUtils.GetAffectedRowsCount(conn_db);
#endif

            sSql = " update tt_prev_charge set nzp_frm = (select max(a.nzp_frm) from temp_table_tarif a where a.nzp_kvar=tt_prev_charge.nzp_kvar " +
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
        public ReturnsType PutUchetOplatPeni(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc)
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
                sqlString = " update tt_prev_charge set dat_po=dat_s ";
#if PG
                count = ClassDBUtils.ExecSQL(sqlString, conn_db, ClassDBUtils.ExecMode.Exception).resultAffectedRows;
#else
                ClassDBUtils.ExecSQL(sqlString, conn_db, ClassDBUtils.ExecMode.Exception);
                count = ClassDBUtils.GetAffectedRowsCount(conn_db);
#endif
                sqlString =
                    " update tt_prev_charge set " +
                    " rashod  =" + sNvlWord + "(rashod,0)+" + sNvlWord + "(rashodp,0)* " + sNvlWord + "((select sum((a.dat_prih-dat_s)*a.sref)/100 from  tt_tekOplP a " +
                    " where (tt_prev_charge.num_ls =a.num_ls  or ( a.num_ls=0 and a.num=0)) and a.dat_prih='" + date1.Substring(0, 10) + "') ,0), " +
                    " rashodp =" + sNvlWord + "(rashodp,0)-" + sNvlWord + "((select sum(a.sum_prih) from tt_tekOplP a  " +
                    " where (a.num_ls=tt_prev_charge.num_ls  or ( a.num_ls=0 and a.num=0) ) and a.dat_prih='" + date1.Substring(0, 10) + "') ,0) , " +
                    " dat_s=" + sNvlWord + "((select max(a.dat_prih) from tt_tekOplP a  where (tt_prev_charge.num_ls =a.num_ls  or ( a.num_ls=0 and a.num=0)) " +
                    "  and a.dat_prih='" + date1.Substring(0, 10) + "'),tt_prev_charge.dat_s), " +
                    " sref =" + sNvlWord + "((select a.sref from tt_tekOplP a  where (tt_prev_charge.num_ls =a.num_ls " +
                    " or ( a.num_ls=0 and a.num=0)) and a.dat_prih='" + date1.Substring(0, 10) + "'),0), " +
                    " sum_prih =" + sNvlWord + "((select sum(a.sum_prih) from tt_tekOplP a " +
                    " where (tt_prev_charge.num_ls =a.num_ls  or ( a.num_ls=0 and a.num=0)) and a.dat_prih='" + date1.Substring(0, 10) + "'),0) ";
#if PG
                count = ClassDBUtils.ExecSQL(sqlString, conn_db, ClassDBUtils.ExecMode.Exception).resultAffectedRows;
#else
                ClassDBUtils.ExecSQL(sqlString, conn_db, ClassDBUtils.ExecMode.Exception);
               count = ClassDBUtils.GetAffectedRowsCount(conn_db);
#endif
                sqlString =
                    " update tt_prev_charge set rashod " +
                    " =" + sNvlWord + "(rashod,0)+(" + sNvlWord + "(rashodp,0)-" + sNvlWord + "(sum_prih,0))* (dat_s-dat_po)*sref/100 where dat_s='" + date1.Substring(0, 10) + "' ";
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

#endregion Расчет пени
    #region Новый алгоритм расчета пени для Тулы


    public class DbCalcPeni : DataBaseHead
    {
        private const string tableLastDate = "t_lastdate_debt"; //таблица с последними рассчитанными задолженностями по лс
        private readonly string TablePeriods = "t_ls_periods_" + DateTime.Now.Ticks; //таблица с окончательными периодами по лс

        private string TableProv = "peni_provodki"; //мастер-таблица проводок 
        private string TablePeniDebt = "peni_debt"; //мастер-таблица задолжностей   
        private string TablePeniCalc = "peni_calc"; //мастер-таблица рассчитанных пени   
        private string TablePeniOff = "peni_off"; //мастер-таблица с причинами отмены начисления пени
        private string TablePeniProvodkiRefs = "peni_provodki_refs"; //мастер-таблица со связями между проводками и задолженностями
        private string TablePeniDebtRefs = "peni_debt_refs"; //мастер-таблица со связями между задолженностями и пенями

        private readonly string TablePeniSettings = Points.Pref + sKernelAliasRest + "peni_settings"; //таблица с настройками по пени

        private readonly string TableTempPeniDebtUp = "t_peni_debt_up_" + DateTime.Now.Ticks; //темповая таблица с задолженностями по начислениями
        private readonly string TableTempPeniDebtDown = "t_peni_debt_down_" + DateTime.Now.Ticks; //темповая таблица с задолженностями 
        private readonly string TempUnionDebts = "t_union_debts_" + DateTime.Now.Ticks;  //таблица с объединением сумм пени: начисленных и перерасчитываемых

        private readonly string TableListLs = "t_list_ls_" + DateTime.Now.Ticks; //темповая таблица со всеми рассчитываемыми в данный момент лс
        private readonly string TableNoCalculetedProv = "t_no_uchet_prov_" + DateTime.Now.Ticks; //темповая таблица со всеми неучтенными проводками
        private readonly string TableStartCalc = "t_start_calc_" + DateTime.Now.Ticks; //темповая таблица с началом периода расчета пени по лс

        private readonly string TableUKPeriods = "t_uk_periods_" + DateTime.Now.Ticks; //темповая таблица датами начала расчета пени на уровне УК и кол-вом дней до даты обязательств
        private readonly string TempPeriodsProhibit = "temp_proh_periods_" + DateTime.Now.Ticks; ////периоды запрета начисления пени на задолженности на уровне ЛС
        private readonly string TablePeriodsMustCalcForPeni = "t_periods_must_calc_" + DateTime.Now.Ticks; //таблица с периодами из must_calc - период перерасчета пени (переформировываем проводки)
        private readonly string TableWithPeniProcents = "t_peni_percent_" + DateTime.Now.Ticks; //таблица с действующими значениями параметров процентов для пени (0, 1/300, 1/130)


        private readonly string TablePeriodsNoCalcPeni = "t_periods_no_calc_peni_" + DateTime.Now.Ticks; //таблица с периодами из peni_no_calc - не учитывать пени по услуге,договору
        /// <summary> типы проводок соответствующие оплатам </summary>
        private readonly string PaymentsProvs = (int)s_prov_types.Payment + "," + (int)s_prov_types.PaymentFromSupp + "," + (int)s_prov_types.Perekidki;
        /// <summary>
        /// Типы проводок по которым обязательства наступают день в день, а не на следующий
        /// </summary>
        private readonly string ProvWithOutShift = (int)s_prov_types.InSaldo + "," +
                                                   (int)s_prov_types.OldCalculatedDebt + "," +
                                                   (int)s_prov_types.CalculatedDebt;
        private bool withProvCurMonth = false; //учет проводок в текущем рас.месяце
        //public string TempTable = "UNLOGGED"; //тип временных таблиц (UNLOGGED - для отладки, TEMP - релиз)
        private string DayNewPeni = "'01.01.2016'::DATE";
        public bool CalcPeniMain(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc, out Returns ret)
        {
            TableProv = paramcalc.pref + sDataAliasRest + TableProv;
            TablePeniDebt = paramcalc.pref + sDataAliasRest + TablePeniDebt;
            TablePeniCalc = paramcalc.pref + sDataAliasRest + TablePeniCalc;
            TablePeniProvodkiRefs = paramcalc.pref + sDataAliasRest + TablePeniProvodkiRefs;
            TablePeniDebtRefs = paramcalc.pref + sDataAliasRest + TablePeniDebtRefs;

            //нужно пробросить в paramcalc nzp_user!!!
            if (paramcalc.nzp_user <= 0)
            {
                paramcalc.nzp_user = 1;
            }

            var curCalcDate = new DateTime(paramcalc.cur_yy, paramcalc.cur_mm, 1); //текущий расчетный месяц
            //1455|Учет проводок для пени в текущем расчетном месяце|||bool||10||||
            withProvCurMonth = DBManager.GetParamValueInPeriod<bool>(conn_db, paramcalc.pref, 1455, 10,
                curCalcDate, curCalcDate.WithLastDayMonth()
                , out ret);

            //определяем дату окончания учета проводок в текущем расчете
            //по умолчанию берем последний день закрытого месяца
            var dateTo = new DateTime(paramcalc.cur_yy, paramcalc.cur_mm, 1).AddDays(-1); //дата окончания периода за которое считаем пени
            //если включен расчет пени и за текущий месяц, от необходимо учитывать параметр "День месяца, до которого начисляются пени"
            if (withProvCurMonth)
            {
                //учитываем проводки и за текущий месяц
                dateTo = new DateTime(curCalcDate.Year, curCalcDate.Month, DateTime.DaysInMonth(curCalcDate.Year, curCalcDate.Month));
            }

            try
            {
                //получение процентов по пени
                ret = GetActivePeniProcents(conn_db, paramcalc);
                if (!ret.result)
                {
                    return false;
                }

                //получаем периоды расчета пени по УК
                ret = GetUKParams(conn_db, paramcalc);
                if (!ret.result)
                {
                    return false;
                }

                //получаем список ЛС для расчета пени
                ret = InitListLsForPeni(conn_db);
                if (!ret.result)
                {
                    return false;
                }
                if (ret.tag == -1)
                {
                    MonitorLog.WriteLog("Пропуск CalcRasPeni: услуга пени отсутствует", MonitorLog.typelog.Info, 1, 2, true);
                    return true;
                }

                #region в разработке
                //получаем периоды перерасчета для пени
                //ret = GetMustCalcForPeni(conn_db, paramcalc);
                //if (!ret.result)
                //{
                //    return false;
                //}
                #endregion в разработке

                //получаем периоды по которым будем рассчитывать задолженности
                //за начало первого периода будем брать окончание периода последней задолженности или если его нет, то дату начала периода за которое считаем пени
                //за окончание последнего периода будем брать дату окончания периода за которое считаем пени
                ret = CreateListPeriodDebt(conn_db, paramcalc, dateTo);
                if (!ret.result)
                {
                    return false;
                }
                MonitorLog.WriteLog("CalcRasPeni: периоды расчетов задолженностей получены", MonitorLog.typelog.Info, 1,
                    2, true);

                //запись в реестр
                var reestrId = CreateRecordInReestr(conn_db, peni_actions_type.InsertCalcDebtAndPeni,
                    new DateTime(dateTo.Year, dateTo.Month, 1), dateTo, paramcalc);
                if (reestrId <= 0)
                    return false;

                //рассчитываем задолженности
                ret = CalcDebts(conn_db, paramcalc, reestrId);
                if (!ret.result)
                    return false;
                MonitorLog.WriteLog("CalcRasPeni: рассчитали задолженности по периодам", MonitorLog.typelog.Info, 1, 2,
                    true);

                //рассчитываем и размазываем переплаты, учитываем дельты
                ret = CalcOverPayments(conn_db, paramcalc, reestrId);
                if (!ret.result)
                    return false;
                MonitorLog.WriteLog("CalcRasPeni: распределили переплаты по периодам", MonitorLog.typelog.Info, 1, 2,
                    true);

                //рассчитываем пени
                ret = CalcPeni(conn_db, paramcalc, reestrId);
                if (!ret.result)
                    return false;
                MonitorLog.WriteLog("CalcRasPeni: рассчитали пени по периодам", MonitorLog.typelog.Info, 1, 2, true);

                //проставляем связи пени
                ret = CalcPeniFinaly(conn_db, paramcalc, reestrId);
                if (!ret.result)
                    return false;
                MonitorLog.WriteLog("CalcRasPeni: сохраняем пени", MonitorLog.typelog.Info, 1, 2, true);

                //сохраняем в таблицу рассчетов
                ret = SavePeniInCalcGku(conn_db, paramcalc, reestrId);
                if (!ret.result)
                    return false;
                MonitorLog.WriteLog("CalcRasPeni: сохранили пени в расходы", MonitorLog.typelog.Info, 1, 2, true);

                //удаление старых записей
                ret = DeleteOldRecords(conn_db, paramcalc, reestrId);
                if (!ret.result)
                    return false;

            }
            catch (Exception ex)
            {
                ret.result = false;
                MonitorLog.WriteLog("Ошибка расчета пени по новому алгоритму: " + ex.Message, MonitorLog.typelog.Error,
                    true);
                return false;
            }
            finally
            {
                ExecSQL(conn_db, "DROP TABLE " + TableTempPeniDebtUp, false);
                ExecSQL(conn_db, "DROP TABLE " + TableTempPeniDebtDown, false);
                ExecSQL(conn_db, "DROP TABLE " + TempUnionDebts, false);
                ExecSQL(conn_db, "DROP TABLE " + TableListLs, false);
                ExecSQL(conn_db, "DROP TABLE " + TableNoCalculetedProv, false);
                ExecSQL(conn_db, "DROP TABLE " + TableStartCalc, false);
                ExecSQL(conn_db, "DROP TABLE " + TableUKPeriods, false);
                ExecSQL(conn_db, "DROP TABLE " + TempPeriodsProhibit, false);
                ExecSQL(conn_db, "DROP TABLE " + TablePeriodsMustCalcForPeni, false);
                ExecSQL(conn_db, "DROP TABLE " + TableWithPeniProcents, false);
                ExecSQL(conn_db, "DROP TABLE " + TablePeriods, false);
                ExecSQL(conn_db, "DROP TABLE " + TablePeriodsNoCalcPeni, false);
                if (!ret.result)
                {
                    MonitorLog.WriteLog("Ошибка расчета пени: " + ret.text, MonitorLog.typelog.Error, true);
                }
            }
            return true;
        }

        /// <summary>
        /// Получение действующих процентов по пени
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="paramcalc"></param>
        /// <returns></returns>
        private Returns GetActivePeniProcents(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc)
        {
            var sql = " CREATE TEMP TABLE " + TableWithPeniProcents + " AS " +
                      " SELECT nzp_key, nzp_prm, val_prm::NUMERIC as peni_percent, dat_s as date_from, " +
                      " dat_po as date_to" +
                      " FROM " + paramcalc.pref + sDataAliasRest + "prm_10" +
                      " WHERE nzp_prm IN (2118, 85, 2119)" +
                      " AND is_actual=1";
            var ret = ExecSQL(conn_db, sql, true);
            if (!ret.result) return ret;

            ret = ExecSQL(conn_db,
            " Create unique index ix1_" + TableWithPeniProcents + "_1 on " + TableWithPeniProcents +
            " (nzp_key,nzp_prm,date_from,date_to) ", true);
            if (!ret.result) return ret;
            ret = ExecSQL(conn_db,
             " Create index ix2_" + TableWithPeniProcents + "_1 on " + TableWithPeniProcents +
             " (nzp_prm,date_from,date_to) ", true);
            if (!ret.result) return ret;

            ExecSQL(conn_db, sUpdStat + " " + TableWithPeniProcents);


            //поиск пересекающихся периодов
            sql = " SELECT COUNT(1)>0 FROM " + TableWithPeniProcents + " a, " + TableWithPeniProcents + " b " +
                  " WHERE  a.nzp_prm=b.nzp_prm AND (a.date_from, a.date_to) OVERLAPS (b.date_from, b.date_to) " +
                  " AND a.nzp_key<>b.nzp_key";
            var exists_dubles = ExecScalar<bool>(conn_db, sql, out ret, true);
            if (!ret.result) return ret;

            if (exists_dubles)
            {
                ret.text = "Имеются пересекающиеся периоды действия параметров процентов пени (2118, 85, 2119)!";
                ret.result = false;
                MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Error, true);
                return ret;
            }

            return ret;
        }


        public Returns DeleteOldRecords(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc, int reestrId)
        {
            //расчетный месяц
            var CalcMonth = new DateTime(paramcalc.cur_yy, paramcalc.cur_mm, 1);
            var s_CalcMonth = Utils.EStrNull(CalcMonth.ToShortDateString());
            var ret = Utils.InitReturns();
            //локальная таблица задолжностей
            var localTableDebtsUp = paramcalc.pref + "_charge_" + (CalcMonth.Year - 2000).ToString("00") + tableDelimiter +
                                  "peni_debt_" + CalcMonth.Year + CalcMonth.Month.ToString("00") + "_" + paramcalc.nzp_wp + "_up";
            //локальная таблица задолжностей с перерасчетными записями
            var localTableDebtsDown = paramcalc.pref + "_charge_" + (CalcMonth.Year - 2000).ToString("00") + tableDelimiter +
                              "peni_debt_" + CalcMonth.Year + CalcMonth.Month.ToString("00") + "_" + paramcalc.nzp_wp + "_down";
            //локальная таблица причин отмены  начисления пени
            var localTablePeniOff = paramcalc.pref + "_charge_" + (CalcMonth.Year - 2000).ToString("00") + tableDelimiter +
                                  "peni_off_" + CalcMonth.Year + CalcMonth.Month.ToString("00") + "_" + paramcalc.nzp_wp;
            //локальная таблица связей между пени и задолженностями
            var localTableDebtRefs = paramcalc.pref + "_charge_" + (CalcMonth.Year - 2000).ToString("00") + tableDelimiter +
                                  "peni_debt_refs_" + CalcMonth.Year + CalcMonth.Month.ToString("00") + "_" + paramcalc.nzp_wp;

            try
            {


            //удаляем старые записи задолженностей по выбранным ЛС
                var sql = " WITH deleted_rows AS ( DELETE FROM " + localTableDebtsUp + " l WHERE peni_actions_id<>" +
                          reestrId +
                          " AND l.date_calc=" + s_CalcMonth + " " +
                          " AND EXISTS (SELECT 1 FROM " + TableListLs + " ld WHERE l.nzp_kvar=ld.nzp_kvar )" +
                          " RETURNING id)" +
                          " DELETE FROM " + localTablePeniOff +
                          " p WHERE EXISTS (SELECT 1 FROM deleted_rows del WHERE del.id=p.peni_debt_id)";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                    return ret;

                //удаляем старые записи задолженностей по выбранным ЛС
                sql = " DELETE FROM " + localTableDebtsDown + " l WHERE peni_actions_id<>" + reestrId +
                      " AND l.date_calc=" + s_CalcMonth + " " +
                      " AND EXISTS (SELECT 1 FROM " + TableListLs + " ld WHERE l.nzp_kvar=ld.nzp_kvar)";
                ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
                return ret;

            //локальная таблица пени
                var localTablePeni = paramcalc.pref + "_charge_" + (CalcMonth.Year - 2000).ToString("00") +
                                     tableDelimiter +
                                  "peni_calc_" + CalcMonth.Year + "_" + paramcalc.nzp_wp;

                //удаляем старые записи пени по выбранным ЛС и связи по ним
                sql = " WITH deleted_rows AS (DELETE FROM " + localTablePeni + " l WHERE peni_actions_id<>" + reestrId +
                  " AND date_calc=" + s_CalcMonth + " " +
                      " AND EXISTS (SELECT 1 FROM " + TableListLs + " ld WHERE l.nzp_kvar=ld.nzp_kvar )" +
                      " RETURNING id)" +
                      " DELETE FROM " + localTableDebtRefs + " r " +
                      " WHERE EXISTS (SELECT 1 FROM deleted_rows del " +
                      "                WHERE r.nzp_wp=" + paramcalc.nzp_wp + " AND r.peni_calc_id=del.id" +
                      "                AND r.date_calc=" + s_CalcMonth + ")";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
                return ret;
            }
            finally
            {

            }
            return ret;
            //пишем в ту таблицу которая соответствует рассчитываемому месяцу
        }


        public DateTime GetDateStartPeni(IDbConnection conn_db, CalcTypes.ParamCalc finder, out Returns ret)
        {
            var res = new DateTime();
            ret = Utils.InitReturns();
            //текущий расчетный месяц
            var prm = new CalcMonthParams();
            prm.pref = finder.pref;
            var rec = Points.GetCalcMonth(prm);
            //текущий расчетный месяц этого банка
            var CalcMonth = new DateTime(rec.year_, rec.month_, 1);
            var sqlString = "select val_prm" +
                         " from " + finder.pref + sDataAliasRest + "prm_10 where is_actual<>100  " +
                         " and nzp_prm in (99) " +
                         " and dat_s <= " + sDefaultSchema + "mdy(" + CalcMonth.Month.ToString("00") + "," +
                         System.DateTime.DaysInMonth(CalcMonth.Year, CalcMonth.Month).ToString("00") + ","
                         + CalcMonth.Year.ToString("0000") + ")" +
                         " and dat_po>= " + sDefaultSchema + "mdy(" + CalcMonth.Month + ",01," + CalcMonth.Year + ") ";
            res = CastValue<DateTime>(ExecScalar(conn_db, sqlString, out ret, true));
            return res;
        }

        /// <summary>
        /// Получить периоды расчета задолженностей для всех ЛС
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="paramcalc"></param>
        /// <param name="dateFrom"></param>
        /// <param name="dateTo"></param>
        /// <returns></returns>
        public Returns CreateListPeriodDebt(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc, DateTime dateTo)
        {
            var ret = Utils.InitReturns();

            //расчетный месяц этого банка данных
            var prevMonth = new DateTime(paramcalc.cur_yy, paramcalc.cur_mm, 1).AddMonths(-1);
            var curMonth = new DateTime(paramcalc.cur_yy, paramcalc.cur_mm, 1);
            var s_curMonth = Utils.EStrNull(curMonth.ToShortDateString());
            var s_prevMonth = Utils.EStrNull(prevMonth.ToShortDateString());
            var tableDateCurDept = "t_date_cur_dept_" + DateTime.Now.Ticks; //темповая таблица с последней датой на которую рассчитана задолженность
            var tableReCalcIds = "t_recalc_ids_" + DateTime.Now.Ticks;
            var tableDatesOneColumn = "t_pd_dats_" + DateTime.Now.Ticks; //таблица с границами всех периодов в одну колонку
            var tableMaxMinDates = "temp_table_max_min_" + DateTime.Now.Ticks;
            var table90Days = "temp_period_for_3p_" + DateTime.Now.Ticks;
            var tableRefsIds = "t_ref_ids_" + DateTime.Now.Ticks;
            //получаем дату с которой начинаем считать пени, а точнее учитывать проводки по date_obliagtion - дате обязательств
            var dateStartPeni = GetDateStartPeni(conn_db, paramcalc, out ret);
            //конец рассчитываемого периода -последний день последнего месяца периода расчета 
            var s_dateTo = Utils.EStrNull(dateTo.ToShortDateString());

            if (!ret.result) return ret;

            try
            {
                //удаляем связи по задолженностям, которые считали в текущем месяце
                var sql = " DELETE FROM  " + TablePeniProvodkiRefs + " r " +
                          " WHERE EXISTS (SELECT 1 FROM " + TableListLs + " l, " + TableUKPeriods + " u" +
                          "               WHERE r.nzp_wp=" + paramcalc.nzp_wp +
                          "               AND r.nzp_kvar=l.nzp_kvar AND l.nzp_area=u.nzp_area" +
                          "               AND r.date_obligation<u.date_end AND r.date_obligation>=u.date_start" +
                          "               AND r.date_calc>=" + s_curMonth + ")";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                    return ret;


                sql = " CREATE TEMP TABLE " + TableNoCalculetedProv + " (LIKE " + TableProv + " INCLUDING DEFAULTS);";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                    return ret;

                //Добавляем колонку и индекс по ней, для выяснения причины отмены начисления пени
                sql = " ALTER TABLE " + TableNoCalculetedProv +
                      " ADD COLUMN nzp_area INTEGER DEFAULT 0";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                    return ret;

                //пишем неучтенные проводки в темповую таблицу, сюда попадают все проводки, которые еще не участвовали в расчете пени
                sql = " INSERT INTO " + TableNoCalculetedProv +
                      " SELECT p.*, l.nzp_area FROM " + TableProv + " p, " + TableListLs + " l , " + TableUKPeriods + " u  " +
                      " WHERE p.nzp_wp=" + paramcalc.nzp_wp +
                      " AND p.nzp_kvar=l.nzp_kvar" +
                      " AND l.nzp_area=u.nzp_area " +
                      " AND p.date_obligation<u.date_end " +
                      " AND p.date_obligation>=u.date_start" +
                      " AND NOT EXISTS (SELECT 1 FROM " + TablePeniSettings + " s WHERE s.nzp_peni_serv = p.nzp_serv)" + //отсеиваем "левые" записи
                      " AND NOT EXISTS (SELECT 1 FROM " + TablePeniProvodkiRefs + " r" +
                      "                 WHERE r.nzp_wp=p.nzp_wp AND r.peni_provodki_id=p.id" +
                      "                 AND r.date_obligation=p.date_obligation) " +
                      " AND s_prov_types_id>0;"; //p.s_prov_types_id>0 - только основные проводки
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return ret;


                ExecSQL(conn_db, " Create index ix_" + TableNoCalculetedProv + "_0 on " + TableNoCalculetedProv + " (nzp_kvar) ", true);
                ExecSQL(conn_db, sUpdStat + " " + TableNoCalculetedProv);

                //таблица для определения начала периода расчета пени по лс
                sql = " CREATE TEMP  TABLE " + TableStartCalc + " (nzp_kvar integer, date_from date)";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                    return ret;

                //пишем начальную границу периода расчета пени по неучтенным проводкам для лс
                sql = " INSERT INTO " + TableStartCalc + " (nzp_kvar,date_from)" +
                      " SELECT nzp_kvar,LEAST(MIN(date_obligation), " + dateTo.WithFirstDayMonth().ToShortDateStringWithQuote() + "::DATE) " +
                      " FROM " + TableNoCalculetedProv +
                      " GROUP BY nzp_kvar";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                    return ret;

                ExecSQL(conn_db, " Create index ix_" + TableStartCalc + "_1 on " + TableStartCalc + " (nzp_kvar,date_from) ", true);
                ExecSQL(conn_db, sUpdStat + " " + TableStartCalc);

                //допишем начальную границу для тех лс, у которых нет новых проводок
                sql = " INSERT INTO " + TableStartCalc + " (nzp_kvar,date_from)" +
                      " SELECT nzp_kvar, " + dateTo.WithFirstDayMonth().ToShortDateStringWithQuote() + "::DATE" +
                      " FROM " + TableListLs + " l " +
                      " WHERE NOT EXISTS (SELECT 1 FROM " + TableStartCalc + " ex WHERE ex.nzp_kvar=l.nzp_kvar)" +
                      " GROUP BY nzp_kvar";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                    return ret;


                //достаем id проводок которые должны участвовать в перерасчитываемом периоде
                sql = " SELECT d.id, d.nzp_kvar,d.date_from" +
                      " INTO TEMP TABLE " + tableReCalcIds +
                      " FROM " + TablePeniDebt + " d, " + TableStartCalc + " s" +
                      " WHERE d.nzp_wp=" + paramcalc.nzp_wp + " AND d.date_calc<" + s_curMonth +
                      " AND s.nzp_kvar=d.nzp_kvar AND s.date_from<d.date_to " +
                      " AND d.s_peni_type_debt_id=" + (int)s_peni_type_debt.IncDebt;
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                    return ret;

                ExecSQL(conn_db, " Create index ix_" + tableReCalcIds + "_1 on " + tableReCalcIds + " (id,nzp_kvar) ", true);
                ExecSQL(conn_db, " Create index ix_" + tableReCalcIds + "_2 on " + tableReCalcIds + " (nzp_kvar) ", true);
                ExecSQL(conn_db, sUpdStat + " " + tableReCalcIds);


                sql = " CREATE TEMP TABLE " + tableRefsIds + " AS " +
                      " SELECT DISTINCT peni_provodki_id, date_obligation,r.nzp_kvar  " +
                      " FROM " + TablePeniProvodkiRefs + " ref, " + tableReCalcIds + " r" +
                      " WHERE ref.peni_debt_id=r.id and ref.nzp_kvar=r.nzp_kvar ";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                    return ret;

                ExecSQL(conn_db,
                    " Create index ix_" + tableRefsIds + "_1 on " + tableRefsIds +
                    " (peni_provodki_id,date_obligation,nzp_kvar ) ", true);
                ExecSQL(conn_db, sUpdStat + " " + tableRefsIds);

                //пишем проводки которые участвуют в перерасчете
                sql = " INSERT INTO " + TableNoCalculetedProv + " " +
                      " SELECT p.* " +
                      " FROM " + TableProv + " p, "+tableRefsIds+"  ref" +
                      " WHERE p.nzp_wp=" + paramcalc.nzp_wp +
                      " AND p.date_obligation>=" + dateStartPeni.ToShortDateStringWithQuote() +
                      " AND p.id=ref.peni_provodki_id " +
                      " AND ref.date_obligation = p.date_obligation " +
                      " AND ref.nzp_kvar=p.nzp_kvar" +
                      " AND NOT EXISTS (SELECT 1 FROM " + TablePeniSettings + " s WHERE s.nzp_peni_serv = p.nzp_serv)" + //отсеиваем "левые" записи
                      " AND p.s_prov_types_id>0";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                    return ret;


                //получаем последнюю дату на которую известны задолженности по ЛС
                sql = " CREATE TEMP  TABLE " + tableDateCurDept + " AS " +
                      " SELECT d.nzp_kvar, MIN(d.date_from) as date_to" +
                      " FROM " + tableReCalcIds + " d " +
                      " GROUP BY d.nzp_kvar";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                    return ret;

                ExecSQL(conn_db, " Create index ix_" + tableDateCurDept + "_0 on " + tableDateCurDept + " (nzp_kvar) ", true);
                ExecSQL(conn_db, sUpdStat + " " + tableDateCurDept);

                //добавляем даты по текущим задолженностям для лс, у которых нет перерасчетов
                sql = " INSERT INTO " + tableDateCurDept + "(nzp_kvar,date_to)" +
                      " SELECT ls.nzp_kvar, " + dateTo.WithFirstDayMonth().ToShortDateStringWithQuote() + "::DATE" +
                      " FROM " + TableListLs + " ls " +
                      " WHERE NOT EXISTS (SELECT 1 FROM " + tableDateCurDept + " ex WHERE ex.nzp_kvar=ls.nzp_kvar)";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                    return ret;

                ExecSQL(conn_db, " Create index ix_" + tableDateCurDept + "_1 on " + tableDateCurDept + " (nzp_kvar,date_to) ", true);
                ExecSQL(conn_db, sUpdStat + " " + tableDateCurDept);


                //получаем Z(-1) - суммы задолженностей образованные из оплат и (начислений с date_obligation<01.01.2016 г).
                //получаем Z(0) - суммы задолженностей образованные из начислений с date_obligation>=01.01.2016 г и свыше 90 дней долга.
                sql = " INSERT INTO " + TableNoCalculetedProv +
                      " (nzp_kvar,num_ls, nzp_supp,nzp_serv,nzp_wp,s_prov_types_id,rsum_tarif,date_obligation,peni_actions_id)" +
                      " SELECT DISTINCT ON (d.nzp_kvar,d.nzp_supp,d.nzp_serv,d.nzp_wp, d.type_period)" +
                      " d.nzp_kvar,d.num_ls, d.nzp_supp,d.nzp_serv,d.nzp_wp," +
                      " CASE" +
                      "  WHEN d.type_period =-1 THEN " + (int)s_prov_types.OldCalculatedDebt + // Z(-1)
                      "  WHEN d.type_period = 0 THEN " + (int)s_prov_types.CalculatedDebt +    // Z(0)
                      " END AS s_prov_types_id," +
                      " d.sum_debt,c.date_to,0 " +
                      " FROM " + TablePeniDebt + " d, " + tableDateCurDept + " c, " + TableListLs + " l, " +
                      TableUKPeriods + " u" +
                      " WHERE d.nzp_wp=" + paramcalc.nzp_wp + " AND date_calc<=" + s_prevMonth + " AND " +
                      " d.nzp_kvar=c.nzp_kvar AND d.date_to=c.date_to" +
                      " AND l.nzp_area=u.nzp_area AND l.nzp_kvar=c.nzp_kvar" +
                      " AND d.type_period IN (-1,0)" +
                      " AND d.s_peni_type_debt_id= " + (int)s_peni_type_debt.IncDebt +
                      " AND c.date_to>u.date_start " +
                      " AND NOT EXISTS (SELECT 1 FROM " + TablePeniSettings + " s WHERE s.nzp_peni_serv = d.nzp_serv)" + //отсеиваем "левые" записи
                      " ORDER BY d.nzp_kvar,d.nzp_supp,d.nzp_serv,d.nzp_wp, d.type_period,date_calc DESC ";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                    return ret;


                ret = ExecSQL(conn_db,
                  "CREATE TEMP TABLE " + TablePeriodsNoCalcPeni +
                  "(nzp_kvar integer,date_from date, date_to date);", true);
                if (!ret.result) return ret;


                //записали периоды не учета услуг и договоров в расчете пени
                sql = " INSERT INTO " + TablePeriodsNoCalcPeni + " (nzp_kvar,date_from,date_to)  " +
                      " SELECT p.nzp_kvar, n.date_from,n.date_to " +
                      " FROM " + Points.Pref + sKernelAliasRest + "peni_no_calc n, " + TableNoCalculetedProv + " p " +
                      " WHERE n.nzp_serv=p.nzp_serv and n.nzp_supp=p.nzp_supp";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                    return ret;


                ExecSQL(conn_db, " Create index ix_" + TablePeriodsNoCalcPeni + "_1 on " + TablePeriodsNoCalcPeni + " (nzp_kvar,date_from,date_to) ", true);
                ExecSQL(conn_db, sUpdStat + "  " + TablePeriodsNoCalcPeni, true);

                //обрежем периоды в пределах рассчитываемого периода
                sql = " UPDATE " + TablePeriodsNoCalcPeni + " SET date_from=n.date_from" +
                      " FROM " + TableStartCalc + " n " +
                      " WHERE " + TablePeriodsNoCalcPeni + ".date_from<n.date_from" +
                      " AND " + TablePeriodsNoCalcPeni + ".nzp_kvar=n.nzp_kvar ";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                    return ret;

                sql = " UPDATE " + TablePeriodsNoCalcPeni + " SET date_to=" + s_dateTo + " " +
                      " WHERE date_to>" + s_dateTo + ";";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                    return ret;



                //пишем начисления, недопоставки, перерасчеты, вх.остаток при условии что date_obligation< date_from - 90 days
                sql = " CREATE TEMP TABLE " + table90Days + " AS " +
                      " SELECT l.nzp_kvar,u.nzp_area," +
                      " GREATEST(s.date_from - interval '90 days',u.date_start )::DATE as date_from,s.date_from  as date_to" +
                      " FROM " + TableStartCalc + " s,  " + TableUKPeriods + " u, " + TableListLs + " l" +
                      " WHERE u.nzp_area=l.nzp_area and l.nzp_kvar=s.nzp_kvar;";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                    return ret;
                ExecSQL(conn_db, " Create index ix_" + table90Days + "_1 on " + table90Days + " (nzp_kvar,nzp_area,date_from, date_to) ", true);
                ExecSQL(conn_db, sUpdStat + " " + table90Days);

                sql = " INSERT INTO " + TableNoCalculetedProv +
                      " (nzp_kvar,num_ls,nzp_supp,nzp_serv,nzp_area,nzp_wp,s_prov_types_id,rsum_tarif,sum_nedop, sum_reval, date_obligation,peni_actions_id)" +
                      " SELECT p.nzp_kvar,p.num_ls, p.nzp_supp, p.nzp_serv,d.nzp_area, p.nzp_wp, p.s_prov_types_id, p.rsum_tarif, p.sum_nedop, p.sum_reval," +
                      " p.date_obligation, -1 " +
                      " FROM " + TableProv + " p, " + table90Days + " d " +
                      " WHERE p.nzp_kvar=d.nzp_kvar" +
                      " AND p.date_obligation>=" + DayNewPeni +
                    //прибавлять начисления начинаем только для проводок с date_obligation>=01.01.2016
                      " AND p.date_obligation BETWEEN d.date_from and d.date_to" +
                      " AND p.s_prov_types_id NOT IN (" + PaymentsProvs + ") " +
                    //для услуг рассчитываемых по старому не пишем (кап.ремонт)
                      " AND NOT EXISTS (SELECT 1 FROM " + TablePeniSettings + " ss WHERE (ss.nzp_serv=p.nzp_serv AND is_old_calc=TRUE)" +
                      " OR (ss.nzp_peni_serv=p.nzp_serv))";//отсеиваем "левые" записи
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                    return ret;

                ExecSQL(conn_db, " Create index ix_" + TableNoCalculetedProv + "_1 on " + TableNoCalculetedProv + " (nzp_kvar,date_obligation) ", true);
                ExecSQL(conn_db, " Create index ix_" + TableNoCalculetedProv + "_2 on " + TableNoCalculetedProv + " (nzp_area,s_prov_types_id,date_obligation) ", true);
                ExecSQL(conn_db, " Create index ix_" + TableNoCalculetedProv + "_3 on " + TableNoCalculetedProv + " (nzp_kvar,nzp_serv,nzp_supp, date_obligation)" +
                                 " WHERE  s_prov_types_id NOT IN (" + (int)s_prov_types.CalculatedDebt + "," + (int)s_prov_types.OldCalculatedDebt + ") ", true);
                ExecSQL(conn_db, sUpdStat + " " + TableNoCalculetedProv);


                #region Запись в одну колонку границ всех периодов
                //записать все в один столбец
                sql = "CREATE TEMP  TABLE " + tableDatesOneColumn + "(nzp_kvar integer,date_val date,type integer);";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) return ret;


                //записываем начало периодов с типом =1
                sql = " INSERT INTO " + tableDatesOneColumn + "(nzp_kvar,date_val,type) " +
                      " SELECT nzp_kvar,date_from,1 " +
                      " FROM " + TablePeriodsNoCalcPeni +
                      " GROUP BY 1,2;";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                    return ret;

                //записываем конец периодов с типом =2
                sql = " INSERT INTO " + tableDatesOneColumn + "(nzp_kvar,date_val,type) " +
                      " SELECT nzp_kvar,date_to,2" +
                      " FROM " + TablePeriodsNoCalcPeni +
                      " GROUP BY 1,2;";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                    return ret;

                //пишем туда же даты обязательств из проводок с типом 3
                sql = " INSERT INTO " + tableDatesOneColumn + "(nzp_kvar,date_val,type) " +
                      " SELECT nzp_kvar," +
                      " CASE WHEN s_prov_types_id IN (" + ProvWithOutShift + ")" +
                      " THEN date_obligation ELSE date_obligation + interval '1 day' END,3 " +
                    //прибавляем 1 день по п.14 ст.155 ЖК РФ
                      " FROM " + TableNoCalculetedProv +
                      " WHERE peni_actions_id>= 0 " + //образуем периоды из проводок текущего расчетного периода
                      " GROUP BY 1,2;";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                    return ret;

                //пишем даты окончания периодов с учетом данных в табл. tableUKPeriods type=4
                sql = " INSERT INTO " + tableDatesOneColumn + "(nzp_kvar,date_val,type) " +
                      " SELECT l.nzp_kvar,u.date_end,4 " +
                      " FROM " + TableListLs + " l, " + TableUKPeriods + " u" +
                      " WHERE l.nzp_area=u.nzp_area " +
                      " GROUP BY 1,2;";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                    return ret;

                //пишем окончание периода перерасчета  type=5
                sql = " INSERT INTO " + tableDatesOneColumn + "(nzp_kvar,date_val,type) " +
                      " SELECT d.nzp_kvar,max(date_to),5 from " + TablePeniDebt + " d, " + tableReCalcIds + " i " +
                      " WHERE d.id=i.id and d.nzp_wp=" + paramcalc.nzp_wp +
                      " AND d.s_peni_type_debt_id=" + (int)s_peni_type_debt.IncDebt +
                      " GROUP BY 1;";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                    return ret;



                ExecSQL(conn_db, " Create index ix_" + tableDatesOneColumn + "_1 on " + tableDatesOneColumn + " (nzp_kvar,date_val) ", true);
                ExecSQL(conn_db, sUpdStat + " " + tableDatesOneColumn);

                #region 1456|Период отключения расчета пени|||bool||1||||

                //определим границы расчета пени
                sql = " CREATE TEMP  TABLE " + tableMaxMinDates + " AS " +
                      " SELECT nzp_kvar, MIN(date_val) min_date, MAX(date_val) max_date" +
                      " FROM " + tableDatesOneColumn +
                      " GROUP BY 1";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                    return ret;

                ExecSQL(conn_db, " Create index ix_" + tableMaxMinDates + "_1 on " + tableMaxMinDates + " (nzp_kvar,min_date,max_date ) ", true);
                ExecSQL(conn_db, sUpdStat + " " + tableMaxMinDates);

                //определяем периоды запрета расчета пени для лицевых счетов
                //по факту, где запрет начисления пени - там число задолженности = 0
                sql = " SELECT l.nzp_kvar, p.dat_s, p.dat_po INTO TEMP TABLE " + TempPeriodsProhibit +
                      " FROM " + paramcalc.pref + sDataAliasRest + "prm_1 p, " + TableListLs + " l,  " +
                      tableMaxMinDates + " mm " +
                      " WHERE l.nzp_kvar=p.nzp AND p.is_actual=1 AND p.nzp_prm=1456 AND p.val_prm" + sConvToInt + "=1" +
                      " AND l.nzp_kvar=mm.nzp_kvar AND p.dat_s<=mm.max_date AND mm.min_date<=p.dat_po ";//отсеим периоды не затрагивающие расчетный период
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                    return ret;

                ExecSQL(conn_db, " Create index ix_" + TempPeriodsProhibit + "_1 on " + TempPeriodsProhibit + " (nzp_kvar, dat_s, dat_po) ", true);
                ExecSQL(conn_db, sUpdStat + " " + TempPeriodsProhibit);

                //обрежем периоды в пределах действующего периода
                sql = " UPDATE " + TempPeriodsProhibit + " SET dat_s=min_date " +
                      " FROM  " + tableMaxMinDates + " mm" +
                      " WHERE mm.nzp_kvar=" + TempPeriodsProhibit + ".nzp_kvar" +
                      " AND " + TempPeriodsProhibit + ".dat_s<mm.min_date";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                    return ret;

                sql = " UPDATE " + TempPeriodsProhibit + " SET dat_po=max_date " +
                      " FROM  " + tableMaxMinDates + " mm" +
                      " WHERE mm.nzp_kvar=" + TempPeriodsProhibit + ".nzp_kvar" +
                      " AND " + TempPeriodsProhibit + ".dat_po>mm.max_date";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                    return ret;

                #endregion

                //добавляем веху для отмены начисления пени по дням  - дата начала
                sql = " INSERT INTO " + tableDatesOneColumn + "(nzp_kvar,date_val,type) " +
                      " SELECT nzp_kvar,dat_s,6" +
                      " FROM " + TempPeriodsProhibit + " p " +
                      " GROUP BY 1,2;";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                    return ret;

                //добавляем веху для отмены начисления пени по дням  - дата окончания
                sql = " INSERT INTO " + tableDatesOneColumn + "(nzp_kvar,date_val,type) " +
                      " SELECT nzp_kvar,dat_po,7" +
                      " FROM " + TempPeriodsProhibit + " p " +
                      " GROUP BY 1,2;";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                    return ret;


                //добавляем веху - 30й день для начислений
                sql = " INSERT INTO " + tableDatesOneColumn + "(nzp_kvar,date_val,type) " +
                      " SELECT nzp_kvar, date_obligation + interval '31 day',8 " +
                      " FROM " + TableNoCalculetedProv + " p, " + TableUKPeriods + " u " +
                      " WHERE u.nzp_area= p.nzp_area AND" +
                      " p.s_prov_types_id NOT IN (" + PaymentsProvs + "," + (int)s_prov_types.OldCalculatedDebt + "," +
                      (int)s_prov_types.CalculatedDebt + ")" +
                      " AND p.date_obligation+ interval '31 day'<u.date_end" +
                      " AND p.peni_actions_id>= 0" +
                      " GROUP BY 1,2;";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                    return ret;

                //добавляем веху - 90й день для начислений
                sql = " INSERT INTO " + tableDatesOneColumn + "(nzp_kvar,date_val,type) " +
                      " SELECT nzp_kvar, date_obligation + interval '91 day',9 " +
                      " FROM " + TableNoCalculetedProv + " p, " + TableUKPeriods + " u " +
                      " WHERE u.nzp_area= p.nzp_area AND" +
                      " p.s_prov_types_id NOT IN (" + PaymentsProvs + "," + (int)s_prov_types.OldCalculatedDebt + "," +
                      (int)s_prov_types.CalculatedDebt + ")" +
                      " AND p.date_obligation+ interval '91 day'<u.date_end" +
                      " AND p.peni_actions_id>= 0" +
                      " GROUP BY 1,2;";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                    return ret;



                //добавляем веху - 30й день для начислений
                sql = " INSERT INTO " + tableDatesOneColumn + "(nzp_kvar,date_val,type) " +
                      " SELECT p.nzp_kvar, p.date_obligation + interval '31 day',13 " +
                      " FROM " + TableNoCalculetedProv + " p, " + tableMaxMinDates + " u " +
                      " WHERE u.nzp_kvar= p.nzp_kvar AND" +
                      " p.s_prov_types_id NOT IN (" + PaymentsProvs + "," + (int)s_prov_types.OldCalculatedDebt + "," +
                      (int)s_prov_types.CalculatedDebt + ")" +
                      " AND (p.date_obligation+ interval '31 day')<u.max_date" +
                      " AND (p.date_obligation+ interval '31 day')>=u.min_date " +
                      " AND p.peni_actions_id< 0" +
                      " GROUP BY 1,2;";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                    return ret;

                //добавляем веху - 90й день для начислений
                sql = " INSERT INTO " + tableDatesOneColumn + "(nzp_kvar,date_val,type) " +
                      " SELECT p.nzp_kvar, p.date_obligation + interval '91 day',14 " +
                      " FROM " + TableNoCalculetedProv + " p, " + tableMaxMinDates + " u " +
                      " WHERE u.nzp_kvar= p.nzp_kvar AND" +
                      " p.s_prov_types_id NOT IN (" + PaymentsProvs + "," + (int)s_prov_types.OldCalculatedDebt + "," +
                      (int)s_prov_types.CalculatedDebt + ")" +
                      " AND (p.date_obligation+ interval '91 day')<u.max_date" +
                      " AND (p.date_obligation+ interval '91 day')>=u.min_date " +
                      " AND p.peni_actions_id<0" +
                      " GROUP BY 1,2;";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                    return ret;



                //добавляем веху  - смена процента по пени
                sql = " INSERT INTO " + tableDatesOneColumn + "(nzp_kvar,date_val,type) " +
                      " SELECT l.nzp_kvar,p.date_from,10" +
                      " FROM " + TableWithPeniProcents + " p,  " + tableMaxMinDates + " l" +
                      " WHERE p.date_from BETWEEN l.min_date AND l.max_date " +
                      " GROUP BY 1,2;";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                    return ret;

                ////добавляем веху  - смена процента по пени  - дата окончания
                sql = " INSERT INTO " + tableDatesOneColumn + "(nzp_kvar,date_val,type) " +
                      " SELECT l.nzp_kvar,p.date_to,11" +
                      " FROM " + TableWithPeniProcents + " p, " + tableMaxMinDates + " l " +
                      " WHERE p.date_to BETWEEN l.min_date AND l.max_date " +
                      " GROUP BY 1,2;";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                    return ret;

                //первое число месяца 
                sql = " INSERT INTO " + tableDatesOneColumn + "(nzp_kvar,date_val,type) " +
                      " SELECT m.nzp_kvar,date_trunc('month', m.date_val),12" +
                      " FROM  " + tableDatesOneColumn + " m" +
                      " GROUP BY 1,2;";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                    return ret;             
              

                ExecSQL(conn_db, sUpdStat + "  " + tableDatesOneColumn, true);

                #endregion Запись в одну колонку границ всех периодов

                //создаем таблицу с окончательными периодами по лс
                sql = "CREATE TEMP  TABLE " + TablePeriods +
                      " (nzp_kvar integer,date_from date, date_to date);";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                    return ret;

                //записываем все в виде периодов
                sql = " INSERT INTO " + TablePeriods + " (nzp_kvar,date_from,date_to) " +
                      " SELECT a.nzp_kvar, a.date_val," +
                      " MIN(b.date_val)" +
                      " FROM " + tableDatesOneColumn + " a, " + tableDatesOneColumn + " b" +
                      " WHERE a.nzp_kvar=b.nzp_kvar and a.date_val<b.date_val" +
                      " GROUP BY 1,2;";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                    return ret;

                ExecSQL(conn_db, " Create index ix_" + TablePeriods + "_1 on " + TablePeriods + " (nzp_kvar,date_from,date_to) ", true);
                ExecSQL(conn_db, sUpdStat + "  " + TablePeriods, true);


            }
            finally
            {
                ExecSQL(conn_db, "DROP TABLE " + tableLastDate, false);
                ExecSQL(conn_db, "DROP TABLE " + table90Days, false);
                ExecSQL(conn_db, "DROP TABLE " + tableReCalcIds, false);
                ExecSQL(conn_db, "DROP TABLE " + tableRefsIds, false);
                ExecSQL(conn_db, "DROP TABLE " + tableDateCurDept, false);
                ExecSQL(conn_db, "DROP TABLE " + tableMaxMinDates, false);
                ExecSQL(conn_db, "DROP TABLE " + tableDatesOneColumn, false);
            }

            return ret;
        }

        private Returns InitListLsForPeni(IDbConnection conn_db)
        {

            var sql = "CREATE TEMP  TABLE " + TableListLs + "(nzp_kvar integer,nzp_area integer); ";
            var ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
                return ret;

            //получаем список ЛС, у которых есть услуга "Пени" и для их УК определены параметры расчета пени, остальные отсекаем
            sql = " INSERT INTO " + TableListLs + " (nzp_kvar, nzp_area)" +
                  " SELECT tt.nzp_kvar,tu.nzp_area " +
                  " FROM temp_table_tarif tt, t_selkvar ts, " + TableUKPeriods + " tu " +
                  " WHERE tt.nzp_kvar=ts.nzp_kvar and ts.nzp_area=tu.nzp_area " +
                  " AND EXISTS (SELECT 1 FROM " + TablePeniSettings + " s WHERE s.nzp_peni_serv = tt.nzp_serv)" + //список услуг "ПЕНИ"
                  " GROUP BY 1,2 ";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
                return ret;

            //если услуги пени нет, то прекращаем выполнение 
            var countLsWithPeni = CastValue<int>(ExecScalar(conn_db, "SELECT count(*) FROM " + TableListLs, out ret, true));
            if (countLsWithPeni == 0)
            {
                ret.tag = -1;
                return ret;
            }

            ExecSQL(conn_db, " Create index ix_" + TableListLs + "_1 on " + TableListLs + " (nzp_kvar) ", true);
            ExecSQL(conn_db, " Create index ix_" + TableListLs + "_2 on " + TableListLs + " (nzp_kvar,nzp_area) ", true);
            ExecSQL(conn_db, sUpdStat + " " + TableListLs);
            return ret;
        }


        /// <summary>
        /// проверка на существование таблицы для пени, если ее нет, то она будет автоматически создана
        /// </summary>
        /// <returns></returns>
        public Returns CheckExistTablePeni(IDbConnection conn_db, int nzp_wp, DateTime date_calc, string pref, TablesForPeniCalc type, int part_id = 0)
        {
            var ret = Utils.InitReturns();
            string sql = "";
            var id = 0;
            var table = "peni_debt";
            switch (type)
            {
                case TablesForPeniCalc.PeniDebt:
                    {
                        sql = " INSERT INTO " + pref + sDataAliasRest + table + " (nzp_supp,nzp_serv,nzp_wp,date_calc,peni_actions_id,s_peni_type_debt_id) " +
                              " VALUES (0,0," + nzp_wp + "," + Utils.EStrNull(date_calc.ToShortDateString()) + ",0,1)";
                        ret = ExecSQL(conn_db, sql);
                        if (!ret.result) return ret;
                        id = DBManager.GetSerialValue(conn_db);
                        sql = "DELETE FROM " + pref + sDataAliasRest + table +
                        " WHERE id=" + id + "AND nzp_wp=" + nzp_wp + " AND date_calc=" +
                         Utils.EStrNull(date_calc.ToShortDateString());
                        ret = ExecSQL(conn_db, sql);
                        if (!ret.result) return ret;

                        sql = " INSERT INTO " + pref + sDataAliasRest + table + " (nzp_supp,nzp_serv,nzp_wp,date_calc,peni_actions_id,s_peni_type_debt_id) " +
                                " VALUES (0,0," + nzp_wp + "," + Utils.EStrNull(date_calc.ToShortDateString()) + ",0,2)";

                        ret = ExecSQL(conn_db, sql);
                        if (!ret.result) return ret;
                        id = DBManager.GetSerialValue(conn_db);
                        sql = "DELETE FROM " + pref + sDataAliasRest + table +
                            " WHERE id=" + id + "AND nzp_wp=" + nzp_wp + " AND date_calc=" +
                             Utils.EStrNull(date_calc.ToShortDateString());
                        ret = ExecSQL(conn_db, sql);
                        break;
                    }
                case TablesForPeniCalc.PeniCalc:
            {
                table = "peni_calc";
                        sql = " INSERT INTO " + pref + sDataAliasRest + table + " (nzp_supp,nzp_wp,date_calc,peni_actions_id) " +
                       " VALUES (0," + nzp_wp + "," + Utils.EStrNull(date_calc.ToShortDateString()) + ",0)";

                        ret = ExecSQL(conn_db, sql);
                        if (!ret.result) return ret;
                        id = DBManager.GetSerialValue(conn_db);
                        sql = "DELETE FROM " + pref + sDataAliasRest + table +
                            " WHERE id=" + id + "AND nzp_wp=" + nzp_wp + " AND date_calc=" +
                             Utils.EStrNull(date_calc.ToShortDateString());
                        ret = ExecSQL(conn_db, sql);
                        break;
                    }
                case TablesForPeniCalc.PeniOff:
                    {
                        table = "peni_off";
                        sql = " INSERT INTO " + pref + sDataAliasRest + table + " (nzp_wp,date_calc,peni_off_id,peni_debt_id) " +
                               " VALUES (" + nzp_wp + "," + date_calc.ToShortDateStringWithQuote() + ",0,0)";
                        ret = ExecSQL(conn_db, sql);
                        if (!ret.result) return ret;

                        sql = " DELETE FROM " + pref + sDataAliasRest + table +
                              " WHERE nzp_wp=" + nzp_wp + " AND date_calc=" + date_calc.ToShortDateStringWithQuote() +
                              " AND peni_off_id=0 AND peni_debt_id=0";
                        ret = ExecSQL(conn_db, sql);
                        break;
            }
                case TablesForPeniCalc.PeniDebtRefs:
            {
                        table = "peni_debt_refs";
                        sql = " INSERT INTO " + pref + sDataAliasRest + table + " (nzp_wp,date_calc,peni_calc_id,peni_debt_id) " +
                               " VALUES (" + nzp_wp + "," + date_calc.ToShortDateStringWithQuote() + ",0,0)";
                        ret = ExecSQL(conn_db, sql);
                        if (!ret.result) return ret;

                        sql = " DELETE FROM " + pref + sDataAliasRest + table +
                              " WHERE nzp_wp=" + nzp_wp + " AND date_calc=" + date_calc.ToShortDateStringWithQuote() +
                              " AND peni_calc_id=0 AND peni_debt_id=0";
                        ret = ExecSQL(conn_db, sql);
                        break;
            }
                case TablesForPeniCalc.PeniProvodkiRefs:
                    {
                        table = "peni_provodki_refs";

                        sql = " INSERT INTO " + pref + sDataAliasRest + table + " (nzp_wp, peni_debt_id, peni_provodki_id, date_obligation, date_calc, nzp_kvar) " +
                                  " VALUES (" + nzp_wp + ",0," + part_id * 5000000 + ", '01.01.1900'::DATE, " + date_calc.ToShortDateStringWithQuote() + ",0)";
            ret = ExecSQL(conn_db, sql);
            if (!ret.result) return ret;

                        sql = " DELETE FROM " + pref + sDataAliasRest + table +
                                " WHERE nzp_wp=" + nzp_wp + " AND date_calc=" + date_calc.ToShortDateStringWithQuote() +
                                " AND peni_provodki_id=" + part_id * 5000000 + " AND peni_debt_id=0";
            ret = ExecSQL(conn_db, sql);
                        if (!ret.result) return ret;
                        break;
                    }
            }

            return ret;
        }

        /// <summary>
        /// Запись в реестр действия по пени
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="action"></param>
        /// <param name="dateFrom"></param>
        /// <param name="dateFrom"></param>
        /// <param name="finder"></param>
        /// <returns></returns>
        public int CreateRecordInReestr(IDbConnection conn_db, peni_actions_type action, DateTime dateFrom, DateTime dateTo, CalcTypes.ParamCalc finder)
        {
            var s_dateFrom = Utils.EStrNull(dateFrom.ToShortDateString());
            var s_dateTo = Utils.EStrNull(dateTo.ToShortDateString());
            Returns ret = ExecSQL(conn_db, "INSERT INTO " + Points.Pref + sDataAliasRest + "peni_actions " +
                              " (peni_actions_type_id,created_by,date_from,date_to,nzp_wp)" +
                             " VALUES (" + (int)action + "," + finder.nzp_user + "," + s_dateFrom + "," + s_dateTo + "," + finder.nzp_wp + ")");
            return GetSerialValue(conn_db);
        }

        /// <summary>
        /// Вычисляем задолженности по периодам
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="paramcalc"></param>
        /// <param name="dateFrom"></param>
        /// <param name="dateTo"></param>
        /// <param name="reestrId"></param>
        /// <returns></returns>
        public Returns CalcDebts(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc, int reestrId)
        {
            var ret = Utils.InitReturns();

            //расчетный месяц этого банка данных
            var CalcMonth = new DateTime(paramcalc.cur_yy, paramcalc.cur_mm, 1);
            var s_CalcMonth = Utils.EStrNull(CalcMonth.ToShortDateString());
            var tableProvPeriods = "t_provs_periods_" + DateTime.Now.Ticks; //таблица со всеми проводками по периодам (нарастающим итогом)
            var tableMiddleDebts = "t_middle_debt_" + DateTime.Now.Ticks; //таблица с промежуточными задолженностями по периодам 
            var tablePeriodsDebt = "t_periods_debt_" + DateTime.Now.Ticks; //таблица с задолженностями по периодам

            try
            {
                // ниспадающие типы периодов:
                // 0 - свыше 90 дней задолженности
                // 1 - более 30 и меньше 90 дней задолженности
                // 2 - до 30 дней задолженности 
                //для каждой из проводок считаем кол-во дней задолженности и определяем к какому типу периода она принадлежит:
                //оплаты всегда идут с типом 0 - для того чтобы они распределялись сначала на старые долги
                //остальные типы проводок по вышеуказанным правилам
                var sql = " CREATE TEMP  TABLE " + tableProvPeriods + " AS " +
                          " SELECT p.nzp_kvar,p.nzp_serv,p.nzp_supp,t.date_from,t.date_to," +
                          " (p.rsum_tarif + p.sum_reval  - p.sum_nedop - p.sum_prih) AS sum_debt,  " +
                          " (t.date_from - p.date_obligation) as cnt_days, " +
                          " CASE" +
                          "  WHEN p.date_obligation< " + DayNewPeni + //проводки с датой обязательства до 01.01.2016 считаем по старому!!
                          "       OR p.s_prov_types_id IN (" + PaymentsProvs + "," + (int)s_prov_types.OldCalculatedDebt + ")" +
                           "       OR ss.is_old_calc" + //признак расчета услуги по старому
                          "       THEN -1  " +
                          "  ELSE " +
                          "     CASE" +
                          "      WHEN (t.date_from - p.date_obligation)>90 OR p.s_prov_types_id=" + (int)s_prov_types.CalculatedDebt + " THEN 0" +
                          "      WHEN (30<(t.date_from - p.date_obligation) AND (t.date_from - p.date_obligation )<=90) THEN 1 " +
                          "      ELSE 2  " +
                          "    END " +
                          " END as type_period " +
                          " FROM " + TablePeriods + " t, " + TableNoCalculetedProv + " p" +
                          " LEFT OUTER JOIN " + TablePeniSettings + " ss ON p.nzp_serv=ss.nzp_serv" +
                          " WHERE t.nzp_kvar=p.nzp_kvar " +
                          " AND (CASE WHEN s_prov_types_id IN (" + ProvWithOutShift + ")" +
                          "       THEN date_obligation " +
                          "       ELSE p.date_obligation+ interval '1 day' " +
                          "      END )<=t.date_from ";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
                return ret;

                ExecSQL(conn_db,
                  " Create index ix_" + tableProvPeriods + "_1 on " + tableProvPeriods +
                  " (nzp_kvar, nzp_serv, nzp_supp, date_from ,date_to, type_period) ", true);
                ExecSQL(conn_db, sUpdStat + " " + tableProvPeriods);

                //получаем сгруппированные строки по периодам c промежуточными сумма задолженностей:
                //они включают положительные суммы задолженностей из ниспадающих типов периодов
                sql = " CREATE TEMP TABLE " + tableMiddleDebts + " AS " +
                      " SELECT DISTINCT ON (d.nzp_kvar, d.nzp_serv, d.nzp_supp, d.date_from ,d.date_to, type_period) " +
                      " d.nzp_kvar, d.nzp_serv, d.nzp_supp, d.date_from ,d.date_to, type_period," +
                      " SUM(sum_debt) OVER w AS mid_sum " +
                      " FROM " + tableProvPeriods + " d " +
                      " WINDOW w AS (PARTITION BY d.nzp_kvar, d.nzp_serv, d.nzp_supp, d.date_from ,d.date_to ORDER BY d.type_period); ";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
                return ret;

                ExecSQL(conn_db,
                 " Create index ix_" + tableMiddleDebts + "_1 on " + tableMiddleDebts +
                 " (nzp_kvar, nzp_serv, nzp_supp, date_from ,date_to, type_period) ", true);
                ExecSQL(conn_db, sUpdStat + " " + tableMiddleDebts);

                //получаем конечные суммы задолженностей по типам периодов 
                sql = " CREATE TEMP TABLE  " + tablePeriodsDebt + " AS " +
                      " SELECT nzp_kvar, nzp_serv, nzp_supp, date_from,date_to,type_period," +
                      " (d.date_to - d.date_from) as cnt_days_debt, TRUE peni_calc," +
                    //корректируем промежуточную сумму путем вычитания положительных задолженностей из предыдущего типа периода
                      " " + sNvlWord + "(mid_sum - GREATEST(0,LAG(mid_sum,1) OVER w),0) AS sum_debt " +
                      " FROM " + tableMiddleDebts + " d" +
                      " WINDOW w AS (PARTITION by d.nzp_kvar, d.nzp_serv, d.nzp_supp, d.date_from ,d.date_to ORDER BY d.type_period)";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
                return ret;

                ExecSQL(conn_db,
                    " Create index ix_" + tablePeriodsDebt + "_1 on " + tablePeriodsDebt +
                    " (nzp_kvar,nzp_supp,nzp_serv,date_to,date_from) ", true);
                ExecSQL(conn_db, " Create index ix_" + tablePeriodsDebt + "_2 on " + tablePeriodsDebt + " (peni_calc) ", true);
                ExecSQL(conn_db, sUpdStat + " " + tablePeriodsDebt);

                //не учитывать записи при расчете пени  - ОСТАВИЛ ДЛЯ ОБРАТНОЙ ПОДДЕРЖКИ 
                sql = " UPDATE " + tablePeriodsDebt + " t SET peni_calc=FALSE " +
                      " FROM " + Points.Pref + sKernelAliasRest + "peni_no_calc  n" +
                      " WHERE n.is_actual<>100 AND n.nzp_serv=t.nzp_serv " +
                      " AND n.nzp_supp=t.nzp_supp " +
                      " AND t.date_from>=n.date_from and t.date_to<=n.date_to;";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
                return ret;


            //локальная таблица задолжностей
                var localTableDebtsUp = paramcalc.pref + "_charge_" + (CalcMonth.Year - 2000).ToString("00") +
                                        tableDelimiter +
                                        "peni_debt_" + CalcMonth.Year + CalcMonth.Month.ToString("00") + "_" +
                                        paramcalc.nzp_wp + "_up";
                //локальная таблица задолжностей с перерасчетными записями
                var localTableDebtsDown = paramcalc.pref + "_charge_" + (CalcMonth.Year - 2000).ToString("00") +
                                          tableDelimiter +
                                          "peni_debt_" + CalcMonth.Year + CalcMonth.Month.ToString("00") + "_" +
                                          paramcalc.nzp_wp + "_down";


                //темповые таблица для peni_debt_up
                sql = " CREATE TEMP  TABLE " + TableTempPeniDebtUp + " (like " + localTableDebtsUp + " INCLUDING DEFAULTS) WITH (FILLFACTOR=50);";
            ret = ExecSQL(conn_db, sql, false);
            if (!ret.result)
            {
                    if (
                        !CheckExistTablePeni(conn_db, paramcalc.nzp_wp, CalcMonth, paramcalc.pref,
                            TablesForPeniCalc.PeniDebt).result)
                {
                    return ret;
                }
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                    return ret;
            }

                //Добавляем колонку и индекс по ней, для выяснения причины отмены начисления пени
                sql = " ALTER TABLE " + TableTempPeniDebtUp +
                      " ADD COLUMN peni_off_id INTEGER DEFAULT 1";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
                return ret;


            //темповые таблица для peni_debt_down
            sql = " CREATE TEMP  TABLE " + TableTempPeniDebtDown + " (like " + localTableDebtsDown +
                  " INCLUDING DEFAULTS);";
                ret = ExecSQL(conn_db, sql, false);
            if (!ret.result)
                {
                    if (
                        !CheckExistTablePeni(conn_db, paramcalc.nzp_wp, CalcMonth, paramcalc.pref,
                            TablesForPeniCalc.PeniDebt).result)
                    {
                return ret;
                    }
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
                return ret;
                }

                //пишем в ту таблицу которая соответствует рассчитываемому месяцу
                sql =
                    " INSERT INTO " + TableTempPeniDebtUp +
                    " (nzp_kvar,num_ls,nzp_supp,nzp_serv,nzp_wp,s_peni_type_debt_id,date_from,date_to,sum_debt,date_calc, " +
                    " created_on,created_by,peni_actions_id,peni_calc, cnt_days ,cnt_days_with_prm, type_period)" +
                    " SELECT d.nzp_kvar,t.num_ls,d.nzp_supp,d.nzp_serv," + paramcalc.nzp_wp + " as nzp_wp," +
                    (int)s_peni_type_debt.IncDebt + " as type_debt,d.date_from,d.date_to,d.sum_debt," +
                    " " + s_CalcMonth + " as calc_month," + sCurDateTime + " as created_on, " + paramcalc.nzp_user +
                    " as created_by, " + reestrId +
                    " as peni_actions_id,d.peni_calc, d.cnt_days_debt, d.cnt_days_debt,d.type_period" +
                    " FROM " + tablePeriodsDebt + " d, t_opn t " +
                    " WHERE d.nzp_kvar=t.nzp_kvar";
                ret = ExecSQL(conn_db, sql, false);
                if (!ret.result)
                {
                    if (
                        !CheckExistTablePeni(conn_db, paramcalc.nzp_wp, CalcMonth, paramcalc.pref,
                            TablesForPeniCalc.PeniDebt).result)
                    {
                        return ret;
                    }
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
                return ret;

                }

                ExecSQL(conn_db, " Create index ix_off_" + TableTempPeniDebtUp + "_0 on " + TableTempPeniDebtUp + " (peni_off_id) ",true);
                ExecSQL(conn_db, " Create index ix_" + TableTempPeniDebtUp + "_1 on " + TableTempPeniDebtUp + "(peni_actions_id, nzp_kvar) WHERE peni_calc AND sum_debt>0 ", true);
                ExecSQL(conn_db, " Create index ix_" + TableTempPeniDebtUp + "_2 on " + TableTempPeniDebtUp + "(peni_actions_id, nzp_kvar) WHERE peni_calc", true);
                ExecSQL(conn_db, " Create index ix_" + TableTempPeniDebtUp + "_3 on " + TableTempPeniDebtUp + "(nzp_kvar,date_from,date_to) ", true);
                ExecSQL(conn_db, " Create index ix_" + TableTempPeniDebtUp + "_4 on " + TableTempPeniDebtUp + "(nzp_kvar,nzp_supp,date_from,date_to) ", true);
                ExecSQL(conn_db, sUpdStat + " " + TableTempPeniDebtUp);

                //ищем есть ли пересечение периодов задолженностей с задолженностями, пени по которым, уже в закрытом месяце
                //если есть, то пишем эти задолженности в таблицу localTableDebts с обратным знаком и date_calc=текущему месяцу и типом долга = 'снятие'
                sql = " INSERT INTO " + TableTempPeniDebtDown +
                      " (nzp_kvar,num_ls,nzp_supp,nzp_serv,nzp_wp,s_peni_type_debt_id,date_from,date_to,sum_debt," +
                      " over_payments,sum_debt_result,cnt_days,cnt_days_with_prm,sum_peni,date_calc, " +
                      " created_on,created_by,peni_actions_id,peni_calc, type_period) " +
                      " SELECT d.nzp_kvar,MAX(d.num_ls),d.nzp_supp,d.nzp_serv,d.nzp_wp," + (int)s_peni_type_debt.DelDebt + "," +
                      " d.date_from,d.date_to,SUM(d.sum_debt*(-1)),SUM(d.over_payments*(-1))," +
                      " SUM(d.sum_debt_result*(-1)),MAX(d.cnt_days),MAX(d.cnt_days_with_prm),SUM(d.sum_peni*(-1))," +
                      s_CalcMonth + ", " +
                      " " + sCurDateTime + "," + paramcalc.nzp_user + "," + reestrId + ",TRUE peni_calc,type_period" +
                      " FROM " + TablePeniDebt + " d, " + TableStartCalc + " s " +
                      " WHERE d.peni_calc=TRUE " +
                      " AND d.cnt_days_with_prm>0" +
                      " AND d.nzp_kvar=s.nzp_kvar " +
                      " AND s.date_from<d.date_to" +
                      " AND d.date_calc<" + s_CalcMonth +
                      " GROUP BY d.nzp_kvar,d.nzp_supp,d.nzp_serv,d.nzp_wp," + //группируем чтобы не "пухла" таблица
                      " d.date_from,d.date_to,type_period";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
                return ret;

            ExecSQL(conn_db, " Create index ix_" + TableTempPeniDebtDown + "_1 on " + TableTempPeniDebtDown + "(peni_actions_id, peni_calc) ", true);
            }
            finally
            {
                ExecSQL(conn_db, "DROP TABLE " + tableProvPeriods, false);
                ExecSQL(conn_db, "DROP TABLE " + tableMiddleDebts, false);
                ExecSQL(conn_db, "DROP TABLE " + tablePeriodsDebt, false);
            }

            return ret;
        }

        /// <summary>
        /// Вычисляем переплаты по ЛС  в периодах и распределяем их
        /// по задолжностям в этом же периоде 
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="paramcalc"></param>
        /// <param name="dateFrom"></param>
        /// <param name="dateTo"></param>
        /// <param name="reestrId"></param>
        /// <returns></returns>
        public Returns CalcOverPayments(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc, int reestrId)
        {

            var ret = Utils.InitReturns();

            var tableLastTypePeriods = "t_last_period_" + DateTime.Now.Ticks; //таблица с последними типами периодов по договорам
            var tableLastOverPayments = "t_last_pere_" + DateTime.Now.Ticks; //таблица с переплатами по периодам
            var tableSumDebtsBySupp = "t_sum_debts_by_supp_" + DateTime.Now.Ticks; //таблица с долгами по периодам,ЛС и договорам
            //получаем общие суммы переплат в разрезе ЛС и договора: остающиеся в пределах договора и те, которые идут на другие договоры
            var tableSumOverPaymentForeignAndOwn = "t_sum_pere_for_other_supp_" + DateTime.Now.Ticks;
            var tableForeignOverPaymentsProportion = "t_foreign_pere_prop_" + DateTime.Now.Ticks; //пропорции переплат на другие договоры
            var tableOwnOverPaymentsProportion = "t_own_pere_prop_" + DateTime.Now.Ticks; //пропорции переплат в пределах своего договоры
            var tableSumDebtsAndOverPayments = "t_sum_debts_and_overpayments_" + DateTime.Now.Ticks; //таблица со всеми переплатами и долгами, на которые эти переплаты нужно распределить
            var tableMiddleOverPayments = "t_overpayments_middle_" + DateTime.Now.Ticks; //промежуточные суммы переплат
            var tableFinalOverPaymentsSums = "t_overpayments_final_" + DateTime.Now.Ticks; //распределенные по всем критериям суммы переплат 
            var tableTotalSumOverPayments = "t_total_overpayments_" + DateTime.Now.Ticks; //таблица с суммарными переплатыми ЛС в периоде
            var tableDelta = "t_delta_over_peyments_" + DateTime.Now.Ticks; //таблица с дельтами по переплатам
            var tableFirstRows = "t_firts_rows_" + DateTime.Now.Ticks; //таблица со строками на которые докидываем дельты
            var tableTotalSumOwnAndForeign = "t_total_foreign_and_own_" + DateTime.Now.Ticks; //таблица с суммами переплат на свои и сторонние договоры в разрезе ЛС- период
            var tableSumDebtsByPeriod = "t_total_sum_debts_" + DateTime.Now.Ticks; //таблица с суммами долгов в разрезе ЛС- период
            try
            {

                //1.распределяем суммы переплат сначала внутри договоров.
                //1.1 для этого определяем сумму задолженности внутри договора по периодам,
                //те договора у которых суммарная задолженность <0 - будут гасить другие договора
                //1.2 определяем сумму переплаты, которую оставим внутри договора и сумму переплаты, которая пойдет на другие договора в этом периоде
                //1.3 определяем общую сумму задолженности в каждом из периодов
                //1.4 пропорционально ей определяем сумму распределенной переплаты на каждый из договоров
                //2. кладем все в таблицу с суммами переплат, переплаты оставшиеся внутри договором заменяют исходные суммы переплат 
                //(учитываем что часть переплат отдали под другие договора) с типом -1
                //2.1 распределяем полученные суммы переплат по ниспадающему правилу и определяем сумму переплаты в каждом из договоров, периодов и типах периода
                //3 распределенные суммы переплат суммируем и сравниваем с общей суммой переплат в каждом из периодов
                //3.1 полученную дельту кладем в первый из договоров в этом периоде и наименьшим типом периода


                //получаем суммы переплат в пределах договора, услуги, последний тип периода включает в себя все переплаты
                var sql = " CREATE TEMP TABLE " + tableLastTypePeriods + " AS " +
                          " SELECT DISTINCT ON (nzp_kvar,nzp_supp,nzp_serv,date_from,date_to) " +
                          " nzp_kvar,nzp_supp,nzp_serv,date_from,date_to,SUM(sum_debt) OVER W as sum_debt, type_period" +
                          " FROM " + TableTempPeniDebtUp + " up" +
                          " WHERE up.peni_actions_id=" + reestrId + " AND up.peni_calc=TRUE " +
                          " WINDOW W as (PARTITION BY nzp_kvar,nzp_supp,nzp_serv,date_from,date_to,type_period)" +
                          " ORDER BY nzp_kvar,nzp_supp,nzp_serv,date_from,date_to,type_period DESC ;";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                    return ret;

                ExecSQL(conn_db,
                    " Create index ix_" + tableLastTypePeriods + "_1 on " + tableLastTypePeriods +
                    " (nzp_kvar,nzp_supp,date_from,date_to) WHERE sum_debt<0 ", true);
                ExecSQL(conn_db, sUpdStat + "  " + tableLastTypePeriods, true);

                //получаем периоды и договора по которым есть переплаты
                sql = " CREATE TEMP TABLE " + tableLastOverPayments + " AS " +
                      " SELECT nzp_kvar,nzp_supp,date_from,date_to, SUM(sum_debt) as sum_debt" +
                      " FROM " + tableLastTypePeriods +
                      " WHERE sum_debt<0" +
                      " GROUP BY nzp_kvar,nzp_supp,date_from,date_to";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                    return ret;


                ExecSQL(conn_db,
                    " Create index ix_" + tableLastOverPayments + "_1 on " + tableLastOverPayments +
                    " (nzp_kvar,nzp_supp,date_from,date_to)", true);
                ExecSQL(conn_db, sUpdStat + "  " + tableLastOverPayments, true);


                //получаем суммы задолженностей в пределах договора и периода
                sql = " CREATE TEMP TABLE " + tableSumDebtsBySupp + " AS " +
                      " SELECT DISTINCT ON (nzp_kvar,nzp_supp,date_from,date_to) " +
                      " nzp_kvar,nzp_supp,date_from,date_to,SUM(sum_debt) OVER W as sum_debt" +
                      " FROM " + TableTempPeniDebtUp + " up" +
                      " WHERE up.peni_calc=TRUE AND peni_actions_id=" + reestrId + " AND sum_debt>0" +
                      " WINDOW W as (PARTITION BY nzp_kvar,nzp_supp,date_from,date_to);";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                    return ret;

                ExecSQL(conn_db,
                  " Create index ix_" + tableSumDebtsBySupp + "_1 on " + tableSumDebtsBySupp +
                  " (nzp_kvar,nzp_supp,date_from,date_to)", true);
                ExecSQL(conn_db, sUpdStat + "  " + tableSumDebtsBySupp, true);

                //определяем суммы переплат которые пойдут на другие договора и суммы которые останутся в пределах договора
                sql = " CREATE TEMP TABLE " + tableSumOverPaymentForeignAndOwn + " AS " +
                      " SELECT p.nzp_kvar,p.nzp_supp,p.date_from,p.date_to," +
                      " LEAST(" + sNvlWord + "(d.sum_debt,0)+p.sum_debt,0) sum_foreign," + //на другие договоры
                      " LEAST(" + sNvlWord + "(d.sum_debt,0),p.sum_debt*(-1))*(-1) as sum_own " + //на текущий договор
                      " FROM " + tableLastOverPayments + " p LEFT OUTER JOIN " + tableSumDebtsBySupp + " d" +
                      " USING (nzp_kvar,nzp_supp,date_from,date_to)";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                    return ret;


                ExecSQL(conn_db,
                  " Create index ix_" + tableSumOverPaymentForeignAndOwn + "_1 on " + tableSumOverPaymentForeignAndOwn +
                  " (nzp_kvar,nzp_supp,date_from,date_to)", true);
                ExecSQL(conn_db, sUpdStat + "  " + tableSumOverPaymentForeignAndOwn, true);

                //определяем пропорции переплат в пределах своего договора
                sql = " CREATE TEMP TABLE " + tableOwnOverPaymentsProportion + " AS " +
                      " SELECT d.nzp_kvar,d.nzp_supp,d.date_from,d.date_to," +
                      " o.sum_own/d.sum_debt as proportion" +
                      " FROM " + tableSumOverPaymentForeignAndOwn + " o JOIN " + tableSumDebtsBySupp + " d" +
                      " USING (nzp_kvar,nzp_supp,date_from,date_to)" +
                      " WHERE o.sum_own<0";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                    return ret;


                ExecSQL(conn_db,
                  " Create index ix_" + tableOwnOverPaymentsProportion + "_1 on " + tableOwnOverPaymentsProportion +
                  " (nzp_kvar,nzp_supp,date_from,date_to)", true);
                ExecSQL(conn_db, sUpdStat + "  " + tableOwnOverPaymentsProportion, true);

                //получаем суммы переплат которые будем распределять по типам периодов (сначала те которые будем распределять в пределах своих договоров)
                sql = " CREATE TEMP TABLE " + tableSumDebtsAndOverPayments + " AS " +
                      " SELECT  d.nzp_kvar,d.nzp_serv,d.nzp_supp,d.date_from,d.date_to, " +
                      " trunc(d.sum_debt * p.proportion,2) as sum_for_distrib,type_period as type_period_source, -1 as type_period" +
                      " FROM " + TableTempPeniDebtUp + " d JOIN " + tableOwnOverPaymentsProportion + " p " +
                      " USING (nzp_kvar,nzp_supp,date_from,date_to) " +
                      " WHERE d.sum_debt>0 AND d.peni_calc=TRUE  AND d.peni_actions_id=" + reestrId;
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                    return ret;

                if (true) //если будет добавлен параметр "не распределять переплаты на другие договоры", то учитывать его тут
                {
                    //получаем итоговую сумму переплат в разрезе ЛС, период
                    sql = " CREATE TEMP TABLE " + tableTotalSumOwnAndForeign + " AS " +
                          " SELECT o.nzp_kvar, o.date_from,o.date_to," +
                          " SUM(o.sum_foreign) as sum_foreign,SUM(o.sum_own) as sum_own" +
                          " FROM   " + tableSumOverPaymentForeignAndOwn + " o " +
                          " GROUP BY o.nzp_kvar, o.date_from,o.date_to";
                    ret = ExecSQL(conn_db, sql, true);
                    if (!ret.result)
                        return ret;

                    ExecSQL(conn_db,
                        " Create index ix_" + tableTotalSumOwnAndForeign + "_1 on " + tableTotalSumOwnAndForeign +
                        " (nzp_kvar, date_from, date_to)", true);
                    ExecSQL(conn_db, sUpdStat + "  " + tableTotalSumOwnAndForeign, true);

                    //получаем итоговую сумму задолженностей в каждом из периодов
                    sql = " CREATE TEMP TABLE " + tableSumDebtsByPeriod + " AS " +
                          " SELECT o.nzp_kvar, o.date_from,o.date_to,SUM(o.sum_debt) as sum_debt" +
                          " FROM " + tableSumDebtsBySupp + " o " +
                          " GROUP BY o.nzp_kvar, o.date_from,o.date_to";
                    ret = ExecSQL(conn_db, sql, true);
                    if (!ret.result)
                        return ret;


                    ExecSQL(conn_db,
                        " Create index ix_" + tableSumDebtsByPeriod + "_1 on " + tableSumDebtsByPeriod +
                        " (nzp_kvar, date_from, date_to)", true);
                    ExecSQL(conn_db, sUpdStat + "  " + tableSumDebtsByPeriod, true);

                    //получаем пропорции переплат для сторонних договоров
                    sql = " CREATE TEMP TABLE " + tableForeignOverPaymentsProportion + " AS " +
                          " SELECT o.nzp_kvar, o.date_from,o.date_to," +
                          " o.sum_foreign, d.sum_debt, o.sum_own," +
                          " o.sum_foreign/(d.sum_debt+o.sum_own) as foreign_proportion" +
                        //из суммы долга вычитаем сумму переплат на свои договоры - они уже погашены
                          " FROM " + tableTotalSumOwnAndForeign + " o JOIN " + tableSumDebtsByPeriod + " d " +
                          " USING (nzp_kvar, date_from, date_to)" +
                          " WHERE (d.sum_debt+o.sum_own)<>0";
                    ret = ExecSQL(conn_db, sql, true);
                    if (!ret.result)
                        return ret;


                    ExecSQL(conn_db,
                        " Create index ix_" + tableForeignOverPaymentsProportion + "_1 on " + tableForeignOverPaymentsProportion +
                        " (nzp_kvar, date_from, date_to)", true);
                    ExecSQL(conn_db, sUpdStat + "  " + tableForeignOverPaymentsProportion, true);


                    ExecSQL(conn_db,
                        " Create index ix_" + tableSumDebtsAndOverPayments + "_1 on " + tableSumDebtsAndOverPayments +
                        " (nzp_kvar,nzp_supp,nzp_serv, date_from, date_to,type_period_source)", true);
                    ExecSQL(conn_db,
                        " Create index ix_" + tableSumDebtsAndOverPayments + "_2 on " + tableSumDebtsAndOverPayments +
                        " (nzp_kvar,nzp_supp,date_from,date_to)", true);
                    ExecSQL(conn_db, sUpdStat + "  " + tableSumDebtsAndOverPayments, true);

                    //определяем суммы переплат для распределения (на сторонние договоры)
                    sql = " INSERT INTO " + tableSumDebtsAndOverPayments +
                          " (nzp_kvar,nzp_serv,nzp_supp,date_from,date_to,sum_for_distrib,type_period)" +
                          " SELECT up.nzp_kvar,up.nzp_serv,up.nzp_supp,up.date_from,up.date_to," +
                          " trunc((up.sum_debt +" + sNvlWord + "(r.sum_for_distrib,0)) * p.foreign_proportion,2),  -1 as type_period " +
                          " FROM " + TableTempPeniDebtUp + " up JOIN " + tableForeignOverPaymentsProportion + " p" +
                          " USING (nzp_kvar, date_from, date_to)" +
                          " LEFT OUTER JOIN " + tableSumDebtsAndOverPayments + " r" +
                          " ON up.nzp_kvar=r.nzp_kvar" +
                          " AND up.nzp_supp=r.nzp_supp" +
                          " AND up.nzp_serv=r.nzp_serv" +
                          " AND up.date_from=r.date_from AND up.date_to=r.date_to AND r.type_period_source=up.type_period" +
                          " WHERE up.sum_debt>0 AND up.peni_calc=TRUE AND up.peni_actions_id=" + reestrId;
                    ret = ExecSQL(conn_db, sql, true);
                    if (!ret.result)
                        return ret;
                }

                //пишем все суммы задолженностей на которые будем распределять переплаты
                sql = " INSERT INTO " + tableSumDebtsAndOverPayments +
                      "(nzp_kvar,nzp_serv,nzp_supp,date_from,date_to, type_period, sum_for_distrib)" +
                      " SELECT DISTINCT ON (up.nzp_kvar,up.nzp_serv,up.nzp_supp,up.date_from,up.date_to, up.type_period)" +
                      " up.nzp_kvar,up.nzp_serv,up.nzp_supp,up.date_from,up.date_to, up.type_period,up.sum_debt " +
                      " FROM " + TableTempPeniDebtUp + " up JOIN " + tableSumDebtsAndOverPayments + " o " +
                      " USING (nzp_kvar,nzp_supp,date_from,date_to) " +
                      " WHERE up.sum_debt>0 AND up.peni_calc=TRUE AND up.peni_actions_id=" + reestrId;
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                    return ret;

                //получаем промежуточные суммы распределенных переплат
                sql = " CREATE TEMP TABLE " + tableMiddleOverPayments + " AS " +
                      " SELECT DISTINCT ON (d.nzp_kvar, d.nzp_serv, d.nzp_supp, d.date_from ,d.date_to, type_period) " +
                      " d.nzp_kvar, d.nzp_serv, d.nzp_supp, d.date_from ,d.date_to, type_period," +
                      " SUM(d.sum_for_distrib) OVER w AS mid_sum " +
                      " FROM " + tableSumDebtsAndOverPayments + " d " +
                      " WINDOW w AS (PARTITION BY d.nzp_kvar, d.nzp_serv, d.nzp_supp, d.date_from ,d.date_to ORDER BY d.type_period)";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                    return ret;

                ExecSQL(conn_db,
                 " Create index ix_" + tableMiddleOverPayments + "_1 on " + tableMiddleOverPayments +
                 " (nzp_kvar, nzp_serv, nzp_supp, date_from ,date_to, type_period) ", true);
                ExecSQL(conn_db, sUpdStat + " " + tableMiddleOverPayments);

                //получаем суммы задолженностей по типам периодов с учетом распределенных переплат
                sql = " CREATE TEMP TABLE  " + tableFinalOverPaymentsSums + " AS " +
                      " SELECT nzp_kvar, nzp_serv, nzp_supp, date_from,date_to,type_period," +
                      " (d.date_to - d.date_from) as cnt_days_debt, " +
                      " CASE WHEN LEAD(mid_sum,1) OVER w IS NULL " + //если нет нижестоящих записей в этом окне , то пропускаем отрицательные значения
                      "      THEN " + sNvlWord + "(mid_sum - GREATEST(0,LAG(mid_sum,1) OVER w),0)  " +
                      "      ELSE GREATEST(mid_sum - GREATEST(0,LAG(mid_sum,1) OVER w),0)" + //иначе берем наибольшее значени между нулем и суммой долга с учетом переплат
                      " END AS sum_debt" + //сумма долга с учетом переплаты
                      " FROM " + tableMiddleOverPayments + " d" +
                      " WINDOW w AS (PARTITION by d.nzp_kvar, d.nzp_serv, d.nzp_supp, d.date_from ,d.date_to ORDER BY d.type_period)";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                    return ret;

                ExecSQL(conn_db,
                    " Create index ix_" + tableFinalOverPaymentsSums + "_1 on " + tableFinalOverPaymentsSums +
                    " (nzp_kvar,nzp_supp,nzp_serv,date_to,date_from,type_period) ", true);
                ExecSQL(conn_db, sUpdStat + " " + tableFinalOverPaymentsSums);


                //проставляем суммы переплат
                sql = " UPDATE " + TableTempPeniDebtUp + " up" +
                      " SET over_payments = up.sum_debt - n.sum_debt " + //дельта между долгом и долгом с учтенной переплатой = учтенной переплате
                      " FROM " + tableFinalOverPaymentsSums + "  n" +
                      " WHERE up.nzp_kvar=n.nzp_kvar AND up.nzp_supp=n.nzp_supp AND up.nzp_serv=n.nzp_serv " +
                      " AND up.date_from=n.date_from AND up.date_to=n.date_to " +
                      " AND up.type_period = n.type_period" +
                      " AND up.sum_debt>0 AND up.peni_calc=TRUE " +
                      " AND peni_actions_id=" + reestrId;
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                    return ret;

                sql = " CREATE TEMP TABLE " + tableTotalSumOverPayments + " AS " +
                      " SELECT o.nzp_kvar, o.date_from, o.date_to, SUM(sum_debt) as total_overpayments" +
                      " FROM " + tableLastOverPayments + " o " +
                      " GROUP BY o.nzp_kvar, o.date_from, o.date_to";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                    return ret;
                ExecSQL(conn_db,
                " Create index ix_" + tableTotalSumOverPayments + "_1 on " + tableTotalSumOverPayments +
                " (nzp_kvar,date_from,date_to) ", true);
                ExecSQL(conn_db, sUpdStat + " " + tableTotalSumOverPayments);

                //получаем дельту между общей суммой переплат и суммами распределенных переплат (копейка в результате округления)
                sql = " CREATE TEMP  TABLE " + tableDelta + " AS " +
                      " SELECT DISTINCT ON (t.nzp_kvar,t.date_from,t.date_to) t.nzp_kvar,t.date_from,t.date_to," +
                      " SUM(t.over_payments) OVER W::" + sDecimalType + "(14,2) + o.total_overpayments as delta" +
                      " FROM " + TableTempPeniDebtUp + " t JOIN " + tableTotalSumOverPayments + " o " +
                      " USING (nzp_kvar,date_from,date_to)" +
                      " WHERE t.over_payments<>0 AND t.peni_calc=TRUE AND peni_actions_id=" + reestrId +
                      " WINDOW w AS (PARTITION by t.nzp_kvar,t.date_from ,t.date_to);";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                    return ret;

                ExecSQL(conn_db,
                    " Create index ix_" + tableDelta + "_1 on " + tableDelta + " (nzp_kvar,date_from,date_to) ", true);
                ExecSQL(conn_db, sUpdStat + "  " + tableDelta, true);


                //находим запись куда положим полученную дельту - выбираем те записи у которых type_period меньше и долг не погашен
                sql = " CREATE TEMP TABLE " + tableFirstRows + " AS " +
                      " SELECT DISTINCT ON (nzp_kvar,date_from,date_to)" +
                      " t.id, t.nzp_kvar,t.date_from,t.date_to, t.type_period " +
                      " FROM " + TableTempPeniDebtUp + " t JOIN " + tableDelta + " d " +
                      " USING (nzp_kvar,date_from,date_to) " +
                      " WHERE (t.sum_debt-t.over_payments)>0  " +
                      " AND t.peni_calc=TRUE and peni_actions_id=" + reestrId + " " +
                      " ORDER BY t.nzp_kvar,t.date_from,t.date_to, t.type_period ASC";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                    return ret;

                ExecSQL(conn_db,
                    " Create index ix_" + tableFirstRows + "_1 on " + tableFirstRows +
                    " (id,nzp_kvar,date_from,date_to,type_period) ", true);
                ExecSQL(conn_db, sUpdStat + " " + tableFirstRows);

                //докидываем дельту
                sql =
                    " UPDATE " + TableTempPeniDebtUp + " up" +
                    " SET over_payments=over_payments - delta" + //дельта отрицательная
                    " FROM " + tableDelta + " d JOIN " + tableFirstRows + " f" +
                    " USING (nzp_kvar,date_from,date_to) " +
                    " WHERE up.id=f.id AND up.nzp_kvar=f.nzp_kvar " +
                    " AND up.peni_actions_id=" + reestrId +
                    " AND f.type_period = up.type_period ";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                    return ret;

            }
            finally
            {
                ExecSQL(conn_db, "DROP TABLE " + tableLastTypePeriods, false);
                ExecSQL(conn_db, "DROP TABLE " + tableLastOverPayments, false);
                ExecSQL(conn_db, "DROP TABLE " + tableSumDebtsBySupp, false);
                ExecSQL(conn_db, "DROP TABLE " + tableSumOverPaymentForeignAndOwn, false);
                ExecSQL(conn_db, "DROP TABLE " + tableForeignOverPaymentsProportion, false);
                ExecSQL(conn_db, "DROP TABLE " + tableOwnOverPaymentsProportion, false);
                ExecSQL(conn_db, "DROP TABLE " + tableSumDebtsAndOverPayments, false);
                ExecSQL(conn_db, "DROP TABLE " + tableMiddleOverPayments, false);
                ExecSQL(conn_db, "DROP TABLE " + tableFinalOverPaymentsSums, false);
                ExecSQL(conn_db, "DROP TABLE " + tableTotalSumOverPayments, false);
                ExecSQL(conn_db, "DROP TABLE " + tableDelta, false);
                ExecSQL(conn_db, "DROP TABLE " + tableFirstRows, false);
                ExecSQL(conn_db, "DROP TABLE " + tableTotalSumOwnAndForeign, false);
                ExecSQL(conn_db, "DROP TABLE " + tableSumDebtsByPeriod, false);
            }

            return ret;
        }

        /// <summary>
        /// Считаем пени и записываем пени
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="paramcalc"></param>
        /// <param name="dateFrom"></param>
        /// <param name="dateTo"></param>
        /// <param name="reestrId"></param>
        /// <returns></returns>
        private Returns CalcPeni(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc, int reestrId)
        {
            var ret = Utils.InitReturns();

            //проставляем долг с учетом распределенных переплат
            var sql = " UPDATE " + TableTempPeniDebtUp + " SET  sum_debt_result=(sum_debt-GREATEST(over_payments,0)) " +
                      " WHERE peni_actions_id=" + reestrId + "  AND sum_debt>0";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
                return ret;


            #region Отключение начисления пени для некоторых ЛС

            //проставляем число дней задолженности=0 c учетом параметра запрета начисления пени на задолженность
            sql = " UPDATE " + TableTempPeniDebtUp + " up SET cnt_days_with_prm=0," +
                  " peni_off_id= " + (int)SPeniOff.ParametrTurnOffPeniOnPeriod +
                  " WHERE EXISTS (SELECT 1 FROM " + TempPeriodsProhibit + " tp" +
                  " WHERE up.date_from<tp.dat_po AND up.date_to>tp.dat_s " +
                  " AND tp.nzp_kvar=up.nzp_kvar )" +
                  " AND peni_actions_id=" + reestrId;
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
                return ret;

            ExecSQL(conn_db, "DROP TABLE " + TempPeriodsProhibit, false);

            #endregion Отключение начисления пени для некоторых ЛС

            //////////////////////////////////////////////////////////
            //получаем пени для каждого из периодов по формуле: D * cnt_days_with_prm * S(0-1-2)
            //где: D=  долг с учетом распределенных переплат
            //cnt_days_with_prm - число дней задолженности c учетом параметра: 1456
            //S0 - 1/130 Ставки ЦБ РФ, S1 - 1/300, S2 - 0
            //////////////////////////////////////////////////////////

            //для явно определенных услуг
            sql = " UPDATE " + TableTempPeniDebtUp + " up " +
                  " SET sum_peni= up.sum_debt_result*up.cnt_days_with_prm* p.peni_percent/100" +
                  " FROM " + TableWithPeniProcents + " p,  " + TablePeniSettings + " s " +
                  " WHERE up.sum_debt>0 AND up.sum_debt_result>0 AND up.peni_actions_id=" + reestrId +
                  " AND up.nzp_serv= s.nzp_serv " +
                  " AND p.date_from<up.date_to AND up.date_from<=p.date_to " +
                  " AND p.nzp_prm=(CASE WHEN type_period =2 THEN s.low_percent ELSE " +
                  "               (CASE WHEN type_period IN (1,-1) THEN s.middle_percent " +
                  "                     ELSE s.high_percent END) END) ";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
                return ret;

            //для невыделенных услуг
            sql = " UPDATE " + TableTempPeniDebtUp + " up " +
                  " SET sum_peni= up.sum_debt_result*up.cnt_days_with_prm* p.peni_percent/100" +
                  " FROM " + TableWithPeniProcents + " p,  " + TablePeniSettings + " s " +
                  " WHERE up.sum_debt>0 AND up.sum_debt_result>0 AND up.peni_actions_id=" + reestrId +
                  " AND s.nzp_serv=1 " +
                  " AND p.date_from<up.date_to AND up.date_from<=p.date_to " +
                  " AND p.nzp_prm=(CASE WHEN type_period =2 THEN s.low_percent ELSE " +
                  "               (CASE WHEN type_period IN (1,-1) THEN s.middle_percent " +
                  "                     ELSE s.high_percent END) END) " +
                  " AND NOT EXISTS (SELECT 1 FROM " + TablePeniSettings + " ss WHERE ss.nzp_serv=up.nzp_serv)";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
                return ret;

            //начисленные 
            sql = " CREATE TEMP  TABLE " + TempUnionDebts + " AS " +
                  " SELECT * FROM " + TableTempPeniDebtUp +
                  " WHERE peni_calc=TRUE and peni_actions_id= " + reestrId;
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
                return ret;
            //перерасчеты
            sql = " INSERT INTO " + TempUnionDebts + "" +
                  " SELECT * FROM " + TableTempPeniDebtDown +
                  " WHERE peni_calc=TRUE and peni_actions_id= " + reestrId;
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
                return ret;

            ExecSQL(conn_db,
                " CREATE INDEX ix_" + TempUnionDebts + "_1 ON " + TempUnionDebts + " (nzp_kvar,num_ls, nzp_supp) ",
                true);
            ExecSQL(conn_db, sUpdStat + " " + TempUnionDebts);

            return ret;
        }

        /// <summary>
        /// Функция завершения расчета пени
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="paramcalc"></param>
        /// <param name="reestrId"></param>
        /// <returns></returns>
        public Returns CalcPeniFinaly(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc, int reestrId)
        {
            //расчетный месяц
            var CalcMonth = new DateTime(paramcalc.cur_yy, paramcalc.cur_mm, 1);
            var s_CalcMonth = Utils.EStrNull(CalcMonth.ToShortDateString());
            //локальная таблица задолжностей
            var localTableDebtsUp = paramcalc.pref + "_charge_" + (CalcMonth.Year - 2000).ToString("0") + tableDelimiter +
                                  "peni_debt_" + CalcMonth.Year + CalcMonth.Month.ToString("00") + "_" + paramcalc.nzp_wp + "_up";
            //локальная таблица задолжностей с перерасчетными записями
            var localTableDebtsDown = paramcalc.pref + "_charge_" + (CalcMonth.Year - 2000).ToString("00") + tableDelimiter +
                              "peni_debt_" + CalcMonth.Year + CalcMonth.Month.ToString("00") + "_" + paramcalc.nzp_wp + "_down";

            //локальная таблица пени
            var localTablePeni = paramcalc.pref + "_charge_" + (CalcMonth.Year - 2000).ToString("00") + tableDelimiter +
                                  "peni_calc_" + CalcMonth.Year + "_" + paramcalc.nzp_wp;

            //локальная таблица причин отмены  начисления пени
            var localTablePeniOff = paramcalc.pref + "_charge_" + (CalcMonth.Year - 2000).ToString("0") + tableDelimiter +
                                    "peni_off_" + CalcMonth.Year + CalcMonth.Month.ToString("00") + "_" +
                                    paramcalc.nzp_wp;

            //локальная таблица связей между пени и задолженностями
            var localTableDebtRefs = paramcalc.pref + "_charge_" + (CalcMonth.Year - 2000).ToString("0") + tableDelimiter +
                                    "peni_debt_refs_" + CalcMonth.Year + CalcMonth.Month.ToString("00") + "_" +
                                    paramcalc.nzp_wp;

            var tableTempDebtsId = "t_debts_id_" + DateTime.Now.Ticks; //темповая таблица с id задолженнотей
            Returns ret;
            try
            {
                #region Запись в peni_calc - для каждого типа peni_off по отдельности

                //пишем в ту таблицу которая соответствует рассчитываемому месяцу
                var sql = "  INSERT INTO " + localTableDebtsUp +
                          " (nzp_kvar, num_ls, nzp_supp, nzp_serv, nzp_wp, s_peni_type_debt_id, date_from, date_to, sum_debt, over_payments, " +
                          " sum_debt_result,cnt_days,cnt_days_with_prm,sum_peni,date_calc,created_on,created_by,peni_actions_id,peni_calc, type_period)" +
                          " SELECT nzp_kvar, num_ls, nzp_supp, nzp_serv, nzp_wp, s_peni_type_debt_id, date_from, date_to, sum_debt, over_payments, " +
                          " sum_debt_result,cnt_days,cnt_days_with_prm,sum_peni,date_calc,created_on,created_by,peni_actions_id,peni_calc,type_period" +
                          " FROM " + TableTempPeniDebtUp + " WHERE peni_off_id=" + (int)SPeniOff.No;

                ret = ExecSQL(conn_db, sql, false);
                if (!ret.result)
                {
                    if (
                        !CheckExistTablePeni(conn_db, paramcalc.nzp_wp, CalcMonth, paramcalc.pref,
                            TablesForPeniCalc.PeniDebt).result)
                    {
                        return ret;
                    }
                    ret = ExecSQL(conn_db, sql, true);
                    if (!ret.result)
                        return ret;
                }

                //пишем в ту таблицу которая соответствует рассчитываемому месяцу
                sql = " WITH inserted_rows AS ( INSERT INTO " + localTableDebtsUp +
                      " (nzp_kvar, num_ls, nzp_supp, nzp_serv, nzp_wp, s_peni_type_debt_id, date_from, date_to, sum_debt, over_payments, " +
                      " sum_debt_result,cnt_days,cnt_days_with_prm,sum_peni,date_calc,created_on,created_by,peni_actions_id,peni_calc,type_period)" +
                      " SELECT nzp_kvar, num_ls, nzp_supp, nzp_serv, nzp_wp, s_peni_type_debt_id, date_from, date_to, sum_debt, over_payments, " +
                      " sum_debt_result,cnt_days,cnt_days_with_prm,sum_peni,date_calc,created_on,created_by,peni_actions_id,peni_calc,type_period" +
                      " FROM " + TableTempPeniDebtUp + " WHERE peni_off_id=" +
                      (int)SPeniOff.ParametrTurnOffPeniOnPeriod +
                      " RETURNING id)" +
                      " INSERT INTO " + localTablePeniOff + "(nzp_wp, peni_off_id,peni_debt_id, date_calc) " +
                      " SELECT " + paramcalc.nzp_wp + ", " + (int)SPeniOff.ParametrTurnOffPeniOnPeriod + ",id, " +
                      CalcMonth.ToShortDateStringWithQuote() +
                      " FROM inserted_rows";
                ret = ExecSQL(conn_db, sql, false);
                if (!ret.result)
                {
                    if (
                        !CheckExistTablePeni(conn_db, paramcalc.nzp_wp, CalcMonth, paramcalc.pref,
                            TablesForPeniCalc.PeniOff).result)
                    {
                        return ret;
                    }
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
                return ret;
                }


                #endregion  Запись в peni_calc - для каждого типа peni_off по отдельности

                //ищем есть ли пересечение периодов задолженностей с задолженностями, пени по которым, уже в закрытом месяце
                //если есть, то пишем эти задолженности в таблицу localTableDebts с обратным знаком и date_calc=текущему месяцу и типом долга = 'снятие'
                sql = " INSERT INTO " + localTableDebtsDown +
                      " (nzp_kvar, num_ls, nzp_supp, nzp_serv, nzp_wp, s_peni_type_debt_id, date_from, date_to, sum_debt, over_payments, " +
                      " sum_debt_result,cnt_days,cnt_days_with_prm,sum_peni,date_calc,created_on,created_by,peni_actions_id,peni_calc)" +
                      " SELECT nzp_kvar, num_ls, nzp_supp, nzp_serv, nzp_wp, s_peni_type_debt_id, date_from, date_to, sum_debt, over_payments, " +
                      " sum_debt_result,cnt_days,cnt_days_with_prm,sum_peni,date_calc,created_on,created_by,peni_actions_id,peni_calc" +
                      " FROM " + TableTempPeniDebtDown;
                ret = ExecSQL(conn_db, sql, false);
                if (!ret.result)
                {
                    if (
                        !CheckExistTablePeni(conn_db, paramcalc.nzp_wp, CalcMonth, paramcalc.pref,
                            TablesForPeniCalc.PeniDebt).result)
                    {
                        return ret;
                    }
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
                return ret;

                }

                //проставляем указатели на записи задолжностей в проводки 
                sql = " CREATE TEMP  TABLE " + tableTempDebtsId + " AS " +
                     " SELECT id,nzp_kvar,nzp_supp,nzp_serv,date_from,date_to, date_calc" +
                     " FROM " + localTableDebtsUp + " up " +
                     " WHERE peni_actions_id=" + reestrId +
                     " AND EXISTS (SELECT 1 FROM " + TableListLs + " ls WHERE ls.nzp_kvar=up.nzp_kvar)";
                ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
                return ret;

                ExecSQL(conn_db,
                    " Create index ix_" + tableTempDebtsId + "_1 on " + tableTempDebtsId +
                    " (nzp_kvar,nzp_supp,nzp_serv,date_to,date_from) ", true);
                ExecSQL(conn_db, sUpdStat + "  " + tableTempDebtsId, true);

                var _5M = 5000000;
                //получаем список партиций для вставки 
                var DT =
                    ClassDBUtils.OpenSQL("SELECT DISTINCT id/" + _5M + "::int as id FROM " + TableNoCalculetedProv + " WHERE id IS NOT NULL", conn_db)
                        .resultData;
                foreach (DataRow row in DT.Rows)
                {
                    var localTablePeniProvRefs = paramcalc.pref + sDataAliasRest + "peni_provodki_refs_" + row["id"] +
                                                 "_" + paramcalc.nzp_wp;
                    //добавляем ссылки на рассчитанные задолженности для проводок 

                    sql = " INSERT INTO " + localTablePeniProvRefs + " (nzp_wp, peni_debt_id, peni_provodki_id, date_obligation, date_calc,nzp_kvar) " +
                          " SELECT p.nzp_wp, n.id, p.id, p.date_obligation, n.date_calc, p.nzp_kvar " +
                          " FROM  " + tableTempDebtsId + " n, " + TableNoCalculetedProv + " p " +
                          " WHERE n.nzp_kvar=p.nzp_kvar" +
                          " AND n.nzp_serv=p.nzp_serv " +
                          " AND n.nzp_supp=p.nzp_supp " +
                          " AND p.date_obligation+ interval '1 day'>=n.date_from and  p.date_obligation<n.date_to" +
                          " AND p.peni_actions_id>=0 " + //peni_actions_id=-1 помечены начисления за предыдущие 90 дней
                          " AND p.id>=" + (CastValue<int>(row["id"]) * _5M) + " AND p.id<" + (CastValue<int>(row["id"]) * _5M + _5M) +
                          " AND p.s_prov_types_id NOT IN (" + (int)s_prov_types.CalculatedDebt + "," + (int)s_prov_types.OldCalculatedDebt + ")  ";
                    ret = ExecSQL(conn_db, sql, false);
                    if (!ret.result)
                    {
                        if (!CheckExistTablePeni(conn_db, paramcalc.nzp_wp, CalcMonth, paramcalc.pref,
                            TablesForPeniCalc.PeniProvodkiRefs, (int)row["id"]).result)
                        {
                            return ret;
                        }
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
                return ret;

                    }

                }



                if (!(paramcalc.nzp_kvar > 0 || paramcalc.nzp_dom > 0))
                {
                    ExecSQL(conn_db, sUpdStat + "  " + localTableDebtsUp, true);
                    ExecSQL(conn_db, sUpdStat + "  " + localTableDebtsDown, true);
                    ExecSQL(conn_db, sUpdStat + "  " + TablePeniProvodkiRefs, true);
                }

            //записываем пени по лс в разрезе по договорам в таблицу + УЧЕТ ПЕРЕРАСЧЕТОВ!
            sql = " INSERT INTO " + localTablePeni + " " +
                      " (nzp_kvar,num_ls,nzp_serv,nzp_supp,nzp_wp,date_from,date_to,sum_peni,sum_old_reval,sum_new_reval," +
                  " date_calc,created_on,created_by,peni_actions_id)" +
                     " SELECT u.nzp_kvar,MAX(u.num_ls),s.nzp_serv, u.nzp_supp," + paramcalc.nzp_wp +
                      ",min(u.date_from), max(u.date_to)," +
                    //полный период расчета пени
                      " SUM(CASE WHEN u.s_peni_type_debt_id<>" + (int)s_peni_type_debt.DelDebt +
                      " THEN u.sum_peni ELSE 0 END)" +
                      " ,sum(CASE WHEN u.s_peni_type_debt_id=" + (int)s_peni_type_debt.DelDebt +
                      " THEN u.sum_peni ELSE 0 END)" +
                      " ,sum(CASE WHEN u.s_peni_type_debt_id<>" + (int)s_peni_type_debt.DelDebt +
                      " THEN u.sum_peni ELSE 0 END)" +
                  " ," + s_CalcMonth + "," + sCurDateTime +
                  " ," + paramcalc.nzp_user + "," + reestrId +
                      " FROM " + TempUnionDebts + " u,  " + TablePeniSettings + " s" +
                      " WHERE  u.nzp_serv=s.nzp_serv" + //прямая связь по услуге
                      " GROUP BY nzp_kvar, s.nzp_serv,nzp_supp";
            ret = ExecSQL(conn_db, sql, false);
            if (!ret.result)
            {
                    if (!CheckExistTablePeni(conn_db, paramcalc.nzp_wp, CalcMonth, paramcalc.pref,
                            TablesForPeniCalc.PeniCalc).result)
                {
                    return ret;
                }
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                    return ret;

            }

                //записываем пени по лс в разрезе по договорам в таблицу + УЧЕТ ПЕРЕРАСЧЕТОВ!
                sql = " INSERT INTO " + localTablePeni + " " +
                      " (nzp_kvar,num_ls,nzp_serv,nzp_supp,nzp_wp,date_from,date_to,sum_peni,sum_old_reval,sum_new_reval," +
                      " date_calc,created_on,created_by,peni_actions_id)" +
                      " SELECT u.nzp_kvar,MAX(u.num_ls),s.nzp_serv, u.nzp_supp," + paramcalc.nzp_wp +
                      ",min(u.date_from), max(u.date_to)," +
                    //полный период расчета пени
                      " SUM(CASE WHEN u.s_peni_type_debt_id<>" + (int)s_peni_type_debt.DelDebt +
                      " THEN u.sum_peni ELSE 0 END)" +
                      " ,sum(CASE WHEN u.s_peni_type_debt_id=" + (int)s_peni_type_debt.DelDebt +
                      " THEN u.sum_peni ELSE 0 END)" +
                      " ,sum(CASE WHEN u.s_peni_type_debt_id<>" + (int)s_peni_type_debt.DelDebt +
                      " THEN u.sum_peni ELSE 0 END)" +
                      " ," + s_CalcMonth + "," + sCurDateTime +
                      " ," + paramcalc.nzp_user + "," + reestrId +
                      " FROM " + TempUnionDebts + " u,  " + TablePeniSettings + " s" +
                      " WHERE  s.nzp_serv=1" + //услуги которые ложатся в nzp_serv =1 
                      " AND NOT EXISTS (SELECT 1 FROM " + TablePeniSettings + " ss WHERE ss.nzp_serv=u.nzp_serv)" +
                      " GROUP BY nzp_kvar, s.nzp_serv,nzp_supp";
                ret = ExecSQL(conn_db, sql, false);
                if (!ret.result)
                {
                    if (!CheckExistTablePeni(conn_db, paramcalc.nzp_wp, CalcMonth, paramcalc.pref,
                            TablesForPeniCalc.PeniCalc).result)
            {
                        return ret;
            }
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
                return ret;

                }

                if (!(paramcalc.nzp_kvar > 0 || paramcalc.nzp_dom > 0))
                {
                    ExecSQL(conn_db, sUpdStat + "  " + localTablePeni, true);
                }



            //проставляем указатели на записи задолженностей с пени в разрезе лс-договор
                sql = " INSERT INTO " + localTableDebtRefs + " (nzp_wp, peni_calc_id, peni_debt_id, date_calc) " +
                      " SELECT c.nzp_wp, c.id, l.id, c.date_calc " +
                      " FROM " + localTablePeni + " c, " + localTableDebtsUp + " l," + TablePeniSettings + " s" +
                      " WHERE c.nzp_wp = " + paramcalc.nzp_wp +
                      " AND c.nzp_wp=l.nzp_wp" +
                      " AND c.date_calc = " + s_CalcMonth +
                      " AND c.date_calc=l.date_calc " +
                      " AND l.nzp_kvar=c.nzp_kvar" +
                      " AND l.nzp_supp=c.nzp_supp" +
                      " AND l.nzp_serv=c.nzp_serv" +
                      " AND c.nzp_serv=s.nzp_serv " +
                      " AND c.peni_actions_id=l.peni_actions_id " +
                      " AND l.peni_actions_id=" + reestrId;
                ret = ExecSQL(conn_db, sql, false);
                if (!ret.result)
                {
                    if (!CheckExistTablePeni(conn_db, paramcalc.nzp_wp, CalcMonth, paramcalc.pref,
                            TablesForPeniCalc.PeniDebtRefs).result)
                    {
                        return ret;
                    }
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
                return ret;

                }

                //для невыделенных услуг
                sql = " INSERT INTO " + localTableDebtRefs + " (nzp_wp, peni_calc_id, peni_debt_id, date_calc) " +
                      " SELECT c.nzp_wp, c.id, l.id, c.date_calc " +
                      " FROM " + localTablePeni + " c, " + localTableDebtsUp + " l " +
                      " WHERE c.nzp_wp = " + paramcalc.nzp_wp +
                      " AND c.nzp_wp=l.nzp_wp" +
                      " AND c.date_calc = " + s_CalcMonth +
                      " AND c.date_calc=l.date_calc " +
                      " AND l.nzp_kvar=c.nzp_kvar" +
                      " AND l.nzp_supp=c.nzp_supp" +
                      " AND c.peni_actions_id=l.peni_actions_id " +
                      " AND l.peni_actions_id=" + reestrId +
                      " AND NOT EXISTS (SELECT 1 FROM " + TablePeniSettings + " ss" +
                      "                 WHERE ss.nzp_serv=c.nzp_serv)";
                ret = ExecSQL(conn_db, sql, false);
                if (!ret.result)
                {
                    if (!CheckExistTablePeni(conn_db, paramcalc.nzp_wp, CalcMonth, paramcalc.pref,
                            TablesForPeniCalc.PeniDebtRefs).result)
                    {
                        return ret;
                    }
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
                return ret;

                }
            }
            finally
            {
                ExecSQL(conn_db, "DROP TABLE " + tableTempDebtsId, false);
            }

            return ret;
        }


        /// <summary>
        /// Сохранение посчитанных пени в calc_gku_xx и reval_xx
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="paramcalc"></param>
        /// <param name="reestrId"></param>
        /// <returns></returns>
        public Returns SavePeniInCalcGku(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc, int reestrId)
        {

            var ret = Utils.InitReturns();
            var gku = new DbCalcCharge.Gku(paramcalc);
            //расчетный месяц
            var CalcMonth = new DateTime(paramcalc.cur_yy, paramcalc.cur_mm, 1);
            //локальная таблица пени
            var localTablePeni = paramcalc.pref + "_charge_" + (CalcMonth.Year - 2000).ToString("00") + tableDelimiter +
                                  "peni_calc_" + CalcMonth.Year + "_" + paramcalc.nzp_wp;

            var sql =
                " DELETE FROM " + gku.calc_gku_xx + " gku " +
                " WHERE EXISTS (SELECT 1 FROM " + TablePeniSettings + " s WHERE s.nzp_peni_serv=gku.nzp_serv) " +
                " AND EXISTS  (SELECT 1 FROM " + TableListLs + " t WHERE t.nzp_kvar = gku.nzp_kvar) ";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                return ret;
            }

            sql =
                " DELETE FROM  " + gku.curSaldoMon_charge + " ch " +
                " WHERE EXISTS (SELECT 1 FROM " + TablePeniSettings + " s WHERE s.nzp_peni_serv=ch.nzp_serv) " +
                " AND EXISTS  (SELECT 1 FROM " + TableListLs + " t WHERE t.nzp_kvar = ch.nzp_kvar) ";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                return ret;
            }

            //пишем расходы в calc_gku
            sql =
                " INSERT INTO " + gku.calc_gku_xx +
                " (nzp_dom, nzp_kvar, nzp_serv, nzp_supp, nzp_frm, nzp_prm_tarif, nzp_prm_rashod, rashod, tarif,dat_s,dat_po) " +
                " SELECT  op.nzp_dom,  t.nzp_kvar, s.nzp_peni_serv, t.nzp_supp, 50, max(t.id), 500, 1, " +
                " SUM(t.sum_peni), " + gku.dat_s + ", " + gku.dat_po +
                " FROM  " + localTablePeni + " t, temp_table_tarif f, t_opn op, " + TablePeniSettings + " s " +
                " WHERE op.nzp_kvar=t.nzp_kvar AND peni_actions_id=" + reestrId +
                " AND f.nzp_kvar = t.nzp_kvar AND f.nzp_serv=s.nzp_peni_serv AND f.nzp_supp=t.nzp_supp" +
                " AND t.nzp_serv = s.nzp_serv " +
                " GROUP BY 1,2,3,4";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                return ret;
            }

            //пишем перерасчеты  в reval_xx
            sql =
                " INSERT INTO " + gku.reval_xx +
                " (nzp_kvar, nzp_serv, nzp_supp, year_,month_,delta1,delta2,reval,tarif,tarif_p,sum_tarif," +
                " sum_tarif_p,sum_nedop,sum_nedop_p,c_calc,c_calcm_p,nzp_frm,nzp_frm_p,type_rsh,kod_info,month_p,year_p) " +
                " SELECT  t.nzp_kvar, s.nzp_peni_serv, t.nzp_supp," + CalcMonth.Year + " as year_," + CalcMonth.Month + " as month_, " +
                " sum(t.sum_old_reval) as delta1, 0 as delta2,sum(t.sum_old_reval) as reval, sum(t.sum_old_reval) as tarif,0 as tarif_p," +
                " sum(t.sum_old_reval) as sum_tarif, 0 as sum_tarif_p, 0 as sum_nedop, 0 as sum_nedop_p, 1 as c_calc, 0 as c_calcm_p,50 as nzp_frm," +
                " 50 as nzp_frm_p,0 as type_rsh,0 as kod_info, 1 as month_p, 1901 as year_p " +
                " FROM  " + localTablePeni + " t,  " + TablePeniSettings + " s , t_opn op, temp_table_tarif f " +
                " WHERE op.nzp_kvar=t.nzp_kvar AND peni_actions_id=" + reestrId +
                " AND f.nzp_kvar = t.nzp_kvar" +
                " AND t.nzp_serv = s.nzp_serv AND f.nzp_serv=s.nzp_peni_serv " +
                " AND f.nzp_supp=t.nzp_supp" +
                " AND ABS(t.sum_old_reval)>0 " +
                " GROUP BY 1,2,3";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                return ret;
            }


            return ret;
        }

        protected Returns GetUKParams(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc)
        {
            var ret = Utils.InitReturns();
            //текущий расчетный месяц
            var prm = new CalcMonthParams { pref = paramcalc.pref };
            var rec = Points.GetCalcMonth(prm);
            //текущий расчетный месяц
            var CurCalcDate = new DateTime(rec.year_, rec.month_, 1);
            var dateStartPeni = GetDateStartPeni(conn_db, paramcalc, out ret);
            if (!ret.result) return ret;

            var sql = " CREATE TEMP  TABLE " + TableUKPeriods +
                           " (nzp_area INTEGER, " +
                           " date_start DATE," +
                           " date_end DATE DEFAULT " + CurCalcDate.ToShortDateStringWithQuote() + ");";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                ret.text = "Ошибка получения параметров УК для расчета пени.";
                return ret;
            }

            sql = " INSERT INTO " + TableUKPeriods + "(nzp_area, date_start, date_end) " +
                  " SELECT DISTINCT nzp_area, " + dateStartPeni.ToShortDateStringWithQuote() + "::DATE, " +
                  (withProvCurMonth
                      ? CurCalcDate.AddMonths(1).ToShortDateStringWithQuote()
                      : CurCalcDate.ToShortDateStringWithQuote()) + "::DATE" +
                  " FROM t_selkvar ";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                ret.text = "Ошибка получения параметров УК для расчета пени.";
                return ret;
        }

            ExecSQL(conn_db, " CREATE INDEX ix_" + TableUKPeriods + "_1 ON " + TableUKPeriods + " (nzp_area) ", true);
            ExecSQL(conn_db, " CREATE INDEX ix_" + TableUKPeriods + "_2 ON " + TableUKPeriods + " (nzp_area,date_start,date_end) ", true);
            ExecSQL(conn_db, sUpdStat + " " + TableUKPeriods);

            return ret;
        }

        public bool GetParamIsNewPeni(IDbConnection conn_db, _Point point, int nzpPrm)
        {
            CalcTypes.ParamCalc prmCalc = new CalcTypes.ParamCalc();
            prmCalc.pref = point.pref;
            return GetParamIsNewPeni(conn_db, prmCalc, nzpPrm);
        }
        /// <summary>
        ///  Получить параметр действия нового режима получения пени для банка данных
        /// </summary>
        /// <returns></returns>
        public bool GetParamIsNewPeni(IDbConnection conn_db, CalcTypes.ParamCalc finder, int nzpPrm)
        {
            //текущий расчетный месяц
            var prm = new CalcMonthParams();
            prm.pref = finder.pref;
            var rec = Points.GetCalcMonth(prm);
            //текущий расчетный месяц этого банка
            var CalcMonth = new DateTime(rec.year_, rec.month_, 1);
            var tableName = finder.pref + sDataAliasRest + "prm_10";
            var ret = Utils.InitReturns();
            var sql = " Select max(val_prm) " +
                      " From " + tableName + " p " +
                      " Where p.nzp_prm =  " + nzpPrm +
                      "   AND p.is_actual = 1 " +
                      "   AND  " + CalcMonth.ToShortDateStringWithQuote() + " BETWEEN p.dat_s AND p.dat_po";
            var res = CastValue<int>(ExecScalar(conn_db, sql, out ret, true));
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка получения параметра типа алгоритма расчета пени GetParamIsNewPeni: " +
                     ret.text, MonitorLog.typelog.Error, 1, 2, true);
            }
            return res == 2;
        }



    }


    #endregion
}



