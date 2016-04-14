namespace Bars.KP50.Utils
{
    /// <summary>
    /// Атрибут мета-данных.
    /// Позволяет указать любую дополнительную мета-информацию.
    /// </summary>
    public class CustomValueAttribute : CustomAttribute
    {
        /// <summary>
        /// Ключ
        /// </summary>
        public string Key { get; protected set; }

        /// <summary>
        /// Значение
        /// </summary>
        public object Value { get; protected set; }

        public CustomValueAttribute(string key, object value)
            :base()
        {
            Key = key;
            Value = value;
        }

        public CustomValueAttribute(string key)
            : this(key, null)
        {
            
        }
    }

    public class StringValueAttribute : CustomValueAttribute
    {
        public new string Value
        {
            get { return (string) base.Value; }
            set { base.Value = value; }
        }

        public StringValueAttribute(string key, string value) 
            : base(key, value)
        {
        }

        public StringValueAttribute(string key) 
            : this(key, string.Empty)
        {
        }
    }
}
