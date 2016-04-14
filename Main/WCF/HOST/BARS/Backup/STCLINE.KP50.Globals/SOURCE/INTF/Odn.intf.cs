using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using STCLINE.KP50.Global;


namespace STCLINE.KP50.Interfaces
{
    [ServiceContract]
    public interface I_Odn
    {
        [OperationContract]
        List<Odn> GetOdn(OdnFinder finder, enSrvOper oper, out Returns ret);
    }

    //----------------------------------------------------------------------
    [DataContract]
    public class Odn : Ls
    //----------------------------------------------------------------------
    {
        [DataMember]
        public RecordMonth YM;

        [DataMember]
        public int month_
        {
            get
            {
                return YM.month_;
            }
            set
            {
                YM.month_ = value;
            }
        }
        [DataMember]
        public int year_
        {
            get
            {
                return YM.year_;
            }
            set
            {
                YM.year_ = value;
            }
        }

        [DataMember]
        public int nzp_correct { get; set; }
        [DataMember]
        public string dat_month { get; set; }
        [DataMember]
        public string dat_charge { get; set; }
        [DataMember]
        public int nzp_serv { get; set; }
        [DataMember]
        public string service { get; set; }
        [DataMember]
        public int is_gkal { get; set; }
        [DataMember]
        public decimal rval_real { get; set; }
        [DataMember]
        public decimal rval_real_p { get; set; }
        [DataMember]
        public decimal rval { get; set; }
        [DataMember]
        public decimal rval_p { get; set; }
        [DataMember]
        public decimal rvaldlt { get; set; }
        [DataMember]
        public decimal rvaldlt_p { get; set; }
        [DataMember]
        public string dnow { get; set; }
        [DataMember]
        public string dnow_p { get; set; }
        [DataMember]
        public string dpred { get; set; }
        [DataMember]
        public string dpred_p { get; set; }
        [DataMember]
        public decimal rval_now { get; set; }
        [DataMember]
        public decimal rval_now_p { get; set; }
        [DataMember]
        public decimal rval_pred { get; set; }
        [DataMember]
        public decimal rval_pred_p { get; set; }
        [DataMember]
        public int cnt_ls_val { get; set; }
        [DataMember]
        public int cnt_ls_val_p { get; set; }
        [DataMember]
        public decimal sum_ls_val { get; set; }
        [DataMember]
        public decimal sum_ls_val_p { get; set; }
        [DataMember]
        public int cnt_ls_norm { get; set; }
        [DataMember]
        public int cnt_ls_norm_p { get; set; }
        [DataMember]
        public decimal sum_ls_norm { get; set; }
        [DataMember]
        public decimal sum_ls_norm_p { get; set; }
        [DataMember]
        public int cnt_ls_25val { get; set; }
        [DataMember]
        public int cnt_ls_25val_p { get; set; }
        [DataMember]
        public decimal sum_ls_25val { get; set; }
        [DataMember]
        public decimal sum_ls_25val_p { get; set; }
        [DataMember]
        public int cnt_ls_25norm { get; set; }
        [DataMember]
        public int cnt_ls_25norm_p { get; set; }
        [DataMember]
        public decimal sum_ls_25norm { get; set; }
        [DataMember]
        public decimal sum_ls_25norm_p { get; set; }
        [DataMember]
        public int cnt_ls_221val { get; set; }
        [DataMember]
        public int cnt_ls_221val_p { get; set; }
        [DataMember]
        public decimal sum_ls_221val { get; set; }
        [DataMember]
        public decimal sum_ls_221val_p { get; set; }
        [DataMember]
        public int cnt_ls_221norm { get; set; }
        [DataMember]
        public int cnt_ls_221norm_p { get; set; }
        [DataMember]
        public decimal sum_ls_221norm { get; set; }
        [DataMember]
        public decimal sum_ls_221norm_p { get; set; }
        [DataMember]
        public int cnt_ls_210val { get; set; }
        [DataMember]
        public int cnt_ls_210val_p { get; set; }
        [DataMember]
        public decimal sum_ls_210val { get; set; }
        [DataMember]
        public decimal sum_ls_210val_p { get; set; }
        [DataMember]
        public int cnt_ls_210norm { get; set; }
        [DataMember]
        public int cnt_ls_210norm_p { get; set; }
        [DataMember]
        public decimal sum_ls_210norm { get; set; }
        [DataMember]
        public decimal sum_ls_210norm_p { get; set; }
        [DataMember]
        public decimal cnt_gils { get; set; }
        [DataMember]
        public decimal cnt_gils_p { get; set; }
        [DataMember]
        public decimal cnt_pl_pu { get; set; }
        [DataMember]
        public decimal cnt_pl_pu_p { get; set; }
        [DataMember]
        public decimal cnt_gils_pu { get; set; }
        [DataMember]
        public decimal cnt_gils_pu_p { get; set; }
        [DataMember]
        public decimal cnt_pl_norm { get; set; }
        [DataMember]
        public decimal cnt_pl_norm_p { get; set; }
        [DataMember]
        public decimal cnt_gils_norm { get; set; }
        [DataMember]
        public decimal cnt_gils_norm_p { get; set; }
        [DataMember]
        public int nzp_type_alg { get; set; }
        [DataMember]
        public string ntalg_short { get; set; }
        [DataMember]
        public int nzp_type_alg_p { get; set; }
        [DataMember]
        public string ntalg_p_short { get; set; }
        [DataMember]
        public int is_uchet { get; set; }
        [DataMember]
        public string dat_when { get; set; }
        
        /// <summary> ID записи
        /// </summary>
        [DataMember]
        public int id { get; set; }
        /// <summary> ID родительской записи (нужен для отображения перерасчетов, указывает на строку с исходными начислениями)
        /// </summary>
        [DataMember]
        public int parent_id { get; set; }
        /// <summary> Признак наличия перерасчетов, вызванных из будущего (1 - да, 0 - нет)
        /// </summary>
        [DataMember]
        public int has_future_reval { get; set; }
        /// <summary> Признак наличия перерасчетов за предыдущие месяцы (1 - да, 0 - нет)
        /// </summary>
        [DataMember]
        public int has_past_reval { get; set; }


        public Odn()
            : base()
        {
            YM.month_ = 0;
            YM.year_ = 0;

            nzp_correct = 0;
            dat_month = "";
            dat_charge = "";
            nzp_serv = 0;
            service = "";
            is_gkal = 0;
            rval_real = 0;
            rval_real_p = 0;
            rval = 0;
            rval_p = 0;
            rvaldlt = 0;
            rvaldlt_p = 0;
            dnow = "";
            dnow_p = "";
            dpred = "";
            dpred_p = "";
            rval_now = 0;
            rval_now_p = 0;
            rval_pred = 0;
            rval_pred_p = 0;
            cnt_ls_val = 0;
            cnt_ls_val_p = 0;
            sum_ls_val = 0;
            sum_ls_val_p = 0;
            cnt_ls_norm = 0;
            cnt_ls_norm_p = 0;
            sum_ls_norm = 0;
            sum_ls_norm_p = 0;
            cnt_ls_25val = 0;
            cnt_ls_25val_p = 0;
            sum_ls_25val = 0;
            sum_ls_25val_p = 0;
            cnt_ls_25norm = 0;
            cnt_ls_25norm_p = 0;
            sum_ls_25norm = 0;
            sum_ls_25norm_p = 0;
            cnt_ls_221val = 0;
            cnt_ls_221val_p = 0;
            sum_ls_221val = 0;
            sum_ls_221val_p = 0;
            cnt_ls_221norm = 0;
            cnt_ls_221norm_p = 0;
            sum_ls_221norm = 0;
            sum_ls_221norm_p = 0;
            cnt_ls_210val = 0;
            cnt_ls_210val_p = 0;
            sum_ls_210val = 0;
            sum_ls_210val_p = 0;
            cnt_ls_210norm = 0;
            cnt_ls_210norm_p = 0;
            sum_ls_210norm = 0;
            sum_ls_210norm_p = 0;
            cnt_gils = 0;
            cnt_gils_p = 0;
            cnt_pl_pu = 0;
            cnt_pl_pu_p = 0;
            cnt_gils_pu = 0;
            cnt_gils_pu_p = 0;
            cnt_pl_norm = 0;
            cnt_pl_norm_p = 0;
            cnt_gils_norm = 0;
            cnt_gils_norm_p = 0;
            nzp_type_alg = 0;
            ntalg_short = "";
            nzp_type_alg_p = 0;
            ntalg_p_short = "";
            is_uchet = 0;
            dat_when = "";

            id = 0;
            parent_id = 0;
            has_future_reval = 0;
            has_past_reval = 0;
        }
    }

