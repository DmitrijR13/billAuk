using NUnit.Framework;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;


namespace Bars.KP50.Test.Report
{
    [TestFixture]
    public class DbMyFilesTest
    {



        [Test]
        public void AddFile()
        {
            Constants.cons_Webdata =
                "Server=192.168.170.215;Port=5432;User Id=postgres;Password=postgres;Database=webtul2;Preload Reader=true;";

            var dbMyFiles = new DBMyFiles();
            Returns ret = dbMyFiles.AddFile(
                new ExcelUtility()
                {
                    nzp_user = 1,
                    status = ExcelUtility.Statuses.InProcess,
                    rep_name = "Тест",
                    progress = 0
                });

            Assert.IsTrue(ret.result, "Ошибка добаления задания");

            ExcelUtility excelUtility = dbMyFiles.GetFile(ret.tag, 1);
            Assert.IsTrue(excelUtility.rep_name == "Тест", "Ошибка считывания задания");

            dbMyFiles.SetFileProgress(ret.tag, 10);
            excelUtility = dbMyFiles.GetFile(ret.tag, 1);
            Assert.IsTrue(excelUtility.progress == 10m, "Ошибка изменения прогресса задания");

            dbMyFiles.SetFileStatus(ret.tag, ExcelUtility.Statuses.Success);
            excelUtility = dbMyFiles.GetFile(ret.tag, 1);
            Assert.IsTrue(excelUtility.status == ExcelUtility.Statuses.Success, "Ошибка изменения статуса задания");

            Returns retDel = dbMyFiles.DelFile(ret.tag);
            Assert.IsTrue(retDel.result, "Ошибка удаления задания");

          
            dbMyFiles.Close();
            
        }




    }
}
