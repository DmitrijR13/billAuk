
namespace Bars.KP50.Faktura.Source.Base.Barcode
{
    /// <summary>Тип штрих кода</summary>
    public class DataMatrix
    {
        private const string ST = "ST"; //Идентификаор формата
        private const string VersionOfStandard = "0001"; //версия стандарта
        private const string ЕncodingСode = "1";//код кодировки

        private string _name;
        private string _personalAcc;
        private string _bankName;
        private string _bic;
        private string _correspAcc;
        private string _payeeINN;
        private string _kpp;
        private string _purpose;
        private string _sum;
        private string _lastName;
        private string _firstName;
        private string _middleName;
        private string _payerAddress;
        private string _persAcc;
        private string _paymPeriod;

        #region реквизиты

        /// <summary>Блок служебных реквизитов</summary>
        public string ST00011
        {
            get { return ST + VersionOfStandard + ЕncodingСode; }
        }

        /// <summary>Наименование получателя платежа</summary>
        public string Name
        {
            get
            {
                if (_name != null)
                {
                    _name = ValidIllegalChar(_name);
                    if (_name.Trim() != string.Empty)
                        return "Name=" + (_name.Length > 160 ? _name.Substring(0, 160) : _name);
                }
                return string.Empty;
            }
            set { _name = value; }
        }

        /// <summary>Номер счета получателя платежа</summary>
        public string PersonalAcc
        {
            get
            {
                if (_personalAcc != null)
                {
                    _personalAcc = ValidIllegalChar(_personalAcc);
                    if (_personalAcc.Trim() != string.Empty)
                        return "PersonalAcc=" + (_personalAcc.Length > 20 ? _personalAcc.Substring(0, 20) : _personalAcc);
                }
                return string.Empty;
            }
            set { _personalAcc = value; }
        }

        /// <summary>Наименование банка получателя платежа</summary>
        public string BankName
        {
            get
            {
                if (_bankName != null)
                {
                    _bankName = ValidIllegalChar(_bankName);
                    if (_bankName.Trim() != string.Empty)
                        return "BankName=" + (_bankName.Length > 45 ? _bankName.Substring(0, 45) : _bankName);
                }
                return string.Empty;
            }
            set { _bankName = value; }
        }

        /// <summary>БИК</summary>
        public string BIC
        {
            get
            {
                if (_bic != null)
                {
                    _bic = ValidIllegalChar(_bic);
                    if (_bic.Trim() != string.Empty)
                        return "BIC=" + (_bic.Length > 9 ? _bic.Substring(0, 9) : _bic);
                }
                return string.Empty;
            }
            set { _bic = value; }
        }

        /// <summary>Номер кор./сч. банка получателя платежа</summary>
        public string CorrespAcc
        {
            get
            {
                if (_correspAcc != null)
                {
                    _correspAcc = ValidIllegalChar(_correspAcc);
                    if (_correspAcc.Trim() != string.Empty)
                        return "CorrespAcc=" + (_correspAcc.Length > 20 ? _correspAcc.Substring(0, 20) : _correspAcc);
                }
                return string.Empty;
            }
            set { _correspAcc = value; }
        }

        /// <summary>ИНН получвтеля платежа</summary>
        public string PayeeINN
        {
            get
            {
                if (_payeeINN != null)
                {
                    _payeeINN = ValidIllegalChar(_payeeINN);
                    if (_payeeINN.Trim() != string.Empty)
                        return "PayeeINN=" + (_payeeINN.Length > 12 ? _payeeINN.Substring(0, 12) : _payeeINN);
                }
                return string.Empty;
            }
            set { _payeeINN = value; }
        }

        /// <summary>КПП получателя платежа</summary>
        public string KPP
        {
            get
            {
                if (_kpp != null)
                {
                    _kpp = ValidIllegalChar(_kpp);
                    if (_kpp.Trim() != string.Empty)
                        return "KPP=" + (_kpp.Length > 9 ? _kpp.Substring(0, 9) : _kpp);
                }
                return string.Empty;
            }
            set { _kpp = value; }
        }

        /// <summary>Фамилия плательщика</summary>
        public string LastName
        {
            get
            {
                if (_lastName != null)
                {
                    _lastName = ValidIllegalChar(_lastName);
                    if (_lastName.Trim() != string.Empty)
                        return "LastName=" + _lastName;
                }
                return string.Empty;
            }
            set { _lastName = value; }
        }

        /// <summary>Имя плательщика</summary>
        public string FirstName
        {
            get
            {
                if (_firstName != null)
                {
                    _firstName = ValidIllegalChar(_firstName);
                    if (_firstName.Trim() != string.Empty)
                        return "FirstName=" + _firstName;
                }
                return string.Empty;
            }
            set { _firstName = value; }
        }

        /// <summary>Отчество плательщика</summary>
        public string MiddleName
        {
            get
            {
                if (_middleName != null)
                {
                    _middleName = ValidIllegalChar(_middleName);
                    if (_middleName.Trim() != string.Empty)
                        return "MiddleName=" + _middleName;
                }
                return string.Empty;
            }
            set { _middleName = value; }
        }

        /// <summary>Адрес плательщика</summary>
        public string PayerAddress
        {
            get
            {
                if (_payerAddress != null)
                {
                    _payerAddress = ValidIllegalChar(_payerAddress);
                    if (_payerAddress.Trim() != string.Empty)
                        return "PayerAddress=" + _payerAddress;
                }
                return string.Empty;
            }
            set { _payerAddress = value; }
        }

