namespace STCLINE.KP50
{
    using System;

    using Globals.SOURCE.Config;

    public static class InputOutput
    {
        public static FtpParams ftpParams = null;
        public static bool useFtp = false;
        private static FileManager _fileManager = null;

        private enum Direction
        {
            Import = 1,
            Export = 2
        }

        public static void InitializeFileManager(FileManager fm)
        {
            _fileManager = fm;
        }

        /// <summary>
        /// Сохраняет счет
        /// </summary>
        /// <param name="sourceFilename"></param>
        /// <returns></returns>
        public static string SaveBill(string sourceFilename)
        {
            return _saveFile(sourceFilename, FileManager.OutputFileCategory.Bill, Direction.Export);
        }

        /// <summary>
        /// Копирует импортируемый указанный файл в файловое хранилище
        /// </summary>
        /// <param name="sourceFilename"></param>
        /// <param name="category"></param>
        /// <returns></returns>
        public static string SaveOutputFile(string sourceFilename)
        {
            return _saveFile(sourceFilename, FileManager.OutputFileCategory.Others, Direction.Export);
        }

        public static string SaveInputFile(string sourceFilename)
        {
            return _saveFile(sourceFilename, FileManager.OutputFileCategory.Others, Direction.Import);
        }

        private static void CheckObjectInitialized()
        {
            if (_fileManager == null) throw new Exception("Объект сохранения файлов не инициализирован");
        }

        private static string _saveFile(string sourceFilename, FileManager.OutputFileCategory category, Direction direction)
        {
            CheckObjectInitialized();

            string fileName;
            if (direction == Direction.Import)
                fileName = _fileManager.SaveInputFile(sourceFilename);
            else
            {
                fileName = _fileManager.SaveOutputFile(sourceFilename, category);
            }
            System.IO.File.Delete(sourceFilename);
            return fileName;
        }

        public static bool DownloadFile(string sourceFileName, string destinationFileName)
        {
            CheckObjectInitialized();

            return _fileManager.DownloadFile(sourceFileName, destinationFileName);
        }

        public static bool DownloadFile(string sourceFileName, string destinationFileName, bool OwerrideLocalFile)
        {
            CheckObjectInitialized();
            return _fileManager.DownloadFile(sourceFileName, destinationFileName, OwerrideLocalFile);
        }

        public static string GetOutputDir()
        {
            CheckObjectInitialized();

            return _fileManager.GetOutputDir(FileManager.OutputFileCategory.Others);
        }

        public static string GetBillDir()
        {
            CheckObjectInitialized();

            return _fileManager.GetOutputDir(FileManager.OutputFileCategory.Bill);
        }

        public static string GetInputDir()
        {
            CheckObjectInitialized();

            return _fileManager.GetInputDir();
        }
    }
}
