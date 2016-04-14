using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace webroles.GenerateScriptTable
{
    public class ChangedRow
    {
        public string WhereDelimeter { get; set; }
        /// <summary>
        ////Наименование таблицы
        /// </summary>
        public String TableName;
        /// <summary>
        /// Словарь значений для инструкций Insert, Update
        /// </summary>
        public Dictionary<string, string> ColValuesDictionary;
        /// <summary>
        /// Словарь условия Where для  инструкций Update, Delete
        /// </summary>
        public Dictionary<string, string> WhereColValuesDictionary;
        /// <summary>
        ////Наименование id колонки таблицы. Применяется: 1.при поиске и удалении экземпляров ChangedRow из коллекции ChangedRowCollection (коллекция, в которой храняться строки для генерирования скрипта) 2.При вставке сущ значений для колонки forscript 
        /// </summary>
        public string NameIdColumn { get; private set; }
        /// <summary>
        /// Значение ячейки id колонки. Применяется: 1.при поиске и удалении экземпляров ChangedRow из коллекции ChangedRowCollection (коллекция, в которой храняться строки для генерирования скрипта) 2.При вставке сущ значений для колонки forscript 
        /// </summary>
        public int IdNum { get; private set; }
        /// <summary>
        ////Наименование базовой колонки (Только для таблиц второго уровня)
        /// </summary>
        public string NameBaseColumn { get; private set; }
        /// <summary>
        /// Значение ячейки базовой колонки (Только для таблиц второго уровня)
        /// </summary>
        public int ValueBaseColumn { get;  set; }
        /// <summary>
        /// Значение, статус строки (Insert, Update, Delete)
        /// </summary>
        public DataRowState State { get; set; }
        /// <summary>
        /// Свойство для корректной сортировки таблицы pages
        /// </summary>
        public bool IsSortKodChanged { get; set; }

        public ChangedRow(string tableName, string nameIdColumn, int idNum, int valueBaseColumn = -1, string whereDelimiter=" and ")
        {
            WhereDelimeter = whereDelimiter;
            ValueBaseColumn = valueBaseColumn;
            TableName = tableName;
            ColValuesDictionary = new Dictionary<string, string>();
            WhereColValuesDictionary= new Dictionary<string, string>();
           
            this.IdNum = idNum;
            this.NameIdColumn = nameIdColumn;
        }

        public void AddToValuesDictionaty(string columnName, string value)
        {
            if (value!=null)
            ColValuesDictionary.Add(columnName,value);
            else
             ColValuesDictionary.Add(columnName, null);
        }


    

        public void AddToWhereDictionaty(string columnName, string value)
        {
            if (value != null)
                WhereColValuesDictionary.Add(columnName, value);
            else
                WhereColValuesDictionary.Add(columnName, null);
        }
    }
}
