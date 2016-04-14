using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Data;
using STCLINE.KP50.Global;


namespace STCLINE.KP50.Interfaces
{
    [ServiceContract]
    public interface I_Charge
    {
        [OperationContract]
        List<Saldo> GetSaldo(Saldo finder, out Returns ret, out _RecordSaldo itog);

        //[OperationContract(IsOneWay=true)]
        [OperationContract]
        void SaldoFon();

        [OperationContract]
        Returns CalcChargeListDom(Dom finder, bool reval);

        [OperationContract]
        Returns CalcChargeDom(Dom finder, bool reval);

        [OperationContract]
        Returns CalcChargeLs(Ls finder, bool reval, bool again, string alias, int id_bill);

        /// <summary>
        /// Возвращает информацию о начислениях
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="oper">Принимает значения: SrvGet, SrvFind, SrvGetBillCharge, SrvGetNewBillCharge, SrvFindChargeStatistics </param>
        /// <param name="ret"></param>
        /// <returns></returns>
        [OperationContract]
        List<Charge> GetCharge(ChargeFind finder, enSrvOper oper, out Returns ret);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="oper">Принимает значения: SrvFindCalcSz, SrvGetCalcSz</param>
        /// <param name="ret"></param>
        /// <returns></returns>
        [OperationContract]
        _RecordSzFin GetCalcSz(ChargeFind finder, enSrvOper oper, out Returns ret);

        [OperationContract]
        List<SaldoRep> FillRep(ChargeFind finder, out Returns ret, int num_rep);

        [OperationContract]
        Returns PrepareReport(ChargeFind finder, int num_rep);

        [OperationContract]
        List<SaldoRep> FillRepServ(ChargeFind finder, out Returns ret, int num_rep);

        [OperationContract]
        List<MoneyDistrib> GetMoneyDistrib(MoneyDistrib finder, enSrvOper oper, out Returns ret);

        [OperationContract]
        List<MoneyDistrib> GetMoneyDistribDom(MoneyDistrib finder, enSrvOper oper, out Returns ret);

        [OperationContract]
        List<MoneySended> GetMoneySended(MoneySended finder, enSrvOper oper, out Returns ret);

        [OperationContract]
        List<MoneyNaud> GetMoneyNaud(MoneyNaud finder, enSrvOper oper, out Returns ret);

        [OperationContract]
        Returns BackComments();

        [OperationContract]
        Returns PerenosReestrPerekidok();

        //достает инфу для справки Лицевой счет;         
        [OperationContract]
        List<Charge> GetLicChetData(ref Kart finder, out Returns ret, int y, int m);
        //Отчеты

        //[OperationContract]
        //string GetLicChetData1(Object rep, ref Kart finder, out Returns ret, int y, int m, DateTime date);

        //[OperationContract]
        //string Fill_web_fin_ls(Object rep, out Returns ret, int y_, int m_, ChargeFind finder);

        //[OperationContract]
        //string Fill_web_s_nodolg(Object rep, out Returns ret, int y_, int m_, ChargeFind finder);

        //[OperationContract]
        //string Fill_web_sparv_nach(Object rep, out Returns ret, int y_, int m_, ChargeFind finder);

        //[OperationContract]
        //string Fill_web_saldo_rep5_10(Object rep, out Returns ret, int y_, int m_, ChargeFind finder);

        //[OperationContract]
        //string Fill_web_saldo_rep5_20(Object rep, out Returns ret, int y_, int m_, ChargeFind finder);

        /// <summary> Проверяет заполнена ли таблица данных
        /// </summary>
        [OperationContract]
        bool IsTableFilledIn(ChargeFind finder, TableName table, out Returns ret);

        [OperationContract]
        List<Perekidka> LoadPerekidki(Perekidka finder, out Returns ret);

        [OperationContract]
        Returns SavePerekidkiOplatami(DelSupplier finder);

        [OperationContract]
        Returns DeletePerekidkaOplatami(DelSupplier finder);

        [OperationContract]
        List<DelSupplier> LoadPerekidkiOplatami(DelSupplier finder, out Returns ret);

        [OperationContract]
        Returns SavePerekidka(Perekidka finder);

        [OperationContract]
        List<Perekidka> LoadSumsPerekidkaLs(Perekidka finder, out Returns ret);

        [OperationContract]
        Returns SaveSumsPerekidkaLs(List<Perekidka> listfinder);

        ////для FBD
        //[OperationContract]
        //List<SaldoRep> FillRep_5_10(ChargeFind finder, out Returns ret, int num_rep);


        [OperationContract]
        List<_RecordDomODN> FillRepProtokolOdn(ChargeFind finder, out Returns ret);

        /*[OperationContract]
        decimal GetSumKOplate(Saldo finder, out Returns ret);*/

        [OperationContract]
        ReturnsObjectType<DataTable> LoadSended(MoneySended finder);

        [OperationContract]
        Returns SaveMoneySended(List<MoneySended> list);

        [OperationContract]
        List<FnPercent> GetFnPercent(FnPercent finder, enSrvOper oper, out Returns ret);

        [OperationContract]
        Returns SaveFnPercent(FnPercent finder);

        [OperationContract]
        Returns SaveFnPercentDom(FnPercent finder);

        [OperationContract]
        Returns DelFnPercent(FnPercent finder);

        [OperationContract]
        Returns DelFnPercentDom(FnPercent finder);

        [OperationContract]
        ReturnsType UpdateKredit(string pref, int inCalcYear, int inCalcMonth);

        [OperationContract]
        List<Credit> GetCredit(Credit finder, out Returns ret);

        [OperationContract]
        void SaveCredit(Credit finder, out Returns ret);

        [OperationContract]
        List<CreditDetails> GetCreditDetails(CreditDetails finder, out Returns ret);

        [OperationContract]
        Returns IsAllowCorrectSaldo(Ls finder);

        [OperationContract]
        Returns SaveCorrectSaldo(List<Saldo> finder);

        [OperationContract]
        Returns MakeProtCalc(Calcs finder);

        [OperationContract]
        bool IsNowCalcCharge(long nzp_dom, string pref, out Returns ret);

        [OperationContract]
        ReturnsType MakeOperation(ChargeOperations operation, Finder finder);

        [OperationContract]
        List<TypeRcl> LoadTypeRcl(TypeRcl finder, out Returns ret);

        [OperationContract]
        List<CheckChMon> LoadCheckChMon(CheckChMon finder, out Returns ret);

        [OperationContract]
        List<GroupsPerekidki> PrepareSplsPerekidki(ParamsForGroupPerekidki finder, out Returns ret);

        [OperationContract]
        Returns SaveGroupPerekidki(ParamsForGroupPerekidki finder);

        [OperationContract]
        Returns DeleteFromReestrPerekidok(ParamsForGroupPerekidki finder);

        [OperationContract]
        List<ParamsForGroupPerekidki> LoadReestrPerekidok(ParamsForGroupPerekidki finder, out Returns ret);

        [OperationContract]
        List<PerekidkaLsToLs> LoadSumsForPerekidkaLsToLs(PerekidkaLsToLs finder, out Returns ret);

        [OperationContract]
        Returns SavePerekidkiLsToLs(List<PerekidkaLsToLs> listfinder, ParamsForGroupPerekidki reestr);

        [OperationContract]
        Returns FindLsForReestrPerekidok(ParamsForGroupPerekidki finder);

        [OperationContract]
        Returns GetNachisl(int mode, int month_, int year_, int user_);

        [OperationContract]
        List<OverPayment> GetOverPayments(OverPayment finder, enSrvOper srv, out Returns ret);

        [OperationContract]
        Returns SaveAddressToForOverPay(OverPayment finder);

        [OperationContract]
        Returns DeletePerekidka(Perekidka finder);

        [OperationContract]
        List<TypeDoc> LoadTypeDoc(TypeDoc finder, out Returns ret);
        [OperationContract]
        List<PeniNoCalc> GetPeniNoCalcList(PeniNoCalc finder, out Returns ret);
        
        [OperationContract]
        List<Prov> GetListProvs(ProvFinder finder, out Returns ret);

        [OperationContract]
        Dictionary<int, string> GetTypesProvs(out Returns ret);

        /// <summary>
        /// Пользователи, которые делали изменения в правилах удержания
        /// </summary>
        /// <param name="finder"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        [OperationContract]
        List<PrmTypes> LoadUsersPercentDom(Finder finder, out Returns ret);

        [OperationContract]
        List<PrmTypes> LoadOperTypesPercentDom(Finder finder, out Returns ret);
    }

    public enum ChargeOperations
    {
        /// <summary>
        /// Выполнить проверки перед закрытием месяца
        /// </summary>
        CheckingBeforeCloseMonth = 1,

