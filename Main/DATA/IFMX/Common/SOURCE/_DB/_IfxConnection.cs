//--------------------------------------------------------------------------------80
//Файл: db.cs
//Дата создания: 26.10.2010
//Дата изменения: 26.10.2010
//Назначение: Системный класс работы со строкой подключения  для Informix
//Автор: Зыкин А.А.
//Copyright (c) Научно-технический центр "Лайн", 2009. 
//--------------------------------------------------------------------------------80
//using IBM.Data.Informix;

//namespace STCLINE.KP50.DataBase
//{
//    public class ClassConnectionParams
//    {

//        //public string getConnectionString()
//        //{
//        //    return this.getConnectionString(ClassDB.dataBaseConnection);
//        //}

//        //--------------------------------------
//        public string getConnectionString(string connectionString)
//        {

//              ///Server=ol_css;Database=css;Host=192.168.1.107;Service=9088;Password=portal;User ID=portal;Client Locale=ru_ru.CP1251;Database Locale=ru_ru.915;Pooling=True;Protocol=olsoctcp
//              ///Client Locale=ru_ru.CP1251;Database=websmr;Database Locale=ru_ru.915;Server=ol_mars;UID = webdb;Pwd = webdb
//              ///

//            connectionString = connectionString.Replace("UID", "User ID");
//            connectionString = connectionString.Replace("Pwd", "Password");

//            IfxConnectionStringBuilder conn = new IfxConnectionStringBuilder(connectionString);

//            return conn.ToString();
//        }
//    }
//}
