using System;
using System.Data;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;


namespace STCLINE.KP50.DataBase
{


    /// <summary>
    /// Базовый класс для классов сохранения
    /// интервальных данных и перерасчетов 
    /// </summary>
    public class DbIntervalBase 
    {
        protected IDbConnection Connection;
        protected EditInterData EditData;
        protected IDbTransaction Trans;

        protected Returns Ret = Utils.InitReturns();

        public DbIntervalBase(IDbConnection connection, EditInterData editData)
        {
            Connection = connection;
            EditData = editData;
            Trans = null;
        }

        public DbIntervalBase(IDbConnection connection, EditInterData editData, IDbTransaction trans)
        {
            Connection = connection;
            EditData = editData;
            Trans = trans;
        }

        /// <summary>
        /// Пустой конструктор по умолчанию вызывать пока нельзя
        /// </summary>
        protected DbIntervalBase()
        {

        }

        /// <summary>
        /// Выполнение запроса к БД
        /// </summary>
        /// <param name="sql">Текст запроса</param>
        /// <param name="inlog">Писать ли в лог</param>
        /// <returns></returns>
        protected bool ExecSQL(string sql, bool inlog)
        {
            Ret = DBManager.ExecSQL(Connection, Trans, sql, inlog);
            return !inlog || Ret.result;
        }

        /// <summary>
        /// Выполнение запроса к БД с возвратом курсора
        /// </summary>
        /// <param name="reader">курсор</param>
        /// <param name="sql">Запрос</param>
        /// <param name="inlog">Писать ли в лог</param>
        /// <returns></returns>
        protected bool ExecRead(out MyDataReader reader, string sql, bool inlog)
        {
            Ret = DBManager.ExecRead(Connection, Trans, out reader, sql, inlog);
            return Ret.result;
        }

        /// <summary>
        /// Выполняет запрос к БД с возвратом одного значения
        /// </summary>
        /// <param name="sql">Запрос к БД</param>
        /// <param name="ret">Резальтат операции</param>
        /// <param name="inlog">Писать ли в лог</param>
        /// <returns>Значение</returns>
        protected object ExecScalar(string sql, out Returns ret, bool inlog)
        {
            return DBManager.ExecScalar(Connection, Trans, sql, out ret, inlog);
        }

        /// <summary>
        /// Создание индекса
        /// </summary>
        /// <param name="sel">Текст запроса на создание индекса</param>
        protected void CreateOneIndex(string sel)
        {
            if (!ExecSQL(sel, true)) throw new Exception("Ошибка создания индексов");
        }
    }



    
}
