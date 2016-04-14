//--------------------------------------------------------------------------------80
//Файл: db.cs
//Дата создания: 14.05.2009
//Дата изменения: 15.05.2009
//Назначение: Системный класс работы с БД 
//Автор: Зыкин А.А.
//Copyright (c) Научно-технический центр "Лайн", 2009. 
//--------------------------------------------------------------------------------80
using System;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Global;
using System.Data;


namespace STCLINE.KP50.DataBase
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="In"></typeparam>
    /// <typeparam name="Out"></typeparam>
    /// <param name="request"></param>
    /// <param name="connectionID"></param>
    /// <returns></returns>
    public delegate Out ActionSqlDelegate<In, Out>(In finder, IDbConnection connectionID)
        where In : Finder
        where Out : ReturnsType;

    /// <summary>
    /// абстрактный класс "Действие с БД"
    /// </summary>
    public abstract class ClassAction
    {

        //-------------------------------------------------------------------
        public ClassAction(Finder _finder)
        {
            try
            {
                this.afinder = _finder;
            }
            catch { this.afinder = new Finder(); }


        }

        public ClassAction() {
        }

        public Finder afinder;

    }
    public abstract class ClassSqlAction : ClassAction
    {
        public ClassSqlAction(Finder finder) : base(finder) { }

        public ClassSqlAction() : base() { }

        public abstract ReturnsType DoAction(IDbConnection connectionID);
    }

    /// <summary>
    /// основной Класс "Работа с БД": соединение с БД, выполнение действия с БД
    /// </summary>
    public class ClassDB
    {
        //static public string dataBaseConnection;

        //--------------------------------------

        private ClassAction _action = null;
        private ClassSqlAction _sqlAction = null;
        private string _connectionString = "";
        public string dataBaseConnection;


        public ReturnsType RunAction()
        {
            try
            {
                if (this._sqlAction != null)
                {
                    this._action = this._sqlAction;
                    using (IDbConnection connectionID = DBManager.newDbConnection(this._connectionString))
                    {
                        //-----------------------------------------------------------
                        // Открыть соединение с БД
                        //-----------------------------------------------------------
                        connectionID.Open();

                        try
                        {

                            Utils.setCulture();
                            //-----------------------------------------------------------
                            // Вызвать переопределенный метод, возвращающий результат
                            //-----------------------------------------------------------
                            return this._sqlAction.DoAction(connectionID);
                        }
                        finally
                        {
                            connectionID.Close();
                        }
                    }


                }
                else
                {
                    throw new Exception("Не определен метод контекста данных");
                }


            }
            catch (Utility.UserException exu)
            {
                //-----------------------------------------------------------
                // обработка пользовательского исключения-проверки - показать на клиенте
                //-----------------------------------------------------------
                MonitorLog.WriteLog(exu.Message, MonitorLog.typelog.Error, 1, 2, true);
                return new ReturnsType(false, exu.Message, -1); ;
            }
            catch (Exception e)
            {
                //-----------------------------------------------------------
                // обработка любой системной ошибки - не показывать на клиенте 
                //-----------------------------------------------------------
                MonitorLog.WriteLog(e.Message, MonitorLog.typelog.Error, 1, 2, true);
                return new ReturnsType(false, e.Message, 0); ;
            }
            finally
            {
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sqlAction"></param>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public ReturnsType RunSqlAction(ClassSqlAction sqlAction, string connectionString)
        {
            try
            {
                this._sqlAction = sqlAction;
                this._connectionString = connectionString;

                return this.RunAction();
            }
            finally
            {
                this._sqlAction = null;
                this._connectionString = "";
           }

        }

        public ReturnsType RunSqlAction(ClassSqlAction sqlAction)
        {
            try
            {
                this._sqlAction = sqlAction;
                return this.RunAction();
            }
            finally
            {
                this._sqlAction = null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="In"></typeparam>
        /// <typeparam name="Out"></typeparam>
        /// <param name="request"></param>
        /// <param name="sqlAction"></param>
        /// <returns></returns>
        public Out RunSqlAction<In, Out>(In finder, ActionSqlDelegate<In, Out> sqlAction)
            where In : Finder
            where Out : ReturnsType, new()
        {
            ClassSqlAction<In, Out> action = new ClassSqlAction<In, Out>(finder, sqlAction);
            ReturnsType r = RunSqlAction(action, Points.GetConnByPref(finder.pref));
            //r.ThrowExceptionIfError();
            if (!r.result) 
                return new Out() { result = r.result, text = r.text, tag = r.tag, sql_error = r.sql_error };
            else
                return action.response;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="In"></typeparam>
        /// <typeparam name="Out"></typeparam>
        /// <param name="request"></param>
        /// <param name="sqlAction"></param>
        /// <returns></returns>
        public Out RunSqlAction<In, Out>(ActionSqlDelegate<In, Out> sqlAction)
            where In : Finder
            where Out : ReturnsType, new()
        {
            ClassSqlAction<In, Out> action = new ClassSqlAction<In, Out>(sqlAction);
            ReturnsType r = RunSqlAction(action, Points.Pref);
            //r.ThrowExceptionIfError();
            if (!r.result)
                return new Out() { result = r.result, text = r.text, tag = r.tag, sql_error = r.sql_error };
            else
                return action.response;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="In"></typeparam>
        /// <typeparam name="Out"></typeparam>
        /// <param name="request"></param>
        /// <param name="sqlAction"></param>
        /// <returns></returns>
        public Out RunSqlActionConnect<In, Out>(ActionSqlDelegate<In, Out> sqlAction)
            where In : Finder
            where Out : ReturnsType, new()
        {
            ClassSqlAction<In, Out> action = new ClassSqlAction<In, Out>(sqlAction);
            ReturnsType r = RunSqlAction(action, this.dataBaseConnection);
            //r.ThrowExceptionIfError();
            if (!r.result)
                return new Out() { result = r.result, text = r.text, tag = r.tag, sql_error = r.sql_error };
            else
                return action.response;
        }


        public Out RunSqlAction<In, Out>(In request, ClassDB_Ext.ActionSqlDelegate_Ext<In, Out> sqlAction)
            where In : IntfRequestType
            where Out : IntfResultType, new()
        {
            ClassDB_Ext.ClassSqlAction_Ext<In, Out> action = new ClassDB_Ext.ClassSqlAction_Ext<In, Out>(request, sqlAction);

            string conn = Points.GetConnByPref(Points.Pref);
            //ReturnsType r = new ClassDB().RunSqlAction(action, conn);
            ReturnsType r = this.RunSqlAction(action, conn);
            r.ThrowExceptionIfError();

            return action.response;
        }

    }
    //===========================================================================================
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="In"></typeparam>
    /// <typeparam name="Out"></typeparam>
    internal class ClassSqlAction<In, Out> : ClassSqlAction
        where In : Finder
        where Out : ReturnsType
    {
        public ClassSqlAction(In finder, ActionSqlDelegate<In, Out> sqlAction)
            : base(finder)
        {
            this.finder = finder;
            this.sqlAction = sqlAction;
        }

        public ClassSqlAction(ActionSqlDelegate<In, Out> sqlAction)
        {
            this.sqlAction = sqlAction;
        }

        public ActionSqlDelegate<In, Out> sqlAction = null;
        private In finder;
        public Out response = null;

        public override ReturnsType DoAction(IDbConnection connectionID)
        {
            response = sqlAction(finder, connectionID);
            return new ReturnsType();
        }
    }


    public static class ClassDB_Ext// : ClassDB
    {
        internal static Out RunSqlAction<In, Out>(In request, ActionSqlDelegate_Ext<In, Out> sqlAction)
            where In : IntfRequestType
            where Out : IntfResultType, new()
        {
            ClassSqlAction_Ext<In, Out> action = new ClassSqlAction_Ext<In, Out>(request, sqlAction);
            
            string conn = Points.GetConnByPref(Points.Pref);
            ReturnsType r = new ClassDB().RunSqlAction(action, conn);
            r.ThrowExceptionIfError();

            return action.response;
        }


        public delegate Out ActionSqlDelegate_Ext<In, Out>(In request, IDbConnection connectionID)
            where In : IntfRequestType
            where Out : IntfResultType;


        internal class ClassSqlAction_Ext<In, Out> : STCLINE.KP50.DataBase.ClassSqlAction
            where In : IntfRequestType
            where Out : IntfResultType
        {
            public ClassSqlAction_Ext(In request, ActionSqlDelegate_Ext<In, Out> sqlAction)
                : base()
            {
                this.request = request;
                this.sqlAction = sqlAction;
            }

            public ActionSqlDelegate_Ext<In, Out> sqlAction = null;
            private In request;
            public Out response = null;

            public override ReturnsType DoAction(IDbConnection connectionID)
            {
                response = sqlAction(request, connectionID);
                response.GetReturnsType().ThrowExceptionIfError();
                return new ReturnsType();
            }
        }
    }

}
