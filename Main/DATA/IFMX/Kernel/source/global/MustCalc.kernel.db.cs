using System;
using System.Data;
using System.Linq;
using System.Collections.Generic;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;


namespace STCLINE.KP50.DataBase
{

    /// <summary>
    /// Класс соответствующий таблице must_calc
    /// </summary>
    public class MustCalcTable
    {
        /// <summary>
        /// Код лицевого счета
        /// </summary>
        public int NzpKvar { set; get; }

        /// <summary>
        /// Код услуги
        /// </summary>
        public int NzpServ { set; get; }

        /// <summary>
        /// Код поставщика
        /// </summary>
        public int NzpSupp { set; get; }

        /// <summary>
        /// Месяц расчета
        /// </summary>
        public int Month { set; get; }

        /// <summary>
        /// Год расчета
        /// </summary>
        public int Year { set; get; }

        /// <summary>
        /// Начало периода перерасчета
        /// </summary>
        public DateTime DatS { set; get; }

        /// <summary>
        /// Окончание периода перерасчета
        /// </summary>
        public DateTime DatPo { set; get; }

        /// <summary>
        /// Резервное поле
        /// </summary>
        public int CntAdd { set; get; }

        /// <summary>
        /// Код параметра, являющегося источником перерасчета
        /// либо вспомогательный параметр
        /// </summary>
        public int Kod2 { set; get; }

        /// <summary>
        /// Причина перерасчета
        /// </summary>
        public MustCalcReasons Reason { set; get; }

        /// <summary>
        /// Код локального пользователя
        /// </summary>
        public int NzpUser { set; get; }

        /// <summary>
        /// Дата записи
        /// </summary>
        public DateTime DatWhen { set; get; }

        /// <summary>
        /// Расшифровка записи о перерасчете
        /// </summary>
        public string Comment { set; get; }

    }



    /// <summary>
    /// Класс отвечающий за сохранение признаков перерасчета
    /// </summary>
    public class DbMustCalcNew : DbIntervalBase
    {

        public DbMustCalcNew(IDbConnection connection)
        {
            Connection = connection;
            Trans = null;
        }

        public DbMustCalcNew(IDbConnection connection, IDbTransaction trans)
        {
            Connection = connection;

            Trans = trans;
        }

        /// <summary>
        /// Пустой конструктор по умолчанию вызывать пока нельзя
        /// </summary>
        protected DbMustCalcNew()
        {

        }

        /// <summary>
        /// Выставляет перерасчеты по группе
        /// </summary>
        /// <param name="editData"></param>
        /// <param name="ret"></param>
        public void MustCalc(EditInterDataMustCalc editData, out Returns ret)
        {

            var must = new MustCalcTable
            {
                NzpUser = editData.nzp_user,
                Kod2 = editData.kod2,
                Comment = editData.comment_action
            };
            //must.Reason = editData.kod1;
            var dbMustCalcPicking = new DbMustCalcPicking(Connection, editData, Trans);
            try
            {

                must.Reason = dbMustCalcPicking.Pick(out ret);
                must.Month = editData.month;
                must.Year = editData.year;

                if (ret.result)
                    ret = InsertListReason(editData.database, "t_mc_selected", must);
            }
            finally
            {
                ExecSQL(" Drop table t_mc_selected ", false);
            }
        }


        /// <summary>
        /// Добавление признака перерасчета по лицевому счету
        /// </summary>
        /// <param name="database">Локальная база данных</param>
        /// <param name="must">Объект MustCalcTable</param>
        /// <returns></returns>
        public Returns InsertReason(string database, MustCalcTable must)
        {

            //проверяем и ограничиваем период перерасчета
            Returns ret = CheckPeriodRecalc(database, ref must);
            if (!ret.result)
            {
                ret.text = ret.tag == Constants.access_code ? ret.text : "Ошибка проверки периода перерасчета";
                if (ret.tag == Constants.access_code)
                {
                    ret.result = true;
                }
                return ret;
            }


            #region проверка на наличие уже существующего периода перерасчета с такими же параметрами
            string sql = "select count(*) from " + database + DBManager.tableDelimiter + "must_calc " +
                         " where nzp_kvar = " + must.NzpKvar + " and nzp_serv = " + must.NzpServ +
                         " and nzp_supp = " + must.NzpSupp + " and month_ = " + must.Month + " and year_ = " +
                          must.Year + " and dat_s = '" + must.DatS.ToShortDateString() + "' and dat_po = '" +
                         must.DatPo.ToShortDateString() + "' and kod1 = " +
                         (int)must.Reason + " and kod2 = " + must.Kod2;

            object count = ExecScalar(sql, out ret, true);
            int recordsTotalCount = 0;
            if (count != DBNull.Value && count != null)
            {
                recordsTotalCount = Convert.ToInt32(count);
            }

            if (recordsTotalCount > 0)
            {
                return new Returns(true, "Такая запись уже существует", -1);
            }
            #endregion проверка на наличие уже существующего периода перерасчета с такими же параметрами


            var sel =
                string.Format(" Insert into {0}{1}must_calc (nzp_kvar, nzp_serv, nzp_supp, month_, year_, dat_s, dat_po, cnt_add, kod1, kod2, nzp_user, dat_when, comment ) " +
                " values( {2},{3}, {4}, {5},{6}, {7}, {8}, 113, {9}, {10}, {11},  {12}, '{13}') ",
                database, DBManager.tableDelimiter, must.NzpKvar, must.NzpServ, must.NzpSupp, must.Month,
                must.Year, Utils.EStrNull(must.DatS.ToShortDateString()), Utils.EStrNull(must.DatPo.ToShortDateString()),
                (int)must.Reason, must.Kod2, must.NzpUser, DBManager.sCurDate, must.Comment);

            if (!ExecSQL(sel, true))
            {
                Ret.text = "Ошибка вставки признака перерасчета ";
            }
            return Ret;
        }

        public Returns InsertReasonDomCounter(string database, MustCalcTable must, long nzp_dom)
        {

            //проверяем и ограничиваем период перерасчета
            Returns ret = CheckPeriodRecalc(database, ref must);
            if (!ret.result)
            {
                ret.text = ret.tag == Constants.access_code ? ret.text : "Ошибка проверки периода перерасчета";
                if (ret.tag == Constants.access_code)
                {
                    ret.result = true;
                }
                return ret;
            }
            #region проверка на наличие уже существующего периода перерасчета с такими же параметрами

            var where_kvar = " select distinct nzp_kvar from " + database + DBManager.tableDelimiter + "must_calc mc " +
                         " where nzp_serv = " + must.NzpServ +
                         " and nzp_supp = " + must.NzpSupp + " and month_ = " + must.Month + " and year_ = " +
                         must.Year + " and dat_s = '" + must.DatS.ToShortDateString() + "' and dat_po = '" +
                         must.DatPo.ToShortDateString() + "' and kod1 = " +
                         (int)must.Reason + " and kod2 = " + must.Kod2 +
                         " AND exists (SELECT nzp_kvar FROM " + database + DBManager.tableDelimiter +
                         "kvar k WHERE k.nzp_kvar=mc.nzp_kvar and k.nzp_dom=" + nzp_dom + ")";

            #endregion проверка на наличие уже существующего периода перерасчета с такими же параметрами

            string sel =
                " Insert into " + database + DBManager.tableDelimiter + "must_calc (nzp_kvar, nzp_serv, nzp_supp, " +
                "   month_, year_, dat_s, dat_po, cnt_add, kod1, kod2, nzp_user, dat_when, comment ) " +
                " Select k.nzp_kvar, " +
                must.NzpServ + ", " +
                must.NzpSupp + ", " +
                must.Month + "," +
                must.Year + ", '" +
                must.DatS.ToShortDateString() + "', '" +
                must.DatPo.ToShortDateString() + "', " +
                "113, " + //cnt_add
                (int)must.Reason + ", " +
                must.Kod2 + ", " +
                must.NzpUser + ",  " +
                DBManager.sCurDate + ", '" +
                must.Comment + "' " +
                " From " + database + DBManager.tableDelimiter + "kvar k where k.nzp_dom = " + nzp_dom +
                " and k.nzp_kvar not in (" + where_kvar + ")";

            if (!ExecSQL(sel, true))
            {
                Ret.text = "Ошибка вставки признака перерасчета ";
            }
            return Ret;
        }



