using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace Template
{

    public abstract class Instrumentary
    {
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
                return File.ReadAllLines(fileName, Encoding.GetEncoding(1251));
            }
            ret = new Returns { result = false, resultMessage = "Файл отсутствует по указанному пути" };
            return null;
        }

        /// <summary>
        /// Получить все строки файла по умолчанию
        /// </summary>
        /// <returns></returns>
        public string[] GetAllFileRows(string Path, string FileName)
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
        public string GetFilesIfWorkWithArchive(string Path, string FileName)
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

        public string[] GetAllFilesIfWorkWithArchive(string Path, string FileName)
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

        public List<string> GetAllFilesRows(string Path, string FileName)
        {
            Returns ret;
            var Files = GetAllFilesIfWorkWithArchive(Path, FileName);
            var strings = new List<string>();
            Files.ToList().ForEach(x => strings.AddRange(GetAllFileRows(Path + "\\" + x, out ret)));
            return strings;
        }
    }

    /// <summary>
    /// Возвращаемый тип
    /// </summary>
    public class Returns
    {
        public Returns(bool result = true, string resultMessage = "")
        {
            this.result = result;
            this.resultMessage = resultMessage;
        }
        /// <summary>
        /// Сообщение
        /// </summary>
        public string resultMessage { get; set; }
        /// <summary>
        /// Рузультат
        /// </summary>
        public bool result { get; set; }
        /// <summary>
        /// Ссылка на файл
        /// </summary>
        public string link { get; set; }
    }
    /// <summary>
    /// Атрибуты класса формата загрузки
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class AssembleAttribute : Attribute
    {
        /// <summary>
        /// Наименование формата
        /// </summary>
        public string FormatName { get; set; }
        /// <summary>
        /// Версия формата
        /// </summary>
        public string Version { get; set; }
        /// <summary>
        /// Имя при регистрации
        /// </summary>
        public string RegistrationName { get; set; }
        /// <summary>
        /// Тип и свойства класс
        /// </summary>
        public Type type { get; set; }
        /// <summary>
        /// Путь до загружаемого файла
        /// </summary>
        public string Path { get; set; }
        /// <summary>
        /// Наименование загружаемого файла
        /// </summary>
        public string FileName { get; set; }
    }
    /// <summary>
    /// Типы данных 
    /// </summary>
    public enum Types
    {
        Int,
        Numeric,
        NumericMoney,
        String,
        DateTime
    }
    public class Template
    {
        public Template(string fieldName, Types type, int? minLength, int? maxLength, bool necessarily = true, int column = 0, string section = null, params string[] correctSymbols)
        {
            this.fieldName = fieldName;
            this.minLength = minLength;
            this.maxLength = maxLength;
            this.type = type;
            //this.mantissa = mantissa;
            this.necessarily = necessarily;
            this.correctSymbols = correctSymbols;
            this.section = section;
            this.column = column;
        }

        public static Dictionary<string, HashSet<string>> CreateSets(Dictionary<string, List<Template>> templateDict, Dictionary<string, List<string[]>> data, ref List<string> error)
        {
            var dict = new Dictionary<string, HashSet<string>>();
            foreach (var template in templateDict)
            {
                foreach (var t in template.Value)
                {
                    if (t.section == null || t.column == 0) continue;
                    if (!data.ContainsKey(t.section)) continue;
                    if (dict.ContainsKey(t.section + t.column)) continue;
                    var hash = new HashSet<string>();
                    foreach (var d in data[t.section])
                    {
                        try
                        {
                            if (!hash.Contains(d[t.column].Trim()))
                            {
                                hash.Add(d[t.column].Trim());
                            }
                            else
                            {
                                error.Add(
                                    "Обнаружена ошибка входных данных.Имеются дублирующиеся записи - " + t.fieldName + "; Значение:" +
                                    d[t.column].Trim());
                            }
                        }
                        catch { }
                    }
                    dict.Add(t.section + t.column, hash);
                }
            }
            return dict;
        }

        /// <summary>
        /// Наименование поля
        /// </summary>
        public string fieldName { get; set; }
        /// <summary>
        /// Тип данных, которым должно являться передаваемое поле
        /// </summary>
        protected Types type { get; set; }
        /// <summary>
        /// Символы которые, могут встречаться во входящем объекте
        /// </summary>
        protected string[] correctSymbols { get; set; }
        /// <summary>
        /// Минимальная длина(для чисел минимальное значение)
        /// </summary>
        protected int? minLength { get; set; }
        /// <summary>
        /// Максимальная длина(для чисел максимальное значение)
        /// </summary>
        protected int? maxLength { get; set; }
        /// <summary>
        /// Обязательность поля
        /// </summary>
        protected bool necessarily { get; set; }
        /// <summary>
        /// Мантисса(в данный момент не используется)
        /// </summary>
        protected int mantissa { get; set; }
        /// <summary>
        /// Порядковый номер зависимой колонки
        /// </summary>
        protected int column { get; set; }
        /// <summary>
        /// Номер зависимой секции
        /// </summary>
        protected string section { get; set; }

        /// <summary>
        /// Проверка объекта
        /// </summary>
        /// <param name="value">Значение объекта</param>
        /// <param name="rowNumber">Номер строки</param>
        /// <param name="section">Номер секции</param>
        /// <returns>Возвращает текст ошибки или null</returns>
        public string CheckValues(string value, int rowNumber, string section, Dictionary<string, HashSet<string>> sets = null)
        {
            if (value.Trim().Length == 0)
                return necessarily ? string.Format("Секция {2},cтрока {1}: Не заполнено обязательное поле:{0}", fieldName, rowNumber, section) : null;
            var newValue = value.Trim();
            correctSymbols.ToList().ForEach(s => newValue = newValue.Replace(s, ""));
            newValue = newValue.Replace("\"", "").Replace("\'", "");
            var customCulture = (CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";
            System.Threading.Thread.CurrentThread.CurrentCulture = customCulture;
            switch (type)
            {
                case Types.Int:
                    {
                        Int64 i_int;
                        if (!Int64.TryParse(newValue, out i_int))
                        {
                            return string.Format("Секция {3},cтрока {1}: Поле имеет неверный формат. Значение = {0}, имя поля: {2}", value, rowNumber, fieldName, section);
                        }
                        if (minLength.HasValue)
                            if (i_int < minLength.Value)
                            {
                                return string.Format("Секция {4},cтрока {1}: Поле имеет неверное значение (меньше {3}). Значение = {0}, имя поля: {2}", value, rowNumber, fieldName, minLength, section);
                            }
                        if (maxLength.HasValue)
                            if (i_int > maxLength.Value)
                            {
                                return string.Format("Секция {4},cтрока {1}: Поле имеет неверное значение (превышает {3}). Значение = {0}, имя поля: {2}", value, rowNumber, fieldName, maxLength, section);
                            }
                    } break;
                case Types.Numeric:
                    {
                        Decimal d_decimal;
                        newValue = newValue.Replace(",", ".");
                        if (!Decimal.TryParse(newValue, out d_decimal))
                        {
                            return string.Format("Секция {3},cтрока {1}: Поле имеет неверный формат. Значение = {0}, имя поля: {2}", value, rowNumber, fieldName, section);
                        }
                        if (Math.Abs(d_decimal - decimal.Truncate(d_decimal)).ToString(CultureInfo.InvariantCulture).Length > 20)
                        {
                            return string.Format("Секция {3},cтрока {1}: Поле имеет неверный формат(дробная часть превышает 20 знаков). Значение = {0}, имя поля: {2}",
                                value, rowNumber, fieldName, section);
                        }
                        if (minLength.HasValue)
                            if (d_decimal < minLength.Value)
                            {
                                return string.Format("Секция {4},cтрока {1}: Поле имеет неверное значение (меньше {3}). Значение = {0}, имя поля: {2}", value, rowNumber, fieldName, minLength, section);
                            }
                        if (maxLength.HasValue)
                            if (d_decimal > maxLength.Value)
                            {
                                return string.Format("Секция {4},cтрока {1}: Поле имеет неверное значение (превышает {3}). Значение = {0}, имя поля: {2}", value, rowNumber, fieldName, maxLength, section);
                            }
                    } break;
                case Types.NumericMoney:
                    {
                        Decimal d_decimal;
                        newValue = newValue.Replace(",", ".");
                        if (!Decimal.TryParse(newValue, out d_decimal))
                        {
                            return string.Format("Секция {3},cтрока {1}: Поле имеет неверный формат. Значение = {0}, имя поля: {2}", value, rowNumber, fieldName, section);
                        }
                        if (Math.Abs(d_decimal - decimal.Truncate(d_decimal)).ToString(CultureInfo.InvariantCulture).Length > 4)
                        {
                            return string.Format("Секция {3},cтрока {1}: Поле имеет неверный формат(дробная часть превышает 2 знака). Значение = {0}, имя поля: {2}",
                                value, rowNumber, fieldName, section);
                        }
                        if (minLength.HasValue)
                            if (d_decimal < minLength.Value)
                            {
                                return string.Format("Секция {4},cтрока {1}: Поле имеет неверное значение (меньше {3}). Значение = {0}, имя поля: {2}", value, rowNumber, fieldName, minLength, section);
                            }
                        if (maxLength.HasValue)
                            if (d_decimal > maxLength.Value)
                            {
                                return string.Format("Секция {4},cтрока {1}: Поле имеет неверное значение (превышает {3}). Значение = {0}, имя поля: {2}", value, rowNumber, fieldName, maxLength, section);
                            }
                    } break;
                case Types.String:
                    {
                        newValue = newValue.Replace("«", "\"").Replace("»", "\"").Replace("'", "\"");
                        if (newValue.Length > maxLength)
                        {
                            return string.Format("Секция {4},cтрока {1}: Длина текста превышает заданный формат ({3}). Значение = {0}, имя поля: {2}", value, rowNumber, fieldName, maxLength, section);
                        }
                    } break;
                case Types.DateTime:
                    {
                        DateTime date;
                        if (!DateTime.TryParse(newValue, out date))
                        {
                            return string.Format("Секция {3},cтрока {1}: Поле имеет неверный формат. Значение = {0}, имя поля: {2}", value, rowNumber, fieldName, section);
                        }
                    } break;
            }
            string l = null;
            try
            {
                if (column == 0 && this.section == null) return null;
                if (!sets[this.section + column].Contains(value))
                    l = string.Format(
                        "Секция {0},cтрока {1}: Поле не связано не с одним значением. Значение = {2}, имя поля: {3}",
                        section, rowNumber, value, fieldName);
            }
            catch
            {
            }
            return l;
        }

        public static string[] FormatTypes =
        {
            "0.11",
            "0.9",
            "1.0",
            "1.2.1",
            "1.3.2",
            "1.3.4",
            "1.3.5",
            "1.3.6",
            "1.3.7",
            "1.3.8",
            "1.3.5.1",
            "1.3.5.3"
        };

        public string GetTemplateFieldValue(int field, Dictionary<string, List<Template>> body, Dictionary<string, string> header, int number)
        {
            switch (field)
            {
                case 0:
                    {
                        return number.ToString();
                    }
                case 1:
                    {
                        return fieldName;
                    } break;
                case 2:
                    {
                        switch (type)
                        {
                            case Types.Int:
                                {
                                    return "Целое";
                                }
                            case Types.Numeric:
                                {
                                    return "Число";
                                }
                            case Types.NumericMoney:
                                {
                                    return "Число в денежном формате";
                                }
                            case Types.String:
                                {
                                    return string.Format("Текст({0})", maxLength);
                                }
                            case Types.DateTime:
                                {
                                    return "Дата";
                                }
                        }
                    } break;
                case 3:
                    {
                        return necessarily ? "Да" : "Нет";
                    }
                case 4:
                    {
                        var str = new StringBuilder();
                        switch (type)
                        {
                            case Types.Int:
                            case Types.NumericMoney:
                            case Types.Numeric:
                                {
                                    if (minLength != null) str.AppendLine(string.Format("Минимальное значение больше или равно {0}.", minLength));
                                    if (maxLength != null) str.AppendLine(string.Format("Максимальное значение меньше или равно {0}.", maxLength));
                                } break;
                            case Types.String:
                                {
                                    if (minLength != null) str.AppendLine(string.Format("Минимальная длина больше или равна {0}.", minLength));
                                    if (maxLength != null) str.AppendLine(string.Format("Максимальная длина меньше или равна {0}.", maxLength));
                                } break;
                            case Types.DateTime:
                                break;
                        }
                        if (section != null)
                        {
                            str.AppendLine(string.Format("Поле связано с полем '{0}' секции '{1}'.", body[section][column].fieldName, header[section]));
                        }
                        return str.ToString();
                    } break;
            }
            return "";
        }
    }

    public class FormatDescription
    {
        public string RegistrationName { get; set; }
        public string FormatName { get; set; }
        public string Version { get; set; }
        public Dictionary<SecionDescription, List<Template>> Dict { get; set; }
    }

    public class SecionDescription
    {
        public string SectionName { get; set; }
        public int SectionId { get; set; }
    }

}
