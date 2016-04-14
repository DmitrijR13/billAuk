namespace STCLINE.KP50.DataBase
{
    using System;

    public class SqlException : ApplicationException
    {
        public SqlException(string message) : base(message)
        {
        }

        public SqlException(string message, Exception exc) : base(message, exc)
        {
        }
    }
}