        public Returns InsertReasonGroupCounter(string database, MustCalcTable must, int nzp_counter)
        {
            //проверяем и ограничиваем период перерасчета
            Returns ret = CheckPeriodRecalc(database, ref must);
            if (!ret.result)
            {
                ret.text = ret.tag == Constants.access_code ? ret.text : "Ошибка проверки периода перерасчета";
                if (ret.tag == Constants.access_code)
                {
                    ret.result = true;
                }
                return ret;
            }



            #region проверка на наличие уже существующего периода перерасчета с такими же параметрами

            var where_kvar = " select distinct nzp_kvar from " + database + DBManager.tableDelimiter + "must_calc mc " +
                         " where nzp_serv = " + must.NzpServ +
                         " and nzp_supp = " + must.NzpSupp + " and month_ = " + must.Month + " and year_ = " +
                         must.Year + " and dat_s = '" + must.DatS.ToShortDateString() + "' and dat_po = '" +
                         must.DatPo.ToShortDateString() + "' and kod1 = " +
                         (int)must.Reason + " and kod2 = " + must.Kod2 +
                         " AND exists (SELECT nzp_kvar FROM " + database + DBManager.tableDelimiter +
                         "counters_link k WHERE k.nzp_kvar=mc.nzp_kvar and k.nzp_counter=" + nzp_counter + ")";

            #endregion проверка на наличие уже существующего периода перерасчета с такими же параметрами


            string sel =
                " Insert into " + database + DBManager.tableDelimiter + "must_calc (nzp_kvar, nzp_serv, nzp_supp, " +
                "   month_, year_, dat_s, dat_po, cnt_add, kod1, kod2, nzp_user, dat_when, comment ) " +
                " Select cl.nzp_kvar, " +
                must.NzpServ + ", " +
                must.NzpSupp + ", " +
                must.Month + "," +
                must.Year + ", '" +
                must.DatS.ToShortDateString() + "', '" +
                must.DatPo.ToShortDateString() + "', " +
                "113, " + //cnt_add
                (int)must.Reason + ", " +
                must.Kod2 + ", " +
                must.NzpUser + ",  " +
                DBManager.sCurDate + ", '" +
                must.Comment + "' " +
                " From " + database + DBManager.tableDelimiter + "counters_link cl where cl.nzp_counter = " + nzp_counter +
                " and cl.nzp_kvar not in (" + where_kvar + ") ";

            if (!ExecSQL(sel, true))
            {
                Ret.text = "Ошибка вставки признака перерасчета ";
            }
            return Ret;
        }

        /// <summary>
        /// Добавление признаков перерасчетов по лицевому счету
        /// </summary>
        /// <param name="database">Локальная база данных</param>
        /// <param name="must">Объект MustCalcTable</param>
        /// <returns></returns>
        public Returns InsertReasons(string database, List<MustCalcTable> must)
        {
            Returns ret = Utils.InitReturns();
            foreach (MustCalcTable mus in must)
            {
                ret = InsertReason(database, mus);
                if (!ret.result) return ret;
            }
            return ret;
        }

        /// <summary>
        /// Проверка и ограничение периода перерасчета
        /// </summary>
        /// <param name="database"></param>
        /// <param name="must"></param>
        /// <returns></returns>
        private Returns CheckPeriodRecalc(string database, ref MustCalcTable must)
        {
            Returns ret = Utils.InitReturns();
            //максимальная дата из дат начала расчета системы и даты начала перерасчета
            var dateStartSystemOrRecalc = DBManager.CastValue<DateTime>(ExecScalar("SELECT max(val_prm) FROM " + database +
                                                                                   DBManager.tableDelimiter +
                                                                                   "prm_10  WHERE nzp_prm IN (82, 771) AND is_actual = 1",
                out ret, true));
            if (!ret.result)
            {
                MonitorLog.WriteLog(" Ошибка получения дат начала расчета системы и даты начала перерасчета:" + ret.text, MonitorLog.typelog.Error, 20, 201, true);
                return ret;
            }
            //перерасчет ведется помесячно, приводим все даты в соответствии с этим правилом
            dateStartSystemOrRecalc = new DateTime(dateStartSystemOrRecalc.Year, dateStartSystemOrRecalc.Month, 1);
            must.DatS = new DateTime(must.DatS.Year, must.DatS.Month, 1);
            must.DatPo = new DateTime(must.DatPo.Year, must.DatPo.Month, DateTime.DaysInMonth(must.DatPo.Year, must.DatPo.Month));


            //проверка на корректность данных
            if (must.DatS >= must.DatPo)
            {
                ret.tag = Constants.access_code;
                ret.result = false;
                ret.text = "Дата начала периода перерасчета не может быть больше даты окончания периода";
                return ret;
            }

            //если период перерасчета целиком находится до даты начала расчета  
            //1) |--------------------|
            //   ----------------------------------(dateStart)---------------------------------------------
            if (dateStartSystemOrRecalc >= must.DatPo && dateStartSystemOrRecalc >= must.DatS)
            {
                ret.tag = Constants.access_code;
                ret.result = false;
                ret.text =
                    "Не возможно выставить период перерасчета до \"Дата начала работы системы\" или \"Дата начала расчета/перерасчета \"";
                return ret;
            }

            //если дата начала периода перерасчета < даты начала расчета, дата конца периода > даты начала расчета
            //2)                              |--------------------|
            //   ----------------------------------(dateStart)---------------------------------------------
            if (dateStartSystemOrRecalc < must.DatPo && dateStartSystemOrRecalc > must.DatS)
            {
                ret.tag = Constants.access_code;
                ret.text =
                    "Дата начала периода перерасчета была ограничена параметром \"Дата начала работы системы\" или \"Дата начала расчета/перерасчета \"";
                //режем дату начала перерасчета
                must.DatS = dateStartSystemOrRecalc;
            }

            //если дата начала периода перерасчета < даты начала расчета, дата конца периода > даты начала расчета
            //3)                                                     |--------------------|
            //   ----------------------------------(dateStart)---------------------------------------------
            //                                                   так и отставляем
            return ret;
        }

        /// <summary>
        /// Групповое добавление признаков перерасчета 
        /// </summary>
        /// <param name="database">Локальная база данных</param>
        /// <param name="tableListReason">Временная таблица с причинами перерасчета</param>
        /// <param name="must">Объект MustCalcTable</param>
        /// <returns></returns>
        public Returns InsertListReason(string database, string tableListReason,
            MustCalcTable must)
        {
            string sel =
                " Insert into " + database + "must_calc (nzp_kvar, nzp_serv, nzp_supp, " +
                "   month_, year_, dat_s, dat_po, cnt_add, kod1, kod2, nzp_user, dat_when, comment ) " +
                " select  nzp_kvar, nzp_serv, 0, " +
                must.Month + "," +
                must.Year + ", work_s, work_po, 113, " +
                (int)must.Reason + ", " +
                must.Kod2 + ", " +
                must.NzpUser + ",  " +
                DBManager.sCurDate + ", '" +
                must.Comment + "' " +
                " from " + tableListReason + " where kod>0 ";

            if (!ExecSQL(sel, true))
            {
                Ret.text = "Ошибка вставки признаков перерасчетов по группе ";
            }
            return Ret;
        }

        /// <summary>
        /// Получение признака перерасчета по лицевому счету
        /// </summary>
        /// <param name="database">Локальная база данных</param>
        /// <param name="nzpKvar">КОд лицевого счета</param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public List<MustCalcTable> GetReason(string database, int nzpKvar, out Returns ret)
        {
            var result = new List<MustCalcTable>();

            string sel =
                " select nzp_kvar, nzp_serv, nzp_supp, " +
                " month_, year_, dat_s, dat_po, cnt_add, kod1, kod2, nzp_user, dat_when, comment " +
                " from " + database + DBManager.tableDelimiter + "must_calc " +
                " where nzp_kvar = " + nzpKvar;
            MyDataReader reader;
            if (!ExecRead(out reader, sel, true))
            {
                ret.text = "Ошибка вставки признака перерасчета ";
                ret = Ret;
                return result;
            }
            try
            {
                while (reader.Read())
                {
                    var mustCalcRecord = new MustCalcTable
                    {
                        NzpKvar = nzpKvar,
                        NzpServ = Convert.ToInt32(reader["nzp_serv"]),
                        NzpSupp = Convert.ToInt32(reader["nzp_supp"]),
                        Month = Convert.ToInt32(reader["month_"]),
                        Year = Convert.ToInt32(reader["year_"]),
                        DatS = Convert.ToDateTime(reader["dat_s"]),
                        DatPo = Convert.ToDateTime(reader["dat_po"]),
                        CntAdd = Convert.ToInt32(reader["cnt_add"]),
                        Reason = (MustCalcReasons)Convert.ToInt32(reader["kod1"]),
                        Kod2 = Convert.ToInt32(reader["kod2"]),
                        NzpUser = Convert.ToInt32(reader["nzp_user"]),
                        DatWhen = Convert.ToDateTime(reader["dat_when"]),
                        Comment = reader["comment"].ToString().Trim()
                    };
                    result.Add(mustCalcRecord);
                }
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = "Ошибка считывания must_calc по nzp_kvar=" + nzpKvar +
                           Environment.NewLine + ex.Message;
            }
            ret = Ret;
            return result;
        }


