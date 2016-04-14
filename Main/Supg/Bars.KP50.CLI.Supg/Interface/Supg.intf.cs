using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.Runtime.Serialization;
using System.Data;
using STCLINE.KP50.Global;
using System.Collections;

namespace STCLINE.KP50.Interfaces
{
    [ServiceContract]
    public interface I_Supg
    {

        //Сервис недопоставок
        [OperationContract]
        bool NedopService(int proc, JobOrder finder, out Returns ret);

        [OperationContract]
        Dictionary<int,string> GetClassMessage(OrderContainer finder, out Returns ret);

        [OperationContract]
        int AddOrder(OrderContainer finder);

        [OperationContract]
        List<OrderContainer> Find_Orders(OrderContainer finder, out Returns ret);

        [OperationContract]
        List<ServiceForwarding> GetServices(OrderContainer finder, out Returns ret);

        [OperationContract]
        bool AddReaddress(ServiceForwarding finder, out Returns ret);

        [OperationContract]
        List<ServiceForwarding> GetReadress(ServiceForwarding finder, out Returns ret);

        [OperationContract]
        ServiceForwarding GetServiceForward_One(ServiceForwarding finder, out Returns ret);

        [OperationContract]
        bool SaveCommentsReadress(ServiceForwarding finder, out Returns ret);

        [OperationContract]
        OrderContainer Result_Generating_Procedure(OrderContainer Container, out Returns ret);

        [OperationContract]
        OrderContainer Find_Orders_One(OrderContainer finder, out Returns ret);

        [OperationContract]
        bool UpdateZvk(OrderContainer finder, out Returns ret);

        [OperationContract]
        Dictionary<int, string> GetAllServices(int nzp_user, out Returns ret);

        [OperationContract]
        Dictionary<int, string> GetDest(int nzp_serv, int nzp_user, out Returns ret);

        [OperationContract]
        Dictionary<string, Dictionary<int, string>> GetSupgLists(SupgFinder finder, out Returns ret);

        [OperationContract]
        List<Dest> GetDestName(int nzp_serv, enSrvOper oper, out Returns ret);

        [OperationContract]
        Dest GetNedops(Dest finder, out Returns ret);

        [OperationContract]
        //string GetSupplier(int nzp_kvar, int nzp_user, int nzp_serv, string act_date, out Returns ret);
        string GetSupplier(JobOrder finder, out Returns ret);

        [OperationContract]
        Dictionary<int, string> GetSuppliersAll(int nzp_user, int supp_filter, string pref, out Returns ret);

        [OperationContract]
        bool AddJobOrder(ref JobOrder finder, out Returns ret);

        [OperationContract]
        List<JobOrder> GetJobOrders(OrderContainer finder, out Returns ret);

        [OperationContract]
        JobOrder GetJobOrderForm(JobOrder finder, out Returns ret);

        [OperationContract]
        Dictionary<int, string> GetJobOrderResultsAll(Finder finder, out Returns ret);

        [OperationContract]
        decimal GetDolgLs(Ls finder, int dat_y, int dat_m, out Returns ret);

        [OperationContract]
        Dictionary<int, string> GetAttistation(Finder finder, out Returns ret);

        [OperationContract]
        int FindZvk(SupgFinder finder, out Returns ret);

        //[OperationContract]
        //bool UpdateZakaz(JobOrder finder, out Returns ret);

        [OperationContract]
        List<ZvkFinder> GetFindZvk(SupgFinder finder, int flag, out Returns ret);

        [OperationContract]
        bool AddRepeatedJobOrder(ref JobOrder finder, out Returns ret);

        [OperationContract]
        bool IsOrderClose(JobOrder finder, out Returns ret);

        [OperationContract]
        List<JobOrder> GetNedopsAll(JobOrder finder, out Returns ret);

        [OperationContract]
        bool CopyFields_WhenResultChanged(JobOrder finder, out Returns ret);

        [OperationContract]
        bool AddNedopJobOrder(JobOrder finder, out Returns ret);

        [OperationContract]
        bool UpdateZvk_armOperator(OrderContainer finder, out Returns ret);

        [OperationContract]
        Returns DbChangeMarksSpisSupg(SupgFinder finder, List<SupgFinder> list1, List<SupgFinder> list2);

