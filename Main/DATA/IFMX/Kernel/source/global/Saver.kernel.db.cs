using System;
using System.Linq;
using System.Collections.Generic;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System.Data;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Text;


namespace STCLINE.KP50.DataBase
{

    /// <summary>
    /// Класс отвечающий за сохранение 
    /// </summary>
    public class DbSaverNew : DbIntervalBase
    {

        /// <summary>
        /// Поля таблицы
        /// </summary>
        private string _fields;

        /// <summary>
        /// Ключевые поля таблицы
        /// </summary>
        private string _keyfields;

        /// <summary>
        /// поля значения таблицы
        /// </summary>
        private string _valfields;

        /// <summary>
        /// Фильтр ограничение на операцию
        /// </summary>
        private string _filter;


        public DbSaverNew(IDbConnection connection, EditInterData editData)
        {
            Connection = connection;
            EditData = editData;
            Trans = null;
        }


        public DbSaverNew(IDbConnection connection)
        {
            Connection = connection;
            Trans = null;
        }
        public DbSaverNew(IDbConnection connection, EditInterData editData, IDbTransaction trans)
        {
            Connection = connection;
            EditData = editData;
            Trans = trans;
        }

        /// <summary>
        /// Пустой конструктор по умолчанию вызывать пока нельзя
        /// </summary>
        protected DbSaverNew()
        {

        }


        // Начальный этап сохранения - добавляются поля в целевую таблицу
        //----------------------------------------------------------------------
        public void PreEdit()
        //----------------------------------------------------------------------
        {
            try
            {

                Ret = Utils.InitReturns();
                if (EditData.nzp_user < 1) throw new Exception("Не определен пользователь");

                if (EditData.keys == null || EditData.vals == null || EditData.dopFind == null)
                    throw new Exception("Не определены ключи выборки данных");

                if (EditData.pref == "") //здесь должна содержаться база данных, где находится editData.table
                {
                    if (EditData.isCentral ||
                        EditData.table.Trim().ToUpper() == "PRM_7" ||
                        EditData.table.Trim().ToUpper() == "PRM_8" ||
                        EditData.table.Trim().ToUpper() == "PRM_12" ||
                        EditData.table.Trim().ToUpper() == "servpriority".ToUpper())
                    {
                        EditData.pref = Points.Pref;
                    }
                    else throw new Exception("Не указана целевая база");

                    RecordMonth rm = Points.GetCalcMonth(new CalcMonthParams());
                    EditData.year = rm.year_;
                    EditData.month = rm.month_;
                }
                else
                {
                    RecordMonth rm = Points.GetCalcMonth(new CalcMonthParams(EditData.pref));
                    EditData.year = rm.year_;
                    EditData.month = rm.month_;
                }


                if (EditData.local_user < 1)
                {
                    EditData.local_user = EditData.nzp_user;

                    /*var db = new DbWorkUser();
                    EditData.local_user = db.GetLocalUser(Connection, Trans, EditData, out Ret);
                    db.Close();
                    if (!Ret.result)
                    {
                        return;
                    }*/
                }

                //определение локального пользователя
                //не знаю, к чему это приведет, но пока убрал определение локального пользователя, если он уже определен и база не менялась. (с)Айдар 01.03.2012
                if (EditData.local_user < 1 || EditData.database != EditData.pref.Trim() + "_data")
                {
                    EditData.local_user = EditData.nzp_user;

                    /*
                    var db = new DbWorkUser();
                    EditData.local_user = db.GetLocalUser(Connection, Trans, EditData, out Ret);
                    db.Close();
                    if (!Ret.result)
                    {
                        return;
                    }*/

                    if (EditData.databaseType == enDataBaseType.kernel ||
                        EditData.table.Trim().ToUpper() == "servpriority".ToUpper())
                    {
#if PG
                        EditData.database = EditData.pref.Trim() + "_kernel.";
#else
                        EditData.database = EditData.pref.Trim() + "_kernel@" + DBManager.getServer(Connection) +
                                            DBManager.tableDelimiter;
#endif
                    }
#if PG
                    else EditData.database = EditData.pref.Trim() + "_data.";
#else
                    else
                        EditData.database = EditData.pref.Trim() + "_data@" + DBManager.getServer(Connection) +
                                            DBManager.tableDelimiter;
#endif
                }

                MakeSelectFields();
            }
            catch (Exception ex)
            {
                Ret.text = ex.Message;
                Ret.result = false;
            }

        }



        //Порядок сохранения интервальных данных:
        // 1) приготовить temp1-таблицу с набором записей, которые будут изменяться (лс, параметры и т.д.), создается по Keys или dopFind
        // 2) Установить отметки редактировнаия (заблокировать запись) 
        // 3) Выбрать задетые интервалы в temp2-таблицу
        // 4) Вычислить измененные интервалы 
        // 5) Вставка (изменения) данных
        // 6) Снятие заблокированных записей
        /// <summary>
        /// Сохранение интервальных данных
        /// </summary>
        /// <returns>Результат операции</returns>
        public Returns Saver(EditInterData editData)
        {
            EditData = editData;
            return Saver();
        }

