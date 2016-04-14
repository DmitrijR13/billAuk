using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using STCLINE.KP50.Global;


namespace STCLINE.KP50.Bars.Interfaces
{
    //----------------------------------------------------------------------------------------
    //Сведения о компании, версии и другие данные
    //----------------------------------------------------------------------------------------
    [ServiceContract(Namespace = Constants.Linespace, Name = "IAboutCompany")]
    public interface IAboutCompany
    {
        //получить название компании
        [OperationContract(Name = "GetAboutCompany")]
        AboutResult GetAboutCompany();
    }
    [DataContract(Namespace = Constants.Linespace, Name = "AboutCompany")]
    public class AboutCompany
    {
        string _nameCompany; //
        string _nameSoft; //
        string _version;  //
        string _comment;  //

        [DataMember(Name = "nameCompany", Order = 0)]
        public string nameCompany { get { return Utils.ENull(_nameCompany); } set { _nameCompany = value; } }
        [DataMember(Name = "nameSoft", Order = 1)]
        public string nameSoft { get { return Utils.ENull(_nameSoft); } set { _nameSoft = value; } }
        [DataMember(Name = "version", Order = 2)]
        public string version { get { return Utils.ENull(_version); } set { _version = value; } }
        [DataMember(Name = "comment", Order = 3)]
        public string comment { get { return Utils.ENull(_comment); } set { _comment = value; } }

        public AboutCompany()
        {
            _nameCompany = "Данные не заданы";
            _nameSoft = "Коммунальные платежи. Версия 5.0";
            _version  = "1.0.0";
            _comment  = "тестовый формат от 28.02.2011";
        }
    }
    [DataContract(Namespace = Constants.Linespace, Name = "AboutResult")]
    public class AboutResult : ServiceResult
    {
        [DataMember(Name = "aboutCompany")]
        public AboutCompany about;

        public AboutResult()
        {
            retcode = Utils.InitReturns();
            about = new AboutCompany();
        }
    }

    //----------------------------------------------------------------------------------------
    //Данные о лицевом счете
    //----------------------------------------------------------------------------------------
    [ServiceContract(Namespace = Constants.Linespace, Name = "IAdres")]
    public interface IAdres
    {
        //получить строку адреса
        [OperationContract(Name = "GetAdresString")]
        AdresResult GetAdresString(AdresID adresID);

        //проверить корректность платежного кода
        [OperationContract(Name = "ValidNumID")]
        AdresResult ValidNumID(string numID);
    }
    [DataContract(Namespace = Constants.Linespace, Name = "AdresID")]
    [KnownType(typeof(Counter))]
    public class AdresID //базовый класс для адреса 
    {
        Int64   _numID;   //платежный код
        string  _numFlat; //номер квартиры

        [DataMember(Name = "numID", Order = 0)]
        public Int64 numID { get { return _numID; } set { _numID = value; } }
        [DataMember(Name = "numFlat", Order = 1)]
        public string numFlat { get { return Utils.ENull(_numFlat); } set { _numFlat = value; } }

        public AdresID()
        {
            numID   = 0;
            numFlat = "";
        }
    }
    [DataContract(Namespace = Constants.Linespace, Name = "AdresResult")]
    public class AdresResult : ServiceResult
    {
        [DataMember(Name = "adres", Order = 1)]
        public string adres;

        public AdresResult()
        {
            retcode = Utils.InitReturns();
            adres = "-";
        }
    }

    //----------------------------------------------------------------------------------------
    //Базовый класс возврата результов
    //----------------------------------------------------------------------------------------
    [DataContract(Namespace = Constants.Linespace, Name = "ServiceResult")]
    [KnownType(typeof(AboutResult))]
    [KnownType(typeof(AdresResult))]
    [KnownType(typeof(CounterResult))]
    public class ServiceResult //
    {
        [DataMember(Name = "retcode", Order = 0)]
        public Returns retcode;

        public ServiceResult()
        {
            retcode = Utils.InitReturns();
        }
    }

}