        [OperationContract]
        bool UpdateStatusJobOrder(JobOrder finder, int status, out Returns ret);

        [OperationContract]
        bool UpdateJobOrder(JobOrder finder,enSupgProc proc ,out Returns ret);

        [OperationContract]
        List<Journal> GetJournal(Journal finder, out Returns ret);

        [OperationContract]
        bool NedopForming(Journal finder, out Returns ret);

        [OperationContract]
        bool NedopPlacement(Journal finder, out Returns ret);

        [OperationContract]
        bool NedopUnload(Journal finder, out Returns ret);
        
        [OperationContract]
        List<JobOrder> GetSpisNedop(JobOrder job_ord, out Returns ret);

        [OperationContract]
        int SetZakazActActual(SupgFinder finder, out Returns ret);

        [OperationContract]
        bool DeleteFromJournal(Journal finder, out Returns ret);

        [OperationContract]
        bool UpdateJournal(Journal finder, out Returns ret);
        
        [OperationContract]
        string DbMakeWhereString(SupgFinder finder, out Returns ret);

        [OperationContract]
        ZvkFinder FastFindZk(SupgFinder finder, out Returns ret);

        [OperationContract]
        ZvkFinder GetCarousel(SupgFinder finder, out Returns ret);

        [OperationContract]
        Dictionary<int, string> GetAnswers(out Returns ret);

        [OperationContract]
        List<SupgAct> GetActs(SupgActFinder finder, enSrvOper oper, out Returns ret);

        [OperationContract]
        DataSet GetZakazReport(SupgFinder finder, string table_name, out Returns ret);

        [OperationContract]
        bool CheckToClose(JobOrder finder, string ord, out Returns ret);

        [OperationContract]
        bool UpdatePlannedWorks(ref SupgAct finder, enSrvOper oper, out Returns ret);

        [OperationContract]
        Returns GetSupgStatistics(SupgFinder finder);

        [OperationContract]
        List<ServiceForwarding> GetServiceCatalog(out Returns ret);

        [OperationContract]
        bool UpdateServiceCatalog(ServiceForwarding finder, out Returns ret);
    
        [OperationContract]
        Dictionary<int, string> GetPhoneList(string pref, int nzp_kvar, out Returns ret);

        [OperationContract]
        bool GetSuppEMail(string nzp_supp, out Returns ret);
    
        [OperationContract]
        bool UpdateSpravSupg(Dest finder, enSrvOper oper, out Returns ret);

        [OperationContract]
        bool FillLSSaldo(out Returns ret);

        [OperationContract]
        bool FillLSTarif(out Returns ret);

        [OperationContract]
        List<Sprav> GetThemesCatalog(out Returns ret);

        [OperationContract]
        bool UpdateThemesCatalog(Sprav finder, out Returns ret);
    }

    public interface ISupgRemoteObject : I_Supg, IDisposable { }

    //перечислитель процедур
    public enum enSupgProc
    {
        SaveMakeNedop = 1,

        UpdateDataJO,       //обновить основные данные наряда-заказа

        UpdateResultJO,     //обновить результат наряда-заказа

        UpdateDocPeriod     //обновить документ согласования сроков


    }


    //контейнер для объектов
    [DataContract]
    public class OrderContainer : Ls
    {
        //Дата заявки
        [DataMember]
        public string zvk_date { set; get; }

        //имя заявителя
        [DataMember]
        public string demand_name { set; get; }

        //Тематика сообщения
        [DataMember]
        public int nzp_ztype { set; get; }

        //Текст сообщения
        [DataMember]
        public string comment { set; get; }

        //nzp
        [DataMember]
        public int nzp_zvk { set; get; }

        //результат(состояние)
        [DataMember]
        public int nzp_res { set; get; }

        //номер результата + сам результат
        [DataMember]
        public Dictionary<int, string> result { set; get; }

        //факт выполнения
        [DataMember]
        public string fact_date { set; get; }

        //тип заявки(классификация)
        [DataMember]
        public string zvk_type { set; get; }

        //Результат заявки
        [DataMember]
        public string res_name { set; get; }

        //срок выполнения
        [DataMember]
        public string exec_date { set; get; }

        //дата изменения
        [DataMember]
        public string last_modified { set; get; }

        //имя пользователя
        [DataMember]
        public string user_name { set; get; }