        /// <summary>Назначения платежа</summary>
        public string Purpose
        {
            get
            {
                if (_purpose != null)
                {
                    _purpose = ValidIllegalChar(_purpose);
                    if (_purpose.Trim() != string.Empty)
                        return "Purpose=" + (_purpose.Length > 210 ? _purpose.Substring(0, 210) : _purpose);
                }
                return string.Empty;
            }
            set { _purpose = value; }
        }

        /// <summary>Сумма платежа в копейках</summary>
        public string Sum
        {
            get
            {
                if (_sum != null)
                {
                    _sum = ValidIllegalChar(_sum);
                    if (_sum.Trim() != string.Empty)
                        return "Sum=" + (_sum.Length > 18 ? _sum.Substring(0, 18) : _sum);
                }
                return string.Empty;
            }
            set { _sum = value; }
        }

        /// <summary>Номер лицевого счета плательщика в организации (в системе учета ПУ)</summary>
        public string PersAcc
        {
            get
            {
                if (_persAcc != null)
                {
                    _persAcc = ValidIllegalChar(_persAcc);
                    if (_persAcc.Trim() != string.Empty)
                        return "PersAcc=" + _persAcc;
                }
                return string.Empty;
            }
            set { _persAcc = value; }
        }

        /// <summary>Период оплаты</summary>
        public string PaymPeriod
        {
            get
            {
                if (_paymPeriod != null)
                {
                    _paymPeriod = ValidIllegalChar(_paymPeriod);
                    if (_paymPeriod.Trim() != string.Empty)
                        return "PaymPeriod=" + _paymPeriod;
                }
                return string.Empty;
            }
            set { _paymPeriod = value; }
        }

        #endregion

        /// <summary>Закодированная строка</summary>
        public string EncodedString { get; private set; }

        /// <summary>Лог</summary>
        public string Log { get; set; }

        /// <summary>Инициализация типа</summary>
        public DataMatrix()
        {
            DefaultPadding();
        }

        /// <summary>Инициализация типа. На входе - обязательные реквизиты</summary>
        /// <param name="name">Наименование получателя платежа</param>
        /// <param name="personalAcc">Номер счета получателя платежа</param>
        /// <param name="bankName">Наименование банка получателя платежа</param>
        /// <param name="bic">БИК</param>
        public DataMatrix(string name, string personalAcc, string bankName, string bic)
        {
            DefaultPadding();
            _name = name;
            _personalAcc = personalAcc;
            _bankName = bankName;
            _bic = bic;
        }

        /// <summary>Заполнение локальных переменных по умолчанию</summary>
        private void DefaultPadding()
        {
            _name = string.Empty;
            _personalAcc = string.Empty;
            _bankName = string.Empty;
            _bic = string.Empty;
            _correspAcc = string.Empty;
            _payeeINN = string.Empty;
            _kpp = string.Empty;
            _purpose = string.Empty;
            _sum = string.Empty;
            _lastName = string.Empty;
            _firstName = string.Empty;
            _middleName = string.Empty;
            _payerAddress = string.Empty;
            _persAcc = string.Empty;
            _paymPeriod = string.Empty;

            Log = string.Empty;
        }

        /// <summary>Проверка на запрещенный символ</summary>
        /// <param name="string">Прверяемая строка</param>
        /// <returns>Проверенная строка</returns>
        private string ValidIllegalChar(string @string)
        {
            if (@string.IndexOf('|') != -1)
            {
                Log += "В строке '" + @string + "' имеется запрещенный символ '|'\n";
                @string = @string.Replace("|", "");
            }
            return @string;
        }

        /// <summary>Кодирование платежа</summary>
        /// <returns>Строка с закодированным платежом</returns>
        public bool CodingPayment()
        {
            if (Name == string.Empty || PersonalAcc == string.Empty || BankName == string.Empty || BIC == string.Empty)
            {
                Log += Name == string.Empty
                    ? "Не указано значение: Name(Наименование получателя платежа)\n"
                    : string.Empty;
                Log += PersonalAcc == string.Empty
                    ? "Не указано значение: PersonalAcc(Номер счета получателя платежа)\n"
                    : string.Empty;
                Log += BankName == string.Empty
                    ? "Не указано значение: BankName(Наименование банка получателя платежа)\n"
                    : string.Empty;
                Log += BIC == string.Empty
                    ? "Не указано значение: BIC(БИК)\n"
                    : string.Empty;
                return false;
            }

            EncodedString = ST00011 + "|" + Name + "|" + PersonalAcc + "|" + BankName + "|" + BIC;
            EncodedString += CorrespAcc != string.Empty ? "|" + CorrespAcc : string.Empty;
            EncodedString += PayeeINN != string.Empty ? "|" + PayeeINN : string.Empty;
            EncodedString += KPP != string.Empty ? "|" + KPP : string.Empty;
            EncodedString += LastName != string.Empty ? "|" + LastName : string.Empty;
            EncodedString += FirstName != string.Empty ? "|" + FirstName : string.Empty;
            EncodedString += MiddleName != string.Empty ? "|" + MiddleName : string.Empty;
            EncodedString += PayerAddress != string.Empty ? "|" + PayerAddress : string.Empty;
            EncodedString += Purpose != string.Empty ? "|" + Purpose : string.Empty;
            EncodedString += Sum != string.Empty ? "|" + Sum : string.Empty;
            EncodedString += PersAcc != string.Empty ? "|" + PersAcc : string.Empty;
            EncodedString += PaymPeriod != string.Empty ? "|" + PaymPeriod : string.Empty;

            return true;
        }


    }
}
