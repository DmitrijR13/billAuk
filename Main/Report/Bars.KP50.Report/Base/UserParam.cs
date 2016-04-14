namespace Bars.KP50.Report
{
    using System;
    
    /// <summary>Параметр отчета</summary>
    public abstract class UserParam
    {
        /// <summary>Тип значения</summary>
        public Type TypeValue { get; set; }

        /// <summary>Наименование параметра</summary>
        public string Name { get; set; }

        /// <summary>Ключ параметра (должен быть уникален в рамках одного отчета)</summary>
        public string Code { get; set; }

        /// <summary>Значение параметра</summary>
        public object Value { get; set; }

        /// <summary>Значение по умолчанию</summary>
        public object DefaultValue { get; set; }

        /// <summary>Является обязательным</summary>
        public bool Require { get; set; }

        /// <summary>Класс javascript'а отвечающего за отрисовку параметра</summary>
        public string JavascriptClassName { get; set; }
    }
}