        //комментарий к сообщению
        [DataMember]
        public string result_comment { set; get; }

        //организация
        [DataMember]
        public BaseUser.OrganizationTypes organization { set; get; }
        
        [DataMember]
        public int nzp_payer { set; get; }

        //текущий диспетчер
        [DataMember]
        public int nzp_payer_disp { set; get; }

        //поставщик (подрядчик)
        [DataMember]
        public int nzp_supp { set; get; }

        public OrderContainer()
            : base()
        {
        }
    }


    //Класс содержащий информацию о службе()
    [DataContract]
    public class ServiceForwarding : Finder
    {
        public ServiceForwarding()
        {
        }

        //Данные по службе (для переадресации жалобы)

        //номер службы
        [DataMember]
        public int nzp_slug { set; get; } 

        //имя службы
        [DataMember]
        public string slug_name { set; get; }

        //номер службы
        [DataMember]
        public string phone { set; get; }

        //дата начала действия службы
        [DataMember]
        public string dat_s { set; get; }

        //Дата конца действия службы
        [DataMember]
        public string dat_po { set; get; }

        //Данные по переадресации
        

        //дата переадресации
        [DataMember]
        public string _date { set; get; }

        //номер переадресации
        [DataMember]
        public int nzp_readdr { set; get; }

        //номер заявки(жалобы)
        [DataMember]
        public int nzp_zvk { set; get; }

        //фамилия пользоватея
        [DataMember]
        public string user_name { set; get; }

        //комментраий
        [DataMember]
        public string comment { set; get; }

        //комментарий в результате?
        [DataMember]
        public string result_comment { set; get; }
    }

    //класс Неиспоавностей и недопоставок, связанных с ними
    [DataContract]
    public class Dest : Finder
    {
        [DataMember]
        //Тип неисправности nzp_dest
        public int nzp_dest { set; get; }

        [DataMember]
        //Тип неисправности dest_name
        public string dest_name { set; get; }

        [DataMember]
        //Услга(по кототорой берутся неисправности)
        public int nzp_serv { set; get; }

        [DataMember]
        //Название услуги (по кототорой берутся неисправности)
        public string service { set; get; }

        [DataMember]
        //нормативные сроки устранения неисправности: дни
        public int term_days { set; get; }

        [DataMember]
        //нормативные сроки устранения неисправности: часы
        public int term_hours { set; get; }

        [DataMember]
        //Тип недопоставки name
        public string nedop_name { set; get; }

        [DataMember]
        //признак температуры
        public int is_param { set; get; }
    }


    //класс журнала для недопоставок
    [DataContract]
    public class Journal
    {

        [DataMember]
        //ключ операции
        public int number { set; get; }

        [DataMember]
        //дата операции
        public string d_when { set; get; }

        [DataMember]
        //начало периода недопоставки
        public string d_begin { set; get; }
        
        [DataMember]
        //конец периода недопоставки
        public string d_end { set; get; }

        [DataMember]
        //начало периода регистрации документа
        public string doc_begin { set; get; }

        [DataMember]
        //конец периода регистрации документа
        public string doc_end { set; get; }
        
        [DataMember]
        //пользователь
        public string name { set; get; }

        [DataMember]
        //Создано недопоставок
        public int crt_count { set; get; }

        [DataMember]
        //Отменено недопоставок
        public int cnc_count { set; get; }

        [DataMember]
        //Статус недопоставки
        public int status { set; get; }

        [DataMember]
        //Статус недопоставки
        public string status_text { set; get; }
        
        [DataMember]
        //Источник недопоставки
        public int is_actual { set; get; }

        [DataMember]
        //Источник недопоставки
        public string is_actual_text { set; get; }
        
        [DataMember]
        //Дата распределения
        public string kp_when { set; get; }

        [DataMember]
        //Пользователь (распределение недоп-к)
        public int kp_nzp_user { set; get; }

        [DataMember]
        //Пользователь (распределение недоп-к)
        public string kp_name { set; get; }

        [DataMember]
        //Пользователь (Подготовка недоп-к)
        public int nzp_user { set; get; }

        [DataMember]
        //Пользователь (Подготовка недоп-к)
        public string user { set; get; }

        [DataMember]
        //Файл выгрузки
        public string exc_path { set; get; }
    }




