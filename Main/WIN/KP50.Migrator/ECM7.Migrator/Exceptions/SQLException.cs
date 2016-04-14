using System;

namespace KP50.DataBase.Migrator.Exceptions
{
    public class SQLException : Exception
    {
        public SQLException(Exception innerException)
            : base(innerException.Message, innerException)
        {
        }
    }
}