        //Порядок сохранения интервальных данных:
        // 1) приготовить temp1-таблицу с набором записей, которые будут изменяться (лс, параметры и т.д.), создается по Keys или dopFind
        // 2) Установить отметки редактировнаия (заблокировать запись) 
        // 3) Выбрать задетые интервалы в temp2-таблицу
        // 4) Вычислить измененные интервалы 
        // 5) Вставка (изменения) данных
        // 6) Снятие заблокированных записей
        /// <summary>
        /// Сохранение интервальных данных
        /// </summary>
        /// <returns>Результат операции</returns>
        public Returns Saver()
        {
            _filter = String.Empty;
            PreEdit();
            if (!Ret.result)
            {
                return Ret;
            }
            try
            {
                SelectStoredData();
                BuildIndexSaver();
                CheckUserBlockRecords();
                string mes = Ret.tag == -1 ? Ret.text : String.Empty;
                DoWork();
                //Формирование протокола содержащего записи по заблокированным данным
                if (!CreateBlockUserEntitiesReport(out mes)) mes += "\r\n Подробности можно увидет в протоколе(Мои файлы -> Протокол заблокированных данных пользователями (режим групповые операции))";
                Ret.result = true;
                if (!String.IsNullOrEmpty(mes))
                {
                    Ret.text = mes;
                    Ret.tag = -1;
                }
            }
            catch (Exception ex)
            {
                Ret.text = ex.Message + " класс Saver";
                Ret.result = false;
                MonitorLog.WriteLog(" Ошибка сохранения интервальных данных " + Ret.text,
                    MonitorLog.typelog.Error, 20, 201, true);
            }
            finally
            {
                DBManager.ExecSQL(Connection, " Drop table t_saver_s", false);
                DBManager.ExecSQL(Connection, " Drop table t_interdata", false);
            }

            return Ret;

        }

        /// <summary>
        /// Метод созедражищий изменение данных в базе
        /// </summary>
        private void DoWork()
        {
            BlockRecords();
            try
            {
                CheckIfAllRecordsLocked();

                SelectIntervals();

                BuildIndexInterData();

                WorkWithInterval();

                SaveData();
            }
            finally
            {
                ResetBlockFromRecords();
            }
        }


        /// <summary>
        /// Снять блокировки с записей
        /// </summary>
        /// <returns></returns>
        private void ResetBlockFromRecords()
        {
            string sel = " Update " + EditData.database + EditData.table +
                         " Set user_block = null " +
                         "    ,dat_block = null " +
                         " Where user_block = " + EditData.local_user +
                         "   and exists ( Select " + EditData.primary +
                         " From t_saver_s  a " +
                         " Where a." + EditData.primary + " = " + EditData.database +
                         EditData.table + "." + EditData.primary + ")";

            if (!ExecSQL(sel, true))
            {
                throw new Exception("Ошибка разблокирования данных");
            }

        }


        /// <summary>
        /// сохранение изменных данных в БД
        /// </summary>
        private void SaveData()
        {
            //.......................................................
            //начинаем изменять в основной базе
            //для начала надо выяснить сколько записей в t_interdata, где kod_inter > 0 - потянет ли транзакция
            //.......................................................
            long icount = GetCountRecords();
            if (icount == -1)
            {
                throw new Exception("Ошибка проверки кол-ва интервалов");
            }
            if (icount < 500)
            {
                //изменение сразу скопом
                ChangeInterData(" 1 = 1 ", true);
                return;
            }

            //сохранение порциями
            //надо делить  траназации по 500 записей


            long iMin = -1;
            long iMax = -1;
#if PG
            string rowid = EditData.primary;
#else
            string rowid = "rowid";
#endif
            string sql = " Select min(" + rowid + ") as i_min, " +
                               " max(" + rowid + ") as i_max " +
                               " From  t_interdata " +
                               " Where kod_inter > 0 ";
            DataTable db = DBManager.ExecSQLToTable(Connection, "select count(*) from t_interdata");
            DataTable db1 = DBManager.ExecSQLToTable(Connection, "select count(*) from t_saver_s");
            MyDataReader reader;

            var ret = ExecRead(out reader, sql, true);
            if (!ret)
            {
                throw new Exception("Ошибка выборки интервалов для изменения");
            }
            try
            {
                if (!reader.Read())
                {
                    throw new Exception("Ошибка выборки интервалов для изменения");
                }


                if (reader["i_min"] != DBNull.Value) Int64.TryParse(reader["i_min"].ToString(), out iMin);
                if (reader["i_max"] != DBNull.Value) Int64.TryParse(reader["i_max"].ToString(), out iMax);
            }
            finally
            {
                reader.Close();
            }
            if (iMin < 1 || iMax < 1)
            {
                throw new Exception("Ошибка выборки интервалов для изменения");
            }

            icount = iMin;
            while (icount <= iMax)
            {
                ChangeInterData(" " + rowid + " >= " + icount + " and " + rowid + " < " + (icount + 500), icount == iMin);
                icount += 500;
            }
        }


