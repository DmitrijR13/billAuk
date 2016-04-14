using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Bars.KP50.Utils;
using Globals.SOURCE.Utility;
using SevenZip;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using System.Data;
using System.Linq;
using System.IO;
using STCLINE.KP50.Utility;
using Excel = Microsoft.Office.Interop.Excel;

namespace STCLINE.KP50.DataBase
{
    /// <summary>
    /// Выгрузка реестра для Тагила (Сбербанк)
    /// </summary>
    public class BankDownloadReestrTagilSber : BankDownloadReestrVersion2
    {
        protected override string GetAddCode()
        {
            return "0";
        }

        protected override Returns SetCntsData(IDbConnection conn_db, string pref, DateTime calc_month)
        {
            return new Returns(true, "");
        }

        protected override int GetCounterCount()
        {
            return 0;
        }

        protected override string AssemblyOneString(DataRow row)
        {
            throw new NotImplementedException();
        }

        protected override string GetFileName(int nzpReestr)
        {
            var date = DateTime.Now;
            return "BEZPB" + date.Year.ToString().Substring(2) + date.Month.ToString().PadLeft(2, '0') + date.Day.ToString().PadLeft(2, '0') + ".zip";
        }

        protected override void WriteReestrToFile(IDbConnection connDb, string fileNameIn, ExcelRep excelRepDb, string[] additionLines)
        {
            var ret = new Returns();
            string fullPathZip = Path.Combine(Constants.ExcelDir, fileNameIn);
            string fullPathDbf = fullPathZip.Replace(".zip", ".dbf");
            string currentDate = DateTime.Now.ToShortDateString();

            exDBF eDbf = new exDBF(fileNameIn.Split('.')[0]);
            try
            {
                eDbf.AddColumn("DATA", typeof(DateTime), 8, 0);
                eDbf.AddColumn("LS", typeof(decimal), 13, 0);
                eDbf.AddColumn("ADDR", typeof(string), 70, 0);
                eDbf.AddColumn("SUMMA", typeof(double), 10, 2);

                eDbf.Save(Constants.ExcelDir, 866);

                var dt = ClassDBUtils.OpenSQL("select * from tmp_reestr", connDb);
                if (dt.resultCode < 0)
                {
                    throw new Exception(dt.resultMessage);
                }

                int num = dt.resultData.Rows.Count;
                for (int i = 0; i < num; i++)
                {
                    DataRow rowOfSelect = dt.resultData.Rows[i];
                    DataRow rowOfDbf = eDbf.DataTable.NewRow();

                    rowOfDbf["DATA"] = currentDate.ToDateTime();
                    rowOfDbf["LS"] = rowOfSelect["pkod"] != DBNull.Value ? ((decimal)rowOfSelect["pkod"]) : 0;
                    rowOfDbf["ADDR"] = rowOfSelect["adr"] != DBNull.Value ? ((string)rowOfSelect["adr"]).Trim() : "";
                    rowOfDbf["SUMMA"] = (rowOfSelect["sum_charge"] != DBNull.Value ? Convert.ToDecimal(rowOfSelect["sum_charge"]) : 0);

                    eDbf.DataTable.Rows.Add(rowOfDbf);
                    if (i % 100 == 0)
                    {
                        excelRepDb.SetMyFileProgress(new ExcelUtility { nzp_exc = _nzpExc, progress = ((decimal)i) / num });
                        eDbf.Append(fullPathDbf);
                        eDbf.DataTable.Rows.Clear();
                    }

                }
            }
            catch (Exception ex)
            {
                // удалить реестр
                throw new Exception("Ошибка при записи данных, функция WriteReestrToFile " + ex.Message);
            }

            if (eDbf.DataTable.Rows.Count > 0)
            {
                eDbf.Append(fullPathDbf);
            }

            /*
                        SevenZipCompressor file = new SevenZipCompressor();
                        file.EncryptHeaders = true;
                        file.CompressionMethod = SevenZip.CompressionMethod.BZip2;
                        file.DefaultItemName = fileNameIn;
                        file.CompressionLevel = SevenZip.CompressionLevel.Normal;

                        file.CompressDirectory(fullPath + dir, fullPath + dir.Substring(0, dir.Length - 1) + ".7z");
            */
            Archive.GetInstance().Compress(fullPathZip, new[] { fullPathDbf }, false);
            File.Delete(fullPathDbf);
            if (InputOutput.useFtp)
            {
                try
                {
                    InputOutput.SaveOutputFile(fullPathZip);
                }
                catch (Exception ex)
                {
                    // удалить реестр
                    throw new Exception("Ошибка при передаче данных на web-сервер, функция GetUploadReestr " + ex.Message);
                }
            }
        }
    }
}