        /// <summary>
        /// Удалени признака перерасчета по лицевому счету
        /// </summary>
        /// <param name="database">Локальная база данных</param>
        /// <param name="nzpKvar">КОд лицевого счета</param>
        /// <param name="year">Год расчета</param>
        /// <param name="month">Месяц расчета</param>
        /// <returns></returns>
        public Returns DeleteReason(string database, int nzpKvar, int year, int month)
        {
            string sel =
                " delete " +
                " from " + database + DBManager.tableDelimiter + "must_calc " +
                " where nzp_kvar = " + nzpKvar +
                "   and year_=" + year +
                "   and month_=" + month;
            ExecSQL(sel, true);

            return Ret;
        }
    }



    /// <summary>
    /// Класс ответственный за сборку периодов и причин перерасчетов
    /// </summary>
    public class DbMustCalcPicking : DbIntervalBase
    {
        readonly EditInterDataMustCalc _editData;

        private string _startDate;
        private string _endDate;

        /// <summary>
        /// Дата начала периода
        /// </summary>
        public string StartDate
        {
            get
            {
                return "'" + _startDate + "'";
            }
            set
            {
                _startDate = value;
            }
        }

        /// <summary>
        /// Дата окончания периода
        /// </summary>
        public string EndDate
        {
            get
            {
                return "'" + _endDate + "'";
            }
            set
            {
                _endDate = value;
            }
        }



        public DbMustCalcPicking(IDbConnection connection,
            EditInterDataMustCalc editData)
        {
            Connection = connection;
            _editData = editData;
            Trans = null;
        }

        public DbMustCalcPicking(IDbConnection connection,
            EditInterDataMustCalc editData, IDbTransaction trans)
        {
            Connection = connection;
            _editData = editData;
            Trans = trans;
        }

        /// <summary>
        /// Пустой конструктор по умолчанию вызывать пока нельзя
        /// </summary>
        protected DbMustCalcPicking()
        {

        }

        /// <summary>
        /// Процедура сбора признака перерасчета 
        /// </summary>
        /// <param name="ret"></param>
        /// <returns></returns>
        public MustCalcReasons Pick(out Returns ret)
        {
            var kod1 = MustCalcReasons.Undefined;

            try
            {


                PreEdit();
                DateTime d1 = new DateTime(_editData.year, _editData.month, 1);
                DateTime d2 = new DateTime(_editData.year, _editData.month, 1).AddMonths(1).AddDays(-1);
                StartDate = new DateTime(d1.Year, d1.Month, 1).ToShortDateString();
                EndDate = new DateTime(d2.Year, d2.Month, 1).AddMonths(1).AddDays(-1).ToShortDateString();

                //построим индекс на must_calc по 
                // тормоза дл Челнов!
                //ExecSQL(conn_db, " update statistics for table must_calc ", true);

                //Убрал Андрей К. 14.01.2014
                //используется для выбора по полю month_calc, но при выставлении признаков перерасчета всегда используется текущий расчетный месяц
                //так что передача этих параметров извне не нужна
                //string s_s = " cast (" + editData.dat_s + " as date) ";
                //string s_po = " cast (" + editData.dat_po + " as date) ";



                ExecSQL(" Drop table t_mc_selected ", false);




                //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                //выбрать измененые данные в текущем месяце
                //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                //nedop_kvar, counters_xx, tarif
                //prm_xx
                //pere_gilec

                kod1 = GetMustCalcReasons();

                // Используем для postgres новый алгоритм, так как старый работает неправильно. 
                // Для Informix оставим старый, так как для него необходима доработка запроса с учётом его возможностей
#if PG
                PickPG();
#else
                PickInformix();
#endif

            }
            catch (Exception ex)
            {
                Ret.text = ex.Message + " класс DbMustCalcPicking";
                MonitorLog.WriteLog(" Ошибка сохранения интервальных данных " + Ret.text,
                    MonitorLog.typelog.Error, 20, 201, true);
                return kod1;
            }
            finally
            {

                ExecSQL(" Drop table t_must ", false);
                ret = Ret;
            }

            return kod1;
        }

        /// <summary>
        /// Поиск интервалов перерасчёта для Postgres (для informix новый алгоритм не работает, нужна эмуляция некоторых функций)
        /// </summary>
        private void PickPG()
        {
            InitSelectedMc();
            LoadExistsMustCalcRecords();
            // Объединение интервалов не требуется, оно произойдёт автоматически
            SubstractExistsIntervals();
        }

        private void PickInformix()
        {
            InitSelectedMc();

            //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            //объединим пересекающиеся интервалы в t_selected
            //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            UnionTempTable("t_mc_selected");

            LoadExistsMustCalcRecords();

            //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            //объединим пересекающиеся интервалы в t_must
            //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            UnionTempTable("t_must");

            //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            //начинаем выцеплять непересекающиеся интервалы
            //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

            //сначала определим интервалы, которые непересекаются вообще 
            SetUnCrossIntervals();

            SetDublicateIntervals();

            FindIntervalEdges();

            WorkWithCrossIntervals();
        }