        /// <summary>
        /// Получение количества записей из таблицы изменений
        /// интервалов
        /// </summary>
        /// <returns></returns>
        private long GetCountRecords()
        {

            object count = ExecScalar(" Select count(*) " +
                                      " From t_interdata " +
                                      " Where kod_inter > 0 ", out Ret, true);
            if (!Ret.result) throw new Exception("Ошибка выборки количества записей GetCountRecords");

            long icount;

            return !Int64.TryParse(count.ToString(), out icount) ? 0 : icount;
        }


        /// <summary>
        /// Работа с интервалами
        /// </summary>
        /// <returns></returns>
        private void WorkWithInterval()
        {
            //проверим, выбраны ли записи, если нет, то вставим новые данные и закончим
            //.....

            //начинаем вычислять новые интервалы, перебираем варианты: 
            //поля dat,dat_po - исходный период
            //   new_s,new_po - новый период
            //   isp_s,isp_po - исправленный исходный период

            //'   and dat_s  - mdy(month(dat_s),  day(dat_s),  year(dat_s) ) in ( "0 23", "0 00", "0 01", "0 02", "0 03", "0 04", "0 05", "0 06" ) '+
            //'   and dat_po - mdy(month(dat_po), day(dat_po), extract(year from dat_s)) in (         "0 00", "0 01", "0 02", "0 03", "0 04", "0 05", "0 06" ) '


            string sel;
            if (EditData.intvType == enIntvType.intv_Hour)
#if PG
                sel = " INTERVAL '1 hours' ";
#else
                sel = " 1 units hour ";
#endif
            //else if (EditData.intvType == enIntvType.intv_Month)
#if PG
            //        sel = " INTERVAL '1 months' ";
#else
            //    sel = " 1 units month ";
#endif
            else
#if PG
                sel = " INTERVAL '1 days' ";
#else
                sel = " 1 units day ";
#endif
            //.......................................................
            //  исх.    <-------->
            //  нов.  <------------>
            // старый интервал удалить 
            //.......................................................
            string filter = " Update t_interdata " +
                            " Set kod_inter = 3 " +
                            " Where kod_inter = 0 and new_s <= dat_s " +
                            " and new_po >= dat_po ";
            if (!ExecSQL(filter, true))
            {
                throw new Exception("Ошибка определения интервалов");
            }

            //.......................................................
            //  исх.  <--------->
            //  нов.     <--------->
            // исправляем левый край
            //.......................................................
            if (EditData.intvType == enIntvType.intv_Hour)
                filter = " Update t_interdata " +
                         " Set kod_inter = 1 " +
                         ", isp_s = dat_s, isp_po = new_s " +
                         " Where kod_inter = 0 and new_s >= dat_s " +
                         "and new_s <= dat_po and new_po >= dat_po ";
            else
                filter = " Update  t_interdata " +
                         " Set kod_inter = 1 " +
                         ", isp_s = dat_s, isp_po = date(new_s) - " + sel +
                         " Where kod_inter = 0 and new_s >= dat_s and new_s <= dat_po and new_po >= dat_po ";

            if (!ExecSQL(filter, true))
            {
                throw new Exception("Ошибка определения интервалов");
            }


            //.......................................................
            //  исх.    <---------->
            //  нов.  <------->
            // исправляем правый край
            //.......................................................
            if (EditData.intvType == enIntvType.intv_Hour)
                filter = " Update  t_interdata " +
                         " Set kod_inter = 2 " +
                         ", isp_po = dat_po, isp_s = new_po" +
                         " Where kod_inter = 0 and new_s <= dat_s and new_po >= dat_s and new_po <= dat_po ";
            else
                filter = " Update  t_interdata " +
                         " Set kod_inter = 2 " +
                         ", isp_po = dat_po, isp_s = new_po + " + sel +
                         " Where kod_inter = 0 and new_s <= dat_s " +
                         " and new_po >= dat_s and new_po <= dat_po ";
            if (!ExecSQL(filter, true))
            {
                throw new Exception("Ошибка определения интервалов");
            }

            //.......................................................
            //  исх.  <------------>
            //  нов.   <--------->
            // надо породить два исправленных интервала
            //.......................................................
            if (EditData.intvType == enIntvType.intv_Hour)
                filter = " Update  t_interdata " +
                         " Set kod_inter = 4 " +
                         ", isp_s = dat_s, isp_po = new_s " +
                         ", isp2_s = new_po, isp2_po = dat_po " +
                         " Where kod_inter = 0 and new_s > dat_s and new_po < dat_po ";
            else
                filter = " Update  t_interdata " +
                         " Set kod_inter = 4 " +
                         ", isp_s = dat_s, isp_po = new_s -  " + sel +
                         ", isp2_po = dat_po, isp2_s = new_po +  " + sel +
                         " Where kod_inter = 0 and new_s > dat_s and new_po < dat_po ";
            if (!ExecSQL(filter, true))
            {
                throw new Exception("Ошибка определения интервалов");
            }


        }


