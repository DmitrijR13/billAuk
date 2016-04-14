using NUnit.Framework;
using STCLINE.KP50;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Bars.KP50.Test.Utils
{
    [TestFixture]
    public class FtpUtilityTest
    {
        [TestCase("192.168.179.112", "BARS", "dvkovenko", "yUkx5zk6", "2015-01\\File.bin", FtpUtility.ExistsMethod.List)]
        [TestCase("192.168.179.112", "BARS", "dvkovenko", "yUkx5zk6", "2015-01\\File.bin", FtpUtility.ExistsMethod.FileSize)]
        [TestCase("192.168.179.112", "BARS", "dvkovenko", "yUkx5zk6", "File.bin", FtpUtility.ExistsMethod.FileSize)]
        public void TestFileExists(string ServerIp, string Domain, string Login, string Password, string FileName, FtpUtility.ExistsMethod Method)
        {
            var utility = new FtpUtility(ServerIp, Login, Password, Domain);
            utility.FileExists(FileName, Method);
        }

        [TestCase("192.168.179.112", "BARS", "dvkovenko", "yUkx5zk6", "Directory/Subdirectory")]
        public void TestMakeDirectory(string ServerIp, string Domain, string Login, string Password, string Directory)
        {
            var utility = new FtpUtility(ServerIp, Login, Password, Domain);
            var result = utility.MakeDirectory(Directory);
            Assert.IsTrue(result, "Не удалось создать директорию");
        }

        [TestCase("192.168.179.112", "BARS", "dvkovenko", "yUkx5zk6", "Directory/Subdirectory")]
        [TestCase("192.168.179.112", "BARS", "dvkovenko", "yUkx5zk6", "File.bin")]
        public void TestDeleteFile(string ServerIp, string Domain, string Login, string Password, string FileName)
        {
            var utility = new FtpUtility(ServerIp, Login, Password, Domain);
            utility.DeleteFile(FileName);
        }

        [TestCase("192.168.179.112", "BARS", "dvkovenko", "yUkx5zk6", "File.bin")]
        [TestCase("192.168.179.112", "BARS", "dvkovenko", "yUkx5zk6", "subfolder/File.bin")]
        [TestCase("192.168.179.230", "BARS", "dvkovenko", "yUkx5zk6", "F:\\file.iso")]
        public void TestUploadFile(string ServerIp, string Domain, string Login, string Password, string FilePath)
        {
            using (var stream = new MemoryStream())
            {
                var utility = new FtpUtility(ServerIp, Login, Password, Domain);
                utility.UploadFile(FilePath, "file.iso");
            }
        }

        [TestCase("192.168.179.112", "BARS", "dvkovenko", "yUkx5zk6", "File.bin")]
        [TestCase("192.168.179.230", "BARS", "dvkovenko", "yUkx5zk6", "2015-02/file.iso")]
        public void TestDownloadFile(string ServerIp, string Domain, string Login, string Password, string FilePath)
        {
            var utility = new FtpUtility(ServerIp, Login, Password, Domain);
            utility.DownloadFile(FilePath);
            //using (var stream = utility.DownloadFile(FilePath))
            //    stream.Close();
        }

        [TestCase("192.168.179.112", "BARS", "dvkovenko", "yUkx5zk6", "2015-01/File.bin", "C:\\Temp\\File.bin")]
        public void TestDownloadFileToFolder(string ServerIp, string Domain, string Login, string Password, string FilePath, string Folder)
        {
            var utility = new FtpUtility(ServerIp, Login, Password, Domain);
            utility.DownloadFile(FilePath, Folder, true);
        }
    }
}
