using System;
using System.Text;
using NUnit.Framework;
using STCLINE.KP50.Utility;
using System.IO;

namespace Bars.KP50.Test.Utils
{
    [TestFixture]
    public class ArchiveZipTest
    {


        private string CreateFile(string dir)
        {
            string fileName = AppDomain.CurrentDomain.BaseDirectory + dir + "\\test.txt";
            

            FileStream fs = File.Create(@fileName);
            fs.Write(Encoding.GetEncoding(1251).GetBytes("тестя"), 0, 5);
            
            fs.Close();
            return fileName;
        }



        private string CreateDirectory()
        {
            string dirName = AppDomain.CurrentDomain.BaseDirectory + "\\Тест";
            Directory.CreateDirectory(@dirName);
            CreateFile("\\Тест");
            return dirName;
        }

        [Test]
        public void TestCompressFile()
        {
            string fileName = CreateFile("");
            string archiveName = fileName.Replace("txt", "zip");
            
            Archive.GetInstance().Compress(@archiveName, new string[] { @fileName }, true);
            Assert.IsTrue(File.Exists(@archiveName), "ошибка архивации");
            File.Delete(@archiveName);
            File.Delete(@fileName);
        }

        [Test]
        public void TestCompressFilePassword()
        {
            string fileName = CreateFile("");
            string archiveName = fileName.Replace("txt", "zip");

            Archive.GetInstance().Compress(@archiveName, new string[] { @fileName }, true);
            Assert.IsTrue(File.Exists(@archiveName), "ошибка архивации");

            File.Delete(@archiveName);
            File.Delete(@fileName);

        }

        [Test]
        public void TestCompressDirectory()
        {
            string dirName = CreateDirectory();
            string archiveName = AppDomain.CurrentDomain.BaseDirectory + "\\test1.zip";

            Archive.GetInstance().Compress(@archiveName, new string[] { @dirName }, true);
            Assert.IsTrue(File.Exists(@archiveName), "ошибка архивации");

        }

    }
}
