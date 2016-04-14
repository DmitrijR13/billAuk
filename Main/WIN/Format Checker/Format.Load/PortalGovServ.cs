using FormatLibrary;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Format.Load
{
    [Assemble(FormatName = "Формат данных для портала гос.услуг", RegistrationName = "PortalGovServ", Version = "1.0")]
    public class PortalGovServ : IFormat
    {
        /// <summary>
        /// Класс проверки формата
        /// </summary>
        public class FormatChecker : IFormatChecker
        {
            /// <summary>
            /// Функция проверки данных
            /// </summary>
            /// <param name="dt">Передаваемый объект</param>
            /// <returns>Возвращаемый результат</returns>
            public override Returns CheckData(ref object dt)
            {
                SetProgress(0.1m);
                var templateDict = FillTemplates();
                var templateHead = GetTemplatesHead();
                FileName = GetFilesIfWorkWithArchive();
                var rowCount = GetAllFileRows().Count();
                var erList = new List<string>();
                erList.AddRange(err);
                var checkList = dt as Dictionary<string, List<string[]>>;
                var hashset = new HashSet<string>();
                var sets = Template.CreateSets(templateDict, checkList, ref erList);
                erList.Add(programVersion);
                try
                {
                    if (checkList != null &&
                        (Template.FormatTypes.Contains(checkList["1"][0][0].Trim()) &&
                         (checkList["1"][0][0].Trim() == "0.11" || checkList["1"][0][0].Trim() == "1.0")))
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
                                          itemc.Select((t, i) => curTemplate[i].CheckValues(i,t, k, item.Key, sets))
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
                            "Формат проверяемого файла ({0}), не совпадает с форматом проверки (0.9)",
                            checkList["1"][0][0].Trim()));
                }
                catch (Exception ex)
                {
                    erList.Add(string.Format(ex.Message));
                }
                dt = erList; //dt служит в качестве передаваемого объекта, который содержит список с ошибками
                SetProgress(0.9m);
                return new Returns();
            }

            public override Dictionary<string, string> GetTemplatesHead()
            {
                return new Dictionary<string, string>
                {
                  {"1","Информационное описание"},
                  {"2","Характеристики жилого фонда"},
                  {"3","Начисления и расходы по услугам"},
                  {"4","Показания счетчиков"},
                  {"5","Платежные реквизиты"},
                  {"6","Оплаты"},
                  {"7","Информация органов социальной защиты"}
                };
            }

            public override Dictionary<string, List<Template>> FillTemplates()
            {
                #region Заполнение шаблона

                var allList = new Dictionary<string, List<Template>>
                    {
                        {
                            "1", new List<Template>
                            {
                                new Template("Версия формата", Types.String, null, 10),
                                new Template("Наименование организации-отправителя ", Types.String, null, 40),
                                new Template("Подразделение организации-отправителя", Types.String, null, 40, false),
                                new Template("ИНН организации-отправителя", Types.String, null, 12),
                                new Template("КПП организации-отправителя", Types.String, null, 9, false),
                                new Template("Код расчетного центра", Types.Int, null,null),
                                new Template("№ файла", Types.Int, 1, null),
                                new Template("Дата файла", Types.DateTime, 0, 14),
                                new Template("Телефон отправителя", Types.String, null, 20),
                                new Template("ФИО отправителя", Types.String, null, 80),
                                new Template("Месяц и год начислений", Types.DateTime, 0, 14),
                                new Template("Количество выгруженных лицевых счетов", Types.Int, 0, null),
                                new Template("Дата начала работы системы", Types.DateTime, 0, 14)
                            }
                        },
                         {
                            "2", new List<Template>
                            {
                                new Template("Тип строки", Types.Int, 2, 2),
                                new Template("Месяц и год начисления", Types.DateTime, 0,14),
                                new Template("Код расчетного центра", Types.Int, null, null),
                                new Template("Платежный код (или номер лицевого счета) однозначно идентифицирующий абонентая", Types.Int, null, null),
                                new Template("Номер лицевого счета", Types.Int, null, 9, false),
                                new Template("Город", Types.String, null,40),
                                new Template("Район", Types.String, null,40),
                                new Template("Улица", Types.String, null,40),
                                new Template("Номер дома", Types.String, null,10),
                                new Template("Комплекс", Types.String, null,10,false),
                                new Template("Номер корпуса", Types.String, null,10),
                                new Template("Номер квартиры", Types.String, null,10),
                                new Template("Номер комнаты", Types.String, null,40,false),
                                new Template("Номер подъезда", Types.Int, null,null,false),
                                new Template("Ф.И.О. Квартиросъемщика / Собственника / Нанимателя", Types.String, null,100,false),
                                new Template("Управляющая компания", Types.String, null,50,false),
                                new Template("Комфортность", Types.Int, 0,2),
                                new Template("Приватизировано", Types.Int, 0,2),
                                new Template("Этаж", Types.Int, 1,null,false),
                                new Template("Квартир на лестничной клетке", Types.Int, 1,null,false),
                                new Template("Общая площадь квартиры", Types.Numeric, null,null,false),
                                new Template("Жилая площадь квартиры", Types.Numeric, null,null,false),
                                new Template("Отапливаемая площадь квартиры", Types.Numeric, null,null,false),
                                new Template("Общая площадь дома", Types.Numeric, null,null,false),
                                new Template("Площадь мест общего пользования дома", Types.Numeric, null,null,false),
                                new Template("Отапливаемая площадь дома", Types.Numeric, null,null,false),
                                new Template("Количество жильцов", Types.Int, 0,null,false),
                                new Template("Количество временно выбывших", Types.Int, 0,null,false),
                                new Template("Количество временно прибывших", Types.Int, 0,null,false),
                                new Template("Количество комнат", Types.Int, 1,null,false)
                            }
                        },
                         {
                            "3", new List<Template>
                            {
                                new Template("Тип строки", Types.Int, 3, 3),
                                new Template("Месяц и год начисления", Types.DateTime, 0,14),
                                new Template("Код расчетного центра", Types.Int, null, null),
                                new Template("Платежный код (или номер лицевого счета) однозначно идентифицирующий абонентая", Types.Int, null, null),
                                new Template("Наименование услуги", Types.String, null, 100),
                                new Template("Единица измерения", Types.String, null,20),
                                new Template("Порядковый номер услуги в ЕПД", Types.Int, null,null),
                                new Template("Код услуги", Types.Int, null,null,false),
                                new Template("Код базовой услуги (если услуга объединяется в ЕПД)", Types.Int, null,null,false),
                                new Template("Группа услуг", Types.String, null,40,false),
                                new Template("Тариф", Types.Numeric, null,null),
                                new Template("Расход по услуге", Types.Numeric, null,null),
                                new Template("Расход ОДН по услуге", Types.Numeric, null,null,false),
                                new Template("Расход по ИПУ", Types.Numeric, null,null,false),
                                new Template("Расход по нормативу", Types.Numeric, null,null,false),
                                new Template("Расход по дому", Types.Numeric, null,null,false),
                                new Template("Расход по квартирам дома", Types.Numeric, null,null,false),
                                new Template("Расход по квартирам дома с ИПУ", Types.Numeric, null,null,false),
                                new Template("Расход по квартирам дома без ИПУ", Types.Numeric, null,null,false),   
                                new Template("Расход по нежилым помещениям", Types.Numeric, null,null,false),  
                                new Template("Расход по лифтам (для услуг электроснабжения)", Types.Numeric, null,null,false),
                                new Template("Расход по дому ОДН", Types.Numeric, null,null,false),
                                new Template("Расход по общедомовым приборам учета", Types.Numeric, null,null,false),
                                new Template("Начислено по тарифу", Types.Numeric, null,null),
                                new Template("Начислено по тарифу с учетом недопоставки", Types.Numeric, null,null),
                                new Template("Сумма недопоставки", Types.Numeric, null,null),
                                new Template("Расход по недопоставкеи", Types.Numeric, null,null),
                                new Template("Количество дней недопоставки", Types.Numeric, 0,null),
                                new Template("Сумма перерасчета за предыдущий период", Types.Numeric, null,null),
                                new Template("Сумма изменений в сальдо", Types.Numeric, null,null),
                                new Template("Сумма начисленная к оплате", Types.Numeric, null,null),
                                new Template("Сумма оплат в ЕПД", Types.Numeric, null,null),
                                new Template("Сумма исходящего сальдо", Types.Numeric, null,null),
                                new Template("Сумма входящего сальдо", Types.Numeric, null,null),
                                new Template("Начислено по тарифу ОДН", Types.Numeric, null,null),
                                new Template("Сумма исходящего сальдо ОДН", Types.Numeric, null,null),
                                new Template("Сумма входящего сальдо ОДН", Types.Numeric, null,null),
                                new Template("Перерасчет ОДН", Types.Numeric, null,null),
                                new Template("Изменения в сальдо ОДН", Types.Numeric, null,null),
                                new Template("Начислено к оплате ОДН", Types.Numeric, null,null),
                                new Template("Оплачено в ЕПД ОДН", Types.Numeric, null,null),
                                new Template("Начислено в пределах социального норматива", Types.Numeric, null,null),
                                new Template("Коэффициент коррекции по ИПУ", Types.Numeric, null,null,false),
                                new Template("Коэффициент коррекции по нормативу", Types.Numeric, null,null,false),
                                new Template("Наименование поставщика услуг", Types.String, null,100),
                                new Template("Количество дней оказания услуги", Types.Int, 0,null,false),
                                new Template("Количество дней недопоставки услуги в прошлых периодахом", Types.Int, 0,null,false),
                            }
                        },
                         {
                            "4", new List<Template>
                            {
                                new Template("Тип строки", Types.Int, 4, 4),
                                new Template("Месяц и год начисления", Types.DateTime, 0,14),
                                new Template("Код расчетного центра", Types.Int, null, null),
                                new Template("Платежный код (или номер лицевого счета) однозначно идентифицирующий абонентая", Types.Int, null, null),
                                new Template("Наименование услуги", Types.String, null, 100),
                                new Template("Единица измерения", Types.String, null, 20),
                                new Template("Порядковый номер ПУ в ЕПД", Types.String, null, 40,false),
                                new Template("Тип прибора учета", Types.Int, 1, 3),
                                new Template("Заводской номер прибора учета", Types.String, null, 40),
                                new Template("Уникальный код прибора учета", Types.Int, null, null,false),
                                new Template("Дата учета показания прибора учета в биллинге", Types.DateTime, 0, 14),
                                new Template("Показание прибора учета", Types.Numeric, null,null),
                                new Template("Дата учета предыдущего показания", Types.DateTime, 0,14,false),
                                new Template("Предыдущее показание", Types.Numeric, null,null,false),
                                new Template("Масштабный множитель", Types.Numeric,null,null),
                                new Template("Расход", Types.Numeric,null,null),
                                new Template("Добавочный расход (если счетчик не действовал часть времени)", Types.Numeric,null,null,false),
                                new Template("Расход по нежилым помещениям", Types.Numeric,null,null,false),
                                new Template("Расход на лифты (по электроснабжению)", Types.Numeric,null,null,false),
                                new Template("Место расположения счетчика", Types.String,null,40,false),
                                new Template("Разрядность прибора", Types.Int,null,null),
                                new Template("Название типа прибора учета", Types.String,null,40),
                                new Template("Дата проверки", Types.DateTime,1,14,false),
                                new Template("Дата следующей проверки", Types.DateTime,1,14,false)
                            }
                        },
                         {
                            "5", new List<Template>
                            {
                                new Template("Тип строки", Types.Int, 5, 5),
                                new Template("Месяц и год начисления", Types.DateTime, 0,14),
                                new Template("Код расчетного центра", Types.Int, null, null),
                                new Template("Платежный код (или номер лицевого счета) однозначно идентифицирующий абонентая", Types.Int, null, null),
                                new Template("Тип строки", Types.Int, 1,2),
                                new Template("Наименование получателя средств", Types.String, null, 50,false),
                                new Template("Наименование банка получателя средств", Types.String, null, 50,false),
                                new Template("Расчетный счет получателя", Types.String, null, 20,false),
                                new Template("Корреспондентский счет банка получателя", Types.String, null, 20,false),
                                new Template("ИНН банка получателя", Types.String, null, 12,false),
                                new Template("БИК банка получателя", Types.String, null, 9,false),
                                new Template("Адрес получателя", Types.String, null, 100,false),
                                new Template("Телефон получателя", Types.String, null, 20,false),
                                new Template("Емайл получателя", Types.String, null, 50,false),
                                new Template("Примечание получателя", Types.String, null, 100,false),
                                new Template("Наименование исполнителя", Types.String, null, 50,false),
                                new Template("Наименование банка исполнителя", Types.String, null, 50,false),
                                new Template("Расчетный счет исполнителя", Types.String, null, 20,false),
                                new Template("Корреспондентский счет банка исполнителя", Types.String, null, 20,false),
                                new Template("ИНН банка исполнителя", Types.String, null, 12,false),
                                new Template("БИК банка исполнителя", Types.String, null, 9,false),
                                new Template("Адрес исполнителя", Types.String, null, 100,false),
                                new Template("Телефон исполнителя", Types.String, null, 20,false),
                                new Template("Емайл исполнителя", Types.String, null, 50,false),
                                new Template("Примечание исполнителя", Types.String, null, 100,false),
                                new Template("12-символьный код УК", Types.String, null, 12,false),
                                new Template("Свободный текст", Types.String, null, 255,false)
                            }
                        },
                         {
                            "6", new List<Template>
                            {
                                new Template("Тип строки", Types.Int, 6, 6),
                                new Template("Месяц и год начисления", Types.DateTime, 0,14),
                                new Template("Код расчетного центра", Types.Int, null, null),
                                new Template("Платежный код (или номер лицевого счета) однозначно идентифицирующий абонентая", Types.Int, null, null),
                                new Template("Дата платежа", Types.DateTime, 0,14),
                                new Template("Дата учета платежа в биллинговой системе", Types.DateTime, 0, 14),
                                new Template("За какой месяц произведена оплата", Types.DateTime, 0, 14),
                                new Template("Сумма оплаты", Types.NumericMoney, null,null),
                                new Template("Место платежа", Types.String, null,null)
                            }
                        },
                         {
                            "7", new List<Template>
                            {
                                new Template("Тип строки", Types.Int, 7, 7),
                                new Template("Месяц и год начисления", Types.DateTime, 0,14),
                                new Template("Код расчетного центра", Types.Int, null, null),
                                new Template("Платежный код (или номер лицевого счета) однозначно идентифицирующий абонентая", Types.Int, null, null),
                                new Template("ФИО получателя", Types.String, null,60),
                                new Template("Уникальный код статьи финансированияя", Types.Int, null,null),
                                new Template("Наименование Сстатьяи финансирования", Types.String, null,40),
                                new Template("Группа статьи финансирования", Types.String, null,10),
                                new Template("Начисленная сумма", Types.NumericMoney, null,null),
                                new Template("Сумма к выплате", Types.NumericMoney, null,null),
                                new Template("Место выплаты", Types.String, null,100,false),
                                new Template("Дата финансирования субсидии", Types.String, null,100,false),
                                new Template("Входящее сальдо (долг на начало месяца)", Types.Numeric, null,null),
                                new Template("Изменение сальдо", Types.Numeric, null,null),
                                new Template("Начисленная перерасчетом сумма субсидий-льгот получателя", Types.Numeric, null,null),
                            }
                        }
                    };

                return allList;

                #endregion
            }
        }

        /// <summary>
        /// Класс загрузки формата
        /// </summary>
        public class FormatLoader : IFormatLoader
        {
            /// <summary>
            /// Функция загрузки
            /// </summary>
            /// <param name="dt">Передаваемый объект</param>
            /// <returns>Возвращаемый результат</returns>
            public override Returns LoadData(ref object dt)
            {
                Returns ret;
                SetProgress(0.05m);
                var ifWorkWithArchive = GetAllFilesIfWorkWithArchive();
                var list = new Dictionary<string, List<string[]>>();
                var strings = new List<string>();
                ifWorkWithArchive.ToList().ForEach(x => strings.AddRange(GetAllFileRows(Path + "\\" + x, out ret)));
                var curList = new List<string[]>();
                var currentSection = 1;
                try
                {
                    var i = 0;
                    string val = null;
                    strings.ToList().ForEach(str =>
                    {
                        try
                        {
                            val = str;
                            i++;
                            if (str.Trim().Length == 0) return;
                            var vals = str.Split(new[] { '|' }, StringSplitOptions.None);
                            int section;
                            try
                            {
                                section = Convert.ToInt32(vals[0].Trim());
                            }
                            catch
                            {
                                section = 1;
                            }
                            if (section != currentSection)
                            {
                                list.Add(currentSection.ToString(CultureInfo.InvariantCulture), curList);
                                curList = new List<string[]>();
                                currentSection = section;
                            }
                            curList.Add(vals);
                        }
                        catch (Exception ex)
                        {
                            err.Add("Ошибка,неверный формат строки, строка " + i);
                            err.Add("Ошибка:" + ex.Message);
                            err.Add("Значение:" + val);
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
        }

        /// <summary>
        /// Класс формирования протокола
        /// </summary>
        public class FormatProtocolCreator : IFormatProtocolCreator
        {
            /// <summary>
            /// Функция формирования протокола
            /// </summary>
            /// <param name="dt">Передаваемый объект</param>
            /// <returns>Возвращаемый результат</returns>
            public override Returns CreateProtocol(ref object dt)
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
                SetProgress(1m);
                return new Returns();
            }
        }

    }

    [Assemble(FormatName = "Формат данных для портала гос.услуг", RegistrationName = "PortalGovServ1", Version = "0.11")]
    public class PortalGovServ11 : IFormat
    {
        /// <summary>
        /// Класс проверки формата
        /// </summary>
        public class FormatChecker : IFormatChecker
        {
            /// <summary>
            /// Функция проверки данных
            /// </summary>
            /// <param name="dt">Передаваемый объект</param>
            /// <returns>Возвращаемый результат</returns>
            public override Returns CheckData(ref object dt)
            {
                SetProgress(0.1m);
                var templateDict = FillTemplates();
                var templateHead = GetTemplatesHead();
                var ifWorkWithArchive = GetAllFilesIfWorkWithArchive();
                var strings = new List<string>();
                var ret=new Returns();
                ifWorkWithArchive.ToList().ForEach(x => strings.AddRange(GetAllFileRows(Path + "\\" + x, out ret)));
                var rowCount = strings.Count();
                var erList = new List<string>();
                erList.AddRange(err);
                var checkList = dt as Dictionary<string, List<string[]>>;
                var hashset = new HashSet<string>();
                erList.Add(programVersion);
                var sets = Template.CreateSets(templateDict, checkList, ref erList);
                try
                {
                    if (checkList != null &&
                        (Template.FormatTypes.Contains(checkList["1"][0][0].Trim()) &&
                         (checkList["1"][0][0].Trim() == "0.11" || checkList["1"][0][0].Trim() == "1.0")))
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
                                          itemc.Select((t, i) => curTemplate[i].CheckValues(i,t, k, item.Key, sets))
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
                            "Формат проверяемого файла ({0}), не совпадает с форматом проверки (0.9)",
                            checkList["1"][0][0].Trim()));
                }
                catch (Exception ex)
                {
                    erList.Add(string.Format(ex.Message));
                }
                dt = erList; //dt служит в качестве передаваемого объекта, который содержит список с ошибками
                SetProgress(0.9m);
                return new Returns();
            }

            public override Dictionary<string, string> GetTemplatesHead()
            {
                return new Dictionary<string, string>
                {
                  {"1","Информационное описание"},
                  {"2","Характеристики жилого фонда"},
                  {"3","Начисления и расходы по услугам"},
                  {"4","Показания счетчиков"},
                  {"5","Платежные реквизиты"},
                  {"6","Оплаты"},
                  {"7","Информация органов социальной защиты"}
                };
            }

            public override Dictionary<string, List<Template>> FillTemplates()
            {
                #region Заполнение шаблона

                var allList = new Dictionary<string, List<Template>>
                    {
                        {
                            "1", new List<Template>
                            {
                                new Template("Версия формата", Types.String, null, 10),
                                new Template("Наименование организации-отправителя ", Types.String, null, 40),
                                new Template("Подразделение организации-отправителя", Types.String, null, 40, false),
                                new Template("ИНН организации-отправителя", Types.String, null, 12),
                                new Template("КПП организации-отправителя", Types.String, null, 9, false),
                                new Template("Код расчетного центра", Types.Int, null,null),
                                new Template("№ файла", Types.Int, 1, null),
                                new Template("Дата файла", Types.DateTime, 0, 14),
                                new Template("Телефон отправителя", Types.String, null, 20),
                                new Template("ФИО отправителя", Types.String, null, 80),
                                new Template("Месяц и год начислений", Types.DateTime, 0, 14),
                                new Template("Количество выгруженных лицевых счетов", Types.Int, 0, null),
                                new Template("Дата начала работы системы", Types.DateTime, 0, 14)
                            }
                        },
                         {
                            "2", new List<Template>
                            {
                                new Template("Тип строки", Types.Int, 2, 2),
                                new Template("Месяц и год начисления", Types.DateTime, 0,14),
                                new Template("Код расчетного центра", Types.Int, null, null),
                                new Template("Платежный код (или номер лицевого счета) однозначно идентифицирующий абонентая", Types.Int, null, null),
                                new Template("Номер лицевого счета", Types.Int, null, null, false),
                                new Template("Город", Types.String, null,40),
                                new Template("Район", Types.String, null,40),
                                new Template("Улица", Types.String, null,40),
                                new Template("Номер дома", Types.String, null,10),
                                new Template("Комплекс", Types.String, null,10,false),
                                new Template("Номер корпуса", Types.String, null,10),
                                new Template("Номер квартиры", Types.String, null,10),
                                new Template("Номер комнаты", Types.String, null,40,false),
                                new Template("Номер подъезда", Types.Int, null,null,false),
                                new Template("Ф.И.О. Квартиросъемщика / Собственника / Нанимателя", Types.String, null,100,false),
                                new Template("Управляющая компания", Types.String, null,50,false),
                                new Template("Комфортность", Types.Int, 0,2),
                                new Template("Приватизировано", Types.Int, 0,2),
                                new Template("Этаж", Types.Int, 1,null,false),
                                new Template("Квартир на лестничной клетке", Types.Int, 1,null,false),
                                new Template("Общая площадь квартиры", Types.Numeric, null,null,false),
                                new Template("Жилая площадь квартиры", Types.Numeric, null,null,false),
                                new Template("Отапливаемая площадь квартиры", Types.Numeric, null,null,false),
                                new Template("Общая площадь дома", Types.Numeric, null,null,false),
                                new Template("Площадь мест общего пользования дома", Types.Numeric, null,null,false),
                                new Template("Отапливаемая площадь дома", Types.Numeric, null,null,false),
                                new Template("Количество жильцов", Types.Int, 0,null,false),
                                new Template("Количество временно выбывших", Types.Int, 0,null,false),
                                new Template("Количество временно прибывших", Types.Int, 0,null,false),
                                new Template("Количество комнат", Types.Int, 1,null,false),
                                new Template("Текстовый идентификатор банка данных", Types.String, null,10)
                            }
                        },
                         {
                            "3", new List<Template>
                            {
                                new Template("Тип строки", Types.Int, 3, 3),
                                new Template("Месяц и год начисления", Types.DateTime, 0,14),
                                new Template("Код расчетного центра", Types.Int, null, null),
                                new Template("Платежный код (или номер лицевого счета) однозначно идентифицирующий абонентая", Types.Int, null, null),
                                new Template("Наименование услуги", Types.String, null, 100),
                                new Template("Единица измерения", Types.String, null,20),
                                new Template("Порядковый номер услуги в ЕПД", Types.Int, null,null),
                                new Template("Код услуги", Types.Int, null,null,false),
                                new Template("Код базовой услуги (если услуга объединяется в ЕПД)", Types.Int, null,null,false),
                                new Template("Группа услуг", Types.String, null,40,false),
                                new Template("Тариф", Types.Numeric, null,null),
                                new Template("Расход по услуге", Types.Numeric, null,null),
                                new Template("Расход ОДН по услуге", Types.Numeric, null,null,false),
                                new Template("Расход по ИПУ", Types.Numeric, null,null,false),
                                new Template("Расход по нормативу", Types.Numeric, null,null,false),
                                new Template("Расход по дому", Types.Numeric, null,null,false),
                                new Template("Расход по квартирам дома", Types.Numeric, null,null,false),
                                new Template("Расход по квартирам дома с ИПУ", Types.Numeric, null,null,false),
                                new Template("Расход по квартирам дома без ИПУ", Types.Numeric, null,null,false),   
                                new Template("Расход по нежилым помещениям", Types.Numeric, null,null,false),  
                                new Template("Расход по лифтам (для услуг электроснабжения)", Types.Numeric, null,null,false),
                                new Template("Расход по дому ОДН", Types.Numeric, null,null,false),
                                new Template("Расход по общедомовым приборам учета", Types.Numeric, null,null,false),
                                new Template("Начислено по тарифу", Types.Numeric, null,null),
                                new Template("Начислено по тарифу с учетом недопоставки", Types.Numeric, null,null),
                                new Template("Сумма недопоставки", Types.Numeric, null,null),
                                new Template("Расход по недопоставкеи", Types.Numeric, null,null),
                                new Template("Количество дней недопоставки", Types.Numeric, 0,null),
                                new Template("Сумма перерасчета за предыдущий период", Types.Numeric, null,null),
                                new Template("Сумма изменений в сальдо", Types.Numeric, null,null),
                                new Template("Сумма начисленная к оплате", Types.Numeric, null,null),
                                new Template("Сумма оплат в ЕПД", Types.Numeric, null,null),
                                new Template("Сумма исходящего сальдо", Types.Numeric, null,null),
                                new Template("Сумма входящего сальдо", Types.Numeric, null,null),
                                new Template("Начислено по тарифу ОДН", Types.Numeric, null,null),
                                new Template("Сумма исходящего сальдо ОДН", Types.Numeric, null,null),
                                new Template("Сумма входящего сальдо ОДН", Types.Numeric, null,null),
                                new Template("Перерасчет ОДН", Types.Numeric, null,null),
                                new Template("Изменения в сальдо ОДН", Types.Numeric, null,null),
                                new Template("Начислено к оплате ОДН", Types.Numeric, null,null),
                                new Template("Оплачено в ЕПД ОДН", Types.Numeric, null,null),
                                new Template("Начислено в пределах социального норматива", Types.Numeric, null,null),
                                new Template("Коэффициент коррекции по ИПУ", Types.Numeric, null,null,false),
                                new Template("Коэффициент коррекции по нормативу", Types.Numeric, null,null,false),
                                new Template("Наименование поставщика услуг", Types.String, null,100),
                                new Template("Количество дней оказания услуги", Types.Int, 0,null,false),
                                new Template("Количество дней недопоставки услуги в прошлых периодахом", Types.Int, 0,null,false),
                            }
                        },
                         {
                            "4", new List<Template>
                            {
                                new Template("Тип строки", Types.Int, 4, 4),
                                new Template("Месяц и год начисления", Types.DateTime, 0,14),
                                new Template("Код расчетного центра", Types.Int, null, null),
                                new Template("Платежный код (или номер лицевого счета) однозначно идентифицирующий абонентая", Types.Int, null, null),
                                new Template("Наименование услуги", Types.String, null, 100),
                                new Template("Единица измерения", Types.String, null, 20),
                                new Template("Порядковый номер ПУ в ЕПД", Types.String, null, 40,false),
                                new Template("Тип прибора учета", Types.Int, 1, 3),
                                new Template("Заводской номер прибора учета", Types.String, null, 40),
                                new Template("Уникальный код прибора учета", Types.Int, null, null,false),
                                new Template("Дата учета показания прибора учета в биллинге", Types.DateTime, 0, 14),
                                new Template("Показание прибора учета", Types.Numeric, null,null),
                                new Template("Дата учета предыдущего показания", Types.DateTime, 0,14,false),
                                new Template("Предыдущее показание", Types.Numeric, null,null,false),
                                new Template("Масштабный множитель", Types.Numeric,null,null),
                                new Template("Расход", Types.Numeric,null,null),
                                new Template("Добавочный расход (если счетчик не действовал часть времени)", Types.Numeric,null,null,false),
                                new Template("Расход по нежилым помещениям", Types.Numeric,null,null,false),
                                new Template("Расход на лифты (по электроснабжению)", Types.Numeric,null,null,false),
                                new Template("Место расположения счетчика", Types.String,null,40,false),
                                new Template("Разрядность прибора", Types.Int,null,null),
                                new Template("Название типа прибора учета", Types.String,null,40,false),
                                new Template("Дата проверки", Types.DateTime,1,14,false),
                                new Template("Дата следующей проверки", Types.DateTime,1,14,false)
                            }
                        },
                         {
                            "5", new List<Template>
                            {
                                new Template("Тип строки", Types.Int, 5, 5),
                                new Template("Месяц и год начисления", Types.DateTime, 0,14),
                                new Template("Код расчетного центра", Types.Int, null, null),
                                new Template("Платежный код (или номер лицевого счета) однозначно идентифицирующий абонентая", Types.Int, null, null),
                                new Template("Тип строки", Types.Int, 1,2),
                                new Template("Наименование получателя средств", Types.String, null, 50,false),
                                new Template("Наименование банка получателя средств", Types.String, null, 50,false),
                                new Template("Расчетный счет получателя", Types.String, null, 20,false),
                                new Template("Корреспондентский счет банка получателя", Types.String, null, 20,false),
                                new Template("ИНН банка получателя", Types.String, null, 12,false),
                                new Template("БИК банка получателя", Types.String, null, 9,false),
                                new Template("Адрес получателя", Types.String, null, 100,false),
                                new Template("Телефон получателя", Types.String, null, 20,false),
                                new Template("Емайл получателя", Types.String, null, 50,false),
                                new Template("Примечание получателя", Types.String, null, 100,false),
                                new Template("Наименование исполнителя", Types.String, null, 50,false),
                                new Template("Наименование банка исполнителя", Types.String, null, 50,false),
                                new Template("Расчетный счет исполнителя", Types.String, null, 20,false),
                                new Template("Корреспондентский счет банка исполнителя", Types.String, null, 20,false),
                                new Template("ИНН банка исполнителя", Types.String, null, 12,false),
                                new Template("БИК банка исполнителя", Types.String, null, 9,false),
                                new Template("Адрес исполнителя", Types.String, null, 100,false),
                                new Template("Телефон исполнителя", Types.String, null, 20,false),
                                new Template("Емайл исполнителя", Types.String, null, 50,false),
                                new Template("Примечание исполнителя", Types.String, null, 100,false),
                                new Template("12-символьный код УК", Types.String, null, 12,false),
                                new Template("Свободный текст", Types.String, null, 255,false)
                            }
                        },
                         {
                            "6", new List<Template>
                            {
                                new Template("Тип строки", Types.Int, 6, 6),
                                new Template("Месяц и год начисления", Types.DateTime, 0,14),
                                new Template("Код расчетного центра", Types.Int, null, null),
                                new Template("Платежный код (или номер лицевого счета) однозначно идентифицирующий абонентая", Types.Int, null, null),
                                new Template("Дата платежа", Types.DateTime, 0,14),
                                new Template("Дата учета платежа в биллинговой системе", Types.DateTime, 0, 14),
                                new Template("За какой месяц произведена оплата", Types.DateTime, 0, 14),
                                new Template("Сумма оплаты", Types.NumericMoney, null,null),
                                new Template("Место платежа", Types.String, null,null)
                            }
                        },
                         {
                            "7", new List<Template>
                            {
                                new Template("Тип строки", Types.Int, 7, 7),
                                new Template("Месяц и год начисления", Types.DateTime, 0,14),
                                new Template("Код расчетного центра", Types.Int, null, null),
                                new Template("Платежный код (или номер лицевого счета) однозначно идентифицирующий абонента", Types.Int, null, null),
                                new Template("ФИО получателя", Types.String, null,60),
                                new Template("Уникальный код статьи финансированияя", Types.Int, null,null),
                                new Template("Наименование Сстатьяи финансирования", Types.String, null,40),
                                new Template("Группа статьи финансирования", Types.String, null,10),
                                new Template("Начисленная сумма", Types.NumericMoney, null,null),
                                new Template("Сумма к выплате", Types.NumericMoney, null,null),
                                new Template("Место выплаты", Types.String, null,100,false),
                                new Template("Дата финансирования субсидии", Types.String, null,100,false),
                                new Template("Входящее сальдо (долг на начало месяца)", Types.Numeric, null,null),
                                new Template("Изменение сальдо", Types.Numeric, null,null),
                                new Template("Начисленная перерасчетом сумма субсидий-льгот получателя", Types.Numeric, null,null),
                            }
                        }
                    };

                return allList;

                #endregion
            }
        }

        /// <summary>
        /// Класс загрузки формата
        /// </summary>
        public class FormatLoader : IFormatLoader
        {
            /// <summary>
            /// Функция загрузки
            /// </summary>
            /// <param name="dt">Передаваемый объект</param>
            /// <returns>Возвращаемый результат</returns>
            public override Returns LoadData(ref object dt)
            {
                Returns ret;
                SetProgress(0.05m);
                var ifWorkWithArchive = GetAllFilesIfWorkWithArchive();
                var list = new Dictionary<string, List<string[]>>();
                var strings = new List<string>();
                ifWorkWithArchive.ToList().ForEach(x => strings.AddRange(GetAllFileRows(Path + "\\" + x, out ret)));
                var curList = new List<string[]>();
                var currentSection = 1;
                try
                {
                    var i = 0;
                    string val = null;
                    strings.ToList().ForEach(str =>
                    {
                        try
                        {
                            val = str;
                            i++;
                            if (str.Trim().Length == 0) return;
                            var vals = str.Split(new[] { '|' }, StringSplitOptions.None);
                            int section;
                            try
                            {
                                section = Convert.ToInt32(vals[0].Trim());
                            }
                            catch
                            {
                                section = 1;
                            }
                            if (section != currentSection)
                            {
                                list.Add(currentSection.ToString(CultureInfo.InvariantCulture), curList);
                                curList = new List<string[]>();
                                currentSection = section;
                            }
                            curList.Add(vals);
                        }
                        catch (Exception ex)
                        {
                            err.Add("Ошибка,неверный формат строки, строка " + i);
                            err.Add("Ошибка:" + ex.Message);
                            err.Add("Значение:" + val);
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
        }

        /// <summary>
        /// Класс формирования протокола
        /// </summary>
        public class FormatProtocolCreator : IFormatProtocolCreator
        {
            /// <summary>
            /// Функция формирования протокола
            /// </summary>
            /// <param name="dt">Передаваемый объект</param>
            /// <returns>Возвращаемый результат</returns>
            public override Returns CreateProtocol(ref object dt)
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
                SetProgress(1m);
                return new Returns();
            }
        }

    }
}
