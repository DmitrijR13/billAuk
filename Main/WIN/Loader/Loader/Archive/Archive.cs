
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace FormatLibrary
{
    [Flags]
    public enum ArchiveFormat
    {
        Zip = 1,
        SevenZip = 2,

        /// <summary>
        /// Decompress only
        /// </summary>
        Rar = 4
    }

    public interface IArchive
    {
        /// <summary>
        /// Событие обработки исключений архиватора.
        /// Жестко возвращает исключение, если обработка отсутствует.
        /// </summary>
        event EventHandler<ArchiveExceptionEventArgs> onExceptionThrowed;

        /// <summary>
        /// Сжимает указанные файлы и/или каталоги в указанную папку
        /// </summary>
        /// <param name="OutputArchiveName">Имя выходного архива</param>
        /// <param name="CompressData">Список файлов и/или каталогов для сжатия</param>
        /// <param name="DeleteArchiveFiles">Удалить файлы после сжатия</param>
        /// <param name="Password">Пароль (null, если пароль не требуется)</param>
        /// <returns></returns>
        bool Compress(string OutputArchiveName, string[] CompressData, bool DeleteArchiveFiles = false, string Password = null);

        /// <summary>
        /// Извлечение файлов из архива
        /// </summary>
        /// <param name="InputArchiveName">Имя входного архива</param>
        /// <param name="ExtractDirectory">Каталог, в который необходимо извлечь файлы. По умолчанию - директория с именем архива.</param>
        /// <param name="DeleteArchive">Удалить архив после извлечения</param>
        /// <param name="Password">Пароль (null, если пароль не требуется)</param>
        /// <returns>Спмсок извлеченных файлов</returns>
        string[] Decompress(string InputArchiveName, string ExtractDirectory = null, bool DeleteArchive = false, string Password = null);

        /// <summary>
        /// Извлечение файлов из архива
        /// </summary>
        /// <param name="InputArchiveName">Поток</param>
        /// <param name="ExtractDirectory">Каталог, в который необходимо извлечь файлы. По умолчанию - директория с именем архива.</param>
        /// <param name="DeleteArchive">Удалить архив после извлечения</param>
        /// <param name="Password">Пароль (null, если пароль не требуется)</param>
        /// <returns>Спмсок извлеченных файлов</returns>
        string[] Decompress(Stream InputArchiveStream, string ExtractDirectory, string Password = null);
    }

    public abstract class Archive : IArchive
    {
        public static event EventHandler<EventArgs> onInstanceCreated;

        public abstract event EventHandler<ArchiveExceptionEventArgs> onExceptionThrowed;

        protected static IDictionary<ArchiveFormat, Type> SupportFormats { get; private set; }

        #region Impliment of Singleton
        protected Archive()
        {
            if(onInstanceCreated != null)
                onInstanceCreated(this, new EventArgs());
        }

        private sealed class ArchiveCreator<I> where I : class
        {
            private static I instance;

            internal static I Instance
            {
                get
                {
                    return instance ?? (instance = typeof(I).GetConstructor(
                        BindingFlags.Instance | BindingFlags.NonPublic, null,
                        Type.EmptyTypes, new ParameterModifier[0]).Invoke(null) as I);
                }
            }
        }

        /// <summary>
        /// Создает экземпляр необходимого объекта
        /// </summary>
        /// <param name="Format">Формат архива. По умолчанию Zip.</param>
        /// <returns>Экземпляр созданного провайдера</returns>
        public static IArchive GetInstance(ArchiveFormat Format = ArchiveFormat.Zip)
        {
            return SupportFormats.Where(row => row.Key == Format).Select(row => row.Value).First().
                GetProperty("Instance", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null, null) as IArchive;
        }

        /// <summary>
        /// Создает экземпляр необходимого объекта
        /// </summary>
        /// <param name="ArchiveName">Имя архива с его расширением</param>
        /// <returns>Экземпляр созданного провайдера</returns>
        public static IArchive GetInstance(string ArchiveName)
        {
            switch (Path.GetExtension(ArchiveName).ToLower())
            {
                case ".zip":
                    return GetInstance(ArchiveFormat.Zip);
                case ".7z":
                case ".7zip":
                    return GetInstance(ArchiveFormat.SevenZip);
                case ".rar":
                    return GetInstance(ArchiveFormat.Rar);
                default:
                    throw new NotSupportedException();
            }
        }
        #endregion

        #region Static methods
        static Archive()
        {
            SupportFormats = new Dictionary<ArchiveFormat, Type>();
            RegisterType(typeof(ArchiveCreator<ZipArchive>), ArchiveFormat.Zip);
            RegisterType(typeof(ArchiveCreator<SevenZipArchive>), ArchiveFormat.SevenZip);
            RegisterType(typeof(ArchiveCreator<RarArchive>), ArchiveFormat.Rar);
        }

        private static void RegisterType(Type T, ArchiveFormat Format)
        {
            if (SupportFormats.Count(row => row.Key == Format) > 0)
                throw new DuplicateWaitObjectException();
            SupportFormats.Add(Format, T);
        }
        #endregion

        protected void DeleteFiles(string[] Files)
        {
            foreach (string strPath in Files)
            {
                if (File.GetAttributes(strPath) == FileAttributes.Directory)
                    Directory.Delete(strPath, true);
                else if (File.Exists(strPath))
                {
                    FileAttributes attributes = File.GetAttributes(strPath);
                    if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                    {
                        attributes = RemoveAttribute(attributes, FileAttributes.ReadOnly);
                        File.SetAttributes(strPath, attributes);
                    }
                    File.Delete(strPath);
                }
            }
        }

        protected string GetCommonFilePath(string[] Pathes)
        {
            switch (Pathes.Count())
            {
                case 1:
                    return (File.GetAttributes(Pathes[0]) == FileAttributes.Directory) ? Path.GetFullPath(Pathes[0]) + "\\" : Path.GetDirectoryName(Pathes[0]) + "\\";
                default:
                    var MatchingChars =
                        from len in Enumerable.Range(0, Pathes.Min(s => s.Length)).Reverse()
                        let possibleMatch = Pathes.First().Substring(0, len)
                        where Pathes.All(f => f.StartsWith(possibleMatch))
                        select possibleMatch;
                    return Path.GetDirectoryName(MatchingChars.First()) + "\\";
            }
        }

        private static IEnumerable<string> GetFilesFromPath(string path)
        {
            var lstPathes = new List<string>();
            path = Path.GetFullPath(path);
            if (File.GetAttributes(path) == FileAttributes.Directory)
            {
                lstPathes = Directory.GetDirectories(path).Aggregate(lstPathes, (current, subdir) => current.Union(GetFilesFromPath(subdir)).ToList());
                lstPathes.AddRange(Directory.GetFiles(path));
            }
            else if (File.Exists(path)) lstPathes.Add(path);
            return lstPathes;
        }

        protected string[] GetPathes(string[] Pathes)
        {
            var lstPathes = new List<string>();

            lstPathes = Pathes.Aggregate(lstPathes, (current, item) => current.Union(GetFilesFromPath(item)).ToList());

            return lstPathes.ToArray();
        }

        protected static FileAttributes RemoveAttribute(FileAttributes attributes, FileAttributes attributesToRemove)
        {
            return attributes & ~attributesToRemove;
        }

        #region Impliment of IArchive
        public abstract bool Compress(string OutputArchiveName, string[] CompressData, bool DeleteArchiveFiles = false, string Password = null);

        public abstract string[] Decompress(string InputArchiveName, string ExtractDirectory = null, bool DeleteArchive = false, string Password = null);

        public abstract string[] Decompress(Stream InputArchiveStream, string ExtractDirectory, string Password = null);
        #endregion
    }
}
