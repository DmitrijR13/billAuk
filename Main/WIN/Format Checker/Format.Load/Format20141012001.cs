using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FormatLibrary;

namespace Format.Load
{
    /// <summary>
    /// Класс Загрузчика
    /// Атрибуты "Наименование формата","Версия формата"
    /// </summary>
    [Assemble(FormatName = "Характеристики жилого фонда", Version = "1.3.6", RegistrationName = "Test")]
    public class Format20141012003 : IFormat
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
                var sucList = new List<string>();
                var checkList = dt as Dictionary<string, List<string[]>>;
                var hashset = new HashSet<string>();
                var sets = Template.CreateSets(templateDict, checkList, ref erList);
                sucList.Add(programVersion);
                try
                {
                    if (checkList != null &&
                        (Template.FormatTypes.Contains(checkList["1"][0][1].Trim()) &&
                         checkList["1"][0][1].Trim() == "1.3.6"))
                    {
                        try
                        {
                            sucList.Add(string.Join("|", checkList["1"][0]));
                            sucList.Add("Проверены следующие секции:");
                            sucList.AddRange(checkList.Select(item => string.Format("Секция {0}:{1} ({2} строк)", item.Key, templateHead[item.Key], checkList[item.Key].Count)));
                            var k = 0;
                            foreach (var item in checkList)
                            {
                                var curTemplate = templateDict[item.Key];
                                foreach (var itemc in item.Value)
                                {
                                    k++;
                                    if (!hashset.Add(string.Join("|", itemc)))
                                    {
                                        erList.Add(
                                            string.Format(
                                                "Ошибка: дублирование строки, строка: {1}, значение: {0}",
                                                string.Join("|", itemc), k));
                                        continue;
                                    }
                                    if (itemc.Count() != curTemplate.Count)
                                    {
                                        erList.Add(
                                            string.Format(
                                                "Неправильный формат файла загрузки: {2}, количество полей = {0} вместо {1}, строка {3}",
                                                itemc.Count(), curTemplate.Count, templateHead[item.Key], k));
                                        // k += item.Value.Count() - 1;
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
                            "Формат проверяемого файла ({0}), не совпадает с форматом проверки (1.3.6)",
                            checkList["1"][0][1].Trim()));
                }
                catch (Exception ex)
                {
                    erList.Add(string.Format(ex.Message));
                }
                //Проверка четчиков на переход через ноль
                var counters = new[] { new Counters("Показания индивидуальных приборов учета", "12", 1, 3, 4), new Counters("Показания индивидуальных приборов учета", "10", 1, 3, 4) };
                erList.AddRange(counters.Select(c => c.CheckCounters(checkList)));
                erList = erList.Where(x => x != null).ToList();
                if (erList.Count == 0)
                {
                    erList.Add("\r\nПроверка файла выполнена, ошибок не найдено\r\n");
                }
                else
                    sucList.Add("Ошибки:");
                sucList.AddRange(erList);
                //Формирование итоговых сумм в разрезе договоров
                var calc = new[] { new SumTemplate("6", new[] { 4, 12, 13, 14, 20, 22 }, 2, templateHead["6"]), new SumTemplate("8", new[] { 12, 13 }, 3, templateHead["8"]) };
                sucList.AddRange(calc.Select(c => c.Calculate(checkList, templateDict)));
                dt = sucList; //dt служит в качестве передаваемого объекта, который содержит список с ошибками
                SetProgress(0.9m);
                return new Returns();
            }

            public override Dictionary<string, string> GetTemplatesHead()
            {
                return new Dictionary<string, string>
                {
                    {"1", "Заголовок файла"},
                    {"2", "УК"},
                    {"3", "Дома"},
                    {"4", "Информация о лицевых счетах"},
                    {"5", "Поставщики услуг"},
                    {"6", "Информация об оказываемых услугах"},
                    {"7", "Информация о параметрах лицевых счетов в месяце перерасчета"},
                    {"8", "Информация о перерасчетах начислений по услугам"},
                    {"9", "Информация об общедомовых приборах учета"},
                    {"10", "Показания общедомовых приборов учета"},
                    {"11", "Информация об индивидуальных приборах учета"},
                    {"12", "Показания ИПУ"},
                    {"13", "Перечень выгруженных услуг"},
                    {"14", "Перечень выгруженных муниципальных образований"},
                    {"15", "Информация по проживающим"},
                    {"16", "Перечень выгруженных параметров"},
                    {"17", "Перечень выгруженных типов жилья по газоснабжению"},
                    {"18", "Перечень выгруженных типов жилья по водоснабжению"},
                    {"19", "Перечень выгруженных категорий благоустройства дома"},
                    {"20", "Перечень дополнительных характеристик дома"},
                    {"21", "Перечень дополнительных характеристик лицевого счета"},
                    {"22", "Перечень оплат проведенных по лицевому счету"},
                    {"23", "Перечень недопоставок"},
                    {"24", "Перечень типов недопоставки"},
                    {"25", "Перечень услуг лицевого счета"},
                    {"26", "Пачки реестров"},
                    {"27", "Реестр ЛС"},
                    {"28", "Реестр временно-убывших"},
                    {"29", "УК"},
                    {"30", "Расчетный счет"},
                    {"31", "Соглашения по перечислениям"},
                    {"32", "Распределения оплат"},
                    {"33", "Перекидки"},
                    {"34", "Информация по лицевым счетам с приборами учета"}
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
                            new Template("Номер секции", Types.Int, 0, null),
                            new Template("Версия формата", Types.String, 0, 10),
                            new Template("Тип файла", Types.String, 0, 30),
                            new Template("Наименование организации-отправителя", Types.String, 0, 40),
                            new Template("Подразделение организации-отправителя", Types.String, null, 40, false),
                            new Template("ИНН", Types.String, null, 14, false),
                            new Template("КПП", Types.String, null, 9, false),
                            new Template("№ файла", Types.Int, 1, null),
                            new Template("Дата файла", Types.DateTime, 0, 14),
                            new Template("Телефон отправителя", Types.String, 0, 20),
                            new Template("ФИО отправителя", Types.String, 0, 80),
                            new Template("Месяц и год начислений", Types.DateTime, 0, 12),
                            new Template("Количество записей в файле", Types.Int, 0, null)
                        }
                    },
                    {
                        "2", new List<Template>
                        {
                            //new Template("Номер секции",Types.Int ,0, null),
                            //new Template("Уникальный код УК",Types.Int ,null ,null),
                            //new Template("Наименование УК",Types.String ,0 ,60),
                            //new Template("Юридический адрес",Types.String ,null ,100,false),
                            //new Template("Фактический адрес",Types.String ,null ,100,false),
                            //new Template("ИНН",Types.String ,0 ,20),
                            //new Template("КПП",Types.String ,0 ,20),
                            //new Template("Расчетный счет",Types.String,null,20,false),
                            //new Template("Банк",Types.String,null,100,false),
                            //new Template("БИК банка",Types.String,null,20,false),
                            //new Template("Корреспондентский счет",Types.String,null,20)
                            new Template("Номер секции", Types.Int, 0, null),
                            new Template("Уникальный код ЮЛ", Types.Int, 1, null),
                            new Template("Наименование ЮЛ", Types.String, null, 100),
                            new Template("Сокращенное наименование ЮЛ", Types.String, null, 40),
                            new Template("Юридический адрес", Types.String, null, 100, false),
                            new Template("Фактический адрес", Types.String, null, 100, false),
                            new Template("ИНН", Types.String, 0, 20),
                            new Template("КПП", Types.String, 0, 20),
                            new Template("Телефон руководителя", Types.String, null, 20, false),
                            new Template("Телефон бухгалтерии", Types.String, null, 20, false),
                            new Template("ФИО руководителя", Types.String, null, 100, false),
                            new Template("Должность руководителя", Types.String, null, 40, false),
                            new Template("ФИО бухгалтера", Types.String, null, 100, false),
                            new Template("ОКОНХ1", Types.String, null, 20, false),
                            new Template("ОКОНХ2", Types.String, null, 20, false),
                            new Template("ОКПО", Types.String, null, 20, false),
                            new Template("Должность + ФИО в Р.п.", Types.String, null, 200, false),
                            new Template("Признак того, что предприятие является УК", Types.Int, 0, 1),
                            new Template("Признак того, что предприятие является поставщиком", Types.Int, 0, 1),
                            new Template("Признак того, что предприятие является арендатором/собственником помещений",
                                Types.Int, 0, 1),
                            new Template("Признак того, что предприятие является РЦ ", Types.Int, 0, 1),
                            new Template("Признак того, что предприятие является РСО ", Types.Int, 0, 1),
                            new Template("Признак того, что предприятие является платежным агентом ", Types.Int, 0, 1),
                            new Template("Признак того, что предприятие является субабонентом ", Types.Int, 0, 1)
                        }
                    },
                    {
                        "3", new List<Template>
                        {
                            new Template("Номер секции", Types.Int, 0, null),
                            new Template("УКДС", Types.Int, null, null, false),
                            new Template("Уникальный код дома в системе отправителя", Types.String, 0, 20, true, 2, "3"),
                            //---
                            new Template("Город/район", Types.String, null, 30),
                            new Template("Село/деревня", Types.String, null, 30, false),
                            new Template("Наименование улицы", Types.String, null, 40),
                            new Template("Дом", Types.String, null, 10),
                            new Template("Корпус", Types.String, null, 10, false),
                            new Template("Код ЮЛ, где лежит паспорт дома (УК)", Types.Int, 1, null, false),
                            new Template("Категория благоустроенности (значение из справочника)", Types.String, null, 30,
                                false),
                            new Template("Этажность", Types.Int, 1, null),
                            new Template("Год постройки", Types.DateTime, null, 14, false),
                            new Template("Общая площадь", Types.Numeric, 0, null, false),
                            new Template("Площадь мест общего пользования", Types.Numeric, 0, null, false),
                            new Template("Полезная (отапливаемая площадь)", Types.Numeric, 0, null, false),
                            new Template("Код Муниципального образования (значение из справочника)", Types.Int, 0, null,
                                false),
                            new Template(
                                "Дополнительные характеристики дома (задается в соответствии с правилами заполнения значений параметров)",
                                Types.String, null, 250, false),
                            new Template("Количество строк - лицевой счет", Types.Int, 0, null, false),
                            new Template("Количество строк - общедомовой прибор учета", Types.Int, 0, null, false),
                            new Template("Код улицы КЛАДР", Types.String, null, 30),
                            new Template("Код улицы ФИАС", Types.String, null, 30)
                        }
                    },
                    {
                        "4", new List<Template>
                        {
                            new Template("Номер секции", Types.Int, 0, null),
                            new Template("УКАС", Types.Int, null, null, false),
                            new Template("Уникальный код дома в системе отправителя", Types.String, null, 20, true, 2,
                                "3"),
                            new Template("№ ЛС в системе поставщика", Types.String, null, 20, false),
                            new Template("Тип ЛС (1 – жилая квартира, 2 – субабонент / арендатор)", Types.Int, null,
                                null),
                            new Template("Фамилия квартиросъемщика", Types.String, null, 200, false),
                            new Template("Имя квартиросъемщика", Types.String, null, 40, false),
                            new Template("Отчество квартиросъемщика", Types.String, null, 40, false),
                            new Template("Дата рождения квартиросъемщика", Types.DateTime, null, 14, false),
                            new Template("Квартира", Types.String, null, 10),
                            new Template("Комната лицевого счета", Types.String, null, 3, false),
                            new Template("Дата открытия ЛС", Types.DateTime, null, 14, false),
                            new Template("Основание открытия ЛС", Types.String, null, 100, false),
                            new Template("Дата закрытия ЛС", Types.DateTime, null, 14, false),
                            new Template("Основание закрытия ЛС", Types.String, null, 100, false),
                            new Template("Количество проживающих", Types.Numeric, 0, null),
                            new Template("Количество врем. прибывших жильцов", Types.Numeric, 0, null),
                            new Template("Количество  врем. убывших жильцов", Types.Numeric, 0, null),
                            new Template("Количество комнат", Types.Int, 1, null),
                            new Template("Общая площадь", Types.Numeric, 0, null),
                            new Template("Жилая площадь", Types.Numeric, 0, null, false),
                            new Template("Отапливаемая площадь", Types.Numeric, 0, null, false),
                            new Template("Площадь для найма", Types.Numeric, 0, null, false),
                            new Template("Признак коммунальной квартиры(1-да, 0 –нет)", Types.Int, 0, 1),
                            new Template("Наличие эл. плиты (1-да, 0 –нет)", Types.Int, 0, 1, false),
                            new Template("Наличие газовой плиты (1-да, 0 –нет)", Types.Int, 0, 1, false),
                            new Template("Наличие газовой колонки (1-да, 0 –нет)", Types.Int, 0, 1, false),
                            new Template("Наличие огневой плиты (1-да, 0 –нет)", Types.Int, 0, 1, false),
                            new Template("Код типа жилья по газоснабжению (из справочника)", Types.Int, 0, null, false),
                            new Template("Код типа жилья по водоснабжению (из справочника)", Types.Int, 0, null, false),
                            new Template("Код типа жилья по горячей воде (из справочника)", Types.Int, 0, null, false),
                            new Template("Код типа жилья по канализации (из справочника)", Types.Int, 0, null, false),
                            new Template("Наличие забора из открытой системы отопления (1-да, 0 –нет)", Types.Int, 0, 1,
                                false),
                            new Template(
                                "Дополнительные характеристики ЛС (задается в соответствии с правилами заполнения значений параметров)",
                                Types.String, null, 250, false),
                            new Template("Количество строк - услуга", Types.Int, 0, null),
                            new Template("Количество строк  – параметры в месяце перерасчета лицевого счета", Types.Int,
                                0, null),
                            new Template("Количество строк – индивидуальный прибор учета", Types.Int, 0, null),
                            new Template("Уникальный код ЮЛ", Types.String, 1, null, false, 1, "2"),
                            new Template("Тип владения", Types.String, null, 30, false),
                            new Template("Уникальный код жильца квартиросъемщика", Types.Int, null, null, false, 2, "15"),
                            new Template("Участок", Types.String, null, 60, false),
                            new Template("Код ЮЛ, где лежит паспорт дома", Types.Int, null, null, true, 1, "2")
                        }
                    },
                    {
                        "5", new List<Template>
                        {
                            new Template("Номер секции", Types.Int, 0, null),
                            new Template("Уникальный код договора", Types.Int, 1, null),
                            new Template("Код агента получателя платеже", Types.Int, null, null, true, 1, "2"),
                            new Template("Код ЮЛ принципала", Types.Int, 1, null, true, 1, "2"),
                            new Template("Код поставщика", Types.Int, null, null, true, 1, "2"),
                            new Template("Наименование договора", Types.String, null, 60, false),
                            new Template("Номер договора", Types.String, null, 20, false),
                            new Template("Дата договора", Types.DateTime, null, 14, false),
                            new Template("Комментарий", Types.String, null, 200, false),
                            new Template("Расчетный счет", Types.String, null, 20, true, 1, "30")
                        }
                    },
                    {
                        "6", new List<Template>
                        {
                            new Template("Номер секции", Types.Int, 0, null),
                            new Template("№ ЛС в системе поставщика", Types.String, null, 20, true, 3, "4"),
                            new Template("Код договора", Types.Int, null, null, true, 1, "5"),
                            new Template("Код услуги (из справочника)", Types.Int, null, null,true,1,"13"),
                            new Template("Входящее сальдо (Долг на начало месяца)", Types.NumericMoney, null, null),
                            new Template("Экономически обоснованный тариф", Types.Numeric, null, null),
                            new Template("Процент регулируемого тарифа от экономически обоснованного", Types.Numeric, 0,
                                100),
                            new Template("Регулируемый тариф", Types.Numeric, null, null),
                            new Template("Код единицы измерения расхода (из справочника)", Types.Int, 1, null),
                            new Template("Расход фактический", Types.Numeric, 0, null),
                            new Template("Расход по нормативу", Types.Numeric, 0, null),
                            new Template("Вид расчета по прибору учета(1-по счетчику / 0-без счетчика)", Types.Numeric,
                                0, 1),
                            new Template("Сумма начисления", Types.Numeric, null, null),
                            new Template("Сумма перерасчета начисления за предыдущий период (изменение сальдо)",
                                Types.NumericMoney, null, null),
                            new Template("Сумма дотации", Types.NumericMoney, 0, null, false),
                            new Template("Сумма перерасчета дотации за предыдущий период (за все месяца)",
                                Types.NumericMoney, null, null, false),
                            new Template(
                                "Сумма льготы (Общая сумма льготы по всем категориям льгот для данной услуги)",
                                Types.NumericMoney, 0, null, false),
                            new Template(
                                "Сумма перерасчета льготы за предыдущий период (Общая по всем категориям льгот для данной услуги) (за все месяца)",
                                Types.NumericMoney, null, null, false),
                            new Template("Сумма СМО", Types.NumericMoney, 0, null, false),
                            new Template("Сумма перерасчета  СМО за предыдущий период (за все месяца)",
                                Types.NumericMoney, null, null, false),
                            new Template("Сумма оплаты, поступившие за месяц начислений", Types.NumericMoney, null, null),
                            new Template("Признак недействующей услуги, по которой остались долги ", Types.Int, 0, 1),
                            new Template("Исходящее сальдо (Долг на окончание месяца)", Types.Numeric, null, null),
                            new Template("Количество строк – перерасчетов начисления по услуге", Types.Int, 0, null),
                            new Template("Номер методики расчета", Types.Int, null, null, false),
                            new Template("Платежный код", Types.Numeric, 0, null, false),
                            new Template("Порядковый номер услуги в ЕПД", Types.Int, null, null, false),
                            new Template("Тип суммы к оплате для ЕПД", Types.Int, null, null, false),
                            new Template("Сумма перерасчета", Types.Numeric, null, null, false),
                            new Template("Сумма перекидки", Types.Numeric, null, null, false),
                            new Template("Сумма учтенной недопоставки", Types.Numeric, null, null, false),
                            new Template("Количество часов недопоставки", Types.Numeric, null, null, false)
                        }
                    },
                    {
                        "7", new List<Template>
                        {
                            new Template("Номер секции", Types.Int, 0, null),
                            new Template("Месяц и год перерасчета", Types.DateTime, null, 14),
                            new Template("№ ЛС в системе поставщика", Types.String, null, 20, true, 3, "4"),
                            new Template("Фамилия квартиросъемщика", Types.String, null, 40, false),
                            new Template("Имя квартиросъемщика", Types.String, null, 40, false),
                            new Template("Отчество квартиросъемщика", Types.String, null, 40, false),
                            new Template("Дата рождения квартиросъемщика", Types.DateTime, null, 14, false),
                            new Template("Квартира", Types.String, null, 10, false),
                            new Template("Комната", Types.String, null, 3, false),
                            new Template("Дата открытия ЛС", Types.DateTime, null, 14, false),
                            new Template("Основание открытия ЛС", Types.String, null, 100, false),
                            new Template("Дата закрытия ЛС", Types.DateTime, null, 14, false),
                            new Template("Основание закрытия ЛС", Types.String, null, 100, false),
                            new Template("Количество проживающих", Types.Int, 0, null),
                            new Template("Количество врем. Прибывших жильцов", Types.Int, 0, null),
                            new Template("Количество  врем. Убывших жильцов", Types.Int, 0, null),
                            new Template("Количество комнат", Types.Int, 1, null),
                            new Template("Общая площадь", Types.Numeric, 0, null, false),
                            new Template("Жилая площадь", Types.Numeric, 0, null, false),
                            new Template("Отапливаемая площадь", Types.Numeric, 0, null, false),
                            new Template("Площадь для найма", Types.Numeric, 0, null, false),
                            new Template("Признак коммунальной квартиры(1-да, 0 –нет)", Types.Int, 0, 1, false),
                            new Template("Наличие эл. Плиты (1-да, 0 –нет)", Types.Int, 0, 1, false),
                            new Template("Наличие газовой плиты (1-да, 0 –нет)", Types.Int, 0, 1, false),
                            new Template("Наличие газовой колонки (1-да, 0 –нет", Types.Int, 0, 1, false),
                            new Template("Наличие огневой плиты (1-да, 0 –нет)", Types.Int, 0, 1, false),
                            new Template("Код типа жилья по газоснабжению (из справочника)", Types.Int, 0, null, false),
                            new Template("Код типа жилья по водоснабжению (из справочника)", Types.Int, 0, null, false),
                            new Template("Код типа жилья по горячей воде (из справочника)", Types.Int, 0, null, false),
                            new Template("Код типа жилья по канализации (из справочника)", Types.Int, 0, null, false),
                            new Template("Наличие забора из открытой системы отопления (1-да, 0 –нет)", Types.Int, 0, 1,
                                false),
                            new Template("Дополнительные характеристики ЛС", Types.Int, null, 250, false)
                        }
                    },
                    {
                        "8", new List<Template>
                        {
                            new Template("Номер секции", Types.Int, 0, null),
                            new Template("Месяц и год перерасчета", Types.DateTime, null, 14),
                            new Template("№ ЛС в системе поставщика", Types.String, null, 20, true, 3, "4"),
                            new Template("Код договора", Types.Int, 1, null,true,1,"5"),
                            new Template("Код услуги (из справочника)", Types.Int, 1, null, true, 1, "13"),
                            new Template("Экономически обоснованный тариф", Types.Numeric, null, null, false),
                            new Template("Процент регулируемого тарифа от экономически обоснованного", Types.Numeric, 0,
                                100),
                            new Template("Регулируемый тариф", Types.Numeric, null, null),
                            new Template("Код единицы измерения расхода (из справочника)", Types.Int, 1, null),
                            new Template("Расход фактический", Types.Numeric, null, null),
                            new Template("Расход по нормативу", Types.Numeric, 0, null),
                            new Template("Вид расчета по прибору учета", Types.Int, 0, 1),
                            new Template("Сумма перерасчета начисления за месяц перерасчета", Types.NumericMoney, null,
                                null),
                            new Template("Сумма перерасчета дотации за месяц перерасчета", Types.NumericMoney, null,
                                null),
                            new Template(
                                "Сумма перерасчета льготы за месяц перерасчета (Общая по всем категориям льгот для данной услуги)",
                                Types.NumericMoney, null, null),
                            new Template("Сумма перерасчета СМО за месяц перерасчета", Types.NumericMoney, null, null)
                        }
                    },
                    {
                        "9", new List<Template>
                        {
                            new Template("Номер секции", Types.Int, 0, null),
                            new Template("Уникальный код дома(char)", Types.String, null, 20, false, 2, "3"),
                            new Template("Уникальный код прибора учета в системе поставщика", Types.String, null, 20),
                            new Template("Код услуги (из справочника)", Types.Int, 1, null,true,1,"13"),
                            new Template("Тип услуги (тарифная зона: 1 –дневное потребление, 2 –ночное потребление)",
                                Types.Int, null, null),
                            new Template("Тип счетчика", Types.String, null, 25),
                            new Template("Разрядность прибора", Types.Int, null, null),
                            new Template("Повышающий коэффициент (коэффициент трансформации тока)", Types.Int, null,
                                null),
                            new Template("Заводской номер прибора учета", Types.String, null, 20),
                            new Template("Код единицы измерения расхода (из справочника)", Types.Int, 1, null),
                            new Template("Дата проверки", Types.DateTime, null, 14, false),
                            new Template("Дата следующей проверки", Types.DateTime, null, 14, false),
                            new Template("Дополнительные характеристики ОДПУ", Types.String, null, 250, false),
                            new Template("Тип прибора учета", Types.Int, 1, 3)
                        }
                    },
                    {
                        "10", new List<Template>
                        {
                            new Template("Номер секции", Types.Int, 0, null),
                            new Template("Уникальный код счетчика", Types.String, null, 20, true, 2, "9"),
                            new Template(
                                "Тип расхода (1 – при выгрузке показаний, 2 – при выгрузке месячного расхода)",
                                Types.Int, null, null),
                            new Template("Дата показания прибора учета / Месяц показания", Types.DateTime, null, 14),
                            new Template("Показание прибора учета / Месячный расход", Types.Numeric, null, null)
                        }
                    },
                    {
                        "11", new List<Template>
                        {
                            new Template("Номер секции", Types.Int, 0, null),
                            new Template("№ ЛС в системе поставщика", Types.String, null, 20, true, 3, "4"),
                            new Template("№ Индивидуального прибора учета в системе поставщика", Types.String, null, 20),
                            new Template("Код услуги (из справочника)", Types.Int, 1, null,true,1,"13"),
                            new Template("Тип услуги (тарифная зона: 1 –дневное потребление, 2 –ночное потребление)",
                                Types.Int, 1, 2),
                            new Template("Тип счетчика", Types.String, null, 25),
                            new Template("Разрядность прибора", Types.Int, null, null),
                            new Template("Повышающий коэффициент (коэффициент трансформации тока)", Types.Int, null,
                                null),
                            new Template("Заводской номер прибора учета", Types.String, null, 20),
                            new Template("Код единицы измерения расхода (из справочника)", Types.Int, 1, null),
                            new Template("Дата поверки", Types.DateTime, null, 14, false),
                            new Template("Дата следующей поверки", Types.DateTime, null, 14, false),
                            new Template("Доп параметры ИПУ", Types.String, null, 250, false)
                        }
                    },
                    {
                        "12", new List<Template>
                        {
                            new Template("Номер секции", Types.Int, 0, null),
                            new Template("№ Индивидуального прибора учета в системе поставщика", Types.String, 1, null),
                            new Template(
                                "Тип расхода (1 – при выгрузке показаний, 2 – при выгрузке месячного расхода)",
                                Types.Int, null, null),
                            new Template("Дата показания прибора учета / Месяц показания", Types.DateTime, null, 14),
                            new Template("Показание прибора учета / Месячный расход", Types.Numeric, null, null),
                            new Template("Код услуги (из справочника)", Types.Numeric, null, null, false)
                        }
                    },
                    {
                        "13", new List<Template>
                        {
                            new Template("Номер секции", Types.Int, 0, null),
                            new Template("Уникальный код услуги в системе поставщика информации", Types.Int, null, null, true,1,"13"),
                            new Template("Наименование услуги", Types.String, null, 60),
                            new Template("Краткое наименование услуги", Types.String, null, 60, false),
                            new Template("Тип услуги (1 - коммунальная, 2 - жилищная, 0 - не определено)", Types.Int, 0,
                                2, false),
                            new Template("Дата начала действия", Types.DateTime, null, null),
                            new Template("Дата отключения", Types.DateTime, null, null, false,1,"13")
                        }
                    },
                    {
                        "14", new List<Template>
                        {
                            new Template("Номер секции", Types.Int, 0, null),
                            new Template(
                                "Уникальный код муниципального образования (МО) в системе поставщика информации",
                                Types.Int, null, null),
                            new Template("Наименование МО", Types.String, null, 60),
                            new Template("Район", Types.String, null, 60),
                            new Template("Уникальный код района", Types.Int, null, null, false),
                            new Template("Код муниципального образования (КЛАДР)", Types.String, null, 30, false)
                        }
                    },
                    {
                        "15", new List<Template>
                        {
                            new Template("Номер секции", Types.Int, 0, null),
                            new Template("Уникальный номер лицевого счета", Types.Int, null, null, true, 3, "4"),
                            new Template("Уникальный номер гражданина", Types.Int, 1, null),
                            new Template("Уникальный номер адресного листка прибытия/убытия гражданина", Types.Int, null,
                                null),
                            new Template("Тип адресного листка", Types.Int, 1, 2),
                            new Template("Фамилия", Types.String, null, 40),
                            new Template("Имя", Types.String, null, 40),
                            new Template("Отчество", Types.String, null, 40, false),
                            new Template("Дата рождения", Types.DateTime, null, 14),
                            new Template("Измененная фамилия", Types.String, null, 40, false),
                            new Template("Измененное имя", Types.String, null, 40, false),
                            new Template("Измененное отчество", Types.String, null, 40, false),
                            new Template("Измененная дата рождения", Types.DateTime, null, 14, false),
                            new Template("Пол (М - мужской, Ж - женский)", Types.String, null, 1),
                            new Template("Тип удостоверения личности", Types.Int, 1, 5),
                            new Template("Серия удостоверения личности", Types.String, null, 10),
                            new Template("Номер удостоверения личности", Types.String, null, 7),
                            new Template("Дата выдачи удостоверения личности", Types.DateTime, null, 14, false),
                            new Template("Место выдачи удостоверения личности", Types.String, null, 70),
                            new Template("Код органа выдачи удостоверения личности", Types.String, null, 7, false),
                            new Template("Страна рождения", Types.String, null, 40, false),
                            new Template("Регион рождения", Types.String, null, 40, false),
                            new Template("Район рождения", Types.String, null, 40, false),
                            new Template("Город рождения", Types.String, null, 40, false),
                            new Template("Нас. пункт рождения", Types.String, null, 40, false),
                            new Template("Страна откуда прибыл", Types.String, null, 40, false),
                            new Template("Регион откуда прибыл", Types.String, null, 40, false),
                            new Template("Район откуда прибыл", Types.String, null, 40, false),
                            new Template("Город откуда прибыл", Types.String, null, 40, false),
                            new Template("Нас. пункт откуда прибыл", Types.String, null, 40, false),
                            new Template("Улица, дом, корпус, квартира откуда прибыл", Types.String, null, 40, false),
                            new Template("Страна куда убыл", Types.String, null, 40, false),
                            new Template("Регион куда убыл", Types.String, null, 40, false),
                            new Template("Район куда убыл", Types.String, null, 40, false),
                            new Template("Город куда убыл", Types.String, null, 40, false),
                            new Template("Нас.пункт куда убыл", Types.String, null, 40, false),
                            new Template("Улица, дом, корпус, квартира куда убыл", Types.String, null, 40, false),
                            new Template("Улица, дом, корпус, квартира для поля \"переезд в том же нас. пункте\"",
                                Types.String, null, 40, false),
                            new Template("Тип регистрации", Types.String, null, 1),
                            new Template("Дата первой регистрации по адресу", Types.DateTime, null, 14, false),
                            new Template("Дата окончания регистрации по месту пребывания", Types.DateTime, null, 14),
                            new Template("Дата постановки на воинский учет", Types.DateTime, null, 14, false),
                            new Template("Орган регистрации воинского учета", Types.String, null, 100, false),
                            new Template("Дата снятия с воинского учета", Types.DateTime, null, 14, false),
                            new Template("Орган регистрационного учета", Types.String, null, 100, false),
                            new Template("Код органа регистрации учета", Types.String, null, 7, false),
                            new Template("Родственные отношения", Types.String, null, 30, false),
                            new Template("Код цели прибытия", Types.Int, null, null, false),
                            new Template("Код цели убытия", Types.Int, null, null, false),
                            new Template("Дата составления адресного листка", Types.DateTime, null, 14),
                            new Template("Дата оформления регистрации", Types.DateTime, null, 14)
                        }
                    },
                    {
                        "16", new List<Template>
                        {
                            new Template("Номер секции", Types.Int, 0, null),
                            new Template("Уникальный код параметра в системе поставщика информации", Types.Int, null,
                                null),
                            new Template("Наименование параметра", Types.String, null, 60),
                            new Template("Принадлежность к уровню", Types.Int, null, null),
                            new Template("Тип параметра", Types.Int, 1, 3)
                        }
                    },
                    {
                        "17", new List<Template>
                        {
                            new Template("Номер секции", Types.Int, 0, null),
                            new Template("Уникальный код типа жилья по газоснабжению в системе поставщика информации",
                                Types.Int, null, null),
                            new Template("Наименование типа", Types.String, null, 60)
                        }
                    },
                    {
                        "18", new List<Template>
                        {
                            new Template("Номер секции", Types.Int, 0, null),
                            new Template("Уникальный код типа жилья по водоснабжению в системе поставщика информации",
                                Types.Int, null, null),
                            new Template("Наименование типа", Types.String, null, 60)
                        }
                    },
                    {
                        "19", new List<Template>
                        {
                            new Template("Номер секции", Types.Int, 0, null),
                            new Template("Уникальный код категории в системе поставщика информации", Types.Int, null,
                                null),
                            new Template("Наименование категории благоустройства", Types.String, null, 60)
                        }
                    },
                    {
                        "20", new List<Template>
                        {
                            new Template("Номер секции", Types.Int, 0, null),
                            new Template("Уникальный код дома в системе поставщика информации", Types.String, null, 20),
                            new Template("Код параметра дома", Types.Int, null, null),
                            new Template("Значение параметра дома", Types.String, null, 80)
                        }
                    },
                    {
                        "21", new List<Template>
                        {
                            new Template("Номер секции", Types.Int, 0, null),
                            new Template("Уникальный код лицевого счета в системе поставщика информации", Types.String,
                                null, 20),
                            new Template("Код параметра лицевого счета", Types.Int, null, null),
                            new Template("Значение параметра лицевого счета", Types.String, null, 80)
                        }
                    },
                    {
                        "22", new List<Template>
                        {
                            new Template("Номер секции", Types.Int, 0, null),
                            new Template("Уникальный код лицевого счета в системе поставщика информации", Types.String,
                                null, 20, true, 3, "4"),
                            new Template("Тип операции(1-оплата, 2-сторнирование оплаты)", Types.Int, 1, 2),
                            new Template("Номер платежного документа", Types.String, null, 80),
                            new Template("Дата оплаты", Types.DateTime, null, 14),
                            new Template("Дата учета", Types.DateTime, null, 14, false),
                            new Template("Дата корректировки", Types.DateTime, null, 14, false),
                            new Template("Сумма оплаты", Types.NumericMoney, null, null),
                            new Template("Источник оплаты", Types.String, null, 60, false),
                            new Template("Месяц, за который произведена оплата", Types.DateTime, null, null, false),
                            new Template("Уникальный номер пачки", Types.Int, null, null, true, 1, "26"),
                            new Template("Уникальный код оплаты", Types.Int, null, null, false, 1, "32")
                        }
                    },
                    {
                        "23", new List<Template>
                        {
                            new Template("Номер секции", Types.Int, 0, null),
                            new Template("Уникальный код лицевого счета в системе поставщика информации", Types.String,
                                null, 20, true, 3, "4"),
                            new Template("Код услуги в системе поставщика", Types.String, null, 20),
                            new Template("Тип недопоставки", Types.Int, null, null, true, 1, "24"),
                            new Template("Температура", Types.Int, null, null, false),
                            new Template("Дата начала недопоставки", Types.DateTime, null, 14),
                            new Template("Дата окончания недопоставки", Types.DateTime, null, 14),
                            new Template("Сумма недопоставки", Types.Numeric, null, null, false),
                            new Template("Процент удержания", Types.Numeric, null, null, false)
                        }
                    },
                    {
                        "24", new List<Template>
                        {
                            new Template("Номер секции", Types.Int, 0, null),
                            new Template("Тип недопоставки", Types.Int, null, null),
                            new Template("Наименование недопоставки", Types.String, null, 100)
                        }
                    },
                    {
                        "25", new List<Template>
                        {
                            new Template("Номер секции", Types.Int, 0, null),
                            new Template("Уникальный код лицевого счета в системе поставщика информации", Types.String,
                                null, 20, true, 3, "4"),
                            new Template("Код услуги в системе поставщика информации", Types.String, null, 100),
                            new Template("Дата начала действия услуг", Types.DateTime, null, 14),
                            new Template("Дата окончания действия услуг", Types.DateTime, null, 14),
                            new Template("Уникальный код поставщика", Types.Numeric, 0, null, true, 1, "5")
                        }
                    },
                    {
                        "26", new List<Template>
                        {
                            new Template("Номер секции", Types.Int, 0, null),
                            new Template("Уникальный номер пачки", Types.Int, null, null),
                            new Template("Дата платежного поручения", Types.DateTime, null, 14),
                            new Template("Номер платежного поручения", Types.Int, null, null),
                            new Template("Сумма платежа", Types.NumericMoney, null, null),
                            new Template("Количество платежей, вошедших в платежное поручение", Types.Int, null, null),
                            new Template("Код типа оплаты", Types.Int, null, null),
                            new Template("Признак распределения пачки", Types.Int, 0, 1)
                        }
                    },
                    {
                        "27", new List<Template>
                        {
                            new Template("Номер секции", Types.Int, 0, null),
                            new Template("Лс в системе поставщика", Types.String, null, 20, true, 3, "4"),
                            new Template("Лс в наследуемой системе", Types.String, null, 20),
                            new Template("Лс поставщика", Types.String, null, 20),
                            new Template("Дата открытия ЛС", Types.DateTime, null, 14, false),
                            new Template("Основание открытия ЛС", Types.String, null, 100, false),
                            new Template("Дата закрытия ЛС", Types.DateTime, null, 14, false),
                            new Template("Основание закрытия ЛС", Types.String, null, 100, false),
                            new Template("Лс в соц защите", Types.String, null, 20, false)
                        }
                    },
                    {
                        "28", new List<Template>
                        {
                            new Template("Номер секции", Types.Int, 0, null),
                            new Template("Уникальный номер лицевого счета", Types.String, null, 20, true, 3, "4"),
                            new Template("Уникальный номер гражданина", Types.Int, null, null,true,2,"15"),
                            new Template("Дата начала", Types.DateTime, null, 14),
                            new Template("Дата окончания", Types.DateTime, null, 14)
                        }
                    },
                    {
                        "29", new List<Template>
                        {
                            new Template("Номер секции", Types.Int, 0, null),
                            new Template("Уникальный код банка", Types.Int, null, null),
                            new Template("Наименование банка", Types.String, null, 100),
                            new Template("Сокращенное наименование банка", Types.String, null, 25),
                            new Template("Юридический адрес", Types.String, null, 100, false),
                            new Template("Фактический адрес", Types.String, null, 100, false),
                            new Template("ИНН", Types.String, 0, 20),
                            new Template("КПП", Types.String, 0, 20),
                            new Template("Телефон руководителя", Types.String, null, 20, false),
                            new Template("Телефон бухгалтерии", Types.String, null, 20, false),
                            new Template("ФИО руководителя", Types.String, null, 100, false),
                            new Template("Должность руководителя", Types.String, null, 40, false),
                            new Template("ФИО бухгалтера", Types.String, null, 100, false),
                            new Template("ОКОНХ1", Types.String, null, 20, false),
                            new Template("ОКОНХ2", Types.String, null, 20, false),
                            new Template("ОКПО", Types.String, null, 20, false),
                            new Template("Должность + ФИО в Р.п.", Types.String, null, 200, false)
                        }
                    },
                    {
                        "30", new List<Template>
                        {
                            new Template("Номер секции", Types.Int, 0, null),
                            new Template("Расчетный счет", Types.String, null, 20),
                            new Template("Уникальный код банка", Types.Int, null, null),
                            new Template("Уникальный код ЮЛ", Types.Int, 1, null, true, 1, "2"),
                            new Template("Кор. счет", Types.String, null, 20, false),
                            new Template("Уникальный код банка", Types.String, null, 20, false),
                        }
                    },
                    {
                        "31", new List<Template>
                        {
                            new Template("Номер секции", Types.Int, 0, null),
                            new Template("Код договора", Types.Int, null, null, true, 1, "5"),
                            new Template("Уникальный код дома в системе отправителя", Types.String, null, 20, true, 2,
                                "3"),
                            new Template("Код услуги, с которой рассчитывается комиссия", Types.Int, null, null),
                            new Template("Код агента-получателя комиссии", Types.Int, 1, null, true, 1, "2"),
                            new Template("Код услуги, на какую начисляется комиссия", Types.Int, null, null),
                            new Template("Процент удержания", Types.Numeric, 0, null),
                            new Template("Дата начала действия соглашения", Types.DateTime, null, 14),
                            new Template("Дата окончания действия соглашения", Types.DateTime, null, 14)
                        }
                    },
                    {
                        "32", new List<Template>
                        {
                            new Template("Номер секции", Types.Int, 0, null),
                            new Template("Уникальный код оплаты", Types.Int, null, null, true, 11, "22"),
                            new Template("Уникальный код услуги", Types.Int, null, null,true,1,"13"),
                            new Template("Код договора", Types.Int, 1, null, true, 1, "5"),
                            new Template("Сумма", Types.Numeric, null, null)
                        }
                    },
                    {
                        "33", new List<Template>
                        {
                            new Template("Номер секции", Types.Int, 0, null),
                            new Template("№ ЛС в системе поставщика", Types.String, null, 20, true, 3, "4"),
                            new Template("Код услуги", Types.Int, null, null,true,1,"13"),
                            new Template("Код договора", Types.Int, null, null, true, 1, "5"),
                            new Template("Код типа перекидки", Types.Int, null, null),
                            new Template("Сумма перекидки", Types.Numeric, null, null),
                            new Template("Тариф", Types.Numeric, null, null, false),
                            new Template("Расход", Types.Numeric, null, null, false),
                            new Template("Комментарий", Types.String, null, 100, false)
                        }
                    },
                    {
                        "34", new List<Template>
                        {
                            new Template("Номер секции", Types.Int, 0, null),
                            new Template("Уникальный код прибора учета в системе поставщика", Types.String, null, 20,
                                true, 2, "9"),
                            new Template("Номер лицевого счета, относящегося к прибору учета", Types.String, null, 20,
                                true, 3, "4")
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
                FileName = GetFilesIfWorkWithArchive();
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
                    string val = null;
                    var section = 0;
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
                CreateProtocolIns(ref dt);
                SetProgress(1m);
                return new Returns();
            }
        }

    }

    [Assemble(FormatName = "Характеристики жилого фонда", Version = "1.3.5", RegistrationName = "Test")]
    public class Format20141117001 : IFormat
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
                var sucList = new List<string>();
                var checkList = dt as Dictionary<string, List<string[]>>;
                var hashset = new HashSet<string>();
                var sets = Template.CreateSets(templateDict, checkList, ref erList);
                sucList.Add(programVersion);
                if (checkList != null && (Template.FormatTypes.Contains(checkList["1"][0][1].Trim()) && checkList["1"][0][1].Trim() == "1.3.5"))
                {
                    try
                    {
                        sucList.Add(string.Join("|", checkList["1"][0]));
                        sucList.Add("Проверены следующие секции:");
                        sucList.AddRange(checkList.Select(item => string.Format("Секция {0}:{1} ({2} строк)", item.Key, templateHead[item.Key], checkList[item.Key].Count)));
                        var k = 0;
                        foreach (var item in checkList)
                        {
                            var curTemplate = templateDict[item.Key];
                            foreach (var itemc in item.Value)
                            {
                                k++;
                                if (!hashset.Add(string.Join("|", itemc)))
                                {
                                    erList.Add(
                                        string.Format(
                                            "Ошибка: дублирование строки, строка: {1}, значение: {0}",
                                            string.Join("|", itemc), k));
                                    continue;
                                }
                                if (itemc.Count() != curTemplate.Count)
                                {
                                    erList.Add(
                                        string.Format(
                                                "Неправильный формат файла загрузки: {2}, количество полей = {0} вместо {1}, строка {3}",
                                                itemc.Count(), curTemplate.Count, templateHead[item.Key], k));
                                    // k += item.Value.Count() - 1;
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
                else erList.Add(string.Format("Формат проверяемого файла ({0}), не совпадает с форматом проверки (1.3.5)", checkList["1"][0][1].Trim()));
                //Проверка четчиков на переход через ноль
                var counters = new[] { new Counters("Показания индивидуальных приборов учета", "12", 1, 3, 4), new Counters("Показания индивидуальных приборов учета", "10", 1, 3, 4) };
                erList.AddRange(counters.Select(c => c.CheckCounters(checkList)));
                erList = erList.Where(x => x != null).ToList();
                if (erList.Count == 0)
                {
                    erList.Add("\r\nПроверка файла выполнена, ошибок не найдено\r\n");
                }
                else
                    sucList.Add("Ошибки:");
                sucList.AddRange(erList);
                //Формирование итоговых сумм в разрезе договоров
                var calc = new[] { new SumTemplate("6", new[] { 4, 12, 13, 14, 20, 22 }, 2, templateHead["6"]), new SumTemplate("8", new[] { 12, 13 }, 3, templateHead["8"]) };
                sucList.AddRange(calc.Select(c => c.Calculate(checkList, templateDict)));
                dt = sucList; //dt служит в качестве передаваемого объекта, который содержит список с ошибками
                SetProgress(0.9m);
                return new Returns();
            }

            public override Dictionary<string, string> GetTemplatesHead()
            {
                return new Dictionary<string, string>
                    {
                        {"1", "Заголовок файла"},
                        {"2", "УК"},
                        {"3", "Дома"},
                        {"4", "Информация о лицевых счетах"},
                        {"5", "Поставщики услуг"},
                        {"6", "Информация об оказываемых услугах"},
                        {"7", "Информация о параметрах лицевых счетов в месяце перерасчета"},
                        {"8", "Информация о перерасчетах начислений по услугам"},
                        {"9", "Информация об общедомовых приборах учета"},
                        {"10", "Показания общедомовых приборов учета"},
                        {"11", "Информация об индивидуальных приборах учета"},
                        {"12", "Показания ИПУ"},
                        {"13", "Перечень выгруженных услуг"},
                        {"14", "Перечень выгруженных муниципальных образований"},
                        {"15", "Информация по проживающим"},
                        {"16", "Перечень выгруженных параметров"},
                        {"17", "Перечень выгруженных типов жилья по газоснабжению"},
                        {"18", "Перечень выгруженных типов жилья по водоснабжению"},
                        {"19", "Перечень выгруженных категорий благоустройства дома"},
                        {"20", "Перечень дополнительных характеристик дома"},
                        {"21", "Перечень дополнительных характеристик лицевого счета"},
                        {"22", "Перечень оплат проведенных по лицевому счету"},
                        {"23", "Перечень недопоставок"},
                        {"24", "Перечень типов недопоставки"},
                        {"25", "Перечень услуг лицевого счета"},
                        {"26", "Пачки реестров"},
                        {"27", "Реестр ЛС"},
                        {"28", "Реестр временно-убывших"},
                        {"29", "УК"},
                        {"30", "Расчетный счет"},
                        {"31", "Соглашения по перечислениям"},
                        {"32", "Распределения оплат"},
                        {"33", "Перекидки"},
                        {"34", "Информация по лицевым счетам с приборами учета"}
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
                                new Template("Номер секции", Types.Int, 0, null),
                                new Template("Версия формата", Types.String, 0, 10),
                                new Template("Тип файла", Types.String, 0, 30),
                                new Template("Наименование организации-отправителя", Types.String, 0, 40),
                                new Template("Подразделение организации-отправителя", Types.String, null, 40, false),
                                new Template("ИНН", Types.String, null, 14, false),
                                new Template("КПП", Types.String, null, 9, false),
                                new Template("№ файла", Types.Int, 1, null),
                                new Template("Дата файла", Types.DateTime, 0, 14),
                                new Template("Телефон отправителя", Types.String, 0, 20),
                                new Template("ФИО отправителя", Types.String, 0, 80),
                                new Template("Месяц и год начислений", Types.DateTime, 0, 12),
                                new Template("Количество записей в файле", Types.Int, 0, null)
                            }
                        },
                        {
                            "2", new List<Template>
                            {
                                new Template("Номер секции", Types.Int, 0, null),
                                new Template("Уникальный код ЮЛ", Types.Int, 1, null),
                                new Template("Наименование ЮЛ", Types.String, null, 100),
                                new Template("Сокращенное наименование ЮЛ", Types.String, null, 40),
                                new Template("Юридический адрес", Types.String, null, 100, false),
                                new Template("Фактический адрес", Types.String, null, 100, false),
                                new Template("ИНН", Types.String, 0, 20),
                                new Template("КПП", Types.String, 0, 20),
                                new Template("Телефон руководителя", Types.String, null, 20, false),
                                new Template("Телефон бухгалтерии", Types.String, null, 20, false),
                                new Template("ФИО руководителя", Types.String, null, 100, false),
                                new Template("Должность руководителя", Types.String, null, 40, false),
                                new Template("ФИО бухгалтера", Types.String, null, 100, false),
                                new Template("ОКОНХ1", Types.String, null, 20, false),
                                new Template("ОКОНХ2", Types.String, null, 20, false),
                                new Template("ОКПО", Types.String, null, 20, false),
                                new Template("Должность + ФИО в Р.п.", Types.String, null, 200, false),
                                new Template("Признак того, что предприятие является УК", Types.Int, 0, 1),
                                new Template("Признак того, что предприятие является поставщиком", Types.Int, 0, 1),
                                new Template(
                                    "Признак того, что предприятие является арендатором/собственником помещений",
                                    Types.Int, 0, 1),
                                new Template("Признак того, что предприятие является РЦ ", Types.Int, 0, 1),
                                new Template("Признак того, что предприятие является РСО ", Types.Int, 0, 1),
                                new Template("Признак того, что предприятие является платежным агентом ", Types.Int, 0,
                                    1),
                                new Template("Признак того, что предприятие является субабонентом ", Types.Int, 0, 1)
                            }
                        },
                        {
                            "3", new List<Template>
                            {
                                new Template("Номер секции", Types.Int, 0, null),
                                new Template("УКДС", Types.Int, null, null, false),
                                new Template("Уникальный код дома в системе отправителя", Types.String, 0, 20,true,2,"3"),
                                new Template("Город/район", Types.String, null, 100, false),
                                new Template("Село/деревня", Types.String, null, 100, false),
                                new Template("Наименование улицы", Types.String, null, 40, false),
                                new Template("Дом", Types.String, null, 10, false),
                                new Template("Корпус", Types.String, null, 10, false),
                                new Template("Код ЮЛ, где лежит паспорт дома (УК)", Types.Int, 1, null,false),
                                new Template("Категория благоустроенности (значение из справочника)", Types.String, null,
                                    30, false),
                                new Template("Этажность", Types.Int, 1, null),
                                new Template("Год постройки", Types.DateTime, null, 14, false),
                                new Template("Общая площадь", Types.Numeric, 0, null, false),
                                new Template("Площадь мест общего пользования", Types.Numeric, 0, null, false),
                                new Template("Полезная (отапливаемая площадь)", Types.Numeric, 0, null, false),
                                new Template("Код Муниципального образования (значение из справочника)", Types.Int, 0,
                                    null, false),
                                new Template(
                                    "Дополнительные характеристики дома (задается в соответствии с правилами заполнения значений параметров)",
                                    Types.String, null, 250, false),
                                new Template("Количество строк - лицевой счет", Types.Int, 0, null, false),
                                new Template("Количество строк - общедомовой прибор учета", Types.Int, 0, null, false),
                                new Template("Код улицы КЛАДР", Types.String, null, 30),
                                new Template("Код улицы ФИАС", Types.String, null, 30)
                            }
                        },
                        {
                            "4", new List<Template>
                            {
                                new Template("Номер секции", Types.Int, 0, null),
                                new Template("УКАС", Types.Int, null, null, false),
                                new Template("Уникальный код дома в системе отправителя", Types.String, null, 20,true,2,"3"),
                                new Template("№ ЛС в системе поставщика", Types.String, null, 20, false),
                                new Template("Тип ЛС (1 – жилая квартира, 2 – субабонент / арендатор)", Types.Int, null,
                                    null),
                                new Template("Фамилия квартиросъемщика", Types.String, null, 200, false),
                                new Template("Имя квартиросъемщика", Types.String, null, 40, false),
                                new Template("Отчество квартиросъемщика", Types.String, null, 40, false),
                                new Template("Дата рождения квартиросъемщика", Types.DateTime, null, 14, false),
                                new Template("Квартира", Types.String, null, 10, false),
                                new Template("Комната лицевого счета", Types.String, null, 3, false),
                                new Template("Дата открытия ЛС", Types.DateTime, null, 14, false),
                                new Template("Основание открытия ЛС", Types.String, null, 100, false),
                                new Template("Дата закрытия ЛС", Types.DateTime, null, 14, false),
                                new Template("Основание закрытия ЛС", Types.String, null, 100, false),
                                new Template("Количество проживающих", Types.Numeric, 0, null),
                                new Template("Количество врем. прибывших жильцов", Types.Numeric, 0, null),
                                new Template("Количество  врем. убывших жильцов", Types.Numeric, 0, null),
                                new Template("Количество комнат", Types.Int, 0, null, false),
                                new Template("Общая площадь", Types.Numeric, 0, null, false),
                                new Template("Жилая площадь", Types.Numeric, 0, null, false),
                                new Template("Отапливаемая площадь", Types.Numeric, 0, null, false),
                                new Template("Площадь для найма", Types.Numeric, 0, null, false),
                                new Template("Признак коммунальной квартиры(1-да, 0 –нет)", Types.Int, 0, 1),
                                new Template("Наличие эл. плиты (1-да, 0 –нет)", Types.Int, 0, 1, false),
                                new Template("Наличие газовой плиты (1-да, 0 –нет)", Types.Int, 0, 1, false),
                                new Template("Наличие газовой колонки (1-да, 0 –нет)", Types.Int, 0, 1, false),
                                new Template("Наличие огневой плиты (1-да, 0 –нет)", Types.Int, 0, 1, false),
                                new Template("Код типа жилья по газоснабжению (из справочника)", Types.Int, 0, null,
                                    false),
                                new Template("Код типа жилья по водоснабжению (из справочника)", Types.Int, 0, null,
                                    false),
                                new Template("Код типа жилья по горячей воде (из справочника)", Types.Int, 0, null,
                                    false),
                                new Template("Код типа жилья по канализации (из справочника)", Types.Int, 0, null, false),
                                new Template("Наличие забора из открытой системы отопления (1-да, 0 –нет)", Types.Int, 0,
                                    1, false),
                                new Template(
                                    "Дополнительные характеристики ЛС (задается в соответствии с правилами заполнения значений параметров)",
                                    Types.String, null, 250, false),
                                new Template("Количество строк - услуга", Types.Int, 0, null),
                                new Template("Количество строк  – параметры в месяце перерасчета лицевого счета",
                                    Types.Int, 0, null),
                                new Template("Количество строк – индивидуальный прибор учета", Types.Int, 0, null),
                                 new Template("Уникальный код ЮЛ",Types.String , 1, null,false,1,"2"),
                                new Template("Тип владения",Types.String , null, 30,false),
                                new Template("Уникальный код жильца квартиросъемщика",Types.Int , null, null,false,2,"15"),
                                new Template("Участок",Types.String , null, 60,false),
                                new Template("Код ЮЛ, где лежит паспорт дома",Types.Int , null, null,true,1,"2")
                            }
                        },
                        {
                            "5", new List<Template>
                            {
                                new Template("Номер секции", Types.Int, 0, null),
                                new Template("Уникальный код договора", Types.Int, 1, null),
                                new Template("Код агента получателя платеже",Types.Int ,null, null,true,1,"2"),
                                new Template("Код ЮЛ принципала",Types.Int ,1, null,true,1,"2"),
                                new Template("Код поставщика",Types.Int ,null, null,true,1,"2"),
                                new Template("Наименование договора",Types.String ,null, 60,false),
                                new Template("Номер договора",Types.String ,null, 20,false),
                                new Template("Дата договора",Types.DateTime ,null, 14,false),
                                new Template("Комментарий",Types.String ,null, 200,false),
                                new Template("Расчетный счет",Types.String ,null, 200,true,1,"30")
                            }
                        },
                        {
                            "6", new List<Template>
                            {
                                new Template("Номер секции", Types.Int, 0, null),
                                new Template("№ ЛС в системе поставщика",Types.String ,null, 20,true,3,"4"),
                                new Template("Код договора",Types.Int ,null, null,true,1,"5"),
                                new Template("Код услуги (из справочника)", Types.Int, null, null,true,1,"13"),
                                new Template("Входящее сальдо (Долг на начало месяца)", Types.NumericMoney, null, null),
                                new Template("Экономически обоснованный тариф", Types.Numeric, null, null),
                                new Template("Процент регулируемого тарифа от экономически обоснованного", Types.Numeric,
                                    0, 100),
                                new Template("Регулируемый тариф", Types.Numeric, null, null),
                                new Template("Код единицы измерения расхода (из справочника)", Types.Int, 1, null),
                                new Template("Расход фактический", Types.Numeric, 0, null),
                                new Template("Расход по нормативу", Types.Numeric, 0, null),
                                new Template("Вид расчета по прибору учета(1-по счетчику / 0-без счетчика)",
                                    Types.Numeric, 0, 1),
                                new Template("Сумма начисления", Types.Numeric, null, null, false),
                                new Template("Сумма перерасчета начисления за предыдущий период (изменение сальдо)",
                                    Types.NumericMoney, null, null),
                                new Template("Сумма дотации", Types.NumericMoney, 0, null, false),
                                new Template("Сумма перерасчета дотации за предыдущий период (за все месяца)",
                                    Types.NumericMoney, null, null, false),
                                new Template(
                                    "Сумма льготы (Общая сумма льготы по всем категориям льгот для данной услуги)",
                                    Types.NumericMoney, 0, null, false),
                                new Template(
                                    "Сумма перерасчета льготы за предыдущий период (Общая по всем категориям льгот для данной услуги) (за все месяца)",
                                    Types.NumericMoney, null, null, false),
                                new Template("Сумма СМО", Types.NumericMoney, 0, null, false),
                                new Template("Сумма перерасчета  СМО за предыдущий период (за все месяца)",
                                    Types.NumericMoney, null, null, false),
                                new Template("Сумма оплаты, поступившие за месяц начислений", Types.NumericMoney, null,
                                    null),
                                new Template("Признак недействующей услуги, по которой остались долги ", Types.Int, 0, 1),
                                new Template("Исходящее сальдо (Долг на окончание месяца)", Types.Numeric, null, null),
                                new Template("Количество строк – перерасчетов начисления по услуге", Types.Int, 0, null),
                                new Template("Номер методики расчета", Types.Int, null, null, false),
                                new Template("Платежный код", Types.Numeric, 0, null, false)
                            }
                        },
                        {
                            "7", new List<Template>
                            {
                                new Template("Номер секции", Types.Int, 0, null),
                                new Template("Месяц и год перерасчета", Types.DateTime, null, 14),
                                new Template("№ ЛС в системе поставщика",Types.String ,null, 20,true,3,"4"),
                                new Template("Фамилия квартиросъемщика", Types.String, null, 40, false),
                                new Template("Имя квартиросъемщика", Types.String, null, 40, false),
                                new Template("Отчество квартиросъемщика", Types.String, null, 40, false),
                                new Template("Дата рождения квартиросъемщика", Types.DateTime, null, 14, false),
                                new Template("Квартира", Types.String, null, 10, false),
                                new Template("Комната", Types.String, null, 3, false),
                                new Template("Дата открытия ЛС", Types.DateTime, null, 14, false),
                                new Template("Основание открытия ЛС", Types.String, null, 100, false),
                                new Template("Дата закрытия ЛС", Types.DateTime, null, 14, false),
                                new Template("Основание закрытия ЛС", Types.String, null, 100, false),
                                new Template("Количество проживающих", Types.Int, 0, null, false),
                                new Template("Количество врем. Прибывших жильцов", Types.Int, 0, null, false),
                                new Template("Количество  врем. Убывших жильцов", Types.Int, 0, null, false),
                                new Template("Количество комнат", Types.Int, 0, null, false),
                                new Template("Общая площадь", Types.Numeric, 0, null, false),
                                new Template("Жилая площадь", Types.Numeric, 0, null, false),
                                new Template("Отапливаемая площадь", Types.Numeric, 0, null, false),
                                new Template("Площадь для найма", Types.Numeric, 0, null, false),
                                new Template("Признак коммунальной квартиры(1-да, 0 –нет)", Types.Int, 0, 1, false),
                                new Template("Наличие эл. Плиты (1-да, 0 –нет)", Types.Int, 0, 1, false),
                                new Template("Наличие газовой плиты (1-да, 0 –нет)", Types.Int, 0, 1, false),
                                new Template("Наличие газовой колонки (1-да, 0 –нет", Types.Int, 0, 1, false),
                                new Template("Наличие огневой плиты (1-да, 0 –нет)", Types.Int, 0, 1, false),
                                new Template("Код типа жилья по газоснабжению (из справочника)", Types.Int, 0, null,
                                    false),
                                new Template("Код типа жилья по водоснабжению (из справочника)", Types.Int, 0, null,
                                    false),
                                new Template("Код типа жилья по горячей воде (из справочника)", Types.Int, 0, null,
                                    false),
                                new Template("Код типа жилья по канализации (из справочника)", Types.Int, 0, null, false),
                                new Template("Наличие забора из открытой системы отопления (1-да, 0 –нет)", Types.Int, 0,
                                    1, false),
                                new Template("Дополнительные характеристики ЛС", Types.Int, null, 250, false)
                            }
                        },
                        {
                            "8", new List<Template>
                            {
                                new Template("Номер секции", Types.Int, 0, null),
                                new Template("Месяц и год перерасчета", Types.DateTime, null, 14, false),
                                new Template("№ ЛС в системе поставщика", Types.String ,null, 20,true,3,"4"),
                                new Template("Код договора", Types.Int, 1, null,true,1,"5"),
                                new Template("Код услуги (из справочника)", Types.Int, 1, null,true,1,"13"),
                                new Template("Экономически обоснованный тариф", Types.Numeric, null, null, false),
                                new Template("Процент регулируемого тарифа от экономически обоснованного", Types.Numeric,
                                    0, 100),
                                new Template("Регулируемый тариф", Types.Numeric, null, null),
                                new Template("Код единицы измерения расхода (из справочника)", Types.Int, 1, null),
                                new Template("Расход фактический", Types.Numeric, null, null),
                                new Template("Расход по нормативу", Types.Numeric, 0, null),
                                new Template("Вид расчета по прибору учета", Types.Int, 0, 1),
                                new Template("Сумма перерасчета начисления за месяц перерасчета", Types.NumericMoney,
                                    null, null),
                                new Template("Сумма перерасчета дотации за месяц перерасчета", Types.NumericMoney, null,
                                    null),
                                new Template(
                                    "Сумма перерасчета льготы за месяц перерасчета (Общая по всем категориям льгот для данной услуги)",
                                    Types.NumericMoney, null, null),
                                new Template("Сумма перерасчета СМО за месяц перерасчета", Types.NumericMoney, null,
                                    null)
                            }
                        },
                        {
                            "9", new List<Template>
                            {
                                new Template("Номер секции", Types.Int, 0, null),
                                new Template("Уникальный код дома(char)",Types.String ,null, 20,false,2,"3"),
                                new Template("Уникальный код прибора учета в системе поставщика", Types.String, null, 20),
                                new Template("Код услуги (из справочника)", Types.Int, 1, null,true,1,"13"),
                                new Template(
                                    "Тип услуги (тарифная зона: 1 –дневное потребление, 2 –ночное потребление)",
                                    Types.Int, null, null),
                                new Template("Тип счетчика", Types.String, null, 25),
                                new Template("Разрядность прибора", Types.Int, null, null),
                                new Template("Повышающий коэффициент (коэффициент трансформации тока)", Types.Int, null,
                                    null),
                                new Template("Заводской номер прибора учета", Types.String, null, 20),
                                new Template("Код единицы измерения расхода (из справочника)", Types.Int, 1, null),
                                new Template("Дата проверки", Types.DateTime, null, 14, false),
                                new Template("Дата следующей проверки", Types.DateTime, null, 14, false),
                                new Template("Дополнительные характеристики ОДПУ", Types.String, null, 250, false),
                                new Template("Тип прибора учета", Types.Int, null, null)
                            }
                        },
                        {
                            "10", new List<Template>
                            {
                                new Template("Номер секции", Types.Int, 0, null),
                                new Template("Уникальный код счетчика",Types.String ,null, 20,true,2,"9"),
                                new Template(
                                    "Тип расхода (1 – при выгрузке показаний, 2 – при выгрузке месячного расхода)",
                                    Types.Int, null, null),
                                new Template("Дата показания прибора учета / Месяц показания", Types.DateTime, null, 14),
                                new Template("Показание прибора учета / Месячный расход", Types.Numeric, null, null)
                            }
                        },
                        {
                            "11", new List<Template>
                            {
                                new Template("Номер секции", Types.Int, 0, null),
                                new Template("№ ЛС в системе поставщика",Types.String ,null, 20,true,3,"4"),
                                new Template("№ Индивидуального прибора учета в системе поставщика", Types.String, null,
                                    20),
                                new Template("Код услуги (из справочника)", Types.Int, 1, null,true,1,"13"),
                                new Template(
                                    "Тип услуги (тарифная зона: 1 –дневное потребление, 2 –ночное потребление)",
                                    Types.Int, null, null, false),
                                new Template("Тип счетчика", Types.String, null, 25),
                                new Template("Разрядность прибора", Types.Int, null, null),
                                new Template("Повышающий коэффициент (коэффициент трансформации тока)", Types.Int, null,
                                    null),
                                new Template("Заводской номер прибора учета", Types.String, null, 20),
                                new Template("Код единицы измерения расхода (из справочника)", Types.Int, 1, null),
                                new Template("Дата поверки", Types.DateTime, null, 14, false),
                                new Template("Дата следующей поверки", Types.DateTime, null, 14, false),
                                new Template("Доп параметры ИПУ", Types.String, null, 250, false)
                            }
                        },
                        {
                            "12", new List<Template>
                            {
                                new Template("Номер секции", Types.Int, 0, null),
                                new Template("№ Индивидуального прибора учета в системе поставщика", Types.String, 1, null),
                                new Template(
                                    "Тип расхода (1 – при выгрузке показаний, 2 – при выгрузке месячного расхода)",
                                    Types.Int, null, null),
                                new Template("Дата показания прибора учета / Месяц показания", Types.DateTime, null, 14),
                                new Template("Показание прибора учета / Месячный расход", Types.Numeric, null, null),
                                new Template("Код услуги (из справочника)", Types.Numeric, null, null)
                            }
                        },
                        {
                            "13", new List<Template>
                            {
                                new Template("Номер секции", Types.Int, 0, null),
                                new Template("Уникальный код услуги в системе поставщика информации", Types.Int, null,
                                    null,false,1,"13"),
                                new Template("Наименование услуги", Types.String, null, 60),
                                new Template("Краткое наименование услуги", Types.String, null, 60, false),
                                new Template("Тип услуги (1 - коммунальная, 2 - жилищная, 0 - не определено)", Types.Int,
                                    null, null, false),
                                 new Template("Дата начала действия",Types.DateTime ,null, null,false),
                                 new Template("Дата окончания действия",Types.DateTime ,null, null,false)
                            }
                        },
                        {
                            "14", new List<Template>
                            {
                                new Template("Номер секции", Types.Int, 0, null),
                                new Template(
                                    "Уникальный код муниципального образования (МО) в системе поставщика информации",
                                    Types.Int, null, null),
                                new Template("Наименование МО", Types.String, null, 60),
                                new Template("Район", Types.String, null, 60),
                                new Template("Уникальный код района", Types.Int, null, null, false),
                                new Template("Код муниципального образования (КЛАДР)", Types.String, null, 30, false)
                            }
                        },
                        {
                            "15", new List<Template>
                            {
                                new Template("Номер секции", Types.Int, 0, null),
                                new Template("Уникальный номер лицевого счета", Types.Int, null, null,true,3,"4"),
                                new Template("Уникальный номер гражданина", Types.Int, 1, null),
                                new Template("Уникальный номер адресного листка прибытия/убытия гражданина", Types.Int,
                                    null, null),
                                new Template("Тип адресного листка", Types.Int, 1, 2),
                                new Template("Фамилия", Types.String, null, 40),
                                new Template("Имя", Types.String, null, 40),
                                new Template("Отчество", Types.String, null, 40, false),
                                new Template("Дата рождения", Types.DateTime, null, 14),
                                new Template("Измененная фамилия", Types.String, null, 40, false),
                                new Template("Измененное имя", Types.String, null, 40, false),
                                new Template("Измененное отчество", Types.String, null, 40, false),
                                new Template("Измененная дата рождения", Types.DateTime, null, 14, false),
                                new Template("Пол (М - мужской, Ж - женский)", Types.String, null, 1),
                                new Template("Тип удостоверения личности", Types.Int, 1, 5),
                                new Template("Серия удостоверения личности", Types.String, null, 10),
                                new Template("Номер удостоверения личности", Types.String, null, 7),
                                new Template("Дата выдачи удостоверения личности", Types.DateTime, null, 14, false),
                                new Template("Место выдачи удостоверения личности", Types.String, null, 70),
                                new Template("Код органа выдачи удостоверения личности", Types.String, null, 7, false),
                                new Template("Страна рождения", Types.String, null, 40, false),
                                new Template("Регион рождения", Types.String, null, 40, false),
                                new Template("Район рождения", Types.String, null, 40, false),
                                new Template("Город рождения", Types.String, null, 40, false),
                                new Template("Нас. пункт рождения", Types.String, null, 40, false),
                                new Template("Страна откуда прибыл", Types.String, null, 40, false),
                                new Template("Регион откуда прибыл", Types.String, null, 40, false),
                                new Template("Район откуда прибыл", Types.String, null, 40, false),
                                new Template("Город откуда прибыл", Types.String, null, 40, false),
                                new Template("Нас. пункт откуда прибыл", Types.String, null, 40, false),
                                new Template("Улица, дом, корпус, квартира откуда прибыл", Types.String, null, 40, false),
                                new Template("Страна куда убыл", Types.String, null, 40, false),
                                new Template("Регион куда убыл", Types.String, null, 40, false),
                                new Template("Район куда убыл", Types.String, null, 40, false),
                                new Template("Город куда убыл", Types.String, null, 40, false),
                                new Template("Нас.пункт куда убыл", Types.String, null, 40, false),
                                new Template("Улица, дом, корпус, квартира куда убыл", Types.String, null, 40, false),
                                new Template("Улица, дом, корпус, квартира для поля \"переезд в том же нас. пункте\"",
                                    Types.String, null, 40, false),
                                new Template("Тип регистрации", Types.String, null, 1),
                                new Template("Дата первой регистрации по адресу", Types.DateTime, null, 14, false),
                                new Template("Дата окончания регистрации по месту пребывания", Types.DateTime, null, 14),
                                new Template("Дата постановки на воинский учет", Types.DateTime, null, 14, false),
                                new Template("Орган регистрации воинского учета", Types.String, null, 100, false),
                                new Template("Дата снятия с воинского учета", Types.DateTime, null, 14, false),
                                new Template("Орган регистрационного учета", Types.String, null, 100, false),
                                new Template("Код органа регистрации учета", Types.String, null, 7, false),
                                new Template("Родственные отношения", Types.String, null, 30, false),
                                new Template("Код цели прибытия", Types.Int, null, null, false),
                                new Template("Код цели убытия", Types.Int, null, null, false),
                                new Template("Дата составления адресного листка", Types.DateTime, null, 14),
                                new Template("Дата оформления регистрации", Types.DateTime, null, 14)
                            }
                        },
                        {
                            "16", new List<Template>
                            {
                                new Template("Номер секции", Types.Int, 0, null),
                                new Template("Уникальный код параметра в системе поставщика информации", Types.Int, null,
                                    null),
                                new Template("Наименование параметра", Types.String, null, 60),
                                new Template("Принадлежность к уровню", Types.Int, null, null),
                                new Template("Тип параметра", Types.Int, null, null)
                            }
                        },
                        {
                            "17", new List<Template>
                            {
                                new Template("Номер секции", Types.Int, 0, null),
                                new Template(
                                    "Уникальный код типа жилья по газоснабжению в системе поставщика информации",
                                    Types.Int, null, null),
                                new Template("Наименование типа", Types.String, null, 60)
                            }
                        },
                        {
                            "18", new List<Template>
                            {
                                new Template("Номер секции", Types.Int, 0, null),
                                new Template(
                                    "Уникальный код типа жилья по водоснабжению в системе поставщика информации",
                                    Types.Int, null, null),
                                new Template("Наименование типа", Types.String, null, 60)
                            }
                        },
                        {
                            "19", new List<Template>
                            {
                                new Template("Номер секции", Types.Int, 0, null),
                                new Template("Уникальный код категории в системе поставщика информации", Types.Int, null,
                                    null),
                                new Template("Наименование категории благоустройства", Types.String, null, 60)
                            }
                        },
                        {
                            "20", new List<Template>
                            {
                                new Template("Номер секции", Types.Int, 0, null),
                                new Template("Уникальный код дома в системе поставщика информации", Types.Numeric, null,
                                    null),
                                new Template("Код параметра дома", Types.Int, null, null),
                                new Template("Значение параметра дома", Types.String, null, 80)
                            }
                        },
                        {
                            "21", new List<Template>
                            {
                                new Template("Номер секции", Types.Int, 0, null),
                                new Template("Уникальный код лицевого счета в системе поставщика информации",
                                    Types.String, null, 20),
                                new Template("Код параметра лицевого счета", Types.Int, null, null),
                                new Template("Значение параметра лицевого счета", Types.String, null, 80)
                            }
                        },
                        {
                            "22", new List<Template>
                            {
                                new Template("Номер секции", Types.Int, 0, null),
                                new Template("Уникальный код лицевого счета в системе поставщика информации",Types.String ,null,20,true,3,"4"),
                                new Template("Тип операции(1-оплата, 2-сторнирование оплаты)",Types.Int ,null,null),
                                new Template("Номер платежного документа",Types.String ,null,80),
                                new Template("Дата оплаты",Types.DateTime ,null,14),
                                new Template("Дата учета",Types.DateTime ,null,14,false),
                                new Template("Дата корректировки",Types.DateTime ,null,14,false),
                                new Template("Сумма оплаты",Types.NumericMoney ,null,null),
                                new Template("Источник оплаты",Types.String ,null,60,false),
                                new Template("Месяц, за который произведена оплата",Types.DateTime ,null,null,false),
                                new Template("Уникальный номер пачки",Types.Int ,null, null,false,1,"26"),
                                new Template("Уникальный код оплаты",Types.Int ,null, null,false)
                            }
                        },
                        {
                            "23", new List<Template>
                            {
                                new Template("Номер секции", Types.Int, 0, null),
                                new Template("Уникальный код лицевого счета в системе поставщика информации",Types.String ,null,20,true,3,"4"),
                                new Template("Код услуги в системе поставщика",Types.String ,null,20),
                                new Template("Тип недопоставки",Types.Int ,null, null,true,1,"24"),
                                new Template("Температура", Types.Int, null, null, false),
                                new Template("Дата начала недопоставки", Types.DateTime, null, 14),
                                new Template("Дата окончания недопоставки", Types.DateTime, null, 14),
                                new Template("Сумма недопоставки", Types.Numeric, null, null, false),
                                new Template("Процент удержания", Types.Numeric, null, null, false)
                            }
                        },
                        {
                            "24", new List<Template>
                            {
                                new Template("Номер секции", Types.Int, 0, null),
                                new Template("Тип недопоставки", Types.Int, null, null),
                                new Template("Наименование недопоставки", Types.String, null, 100)
                            }
                        },
                        {
                            "25", new List<Template>
                            {
                                new Template("Номер секции", Types.Int, 0, null),
                                new Template("Уникальный код лицевого счета в системе поставщика информации",Types.String ,null,20,true,3,"4"),
                                new Template("Код услуги в системе поставщика информации",Types.String ,null,100),
                                new Template("Дата начала действия услуг",Types.DateTime ,null,14),
                                new Template("Дата окончания действия услуг",Types.DateTime ,null,14),
                                new Template("Уникальный код поставщика",Types.Numeric ,0, null,true,1,"5")
                            }
                        },
                        {
                            "26", new List<Template>
                            {
                                new Template("Номер секции", Types.Int, 0, null),
                                new Template("Уникальный номер пачки", Types.Int, null, null),
                                new Template("Дата платежного поручения", Types.DateTime, null, 14),
                                new Template("Номер платежного поручения", Types.Int, null, null),
                                new Template("Сумма платежа", Types.NumericMoney, null, null),
                                new Template("Количество платежей, вошедших в платежное поручение", Types.Int, null,
                                    null),
                                new Template("Код типа оплаты", Types.Int, null, null),
                                new Template("Признак распределения пачки", Types.Int, null, null)
                            }
                        },
                        {
                            "27", new List<Template>
                            {
                                new Template("Номер секции", Types.Int, 0, null),
                                new Template("Лс в системе поставщика", Types.String, null, 20,true,3,"4"),
                                new Template("Лс в наследуемой системе", Types.String, null, 20),
                                new Template("Лс поставщика", Types.String, null, 20),
                                new Template("Дата открытия ЛС", Types.DateTime, null, 14, false),
                                new Template("Основание открытия ЛС", Types.String, null, 100, false),
                                new Template("Дата закрытия ЛС", Types.DateTime, null, 14, false),
                                new Template("Основание закрытия ЛС", Types.String, null, 100, false),
                                new Template("Лс в соц защите", Types.String, null, 20, false)
                            }
                        },
                        {
                            "28", new List<Template>
                            {
                                new Template("Номер секции", Types.Int, 0, null),
                                new Template("Уникальный номер лицевого счета", Types.String, null, 20,true,3,"4"),
                                new Template("Уникальный номер гражданина", Types.Int, null, null,true,2,"15"),
                                new Template("Дата начала", Types.DateTime, null, 14, false),
                                new Template("Дата окончания", Types.DateTime, null, 14, false)
                            }
                        },
                        {
                            "29", new List<Template>
                            {
                                new Template("Номер секции", Types.Int, 0, null),
                                new Template("Уникальный код банка", Types.Int, null, null),
                                new Template("Наименование банка", Types.String, null, 100),
                                new Template("Сокращенное наименование банка", Types.String, null, 25),
                                new Template("Юридический адрес", Types.String, null, 100, false),
                                new Template("Фактический адрес", Types.String, null, 100, false),
                                new Template("ИНН", Types.String, 0, 20),
                                new Template("КПП", Types.String, 0, 20),
                                new Template("Телефон руководителя", Types.String, null, 20, false),
                                new Template("Телефон бухгалтерии", Types.String, null, 20, false),
                                new Template("ФИО руководителя", Types.String, null, 100, false),
                                new Template("Должность руководителя", Types.String, null, 40, false),
                                new Template("ФИО бухгалтера", Types.String, null, 100, false),
                                new Template("ОКОНХ1", Types.String, null, 20, false),
                                new Template("ОКОНХ2", Types.String, null, 20, false),
                                new Template("ОКПО", Types.String, null, 20, false),
                                new Template("Должность + ФИО в Р.п.", Types.String, null, 200, false)
                            }
                        },
                        {
                            "30", new List<Template>
                            {
                                new Template("Номер секции", Types.Int, 0, null),
                                new Template("Расчетный счет", Types.String, null, 20),
                                new Template("Уникальный код банка", Types.Int, null, null),
                                new Template("Уникальный код ЮЛ", Types.Int, 1, null,true,1,"2"),
                                new Template("Кор. счет", Types.String, null, 20, false),
                                new Template("Уникальный код банка", Types.String, null, 20, false),
                            }
                        },
                        {
                            "31", new List<Template>
                            {
                                new Template("Номер секции", Types.Int, 0, null),
                                new Template("Код договора",Types.Int ,null ,null,true,1,"5"),
                                new Template("Уникальный код дома в системе отправителя",Types.String ,null ,20,true,2,"3"),
                                new Template("Код услуги, с которой рассчитывается комиссия",Types.Int ,null ,null),
                                new Template("Код агента-получателя комиссии" +
                                             "",Types.Int ,1 ,null,true,1,"2"),
                                new Template("Код услуги, на какую начисляется комиссия", Types.Int, null, null),
                                new Template("Процент удержания", Types.Numeric, null, null),
                                new Template("Дата начала действия соглашения", Types.DateTime, null, 14),
                                new Template("Дата окончания действия соглашения", Types.DateTime, null, 14)
                            }
                        },
                        {
                            "32", new List<Template>
                            {
                                new Template("Номер секции", Types.Int, 0, null),
                                new Template("Уникальный код оплаты",Types.Int ,null ,null,true,11,"22"),
                                new Template("Уникальный код услуги",Types.Int ,null ,null,true,1,"13"),
                                new Template("Код договора",Types.Int ,1, null,true,1,"5"),
                                new Template("Сумма", Types.Numeric, null, null)
                            }
                        },
                        {
                            "33", new List<Template>
                            {
                                new Template("Номер секции", Types.Int, 0, null),
                                new Template("№ ЛС в системе поставщика",Types.String ,null ,20,true,3,"4"),
                                new Template("Код услуги",Types.Int ,null ,null,true,1,"13"),
                                new Template("Код договора",Types.Int ,null ,null,true,1,"5"),
                                new Template("Код типа перекидки", Types.Int, null, null),
                                new Template("Сумма перекидки", Types.Numeric, null, null),
                                new Template("Тариф", Types.Numeric, null, null, false),
                                new Template("Расход", Types.Numeric, null, null, false),
                                new Template("Комментарий", Types.String, null, 100, false)
                            }
                        },
                        {
                            "34", new List<Template>
                            {
                                new Template("Номер секции", Types.Int, 0, null),
                                new Template("Уникальный код прибора учета в системе поставщика", Types.String, null, 20),
                                new Template("Номер лицевого счета, относящегося к прибору учета", Types.String, null,20)
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
                FileName = GetFilesIfWorkWithArchive();
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
                    string val = null;
                    var section = 0;
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
                                curList.Add(vals.Take(vals.Length - 1).ToArray());
                            }
                        }
                        catch (Exception ex)
                        {
                            curList.Add(new[] { section.ToString(), val });
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
                CreateProtocolIns(ref dt);
                SetProgress(1m);
                return new Returns();
            }
        }
    }



    [Assemble(FormatName = "Характеристики жилого фонда", Version = "1.3.4", RegistrationName = "Test")]
    public class Format20141117002 : IFormat
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
                var sucList = new List<string>();
                var checkList = dt as Dictionary<string, List<string[]>>;
                var hashset = new HashSet<string>();
                sucList.Add(programVersion);
                var sets = Template.CreateSets(templateDict, checkList, ref erList);
                if (checkList != null &&
                       (Template.FormatTypes.Contains(checkList["1"][0][1].Trim()) &&
                        checkList["1"][0][1].Trim() == "1.3.4"))
                {
                    try
                    {
                        sucList.Add(string.Join("|", checkList["1"][0]));
                        sucList.Add("Проверены следующие секции:");
                        sucList.AddRange(checkList.Select(item => string.Format("Секция {0}:{1} ({2} строк)", item.Key, templateHead[item.Key], checkList[item.Key].Count)));
                        var k = 0;
                        foreach (var item in checkList)
                        {
                            if (!templateDict.ContainsKey(item.Key)) continue;
                            var curTemplate = templateDict[item.Key];
                            foreach (var itemc in item.Value)
                            {
                                k++;
                                try
                                {
                                    if (!hashset.Add(string.Join("|", itemc)))
                                    {
                                        erList.Add(
                                            string.Format(
                                                "Ошибка: дублирование строки, строка: {1}, значение: {0}",
                                                string.Join("|", itemc), k));
                                        continue;
                                    }
                                    if (itemc.Count() != curTemplate.Count)
                                    {
                                        erList.Add(
                                           string.Format(
                                                "Неправильный формат файла загрузки: {2}, количество полей = {0} вместо {1}, строка {3}",
                                                itemc.Count(), curTemplate.Count, templateHead[item.Key], k));
                                        // k += item.Value.Count() - 1;
                                        continue;
                                    }
                                    erList.AddRange(
                                          itemc.Select((t, i) => curTemplate[i].CheckValues(t, k, item.Key, sets))
                                            .Where(str => str != null));
                                    if (k % 5000 == 0) SetProgress(decimal.Round(0.1m + 0.8m / (rowCount) * k, 2));
                                }
                                catch (Exception ex)
                                {
                                    erList.Add("Ошибка строка " + k + " ,значения:" + string.Join("|", itemc));
                                }
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
                        "Формат проверяемого файла ({0}), не совпадает с форматом проверки (1.3.4)",
                        checkList["1"][0][1].Trim()));
                //Проверка четчиков на переход через ноль
                var counters = new[] { new Counters("Показания индивидуальных приборов учета", "12", 1, 3, 4), new Counters("Показания индивидуальных приборов учета", "10", 1, 3, 4) };
                erList.AddRange(counters.Select(c => c.CheckCounters(checkList)));
                erList = erList.Where(x => x != null).ToList();
                if (erList.Count == 0)
                {
                    erList.Add("\r\nПроверка файла выполнена, ошибок не найдено\r\n");
                }
                else
                    sucList.Add("Ошибки:");
                sucList.AddRange(erList);
                //Формирование итоговых сумм в разрезе договоров
                var calc = new[] { new SumTemplate("6", new[] { 4, 12, 13, 14, 20, 22 }, 2, templateHead["6"]), new SumTemplate("8", new[] { 12, 13 }, 3, templateHead["8"]) };
                sucList.AddRange(calc.Select(c => c.Calculate(checkList, templateDict)));
                dt = sucList; //dt служит в качестве передаваемого объекта, который содержит список с ошибками
                SetProgress(0.9m);
                return new Returns();
            }

            public override Dictionary<string, string> GetTemplatesHead()
            {
                return new Dictionary<string, string>
                    {
                        {"1", "Заголовок файла"},
                        {"2", "УК"},
                        {"3", "Дома"},
                        {"4", "Информация о лицевых счетах"},
                        {"5", "Поставщики услуг"},
                        {"6", "Информация об оказываемых услугах"},
                        {"7", "Информация о параметрах лицевых счетов в месяце перерасчета"},
                        {"8", "Информация о перерасчетах начислений по услугам"},
                        {"9", "Информация об общедомовых приборах учета"},
                        {"10", "Показания общедомовых приборов учета"},
                        {"11", "Информация об индивидуальных приборах учета"},
                        {"12", "Показания ИПУ"},
                        {"13", "Перечень выгруженных услуг"},
                        {"14", "Перечень выгруженных муниципальных образований"},
                        {"15", "Информация по проживающим"},
                        {"16", "Перечень выгруженных параметров"},
                        {"17", "Перечень выгруженных типов жилья по газоснабжению"},
                        {"18", "Перечень выгруженных типов жилья по водоснабжению"},
                        {"19", "Перечень выгруженных категорий благоустройства дома"},
                        {"20", "Перечень дополнительных характеристик дома"},
                        {"21", "Перечень дополнительных характеристик лицевого счета"},
                        {"22", "Перечень оплат проведенных по лицевому счету"},
                        {"23", "Перечень недопоставок"},
                        {"24", "Перечень типов недопоставки"},
                        {"25", "Перечень услуг лицевого счета"},
                        {"26", "Пачки реестров"},
                        {"27", "Реестр ЛС"},
                        {"28", "Реестр временно-убывших"},
                        {"29", "УК"},
                        {"30", "Расчетный счет"},
                        {"31", "Соглашения по перечислениям"},
                        {"32", "Распределения оплат"},
                        {"33", "Перекидки"}
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
                                new Template("Номер секции", Types.Int, 0, null),
                                new Template("Версия формата", Types.String, 0, 10),
                                new Template("Тип файла", Types.String, 0, 30),
                                new Template("Наименование организации-отправителя", Types.String, 0, 40),
                                new Template("Подразделение организации-отправителя", Types.String, null, 40, false),
                                new Template("ИНН", Types.String, null, 14, false),
                                new Template("КПП", Types.String, null, 9, false),
                                new Template("№ файла", Types.Int, 1, null),
                                new Template("Дата файла", Types.DateTime, 0, 14),
                                new Template("Телефон отправителя", Types.String, 0, 20),
                                new Template("ФИО отправителя", Types.String, 0, 80),
                                new Template("Месяц и год начислений", Types.DateTime, 0, 12),
                                new Template("Количество записей в файле", Types.Int, 0, null)
                            }
                        },
                        {
                            "2", new List<Template>
                            {
                                new Template("Номер секции", Types.Int, 0, null),
                                new Template("Уникальный код ЮЛ", Types.Int, 1, null),
                                new Template("Наименование ЮЛ", Types.String, null, 100),
                                new Template("Сокращенное наименование ЮЛ", Types.String, null, 40),
                                new Template("Юридический адрес", Types.String, null, 100, false),
                                new Template("Фактический адрес", Types.String, null, 100, false),
                                new Template("ИНН", Types.String, 0, 20),
                                new Template("КПП", Types.String, 0, 20),
                                new Template("Телефон руководителя", Types.String, null, 20, false),
                                new Template("Телефон бухгалтерии", Types.String, null, 20, false),
                                new Template("ФИО руководителя", Types.String, null, 100, false),
                                new Template("Должность руководителя", Types.String, null, 40, false),
                                new Template("ФИО бухгалтера", Types.String, null, 100, false),
                                new Template("ОКОНХ1", Types.String, null, 20, false),
                                new Template("ОКОНХ2", Types.String, null, 20, false),
                                new Template("ОКПО", Types.String, null, 20, false),
                                new Template("Должность + ФИО в Р.п.", Types.String, null, 200, false),
                                new Template("Признак того, что предприятие является УК", Types.Int, 0, 1),
                                new Template("Признак того, что предприятие является поставщиком", Types.Int, 0, 1),
                                new Template(
                                    "Признак того, что предприятие является арендатором/собственником помещений",
                                    Types.Int, 0, 1),
                                new Template("Признак того, что предприятие является РЦ ", Types.Int, 0, 1),
                                new Template("Признак того, что предприятие является РСО ", Types.Int, 0, 1),
                                new Template("Признак того, что предприятие является платежным агентом ", Types.Int, 0,
                                    1),
                                new Template("Признак того, что предприятие является субабонентом ", Types.Int, 0, 1)
                            }
                        },
                        {
                            "3", new List<Template>
                            {
                                new Template("Номер секции", Types.Int, 0, null),
                                new Template("УКДС", Types.Int, null, null, false),
                                new Template("Уникальный код дома в системе отправителя", Types.String, 0, 20,true,2,"3"),
                                new Template("Город/район", Types.String, null, 100, false),
                                new Template("Село/деревня", Types.String, null, 100, false),
                                new Template("Наименование улицы", Types.String, null, 40, false),
                                new Template("Дом", Types.String, null, 10, false),
                                new Template("Корпус", Types.String, null, 10, false),
                                new Template("Код ЮЛ, где лежит паспорт дома (УК)", Types.Int, 1, null,false),
                                new Template("Категория благоустроенности (значение из справочника)", Types.String, null,
                                    30, false),
                                new Template("Этажность", Types.Int, 1, null),
                                new Template("Год постройки", Types.DateTime, null, 14, false),
                                new Template("Общая площадь", Types.Numeric, 0, null, false),
                                new Template("Площадь мест общего пользования", Types.Numeric, 0, null, false),
                                new Template("Полезная (отапливаемая площадь)", Types.Numeric, 0, null, false),
                                new Template("Код Муниципального образования (значение из справочника)", Types.Int, 0,
                                    null, false),
                                new Template(
                                    "Дополнительные характеристики дома (задается в соответствии с правилами заполнения значений параметров)",
                                    Types.String, null, 250, false),
                                new Template("Количество строк - лицевой счет", Types.Int, 0, null, false),
                                new Template("Количество строк - общедомовой прибор учета", Types.Int, 0, null, false),
                                new Template("Код улицы КЛАДР", Types.String, null, 30),
                                new Template("Код улицы ФИАС", Types.String, null, 30)
                            }
                        },
                        {
                            "4", new List<Template>
                            {
                                new Template("Номер секции", Types.Int, 0, null),
                                new Template("УКАС", Types.Int, null, null, false),
                                new Template("Уникальный код дома в системе отправителя", Types.String, null, 20),
                                new Template("№ ЛС в системе поставщика", Types.String, null, 20, false),
                                new Template("Тип ЛС (1 – жилая квартира, 2 – субабонент / арендатор)", Types.Int, null,
                                    null),
                                new Template("Фамилия квартиросъемщика", Types.String, null, 200, false),
                                new Template("Имя квартиросъемщика", Types.String, null, 40, false),
                                new Template("Отчество квартиросъемщика", Types.String, null, 40, false),
                                new Template("Дата рождения квартиросъемщика", Types.DateTime, null, 14, false),
                                new Template("Квартира", Types.String, null, 10, false),
                                new Template("Комната лицевого счета", Types.String, null, 3, false),
                                new Template("Дата открытия ЛС", Types.DateTime, null, 14, false),
                                new Template("Основание открытия ЛС", Types.String, null, 100, false),
                                new Template("Дата закрытия ЛС", Types.DateTime, null, 14, false),
                                new Template("Основание закрытия ЛС", Types.String, null, 100, false),
                                new Template("Количество проживающих", Types.Numeric, 0, null),
                                new Template("Количество врем. прибывших жильцов", Types.Numeric, 0, null),
                                new Template("Количество  врем. убывших жильцов", Types.Numeric, 0, null),
                                new Template("Количество комнат", Types.Int, 0, null, false),
                                new Template("Общая площадь", Types.Numeric, 0, null, false),
                                new Template("Жилая площадь", Types.Numeric, 0, null, false),
                                new Template("Отапливаемая площадь", Types.Numeric, 0, null, false),
                                new Template("Площадь для найма", Types.Numeric, 0, null, false),
                                new Template("Признак коммунальной квартиры(1-да, 0 –нет)", Types.Int, 0, 1),
                                new Template("Наличие эл. плиты (1-да, 0 –нет)", Types.Int, 0, 1, false),
                                new Template("Наличие газовой плиты (1-да, 0 –нет)", Types.Int, 0, 1, false),
                                new Template("Наличие газовой колонки (1-да, 0 –нет)", Types.Int, 0, 1, false),
                                new Template("Наличие огневой плиты (1-да, 0 –нет)", Types.Int, 0, 1, false),
                                new Template("Код типа жилья по газоснабжению (из справочника)", Types.Int, 0, null,
                                    false),
                                new Template("Код типа жилья по водоснабжению (из справочника)", Types.Int, 0, null,
                                    false),
                                new Template("Код типа жилья по горячей воде (из справочника)", Types.Int, 0, null,
                                    false),
                                new Template("Код типа жилья по канализации (из справочника)", Types.Int, 0, null, false),
                                new Template("Наличие забора из открытой системы отопления (1-да, 0 –нет)", Types.Int, 0,
                                    1, false),
                                new Template(
                                    "Дополнительные характеристики ЛС (задается в соответствии с правилами заполнения значений параметров)",
                                    Types.String, null, 250, false),
                                new Template("Количество строк - услуга", Types.Int, 0, null),
                                new Template("Количество строк  – параметры в месяце перерасчета лицевого счета",
                                    Types.Int, 0, null),
                                new Template("Количество строк – индивидуальный прибор учета", Types.Int, 0, null),
                                new Template("Уникальный код ЮЛ",Types.String , 1, null,false,1,"2"),
                                new Template("Тип владения",Types.String , null, 30,false),
                                new Template("Уникальный код жильца квартиросъемщика",Types.Int , null, null,false,2,"15"),
                                new Template("Участок",Types.String , null, 60,false),
                                new Template("Код ЮЛ, где лежит паспорт дома",Types.Int , null, null,false,1,"2")
                            }
                        },
                        {
                            "5", new List<Template>
                            {
                                new Template("Номер секции", Types.Int, 0, null),
                                new Template("Уникальный код договора", Types.Int, 1, null),
                                new Template("Код агента получателя платеже",Types.Int ,null, null,true,1,"2"),
                                new Template("Код ЮЛ принципала",Types.Int ,1, null,true,1,"2"),
                                new Template("Код поставщика",Types.Int ,null, null,true,1,"2"),
                                new Template("Наименование договора",Types.String ,null, 60,false),
                                new Template("Номер договора",Types.String ,null, 20,false),
                                new Template("Дата договора",Types.DateTime ,null, 14,false),
                                new Template("Комментарий",Types.String ,null, 200,false),
                                new Template("Расчетный счет",Types.String ,null, 200,true,1,"30")
                            }
                        },
                        {
                            "6", new List<Template>
                            {
                                new Template("Номер секции", Types.Int, 0, null),
                                new Template("№ ЛС в системе поставщика",Types.String ,null, 20,true,3,"4"),
                                new Template("Код договора",Types.Int ,null, null,true,1,"5"),
                                new Template("Код услуги (из справочника)", Types.Int, null, null,true,1,"13"),
                                new Template("Входящее сальдо (Долг на начало месяца)", Types.NumericMoney, null, null),
                                new Template("Экономически обоснованный тариф", Types.Numeric, null, null),
                                new Template("Процент регулируемого тарифа от экономически обоснованного", Types.Numeric,
                                    0, 100),
                                new Template("Регулируемый тариф", Types.Numeric, null, null),
                                new Template("Код единицы измерения расхода (из справочника)", Types.Int, 1, null),
                                new Template("Расход фактический", Types.Numeric, 0, null),
                                new Template("Расход по нормативу", Types.Numeric, 0, null),
                                new Template("Вид расчета по прибору учета(1-по счетчику / 0-без счетчика)",
                                    Types.Numeric, 0, 1),
                                new Template("Сумма начисления", Types.Numeric, null, null, false),
                                new Template("Сумма перерасчета начисления за предыдущий период (изменение сальдо)",
                                    Types.NumericMoney, null, null),
                                new Template("Сумма дотации", Types.NumericMoney, 0, null, false),
                                new Template("Сумма перерасчета дотации за предыдущий период (за все месяца)",
                                    Types.NumericMoney, null, null, false),
                                new Template(
                                    "Сумма льготы (Общая сумма льготы по всем категориям льгот для данной услуги)",
                                    Types.NumericMoney, 0, null, false),
                                new Template(
                                    "Сумма перерасчета льготы за предыдущий период (Общая по всем категориям льгот для данной услуги) (за все месяца)",
                                    Types.NumericMoney, null, null, false),
                                new Template("Сумма СМО", Types.NumericMoney, 0, null, false),
                                new Template("Сумма перерасчета  СМО за предыдущий период (за все месяца)",
                                    Types.NumericMoney, null, null, false),
                                new Template("Сумма оплаты, поступившие за месяц начислений", Types.NumericMoney, null,
                                    null),
                                new Template("Признак недействующей услуги, по которой остались долги ", Types.Int, 0, 1),
                                new Template("Исходящее сальдо (Долг на окончание месяца)", Types.Numeric, null, null),
                                new Template("Количество строк – перерасчетов начисления по услуге", Types.Int, 0, null),
                                new Template("Номер методики расчета", Types.Int, null, null, false),
                                new Template("Платежный код", Types.Numeric, 0, null, false)
                            }
                        },
                        {
                            "7", new List<Template>
                            {
                                new Template("Номер секции", Types.Int, 0, null),
                                new Template("Месяц и год перерасчета", Types.DateTime, null, 14),
                                new Template("№ ЛС в системе поставщика",Types.String ,null, 20,true,3,"4"),
                                new Template("Фамилия квартиросъемщика", Types.String, null, 40, false),
                                new Template("Имя квартиросъемщика", Types.String, null, 40, false),
                                new Template("Отчество квартиросъемщика", Types.String, null, 40, false),
                                new Template("Дата рождения квартиросъемщика", Types.DateTime, null, 14, false),
                                new Template("Квартира", Types.String, null, 10, false),
                                new Template("Комната", Types.String, null, 3, false),
                                new Template("Дата открытия ЛС", Types.DateTime, null, 14, false),
                                new Template("Основание открытия ЛС", Types.String, null, 100, false),
                                new Template("Дата закрытия ЛС", Types.DateTime, null, 14, false),
                                new Template("Основание закрытия ЛС", Types.String, null, 100, false),
                                new Template("Количество проживающих", Types.Int, 0, null, false),
                                new Template("Количество врем. Прибывших жильцов", Types.Int, 0, null, false),
                                new Template("Количество  врем. Убывших жильцов", Types.Int, 0, null, false),
                                new Template("Количество комнат", Types.Int, 0, null, false),
                                new Template("Общая площадь", Types.Numeric, 0, null, false),
                                new Template("Жилая площадь", Types.Numeric, 0, null, false),
                                new Template("Отапливаемая площадь", Types.Numeric, 0, null, false),
                                new Template("Площадь для найма", Types.Numeric, 0, null, false),
                                new Template("Признак коммунальной квартиры(1-да, 0 –нет)", Types.Int, 0, 1, false),
                                new Template("Наличие эл. Плиты (1-да, 0 –нет)", Types.Int, 0, 1, false),
                                new Template("Наличие газовой плиты (1-да, 0 –нет)", Types.Int, 0, 1, false),
                                new Template("Наличие газовой колонки (1-да, 0 –нет", Types.Int, 0, 1, false),
                                new Template("Наличие огневой плиты (1-да, 0 –нет)", Types.Int, 0, 1, false),
                                new Template("Код типа жилья по газоснабжению (из справочника)", Types.Int, 0, null,
                                    false),
                                new Template("Код типа жилья по водоснабжению (из справочника)", Types.Int, 0, null,
                                    false),
                                new Template("Код типа жилья по горячей воде (из справочника)", Types.Int, 0, null,
                                    false),
                                new Template("Код типа жилья по канализации (из справочника)", Types.Int, 0, null, false),
                                new Template("Наличие забора из открытой системы отопления (1-да, 0 –нет)", Types.Int, 0,
                                    1, false),
                                new Template("Дополнительные характеристики ЛС", Types.Int, null, 250, false)
                            }
                        },
                        {
                            "8", new List<Template>
                            {
                                new Template("Номер секции", Types.Int, 0, null),
                                new Template("Месяц и год перерасчета", Types.DateTime, null, 14, false),
                                new Template("№ ЛС в системе поставщика",Types.String ,null, 20,true,3,"4"),
                                new Template("Код договора", Types.Int, 1, null,true,1,"5"),
                                new Template("Код услуги (из справочника)", Types.Int, 1, null,true,1,"13"),
                                new Template("Экономически обоснованный тариф", Types.Numeric, null, null, false),
                                new Template("Процент регулируемого тарифа от экономически обоснованного", Types.Numeric,
                                    0, 100),
                                new Template("Регулируемый тариф", Types.Numeric, null, null),
                                new Template("Код единицы измерения расхода (из справочника)", Types.Int, 1, null),
                                new Template("Расход фактический", Types.Numeric, null, null),
                                new Template("Расход по нормативу", Types.Numeric, 0, null),
                                new Template("Вид расчета по прибору учета", Types.Int, 0, 1),
                                new Template("Сумма перерасчета начисления за месяц перерасчета", Types.NumericMoney,
                                    null, null),
                                new Template("Сумма перерасчета дотации за месяц перерасчета", Types.NumericMoney, null,
                                    null),
                                new Template(
                                    "Сумма перерасчета льготы за месяц перерасчета (Общая по всем категориям льгот для данной услуги)",
                                    Types.NumericMoney, null, null),
                                new Template("Сумма перерасчета СМО за месяц перерасчета", Types.NumericMoney, null,
                                    null)
                            }
                        },
                        {
                            "9", new List<Template>
                            {
                                new Template("Номер секции", Types.Int, 0, null),
                                new Template("Уникальный код дома(char)",Types.String ,null, 20,false,2,"3"),
                                new Template("Уникальный код прибора учета в системе поставщика", Types.String, null, 20),
                                new Template("Код услуги (из справочника)", Types.Int, 1, null,true,1,"13"),
                                new Template(
                                    "Тип услуги (тарифная зона: 1 –дневное потребление, 2 –ночное потребление)",
                                    Types.Int, null, null),
                                new Template("Тип счетчика", Types.String, null, 25),
                                new Template("Разрядность прибора", Types.Int, null, null),
                                new Template("Повышающий коэффициент (коэффициент трансформации тока)", Types.Int, null,
                                    null),
                                new Template("Заводской номер прибора учета", Types.String, null, 20),
                                new Template("Код единицы измерения расхода (из справочника)", Types.Int, 1, null),
                                new Template("Дата проверки", Types.DateTime, null, 14, false),
                                new Template("Дата следующей проверки", Types.DateTime, null, 14, false),
                                new Template("Дополнительные характеристики ОДПУ", Types.String, null, 250, false)
                            }
                        },
                        {
                            "10", new List<Template>
                            {
                                new Template("Номер секции", Types.Int, 0, null),
                                new Template("Уникальный код счетчика",Types.String ,null, 20,true,2,"9"),
                                new Template(
                                    "Тип расхода (1 – при выгрузке показаний, 2 – при выгрузке месячного расхода)",
                                    Types.Int, null, null),
                                new Template("Дата показания прибора учета / Месяц показания", Types.DateTime, null, 14),
                                new Template("Показание прибора учета / Месячный расход", Types.Numeric, null, null)
                            }
                        },
                        {
                            "11", new List<Template>
                            {
                                new Template("Номер секции", Types.Int, 0, null),
                                new Template("№ ЛС в системе поставщика",Types.String ,null, 20,true,3,"4"),
                                new Template("№ Индивидуального прибора учета в системе поставщика", Types.String, null,
                                    20),
                                new Template("Код услуги (из справочника)", Types.Int, 1, null,true,1,"13"),
                                new Template(
                                    "Тип услуги (тарифная зона: 1 –дневное потребление, 2 –ночное потребление)",
                                    Types.Int, null, null, false),
                                new Template("Тип счетчика", Types.String, null, 25),
                                new Template("Разрядность прибора", Types.Int, null, null),
                                new Template("Повышающий коэффициент (коэффициент трансформации тока)", Types.Int, null,
                                    null),
                                new Template("Заводской номер прибора учета", Types.String, null, 20),
                                new Template("Код единицы измерения расхода (из справочника)", Types.Int, 1, null),
                                new Template("Дата поверки", Types.DateTime, null, 14, false),
                                new Template("Дата следующей поверки", Types.DateTime, null, 14, false),
                                new Template("Доп параметры ИПУ", Types.String, null, 250, false)
                            }
                        },
                        {
                            "12", new List<Template>
                            {
                                new Template("Номер секции", Types.Int, 0, null),
                                new Template("№ Индивидуального прибора учета в системе поставщика", Types.String, 1, null),
                                new Template(
                                    "Тип расхода (1 – при выгрузке показаний, 2 – при выгрузке месячного расхода)",
                                    Types.Int, null, null),
                                new Template("Дата показания прибора учета / Месяц показания", Types.DateTime, null, 14),
                                new Template("Показание прибора учета / Месячный расход", Types.Numeric, null, null),
                                new Template("Код услуги (из справочника)", Types.Numeric, null, null)
                            }
                        },
                        {
                            "13", new List<Template>
                            {
                                new Template("Номер секции", Types.Int, 0, null),
                                new Template("Уникальный код услуги в системе поставщика информации", Types.Int, null,
                                    null,true,1,"13"),
                                new Template("Наименование услуги", Types.String, null, 60),
                                new Template("Краткое наименование услуги", Types.String, null, 60, false),
                                new Template("Тип услуги (1 - коммунальная, 2 - жилищная, 0 - не определено)", Types.Int,
                                    null, null, false),
                                new Template("Дата начала действия",Types.DateTime ,null, null,false),
                                new Template("Дата отключения",Types.DateTime ,null, null, false)
                            }
                        },
                        {
                            "14", new List<Template>
                            {
                                new Template("Номер секции", Types.Int, 0, null),
                                new Template(
                                    "Уникальный код муниципального образования (МО) в системе поставщика информации",
                                    Types.Int, null, null),
                                new Template("Наименование МО", Types.String, null, 60),
                                new Template("Район", Types.String, null, 60),
                                new Template("Уникальный код района", Types.Int, null, null, false),
                                new Template("Код муниципального образования (КЛАДР)", Types.String, null, 30, false)
                            }
                        },
                        {
                            "15", new List<Template>
                            {
                                new Template("Номер секции", Types.Int, 0, null),
                                new Template("Уникальный номер лицевого счета", Types.Int, null, null,true,3,"4"),
                                new Template("Уникальный номер гражданина", Types.Int, 1, null),
                                new Template("Уникальный номер адресного листка прибытия/убытия гражданина", Types.Int,
                                    null, null),
                                new Template("Тип адресного листка", Types.Int, 1, 2),
                                new Template("Фамилия", Types.String, null, 40),
                                new Template("Имя", Types.String, null, 40),
                                new Template("Отчество", Types.String, null, 40, false),
                                new Template("Дата рождения", Types.DateTime, null, 14),
                                new Template("Измененная фамилия", Types.String, null, 40, false),
                                new Template("Измененное имя", Types.String, null, 40, false),
                                new Template("Измененное отчество", Types.String, null, 40, false),
                                new Template("Измененная дата рождения", Types.DateTime, null, 14, false),
                                new Template("Пол (М - мужской, Ж - женский)", Types.String, null, 1),
                                new Template("Тип удостоверения личности", Types.Int, 1, 5),
                                new Template("Серия удостоверения личности", Types.String, null, 10),
                                new Template("Номер удостоверения личности", Types.String, null, 7),
                                new Template("Дата выдачи удостоверения личности", Types.DateTime, null, 14, false),
                                new Template("Место выдачи удостоверения личности", Types.String, null, 70),
                                new Template("Код органа выдачи удостоверения личности", Types.String, null, 7, false),
                                new Template("Страна рождения", Types.String, null, 40, false),
                                new Template("Регион рождения", Types.String, null, 40, false),
                                new Template("Район рождения", Types.String, null, 40, false),
                                new Template("Город рождения", Types.String, null, 40, false),
                                new Template("Нас. пункт рождения", Types.String, null, 40, false),
                                new Template("Страна откуда прибыл", Types.String, null, 40, false),
                                new Template("Регион откуда прибыл", Types.String, null, 40, false),
                                new Template("Район откуда прибыл", Types.String, null, 40, false),
                                new Template("Город откуда прибыл", Types.String, null, 40, false),
                                new Template("Нас. пункт откуда прибыл", Types.String, null, 40, false),
                                new Template("Улица, дом, корпус, квартира откуда прибыл", Types.String, null, 40, false),
                                new Template("Страна куда убыл", Types.String, null, 40, false),
                                new Template("Регион куда убыл", Types.String, null, 40, false),
                                new Template("Район куда убыл", Types.String, null, 40, false),
                                new Template("Город куда убыл", Types.String, null, 40, false),
                                new Template("Нас.пункт куда убыл", Types.String, null, 40, false),
                                new Template("Улица, дом, корпус, квартира куда убыл", Types.String, null, 40, false),
                                new Template("Улица, дом, корпус, квартира для поля \"переезд в том же нас. пункте\"",
                                    Types.String, null, 40, false),
                                new Template("Тип регистрации", Types.String, null, 1),
                                new Template("Дата первой регистрации по адресу", Types.DateTime, null, 14, false),
                                new Template("Дата окончания регистрации по месту пребывания", Types.DateTime, null, 14),
                                new Template("Дата постановки на воинский учет", Types.DateTime, null, 14, false),
                                new Template("Орган регистрации воинского учета", Types.String, null, 100, false),
                                new Template("Дата снятия с воинского учета", Types.DateTime, null, 14, false),
                                new Template("Орган регистрационного учета", Types.String, null, 100, false),
                                new Template("Код органа регистрации учета", Types.String, null, 7, false),
                                new Template("Родственные отношения", Types.String, null, 30, false),
                                new Template("Код цели прибытия", Types.Int, null, null, false),
                                new Template("Код цели убытия", Types.Int, null, null, false),
                                new Template("Дата составления адресного листка", Types.DateTime, null, 14),
                                new Template("Дата оформления регистрации", Types.DateTime, null, 14)
                            }
                        },
                        {
                            "16", new List<Template>
                            {
                                new Template("Номер секции", Types.Int, 0, null),
                                new Template("Уникальный код параметра в системе поставщика информации", Types.Int, null,
                                    null),
                                new Template("Наименование параметра", Types.String, null, 60),
                                new Template("Принадлежность к уровню", Types.Int, null, null),
                                new Template("Тип параметра", Types.Int, null, null)
                            }
                        },
                        {
                            "17", new List<Template>
                            {
                                new Template("Номер секции", Types.Int, 0, null),
                                new Template(
                                    "Уникальный код типа жилья по газоснабжению в системе поставщика информации",
                                    Types.Int, null, null),
                                new Template("Наименование типа", Types.String, null, 60)
                            }
                        },
                        {
                            "18", new List<Template>
                            {
                                new Template("Номер секции", Types.Int, 0, null),
                                new Template(
                                    "Уникальный код типа жилья по водоснабжению в системе поставщика информации",
                                    Types.Int, null, null),
                                new Template("Наименование типа", Types.String, null, 60)
                            }
                        },
                        {
                            "19", new List<Template>
                            {
                                new Template("Номер секции", Types.Int, 0, null),
                                new Template("Уникальный код категории в системе поставщика информации", Types.Int, null,
                                    null),
                                new Template("Наименование категории благоустройства", Types.String, null, 60)
                            }
                        },
                        {
                            "20", new List<Template>
                            {
                                new Template("Номер секции", Types.Int, 0, null),
                                new Template("Уникальный код дома в системе поставщика информации", Types.Numeric, null,
                                    null),
                                new Template("Код параметра дома", Types.Int, null, null),
                                new Template("Значение параметра дома", Types.String, null, 80)
                            }
                        },
                        {
                            "21", new List<Template>
                            {
                                new Template("Номер секции", Types.Int, 0, null),
                                new Template("Уникальный код лицевого счета в системе поставщика информации",
                                    Types.String, null, 20),
                                new Template("Код параметра лицевого счета", Types.Int, null, null),
                                new Template("Значение параметра лицевого счета", Types.String, null, 80)
                            }
                        },
                        {
                            "22", new List<Template>
                            {
                                new Template("Номер секции", Types.Int, 0, null),
                                new Template("Уникальный код лицевого счета в системе поставщика информации",
                                    Types.String, null, 20),
                                new Template("Тип операции(1-оплата, 2-сторнирование оплаты)", Types.Int, null, null),
                                new Template("Номер платежного документа", Types.String, null, 80),
                                new Template("Дата оплаты", Types.DateTime, null, 14),
                                new Template("Дата учета", Types.DateTime, null, 14, false),
                                new Template("Дата корректировки", Types.DateTime, null, 14, false),
                                new Template("Сумма оплаты", Types.NumericMoney, null, null),
                                new Template("Источник оплаты", Types.String, null, 60, false),
                                new Template("Месяц, за который произведена оплата", Types.DateTime, null, null, false),
                                new Template("Уникальный номер пачки", Types.Int, null, null, false),
                                new Template("Уникальный код оплаты", Types.Int, null, null, false)
                            }
                        },
                        {
                            "23", new List<Template>
                            {
                                new Template("Номер секции", Types.Int, 0, null),
                                new Template("Уникальный код лицевого счета в системе поставщика информации",Types.String ,null,20,true,3,"4"),
                                new Template("Код услуги в системе поставщика",Types.String ,null,20),
                                new Template("Тип недопоставки",Types.Int ,null, null,true,1,"24"),
                                new Template("Температура", Types.Int, null, null, false),
                                new Template("Дата начала недопоставки", Types.DateTime, null, 14),
                                new Template("Дата окончания недопоставки", Types.DateTime, null, 14),
                                new Template("Сумма недопоставки", Types.Numeric, null, null, false),
                                new Template("Процент удержания", Types.Numeric, null, null, false)
                            }
                        },
                        {
                            "24", new List<Template>
                            {
                                new Template("Номер секции", Types.Int, 0, null),
                                new Template("Тип недопоставки", Types.Int, null, null),
                                new Template("Наименование недопоставки", Types.String, null, 100)
                            }
                        },
                        {
                            "25", new List<Template>
                            {
                                new Template("Номер секции", Types.Int, 0, null),
                                new Template("Уникальный код лицевого счета в системе поставщика информации",Types.String ,null,20,true,3,"4"),
                                new Template("Код услуги в системе поставщика информации",Types.String ,null,100),
                                new Template("Дата начала действия услуг",Types.DateTime ,null,14),
                                new Template("Дата окончания действия услуг",Types.DateTime ,null,14),
                                new Template("Уникальный код поставщика",Types.Numeric ,0, null,true,1,"5")
                            }
                        },
                        {
                            "26", new List<Template>
                            {
                                new Template("Номер секции", Types.Int, 0, null),
                                new Template("Уникальный номер пачки", Types.Int, null, null),
                                new Template("Дата платежного поручения", Types.DateTime, null, 14),
                                new Template("Номер платежного поручения", Types.Int, null, null),
                                new Template("Сумма платежа", Types.NumericMoney, null, null),
                                new Template("Количество платежей, вошедших в платежное поручение", Types.Int, null,
                                    null),
                                new Template("Код типа оплаты", Types.Int, null, null),
                                new Template("Признак распределения пачки", Types.Int, null, null)
                            }
                        },
                        {
                            "27", new List<Template>
                            {
                                new Template("Номер секции", Types.Int, 0, null),
                                new Template("Лс в системе поставщика", Types.String, null, 20,true,3,"4"),
                                new Template("Лс в наследуемой системе", Types.String, null, 20),
                                new Template("Лс поставщика", Types.String, null, 20),
                                new Template("Дата открытия ЛС", Types.DateTime, null, 14, false),
                                new Template("Основание открытия ЛС", Types.String, null, 100, false),
                                new Template("Дата закрытия ЛС", Types.DateTime, null, 14, false),
                                new Template("Основание закрытия ЛС", Types.String, null, 100, false),
                                new Template("Лс в соц защите", Types.String, null, 20, false)
                            }
                        },
                        {
                            "28", new List<Template>
                            {
                                new Template("Номер секции", Types.Int, 0, null),
                                new Template("Уникальный номер лицевого счета", Types.String, null, 20,true,3,"4"),
                                new Template("Уникальный номер гражданина", Types.Int, null, null,true,2,"15"),
                                new Template("Дата начала", Types.DateTime, null, 14, false),
                                new Template("Дата окончания", Types.DateTime, null, 14, false)
                            }
                        },
                        {
                            "29", new List<Template>
                            {
                                new Template("Номер секции", Types.Int, 0, null),
                                new Template("Уникальный код банка", Types.Int, null, null),
                                new Template("Наименование банка", Types.String, null, 100),
                                new Template("Сокращенное наименование банка", Types.String, null, 25),
                                new Template("Юридический адрес", Types.String, null, 100, false),
                                new Template("Фактический адрес", Types.String, null, 100, false),
                                new Template("ИНН", Types.String, 0, 20),
                                new Template("КПП", Types.String, 0, 20),
                                new Template("Телефон руководителя", Types.String, null, 20, false),
                                new Template("Телефон бухгалтерии", Types.String, null, 20, false),
                                new Template("ФИО руководителя", Types.String, null, 100, false),
                                new Template("Должность руководителя", Types.String, null, 40, false),
                                new Template("ФИО бухгалтера", Types.String, null, 100, false),
                                new Template("ОКОНХ1", Types.String, null, 20, false),
                                new Template("ОКОНХ2", Types.String, null, 20, false),
                                new Template("ОКПО", Types.String, null, 20, false),
                                new Template("Должность + ФИО в Р.п.", Types.String, null, 200, false)
                            }
                        },
                        {
                            "30", new List<Template>
                            {
                                new Template("Номер секции", Types.Int, 0, null),
                                new Template("Расчетный счет", Types.String, null, 20),
                                new Template("Уникальный код банка", Types.Int, null, null),
                                new Template("Уникальный код ЮЛ", Types.Int, 1, null,true,1,"2"),
                                new Template("Кор. счет", Types.String, null, 20, false),
                                new Template("Уникальный код банка", Types.String, null, 20, false),
                            }
                        },
                        {
                            "31", new List<Template>
                            {
                                new Template("Номер секции", Types.Int, 0, null),
                                new Template("Код договора",Types.Int ,null ,null,true,1,"5"),
                                new Template("Уникальный код дома в системе отправителя",Types.String ,null ,20,true,2,"3"),
                                new Template("Код услуги, с которой рассчитывается комиссия",Types.Int ,null ,null),
                                new Template("Код агента-получателя комиссии",Types.Int ,1 ,null,true,1,"2"),
                                new Template("Код услуги, на какую начисляется комиссия", Types.Int, null, null),
                                new Template("Процент удержания", Types.Numeric, null, null),
                                new Template("Дата начала действия соглашения", Types.DateTime, null, 14),
                                new Template("Дата окончания действия соглашения", Types.DateTime, null, 14)
                            }
                        },
                        {
                            "32", new List<Template>
                            {
                                new Template("Номер секции", Types.Int, 0, null),
                                new Template("Уникальный код оплаты",Types.Int ,null ,null,true,11,"22"),
                                new Template("Уникальный код услуги",Types.Int ,null ,null,true,1,"13"),
                                new Template("Код договора",Types.Int ,1, null,true,1,"5"),
                                new Template("Сумма", Types.Numeric, null, null)
                            }
                        },
                        {
                            "33", new List<Template>
                            {
                                new Template("Номер секции", Types.Int, 0, null),
                                new Template("№ ЛС в системе поставщика",Types.String ,null ,20,true,3,"4"),
                                new Template("Код услуги",Types.Int ,null ,null,true,1,"13"),
                                new Template("Код договора",Types.Int ,null ,null,true,1,"5"),
                                new Template("Код типа перекидки", Types.Int, null, null),
                                new Template("Сумма перекидки", Types.Numeric, null, null),
                                new Template("Тариф", Types.Numeric, null, null, false),
                                new Template("Расход", Types.Numeric, null, null, false),
                                new Template("Комментарий", Types.String, null, 100, false)
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
                FileName = GetFilesIfWorkWithArchive();
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
                    string val = null;
                    var section = 0;
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
                CreateProtocolIns(ref dt);
                SetProgress(1m);
                return new Returns();
            }
        }
    }

}


