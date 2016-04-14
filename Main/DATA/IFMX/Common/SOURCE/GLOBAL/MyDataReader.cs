namespace STCLINE.KP50.DataBase
{
    using System;
    using System.Data;

    public class MyDataReader
    {
        protected IDataReader _reader = null;

        protected IDbCommand _command = null;

        public void setReaderAndCommand(IDataReader reader, IDbCommand command)
        {
            this.Close();
            this._reader = reader;
            this._command = command;
        }

        /// <summary>
        /// Выполняет чтение следующей записи
        /// </summary>
        /// <returns>Результат чтения</returns>
        public bool Read()
        {
            if (this._reader != null) return this._reader.Read();
            else return false;
        }

        /// <summary>
        /// Возвращает значение поля по индексу в запросе
        /// </summary>
        /// <param name="i"></param>
        /// <returns>Значение поля</returns>
        public object this[int i]
        {
            get
            {
                return (this._reader != null) ? this._reader[i] : DBNull.Value;
            }
        }

        /// <summary>
        /// Возвращает значение поля по наименованию
        /// </summary>
        /// <param name="i"></param>
        /// <returns>Значение поля</returns>
        public object this[string name]
        {
            get
            {
                return (this._reader != null) ? this._reader[name] : DBNull.Value;
            }
        }

        /// <summary>
        /// Выполняет освобождение ресурсов
        /// </summary>
        public void Close()
        {
            if (this._reader != null)
            {
                if (!this._reader.IsClosed)
                {
                    this._reader.Close();
                    this._reader.Dispose();
                }
                this._reader = null;
            }

            if (this._command != null)
            {
                this._command.Dispose();
                this._command = null;
            }
        }
    }
}