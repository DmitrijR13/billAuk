//--------------------------------------------------------------------------------80
//Файл: _IntfResult.Model.cs
//Дата создания: 25.09.2012
//Дата изменения: 25.09.2012
//Назначение: Описание сериализуемых классов для возврата кода и сообщения о результатах выполнения
//Автор: Зыкин А.А.
//Copyright (c) Научно-технический центр "Лайн", 2012. 
//--------------------------------------------------------------------------------80
using System;
using System.Data;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace STCLINE.KP50.Interfaces
{
    /// <summary>
    /// 
    /// </summary>
    public class ReturnsExceptionType : Exception
    {
        public ReturnsExceptionType(ReturnsType returns)
            : base(returns.text)
        {
            _returns = returns;
        }
        private ReturnsType _returns;
        public ReturnsType Returns
        {
            get { return _returns; }
        }
    }


    public class ReturnsType
    {
        public ReturnsType() : this(true, "", 0, "") { }
        public ReturnsType(bool result) : this(result, "", 0, "") { }
        public ReturnsType(string text) : this(false, text, -1, "") { }
        public ReturnsType(bool result, string text) : this(result, text, 0, "") { }
        public ReturnsType(bool result, string text, int tag) : this(result, text, tag, "") { }
        public ReturnsType(bool result, string text, int tag, string sql_error)
        {
            this.result = result;
            this.text = text;
            this.tag = tag;
            this.sql_error = sql_error;
        }

        public bool result;
        public string text;
        public int tag;
        public string sql_error;

        public void ThrowExceptionIfError()
        {
            if (!result || tag == -1)
            {
                throw new ReturnsExceptionType(this);
            }
        }

        /*public STCLINE.KP50.Global.Returns GetReturns()
        {
            //ThrowExceptionIfError();

            STCLINE.KP50.Global.Returns ret;

            ret.result = this.result;
            ret.text = this.text;
            ret.tag = this.tag;
            ret.sql_error = this.sql_error;

            return ret;

        }*/


    }

    public class ReturnsObjectType<T> : ReturnsType where T : class, new()
    {
        public ReturnsObjectType() : this(null, true, "Выполнено", 0) { }
        public ReturnsObjectType(T returnsData) : this(returnsData, true, "Выполнено", 0) { }
        public ReturnsObjectType(string text) : this(null, false, text, -1) { }
        public ReturnsObjectType(bool result, string text) : this(null, result, text, 0) { }
        public ReturnsObjectType(T returnsData, bool result, string text) : this(returnsData, result, text, 0) { }
        public ReturnsObjectType(T returnsData, bool result, string text, int tag) :
            base(result, text, tag)
        {
            this.returnsData = returnsData;
        }
        public T returnsData;

        public T GetData()
        {
            ThrowExceptionIfError();
            return returnsData;
        }

    }



    /// <summary>
    /// Сериализуемый класс исключений для возврата результата
    /// </summary>
    [Serializable]
    public class IntfResultExceptionType : Exception
    {
        public IntfResultExceptionType(IntfResultType intfResult)
            : base(intfResult.resultMessage)
        {
            _intfResult = intfResult;
        }
        private IntfResultType _intfResult;
        public IntfResultType IntfResult
        {
            get { return _intfResult; }
        }
    }

    /// <summary>
    /// Сериализуемый класс для возврата результата: Только сообщение
    /// </summary>
    [Serializable]
    public class IntfResultType
    {
        public IntfResultType() : this(0, "Выполнено", 0, "") { }
        public IntfResultType(decimal resultCode) : this(resultCode, "", 0, "") { }
        public IntfResultType(decimal resultCode, string resultMessage) : this(resultCode, resultMessage, 0, "") { }
        public IntfResultType(decimal resultCode, string resultMessage, decimal resultID) : this(resultCode, resultMessage, resultID, "") { }
        public IntfResultType(decimal resultCode, string resultMessage, string resultString) : this(resultCode, resultMessage, 0, resultString) { }
        public IntfResultType(decimal resultCode, string resultMessage, decimal resultID, string resultString) : this(resultCode, resultMessage, resultID, resultString, 0) { }
        public IntfResultType(decimal resultCode, string resultMessage, decimal resultID, string resultString, int resultAffectedRows)
        {
            this.resultID = resultID;
            this.resultString = resultString;
            this.resultCode = resultCode;
            this.resultMessage = resultMessage;
            this.resultAffectedRows = resultAffectedRows;
        }

        public decimal resultID;
        public string resultString;
        public decimal resultCode;
        public string resultMessage;
        public int resultAffectedRows;

        //public void ThrowExceptionIfError()
        //{
        //    if (resultCode < 0)
        //    {
        //        throw new IntfResultExceptionType(this);
        //    }
        //}
        public decimal GetID()
        {
            //   ThrowExceptionIfError();
            return resultID;
        }
        public string GetString()
        {
            //ThrowExceptionIfError();
            return resultString;
        }
        public ReturnsType GetReturnsType()
        {
            return (new ReturnsType()
            {
                result = (resultCode < 0 ? false : true),
                sql_error = "",
                tag = 0,
                text = resultMessage
            });

        }

    }

    /// <summary>
    /// Сериализуемый класс для возврата результата: сообщение + таблица
    /// </summary>
    [Serializable]
    public class IntfResultTableType : IntfResultType
    {
        public IntfResultTableType() : this(null, 0, "Выполнено") { }
        public IntfResultTableType(DataTable resultData) : this(resultData, 0, "Выполнено") { }
        public IntfResultTableType(DataTable resultData, decimal resultCode, string resultMessage) :
            base(resultCode, resultMessage)
        {
            this.resultData = resultData;
        }
        public DataTable resultData;

        public DataTable GetData()
        {
            //ThrowExceptionIfError();
            return resultData;
        }

    }

    /// <summary>
    /// Сериализуемый класс для возврата результата: сообщение + простой класс
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Serializable]
    public class IntfResultObjectType<T> : IntfResultType where T : class, new()
    {
        public IntfResultObjectType() : this(null, 0, "Выполнено") { }
        public IntfResultObjectType(T resultData) : this(resultData, 0, "Выполнено") { }
        //        public IntfResultObjectType(T resultData) : this(resultData, (resultData as IntfResultType).resultCode, (resultData as IntfResultType).resultMessage) { }
        public IntfResultObjectType(decimal resultCode, string resultMessage) : this(null, resultCode, resultMessage) { }
        public IntfResultObjectType(T resultData, decimal resultCode, string resultMessage) :
            base(resultCode, resultMessage)
        {
            this.resultData = resultData;
        }
        public T resultData;

        public T GetData()
        {
            //ThrowExceptionIfError();
            return resultData;
        }

    }

}
