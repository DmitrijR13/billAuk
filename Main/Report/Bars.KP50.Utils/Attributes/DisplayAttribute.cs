namespace Bars.KP50.Utils
{
    using System;

    /// <summary>
    /// Аттрибут позволяющий указать отображаемое имя
    /// (DisplayName применим только к классам)
    /// </summary>
    [AttributeUsage(AttributeTargets.All, Inherited = true, AllowMultiple = true)]
    public class DisplayAttribute : StringValueAttribute
    {        
        public DisplayAttribute(string displayName)
            : base("Display", displayName)
        {
        }

        public DisplayAttribute()
            :this(string.Empty)
        {

        }
    }

    public class DescriptionAttribute : StringValueAttribute
    {
        public DescriptionAttribute(string description)
            :base ("Description", description)
        {
            
        }

        public DescriptionAttribute()
            :this(string.Empty)
        {
            
        }
    }
}