    //класс наряд-заказов
    [DataContract]
    public class JobOrder :Ls
    {

        //код услуга
        [DataMember]
        public int nzp_serv { set; get; }

        //услуга
        [DataMember]
        public string service { set; get; }

        //дата с, по
        [DataMember]
        public string dat_s_po { set; get; }

        #region Данные для добавления наряд-заказа
                
        //код наряд - заказа
        [DataMember]
        public int nzp_zk { set; get; }

        //ссылка на заявку
        [DataMember]
        public int nzp_zvk { set; get; }

        //неисправность
        [DataMember]
        public int nzp_dest { set; get; }

        //поставщик
        [DataMember]
        public int nzp_supp { set; get; }

        //поставщик
        [DataMember]
        public long nzp_supp_long { set; get; }

        //код результат
        [DataMember]
        public int nzp_res { set; get; }

        //результат наряд-заказа
        [DataMember]
        public string result { set; get; }

        //номер начального наряд заказа
        [DataMember]
        public int norm { set; get; }

        //срок выполнения
        [DataMember]
        public string exec_date { set; get; }

        //дата наряд-заказа
        [DataMember]
        public string order_date { set; get; }

        //температура
        [DataMember]
        public int temperature { set; get; }

        //строковое представление температуры
        [DataMember]
        public string temperature_str { set; get; }

        //дата начала недопоставки (по заявителю)
        [DataMember]
        public string nedop_s { set; get; }

        //наименование неисправности
        [DataMember]
        public string dest_name { set; get; }

        //тип недопоставки
        [DataMember]
        public string name { set; get; }

        //исполнитель наряда заказа
        [DataMember]
        public string ispolnit { set; get; }

        //дата факта выполнения
        [DataMember]
        public string fact_date { set; get; }

        //рез нар зак
        [DataMember]
        public string res_name { set; get; }

        //комментарий
        [DataMember]
        public string comment_n { set; get; }

        //дата документа
        [DataMember]
        public string document_date { set; get; }

        //контрольная дата выполнения
        [DataMember]
        public string document_controlDate { set; get; }

        #endregion

        //факт подтверждения
        [DataMember]
        public int nzp_atts { set; get; }

        //факт подтверждения(строка)
        [DataMember]
        public string atts { set; get; }

        //оператор
        [DataMember]
        public string user_comment { set; get; }

        //последнее изменение
        [DataMember]
        public string last_modified { set; get; }

        //статус повторного
        [DataMember]
        public int is_replicate { set; get; }

        //номер родительского наряд-заказа
        [DataMember]
        public int parentno { set; get; }

        //номер следующего повторного наряд-заказа
        [DataMember]
        public int replno { set; get; }

        //номер следующего повторного наряд-заказа(строка)
        [DataMember]
        public string replnoStr { set; get; }

        //код типа недопоставки
        [DataMember]
        public int nzp_kind { set; get; }

        //признак наличия температуры у недопоставки
        [DataMember]
        public int is_param { set; get; }

        //дополнительные отметки
        [DataMember]
        public int nzp_answer { set; get; }

        #region Недопоставка                
        //Начало недопоставки(по акту)
        [DataMember]
        public string act_s { set; get; }

        //конец недопоставки (по акту)
        [DataMember]
        public string act_po { set; get; }

        //тип недопоставки
        [DataMember]
        public int act_num_nedop { set; get; }

        //температура
        [DataMember]
        public int act_temperature { set; get; }
        
        //статус наряд-заказа
        [DataMember]
        public int status { set; get; }

        //начало периода недопоставки
        [DataMember]
        public string dat_s { set; get; }

        //конец периода недопоставки
        [DataMember]
        public string dat_po { set; get; }

        //признак установки
        [DataMember]
        public string is_actual { set; get; }

        //Признак обработки
        [DataMember]
        public string fl_state { set; get; }

        //Дата обработки
        [DataMember]
        public string date_nedop { set; get; }

        //акт действителен
        [DataMember]
        public int act_actual { set; get; }

        //дата создания недопоставки
        [DataMember]
        public string dat_when { set; get; }

        //месяц расчета
        [DataMember]
        public string month_calc { set; get; }

        //галочка формирование недопоставки
        [DataMember]
        public int ds_actual { set; get; }

        //дата установки/снятия
        [DataMember]
        public string ds_date { set; get; }

