using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace FormatLibrary
{
    public class SplitFile
    {
        private class Section
        {
            public int section_id { get; set; }//номер секции
            public string section_name { get; set; }//Наименование секции
            public int section_start { get; set; }//Номер строки начала секции
            public int section_end { get; set; }//Последняя строка секции
        }
        public event ProgressEventHandler mainProgress;
        private string type { get; set; }
        private string Path { get; set; }//Путь до файла
        private string FileName { get; set; }//Имя файла
        private string SaveFileName { get; set; }//Имя файла
        private Type Format { get; set; }//Формат загружаемого файла
        private Dictionary<string, string> templatesHead;//Шаблон с именами заголовков
        private Dictionary<string, List<Template>> bodyDict;//Шаблон с данными формата
        private List<Section> listSect = new List<Section>();//Список секций файла
        private int lineCount { get; set; }//Количество строк файла
        public SplitFile(string type, string Path, string FileName, string SaveFileName, Type Format)
        {
            this.Path = Path;
            this.FileName = FileName;
            this.SaveFileName = SaveFileName;
            this.type = type;
            if (Format.Name == "PortalGovServ11" || Format.Name == "PortalGovServ1")
            {
                MessageBox.Show(@"Режим для данного типа формата не поддерживается");
                return;
            }
            if (GetFilesIfWorkWithArchive(Path, FileName) == null)
                return;
            this.Format = Format;
            var list = Format.GetNestedTypes(BindingFlags.Instance | BindingFlags.Public).ToList();
            IFormatChecker Checker = null;
            list.ForEach(x =>
            {
                if (!typeof(IFormatChecker).IsAssignableFrom(x))
                    return;
                Checker = (IFormatChecker)Create<IFormatChecker>(x);
            });
            templatesHead = Checker.GetTemplatesHead();
            bodyDict = Checker.FillTemplates();
        }
        protected IFormatBase Create<T>(Type type) where T : IFormatBase
        {
            return (IFormatBase)Activator.CreateInstance(type);
        }

        //Разбитие файла 
        public void Split()
        {
            if (!SectionAnalyze()) return;
            var newDir = Directory.CreateDirectory(System.IO.Path.Combine(Path, FileName.Split('.').First() + DateTime.Now.Ticks)).FullName;
            SetProgress(0.1m, null);
            using (var file = new FileStream(System.IO.Path.Combine(Path, FileName), FileMode.Open))
            {
                using (var reader = new StreamReader(file, Encoding.GetEncoding(1251)))
                {
                    var counter = 1;
                    foreach (var section in listSect)
                    {
                        using (var file_in = new FileStream(System.IO.Path.Combine(newDir, section.section_id + ".sec"), FileMode.Create))
                        {
                            using (var writer = new StreamWriter(file_in, Encoding.GetEncoding(1251)))
                            {
                                while (counter <= section.section_end)
                                {
                                    writer.WriteLine(reader.ReadLine());
                                    if (counter % 5000 == 0) SetProgress(decimal.Round(0.1m + 0.8m / (lineCount) * counter, 2), null);
                                    counter++;
                                }
                                writer.Flush();
                                writer.Close();
                            }
                            file_in.Close();
                        }
                    }
                    reader.Close();
                }
                file.Close();
            }
            var stringNames = new string[listSect.Count];
            for (var i = 0; i < listSect.Count; i++)
            {
                stringNames[i] = System.IO.Path.Combine(newDir, listSect[i].section_id + ".sec");
            }
            var saveName = System.IO.Path.Combine(Path,
                SaveFileName + "от " + DateTime.Now.ToString("dd.MM.yyyy") + "." + type);
            try
            {
                Archive.GetInstance(saveName)
                    .Compress(saveName, stringNames, true, (DateTime.Now.Day +
                                                            DateTime.Now.Month * 100 + DateTime.Now.Year * 1000).ToString(
                                                                CultureInfo.InvariantCulture));
            }
            finally
            {
                if (File.Exists(System.IO.Path.Combine(Path, FileName)))
                    File.Delete(System.IO.Path.Combine(Path, FileName));
                if (Directory.Exists(newDir))
                    Directory.Delete(newDir);
            }
            SetProgress(1m, saveName);
        }

        public string GetFilesIfWorkWithArchive(string Path, string FileName)
        {
            string file = null;
            try
            {
                var tempPath =
                    Directory.CreateDirectory(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "FormatChecker"));
                try
                {
                    file =
                        Archive.GetInstance(Path + FileName)
                            .Decompress(Path + FileName, tempPath.FullName)
                            .FirstOrDefault();
                }
                catch (Exception ex)
                {
                    if (!File.Exists(System.IO.Path.Combine(tempPath.FullName, FileName)))
                        File.Copy(System.IO.Path.Combine(Path, FileName),
                            System.IO.Path.Combine(tempPath.FullName, FileName));
                    file = FileName;
                }
                this.Path = tempPath.FullName + "\\";
            }
            catch
            {
                MessageBox.Show(@"Указанного файла по заданному пути не найдено");
            }
            return file;
        }
        public string[] ReadLines()
        {
            string[] str = null;
            try
            {
                str = File.ReadAllLines(System.IO.Path.Combine(Path, FileName), Encoding.GetEncoding(1251));
            }
            catch
            {
                MessageBox.Show(@"Указанный файл не соответствует формату");
            }
            return str;
        }

        public bool SectionAnalyze()
        {
            var headers = templatesHead;
            listSect = new List<Section>();
            var strings = ReadLines();
            if (strings == null)
                return false;
            lineCount = strings.Count();
            var dict = new Dictionary<string, int>();
            for (var i = 0; i < strings.Count(); i++)
            {
                var section = strings[i].Split('|')[0];
                if (dict.ContainsKey(section))
                {
                    dict[section] = ++dict[section];
                }
                else
                    dict.Add(section, 1);
            }
            var count = 1;
            try
            {
                foreach (var d in dict)
                {
                    listSect.Add(new Section
                    {
                        section_id = Convert.ToInt32(d.Key),
                        section_name = headers[d.Key],
                        section_start = d.Key == "1" ? 1 : count,
                        section_end = count + d.Value - 1
                    });
                    count += d.Value;
                }
            }
            catch
            {
                MessageBox.Show("Неверный формат файла");
                return false;
            }
            return true;
        }

        protected virtual void SetProgress(Decimal progress, string link)
        {
            if (mainProgress == null)
                return;
            mainProgress(this, new ProgressArgs(progress, 0, link));
        }
    }
}