        /// <summary>
        /// Закрыть месяц
        /// </summary>
        CloseCalcMonth = 2,

        /// <summary>
        /// Открыть месяц
        /// </summary>
        OpenCalcMonth = 3
    }

    /// <summary> Варианты таблиц
    /// </summary>
    public enum TableName
    {
        UkRguCharge = 0x01,
        Distrib = 0x02
    }

    /// <summary>
    /// способ учета перекидки
    /// </summary>
    public enum TypeUchetPerekidka
    {
        /// <summary>
        /// Входящее сальдо
        /// </summary>
        Insaldo = 1,

        /// <summary>
        /// Исходящее сальдо
        /// </summary>
        OutSaldo = 2,

        /// <summary>
        /// Начисление
        /// </summary>
        Nachisl = 3,

        /// <summary>
        /// оплаты
        /// </summary>
        Oplats = 4,

        /// <summary>
        /// Расход
        /// </summary>
        Rashod = 5,

        /// <summary>
        /// изменение сальдо
        /// </summary>
        IzmSaldo = 6
    }

    //----------------------------------------------------------------------
    [DataContract]
    [Serializable]
    public struct _RecordSaldo    //запись сальдо
    //----------------------------------------------------------------------
    {
        [DataMember]
        public decimal sum_real;
        [DataMember]
        public decimal rsum_tarif;
        [DataMember]
        public decimal sum_charge;
        [DataMember]
        public decimal reval;
        [DataMember]
        public decimal real_charge;
        [DataMember]
        public decimal sum_money;
        [DataMember]
        public decimal money_to;
        [DataMember]
        public decimal money_from;
        [DataMember]
        public decimal money_del;
        [DataMember]
        public decimal sum_insaldo;
        [DataMember]
        public decimal izm_saldo;
        [DataMember]
        public decimal sum_outsaldo;
        [DataMember]
        public decimal sum_fin;
        [DataMember]
        public decimal sum_dolg;
    }


    [DataContract]
    [Serializable]
    public struct _RecordDomODN    //запись квартиры по ОДН
    //----------------------------------------------------------------------
    {
        [DataMember]
        public string fio;
        [DataMember]
        public string alg;
        [DataMember]
        public int num_ls;
        [DataMember]
        public decimal kpu_rashod;
        [DataMember]
        public decimal norma_rashod;
        [DataMember]
        public decimal rashod;
        [DataMember]
        public decimal pl_kvar;
        [DataMember]
        public decimal count_gil;
        [DataMember]
        public decimal count_room;
        [DataMember]
        public decimal norma;
        [DataMember]
        public string nkvar;
    }


    [DataContract]
    [Serializable]
    public struct _RecordSzFin    //запись сальдо
    //----------------------------------------------------------------------
    {
        [DataMember]
        public decimal ls_lgota;
        [DataMember]
        public decimal ls_edv;
        [DataMember]
        public decimal ls_teplo;
        [DataMember]
        public decimal ls_smo;
    }

