using STCLINE.KP50.Global;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace STCLINE.KP50
{
    public class FtpUtility
    {
        /// <summary>
        /// Метод поиска файла на Ftp сервере
        /// </summary>
        [Flags]
        public enum ExistsMethod
        {
            /// <summary>
            /// По размеру файла
            /// </summary>
            FileSize,

            /// <summary>
            /// По списку файлов в каталоге
            /// </summary>
            List
        }

        /// <summary>
        /// Домен пользователя
        /// </summary>
        public string Domain { get; private set; }

        /// <summary>
        /// Имя пользователя
        /// </summary>
        public string User { get; private set; }

        /// <summary>
        /// Пароль пользователя
        /// </summary>
        public string Password { get; private set; }

        /// <summary>
        /// Прокси для подключения
        /// </summary>
        public WebProxy Proxy { get; private set; }

        /// <summary>
        /// Uri сервера
        /// </summary>
        public Uri ServerUri { get; private set; }

        /// <summary>
        /// Создает подключение к серверу и авторизуется как анонимный пользователь
        /// </summary>
        /// <param name="ServerIP">Строка следующего вида: "IP_Address[:Port_number]"</param>
        public FtpUtility(string ServerIP) :
            this(ServerIP, "anonymous", String.Empty, String.Empty) { }

        /// <summary>
        /// Создает подключение к серверу и авторизуется заданным пользователем
        /// </summary>
        /// <param name="ServerIP">Строка следующего вида: "IP_Address[:Port_number]"</param>
        /// <param name="User">Имя пользователя для входа на удаленный FTP сервер.</param>
        /// <param name="Password">Пароль пользователя для входа на удаленный FTP сервер.</param>
        public FtpUtility(string ServerIP, string User, string Password) :
            this(ServerIP, User, Password, string.Empty) { }

        /// <summary>
        /// Создает подключение к серверу и авторизуется заданным пользователем
        /// </summary>
        /// <param name="ServerIP">Строка следующего вида: "IP_Address[:Port_number]"</param>
        /// <param name="user">Имя пользователя для входа на удаленный FTP сервер.</param>
        /// <param name="password">Пароль пользователя для входа на удаленный FTP сервер.</param>
        /// <param name="domain">Домен пользователя для входа на удаленный FTP сервер.</param>
        public FtpUtility(string ServerIP, string user, string password, string domain)
        {
            if (String.IsNullOrEmpty(ServerIP)) throw new ArgumentException("Строка подключения не может быть пустой.\n");
            FtpWebRequest.DefaultCachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);
            ServerUri = new Uri(String.Format("ftp://{0}/", ServerIP));
            User = user;
            Password = password;
            Domain = domain;
            Proxy = null;
        }

        /// <summary>
        /// Задает настройки Proxy-сервера с авторизацией пользователя.
        /// </summary>
        /// <param name="ProxyAddress">Адрес Proxy-сервера вида [http[s]://]ProxyAddres[:PortNumber].</param>
        /// <param name="ProxyUser">Имя пользователя Proxy-сервера.</param>
        /// <param name="ProxyPassword">Пароль пользователя Proxy-сервера.</param>
        /// <param name="Domain">Домен пользователя Proxy-сервера.</param>
        public void SetProxy(string ProxyAddress, string ProxyUser, string ProxyPassword, string Domain)
        {
            var wp = new WebProxy(new Uri(ProxyAddress));
            if (!String.IsNullOrEmpty(ProxyUser) && !String.IsNullOrEmpty(ProxyPassword)) wp.Credentials = new NetworkCredential(ProxyUser, ProxyPassword, Domain);
            Proxy = wp;
        }

        /// <summary>
        /// Удаляет ранее заданный Proxy-сервер
        /// </summary>
        public void ClearProxy() { Proxy = null; }

        private FtpWebRequest CreateRequest(string subUri, string Method)
        {
            var request = (FtpWebRequest)WebRequest.Create(new Uri(ServerUri, subUri));
            request.Credentials = new NetworkCredential(User, Password, Domain);
            request.Method = Method;
            request.KeepAlive = false;
            request.UseBinary = true;
            request.UsePassive = true;
            request.Proxy = Proxy;
            return request;
        }

        /// <summary>
        /// Декорирует директорию
        /// </summary>
        /// <param name="path">Сиходный путь</param>
        /// <returns>Декорированный путь</returns>
        private string MasquaradePath(string path)
        {
            var regexp = new Regex(@"^(\\?|/?)\d{4,4}-\d{2,2}(\\|/)");
            return ((regexp.IsMatch(path)) ? path :
                string.Format("{0:yyyy-MM}/{1}", DateTime.UtcNow, path)).Replace('\\', '/');
        }

        /// <summary>
        /// Функция проверяет существует ли файл на FTP сервере
        /// </summary>
        /// <param name="FtpPath">Путь к файлу на удаленном сервере вида "Folder\SubFolder\File.bin"</param>
        /// <param name="Method">Метод поиска файла на FTP сервере</param>
        /// <returns>Возвращает значение True если файл найден и имеет не нулевой размер, False если файл отсутствует или его размер равен 0.</returns>
        public bool FileExists(string FtpPath, ExistsMethod Method = ExistsMethod.FileSize)
        {
            FtpPath = MasquaradePath(FtpPath);
            bool boolExistFile = false;

            var FileName = String.Empty;
            var Directory = String.Empty;
            switch (Method)
            {
                case ExistsMethod.List:
                    foreach (var split in FtpPath.Split('/'))
                    {
                        Directory += String.Format("{0}/", FileName);
                        FileName = split;
                    }
                    break;
            }

            FtpWebRequest request = null;
            switch (Method)
            {
                case ExistsMethod.FileSize:
                    request = CreateRequest(FtpPath, WebRequestMethods.Ftp.GetFileSize);
                    break;

                case ExistsMethod.List:
                    request = CreateRequest(Directory, WebRequestMethods.Ftp.ListDirectory);
                    break;

                default: throw new InvalidOperationException("Unknown search method.");
            }

            try
            {
                using (var response = (FtpWebResponse)request.GetResponse())
                using (var responseStream = response.GetResponseStream())
                {
                    switch (Method)
                    {
                        case ExistsMethod.List:
                            using (var reader = new StreamReader(responseStream))
                            {
                                var files = reader.ReadToEnd().Replace("\r\n", "\n");
                                boolExistFile = files.Split('\n').Select(x => x.TrimEnd().ToLower()).Contains(FileName.ToLower());
                            }
                            break;
                        case ExistsMethod.FileSize:
                            boolExistFile = true;
                            break;
                        default: throw new InvalidOperationException("Unknown search method.");
                    }
                    responseStream.Close();
                    response.Close();
                }
            }
            catch (WebException ex)
            {
                using (var response = (FtpWebResponse)ex.Response)
                {
                    switch (Method)
                    {
                        case ExistsMethod.FileSize:
                            if (response.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable) boolExistFile = false;
                            else
                            {
                                MonitorLog.WriteException("FtpUtility: Can't exists file.", ex);
                                throw;
                            }
                            break;
                        default:
                            MonitorLog.WriteException("FtpUtility: Can't exists file.", ex);
                            throw;
                    }
                    response.Close();
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteException("FtpUtility: Can't exists file.", ex);
                throw;
            }

            return boolExistFile;
        }

        /// <summary>
        /// Создает рекурсивно папки на удаленном FTP сервере.
        /// </summary>
        /// <param name="FtpDirectory">Путь для создания на улаленном FTP сервере вида "Folder\SubFolder".</param>
        /// <returns>Возвращает True в случае успеха или False при ошибке.</returns>
        public bool MakeDirectory(string FtpDirectory)
        {
            bool boolIsCreated = true;
            FtpDirectory = MasquaradePath(FtpDirectory);
            var path = string.Empty;
            foreach (string directory in FtpDirectory.Split('/'))
            {
                if (string.IsNullOrWhiteSpace(directory)) continue;
                path = string.Format("{0}/{1}", path, directory);
                var request = CreateRequest(path, WebRequestMethods.Ftp.MakeDirectory);
                try
                {
                    using (var response = request.GetResponse())
                    {
                        var responseStream = response.GetResponseStream();
                        responseStream.Close();
                        response.Close();
                    }
                }
                catch (WebException ex)
                {
                    using (var response = (FtpWebResponse)ex.Response)
                    {
                        if (response.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable)
                        {
                            response.Close();
                            continue;
                        }
                        else
                        {
                            response.Close();
                            boolIsCreated = false;
                            MonitorLog.WriteException("FtpUtility: Can't make directory.", ex);
                            throw;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MonitorLog.WriteException("FtpUtility: Can't make directory.", ex);
                    throw;
                }
            }
            return boolIsCreated;
        }

        /// <summary>
        /// Удаляет файл с FTP сервера
        /// </summary>
        /// <param name="FtpPath">Путь к файлу на удаленном сервере вида "Folder\SubFolder\File.bin"</param>
        /// <returns>Возвращает True при успешном удалении файла или False при возникновении ошибки</returns>
        public bool DeleteFile(string FtpPath)
        {
            FtpPath = MasquaradePath(FtpPath);
            bool boolDeleteFile = false;
            var request = CreateRequest(FtpPath, WebRequestMethods.Ftp.DeleteFile);
            try
            {
                using (var response = request.GetResponse())
                    response.Close();
                boolDeleteFile = true;
            }
            catch (WebException ex)
            {
                using (var response = (FtpWebResponse)ex.Response)
                {
                    if (response.StatusCode != FtpStatusCode.ActionNotTakenFileUnavailable)
                    {
                        MonitorLog.WriteException("FtpUtility: Can't delete file.", ex);
                        throw;
                    }
                    response.Close();
                }
            }
            catch (Exception ex)
            {
                MonitorLog.WriteException("FtpUtility: Can't delete file.", ex);
                throw;
            }
            return boolDeleteFile;
        }

        /// <summary>
        /// Выгружает файл на удаленный FTP сервер.
        /// </summary>
        /// <param name="FilePath">Путь к локальному файлу.</param>
        /// <param name="FtpPath">Путь к файлу на удаленном сервере вида "Folder\SubFolder\File.bin"</param>
        /// <returns>Путь к файлу на FTP сервере</returns>
        public string UploadFile(string FilePath, string FtpPath)
        {
            var result = string.Empty;
            using (var stream = new FileStream(FilePath, FileMode.Open, FileAccess.Read))
            {
                result = UploadFile(stream, FtpPath, false);
                stream.Close();
            }
            return result;
        }

        /// <summary>
        /// Выгружает файл на удаленный FTP сервер.
        /// </summary>
        /// <param name="stream">Поток для отправки на сервер</param>
        /// <param name="FilePath">Путь к локальному файлу.</param>
        /// <param name="DisposeStreamAfterUpload">Уничтожить поток после работы</param>
        /// <param name="CreateDirectories">Создать директории при их отсутствии</param>
        /// <returns>Путь к файлу на FTP сервере</returns>
        public string UploadFile(Stream stream, string FtpPath, bool DisposeStreamAfterUpload = false, bool CreateDirectories = true)
        {
            FtpPath = MasquaradePath(FtpPath);
            var request = CreateRequest(FtpPath, WebRequestMethods.Ftp.UploadFile);
            try
            {
                var requestStream = request.GetRequestStream();
                stream.Seek(0, SeekOrigin.Begin);
                stream.CopyTo(requestStream);
                requestStream.Close();
                using (var response = (FtpWebResponse)request.GetResponse())
                    response.Close();
            }
            catch (WebException ex)
            {
                using (var response = (FtpWebResponse)ex.Response)
                {
                    if (response.StatusCode != FtpStatusCode.ActionNotTakenFileUnavailable)
                    {
                        MonitorLog.WriteException("FtpUtility: Can't upload file.", ex);
                        throw;
                    }
                    response.Close();
                }
                if (CreateDirectories)
                {
                    var Directory = string.Empty;
                    var FileName = string.Empty;
                    foreach (var split in FtpPath.Split('/'))
                    {
                        Directory += String.Format("{0}/", FileName);
                        FileName = split;
                    }
                    if (MakeDirectory(Directory))
                        return UploadFile(stream, FtpPath, DisposeStreamAfterUpload, false);
                }
                MonitorLog.WriteException("FtpUtility: Can't upload file.", ex);
                throw;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteException("FtpUtility: Can't upload file.", ex);
                throw;
            }
            finally
            {
                if (DisposeStreamAfterUpload && stream != null)
                {
                    stream.Close();
                    stream.Dispose();
                }
            }
            return FtpPath;
        }

        /// <summary>
        /// Функция скачивает удаленный файл с FTP сервера на локальный компьютер.
        /// </summary>
        /// <param name="FtpPath">Путь к файлу на удаленном сервере вида "Folder\SubFolder\File.bin".</param>
        /// <param name="LocalPath">Путь и имя локального файла.</param>
        /// <param name="OwerrideLocalFile">Переписать файл при существовании</param>
        /// <returns>Возвращает True в случае успеха или False в случае возникновения ошибки.</returns>
        public bool DownloadFile(string FtpPath, string LocalFile, bool OwerrideLocalFile = false)
        {
            var request = CreateRequest(FtpPath, WebRequestMethods.Ftp.DownloadFile);
            request.KeepAlive = true;
            try
            {
                using (var streamFile = new FileStream(LocalFile,
                    OwerrideLocalFile ? FileMode.Create : FileMode.CreateNew, FileAccess.Write))
                using (var response = (FtpWebResponse)request.GetResponse())
                using (var responseStream = response.GetResponseStream())
                {
                    responseStream.CopyTo(streamFile);
                    responseStream.Close();
                    streamFile.Flush();
                    streamFile.Close();
                    response.Close();
                    return true;
                }
            }
            catch (DirectoryNotFoundException)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LocalFile));
                return DownloadFile(FtpPath, LocalFile, true);
            }
            catch (WebException ex)
            {
                using (var response = (FtpWebResponse)ex.Response)
                {
                    if (response.StatusCode != FtpStatusCode.ActionNotTakenFileUnavailable)
                    {
                        MonitorLog.WriteException("FtpUtility: Can't download file.", ex);
                        throw;
                    }
                    response.Close();
                }
                if (FtpPath != MasquaradePath(FtpPath))
                    return DownloadFile(MasquaradePath(FtpPath), LocalFile, true);

                MonitorLog.WriteException("FtpUtility: Can't download file.", ex);
                return false;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteException("FtpUtility: Can't download file.", ex);
                return false;
            }
        }

        /// <summary>
        /// Функция скачивает удаленный файл с FTP сервера на локальный компьютер.
        /// </summary>
        /// <param name="FtpPath">Путь к файлу на удаленном сервере вида "Folder\SubFolder\File.bin".</param>
        /// <returns>Поток скаченного файла</returns>
        public Stream DownloadFile(string FtpPath)
        {
            var request = CreateRequest(FtpPath, WebRequestMethods.Ftp.DownloadFile);
            request.KeepAlive = true;
            try
            {
                using (var response = (FtpWebResponse)request.GetResponse())
                using (var responseStream = response.GetResponseStream())
                {
                    var resultStream = new MemoryStream();
                    try
                    {
                        responseStream.CopyTo(resultStream);
                        resultStream.Seek(0, SeekOrigin.Begin);
                    }
                    catch (OutOfMemoryException)
                    {
                        resultStream.Flush();
                        resultStream.Close();
                        resultStream.Dispose();
                        resultStream = null;
                        MonitorLog.WriteLog(
                            "FtpUtility: Exception was throwed while getting file from FTP server.\n" +
                            "File is too large to copy it to memory.\nFile path is: " + request.RequestUri,
                            MonitorLog.typelog.Warn, true);
                    }
                    responseStream.Close();
                    response.Close();
                    return resultStream;
                }
            }
            catch (WebException ex)
            {
                using (var response = (FtpWebResponse)ex.Response)
                {
                    if (response.StatusCode != FtpStatusCode.ActionNotTakenFileUnavailable)
                    {
                        MonitorLog.WriteException("FtpUtility: Can't download file.", ex);
                        throw;
                    }
                    response.Close();
                }
                if (FtpPath != MasquaradePath(FtpPath))
                    return DownloadFile(MasquaradePath(FtpPath));
                MonitorLog.WriteException("FtpUtility: Can't download file.", ex);
                throw;
            }
            catch (Exception ex)
            {
                MonitorLog.WriteException("FtpUtility: Can't download file.", ex);
                return null;
            }
        }

        /// <summary>
        /// Функция возвращает ответ от Ftp-сервера
        /// </summary>
        /// <param name="FtpPath">Путь к файлу на удаленном сервере вида "Folder\SubFolder\File.bin".</param>
        /// <returns>Ответ Ftp-сервера</returns>
        public FtpWebRequest UnsafeGetDownloadRequest(string FtpPath)
        {
            return CreateRequest(FtpPath, WebRequestMethods.Ftp.DownloadFile);
        }
    }
}
