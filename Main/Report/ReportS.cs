using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using FastReport;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.Client
{
    [DataContract]
    public class ReportS
    {
        [DataMember]
        public int nzp_user { set; get; }
        [DataMember]
        public int CurT_nzp_kvar { set; get; }
        [DataMember]
        public string CurT_pref { set; get; }
        [DataMember]
        public int CurT_Supg_nzp_zk { set; get; }
        [DataMember]
        public int CurT_nzp_Kart { set; get; }
        [DataMember]
        public int CurT_nzp_user { set; get; }
        [DataMember]
        public bool CurT_Kart_Archive { set; get; }
        [DataMember]
        public int CurT_nzp_area { set; get; }
        [DataMember]
        public int CurT_nzp_dom { set; get; }

        [DataMember]
        public string table_name { set; get; }//для отчета по заказам

        [DataMember]
        public List<_RolesVal> RolesVal { set; get; }
        //Конструктор1
        public ReportS() {
            this.nzp_user = -1;
            this.CurT_nzp_kvar = -1;
            //this.CurT_pref = "";
            this.CurT_nzp_Kart = -1;
            this.CurT_nzp_user = -1;
            this.RolesVal = null;
        }
        //Конструктор2
        public ReportS(int nzp_area, int nzp_user, int CurT_nzp_kvar, string CurT_pref, int CurT_nzp_Kart, int CurT_nzp_user, List<_RolesVal> RolesVal) {
            this.nzp_user = nzp_user;
            this.CurT_nzp_kvar = CurT_nzp_kvar;
            this.CurT_pref = CurT_pref;
            this.CurT_nzp_Kart = CurT_nzp_Kart;
            this.CurT_nzp_user = CurT_nzp_user;
            this.RolesVal = RolesVal;
        }

        public ReportS(int nzp_user, string CurT_pref, int CurT_Supg_nzp_zk, string table_name) {
            this.nzp_user = nzp_user;
            this.CurT_pref = CurT_pref;
            this.CurT_Supg_nzp_zk = CurT_Supg_nzp_zk;
            this.table_name = table_name;
        }

        protected bool SetKvarAdres(Report rep) {
            return SetKvarAdres(rep, 0);
        }

        protected bool SetKvarAdres(Report rep, int reportID) {
            if (Points.IsDemo)
                rep.SetParameterValue("demo", "1");
            else rep.SetParameterValue("demo", "");
            Returns ret = Utils.InitReturns();
            //cli_Prm cli2 = new cli_Prm();
            //Prm finder = new Prm();
            //finder.prm_num = 19;
            //finder.is_actual = -100;
            //finder.nzp_user = CurT_nzp_user;
            //finder.pref = CurT_pref;
            //finder.nzp_kvar = CurT_nzp_kvar;
            //finder.month_ = DateTime.Now.Month;//Points.CalcMonth.month_;
            //finder.year_ = DateTime.Now.Year;//Points.CalcMonth.year_;
            //finder.rows = 1000;
            //List<Prm> list = cli2.GetPrm(finder, enSrvOper.SrvFind, out ret);


            cli_Prm cli2 = new cli_Prm(WCFParams.AdresWcfWeb.CurT_Server);
            Prm finder = new Prm();
            finder.prm_num = 19;
            finder.is_actual = -100;
            finder.nzp_user = CurT_nzp_user;
            finder.pref = CurT_pref;
            finder.nzp_kvar = CurT_nzp_kvar;
            finder.nzp = CurT_nzp_kvar;
            finder.month_ = DateTime.Now.Month;//Points.CalcMonth.month_;
            finder.year_ = DateTime.Now.Year;//Points.CalcMonth.year_;
            finder.rows = 1000;
            List<Prm> list = cli2.FindPrmValues(finder, out ret);
            string ulica = "", ndom = "", korpus = "", nkvar = "", nkvar_n = "";
            if (ret.result && list != null && list.Count > 0)
            {
                for (int i = 0; i < list.Count; i++)
                    switch (list[i].nzp_prm)
                    {
                        case 2017: ulica = list[i].val_prm.Trim().ToUpper(); break;
                        case 2018: ndom = list[i].val_prm.Trim().ToUpper(); break;
                        case 2019: korpus = list[i].val_prm.Trim().ToUpper(); break;
                        case 2020: nkvar = list[i].val_prm.Trim().ToUpper(); break;
                        case 2021: nkvar_n = list[i].val_prm.Trim().ToUpper(); break;
                    }
            }

            Ls finderls = new Ls();
            finderls.nzp_user = nzp_user;//web_user.nzp_user;
            finderls.nzp_kvar = CurT_nzp_kvar;
            finderls.pref = CurT_pref;

            cli_AdresHard cli = new cli_AdresHard(WCFParams.AdresWcfWeb.CurT_Server);

            //List<Ls> LsInfo = cli.LoadLs(finderls, out ret);
            List<Ls> LsInfo = cli.GetLs(finderls, enSrvOper.SrvLoad, out ret);

            string adr = "";

            if (ret.result && LsInfo != null && LsInfo.Count > 0)
            {
                if (reportID == Constants.act_report_spravka_o_sostave_semji)
                {
                    if (LsInfo[0].rajon != "" && LsInfo[0].rajon != "-") rep.SetParameterValue("rajon", LsInfo[0].rajon + ","); else rep.SetParameterValue("rajon", "");

                    if (ulica.Trim() == "") ulica = LsInfo[0].ulica.Trim();
                    adr = ulica;
                    if (ndom.Trim() == "") ndom = LsInfo[0].ndom.Trim();
                    adr += ", д. " + ndom;
                    if (korpus.Trim() == "")
                    {
                        if (LsInfo[0].nkor.Trim() != "-" && LsInfo[0].nkor != "") korpus += " корп. " + LsInfo[0].nkor.Trim();
                    }
                    else
                    {
                        if (korpus.Trim() != "-" && korpus.Trim() != "") korpus += " корп. " + korpus.Trim();
                    }
                    if (korpus != "-") adr += ", корп. " + korpus;
                    if (nkvar.Trim() == "") nkvar = LsInfo[0].nkvar.Trim();
                    if (LsInfo[0].nkvar != "" && LsInfo[0].nkvar != "-") adr += ", кв. " + nkvar;

                    if (nkvar_n.Trim() == "")
                    {
                        if (LsInfo[0].nkvar_n.Trim() != "-" && LsInfo[0].nkvar_n.Trim() != "") nkvar_n += " комн. " + LsInfo[0].nkvar_n.Trim();
                    }
                    else
                    {
                        if (nkvar_n.Trim() != "-" && nkvar_n.Trim() != "") nkvar_n += " комн. " + nkvar_n.Trim();
                    }
                    if (nkvar_n != "-") adr += ", комн. " + nkvar_n;
                    rep.SetParameterValue("adr", adr);

                    return true;
                }
                else
                {
                    adr = "";
                    rep.SetParameterValue("Lschet", LsInfo[0].pkod);
                    rep.SetParameterValue("Lschet_short", LsInfo[0].pkod10);
                    rep.SetParameterValue("fio", LsInfo[0].fio);
                    rep.SetParameterValue("Platelchik", LsInfo[0].fio);

                    if (ulica.Trim() == "") ulica = LsInfo[0].ulica.Trim();
                    adr = ulica;

                    if (ndom.Trim() == "") ndom = LsInfo[0].ndom.Trim();
                    adr += ", д." + ndom;
                    if (korpus.Trim() == "")
                    {
                        if (LsInfo[0].nkor.Trim() != "-" && LsInfo[0].nkor != "") ndom += " корп. " + LsInfo[0].nkor.Trim();
                        korpus = LsInfo[0].nkor.Trim();
                    }
                    else
                    {
                        if (korpus.Trim() != "-" && korpus.Trim() != "") ndom += " корп. " + korpus.Trim();
                    }
                    adr += " " + korpus;

                    if (nkvar.Trim() == "") nkvar = LsInfo[0].nkvar.Trim();
                    adr += ", кв." + nkvar;
                    if (nkvar_n.Trim() == "")
                    {
                        if (LsInfo[0].nkvar_n.Trim() != "-" && LsInfo[0].nkvar_n.Trim() != "") nkvar += " комн. " + LsInfo[0].nkvar_n.Trim();
                        nkvar_n = LsInfo[0].nkvar_n.Trim();
                    }
                    else
                    {
                        if (nkvar_n.Trim() != "-" && nkvar_n.Trim() != "") nkvar += " комн. " + nkvar_n.Trim();
                    }
                    adr += " " + nkvar_n;

                    rep.SetParameterValue("ulica", ulica);
                    rep.SetParameterValue("numdom", ndom);
                    rep.SetParameterValue("ndom", ndom);
                    rep.SetParameterValue("nkvar", nkvar);
                    if (nkvar_n == "-")
                    {
                        rep.SetParameterValue("komn", "");
                        rep.SetParameterValue("nkvar_n", "");
                        rep.SetParameterValue("kind_priv", "Квартира");
                    }
                    else
                    {
                        rep.SetParameterValue("komn", "Комната");
                        rep.SetParameterValue("nkvar_n", LsInfo[0].nkvar_n);
                        rep.SetParameterValue("kind_priv", "Комната");
                    }


                    if (korpus == "-")
                    {
                        rep.SetParameterValue("korp", "");
                        rep.SetParameterValue("nkor", "");
                    }
                    else
                    {
                        rep.SetParameterValue("korp", "Корпус");
                        rep.SetParameterValue("nkor", LsInfo[0].nkor);
                    }

                    rep.SetParameterValue("kvnum", nkvar);
                    rep.SetParameterValue("town", LsInfo[0].rajon);
                    rep.SetParameterValue("Adres", adr);//LsInfo[0].adr);
                    rep.SetParameterValue("pod", LsInfo[0].porch);
                    rep.SetParameterValue("num_geu", LsInfo[0].geu);
                    return true;
                }
            }
            else
            {
                rep.SetParameterValue("pod", "-");
                rep.SetParameterValue("Lschet", "");
                rep.SetParameterValue("Lschet_short", "");
                rep.SetParameterValue("fio", "");
                rep.SetParameterValue("Platelchik", "");
                rep.SetParameterValue("ndom", "");
                rep.SetParameterValue("nkvar", "");
                rep.SetParameterValue("nkvar_n", "");
                rep.SetParameterValue("komn", "");
                rep.SetParameterValue("nkor", "");
                rep.SetParameterValue("korp", "");
                rep.SetParameterValue("ulica", "");
                rep.SetParameterValue("numdom", "");
                rep.SetParameterValue("town", "");
                rep.SetParameterValue("kvnum", "");
                rep.SetParameterValue("Adres", "");
                rep.SetParameterValue("num_geu", "");
                MonitorLog.WriteLog("Ошибка поиска ЛС " + ret.text, MonitorLog.typelog.Error, true);

                return false;
            }


        }


        /// <summary>
        /// Квартирные параметры
        /// </summary>
        /// <param name="rep">Ссылка на шаблон отчета</param>
        /// <param name="y_">Год</param>
        /// <param name="m_">Месяц</param>
        /// <returns></returns>
        protected bool SetKvarPrms(Report rep, int y_, int m_) {
            List<Prm> SpisPrm = new List<Prm>();
            Returns ret = Utils.InitReturns();
            Prm finder = new Prm();

            finder.nzp_user = nzp_user;//web_user.nzp_user;
            finder.nzp_kvar = CurT_nzp_kvar;
            finder.nzp = CurT_nzp_kvar;
            finder.pref = CurT_pref;
            finder.prm_num = 1;
            finder.rows = 100;
            finder.month_ = m_;
            finder.year_ = y_;

            cli_Prm cliPrm = new cli_Prm(WCFParams.AdresWcfWeb.CurT_Server);

            //SpisPrm = cliPrm.GetPrm(finder, enSrvOper.SrvFind, out ret);
            SpisPrm = cliPrm.FindPrmValues(finder, out ret);

            rep.SetParameterValue("pl_all", "_____");
            rep.SetParameterValue("kv_pl", "_____");
            rep.SetParameterValue("pl_gil", "____");
            rep.SetParameterValue("kol_kom", "1");
            rep.SetParameterValue("privat", "Не приватизированна");
            rep.SetParameterValue("komf", "");
            rep.SetParameterValue("kolvgil", "");
            rep.SetParameterValue("fact_gil", "");
            rep.SetParameterValue("et", "-");
            rep.SetParameterValue("kvonl", "-");
            rep.SetParameterValue("pss", "-");
            rep.SetParameterValue("poluch", "");
            rep.SetParameterValue("adres_IRC", "");
            rep.SetParameterValue("tel_IRC", "");
            rep.SetParameterValue("stat_Lschet", "");
            rep.SetParameterValue("date_priv", "");
            rep.SetParameterValue("mnogokv", "");

            if (ret.result)
            {

                for (int i = 0; i < SpisPrm.Count; i++)
                {
                    #region Разбираем параметры
                    switch (SpisPrm[i].nzp_prm)
                    {
                        case 3:
                            if ((SpisPrm[i].val_prm == "коммунальное") || (SpisPrm[i].val_prm == "коммунальная"))
                            {

                                rep.SetParameterValue("komf", "коммунальная");
                            }
                            else
                            {
                                rep.SetParameterValue("komf", "изолированная");
                            }
                            break;
                        case 8:
                            if (SpisPrm[i].val_prm == "да")
                            {
                                rep.SetParameterValue("privat", "приватизирована");
                                rep.SetParameterValue("date_priv", SpisPrm[i].dat_s);
                            }
                            else
                            {
                                rep.SetParameterValue("privat", "не приватизирована");
                            }
                            break;

                        case 4:
                            {
                                decimal dl;
                                if (Decimal.TryParse(SpisPrm[i].val_prm, out dl) == true)
                                {
                                    string pl = System.Convert.ToDecimal(SpisPrm[i].val_prm).ToString("f2");
                                    rep.SetParameterValue("pl_all", pl);
                                    rep.SetParameterValue("kv_pl", pl);
                                }
                            }
                            break;
                        case 51: rep.SetParameterValue("stat_Lschet", SpisPrm[i].val_prm); break;
                        case 6:
                            {
                                string pl = SpisPrm[i].val_prm;
                                decimal dl = 0;
                                if (Decimal.TryParse(pl, out dl) == true)
                                {
                                    rep.SetParameterValue("pl_gil", dl.ToString("f2"));
                                }
                                else
                                    rep.SetParameterValue("pl_gil", pl);
                            } break;
                        case 10: rep.SetParameterValue("kolvgil", SpisPrm[i].val_prm); break;
                        case 42: rep.SetParameterValue("kvonl", SpisPrm[i].val_prm); break;
                        case 80: rep.SetParameterValue("poluch", SpisPrm[i].val_prm); break;
                        case 107: rep.SetParameterValue("kol_kom", SpisPrm[i].val_prm); break;
                        case 162: rep.SetParameterValue("pss", SpisPrm[i].val_prm); break;
                        //----
                        case 5: rep.SetParameterValue("kolgil", SpisPrm[i].val_prm); break;
                        case 88: rep.SetParameterValue("IRC", SpisPrm[i].val_prm); break;
                        case 81: rep.SetParameterValue("adres_IRC", SpisPrm[i].val_prm); break;
                        case 96: rep.SetParameterValue("tel_IRC", SpisPrm[i].val_prm); break;
                        case 73: rep.SetParameterValue("num_geu", SpisPrm[i].val_prm); break;
                        case 2005: rep.SetParameterValue("fact_gil", SpisPrm[i].val_prm); break;
                        default:
                            break;
                    }
                    #endregion

                }
            }

            #region Примечание к ЛС
            Prm finder1 = new Prm();

            finder1.nzp_user = nzp_user;//web_user.nzp_user;
            finder1.nzp_kvar = CurT_nzp_kvar;
            finder1.nzp = CurT_nzp_kvar;
            finder1.pref = CurT_pref;
            finder1.prm_num = 18;
            finder1.rows = 10;
            finder1.month_ = m_;
            finder1.year_ = y_;
            finder1.nzp_prm = 2012;

            Prm Primech = new Prm();

            ret = Utils.InitReturns();
            cli_Prm cliPrm1 = new cli_Prm(WCFParams.AdresWcfWeb.CurT_Server);
            Primech = cliPrm1.FindPrmValue(finder1, out ret);

            if ((ret.result == true) & (Primech != null))
            {
                rep.SetParameterValue("primech", Primech.val_prm);
            }
            else
            {
                rep.SetParameterValue("primech", "");
            }
            #endregion


            #region Многоквартирный дом
            Prm finder11 = new Prm();

            finder1.nzp_user = nzp_user;//web_user.nzp_user;
            finder1.nzp_kvar = CurT_nzp_kvar;
            finder1.nzp = CurT_nzp_dom;
            finder1.pref = CurT_pref;
            finder1.prm_num = 2;
            finder1.rows = 100;
            finder1.month_ = m_;
            finder1.year_ = y_;
            finder1.nzp_prm = 730;



            ret = Utils.InitReturns();
            cli_Prm cliPrm11 = new cli_Prm(WCFParams.AdresWcfWeb.CurT_Server);
            Prm mnogokv = cliPrm11.FindPrmValue(finder11, out ret);

            if ((ret.result == true) & (mnogokv != null))
            {
                rep.SetParameterValue("mnogokv", "");
            }
            else
            {
                rep.SetParameterValue("mnogokv", "многоквартирный");
            }
            #endregion

            #region Открыт ЛС
            Prm finder2 = new Prm();
            finder2.nzp_user = nzp_user;//web_user.nzp_user;
            finder2.nzp_kvar = CurT_nzp_kvar;
            finder2.nzp = CurT_nzp_kvar;
            finder2.pref = CurT_pref;
            finder2.prm_num = 3;
            finder2.rows = 10;
            finder2.month_ = m_;
            finder2.year_ = y_;
            finder2.nzp_prm = 51;

            ret = Utils.InitReturns();
            cli_Prm cliPrm2 = new cli_Prm(WCFParams.AdresWcfWeb.CurT_Server);
            Primech = cliPrm2.FindPrmValue(finder2, out ret);

            if ((ret.result == true) & (Primech != null))
            {
                rep.SetParameterValue("stat_Lschet", Primech.val_prm);
            }
            else
            {
                rep.SetParameterValue("stat_Lschet", "");
            }
            #endregion


            #region ФИО для самары
            Prm finder_f = new Prm();
            finder_f.nzp_user = nzp_user;//web_user.nzp_user;
            finder_f.nzp_kvar = CurT_nzp_kvar;
            finder_f.nzp = CurT_nzp_kvar;
            finder_f.pref = CurT_pref;
            finder_f.prm_num = 3;
            finder_f.rows = 10;
            finder_f.month_ = m_;
            finder_f.year_ = y_;
            finder_f.nzp_prm = 46;

            ret = Utils.InitReturns();
            cli_Prm cliPrm_f = new cli_Prm(WCFParams.AdresWcfWeb.CurT_Server);
            Primech = cliPrm_f.FindPrmValue(finder_f, out ret);

            if ((ret.result == true) & (Primech != null))
            {
                rep.SetParameterValue("fio", Primech.val_prm);
            }

            #endregion

            #region ПСС
            finder1.nzp_user = nzp_user;//web_user.nzp_user;
            finder1.nzp_kvar = CurT_nzp_kvar;
            finder1.nzp = CurT_nzp_kvar;
            finder1.pref = CurT_pref;
            finder1.prm_num = 15;
            finder1.rows = 10;
            finder1.month_ = m_;
            finder1.year_ = y_;
            finder1.nzp_prm = 162;

            ret = Utils.InitReturns();
            cli_Prm cliPrm3 = new cli_Prm(WCFParams.AdresWcfWeb.CurT_Server);
            Primech = cliPrm3.FindPrmValue(finder1, out ret);

            if ((ret.result == true) & (Primech != null))
            {
                rep.SetParameterValue("pss", Primech.val_prm);
            }
            else
            {
                rep.SetParameterValue("pss", "");
            }
            #endregion

            #region Параметры ИРЦ
            finder.nzp_user = nzp_user;//web_user.nzp_user;
            finder.nzp_kvar = CurT_nzp_kvar;
            finder.nzp = CurT_nzp_kvar;
            finder.pref = CurT_pref;
            finder.prm_num = 10;
            finder.rows = 100;
            finder.month_ = m_;
            finder.year_ = y_;
            cli_Prm cliPrm4 = new cli_Prm(WCFParams.AdresWcfWeb.CurT_Server);
            SpisPrm = cliPrm4.FindPrmValues(finder, out ret);


            if ((ret.result) & (SpisPrm != null))
            {

                for (int i = 0; i < SpisPrm.Count; i++)
                {
                    #region Разбираем параметры
                    switch (SpisPrm[i].nzp_prm)
                    {
                        case 88: rep.SetParameterValue("IRC", SpisPrm[i].val_prm); break;
                        case 81: rep.SetParameterValue("adres_IRC", SpisPrm[i].val_prm); break;
                        case 96: rep.SetParameterValue("tel_IRC", SpisPrm[i].val_prm); break;
                        default:
                            break;
                    }
                    #endregion

                }
            }
            #endregion

            return true;

        }

        /// <summary>
        /// Количество проживающих
        /// </summary>
        /// <param name="rep">Ссылка на шаблон отчета</param>
        /// <param name="y_">Год</param>
        /// <param name="m_">Месяц</param>
        /// <returns></returns>
        protected string SetKolGil(Report rep, int y_, int m_) {
            MonthLs finderls = new MonthLs();
            rep.SetParameterValue("kolgil", 0);
            finderls.nzp_user = this.nzp_user; //web_user.nzp_user;
            finderls.nzp_kvar = CurT_nzp_kvar;
            finderls.pref = CurT_pref;
            string s = "01." + m_.ToString() + "." + y_.ToString();


            finderls.dat_month = System.Convert.ToDateTime(s);

            cli_Adres cli = new cli_Adres(WCFParams.AdresWcfWeb.CurT_Server);
            Returns ret = Utils.InitReturns();
            string kol_gil = cli.GetKolGil(finderls, out ret);

            if (ret.result)
            {
                rep.SetParameterValue("kolgil", kol_gil);
            }
            return kol_gil;

        }

        //--------------------------------Отчеты------------------------------------------------------------------------------------------------------------------------------

        public string Fill_web_sprav_samara(Report rep, int y_, int m_, string date, int vidSprav, Gilec finder, out Returns ret, List<GilecFullInf> listgil, string[] psp) {
            #region Заполнение глобальных параметров
            //Returns ret = Utils.InitReturns();
            ret = Utils.InitReturns();
            try
            {

                this.nzp_user = finder.nzp_user;
                this.CurT_nzp_kvar = finder.nzp_kvar;
                this.CurT_pref = finder.pref;
                this.CurT_nzp_Kart = finder.nzp_kart;
                this.CurT_nzp_user = finder.nzp_user;
            }
            catch
            {
                return "";
            }

            #endregion


            #region Запонение таблицы для отчета

            DataSet FDataSet = new DataSet();

            DataTable table = new DataTable();
            table.TableName = "Q_master";
            FDataSet.Tables.Add(table);

            table.Columns.Add("fam", typeof(string));
            table.Columns.Add("ima", typeof(string));
            table.Columns.Add("otch", typeof(string));


            //выбор параметров для конкретной справки
            switch (vidSprav)
            {
                case 5:
                    {
                        table.Columns.Add("rod", typeof(string));
                        table.Columns.Add("dat_rog", typeof(string));
                        table.Columns.Add("dat_prib", typeof(string));
                        table.Columns.Add("dat_vip", typeof(string));
                        break;
                    }
            }
            #endregion

            #region Выборка данных


            //Gilec finder = new Gilec();

            //finder.nzp_user = nzp_user;//web_user.nzp_user;
            //finder.nzp_kvar = CurT_nzp_kvar;
            //finder.nzp_kart = CurT_nzp_Kart;
            //finder.pref = CurT_pref;


            //List<GilecFullInf> listgil = null;

            //cli_Gilec cli = new cli_Gilec();          
            //listgil = cli.GetFullInfGilList_AllKards(finder, out ret);

            //listgil = DbGilec.GetFullInfGilList_AllKards(finder, out ret);



            if (listgil != null)// & ret.result)
            {
                if (listgil.Count > 0)
                {
                    for (int i = 0; i < listgil.Count; i++)
                    {
                        //для таблицы           
                        string dat_rog = "";
                        string dat_prib = "";
                        string dat_vip = "";

                        if (listgil[i].dat_rog.Trim() != "")
                        {
                            dat_rog = System.Convert.ToDateTime(listgil[i].dat_rog).ToShortDateString();
                        }

                        if (listgil[i].rod.ToUpper().Trim() == "НАНИМАТЕЛЬ")
                        {
                            rep.SetParameterValue("vl_fam", listgil[i].fam.Trim());
                            rep.SetParameterValue("vl_ima", listgil[i].ima.Trim());
                            rep.SetParameterValue("vl_otch", listgil[i].otch.Trim());
                        }


                        if (listgil[i].nzp_kart != CurT_nzp_Kart)
                        {
                            //выбор параметров для конкретной справки
                            switch (vidSprav)
                            {
                                //case 1:
                                case 5:
                                    {
                                        if (listgil[i].dat_prib.Trim() != "")
                                        {
                                            dat_prib = System.Convert.ToDateTime(listgil[i].dat_prib).ToShortDateString();
                                        }

                                        if (listgil[i].dat_vip.Trim() != "")
                                        {
                                            dat_vip = System.Convert.ToDateTime(listgil[i].dat_vip).ToShortDateString();
                                        }
                                        table.Rows.Add(
                                           listgil[i].fam.Trim(),
                                           listgil[i].ima.Trim(),
                                           listgil[i].otch.Trim(),
                                           listgil[i].rod.Trim(),
                                           dat_rog.Trim(),
                                           dat_prib.Trim(),
                                           dat_vip.Trim()
                                         );

                                        break;
                                    }
                            }
                        }
                        //шапка отчета
                        else
                        {
                            rep.SetParameterValue("gil_fam", listgil[i].fam.Trim());
                            rep.SetParameterValue("gil_ima", listgil[i].ima.Trim());
                            rep.SetParameterValue("gil_otch", listgil[i].otch.Trim());
                            if (listgil[i].dat_rog.Trim() != "")
                            {
                                rep.SetParameterValue("b_date", System.Convert.ToDateTime(listgil[i].dat_rog).ToShortDateString());
                            }
                            //проверка смерть
                            //if (listgil[i].cel == "умер")                                
                            //{  
                            if (listgil[i].dat_ofor.Trim() != "")
                            {
                                rep.SetParameterValue("dat_smert", System.Convert.ToDateTime(listgil[i].dat_ofor).ToShortDateString());
                            }
                            //}
                            // else
                            // {
                            //rep.SetParameterValue("dat_smert", "НЕТ ДАННЫХ");
                            // }
                            //отличные параметры в шапке
                            switch (vidSprav)
                            {
                                case 5:
                                    {
                                        string dat_prib_1 = "";
                                        if (listgil[i].dat_prib.Trim() != "")
                                        {
                                            dat_prib_1 = System.Convert.ToDateTime(listgil[i].dat_prib).ToShortDateString();
                                        }
                                        rep.SetParameterValue("dat_prib", dat_prib_1);
                                        break;
                                    }
                            }
                        }
                    }
                    //дата
                    rep.SetParameterValue("get_date", date.Trim());
                    //паспортист,начальник
                    //Gilec us = new Gilec();
                    //us.nzp_user = CurT_nzp_user;
                    //us.nzp_kvar = CurT_nzp_kvar;
                    //us.pref = CurT_pref;

                    //string[] NachInfo = DbGilec.GetPasportistInformation(us, out ret); //cli.GetPasportistInformation(us, out ret);
                    string[] NachInfo = psp;
                    rep.SetParameterValue("fim_pasportist", NachInfo[0].Trim());
                    rep.SetParameterValue("dolgnost_pasport", NachInfo[1].Trim());
                    rep.SetParameterValue("dolgnost_nach", NachInfo[2].Trim());
                    rep.SetParameterValue("fim_nachPus", NachInfo[3].Trim());

                    #region Адрес
                    SetKvarAdres(rep);
                    rep.SetParameterValue("town", listgil[0].town_.Trim());
                    rep.SetParameterValue("rajon", listgil[0].rajon_.Trim());
                    #endregion
                }
            }

            rep.RegisterData(FDataSet);
            rep.GetDataSource("Q_master").Enabled = true;
            #endregion

            #region Зашифровать отчет в строку
            MemoryStream ms = new MemoryStream();
            rep.Prepare();
            rep.SavePrepared(ms);
            ms.Position = 0;

            return System.Convert.ToBase64String(ms.ToArray());
            #endregion
        }



        #region ПЕРЕНОС
        //Выбор процедуры отчета
        public string SelectProc(Report Report, string reportFile, Int32 ye_, Int32 mo_, int nzp_user1,
            int CurT_nzp_kvar, string CurT_pref, int CurT_nzp_Kart, int CurT_nzp_user, bool CurT_Kart_Archive,
            List<_RolesVal> RolesVal, Email email, out Returns res) {
            //временно
            res = Utils.InitReturns();

            #region Глобальные переменные
            this.nzp_user = nzp_user1;
            this.CurT_nzp_kvar = CurT_nzp_kvar;
            this.CurT_pref = CurT_pref;
            this.CurT_nzp_Kart = CurT_nzp_Kart;
            this.CurT_nzp_user = CurT_nzp_user;
            this.CurT_Kart_Archive = CurT_Kart_Archive;
            this.RolesVal = RolesVal;
            #endregion

            string date = DateTime.Now.Date.ToShortDateString();

            if (reportFile == "~/App_Data/Web_fin_ls.frx")
            {
                return Fill_web_fin_ls(Report, ye_, mo_);
            }
            if (reportFile == "~/App_Data/Web_s_nodolg.frx")
            {
                return Fill_web_s_nodolg(Report, ye_, mo_);
            }
            if (reportFile == "~/App_Data/Web_sparv_nach.frx")
            {
                return Fill_web_sparv_nach(Report, ye_, mo_);
            }
            //if (reportFile == "~/App_Data/Web_sost_fam.frx")
            //{
            //    return Fill_web_sost_fam(Report, 0);
            //}

            if (reportFile == "~/App_Data/Web_vip_dom.frx")
            {
                return Fill_web_vip_dom(Report, ye_, mo_);
            }
            //if (WebSprav.ReportFile == "~/App_Data/web_avia.frx") MakeKvit(WebSprav.Report, ye_, mo_, "web_avia.frx");
            //if (WebSprav.ReportFile == "~/App_Data/web_zel.frx") MakeKvit(WebSprav.Report, ye_, mo_, "web_zel.frx");
            //if (reportFile == "~/App_Data/Web_saldo_rep5_10.frx")
            //{
            //    return Fill_web_saldo_rep5_10(Report, ye_, mo_);                
            //}
            if (reportFile == "~/App_Data/Web_saldo_rep5_20.frx")
            {
                return Fill_web_saldo_rep5_20(Report, ye_, mo_);
            }

            //новые отчеты            
            if (reportFile == "~/App_Data/Web_sprav_bilZareg_f_5.frx")
            {
                return Fill_web_sprav_samara(Report, ye_, mo_, date, 5);
            }

            if (reportFile == "~/App_Data/Web_spravOsostaveSem_f_2.frx")
            {
                return Fill_web_sprav_samara2(Report, ye_, mo_, date, 7);
            }
            if (reportFile == "~/App_Data/Web_sprav_v_Sudi.frx")
            {
                return Fill_web_sprav_samara2(Report, ye_, mo_, date, 8);
            }
            if (reportFile == "~/App_Data/Web_sprav_Privatizaciya.frx")
            {
                return Fill_web_sprav_samara2(Report, ye_, mo_, date, 9);
            }
            if (reportFile == "~/App_Data/Web_sprav_Propiska.frx")
            {
                return Fill_web_sprav_samara2(Report, ye_, mo_, date, 11);
            }

            if (reportFile == "~/App_Data/Web_licSchet.frx")
            {
                return Fill_licShet(Report, ye_, mo_, DateTime.Now, out res);
            }
            if (reportFile == "~/App_Data/Web_licSchetwd.frx")
            {
                return Fill_licShet(Report, ye_, mo_, DateTime.Now, out res);
            }

            //--------------Заявление о регистрации по месту пребывания-------------
            if (reportFile == "~/App_Data/Web_Zay_reg_git.frx")
            {
                return Reg_Po_MestuGilec(Report, ye_, mo_, DateTime.Now);
            }
            if (reportFile == "~/App_Data/Web_Zay_reg_git_f6_1.frx")
            {
                return Reg_Po_MestuGilec(Report, ye_, mo_, DateTime.Now);
            }
            if (reportFile == "~/App_Data/Web_Zay_reg_git_f6_2.frx")
            {
                return Reg_Po_MestuGilec(Report, ye_, mo_, DateTime.Now);
            }
            ////----------------------------------------------------------------------
            if (reportFile == "~/App_Data/Web_pasp_fils.frx")
            {
                return Fill_web_sprav_samara2(Report, ye_, mo_, date, 10);
            }
            if (reportFile == "~/App_Data/web_listok_ubit.frx")
            {
                return Fill_web_listok_ubit(Report, ye_, mo_, DateTime.Now);
            }

            //-----------------------отчет по нарядам-заказам-------------------------
            if (reportFile == "~/App_Data/zakaz.frx")
            {
                return fill_zakaz_report(Report, table_name, mo_, email);
            }
            //------------------------------------------------------------------------

            if (reportFile == "~/App_Data/web_listok_pribit.frx")
            {
                //  return Fill_web_listok_pribit(Report, ye_, mo_, DateTime.Now);
            }
            //---------------------сведения о должниках-----------------------------
            if (reportFile == "~/App_Data/tula_5.frx")
            {
                return Fill_web_sve_dolzhnik(ye_, mo_, out res);
            }
            MonitorLog.WriteLog("Выбранный отчет не обработан кодом", MonitorLog.typelog.Error, true);
            return null;
        }

        //-------------------Остальные----------------------------------
        public string Fill_web_fin_ls(Report rep, int y_, int m_) {
            #region Сумма начисленная за месяц
            rep.SetParameterValue("sum_real", 0);
            int y1 = y_ - 2000;

            Returns ret = Utils.InitReturns();

            ChargeFind finder = new ChargeFind();
            finder.nzp_user = nzp_user;
            finder.nzp_kvar = CurT_nzp_kvar;
            finder.pref = CurT_pref;
            finder.YM.year_ = y_;
            finder.YM.month_ = m_;

            List<Charge> charges = null;


            cli_Charge cli = new cli_Charge(WCFParams.AdresWcfWeb.CurT_Server);
            charges = cli.GetCharge(finder, enSrvOper.SrvGetBillCharge, out ret);

            decimal sum_real = 0;

            if (charges != null & ret.result)
            {
                if (charges.Count > 0)
                {

                    for (int i = 0; i < charges.Count; i++)
                    {
                        sum_real = sum_real + charges[i].sum_real;
                    }

                    rep.SetParameterValue("sum_real", sum_real);
                }


            }
            #endregion

            #region Параметры
            SetKvarPrms(rep, y_, m_);
            #endregion

            #region Количество жильцов
            SetKolGil(rep, y_, m_);
            #endregion

            #region Адрес
            SetKvarAdres(rep);
            #endregion

            #region Зашифровать отчет в строку
            MemoryStream ms = new MemoryStream();
            try
            {
                rep.Prepare();
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка обработки отчета: " + ex.Message, MonitorLog.typelog.Error, true);
            }
            rep.SavePrepared(ms);
            ms.Position = 0;

            //return System.Text.Encoding.Default.GetString(ms.ToArray());
            return System.Convert.ToBase64String(ms.ToArray());
            #endregion

        }

        public string Fill_web_sve_dolzhnik(int y_, int m_, out Returns ret)
        {
            #region Выборка начислений
            var finder = new ChargeFind();
          
            string fileName = string.Empty, infoFile = string.Empty;
            var ms = new MemoryStream { Position = 0 };

            finder.nzp_user = CurT_nzp_user;
            finder.YM.year_ = y_;
            finder.YM.month_ = m_;

            var cli = new cli_Debitor(WCFParams.AdresWcfWeb.CurT_Server);
            string charges = cli.GetDebtorList(finder, out ret) ?? string.Empty;
            if (ret.result && charges != "")
            {
                fileName = Path.Combine(Constants.FilesDir, charges);
                if (InputOutput.useFtp)
                    if (!InputOutput.DownloadFile(charges, fileName)) throw new Exception("Не удалось загрузить файл с сервера");

                var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read) { Position = 0 };
                while (fs.Position <= fs.Length - 1)
                {
                    ms.WriteByte((byte) fs.ReadByte());
                }
                infoFile = Convert.ToBase64String(ms.ToArray());
                fs.Close();
                ms.Close();
            }
            #endregion
            return infoFile; 
        }

        public string Fill_web_sparv_nach(Report rep, int y_, int m_) {
            #region Запонение таблицы для отчета
            DataSet FDataSet = new DataSet();

            DataTable table = new DataTable();
            table.TableName = "Q_master";
            FDataSet.Tables.Add(table);

            table.Columns.Add("month_", typeof(int));
            table.Columns.Add("year_", typeof(int));
            table.Columns.Add("sum_insaldo", typeof(decimal));
            table.Columns.Add("sum_tarif", typeof(decimal));
            table.Columns.Add("sum_real", typeof(decimal));
            table.Columns.Add("reval", typeof(decimal));
            table.Columns.Add("sum_money", typeof(decimal));
            table.Columns.Add("real_charge", typeof(decimal));
            table.Columns.Add("sum_outsaldo", typeof(decimal));
            table.Columns.Add("sum_charge", typeof(decimal));

            #endregion


            #region Выборка начислений
            ChargeFind finder = new ChargeFind();
            Returns ret = Utils.InitReturns();

            finder.nzp_user = CurT_nzp_user;
            finder.nzp_kvar = CurT_nzp_kvar;
            finder.pref = CurT_pref;
            finder.groupby = "," + Constants.act_groupby_month;
            finder.find_from_the_start = 1;
            finder.YM.year_ = y_;
            finder.YM.month_ = 1;
            finder.month_po = 12;
            finder.is_show_reval = 0;
            finder.RolesVal = RolesVal;

            rep.SetParameterValue("y1", y_);
            rep.SetParameterValue("m1", 01);
            rep.SetParameterValue("y2", y_);
            rep.SetParameterValue("m2", 12);

            string old_month = "";
            decimal sum_insaldo = 0;
            decimal sum_tarif = 0;
            decimal sum_real = 0;
            decimal reval = 0;
            decimal sum_money = 0;
            decimal real_charge = 0;
            decimal sum_outsaldo = 0;
            decimal sum_charge = 0;


            cli_Charge cli = new cli_Charge(WCFParams.AdresWcfWeb.CurT_Server);
            List<Charge> charges;
            charges = cli.GetCharge(finder, enSrvOper.SrvGet, out ret);

            ret = Utils.InitReturns();
            if (charges == null) charges = new List<Charge>();

            List<Charge> newListCharges = new List<Charge>();
            for (var i = 0; i < charges.Count; i++)
            {
                Charge ch = charges[i];
                if (ch.dat_month != old_month)
                {
                    #region Суммы
                    if (old_month != "")
                    {
                        table.Rows.Add(System.Convert.ToDateTime(old_month).Month,
                            System.Convert.ToDateTime(old_month).Year,
                            sum_insaldo, sum_tarif, sum_real, reval, sum_money,
                            real_charge, sum_outsaldo, sum_charge);

                    }
                    sum_insaldo = 0;
                    sum_tarif = 0;
                    sum_real = 0;
                    reval = 0;
                    sum_money = 0;
                    real_charge = 0;
                    sum_outsaldo = 0;
                    sum_charge = 0;
                    old_month = ch.dat_month;

                    #endregion
                }

                sum_insaldo = sum_insaldo + ch.sum_insaldo;
                sum_tarif = sum_tarif + ch.sum_tarif;
                sum_real = sum_real + ch.sum_real;
                reval = reval + ch.reval;
                sum_money = sum_money + ch.sum_money;
                real_charge = real_charge + ch.real_charge;
                sum_outsaldo = sum_outsaldo + ch.sum_outsaldo;
                sum_charge = sum_charge + ch.sum_charge;

            }

            if (old_month != "")
                table.Rows.Add(System.Convert.ToDateTime(old_month).Month,
                              System.Convert.ToDateTime(old_month).Year,
                              sum_insaldo, sum_tarif, sum_real, reval, sum_money,
                              real_charge, sum_outsaldo, sum_charge);

            #endregion

            rep.RegisterData(FDataSet);
            rep.GetDataSource("Q_master").Enabled = true;
            #region Адрес
            SetKvarAdres(rep);
            #endregion

            #region Зашифровать отчет в строку
            MemoryStream ms = new MemoryStream();
            rep.Prepare();
            rep.SavePrepared(ms);
            ms.Position = 0;
            return System.Convert.ToBase64String(ms.ToArray());
            //return System.Text.Encoding.Default.GetString(ms.ToArray());
            #endregion


        }

        public string Fill_web_s_nodolg(Report rep, int y_, int m_) {
            #region Сумма долга
            rep.SetParameterValue("sum_dolg", 0);

            Returns ret = Utils.InitReturns();

            ChargeFind finder = new ChargeFind();
            finder.nzp_user = nzp_user;
            finder.nzp_kvar = CurT_nzp_kvar;
            finder.pref = CurT_pref;
            finder.YM.year_ = y_;
            finder.YM.month_ = m_;

            List<Charge> charges = null;


            cli_Charge cli = new cli_Charge(WCFParams.AdresWcfWeb.CurT_Server);
            charges = cli.GetCharge(finder, enSrvOper.SrvGetBillCharge, out ret);

            decimal sum_dolg = 0;

            if (charges != null & ret.result)
            {
                if (charges.Count > 0)
                {

                    for (int i = 0; i < charges.Count; i++)
                    {
                        sum_dolg = sum_dolg + charges[i].sum_outsaldo - charges[i].sum_real;
                    }

                    rep.SetParameterValue("sum_dolg", sum_dolg);
                }


            }
            if (sum_dolg <= 0)
                rep.SetParameterValue("ne", "не имеет задолженности");
            else
                rep.SetParameterValue("ne", "имеет задолжность в размере " + sum_dolg.ToString() + " рублей");


            #endregion

            #region Адрес
            SetKvarAdres(rep);
            #endregion

            #region Зашифровать отчет в строку
            MemoryStream ms = new MemoryStream();
            rep.Prepare();
            rep.SavePrepared(ms);
            ms.Position = 0;
            return System.Convert.ToBase64String(ms.ToArray());
            //return System.Text.Encoding.Default.GetString(ms.ToArray());
            #endregion

        }

        public bool Fill_web_sost_fam(Report rep, int header, string place, string reason, List<GilecFullInf> karts) {
            Returns ret = Utils.InitReturns();

            #region Запонение таблицы для отчета
            DataSet FDataSet = new DataSet();

            DataTable table = new DataTable();
            table.TableName = "Q_master";
            FDataSet.Tables.Add(table);

            table.Columns.Add("fio", typeof(string));
            table.Columns.Add("dat_rog", typeof(string));
            table.Columns.Add("rod", typeof(string));
            table.Columns.Add("arrival", typeof(string));
            table.Columns.Add("date_gil", typeof(string));
            table.Columns.Add("date_arrival", typeof(string));
            table.Columns.Add("date_uchet", typeof(string));
            table.Columns.Add("remark", typeof(string));

            #endregion

            rep.SetParameterValue("title", "");
            rep.SetParameterValue("address", "");
            rep.SetParameterValue("citizen", "");
            rep.SetParameterValue("property", "");
            rep.SetParameterValue("s_ob", "");
            rep.SetParameterValue("s_gil", "");
            rep.SetParameterValue("sobstw", "");
            rep.SetParameterValue("resp", "");
            rep.SetParameterValue("dok", "");
            rep.SetParameterValue("place", "");
            rep.SetParameterValue("user", "");
            rep.SetParameterValue("dat", "");
            rep.SetParameterValue("reason", "");
            rep.SetParameterValue("area", "");

            #region Выборка данных

            Gilec finder = new Gilec();
            finder.nzp_user = nzp_user;
            finder.nzp_kvar = CurT_nzp_kvar;
            //finder.num_ls = CurT_nzp_kvar;
            finder.pref = CurT_pref;
            finder.header = header;

            cli_Gilec cli = new cli_Gilec(WCFParams.AdresWcfWeb.CurT_Server);
            List<GilecFullInf> listgil = null;
            listgil = cli.CallFullInfGilList(finder, out ret);
            //cli.Fill_web_sost_fam(rep, y_, m_, out ret, finder);




            if (listgil != null & ret.result)
            {
                if (listgil.Count > 0)
                {
                    foreach (var kart in karts)
                    {
                        if (listgil.Exists(x => x.nzp_kart == kart.nzp_kart))
                        {
                            listgil.First(x => x.nzp_kart == kart.nzp_kart).fam = kart.fam;
                            listgil.First(x => x.nzp_kart == kart.nzp_kart).ima = kart.ima;
                            listgil.First(x => x.nzp_kart == kart.nzp_kart).otch = kart.otch;
                            listgil.First(x => x.nzp_kart == kart.nzp_kart).rod = kart.rod;
                            listgil.First(x => x.nzp_kart == kart.nzp_kart).dat_prib = kart.dat_prib;
                            listgil.First(x => x.nzp_kart == kart.nzp_kart).dat_ofor = kart.dat_ofor;
                            listgil.First(x => x.nzp_kart == kart.nzp_kart).dat_prop = kart.dat_prop;
                            listgil.First(x => x.nzp_kart == kart.nzp_kart).dat_oprp = kart.dat_oprp;
                            listgil.First(x => x.nzp_kart == kart.nzp_kart).remark = kart.remark;
                        }
                    }

                    for (int i = 0; i < listgil.Count; i++)
                    {
                        table.Rows.Add(listgil[i].fam + " " +
                                        listgil[i].ima + " " +
                                        listgil[i].otch,
                            listgil[i].dat_rog,
                            listgil[i].rod,
                            listgil[i].dat_prib,
                            listgil[i].dat_ofor,
                            listgil[i].dat_prop,
                            listgil[i].dat_oprp,
                            listgil[i].remark);
                        if (listgil[i].nzp_kart == CurT_nzp_Kart)
                        //(listgil[i].rod.ToUpper().Trim() == "НАНИМАТЕЛЬ")
                        {
                            rep.SetParameterValue("isAlwaysLiving", false);
                            switch (header)
                            {
                                case 0:
                                    rep.SetParameterValue("title",
                                        "СПРАВКА О ЗАРЕГИСТРИРОВАННЫХ (ПОСТОЯННО ПРОЖИВАЮЩИХ)");
                                    rep.SetParameterValue("isAlwaysLiving", true);
                                    break;
                                case 1:
                                    rep.SetParameterValue("title",
                                        "СПРАВКА О ЗАРЕГИСТРИРОВАННЫХ (ПОСТОЯННО ПРОЖИВАЮЩИХ И ВРЕМЕННО ПРИБЫВШИХ)");
                                    break;
                                case 2:
                                    rep.SetParameterValue("title",
                                        "СПРАВКА О ЗАРЕГИСТРИРОВАННЫХ (ПОСТОЯННО ПРОЖИВАЮЩИХ) АРХИВНАЯ");
                                    break;
                            }
                            rep.SetParameterValue("address", listgil[i].address);
                            rep.SetParameterValue("citizen",
                                listgil[i].fam + " " + listgil[i].ima + " " + listgil[i].otch);
                            rep.SetParameterValue("property", listgil[i].property);
                            rep.SetParameterValue("s_ob", listgil[i].s_ob);
                            rep.SetParameterValue("s_gil", listgil[i].s_gil);
                            rep.SetParameterValue("sobstw", listgil[i].sobstw);
                            rep.SetParameterValue("resp", listgil[i].fio);
                            rep.SetParameterValue("dok", listgil[i].dok_sv);
                            rep.SetParameterValue("place", place);
                            try
                            {
                                IDbConnection conn_web = DBManager.GetConnection(Constants.cons_Webdata);
                                conn_web.Open();
                                DBManager.ExecSQL(conn_web, " set encryption password '" + DBManager.BasePwd + "'",
                                    false);
                                var uname = DBManager.ExecSQLToTable(conn_web,
                                    " select decrypt_char(uname) as uname from " +
                                    DBManager.GetFullBaseName(conn_web) + DBManager.tableDelimiter + "users" +
                                    " where nzp_user = " + nzp_user);
                                rep.SetParameterValue("user",
                                    uname.Rows.Count == 1
                                        ? Utils.GetCorrectFIO(uname.Rows[0]["uname"].ToString().Trim())
                                        : "");
                                conn_web.Close();
                            }
                            catch (Exception)
                            {
                                rep.SetParameterValue("user", "");
                            }
                            rep.SetParameterValue("dat", DateTime.Now.ToLongDateString());
                            rep.SetParameterValue("reason", reason);
                            rep.SetParameterValue("area", listgil[i].area);
                        }

                    }
                }
            }

            #endregion

            rep.RegisterData(FDataSet);
            rep.GetDataSource("Q_master").Enabled = true;


            SetKvarAdres(rep);
            return rep.Prepare();
        }

		public string Fill_web_vip_dom(Report rep, int y, int m) {

			#region Запонение таблицы для отчета
			var fDataSet = new DataSet();

			var table = new DataTable { TableName = "Q_master" };
			fDataSet.Tables.Add(table);

			table.Columns.Add("rod", typeof(string));
			table.Columns.Add("fam", typeof(string));
			table.Columns.Add("ima", typeof(string));
			table.Columns.Add("otch", typeof(string));
			table.Columns.Add("dolya", typeof(string));
			//table.Columns.Add("dolyaDown", typeof(string));
			table.Columns.Add("dat_rog", typeof(string));
			table.Columns.Add("landop", typeof(string));
			table.Columns.Add("statop", typeof(string));
			table.Columns.Add("twnop", typeof(string));
			table.Columns.Add("rajonop", typeof(string));
			table.Columns.Add("rem_op", typeof(string));
			table.Columns.Add("dok", typeof(string));
			table.Columns.Add("serij", typeof(string));
			table.Columns.Add("nomer", typeof(string));
			table.Columns.Add("vid_dat", typeof(string));
			table.Columns.Add("vid_mes", typeof(string));
			table.Columns.Add("dat_prop", typeof(string));
			table.Columns.Add("mKu", typeof(string));
			table.Columns.Add("datUbit", typeof(string));

			#endregion

			#region Выборка данных
			Returns ret;

			var finder = new Gilec { nzp_user = nzp_user, nzp_kvar = CurT_nzp_kvar, pref = CurT_pref, header = 2 };
			var finderSobstw = new Sobstw { nzp_user = nzp_user, nzp_kvar = CurT_nzp_kvar, pref = CurT_pref };

			var cli = new cli_Gilec(WCFParams.AdresWcfWeb.CurT_Server);
			List<GilecFullInf> listgil = cli.CallFullInfGilList(finder, out ret);
            cli = new cli_Gilec(WCFParams.AdresWcfWeb.CurT_Server);
			List<Sobstw> listSobstw = cli.FindSobstw(finderSobstw, out ret) ?? new List<Sobstw>();

			var finderLs = new Ls { nzp_user = nzp_user, nzp_kvar = CurT_nzp_kvar, pref = CurT_pref };

			var cliAdres = new cli_AdresHard(WCFParams.AdresWcfWeb.CurT_Server);
			List<Ls> listLs = cliAdres.LoadLs(finderLs, out ret);

			rep.SetParameterValue("date", DateTime.Now.ToShortDateString());

			rep.SetParameterValue("address", listLs[0].town +
											 (listLs[0].ulica != "-" && !string.IsNullOrEmpty(listLs[0].ulica) ? ", " + listLs[0].ulica : string.Empty) +
											 (listLs[0].ndom != "-" && !string.IsNullOrEmpty(listLs[0].ndom) ? ", д." + listLs[0].ndom : string.Empty) +
											 (listLs[0].nkor != "-" && !string.IsNullOrEmpty(listLs[0].nkor) ? ", кор." + listLs[0].nkor : string.Empty) +
											 (listLs[0].nkvar != "-" && !string.IsNullOrEmpty(listLs[0].nkvar) ? ", кв. " + listLs[0].nkvar : string.Empty));

			string mKu = string.Empty;
            string datUbit = string.Empty;
			if (listgil != null & ret.result)
			{
				if (listgil.Count > 0)
				{
					foreach (GilecFullInf gil in listgil)
					{
					    if (gil.nzp_tkrt != "1")
					    {
					        mKu = gil.landku + " " + gil.statku + " " +
					              gil.twnku + " " + gil.rajonku + " " +
					              gil.rem_ku;
					        datUbit = gil.dat_ofor.Trim() != string.Empty
                                ? Convert.ToDateTime(gil.dat_ofor).ToShortDateString()
					            : string.Empty;
					    }

                       
					    string dolya = string.Empty;
						if (listSobstw.Count > 0)
						{
							GilecFullInf localGil = gil;
                            Sobstw selectSobstw = listSobstw.Find(sobstw => sobstw.fam.ToUpper().Trim() == localGil.fam.ToUpper().Trim() &&
																		  sobstw.ima.ToUpper().Trim() == localGil.ima.ToUpper().Trim() &&
																		  sobstw.otch.ToUpper().Trim() == localGil.otch.ToUpper().Trim() &&
																		  sobstw.dat_rog == localGil.dat_rog);
						    dolya = selectSobstw.dolya;

						}

						string datRog = gil.dat_rog.Trim() != string.Empty
							? Convert.ToDateTime(gil.dat_rog).ToShortDateString()
							: string.Empty;

						string vidDat = gil.vid_dat.Trim() != string.Empty
							? Convert.ToDateTime(gil.vid_dat).ToShortDateString()
							: string.Empty;

						table.Rows.Add(gil.rod,
									   gil.fam, gil.ima, gil.otch, dolya,
									   datRog,
									   gil.landop, gil.statop, gil.twnop, gil.rajonop, gil.rem_op,
									   gil.dok, gil.serij, gil.nomer, vidDat, gil.vid_mes,
									   gil.dat_prop,
									   datUbit, mKu);
					}
				}
			}

			//паспортист,начальник
			var us = new Gilec { nzp_user = CurT_nzp_user, nzp_kvar = CurT_nzp_kvar, pref = CurT_pref };

			cli = new cli_Gilec(WCFParams.AdresWcfWeb.CurT_Server);
			string[] nachInfo = cli.GetPasportistInformation(us, out ret);
			if (nachInfo != null)
			{
				rep.SetParameterValue("fim_pasportist", (nachInfo[0].Trim() != "-" ? nachInfo[0].Trim() : string.Empty));
				rep.SetParameterValue("dolgnost_pasport", (nachInfo[1].Trim() != "-" ? nachInfo[1].Trim() : string.Empty));
				rep.SetParameterValue("dolgnost_nach", (nachInfo[2].Trim() != "-" ? nachInfo[2].Trim() : string.Empty));
				rep.SetParameterValue("fim_nachPus", (nachInfo[3].Trim() != "-" ? nachInfo[3].Trim() : string.Empty));
			}

			rep.RegisterData(fDataSet);
			rep.GetDataSource("Q_master").Enabled = true;

			#endregion

			#region Зашифровать отчет в строку
			var ms = new MemoryStream();
			rep.Prepare();
			rep.SavePrepared(ms);
			ms.Position = 0;
			return Convert.ToBase64String(ms.ToArray());
			//return System.Text.Encoding.Default.GetString(ms.ToArray());
			#endregion
		}



        public string Fill_web_protokolODN(Report rep, int y_, int m_, int nzp_serv) {
            return "";
            //#region Запонение таблицы для отчета
            //DataSet FDataSet = new DataSet();

            //DataTable table = new DataTable();
            //table.TableName = "Q_master";
            //FDataSet.Tables.Add(table);

            //table.Columns.Add("num_ls", typeof(string));
            //table.Columns.Add("fio", typeof(string));
            //table.Columns.Add("count_gil", typeof(string));
            //table.Columns.Add("count_room", typeof(string));
            //table.Columns.Add("norma", typeof(string));
            //table.Columns.Add("ipu", typeof(decimal));
            //table.Columns.Add("wipu", typeof(decimal));
            //table.Columns.Add("c_calc", typeof(decimal));

            //#endregion

            //#region Выборка данных
            //Returns ret = Utils.InitReturns();

            //ChargeFind finder = new ChargeFind();
            //finder.nzp_user = nzp_user;
            //finder.pref = CurT_pref;
            //finder.RolesVal = RolesVal;
            //finder.YM.month_ = m_;
            //finder.YM.year_ = y_;
            //finder.nzp_dom = CurT_nzp_dom;

            //List<SaldoRep> listRepData = null;

            //cli_Charge cli = new cli_Charge();
            ////новая процедура
            //listRepData = cli.FillRep(finder, out ret, 11);


            ////готовый список для вывода
            //for (int i = 0; i < listRepData.Count; i++)
            //{
            //    table.Rows.Add(listRepData[i].num_ls, listRepData[i].fio,
            //        listRepData[i].dopParams[0].val_prm, listRepData[i].dopParams[0].val_prm,
            //        listRepData[i].dopParams[0].val_prm, listRepData[i].pm_x,
            //        listRepData[i].pm_y, listRepData[i].c_calc);
            //}

            //rep.RegisterData(FDataSet);
            //rep.GetDataSource("Q_master").Enabled = true;

            //rep.SetParameterValue("months", "01."+finder.month_+"."+finder.year_);


            //Ls finderls = new Ls();
            //finderls.nzp_user = nzp_user;//web_user.nzp_user;
            //finderls.nzp_kvar = CurT_nzp_kvar;
            //finderls.pref = CurT_pref;

            //cli_Adres cli = new cli_Adres();
            //List<Ls> LsInfo = cli.GetLs(finderls, enSrvOper.SrvLoad, out ret);

            //string adr = "";

            //if (ret.result && LsInfo != null && LsInfo.Count > 0)
            //{
            //    adr = "";
            //    ulica = LsInfo[0].ulica.Trim();

            //    ndom = LsInfo[0].ndom.Trim();
            //    adr += ", д." + ndom;
            //    if (LsInfo[0].nkor.Trim() != "-" && LsInfo[0].nkor != "") adr += " корп. " + LsInfo[0].nkor.Trim();
            //}


            //rep.SetParameterValue("adr_dom", adr);
            //rep.SetParameterValue("service", finder.service);

            //#endregion

            //#region Зашифровать отчет в строку
            //MemoryStream ms = new MemoryStream();
            //rep.Prepare();
            //rep.SavePrepared(ms);
            //ms.Position = 0;
            //return System.Convert.ToBase64String(ms.ToArray());
            ////return System.Text.Encoding.Default.GetString(ms.ToArray());
            //#endregion

        }

        public string Fill_web_saldo_rep5_20(Report rep, int y_, int m_) {
            #region Заполнение таблицы для отчета
            DataSet FDataSet = new DataSet();

            DataTable table = new DataTable();
            table.TableName = "Q_master";
            FDataSet.Tables.Add(table);

            table.Columns.Add("adres", typeof(string));
            table.Columns.Add("sum_insaldo", typeof(decimal));
            table.Columns.Add("sum_real", typeof(decimal));
            table.Columns.Add("sum_money", typeof(decimal));
            table.Columns.Add("reval", typeof(decimal));
            table.Columns.Add("sum_outsaldo", typeof(decimal));
            #endregion

            #region Выборка данных
            ChargeFind finder = new ChargeFind();
            finder.nzp_user = nzp_user;
            finder.nzp_kvar = CurT_nzp_kvar;
            finder.pref = CurT_pref;
            finder.RolesVal = RolesVal;
            finder.YM.month_ = m_;
            finder.YM.year_ = y_;

            cli_Charge cli = new cli_Charge(WCFParams.AdresWcfWeb.CurT_Server);
            //Returns ret = cli.PrepareReport(finder, 1); // Подготовка данных для отчета
            Returns ret = cli.PrepareReport(finder, 2); // Подготовка данных для отчета

            List<SaldoRep> listRepData = null;

            if (ret.result)
            {
                DbChargeClient db = new DbChargeClient();
                listRepData = db.GetPreparedReport5_20(finder, out ret); // Получение подготовленных данных для отчета
                //тест другой процедуры
                //listRepData = db.FillRep(finder, out ret, 2);
                db.Close();
            }

            if (listRepData != null && ret.result)
                foreach (SaldoRep s in listRepData)
                    table.Rows.Add(s.adr, s.sum_insaldo, s.sum_real, s.sum_money, s.reval, s.sum_outsaldo);

            rep.RegisterData(FDataSet);
            rep.GetDataSource("Q_master").Enabled = true;
            #endregion

            #region Зашифровать отчет в строку
            MemoryStream ms = new MemoryStream();
            rep.Prepare();
            rep.SavePrepared(ms);
            ms.Position = 0;
            return System.Convert.ToBase64String(ms.ToArray());
            //return System.Text.Encoding.Default.GetString(ms.ToArray());
            #endregion
        }

        //--------Самара------------------------------
        public string Fill_web_sprav_samara(Report rep, int y_, int m_, string date, int vidSprav) {

            #region Запонение таблицы для отчета

            DataSet FDataSet = new DataSet();

            DataTable table = new DataTable();
            table.TableName = "Q_master";
            FDataSet.Tables.Add(table);

            table.Columns.Add("fam", typeof(string));
            table.Columns.Add("ima", typeof(string));
            table.Columns.Add("otch", typeof(string));


            //выбор параметров для конкретной справки
            switch (vidSprav)
            {
                case 5:
                    {
                        table.Columns.Add("rod", typeof(string));
                        table.Columns.Add("dat_rog", typeof(string));
                        table.Columns.Add("dat_prib", typeof(string));
                        table.Columns.Add("dat_vip", typeof(string));
                        break;
                    }
            }
            #endregion

            #region Выборка данных
            Returns ret = Utils.InitReturns();

            Gilec finder = new Gilec();

            finder.nzp_user = nzp_user;//web_user.nzp_user;
            finder.nzp_kvar = CurT_nzp_kvar;
            finder.nzp_kart = CurT_nzp_Kart;
            finder.pref = CurT_pref;


            List<GilecFullInf> listgil = null;

            cli_Gilec cli = new cli_Gilec(WCFParams.AdresWcfWeb.CurT_Server);
            listgil = cli.GetFullInfGilList_AllKards(finder, out ret);




            if (listgil != null & ret.result)
            {
                if (listgil.Count > 0)
                {
                    for (int i = 0; i < listgil.Count; i++)
                    {
                        //для таблицы           
                        string dat_rog = "";
                        string dat_prib = "";
                        string dat_vip = "";

                        if (listgil[i].dat_rog.Trim() != "")
                        {
                            dat_rog = System.Convert.ToDateTime(listgil[i].dat_rog).ToShortDateString();
                        }

                        if (listgil[i].rod.ToUpper().Trim() == "НАНИМАТЕЛЬ")
                        {
                            rep.SetParameterValue("vl_fam", listgil[i].fam.Trim());
                            rep.SetParameterValue("vl_ima", listgil[i].ima.Trim());
                            rep.SetParameterValue("vl_otch", listgil[i].otch.Trim());
                        }


                        if (listgil[i].nzp_kart != CurT_nzp_Kart)
                        {
                            //выбор параметров для конкретной справки
                            switch (vidSprav)
                            {
                                //case 1:
                                case 5:
                                    {
                                        if (listgil[i].dat_prib.Trim() != "")
                                        {
                                            dat_prib = System.Convert.ToDateTime(listgil[i].dat_prib).ToShortDateString();
                                        }

                                        if (listgil[i].dat_vip.Trim() != "")
                                        {
                                            dat_vip = System.Convert.ToDateTime(listgil[i].dat_vip).ToShortDateString();
                                        }
                                        table.Rows.Add(
                                           listgil[i].fam.Trim(),
                                           listgil[i].ima.Trim(),
                                           listgil[i].otch.Trim(),
                                           listgil[i].rod.Trim(),
                                           dat_rog.Trim(),
                                           dat_prib.Trim(),
                                           dat_vip.Trim()
                                         );

                                        break;
                                    }
                            }
                        }
                        //шапка отчета
                        else
                        {
                            rep.SetParameterValue("gil_fam", listgil[i].fam.Trim());
                            rep.SetParameterValue("gil_ima", listgil[i].ima.Trim());
                            rep.SetParameterValue("gil_otch", listgil[i].otch.Trim());
                            if (listgil[i].dat_rog.Trim() != "")
                            {
                                rep.SetParameterValue("b_date", System.Convert.ToDateTime(listgil[i].dat_rog).ToShortDateString());
                            }
                            //проверка смерть
                            //if (listgil[i].cel == "умер")                                
                            //{  
                            if (listgil[i].dat_ofor.Trim() != "")
                            {
                                rep.SetParameterValue("dat_smert", System.Convert.ToDateTime(listgil[i].dat_ofor).ToShortDateString());
                            }
                            //}
                            // else
                            // {
                            //rep.SetParameterValue("dat_smert", "НЕТ ДАННЫХ");
                            // }
                            //отличные параметры в шапке
                            switch (vidSprav)
                            {
                                case 5:
                                    {
                                        string dat_prib_1 = "";
                                        if (listgil[i].dat_prib.Trim() != "")
                                        {
                                            dat_prib_1 = System.Convert.ToDateTime(listgil[i].dat_prib).ToShortDateString();
                                        }
                                        rep.SetParameterValue("dat_prib", dat_prib_1);
                                        break;
                                    }
                            }
                        }
                    }
                    //дата
                    rep.SetParameterValue("get_date", date);
                    //паспортист,начальник
                    Gilec us = new Gilec();
                    us.nzp_user = CurT_nzp_user;
                    us.nzp_kvar = CurT_nzp_kvar;
                    us.pref = CurT_pref;

                    string[] NachInfo = cli.GetPasportistInformation(us, out ret);

                    rep.SetParameterValue("fim_pasportist", NachInfo[0].Trim());
                    rep.SetParameterValue("dolgnost_pasport", NachInfo[1].Trim());
                    rep.SetParameterValue("dolgnost_nach", NachInfo[2].Trim());
                    rep.SetParameterValue("fim_nachPus", NachInfo[3].Trim());

                    #region Адрес
                    SetKvarAdres(rep);
                    rep.SetParameterValue("town", listgil[0].town_.Trim());
                    rep.SetParameterValue("rajon", listgil[0].rajon_.Trim());
                    #endregion
                }
            }

            rep.RegisterData(FDataSet);
            rep.GetDataSource("Q_master").Enabled = true;
            #endregion

            #region Зашифровать отчет в строку
            MemoryStream ms = new MemoryStream();
            rep.Prepare();
            rep.SavePrepared(ms);
            ms.Position = 0;

            return System.Convert.ToBase64String(ms.ToArray());
            #endregion

        }

        public string Fill_web_sprav_samara2(Report rep, int y_, int m_, string date, int vidSprav) {

            #region Запонение таблицы для отчета

            DataSet FDataSet = new DataSet();

            DataTable table = new DataTable();
            table.TableName = "Q_master";
            FDataSet.Tables.Add(table);

            table.Columns.Add("fam", typeof(string));
            table.Columns.Add("ima", typeof(string));
            table.Columns.Add("otch", typeof(string));
            table.Columns.Add("mesto_rog", typeof(string));


            //выбор параметров для конкретной справки
            switch (vidSprav)
            {
                //case 1:
                case 5:
                    {
                        table.Columns.Add("rod", typeof(string));
                        table.Columns.Add("dat_rog", typeof(string));
                        table.Columns.Add("dat_prib", typeof(string));
                        table.Columns.Add("dat_vip", typeof(string));
                        break;
                    }
                // case 2:
                // case 3:
                // case 4:
                //case 10:
                //    {
                //        table.Columns.Add("rod", typeof(string));
                //        table.Columns.Add("dat_rog", typeof(string));
                //        table.Columns.Add("dat_prib", typeof(string));
                //        break;
                //    }
                //case 6:
                //    {
                //        table.Columns.Add("rod", typeof(string));
                //        table.Columns.Add("dat_rog", typeof(string));
                //        break;
                //    }

                case 7:
                    {
                        table.Columns.Add("dat_rog", typeof(string));
                        table.Columns.Add("dat_prib", typeof(string));
                        break;
                    }
                case 8:
                    {
                        table.Columns.Add("rod", typeof(string));
                        table.Columns.Add("dat_rog", typeof(string));
                        table.Columns.Add("dat_prib", typeof(string));
                        table.Columns.Add("type_prop", typeof(string));
                        break;
                    }
                case 9:
                    {
                        table.Columns.Add("rod", typeof(string));
                        table.Columns.Add("dat_rog", typeof(string));
                        table.Columns.Add("dat_prib", typeof(string));
                        table.Columns.Add("type_prop1", typeof(string));
                        table.Columns.Add("dat_oprp", typeof(string));
                        break;
                    }
                case 10:
                    {
                        table.Columns.Remove("mesto_rog");
                        table.Columns.Add("rod", typeof(string));
                        table.Columns.Add("dat_prib", typeof(string));

                        break;
                    }

            }


            #endregion

            #region Выборка данных
            Returns ret = Utils.InitReturns();

            Gilec finder = new Gilec();
            finder.nzp_user = nzp_user;// web_user.nzp_user;
            finder.nzp_kvar = CurT_nzp_kvar;
            finder.pref = CurT_pref;
            finder.is_arx = CurT_Kart_Archive;

            List<GilecFullInf> listgil = null;

            cli_Gilec cli = new cli_Gilec(WCFParams.AdresWcfWeb.CurT_Server);
            listgil = cli.CallFullInfGilList(finder, out ret);
            //listgil = DbGilec.GetFullInfGilList(finder, out ret);

            bool keykart = false;
            string has_fam = "";
            if (listgil != null & ret.result)
            {
                if (listgil.Count > 0)
                {
                    for (int i = 0; i < listgil.Count; i++)
                    {

                        //для таблицы           
                        string dat_rog = "";
                        string dat_prib = "";
                        string dat_vip = "";
                        string dat_oprp = "";

                        if (listgil[i].dat_rog.Trim() != "")
                        {
                            dat_rog = System.Convert.ToDateTime(listgil[i].dat_rog).ToShortDateString();
                        }

                        if ((listgil[i].rod.ToUpper().Trim() == "НАНИМАТЕЛЬ") || (listgil[i].rod.ToUpper().Trim() == "СОБСТВЕННИК"))
                        {
                            rep.SetParameterValue("vl_fam", listgil[i].fam.Trim());
                            has_fam = listgil[i].fam;
                            rep.SetParameterValue("vl_ima", listgil[i].ima.Trim());
                            rep.SetParameterValue("vl_otch", listgil[i].otch.Trim());
                        }


                        if (listgil[i].nzp_kart != CurT_nzp_Kart)
                        {

                            //выбор параметров для конкретной справки
                            switch (vidSprav)
                            {
                                //case 1:
                                case 5:
                                    {
                                        if (listgil[i].dat_prib.Trim() != "")
                                        {
                                            dat_prib = System.Convert.ToDateTime(listgil[i].dat_prib).ToShortDateString();
                                        }

                                        if (listgil[i].dat_vip.Trim() != "")
                                        {
                                            dat_vip = System.Convert.ToDateTime(listgil[i].dat_vip).ToShortDateString();
                                        }
                                        table.Rows.Add(
                                           listgil[i].fam.Trim(),
                                           listgil[i].ima.Trim(),
                                           listgil[i].otch.Trim(),
                                           listgil[i].rem_mr.Trim(),
                                           listgil[i].rod.Trim(),
                                           dat_rog.Trim(),
                                           dat_prib.Trim(),
                                           dat_vip.Trim()
                                         );

                                        break;
                                    }
                                //case 2:
                                //case 3:
                                //case 4:
                                //case 10:
                                //    {
                                //        if (listgil[i].dat_prib.Trim() != "")
                                //        {
                                //            dat_prib = System.Convert.ToDateTime(listgil[i].dat_prib).ToShortDateString();
                                //        }
                                //        table.Rows.Add(
                                //           listgil[i].fam,
                                //           listgil[i].ima,
                                //           listgil[i].otch,
                                //           listgil[i].rod,
                                //           dat_rog,
                                //           dat_prib
                                //         );

                                //        break;
                                //    }
                                //case 6:
                                //    {
                                //        table.Rows.Add(
                                //       listgil[i].fam,
                                //       listgil[i].ima,
                                //       listgil[i].otch,
                                //       listgil[i].rod,
                                //       dat_rog
                                //     );
                                //        break;
                                //    }
                                case 7:
                                    {
                                        if (listgil[i].dat_prib.Trim() != "")
                                        {
                                            dat_prib = System.Convert.ToDateTime(listgil[i].dat_prib).ToShortDateString();
                                        }
                                        table.Rows.Add(
                                           listgil[i].fam.Trim(),
                                           listgil[i].ima.Trim(),
                                           listgil[i].otch.Trim(),
                                           listgil[i].rem_mr.Trim(),
                                           dat_rog.Trim(),
                                           dat_prib.Trim()
                                         );
                                        break;
                                    }
                                case 8:
                                    {
                                        if (listgil[i].dat_prib.Trim() != "")
                                        {
                                            dat_prib = System.Convert.ToDateTime(listgil[i].dat_prib).ToShortDateString();
                                        }
                                        table.Rows.Add(
                                           listgil[i].fam.Trim(),
                                           listgil[i].ima.Trim(),
                                           listgil[i].otch.Trim(),
                                           listgil[i].rem_mr.Trim(),
                                           listgil[i].rod.Trim(),
                                           dat_rog.Trim(),
                                           dat_prib.Trim(),
                                           listgil[i].type_prop.Trim()
                                         );
                                        break;
                                    }
                                case 9:
                                    {
                                        if (listgil[i].dat_prib.Trim() != "")
                                        {
                                            dat_prib = System.Convert.ToDateTime(listgil[i].dat_prib).ToShortDateString();
                                        }
                                        if (listgil[i].dat_oprp.Trim() != "")
                                        {
                                            dat_oprp = System.Convert.ToDateTime(listgil[i].dat_oprp).ToShortDateString();
                                        }
                                        table.Rows.Add(
                                           listgil[i].fam.Trim(),
                                           listgil[i].ima.Trim(),
                                           listgil[i].otch.Trim(),
                                           listgil[i].rem_mr.Trim(),
                                           listgil[i].rod.Trim(),
                                           dat_rog.Trim(),
                                           dat_prib.Trim(),
                                           listgil[i].type_prop.Trim(),
                                           dat_vip.Trim()
                                         );
                                        break;
                                    }

                                case 10:
                                    {
                                        if (listgil[i].dat_prib.Trim() != "")
                                        {
                                            dat_prib = System.Convert.ToDateTime(listgil[i].dat_prib).ToShortDateString();
                                        }
                                        table.Rows.Add(
                                          listgil[i].fam.Trim(),
                                          listgil[i].ima.Trim(),
                                          listgil[i].otch.Trim(),
                                          listgil[i].rod.Trim(),
                                          dat_prib
                                        );
                                        break;
                                    }

                            }
                        }
                        //шапка отчета
                        else
                        {
                            rep.SetParameterValue("text", "");
                            keykart = true;
                            rep.SetParameterValue("gil_fam", listgil[i].fam.Trim());
                            rep.SetParameterValue("gil_ima", listgil[i].ima.Trim());
                            rep.SetParameterValue("gil_otch", listgil[i].otch.Trim());
                            if (listgil[i].dat_rog.Trim() != "")
                            {
                                rep.SetParameterValue("b_date", System.Convert.ToDateTime(listgil[i].dat_rog).ToShortDateString());
                            }
                            //проверка смерть
                            //if (listgil[i].cel == "умер")                                
                            //{                              
                            //rep.SetParameterValue("dat_smert", System.Convert.ToDateTime(listgil[i].dat_ofor).ToShortDateString());
                            //}
                            // else
                            // {
                            //rep.SetParameterValue("dat_smert", "НЕТ ДАННЫХ");
                            // }
                            //отличные параметры в шапке
                            switch (vidSprav)
                            {
                                //case 3:
                                //    {
                                //        string dat_prib_1 = "";
                                //        string dat_vip_1 = "";
                                //        if (listgil[i].dat_prib.Trim() != "")
                                //        {
                                //            dat_prib_1 = System.Convert.ToDateTime(listgil[i].dat_prib).ToShortDateString();
                                //        }

                                //        if (listgil[i].dat_vip.Trim() != "")
                                //        {
                                //            dat_vip_1 = System.Convert.ToDateTime(listgil[i].dat_vip).ToShortDateString();
                                //        }
                                //        rep.SetParameterValue("dat_prib", dat_prib_1);
                                //        rep.SetParameterValue("dat_vip", dat_vip_1);
                                //        break;
                                //    }
                                //case 4:
                                case 5:
                                    {
                                        string dat_prib_1 = "";
                                        if (listgil[i].dat_prib.Trim() != "")
                                        {
                                            dat_prib_1 = System.Convert.ToDateTime(listgil[i].dat_prib).ToShortDateString();
                                        }
                                        rep.SetParameterValue("dat_prib", dat_prib_1);
                                        break;
                                    }
                                case 7:
                                    {
                                        string dat_prib_1 = "";
                                        if (listgil[i].dat_prib.Trim() != "")
                                        {
                                            dat_prib_1 = System.Convert.ToDateTime(listgil[i].dat_prib).ToShortDateString();
                                        }
                                        rep.SetParameterValue("dat_prib", dat_prib_1);
                                        ////////
                                        if (listgil[i].dat_prib.Trim() != "")
                                        {
                                            dat_prib = System.Convert.ToDateTime(listgil[i].dat_prib).ToShortDateString();
                                        }
                                        table.Rows.Add(
                                           listgil[i].fam.Trim(),
                                           listgil[i].ima.Trim(),
                                           listgil[i].otch.Trim(),
                                           listgil[i].rem_mr.Trim(),
                                           dat_rog.Trim(),
                                           dat_prib.Trim()
                                         );
                                        break;
                                    }
                                //case 6:
                                //    {
                                //        SetKvarPrms(rep, y_, m_);
                                //        break;
                                //    }
                                case 8:
                                    {
                                        string dat_prib_1 = "";
                                        if (listgil[i].dat_prib.Trim() != "")
                                        {
                                            dat_prib_1 = System.Convert.ToDateTime(listgil[i].dat_prib).ToShortDateString();
                                        }
                                        rep.SetParameterValue("dat_prib", dat_prib_1);

                                        SetKvarPrms(rep, y_, m_);
                                        ////////
                                        if (listgil[i].dat_prib.Trim() != "")
                                        {
                                            dat_prib = System.Convert.ToDateTime(listgil[i].dat_prib).ToShortDateString();
                                        }
                                        if (listgil[i].dat_rog.Trim() != "")
                                        {
                                            dat_rog = System.Convert.ToDateTime(listgil[i].dat_rog).ToShortDateString();
                                        }
                                        table.Rows.Add(
                                           listgil[i].fam.Trim(),
                                           listgil[i].ima.Trim(),
                                           listgil[i].otch.Trim(),
                                           listgil[i].rem_mr.Trim(),
                                           listgil[i].rod.Trim(),
                                           dat_rog.Trim(),
                                           dat_prib.Trim(),
                                           listgil[i].type_prop.Trim()
                                         );

                                        break;
                                    }
                                case 9:
                                    {
                                        string dat_prib_1 = "";
                                        if (listgil[i].dat_prib.Trim() != "")
                                        {
                                            dat_prib_1 = System.Convert.ToDateTime(listgil[i].dat_prib).ToShortDateString();
                                        }
                                        rep.SetParameterValue("dat_prib", dat_prib_1);

                                        SetKvarPrms(rep, y_, m_);
                                        //////////
                                        if (listgil[i].dat_prib.Trim() != "")
                                        {
                                            dat_prib = System.Convert.ToDateTime(listgil[i].dat_prib).ToShortDateString();
                                        }
                                        if (listgil[i].dat_oprp.Trim() != "")
                                        {
                                            dat_oprp = System.Convert.ToDateTime(listgil[i].dat_oprp).ToShortDateString();
                                        }
                                        table.Rows.Add(
                                           listgil[i].fam.Trim(),
                                           listgil[i].ima.Trim(),
                                           listgil[i].otch.Trim(),
                                           listgil[i].rem_mr.Trim(),
                                           listgil[i].rod.Trim(),
                                           dat_rog.Trim(),
                                           dat_prib.Trim(),
                                           listgil[i].type_prop.Trim(),
                                           dat_oprp.Trim()
                                         );

                                        break;
                                    }
                                case 10:
                                    {
                                        SetKvarPrms(rep, y_, m_);

                                        SetKolGil(rep, y_, m_);

                                        break;
                                    }
                                case 11:
                                    {
                                        string dat_prib_1 = "";
                                        if (listgil[i].dat_prib.Trim() != "")
                                        {
                                            dat_prib_1 = System.Convert.ToDateTime(listgil[i].dat_prib).ToShortDateString();
                                        }
                                        rep.SetParameterValue("dat_prib", dat_prib_1);

                                        //паспорт
                                        rep.SetParameterValue("serij", listgil[i].serij.Trim());
                                        rep.SetParameterValue("nomer", listgil[i].nomer.Trim());
                                        rep.SetParameterValue("vid_mes", listgil[i].vid_mes.Trim());
                                        if (listgil[i].vid_dat.Trim() != "")
                                        {
                                            rep.SetParameterValue("vid_dat", System.Convert.ToDateTime(listgil[i].vid_dat).ToShortDateString());
                                        }
                                        else
                                        {
                                            rep.SetParameterValue("vid_dat", "дата выдачи:нет данных");
                                        }
                                        break;
                                    }
                            }
                        }
                    }
                    //дата
                    rep.SetParameterValue("get_date", date);

                    //паспортист,начальник
                    Gilec us = new Gilec();
                    us.nzp_user = CurT_nzp_user;
                    us.nzp_kvar = CurT_nzp_kvar;
                    us.pref = CurT_pref;

                    cli = new cli_Gilec(WCFParams.AdresWcfWeb.CurT_Server);
                    string[] NachInfo = cli.GetPasportistInformation(us, out ret);
                    if (NachInfo != null)
                    {
                        rep.SetParameterValue("fim_pasportist", NachInfo[0].Trim());
                        rep.SetParameterValue("dolgnost_pasport", NachInfo[1].Trim());
                        rep.SetParameterValue("dolgnost_nach", NachInfo[2].Trim());
                        rep.SetParameterValue("fim_nachPus", NachInfo[3].Trim());
                    }


                    #region Адрес
                    SetKvarAdres(rep);

                    rep.SetParameterValue("town", listgil[0].town_.Trim());
                    rep.SetParameterValue("rajon", listgil[0].rajon_.Trim());
                    #endregion
                }
            }

            if (!keykart)
            {
                rep.SetParameterValue("text", "НЕ");
                Kart kart = GetData(out ret);
                rep.SetParameterValue("gil_fam", kart.fam.Trim());
                rep.SetParameterValue("gil_ima", kart.ima.Trim());
                rep.SetParameterValue("gil_otch", kart.otch.Trim());
                rep.SetParameterValue("town", kart.town.Trim());
                string rajon = kart.rajon.Trim();
                if (rajon == "" || rajon == "-") rajon = kart.rajon_dom.Trim();
                rep.SetParameterValue("rajon", rajon.Trim());
                if (kart.dat_rog.Trim() != "")
                {
                    rep.SetParameterValue("b_date", System.Convert.ToDateTime(kart.dat_rog).ToShortDateString());
                }
                string dat_prib_1 = "", dat_rog = "";
                rep.SetParameterValue("dat_prib", dat_prib_1.Trim());
                //отличные параметры в шапке
                switch (vidSprav)
                {
                    case 7:
                        {
                            table.Rows.Add(
                               kart.fam.Trim(),
                               kart.ima.Trim(),
                               kart.otch.Trim(),
                               kart.rem_mr.Trim(),
                               kart.dat_rog.Trim(),
                               dat_prib_1.Trim()
                             );
                            break;
                        }
                    case 8:
                        {
                            SetKvarPrms(rep, y_, m_);
                            if (kart.dat_rog.Trim() != "")
                            {
                                dat_rog = System.Convert.ToDateTime(kart.dat_rog).ToShortDateString();
                            }
                            table.Rows.Add(
                               kart.fam.Trim(),
                               kart.ima.Trim(),
                               kart.otch.Trim(),
                               kart.rem_mr.Trim(),
                               kart.rod.Trim(),
                               dat_rog.Trim(),
                               "",
                               ""//kart.type_prop
                             );

                            break;
                        }
                    case 9:
                        {
                            SetKvarPrms(rep, y_, m_);
                            table.Rows.Add(
                               kart.fam.Trim(),
                               kart.ima.Trim(),
                               kart.otch.Trim(),
                               kart.rem_mr.Trim(),
                               kart.rod.Trim(),
                               kart.dat_rog.Trim(),
                               "",
                               "",
                               kart.dat_oprp.Trim()
                             );

                            break;
                        }
                    case 10:
                        {
                            SetKvarPrms(rep, y_, m_);
                            SetKolGil(rep, y_, m_);
                            break;
                        }
                    case 11:
                        {
                            //паспорт
                            rep.SetParameterValue("serij", kart.serij.Trim());
                            rep.SetParameterValue("nomer", kart.nomer.Trim());
                            rep.SetParameterValue("vid_mes", kart.vid_mes.Trim());
                            if (kart.vid_dat.Trim() != "")
                            {
                                rep.SetParameterValue("vid_dat", System.Convert.ToDateTime(kart.vid_dat.Trim()).ToShortDateString());
                            }
                            else
                            {
                                rep.SetParameterValue("vid_dat", "дата выдачи:нет данных");
                            }
                            break;
                        }
                }
            }

            #region Адрес
            SetKvarAdres(rep);
            #endregion
            if (has_fam == "")
            {
                rep.SetParameterValue("vl_fam", rep.GetParameterValue("fio"));
            }

            rep.RegisterData(FDataSet);
            rep.GetDataSource("Q_master").Enabled = true;
            #endregion

            if (listgil != null && ret.result && listgil.Count > 0 && vidSprav == 11)
            {
                rep.SetParameterValue("town", listgil[0].town_.Trim());
                rep.SetParameterValue("rajon", listgil[0].rajon_.Trim());
            }

            #region Квартирные параметры
            SetKvarPrms(rep, y_, m_);
            #endregion

            #region Зашифровать отчет в строку
            MemoryStream ms = new MemoryStream();
            if (Points.IsSmr)
                rep.SetParameterValue("isSmr", "1");
            rep.SetParameterValue("IsGubkin", Points.Region == Regions.Region.Belgorodskaya_obl);
            rep.Prepare();
            rep.SavePrepared(ms);
            ms.Position = 0;

            //return System.Text.Encoding.Default.GetString(ms.ToArray());
            return System.Convert.ToBase64String(ms.ToArray());
            #endregion

        }

        public Kart GetData(out Returns ret) {
            cli_Gilec cli = new cli_Gilec(WCFParams.AdresWcfWeb.CurT_Server);
            Kart finder = new Kart();
            finder.nzp_kart = CurT_nzp_Kart.ToString();
            finder.pref = CurT_pref;
            finder.is_arx = CurT_Kart_Archive;
            ret = Utils.InitReturns();
            Kart card = cli.LoadKart(finder, out ret);
            if (card == null) card = new Kart();
            return card;
        }

        public string Fill_licShet(Report rep, int y_, int m_, DateTime date, out Returns ret) {

            #region Запонение таблицы для отчета

            DataSet FDataSet = new DataSet();

            DataTable table = new DataTable();
            table.TableName = "Q_master";
            FDataSet.Tables.Add(table);
            for (int i = 1; i < 16; i++)
            {
                table.Columns.Add("service" + i, typeof(string));
                table.Columns.Add("tarif" + i, typeof(string));
                table.Columns.Add("name_frm" + i, typeof(string));
                table.Columns.Add("sum_tarif" + i, typeof(string));
                table.Columns.Add("c_sn" + i, typeof(string));
                table.Columns.Add("c_calc" + i, typeof(string));
                table.Columns.Add("measure" + i, typeof(string));
                table.Columns.Add("sum_charge" + i, typeof(string));
                table.Columns.Add("primech" + i, typeof(string));
            }

            table.Columns.Add("month_", typeof(string));
            table.Columns.Add("year_", typeof(string));
            table.Columns.Add("sum_charge", typeof(string));
            table.Columns.Add("sum_bez_lgot", typeof(string));
            table.Columns.Add("sum_dolg", typeof(string));
            table.Columns.Add("IRC", typeof(string));
            table.Columns.Add("adres_IRC", typeof(string));
            table.Columns.Add("tel_IRC", typeof(string));
            table.Columns.Add("pl_all", typeof(string));
            table.Columns.Add("pl_gil", typeof(string));
            table.Columns.Add("kol_kom", typeof(string));
            table.Columns.Add("komf", typeof(string));
            table.Columns.Add("kolgil", typeof(string));
            table.Columns.Add("fact_gil", typeof(string));
            table.Columns.Add("stat_Lschet", typeof(string));
            table.Columns.Add("privat", typeof(string));
            table.Columns.Add("date_priv", typeof(string));
            table.Columns.Add("primech", typeof(string));
            table.Columns.Add("Lschet_short", typeof(string));
            table.Columns.Add("fio", typeof(string));
            table.Columns.Add("ndom", typeof(string));
            table.Columns.Add("ulica", typeof(string));
            table.Columns.Add("nkvar", typeof(string));
            table.Columns.Add("num_geu", typeof(string));
            table.Columns.Add("mnogokv", typeof(string));

            #endregion

            #region Выборка данных
            ret = Utils.InitReturns();

            Kart finder = new Kart();
            finder.nzp_user = nzp_user;
            finder.nzp_kvar = CurT_nzp_kvar;
            finder.nzp_dom = CurT_nzp_dom;
            finder.nzp_kart = CurT_nzp_Kart.ToString();
            finder.fam = "";
            finder.ima = "";
            finder.otch = "";
            finder.pref = CurT_pref;


            int y1 = y_ - 2000;

            List<Charge> Lst = null;
            cli_Charge cli = new cli_Charge(WCFParams.AdresWcfWeb.CurT_Server);

            Lst = cli.GetLicChetData(ref finder, out ret, y1, m_);
            if (!ret.result)
            {
                return "";
            }

            decimal sum_bez_lgot = 0;
            decimal sum_charge = 0;
            decimal sum_dolg = 0;
            //string fio = "";
            DataRow dr = table.Rows.Add();
            int stIndex = 1;
            if (Lst != null & ret.result)
            {
                if (Lst.Count > 0)
                {
                    for (int i = 0; i < Lst.Count; i++)
                    {
                        if (i < 16)
                        {
                            if (Lst[i].tarif + System.Math.Abs(Lst[i].sum_charge) +
                                System.Math.Abs(Lst[i].sum_tarif) +
                                Lst[i].c_sn > 0.0001m)
                            {

                                dr["service" + stIndex] = Lst[i].service;
                                dr["measure" + stIndex] = Lst[i].measure;
                                dr["name_frm" + stIndex] = Lst[i].name_frm;
                                if (Lst[i].nzp_serv != -515)
                                {


                                    if (Lst[i].tarif < 0.001m)
                                    {
                                        dr["tarif" + stIndex] = "";
                                        dr["c_calc" + stIndex] = "";
                                    }
                                    else
                                    {
                                        dr["tarif" + stIndex] = Lst[i].tarif.ToString("0.00");
                                        dr["c_calc" + stIndex] = Lst[i].c_calc.ToString("0.00");
                                    }


                                    if (Lst[i].nzp_serv == 515)
                                    {
                                        dr["c_sn" + stIndex] = "";
                                    }
                                    else
                                        dr["c_sn" + stIndex] = Lst[i].c_sn.ToString("0.00");


                                }

                                if ((Lst[i].nzp_serv == 25) & (System.Math.Abs(Lst[i].sum_tarif) +
                                    System.Math.Abs(Lst[i].sum_charge) < 0.001m))
                                {
                                    dr["sum_tarif" + stIndex] = "";
                                    dr["sum_charge" + stIndex] = "";
                                }
                                else
                                {
                                    dr["sum_tarif" + stIndex] = Lst[i].sum_tarif.ToString("0.00");
                                    dr["sum_charge" + stIndex] = Lst[i].sum_charge.ToString("0.00");
                                }
                                stIndex++;
                            }
                        }
                        sum_bez_lgot += Lst[i].sum_tarif;
                        sum_charge += Lst[i].sum_charge;
                        sum_dolg += Lst[i].sum_pere;
                    }

                }

            }





            DateTime date_ = new DateTime(y_, m_, 15);
            rep.SetParameterValue("date", date_.ToShortDateString());
            string[] months = new string[] {"","Январь","Февраль",
                 "Март","Апрель","Май","Июнь","Июль","Август","Сентябрь",
                 "Октябрь","Ноябрь","Декабрь"};
            dr["month_"] = months[m_];
            dr["year_"] = y_;
            dr["sum_charge"] = sum_charge.ToString("0.00");
            dr["sum_bez_lgot"] = sum_bez_lgot.ToString("0.00");
            dr["sum_dolg"] = sum_dolg.ToString("0.00");



            #endregion



            #region Адрес
            SetKvarAdres(rep);
            #endregion

            #region Квартирные параметры
            SetKvarPrms(rep, y_, m_);
            #endregion

            #region заполнение параметров подписей отчета
            /* 579 - Наименование должности бухгалтера
               1047 - ФИО руководителя ПУС 
               1048 - Должность руководителя ПУС */
            var cliPrm = new cli_Prm(WCFParams.AdresWcfWeb.CurT_Server);
            var finderPrm = new Prm
            {
                pref = CurT_pref,
                prm_num = 10,
                spis_prm = "579, 1047, 1048",
                is_actual = 1,
                date_begin = DateTime.Now.ToShortDateString(),
                nzp_user = nzp_user
            };
            List<Prm> listPrms = cliPrm.GetPrm(finderPrm, enSrvOper.SrvFind, out ret);
            if (!ret.result)
            {
                MonitorLog.WriteLog("class ReportS, метод Fill_licShet \n " +
                                    " Ошибка формирования списка параметров \"подписи\" :" + ret.text, MonitorLog.typelog.Error, true);
                return string.Empty;
            }
            rep.SetParameterValue("pasport_dol",
                listPrms.Find(x => x.nzp_prm == 579) != null
                    ? listPrms.Find(x => x.nzp_prm == 579).val_prm
                    : string.Empty);

            rep.SetParameterValue("nachal_fio",
                listPrms.Find(x => x.nzp_prm == 1047) != null
                    ? listPrms.Find(x => x.nzp_prm == 1047).val_prm
                    : string.Empty);

            rep.SetParameterValue("nachal_dol",
                listPrms.Find(x => x.nzp_prm == 1048) != null
                    ? listPrms.Find(x => x.nzp_prm == 1048).val_prm
                    : string.Empty);

            var finderUser = new User { nzpuser = nzp_user, nzp_user = nzp_user };

            //получить пользователя
            ReturnsObjectType<List<User>> users = cli_Admin.DbGetUsers(finderUser);

            rep.SetParameterValue("pasport_fio",
                (users != null && users.result && users.returnsData != null && users.returnsData.Count > 0)
                    ? Utils.GetCorrectFIO(users.returnsData[0].uname)
                    : string.Empty);
            #endregion

            dr["IRC"] = rep.GetParameterValue("IRC");
            dr["adres_IRC"] = rep.GetParameterValue("adres_IRC");
            dr["tel_IRC"] = rep.GetParameterValue("tel_IRC");
            dr["pl_all"] = rep.GetParameterValue("pl_all");
            dr["pl_gil"] = rep.GetParameterValue("pl_gil");
            dr["kol_kom"] = rep.GetParameterValue("kol_kom");
            if (rep.GetParameterValue("komf").ToString() == "коммунальная")
                dr["komf"] = "да";
            else
                dr["komf"] = "нет";
            dr["kolgil"] = rep.GetParameterValue("kolgil");
            dr["fact_gil"] = rep.GetParameterValue("fact_gil");
            dr["stat_Lschet"] = rep.GetParameterValue("stat_Lschet");
            dr["privat"] = rep.GetParameterValue("privat");
            dr["date_priv"] = rep.GetParameterValue("date_priv");
            dr["primech"] = rep.GetParameterValue("primech");
            dr["Lschet_short"] = rep.GetParameterValue("Lschet_short");
            dr["fio"] = rep.GetParameterValue("fio");
            dr["ulica"] = rep.GetParameterValue("ulica");
            dr["ndom"] = rep.GetParameterValue("ndom");
            dr["nkvar"] = rep.GetParameterValue("nkvar");
            dr["num_geu"] = rep.GetParameterValue("num_geu");
            dr["mnogokv"] = rep.GetParameterValue("mnogokv");







            rep.RegisterData(FDataSet);
            rep.GetDataSource("Q_master").Enabled = true;

            #region Зашифровать отчет в строку
            MemoryStream ms = new MemoryStream();
            try
            {
                rep.Prepare();
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка формирования отчета по ЛС " + ex.Message, MonitorLog.typelog.Error, true);
            }
            rep.SavePrepared(ms);
            ms.Position = 0;

            //return System.Text.Encoding.Default.GetString(ms.ToArray());
            return System.Convert.ToBase64String(ms.ToArray());
            #endregion


        }

        public string Reg_Po_MestuGilec(Report rep, int y_, int m_, DateTime date) {

            cli_Gilec cli = new cli_Gilec(WCFParams.AdresWcfWeb.CurT_Server);
            Returns ret = Utils.InitReturns();

            Gilec finder = new Gilec();
            finder.nzp_user = nzp_user;
            finder.nzp_kvar = CurT_nzp_kvar;
            finder.nzp_kart = CurT_nzp_Kart;
            finder.pref = CurT_pref;
            finder.is_arx = CurT_Kart_Archive;
            List<Kart> listKart = null;
            listKart = cli.Reg_Po_MestuGilec(out ret, finder);



            string[] MonthMasR = new string[] { "", "Января", "Февраля", "Марта", "Апреля", "Мая", "Июня", 
                                               "Июля", "Августа", "Сентября", "Октября", "Ноября", "Декабря" };

            if (listKart != null & ret.result)
            {
                if (listKart.Count > 0)
                {
                    //констр for потом убрать
                    for (int i = 0; i < listKart.Count; i++)
                    {
                        if ((long) System.Convert.ToInt64(listKart[i].nzp_kart) == CurT_nzp_Kart)
                        {
                            rep.SetParameterValue("fam", listKart[i].fam.ToString().Trim());
                            rep.SetParameterValue("ima", listKart[i].ima.ToString().Trim());
                            rep.SetParameterValue("otch", listKart[i].otch.ToString().Trim());
                            rep.SetParameterValue("namereg", listKart[i].namereg.ToString().Trim());

                            rep.SetParameterValue("dok", listKart[i].dok.ToString());
                            rep.SetParameterValue("serij", listKart[i].serij.ToString());
                            rep.SetParameterValue("nomer", listKart[i].nomer.ToString());
                            rep.SetParameterValue("vid_mes", listKart[i].vid_mes.ToString());
                            if (listKart[i].vid_dat.ToString() != "")
                            {
                                string vid_dat = listKart[i].vid_dat.ToString();
                                int month_ = System.Convert.ToInt16(vid_dat.Substring(3, 2));
                                rep.SetParameterValue("date_dat_vid", vid_dat.Substring(0, 2));
                                rep.SetParameterValue("month_vid", MonthMasR[month_]);
                                rep.SetParameterValue("y_vid", vid_dat.Substring(6, 4));
                            }

                            if (listKart[i].dat_ofor.ToString() != "")
                            {
                                string vid_dat = listKart[i].dat_ofor.ToString();
                                int month_ = System.Convert.ToInt16(vid_dat.Substring(3, 2));
                                rep.SetParameterValue("date_dat_ofor", vid_dat.Substring(0, 2));
                                rep.SetParameterValue("month_ofor", MonthMasR[month_]);
                                rep.SetParameterValue("y_ofor", vid_dat.Substring(6, 4));
                            }

                            if (listKart[i].dat_oprp.ToString() != "")
                            {
                                string vid_dat = listKart[i].dat_oprp.ToString();
                                int month_ = System.Convert.ToInt16(vid_dat.Substring(3, 2));
                                rep.SetParameterValue("date_dat_oprp", vid_dat.Substring(0, 2));
                                rep.SetParameterValue("month_oprp", MonthMasR[month_]);
                                rep.SetParameterValue("y_oprp", vid_dat.Substring(6, 4));
                            }

                            if (listKart[i].dat_rog.ToString() != "")
                            {
                                rep.SetParameterValue("dat_rog", listKart[i].dat_rog.Substring(0, 10));
                            }

                            rep.SetParameterValue("land_op", listKart[i].land_op.ToString().Trim());
                            rep.SetParameterValue("stat_op", listKart[i].stat_op.ToString().Trim());
                            rep.SetParameterValue("town_op", listKart[i].town_op.ToString().Trim());
                            rep.SetParameterValue("rajon_op", listKart[i].rajon_op.ToString().Trim());
                            rep.SetParameterValue("rem_op", listKart[i].rem_op.ToString().Trim());

                            rep.SetParameterValue("land", listKart[i].land.ToString().Trim());
                            rep.SetParameterValue("stat", listKart[i].stat.ToString().Trim());
                            rep.SetParameterValue("town", listKart[i].town.ToString().Trim());
                            rep.SetParameterValue("rajon", listKart[i].rajon.ToString().Trim());
                            rep.SetParameterValue("ulica", listKart[i].ulica.ToString().Trim());
                            rep.SetParameterValue("ndom", listKart[i].ndom.ToString().Trim());
                            rep.SetParameterValue("nkor", listKart[i].nkor.ToString().Trim());
                            rep.SetParameterValue("nkvar", listKart[i].nkvar.ToString().Trim());

                            //Дата
                            {
                                string vid_dat = DateTime.Now.ToString();
                                int month_ = System.Convert.ToInt16(vid_dat.Substring(3, 2));
                                rep.SetParameterValue("date_get_date", vid_dat.Substring(0, 2));
                                rep.SetParameterValue("month_get_date", MonthMasR[month_]);
                                rep.SetParameterValue("y_get_date", vid_dat.Substring(6, 4));
                            }

                        }
                    }
                }
            }

            #region Зашифровать отчет в строку
            MemoryStream ms = new MemoryStream();
            rep.Prepare();
            rep.SavePrepared(ms);
            ms.Position = 0;

            return System.Convert.ToBase64String(ms.ToArray());
            #endregion

        }

        public string Fill_web_listok_ubit(Report rep, int y_, int m_, DateTime date) {
            cli_Gilec cli = new cli_Gilec(WCFParams.AdresWcfWeb.CurT_Server);
            Kart finder = new Kart();
            finder.nzp_kart = CurT_nzp_Kart.ToString();
            finder.pref = CurT_pref;
            Returns ret = Utils.InitReturns();
            Kart card = cli.LoadKart(finder, out ret);
            if (card == null) card = new Kart();
            if (!ret.result) return "";

            #region тип регистрации
            if (card.tprp.Trim().ToUpper() == "ПРЕБЫВАНИЯ")
            {
                rep.SetParameterValue("tprp", "В");
                rep.SetParameterValue("reg_zit", "");
                rep.SetParameterValue("reg_preb", card.dat_ofor.Trim());
            }
            else
            {
                rep.SetParameterValue("tprp", "П");
                rep.SetParameterValue("reg_zit", card.dat_ofor.Trim());
                rep.SetParameterValue("reg_preb", "");
            }
            #endregion
            rep.SetParameterValue("fam", card.fam.Trim().ToUpper());
            rep.SetParameterValue("ima", card.ima.Trim().ToUpper());
            rep.SetParameterValue("otch", card.otch.Trim().ToUpper());
            rep.SetParameterValue("dat_rog", card.dat_rog.Trim());
            string gender = "";
            if (card.gender.Trim().ToUpper() == "Ж") gender = "ЖЕН";
            else if (card.gender.Trim().ToUpper() == "М") gender = "МУЖ";
            else gender = card.gender.Trim().ToUpper();
            rep.SetParameterValue("pol" +
                                  "" +
                                  "", gender);
            #region место рождения
            rep.SetParameterValue("mr_country", card.lnmr.Trim());
            rep.SetParameterValue("mr_region", card.stmr.Trim());
            /*if (card.tnmr.Trim().ToUpper().Substring(card.tnmr.Trim().Length - 2, 2) == " Г")
            {
                rep.SetParameterValue("mr_city", card.tnmr.Trim());
                rep.SetParameterValue("mr_rajon", "");
            }
            else
            {*/
            rep.SetParameterValue("mr_rajon", card.tnmr.Trim());
            rep.SetParameterValue("mr_city", "");
            //}

            rep.SetParameterValue("mr_nas_punkt", card.rnmr.Trim());
            #endregion

            string gragd = "";
            foreach (KartGrgd kgragd in card.listKartGrgd)
            {
                if (gragd == "") gragd += kgragd.grgd;
                else gragd += "," + kgragd.grgd;
            }
            rep.SetParameterValue("grazhd", gragd);

            rep.SetParameterValue("reg_region", card.rajon);

            /*if (card.tnmr.Trim().ToUpper().Substring(card.tnmr.Trim().Length - 2, 2) == " Г")
            {
                rep.SetParameterValue("reg_city", card.stat.Trim());
                rep.SetParameterValue("reg_rajon", "");
            }
            else
            {*/
            rep.SetParameterValue("reg_rajon", card.stat.Trim());
            rep.SetParameterValue("reg_city", "");
            //}
            rep.SetParameterValue("reg_nas_punkt", card.town);
            string adres = "ул. " + card.ulica + ", дом " + card.ndom;
            if (card.nkor.Trim() == "") adres += ", корп. " + card.nkor;
            if (card.nkvar.Trim() == "") adres += ", кв. " + card.nkvar;
            if (card.nkvar_n.Trim() == "") adres += ", комн. " + card.nkvar_n;
            rep.SetParameterValue("reg_adres", adres);

            rep.SetParameterValue("organ_reg_uchet", card.namereg);
            rep.SetParameterValue("kod_podrazd", "");
            rep.SetParameterValue("vid_doc", "");
            rep.SetParameterValue("dat_vid", "");
            rep.SetParameterValue("who_vid", "");
            rep.SetParameterValue("kod_who_vid", "");

            rep.SetParameterValue("vib_country", "");
            rep.SetParameterValue("vib_region", "");
            rep.SetParameterValue("vib_rajon", "");
            rep.SetParameterValue("vib_city", "");
            rep.SetParameterValue("vib_nas_punkt", "");
            rep.SetParameterValue("vib_adres", "");

            rep.SetParameterValue("from_country", "");
            rep.SetParameterValue("from_region", "");
            rep.SetParameterValue("from_rajon", "");
            rep.SetParameterValue("from_city", "");
            rep.SetParameterValue("from_nas_punkt", "");
            rep.SetParameterValue("from_adres", "");

            rep.SetParameterValue("ulica", "");
            rep.SetParameterValue("dom", "");
            rep.SetParameterValue("kvar", "");
            rep.SetParameterValue("new_fam", "");
            rep.SetParameterValue("new_ima", "");
            rep.SetParameterValue("new_otch", "");
            rep.SetParameterValue("new_day_rog", "");
            rep.SetParameterValue("new_month_rog", "");
            rep.SetParameterValue("new_year_rog", "");

            rep.SetParameterValue("prichini", "");
            rep.SetParameterValue("dat_sost", "");
            rep.SetParameterValue("dat_oform", "");
            rep.SetParameterValue("seria", "");
            rep.SetParameterValue("nomer", "");
            rep.SetParameterValue("new_pol", "");


            MemoryStream ms = new MemoryStream();
            rep.Prepare();
            rep.SavePrepared(ms);
            ms.Position = 0;

            return System.Convert.ToBase64String(ms.ToArray());

        }

        public string fill_zakaz_report(Report rep, string table_name, int nzp, Email email) {
            #region заполняем параметры

            cli_Supg cli = new cli_Supg(WCFParams.AdresWcfWeb.CurT_Server);
            SupgFinder finder = new SupgFinder();
            finder.nzp_user = CurT_nzp_user;
            finder.nzp_zk = CurT_Supg_nzp_zk;
            finder.pref = CurT_pref;

            #endregion

            #region получение данных для отчета

            Returns ret = Utils.InitReturns();
            DataSet dataset = cli.GetZakazReport(finder, table_name, out ret);
            if (dataset == null) dataset = new DataSet();
            if (ret.result == false) return "";

            rep.RegisterData(dataset);
            rep.GetDataSource("Q_master").Enabled = true;

            #endregion

            //FastReport.Export.Email.EmailSettings email_set = new FastReport.Export.Email.EmailSettings();
            //email_set.Address = email.smtp.fromEmail;
            //email_set.Name = email.smtp.fromName;
            //email_set.Host = email.smtp.host;
            //email_set.Port = email.smtp.port;
            //email_set.UserName = email.smtp.userName;
            //email_set.Password = email.smtp.userPwd;

            ////rep.EmailSettings = email_set;?????

            //FastReport.Export.Email.EmailExport export_mail = new FastReport.Export.Email.EmailExport();
            //export_mail.Address = "nail@stcline.ru";

            rep.Prepare();

            //export_mail.SendEmail(rep);
            ////Email email = new Email();
            ////    email.smtp = smtp;
            ////    email.smtp.fromName = "STC Line";
            ////    email.to = mailAddress.Address;
            ////    email.toName = mailAddress.DisplayName;
            ////    email.bcc = "valya@stcline.ru";
            ////    email.subject = "№ наряда-заказа: " + Current_nzp_zk;
            ////    if (CheckSent())
            ////        email.subject += "(повторный)";
            ////    email.body = "<p>Номер наряда-заказа: " + Current_nzp_zk + " " + email.toName + "</p>";
            ////    email.attachments = new List<string>();
            ////    email.attachments.Add(fileName);

            ////FastReport.Export.Pdf.PDFExport export_pdf = new FastReport.Export.Pdf.PDFExport();
            ////export_pdf.ShowProgress = false;
            ////Report repp = rep;
            ////export_pdf.Export(rep, Constants.TmpFilesDirWeb + nzp);
            ////export_pdf.Dispose();

            return "";
        }

        #endregion
    }
}