    //----------------------------------------------------------------------
    [DataContract]
    public class Saldo : Ls
    //----------------------------------------------------------------------
    {
        string _groupby;


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
        public string groupby { get { return Utils.ENull(_groupby); } set { _groupby = value; } } //

        [DataMember]
        public int nzp_charge { get; set; }
        [DataMember]
        public int nzp_serv { get; set; }
        [DataMember]
        public string service { get; set; }
        [DataMember]
        public string measure { get; set; }
        [DataMember]
        public long nzp_supp { get; set; }
        [DataMember]
        public int nzp_payer { get; set; }
        [DataMember]
        public string supplier { get; set; }

        //[DataMember]
        //public RecordSaldo saldo;
        _RecordSaldo saldo;

        [DataMember]
        public decimal sum_real { get { return saldo.sum_real; } set { saldo.sum_real = value; } }
        [DataMember]
        public decimal rsum_tarif { get { return saldo.rsum_tarif; } set { saldo.rsum_tarif = value; } }
        [DataMember]
        public decimal sum_charge { get { return saldo.sum_charge; } set { saldo.sum_charge = value; } }
        [DataMember]
        public decimal reval { get { return saldo.reval; } set { saldo.reval = value; } }
        [DataMember]
        public decimal real_charge { get { return saldo.real_charge; } set { saldo.real_charge = value; } }
        [DataMember]
        public decimal sum_money { get { return saldo.sum_money; } set { saldo.sum_money = value; } }
        [DataMember]
        public decimal money_to { get { return saldo.money_to; } set { saldo.money_to = value; } }
        [DataMember]
        public decimal money_from { get { return saldo.money_from; } set { saldo.money_from = value; } }
        [DataMember]
        public decimal money_del { get { return saldo.money_del; } set { saldo.money_del = value; } }
        [DataMember]
        public decimal sum_insaldo { get { return saldo.sum_insaldo; } set { saldo.sum_insaldo = value; } }
        [DataMember]
        public decimal izm_saldo { get { return saldo.izm_saldo; } set { saldo.izm_saldo = value; } }
        [DataMember]
        public decimal sum_outsaldo { get { return saldo.sum_outsaldo; } set { saldo.sum_outsaldo = value; } }

        [DataMember]
        public decimal sum_fin { get { return saldo.sum_fin; } set { saldo.sum_fin = value; } }
        [DataMember]
        public decimal sum_dolg { get { return saldo.sum_dolg; } set { saldo.sum_dolg = value; } }

        [DataMember]
        public int find_from_the_start { get; set; }

        [DataMember]
        public int order_by { get; set; }

        [DataMember]
        public int ordering { get; set; }

        /*
        public _ChargeS AsChargeS()
        {
            _ChargeS charge = new _ChargeS();
            charge.YM = YM;

            charge.groupby = groupby;

            charge.nzp_charge = nzp_charge;
            charge.nzp_serv = nzp_serv;
            charge.service = service;
            charge.nzp_supp = nzp_supp;
            charge.supplier = supplier;

            if (saldo.sum_real != 0) charge.sum_real = saldo.sum_real.ToString();
            if (saldo.sum_real != 0) charge.sum_charge = saldo.sum_charge.ToString();
            if (saldo.sum_real != 0) charge.reval = saldo.reval.ToString();
            if (saldo.sum_real != 0) charge.real_charge = saldo.real_charge.ToString();
            if (saldo.sum_real != 0) charge.sum_money = saldo.sum_money.ToString();
            if (saldo.sum_real != 0) charge.money_to = saldo.money_to.ToString();
            if (saldo.sum_real != 0) charge.money_from = saldo.money_from.ToString();
            if (saldo.sum_real != 0) charge.money_del = saldo.money_del.ToString();
            if (saldo.sum_real != 0) charge.sum_insaldo = saldo.sum_insaldo.ToString();
            if (saldo.sum_real != 0) charge.izm_saldo = saldo.izm_saldo.ToString();
            if (saldo.sum_real != 0) charge.sum_outsaldo = saldo.sum_outsaldo.ToString();

            if (saldo.sum_real != 0) charge.sum_fin = saldo.sum_fin.ToString();
            if (saldo.sum_real != 0) charge.sum_dolg = saldo.sum_dolg.ToString();

            charge.find_from_the_start = find_from_the_start = 0;

            return charge;
        }
        */

        public Saldo()
            : base()
        {
            order_by = ordering = 0;
            YM.month_ = 0;
            YM.year_ = 0;

            groupby = "";
            nzp_payer = 0;
            nzp_charge = 0;
            nzp_serv = 0;
            service = "";
            measure = "";
            nzp_supp = 0;
            supplier = "";

            saldo.sum_real = 0;
            saldo.sum_charge = 0;
            saldo.reval = 0;
            saldo.real_charge = 0;

            saldo.sum_money = 0;
            saldo.money_to = 0;
            saldo.money_from = 0;
            saldo.money_del = 0;
            saldo.sum_insaldo = 0;
            saldo.izm_saldo = 0;
            saldo.sum_outsaldo = 0;

            saldo.sum_fin = 0;
            saldo.sum_dolg = 0;

            find_from_the_start = 0;
        }
    }
    //----------------------------------------------------------------------
    [DataContract]
    public class Charge : Saldo
    //----------------------------------------------------------------------
    {
        string _dat_charge;

        /// <summary> Месяц начислений
        /// </summary>
        [DataMember]
        public string dat_month { get; set; }
        /// <summary> Признак наличия перерасчетов, вызванных из будущего (1 - да, 0 - нет)
        /// </summary>
        [DataMember]
        public int has_future_reval { get; set; }
        /// <summary> Признак наличия перерасчетов за предыдущие месяцы (1 - да, 0 - нет)
        /// </summary>
        [DataMember]
        public int has_past_reval { get; set; }
        [DataMember]
        public int month_po { get; set; }

        [DataMember]
        public int nzp_frm { get; set; }

        [DataMember]
        public int isdel { get; set; }

        [DataMember]
        public string name_frm { get; set; }
        /// <summary> ID записи
        /// </summary>
        [DataMember]
        public int id { get; set; }
        /// <summary> ID родительской записи (нужен для отображения перерасчетов, указывает на строку с исходными начислениями)
        /// </summary>
        [DataMember]
        public int parent_id { get; set; }

        [DataMember]
        public string dat_charge { get { return Utils.ENull(_dat_charge); } set { _dat_charge = value; } } //
        [DataMember]
        public decimal tarif { get; set; }
        [DataMember]
        public decimal tarif_p { get; set; }
        [DataMember]
        public decimal rsum_tarif_p { get; set; }
        [DataMember]
        public decimal rsum_lgota { get; set; }
        [DataMember]
        public decimal sum_tarif { get; set; }
        [DataMember]
        public decimal sum_dlt_tarif { get; set; }
        [DataMember]
        public decimal sum_dlt_tarif_p { get; set; }
        [DataMember]
        public decimal sum_tarif_p { get; set; }
        [DataMember]
        public decimal sum_lgota { get; set; }
        [DataMember]
        public decimal sum_dlt_lgota { get; set; }
        [DataMember]
        public decimal sum_dlt_lgota_p { get; set; }
        [DataMember]
        public decimal sum_lgota_p { get; set; }
        [DataMember]
        public decimal sum_nedop { get; set; }
        [DataMember]
        public decimal sum_nedop_p { get; set; }
        [DataMember]
        public decimal real_pere { get; set; }
        [DataMember]
        public decimal sum_pere { get; set; }
        [DataMember]
        public decimal sum_fakt { get; set; }
        [DataMember]
        public decimal fakt_to { get; set; }
        [DataMember]
        public decimal fakt_from { get; set; }
        [DataMember]
        public decimal fakt_del { get; set; }
        [DataMember]
        public int is_device { get; set; }
        /// <summary> Начисленный расход (Начислено (sum_tarif) / Тариф (tarif))
        /// </summary>
        [DataMember]
        public decimal c_calc { get; set; }
        /// <summary> Прошлый расход
        /// </summary>
        [DataMember]
        public decimal c_calc_p { get; set; }
        /// <summary> Полный расход (Полный расчет (rsum_tarif) / Тариф (tarif))
        /// </summary>
        [DataMember]
        public decimal c_calc_full { get; set; }
        [DataMember]
        public decimal c_calc_full_p { get; set; }
        [DataMember]
        public decimal c_sn { get; set; }
        [DataMember]
        public decimal c_okaz { get; set; }
        [DataMember]
        public decimal c_nedop { get; set; }
        [DataMember]
        public decimal c_reval { get; set; }
        [DataMember]
        public decimal reval_tarif { get; set; }
        [DataMember]
        public decimal reval_lgota { get; set; }
        [DataMember]
        public decimal tarif_f { get; set; }
        [DataMember]
        public decimal sum_tarif_eot { get; set; }
        [DataMember]
        public decimal sum_tarif_sn_eot { get; set; }
        [DataMember]
        public decimal sum_tarif_sn_f { get; set; }
        [DataMember]
        public decimal rsum_subsidy { get; set; }
        [DataMember]
        public decimal sum_subsidy { get; set; }
        [DataMember]
        public decimal sum_subsidy_p { get; set; }
        [DataMember]
        public decimal sum_subsidy_reval { get; set; }
        [DataMember]
        public decimal sum_subsidy_all { get; set; }
        [DataMember]
        public decimal sum_lgota_eot { get; set; }
        [DataMember]
        public decimal sum_lgota_f { get; set; }
        [DataMember]
        public decimal sum_smo { get; set; }
        [DataMember]
        public decimal tarif_f_p { get; set; }
        [DataMember]
        public decimal sum_tarif_eot_p { get; set; }
        [DataMember]
        public decimal sum_tarif_sn_eot_p { get; set; }
        [DataMember]
        public decimal sum_tarif_sn_f_p { get; set; }
        [DataMember]
        public decimal sum_lgota_eot_p { get; set; }
        [DataMember]
        public decimal sum_lgota_f_p { get; set; }
        [DataMember]
        public decimal reval_pol { get; set; }
        [DataMember]
        public decimal reval_otr { get; set; }
        /// <summary> Отрицательная часть входящего сальдо
        /// </summary>
        [DataMember]
        public decimal sum_insaldo_k { get; set; }

        /// <summary> Положительная часть входящего сальдо
        /// </summary>
        [DataMember]
        public decimal sum_insaldo_d { get; set; }

        /// <summary> Отрицательная часть исходящего сальдо
        /// </summary>
        [DataMember]
        public decimal sum_outsaldo_k { get; set; }

        /// <summary> Положительная часть исходящего сальдо
        /// </summary>
        [DataMember]
        public decimal sum_outsaldo_d { get; set; }

        [DataMember]
        public decimal real_charge_otr { get; set; }

        [DataMember]
        public decimal real_charge_pol { get; set; }

        [DataMember]
        public bool clickable { get; set; }

        [DataMember]
        public decimal sum_tarif_f { get; set; }
        [DataMember]
        public decimal sum_tarif_f_p { get; set; }
        [DataMember]
        public decimal norma { get; set; }
        [DataMember]
        public decimal norma_rashod { get; set; }

        [DataMember]
        public int priznak_rasch { get; set; }

        [DataMember]
        public decimal rashod { get; set; }

        [DataMember]
        public decimal rashod_odn { get; set; }

        [DataMember]
        public long nzp_payer_princip { get; set; }
        [DataMember]
        public string princip { get; set; }
        [DataMember]
        public long nzp_payer_agent { get; set; }
        [DataMember]
        public string agent { get; set; }
        [DataMember]
        public long nzp_payer_supp { get; set; }
        [DataMember]
        public string supp { get; set; }

        public Charge()
            : base()
        {
            princip = agent = supp = "";
            nzp_payer_agent = nzp_payer_princip = nzp_payer_supp = 0;
            dat_month = "";
            has_future_reval = 0;
            has_past_reval = 0;
            nzp_frm = 0;
            name_frm = "";
            norma = 0;
            norma_rashod = 0;
            rashod = rashod_odn = 0;
            priznak_rasch = 0;
            id = 0;
            parent_id = 0;
            nzp_charge = 0;
            nzp_serv = 0;
            nzp_supp = 0;
            nzp_frm = 0;
            dat_charge = "";
            tarif = 0;
            tarif_p = 0;
            rsum_tarif = 0;
            rsum_tarif_p = 0;
            rsum_lgota = 0;
            sum_tarif = 0;
            isdel = 0;
            sum_dlt_tarif = 0;
            sum_dlt_tarif_p = 0;
            sum_tarif_p = 0;
            sum_lgota = 0;
            sum_dlt_lgota = 0;
            sum_dlt_lgota_p = 0;
            sum_lgota_p = 0;
            sum_nedop = 0;
            sum_nedop_p = 0;
            sum_fakt = 0;
            fakt_to = 0;
            fakt_from = 0;
            fakt_del = 0;
            is_device = 0;
            c_calc = 0;
            c_calc_p = 0;
            c_calc_full = 0;
            c_calc_full_p = 0;
            c_sn = 0;
            c_okaz = 0;
            c_nedop = 0;
            c_reval = 0;
            reval_tarif = 0;
            reval_lgota = 0;
            tarif_f = 0;
            sum_tarif_eot = 0;
            sum_tarif_sn_eot = 0;
            sum_tarif_sn_f = 0;
            rsum_subsidy = 0;
            sum_subsidy = 0;
            sum_subsidy_p = 0;
            sum_subsidy_reval = 0;
            sum_subsidy_all = 0;
            sum_lgota_eot = 0;
            sum_lgota_f = 0;
            sum_smo = 0;
            tarif_f_p = 0;
            sum_tarif_eot_p = 0;
            sum_tarif_sn_eot_p = 0;
            sum_tarif_sn_f_p = 0;
            sum_lgota_eot_p = 0;
            sum_lgota_f_p = 0;

            sum_insaldo_k = 0;
            sum_insaldo_d = 0;
            sum_outsaldo_k = 0;
            sum_outsaldo_d = 0;
            real_charge_otr = 0;
            real_charge_pol = 0;
            reval_pol = 0;
            reval_otr = 0;
            clickable = false;
            sum_tarif_f = 0;
            sum_tarif_f_p = 0;
        }
    }