        /// <summary>
        /// Вычитание уже существующих в t_must интервалов из необработанных интервалов t_mc_selected, а так же их объединение
        /// Предполагается, что интервалы уже выровнены до полного месяца в обоих таблицах
        /// </summary>
        internal static string GetIntervalsSubstractQuery(string t_selected, string t_must)
        {
            // Идея проста - разбиваем интервалы на месяцы, выкидываем те месяцы, которые уже есть в t_must, собираем назад в интервалы.
            // Алгоритм можно переделать под дни небольшими изменениями, но потеряется скорость
            return string.Format(@"
WITH selected_period_items AS ( -- разбираем периоды, которые нужно добавить, по отдельным частям (месяц)
	SELECT DISTINCT nzp_kvar, nzp_serv, generate_series(dat_s, dat_po, '1 month'::interval)::date period_item
	FROM {0}
    WHERE kod = 0
), new_period_items AS ( -- отфильтровываем части, которые уже есть в must_calc и нумеруем оставшиеся по порядку
	SELECT sd.nzp_kvar, sd.nzp_serv, sd.period_item, row_number() OVER (ORDER BY sd.nzp_kvar, sd.nzp_serv, sd.period_item) day_number
	FROM selected_period_items sd
	WHERE NOT EXISTS (SELECT 1 FROM {1} m WHERE m.nzp_kvar = sd.nzp_kvar AND m.nzp_serv = sd.nzp_serv AND m.dat_s <= period_item AND m.dat_po >= period_item)
), holes AS ( -- находим разрывы в последовательности кусочков
	SELECT d1.nzp_kvar, d1.nzp_serv, d1.period_item as prev_end_date, d2.period_item as next_start_date
	FROM new_period_items d1
			 INNER JOIN new_period_items d2 ON d1.nzp_kvar = d2.nzp_kvar AND d1.nzp_serv = d2.nzp_serv AND d1.day_number = (d2.day_number - 1)
	WHERE d1.period_item != (d2.period_item - INTERVAL '1 month')

	UNION

	SELECT nzp_kvar, nzp_serv, NULL prev_end_date, MIN(period_item) as next_start_date
	FROM new_period_items
	GROUP BY nzp_kvar, nzp_serv

	UNION

	SELECT nzp_kvar, nzp_serv, MAX(period_item) as prev_end_date, NULL as next_start_date
	FROM new_period_items
	GROUP BY nzp_kvar, nzp_serv
), sorted_holes AS ( -- последовательно нумеруем найденные разрывы
	SELECT nzp_kvar, nzp_serv, next_start_date, prev_end_date, row_number() OVER (ORDER BY nzp_kvar, nzp_serv, next_start_date) hole_number
	FROM holes
)
-- выбираем получившиеся периоды и вставляем в {0}
INSERT INTO {0} (nzp_kvar, nzp_serv, dat_s, dat_po, work_s, work_po, kod)
SELECT h1.nzp_kvar, h1.nzp_serv, h1.next_start_date, (h2.prev_end_date + INTERVAL '1 month' - INTERVAL '1 day')::date,
       h1.next_start_date, (h2.prev_end_date + INTERVAL '1 month' - INTERVAL '1 day')::date, 1
FROM sorted_holes h1
     INNER JOIN sorted_holes h2 ON h1.nzp_kvar = h2.nzp_kvar AND h1.nzp_serv = h2.nzp_serv AND h1.hole_number = (h2.hole_number - 1)", t_selected, t_must);
        }


        private void SubstractExistsIntervals()
        {

            var query = GetIntervalsSubstractQuery("t_mc_selected", "t_must");
            if (!ExecSQL(query, true))
            {
                throw new Exception("Ошибка вычитания существующих интервалов");
            }

            // Убиваем исходные интервалы
            query = @"
UPDATE t_mc_selected
SET kod = -1
WHERE kod = 0;
";
            if (!ExecSQL(query, true))
            {
                throw new Exception("Ошибка очистки ненужных интервалов");
            }
        }

        /// <summary>
        /// работа по пересекающимся интервалам
        /// </summary>
        /// <returns></returns>
        private void WorkWithCrossIntervals()
        {

            //затем работаем по пересекающимся интервалам
            int kod = 1;

            //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            while (kod < 1000) //делаем это в цикле, пока не перебрем все интервалы в t_selected
            //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            {
                kod += 1;

                //ищем ближайший правый край после work_s
                //при этом не должны покрываться другими интервалами из must_calc
                //      [-------]       :table
                //         [--]         :must_calc
                string sel = " Update t_mc_selected " +
                             " Set work_po= ( Select min(m.dat_s - 1) From  t_must  m " +
                             " Where t_mc_selected.nzp_kvar = m.nzp_kvar " +
                             " and t_mc_selected.nzp_serv = m.nzp_serv " +
                             " and t_mc_selected.work_s <= m.dat_s - 1 " +
                             " and m.kod <> -1 " +
                             " ) " +
                             " Where kod = 0 " +
                             "  and ( Select count(*) From  t_must m " +
                             " Where t_mc_selected.nzp_kvar = m.nzp_kvar " +
                             " and t_mc_selected.nzp_serv = m.nzp_serv " +
                             " and t_mc_selected.work_s <= m.dat_s - 1 " +
                             " and m.kod <> -1 " +
                             " ) > 0 ";

                if (!ExecSQL(sel, true))
                {
                    throw new Exception("Ошибка выборки правого края интервала");
                }

                //выкинем все ошибочные интервалы
                sel = " Update t_mc_selected" +
                      " Set kod = -1 " +
                      " Where work_s > work_po ";

                if (!ExecSQL(sel, true))
                {
                    throw new Exception("Ошибка удаления ошибочных интервалов");
                }

                //проверим, что интервалы непересекаются
                sel = " Update t_mc_selected" +
                      " Set kod = " + kod +
                      " Where kod = 0 " +
                      " and not Exists ( " +
                      " Select 1 From  t_must  m " +
                      " Where t_mc_selected.nzp_kvar = m.nzp_kvar " +
                      " and t_mc_selected.nzp_serv = m.nzp_serv " +
                      " and t_mc_selected.work_s  <= m.dat_po " +
                      " and t_mc_selected.work_po >= m.dat_s  " +
                      " and m.kod <> -1 " +
                      " ) ";
                if (!ExecSQL(sel, true))
                {
                    throw new Exception("Ошибка проверка пересекающихся интервалов");
                }

                //передвинем work_s
                sel = " Update t_mc_selected " +
                      " Set kod = 0 " +
                      ", work_po = dat_po " +
                      ", work_s = ( Select min(m.dat_po + 1) From t_must m " +
                      " Where t_mc_selected.nzp_kvar = m.nzp_kvar " +
                      " and t_mc_selected.nzp_serv = m.nzp_serv " +
                      " and t_mc_selected.work_s  < m.dat_po    " +
                      " and t_mc_selected.work_po < m.dat_po    " +
                      " and m.kod <> -1 " +
                      " ) " +
                      " Where kod >= 0 " +
                      "  and Exists ( Select dat_po From  t_must m " +
                      " Where t_mc_selected.nzp_kvar = m.nzp_kvar " +
                      " and t_mc_selected.nzp_serv = m.nzp_serv " +
                      " and t_mc_selected.work_s  < m.dat_po    " +
                      " and t_mc_selected.work_po < m.dat_po    " +
                      " and m.kod <> -1 " +
                      " ) ";
                if (!ExecSQL(sel, true))
                {
                    throw new Exception("Ошибка проверка пересекающихся интервалов");
                }

                //выкинем все ошибочные интервалы
                sel = " Update t_mc_selected" +
                      " Set kod = -1 " +
                      " Where work_s > work_po ";
                if (!ExecSQL(sel, true))
                {
                    throw new Exception("Ошибка удаления ошибочных интервалов");
                }

                MyDataReader reader;
                //определим, есть ли еще необработанные интервалы
                if (!ExecRead(out reader, " Select 1 From t_mc_selected" +
                                          " Where kod = 0 ", true))
                {
                    throw new Exception("Ошибка поиска интервала");
                }

                if (!reader.Read())
                {
                    //все интервалы обработаны, выходим из цикла
                    reader.Close();
                    break;
                }

                reader.Close();
            }

        }


        /// <summary>
        /// ищем ближайшие края среди пересекающихся интервалов в must_calc
        /// </summary>
        /// <returns></returns>
        private void FindIntervalEdges()
        {

            //чтобы ужать рабочую область
            //      [-------]       :table
            // [------]             :must_calc
#if PG
            string sel =
                " with tt as (select max(m.dat_po + 1) as max1,max(m.dat_s + 1) as max2 From  t_must  m, t_mc_selected " +
                "               Where t_mc_selected.nzp_kvar = m.nzp_kvar " +
                "                     and t_mc_selected.nzp_serv = m.nzp_serv " +
                "                     and t_mc_selected.dat_s >= m.dat_s " +
                "                     and t_mc_selected.dat_s <= m.dat_po " +
                "                     and m.kod <> -1) " +
                " Update t_mc_selected " +
                "        Set dat_s = max1," +
                "            work_s = max2" +
                "            from tt" +
                " Where kod = 0 " +
                "       and exists ( select 1 from tt ) ";
#else
            string sel = " Update  t_mc_selected " +
                         " Set (dat_s,work_s) = (( " +
                         " Select max(m.dat_po+1),max(m.dat_po+1) From t_must m " +
                         " Where t_mc_selected.nzp_kvar = m.nzp_kvar " +
                         " and t_mc_selected.nzp_serv = m.nzp_serv " +
                         " and t_mc_selected.dat_s >= m.dat_s " +
                         " and t_mc_selected.dat_s <= m.dat_po " +
                         " and m.kod <> -1 " +
                         " )) " +
                         " Where kod = 0 " +
                         "  and Exists (" +
                         " Select 1 From t_must m " +
                         " Where t_mc_selected.nzp_kvar = m.nzp_kvar " +
                         " and t_mc_selected.nzp_serv = m.nzp_serv " +
                         " and t_mc_selected.dat_s >= m.dat_s " +
                         " and t_mc_selected.dat_s <= m.dat_po " +
                         " and m.kod <> -1 " +
                         " ) ";
#endif

            if (!ExecSQL(sel, true))
            {
                throw new Exception("Ошибка поиска интервала");
            }

            //      [-------]       :table
            //           [-----]    :must_calc
#if PG
            sel = " with tt as (select min(m.dat_s - 1) as min1,min(m.dat_s - 1) as min2 From  t_must  m, t_mc_selected " +
                                 " Where t_mc_selected.nzp_kvar = m.nzp_kvar " +
                                   " and t_mc_selected.nzp_serv = m.nzp_serv " +
                                   " and t_mc_selected.dat_po >= m.dat_s " +
                                   " and t_mc_selected.dat_po <= m.dat_po " +
                                   " and m.kod <> -1) " +
                " Update t_mc_selected " +
                  " Set dat_po = min1,work_po = min2 from tt " +
                   " Where kod = 0 " +
                   "  and Exists ( Select 1 From tt ) ";
#else
            sel = " Update t_mc_selected " +
                  " Set (dat_po,work_po) = (( " +
                  " Select min(m.dat_s - 1),min(m.dat_s - 1) From  t_must  m " +
                  " Where t_mc_selected.nzp_kvar = m.nzp_kvar " +
                  " and t_mc_selected.nzp_serv = m.nzp_serv " +
                  " and t_mc_selected.dat_po >= m.dat_s " +
                  " and t_mc_selected.dat_po <= m.dat_po " +
                  " and m.kod <> -1 " +
                  " )) " +
                  " Where kod = 0 " +
                  "  and Exists ( Select 1 From  t_must  m " +
                  " Where t_mc_selected.nzp_kvar = m.nzp_kvar " +
                  " and t_mc_selected.nzp_serv = m.nzp_serv " +
                  " and t_mc_selected.dat_po >= m.dat_s " +
                  " and t_mc_selected.dat_po <= m.dat_po " +
                  " and m.kod <> -1 " +
                  " ) ";
#endif
            if (!ExecSQL(sel, true))
            {
                throw new Exception("Ошибка поиска интервала");
            }

        }