        /// <summary>
        /// Постройка индекса на таблицу
        /// </summary>
        private void BuildIndexInterData()
        {
            string indexNum = RandomText.Generate();

            string sel = "Create index int_" + indexNum + "_" +
                         EditData.local_user + "_2 on t_interdata (" + _keyfields + ")";
            if (!ExecSQL(sel, true))
            {
                throw new Exception("Ошибка создания индексов");
            }

            sel = "Create unique index int_" + indexNum + "_" +
                  EditData.local_user + "_1 on  t_interdata (" + EditData.primary + ")";
            if (!ExecSQL(sel, true))
            {
                throw new Exception("Ошибка создания индексов");
            }

            sel = "Create index int_" + indexNum + "_" +
                  EditData.local_user + "_3 on t_interdata (kod_inter)";
            if (!ExecSQL(sel, true))
            {
                throw new Exception("Ошибка создания индексов");
            }

        }


        /// <summary>
        /// Установка правильных дат 
        /// </summary>
        private void SelectIntervals()
        {
            ExecSQL(" Drop table t_interdata", false);

            string filter = " Select " + EditData.primary + "," + _fields + ", 0 as kod_inter, ";


            PrepareDataParametrs();

            filter += " " + EditData.dat_s + " as new_s, " + EditData.dat_po + " as new_po ";

            if (EditData.intvType == enIntvType.intv_Month)
                //выправим по-месячные даты на первое число
#if PG
                filter += ", public.mdy(" + "dat_s".Month().CastTo("integer") + ",1," + "dat_s".Year().CastTo("integer") + ") as dat_s,  public.mdy(" + "dat_po".Month().CastTo("integer") + ",1," + "dat_po".Year().CastTo("integer") + ") as dat_po  " +
                       ", public.mdy(" + "dat_s".Month().CastTo("integer") + ",1," + "dat_s".Year().CastTo("integer") + ") as isp_s,  public.mdy(" + "dat_po".Month().CastTo("integer") + ",1," + "dat_po".Year().CastTo("integer") + ") as isp_po  " +
                       ", public.mdy(" + "dat_s".Month().CastTo("integer") + ",1," + "dat_s".Year().CastTo("integer") + ") as isp2_s, public.mdy(" + "dat_po".Month().CastTo("integer") + ",1," + "dat_po".Year().CastTo("integer") + ") as isp2_po ";
#else
                filter += ", mdy(month(dat_s),1,year(dat_s)) as dat_s,  mdy(month(dat_po),1,extract(year from dat_s)) as dat_po  " +
                          ", mdy(month(dat_s),1,year(dat_s)) as isp_s,  mdy(month(dat_po),1,extract(year from dat_s)) as isp_po  " +
                          ", mdy(month(dat_s),1,year(dat_s)) as isp2_s, mdy(month(dat_po),1,extract(year from dat_s)) as isp2_po ";
#endif
            else
                filter += ", dat_s,dat_po, dat_s as isp_s, dat_po as isp_po, dat_s as isp2_s, dat_po as isp2_po ";


#if PG
            filter += (" From t_saver_s" +
                   " Where dat_s  <= " + EditData.dat_po +
                   "   and dat_po >= " + EditData.dat_s).AddIntoStatement(" Into temp t_interdata");
#else
            filter += " From t_saver_s" +
                      " Where dat_s  <= " + EditData.dat_po +
                      "   and dat_po >= " + EditData.dat_s +
                      " Into temp t_interdata With no log ";
#endif


            if (!ExecSQL(filter, true))
            {
                throw new Exception("Ошибка выборки интервалов ");
            }

        }


        /// <summary>
        /// Подготовка данных
        /// </summary>
        /// <returns></returns>
        private void PrepareDataParametrs()
        {


            string dateStart = EditData.dat_s;
            string dateEnd = EditData.dat_po;

            DateTime d1;
            if (!DateTime.TryParse(dateStart, out d1))
            {
                throw new Exception("Ошибка при преобразовании строки \"" + dateStart + "\" в дату");
            }
            DateTime d2;
            if (!DateTime.TryParse(dateEnd, out d2))
            {
                throw new Exception("Ошибка при преобразовании строки \"" + dateEnd + "\" в дату");
            }

            if (EditData.intvType == enIntvType.intv_Hour)
            {
                //привести "дд.мм.гггг чч:мм" к формату "гггг-мм-дд ч"
#if PG
                dateStart = Utils.EStrNull(d1.ToString("yyyy-MM-dd H:mm:ss")) + "::timestamp";
                dateEnd = Utils.EStrNull(d2.ToString("yyyy-MM-dd H:mm:ss")) + "::timestamp";
#else
                dateStart = "cast (" + Utils.EStrNull(d1.ToString("yyyy-MM-dd H")) + " as datetime year to hour) ";
                dateEnd = "cast (" + Utils.EStrNull(d2.ToString("yyyy-MM-dd H")) + " as datetime year to hour) ";
#endif
            }
            else if (EditData.intvType == enIntvType.intv_Month)
            {
                //выправим по-месячные даты на первое число
                dateStart = "Date('01." + d1.Month.ToString("00") + "." + d1.Year + "')";
                // выровнять дату окончания на конец месяца
                d2 = (new DateTime(d2.Year, d2.Month, 1)).AddMonths(1).AddDays(-1);
                dateEnd = "Date('" + d2.Day.ToString("00") + "." + d2.Month.ToString("00") + "." + d2.Year + "')";
            }
            else //Подневной интервал
            {
                dateStart = "Date('" + d1.Day.ToString("00") + "." + d1.Month.ToString("00") + "." + d1.Year + "')";
                dateEnd = "Date('" + d2.Day.ToString("00") + "." + d2.Month.ToString("00") + "." + d2.Year + "')";
            }

            EditData.dat_s = dateStart;
            EditData.dat_po = dateEnd;



        }


