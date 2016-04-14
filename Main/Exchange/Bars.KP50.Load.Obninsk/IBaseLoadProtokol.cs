using System;
using System.Collections.Generic;
using System.Data;
using STCLINE.KP50.Global;
using System.Collections;

namespace Bars.KP50.Load.Obninsk
{
    public interface IBaseLoadProtokol
    {
        IDbConnection ConnDB { get; set; }

        /// <summary>
        /// Таблица счетчиков, которые не были сопоставлены в ходе разбора 
        /// </summary>
        DataTable UnrecognizedRows { get; }

        /// <summary>
        /// Список некорректных строк
        /// </summary>
        DataTable UncorrectRows { get; }
        /// <summary>
        /// Комментарии в ходе загрузки
        /// </summary>
        DataTable Comments { get; }
        /// <summary>
        /// Количество добавленных строк
        /// </summary>
        int CountInsertedRows{ get; set; }

        /// <summary>
        /// Добавить комментарий в протокол 
        /// </summary>
        /// <param name="comment">Комментарий</param>
        void AddComment(string comment);

        /// <summary>
        /// Добавить некорректные по формату строки- сообщения
        /// </summary>
        /// <param name="sourceSring">Некорректная стока + сообщение</param>
        void AddUncorrectedRow(string sourceSring, string bank="");

        /// <summary>
        /// Добавить несопоставленные строки
        /// </summary>
        /// <param name="sourceSring">Несопоставленная стока</param>
        void AddUnrecognisedRow(string sourceSring, string bank = "");

        /// <summary>
        /// Установка процента прогресса
        /// </summary>
        /// <param name="proc">Процент прогресса</param>
        /// <param name="status">Статус загрузки</param>
        /// <returns></returns>
        bool SetProcent(double proc, ExcelUtility.Statuses status);


    }
}