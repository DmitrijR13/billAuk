using System;
using System.Collections.Generic;
using System.Linq;

using System.Data;
using Bars.KP50.Report;
using Bars.KP50.Report.Base;

using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;

using FastReport.Export;
using FastReport.Export.Csv;
using FastReport.Export.Html;
using FastReport.Export.Image;
using FastReport.Export.OoXML;
using FastReport.Export.Pdf;
using FastReport.Export.Text;

namespace Bars.Report.Soft
{
    /// <summary>
    /// Процессор гибких отчетов
    /// </summary>
    public class Processor : BaseSqlReport, ISoftReport
    {
        /// <summary>
        /// Заголовок
        /// </summary>
        public override string Name
        {
            get { return _name; }
        }
        private string _name = "Гибкий отчет";

        /// <summary>
        /// Описание
        /// </summary>
        public override string Description
        {
            get { return _description; }
        }
        private string _description = "Загружается динамически, без перезагрузки серверов. Данные выбираются непосредственно SQL хранимой процедурой.";

        public override IList<ReportGroup> ReportGroups
        {
            //get { return null; }
            get { return new[] { ReportGroup.Reports }; }
        }

        public override IList<ReportKind> ReportKinds
        {
            //get { return null; }
            get { return new List<ReportKind> { ReportKind.Base }; }
        }

        /// <summary>
        /// Выводить на просмотр или экспорт
        /// </summary>
        public override bool IsPreview
        {
            get { return _isPreview; }
        }
        private bool _isPreview;


        /// <summary>
        /// Шаблон. 
        /// Путь к frx файлу относительно Bars.KP50.Host.exe
        /// </summary>
        protected override byte[] Template
        {
            get
            {
                return System.IO.File.ReadAllBytes(ExecScalar(String.Format("select report.get_frx({0}, {1});", SystemParams["NzpObject"], ReportParams.User.nzp_user)).ToString());
            }
        }

        /// <summary>
        /// Параметры отчета.
        /// Заглушка. Реальное заполнение параметров делается в Bars.KP50.Report.Handlers.ReportHandler
        /// </summary>
        /// <returns></returns>
        public override List<UserParam> GetUserParams()
        {
            return new List<UserParam>(); // Пустышка. Чтобы не было ошибки.
        }

        /// <summary>
        /// Наборы данных для шаблона
        /// </summary>
        private class ReportData
        {
            /// <summary>Порядок выполнения</summary>
            public int Turn { get; set; }

            /// <summary>Sql инструкция</summary>
            public string Sql { get; set; }

            /// <summary>Наименование набора данных</summary>
            public string Title { get; set; }
        }


        /// <summary>
        /// Выбрать данные
        /// </summary>        
        public override DataSet GetData()
        {
            // "Системные" параметры для sql инструкций
            string pReport = SystemParams["NzpObject"];             // Код отчёта
            string pUser = ReportParams.User.nzp_user.ToString();   // Код пользователя
            string pPoint = ReportParams.Pref;                      // Наименование центрального банка

            // Наборы данных
            var ds = new DataSet();

            // Параметризированные запросы
            List<ReportData> data = new List<ReportData>();

            IDataReader reader;
            ExecRead(out reader, String.Format("select * from report.get_data({0}, {1})", pReport, pUser));
            while (reader.Read())
            {
                data.Add
                (
                    new ReportData()
                    {
                        Turn = (int)reader["turn"],
                        Sql = (string)reader["sql"],
                        Title = (string)reader["title"]
                    }
                );
            }
            reader.Close();


            // Подставить параметры и выполнить sql инструкцию
            foreach (var dt in data)
            {
                // Системные
                dt.Sql = dt.Sql.Replace("@Report", pReport);
                dt.Sql = dt.Sql.Replace("@User", pUser);
                dt.Sql = dt.Sql.Replace("@Point", String.Format("'{0}'", pPoint)); // Т.к. параметр текстовый, то взять его в кавычки

                foreach (var userParameter in UserParamValues)
                {
                    string sqlParameter = "@" + userParameter.Key;
                    string value = "null"; // По умолчанию значения нет
                    if (userParameter.Value.Value != null)
                    {
                        value = String.Format("'{0}'", userParameter.Value.Value); // Все параметры пока строковые. Берутся в кавычки. Найти способ вытащить тип из userParameter.Value
                    }
                    dt.Sql = dt.Sql.Replace(sqlParameter, value);
                }

                var table = SQLToTable(this.Connection, dt.Sql);
                table.TableName = dt.Title;
                ds.Tables.Add(table);
            }

            return ds;
        }

