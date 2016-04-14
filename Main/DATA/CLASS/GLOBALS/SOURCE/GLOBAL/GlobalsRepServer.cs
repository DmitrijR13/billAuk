using System;
using System.Text;
using System.Data;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Runtime.Serialization;
using System.Threading;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Utility;
using System.IO;
using Globals.SOURCE.INTF;

namespace STCLINE.KP50.Global
{
    
  

    #region для универсального сервера отчета

    /// <summary>
    /// класс для параметров отчета
    /// </summary>
    [Serializable]
    public class ReportParams : EntityDescription
    {
        //базовый конструктор
        public ReportParams()
        {
            isSendEmail = false;
            fileType = ReportType.Pdf;
            prmsStr = "";
            param = new List<ReportParameters>();
            dicts = new List<Dict>();
            reportFinder = new ReportFinder();
            prms = new List<int>();
            selectedValues = new Dictionary<int, string>();
        }

        /// <summary>
        /// параметры отчета для шаблона отчета
        /// </summary>
        [DataMember]
        public List<ReportParameters> param { set; get; }

        /// <summary>
        /// справочник выбранных значений
        /// </summary>
        [DataMember]
        public Dictionary<int, string> selectedValues { set; get; }

        /// <summary>
        /// название файла выгружаемого отчета
        /// </summary>
        [DataMember]
        public string fileName { set; get; }

        /// <summary>
        /// идентификатор выгрузки отчета в базе
        /// </summary>
        [DataMember]
        public int nzp { set; get; }

        /// <summary>
        /// объект для работы с переменными программы Комплат
        /// </summary>
        [DataMember]
        public ReportFinder reportFinder { set; get; }

        /// <summary>
        /// наименование dll файла отчета
        /// </summary>
        [DataMember]
        public string dllName { set; get; }

        /// <summary>
        /// время записи отчета в базу
        /// </summary>
        public string date_in { set; get; }

        /// <summary>
        /// {параметр: значение}
        /// </summary>
        public string prmsStr { set; get; }

        /// <summary>
        /// справочники
        /// </summary>
        [DataMember]
        public List<Dict> dicts { set; get; }

        /// <summary>
        /// даты/периоды
        /// </summary>
        [DataMember]
        public List<DatePeriod> datePeriods { set; get; }

        /// <summary>
        /// тип файла отчета
        /// </summary>
        [DataMember]
        public ReportType fileType { set; get; }

        /// <summary>
        /// признак : отослать по email после выгрузки
        /// true: отправить
        /// </summary>
        [DataMember]
        public bool isSendEmail { set; get; }

        /// <summary>
        /// приоритет отчета
        /// </summary>
        [DataMember]
        public int priority { set; get; }

        /// <summary>
        /// идентификатор пользователя
        /// </summary>
        [DataMember]
        public int nzp_user { set; get; }

        /// <summary>
        /// список параметров отчета
        /// </summary>
        [DataMember]
        public List<int> prms { set; get; }

        /// <summary>
        /// комментарий
        /// </summary>
        [DataMember]
        public string comment { set; get; }

        /// <summary>
        /// задача запущена/не запущена
        /// </summary>
        [DataMember]
        public bool isRunned { set; get; }

        /// <summary>
        /// задача
        /// </summary>
        [DataMember]
        public Action<ReportParams> work { set; get; }

        /// <summary>
        /// название шаблона отчета
        /// </summary>
        [DataMember]
        public string ftemplateName { set; get; }

        /// <summary>
        /// путь к шаблону отчета
        /// </summary>
        [DataMember]
        public string ftemplatePath { set; get; }


        /// <summary>
        /// путь к сохранению готового отчета
        /// </summary>
        [DataMember]
        public string exportPath { set; get; }


        /// <summary>
        /// Запускает отчет
        /// </summary>
        public void Execute(ReportParams prm)
        {
            work(prm);
        }

        public void AddParameter(string name, string value)
        {
            ReportParameters p = new ReportParameters(name, value);
            param.Add(p);
        }

        /// <summary>
        /// Приоритет задачи(устанавливается только при создании задачи)
        /// </summary>
        public int getPriority
        {
            get { return priority; }
        }