    //----------------------------------------------------------------------
    [DataContract]
    public class SaldoRep : Saldo
    //----------------------------------------------------------------------
    {
        /// <summary> Месяц начислений
        /// </summary>
        [DataMember]
        public string dat_month { get; set; }
        [DataMember]
        public int month_po { get; set; }
        [DataMember]
        public decimal tarif { get; set; }
        [DataMember]
        public decimal sum_tarif { get; set; }
        [DataMember]
        public decimal sum_nedop { get; set; }
        [DataMember]
        public int is_device { get; set; }
        [DataMember]
        public decimal c_calc { get; set; }
        [DataMember]
        public decimal c_okaz { get; set; }
        [DataMember]
        public decimal c_nedop { get; set; }
        [DataMember]
        public decimal c_reval { get; set; }
        [DataMember]
        public decimal sum_insaldo_k { get; set; }
        [DataMember]
        public decimal sum_insaldo_d { get; set; }
        [DataMember]
        public decimal sum_outsaldo_k { get; set; }
        [DataMember]
        public decimal sum_outsaldo_d { get; set; }

        public SaldoRep()
            : base()
        {
            dat_month = "";
            month_po = 0;
            tarif = 0;
            rsum_tarif = 0;
            sum_tarif = 0;
            sum_nedop = 0;
            is_device = 0;
            c_calc = 0;
            c_okaz = 0;
            c_nedop = 0;
            c_reval = 0;
            sum_insaldo_k = 0;
            sum_insaldo_d = 0;
            sum_outsaldo_k = 0;
            sum_outsaldo_d = 0;
        }
    }

    //----------------------------------------------------------------------
    [DataContract]
    public class ChargeFind : Ls
    //----------------------------------------------------------------------
    {
        string _groupby;

        [DataMember]
        public RecordMonth YM;
        [DataMember]
        public RecordMonth YM_po;

        [DataMember]
        public int month_
        {
            get { return YM.month_; }
            set { YM.month_ = value; }
        }
        [DataMember]
        public int month_po
        {
            get { return YM_po.month_; }
            set { YM_po.month_ = value; }
        }
        [DataMember]
        public int year_
        {
            get { return YM.year_; }
            set { YM.year_ = value; }
        }
        [DataMember]
        public int year_po
        {
            get { return YM_po.year_; }
            set { YM_po.year_ = value; }
        }
        [DataMember]
        public int nzp_serv { get; set; }
        [DataMember]
        public string service { get; set; }
        [DataMember]
        public int nzp_supp { get; set; }
        [DataMember]
        public string supplier { get; set; }

        private string _sum_real;
        private string _sum_real_po;
        private string _sum_charge;
        private string _sum_charge_po;
        private string _reval;
        private string _reval_po;
        private string _real_charge;
        private string _real_charge_po;
        private string _sum_money;
        private string _sum_money_po;
        private string _sum_insaldo;
        private string _sum_insaldo_po;
        private string _sum_outsaldo;
        private string _sum_outsaldo_po;
        private string _tarif;
        private string _tarif_po;
        private string _rsum_tarif;
        private string _rsum_tarif_po;
        private string _sum_tarif;
        private string _sum_tarif_po;
        private string _sum_dlt_tarif;
        private string _sum_dlt_tarif_po;
        private string _sum_lgota;
        private string _sum_lgota_po;
        private string _sum_nedop;
        private string _sum_nedop_po;
        private string _c_calc;
        private string _c_calc_po;
        private string _money_to;
        private string _money_from;
        private string _money_del;
        private string _izm_saldo;
        private string _sum_fin;
        private string _sum_dolg;
        private string _is_device;

        [DataMember]
        public string sum_real { get { return Utils.ENull(_sum_real); } set { _sum_real = value; } }
        [DataMember]
        public string sum_real_po { get { return Utils.ENull(_sum_real_po); } set { _sum_real_po = value; } }
        [DataMember]
        public string sum_charge { get { return Utils.ENull(_sum_charge); } set { _sum_charge = value; } }
        [DataMember]
        public string sum_charge_po { get { return Utils.ENull(_sum_charge_po); } set { _sum_charge_po = value; } }
        [DataMember]
        public string reval { get { return Utils.ENull(_reval); } set { _reval = value; } }
        [DataMember]
        public string reval_po { get { return Utils.ENull(_reval_po); } set { _reval_po = value; } }
        [DataMember]
        public string real_charge { get { return Utils.ENull(_real_charge); } set { _real_charge = value; } }
        [DataMember]
        public string real_charge_po { get { return Utils.ENull(_real_charge_po); } set { _real_charge_po = value; } }
        [DataMember]
        public string sum_money { get { return Utils.ENull(_sum_money); } set { _sum_money = value; } }
        [DataMember]
        public string sum_money_po { get { return Utils.ENull(_sum_money_po); } set { _sum_money_po = value; } }
        [DataMember]
        public string sum_insaldo { get { return Utils.ENull(_sum_insaldo); } set { _sum_insaldo = value; } }
        [DataMember]
        public string sum_insaldo_po { get { return Utils.ENull(_sum_insaldo_po); } set { _sum_insaldo_po = value; } }
        [DataMember]
        public string sum_outsaldo { get { return Utils.ENull(_sum_outsaldo); } set { _sum_outsaldo = value; } }
        [DataMember]
        public string sum_outsaldo_po { get { return Utils.ENull(_sum_outsaldo_po); } set { _sum_outsaldo_po = value; } }
        [DataMember]
        public string tarif { get { return Utils.ENull(_tarif); } set { _tarif = value; } }
        [DataMember]
        public string tarif_po { get { return Utils.ENull(_tarif_po); } set { _tarif_po = value; } }
        [DataMember]
        public string rsum_tarif { get { return Utils.ENull(_rsum_tarif); } set { _rsum_tarif = value; } }
        [DataMember]
        public string rsum_tarif_po { get { return Utils.ENull(_rsum_tarif_po); } set { _rsum_tarif_po = value; } }
        [DataMember]
        public string sum_tarif { get { return Utils.ENull(_sum_tarif); } set { _sum_tarif = value; } }
        [DataMember]
        public string sum_tarif_po { get { return Utils.ENull(_sum_tarif_po); } set { _sum_tarif_po = value; } }
        [DataMember]
        public string sum_dlt_tarif { get { return Utils.ENull(_sum_dlt_tarif); } set { _sum_dlt_tarif = value; } }
        [DataMember]
        public string sum_dlt_tarif_po { get { return Utils.ENull(_sum_dlt_tarif_po); } set { _sum_dlt_tarif_po = value; } }
        [DataMember]
        public string sum_lgota { get { return Utils.ENull(_sum_lgota); } set { _sum_lgota = value; } }
        [DataMember]
        public string sum_lgota_po { get { return Utils.ENull(_sum_lgota_po); } set { _sum_lgota_po = value; } }
        [DataMember]
        public string sum_nedop { get { return Utils.ENull(_sum_nedop); } set { _sum_nedop = value; } }
        [DataMember]
        public string sum_nedop_po { get { return Utils.ENull(_sum_nedop_po); } set { _sum_nedop_po = value; } }
        [DataMember]
        public string c_calc { get { return Utils.ENull(_c_calc); } set { _c_calc = value; } }
        [DataMember]
        public string c_calc_po { get { return Utils.ENull(_c_calc_po); } set { _c_calc_po = value; } }

        [DataMember]
        public int find_from_the_start { get; set; }

        [DataMember]
        public string groupby { get { return Utils.ENull(_groupby); } set { _groupby = value; } } //
        [DataMember]
        public int nzp_charge { get; set; }
        [DataMember]
        public string money_to { get { return Utils.ENull(_money_to); } set { _money_to = value; } }
        [DataMember]
        public string money_from { get { return Utils.ENull(_money_from); } set { _money_from = value; } }
        [DataMember]
        public string money_del { get { return Utils.ENull(_money_del); } set { _money_del = value; } }
        [DataMember]
        public string izm_saldo { get { return Utils.ENull(_izm_saldo); } set { _izm_saldo = value; } }
        [DataMember]
        public string sum_fin { get { return Utils.ENull(_sum_fin); } set { _sum_fin = value; } }
        [DataMember]
        public string sum_dolg { get { return Utils.ENull(_sum_dolg); } set { _sum_dolg = value; } }
        [DataMember]
        public string is_device { get { return Utils.ENull(_is_device); } set { _is_device = value; } }

        /// <summary>
        /// признак "загружать информацию о перерасчетах": 1 - да, 0 - нет
        /// </summary>
        [DataMember]
        public int is_show_reval { get; set; }

        //соц. норма
        [DataMember]
        public string c_sn { get; set; }
        //ед.измерения
        [DataMember]
        public string measure { get; set; }
        //Адрес Расчетного центра
        [DataMember]
        public string aderes_IRC;
        //телефон расчетного центра
        [DataMember]
        public string tel_IRC;
        //статус лицевого счета
        [DataMember]
        public int stat_lic_cset;
        [DataMember]
        public decimal dolg;
        [DataMember]
        public string fact_gil;

        [DataMember]
        public long nzp_payer_princip { get; set; }
        [DataMember]
        public string princip { get; set; }
        [DataMember]
        public long nzp_payer_agent { get; set; }
        [DataMember]
        public string agent { get; set; }
        [DataMember]
        public long nzp_payer_supp { get; set; }
        [DataMember]
        public string supp { get; set; }

        [DataMember]
        public int no_turnover { get; set; }

        public ChargeFind()
            : base()
        {
            agent = "";
            nzp_payer_agent = 0;
            princip = "";
            nzp_payer_princip = 0;
            supp = "";
            nzp_payer_supp = 0;
            nzp_serv = 0;
            nzp_supp = 0;
            sum_real = "";
            sum_real_po = "";
            sum_charge = "";
            sum_charge_po = "";
            reval = "";
            reval_po = "";
            real_charge = "";
            real_charge_po = "";
            sum_money = "";
            sum_money_po = "";
            sum_insaldo = "";
            sum_insaldo_po = "";
            sum_outsaldo = "";
            sum_outsaldo_po = "";
            tarif = "";
            tarif_po = "";
            rsum_tarif = "";
            rsum_tarif_po = "";
            sum_tarif = "";
            sum_tarif_po = "";
            sum_dlt_tarif = "";
            sum_dlt_tarif_po = "";
            sum_lgota = "";
            sum_lgota_po = "";
            sum_nedop = "";
            sum_nedop_po = "";
            c_calc = "";
            c_calc_po = "";

            find_from_the_start = 0;
            _groupby = "";
            nzp_charge = 0;
            money_to = "";
            money_from = "";
            money_del = "";
            izm_saldo = "";
            sum_fin = "";
            sum_dolg = "";
            is_device = "";

            is_show_reval = 1;
            //
            c_sn = "";
            measure = "";
            aderes_IRC = "";
            tel_IRC = "";
            stat_lic_cset = 0;
            dolg = 0;
            fact_gil = "";

            no_turnover = 0;

            YM = new RecordMonth();
            YM_po = new RecordMonth();
        }
    }