        /// <summary>
        /// Проверка заблокировали ли записи
        /// </summary>
        /// <returns></returns>
        private void CheckIfAllRecordsLocked()
        {
            //            string sel;
            ////проверить все ли записи мною заблокированы (т.е. есть ли другие блокировки)
            //#if PG
            //            sel = " Select CTID From " + editData.database + editData.table +
            //                  " Where is_actual <> 100 and exists ( Select oid From " + t_selected + " a Where 1 = 1 " + sql + ")" +
            //                  "   and coalesce(user_block,0) <> " + editData.local_user +
            //                  "   and now() - coalesce(dat_block,public.mdy(1,1,2001)) < " + string.Format(" INTERVAL '{0} minutes' ", Constants.users_min);
            //#else
            //            sel = " Select rowid From " + _editData.database + _editData.table +
            //                  " Where is_actual <> 100 and exists ( Select rowid From t_saver_s a " +
            //                  " Where 1 = 1 " + filter + ")" +
            //                  "   and nvl(user_block,0) <> " + _editData.local_user +
            //                  "   and current - nvl(dat_block,mdy(1,1,2001)) < " + Constants.users_min + " units minute ";
            //#endif
            //            MyDataReader reader;
            //            _ret = DBManager.ExecRead(_connection, out reader, sel, true);
            //            if (!_ret.result)
            //            {
            //                trans.Rollback();
            //                _ret.text = "Ошибка проверки блокировки записей";
            //                return true;
            //            }
            //            if (reader.Read())
            //            {
            //                reader.Close();

            //                //кто-то уже успел как-то блокирнуть записи, откатываем блокировку через Rollback
            //                trans.Rollback();
            //                _ret.result = false;
            //                _ret.text = "Данные блокированы для изменения другим пользователем";

            //                ExecSQL(_connection, " Drop table t_saver_s ", false);
            //                return true;
            //            }
            //            trans.Commit();

        }


        /// <summary>
        /// Блокировка данных перед изменением
        /// </summary>
        /// <returns></returns>
        private void BlockRecords()
        {
            //блокировка данных
            string[] mIxs = _keyfields.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);

            string filter = mIxs.Aggregate(" ", (current, field) =>
                current + (" and a." + field + " = " + EditData.database + EditData.table + "." + field));

            string sel = " Update " + EditData.database + EditData.table + " " +
                         " Set user_block = " + EditData.local_user + "," +
                         "     dat_block = " + DBManager.sCurDateTime +
                         " Where is_actual <> 100 and exists ( Select 1 From t_saver_s a " +
                         " Where 1 = 1 " + filter + ")";
            if (!ExecSQL(sel, true))
            {
                throw new Exception("Ошибка блокирования данных");

            }

        }


        /// <summary>
        /// Проверка на блокирование данных другим пользователем
        /// </summary>
        /// <returns></returns>
        private void CheckUserBlockRecords()
        {
            //проверить блокировки записей

            Ret = Utils.InitReturns();
            string sel =
#if PG
 " Select * From t_saver_s " +
                 " Where coalesce(user_block,0) <> " + EditData.local_user +
                 "   and now() - coalesce(dat_block,public.mdy(1,1,2001)) < " +
                string.Format(" INTERVAL '{0} minutes' ", Constants.users_min);
#else
                " Select * From t_saver_s " +
                " Where nvl(user_block,0) <> " + EditData.local_user +
                "   and current - nvl(dat_block,mdy(1,1,2001)) < " +
                Constants.users_min + " units minute ";
#endif

            MyDataReader reader;

            bool ret = ExecRead(out reader, sel, true);
            if (!ret)
            {
                throw new Exception("Ошибка проверки блокировки записей");
            }
            try
            {
                ExecSQL("drop table t" + EditData.nzp_user + "_user_block_values; ", false);
                sel = " select * into temp t" + EditData.nzp_user + "_user_block_values From t_saver_s  " +
#if PG
 " Where coalesce(user_block,0) <> " + EditData.local_user +
                                      "   and now() - coalesce(dat_block,public.mdy(1,1,2001)) < " +
                                      string.Format(" INTERVAL '{0} minutes' ", Constants.users_min);
#else
                    " Where nvl(user_block,0) <> " + EditData.local_user +                
                    "   and current - nvl(dat_block,mdy(1,1,2001)) < " +
                                   Constants.users_min + " units minute ";
#endif
                if (!ExecSQL(sel, true))
                {
                    throw new Exception("Ошибка проверки блокировки записей");
                }
                if (reader.Read())
                {
                    //Если данные заблокированы, то исключаем их из рассмотрения
                    sel = " delete From t_saver_s " +


#if PG
 " Where coalesce(user_block,0) <> " + EditData.local_user +
                                       "   and now() - coalesce(dat_block,public.mdy(1,1,2001)) < " +
                                       string.Format(" INTERVAL '{0} minutes' ", Constants.users_min);
#else
                    " Where nvl(user_block,0) <> " + EditData.local_user +                
                    "   and current - nvl(dat_block,mdy(1,1,2001)) < " +
                                   Constants.users_min + " units minute ";
#endif
                    if (!ExecSQL(sel, true))
                    {
                        throw new Exception("Ошибка проверки блокировки записей");
                    }

                    Ret.text = "Часть записей заблокировано другим пользователем, они будут исключены из операции";
                    Ret.tag = -1;
                }
            }
            finally
            {
                reader.Close();
            }
        }


