using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Web;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.Utility
{
    public class UserException : Exception
    { 
       public UserException(string exceptionMessage): base(exceptionMessage){}
    
    }


    public static class ExceptionUtility
    {


        /// <summary>
        /// Замена системных сообщений на пользовательские
        /// </summary>
        /// <param name="originalIntfResult"></param>
        /// <returns></returns>
        public static ReturnsType GetErrorMessage(ReturnsType newResult, string originalMessage)
        {

            return newResult;
        
        }


        public static STCLINE.KP50.Global.Returns OnException(Exception ex, string methodName)
        {
            return OnException(ex, methodName, 20, 201);
        }
        public static STCLINE.KP50.Global.Returns OnException(Exception ex, string methodName, int eventID, short category)
        {
            //Сделать запись в лог-журнал
//            ClassLog.WriteLog(ex, "ExceptionImpl", "", "");
            STCLINE.KP50.Global.MonitorLog.WriteLog("Ошибка " + methodName + (STCLINE.KP50.Global.Constants.Viewerror ? "\n" + ex.Message : ""), 
                STCLINE.KP50.Global.MonitorLog.typelog.Error, eventID, category, true);


            ReturnsType newResult = new  ReturnsType();
            //Поиск ошибки в основной части исключения - можно заменить системные сообщения на пользовательские
            newResult = ExceptionUtility.GetErrorMessage(newResult, ex.Message);
            //Поиск ошибки в InnerExeption
            if (newResult.text == "" && ex.InnerException != null) newResult = ExceptionUtility.GetErrorMessage(newResult, ex.InnerException.Message);

            //Если  ошибка не определилась - берем ошибку из исключения
            if (newResult.text == "") newResult.text = ex.Message;
            if (newResult.result) newResult.result = false;
            if (newResult.tag > -1) newResult.tag = -1;


            STCLINE.KP50.Global.Returns ret;

            ret.result = newResult.result;
            ret.text = newResult.text;
            ret.tag = newResult.tag;
            ret.sql_error = newResult.sql_error;

            return ret;

            
            //return new T()
            //{
            //    result = newResult.result,
            //    text = newResult.text,
            //    tag = newResult.tag

            //};
        }

    }



    public class ExceptionDetails
    {
        public ExceptionDetails(string _methodName)
        {
            methodName = _methodName;
        }
        
        public string methodName { get; set; }
    
    }


}