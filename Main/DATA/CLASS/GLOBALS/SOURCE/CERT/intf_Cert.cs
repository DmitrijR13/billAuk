using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using STCLINE.KP50.Global;

namespace STCLINE.KP50.Interfaces
{
    //----------------------------------------------------------------------
    public class CrtClient //клиенты
    //----------------------------------------------------------------------
    {
        public int    nzp_ucer  { get; set; }
        public string uc_c   	{ get; set; } //=RU
        public string uc_st  	{ get; set; } //=Tatarstan Rep.
        public string uc_l	 	{ get; set; } //=Kazan
        public string uc_o	 	{ get; set; } //=Client
        public string uc_ou	 	{ get; set; } //=Web Client
        public string uc_em  	{ get; set; } //=info@portal.ru
        public string uc_prcn	{ get; set; } //=www.portla.ru
        public string uc_clcn	{ get; set; } //=Client's certs
        public string uc_name   { get; set; } //

        public CrtClient()
        {
            nzp_ucer = Constants._ZERO_; ;

            uc_c    = "RU";
            uc_st   = "Tatarstan Rep.";
            uc_l    = "Kazan";
            uc_o    = "firma";
            uc_ou   = "IT";
            uc_em   = "info@firma.ru";
            uc_prcn = "www.firma.ru";
            uc_clcn = "www.firma.ru Client";
            uc_name = "";
        }

        public void SetCliData(CrtClient cli)
        {
            if (cli != null)
            {
                this.nzp_ucer = cli.nzp_ucer;
                this.uc_c     = cli.uc_c;
                this.uc_st    = cli.uc_st;
                this.uc_l     = cli.uc_l;
                this.uc_o     = cli.uc_o;
                this.uc_ou    = cli.uc_ou;
                this.uc_em    = cli.uc_em;
                this.uc_prcn  = cli.uc_prcn;
                this.uc_clcn  = cli.uc_clcn;
                this.uc_name  = cli.uc_name;
            }
        }
    }
    //----------------------------------------------------------------------
    public class CrtList : CrtClient //сертификаты
    //----------------------------------------------------------------------
    {
        public int    nzp_lcer 	{ get; set; }
        public int    kod       { get; set; }

        public string dat_create{ get; set; }
        public int    crt_days  { get; set; }
        public string dat_revoke{ get; set; }
        public string num_lcer  { get; set; }
        public string lcn	    { get; set; }
        public string cer_path	{ get; set; }
        public string cer_pwd   { get; set; }

        void _CrtList(CrtClient cli) 
        {
            nzp_lcer   = Constants._ZERO_; 
            kod        = Constants._ZERO_;
            crt_days   = Constants._ZERO_;

            dat_create = "";
            dat_revoke = "";
            num_lcer   = "";
            lcn        = "";
            cer_path   = "";
            cer_pwd    = "";

            SetCliData(cli);
        }

        public CrtList(CrtClient cli)
            : base()
        {
            _CrtList(cli);
        }
        public CrtList()
        {
            _CrtList(null);
        }
    }
}