        /// <summary>
        /// Запущена ли задача(true - запущена, false - стоит в очереди на выполнение)
        /// </summary>
        public bool IsRunned
        {
            get { return isRunned; }
        }

        /// <summary>
        /// получение приоритета
        /// </summary>
        /// <param name="tip"></param>
        /// <returns></returns>
        public static ReportPriority getPriorityEnum(int priority)
        {
            switch (priority)
            {
                case (int)ReportPriority.High: return ReportPriority.High;
                case (int)ReportPriority.Low: return ReportPriority.Low;
                case (int)ReportPriority.Normal: return ReportPriority.Normal;
                default: return ReportPriority.None;
            }
        }
    }

    [Serializable]
    [DataContract]
    public class ReportFinder
    {
        /// <summary>
        /// префикс базы данных
        /// </summary>
        [DataMember]
        public string pref { set; get; }

        /// <summary>
        /// текущий расчетный месяц
        /// </summary>
        [DataMember]
        public int calcMonth { set; get; }

        /// <summary>
        /// текущий расчетный год
        /// </summary>
        [DataMember]
        public int calcYear { set; get; }

        /// <summary>
        /// строка подключения
        /// </summary>
        [DataMember]
        public string connWebString { set; get; }

        /// <summary>
        /// строка подключения
        /// </summary>
        [DataMember]
        public string connKernelString { set; get; }

        /// <summary>
        /// список префиксов БД
        /// </summary>
        [DataMember]
        public List<_Point> pointList { set; get; }
    }


    /// <summary>
    /// параметры отчета
    /// </summary>
    [Serializable]
    [DataContract]
    public class ReportParameters
    {
        /// <summary>
        /// конструктор по умолчанию
        /// </summary>
        public ReportParameters(string pname, string pvalue)
        {
            name = pname;
            value = pvalue;
        }

        /// <summary>
        /// имя/название
        /// </summary>
        [DataMember]
        public string name { set; get; }

        /// <summary>
        /// значение параметра
        /// </summary>
        [DataMember]
        public string value { set; get; }
    }

    /// <summary>
    /// универсальный класс
    /// </summary>
    [Serializable]
    [DataContract]
    public class EntityDescription
    {
        /// <summary>
        /// идентификатор
        /// </summary>
        [DataMember]
        public int id { set; get; }

        /// <summary>
        /// имя/название
        /// </summary>
        [DataMember]
        public string name { set; get; }

        /// <summary>
        /// имя параметра для доступа к значению 
        /// элемента в шаблоне отчета
        /// </summary>
        [DataMember]
        public string paramName { set; get; }

        /// <summary>
        /// текст/лейбл
        /// </summary>
        [DataMember]
        public string label { set; get; }

        /// <summary>
        /// дополнительное описание для пользователя
        /// (пример: выберите значение...)
        /// </summary>
        [DataMember]
        public string text { set; get; }
    }

    /// <summary>
    /// класс справочник
    /// </summary>
    [Serializable]
    [DataContract]
    public class Dict : EntityDescription
    {
        //базовый конструктор
        public Dict()
        {
            extraParam = new ExtraParams();
        }

        /// <summary>
        /// имя спрвочника для отображения(label)
        /// </summary>
        [DataMember]
        public string shortName { set; get; }

        /// <summary>
        /// тип справочника
        /// </summary>
        [DataMember]
        public int type { set; get; }

        /// <summary>
        /// данные справочника
        /// </summary>
        [DataMember]
        public List<DictionaryItem> items { set; get; }

        /// <summary>
        /// признак возможности множественного выбора
        /// </summary>
        [DataMember]
        bool isMultiselect { set; get; }

        /// <summary>
        /// элементы управления справочников
        /// </summary>
        [DataMember]
        public ExtraParams extraParam { set; get; }