        /// <summary>
        /// потом определим интервалы, которые полностью покрываются из must_calc, чтобы потом их игнорировать
        /// </summary>
        /// <returns></returns>
        private void SetDublicateIntervals()
        {
            const string sel = " Update t_mc_selected " +
                               " Set kod = -1 " +
                               " Where kod <> -1 " +
                               " and Exists ( " +
                               "Select 1 From  t_must  m " +
                               " Where t_mc_selected.nzp_kvar = m.nzp_kvar " +
                               " and t_mc_selected.nzp_serv = m.nzp_serv " +
                               " and t_mc_selected.dat_s  >= m.dat_s  " +
                               " and t_mc_selected.dat_po <= m.dat_po " +
                               " and m.kod <> -1 " +
                               " ) ";

            if (!ExecSQL(sel, true))
            {
                throw new Exception("Ошибка поиска интервала");
            }
        }


        /// <summary>
        /// Определение непересекающихся интервалов
        /// </summary>
        /// <returns></returns>
        private void SetUnCrossIntervals()
        {
            const string sel = " Update t_mc_selected " +
                               " Set kod = 1 " +
                               " Where kod <> -1 " +
                               " and not Exists ( " +
                               " Select 1 From  t_must  m " +
                               " Where t_mc_selected.nzp_kvar = m.nzp_kvar " +
                               " and t_mc_selected.nzp_serv = m.nzp_serv " +
                               " and t_mc_selected.dat_s  <= m.dat_po " +
                               " and t_mc_selected.dat_po >= m.dat_s  " +
                               " and m.kod <> -1 " +
                               " ) ";

            if (!ExecSQL(sel, true))
            {
                throw new Exception("Ошибка поиска интервала");
            }
        }

        /// <summary>
        /// Загрузка существующих записей из must_calc
        /// </summary>
        /// <returns></returns>
        private void LoadExistsMustCalcRecords()
        {
            //для быстроты сначала выберим все из must_calc

            ExecSQL(" Drop table t_must ", false);

            string sel = "Create temp table t_must (nzp_kvar integer, nzp_serv integer, " +
                         "dat_s date, dat_po date, kod integer, work_s date," +
                         " work_po date)" + DBManager.sUnlogTempTable;

#if PG
            sel += " with oids";
#else
#endif
            if (!ExecSQL(sel, true))
            {
                throw new Exception("Ошибка создания таблицы t_must ");
            }


            sel = "insert into t_must " +
                        " Select distinct m.nzp_kvar, m.nzp_serv, m.dat_s, m.dat_po, 0 kod, " +
                        DBManager.sCurDate + " as work_s, " + DBManager.sCurDate + " as work_po " +
                        " From " + _editData.database + "must_calc m, t_mc_selected t " +
                        " Where t.nzp_kvar = m.nzp_kvar " +
                        " and t.nzp_serv = m.nzp_serv " +
                        " and m.year_  = " + _editData.year +
                        " and m.month_ = " + _editData.month;

            if (!ExecSQL(sel, true))
            {
                throw new Exception("Ошибка выборки данных ");
            }

            //создать случайный индекс 
            CreateOneIndex("Create index msel_" + RandomText.Generate() + "_" +
                Math.Abs(_editData.local_user) + "_1 on t_must " +
                 "(nzp_kvar,nzp_serv,dat_s,dat_po,kod)");

            CreateOneIndex("Create index msel_" + RandomText.Generate() + "_" +
                Math.Abs(_editData.local_user) + "_2 on t_must(kod)");


            ExecSQL(DBManager.sUpdStat + " t_must", true);

        }


        /// <summary>
        /// Установка признака перерасчета
        /// </summary>
        /// <returns></returns>
        private MustCalcReasons GetMustCalcReasons()
        {
            var result = MustCalcReasons.Undefined;
            switch (_editData.mcalcType)
            {
                case enMustCalcType.mcalc_Serv: result = MustCalcReasons.Service; break;
                case enMustCalcType.Counter: result = MustCalcReasons.Counter; break;
                case enMustCalcType.Nedop: result = MustCalcReasons.Nedop; break;
                case enMustCalcType.mcalc_Prm1: result = MustCalcReasons.Parameter; break;
                case enMustCalcType.mcalc_Prm2: result = MustCalcReasons.Parameter; break;
                case enMustCalcType.mcalc_Gil: result = MustCalcReasons.Gil; break;
                case enMustCalcType.DomCounter: result = MustCalcReasons.DomCounter; break;
                case enMustCalcType.GroupCounter: result = MustCalcReasons.Counter; break;
                case enMustCalcType.Prm17: result = MustCalcReasons.Parameter; break;
            }
            return result;
        }