    //----------------------------------------------------------------------
    [DataContract]
    public class OdnFinder : Ls
    //----------------------------------------------------------------------
    {
        [DataMember]
        public RecordMonth YM;

        [DataMember]
        public int month_
        {
            get
            {
                return YM.month_;
            }
            set
            {
                YM.month_ = value;
            }
        }
        [DataMember]
        public int year_
        {
            get
            {
                return YM.year_;
            }
            set
            {
                YM.year_ = value;
            }
        }            

        [DataMember]
        public string dat_month { get; set; }
        [DataMember]
        public string dat_month_po { get; set; } 
        [DataMember]
        public int nzp_serv { get; set; }
        [DataMember]
        public string rval_real { get; set; }
        [DataMember]
        public string rval_real_po { get; set; }
        [DataMember]
        public string rval { get; set; }
        [DataMember]
        public string rval_po { get; set; }
        [DataMember]
        public string rvaldlt { get; set; }
        [DataMember]
        public string rvaldlt_po { get; set; }       
        [DataMember]
        public string sum_ls_val { get; set; }
        [DataMember]
        public string sum_ls_val_po { get; set; }       
        [DataMember]
        public string sum_ls_norm { get; set; }
        [DataMember]
        public string sum_ls_norm_po { get; set; }        
        [DataMember]
        public int nzp_type_alg { get; set; }
        [DataMember]
        public string ntalg_short { get; set; }      


        public OdnFinder()
            : base()
        {
            YM.month_ = 0;
            YM.year_ = 0;
           
            dat_month = "";
            dat_month_po = "";          
            nzp_serv = 0;
            rval_real = "";
            rval_real_po = "";
            rval = "";
            rval_po = "";
            rvaldlt = "";
            rvaldlt_po = "";
            sum_ls_val = "";
            sum_ls_val_po = "";
            sum_ls_norm = "";
            sum_ls_norm_po = "";
            nzp_type_alg = 0;
            ntalg_short = "";
        }
    }
}