        /// <summary>
        /// получение типа
        /// </summary>
        /// <param name="tip"></param>
        /// <returns></returns>
        public ExtraParamEditor getTypeEnum(int type)
        {
            switch (type)
            {
                case (int)ExtraParamEditor.CheckBox: return ExtraParamEditor.CheckBox;
                case (int)ExtraParamEditor.ComboBox: return ExtraParamEditor.ComboBox;
                case (int)ExtraParamEditor.RadioButton: return ExtraParamEditor.RadioButton;
                case (int)ExtraParamEditor.TextBox: return ExtraParamEditor.TextBox;
                default: return ExtraParamEditor.None;
            }
        }
    }

    /// <summary>
    /// класс элемент
    /// </summary>
    [Serializable]
    [DataContract]
    public class DictionaryItem
    {
        /// <summary>
        /// ключ
        /// </summary>
        [DataMember]
        public int key { set; get; }

        /// <summary>
        /// значение
        /// </summary>
        [DataMember]
        public string value { set; get; }

        /// <summary>
        /// признак выбранности элемента
        /// </summary>
        [DataMember]
        public bool isSelect { set; get; }
    }

    /// <summary>
    /// класс даты
    /// </summary>]
    [Serializable]
    [DataContract]
    public class DatePeriod : EntityDescription
    {
        /// <summary>
        /// дата с
        /// </summary>
        [DataMember]
        public DatePeriodItem from { set; get; }

        /// <summary>
        /// дата по
        /// </summary>
        [DataMember]
        public DatePeriodItem to { set; get; }

        /// <summary>
        /// тип
        /// </summary>
        [DataMember]
        public int type { set; get; }

        /// <summary>
        /// формат отображения
        /// </summary>
        [DataMember]
        public string format { set; get; }

        /// <summary>
        /// получение типа
        /// </summary>
        /// <param name="tip"></param>
        /// <returns></returns>
        public static DatePeriodType getTypeEnum(int type)
        {
            switch (type)
            {
                case (int)DatePeriodType.Day: return DatePeriodType.Day;
                case (int)DatePeriodType.Month: return DatePeriodType.Month;
                case (int)DatePeriodType.Quarter: return DatePeriodType.Quarter;
                case (int)DatePeriodType.Week: return DatePeriodType.Week;
                case (int)DatePeriodType.Year: return DatePeriodType.Year;
                default: return DatePeriodType.None;
            }
        }
    }

    /// <summary>
    /// класс элемента класса даты
    /// </summary>
    [Serializable]
    [DataContract]
    public class DatePeriodItem
    {
        /// <summary>
        /// значение
        /// </summary>
        [DataMember]
        public DateTime value { set; get; }

        /// <summary>
        /// значение по умолчанию
        /// </summary>
        [DataMember]
        public DateTime defaultValue { set; get; }
    }

    /// <summary>
    /// дополнительный параметр
    /// </summary>
    [Serializable]
    [DataContract]
    public class ExtraParams
    {
        //базовый конструктор
        public ExtraParams()
        {
            checkBoxes = new CheckBoxEditor();
            textBoxes = new TextBoxEditor();
            radioButtons = new RadioButtonEditor();
            comboBoxes = new ComboBoxEditor();
        }

        /// <summary>
        /// чекбоксы
        /// </summary>
        [DataMember]
        public CheckBoxEditor checkBoxes { set; get; }

        /// <summary>
        /// текстбоксы
        /// </summary>
        [DataMember]
        public TextBoxEditor textBoxes { set; get; }

        /// <summary>
        /// радиобаттоны
        /// </summary>
        [DataMember]
        public RadioButtonEditor radioButtons { set; get; }

        /// <summary>
        /// комбобоксы
        /// </summary>
        public ComboBoxEditor comboBoxes { set; get; }
    }

    /// <summary>
    /// класс элемента чекбокса доп. параметра
    /// </summary>
    [Serializable]
    [DataContract]
    public class CheckBoxEditor : EntityDescription
    {
        /// <summary>
        /// значение
        /// </summary>
        [DataMember]
        public string value { set; get; }

        /// <summary>
        /// выбранность по умолчанию
        /// </summary>
        [DataMember]
        public bool isSelect { set; get; }
    }

    /// <summary>
    /// класс элемента текстбокса доп. параметра
    /// </summary>
    [Serializable]
    [DataContract]
    public class TextBoxEditor : EntityDescription
    {
        /// <summary>
        /// значение по умолчанию
        /// </summary>
        [DataMember]
        public string defaultValue { set; get; }