        /// <summary>
        /// Подготовка таблицы для групповых изменений
        /// </summary>
        /// <returns></returns>
        private void InitSelectedMc()
        {
            string sel = "";

            const string sql = " Create temp table t_mc_selected (" +
                               " nzp_kvar integer, " +
                               " nzp_serv integer," +
                               " kod integer default 0," +
                               " dat_s Date," +
                               " dat_po Date, " +
                               " work_s Date, " +
                               " work_po Date)" + DBManager.sUnlogTempTable +

#if PG
 " with oids" +
#endif
 "";
            ExecSQL(sql, true);
            string filter = _editData.dopFind.Where(s => s.Trim() != "").Aggregate(String.Empty, (current, s) => current + s);

            string destinationTable = _editData.database + _editData.table;
            string localKernel = _editData.pref + DBManager.sKernelAliasRest;
            string localData = _editData.pref + DBManager.sDataAliasRest;

            switch (_editData.mcalcType)
            {
                case enMustCalcType.mcalc_Serv:
                    sel = " insert into t_mc_selected(nzp_kvar, nzp_serv, dat_s, dat_po, work_s, work_po) " +
                          " Select distinct nzp_kvar, nzp_serv, dat_s, dat_po, dat_s, dat_po " +
                          " From " + destinationTable + " p " +
                          " Where month_calc <= " + EndDate + " and month_calc >= " + StartDate +
                          " " + filter;
                    break;
                case enMustCalcType.Counter:
                    sel = " insert into t_mc_selected(nzp_kvar, nzp_serv, dat_s, dat_po, work_s, work_po) " +
                          " Select distinct nzp_kvar, nzp_serv, dat_s, dat_po, dat_s, dat_po " +
                          " From " + destinationTable + " p " +
                          " Where month_calc <= " + EndDate + " and month_calc >= " + StartDate +
                          " " + filter;

                    break;
                case enMustCalcType.Nedop:
                    sel = " insert into t_mc_selected(nzp_kvar, nzp_serv, dat_s, dat_po, work_s, work_po) " +
                          " Select distinct nzp_kvar, nzp_serv, dat_s, dat_po, dat_s, dat_po " +
                          " From " + destinationTable + " p " +
                          " Where month_calc <= " + EndDate + " and month_calc >= " + StartDate +
                          " " + filter;
                    break;


                //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                // выборка из prm_1,3 - связка prm_1, prm_frm,
                //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                case enMustCalcType.mcalc_Prm1:
                    {
                        Returns ret;
                        var disableTotalRecalcObj = ExecScalar(string.Format(@"
                        SELECT val_prm
                        FROM {0}prm_5 
                        WHERE is_actual = 1 AND nzp_prm = 2201 AND dat_s <= {1} AND dat_po >= {1}", localData, DBManager.sCurDate), out ret,
                            true);
                        if (!ret.result)
                        {
                            throw new Exception("Ошибка чтения настроек перерасчёта");
                        }

                        var disableTotalRecalc = disableTotalRecalcObj != null && disableTotalRecalcObj.ToString().PrmValToBool();

                        sel = " insert into t_mc_selected(nzp_kvar, nzp_serv, dat_s, dat_po, work_s, work_po) " +
                              " Select distinct t.nzp_kvar, t.nzp_serv, p.dat_s, p.dat_po, p.dat_s, p.dat_po " +
                              " From " + destinationTable + " p, " +
                              localData + "tarif t, " +
                              localKernel + "prm_frm f " +
                              " Where p.nzp = t.nzp_kvar " +
                              " and t.is_actual <> 100 " +
                              " and p.nzp_prm = f.nzp_prm " +
                              (!disableTotalRecalc ? " and p.nzp_prm NOT IN (SELECT nzp_prm FROM " + localKernel + "prm_frm WHERE nzp_frm=2004) " : string.Empty) + //кроме параметров явно не связанных с формулами 
                              " and f.nzp_frm = t.nzp_frm and f.is_prm = 1" +
                              " and p.month_calc <= " + EndDate + " and p.month_calc >= " + StartDate +
                              " " + filter;

                        if (!disableTotalRecalc)
                        {
                            if (!ExecSQL(sel, true))
                            {
                                throw new Exception("Ошибка выборки данных из целевой таблицы ");
                            }
                            sel = " insert into t_mc_selected(nzp_kvar, nzp_serv, dat_s, dat_po, work_s, work_po) " +
                                  " Select p.nzp as nzp_kvar, " + DBManager.sNvlWord + "(pf.frm_p1" + DBManager.sConvToInt + ", 0) as nzp_serv, p.dat_s, p.dat_po, p.dat_s, p.dat_po " +
                                  " From " + destinationTable + " p,  " + localKernel + "prm_frm pf " +
                                  " Where p.nzp_prm=pf.nzp_prm and pf.nzp_frm=2004 " + //для параметров явно не связанных с формулами 
                                  " and p.month_calc <= " + EndDate + " and p.month_calc >= " + StartDate +
                                  " " + filter;
                        }
                    }
                    break;


                //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                // выборка из prm_2,4 - связка prm_2, prm_frm, kvar (по дому)
                //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                case enMustCalcType.mcalc_Prm2:

                    ExecSQL("drop table temp_pp", false);

                    sel = " Create temp table temp_pp (nzp integer," +
                          " month_calc Date, " +
                          " nzp_prm integer," +
                          " dat_s Date," +
                          " dat_po Date)" + DBManager.sUnlogTempTable;
                    if (!ExecSQL(sel, true))
                    {
                        throw new Exception("Ошибка сохранения признака перерасчета по параметрам");
                    }

                    sel = "insert into temp_pp (nzp, month_calc, nzp_prm, dat_s, dat_po )" +
                          " Select nzp, month_calc, nzp_prm, dat_s, dat_po  " +
                          " From " + destinationTable + " p " +
                          " Where p.month_calc <= " + EndDate + " and p.month_calc >= " + StartDate +
                          " " + filter;
                    if (!ExecSQL(sel, true))
                    {
                        throw new Exception("Ошибка выборки данных из prm_2");
                    }

                    CreateOneIndex("Create index uni_temp_pp1 on temp_pp (nzp,month_calc, nzp_prm)");
                    ExecSQL(sel, true);
                    ExecSQL(DBManager.sUpdStat + " temp_pp ", true);


                    sel = " insert into t_mc_selected(nzp_kvar, nzp_serv, dat_s, dat_po, work_s, work_po) " +
                          " Select distinct t.nzp_kvar, t.nzp_serv, p.dat_s, p.dat_po, p.dat_s, p.dat_po " +
                          " From temp_pp p, " +
                          localData + "kvar k, " +
                          localData + "tarif t, " +
                          localKernel + "prm_frm f " +
                          " Where p.nzp = k.nzp_dom " +
                          " and k.nzp_kvar = t.nzp_kvar " +
                          " and t.is_actual <> 100 " +
                          " and p.nzp_prm = f.nzp_prm " +
                          " and f.nzp_frm = t.nzp_frm and f.is_prm = 1" +
                          filter;
                    if (!ExecSQL(sel, true))
                    {
                        throw new Exception("Ошибка сохранения признака перерасчета по параметрам");
                    }

                    //добавляем перерасчет по параметрам влияющим на начисления
                    sel = " insert into t_mc_selected(nzp_kvar, nzp_serv, dat_s, dat_po, work_s, work_po) " +
                             " Select distinct k.nzp_kvar, " + DBManager.sNvlWord + "(f.frm_p1" + DBManager.sConvToInt + ", 0) as nzp_serv," +
                             " p.dat_s, p.dat_po, p.dat_s, p.dat_po " +
                             " From temp_pp p, " +
                             localData + "kvar k, " +
                             localKernel + "prm_frm f " +
                             " Where p.nzp = k.nzp_dom " +
                             " and p.nzp_prm = f.nzp_prm " +
                             " and f.nzp_frm=2004" + //для параметров явно не связанных с формулами 
                             filter;
                    break;

                case enMustCalcType.mcalc_Gil:
                    sel = " insert into t_mc_selected(nzp_kvar, nzp_serv, dat_s, dat_po, work_s, work_po) " +
                       " Select distinct p.nzp_kvar, a.nzp_serv, p.dat_s, p.dat_po, p.dat_s, p.dat_po " +
                       " From " + destinationTable + " p " +
                       ", " + Points.Pref + DBManager.sKernelAliasRest + "serv_must_calc a" +
                       ", " + localData + "tarif t " +
                       " Where a.nzp_reason = " + (int)MustCalcReasons.Gil +
                       " and t.nzp_kvar = p.nzp_kvar and t.nzp_serv = a.nzp_serv and t.is_actual <> 100 and p.is_actual <> 100 " +
                       filter;
                    break;

                case enMustCalcType.DomCounter:
                    sel = " insert into t_mc_selected(nzp_kvar, nzp_serv, dat_s, dat_po, work_s, work_po) " +
                          " Select distinct k.nzp_kvar, p.nzp_serv, p.dat_s, p.dat_po, p.dat_s, p.dat_po " +
                          " From " + destinationTable + " p, " +
                          localData + "kvar k" +
                          " Where month_calc <= " + EndDate + " and month_calc >= " + StartDate +
                          " and k.nzp_dom = p.nzp_dom and is_actual<>100" +
                          filter;
                    break;

                case enMustCalcType.GroupCounter:
                    sel = " insert into t_mc_selected(nzp_kvar, nzp_serv, dat_s, dat_po, work_s, work_po) " +
                          " Select distinct k.nzp_kvar, p.nzp_serv, p.dat_s, p.dat_po, p.dat_s, p.dat_po " +
                          " From " + destinationTable + " p" +
                          ", " + localData + "counters_link k" +
                          " Where month_calc <= " + EndDate + " and month_calc >= " + StartDate +
                          " and k.nzp_counter = p.nzp_counter " +
                          filter;
                    break;

                case enMustCalcType.Prm17:
                    ExecSQL("drop table temp_pp", false);

                    sel = " Create temp table temp_pp (nzp_counter integer," +
                          " nzp_type integer, " +
                          " nzp integer," +
                          " dat_s Date," +
                          " dat_po Date)" + DBManager.sUnlogTempTable;
                    if (!ExecSQL(sel, true)) throw new Exception();

                    sel = " insert into temp_pp(nzp_counter, nzp_type, nzp, dat_s, dat_po) " +
                          " Select distinct nzp_counter, nzp_type, nzp, p.dat_s, p.dat_po " +
                          " From " + destinationTable + " p, " +
                          localData + "counters_spis c " +
                          " Where p.nzp = c.nzp_counter " + filter;
                    if (!ExecSQL(sel, true)) throw new Exception();

                    sel = "Create index uni_temp_pp2 on temp_pp (nzp_counter, nzp_type, nzp)";
                    if (!ExecSQL(sel, true)) throw new Exception();


                    ExecSQL(DBManager.sUpdStat + " temp_pp ", true);
                    ExecSQL("t_mc_selected1", true);

                    string sel1 = " Create temp table t_mc_selected1 (" +
                                  " nzp_kvar integer, " +
                                  " nzp_serv integer," +
                                  " kod integer default 0," +
                                  " dat_s Date," +
                                  " dat_po Date, " +
                                  " work_s Date, " +
                                  " work_po Date)" + DBManager.sUnlogTempTable;
                    ExecSQL(sel1, true);

                    sel1 = " insert into t_mc_selected1(nzp_kvar, nzp_serv, dat_s, dat_po, work_s, work_po) " +
                           " Select distinct t.nzp_kvar, t.nzp_serv, p.dat_s, p.dat_po, p.dat_s, p.dat_po " +
                           " From temp_pp c, " + destinationTable + " p, " +
                           localData + "tarif t, " +
                           localKernel + "prm_frm f " +
                           " Where c.nzp_counter = p.nzp and c.nzp_type = " + (int)CounterKinds.Kvar +
                           " and c.nzp = t.nzp_kvar " +
                           " and t.is_actual <> 100 " +
                           " and p.nzp_prm = f.nzp_prm " +
                           " and f.nzp_frm = t.nzp_frm and f.is_prm = 1" +
                           " and p.month_calc <= " + EndDate + " and p.month_calc >= " + StartDate
                           + filter;
                    if (!ExecSQL(sel1, true))
                    {
                        throw new Exception("Ошибка выборки данных из целевой таблицы ");
                    }

                    sel1 = " insert into t_mc_selected1(nzp_kvar, nzp_serv, dat_s, dat_po, work_s, work_po) " +
                           " Select distinct t.nzp_kvar, t.nzp_serv,p.dat_s, p.dat_po, p.dat_s, p.dat_po " +
                           " From temp_pp c, " + destinationTable + " p " +
                           ", " + localData + "kvar k" +
                           ", " + localData + "tarif t, " +
                           localKernel + "prm_frm f " +
                           " Where c.nzp_counter = p.nzp and c.nzp_type = " + (int)CounterKinds.Dom +
                           " and c.nzp = k.nzp_dom " +
                           " and k.nzp_kvar = t.nzp_kvar " +
                           " and t.is_actual <> 100 " +
                           " and p.nzp_prm = f.nzp_prm " +
                           " and f.nzp_frm = t.nzp_frm and f.is_prm = 1" +
                           " and p.month_calc <= " + EndDate + " and p.month_calc >= " + StartDate
                           + filter;
                    if (!ExecSQL(sel1, true))
                    {
                        throw new Exception("Ошибка выборки данных из целевой таблицы ");
                    }

                    sel1 = " insert into t_mc_selected1(nzp_kvar, nzp_serv, dat_s, dat_po, work_s, work_po) " +
                           " Select distinct t.nzp_kvar, t.nzp_serv,p.dat_s, p.dat_po, p.dat_s, p.dat_po " +
                           " From temp_pp c, " + destinationTable + " p, " +
                           localData + "counters_link k, " +
                           localData + "tarif t, " +
                           localKernel + "prm_frm f " +
                           " Where c.nzp_counter = p.nzp and c.nzp_type in (" + (int)CounterKinds.Group + "," +
                           (int)CounterKinds.Communal + ") " +
                           " and c.nzp_counter = k.nzp_counter " +
                           " and k.nzp_kvar = t.nzp_kvar " +
                           " and t.is_actual <> 100 " +
                           " and p.nzp_prm = f.nzp_prm " +
                           " and f.nzp_frm = t.nzp_frm and f.is_prm = 1" +
                           " and p.month_calc <= " + EndDate + " and p.month_calc >= " + StartDate
                           + filter;
                    if (!ExecSQL(sel1, true))
                    {
                        throw new Exception("Ошибка выборки данных из целевой таблицы ");
                    }

                    sel = " insert into t_mc_selected(nzp_kvar, nzp_serv, dat_s, dat_po, work_s, work_po) " +
                           " Select distinct t.nzp_kvar, t.nzp_serv,p.dat_s, p.dat_po, p.dat_s, p.dat_po " +
                           " From  t_mc_selected1";
                    break;
            }

            try
            {



                if (!ExecSQL(sel, true))
                {
                    throw new Exception("Ошибка выборки данных из целевой таблицы ");
                }
            }
            finally
            {
                ExecSQL("drop table t_mc_selected1", false);
                ExecSQL("drop table temp_pp", false);
            }


            SetRightDate();

            InsertDependensServices();

            MakeIndexForSelected();

            DeleteUnusedRecords();

            ExecSQL(DBManager.sUpdStat + " t_mc_selected", true);

        }


        /// <summary>
        /// Удаление бесполезных записей
        /// </summary>
        /// <returns></returns>
        private void DeleteUnusedRecords()
        {
            const string sel = " Delete From t_mc_selected " +
                               " Where dat_s > dat_po  ";

            if (!ExecSQL(sel, true))
            {
                throw new Exception("Ошибка усечения интервала");
            }

        }


        /// <summary>
        /// Создание индексов для таблицы с выбранными перерасчетами
        /// </summary>
        /// <returns></returns>
        private void MakeIndexForSelected()
        {
            var user = Math.Abs(_editData.nzp_user);
            CreateOneIndex("Create index sel_" + RandomText.Generate() + "_" +
                           user +
                           "_1 on t_mc_selected(nzp_kvar,nzp_serv,dat_s,dat_po,kod)");

            CreateOneIndex("Create index sel_" + RandomText.Generate() + "_" +
                           user + "_2 on t_mc_selected(dat_s,dat_po)");


            CreateOneIndex("Create index sel_" + RandomText.Generate() + "_" +
                           user + "_3 on t_mc_selected(kod)");

        }


        /// <summary>
        /// Установка правильных дат по Айдару
        /// на конец периода - последний день
        /// </summary>
        private void SetRightDate()
        {
            string firstDayMonth = new DateTime(_editData.year, _editData.month, 1).AddDays(-1).ToShortDateString();

#if PG
            const string firstDayMonthBd = "date(date_trunc('month',dat_s))";
#else
            const string firstDayMonthBd = "mdy(month(dat_s),1,year(dat_s))";
#endif

            string sel = " update t_mc_selected set dat_s= (case when dat_s is null then Date('" + firstDayMonth +
                         "') else " + firstDayMonthBd + "  end)";
            ExecSQL(sel, true);

            sel = "update t_mc_selected set work_s = dat_s";
            if (!ExecSQL(sel, true))
            {
                throw new Exception("Ошибка обновления таблицы t_mc_selected");
            }


            string lastDayMonth = new DateTime(_editData.year, _editData.month, 1).AddDays(-1).ToShortDateString();
            string curDayMonth = new DateTime(_editData.year, _editData.month, 1).ToShortDateString();
#if PG
            const string lastDayBdField = "date(date_trunc('month',dat_po)+interval '1 month'-interval '1 second'";
#else
            const string lastDayBdField = "date(mdy( month(dat_po),1,extract(year from dat_s)) + 1 units month - 1 units day";
#endif

            sel =
                " update t_mc_selected set dat_po = " +
                " (" +
                " case when dat_po is null then date('" + lastDayMonth + "') " +
                " when dat_po >= '" + curDayMonth + "' then date('" + lastDayMonth + "') " +
                "       else " + lastDayBdField + ") end " +
                " )";

            if (!ExecSQL(sel, true))
            {
                throw new Exception("Ошибка обновления таблицы t_mc_selected");
            }

            sel = "update t_mc_selected set work_po = dat_po";
            if (!ExecSQL(sel, true))
            {
                throw new Exception("Ошибка обновления таблицы t_mc_selected");
            }

        }


        /// <summary>
        /// Добавление перерасчета по связанным услугам
        /// </summary>
        /// <returns></returns>
        private void InsertDependensServices()
        {
            //---------------------------------------------------------------------------------------------------------------------------------------------------
            // сохранить данные из t_selected во временную таблицу 


            ExecSQL("Drop table t_mc_dep_serv", false);

            var sel = String.Empty;

            sel = " Create temp table t_mc_dep_serv(" +
                          " nzp_serv integer, " +
                          " nzp_kvar integer, " +
                          " dat_s Date, " +
                          " dat_po Date, " +
                          " kod integer, " +
                          " work_s Date, " +
                          " work_po Date," +
                          " nzp_area integer )" +
                          DBManager.sUnlogTempTable;
            ExecSQL(sel, true);


            //получаем только действующие связанные услуги
            ExecSQL("Drop table t_active_serv", false);
            sel = "create temp table t_active_serv (nzp_kvar integer, nzp_serv integer);";
            if (!ExecSQL(sel, true)) throw new Exception();

            sel = " insert into t_active_serv (nzp_kvar,nzp_serv) " +
                " select nzp_kvar, nzp_serv from " + _editData.database + "tarif t " +
                " where is_actual<>100 and " + _editData.dat_s + "<=dat_po and dat_s<=" + _editData.dat_po +
                " and exists (select 1 from t_mc_selected mc where t.nzp_kvar=mc.nzp_kvar) ";
            if (!ExecSQL(sel, true)) throw new Exception();

            if (_editData.mcalcType != enMustCalcType.mcalc_Serv)
            {
                sel = " insert into t_mc_dep_serv(nzp_serv, nzp_kvar, dat_s, " +
                  " dat_po, kod, work_s, work_po, nzp_area) " +
                  " select t.nzp_serv, t.nzp_kvar, t.dat_s, t.dat_po, t.kod, " +
                  " t.work_s, t.work_po, k.nzp_area " +
                  " from t_mc_selected t, " + Points.Pref + DBManager.sDataAliasRest + "kvar k, t_active_serv a " +
                  " where t.nzp_kvar = k.nzp_kvar and a.nzp_kvar=t.nzp_kvar and a.nzp_serv=t.nzp_serv ";
                if (!ExecSQL(sel, true)) throw new Exception();
            }
            else //действие с услугами (услуга может быть закрыта в текущей операции)
            {
                sel = " insert into t_mc_dep_serv(nzp_serv, nzp_kvar, dat_s, " +
                   " dat_po, kod, work_s, work_po, nzp_area) " +
                   " select t.nzp_serv, t.nzp_kvar, t.dat_s, t.dat_po, t.kod, " +
                   " t.work_s, t.work_po, k.nzp_area " +
                   " from t_mc_selected t, " + Points.Pref + DBManager.sDataAliasRest + "kvar k  " +
                   " where t.nzp_kvar = k.nzp_kvar";
                if (!ExecSQL(sel, true)) throw new Exception();
            }

            sel = "SELECT distinct nzp_serv, nzp_area FROM t_mc_dep_serv ";
            using (var DT = ClassDBUtils.OpenSQL(sel, Connection).resultData)
            {

                int nzpServ = 0;
                int nzpArea = 0;
                foreach (DataRow rdr in DT.Rows)
                {
                    if (rdr["nzp_serv"].ToString().Trim() != "") nzpServ = Convert.ToInt32(rdr["nzp_serv"]);
                    if (rdr["nzp_area"] != null)
                        if (rdr["nzp_area"].ToString().Trim() != "") nzpArea = Convert.ToInt32(rdr["nzp_area"]);

                    var srv = new DbServKernel();
                    List<int> lstServices = srv.GetDependenciesServicesList(Connection, Trans, nzpServ, nzpArea, out Ret).Distinct().ToList();
                    if (!Ret.result)
                    {
                        throw new Exception("Ошибка определения связанных услуг " + Ret.text);
                    }

                    if (lstServices.Count <= 0) continue;

                    foreach (int t in lstServices)
                    {

                        sel = " INSERT INTO t_mc_selected(nzp_serv, nzp_kvar, dat_s, dat_po," +
                              " kod, work_s, work_po)" +
                              " select " + t + ", t.nzp_kvar, t.dat_s, t.dat_po, t.kod, t.work_s, t.work_po " +
                              " from t_mc_dep_serv t, t_active_serv a  " +
                              " where t.nzp_area = " + nzpArea + " and t.nzp_serv = " + nzpServ +
                              " and " + t + "=a.nzp_serv and t.nzp_kvar=a.nzp_kvar";

                        if (!ExecSQL(sel, true)) throw new Exception();
                    }
                }
            }
            ExecSQL("Drop table t_mc_dep_serv", false);
            ExecSQL("Drop table t_active_serv", false);


        }




        // Начальный этап сохранения - добавляются поля в целевую таблицу
        //----------------------------------------------------------------------
        public void PreEdit()
        //----------------------------------------------------------------------
        {
            try
            {

                Ret = Utils.InitReturns();
                if (_editData.nzp_user < 1) throw new Exception("Не определен пользователь");

                if (_editData.keys == null || _editData.vals == null || _editData.dopFind == null)
                    throw new Exception("Не определены ключи выборки данных");

                if (_editData.pref == "") //здесь должна содержаться база данных, где находится _editData.table
                {
                    if (_editData.isCentral ||
                        _editData.table.Trim().ToUpper() == "PRM_7" ||
                        _editData.table.Trim().ToUpper() == "PRM_8" ||
                        _editData.table.Trim().ToUpper() == "PRM_12" ||
                        _editData.table.Trim().ToUpper() == "servpriority".ToUpper())
                    {
                        _editData.pref = Points.Pref;
                    }
                    else throw new Exception("Не указана целевая база");

                    RecordMonth rm = Points.GetCalcMonth(new CalcMonthParams());
                    _editData.year = rm.year_;
                    _editData.month = rm.month_;
                }
                else
                {
                    RecordMonth rm = Points.GetCalcMonth(new CalcMonthParams(_editData.pref));
                    _editData.year = rm.year_;
                    _editData.month = rm.month_;
                }

                //определение локального пользователя
                //не знаю, к чему это приведет, но пока убрал определение локального пользователя, если он уже определен и база не менялась. (с)Айдар 01.03.2012
                if (_editData.local_user < 1 || _editData.database != _editData.pref.Trim() + "_data")
                {
                    _editData.local_user = _editData.nzp_user;

                    /*var db = new DbWorkUser();
                    _editData.local_user = db.GetLocalUser(Connection, Trans, _editData, out Ret);
                    db.Close();
                    if (!Ret.result)
                    {
                        return;
                    }*/

                    string baseName;
                    if (_editData.databaseType == enDataBaseType.kernel ||
                        _editData.table.Trim().ToUpper() == "servpriority".ToUpper())
                    {
                        baseName = _editData.pref.Trim() + "_kernel";
                    }
                    else
                        baseName = _editData.pref.Trim() + "_data";

                    _editData.database = DBManager.GetFullBaseName(Connection, baseName, "");
                }
            }
            catch (Exception ex)
            {
                Ret.text = ex.Message;
                Ret.result = false;
            }

        }

        //объединение пересекающихся интервалов во временной таблице
        //----------------------------------------------------------------------
        void UnionTempTable(string tSelected)
        //----------------------------------------------------------------------
        {
            ExecSQL(" Drop table t_union", false);
            string sel = "Create temp table t_union (" +
                         " nzp_kvar integer," +
                         " nzp_serv integer," +
                         " dat_s date," +
                         " dat_po Date)" + DBManager.sUnlogTempTable;
            if (!ExecSQL(sel, true)) return;

            try
            {
                // последовательные периоды тоже объединяются
                // todo: при наличии 2 независимых пар пересекающихся периодов данный код сработает неправильно, он создаст 1 общий период вместо 2 отдельных
                sel = " insert into t_union ( nzp_kvar, nzp_serv, dat_s, dat_po) " +
                      " Select a.nzp_kvar, a.nzp_serv, min(a.dat_s) as dat_s, max(a.dat_po) as dat_po " +
                      " From " + tSelected + " a, " + tSelected + " b " +
                      " Where a.nzp_kvar = b.nzp_kvar " +
                      "     and a.nzp_serv = b.nzp_serv " +
                      "     and a.dat_s  <= b.dat_po " +
                      "     and a.dat_po >= b.dat_s  " +
#if PG
 "     and a.oid <> b.oid   " +
#else
                      "     and a.rowid <> b.rowid   " +
#endif
 " Group by 1,2 ";


                if (!ExecSQL(sel, true)) throw new Exception("Ошибка выборки данных из целевой таблицы");


                //создать случайный индекс 
                sel = "Create index uni_" + RandomText.Generate() + "_" + Math.Abs(_editData.nzp_user) + "1 on t_union" +
                      "(nzp_kvar,nzp_serv,dat_s,dat_po)";


                if (!ExecSQL(sel, true)) throw new Exception("Ошибка создания индексов");

                ExecSQL(DBManager.sUpdStat + " t_union ", true);

                //удалить пересекающиеся интервалы
                sel = " Update " + tSelected +
                      " Set kod = -1 " +
                      " Where Exists ( " +
                      " Select 1 From t_union u " +
                      " Where u.nzp_kvar = " + tSelected + ".nzp_kvar " +
                      "   and u.nzp_serv = " + tSelected + ".nzp_serv " +
                      "   and u.dat_s  <= " + tSelected + ".dat_po " +
                      "   and u.dat_po >= " + tSelected + ".dat_s " +
                      " ) ";


                if (!ExecSQL(sel, true)) throw new Exception("Ошибка поиска дублирования");


                //вставка объединенных интервалов
                sel = " Insert into " + tSelected + " (nzp_kvar,nzp_serv,dat_s,dat_po,work_s,work_po,kod)  " +
                      " Select nzp_kvar,nzp_serv,dat_s,dat_po,dat_s,dat_po,0 " +
                      " From t_union";

                if (!ExecSQL(sel, true)) throw new Exception("Ошибка поиска дублирования");

            }
            catch (Exception ex)
            {
                Ret.text = ex.Message;
            }
            finally
            {
                //удалить t_union - он больше не нужен
                ExecSQL(" Drop table t_union", false);
            }
        }
    }

}