    [DataContract]
    public class MoneyDistrib : Dom
    {
        string _groupby;
        string _dat_oper;
        string _dat_oper_po;

        [DataMember]
        public RecordMonth YM;

        [DataMember]
        public int month_
        {
            get { return YM.month_; }
            set { YM.month_ = value; }
        }
        [DataMember]
        public int year_
        {
            get { return YM.year_; }
            set { YM.year_ = value; }
        }

        [DataMember]
        public string groupby { get { return Utils.ENull(_groupby); } set { _groupby = value; } } //

        [DataMember]
        public int nzp_dis { get; set; }

        [DataMember]
        public long nzp_payer { get; set; }
        [DataMember]
        public string payer { get; set; }
        [DataMember]
        public long nzp_payer_princip { get; set; }
        [DataMember]
        public string princip { get; set; }
        [DataMember]
        public long nzp_payer_agent { get; set; }
        [DataMember]
        public string agent { get; set; }
        [DataMember]
        public long nzp_payer_supp { get; set; }
        [DataMember]
        public string supp { get; set; }

        [DataMember]
        public long nzp_supp { get; set; }

        [DataMember]
        public int nzp_serv { get; set; }
        [DataMember]
        public int nzp_serv_from { get; set; }
        [DataMember]
        public string service { get; set; }
        [DataMember]
        public string service_from { get; set; }
        [DataMember]
        public int nzp_bank { get; set; }

        [DataMember]
        public int bank_not_choosen { get; set; }

        //[DataMember]
        //public string bank { get; set; }

        [DataMember]
        public string dat_oper { get { return Utils.ENull(_dat_oper); } set { _dat_oper = value; } } //
        [DataMember]
        public string dat_oper_po { get { return Utils.ENull(_dat_oper_po); } set { _dat_oper_po = value; } } //

        [DataMember]
        public decimal sum_in { get; set; }     // входящее сальдо
        [DataMember]
        public decimal sum_rasp { get; set; }   // 
        [DataMember]
        public decimal sum_ud { get; set; }     // сумма удержания
        [DataMember]
        public decimal sum_naud { get; set; }   // начислено к удержанию
        [DataMember]
        public decimal sum_reval { get; set; }  // перерасчет
        [DataMember]
        public decimal sum_charge { get; set; } // 
        [DataMember]
        public decimal sum_send { get; set; }   //
        [DataMember]
        public decimal sum_out { get; set; }    // исходящее сальдо
        public long nzp_payer_podr { get; set; }
        [DataMember]
        public string podr { get; set; }

        public MoneyDistrib()
            : base()
        {
            _groupby = "";
            _dat_oper = "";
            _dat_oper_po = "";
            month_ = 0;
            year_ = 0;
            nzp_dis = 0;
            nzp_payer = 0;
            payer = "";
            nzp_supp = 0;
            nzp_serv = 0;
            nzp_serv_from = 0;
            service = "";
            service_from = "";
            nzp_bank = 0;
            bank = "";
            sum_charge = 0;
            sum_in = 0;
            bank_not_choosen = 0;
            sum_naud = 0;
            sum_out = 0;
            sum_rasp = 0;
            sum_reval = 0;
            sum_send = 0;
            sum_ud = 0;
            agent = "";
            nzp_payer_agent = 0;
            princip = "";
            nzp_payer_princip = 0;
            supp = "";
            nzp_payer_supp = 0;
            nzp_payer_podr = 0;
            podr = "";
        }
    }

    public class Dept : Finder
    {
        public int nzp_kvar { get; set; }
        public string dat_s { get; set; }
        public string dat_po { get; set; }
    }

    public class ChargeUnloadPrm : Finder
    {
        public int nzp_payer_agent { get; set; }
        public int nzp_payer_princip { get; set; }
        public string dat_s { get; set; }
        public string dat_po { get; set; }

        public ChargeUnloadPrm()
        {
            dat_s = "";
            dat_po = "";
            nzp_payer_agent = 0;
        }
    }

    [DataContract]
    public class Credit : Ls
    {
        [DataMember]
        public int nzp_kredit { get; set; }
        [DataMember]
        public int nzp_serv { get; set; }
        [DataMember]
        public string service { get; set; }
        [DataMember]
        public string dat_month { get; set; }
        [DataMember]
        public string dat_s { get; set; }
        [DataMember]
        public string dat_po { get; set; }
        [DataMember]
        public decimal refinance_state { get; set; }
        [DataMember]
        public string period
        {
            get
            {
                return dat_s + " - " + dat_po;
            }
            set { }
        }
        [DataMember]
        public int valid { get; set; }
        [DataMember]
        public decimal sum_dolg { get; set; }   // сумма кредита
        [DataMember]
        public decimal perc { get; set; }   // процент рассрочки

        [DataMember]
        public RecordMonth YM;

        [DataMember]
        public int month_
        {
            get { return YM.month_; }
            set { YM.month_ = value; }
        }
        [DataMember]
        public int year_
        {
            get { return YM.year_; }
            set { YM.year_ = value; }
        }
        [DataMember]
        public string name_month
        {
            get { return YM.name_month; }
            set { }
        }

        [DataMember]
        public string dog_num { get; set; }

        [DataMember]
        public DateTime? dog_dat { get; set; }

        [DataMember]
        public string state_id { get; set; }

        public Credit()
            : base()
        {
            nzp_kredit = 0;
            nzp_serv = 0;
            service = "";
            dat_month = "";
            dat_s = "";
            dat_po = "";
            valid = Constants._ZERO_;
            sum_dolg = 0;
            perc = 0;
            YM.month_ = 0;
            YM.year_ = 0;
            dog_dat = null;
            dog_num = "";
            state_id = "";
        }
    }

    [DataContract]
    public class CreditDetails : Credit
    {
        [DataMember]
        public int nzp_kredx { get; set; }
        [DataMember]
        public decimal sum_indolg { get; set; }
        [DataMember]
        public decimal sum_odna12 { get; set; }
        [DataMember]
        public decimal sum_perc { get; set; }
        [DataMember]
        public decimal sum_charge { get; set; }
        [DataMember]
        public decimal sum_outdolg { get; set; }
        [DataMember]
        public decimal sum_money { get; set; }

        public CreditDetails()
            : base()
        {
            nzp_kredx = 0;
            sum_indolg = 0;
            sum_odna12 = 0;
            sum_perc = 0;
            sum_charge = 0;
            sum_outdolg = 0;
        }
    }

