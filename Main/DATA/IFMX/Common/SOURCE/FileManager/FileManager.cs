namespace STCLINE.KP50
{
    using System;
    using System.IO;

    using Globals.SOURCE.Config;
    using Globals.SOURCE.Container;
    using STCLINE.KP50.Global;

    public class FileManager
    {
        #region public
        /// <summary>
        /// Категория исходящего файла
        /// </summary>
        public enum OutputFileCategory
        {
            Bill = 1,
            Others = 2
        }

        public static FileManager GetFolderInstance(string destinationFolder)
        {
            return new FileManager
            {
                _mode = Mode.Folder,
                _destinationFolder = destinationFolder
            };
        }

        public static FileManager GetFtpInstance(string localFolder)
        {
            return new FileManager
            {
                _mode = Mode.Ftp,
                _destinationFolder = localFolder
            };
        }

        /// <summary>
        /// Копирует указанный файл в файловое хранилище
        /// </summary>
        /// <param name="sourceFilename">копируемый файл</param>
        /// <param name="category">категория файла</param>
        /// <returns>имя сохраненного файла</returns>
        public string SaveOutputFile(string sourceFilename, OutputFileCategory category)
        {
            if (_mode == Mode.Ftp) return SaveExportFileToFtp(sourceFilename, category);
            if (_mode == Mode.Folder) return SaveExportFileToFolder(sourceFilename);
            return string.Empty;
        }

        /// <summary>
        /// Копирует импортируемый указанный файл в файловое хранилище
        /// </summary>
        /// <param name="sourceFilename"></param>
        /// <param name="category"></param>
        /// <returns></returns>
        public string SaveInputFile(string sourceFilename)
        {
            if (_mode == Mode.Ftp) return SaveImportFileToFtp(sourceFilename);
            if (_mode == Mode.Folder) return SaveImportFileToFolder(sourceFilename);
            return string.Empty;
        }

        public bool DownloadFile(string sourceFileName, string destinationFileName)
        {
            var ftp = _GetFtpUtility();
            return ftp.DownloadFile(sourceFileName, destinationFileName);
        }
        public bool DownloadFile(string sourceFileName, string destinationFileName, bool OwerrideLocalFile)
        {
            var ftp = _GetFtpUtility();
            return ftp.DownloadFile(sourceFileName, destinationFileName, OwerrideLocalFile);
        }


        public string GetOutputDir(OutputFileCategory category)
        {
            var dir = Path.Combine(_destinationFolder, getExportDir(category));
            if (!Directory.Exists(dir))
            {
                try
                {
                    Directory.CreateDirectory(dir);
                }
                catch 
                {
                    MonitorLog.WriteLog("Неверно заданный путь в файле конфигурации:" + dir, MonitorLog.typelog.Error, true);
                }
            }

            return dir;
        }

        public string GetInputDir()
        {
            string dir = Path.Combine(_destinationFolder, getImportDir());
            if (!Directory.Exists(dir))
            {
                try
                {
                    Directory.CreateDirectory(dir);
                }
                catch 
                {
                    MonitorLog.WriteLog("Неверно заданный путь в файле конфигурации:" + dir, MonitorLog.typelog.Error, true);
                }
            }

            return dir;
        }

        #endregion

        #region private
        private enum Mode
        {
            Folder = 1,
            Ftp = 2
        }

        private Mode _mode;

        const string pathDelimiter = "/";

        string IMPORT_DIR = "import" + pathDelimiter;
        string EXPORT_DIR = ""; //"reports" + pathDelimiter;
        string BILL_DIR = "bill" + pathDelimiter;
        //string WEB_DIR = "web" + pathDelimiter;

        private FtpParams _ftpParams;

        protected FtpParams FtpParams
        {
            get
            {
                return _ftpParams ?? (_ftpParams = IocContainer.Current.Resolve<IConfigProvider>().GetConfig().FtpParams);
            }
        }

        string _destinationFolder;

        private FileManager()
        {
        }

        private string getExportDir(OutputFileCategory category)
        {
            DateTime dt = DateTime.Now;
            //string subDir = dt.Year.ToString("0000") + pathDelimiter + dt.Month.ToString("00") + pathDelimiter + dt.Day.ToString("00") + pathDelimiter;
            string subDir = "";

            if (category == OutputFileCategory.Bill) return EXPORT_DIR + BILL_DIR + subDir;
            else if (category == OutputFileCategory.Others) return EXPORT_DIR + subDir;
            else return "";
        }

        private string getImportDir()
        {
            DateTime dt = DateTime.Now;
            string subDir = "";//dt.Year.ToString("0000") + pathDelimiter + dt.Month.ToString("00") + pathDelimiter + dt.Day.ToString("00") + pathDelimiter;
            return IMPORT_DIR + subDir;
        }

        private string SaveExportFileToFtp(string sourceFilename, OutputFileCategory category)
        {
            FtpUtility ftp = new FtpUtility(FtpParams.Address, FtpParams.Credentials.UserName, FtpParams.Credentials.Password, FtpParams.Credentials.Domain);

            string dir = getExportDir(category);

            ftp.MakeDirectory(dir);

            string targetfileName = dir + Path.GetFileName(sourceFilename);
            int k = 0;
            try
            {
                while (ftp.FileExists(targetfileName, FtpUtility.ExistsMethod.FileSize))
                {
                    k++;
                    targetfileName = dir + Path.GetFileNameWithoutExtension(sourceFilename) + "_" + k +
                                     Path.GetExtension(sourceFilename);
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteException("Can not exists file of FTP. File is \"" + sourceFilename + "\"", ex);
            }
            try
            {
                targetfileName = ftp.UploadFile(sourceFilename, targetfileName);
            }
            catch (Exception ex)
            {
                MonitorLog.WriteException("Can not exists file of FTP. File is \"" + sourceFilename + "\"", ex);
            }
            return targetfileName;
        }

        private string SaveExportFileToFolder(string sourceFilename)
        {
            //todo: не реализовано
            return "";
        }

        private FtpUtility _GetFtpUtility()
        {
            return new FtpUtility(FtpParams.Address, FtpParams.Credentials.UserName, FtpParams.Credentials.Password, FtpParams.Credentials.Domain);
        }

        private string SaveImportFileToFtp(string sourceFilename)
        {
            FtpUtility ftp = _GetFtpUtility();

            string dir = getImportDir();
            string targetfileName = dir + Path.GetFileName(sourceFilename);
            int k = 0;
            if (!System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(dir)))
            {
                ftp.MakeDirectory(dir);
            }

            while (ftp.FileExists(targetfileName, FtpUtility.ExistsMethod.List))
            {
                k++;
                targetfileName = dir + Path.GetFileNameWithoutExtension(sourceFilename) + "_" + k + Path.GetExtension(sourceFilename);
            }

            targetfileName = ftp.UploadFile(sourceFilename, targetfileName);

            return targetfileName;
        }

        private string SaveImportFileToFolder(string sourceFilename)
        {
            //todo: не реализовано
            return "";
        }

        #endregion
    }
}