        /// <summary>
        /// Подготавливает поля для таблицы
        /// </summary>
        /// <returns></returns>
        private void MakeSelectFields()
        {
            _keyfields = EditData.keys.Aggregate("", (current, kvp) => current + (", " + kvp.Key)).TrimStart(',');
            _valfields = EditData.vals.Aggregate("", (current, kvp) => current + (", " + kvp.Key)).TrimStart(',');
            _fields = _keyfields + "," + _valfields;

            //условие выборки данных из целевой таблицы
            foreach (string s in EditData.dopFind)
            {
                if (!String.IsNullOrEmpty(s)) _filter += s;
            }
        }


        /// <summary>
        /// Построение индексов
        /// </summary>
        /// <returns></returns>
        private void BuildIndexSaver()
        {
            string indexNum = RandomText.Generate();

            CreateOneIndex("Create index sel_" + indexNum + "_" + EditData.local_user + "_2 on " +
                           "t_saver_s(" + _keyfields + ")");

            CreateOneIndex("Create unique index sel_" + indexNum + "_" + EditData.local_user + "_1 on " +
                           "t_saver_s(" + EditData.primary + ")");

            if (!ExecSQL(DBManager.sUpdStat + " t_saver_s", true))
            {
                throw new Exception("Обновление статистики");
            }

        }


        /// <summary>
        /// Выборка данных перед изменением
        /// </summary>
        /// <returns></returns>
        private void SelectStoredData()
        {

            ExecSQL(" Drop table t_saver_s", false);
            //выбрать все записи с полями по целевым ключам (dat_s, dat_po считаем, что везде одинаково называются)

#if PG
            string sel =
                " Select " + EditData.primary + "," + _keyfields + "," + _valfields + " ,dat_s, dat_po " +
                         " , coalesce(user_block,0) as user_block, coalesce(dat_block,public.mdy(1,1,2001)) as dat_block  " +
                " Into temp t_saver_s " +
                " From " + EditData.database + EditData.table +
                " Where is_actual <> 100 " + _filter;
#else
            string sel =
                " Select " + EditData.primary + "," + _keyfields +","+ _valfields + " ,dat_s, dat_po " +
                " ,nvl(user_block,0) as user_block, nvl(dat_block,mdy(1,1,2001)) as dat_block  " +
                " From " + EditData.database + EditData.table +
                " Where is_actual <> 100 " + _filter +
                " Into temp t_saver_s With no log";
#endif

            if (!ExecSQL(sel, true))
            {
                throw new Exception("Ошибка выборки данных, возможно данные заблокированы, обратитесь позднее ");

            }


        }