        //Диспетчер
        [DataMember]
        public string ds_user { set; get; }

        //Диспетчер(имя)
        [DataMember]
        public string ds_user_name { set; get; }

        //номер планового ремонта
        [DataMember]
        public int nzp_plan_no { set; get; }

        [DataMember]
        public int nzp_payer { set; get; }

        #endregion    

    }

  

    //класс для результата поиска по заявкам
    [DataContract]
    public class ZvkFinder: Ls
    {

        public ZvkFinder()
            : base()
        {
            nzp = 0;
            nzp_zvk = 0;
            zvk_date = "";
            comment = "";
            res_name = "";
            order_date = "";
            nzp_zk = "";
            service = "";
            res_zakaz = "";
            nzp_zvk_prev = 0;
            nzp_zvk_next = 0;
            nzp_zk_prev = 0;
            nzp_zk_next = 0;
            pref_prev = "";
            pref_next = "";
            fact_date = "";
            control_date = "";
            name_supp = "";
            nzp_serv = "";
            r_comment = "";
            result_comment = "";
            slug_name = "";
            type = "";
            nzp_ztype = "";
            nzp_slug = "";
            nzp_dest = "";
            cnt = "";
            n_comment = "";
            nzp_atts = "";
            atts_name = "";
            replno = "";
            replno_date = "";
            replicated = "";
            nzp_res = "";
            result = "";
        }

        //номер
        [DataMember]
        public int nzp { set; get; }

        //номер заявки
        [DataMember]
        public int nzp_zvk { set; get; }

        //дата заявки
        [DataMember]
        public string zvk_date { set; get; }

        //комментарий
        [DataMember]
        public string comment { set; get; }

        //результат
        [DataMember]
        public string res_name { set; get; }


        //номер наряда - заказа
        [DataMember]
        public string nzp_zk { set; get; }

        //дата наряда-заказа
        [DataMember]
        public string order_date { set; get; }

        //услуга
        [DataMember]
        public string service { set; get; }

        //результат наряда-заказа
        [DataMember]
        public string res_zakaz { set; get; }

        //признак формирования недопоставки
        [DataMember]
        public string ds_actual { set; get; }

        //номер предыдущей заявки
        [DataMember]
        public int nzp_zvk_prev { set; get; }

        //номер следующей заявки
        [DataMember]
        public int nzp_zvk_next { set; get; }

        //номер предыдущего наряда - заказа
        [DataMember]
        public int nzp_zk_prev { set; get; }

        //номер следующего наряда - заказа
        [DataMember]
        public int nzp_zk_next { set; get; }

        //предыдущий префикс наряда - заказа
        [DataMember]
        public string pref_prev { set; get; }

        //следующий префикс наряда - заказа
        [DataMember]
        public string pref_next { set; get; }

        //предыдущий nzp_kvar
        [DataMember]
        public int nzp_kvar_prev { set; get; }

        //следующий nzp_kvar
        [DataMember]
        public int nzp_kvar_next { set; get; }

        //результат
        [DataMember]
        public string result { set; get; }

        #region для отчетов

        [DataMember]
        public string fact_date { set; get; }

        [DataMember]
        public string control_date { set; get; }

        [DataMember]
        public string name_supp { set; get; }

        [DataMember]
        public string nzp_serv { set; get; }

        [DataMember]
        public string r_comment { set; get; }

        [DataMember]
        public string result_comment { set; get; }

        [DataMember]
        public string slug_name { set; get; }

        [DataMember]
        public string type { set; get; }

        [DataMember]
        public string nzp_ztype { set; get; }

        [DataMember]
        public string nzp_slug { set; get; }

        [DataMember]
        public string nzp_dest { set; get; }

        [DataMember]
        public string cnt { set; get; }

        [DataMember]
        public string n_comment { set; get; }

        [DataMember]
        public string nzp_atts { set; get; }

        [DataMember]
        public string atts_name { set; get; }

        [DataMember]
        public string replno_date { set; get; }

        [DataMember]
        public string replno { set; get; }

        [DataMember]
        public string replicated { set; get; }

        [DataMember]
        public string nzp_res { set; get; }

        [DataMember]
        public new string nzp_geu { set; get; }

        #endregion

    }

