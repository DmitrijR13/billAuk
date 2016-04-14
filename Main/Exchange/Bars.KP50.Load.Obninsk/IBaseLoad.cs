using System.Collections.Generic;
using System.Collections.Specialized;
using Bars.KP50.Report;
using STCLINE.KP50.Global;
using ReportParams = Bars.KP50.Report.Base.ReportParams;

namespace Bars.KP50.Load.Obninsk
{
    public interface IBaseLoad
    {
        /// <summary>Уникальный идентификатор загрузки</summary>
        string Code { get; }

        /// <summary>Название загрузки</summary>
        string Name { get; }

        /// <summary>Описание загрузки</summary>
        string Description { get; }

        /// <summary>Имя файла загрузки</summary>
        string FileName { get; }

        /// <summary>Временное имя файла загрузки в локальном хранилище</summary>
        string TemporaryFileName { get;  }

        /// <summary>Имя файла протокола загрузки</summary>
        string ProtocolFileName { get; }


        /// <summary>Организация - источник файла</summary>
        string SourceOrg { get; }

        /// <summary>Ответственный за выгрузку</summary>
        string UserSourceOrg { get; }

        IBaseLoadProtokol Protokol { get; }

        /// <summary>Парметры загрузки</summary>
        ReportParams ReportParams { get; set; }

        /// <summary>Получить пользовательские параметры загрузки</summary>
        /// <returns>Параметры загрузки</returns>
        List<UserParam> GetUserParams();

        /// <summary>Задать параметры для загрузки</summary>
        /// <param name="reportParameters">Параметры загрузки</param>
        void SetLoadParameters(NameValueCollection reportParameters);

        /// <summary>Сформировать загрузку</summary>
        /// <param name="reportParameters">Параметры загрузки</param>
        void GenerateLoad(NameValueCollection reportParameters);

        /// <summary>Загрузкить</summary>
        /// <returns>Данные отчета в виде DataSet</returns>
        void LoadData();

        /// <summary>Получить протокол загрузки</summary>
        string GetProtocolName();

        void SetProcessPercent(double percent, ExcelUtility.Statuses status);

    }
}