    [DataContract]
    public class MoneyNaud : Finder
    {
        [DataMember]
        public int nzp_dis { get; set; }
        [DataMember]
        public int nzp_payer { get; set; }
        [DataMember]
        public string payer { get; set; }
        [DataMember]
        public string service { get; set; }
        [DataMember]
        public decimal sum_ud { get; set; }     // сумма удержания
        [DataMember]
        public decimal sum_naud { get; set; }   // начислено к удержанию
        [DataMember]
        public decimal sum_prih { get; set; }
        [DataMember]
        public decimal perc_ud { get; set; }

        [DataMember]
        public int distrib { get; set; }

        /// <summary>
        /// mode = 1 - Следует удержать за обслуживание
        /// = 2 - Начислено за обслуживание
        /// </summary>
        [DataMember]
        public int mode { get; set; }

        public MoneyNaud()
            : base()
        {
            nzp_dis = distrib = 0;
            sum_prih = 0;
            nzp_payer = 0;
            perc_ud = 0;
            payer = "";
            service = "";
            sum_ud = 0;
            mode = 0;
            sum_naud = 0;
        }
    }

    [DataContract]
    public class MoneySended : MoneyDistrib
    {
        [DataMember]
        public int nzp_snd { get; set; }
        [DataMember]
        public int nzp_fd { get; set; }
        [DataMember]
        public string dogovor { get; set; }
        [DataMember]
        public int nzp_fb { get; set; }
        [DataMember]
        public string dogovor_bank { get; set; }
        [DataMember]
        public int num_pp { get; set; }
        [DataMember]
        public string dat_pp { get; set; }
        [DataMember]
        public string dat_when { get; set; }
        [DataMember]
        public int nzp_snd_ret { get; set; }
        [DataMember]
        public decimal max_sum { get; set; }
        [DataMember]
        public int copy_id { get; set; }

        [DataMember]
        public int fn_dogovor_bank_lnk_id { get; set; }

        public MoneySended()
            : base()
        {
            nzp_snd = 0;
            nzp_fd = 0;
            dogovor = "";
            num_pp = 0;
            dat_pp = "";
            dat_when = "";
            nzp_snd_ret = 0;
            max_sum = 0;
            fn_dogovor_bank_lnk_id = 0;
            copy_id = 0;
        }
    }

    [DataContract]
    public class FnPercent : MoneyDistrib
    {
        [DataMember]
        public int nzp_fp { get; set; }
        [DataMember]
        public int nzp_supp { get; set; }
        [DataMember]
        public int nzp_supp_snyat { get; set; }
        [DataMember]
        public string name_supp { get; set; }
        [DataMember]
        public string name_supp_snyat { get; set; }
        [DataMember]
        public decimal perc_ud { get; set; }
        [DataMember]
        public string dat_s { get; set; }
        [DataMember]
        public string dat_po { get; set; }
        [DataMember]
        public decimal minpl { get; set; }

        [DataMember]
        public List<int> nzp_payers { get; set; }

        [DataMember]
        public string changed_on { get; set; }

        [DataMember]
        public string changed_on_po { get; set; }

        [DataMember]
        public string data_operation { get; set; }

         [DataMember]
        public int nzp_data_operation { get; set; }
        
        [DataMember]
        public int changed_by { get; set; }

        [DataMember]
        public string changed_by_name { get; set; }

        public FnPercent()
            : base()
        {
            nzp_fp = 0;
            nzp_supp = 0;
            name_supp_snyat = "";
            name_supp = "";
            perc_ud = 0;
            nzp_supp_snyat = 0;
            nzp_data_operation = changed_by = 0;
            dat_s = "";
            dat_po = "";
            nzp_payers = new List<int>();
            changed_on = data_operation = changed_by_name = changed_on_po = "";
        }

    }

    [DataContract]
    public class TDocumentBase
    {
        [DataMember]
        public int nzp_doc_base { get; set; }

        [DataMember]
        public int nzp_type_doc { get; set; }

        [DataMember]
        public string type_doc { get; set; }

        [DataMember]
        public string num_doc { get; set; }

        [DataMember]
        public string dat_doc { get; set; }

        [DataMember]
        public string comment { get; set; }

        [DataMember]
        public string note { get; set; }

        [DataMember]
        public string doc_name { get; set; }

        public TDocumentBase()
            : base()
        {
            nzp_doc_base = 0;
            nzp_type_doc = 0;
            type_doc = "";
            num_doc = "";
            dat_doc = "";
            comment = "";
            note = "";
            doc_name = "";
        }
    }

    [DataContract]
    public class Perekidka : Ls
    {
        [DataMember]
        public int nzp_rcl { get; set; }
        [DataMember]
        public int nzp_serv { get; set; }

        [DataMember]
        public string service { get; set; }

        [DataMember]
        public int nzp_supp { get; set; }

        [DataMember]
        public string name_supp { get; set; }

        [DataMember]
        public DateTime date_rcl { get; set; }

        [DataMember]
        public decimal sum_rcl { get; set; }

        [DataMember]
        public decimal distr_sum { set; get; }

        [DataMember]
        public decimal tarif { get; set; }

        [DataMember]
        public decimal volum { get; set; }

        [DataMember]
        public int month_ { get; set; }

        [DataMember]
        public int year_ { get; set; }

        [DataMember]
        public TypeRcl typercl { get; set; }

        [DataMember]
        public int etalon { get; set; }

        /// <summary>
        /// Код группы перекидок
        /// </summary>
        [DataMember]
        public int nzp_reestr { get; set; }

        /// <summary>
        /// Наименование группы перекидок
        /// </summary>
        [DataMember]
        public string reestr { get; set; }

        /// </summary>
        [DataMember]
        public int nzp_oper { get; set; }

        [DataMember]
        public TDocumentBase doc_base { get; set; }

        [DataMember]
        public List<decimal> sums { get; set; }


        public Perekidka()
            : base()
        {
            nzp_oper = 0;
            doc_base = new TDocumentBase();
            sums = new List<decimal>();
            distr_sum = 0;
            nzp_rcl = 0;
            nzp_serv = 0;
            service = "";
            nzp_supp = 0;
            name_supp = "";
            date_rcl = DateTime.MinValue;
            sum_rcl = 0;
            tarif = 0;
            volum = 0;
            month_ = 0;
            year_ = 0;
            typercl = new TypeRcl();
            etalon = 0;
        }

    }

    public class TypeRcl : Finder
    {
        [DataMember]
        public int type_rcl { get; set; }

        [DataMember]
        public int is_volum { get; set; }

        [DataMember]
        public string typename { get; set; }

        [DataMember]
        public int nzp_type_uchet { get; set; }

        [DataMember]
        public string nzp_type_uchet_exclude { get; set; }

        [DataMember]
        public string is_auto { get; set; }

        [DataMember]
        public string comment { get; set; }

        [DataMember]
        public int is_actual { get; set; }

        [DataMember]
        public string changed_by { get; set; }

        public TypeRcl()
            : base()
        {
            type_rcl = 0;
            is_volum = 0;
            typename = "";
            nzp_type_uchet = 0;
            is_auto = "";
            comment = "";
            is_actual = 0;
            changed_by = "";
            nzp_type_uchet_exclude = "";
        }

    }

    public class TypeDoc : Finder
    {
        [DataMember]
        public int nzp_type_doc { get; set; }

        [DataMember]
        public string doc_name { get; set; }

        [DataMember]
        public int nzp_doc_group { get; set; }

        public TypeDoc()
            : base()
        {
            nzp_type_doc = 0;
            doc_name = "";
            nzp_doc_group = 0;
        }

    }

    public class CheckChMon : Finder
    {
        [DataMember]
        public int nzp_check { get; set; }

        [DataMember]
        public string dat_check { get; set; }

        [DataMember]
        public int month_ { get; set; }

        [DataMember]
        public int yearr { get; set; }

        [DataMember]
        public string note { get; set; }

        [DataMember]
        public int nzp_grp { get; set; }

        [DataMember]
        public string ngroup { get; set; }

        [DataMember]
        public string name_prov { get; set; }

        [DataMember]
        public int status_ { get; set; }

        [DataMember]
        public string status { get; set; }

        [DataMember]
        public int is_critical { get; set; }

        [DataMember]
        public string is_critical_name { get; set; }

        [DataMember]
        public int count_ls { get; set; }

        [DataMember]
        public int nzp_exc { get; set; }

        [DataMember]
        public string rep_name { get; set; }

        [DataMember]
        public string ex_path { get; set; }

        //[DataMember]
        //public string point { get; set; }

        public CheckChMon()
            : base()
        {
            nzp_check = 0;
            month_ = 0;
            dat_check = "";
            yearr = 0;
            note = "";
            nzp_grp = 0;
            ngroup = "";
            name_prov = "";
            status_ = 0;
            status = "";
            point = "";
            is_critical = 0;
            is_critical_name = "";
            count_ls = 0;
            nzp_exc = 0;
            rep_name = "";
            ex_path = "";
        }

    }