    /// <summary> Плановые, ремонтные, аварийные и другие работы
    /// </summary>
    [DataContract]
    public class SupgAct : Ls
    {
        [DataMember]
        public int nzp_act { set; get; }
        [DataMember]
        public string plan_number { set; get; }
        [DataMember]
        public string plan_date { set; get; }
        [DataMember]
        public int nzp_supp_plant { set; get; }
        [DataMember]
        public int nzp_work_type { set; get; }
        [DataMember]
        public string name_work_type { set; get; }
        [DataMember]
        public string comment { set; get; }

        // Сопутствующая недопоставка
        //[DataMember]
        //public int nzp_ul {set; get;}
        //[DataMember]
        //public int nzp_kvar { set; get; }
        //[DataMember]
        //public long nzp_dom { set; get; }

        [DataMember]
        public List<Ls> adrList { set; get; } //список адресов, по которым проводится плановая работа

        [DataMember]
        public int nzp_supp { set; get; }
        [DataMember]
        public int nzp_serv_supp { set; get; }
        [DataMember]
        public string supplier { get; set; }
        [DataMember]
        public int nzp_serv { get; set; }
        [DataMember]
        public string service { get; set; }
        /// <summary> код типа недопоставки
        /// </summary>
        [DataMember]
        public int nzp_kind { get; set; }
        /// <summary> тип недопоставки
        /// </summary>
        [DataMember]
        public string kind { get; set; }
        [DataMember]
        public string dat_s { set; get; }
        [DataMember]
        public string dat_po { set; get; }
        /// <summary> температура
        /// </summary>
        [DataMember]
        public int tn { get; set; }

        // Акт о недопоставке
        [DataMember]
        public int is_actual { get; set; }
        [DataMember]
        public string is_actual_name { get; set; }
        [DataMember]
        public string _date { get; set; }               //дата акта о недопоставке
        [DataMember]
        public string number { get; set; }              // номер акта о недопоставке
        [DataMember]
        public string reply_date { get; set; }          //ответ поставщика: дата
        [DataMember]
        public string reply_comment { get; set; }       //ответ поставщика: комментарий

        [DataMember]
        public string registration_date { get; set; }   //дата регистрации документа
        [DataMember]
        public string last_modified { get; set; }       //дата последнего изменения
        [DataMember]
        public string user_no { get; set; }             //оператор
        [DataMember]
        public string unload_date { get; set; }         //дата выгрузки недопоставки

        public SupgAct()
            : base()
        { 
            nzp_act = 0;
            plan_number = "";
            plan_date = "";
            nzp_supp_plant = 0;
            nzp_work_type = 0;
            comment = "";
            nzp_supp = 0;
            supplier = "";
            nzp_serv = 0;
            service = "";
            nzp_kind = 0;
            kind = "";
            dat_s = "";
            dat_po = "";
            tn = 0;
            is_actual = 0;
            is_actual_name = "";
            _date = "";
            number = "";
            reply_date = "";
            reply_comment = "";
            registration_date = "";
            last_modified = "";
            user_no = "";
            unload_date = "";
            adrList = new List<Ls>();
        }
    }

    [DataContract]
    public class SupgActFinder : SupgAct
    {
        [DataMember]
        public string _date_to { get; set; }
        [DataMember]
        public string registration_date_to { get; set; }
        [DataMember]
        public string dat_s_to { set; get; }
        [DataMember]
        public string dat_po_to { set; get; }
        [DataMember]
        public string plan_date_to { set; get; }
        [DataMember]
        public int tn_to { get; set; }
        [DataMember]
        public string reply_date_to { get; set; }
        [DataMember]
        public int nzp_payer { set; get; }
        [DataMember]
        public int nzp_disp { set; get; }
        [DataMember]
        public string registration { set; get; }
        [DataMember]
        public BaseUser.OrganizationTypes organization { set; get; }
        [DataMember]
        public List<int> work_type_list { set; get; }

        public SupgActFinder()
            : base()
        {
            _date_to = "";
            registration_date_to = "";
            dat_s_to = "";
            dat_po_to = "";
            plan_date_to = "";
            tn_to = Constants._ZERO_;
            reply_date_to = "";
            nzp_payer = 0;
            nzp_disp = 0;
            registration = "";
            nzp_supp = 0;
            work_type_list = null;
        }
    }
} 
