using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using STCLINE.KP50.EPaspXsd;

namespace Globals.SOURCE.INTF._EPasp.classes
{
    public class custom_multiApartmentBuilding : multiApartmentBuilding
    {
        /// <summary>
        /// префик БД
        /// </summary>
        public string pref { set; get; }
    }

    public class custom_mabResourceSupplyOrganization : mabResourceSupplyOrganization
    {
        /// <summary>
        /// идентификатор услуги
        /// </summary>
        public int nzp_serv { set; get; }
    }

    public class custom_flat : flat
    {
        /// <summary>
        /// лицевой счет
        /// </summary>
        public custom_personalAccount personalAccount { set; get; }

        /// <summary>
        /// показания счетчиков
        /// </summary>
        public List<measurementReading> measurementReading { set; get; }
    }

    public class custom_personalAccount : personalAccount
    {
        public custom_personalAccount()
        {
            registeredPersons = new List<custom_apartmentRegisteredPerson>();
            temporaryRegisteredPersons = new List<custom_apartmentRegisteredPerson>();
            residentCountPeriods = new personalAccountNaturPersonCount();
        }
        /// <summary>
        /// зарегистрированные граждане
        /// </summary>
        public List<custom_apartmentRegisteredPerson> registeredPersons { set; get; }

        /// <summary>
        /// временно зарегистрированные граждане
        /// </summary>
        public List<custom_apartmentRegisteredPerson> temporaryRegisteredPersons { set; get; }

        /// <summary>
        /// количество проживающих по периодам
        /// </summary>
        public personalAccountNaturPersonCount residentCountPeriods { set; get; }

        /// <summary>
        /// количество зарегистрированных граждан по периодам
        /// </summary>
        public personalAccountNaturPersonCount registeredPersonCountPeriods { set; get; }
    }

    public class custom_apartmentRegisteredPerson : apartmentRegisteredPerson
    {
        /// <summary>
        /// физ. лицо
        /// </summary>
        public naturPerson naturPerson { set; get; }

        /// <summary>
        /// дата регистрации
        /// </summary>
        public string registrationStartDate { set; get; }

        /// <summary>
        /// дата окончания регистрации
        /// </summary>
        public string registrationEndDate { set; get; }
    }
}