        private void ChangeInterData(string sw, bool createTempIndex = false)
        //----------------------------------------------------------------------
        {
            //начинаем изменять основную базу

            //finder.YM.year_  == Points.CalcMonth.year_ &&
            //finder.YM.month_ == Points.CalcMonth.month_
            //.......................................................
            //сначало сохраним в архиве предыдущие интервалы
            //пока сделаем через is_actual, затем надо переделать и бросать в таблицу (_arx)!
            //.......................................................
#if PG
            string def = String.Empty;
            string primaryInsert = String.Empty;

#else
            string def = "0, ";
            string primaryInsert = EditData.primary + ",";
#endif
            string sql =

                " Update " + EditData.database + EditData.table +
                " Set is_actual = 100, dat_del = " + DBManager.sCurDate + ", " +
                "                  user_del = " + EditData.local_user + ","+
                //month_calc необходим для добавления признаков перерасчета
                "  month_calc = "+ DBManager.sDefaultSchema + "mdy(" + EditData.month + ",1," + EditData.year + ")" + 
                " Where exists ( Select " + EditData.primary +
                " From t_interdata  a " +
                " Where " + sw +
                "   and a.kod_inter > 0 and a." + EditData.primary + " = " +
                EditData.database + EditData.table + "." + EditData.primary + ")";


            if (!ExecSQL(sql, true))
            {
                throw new Exception("Ошибка сохранения интервалов в архиве");

            }

            string valFields = "";
            string valVals = "";


#if PG
            valFields += "," + string.Join(",", EditData.vals.Keys.ToArray());
            valVals += "," + string.Join(
                ",",
                EditData.vals.Values.Select(
                    x =>
                    {
                        var value = Utils.ENull(x);

                        decimal tmp;
                        return decimal.TryParse(value, out tmp) ? tmp.ToString() : string.Format("'{0}'", value);
                    }).ToArray());
#else
            foreach (KeyValuePair<string, string> kvp in EditData.vals)
            {
                valFields += "," + kvp.Key;
                valVals += ",'" + Utils.ENull(kvp.Value) + "'";
            }
#endif
            //.......................................................
            //потом сохраним исправленные сосоедние интервалы
            //.......................................................
            sql = " Insert into " + EditData.database + EditData.table +
                  "( " + primaryInsert + _keyfields + valFields +
                  ", dat_s,dat_po,is_actual,dat_when,nzp_user ) " +
                  " Select distinct " + def + " " + _keyfields + valFields + ", isp_s,isp_po,1," + DBManager.sCurDate + "," +
                  EditData.local_user +
                  " From t_interdata " +
                  " Where " + sw + " and kod_inter > 0 and kod_inter <> 3";

            if (!ExecSQL(sql, true))
            {
                throw new Exception("Ошибка исправления соседних интервалов");
            }
            //.......................................................
            //также внесем второй исправленный интервал для 4-го варианта
            //.......................................................
            sql = " Insert into " + EditData.database + EditData.table +
                  "( " + primaryInsert + _keyfields + valFields +
                  ", dat_s,dat_po,is_actual,dat_when,nzp_user ) " +
                  " Select " + def + " " + _keyfields + valFields + ", isp2_s,isp2_po,1," + DBManager.sCurDate + "," +
                  EditData.local_user +
                  " From  t_interdata " +
                  " Where " + sw + " and kod_inter = 4 ";
            if (!ExecSQL(sql, true))
            {
                throw new Exception("Ошибка исправления соседних интервалов");
            }
            //.......................................................
            //и наконец введем новый интервал, в самом конце один раз
            //.......................................................
            string isActual = "1 ";
            if (EditData.todelete)
            {
                //значит выполняется удаление интервала
                isActual = "100 ";
            }
            //выставили удаленный интервал и дальше ничего не делаем
            if (EditData.todelete)
            {
                return;
            }
            sql = " Insert into " + EditData.database + EditData.table +
                  "( " + primaryInsert + _keyfields + valFields +
                  ", dat_s,dat_po,dat_when,nzp_user, is_actual, month_calc ) " +
                  " Select distinct " + def + " " + _keyfields + valVals + ", new_s,new_po," + DBManager.sCurDate + "," + EditData.local_user + "," +
                  isActual +
                  ", " + DBManager.sDefaultSchema + "mdy(" + EditData.month + ",1," + EditData.year + ")" +
                  " From  t_interdata Where " + sw + "";

            if (!ExecSQL(sql, true))
            {
                throw new Exception("Ошибка исправления соседних интервалов");
            }

            //вставка записей, где ключевых запсией нет в t_interdata
            //выберем такие записи

            string keyVals = "";
            string fromTable = "";
            //0|   1    |  2 |     3
            //keys.Add("nzp_kvar", "1|nzp_kvar|kvar|nzp_kvar=303080"); //ссылка на ключевую таблицу
            //keys.Add("nzp_serv", "2|5");
            bool firstEnter = true;
            foreach (KeyValuePair<string, string> kvp in EditData.keys)
            {
                //сопоставить первичный ключ с базовой таблицой (nzp.prm_1 -> nzp_kvar.kvar)
                string[] mIns = kvp.Value.Split(new[] { "|" }, StringSplitOptions.RemoveEmptyEntries);
                keyVals += "," + mIns[1];

                if (mIns[0] == "1")
                {
                    string table = mIns[2].Trim().ToLower();
                    if (table.IndexOf(DBManager.tableDelimiter, StringComparison.Ordinal) < 0)
                    {
                        if (table == "services" || table == "supplier" || table == "s_payer")
                            table =
                                DBManager.GetFullBaseName(Connection, Points.Pref + "_kernel", table);
                        else table = EditData.database + table;
                    }

                    if (createTempIndex && firstEnter)
                    {
                        firstEnter = false;
                        try
                        {

                            CreateOneIndex("Create index sel_tmp_" + EditData.local_user + "_3 on " +
                                           "t_saver_s(" + kvp.Key + ", dat_s, dat_po)");
                            ExecSQL(DBManager.sUpdStat + " t_saver_s", false);
                        }
                        catch
                        {
                        }
                    }
                    fromTable =
                        " From " + table + " k " +
                        " Where " + mIns[3] +
                        " and 1 > ( Select count(*) From t_saver_s i " +
                        " Where i." + kvp.Key + " = k." + mIns[1] +
                        "   and i.dat_s  <= " + EditData.dat_po +
                        "   and i.dat_po >= " + EditData.dat_s + ")";

                    fromTable += " and not exists (select 1 from t" + EditData.nzp_user + "_user_block_values t where  " +
                    "  t.dat_s  <= " + EditData.dat_po +
                    "   and t.dat_po >= " + EditData.dat_s + " and k." + mIns[1] + " = " + kvp.Key + ")";
                }
                if (mIns[0] == "5")
                {
                    //обработка prm_5
                    fromTable = " From " + DBManager.GetFullBaseName(Connection, Points.Pref + "_data", "dual") +
                                " Where 1 > ( Select count(*) From t_saver_s i " +
                                " Where i.dat_s  <= " + EditData.dat_po +
                                "   and i.dat_po >= " + EditData.dat_s + ")";
                }
            }
            sql = " Insert into " + EditData.database + EditData.table +
                  "( " + primaryInsert + _keyfields + valFields +
                  ", dat_s,dat_po,is_actual,dat_when,nzp_user, month_calc ) " +
                  " Select distinct " + def + "  " + keyVals.TrimStart(',') + valVals + ", " + EditData.dat_s + "," + EditData.dat_po +
                  ",1," + DBManager.sCurDate + "," + EditData.local_user +
                  ", MDY(" + EditData.month + ",1," + EditData.year + ")" +
                  fromTable;
            if (!createTempIndex) return;
            var ret = ExecSQL(sql, true);
            if (!ret)
            {
                throw new Exception("Ошибка вставки данных");
            }
        }

