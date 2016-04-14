using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.Global
{

    //класс для поиска
    [DataContract]
    public class SupgFinder : Ls
    {
        public SupgFinder()
            : base()
        {
            nzp_zvk = 0;
            zvk_date_from = "";
            zvk_date_to = "";
            zvk_ztype = 0;
            zvk_exec_date_from = "";
            zvk_exec_date_to = "";
            zvk_fact_date_from = "";
            zvk_fact_date_to = "";
            zvk_res = 0;
            zvk_result_comment = "";
            zvk_demand_name = "";
            _date_from = "";
            _date_to = "";
            nzp_slug = 0;
            nzp_zk = 0;
            zk_order_date_from = "";
            zk_order_date_to = "";

            plan_date_from = "";
            plan_date_to = "";

            control_date_from = "";
            control_date_to = "";

            zk_fact_date_from = "";
            zk_fact_date_to = "";
            zk_plan_date_from = "";
            zk_plan_date_to = "";
            zk_control_date_from = "";
            zk_control_date_to = "";
            zk_accept_date_from = "";
            zk_accept_date_to = "";
            zk_mail_date_from = "";
            zk_mail_date_to = "";
            nzp_serv = 0;
            nzp_dest = 0;
            nzp_res = 0;
            nzp_atts = 0;
            repeated = -1;
            is_replicate = -1;
            nzp_status = 0;
            act_flag = 0;
            number = -1;
            flag_get_zakaz = 0;
            flag_disp = 0;
            flag_survey = 0;
            nzp_payer = 0;
            nzp_disp = 0;
            nzp_supp = 0;
            registration = "";
        }

        #region первая вкладка
        [DataMember]
        //номер заявки
        public int nzp_zvk { set; get; }
        //дата регистрации с
        [DataMember]
        public string zvk_date_from { set; get; }
        //дата регистрации по
        [DataMember]
        public string zvk_date_to { set; get; }
        //классификация сообщения
        [DataMember]
        public int zvk_ztype { set; get; }
        //срок выполнения с
        [DataMember]
        public string zvk_exec_date_from { set; get; }
        //срок выполнения по
        [DataMember]
        public string zvk_exec_date_to { set; get; }
        //факт выполнения с
        [DataMember]
        public string zvk_fact_date_from { set; get; }
        //факт выполнения по
        [DataMember]
        public string zvk_fact_date_to { set; get; }
        //результат выполнения
        [DataMember]
        public int zvk_res { set; get; }
        //комментарий результата
        [DataMember]
        public string zvk_result_comment { set; get; }
        //ф.и.о. заявителя
        [DataMember]
        public string zvk_demand_name { set; get; }

        #endregion

        #region вторая вкладка
        //дата с
        [DataMember]
        public string _date_from { set; get; }
        //дата по
        [DataMember]
        public string _date_to { set; get; }
        //служба
        [DataMember]
        public int nzp_slug { set; get; }

        #endregion

        #region третья вкладка

        //номер заявления
        [DataMember]
        public int nzp_zk { set; get; }
        //дата наряда заказа с
        [DataMember]
        public string zk_order_date_from { set; get; }
        //дата наряда заказа по
        [DataMember]
        public string zk_order_date_to { set; get; }
        //факт выполнения заказа с
        [DataMember]
        public string zk_fact_date_from { set; get; }
        //факт выполнения заказа по
        [DataMember]
        public string zk_fact_date_to { set; get; }
        //дата. план ремонт с
        [DataMember]
        public string zk_plan_date_from { set; get; }
        //дата. план ремонт по
        [DataMember]
        public string zk_plan_date_to { set; get; }
        //контр. срок выполнения с
        [DataMember]
        public string zk_control_date_from { set; get; }
        //контр. срок выполнения по
        [DataMember]
        public string zk_control_date_to { set; get; }

        //дата документа с 
        [DataMember]
        public string plan_date_from { set; get; }
        //дата документа по
        [DataMember]
        public string plan_date_to { set; get; }

        //контрольный срок с
        [DataMember]
        public string control_date_from { set; get; }
        //контрольный срок по
        [DataMember]
        public string control_date_to { set; get; }

        //дата  отправки  исполнителю с
        [DataMember]
        public string zk_mail_date_from { set; get; }
        //дата  отправки  исполнителю по
        [DataMember]
        public string zk_mail_date_to { set; get; }


        //дата получения исполнителем с
        [DataMember]
        public string zk_accept_date_from { set; get; }
        //дата получения исполнителем по
        [DataMember]
        public string zk_accept_date_to { set; get; }

        //исполнитель
        [DataMember]
        public int zk_nzp_supp { set; get; }
        //услуга
        [DataMember]
        public int nzp_serv { set; get; }
        //претензия
        [DataMember]
        public int nzp_dest { set; get; }
        //результат
        [DataMember]
        public int nzp_res { set; get; }
        //подтверждение выполнения
        [DataMember]
        public int nzp_atts { set; get; }
        //выдано повторное
        [DataMember]
        public int repeated { set; get; }
        //повторное
        [DataMember]
        public int is_replicate { set; get; }
        //статус наряда-заказа
        [DataMember]
        public int nzp_status { set; get; }

        //флаг формирования признака недопоставки
        [DataMember]
        public int act_flag { set; get; }

        //номер для журнала
        [DataMember]
        public int number { set; get; }

        //признак для принудительной выборки нарядов-заказов по л/c
        [DataMember]
        public int flag_get_zakaz { set; get; }

        //список для диспетчера
        [DataMember]
        public int flag_disp { set; get; }

        //флаг для формирования списка для опроса
        [DataMember]
        public int flag_survey { set; get; }

        #endregion

        #region доп.

        //поле группировки для статистики
        [DataMember]
        public string group_fld { set; get; }

        //начало периода для статистики
        [DataMember]
        public string ps { set; get; }

        //
        [DataMember]
        public int nzp_payer { set; get; }

        //
        [DataMember]
        public int nzp_disp { set; get; }

        //
        [DataMember]
        public string registration { set; get; }

        //
        [DataMember]
        public int nzp_supp { set; get; }

        [DataMember]
        public BaseUser.OrganizationTypes organization { set; get; }

        [DataMember]
        public string year { set; get; }

        [DataMember]
        public string month { set; get; }
        [DataMember]
        public List<string> prefList { set; get; }

        #endregion

    }
}
