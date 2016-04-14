using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Bars.KP50.Load.Obninsk.Progress;
using Bars.KP50.Load.Obninsk.Progress.Interfaces;
using STCLINE.KP50.DataBase;

namespace Bars.KP50.Load.Obninsk.CountersLoad.Interfaces
{
    /// <summary>
    /// Интерфейс для загрузки счетчиков в различных форматах
    /// </summary>
    public interface ICountersLoad:IDisposable
    {
        /// <summary>
        /// Инициализация
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="protokol"></param>
        /// <param name="nzp_user"></param>
        void Init(IDbConnection connection, IBaseLoadProtokol protokol,  EventHandler<ProgressEventArgs> progress, int nzp_user);
        /// <summary>
        /// Разбор строк файла
        /// </summary>
        /// <param name="fileNameExthPath"></param>
        /// <param name="nzpLoad"></param>
        bool ParseFileRows(string fileNameExthPath, int nzpLoad);
        /// <summary>
        /// Сохранение в базе
        /// </summary>
        void SaveCountersInDB(string userLoadedFileName);
        /// <summary>
        /// Наименование загрузки
        /// </summary>
        String Name { get; }
        /// <summary>
        /// Описание загрузки
        /// </summary>
        String Description { get; }
    }
}