        /// <summary>
        ///  Возвращает содержимо запроса в таблицу.
        ///  Заменяет "Стандартный" ExecSQLToTable. Исключительно ради того, чтобы задать таймаут.
        /// </summary>        
        /// <param name="sql">SQL Запрос</param>
        /// <returns>Таблицу с заполненными данными по запросу</returns>
        private DataTable SQLToTable(IDbConnection connection, string sql)
        {
            DataTable Data_Table = new DataTable();

            IDbCommand cmd = null;
            IDataReader reader = null;
            string err = String.Empty;

            try
            {
                cmd = DBManager.newDbCommand(sql, connection);
                cmd.CommandTimeout = 86400; // 24 часа
                reader = cmd.ExecuteReader();

                Utils.setCulture();
                if (reader != null) Data_Table.Load(reader, LoadOption.OverwriteChanges);
            }
            catch (Exception ex)
            {
                err = " Ошибка чтения из базы данных  \n " +
                      " БД " + connection.Database + " \n '" + sql + "' \n " + ex.Message;

                MonitorLog.WriteException(err);
                MonitorLog.WriteLog(" Выполнение запроса неудачно в DataTable " + err, MonitorLog.typelog.Info, 1, 1, true);
            }
            finally
            {
                if (reader != null) reader.Dispose();
                if (cmd != null) cmd.Dispose();

            }
            if (err != String.Empty)
                throw new Exception(err);

            return Data_Table;
        }


        protected override void PrepareParams()
        {
        }

        protected override void CreateTempTable()
        { }

        protected override void DropTempTable()
        { }


        /// <summary>
        /// Загрузить информацию об отчете: Name, Description, IsPreview и т.д. 
        /// </summary>
        /// <param name="reportID">Идентификатор отчета. Из таблицы report.list</param>
        /// <param name="userID">Идентификатор пользователя</param>
        /// <param name="connection">Подключение к базе данных</param>
        public void InitSoftProperties(int reportID, int userID, IDbConnection connection)
        {
            ID = reportID;

            IDataReader reader;
            DBManager.ExecRead(connection, out reader, String.Format("select * from report.get_info({0}, {1})", ID, userID), true);
            reader.Read();

            _name = (string)reader["title"];
            _description = (string)reader["description"];
            _isPreview = (bool)reader["preview"];

            reader.Close();
        }

        /// <summary>Получить экпортер</summary>
        /// <returns>Экспортер</returns>
        protected override ExportBase GetExporter()
        {
            ExportBase exporter = null;

            switch (ReportParams.ExportFormat)
            {
                case ExportFormat.Excel2007:
                    exporter = new Excel2007Export();
                    break;
                case ExportFormat.Pdf:
                    {
                        exporter = new PDFExport();
                        ((PDFExport)exporter).Compressed = false;
                    }
                    break;
                case ExportFormat.Html:
                    exporter = new HTMLExport();
                    break;
                case ExportFormat.Gif:
                    exporter = new ImageExport { ImageFormat = ImageExportFormat.Gif };
                    break;
                case ExportFormat.Jpg:
                    exporter = new ImageExport { ImageFormat = ImageExportFormat.Jpeg };
                    break;
                case ExportFormat.Png:
                    exporter = new ImageExport { ImageFormat = ImageExportFormat.Png };
                    break;
                case ExportFormat.Txt:
                    exporter = new TextExport();
                    ((TextExport)exporter).PageBreaks = false;                                  // Не разрывать страницы.
                    ((TextExport)exporter).Encoding = System.Text.Encoding.GetEncoding(1251);   // Кодировка win-1251.
                    break;
                case ExportFormat.Csv:
                    exporter = new CSVExport();
                    break;
            }

            if (exporter == null)
            {
                throw new ReportException("Неизвестный формат отчета");
            }

            return exporter;
        }

        /// <summary>
        /// Идентификатор отчета. Из таблицы report.list
        /// </summary>
        private int ID { get; set; }
    }
}
