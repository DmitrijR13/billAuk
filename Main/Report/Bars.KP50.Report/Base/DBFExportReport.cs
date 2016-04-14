namespace Bars.KP50.Report.Base
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using Globals.SOURCE.Utility;

   /// <summary>
   /// Класс экспорта отчета в DBF
   /// </summary>
    public class DBFExportReport
    {
        private string savePath;
        private string[] dbfList;

        /// <summary>
        /// Сохранение отчета в DBF
        /// </summary>
        /// <param name="ds">Датасет передаваемый в отчет</param>
        public string[] SaveReportDbf(DataSet ds, string filePath)
        {
            savePath = filePath;
            
            int numberInfoTable = -1;
            int numberDataTable = -1;
            
            //Предварительная проверка на спец структуры для выгрузки
            for (int i = 0; i < ds.Tables.Count; i++)
            {
                if (ds.Tables[i].TableName.ToUpper() == "DBFINFO") numberInfoTable = i;
                if (ds.Tables[i].TableName.ToUpper() == "DATA") numberDataTable = i;
            }

            if (numberInfoTable > -1 && numberDataTable>-1)
            {
                SaveSpecialReport(ds.Tables[numberInfoTable], ds.Tables[numberDataTable]);
            }
            else
            {
                SaveStandartReport(ds);
            }

            return dbfList;
        }



        /// <summary>
        /// Сохранение отчета со спецификацией
        /// </summary>
        /// <param name="infoTable">Таблица спецификации</param>
        /// <param name="dataTable">Таблица с данными</param>
        private void SaveSpecialReport(DataTable infoTable, DataTable dataTable)
        {
                dbfList = new string[1];
                string newFileName = Path.GetFileNameWithoutExtension(savePath);
                string codepage = "1251";
                var fields = new List<string>();
                var precisions = new List<string>();
                var scale = new List<string>();
                exDBF eDBF = null;

                for (int i = 0; i<infoTable.Rows.Count;i++)
                {
                    string param = infoTable.Rows[i][0].ToString().Trim();
                    string value = infoTable.Rows[i][1].ToString().Trim();
                    if (param == "file")
                    {
                        newFileName = value;
                        eDBF = new exDBF(newFileName);
                        
                    }
                    if (param == "codepade")
                        codepage = value;
                    if (param == "field")
                    {
                        fields.Add(value);
                        precisions.Add(infoTable.Rows[i][2].ToString().Trim());
                        scale.Add(infoTable.Rows[i][3].ToString().Trim());
                    }
                }

                if (eDBF != null)
                {
                    for (int i = 0; i < dataTable.Columns.Count; i++)
                    {
                        eDBF.AddColumn(dataTable.Columns[i].ColumnName,
                            Type.GetType(fields[i]), Convert.ToByte(precisions[i]),
                            Convert.ToByte(scale[i]));
                    }
                    foreach (DataRow dr in dataTable.Rows)
                    {
                        eDBF.DataTable.ImportRow(dr);
                    }
                    string newPath = Path.GetDirectoryName(savePath);

                    eDBF.Save(newPath, Convert.ToInt32(codepage));
                    dbfList[0] = newPath + "\\" + newFileName + ".DBF";

                }
        }


        /// <summary>
        /// Сохранение стандартного отчета без учета спецификации
        /// </summary>
        /// <param name="ds"></param>
        private void SaveStandartReport(DataSet ds)
        {
            dbfList = new string[ds.Tables.Count];
            for (int i = 0; i < ds.Tables.Count; i++)
            {
                string newPath = Path.GetDirectoryName(savePath);

                string newFileName = Path.GetFileNameWithoutExtension(savePath) + "_" + i;

                exDBF eDBF = new exDBF(newFileName);
                eDBF.AddTable(ds.Tables[i]);
                eDBF.Save(newPath, 1251);
                dbfList[i] = newPath + "\\" + newFileName + ".DBF";

            }
        }

    }
}