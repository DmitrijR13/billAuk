
using Microsoft.Office.Interop.Word;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace FormatLibrary
{
    public class FormatMetaData
    {
        public Dictionary<int, string> TableHeader = new Dictionary<int, string>()
        {
            {0, "Номер"},
            {1, "Наименование поля"},
            {2, "Тип поля"},
            {3, "Обязательное заполнение значением"},
            {4, "Правила"}
        };

        public static bool Flag { get; set; }

        private Type FormatType { get; set; }

        public static event ProgressEventHandler mainProgress;

        public FormatMetaData(Type FormatType)
        {
            this.FormatType = FormatType;
        }

        public void Run()
        {
            try
            {
                Flag = true;
                var list = FormatType.GetNestedTypes(BindingFlags.Instance | BindingFlags.Public).ToList();
                IFormatChecker Checker = null;
                list.ForEach(x =>
                {
                    if (!typeof(IFormatChecker).IsAssignableFrom(x))
                        return;
                    Checker = (IFormatChecker)Create<IFormatChecker>(x);
                });
                var templatesHead = Checker.GetTemplatesHead();
                var bodyDict = Checker.FillTemplates();
                var assembleAttribute =
                    FormatType.GetCustomAttributes(typeof(AssembleAttribute), true).Cast<AssembleAttribute>().Single();
                var formatName = assembleAttribute.FormatName + ", Версия - " + assembleAttribute.Version;
                CreateWordDocument(templatesHead, bodyDict, formatName);
            }
            catch
            {
            }
        }

        private void CreateWordDocument(Dictionary<string, string> headerDict, Dictionary<string, List<Template>> bodyDict, string formatName)
        {
            try
            {
                var winword = new Application
                {
                    Visible = false
                };
                object missing = Missing.Value;

                var document = winword.Documents.Add(ref missing, ref missing, ref missing, ref missing);
                var paragraph1 = document.Content.Paragraphs.Add(ref missing);
                paragraph1.Range.Text = string.Format(formatName);
                paragraph1.Range.Font.Bold = 1;
                paragraph1.Range.Font.Size = 15f;
                paragraph1.Range.Font.Name = "verdana";

                paragraph1.Range.InsertParagraphAfter();
                var num = 0;
               
                foreach (KeyValuePair<string, string> keyValuePair in headerDict)
                {
                    var para1 = document.Content.Paragraphs.Add(ref missing);
  
                    para1.Range.Font.Bold = 1;
                    para1.Range.Font.Name = "verdana";
                    para1.Range.Font.Size = 12f;
 
                    para1.Range.Text = string.Format("");
                    para1.Range.InsertParagraphAfter();

                    para1.Range.Font.Name = "verdana";
                    para1.Range.Font.Size = 12f;
                    para1.Range.Font.Name = "verdana";
                    para1.Range.Text = string.Format("Секция {0}: {1}", keyValuePair.Key, keyValuePair.Value);
                    para1.Range.InsertParagraphAfter();
                    var index = 1;
                    SetProgress(num / (Decimal)headerDict.Count, 0, null);
                    ++num;
                    var list = bodyDict[keyValuePair.Key];
                    var firstTable = document.Tables.Add(para1.Range, list.Count + 1, 5, ref missing, ref missing);

                    firstTable.Borders.Enable = 1;
                    foreach (var cell in from Row row in firstTable.Rows from Cell cell in row.Cells select cell)
                    {
                        //Header
                        if (cell.RowIndex == 1)
                        {
                            cell.Range.Text = TableHeader[cell.ColumnIndex - 1];
                            cell.Range.Font.Bold = 1;
                            cell.Range.Font.Name = "verdana";
                            cell.Range.Font.Size = 10f;
                            cell.Shading.BackgroundPatternColor = WdColor.wdColorGray25;
                            cell.VerticalAlignment = WdCellVerticalAlignment.wdCellAlignVerticalCenter;
                            cell.Range.ParagraphFormat.Alignment = WdParagraphAlignment.wdAlignParagraphCenter;
                        }
                        else
                        {
                            cell.Range.Font.Bold = 0;
                            //Rows
                            cell.Range.Font.Size = 10f;
                            cell.Range.Text = list[cell.RowIndex - 2].GetTemplateFieldValue(cell.ColumnIndex - 1, bodyDict, headerDict, index);
                            if (cell.ColumnIndex - 1 == 0) index++;
                        }
                    }
                }
                //Save the document
                object filename = Path.Combine(GetPath(), string.Format("Мета данные формата - '{0}'.docx", formatName));
                document.SaveAs(ref filename);
                document.Close(ref missing, ref missing, ref missing);
                document = null;
                winword.Quit(ref missing, ref missing, ref missing);
                winword = null;
                SetProgress(num / headerDict.Count, 0, filename.ToString());
                Flag = false;
            }
            catch (Exception ex)
            {
                Flag = false;
            }
        }

        public string GetPath()
        {
            return Directory.CreateDirectory(string.Format("{0}\\Download\\{1}\\{2}\\{3}", (object)new FileInfo(Assembly.GetEntryAssembly().Location).Directory, (object)DateTime.Now.Year, (object)DateTime.Now.Month, (object)DateTime.Now.Day)).FullName;
        }

        protected IFormatBase Create<T>(Type type) where T : IFormatBase
        {
            return (IFormatBase)Activator.CreateInstance(type);
        }

        protected virtual void SetProgress(Decimal progress, int formatID, string link)
        {
            if (mainProgress == null)
                return;
            mainProgress(this, new ProgressArgs(progress, formatID, link));
        }
    }
}
