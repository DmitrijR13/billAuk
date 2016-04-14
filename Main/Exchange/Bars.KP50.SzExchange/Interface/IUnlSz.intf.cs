using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using STCLINE.KP50.Global;

namespace Bars.KP50.SzExchange.Interface
{
    public interface IUnlSz
    {
        /// <summary>Уникальный идентификатор</summary>
        int Code { get; }

        /// <summary>Название</summary>
        string Name { get; }

        string NameText { get; }

        /// <summary>Создание временных таблиц</summary>
        void CreateTempTable();

        /// <summary>Удаление временных таблиц</summary>
        void DropTempTable();

        /// <summary>
        /// Выполнить
        /// </summary>
        /// <param name="pref"></param>
        void Start();

        void Start(FilesImported finder);
    }
}
