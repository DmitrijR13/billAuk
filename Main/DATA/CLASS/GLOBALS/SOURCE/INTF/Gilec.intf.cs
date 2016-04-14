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
    public interface I_Gilec
    {
        [OperationContract]
        List<GilecFullInf> GetFullInfGilList(Gilec finder, out Returns ret);

        [OperationContract]
        List<Kart> GetKart(Kart finder, out Returns ret);

        [OperationContract]
        List<Kart> FindKart(Kart finder, out Returns ret);

        [OperationContract]
        Kart LoadKart(Kart finder, out Returns ret);

        [OperationContract]
        List<Kart> LoadPaspInfo(Kart finder, out Returns ret);

        [OperationContract]
        List<Sprav> FindSprav(Sprav finder, out Returns ret);

        [OperationContract]
        List<String> ProverkaKart(Kart new_kart, out Returns ret);

        [OperationContract]
        Kart SaveKart(Kart new_kart, Kart old_kart, out Returns ret);

        [OperationContract]
        List<GilPer> GetGilPer(GilPer finder, out Returns ret);

        [OperationContract]
        List<GilPer> FindGilPer(GilPer finder, out Returns ret);

        [OperationContract]
        GilPer LoadGilPer(GilPer finder, out Returns ret);

        [OperationContract]
        GilPer SaveGilPer(GilPer new_gilper, GilPer old_gilper, bool delete_flag, out Returns ret);

        [OperationContract]
        string[] GetPasportistInformation(Gilec US, out Returns ret);

        [OperationContract]
        List<GilecFullInf> GetFullInfGilList_AllKards(Gilec finder, out Returns ret);

        [OperationContract]
        Returns RecalcGillXX(Kart finder);

        //[OperationContract]
        //string Fill_Sprav_Smert(Object rep, int y_, int m_, string date, int vidSprav, Gilec finder, Gilec us, out Returns ret);

        //[OperationContract]
        //string Fill_web_sprav_samara2(Object rep, int y_, int m_, string date, int vidSprav, Ls finder, Gilec us, out Returns ret);

        //[OperationContract]
        //string Fill_web_sost_fam(Object rep, int y_, int m_,out Returns ret, Ls finder);

        //[OperationContract]
        //string Fill_web_vip_dom(Object rep, out Returns ret, int y_, int m_, Ls finder);

        [OperationContract]
        List<Kart> Reg_Po_MestuGilec(out Returns ret, Gilec finder);
        //[OperationContract]
        //string Reg_Po_MestuGilec_1(Object rep, out Returns ret, int y_, int m_, Gilec finder);


        [OperationContract]
        Kart KartForSprav(Kart finder, out Returns ret);

        [OperationContract]
        void MakeResponsible(Otvetstv finder, out Returns ret);

        [OperationContract]
        void CopyToOwner(Gilec finder, List<string> list, out Returns ret);

        [OperationContract]
        List<Kart> NeighborKart(Kart finder, out Returns ret);

        [OperationContract]
        FullAddress LoadFullAddress(Ls finder, out Returns ret);

        [OperationContract]
        List<Sobstw> GetSobstw(Sobstw finder, out Returns ret);

        [OperationContract]
        List<Sobstw> FindSobstw(Sobstw finder, out Returns ret);

        [OperationContract]
        Sobstw LoadSobstw(Sobstw finder, out Returns ret);

        [OperationContract]
        Sobstw SaveSobstw(Sobstw new_Sobstw, Sobstw old_Sobstw, bool delete_flag, out Returns ret);

        [OperationContract]
        List<String> ProverkaSobstw(Sobstw new_Sobstw, bool delete_flag, out Returns ret);

        [OperationContract]
        string TotalRooms(Ls finder, out Returns ret);

        [OperationContract]
        List<PaspCelPrib> FindCelPrib(PaspCelPrib finder, out Returns ret);

        [OperationContract]
        Returns OperateWithCelPrib(PaspCelPrib finder, enSrvOper oper);

        [OperationContract]
        List<PaspDoc> FindDocs(PaspDoc finder, out Returns ret);

        [OperationContract]
        Returns OperateWithDoc(PaspDoc finder, enSrvOper oper);

        [OperationContract]
        Returns OperateWithSprav(Sprav finder, enSrvOper oper);

        [OperationContract]
        List<PaspOrganRegUcheta> FindOrganRegUcheta(PaspOrganRegUcheta finder, out bool hasRequisites, out Returns ret);

        [OperationContract]
        Returns OperateWithOrganRegUcheta(PaspOrganRegUcheta finder, enSrvOper oper);

        [OperationContract]
        List<Sobstw> GetSobstvForOtchet(Sobstw finder, out Returns ret);

        [OperationContract]
        Returns GetFioVlad(Ls finder);

        [OperationContract]
        List<Kart> GetDataFromTXXTable(Kart finder, out Returns ret);

        [OperationContract]
        List<Kart> GetDataFromKart(Kart finder, out Returns ret);

        [OperationContract]
        List<DocSobstw> GetListDocSobstv(DocSobstw finder, out Returns ret);

        [OperationContract]
        Returns DeleteDocSobstv(DocSobstw finder);

        [OperationContract]
        Returns SaveDocSobstw(List<DocSobstw> finder);

        [OperationContract]
        Returns SaveDepartureType(Sprav finder);

        [OperationContract]
        Returns SavePlaceRequirement(Sprav finder);

        [OperationContract]
        List<Otvetstv> GetOtvetstv(Ls finder, out Returns ret);

        [OperationContract]
        Returns SaveOtvetstv(Otvetstv finder);

        [OperationContract]
        List<RelationsFinder> LoadRelations(RelationsFinder finder, out Returns ret);

        [OperationContract]
        List<Kart> GetKvarKart(Kart finder, out Returns ret);
    }

    //----------------------------------------------------------------------
    [DataContract]
    public class Gilec : Ls
    //----------------------------------------------------------------------
    {
        [DataMember]
        public long nzp_gil { get; set; }

        //---3.0 only---        
        [DataMember]
        public string job_post { get; set; }
        [DataMember]
        public string job_name { get; set; }
        [DataMember]
        public string dat_pvu { get; set; }
        [DataMember]
        public string who_pvu { get; set; }
        [DataMember]
        public string dat_svu { get; set; }
        [DataMember]
        public string ndees { get; set; }
        [DataMember]
        public string rem_op { get; set; }
        [DataMember]
        public string dat_prib { get; set; }
        [DataMember]
        public string dat_ubit { get; set; }
        [DataMember]
        public string soc_kod { get; set; }
        [DataMember]
        public string inn { get; set; }
        [DataMember]
        public string dat_izm { get; set; }
        [DataMember]
        public string opis { get; set; }
        [DataMember]
        public long nzp_dict_sud { get; set; }
        [DataMember]
        public string sud { get; set; }
        [DataMember]
        public int cur_unl { get; set; }
        [DataMember]
        public int sogl { get; set; }

        //---2.0 only---
        [DataMember]
        public int nzp_kart { get; set; }
        [DataMember]
        public string nzp_tkrt { get; set; }
        [DataMember]
        public string isactual { get; set; }
        [DataMember]
        public string dat_ofor { get; set; }
        [DataMember]
        public string dat_sost { get; set; }
        [DataMember]
        public List<GilecPrib> listPrib { get; set; }
        [DataMember]
        public List<GilecGragd> listGragd { get; set; }

        [DataMember]
        public bool callFromFindLs; //вызов списка ПУ из экрана поиска адресов, значит надо сначало найти список лс

        // Для Самары
        [DataMember]
        public bool is_arx { get; set; }
        // Для справки о составе семьи заголовок
        [DataMember]
        public int header { get; set; }


        public Gilec()
            : base()
        {
            nzp_gil = 0;
            //---3.0 only---
            job_post = "";
            job_name = "";

            dat_pvu = "";
            who_pvu = "";
            dat_svu = "";
            ndees = "";
            rem_op = "";
            dat_prib = "";
            dat_ubit = "";
            soc_kod = "";
            inn = "";
            dat_izm = "";
            opis = "";
            nzp_dict_sud = 0;
            sud = "";
            cur_unl = 0;
            sogl = 0;
            //--end--3.0 only---

            //---2.0 only---
            nzp_kart = 0;
            nzp_tkrt = "";
            isactual = "";


            dat_ofor = "";
            dat_sost = "";
            //--end--2.0 only---

            listPrib = new List<GilecPrib>();
            listGragd = new List<GilecGragd>();

            callFromFindLs = true;
            // Для Самары
            is_arx = false;

        }
    }

    [DataContract]
    public class GilecFullInf : Gilec
    //----------------------------------------------------------------------
    {
        [DataMember]
        public string fam { get; set; }
        [DataMember]
        public string ima { get; set; }
        [DataMember]
        public string otch { get; set; }
        [DataMember]
        public string dat_rog { get; set; }

        [DataMember]
        public string dat_smert { get; set; }

        [DataMember]
        public string serij { get; set; }
        [DataMember]
        public string nomer { get; set; }
        [DataMember]
        public string vid_dat { get; set; }
        [DataMember]
        public string vid_mes { get; set; }
        [DataMember]
        public string cel { get; set; }
        [DataMember]
        public string rod { get; set; }
        [DataMember]
        public string landop { get; set; }
        [DataMember]
        public string statop { get; set; }
        [DataMember]
        public string twnop { get; set; }
        [DataMember]
        public string rajonop { get; set; }
        [DataMember]
        public string landku { get; set; }
        [DataMember]
        public string statku { get; set; }
        [DataMember]
        public string twnku { get; set; }
        [DataMember]
        public string rajonku { get; set; }
        [DataMember]
        public string rem_ku { get; set; }
        [DataMember]
        public string dat_oprp { get; set; }
        [DataMember]
        public string tprp { get; set; }
        [DataMember]
        public string type_prop { get; set; }
        [DataMember]
        public string dat_vip { get; set; }
        [DataMember]
        public string dat_prop { get; set; }

        //новые поля
        [DataMember]
        public string town_ { get; set; }
        [DataMember]
        public string rajon_ { get; set; }
        //место рождения
        [DataMember]
        public string rem_mr { get; set; }

        [DataMember]
        public string fio { get; set; }
        [DataMember]
        public string address { get; set; }
        [DataMember]
        public string property { get; set; }
        [DataMember]
        public string s_ob { get; set; }
        [DataMember]
        public string s_gil { get; set; }
        [DataMember]
        public string sobstw { get; set; }
        [DataMember]
        public string dok { get; set; }
        [DataMember]
        public string dok_sv { get; set; }
        [DataMember]
        public string area { get; set; }




        public GilecFullInf()
            : base()
        {
            fam = "";
            ima = "";
            otch = "";
            dat_rog = "";

            dat_smert = "";

            serij = "";
            nomer = "";
            vid_dat = "";
            vid_mes = "";
            cel = "";
            rod = "";
            landop = "";
            statop = "";
            twnop = "";
            rajonop = "";
            landku = "";
            statku = "";
            twnku = "";
            rajonku = "";
            rem_ku = "";
            dat_oprp = "";
            tprp = "";
            type_prop = "";
            dat_vip = "";
            dat_prop = "";
            //
            town_ = "";
            rajon_ = "";
            rem_mr = "";

            fio = "";
            address = "";
            property = "";
            s_ob = "";
            s_gil = "";
            sobstw = "";
            dok = "";
            dok_sv = "";
            area = "";


        }
    }


    public class GilecPasp : Gilec
    {
        [DataMember]
        public long nzp_pasp { get; set; }
        [DataMember]
        public string fam { get; set; }
        [DataMember]
        public string ima { get; set; }
        [DataMember]
        public string otch { get; set; }
        [DataMember]
        public string dat_rog { get; set; }
        [DataMember]
        public string dat_rog_po { get; set; }
        [DataMember]
        public string gender { get; set; }
        [DataMember]
        public string rem_mr { get; set; }
        [DataMember]
        public string serij { get; set; }
        [DataMember]
        public string nomer { get; set; }
        [DataMember]
        public string vid_date { get; set; }
        [DataMember]
        public string vid_mes { get; set; }
        [DataMember]
        public string kod_podrazd { get; set; }
        [DataMember]
        public string kod_lich { get; set; }
        [DataMember]
        public string dat_po { get; set; }
        [DataMember]
        public long nzp_dict_doc { get; set; }
        [DataMember]
        public string doc { get; set; }

        public GilecPasp()
            : base()
        {
            nzp_pasp = 0;
            fam = "";
            ima = "";
            otch = "";
            dat_rog = "";
            dat_rog_po = "";
            gender = "";
            rem_mr = "";
            serij = "";
            nomer = "";
            vid_date = "";
            vid_mes = "";
            kod_podrazd = "";
            kod_lich = "";
            dat_po = "";
            nzp_dict_doc = 0;
            doc = "";
        }
    }

    public class GilecPrib : GilecPasp
    {
        [DataMember]
        public long nzp_prib { get; set; }
        [DataMember] //dat_s
        public string dat_reg_begin { get; set; }
        [DataMember]
        public string dat_reg_begin_po { get; set; }
        [DataMember] //dat_po
        public string dat_reg_end { get; set; }
        [DataMember]
        public string dat_reg_end_po { get; set; }
        [DataMember]
        public string dat_sostp { get; set; }
        [DataMember]
        public string dat_sostu { get; set; }
        [DataMember]
        public string rem_p { get; set; }
        [DataMember]
        public string rem_ku { get; set; }
        [DataMember]
        public long nzp_dict_rod { get; set; }
        [DataMember]
        public string rod { get; set; }
        [DataMember]
        public long nzp_dict_celp { get; set; }
        [DataMember]
        public string celp { get; set; }
        [DataMember]
        public long nzp_dict_celu { get; set; }
        [DataMember]
        public string celu { get; set; }
        [DataMember]
        public string tprp { get; set; }
        [DataMember]
        public string namereg { get; set; }
        [DataMember]
        public string dat_prop { get; set; }
        [DataMember]
        public List<GilecUbit> listUbit { get; set; }


        public GilecPrib()
            : base()
        {
            nzp_prib = 0;
            dat_reg_begin = "";
            dat_reg_begin_po = "";
            dat_reg_end = "";
            dat_reg_end_po = "";
            dat_sostp = "";
            dat_sostu = "";
            rem_p = "";
            rem_ku = "";
            nzp_dict_rod = 0;
            rod = "";
            nzp_dict_celp = 0;
            celp = "";
            nzp_dict_celu = 0;
            celu = "";
            tprp = "";
            namereg = "";
            dat_prop = "";
            listUbit = new List<GilecUbit>();
        }
    }

    public class GilecUbit : Finder
    {
        [DataMember]
        public string ubnum { get; set; }
        [DataMember]
        public long nzp_prib { get; set; }
        [DataMember]
        public long nzp_ubit { get; set; }
        [DataMember]
        public string dat_ubit_begin { get; set; }
        [DataMember]
        public string dat_ubit_end { get; set; }
        [DataMember]
        public string ubit_rem_ku { get; set; }

        public GilecUbit()
            : base()
        {
            nzp_prib = 0;
            nzp_ubit = 0;
            dat_ubit_begin = "";
            dat_ubit_end = "";
            ubit_rem_ku = "";
            ubnum = "";
        }
    }

    public class GilecGragd : Finder
    {
        [DataMember]
        public long nzp_gragd { get; set; }
        [DataMember]
        public long nzp_gil { get; set; }
        [DataMember]
        public long nzp_dict_grgd { get; set; }
        [DataMember]
        public string grgd { get; set; }

        public GilecGragd()
            : base()
        {
            nzp_gragd = 0;
            nzp_gil = 0;
            nzp_dict_grgd = 0;
            grgd = "";
        }
    }


    //----------------------------------------------------------------------
    [DataContract]
    public class Kart : Ls
    //----------------------------------------------------------------------
    {
        [DataMember]
        public string nzp_sud { get; set; }
        [DataMember]
        public string sud { get; set; }
        [DataMember]
        public bool is_checked { get; set; }
        [DataMember]
        public string nzp_gil { get; set; }
        [DataMember]
        public string nzp_kart { get; set; }

        [DataMember]
        public string fam { get; set; }
        [DataMember]
        public string ima { get; set; }
        [DataMember]
        public string otch { get; set; }
        [DataMember]
        public string dat_rog { get; set; }
        [DataMember]
        public string dat_rog_po { get; set; }



        [DataMember]
        public string fam_c { get; set; }

        [DataMember]
        public string ima_c { get; set; }
        [DataMember]
        public string otch_c { get; set; }
        [DataMember]
        public string dat_rog_c { get; set; }
        [DataMember]
        public string gender { get; set; }
        [DataMember]
        public string grgd { get; set; }


        [DataMember]
        public string nzp_dok { get; set; }
        [DataMember]
        public string dok { get; set; }

        [DataMember]
        public string serij { get; set; }
        [DataMember]
        public string nomer { get; set; }
        [DataMember]
        public string vid_mes { get; set; }
        [DataMember]
        public string vid_dat { get; set; }
        [DataMember]
        public string vid_dat_po { get; set; }

        [DataMember]
        public string tprp { get; set; }

        [DataMember]
        public string jobpost { get; set; }
        [DataMember]
        public string jobname { get; set; }
        [DataMember]
        public string dat_pvu { get; set; }
        [DataMember]
        public string who_pvu { get; set; }

        [DataMember]
        public string dat_svu { get; set; }

        [DataMember]
        public string namereg { get; set; }
        [DataMember]
        public string kod_namereg_prn { get; set; }

        [DataMember]
        public string nzp_rod { get; set; }
        [DataMember]
        public string rod { get; set; }


        [DataMember]
        public string nzp_celp { get; set; }
        [DataMember]
        public string celp { get; set; }

        [DataMember]
        public string nzp_celu { get; set; }
        [DataMember]
        public string celu { get; set; }

        [DataMember]
        public string ndees { get; set; }
        [DataMember]
        public string neuch { get; set; }

        [DataMember]
        public string nzp_tkrt { get; set; }
        [DataMember]
        public string isactual { get; set; }

        [DataMember]
        public new string nzp_land { get; set; }
        [DataMember]
        public new string nzp_stat { get; set; }
        [DataMember]
        public new string nzp_town { get; set; }
        //[DataMember] public string land { get; set; }
        //[DataMember] public string stat{ get; set; }


        [DataMember]
        public string rajon_dom { get; set; }


        [DataMember]
        public string nzp_lnmr { get; set; }
        [DataMember]
        public string nzp_stmr { get; set; }
        [DataMember]
        public string nzp_tnmr { get; set; }
        [DataMember]
        public string nzp_rnmr { get; set; }
        [DataMember]
        public string lnmr { get; set; }
        [DataMember]
        public string stmr { get; set; }
        [DataMember]
        public string tnmr { get; set; }
        [DataMember]
        public string rnmr { get; set; }
        [DataMember]
        public string rem_mr { get; set; }

        [DataMember]
        public string nzp_lnop { get; set; }
        [DataMember]
        public string nzp_stop { get; set; }
        [DataMember]
        public string nzp_tnop { get; set; }
        [DataMember]
        public string nzp_rnop { get; set; }
        [DataMember]
        public string lnop { get; set; }
        [DataMember]
        public string stop { get; set; }
        [DataMember]
        public string tnop { get; set; }
        [DataMember]
        public string rnop { get; set; }
        [DataMember]
        public string rem_op { get; set; }

        [DataMember]
        public string adr_op
        {
            get
            {
                string adr = lnop;
                if (stop != "")
                {
                    if (adr != "") adr += ", ";
                    adr += stop;
                }
                if (tnop != "")
                {
                    if (adr != "") adr += ", ";
                    adr += tnop;
                }
                if (rnop != "")
                {
                    if (adr != "") adr += ", ";
                    adr += rnop;
                }
                if (rem_op != "")
                {
                    if (adr != "") adr += ", ";
                    adr += rem_op;
                }
                return adr;
            }
            set { }
        }

        [DataMember]
        public string nzp_lnku { get; set; }
        [DataMember]
        public string nzp_stku { get; set; }
        [DataMember]
        public string nzp_tnku { get; set; }
        [DataMember]
        public string nzp_rnku { get; set; }
        [DataMember]
        public string lnku { get; set; }
        [DataMember]
        public string stku { get; set; }
        [DataMember]
        public string tnku { get; set; }
        [DataMember]
        public string rnku { get; set; }
        [DataMember]
        public string rem_ku { get; set; }

        [DataMember]
        public string rem_p { get; set; }


        [DataMember]
        public string dat_prop { get; set; }
        [DataMember]
        public string dat_prib { get; set; }
        [DataMember]
        public string dat_vip { get; set; }
        [DataMember]
        public string dat_oprp { get; set; }
        [DataMember]
        public string dat_oprp_po { get; set; }

        [DataMember]
        public string dat_ofor { get; set; }
        [DataMember]
        public string dat_ofor_po { get; set; }
        [DataMember]
        public string dat_sost { get; set; }
        [DataMember]
        public string dat_izm { get; set; }
        [DataMember]
        public string dat_izm_po { get; set; }

        [DataMember]
        public bool callFromFindLs; //вызов списка карточек из экрана поиска адресов, значит надо сначало найти список лс
        [DataMember]
        public List<KartGrgd> listKartGrgd { get; set; }
        [DataMember]
        public string nzp_serial { get; set; }

        [DataMember]
        public string kod_podrazd { get; set; }

        [DataMember]
        public string land_op { set; get; }
        [DataMember]
        public string stat_op { set; get; }
        [DataMember]
        public string town_op { set; get; }
        [DataMember]
        public string rajon_op { set; get; }

        [DataMember]
        public string no_get { set; get; }
        [DataMember]
        public string no_change { set; get; }

        //Для Cамары
        [DataMember]
        public string strana_mr { set; get; }
        [DataMember]
        public string region_mr { set; get; }
        [DataMember]
        public string okrug_mr { set; get; }
        [DataMember]
        public string gorod_mr { set; get; }
        [DataMember]
        public string npunkt_mr { set; get; }


        [DataMember]
        public string strana_op { set; get; }
        [DataMember]
        public string region_op { set; get; }
        [DataMember]
        public string okrug_op { set; get; }
        [DataMember]
        public string gorod_op { set; get; }
        [DataMember]
        public string npunkt_op { set; get; }

        [DataMember]
        public string adr_op_smr
        {
            get
            {
                string adr = strana_op;
                if (region_op != "")
                {
                    if (adr != "") adr += ", ";
                    adr += region_op;
                }
                if (okrug_op != "")
                {
                    if (adr != "") adr += ", ";
                    adr += okrug_op;
                }
                if (gorod_op != "")
                {
                    if (adr != "") adr += ", ";
                    adr += gorod_op;
                }
                if (npunkt_op != "")
                {
                    if (adr != "") adr += ", ";
                    adr += npunkt_op;
                }
                return adr;
            }
            set { }
        }

        [DataMember]
        public string strana_ku { set; get; }
        [DataMember]
        public string region_ku { set; get; }
        [DataMember]
        public string okrug_ku { set; get; }
        [DataMember]
        public string gorod_ku { set; get; }
        [DataMember]
        public string npunkt_ku { set; get; }

        [DataMember]
        public string adr_ku
        {
            get
            {
                string adr = strana_ku;
                if (region_ku != "")
                {
                    if (adr != "") adr += ", ";
                    adr += region_ku;
                }
                if (okrug_ku != "")
                {
                    if (adr != "") adr += ", ";
                    adr += okrug_ku;
                }
                if (gorod_ku != "")
                {
                    if (adr != "") adr += ", ";
                    adr += gorod_ku;
                }
                if (npunkt_ku != "")
                {
                    if (adr != "") adr += ", ";
                    adr += npunkt_ku;
                }
                return adr;
            }
            set { }
        }

        [DataMember]
        public string dat_smert { get; set; }

        [DataMember]
        public string dat_fio_c { get; set; }

        [DataMember]
        public bool is_arx { set; get; }

        [DataMember]
        public List<string> dopFind2;

        [DataMember]
        public string fio_kvs { get; set; }

        [DataMember]
        public string obsh_plosh { get; set; }

        [DataMember]
        public string gil_plosh { get; set; }

        [DataMember]
        public int projiv { get; set; }

        [DataMember]
        public int propis { get; set; }

        [DataMember]
        public string tip_sobstv { get; set; }

        [DataMember]
        public string komfortnost { get; set; }
        [DataMember]
        public int no_podtv_docs { get; set; }

        public Kart()
            : base()
        {
            nzp_sud = "";
            sud = "";
            is_checked = false;
            no_podtv_docs = -1;
            nzp_gil = "";
            nzp_kart = "";

            fam = "";
            ima = "";
            otch = "";
            dat_rog = "";
            dat_rog_po = "";

            fam_c = "";
            ima_c = "";
            otch_c = "";
            dat_rog_c = "";

            gender = "";

            grgd = "";

            nzp_dok = "";
            dok = "";
            serij = "";
            nomer = "";
            vid_mes = "";
            vid_dat = "";
            vid_dat_po = "";

            tprp = "";

            jobpost = "";
            jobname = "";

            dat_pvu = "";
            who_pvu = "";
            dat_svu = "";
            namereg = "";
            kod_namereg_prn = "";

            nzp_rod = "";
            rod = "";
            nzp_celp = "";
            celp = "";
            nzp_celu = "";
            celu = "";

            ndees = "";
            neuch = "";

            nzp_tkrt = "";
            isactual = "";

            nzp_land = "";
            nzp_stat = "";
            nzp_town = "";
            land = "";
            stat = "";

            rajon_dom = "";

            nzp_lnmr = "";
            nzp_stmr = "";
            nzp_tnmr = "";
            nzp_rnmr = "";
            lnmr = "";
            stmr = "";
            tnmr = "";
            rnmr = "";
            rem_mr = "";
            rem_p = "";

            nzp_lnop = "";
            nzp_stop = "";
            nzp_tnop = "";
            nzp_rnop = "";
            lnop = "";
            stop = "";
            tnop = "";
            rnop = "";
            rem_op = "";

            nzp_lnku = "";
            nzp_stku = "";
            nzp_tnku = "";
            nzp_rnku = "";
            lnku = "";
            stku = "";
            tnku = "";
            rnku = "";
            rem_ku = "";


            dat_prop = "";
            dat_prib = "";
            dat_vip = "";
            dat_oprp = "";
            dat_oprp_po = "";

            dat_ofor = "";
            dat_ofor_po = "";
            dat_sost = "";
            dat_izm = "";
            dat_izm_po = "";

            nzp_serial = "";

            listKartGrgd = new List<KartGrgd>();

            callFromFindLs = true;

            kod_podrazd = "";
            land_op = "";
            stat_op = "";
            town_op = "";
            rajon_op = "";

            no_get = "";
            no_change = "";

            //Для Cамары
            strana_mr = "";
            region_mr = "";
            okrug_mr = "";
            gorod_mr = "";
            npunkt_mr = "";

            strana_op = "";
            region_op = "";
            okrug_op = "";
            gorod_op = "";
            npunkt_op = "";

            strana_ku = "";
            region_ku = "";
            okrug_ku = "";
            gorod_ku = "";
            npunkt_ku = "";

            dat_smert = "";
            dat_fio_c = "";


            is_arx = false;

            dopFind = new List<string>();

            fio_kvs = "";
            obsh_plosh = "";
            gil_plosh = "";
            projiv = 0;
            propis = 0;
            tip_sobstv = "";
            komfortnost = "";
        }
    }

    public class KartGrgd : Finder
    {
        [DataMember]
        public string nzp_kart { get; set; }
        [DataMember]
        public string nzp_grgd { get; set; }
        [DataMember]
        public string grgd { get; set; }

        public KartGrgd()
            : base()
        {
            nzp_kart = "";
            nzp_grgd = "";
            grgd = "";
        }
    }


    //----------------------------------------------------------------------
    [DataContract]
    public class Sprav : Finder
    //----------------------------------------------------------------------
    {
        [DataMember]
        public string name_sprav { get; set; }
        [DataMember]
        public string nzp_sprav { get; set; }
        [DataMember]
        public int parent_nzp_sprav { get; set; }
        [DataMember]
        public string val_sprav { get; set; }
        [DataMember]
        public string dop_kod { get; set; }

        public Sprav()
            : base()
        {
            name_sprav = "";
            nzp_sprav = "";
            parent_nzp_sprav = 0;
            val_sprav = "";
            dop_kod = "";
        }
    }
    //----------------------------------------------------------------------
    [DataContract]
    public class GilPer : Ls
    //----------------------------------------------------------------------
    {
        [DataMember]
        public string nzp_glp { get; set; }
        [DataMember]
        public string nzp_gilec { get; set; }
        [DataMember]
        public string dat_s { get; set; }
        [DataMember]
        public string dat_po { get; set; }
        [DataMember]
        public string is_actual { get; set; }
        [DataMember]
        public string sost { get; set; }
        [DataMember]
        public string osnov { get; set; }
        [DataMember]
        public string dat_when { get; set; }
        [DataMember]
        public string dat_del { get; set; }

        [DataMember]
        public string nzp_kart { get; set; }
        [DataMember]
        public string fam { get; set; }
        [DataMember]
        public string ima { get; set; }
        [DataMember]
        public string otch { get; set; }
        [DataMember]
        public string dat_rog { get; set; }
        [DataMember]
        public int no_podtv_docs { get; set; }
        [DataMember]
        public string podtv_doc_exist { get; set; }

        [DataMember]
        public int id_departure_types { get; set; }
        [DataMember]
        public string departure_types { get; set; }
        public GilPer()
            : base()
        {
            id_departure_types = 0;
            departure_types = "";
            nzp_glp = "";
            nzp_gilec = "";
            dat_s = "";
            dat_po = "";
            is_actual = "";
            sost = "";
            osnov = "";
            dat_when = "";
            podtv_doc_exist = "";
            dat_del = "";
            no_podtv_docs = 0;


            nzp_kart = "";
            fam = "";
            ima = "";
            otch = "";
            dat_rog = "";
        }
    }

    //----------------------------------------------------------------------
    [DataContract]
    public class Sobstw : Ls
    //----------------------------------------------------------------------
    {
        [DataMember]
        public string nzp_sobstw { get; set; }
        [DataMember]
        public string nzp_gil { get; set; }

        [DataMember]
        public string nzp_rod { get; set; }
        [DataMember]
        public string rod { get; set; }
        [DataMember]
        public string fam { get; set; }
        [DataMember]
        public string ima { get; set; }
        [DataMember]
        public string otch { get; set; }
        [DataMember]
        public string dat_rog { get; set; }

        [DataMember]
        public string adress { get; set; }
        [DataMember]
        public string dop_info { get; set; }

        [DataMember]
        public string nzp_dok { get; set; }
        [DataMember]
        public string dok { get; set; }
        [DataMember]
        public string serij { get; set; }
        [DataMember]
        public string nomer { get; set; }
        [DataMember]
        public string vid_mes { get; set; }
        [DataMember]
        public string vid_dat { get; set; }

        [DataMember]
        public string dolya { get; set; }
        [DataMember]
        public string dolya_up { get; set; }
        [DataMember]
        public string dolya_down { get; set; }

        [DataMember]
        public string nzp_dok_sv { get; set; }
        [DataMember]
        public string dok_sv { get; set; }
        [DataMember]
        public string serij_sv { get; set; }
        [DataMember]
        public string nomer_sv { get; set; }
        [DataMember]
        public string vid_mes_sv { get; set; }
        [DataMember]
        public string vid_dat_sv { get; set; }
        [DataMember]
        public string is_actual { get; set; }
        [DataMember]
        public string sost { get; set; }
        [DataMember]
        public string tel { get; set; }

        [DataMember]
        public int num_doc { get; set; }

        public Sobstw()
            : base()
        {
            num_doc = 0;
            nzp_sobstw = "";
            nzp_gil = "";
            nzp_rod = "";
            rod = "";
            fam = "";
            ima = "";
            otch = "";
            dat_rog = "";

            adress = "";
            dop_info = "";
            nzp_dok = "";
            dok = "";
            serij = "";
            nomer = "";
            vid_mes = "";
            vid_dat = "";
            dolya = "";
            dolya_up = "";
            dolya_down = "";
            tel = "";

            nzp_dok_sv = "";
            dok_sv = "";
            serij_sv = "";
            nomer_sv = "";
            vid_mes_sv = "";
            vid_dat_sv = "";

            is_actual = "";
            sost = "";
        }
    }

    [DataContract]
    public class DocSobstw : Finder
    {
        [DataMember]
        public int nzp_doc { get; set; }
        [DataMember]
        public int nzp_sobstw { get; set; }
        [DataMember]
        public int dolya_up { get; set; }
        [DataMember]
        public int dolya_down { get; set; }

        [DataMember]
        public int nzp_dok_sv { get; set; }
        [DataMember]
        public string dok_sv { get; set; }
        [DataMember]
        public string serij_sv { get; set; }
        [DataMember]
        public string nomer_sv { get; set; }
        [DataMember]
        public string vid_mes_sv { get; set; }
        [DataMember]
        public string vid_dat_sv { get; set; }
       
        [DataMember]
        public string sost { get; set; }

        public DocSobstw()
            : base()
        {
            nzp_doc = 0;
            nzp_sobstw = 0;
            dolya_up = 0;
            dolya_down = 0;
            nzp_dok_sv = 0;
            dok_sv = "";
            serij_sv = "";
            nomer_sv = "";
            vid_mes_sv = "";
            vid_dat_sv = "";          
            sost = "";
            dok_sv = "";
        }
    }

    //----------------------------------------------------------------------
    [DataContract]
    public class FullAddress : Finder
    //----------------------------------------------------------------------
    {
        [DataMember]
        public string nzp_kvar { get; set; }
        [DataMember]
        public string nkvar { get; set; }
        [DataMember]
        public string nkvar_n { get; set; }
        [DataMember]
        public string nzp_dom { get; set; }
        [DataMember]
        public string ndom { get; set; }
        [DataMember]
        public string nkor { get; set; }
        [DataMember]
        public string nzp_ul { get; set; }
        [DataMember]
        public string ulica { get; set; }
        [DataMember]
        public string ulicareg { get; set; }
        [DataMember]
        public string nzp_raj { get; set; }
        [DataMember]
        public string rajon { get; set; }
        [DataMember]
        public string nzp_town { get; set; }
        [DataMember]
        public string town { get; set; }
        [DataMember]
        public string nzp_stat { get; set; }
        [DataMember]
        public string stat { get; set; }
        [DataMember]
        public string nzp_land { get; set; }
        [DataMember]
        public string land { get; set; }
        [DataMember]
        public string nzp_raj_dom { get; set; }
        [DataMember]
        public string rajon_dom { get; set; }

        public FullAddress()
            : base()
        {
            nzp_kvar = "";
            nkvar = "";
            nkvar_n = "";
            nzp_dom = "";
            ndom = "";
            nkor = "";
            nzp_ul = "";
            ulica = "";
            ulicareg = "";
            nzp_raj = "";
            rajon = "";
            nzp_town = "";
            town = "";
            nzp_stat = "";
            stat = "";
            nzp_land = "";
            land = "";
            nzp_raj_dom = "";
            rajon_dom = "";
        }
    }

    [DataContract]
    public class PaspCelPrib : Finder
    {
        [DataMember]
        public int nzp_cel { get; set; }
        [DataMember]
        public string cel { get; set; }
        [DataMember]
        public int nzp_tkrt { get; set; }
        [DataMember]
        public string tkrt
        {
            get
            {
                switch (nzp_tkrt)
                {
                    case 1: return "ПРИБЫТИЕ";
                    case 2: return "УБЫТИЕ";
                    default: return "";
                }
            }
            set { }
        }

        public PaspCelPrib()
            : base()
        {
            nzp_cel = 0;
            cel = "";
            nzp_tkrt = 0;
        }
    }

    [DataContract]
    public class PaspDoc : Finder
    {
        [DataMember]
        public int nzp_dok { get; set; }
        [DataMember]
        public string dok { get; set; }
        [DataMember]
        public string serij_mask { get; set; }
        [DataMember]
        public string nomer_mask { get; set; }
        [DataMember]
        public string nzp_oso { get; set; }

        public PaspDoc()
            : base()
        {
            nzp_dok = 0;
            dok = "";
            serij_mask = "";
            nomer_mask = "";
            nzp_oso = "";
        }
    }

    [DataContract]
    public class PaspRodst : Finder
    {
        [DataMember]
        public int nzp_rod { get; set; }
        [DataMember]
        public string rod { get; set; }

        public PaspRodst()
            : base()
        {
            nzp_rod = 0;
            rod = "";
        }
    }

    [DataContract]
    public class PaspOrganRegUcheta : Finder
    {
        [DataMember]
        public int kod_namereg { get; set; }
        [DataMember]
        public string namereg { get; set; }
        [DataMember]
        public string ogrn { get; set; }
        [DataMember]
        public string inn { get; set; }
        [DataMember]
        public string kpp { get; set; }
        [DataMember]
        public string adr_namereg { get; set; }
        [DataMember]
        public string tel_namereg { get; set; }
        [DataMember]
        public string dolgnost { get; set; }
        [DataMember]
        public string fio_namereg { get; set; }
        [DataMember]
        public string kod_namereg_prn { get; set; }

        public PaspOrganRegUcheta()
            : base()
        {
            kod_namereg = 0;
            namereg = "";
            ogrn = "";
            inn = "";
            kpp = "";
            adr_namereg = "";
            tel_namereg = "";
            dolgnost = "";
            fio_namereg = "";
            kod_namereg_prn = "";
        }
    }

    [DataContract]
    public class Otvetstv : Finder
    {
        [DataMember]
        public int nzp_otv { get; set; }
        [DataMember]
        public int nzp_kvar { get; set; }
        [DataMember]
        public int nzp_rod { get; set; }
        [DataMember]
        public string fam { get; set; }
        [DataMember]
        public string ima { get; set; }
        [DataMember]
        public string otch { get; set; }
        [DataMember]
        public string dat_rog { get; set; }
        [DataMember]
        public string adress { get; set; }
        [DataMember]
        public string dop_info { get; set; }
        [DataMember]
        public int nzp_dok { get; set; }
        [DataMember]
        public string serij { get; set; }
        [DataMember]
        public string nomer { get; set; }
        [DataMember]
        public string kod_podr { get; set; }
        [DataMember]
        public string vid_mes { get; set; }
        [DataMember]
        public string vid_dat { get; set; }
        [DataMember]
        public string vipis_dat { get; set; }
        [DataMember]
        public int nzp_gil { get; set; }
        [DataMember]
        public string dat_s { get; set; }
        [DataMember]
        public string dat_po { get; set; }
        [DataMember]
        public int is_actual { get; set; }
        [DataMember]
        public int nzp_user { get; set; }
        [DataMember]
        public string dat_when { get; set; }
        [DataMember]
        public string rod { get; set; }
        [DataMember]
        public int num { get; set; }

        public Otvetstv()
            : base()
        {
            nzp_otv = 0;
            nzp_kvar = 0;
            nzp_rod = 0;
            fam = "";
            ima = "";
            otch = "";
            dat_rog = "";
            adress = "";
            dop_info = "";
            nzp_dok = 0;
            serij = "";
            nomer = "";
            vid_mes = "";
            vid_dat = "";
            vipis_dat = "";
            nzp_gil = 0;
            dat_s = "";
            dat_po = "";
            is_actual = 0;
            nzp_user = 0;
            dat_when = "";
            rod = "";
            num = 0;
        }

       
    }
    [DataContract]
    public class RelationsFinder : Finder
    {
        [DataMember]
        public int nzp_rod { get; set; }

        [DataMember]
        public string rodstvo { get; set; }

        public RelationsFinder()
            : base()
        {
            nzp_rod = 0;
            rodstvo = "";
        }
    }

    [DataContract]
    public class GilecSplitFinder : Finder
    {
        [DataMember]
        public int nzp_kart { get; set; }

        [DataMember]
        public int nzp_kvar { get; set; }

        [DataMember]
        public string pref { get; set; }

        public GilecSplitFinder()
            : base()
        {
            nzp_kart = 0;
            pref = "";
            nzp_kvar = 0;
        }
    }
}

