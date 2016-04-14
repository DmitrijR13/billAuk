using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace FormatLibrary
{
    /// <summary>
    /// Класс предоставляющий методы для работы с БД
    /// </summary>
    public abstract class Instrumentary
    {
        public string Path { get; set; }
        public string FileName { get; set; }
        public string[] allLines { get; set; }

        /// <summary>
        /// Функция создающая цифровую подпись для файла 
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public string CreateShaSignature(string str)
        {
            var result = "";
            try
            {
                //Create a new instance of RSACryptoServiceProvider.
                using (var rsa = new RSACryptoServiceProvider())
                {
                    //The hash to sign.
                    byte[] hash;
                    using (var sha256 = SHA256.Create())
                    {
                        var bytes = new byte[str.Length * sizeof(char)];
                        Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
                        hash = sha256.ComputeHash(bytes);
                    }
                    //Create an RSASignatureFormatter object and pass it the 
                    //RSACryptoServiceProvider to transfer the key information.
                    var RSAFormatter = new RSAPKCS1SignatureFormatter(rsa);

                    //Set the hash algorithm to SHA256.
                    RSAFormatter.SetHashAlgorithm("SHA256");

                    //Create a signature for HashValue and return it.
                    byte[] SignedHash = RSAFormatter.CreateSignature(hash);
                    result = Encoding.Default.GetString(SignedHash);
                }
            }
            catch (CryptographicException e)
            {
                Console.WriteLine(e.Message);
            }
            return result;
        }

        /// <summary>
        /// Создать файл цифровой подписи
        /// </summary>
        /// <param name="path">Путь к файлу</param>
        /// <param name="fileName">Имя загруженного файла</param>
        /// <param name="SignValue">Ключ</param>
        public virtual void CreateFileWithSign(string path, string fileName, string SignValue)
        {
            FileStream file = null;
            StreamWriter writer = null;
            try
            {
                file = new FileStream(path + "\\" + fileName.Replace(".txt", ".sign.txt"), FileMode.OpenOrCreate);
                writer = new StreamWriter(file);
                var sign = CreateShaSignature(SignValue);
                writer.WriteLine(sign);
            }
            finally
            {
                if (writer != null)
                {
                    writer.Flush();
                    writer.Close();
                }
                if (file != null)
                    file.Close();
            }
        }

        /// <summary>
        /// Получить все строки укакзанного файла 
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public string[] GetAllFileRows(string fileName, out Returns ret)
        {
            if (File.Exists(fileName))
            {
                ret = new Returns { result = true, resultMessage = "Выполнено" };
                return allLines = File.ReadAllLines(fileName, Encoding.GetEncoding(1251));
            }
            ret = new Returns { result = false, resultMessage = "Файл отсутствует по указанному пути" };
            return null;
        }

        /// <summary>
        /// Получить все строки файла по умолчанию
        /// </summary>
        /// <returns></returns>
        public string[] GetAllFileRows()
        {
            Returns ret;
            return GetAllFileRows(Path + "\\" + FileName, out ret);
        }

        /// <summary>
        /// Путь до директории с файлами
        /// </summary>
        /// <returns>Директория с файлами</returns>
        public string GetPath()
        {
            var parentDir = (new FileInfo(Assembly.GetEntryAssembly().Location)).Directory;
            var directory = Directory.CreateDirectory(string.Format("{0}\\Download\\{1}\\{2}\\{3}",
                parentDir, DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day));
            return directory.FullName;
        }
        /// <summary>
        /// Получить наименование вложенного в архив файла, если прикрепленный файл является архивом
        /// </summary>
        /// <returns></returns>
        public string GetFilesIfWorkWithArchive()
        {
            string file;
            try
            {
                file = Archive.GetInstance(Path + FileName).Decompress(Path + FileName, Path).FirstOrDefault();
            }
            catch
            {
                file = FileName;
            }
            return file;
        }

        public string[] GetAllFilesIfWorkWithArchive()
        {
            string[] files;
            try
            {
                files = Archive.GetInstance(Path + FileName).Decompress(Path + FileName, Path);
            }
            catch
            {
                files = new[] { FileName };
            }
            return files;
        }

        public List<string> GetAllFilesRows()
        {
            Returns ret;
            var Files = GetAllFilesIfWorkWithArchive();
            var strings = new List<string>();
            Files.ToList().ForEach(x => strings.AddRange(GetAllFileRows(Path + "\\" + x, out ret)));
            return strings;
        }

        public Returns CreateProtocolIns(ref object dt)
        {
            FileStream file = null;
            string fileName;
            FileName = GetFilesIfWorkWithArchive();
            var newFileName = FileName.Replace(".txt", "");
            StreamWriter writer = null;
            try
            {
                newFileName += string.Format(" от {0}.txt", DateTime.Now.ToString("dd-MM-yyyy HH-mm-ss"));
                if (!File.Exists(GetPath() + "\\" + newFileName))
                {
                    File.Copy(Path + "\\" + FileName, GetPath() + "\\" + newFileName);
                    CreateFileWithSign(GetPath(), newFileName, string.Join("\n", GetAllFileRows()));
                }
                fileName = string.Format("{0}\\{1}", GetPath(),
                    "Протокол сформированный при проверке файла '" + newFileName.Replace(".txt", "") + "'.txt");
                if (File.Exists(fileName))
                    File.Delete(fileName);
                file = new FileStream(fileName, FileMode.CreateNew);
                writer = new StreamWriter(file);
                var list = dt as List<string>;
                if (list != null)
                    foreach (var str in list)
                    {
                        writer.WriteLine(str);
                    }
            }
            catch (Exception ex)
            {
                dt = null;
                return new Returns(false, ex.Message);
            }
            finally
            {
                if (writer != null)
                {
                    writer.Flush();
                    writer.Close();
                }
                if (file != null)
                    file.Close();
            }
            dt = fileName; //В данном случае dt служит для возвращения ссылки на созданный файл протокола
            return new Returns();
        }
    }
}