    public enum SaldoPart
    {
        /// <summary>
        /// положительная и отрицательная часть сальдо
        /// </summary>
        PositiveNegative = 1,

        /// <summary>
        /// положительная
        /// </summary>
        Positive = 2,

        /// <summary>
        /// отрицательная
        /// </summary>
        Negative = 3
    }

    public enum SposobRaspr
    {
        /// <summary>
        /// пропорционально общей площади
        /// </summary>
        TotSquare = 1,

        /// <summary>
        /// пропорционально количеству жильцов
        /// </summary>
        CountGil = 2,

        /// <summary>
        /// равномерно
        /// </summary>
        Ravnomerno = 3,

        /// <summary>
        /// пропорционально отапливаемой площади
        /// </summary>
        OtoplSquare = 4

    }

    public class ParamsForGroupPerekidki : Finder
    {
        [DataMember]
        public int nzp_reestr { get; set; }
        [DataMember]
        public string dat_uchet { get; set; }
        [DataMember]
        public int nzp_serv { get; set; }
        [DataMember]
        public string service { get; set; }
        [DataMember]
        public string num { get; set; }
        [DataMember]
        public int nzp_supp { get; set; }
        [DataMember]
        public string name_supp { get; set; }
        [DataMember]
        public int saldo_part { get; set; }
        [DataMember]
        public string saldo_part_text { get; set; }
        [DataMember]
        public Decimal sum_izm { get; set; }
        [DataMember]
        public Decimal sum_raspr { get; set; }
        [DataMember]
        public int sposob_raspr { get; set; }
        [DataMember]
        public string sposob_raspr_text { get; set; }
        [DataMember]
        public int on_nzp_serv { get; set; }
        [DataMember]
        public string on_service { get; set; }
        [DataMember]
        public int on_nzp_supp { get; set; }
        [DataMember]
        public string on_name_supp { get; set; }
        [DataMember]
        public int oper_perekidri { get; set; }
        [DataMember]
        public string oper_perekidri_text { get; set; }
        [DataMember]
        public int find_from_start { get; set; }
        [DataMember]
        public string comment { get; set; }

        [DataMember]
        public string created_by { get; set; }
        [DataMember]
        public string changed_by { get; set; }
        [DataMember]
        public int nzp_kvar { get; set; }

        [DataMember]
        public TDocumentBase doc_base { get; set; }

        [DataMember]
        public int type_rcl { get; set; }

        [DataMember]
        public int nzp_payer_supp { get; set; }

        [DataMember]
        public string month_etalon { get; set; }

        /// <summary>
        /// Виды групповых перекидок
        /// </summary>
        public enum Operations
        {
            None = 0,

            /// <summary>
            /// списание вх сальдо
            /// </summary>
            SpisInSaldo = 1,

            /// <summary>
            /// списание исх сальдо
            /// </summary>
            SpisOutSaldo = 2,

            /// <summary>
            /// изменение сальдо на фиксированную сумму
            /// </summary>
            IzmSaldoFixSum = 3,

            /// <summary>
            /// изменение сальдо на расчетную сумму
            /// </summary>
            IzmSaldoRaschSum = 4,

            /// <summary>
            /// перенос исходящего сальдо между услугами
            /// </summary>
            PerenosOutSaldo = 5,

            /// <summary>
            /// перенос суммы между л/с
            /// </summary>
            PerekidkaLsToLs = 6,

            /// <summary>
            /// распределение суммы по услугам
            /// </summary>
            PerekidkaLs = 7
        }

        /// <summary>
        /// Возвращает наименование операции по коду
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static string GetOperationNameById(int id)
        {
            Operations t = GetOperationById(id);
            switch (t)
            {
                case Operations.SpisInSaldo: return "Списание входящего сальдо";
                case Operations.SpisOutSaldo: return "Списание исходящего сальдо";
                case Operations.IzmSaldoFixSum: return "Изменение сальдо на фиксированную сумму";
                case Operations.IzmSaldoRaschSum: return "Изменение сальдо на расчетную сумму";
                case Operations.PerenosOutSaldo: return "Перенос исходящего сальдо между услугами";
                case Operations.PerekidkaLsToLs: return "Перенос суммы между лицевыми счетами";
                case Operations.PerekidkaLs: return "Изменение сальдо по лицевому счету";
                default: return "Операция №" + id;
            }
        }

        /// <summary>
        /// Возвращает операцию по коду
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static Operations GetOperationById(int id)
        {
            if (id == (int)Operations.SpisInSaldo) return Operations.SpisInSaldo;
            else if (id == (int)Operations.SpisOutSaldo) return Operations.SpisOutSaldo;
            else if (id == (int)Operations.IzmSaldoFixSum) return Operations.IzmSaldoFixSum;
            else if (id == (int)Operations.IzmSaldoRaschSum) return Operations.IzmSaldoRaschSum;
            else if (id == (int)Operations.PerenosOutSaldo) return Operations.PerenosOutSaldo;
            else if (id == (int)Operations.PerekidkaLsToLs) return Operations.PerekidkaLsToLs;
            else if (id == (int)Operations.PerekidkaLs) return Operations.PerekidkaLs;
            else return Operations.None;
        }

        public ParamsForGroupPerekidki()
            : base()
        {
            nzp_reestr = 0;
            nzp_serv = nzp_supp = on_nzp_serv = on_nzp_supp = 0;
            nzp_kvar = 0;
            sum_izm = sum_raspr = 0;
            find_from_start = 0;
            comment = num = "";
            service = created_by = changed_by = "";
            name_supp = sposob_raspr_text = oper_perekidri_text = saldo_part_text = "";
            doc_base = new TDocumentBase();
            type_rcl = 0;
            nzp_payer_supp = 0;
            month_etalon = "";
        }
    }

    [DataContract]
    public class GroupsPerekidki : Ls
    {
        [DataMember]
        public int nzp_spls_per { get; set; }
        [DataMember]
        public int month_ { get; set; }
        [DataMember]
        public int year_ { get; set; }
        [DataMember]
        public int nzp_serv { get; set; }
        [DataMember]
        public string service { get; set; }
        [DataMember]
        public long nzp_supp { get; set; }
        [DataMember]
        public decimal sum_real { get; set; }
        [DataMember]
        public decimal real_charge { get; set; }
        [DataMember]
        public decimal sum_money { get; set; }
        [DataMember]
        public decimal sum_insaldo { get; set; }
        [DataMember]
        public decimal sum_izm { get; set; }
        [DataMember]
        public decimal sum_outsaldo { get; set; }
        [DataMember]
        public decimal sum_new_outsaldo { get; set; }
        [DataMember]
        public decimal tot_square { get; set; }
        [DataMember]
        public decimal otopl_square { get; set; }
        [DataMember]
        public decimal kol_gil { get; set; }

        public GroupsPerekidki()
            : base()
        {
            nzp_spls_per = 0;
            month_ = 0;
            otopl_square = 0;
            year_ = 0;
            nzp_serv = 0;
            service = "";
            nzp_supp = 0;
            sum_real = 0;
            real_charge = 0;
            sum_money = 0;
            sum_insaldo = 0;
            sum_izm = 0;
            sum_outsaldo = 0;
            sum_new_outsaldo = 0;
            tot_square = 0;
            kol_gil = 0;
        }
    }

    [DataContract]
    public class PerekidkaLsToLs : Finder
    {
        [DataMember]
        public string dat_uchet { set; get; }

        [DataMember]
        public int etalon { set; get; }

        [DataMember]
        public int nzp_kvar { set; get; }

        [DataMember]
        public int nzp_kvar2 { set; get; }

        //[DataMember]
        //public string pref { set; get; }

        [DataMember]
        public string pref2 { set; get; }

        [DataMember]
        public int nzp_serv { set; get; }

        [DataMember]
        public string service { set; get; }

        [DataMember]
        public int nzp_supp { set; get; }

        [DataMember]
        public string name_supp { set; get; }

        [DataMember]
        public string sum_etalon { set; get; }

        [DataMember]
        public string sum_etalon2 { set; get; }

        [DataMember]
        public string distr_sum { set; get; }

        [DataMember]
        public string distr_sum2 { set; get; }

        [DataMember]
        public string new_outsaldo1 { set; get; }

        [DataMember]
        public string new_outsaldo2 { set; get; }

        [DataMember]
        public string sum_outsaldo { set; get; }

        [DataMember]
        public string sum_outsaldo2 { set; get; }

        [DataMember]
        public decimal izm_saldo { set; get; }

        [DataMember]
        public int type_rcl { set; get; }


        public bool added { set; get; }

        [DataMember]
        public string comment { set; get; }

        [DataMember]
        public int nzp_type_doc { set; get; }

        [DataMember]
        public string num_doc { set; get; }

