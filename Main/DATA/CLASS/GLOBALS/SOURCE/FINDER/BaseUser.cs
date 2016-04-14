using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.Global
{

    //класс для поиска
    [DataContract]
    public class BaseUser : Finder //данные пользователя
    {
        public BaseUser()
            : base()
        {
            login = "";
            password = "";
            info = "";
            uname = "";
            dat_log = "";
            idses = "";
            ip_log = "";
            browser = "";
            nzp_payer = 0;
            payer = "";
            nzp_area = 0;
            nzp_supp = 0;
            nzp_disp = 0;
            sessionId = "";
        }

        [DataMember]
        public string login { get; set; }
        [DataMember]
        public string password { get; set; }
        [DataMember]
        public string info { get; set; }
        [DataMember]
        public string uname { get; set; }
        [DataMember]
        public string dat_log { get; set; }
        [DataMember]
        public string idses { get; set; }
        [DataMember]
        public string ip_log { get; set; }
        [DataMember]
        public string browser { get; set; }
        [DataMember]
        public string sessionId { get; set; }

        /// <summary>
        /// Тип организации
        /// </summary>
        public OrganizationTypes organizationType
        {
            get
            {
                if (nzp_payer > 0)
                {
                    if (nzp_payer == Payers.DispatchingOffice.GetHashCode()) return OrganizationTypes.DispatchingOffice;
                    else if (nzp_area > 0) return OrganizationTypes.UK;
                    else if (nzp_supp > 0) return OrganizationTypes.Supplier;
                    else return OrganizationTypes.Other;
                }
                else return OrganizationTypes.None;
            }
        }

        /// <summary>
        /// Типы организаций, к которым принадлежат пользователи
        /// </summary>
        public enum OrganizationTypes
        {
            /// <summary>
            /// Организация не задана
            /// </summary>
            None = 0,

            /// <summary>
            /// Диспетчерская
            /// </summary>
            DispatchingOffice = 1,

            /// <summary>
            /// Управляющая компания (Управляющая организация)
            /// </summary>
            UK = 2,

            /// <summary>
            /// Поставщик услуг
            /// </summary>
            Supplier = 3,

            /// <summary>
            /// Прочие
            /// </summary>
            Other = 4
        }

        /// <summary>
        /// Код организации (подрядчик), к которой привязан пользователь
        /// </summary>
        [DataMember]
        public int nzp_payer { get; set; }

        /// <summary>
        /// Наименование организации, к которой привязан пользователь
        /// </summary>
        [DataMember]
        public string payer { get; set; }

        /// <summary>
        /// Код Управляющейй организации
        /// </summary>
        [DataMember]
        public int nzp_area { get; set; }

        /// <summary>
        /// Код поставщика
        /// </summary>
        [DataMember]
        public int nzp_supp { get; set; }

        /// <summary>
        /// Код диспетчерской
        /// </summary>
        [DataMember]
        public int nzp_disp { get; set; }
    }


    [DataContract]
    public class User : BaseUser
    {
        [DataMember]
        public int num { get; set; }
        [DataMember]
        public int nzpuser { get; set; }
        [DataMember]
        public string pwd { get; set; }
        [DataMember]
        public string email { get; set; }
        [DataMember]
        public byte is_blocked { get; set; }
        [DataMember]
        public byte is_remote { get; set; }
        [DataMember]
        public string blockName { get { if (is_blocked == 1) return "Заблокирован"; else return ""; } set { } }
        [DataMember]
        public int rolesNumber { get; set; }
        [DataMember]
        public List<Role> roles { get; set; }
        [DataMember]
        public int accessNumber { get; set; }
        [DataMember]
        public List<UserAccess> access { get; set; }
        [DataMember]
        public Role roleFinder { get; set; }
        [DataMember]
        public UserAccess accessFinder { get; set; }
        [DataMember]
        public string appointment { get; set; }
        [DataMember]
        public int nzp_bank { get; set; }

        //[DataMember]
        //public string bank { get; set; }

        /// <summary>
        /// признак, что пользователь на сайте
        /// > 0 - на сайте
        /// = 0 - не на сайте
        /// < 0 - не важно (применяется как фильтр для поиска)
        /// </summary>
        [DataMember]
        public int isOnline { get; set; }
        [DataMember]
        public string isOnlineText
        {
            get { if (isOnline > 0) return "На сайте"; else return ""; }
            set { }
        }

        /// <summary>
        /// уникальный идентификатор запроса на восстановление пароля
        /// </summary>
        [DataMember]
        public string requestId { get; set; }

        public User()
            : base()
        {
            num = 0;
            nzpuser = 0;
            pwd = "";
            uname = "";
            email = "";
            is_blocked = 0;
            is_remote = 0;
            rolesNumber = 0;
            roles = new List<Role>();
            accessNumber = 0;
            access = new List<UserAccess>();
            roleFinder = null;
            accessFinder = null;
            isOnline = 0;
            requestId = "";
            appointment = "";
            nzp_bank = 0;
            bank = "";
        }

    }

    [DataContract]
    public class Role : Finder
    {
        [DataMember]
        public int num { get; set; }
        [DataMember]
        public string role { get; set; }
        [DataMember]
        public int page_url { get; set; }
        [DataMember]
        public string page_name { get; set; }
        [DataMember]
        public int sort { get; set; }
        [DataMember]
        public int nzpuser { get; set; }
        [DataMember]
        public int is_active { get; set; }
        [DataMember]
        public int role_tip { get; set; }

        public Role()
            : base()
        {
            num = 0;
            nzp_role = 0;
            role = "";
            page_url = 0;
            page_name = "";
            sort = 0;
            nzpuser = 0;
            is_active = 1;
            role_tip = 0;
        }

    }

    [DataContract]
    public class UserAccess : Finder
    {
        [DataMember]
        public int num { get; set; }

        [DataMember]
        public int nzp_lacc { get; set; }

        [DataMember]
        public int nzpuser { get; set; }

        [DataMember]
        public int acc_kod { get; set; }

        [DataMember]
        public string accName
        {
            get
            {
                switch (acc_kod)
                {
                    case 1:
                        return "Вход в систему";
                    case 2:
                        return "Завершение работы";
                    default:
                        return "";
                }
            }
            set { }
        }

        [DataMember]
        public string dat_log { get; set; }

        [DataMember]
        public string dat_log_po { get; set; }

        [DataMember]
        public string ip_log { get; set; }

        [DataMember]
        public string browser { get; set; }

        [DataMember]
        public string login { get; set; }

        [DataMember]
        public string pwd { get; set; }

        [DataMember]
        public string idses { get; set; }

        [DataMember]
        public string dat_exit { get; set; }

        public UserAccess()
            : base()
        {
            num = 0;
            nzp_lacc = 0;
            nzpuser = 0;
            acc_kod = 0;
            dat_log = "";
            dat_log_po = "";
            ip_log = "";
            browser = "";
            login = "";
            pwd = "";
            idses = "";
            dat_exit = "";
        }
    }
}
