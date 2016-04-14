using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Bars.KP50.Utils;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.DataImport.SOURCE.LOAD
{
    public enum ParameterTypes
    {
        Int32,
        Decimal,
        Text,
        DateTime
    }

    [Flags]
    public enum ParameterFilters
    {
        IsNull = 1,
        IsMoney = 2,
        Low = 4,
        Hight = 8,
        Lenght = 16
    }

    public static class ParametersExtentions
    {
        public static bool HasFlag(this Enum Source, Enum Value)
        {
            return getFlags(Source.ToInt()).Intersect(getFlags(Value.ToInt())).Count() > 0;
        }

        private static List<int> getFlags(int flagMult)
        {
            List<int> flags = new List<int>();
            while (flagMult != 0)
            {
                int pow = (Math.Pow(2,getPow(flagMult)).ToInt());
                flags.Add(pow);
                flagMult -= pow;
            }
            return flags;
        }

        private static int getPow(int flagMult)
        {
            int pow = 0;
            while (Math.Pow(2, pow) < flagMult)
                pow ++;
            return pow;
        }

    }




    public abstract class Parameter
    {
        protected virtual ParameterFilters IgnoreParameterFilters
        {
            get
            {
                return (
                    ParameterFilters.Lenght |
                    ParameterFilters.Low |
                    ParameterFilters.Hight |
                    ParameterFilters.IsMoney);
            }
        }

        protected object value;

        public virtual string Value
        {
            get { return value.ToString(); }
            set { this.value = value as object; }
        }

        public string FieldName { get; set; }
        public Type Type { get; set; }

        protected static List<string> Logger { get; private set; }

        protected static Dictionary<ParameterTypes, Type> DicRegistredTypes { get; private set; }

        protected Dictionary<ParameterFilters, object> DicFilters { get; private set; }

        static Parameter()
        {
            DicRegistredTypes = new Dictionary<ParameterTypes, Type>();
            DicRegistredTypes.Add(ParameterTypes.Int32, typeof (Int32Parameter));
            DicRegistredTypes.Add(ParameterTypes.Decimal, typeof (DecimalParameter));
            DicRegistredTypes.Add(ParameterTypes.Text, typeof (TextParameter));
            DicRegistredTypes.Add(ParameterTypes.DateTime, typeof (DateTimeParameter));
        }

        public bool AddFilter(ParameterFilters ParameterFilter, object Value)
        {
            if (!IgnoreParameterFilters.HasFlag(ParameterFilter))
            {
                DicFilters.Add(ParameterFilter, Value);
                return true;
            }
            return false;
        }

        public static Parameter GetInstance(ParameterTypes ParameterType)
        {
            Type InstanceType = null;
            return DicRegistredTypes.TryGetValue(ParameterType, out InstanceType)
                ? InstanceType.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, Type.EmptyTypes,
                    new ParameterModifier[] { }).Invoke(null) as Parameter
                : null;
        }

        public override string ToString()
        {
            return string.Join("\n", Logger.ToArray());
        }

        protected Parameter()
        {
            Logger = new List<string>();
            DicFilters = new Dictionary<ParameterFilters, object>();
            FieldName = "";
        }

        public void Check()
        {
            DicFilters.ForEach
                (row =>
                {
                    switch (row.Key)
                    {
                        case ParameterFilters.IsNull:
                            if (!row.Value.ToBool() && Value.IsNull())
                                Logger.Add("Не заполнено обязательное поле " + FieldName);
                            break;
                        case ParameterFilters.IsMoney:
                            if (Math.Abs(Value.ToDecimal() - decimal.Truncate(Value.ToDecimal())).ToString().Length > 4)
                                Logger.Add("Поле имеет неверный формат(дробная часть превышает 2 знака). Значение = " + Value + ", поле: " + FieldName);
                            break;
                        case ParameterFilters.Low:
                            if (Value.ToDecimal() < row.Value.ToDecimal())
                                Logger.Add("Поле " + FieldName + " меньше допустимого значения " + row.Value.ToDecimal());
                            break;
                        case ParameterFilters.Hight:
                            if (Value.ToDecimal() > row.Value.ToDecimal())
                                Logger.Add("Поле " + FieldName + " превышает допустимое значение " +
                                           row.Value.ToDecimal());
                            break;
                        case ParameterFilters.Lenght:
                            if (Value.Length > row.Value.ToInt())
                                Logger.Add("Поле " + FieldName + " превышает допустимое число символов " +
                                           row.Value.ToInt());
                            break;
                    }
                }
                );
        }
    }

    internal class Int32Parameter : Parameter
    {
        protected override ParameterFilters IgnoreParameterFilters
        {
            get
            {
                return (
                  ParameterFilters.Lenght |
                  ParameterFilters.IsMoney);
            }
        }

        private Int32Parameter()
            :base()
        {
            
        }
    }

    internal class DecimalParameter : Parameter
    {
        public override string Value {
            get { return this.value.ToString(); }
            set
            {
                decimal decimalParsed;
                if (Decimal.TryParse(value, out decimalParsed)) this.value = decimalParsed as object;
                else
                {
                    this.value = null;
                    Logger.Add("");
                };
            }
        }

        protected override ParameterFilters IgnoreParameterFilters
        {
            get { return ( ParameterFilters.Lenght ); }
        }

        private DecimalParameter()
            : base()
        {

        }
    }

    internal class TextParameter : Parameter
    {
        protected override ParameterFilters IgnoreParameterFilters
        {
            get
            {
                return (
                  ParameterFilters.Low |
                  ParameterFilters.Hight |
                  ParameterFilters.IsMoney);
            }
        }

        private TextParameter()
            : base()
        {

        }
    }

    internal class DateTimeParameter : Parameter
    {
        protected override ParameterFilters IgnoreParameterFilters
        {
            get
            {
                return (
                  ParameterFilters.Lenght |
                  ParameterFilters.Low |
                  ParameterFilters.Hight |
                  ParameterFilters.IsMoney);
            }
        }

        private DateTimeParameter()
            : base()
        {

        }
    }

}