        [DataMember]
        public string dat_doc { set; get; }

        [DataMember]
        public TDocumentBase doc_base { set; get; }

        public PerekidkaLsToLs()
            : base()
        {
            comment = num_doc = dat_doc = "";
            type_rcl = nzp_type_doc = 0;
            nzp_serv = nzp_supp = nzp_kvar = nzp_kvar2 = 0;
            service = name_supp = pref = pref2 = "";
            sum_etalon = sum_etalon2 = "";
            dat_uchet = "";
            etalon = 0;
            izm_saldo = 0;
            sum_outsaldo = sum_outsaldo2 = "";
            new_outsaldo1 = new_outsaldo2 = "";
            distr_sum = distr_sum2 = "";
            added = false;
            doc_base = new TDocumentBase();
        }
    }

    public class OverPayment : Finder
    {
        [DataMember]
        public int nzp_overpay { set; get; }

        [DataMember]
        public int mark { set; get; }

        [DataMember]
        public int nzp_kvar_from { set; get; }

        [DataMember]
        public int nzp_kvar_to { set; get; }

        [DataMember]
        public string adr_from { set; get; }

        [DataMember]
        public string adr_to { set; get; }

        [DataMember]
        public string pref_to { set; get; }

        [DataMember]
        public int nzp_geu_from { set; get; }

        [DataMember]
        public string geu_from { set; get; }

        [DataMember]
        public int nzp_geu_to { set; get; }

        [DataMember]
        public string geu_to { set; get; }

        [DataMember]
        public int num_ls { set; get; }
        [DataMember]
        public int num_ls_to { set; get; }
        [DataMember]
        public int pkod10 { set; get; }

        [DataMember]
        public int sost_ls { set; get; }

        [DataMember]
        public int nzp_area_from { set; get; }

        [DataMember]
        public decimal sum_overpayment { set; get; }

        [DataMember]
        public decimal sum_overpayment_po { set; get; }

        [DataMember]
        public string calc_month { set; get; }

        [DataMember]
        public int litera { set; get; }

        [DataMember]
        public int nzp_dom_from { set; get; }

        [DataMember]
        public int nzp_ul_from { set; get; }

        [DataMember]
        public string litera_str { set; get; }
        public OverPayment()
            : base()
        {
            nzp_overpay = mark = nzp_geu_from = nzp_geu_to = num_ls = sost_ls = nzp_area_from = nzp_kvar_from = nzp_kvar_to = litera =
                nzp_dom_from = nzp_ul_from = 0;
            adr_from = adr_to = geu_from = geu_to = calc_month = pref_to = litera_str = "";
            sum_overpayment = sum_overpayment_po = 0;
        }
    }

    public class DelSupplier : Finder
    {
        [DataMember]
        public int nzp_del { get; set; }

        [DataMember]
        public int nzp_kvar { get; set; }

        [DataMember]
        public int nzp_serv { get; set; }

        [DataMember]
        public int nzp_supp { get; set; }

        [DataMember]
        public int num_ls { get; set; }

        [DataMember]
        public int year_ { get; set; }

        [DataMember]
        public int kod_sum { get; set; }

        [DataMember]
        public decimal sum_prih { get; set; }

        [DataMember]
        public string dat_month { get; set; }

        [DataMember]
        public string dat_prih { get; set; }

        [DataMember]
        public string dat_uchet { get; set; }

        [DataMember]
        public string dat_plat { get; set; }

        [DataMember]
        public string dat_calc { get; set; }

        [DataMember]
        public string dat_account { get; set; }

        [DataMember]
        public int nzp_doc_base { get; set; }

        [DataMember]
        public int nzp_reestr { get; set; }

        [DataMember]
        public TDocumentBase doc_base { get; set; }

        [DataMember]
        public TypeRcl typercl { get; set; }

        [DataMember]
        public string service { get; set; }

        [DataMember]
        public string name_supp { get; set; }

        [DataMember]
        public int nzp_oper { get; set; }

        [DataMember]
        public string reestr { get; set; }

        public DelSupplier()
            : base()
        {
            nzp_del = 0;
            nzp_oper = 0;
            reestr = "";
            typercl = new TypeRcl();
            service = name_supp = "";
            nzp_serv = 0;
            nzp_kvar = 0;
            nzp_supp = 0;
            num_ls = 0;
            year_ = 0;
            sum_prih = 0;
            kod_sum = 0;
            dat_month = "";
            dat_prih = "";
            dat_uchet = "";
            dat_plat = "";
            dat_calc = "";
            dat_account = "";
            nzp_doc_base = 0;
            nzp_reestr = 0;
            doc_base = new TDocumentBase();
        }

    }


    [DataContract]
    public class CalcMonthForProv
    {
        [DataMember]
        public int nzp_wp { get; set; }
        [DataMember]
        public string pref { get; set; }
        [DataMember]
        public List<DateTime> listCalcMonths { get; set; }

        public CalcMonthForProv()
        {
            nzp_wp = 0;
            pref = "";
            listCalcMonths = new List<DateTime>();
        }
    }
    [DataContract]
    public class PeniNoCalc : Finder
    {
        [DataMember]
        public List<int> nzp_serv_list { get; set; }
        [DataMember]
        public int nzp_supp { get; set; }
        [DataMember]
        public int is_actual { get; set; }
        [DataMember]
        public string date_from { get; set; }
        [DataMember]
        public string date_to { get; set; }
        [DataMember]
        public string created_on { get; set; }
        [DataMember]
        public string changed_on { get; set; }
        [DataMember]
        public string user_created { get; set; }
        [DataMember]
        public string user_changed { get; set; }
        [DataMember]
        public string supp_name { get; set; }
        [DataMember]
        public int nzp_serv { get; set; }
        [DataMember]
        public int id { get; set; }
        [DataMember]
        public int order_num { get; set; }
        [DataMember]
        public string service_name { get; set; }
        [DataMember]
        public string is_actual_str { get; set; }
        [DataMember]
        public Actions CurAction;

        public PeniNoCalc()
        {
            nzp_serv_list = new List<int>();
            nzp_supp = 0;
            is_actual = 0;
            date_from = "";
            date_to = "";
            created_on = "";
            user_created = "";
            changed_on = "";
            user_changed = "";
            is_actual_str = "";
            id = 0;
        }
        public enum Actions
        {
            none = 0,
            Add = 169,
            Delete = 158,
            Update = 5
        }
    }

    /// <summary>
    /// Проводка для пени
    /// </summary>
    [DataContract]
    [Serializable]
    public class Prov
    {

        [DataMember]
        public int id { get; set; }
        [DataMember]
        public int num { get; set; }
        [DataMember]
        public int nzp_kvar { get; set; }
        [DataMember]
        public int num_ls { get; set; }
        [DataMember]
        public int nzp_dom { get; set; }
        [DataMember]
        public int nzp_serv { get; set; }
        [DataMember]
        public int nzp_supp { get; set; }
        [DataMember]
        public int nzp_wp { get; set; }
        [DataMember]
        public int type { get; set; }
        [DataMember]
        public int nzp_source { get; set; }
        [DataMember]
        public decimal rsum_tarif { get; set; }
        [DataMember]
        public decimal sum_prih { get; set; }
        [DataMember]
        public decimal sum_nedop { get; set; }
        [DataMember]
        public decimal sum_reval { get; set; }

        [DataMember]
        public DateTime date_prov { get; set; }
        [DataMember]
        public DateTime date_obligation { get; set; }
        [DataMember]
        public DateTime created_on { get; set; }
        [DataMember]
        public DateTime changed_on { get; set; }

        [DataMember]
        public int created_by { get; set; }

        [DataMember]
        public int changed_by { get; set; }

        [DataMember]
        public int peni_actions_id { get; set; }
        [DataMember]
        public int peni_debt_id { get; set; }


        /// <summary>
        /// доп.поля 
        /// </summary>
        [DataMember]
        public string service { get; set; }
        [DataMember]
        public string supplier { get; set; }
        [DataMember]
        public string username { get; set; }
        [DataMember]
        public string type_prov { get; set; }
    }


    [DataContract]
    public class ProvFinder : Ls
    {
        public ProvFinder()
        {
            type = -1;
            date_obligation = DateTime.MinValue;
            date_prov_from = DateTime.MinValue;
            date_prov_to = DateTime.MaxValue;
            all_year = false;
        }

        [DataMember]
        public DateTime date_prov_from { get; set; }
        [DataMember]
        public DateTime date_prov_to { get; set; }
        [DataMember]
        public DateTime date_obligation { get; set; }
        [DataMember]
        public int nzp_serv { get; set; }
        [DataMember]
        public int nzp_supp { get; set; }
        [DataMember]
        public int type { get; set; }
        [DataMember]
        public bool all_year { get; set; }

    }

}

