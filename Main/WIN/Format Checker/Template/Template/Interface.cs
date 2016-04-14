using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Template
{
    /// <summary>
    /// Делегат
    /// </summary>
    /// <typeparam name="T">Возвращаемый тип</typeparam>
    /// <typeparam name="V">Передаваемый тип объекта</typeparam>
    /// <param name="dt">Передаваемый объект</param>
    /// <returns></returns>
    public delegate T delegateType<out T, V>(ref V dt);

    /// <summary>
    /// Интерфейс шаблона
    /// </summary>
    public interface IFormat
    {
        delegateType<Returns, object> Check { get; set; }
        delegateType<Returns, object> Load { get; set; }
        delegateType<Returns, object> CreateProtocol { get; set; }
        int formatID { get; set; }
        string result { get; set; }
        decimal progress { get; set; }
        string link { get; set; }
        string FileName { get; set; }
        string Path { get; set; }
        string FormatName { get; set; }
        string Version { get; set; }
        string RegistrationName { get; set; }
        Dictionary<SecionDescription, List<Template>> Dict { get; set; }
    }

    /// <summary>
    /// Тип шаблона 
    /// </summary>
    public class FormatTemplate : Instrumentary, IFormat
    {
        public FormatTemplate(string Version, string FormatName)
        {
            Check = CheckRealization;
            Load = LoadRealization;
            CreateProtocol = CreateProtocolRealization;
            this.Version = Version;
            this.FormatName = FormatName;
        }

        public FormatTemplate(string Version, string FormatName, int formatID)
        {
            Check = CheckRealization;
            Load = LoadRealization;
            CreateProtocol = CreateProtocolRealization;
            this.formatID = formatID;
            this.Version = Version;
            this.FormatName = FormatName;
        }

        public FormatTemplate(string Version, string FormatName, int formatID, string FileName, string Path)
        {
            Check = CheckRealization;
            Load = LoadRealization;
            CreateProtocol = CreateProtocolRealization;
            this.formatID = formatID;
            this.FileName = FileName;
            this.Path = Path;
            this.Version = Version;
            this.FormatName = FormatName;
        }

        public delegateType<Returns, object> Load { get; set; }
        public delegateType<Returns, object> Check { get; set; }
        public delegateType<Returns, object> CreateProtocol { get; set; }
        public Dictionary<SecionDescription, List<Template>> Dict { get; set; }
        delegateType<Returns, object> IFormat.Check
        {
            get { return Check; }
            set { Check = value; }
        }
        delegateType<Returns, object> IFormat.Load
        {
            get { return Load; }
            set { Load = value; }
        }
        delegateType<Returns, object> IFormat.CreateProtocol
        {
            get { return CreateProtocol; }
            set { CreateProtocol = value; }
        }
        Dictionary<SecionDescription, List<Template>> IFormat.Dict
        {
            get { return Dict; }
            set { Dict = value; }
        }
        public string FormatName { get; set; }
        public string Version { get; set; }
        public int formatID { get; set; }
        public string result { get; set; }
        public decimal progress { get; set; }
        public string link { get; set; }
        private List<string> ErrorList { get; set; }
        public string FileName { get; set; }
        public string Path { get; set; }
        public string RegistrationName { get; set; }

        private void SetProgress(decimal progress)
        {
            this.progress = progress;
        }

        private Dictionary<string, string> GetHead()
        {
            return Dict.ToDictionary(d => d.Key.SectionId.ToString(CultureInfo.InvariantCulture), d => d.Key.SectionName);
        }

        private Dictionary<string, List<Template>> GetBody()
        {
            return Dict.ToDictionary(d => d.Key.SectionId.ToString(CultureInfo.InvariantCulture), d => d.Value);
        }

        /// <summary>
        /// Реализация метода Проверки по умолчанию
        /// </summary>
        /// <param name="dt">Возвращаемый объект</param>
        /// <returns></returns>
        private Returns CheckRealization(ref object dt)
        {
            SetProgress(0.1m);
            var templateHead = GetHead();
            var templateDict = GetBody();
            FileName = GetFilesIfWorkWithArchive(Path, FileName);
            var rowCount = GetAllFileRows(Path, FileName).Count();
            var erList = new List<string>();
            erList.AddRange(ErrorList);
            var checkList = dt as Dictionary<string, List<string[]>>;
            var hashset = new HashSet<string>();
            var sets = Template.CreateSets(templateDict, checkList, ref erList);
            try
            {
                if (checkList != null &&
                    (Template.FormatTypes.Contains(checkList["1"][0][0].Trim()) &&
                     (checkList["1"][0][0].Trim() == Version)))
                {
                    try
                    {
                        erList.Add("Проверены следующие секции:");
                        erList.AddRange(checkList.Select(item => string.Format("Секция {0}:{1}", item.Key, templateHead[item.Key])));
                        var k = 0;
                        foreach (var item in checkList)
                        {
                            var curTemplate = templateDict[item.Key];
                            foreach (var itemc in item.Value)
                            {
                                k++;
                                if (itemc.Count() != curTemplate.Count)
                                {
                                    erList.Add(
                                        string.Format(
                                            "Неправильный формат файла загрузки: {2}, количество полей = {0} вместо {1}, строка {3}",
                                            itemc.Count(), curTemplate.Count, templateHead[item.Key], k));
                                    // k += item.Value.Count() - 1;
                                    continue;
                                }
                                if (!hashset.Add(string.Join("|", itemc)))
                                {
                                    erList.Add(
                                        string.Format(
                                            "Ошибка: дублирование строки, строка: {1}, значение: {0}",
                                            string.Join("|", itemc), k));
                                    continue;
                                }
                                erList.AddRange(
                                      itemc.Select((t, i) => curTemplate[i].CheckValues(t, k, item.Key, sets))
                                        .Where(str => str != null));
                                if (k % 5000 == 0) SetProgress(decimal.Round(0.1m + 0.8m / (rowCount) * k, 2));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        return new Returns(false, ex.Message);
                    }
                }
                else
                    erList.Add(string.Format(
                        "Формат проверяемого файла ({0}), не совпадает с форматом проверки (" + Version + ")",
                        checkList["1"][0][0].Trim()));
            }
            catch (Exception ex)
            {
                erList.Add(string.Format(ex.Message));
            }
            dt = ErrorList = erList; //dt служит в качестве передаваемого объекта, который содержит список с ошибками
            SetProgress(0.9m);
            return new Returns();
        }

        /// <summary>
        /// Реализация метода Загрузки данных в объект по умолчанию
        /// </summary>
        /// <param name="dt">Возвращаемый объект</param>
        /// <returns></returns>
        private Returns LoadRealization(ref object dt)
        {
            Returns ret;
            SetProgress(0.05m);
            FileName = GetFilesIfWorkWithArchive(Path, FileName);
            var list = new Dictionary<string, List<string[]>>();
            var strings = GetAllFileRows(Path + "\\" + FileName, out ret);
            if (!ret.result)
            {
                return ret;
            }
            var curList = new List<string[]>();
            var currentSection = 1;
            try
            {
                var i = 0;
                var section = 0;
                string val = null;
                strings.ToList().ForEach(str =>
                {
                    try
                    {
                        val = str;
                        i++;
                        if (str.Trim().Length != 0)
                        {
                            var vals = str.Split(new[] { '|' }, StringSplitOptions.None);
                            section = Convert.ToInt32(vals[0].Trim());
                            if (section != currentSection)
                            {
                                list.Add(currentSection.ToString(CultureInfo.InvariantCulture), curList);
                                curList = new List<string[]>();
                                currentSection = section;
                            }
                            curList.Add(vals);
                        }
                    }
                    catch (Exception ex)
                    {
                        curList.Add(new[] { section.ToString(), val });
                        ErrorList.Add("Ошибка,неверный формат строки, строка " + i);
                        ErrorList.Add("Ошибка:" + ex.Message);
                        ErrorList.Add("Значение:" + val);
                    }

                });
                list.Add(currentSection.ToString(CultureInfo.InvariantCulture), curList);
            }
            catch (Exception ex)
            {
                return new Returns(false, ex.Message);
            }
            dt = list;
            //dt служит в качестве передаваемого объекта, который содержит список, элементы которого загружены из файла
            SetProgress(0.1m);
            return new Returns();
        }

        /// <summary>
        /// Реализация метода Формирования протокола по умолчанию
        /// </summary>
        /// <param name="dt">Возвращаемый объект</param>
        /// <returns></returns>
        private Returns CreateProtocolRealization(ref object dt)
        {
            FileStream file = null;
            string fileName;
            FileName = GetFilesIfWorkWithArchive(Path, FileName);
            var newFileName = FileName.Replace(".txt", "");
            StreamWriter writer = null;
            try
            {
                newFileName += string.Format(" от {0}.txt", DateTime.Now.ToString("dd-MM-yyyy HH-mm-ss"));
                if (!File.Exists(GetPath() + "\\" + newFileName))
                {
                    File.Copy(Path + "\\" + FileName, GetPath() + "\\" + newFileName);
                    CreateFileWithSign(GetPath(), newFileName, string.Join("\n", GetAllFileRows(Path, FileName)));
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
