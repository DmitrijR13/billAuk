using System.Data;
using STCLINE.KP50.Global;

namespace Bars.KP50.Load.Obninsk
{
    public class BaseLoadProtocol: IBaseLoadProtokol
    {
        public IDbConnection ConnDB { get; set; }

        /// <summary>
        /// Таблица счетчиков, которые не были сопоставлены в ходе разбора 
        /// </summary>
        public DataTable UnrecognizedRows { get; set; }

        /// <summary>
        /// Список некорректных строк
        /// </summary>
        public DataTable UncorrectRows { get; set; }
        /// <summary>
        /// Комментарии в ходе загрузки
        /// </summary>
        public DataTable Comments { get; set; }
        /// <summary>
        /// Количество добавленных строк
        /// </summary>
        public int CountInsertedRows { get; set; }


        public BaseLoadProtocol()
        {
            UnrecognizedRows = new DataTable{TableName = "unrecog"};
            UnrecognizedRows.Columns.Add("sourceString", typeof(string));
            UnrecognizedRows.Columns.Add("bank", typeof(string));
            UncorrectRows = new DataTable { TableName = "uncorrect" };
            UncorrectRows.Columns.Add("sourceString", typeof(string));
            UncorrectRows.Columns.Add("bank", typeof(string));
            Comments = new DataTable { TableName = "comment" };
            Comments.Columns.Add("sourceString", typeof(string));
            CountInsertedRows = 0;
        }

        /// <summary>
        /// Добавить комментарий в протокол 
        /// </summary>
        /// <param name="comment">Комментарий</param>
        public void AddComment(string comment)
        {
            var dr = Comments.Rows.Add();
            dr["sourceString"] = comment;
        }

        /// <summary>
        /// Добавить некорректные по формату строки- сообщения
        /// </summary>
        /// <param name="sourceSring">Некорректная стока + сообщение</param>
        public void AddUncorrectedRow(string sourceSring, string bank="")
        {
            var dr = UncorrectRows.Rows.Add();
            dr["sourceString"] = sourceSring;
            dr["bank"] = bank;
        }

        /// <summary>
        /// Добавить несопоставленные строки
        /// </summary>
        /// <param name="sourceSring">Несопоставленная стока</param>
        public void AddUnrecognisedRow(string sourceSring, string bank="")
        {
            var dr = UnrecognizedRows.Rows.Add();
            dr["sourceString"] = sourceSring;
            dr["bank"] = bank;
        }

        /// <summary>
        /// Установка процента прогресса
        /// </summary>
        /// <param name="proc">Процент прогресса</param>
        /// <param name="status">Статус загрузки</param>
        /// <returns></returns>
        public bool SetProcent(double proc, ExcelUtility.Statuses status)
        {
            return false;
        }



    }
}