        /// <summary>
        /// валидация/маска текстбокса
        /// </summary>
        [DataMember]
        public string validation { set; get; }
    }

    /// <summary>
    /// класс элемента радиобаттона доп. параметра
    /// </summary>
    [Serializable]
    [DataContract]
    public class RadioButtonEditor : EntityDescription
    {
        /// <summary>
        /// элементы
        /// bool: true - элемент выбран
        /// string: значение
        /// </summary>
        [DataMember]
        public Dictionary<bool, string> values { set; get; }

        HashSet<string> secondary = new HashSet<string>(/*StringComparer.InvariantCultureIgnoreCase*/);

        public void AddValue(bool k, string v)
        {
            if (values.ContainsKey(k))
            {
                throw new Exception("RadioButton уже имеет выбранный элемент");
            }

            if (secondary.Add(v))
            {
                throw new Exception("RadioButton с таким значением уже был добавлен");
            }
            values.Add(k, v);
        }
    }

    /// <summary>
    /// класс элемента комбобокса доп. параметра
    /// </summary>
    [Serializable]
    [DataContract]
    public class ComboBoxEditor : EntityDescription
    {
        /// <summary>
        /// значения
        /// int - ключ
        /// string - отображаемое значение
        /// </summary>
        [DataMember]
        public Dictionary<int, string> values { set; get; }

        /// <summary>
        /// выбранный элемент по умолчанию
        /// </summary>
        public int defaultValue { set; get; }
    }

    /// <summary>
    /// перечислитель тип доп. параметра
    /// </summary>
    public enum ExtraParamEditor
    {
        /// <summary>
        /// не задано
        /// </summary>
        None = 0,

        /// <summary>
        /// чекбокс
        /// </summary>
        CheckBox = 1,

        /// <summary>
        /// текстбокс
        /// </summary>
        TextBox = 2,

        /// <summary>
        /// радиобаттон
        /// </summary>
        RadioButton = 3,

        /// <summary>
        /// комбобокс
        /// </summary>
        ComboBox = 4
    }

    /// <summary>
    /// тип файла отчета
    /// </summary>
    public enum ReportType
    {
        /// <summary>
        /// .xls файл
        /// </summary>
        Excel = 1,

        /// <summary>
        /// .pdf файд
        /// </summary>
        Pdf = 2,

        /// <summary>
        /// .doc файл
        /// </summary>
        Doc = 3,

        /// <summary>
        /// .rtf файл
        /// </summary>
        Rtf = 4,

        /// <summary>
        /// .jpeg файл
        /// </summary>
        Jpeg = 5
    }

    /// <summary>
    /// перечислитель приоритета отчета
    /// </summary>
    public enum ReportPriority
    {
        /// <summary>
        /// не задано
        /// </summary>
        None = 0,

        /// <summary>
        /// Высокий приоритет
        /// </summary>
        High = 2,
        /// <summary>
        /// Средний приоритет
        /// </summary>
        Normal = 1,
        /// <summary>
        /// Низкий приоритет
        /// </summary>
        Low = 0
    }

    /// <summary>
    /// перечислитель тип периода
    /// </summary>
    public enum DatePeriodType
    {
        /// <summary>
        /// не задано
        /// </summary>
        None = 0,

        /// <summary>
        /// день
        /// </summary>
        Day = 1,

        /// <summary>
        /// неделя
        /// </summary>
        Week = 2,

        /// <summary>
        /// месяц
        /// </summary>
        Month = 3,

        /// <summary>
        /// квартал
        /// </summary>
        Quarter = 4,

        /// <summary>
        /// месяц
        /// </summary>
        Year = 5
    }

    /// <summary>
    /// перечислитель тип отчета
    /// </summary>
    public enum ReportSverType
    {
        /// <summary>
        /// лицевые счета
        /// </summary>
        Ls = 1,

        /// <summary>
        /// дома
        /// </summary>
        Dom = 2,

        /// <summary>
        /// услуги
        /// </summary>
        Service = 3
    }

    #endregion

}