        public bool CreateBlockUserEntitiesReport(out string mes)
        {
            Returns returns;
            mes = "";
            if (DBManager.CastValue<int>(ExecScalar(string.Format("select count(*) from {0}", "t" + EditData.nzp_user + "_user_block_values"), out returns, true)) == 0)
            {
                return true;
            }
            string fileName = Constants.Directories.ReportDir + "//Протокол_заблокированных_данных_(режим_групповые_операции)" +
                                       DateTime.Now.ToShortDateString() + "_" + DateTime.Now.Ticks;
            //постановка на поток
            ExcelRepClient excelRep = new ExcelRepClient();
            var ret = excelRep.AddMyFile(new ExcelUtility()
             {
                 nzp_user = EditData.nzp_user,
                 status = ExcelUtility.Statuses.InProcess,
                 rep_name = "Протокол заблокированных данных пользователями (режим групповые операции) от " + DateTime.Now.ToShortDateString()
             });
            #region Получение данных

            MyDataReader reader = null;
            var valFields = "" + string.Join("|", EditData.keys.Values.ToArray());
            var valVals = "" + string.Join(
                "|",
                EditData.vals.Values.Select(
                    x =>
                    {
                        var value = Utils.ENull(x);

                        decimal tmp;
                        return decimal.TryParse(value, out tmp) ? tmp.ToString() : string.Format("'{0}'", value);
                    }).ToArray());
            var r = ExecRead(out reader, string.Format("select t.*,k.fio,k.num_ls,coalesce(u.name,'')||' '||coalesce(u.comment,'') as name from {0} t,{1}kvar k,{1}users u where t.{2} = k.nzp_kvar and t.user_block = u.nzp_user ", "t" +
                EditData.nzp_user + "_user_block_values", Points.Pref + "_data.", (EditData.primary.Trim() == "nzp_key" ? "nzp" : "nzp_kvar")), true);
            if (!r)
            {
                mes = "";
                return true;
            }
            try
            {
                using (var file = File.Create(fileName + ".txt"))
                {
                    using (var sw = new StreamWriter(file))
                    {
                        sw.WriteLine("Протокол заблокированных данных пользователями (режим групповые операции)");
                        sw.WriteLine("Дата формирования протокола: {0}",
                            DateTime.Now.ToString("F", CultureInfo.CurrentCulture));
                        sw.WriteLine("Заблокированы следующие записи");
                        while (reader.Read())
                        {
                            sw.WriteLine(
                                "Заблокирована запись: Таблица - {2}, Номер лс - {1} , ФИО - {0},Заблокировавший пользователь - {6}, Период - {3}, keys - {4}, vals: - {5} ",
                                reader["fio"], reader["num_ls"], EditData.table,
                                EditData.dat_s.Replace("Date('", "").Replace("')", "") + " - " + EditData.dat_po.Replace("Date('", "").Replace("')", ""), valFields, valVals, reader["name"]);
                            mes += "Параметр заблокирован пользователем " +
                                   DBManager.CastValue<string>(reader["name"]).Trim() + " " + DBManager.CastValue<DateTime>(reader["dat_block"]).ToString("dd.MM.yyyy HH:mm") + ".Редактировать данные запрещено.\r\n";
                        }
                        sw.Flush();
                    }
                }
            }
            catch (Exception Ex)
            {
                throw new Exception("Ошибка: " + Ex.Message);
            }
            finally
            {
                reader.Close();
            }
            #endregion

            if (!ret.result)
            {
                mes = "";
                return true;
            }
            var nzp_exc = ret.tag;

            excelRep.SetMyFileState(new ExcelUtility()
            {
                nzp_exc = nzp_exc,
                status = ExcelUtility.Statuses.Success,
                exc_path = fileName,
                nzp_user = EditData.nzp_user,

                rep_name = "Протокол заблокированных данных пользователями (режим групповые операции) от " + DateTime.Now.ToShortDateString()
            });

            return false;
        }
    }
}
