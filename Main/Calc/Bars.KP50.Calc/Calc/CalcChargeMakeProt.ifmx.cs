using System;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Data;
using System.Collections.Generic;
using System.Globalization;
using Bars.KP50.Utils;
using STCLINE.KP50.EPaspXsd;
using STCLINE.KP50.Global;
using STCLINE.KP50.IFMX.Kernel.source.CommonType;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase
{
    public partial class DbCalcCharge : DataBaseHead
    {
        #region структура для выборки из CalcGku
        //---------------------------------------------------
        public struct CalcGkuVals
        {
            public Decimal tarif;
            public Decimal rashod;
            public Decimal rashod_full;
            public int nzp_frm;
            public string name_frm;
            public int nzp_frm_typ;
            public int nzp_frm_typrs;
            public int nzp_prm_tarif;
            public int num_prm_tarif;
            public string name_prm_tarif;
            public int nzp_prm_rashod;
            public int num_prm_rashod;
            public string name_prm_rashod;
            public DateTime dat_s;
            public DateTime dat_po;
            public string measure;
            public int is_device;
            public int cntd;
            public int cntd_mn;
            public Decimal trf1;
            public Decimal trf2;
            public Decimal trf3;
            public Decimal trf4;
            public Decimal rsh1;
            public Decimal rsh2;
            public Decimal rsh3;
            public Decimal valm;
            public Decimal dop87;
            public Decimal dlt_reval;
            public Decimal gil;
            public Decimal squ;
            public Decimal rash_norm_one;
            public Decimal rash_norm;
            public Decimal rashod_source;
            public Decimal up_kf;
            public Decimal val3;
            public Decimal squ2;

            public string body_calc;
            public string body_text;
        }

        public CalcGkuVals InitCalcGkuVals()
        {
            CalcGkuVals pCalcGkuVals;

            pCalcGkuVals.tarif = 0;
            pCalcGkuVals.rashod = 0;
            pCalcGkuVals.rashod_full = 0;
            pCalcGkuVals.nzp_frm = 0;
            pCalcGkuVals.name_frm = "";
            pCalcGkuVals.nzp_frm_typ = 0;
            pCalcGkuVals.nzp_frm_typrs = 0;
            pCalcGkuVals.nzp_prm_tarif = 0;
            pCalcGkuVals.name_prm_tarif = "";
            pCalcGkuVals.nzp_prm_rashod = 0;
            pCalcGkuVals.name_prm_rashod = "";
            pCalcGkuVals.dat_s = DateTime.Today;
            pCalcGkuVals.dat_po = DateTime.Today;
            pCalcGkuVals.measure = "";
            pCalcGkuVals.is_device = 0;
            pCalcGkuVals.num_prm_tarif = 0;
            pCalcGkuVals.num_prm_rashod = 0;
            pCalcGkuVals.cntd = 0;
            pCalcGkuVals.cntd_mn = 0;

            pCalcGkuVals.trf1 = 0;
            pCalcGkuVals.trf2 = 0;
            pCalcGkuVals.trf3 = 0;
            pCalcGkuVals.trf4 = 0;
            pCalcGkuVals.rsh1 = 0;
            pCalcGkuVals.rsh2 = 0;
            pCalcGkuVals.rsh3 = 0;
            pCalcGkuVals.valm = 0;
            pCalcGkuVals.dop87 = 0;
            pCalcGkuVals.dlt_reval = 0;
            pCalcGkuVals.gil = 0;
            pCalcGkuVals.squ = 0;
            pCalcGkuVals.rash_norm_one = 0;
            pCalcGkuVals.rash_norm = 0;
            pCalcGkuVals.up_kf = 1;
            pCalcGkuVals.rashod_source = 0;

            pCalcGkuVals.val3 = 0;
            pCalcGkuVals.squ2 = 0;

            pCalcGkuVals.body_calc = "";
            pCalcGkuVals.body_text = "";
            return pCalcGkuVals;
        }

        //-----------------------------------------------------------------------------
        #endregion структура для выборки из CalcGku

        #region Вставить значения в текст протокола расчета
        string ReplFrmValues(string pCurOpisFrm, string[] pNames, string[] pVals)
        {
            string sResOpisFrm = pCurOpisFrm;

            string sCurOpisFrm = pCurOpisFrm;
            int iPos = 1;

            while (iPos > 0)
            {
                string sNameValues = "";

                int iPosEnd = sCurOpisFrm.Length;
                //StringComparison.InvariantCulture;
                iPos = sCurOpisFrm.IndexOf('[');

                if (iPos > 0)
                {
                    bool bIgnore = false;

                    for (int i = iPos + 1; i <= sCurOpisFrm.Length; i++)
                    {
                        if (sCurOpisFrm[i] == '<') { bIgnore = true; }

                        if (sCurOpisFrm[i] == '>') { bIgnore = false; continue; }

                        if (bIgnore) { continue; }
                        //
                        if (sCurOpisFrm[i] == ']') { iPosEnd = i; break; }
                        sNameValues = sNameValues + sCurOpisFrm[i];
                    }
                    //
                    string sValue = ""; // FloatToStr(Formuls_Calc.Export_Data(sNameValues, False));
                    for (int i = 0; i < pNames.Length; i++)
                    {
                        if (sNameValues == pNames[i])
                        {
                            sValue = pVals[i]; break;
                        }
                    }
                    //

                    string sTmpPrm = sCurOpisFrm.Substring(iPos, iPosEnd - iPos);

                    string sTmpPrmRes = sTmpPrm.Replace(sNameValues, sValue);

                    sResOpisFrm = sResOpisFrm.Replace(sTmpPrm, sTmpPrmRes);

                    string sTmpBeg = sCurOpisFrm.Substring(0, iPos);
                    int ilength = (sCurOpisFrm.Trim()).Length - 1;
                    //string sTmpEnd = sCurOpisFrm.Substring(iPos + 1, ilength);
                    string sTmpEnd = sCurOpisFrm.Substring(iPos + 1);
                    sCurOpisFrm = sTmpBeg + " " + sTmpEnd;

                }

            }

            sResOpisFrm = sResOpisFrm.Replace('[', ' ');
            sResOpisFrm = sResOpisFrm.Replace(']', ' ');

            return sResOpisFrm;
        }
        #endregion Вставить значения в текст протокола расчета

        #region Загрузить заготовки текста протокола расчета
        string GetDescrFrm(IDbConnection conn_db, CalcTypes.ChargeXX chargeXX, int nzp_frm, out Returns ret)
        {
            MyDataReader reader;

            string sText = "";
            ret = ExecRead(conn_db, out reader, " Select prot_html From " + chargeXX.paramcalc.kernel_alias + "frm_descr Where nzp_frm="
                + nzp_frm, true);
            if (!ret.result) { return sText; }

            try
            {
                if (reader.Read())
                {
                    if (reader["prot_html"] != DBNull.Value)
                        sText = ((string)reader["prot_html"]).Trim();
                }
                else
                { ret.tag = -1; }
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;

                return sText;
            }
            finally
            {
                reader.Close();
            }

            return sText;
        }
        #endregion Загрузить заготовки текста протокола расчета

        #region Выбрать значение параметра расчета и его наименование
        bool _GetValPrm(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc, bool reg, string inumprm, string prm_nm, string nzp_nm, out Returns ret)
        {
            ret = Utils.InitReturns();
            MyDataReader reader;

            string snzp_prm = "";
            if (reg)
            { snzp_prm = "(select " + prm_nm + " from tls where " + prm_nm + ">0 )"; }
            else
            { snzp_prm = prm_nm; }

            string snzp = "";
         
            snzp = "(select " + nzp_nm + " from tls)"; 

            ret = ExecRead(conn_db, out reader,
                " Select p.val_prm,n.name_prm" +
                " From " + paramcalc.data_alias + "prm_" + inumprm + " p, " + paramcalc.kernel_alias + "prm_name n" +
                " Where p.nzp_prm=n.nzp_prm and p.nzp_prm=" + snzp_prm + " and p.nzp= " + snzp +
                " and p.is_actual<>100" +
                " and p.dat_s<=" + MDY(paramcalc.calc_mm, DateTime.DaysInMonth(paramcalc.calc_yy, paramcalc.calc_mm), paramcalc.calc_yy) +
                " and p.dat_po>=" + MDY(paramcalc.calc_mm, 1, paramcalc.calc_yy)
                , true);
            if (!ret.result) { return false; }

            try
            {
                if (reader.Read())
                {
                    if (reader["val_prm"] != DBNull.Value)
                    {
                        if (reg)
                        { ret.text = ((string)reader["val_prm"]) + "(" + ((string)reader["name_prm"]) + ")"; }
                        else
                        { ret.text = ((string)reader["val_prm"]) + "(" + ((string)reader["name_prm"]) + ")"; }
                        switch (inumprm)
                        {
                            case "1": { ret.text = ret.text.Trim() + "-квартирный"; } break;
                            case "2": { ret.text = ret.text.Trim() + "-домовой"; } break;
                            case "3": { ret.text = ret.text.Trim() + "-квартирный"; } break;
                            case "4": { ret.text = ret.text.Trim() + "-домовой"; } break;
                            case "5": { ret.text = ret.text.Trim() + "-для всех ЛС"; } break;
                            case "11": { ret.text = ret.text.Trim() + "-для договора/поставщика"; } break;
                            default: { } break;
                        }
                        ret.sql_error = ((string)reader["val_prm"]);
                    }
                }
            }
            catch (Exception ex)
            {
                ExecSQL(conn_db, " Drop table tls ", false);
                ret.result = false;
                ret.text = ex.Message;
                return false;
            }
            finally
            {
                reader.Close();
            }

            return true;
        }
        #endregion Выбрать значение параметра расчета и его наименование

        #region наименование ключевого поля по номеру prm_XX
        public string GetNameNzp(int num_prm)
        {
            string sRet;
            switch (num_prm)
            {
                case 2:
                    {
                        sRet = "nzp_dom";
                    }
                    break;
                case 11:
                    {
                        sRet = "nzp_supp";
                    }
                    break;
                case 5:
                    {
                        sRet = "nzp";
                    }
                    break;
                default:
                    {
                        sRet = "nzp_kvar";
                    }
                    break;
            }
            return sRet;
        }
        #endregion наименование ключевого поля по номеру pmm_XX

        #region расшифровка тарифа по типу тарифа - nzp_frm_typ
        public bool SelTarifByType(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc,
            int nzp_serv, CalcTypes.ChargeXX chargeXX, CalcGkuVals pCalcGkuVals, out Returns ret)
        {
            ret = Utils.InitReturns();

            //MyDataReader reader;
            // расшифровка тарифа по типу тарифа - nzp_frm_typ
            string sRetVal = "Определение тарифа по параметрам расчета:<br />";
            string snzp_prm = GetNameNzp(pCalcGkuVals.num_prm_tarif);

            int iRegTarif = 1;  // nzp_frm_typ = 1,2,12,26,40,101,312,412, 514,1814 ...
            switch (pCalcGkuVals.nzp_frm_typ)
            {
                case 400: { iRegTarif = 2; } break;
                case 40: { iRegTarif = 3; } break;
                case 440: { iRegTarif = 3; } break;
                case 1140: { iRegTarif = 3; } break;
                case 814: { iRegTarif = 4; } break;  // Отопление от ГКал - тариф на кв.м
                default: { } break;
            }

            switch (iRegTarif)
            {
                case 1:
                    #region расшифровка простого тарифа
                    {
                        if (pCalcGkuVals.nzp_prm_tarif > 0)
                        {
                            _GetValPrm(conn_db, paramcalc, false, pCalcGkuVals.num_prm_tarif.ToString(), pCalcGkuVals.nzp_prm_tarif.ToString(), snzp_prm, out ret);
                            //MakeValTarif(conn_db, paramcalc, out ret);
                            if (!ret.result) { ExecSQL(conn_db, " Drop table tls ", false); return false; }

                            sRetVal = sRetVal.Trim() + "Тариф на 1 " + pCalcGkuVals.measure.Trim() + " = <font color='#0000FF'><b> " + ret.text.Trim() + "</b></font>";
                        }
                    }
                    break;
                    #endregion расшифровка простого тарифа
                case 2:
                    #region расшифровка тарифа найма от базовой ставки
                    {
                        _GetValPrm(conn_db, paramcalc, false, pCalcGkuVals.num_prm_tarif.ToString(), pCalcGkuVals.nzp_prm_tarif.ToString(), snzp_prm, out ret);
                        if (!ret.result) { ExecSQL(conn_db, " Drop table tls ", false); return false; }

                        decimal rTarif;
                        try
                        {
                            rTarif = Convert.ToDecimal(ret.sql_error);
                        }
                        catch (Exception)
                        {
                            rTarif = 0;
                        }
                        decimal rKoef = 0;
                        if (rTarif > 0)
                        {
                            rKoef = pCalcGkuVals.tarif / rTarif;
                        }
                        sRetVal = sRetVal.Trim() +
                        "Тариф на 1 " + pCalcGkuVals.measure.Trim() + " ( <font color='#0000FF'><b> " + pCalcGkuVals.tarif + "</b></font>) = " +
                        "Базовый тариф на 1 " + pCalcGkuVals.measure.Trim() + " (<font color='#0000FF'><b> " + rTarif + "</b></font>)" +
                        " * Коэффициент (<font color='#0000FF'><b> " + rKoef + "</b></font>) ";
                    }
                    break;
                    #endregion расшифровка тарифа найма от базовой ставки
                case 3:
                    #region расшифровка расчета ГВС от ГКал
                    {
                        _GetValPrm(conn_db, paramcalc, false, pCalcGkuVals.num_prm_tarif.ToString(), pCalcGkuVals.nzp_prm_tarif.ToString(), snzp_prm, out ret);
                        //MakeValTarif(conn_db, paramcalc, out ret);
                        if (!ret.result) { ExecSQL(conn_db, " Drop table tls ", false); return false; }

                        sRetVal = sRetVal.Trim() + "Тариф на 1 ГКал = <font color='#0000FF'><b> " + ret.text.Trim() + "</b></font>";
                        sRetVal = sRetVal.Trim() + "<br />";
                        sRetVal = sRetVal.Trim() + "<br /> Определение расхода:";
                        sRetVal = sRetVal.Trim() +
                            "Расход в ГКал (<font color='#0000FF'><b>" + pCalcGkuVals.rashod + "</b></font>) = " +
                            "Норма в ГКал на подогрев 1 куб.метра воды (<font color='#0000FF'><b> ";

                        _GetValPrm(conn_db, paramcalc, false, "1", "894", "nzp_kvar", out ret);
                        if (!ret.result) { ExecSQL(conn_db, " Drop table tls ", false); return false; }
                        if ((ret.text).Trim() != "")
                        {
                            sRetVal = sRetVal.Trim() + " " + ret.text.Trim();
                        }
                        else
                        {
                            _GetValPrm(conn_db, paramcalc, false, "2", "436", "nzp_dom", out ret);
                            if (!ret.result) { ExecSQL(conn_db, " Drop table tls ", false); return false; }
                            if ((ret.text).Trim() != "")
                            {
                                sRetVal = sRetVal.Trim() + " " + ret.text.Trim();
                            }
                            else
                            {
                                _GetValPrm(conn_db, paramcalc, false, "5", "253", "nzp", out ret);
                                if (!ret.result) { ExecSQL(conn_db, " Drop table tls ", false); return false; }

                                if ((ret.text).Trim() != "")
                                {
                                    // запрещено применять повышающий коэффициент для ГВС ?
                                    _GetValPrm(conn_db, paramcalc, false, "5", "1173", "nzp", out ret);
                                    if (!ret.result) { ExecSQL(conn_db, " Drop table tls ", false); return false; }
                                    if ((ret.text).Trim() == "")
                                    {
                                        Decimal rKoef = 1.1m;
                                        bool bUsed = false;

                                        // полотенцесушитель
                                        _GetValPrm(conn_db, paramcalc, false, "1", "59", "nzp_kvar", out ret);
                                        if (!ret.result) { ExecSQL(conn_db, " Drop table tls ", false); return false; }
                                        if ((ret.text).Trim() != "") { rKoef = rKoef + 0.1m; bUsed = true; }

                                        // Неизолированный трубопровод для горячей воды
                                        _GetValPrm(conn_db, paramcalc, false, "1", "327", "nzp_kvar", out ret);
                                        if (!ret.result) { ExecSQL(conn_db, " Drop table tls ", false); return false; }
                                        if (((ret.text).Trim() != "") && bUsed)
                                        {
                                            rKoef = rKoef + 0.1m;
                                        }
                                        else
                                        {
                                            if ((ret.text).Trim() != "")
                                            {
                                                rKoef = rKoef + 0.1m;
                                            }
                                        }
                                        sRetVal = sRetVal.Trim() + " Повышающий коэффициент= " + rKoef;
                                    }

                                }
                                else
                                {
                                    sRetVal = sRetVal.Trim() + " - ";
                                }
                            }

                        }

                        sRetVal = sRetVal.Trim() + " </b></font>) * ";

                        sRetVal = sRetVal.Trim() + " Расход ГВС в куб. метрах (<font color='#0000FF'><b> ";

                        switch (pCalcGkuVals.is_device)
                        {
                            case 1:
                                {
                                    sRetVal = sRetVal.Trim() + " Расход по ИПУ " + pCalcGkuVals.rsh1 + " </b></font>). ";
                                    sRetVal = sRetVal.Trim() + " <br /> Справочно:";
                                }
                                break;
                            case 9:
                                {
                                    sRetVal = sRetVal.Trim() + " Расход по среднему расходу ИПУ " + pCalcGkuVals.rsh1 + " </b></font>). ";
                                    sRetVal = sRetVal.Trim() + " <br /> Справочно:";
                                }
                                break;
                            default:
                                {
                                }
                                break;
                        }
                        sRetVal = sRetVal.Trim() +
                        " Расход по нормативу <br />" +
                            "Норматив на ЛС в куб.м (" + pCalcGkuVals.rash_norm + ") = Норматив на человека в куб. метрах (" + pCalcGkuVals.rash_norm_one +
                            ") * Количество жильцов (" + pCalcGkuVals.gil + ").";
                    }
                    break;
                    #endregion расшифровка расчета ГВС от ГКал
                case 4:
                    #region расшифровка - Отопление от ГКал - тариф на кв.м
                    {
                        _GetValPrm(conn_db, paramcalc, false, pCalcGkuVals.num_prm_tarif.ToString(), pCalcGkuVals.nzp_prm_tarif.ToString(), snzp_prm, out ret);
                        //MakeValTarif(conn_db, paramcalc, out ret);
                        if (!ret.result) { ExecSQL(conn_db, " Drop table tls ", false); return false; }

                        sRetVal = sRetVal.Trim() +
                            "Тариф на 1 кв.м (<font color='#0000FF'><b> " + pCalcGkuVals.tarif + "</b></font>) = " +
                            " Стоимость 1 ГКал (<font color='#0000FF'><b> " + ret.text.Trim() + "</b></font>) * " +
                            "Расход в ГКал на 1 кв.м = (<font color='#0000FF'><b> " + pCalcGkuVals.rsh1 + "</b></font>)<br />." +
                            "Расход = площадь (<font color='#0000FF'><b> " + pCalcGkuVals.squ + "</b></font> кв.м). ";

                        _GetValPrm(conn_db, paramcalc, false, "2", "723", "nzp_dom", out ret);
                        if (!ret.result) { ExecSQL(conn_db, " Drop table tls ", false); return false; }
                        if ((ret.text).Trim() != "")
                        {
                            sRetVal = sRetVal.Trim() +
                                "<br />Установлен домовой расход в ГКал на 1 кв.м отапливаемой площади = " +
                                "<font color='#0000FF'><b> " + ret.text.Trim() + "</b></font>.";
                        }
                    }
                    break;
                    #endregion расшифровка - Отопление от ГКал - тариф на кв.м

                default: { } break;
            }
            if (ret.result) ret.text = sRetVal;
            return true;
        }
        #endregion расшифровка тарифа по типу тарифа - nzp_frm_typ

        #region расшифровка расхода по типу расхода - nzp_frm_typrs
        public bool SelRashodByType(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc,
            int nzp_serv, CalcTypes.ChargeXX chargeXX, CalcGkuVals pCalcGkuVals, out Returns ret)
        {
            ret = Utils.InitReturns();

            MyDataReader reader;
            string sRetVal = "Определение расхода по параметрам расчета:<br />";
            string snzp_prm = GetNameNzp(pCalcGkuVals.num_prm_rashod);

            // расшифровка расхода по типу расхода - nzp_frm_typrs
            int iRegRashod = 1;
            switch (pCalcGkuVals.nzp_frm_typrs)
            {
                case 3: { iRegRashod = 2; } break; // по жильцам
                case 5: { iRegRashod = 3; } break; // по расходу коммунальной услуги counters_xx
                case 11: { iRegRashod = 4; } break; // разная площадь изолированных и коммунальных ЛС
                case 39: { iRegRashod = 5; } break; // канализация
                case 390: { iRegRashod = 5; } break; // канализация
                case 391: { iRegRashod = 8; } break; // канализация
                case 339: { iRegRashod = 6; } break; // канализация ХВС
                case 514: { iRegRashod = 7; } break; // отопление от ГКал
                case 1814: { iRegRashod = 7; } break; // отопление от ГКал
                case 814: { iRegRashod = -1; } break; // пусто! расход описан в протоколе для тарифа!

                default: { } break;
            }

            switch (iRegRashod)
            {
                case 1:
                    #region расшифровка расхода по услугам по параметру
                    {
                        if (pCalcGkuVals.nzp_prm_rashod > 0)
                        {
                            _GetValPrm(conn_db, paramcalc, false, pCalcGkuVals.num_prm_rashod.ToString(), pCalcGkuVals.nzp_prm_rashod.ToString(), snzp_prm, out ret);
                            if (!ret.result) { ExecSQL(conn_db, " Drop table tls ", false); return false; }

                            sRetVal = sRetVal.Trim() + "Расход = <font color='#0000FF'><b> " + ret.text.Trim() + " в (" + pCalcGkuVals.measure.Trim() + ")</b></font>";
                        }
                    }
                    break;
                    #endregion расшифровка расхода по услугам по параметру
                case 2:
                    #region расшифровка расхода по жильцам
                    {
                        /*
                        select stek,cnt2,val5,val3, nzp_gil,cnt1,cnt3,dat_s,dat_po,'' fam,'' ima,'' otch,mdy(1,1,1900) dat_rog
                        from smr36_charge_12:gil_10 g
                        where g.stek=3 and g.nzp_kvar=(select nzp_kvar from tls)
                        union all
                        select g.stek,g.cnt2,g.val5,g.val3, g.nzp_gil,g.cnt1,g.cnt3,g.dat_s,g.dat_po,k.fam,k.ima,k.otch,k.dat_rog
                        from smr36_charge_12:gil_10 g,smr36_data:kart k
                        where g.nzp_gil=k.nzp_gil and g.nzp_kvar=(select nzp_kvar from tls) and k.isactual='1'
                        */
                    }
                    break;
                    #endregion расшифровка расхода по жильцам
                case 3:
                    #region расшифровка расхода по коммунальной услуги - counters_xx
                    {
                        sRetVal = sRetVal.Trim() + " Расход в " + pCalcGkuVals.measure.Trim();


                        string snzp_serv = nzp_serv.ToString();

                        if (nzp_serv == 200) //полив
                        {
                            //кол-во соток в периоде 
                            var countHundreds = DBManager.ExecScalar<decimal>(conn_db,
                                "SELECT MAX(val_prm) FROM " + chargeXX.paramcalc.pref + sDataAliasRest + "prm_1  " +
                                "WHERE nzp_prm=262  AND nzp=" + chargeXX.paramcalc.nzp_kvar +
                                " AND is_actual<>100 " +
                                " AND " + Utils.EStrNull(pCalcGkuVals.dat_s.ToShortDateString()) + "<=dat_po " +
                                " AND " + Utils.EStrNull(pCalcGkuVals.dat_po.ToShortDateString()) + ">=dat_s");
                            //Расход на сотку (куб.м)
                            var volumeOnHundred = DBManager.ExecScalar<decimal>(conn_db,
                                "SELECT MAX(val_prm) FROM " + chargeXX.paramcalc.pref + sDataAliasRest + "prm_1  " +
                                "WHERE nzp_prm=390 AND nzp=" + chargeXX.paramcalc.nzp_kvar +
                                " AND is_actual<>100 " +
                                " AND " + Utils.EStrNull(pCalcGkuVals.dat_s.ToShortDateString()) + "<=dat_po " +
                                " AND " + Utils.EStrNull(pCalcGkuVals.dat_po.ToShortDateString()) + ">=dat_s");

                            sRetVal = sRetVal.Trim() +
                         " Расход по нормативу <br />" +
                         " Норматив на ЛС в " + pCalcGkuVals.measure.Trim() + " (" + Decimal.Round(volumeOnHundred * pCalcGkuVals.up_kf * countHundreds, 7) + ") =" +
                         " Норматив на сотку (" + volumeOnHundred + " " + pCalcGkuVals.measure.Trim() + ") *" +
                         " Количество соток (" + countHundreds + ") " +
                         ((pCalcGkuVals.up_kf != 1 && pCalcGkuVals.up_kf != 0) ?
                         "* Повышающий коэффициент (" + pCalcGkuVals.up_kf + ")." : "");

                            break;
                        }

                        if (nzp_serv == 325)
                        {
                            sRetVal = sRetVal.Trim() +
                            " Расход по нормативу <br />" +
                            " Норматив на ЛС в " + pCalcGkuVals.measure.Trim() + " (" + Decimal.Round(pCalcGkuVals.val3 * pCalcGkuVals.squ2, 7) + ") =" +
                            " Норматив на 1 кв.м (" + pCalcGkuVals.val3 + " " + pCalcGkuVals.measure.Trim() + ") *" +
                            " Отапливаемая площадь (" + pCalcGkuVals.squ2 + ") ";

                            break;
                        }

                        if (nzp_serv > 500)
                        {
                            snzp_serv = "(select nzp_serv_link from " + chargeXX.paramcalc.kernel_alias +
                                    "serv_odn where nzp_serv=" + nzp_serv + ")";
                        }
                        if ((nzp_serv == 14) || (nzp_serv == 514)) { snzp_serv = "9"; }

                        // определить - ОДН есть?
                        decimal kod_info = 0;
                        decimal norma_odn = 0;
                        decimal rashod_odn = 0;
                        decimal squ1 = 0;
                        ret = ExecRead(conn_db, out reader,
                              " Select max(kod_info) kod_info, max(kf307) norma_odn, max(dop87) dop87, max(squ1) squ1" +
                              " From " + chargeXX.counters_xx +
                              " Where " + chargeXX.where_kvar + " and nzp_serv=" + snzp_serv + " and stek=3 and nzp_type=3 "
                            , true);
                        if (ret.result)
                        {
                            try
                            {
                                if (reader.Read())
                                {
                                    if (reader["kod_info"] != DBNull.Value) kod_info = ((int)reader["kod_info"]);
                                    if (reader["norma_odn"] != DBNull.Value) norma_odn = ((decimal)reader["norma_odn"]);
                                    if (reader["dop87"] != DBNull.Value) rashod_odn = ((decimal)reader["dop87"]);
                                    if (reader["squ1"] != DBNull.Value) squ1 = ((decimal)reader["squ1"]);
                                }
                            }
                            catch (Exception ex)
                            {
                                ExecSQL(conn_db, " Drop table tls ", false);
                                ret.result = false;
                                ret.text = ex.Message;
                                return false;
                            }
                            finally
                            {
                                reader.Close();
                            }
                        }

                        if (nzp_serv < 500)
                        {
                            switch (pCalcGkuVals.is_device)
                            {
                                case 1:
                                    {
                                        sRetVal = sRetVal.Trim() + " по ИПУ  (<font color='#0000FF'><b>" + pCalcGkuVals.rashod + "</b></font>). ";
                                        sRetVal = sRetVal.Trim() + " <br /> Справочно:";
                                    }
                                    break;
                                case 9:
                                    {
                                        sRetVal = sRetVal.Trim() + " по среднему расходу ИПУ  (<font color='#0000FF'><b>" + pCalcGkuVals.rashod + "</b></font>). ";
                                        sRetVal = sRetVal.Trim() + " <br /> Справочно:";
                                    }
                                    break;

                                default: { } break;
                            }
                            sRetVal = sRetVal.Trim() +
                            " Расход по нормативу <br />" +
                            " Норматив на ЛС в " + pCalcGkuVals.measure.Trim() + " (" + Decimal.Round(pCalcGkuVals.rash_norm_one * pCalcGkuVals.gil * pCalcGkuVals.up_kf, 7) + ") =" +
                            " Норматив на человека (" + pCalcGkuVals.rash_norm_one + " " + pCalcGkuVals.measure.Trim() + ") *" +
                            " Количество жильцов (" + pCalcGkuVals.gil + ") " +
                            ((pCalcGkuVals.up_kf != 1 && pCalcGkuVals.up_kf != 0) ?
                            "* Повышающий коэффициент (" + pCalcGkuVals.up_kf + ")." : "");

                            if ((kod_info > 30) && (kod_info < 39))
                                sRetVal = sRetVal.Trim() +
                                "<br /> Начислен отрицательный ОДН:<br />" +
                                "Начисленный расход на ЛС в " + pCalcGkuVals.measure + " (" + pCalcGkuVals.rashod + ") = " +
                                "Расход на ЛС (" + pCalcGkuVals.valm + ") - " +
                                "[Рассчитанный домовой расход ОДН на 1 человека (" + norma_odn + ") * " +
                                "Количество жильцов (" + pCalcGkuVals.gil + ")].";
                        }
                        else
                        {
                            sRetVal = sRetVal.Trim() +
                                "<br /> Начислен ОДН:<br />" +
                                " Расход ОДН (<font color='#0000FF'><b> " + rashod_odn + " </b></font>) =" +
                                " Общая площадь ЛС (<font color='#0000FF'><b> " + squ1 + " </b></font> кв.м) *" +
                                " Норма расхода на 1 кв.м площади (<font color='#0000FF'><b> " + norma_odn + " </b></font>)";

                            sRetVal = sRetVal.Trim() + "<br /> Определение нормы расхода на 1 кв.м площади:";
                            sRetVal = sRetVal.Trim() + "<br />";

                            ret = ExecRead(conn_db, out reader,
                                  " Select " +
                                  " max(case when stek=3 then cnt_stage end) is_device, " +
                                  " max(case when stek=3 then kf_dpu_ls end) rashod_norm_odn, " +
                                  " max(case when stek=3 then kf_dpu_kg end) rashod_odn, " +
                                  " max(case when stek=3 then val4  end) rashod_odpu, " +
                                  " max(case when stek=3 then val3  end) rashod_odnd, " +
                                  " max(case when stek=3 then vl210 end) norma_odn_kvm, " +
                                  " max(case when stek=3 then squ1 end) pl_ls, " +
                                  " max(case when stek=3 then pu7kw end) squ_mop, " +
                                  " max(case when stek=3 then kf307f9 end) squ_dom, " +
                                  " max(case when stek=1 then val1 end) rashod_odpu, " +
                                  " max(case when stek=2 then val1 end) rashod_odpu_sr " +
                                  " From " + chargeXX.counters_xx +
                                  " Where nzp_dom in (select nzp_dom from tls) and nzp_serv=" + snzp_serv + " and stek in (1,2,3) and nzp_type=1 "
                                , true);
                            if (ret.result)
                            {

                                decimal rashod_odn_dom = 0;
                                decimal rashod_norm_odn = 0;
                                decimal rashod_odnd = 0;
                                decimal rashod_odpu = 0;
                                decimal norma_odn_kvm = 0;
                                decimal pl_ls = 0;
                                decimal squ_dom = 0;
                                decimal squ_mop = 0;
                                bool is_rashod_odpu = false;
                                bool is_rashod_odpu_sr = false;
                                int is_device = 0;
                                try
                                {
                                    if (reader.Read())
                                    {
                                        if (reader["is_device"] != DBNull.Value) is_device = ((int)reader["is_device"]);
                                        if (reader["rashod_odn"] != DBNull.Value) rashod_odn_dom = ((decimal)reader["rashod_odn"]);
                                        if (reader["rashod_norm_odn"] != DBNull.Value) rashod_norm_odn = ((decimal)reader["rashod_norm_odn"]);
                                        if (reader["rashod_odpu"] != DBNull.Value) rashod_odpu = ((decimal)reader["rashod_odpu"]);
                                        if (reader["rashod_odnd"] != DBNull.Value) rashod_odnd = ((decimal)reader["rashod_odnd"]);
                                        if (reader["norma_odn_kvm"] != DBNull.Value) norma_odn_kvm = ((decimal)reader["norma_odn_kvm"]);
                                        if (reader["pl_ls"] != DBNull.Value) pl_ls = ((decimal)reader["pl_ls"]);
                                        if (reader["squ_dom"] != DBNull.Value) squ_dom = ((decimal)reader["squ_dom"]);
                                        if (reader["squ_mop"] != DBNull.Value) squ_mop = ((decimal)reader["squ_mop"]);
                                        is_rashod_odpu = (reader["rashod_odpu"] != DBNull.Value);
                                        is_rashod_odpu_sr = (reader["rashod_odpu_sr"] != DBNull.Value);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    ExecSQL(conn_db, " Drop table tls ", false);
                                    ret.result = false;
                                    ret.text = ex.Message;
                                    return false;
                                }
                                finally
                                {
                                    reader.Close();
                                }

                                sRetVal = sRetVal.Trim() + "Норма расхода на 1 кв.м = Расход ОДН";
                                if (is_device > 0)
                                {
                                    sRetVal = sRetVal.Trim() + " по ОДПУ (или среднему расходу ОДПУ)";
                                }
                                else
                                {
                                    sRetVal = sRetVal.Trim() + " по нормативу ОДН на дом";
                                }
                                sRetVal = sRetVal.Trim() + " (<font color='#0000FF'><b> " + rashod_odn_dom + " </b></font>) / ";
                                if (Points.IsSmr)
                                {
                                    sRetVal = sRetVal.Trim() +
                                        "[Площадь дома(<font color='#0000FF'><b> " + squ_dom + " </b></font>) -" +
                                        " Площадь МОП дома(<font color='#0000FF'><b> " + squ_mop + " </b></font>)]";
                                }
                                else
                                {
                                    sRetVal = sRetVal.Trim() +
                                        "Суммарная площадь ЛС дома (<font color='#0000FF'><b> " + pl_ls + " </b></font>)";
                                }
                                sRetVal = sRetVal.Trim() + "<br /> Справочно: <br />";
                                sRetVal = sRetVal.Trim() +
                                    "Норматив ОДН (<font color='#0000FF'><b> " + rashod_norm_odn + " </b></font>) = " +
                                    "Нормативный расход на 1 кв. метр площади МОП (<font color='#0000FF'><b> " + norma_odn_kvm + " </b></font>) * " +
                                    "Площадь МОП дома (<font color='#0000FF'><b> " + squ_mop + " </b></font>).";
                                sRetVal = sRetVal.Trim() +
                                    "<br /> Расход для начисления (расход ОДПУ может быть ограничен по Пост.№344)= " + rashod_odnd;
                                if (is_device > 0)
                                {
                                    sRetVal = sRetVal.Trim() + " Расход ОДПУ= " + rashod_odpu;
                                }
                            }
                            else
                            {
                                sRetVal = sRetVal.Trim() + ret.text;
                            }
                            sRetVal = sRetVal.Trim() + "<br />";
                        }
                    }
                    break;
                    #endregion расшифровка расхода по коммунальной услуги - counters_xx
                case 4:
                    #region расшифровка расхода по услугам с кв. метров
                    {
                        _GetValPrm(conn_db, paramcalc, false, "1", "3", "nzp_kvar", out ret);
                        if (!ret.result) { ExecSQL(conn_db, " Drop table tls ", false); return false; }

                        // string sNmPrm = "nzp_prm_rash";
                        string sKommunal = "Для изолированных квартир берется площадь";
                        if ((ret.text).Trim() == "2")
                        {
                            //    sNmPrm = "nzp_prm_rash1";
                            sKommunal = "Для коммунальных квартир берется площадь";
                        }
                        sRetVal = sRetVal.Trim() + sKommunal;

                        _GetValPrm(conn_db, paramcalc, false, "1", pCalcGkuVals.nzp_prm_rashod.ToString(), "nzp_kvar", out ret);
                        if (!ret.result) { ExecSQL(conn_db, " Drop table tls ", false); return false; }

                        if ((ret.text).Trim() != "")
                        {
                            sRetVal = sRetVal.Trim() + " квартирный параметр: " + ret.text;
                        }
                    }
                    break;
                    #endregion расшифровка расхода по услугам с кв. метров
                case 5:
                    #region если канализация и расход с куб.м
                    {
                        // если канализация и расход с куб.м
                        //if ((nzp_serv == 7) && (nzp_frm_typrs == 390))
                        ret = ExecRead(conn_db, out reader,
                            " Select nzp_kvar, " +
                            " max(case when nzp_serv=6 then val1+val2 end) rashod_hv, " +
                            " max(case when nzp_serv=9 then val1+val2 end) rashod_gv  " +
                            " From " + chargeXX.counters_xx +
                            " Where " + chargeXX.where_kvar + " and nzp_serv in (6,9) and stek=3 " +
                            " group by nzp_kvar "
                            , true);
                        if (ret.result)
                        {
                            decimal rashod_hv = 0;
                            decimal rashod_gv = 0;
                            try
                            {
                                if (reader.Read())
                                {
                                    if (reader["rashod_hv"] != DBNull.Value) rashod_hv = ((decimal)reader["rashod_hv"]);
                                    if (reader["rashod_gv"] != DBNull.Value) rashod_gv = ((decimal)reader["rashod_gv"]);
                                }
                            }
                            catch (Exception ex)
                            {
                                ExecSQL(conn_db, " Drop table tls ", false);
                                ret.result = false;
                                ret.text = ex.Message;
                                return false;
                            }
                            finally
                            {
                                reader.Close();
                            }
                            sRetVal = sRetVal.Trim() +
                                      "Расход по канализации (<font color='#0000FF'><b> " + pCalcGkuVals.rashod + " </b></font>) = " +
                                      "Расход ХВС (<font color='#0000FF'><b> " + rashod_hv + " </b></font>) + " +
                                      "Расход ГВС (<font color='#0000FF'><b> " + rashod_gv + " </b></font>).";

                            if (pCalcGkuVals.up_kf > 1)
                            {
                                sRetVal += " - Расход п/к (<font color='#0000FF'><b>" + CastValue<decimal>(ExecScalar(conn_db, " select rashod from " + chargeXX.calc_gku_xx + " r where r." +
                                chargeXX.where_kvar +
                                " and exists (select 1 from " + Points.Pref + "_kernel" + tableDelimiter +
                                "serv_norm_koef snk where snk.nzp_serv = r.nzp_serv and snk.nzp_serv_link = " + nzp_serv +
                                ") " +
                                " and r.stek=3", out ret, true)) + "</b></font>)";
                            }

                            sRetVal = sRetVal.Trim() + "." + "<br />";
                        }
                    }
                    break;
                    #endregion если канализация и расход с куб.м
                case 6:
                    #region если канализация и расход с куб.м ХВС
                    {
                        // если канализация и расход с куб.м
                        //if ((nzp_serv == 7) && (nzp_frm_typrs == 390))
                        ret = ExecRead(conn_db, out reader,
                            " Select nzp_kvar, " +
                            " max(case when nzp_serv=6 then val1+val2 end) rashod_hv " +
                            " From " + chargeXX.counters_xx +
                            " Where " + chargeXX.where_kvar + " and nzp_serv=6 and stek=3 " +
                            " group by nzp_kvar "
                            , true);
                        if (ret.result)
                        {
                            decimal rashod_hv = 0;
                            try
                            {
                                if (reader.Read())
                                {
                                    if (reader["rashod_hv"] != DBNull.Value) rashod_hv = ((decimal)reader["rashod_hv"]);
                                }
                            }
                            catch (Exception ex)
                            {
                                ExecSQL(conn_db, " Drop table tls ", false);
                                ret.result = false;
                                ret.text = ex.Message;
                                return false;
                            }
                            finally
                            {
                                reader.Close();
                            }
                            sRetVal = sRetVal.Trim() +
                                      "Расход по канализации (<font color='#0000FF'><b> " + pCalcGkuVals.rashod + " </b></font>) = " +
                                      "Расход ХВС (<font color='#0000FF'><b> " + rashod_hv + " </b></font>).";

                            if (pCalcGkuVals.up_kf > 1)
                            {
                                sRetVal += " - Расход п/к (<font color='#0000FF'><b>" + CastValue<decimal>(ExecScalar(conn_db, " select rashod from " + chargeXX.calc_gku_xx + " r where r." +
                                chargeXX.where_kvar +
                                " and exists (select 1 from " + Points.Pref + "_kernel" + tableDelimiter +
                                "serv_norm_koef snk where snk.nzp_serv = r.nzp_serv and snk.nzp_serv_link = " + nzp_serv +
                                ") " +
                                " and r.stek=3", out ret, true)) + "</b></font>)";
                            }

                            sRetVal = sRetVal.Trim() + "." + "<br />";
                        }
                    }
                    break;
                    #endregion если канализация и расход с куб.м ХВС
                case 7:
                    #region отопление от ГКал
                    {
                        if (pCalcGkuVals.is_device == 1)
                        {
                            decimal val2 = 0;
                            ret = ExecRead(conn_db, out reader,
                                " Select val2 From " + chargeXX.counters_xx + 
                                " Where  nzp_serv = 8 AND stek = 3 and nzp_type = 3 and " + chargeXX.where_kvar + " and dat_charge is null", true);
                            if (ret.result)
                            {
                                try
                                {
                                    if (reader.Read())
                                    {
                                        if (reader["val2"] != DBNull.Value) val2 = ((decimal)reader["val2"]);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    ExecSQL(conn_db, " Drop table tls ", false);
                                    ret.result = false;
                                    ret.text = ex.Message;
                                    return false;
                                }
                                finally
                                {
                                    reader.Close();
                                }
                            }
                            if (val2 != 0)
                                sRetVal = sRetVal.Trim() + " Расход в " + pCalcGkuVals.measure.Trim() + " по ИПУ  (<font color='#0000FF'><b>" + val2 + "</b></font>).</br> ";
                        }
                        sRetVal = sRetVal.Trim() +
                              "Расход в ГКал (<font color='#0000FF'><b> " + pCalcGkuVals.rashod + "</b></font>) = " +
                              "Расход в ГКал на 1 кв.м = (<font color='#0000FF'><b> " + pCalcGkuVals.rsh2 +
                              "</b></font>) " +
                              " * " +
                              "Площадь (<font color='#0000FF'><b> " + pCalcGkuVals.squ + "</b></font> кв.м) ";

                        _GetValPrm(conn_db, paramcalc, false, "2", "723", "nzp_dom", out ret);
                        if (!ret.result) { ExecSQL(conn_db, " Drop table tls ", false); return false; }
                        if ((ret.text).Trim() != "")
                        {
                            sRetVal = sRetVal.Trim() +
                                "<br />Установлен домовой расход в ГКал на 1 кв.м отапливаемой площади = " +
                                "<font color='#0000FF'><b> " + ret.text.Trim() + "</b></font>" +
                                  ((pCalcGkuVals.up_kf != 1 && pCalcGkuVals.up_kf != 0) ?
                            " <font color='#0000FF'><b>* Повышающий коэффициент (" + pCalcGkuVals.up_kf + ").</b></font>" : "");
                            ;
                        }
                    }
                    break;
                    #endregion отопление от ГКал
                case 8:
                    #region если канализация и расход с куб.м -- при наличиии ИПУ на одной услуге и норм. на другой

                    {
                        int cnt_stage_hv = 0;
                        int cnt_stage_gv = 0;
                        const int Odn = 100;

                        try
                        {

                            cnt_stage_hv = CastValue<int>(ExecScalar(conn_db, "SELECT max(case when kod_info>100 then " + Odn + " else cnt_stage end)" +
                                                                              " FROM " + chargeXX.counters_xx +
                                                                   " Where " + chargeXX.where_kvar +
                                                                   " and nzp_serv in (6) and stek=3 ", out ret, true));

                            cnt_stage_gv = CastValue<int>(ExecScalar(conn_db, "SELECT max(case when kod_info>100 then " + Odn + " else cnt_stage end)" +
                                                                              " FROM " + chargeXX.counters_xx +
                                                               " Where " + chargeXX.where_kvar +
                                                               " and nzp_serv in (9) and stek=3 ", out ret, true));

                        }
                        catch (Exception ex)
                        {
                            ExecSQL(conn_db, " Drop table tls ", false);
                            ret.result = false;
                            ret.text = ex.Message;
                            return false;
                        }
                        if (cnt_stage_hv == 0 || cnt_stage_gv == 0)
                        {
                            ret = ExecRead(conn_db, out reader,
                                " Select nzp_kvar, " +
                                " max(val4) rashod_hv, " +
                                " max(val1-val4) rashod_gv  " +
                                " From " + chargeXX.counters_xx +
                                " Where " + chargeXX.where_kvar + " and nzp_serv in (7) and stek=3 " +
                                " group by nzp_kvar "
                                , true);
                        }
                        else
                        {
                            ret = ExecRead(conn_db, out reader,
                                " Select nzp_kvar, " +
                                " max(case when nzp_serv=6 then val1+val2 end) rashod_hv, " +
                                " max(case when nzp_serv=9 then val1+val2 end) rashod_gv  " +
                                " From " + chargeXX.counters_xx +
                                " Where " + chargeXX.where_kvar + " and nzp_serv in (6,9) and stek=3 " +
                                " group by nzp_kvar "
                                , true);
                        }

                        if (ret.result)
                        {
                            decimal rashod_hv = 0;
                            decimal rashod_gv = 0;
                            try
                            {
                                if (reader.Read())
                                {
                                    if (reader["rashod_hv"] != DBNull.Value)
                                        rashod_hv = ((decimal)reader["rashod_hv"]);
                                    if (reader["rashod_gv"] != DBNull.Value)
                                        rashod_gv = ((decimal)reader["rashod_gv"]);
                                }
                            }
                            catch (Exception ex)
                            {
                                ExecSQL(conn_db, " Drop table tls ", false);
                                ret.result = false;
                                ret.text = ex.Message;
                                return false;
                            }
                            finally
                            {
                                reader.Close();
                            }

                            var sHV = "";
                            switch (cnt_stage_hv)
                            {
                                case 0: sHV = "Норматив по водоотведению ХВС (<font color='#0000FF'><b> " + rashod_hv + " </b></font>) "; break;
                                case 1: sHV = "Расход ХВС по ИПУ (<font color='#0000FF'><b> " + rashod_hv + " </b></font>) "; break;
                                case 9: sHV = "Расход ХВС по ИПУ (среднее значение) (<font color='#0000FF'><b> " + rashod_hv + " </b></font>) "; break;
                                case 100: sHV = "Расход по ОДПУ (<font color='#0000FF'><b> " + rashod_hv + " </b></font>) "; break;
                            }
                            var sGV = "";
                            switch (cnt_stage_gv)
                            {
                                case 0: sGV = "Норматив по водоотведению ГВС (<font color='#0000FF'><b> " + rashod_gv + " </b></font>) "; break;
                                case 1: sGV = "Расход ГВС по ИПУ (<font color='#0000FF'><b> " + rashod_gv + " </b></font>) "; break;
                                case 9: sGV = "Расход ГВС по ИПУ (среднее значение) (<font color='#0000FF'><b> " + rashod_gv + " </b></font>) "; break;
                                case 100: sGV = "Расход по ОДПУ (<font color='#0000FF'><b> " + rashod_gv + " </b></font>) "; break;
                            }

                            sRetVal = sRetVal.Trim() +
                                      "Расход по канализации (<font color='#0000FF'><b> " + pCalcGkuVals.rashod +
                                      " </b></font>) = " + sHV + " + " + sGV + ".";
                            sRetVal = sRetVal.Trim() + "<br />";
                        }
                    }

                    break;
                    #endregion если канализация и расход с куб.м

                default: { } break;
            }
            if (ret.result) ret.text = sRetVal;
            return true;
        }
        #endregion расшифровка расхода по типу расхода - nzp_frm_typrs

        #region Формирование перечня возможных значений тарифа
        public bool MakeValTarif(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc, out Returns ret)
        //-----------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            string sBodyText = "";

            if (Points.IsSmr)
            {
                _GetValPrm(conn_db, paramcalc, true, "5", "nzp_prm_tarif_bd", "0", out ret);
                if (!ret.result) { ExecSQL(conn_db, " Drop table tls ", false); return false; }

                if ((ret.text).Trim() != "")
                {
                    sBodyText = ret.text.Trim();
                }
            }
            else
            {
                int iTmp = 0;

                _GetValPrm(conn_db, paramcalc, true, "1", "nzp_prm_tarif_ls", "nzp_kvar", out ret);
                if (!ret.result) { ExecSQL(conn_db, " Drop table tls ", false); return false; }

                if ((ret.text).Trim() != "")
                {
                    iTmp++;
                    sBodyText = sBodyText + " во " + iTmp + "-х. Квартирный параметр: " + ret.text;
                }

                _GetValPrm(conn_db, paramcalc, true, "1", "nzp_prm_tarif_lsp", "nzp_kvar", out ret);
                if (!ret.result) { ExecSQL(conn_db, " Drop table tls ", false); return false; }

                if ((ret.text).Trim() != "")
                {
                    sBodyText = sBodyText + " + " + ret.text;
                }
                if (iTmp > 0) sBodyText = sBodyText.Trim() + "<br />";

                _GetValPrm(conn_db, paramcalc, true, "2", "nzp_prm_tarif_dm", "nzp_dom", out ret);
                if (!ret.result) { ExecSQL(conn_db, " Drop table tls ", false); return false; }

                if ((ret.text).Trim() != "")
                {
                    iTmp++;
                    sBodyText = sBodyText + " во " + iTmp + "-х. Домовой параметр: " + ret.text;
                    sBodyText = sBodyText.Trim() + "<br />";
                }

                _GetValPrm(conn_db, paramcalc, true, "11", "nzp_prm_tarif_su", "nzp_supp", out ret);
                if (!ret.result) { ExecSQL(conn_db, " Drop table tls ", false); return false; }

                if ((ret.text).Trim() != "")
                {
                    iTmp++;
                    sBodyText = sBodyText + " во " + iTmp + "-х. Параметр поставщика: " + ret.text;
                    sBodyText = sBodyText.Trim() + "<br />";
                }

                _GetValPrm(conn_db, paramcalc, true, "5", "nzp_prm_tarif_bd", "0", out ret);
                if (!ret.result) { ExecSQL(conn_db, " Drop table tls ", false); return false; }

                if ((ret.text).Trim() != "")
                {
                    iTmp++;
                    sBodyText = sBodyText + " во " + iTmp + "-х. Параметр для всех ЛС: " + ret.text;
                    sBodyText = sBodyText.Trim() + "<br />";
                }
            }
            ret.text = sBodyText.Trim();
            return true;
        }
        #endregion Формирование перечня возможных значений тарифа

        #region Формирование протокола по записям calc_gku
        public Returns SelCalcGkuVals(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc, CalcTypes.ChargeXX chargeXX,
            int nzp_serv, int nzp_supp, string sqldop, out CalcGkuVals pCalcGkuVals)
        //-----------------------------------------------------------------------------
        {
            Returns ret = Utils.InitReturns();
            MyDataReader reader;
            pCalcGkuVals = InitCalcGkuVals();

            if (!TempTableInWebCashe(conn_db, chargeXX.calc_gku_xx))
            {
                return new Returns(false, "Нет данных", -1);
            }
            ret = ExecRead(conn_db, out reader,
                " Select " +
                " r.nzp_prm_tarif, r.nzp_prm_rashod, r.tarif, r.rashod, r.is_device, r.gil, r.squ, r.nzp_frm_typ, r.nzp_frm_typrs," +
                " r.dat_s, r.dat_po, r.rashod_full, r.cntd, r.cntd_mn, r.stek," +
                " r.trf1,r.trf2,r.trf3,r.trf4, r.rsh1,r.rsh2,r.rsh3, r.valm,r.dop87,r.dlt_reval, r.squ,r.gil,r.rash_norm_one,r.rashod_norm,r.rashod_source,r.up_kf" +

                " ,r.nzp_frm,  (select " + sNvlWord + "(f.name_frm,'')  from " + chargeXX.paramcalc.kernel_alias + "formuls  f where f.nzp_frm =r.nzp_frm ) as name_frm" +
                " ,(select " + sNvlWord + "(m.measure,'')" +
                  " from " + chargeXX.paramcalc.kernel_alias + "formuls  f," + chargeXX.paramcalc.kernel_alias + "s_measure m" +
                  " where f.nzp_frm =r.nzp_frm and f.nzp_measure=m.nzp_measure) as measure" +
                " ,r.nzp_prm_tarif, (select " + sNvlWord + "(p.name_prm,'')  from " + chargeXX.paramcalc.kernel_alias + "prm_name p where p.nzp_prm =r.nzp_prm_tarif ) as name_prm_tarif" +
                " , (select " + sNvlWord + "(p.prm_num,0)  from " + chargeXX.paramcalc.kernel_alias + "prm_name p where p.nzp_prm =r.nzp_prm_tarif ) as num_prm_tarif" +
                " ,r.nzp_prm_rashod, (select " + sNvlWord + "(p.name_prm,'')  from " + chargeXX.paramcalc.kernel_alias + "prm_name p where p.nzp_prm =r.nzp_prm_rashod ) as name_prm_rashod" +
                " , (select " + sNvlWord + "(p.prm_num,0)  from " + chargeXX.paramcalc.kernel_alias + "prm_name p where p.nzp_prm =r.nzp_prm_rashod ) as num_prm_rashod" +

                " , (select " + sNvlWord + "(p.squ2,0)  from " + chargeXX.counters_xx + " p where p.nzp_kvar =r.nzp_kvar AND p.nzp_serv = r.nzp_serv AND p.stek = 3 ) as squ2" +
                " , (select " + sNvlWord + "(p.val3,0)  from " + chargeXX.counters_xx + " p where p.nzp_kvar =r.nzp_kvar AND p.nzp_serv = r.nzp_serv AND p.stek = 3 ) as val3" +

                "  ,(select " + sNvlWord + "(f.nzp_prm_tarif_ls,0)  from " + chargeXX.paramcalc.kernel_alias + "formuls_opis f where f.nzp_frm =r.nzp_frm ) " +
                "  ,(select " + sNvlWord + "(f.nzp_prm_tarif_lsp,0) from " + chargeXX.paramcalc.kernel_alias + "formuls_opis f where f.nzp_frm =r.nzp_frm ) " +
                "  ,(select " + sNvlWord + "(f.nzp_prm_tarif_dm,0) from " + chargeXX.paramcalc.kernel_alias + "formuls_opis f where f.nzp_frm =r.nzp_frm ) " +
                "  ,(select " + sNvlWord + "(f.nzp_prm_tarif_su,0) from " + chargeXX.paramcalc.kernel_alias + "formuls_opis f where f.nzp_frm =r.nzp_frm ) " +
                "  ,(select " + sNvlWord + "(f.nzp_prm_tarif_bd,0) from " + chargeXX.paramcalc.kernel_alias + "formuls_opis f where f.nzp_frm =r.nzp_frm ) " +
                "  ,(select " + sNvlWord + "(f.nzp_prm_rash, 0) from " + chargeXX.paramcalc.kernel_alias + "formuls_opis f where f.nzp_frm =r.nzp_frm ) " +
                "  ,(select " + sNvlWord + "(f.nzp_prm_rash1,0) from " + chargeXX.paramcalc.kernel_alias + "formuls_opis f where f.nzp_frm =r.nzp_frm ) " +
                "  ,(select " + sNvlWord + "(f.nzp_prm_rash2,0) from " + chargeXX.paramcalc.kernel_alias + "formuls_opis f where f.nzp_frm =r.nzp_frm ) " +
                " , snk.nzp_serv nzp_serv_pk, sosn.service service_osn" +
                " From " + chargeXX.calc_gku_xx + " r" +
                " left outer join " + Points.Pref + "_kernel" + tableDelimiter + "serv_norm_koef snk " +
                "  left outer join " + Points.Pref + "_kernel" + tableDelimiter + "services sosn on sosn.nzp_serv = snk.nzp_serv_link " +
                "  on snk.nzp_serv = r.nzp_serv " +
                " Where r." + chargeXX.where_kvar +
                " and r.nzp_serv=" + nzp_serv + " and r.nzp_supp=" + nzp_supp // + " and r.nzp_frm=" + nzp_frm +
                + " " + sqldop
                //" and r.stek=3 ";
                //" and r.stek=1 order by r.dat_s ";
                , true);
            if (!ret.result) { ret.tag = 1; return ret; }

            try
            {
                while (reader.Read())
                {
                    //если не определены формула, тип получения тарифа и расхода, то в протоколе показывать нечего..
                    if (((int)reader["nzp_frm"]) == 0 && ((int)reader["nzp_frm_typ"]) == 0 && ((int)reader["nzp_frm_typrs"]) == 0) continue;

                    if (reader["tarif"] != DBNull.Value) pCalcGkuVals.tarif = ((decimal)reader["tarif"]);
                    if (reader["rashod"] != DBNull.Value) pCalcGkuVals.rashod = ((decimal)reader["rashod"]);
                    if (reader["rashod_full"] != DBNull.Value) pCalcGkuVals.rashod_full = ((decimal)reader["rashod_full"]);
                    if (reader["is_device"] != DBNull.Value) pCalcGkuVals.is_device = ((int)reader["is_device"]);

                    if (reader["nzp_frm"] != DBNull.Value) pCalcGkuVals.nzp_frm = ((int)reader["nzp_frm"]);
                    if (reader["name_frm"] != DBNull.Value) pCalcGkuVals.name_frm = ((string)reader["name_frm"]);
                    if (reader["measure"] != DBNull.Value) pCalcGkuVals.measure = ((string)reader["measure"]);

                    if (reader["nzp_frm_typ"] != DBNull.Value) pCalcGkuVals.nzp_frm_typ = ((int)reader["nzp_frm_typ"]);
                    if (reader["nzp_prm_tarif"] != DBNull.Value) pCalcGkuVals.nzp_prm_tarif = ((int)reader["nzp_prm_tarif"]);
                    if (reader["name_prm_tarif"] != DBNull.Value) pCalcGkuVals.name_prm_tarif = ((string)reader["name_prm_tarif"]);
                    if (reader["num_prm_tarif"] != DBNull.Value) pCalcGkuVals.num_prm_tarif = ((int)reader["num_prm_tarif"]);

                    if (reader["nzp_frm_typrs"] != DBNull.Value) pCalcGkuVals.nzp_frm_typrs = ((int)reader["nzp_frm_typrs"]);
                    if (reader["nzp_prm_rashod"] != DBNull.Value) pCalcGkuVals.nzp_prm_rashod = ((int)reader["nzp_prm_rashod"]);
                    if (reader["name_prm_rashod"] != DBNull.Value) pCalcGkuVals.name_prm_rashod = ((string)reader["name_prm_rashod"]);
                    if (reader["num_prm_rashod"] != DBNull.Value) pCalcGkuVals.num_prm_rashod = ((int)reader["num_prm_rashod"]);

                    if (reader["dat_s"] != DBNull.Value) pCalcGkuVals.dat_s = ((DateTime)reader["dat_s"]);
                    if (reader["dat_po"] != DBNull.Value) pCalcGkuVals.dat_po = ((DateTime)reader["dat_po"]);
                    if (reader["cntd"] != DBNull.Value) pCalcGkuVals.cntd = ((int)reader["cntd"]);
                    if (reader["cntd_mn"] != DBNull.Value) pCalcGkuVals.cntd_mn = ((int)reader["cntd_mn"]);

                    if (reader["trf1"] != DBNull.Value) pCalcGkuVals.trf1 = ((decimal)reader["trf1"]);
                    if (reader["trf2"] != DBNull.Value) pCalcGkuVals.trf2 = ((decimal)reader["trf2"]);
                    if (reader["trf3"] != DBNull.Value) pCalcGkuVals.trf3 = ((decimal)reader["trf3"]);
                    if (reader["trf4"] != DBNull.Value) pCalcGkuVals.trf4 = ((decimal)reader["trf4"]);
                    if (reader["rsh1"] != DBNull.Value) pCalcGkuVals.rsh1 = ((decimal)reader["rsh1"]);
                    if (reader["rsh2"] != DBNull.Value) pCalcGkuVals.rsh2 = ((decimal)reader["rsh2"]);
                    if (reader["rsh3"] != DBNull.Value) pCalcGkuVals.rsh3 = ((decimal)reader["rsh3"]);
                    if (reader["valm"] != DBNull.Value) pCalcGkuVals.valm = ((decimal)reader["valm"]);
                    if (reader["dop87"] != DBNull.Value) pCalcGkuVals.dop87 = ((decimal)reader["dop87"]);
                    if (reader["dlt_reval"] != DBNull.Value) pCalcGkuVals.dlt_reval = ((decimal)reader["dlt_reval"]);

                    if (reader["gil"] != DBNull.Value) pCalcGkuVals.gil = ((decimal)reader["gil"]);
                    if (reader["squ"] != DBNull.Value) pCalcGkuVals.squ = ((decimal)reader["squ"]);
                    if (reader["rash_norm_one"] != DBNull.Value) pCalcGkuVals.rash_norm_one = ((decimal)reader["rash_norm_one"]);
                    if (reader["rashod_norm"] != DBNull.Value) pCalcGkuVals.rash_norm = ((decimal)reader["rashod_norm"]);

                    if (reader["squ2"] != DBNull.Value) pCalcGkuVals.squ2 = ((decimal)reader["squ2"]);
                    if (reader["val3"] != DBNull.Value) pCalcGkuVals.val3 = ((decimal)reader["val3"]);

                    pCalcGkuVals.rashod_source = CastValue<decimal>(reader["rashod_source"]);
                    pCalcGkuVals.up_kf = CastValue<decimal>(reader["up_kf"]);

                    bool isNotStek3 = false;
                    if (reader["stek"] != DBNull.Value) isNotStek3 = (((int)reader["stek"]) != 3);

                    pCalcGkuVals.body_text = pCalcGkuVals.body_text.Trim() +
                      "</font><br />В период с " + pCalcGkuVals.dat_s.ToShortDateString() + " по " + pCalcGkuVals.dat_po.ToShortDateString();
                    pCalcGkuVals.body_text = pCalcGkuVals.body_text.Trim() +
                      " - формула расчета: <font color='#0000FF'> " + pCalcGkuVals.name_frm + "</font><br />";
                    if (isNotStek3)
                    {
                        pCalcGkuVals.body_text = pCalcGkuVals.body_text.Trim() +
                          " тариф=<font color='#0000FF'><b>" + pCalcGkuVals.tarif + "</b></font> * расход=<font color='#0000FF'><b>" + pCalcGkuVals.rashod +
                          "</b></font>";

                        if (reader["nzp_serv_pk"] != DBNull.Value)
                        {
                            pCalcGkuVals.body_text += " (разница в объемах потребления в связи с применением повышающего коэффициента, см. протокол расчета по основной услуге " +
                                (reader["service_osn"] != DBNull.Value ? "\"<font color='#0000FF'>" + reader["service_osn"].ToString().Trim() + "\"</font>" : "") + ").";
                        }
                        else
                            pCalcGkuVals.body_text += " (полный расход " + pCalcGkuVals.rashod_full + " " +
                                                      pCalcGkuVals.measure.Trim() + ")";

                        pCalcGkuVals.body_text = pCalcGkuVals.body_text.Trim() +
                          "<br />Доля для по-дневного учета расхода: количество дней в периоде расчета=<font color='#0000FF'><b>" + pCalcGkuVals.cntd +
                          "</b></font> / количество дней в месяце=<font color='#0000FF'><b>" + pCalcGkuVals.cntd_mn + "</b></font><br />";
                    }
                    // расшифровка тарифа по типу тарифа - nzp_frm_typ
                    if (reader["nzp_serv_pk"] == DBNull.Value)
                    {
                        SelTarifByType(conn_db, paramcalc, nzp_serv, chargeXX, pCalcGkuVals, out ret);
                        if (!ret.result)
                        {
                            ret.tag = 1;
                            return ret;
                        }

                        pCalcGkuVals.body_text = pCalcGkuVals.body_text.Trim() + ret.text.Trim() + "<br />";

                        SelRashodByType(conn_db, paramcalc, nzp_serv, chargeXX, pCalcGkuVals, out ret);
                        if (!ret.result)
                        {
                            ret.tag = 1;
                            return ret;
                        }

                        pCalcGkuVals.body_text = pCalcGkuVals.body_text.Trim() + ret.text.Trim() + "<br />";
                    }
                }

                string text;
                ret = SelCountersXX(conn_db, chargeXX, nzp_serv, out text);
                if (!ret.result) { if (ret.tag != -1) ret.tag = 1; return ret; }
                if (text != "")
                    pCalcGkuVals.body_text += "</br>Расход в ГКал на 1 кв.м (<font color='#0000FF'><b> " + pCalcGkuVals.rsh2 +
                               "</b></font>) = " + text;
            }
            catch (Exception ex)
            {
                ExecSQL(conn_db, " Drop table tls ", false);
                ret.result = false;
                ret.text = ex.Message;
                return ret;
            }
            finally
            {
                reader.Close();
            }

            return ret;
        }
        #endregion Формирование протокола по записям calc_gku

        #region если ОДПУ
        public Returns SelCountersXX(IDbConnection conn_db, CalcTypes.ChargeXX chargeXX,
         int nzp_serv, out string text)
        //-----------------------------------------------------------------------------
        {
            string sql = "Select "+
                            " a.name_type,  "+
                            " val3 rashod_po_odpu, "+
                            " case when kod_info = 101 then 'Суммарное количество жильцов'  "+
                            " when kod_info = 102 then 'Суммарная площадь' "+
                            " when kod_info = 103 then 'Суммарная площадь' "+
                            " when kod_info = 104 then 'Суммарное количество ЛС' end txt,  "+
                            " case when kod_info = 101 then gil1  "+
                            " when kod_info = 102 then squ1 "+
                            " when kod_info = 103 then squ1 "+
                            " when kod_info = 104 then cls1 end val "+
                            " From " + chargeXX.counters_xx + " c,  " + chargeXX.paramcalc.kernel_alias + "s_type_alg a " +
                            " Where c.nzp_serv = "+nzp_serv+" AND c.Stek = 3 AND c.nzp_type=1  "+
                            " AND c.nzp_kvar = 0 AND c.nzp_dom = (select nzp_dom from " + chargeXX.paramcalc.data_alias + "kvar where " + chargeXX.where_kvar +") " +
                            " AND c.kod_info > 100 " +
                            " AND c.kod_info = a.nzp_type_alg";
            MyDataReader reader;
            text = "";
            Returns ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result) return ret;
              
            while (reader.Read())
            {
                if (reader["rashod_po_odpu"] != DBNull.Value) text += "Расход по ОДПУ (<font color='#0000FF'><b>" + reader["rashod_po_odpu"]+"</b></font>) / ";
                if (reader["txt"] != DBNull.Value) text += reader["txt"] + " (<font color='#0000FF'><b>";
                if (reader["val"] != DBNull.Value) text += reader["val"] + "</b></font>)</br>";
                if (reader["name_type"] != DBNull.Value) text += "</br>Способ расчета: <font color='#0000FF'>" + reader["name_type"] + "</font>";
            }

            return ret;
        }
        #endregion


        #region Формирование протокола расчета
        /// <summary>
        /// Выполняет формирование протокола расчета для заданного лицевого счета,услуги,договора/поставщика
        /// </summary>
        /// <param name="conn_db">соединение с БД</param>
        /// <param name="paramcalc1">параметры месяца расчета</param>
        /// <param name="nzp_serv">код услуги</param>
        /// <param name="nzp_supp">код поставщика</param>
        /// <returns></returns>
        public bool MakeProtCalcForMonth(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc1, int nzp_serv, int nzp_supp, out Returns ret)
        {
            ret = Utils.InitReturns();

            /*
            входящие параметры:
            1. параметры расчета - ParamCalc paramcalc = new ParamCalc(nzp_kvar, 0, pref, calc_yy, calc_mm, cur_yy, cur_mm);
               paramcalc.num_ls = <num_ls>
            2. nzp_serv - код услуги
            3. nzp_supp - код поставщика
             
            выходящие параметры:
            ret.tag = 0 & ret.result = true - нормальное завершение
            ret.text = html-текст для отображения
            */
            /* пока отложим... Thread???
            //Новая культура для дат------------------------
            CultureInfo newCI = (CultureInfo)Thread.CurrentThread.CurrentCulture.Clone();
            newCI.NumberFormat.NumberDecimalSeparator = ".";
            newCI.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
            newCI.DateTimeFormat.LongTimePattern = "";
            Thread.CurrentThread.CurrentCulture = newCI;
            //Конец Новая культура для дат------------------------
            */
            if ((paramcalc1.nzp_kvar > 0) && (nzp_serv > 0) && (nzp_supp > 0))
            {
                #region выбираем начисления/тип расчета,наименования услуги/поставщика/формулы/ед.езмерения

                string[] month_names = new string[12];
                month_names[0] = "январь";
                month_names[1] = "февраль";
                month_names[2] = "март";
                month_names[3] = "апрель";
                month_names[4] = "май";
                month_names[5] = "июнь";
                month_names[6] = "июль";
                month_names[7] = "август";
                month_names[8] = "сентябрь";
                month_names[9] = "октябрь";
                month_names[10] = "ноябрь";
                month_names[11] = "декабрь";

                CalcTypes.ParamCalc paramcalc = new CalcTypes.ParamCalc(paramcalc1.nzp_kvar, 0, paramcalc1.pref, paramcalc1.calc_yy, paramcalc1.calc_mm, paramcalc1.cur_yy, paramcalc1.cur_mm);

                paramcalc.b_reval = false; //нельзя включать! - включается только в CalcRevalXX
                paramcalc.b_handl = !((paramcalc.cur_yy == paramcalc.calc_yy) && (paramcalc.cur_mm == paramcalc.calc_mm));

                paramcalc.b_report = false;
                paramcalc.b_must = false;

                CalcTypes.ChargeXX chargeXX = new CalcTypes.ChargeXX(paramcalc);

                // выбираем перечень параметров протокола для формулы/типа расчета для заголовка протокола
                string[] param_name = new string[9];
                string[] val_param = new string[9];

                param_name[0] = "calcmn";
                param_name[1] = "calcyr";
                param_name[2] = "recalc";

                param_name[3] = "service";
                param_name[4] = "name_supp";

                param_name[5] = "result";
                param_name[6] = "tarif";
                param_name[7] = "rashod";

                param_name[8] = "nedop";
                //
                val_param[0] = month_names[paramcalc.calc_mm - 1];
                val_param[1] = paramcalc.calc_yy.ToString();

                val_param[2] = "";
                if (!paramcalc.b_cur)
                {
                    val_param[2] = " (Перерасчет за " + month_names[paramcalc.cur_mm - 1] + " " + paramcalc.cur_yy + "г)";
                }
                //
                ExecSQL(conn_db, " Drop table tls ", false);

                ret = ExecSQL(conn_db,
                    " Create temp table tls " +
                    " (  nzp_dom        integer, " +
                    "    nzp_kvar       integer, " +
                    "    nzp_serv       integer, " +
                    "    service        char(100)," +
                    "    nzp_supp       integer, " +
                    "    name_supp      char(100)," +
                    "    tarif          " + sDecimalType + "(14,3) default 0.000, " +
                    "    rsum_tarif     " + sDecimalType + "(14,2) default 0.00, " +
                    "    sum_nedop      " + sDecimalType + "(14,2) default 0.00, " +
                    "    is_device      integer default 0, " +
                    "    isdel          integer default 0  " +
                    "    ) " + sUnlogTempTable
                    , true);
                if (!ret.result) { return false; }

                string p_dat_charge = " '' ";
                if (chargeXX.paramcalc.b_cur)
                {
                    p_dat_charge = "dat_charge is null";
                }
                else
                {
                    p_dat_charge = "dat_charge=" + MDY(chargeXX.paramcalc.cur_mm, 28, chargeXX.paramcalc.cur_yy);
                }

                // выбираем начисления/тип расчета по ЛС
                ret = ExecSQL(conn_db,
                    " Insert into tls " +
                    "  (nzp_dom,nzp_kvar,nzp_serv,service,nzp_supp,name_supp,tarif,rsum_tarif,sum_nedop,is_device,isdel) " +
                    " Select " +
                    "  k.nzp_dom,k.nzp_kvar," +
                    "  c.nzp_serv, (select " + sNvlWord + "(s.service,'')   from " + chargeXX.paramcalc.kernel_alias + "services s where s.nzp_serv=c.nzp_serv)," +
                    "  c.nzp_supp, (select " + sNvlWord + "(p.name_supp,'') from " + chargeXX.paramcalc.kernel_alias + "supplier p where p.nzp_supp=c.nzp_supp)," +
                    "  c.tarif,c.rsum_tarif,c.sum_nedop,c.is_device,c.isdel " +
                    " From " + chargeXX.charge_xx_ishod + " c," + chargeXX.paramcalc.data_alias + "kvar k " +
                    " Where c.nzp_kvar=k.nzp_kvar" +
                    " and c." + chargeXX.where_kvar +
                    " and c.nzp_serv=" + nzp_serv + " and c.nzp_supp=" + nzp_supp + " and c." + p_dat_charge
                    , true);
                if (!ret.result) { return false; }

                ExecSQL(conn_db, sUpdStat + " tls ", true);

                #endregion выбираем начисления/тип расчета,наименования услуги/поставщика/формулы/ед.езмерения

                #region считать выбранные начисления

                MyDataReader reader;

                ret = ExecRead(conn_db, out reader,
                    " Select * From tls t Where 1=1 "
                    , true);
                if (!ret.result) { ExecSQL(conn_db, " Drop table tls ", false); return false; }

                decimal sum_nedop = 0;
                decimal rsum_tarif = 0;
                string service = "";
                string name_supp = "";
                int is_device = 0;
                int isdel = 0;
                try
                {
                    if (reader.Read())
                    {
                        if (reader["service"] != DBNull.Value) service = ((string)reader["service"]);
                        if (reader["name_supp"] != DBNull.Value) name_supp = ((string)reader["name_supp"]);
                        if (reader["rsum_tarif"] != DBNull.Value) rsum_tarif = ((decimal)reader["rsum_tarif"]);
                        if (reader["sum_nedop"] != DBNull.Value) sum_nedop = ((decimal)reader["sum_nedop"]);
                        if (reader["is_device"] != DBNull.Value) is_device = ((int)reader["is_device"]);
                        if (reader["isdel"] != DBNull.Value) isdel = ((int)reader["isdel"]);
                    }
                }
                catch (Exception ex)
                {
                    ExecSQL(conn_db, " Drop table tls ", false);
                    ret.result = false;
                    ret.text = ex.Message;
                    return false;
                }
                finally
                {
                    reader.Close();
                }

                val_param[3] = service;
                val_param[4] = name_supp;
                val_param[5] = rsum_tarif.ToString();

                val_param[8] = "";

                #endregion считать выбранные начисления

                #region отображение недопоставки

                // отображение недопоставки если она была
                if (Math.Abs(sum_nedop) > 0)
                {
                    val_param[8] = "Рассчитана недопоставка = " + sum_nedop + ".";

                    string sKolHourNedo =
#if PG
 " ( extract(days from (n.cnts - n.cnts_del))*24 + extract(hour from (n.cnts - n.cnts_del)) ) cnt_hour," +
                    " ( extract(days from (n.cnts))*24 + extract(hour from (n.cnts)) ) cnt_nedo," +
                    " ( extract(days from (n.cnts_del))*24 + extract(hour from (n.cnts_del)) ) cnt_good ";
#else
                    chargeXX.paramcalc.data_alias + "sortnum((n.cnts - n.cnts_del)) cnt_hour," +
                    chargeXX.paramcalc.data_alias + "sortnum((n.cnts)) cnt_nedo," +  
                    chargeXX.paramcalc.data_alias + "sortnum((n.cnts_del)) cnt_good ";
#endif

                    ret = ExecRead(conn_db, out reader,
                          " Select " +
                          "  n.nzp_serv,n.nzp_kind,u.name,n.koef,n.cnts,n.cnts_del,n.tn,n.dat_s,n.dat_po,n.perc,n.kod_info," + sKolHourNedo +
                          " From " + chargeXX.calc_nedo_xx + " n," + chargeXX.paramcalc.data_alias + "upg_s_kind_nedop u " +
                          " Where n.nzp_kind=u.nzp_kind and u.kod_kind=1 and n." + chargeXX.where_kvar + " and n.nzp_serv=" + nzp_serv +
                          " order by 1,2 "
                        , true);
                    if (!ret.result) { ExecSQL(conn_db, " Drop table tls ", false); return false; }

                    try
                    {
                        while (reader.Read())
                        {
                            string nm = "";
                            if (reader["name"] != DBNull.Value) nm = ((string)reader["name"]);
                            decimal koef = 0;
                            if (reader["koef"] != DBNull.Value) koef = ((decimal)reader["koef"]);
                            string cnts = "0";
                            if (reader["cnt_hour"] != DBNull.Value) cnts = Convert.ToString(reader["cnt_hour"]);
                            string cnt_nedo = "0";
                            if (reader["cnt_nedo"] != DBNull.Value) cnt_nedo = Convert.ToString(reader["cnt_nedo"]);
                            string cnt_good = "0";
                            if (reader["cnt_good"] != DBNull.Value) cnt_good = Convert.ToString(reader["cnt_good"]);
                            decimal perc = 0;
                            if (reader["perc"] != DBNull.Value) perc = ((decimal)reader["perc"]);

                            val_param[8] = val_param[8].Trim() + "<br />" +
                                " Тип недопоставки:" + nm + "." + "<br />" +
                                " Доля возврата = " + koef + "." + "<br />" +
                                " % возврата = " + perc + "." + "<br />" +
                                " Количество часов недопоставки для учета (" + cnts + ") =" +
                                " Количество часов недопоставки  (" + cnt_nedo + ") - " +
                                " Допустимый перерыв (" + cnt_good + ")." + "<br />" +
                                " Период: с " +
                                Convert.ToString(reader["dat_s"]).Trim() +
                                " по " +
                                Convert.ToString(reader["dat_po"]).Trim() + "." +
                                "";
                        }
                    }
                    catch (Exception ex)
                    {
                        ExecSQL(conn_db, " Drop table tls ", false);
                        ret.result = false;
                        ret.text = ex.Message;
                        return false;
                    }
                    finally
                    {
                        reader.Close();
                    }

                    val_param[8] = val_param[8].Trim() + "<br />";
                }
                #endregion отображение недопоставки

                // выбираем перечень параметров протокола для формулы/типа расчета для содержания/тела протокола
                string sBodyText = "<br />";

                CalcGkuVals aCalcGkuVals = new CalcGkuVals();
                int nzp_frm = 0;

                bool showTulaPeni = false;
                // получить режим расчета пени
                if (DBManager.ExecScalar<bool>(conn_db, "SELECT count(1)>0 FROM " + Points.Pref + sKernelAliasRest + "peni_settings WHERE nzp_peni_serv=" + nzp_serv))
                {
                    string sql = " select count(*) from " + paramcalc1.pref + "_data" + DBManager.tableDelimiter + "prm_10 " +
                        " where nzp_prm = 1382 " +
                        "   and is_actual = 1 " +
                        "   and " + DBManager.MDY(paramcalc1.calc_mm, 1, paramcalc1.calc_yy) + " between dat_s and dat_po " +
                        "   and trim(val_prm) = '2'";

                    Object obj = ExecScalar(conn_db, sql, out ret, true);
                    if (!ret.result) { ret.tag = 1; return false; }

                    int cnt = Convert.ToInt32(obj);

                    if (cnt > 0) showTulaPeni = true;
                }

                if (showTulaPeni)
                {
                    var peniProt = new PeniProtocol();
                    ret = peniProt.GetCalcProtocol(conn_db, paramcalc1, nzp_supp,nzp_serv);
                    if (!ret.result) { ret.tag = 1; return false; }

                    sBodyText = ret.text;
                }
                else
                {
                    // выборка тарифа и расхода
                    ret = SelCalcGkuVals(conn_db, paramcalc, chargeXX, nzp_serv, nzp_supp, "and r.stek=3", out aCalcGkuVals);
                    if (!ret.result) { if (ret.tag != -1) ret.tag = 1; return false; }
                    nzp_frm = aCalcGkuVals.nzp_frm;
                    string sOneServ = aCalcGkuVals.body_text;

                    #region выборка тарифа и расхода -- comment
                    /*
                string sqlcalc =
                    " Select " +
                    " r.nzp_prm_tarif, r.nzp_prm_rashod, r.tarif, r.rashod, r.gil, r.squ, r.nzp_frm_typ, r.nzp_frm_typrs," +
                    " r.dat_s, r.dat_po " +
                    " From " + chargeXX.calc_gku_xx + " r" +
                    " Where r." + chargeXX.where_kvar +
                    " and r.nzp_serv=" + nzp_serv + " and r.nzp_supp=" + nzp_supp; // + " and r.nzp_frm=" + nzp_frm +
                    
                    //" and r.stek=1 order by r.dat_s ";

                // для всех типов расчета - выборка тарифа и расхода
                decimal tarif = 0;
                decimal rashod = 0;
                ret = ExecRead(conn_db, out reader,
                      sqlcalc + " and r.stek=3 "
                    , true);
                if (!ret.result) { ret.tag = 1; return false; }

                try
                {
                    if (reader.Read())
                    {
                        if (reader["tarif"] != DBNull.Value) tarif = ((decimal)reader["tarif"]);
                        if (reader["rashod"] != DBNull.Value) rashod = ((decimal)reader["rashod"]);
                        if (reader["nzp_frm_typ"] != DBNull.Value) nzp_frm_typ = ((int)reader["nzp_frm_typ"]);
                        if (reader["nzp_frm_typrs"] != DBNull.Value) nzp_frm_typrs = ((int)reader["nzp_frm_typrs"]);
                    }
                }
                catch (Exception ex)
                {
                    ExecSQL(conn_db, " Drop table tls ", false);
                    ret.result = false;
                    ret.text = ex.Message;
                    return false;
                }
                finally
                {
                    reader.Close();
                }
                */
                    #endregion выборка тарифа и расхода

                    val_param[6] = aCalcGkuVals.tarif.ToString();
                    val_param[7] = aCalcGkuVals.rashod.ToString();
                    if (aCalcGkuVals.measure.Trim() != "") { val_param[7] = val_param[7] + " (" + aCalcGkuVals.measure.Trim() + ")"; }


                    // расшифровка тарифа/расхода для по-дневного расчета
                    ret = SelCalcGkuVals(conn_db, paramcalc, chargeXX, nzp_serv, nzp_supp, "and r.stek=1 order by r.dat_s", out aCalcGkuVals);
                    if (!ret.result) { if (ret.tag != -1) ret.tag = 1; return false; }

                    if (aCalcGkuVals.nzp_frm > 0)
                    {
                        sBodyText = sBodyText.Trim() + "Прошел по-дневной расчет:<br />";
                        sBodyText = sBodyText.Trim() + aCalcGkuVals.body_text.Trim() + "<br />";
                    }
                    else
                    {
                        sBodyText = sBodyText.Trim() + sOneServ.Trim() + "<br />";
                    }


                }
                
                #region расшифровка тарифа/расхода для по-дневного расчета -- comment
                /*
                ret = ExecRead(conn_db, out reader,
                      " Select " +
                       " r.nzp_prm_tarif, r.nzp_prm_rashod, r.tarif, r.rashod, r.gil, r.squ, r.nzp_frm_typ, r.nzp_frm_typrs," +
                       " r.dat_s, r.dat_po " +
                      " From " + chargeXX.calc_gku_xx + " r" +
                      " Where r." + chargeXX.where_kvar +
                      " and r.stek=1 and r.nzp_serv=" + nzp_serv + " and r.nzp_supp=" + nzp_supp + " and r.nzp_frm=" + nzp_frm +
                      " order by r.dat_s "
                    , true);
                if (!ret.result) { ret.tag = 1; return false; }

                bool bIsPoD = false;
                string sIsPoD = "";
                DateTime dat_s = new DateTime();
                DateTime dat_po = new DateTime();
                try
                {
                    while (reader.Read())
                    {
                        bIsPoD = true;
                        if (reader["tarif"]  != DBNull.Value) tarif  = ((decimal)reader["tarif"]);
                        if (reader["rashod"] != DBNull.Value) rashod = ((decimal)reader["rashod"]);
                        if (reader["dat_s"] != DBNull.Value) dat_s   = ((DateTime)reader["dat_s"]);
                        if (reader["dat_po"] != DBNull.Value) dat_po = ((DateTime)reader["dat_po"]);

                        sIsPoD = sIsPoD.Trim() + dat_s.ToShortDateString() + " " + dat_po.ToShortDateString() + " " + tarif + " " + rashod + " " + "<br />";
                    }
                }
                catch (Exception ex)
                {
                    ExecSQL(conn_db, " Drop table tls ", false);
                    ret.result = false;
                    ret.text = ex.Message;
                    return false;
                }
                finally
                {
                    reader.Close();
                }

                if (bIsPoD && (sIsPoD.Trim()!=""))
                {
                    sBodyText = sBodyText.Trim() + "По-дневной расчет:<br />";
                    //                             "123456789|123456789|1234567890|1234567890"
                    sBodyText = sBodyText.Trim() + " Дата с  | Дата по | ТарифПД  | РасходПД " + "<br />";
                    sBodyText = sBodyText.Trim() + sIsPoD + "<br />";
                }
                */

                #endregion расшифровка тарифа/расхода для по-дневного расчета

                //bool bUseTypRS = true;

                // расшифровка тарифа по типу тарифа - nzp_frm_typ
                //SelTarifByType(conn_db, paramcalc, nzp_frm_typ, nzp_serv, measure, rashod, is_device, chargeXX, out ret);
                //if (!ret.result) { ret.tag = 1; return false; }

                #region 1
                /*
                // расшифровка тарифа по типу тарифа - nzp_frm_typ
                switch (nzp_frm_typ)
                {
                    case 1: // 2,12,26,40,101
                        #region расшифровка простого тарифа
                        {
                            sBodyText = sBodyText.Trim() + "Определение тарифа:";
                            sBodyText = sBodyText.Trim() + "<br />";

                            MakeValTarif(conn_db, paramcalc, out ret);
                            if (!ret.result) { ExecSQL(conn_db, " Drop table tls ", false); return false; }
                            sBodyText = sBodyText.Trim() + "Тариф на 1 " + measure + " = <font color='#0000FF'><b> " + ret.text.Trim() + "</b></font>";
                            sBodyText = sBodyText.Trim() + "<br />";
                        }
                        // если канализация и расход с куб.м
                        if ((nzp_serv == 7) && (nzp_frm_typrs == 390))
                        {
                            ret = ExecRead(conn_db, out reader,
                                  " Select nzp_kvar, " +
                                  " max(case when nzp_serv=6 then val1+val2 end) rashod_hv, " +
                                  " max(case when nzp_serv=9 then val1+val2 end) rashod_gv  " +
                                  " From " + chargeXX.counters_xx +
                                  " Where " + chargeXX.where_kvar + " and nzp_serv in (6,9) and stek=3 " +
                                  " group by nzp_kvar "
                                , true);
                            if (ret.result)
                            {
                                decimal rashod_hv = 0;
                                decimal rashod_gv = 0;
                                try
                                {
                                    if (reader.Read())
                                    {
                                        if (reader["rashod_hv"] != DBNull.Value) rashod_hv = ((decimal)reader["rashod_hv"]);
                                        if (reader["rashod_gv"] != DBNull.Value) rashod_gv = ((decimal)reader["rashod_gv"]);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    ExecSQL(conn_db, " Drop table tls ", false);
                                    ret.result = false;
                                    ret.text = ex.Message;
                                    return false;
                                }
                                finally
                                {
                                    reader.Close();
                                }
                                sBodyText = sBodyText.Trim() + "<br /> Определение расхода:";
                                sBodyText = sBodyText.Trim() + "<br />";

                                sBodyText = sBodyText.Trim() +
                                    "Расход по канализации (<font color='#0000FF'><b> " + rashod + " </b></font>) = " +
                                    "Расход ХВС (<font color='#0000FF'><b> " + rashod_hv + " </b></font>) + " +
                                    "Расход ГВС (<font color='#0000FF'><b> " + rashod_gv + " </b></font>).";

                                sBodyText = sBodyText.Trim() + "<br />";
                            }
                            bUseTypRS = false;
                        }
                        break;
                        #endregion расшифровка простого тарифа
                    case 1140:
                        #region расшифровка расчета ГВС от ГКал
                        {
                            sBodyText = sBodyText.Trim() + "Определение тарифа:";
                            sBodyText = sBodyText.Trim() + "<br />";

                            MakeValTarif(conn_db, paramcalc, out ret);
                            if (!ret.result) { ExecSQL(conn_db, " Drop table tls ", false); return false; }

                            sBodyText = sBodyText.Trim() + "Тариф на 1 ГКал = <font color='#0000FF'><b> " + ret.text.Trim() + "</b></font>";
                            sBodyText = sBodyText.Trim() + "<br />";
                            sBodyText = sBodyText.Trim() + "<br /> Определение расхода:";
                            sBodyText = sBodyText.Trim() + "<br />";
                            sBodyText = sBodyText.Trim() +
                                "Расход в ГКал (<font color='#0000FF'><b>" + rashod + "</b></font>) = " +
                                "Норма в ГКал на подогрев 1 куб.метра воды (<font color='#0000FF'><b> ";

                            _GetValPrm(conn_db, paramcalc, false, "2", "436", "nzp_dom", out ret);
                            if (!ret.result) { ExecSQL(conn_db, " Drop table tls ", false); return false; }

                            if ((ret.text).Trim() != "")
                            {
                                sBodyText = sBodyText.Trim() + " " + ret.text.Trim();
                            }
                            else
                            {
                                sBodyText = sBodyText.Trim() + "-";
                            }
                            sBodyText = sBodyText.Trim() + " </b></font>) * ";

                            sBodyText = sBodyText.Trim() + " Расход ГВС в куб. метрах (<font color='#0000FF'><b> ";

                            if (is_device > 0)
                            {
                                sBodyText = sBodyText.Trim() + " Расход по ИПУ ";
                            }
                            else
                            {
                                sBodyText = sBodyText.Trim() + " Расход по нормативу ";
                            }

                            ret = ExecRead(conn_db, out reader,
                                  " Select nzp_kvar,nzp_serv, " +
                                  " max(case when stek=30 then val1 end) rashod_norm, " +
                                  " max(case when stek=30 and cnt1>0 then val1/cnt1 end) rashod_norm_ch, " +
                                  " max(case when stek=30 then cnt1 end) kol_gil, " +
                                  " max(case when stek=1  then val1 end) rashod_ipu, " +
                                  " max(case when stek=2  then val1 end) rashod_ipu_sr " +
                                  " From " + chargeXX.counters_xx +
                                  " Where " + chargeXX.where_kvar + " and nzp_serv=" + nzp_serv + " and stek in (1,2,30) " +
                                  " group by nzp_kvar,nzp_serv "
                                , true);
                            if (ret.result)
                            {
                                decimal rashod_norm = 0;
                                decimal rashod_norm_ch = 0;
                                decimal rashod_ipu = 0;
                                decimal rashod_ipu_sr = 0;
                                int kol_gil = 0;
                                bool is_rashod_ipu = false;
                                try
                                {
                                    if (reader.Read())
                                    {
                                        if (reader["rashod_norm"] != DBNull.Value)
                                            rashod_norm = ((decimal) reader["rashod_norm"]);
                                        if (reader["rashod_norm_ch"] != DBNull.Value)
                                            rashod_norm_ch = ((decimal) reader["rashod_norm_ch"]);
                                        if (reader["rashod_ipu"] != DBNull.Value)
                                        {
                                            rashod_ipu = ((decimal) reader["rashod_ipu"]);
                                            is_rashod_ipu = true;
                                        }
                                        ;
                                        if (reader["rashod_ipu_sr"] != DBNull.Value)
                                            rashod_ipu_sr = ((decimal) reader["rashod_ipu_sr"]);
                                        if (reader["kol_gil"] != DBNull.Value) kol_gil = ((int) reader["kol_gil"]);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    ExecSQL(conn_db, " Drop table tls ", false);
                                    ret.result = false;
                                    ret.text = ex.Message;
                                    return false;
                                }
                                finally
                                {
                                    reader.Close();
                                }

                                if (is_device > 0)
                                {
                                    if (is_rashod_ipu)
                                    {
                                        sBodyText = sBodyText.Trim() + "= " + rashod_ipu.ToString();
                                    }
                                    else
                                    {
                                        sBodyText = sBodyText.Trim() + "(среднему расходу)= " + rashod_ipu_sr.ToString(); ;
                                    }
                                }
                                sBodyText = sBodyText.Trim() + " </b></font>). " +
                                    "<br /> Справочно:<br />" +
                                    "Норматив на ЛС в куб.м (" + rashod_norm.ToString() + ") = Норматив на человека в куб. метрах (" + rashod_norm_ch.ToString() +
                                    ") * Количество жильцов (" + kol_gil + ").";
                            }
                            else
                            {
                                sBodyText = sBodyText.Trim() + " -- </b></font>) ";
                            }
                            bUseTypRS = false;
                        }
                        break;
                        #endregion расшифровка расчета ГВС от ГКал
                    default:
                        {
                        }
                        break;
                }
                */
                #endregion 1

                //sBodyText = sBodyText.Trim() + ret.text.Trim() + "<br />";

                //SelRashodByType(conn_db, paramcalc, nzp_frm_typrs, nzp_serv, measure, rashod, is_device, chargeXX, out ret);
                //if (!ret.result) { ret.tag = 1; return false; }

                //sBodyText = sBodyText.Trim() + ret.text.Trim() + "<br />";

                #region 2
                /*
                if (bUseTypRS) // расход расписан при расшифровки тарифа!
                {
                    // расшифровка расхода по типу расхода - nzp_frm_typrs
                    switch (nzp_frm_typrs)
                    {
                        case 3: // по жильцам
                            #region расшифровка расхода по жильцам
                            {
                                
                                //select stek,cnt2,val5,val3, nzp_gil,cnt1,cnt3,dat_s,dat_po,'' fam,'' ima,'' otch,mdy(1,1,1900) dat_rog
                                //from smr36_charge_12:gil_10 g
                                //where g.stek=3 and g.nzp_kvar=(select nzp_kvar from tls)
                                //union all
                                //select g.stek,g.cnt2,g.val5,g.val3, g.nzp_gil,g.cnt1,g.cnt3,g.dat_s,g.dat_po,k.fam,k.ima,k.otch,k.dat_rog
                                //from smr36_charge_12:gil_10 g,smr36_data:kart k
                                //where g.nzp_gil=k.nzp_gil and g.nzp_kvar=(select nzp_kvar from tls) and k.isactual='1'
                                
                            }
                            break;
                            #endregion расшифровка расхода по жильцам

                        case 5: // по расходу коммунальной услуги counters_xx
                            #region расшифровка расхода по коммунальной услуги - counters_xx
                            {
                                sBodyText = sBodyText.Trim() + "<br /> Определение расхода:";
                                sBodyText = sBodyText.Trim() + "<br />";
                                sBodyText = sBodyText.Trim() + " Расход в " + measure + " ";

                                string snzp_serv = nzp_serv.ToString();
                                if (nzp_serv < 500)
                                {
                                    if (is_device > 0)
                                    {
                                        sBodyText = sBodyText.Trim() + " Показания ИПУ ";
                                    }
                                    else
                                    {
                                        sBodyText = sBodyText.Trim() + " Норматив ";
                                    }
                                    if (nzp_serv == 14)
                                    {
                                        snzp_serv = "9";
                                    }
                                }
                                else
                                {
                                    snzp_serv = "(select nzp_serv_link from " + chargeXX.paramcalc.kernel_alias + "serv_odn where nzp_serv=" + nzp_serv + ")";
                                }

                                ret = ExecRead(conn_db, out reader,
                                      " Select nzp_kvar,nzp_serv, " +
                                      " max(case when stek=30 then val1 end) rashod_norm, " +
                                      " max(case when stek=30 and cnt1>0 then val1/cnt1 end) rashod_norm_ch, " +
                                      " max(case when stek=30 then cnt1 end) kol_gil, " +
                                      " max(case when stek=3  then dop87 end) rashod_odn, " +
                                      " max(case when stek=3  then kf307 end) norma_odn, " +
                                      " max(case when stek=3  then squ1 end) pl_odn, " +
                                      " max(case when stek=1  then val1 end) rashod_ipu, " +
                                      " max(case when stek=2  then val1 end) rashod_ipu_sr " +
                                      " From " + chargeXX.counters_xx +
                                      " Where " + chargeXX.where_kvar + " and nzp_serv=" + snzp_serv + " and stek in (1,2,30,3) and nzp_type=3 " +
                                      " group by nzp_kvar,nzp_serv "
                                    , true);
                                if (ret.result)
                                {
                                    decimal rashod_norm = 0;
                                    decimal rashod_norm_ch = 0;
                                    decimal rashod_ipu = 0;
                                    decimal rashod_odn = 0;
                                    decimal norma_odn = 0;
                                    decimal pl_odn = 0;
                                    decimal rashod_ipu_sr = 0;
                                    int kol_gil = 0;
                                    bool is_rashod_ipu = false;
                                    try
                                    {
                                        if (reader.Read())
                                        {
                                            if (reader["rashod_norm"] != DBNull.Value)
                                                rashod_norm = ((decimal) reader["rashod_norm"]);
                                            if (reader["rashod_norm_ch"] != DBNull.Value)
                                                rashod_norm_ch = ((decimal) reader["rashod_norm_ch"]);
                                            if (reader["rashod_ipu"] != DBNull.Value)
                                            {
                                                rashod_ipu = ((decimal) reader["rashod_ipu"]);
                                                is_rashod_ipu = true;
                                            }
                                            ;
                                            if (reader["rashod_ipu_sr"] != DBNull.Value)
                                                rashod_ipu_sr = ((decimal) reader["rashod_ipu_sr"]);
                                            if (reader["kol_gil"] != DBNull.Value) kol_gil = ((int) reader["kol_gil"]);
                                            if (reader["rashod_odn"] != DBNull.Value)
                                                rashod_odn = ((decimal) reader["rashod_odn"]);
                                            if (reader["norma_odn"] != DBNull.Value)
                                                norma_odn = ((decimal) reader["norma_odn"]);
                                            if (reader["pl_odn"] != DBNull.Value) pl_odn = ((decimal) reader["pl_odn"]);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        ExecSQL(conn_db, " Drop table tls ", false);
                                        ret.result = false;
                                        ret.text = ex.Message;
                                        return false;
                                    }
                                    finally
                                    {
                                        reader.Close();
                                    }

                                    if (nzp_serv < 500)
                                    {
                                        if ((is_device > 0) || (rashod_ipu > 0) || (rashod_ipu_sr > 0))
                                        {
                                            if (is_device == 0)
                                            {
                                                sBodyText = sBodyText.Trim() + " - по ИПУ!";
                                            }
                                            sBodyText = sBodyText.Trim() + "(<font color='#0000FF'><b>";
                                            if (is_rashod_ipu)
                                            {
                                                sBodyText = sBodyText.Trim() + "= " + rashod_ipu.ToString();
                                            }
                                            else
                                            {
                                                sBodyText = sBodyText.Trim() + "(среднему расходу)= " + rashod_ipu_sr.ToString(); ;
                                            }
                                            sBodyText = sBodyText.Trim() + " </b></font>). ";
                                        }
                                        sBodyText = sBodyText.Trim() +
                                            "<br /> Справочно:<br />" +
                                            "Норматив на ЛС в " + measure + " (" + rashod_norm.ToString() + ") = Норматив на человека в " + measure + " (" + rashod_norm_ch.ToString() +
                                            ") * Количество жильцов (" + kol_gil + ").";
                                    }
                                    else
                                    {
                                        sBodyText = sBodyText.Trim() + " (<font color='#0000FF'><b> " + rashod_odn.ToString() + " </b></font>) = " +
                                            "Общая площадь ЛС (<font color='#0000FF'><b> " + pl_odn.ToString() + " </b></font> кв.м) * " +
                                            "Норма расхода на 1 кв.м площади (<font color='#0000FF'><b> " + norma_odn.ToString() + " </b></font>)";

                                        sBodyText = sBodyText.Trim() + "<br /> Определение нормы расхода на 1 кв.м площади:";
                                        sBodyText = sBodyText.Trim() + "<br />";

                                        ret = ExecRead(conn_db, out reader,
                                              " Select nzp_kvar,nzp_serv, " +
                                              " max(case when stek=3  then (case when cnt_stage=0 then val3-val4 else rvirt-val4 end) end) rashod_norm_odn, " +
                                              " max(case when stek=3  then val3-val4 end) rashod_odnd, " +
                                              " max(case when stek=3  then vl210 end) norma_odn_kvm, " +
                                              " max(case when stek=3  then pu7kw end) pl_ls, " +
                                              " max(case when stek=354 then kf307f9-pu7kw end) squ_mop, " +
                                              " max(case when stek=354 then kf307f9 end) squ_dom, " +
                                              " max(case when stek=1  then val1 end) rashod_odpu, " +
                                              " max(case when stek=2  then val1 end) rashod_odpu_sr " +
                                              " From " + chargeXX.counters_xx +
                                              " Where nzp_dom in (select nzp_dom from tls) and nzp_serv=" + snzp_serv + " and stek in (1,2,3,354) and nzp_type=1 " +
                                              " group by nzp_kvar,nzp_serv "
                                            , true);
                                        if (ret.result)
                                        {

                                            decimal rashod_norm_odn = 0;
                                            decimal rashod_odnd = 0;
                                            decimal norma_odn_kvm = 0;
                                            decimal pl_ls = 0;
                                            decimal squ_dom = 0;
                                            decimal squ_mop = 0;
                                            decimal rashod_odpu = 0;
                                            decimal rashod_odpu_sr = 0;
                                            bool is_rashod_odpu = false;
                                            try
                                            {
                                                if (reader.Read())
                                                {
                                                    if (reader["rashod_norm_odn"] != DBNull.Value)
                                                        rashod_norm_odn = ((decimal) reader["rashod_norm_odn"]);
                                                    if (reader["rashod_odnd"] != DBNull.Value)
                                                        rashod_odnd = ((decimal) reader["rashod_odnd"]);
                                                    if (reader["norma_odn_kvm"] != DBNull.Value)
                                                        norma_odn_kvm = ((decimal) reader["norma_odn_kvm"]);
                                                    if (reader["pl_ls"] != DBNull.Value)
                                                        pl_ls = ((decimal) reader["pl_ls"]);
                                                    if (reader["squ_dom"] != DBNull.Value)
                                                        squ_dom = ((decimal) reader["squ_dom"]);
                                                    if (reader["squ_mop"] != DBNull.Value)
                                                        squ_mop = ((decimal) reader["squ_mop"]);
                                                    if (reader["rashod_odpu"] != DBNull.Value)
                                                    {
                                                        rashod_odpu = ((decimal) reader["rashod_odpu"]);
                                                        is_rashod_odpu = true;
                                                    }
                                                    ;
                                                    if (reader["rashod_odpu_sr"] != DBNull.Value)
                                                    {
                                                        rashod_odpu_sr = ((decimal) reader["rashod_odpu_sr"]);
                                                        is_rashod_odpu = true;
                                                    }
                                                    ;
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                ExecSQL(conn_db, " Drop table tls ", false);
                                                ret.result = false;
                                                ret.text = ex.Message;
                                                return false;
                                            }
                                            finally
                                            {
                                                reader.Close();
                                            }

                                            sBodyText = sBodyText.Trim() + "Норма расхода на 1 кв.м = Расход ОДН";
                                            if (is_rashod_odpu)
                                            {
                                                sBodyText = sBodyText.Trim() + " по ОДПУ";
                                            }
                                            else
                                            {
                                                sBodyText = sBodyText.Trim() + " по нормативу";
                                            }
                                            sBodyText = sBodyText.Trim() +
                                                " (<font color='#0000FF'><b> " + rashod_odnd.ToString() + " </b></font>) / " +
                                                "Площадь дома(<font color='#0000FF'><b> " + squ_dom.ToString() + " </b></font>) - " +
                                                "Площадь МОП дома(<font color='#0000FF'><b> " + squ_mop.ToString() + " </b></font>)";

                                            sBodyText = sBodyText.Trim() + "<br /> Справочно: <br />";
                                            sBodyText = sBodyText.Trim() +
                                                "Норматив ОДН (<font color='#0000FF'><b> " + rashod_norm_odn.ToString() + " </b></font>) = " +
                                                "Нормативный расход на 1 кв. метр площади МОП (<font color='#0000FF'><b> " + norma_odn_kvm.ToString() + " </b></font>) * " +
                                                "Площадь МОП дома (<font color='#0000FF'><b> " + squ_mop.ToString() + " </b></font>).";

                                        }
                                        else
                                        {
                                            sBodyText = sBodyText.Trim() + ret.text;
                                        }
                                        sBodyText = sBodyText.Trim() + "<br />";
                                    }
                                }
                                else
                                {
                                    sBodyText = sBodyText.Trim() + (ret.text).Trim() + "<br />";
                                }
                            }
                            break;
                            #endregion расшифровка расхода по коммунальной услуги - counters_xx

                        case 11:
                            #region расшифровка расхода по услугам с кв. метров
                            {
                                sBodyText = sBodyText.Trim() + "Определение расхода:";
                                sBodyText = sBodyText.Trim() + "<br />";

                                _GetValPrm(conn_db, paramcalc, false, "1", "3", "nzp_kvar", out ret);
                                if (!ret.result) { ExecSQL(conn_db, " Drop table tls ", false); return false; }

                                string sNmPrm = "nzp_prm_rash";
                                string sKommunal = "Для изолированных квартир берется";
                                if ((ret.text).Trim() == "2")
                                {
                                    sNmPrm = "nzp_prm_rash1";
                                    sKommunal = "Для коммунальных квартир берется";
                                }
                                sBodyText = sBodyText.Trim() + sKommunal;

                                _GetValPrm(conn_db, paramcalc, true, "1", sNmPrm, "nzp_kvar", out ret);
                                if (!ret.result) { ExecSQL(conn_db, " Drop table tls ", false); return false; }

                                if ((ret.text).Trim() != "")
                                {
                                    sBodyText = sBodyText.Trim() + " квартирный параметр: " + ret.text;
                                }

                            }
                            break;
                            #endregion расшифровка расхода по услугам с кв. метров
                        default:
                            {
                            }
                            break;
                    }
                }
                */
                #endregion 2

                #region заголовок html-файла

                // выбираем заголовок html-файла
                string sHeaderHtml = GetDescrFrm(conn_db, chargeXX, -1, out ret);
                if (!ret.result) { ExecSQL(conn_db, " Drop table tls ", false); return false; }

                // выбираем "охвосток" html-файла
                string sFooterHtml = GetDescrFrm(conn_db, chargeXX, -2, out ret);
                if (!ret.result) { ExecSQL(conn_db, " Drop table tls ", false); return false; }

                // выбираем текст протокола для формулы/типа расчета
                string sBodyHtml = GetDescrFrm(conn_db, chargeXX, nzp_frm, out ret);
                if (!ret.result) { ExecSQL(conn_db, " Drop table tls ", false); return false; }
                //if (sBodyHtml.Length <= 0) { sBodyHtml = "Описание расчета не найдено."; }

                // заполнение значений параметров в шаблоне заголовка html-файла 
                sHeaderHtml = ReplFrmValues(sHeaderHtml, param_name, val_param);

                #endregion заголовок html-файла

                #region формируем html-файл
                // заполнение значений параметров в шаблоне тела html-файла 
                sBodyHtml = ReplFrmValues(sBodyHtml, param_name, val_param);

                var sNedoText = val_param[8] != "" ? val_param[8] : string.Empty;
                // формируем html-файл для отображения 
                ret.text = sHeaderHtml + sBodyHtml + sBodyText + sNedoText + sFooterHtml;

                ExecSQL(conn_db, " Drop table tls ", false);
                #endregion формируем html-файл
            }
            else
            {
                ret.result = false;
                ret.tag = 2;
                ret.text = "Неопределены параметры для вывода протокола расчета. (" + paramcalc1.nzp_kvar + "/" + nzp_serv + "/" + nzp_supp + ")";
                MonitorLog.WriteLog(ret.text, MonitorLog.typelog.Info, 1, 2, true);
            }

            return true;
        }

        #endregion Формирование протокола расчета

        #region Формирование протокола расчета для пени
        #endregion

    }

    public class PeniProtocol : DataBaseHead
    {
        private string FirstPeni { get; set; }
        private string PeniDebtUp { get; set; }
        private string PeniDebtDown { get; set; }
        private string OverPayments { get; set; }
        private string CalcProvodki { get; set; }

        //временная таблица с проводками
        private string TempTableProvs { get; set; }

        private CalcTypes.ParamCalc _calc;
        private int _nzpSupp;
        private IDbConnection _connDB;
        private int _nzpWp;
        private string _pref;
        private int _numLs;
        private DateTime _calcMonth;
        private int _nzpKvar;
        private bool withProvCurMonth;

        public Returns GetCalcProtocol(IDbConnection conn_db, CalcTypes.ParamCalc calc, int nzp_supp, int _nzpServ)
        {
            Returns ret = new Returns(true);

            _calc = calc;
            _nzpSupp = nzp_supp;
            _connDB = conn_db;
            _calcMonth = new DateTime(_calc.calc_yy, _calc.calc_mm, 1);
            _nzpKvar = calc.nzp_kvar;
            _pref = calc.pref;
            //1455|Учет проводок для пени в текущем расчетном месяце|||bool||10||||
            withProvCurMonth = DBManager.ExecScalar<bool>(conn_db, " SELECT CASE WHEN trim(val_prm)='1' THEN true ELSE false END" +
                                                          " FROM " + _calc.pref + sDataAliasRest + "prm_10 n " +
                                                          " WHERE is_actual<>100 AND" + Utils.EStrNull(_calcMonth.ToShortDateString()) +
                                                          " BETWEEN n.dat_s AND n.dat_po AND nzp_prm=1455");

            try
            {
                // получить код  локального банка и номер лицевого счета
                ret = GetNzpWpAndNumLs();
                if (!ret.result) throw new Exception(ret.text);

                // получить общую сумму пени
                decimal sum_peni = 0;
                ret = GetTotalSumPeni(_nzpServ, out sum_peni);
                if (!ret.result) throw new Exception(ret.text);

                string htmlBody = "Сумма пени: <font color='#0000FF'>" + sum_peni.ToString("0.00") + "</font><br>";

                //если услуга пени на этот договор закрыта - не отображаем протокол
                if (ExecScalar<bool>(conn_db, "SELECT count(*)=0 FROM " + _pref + sDataAliasRest + "tarif " +
                                              "WHERE nzp_serv=" + _nzpServ + " AND nzp_kvar=" + _nzpKvar + " AND nzp_supp=" + _nzpSupp + " AND is_actual<>100" +
                                              " AND " + _calcMonth.ToShortDateStringWithQuote() +
                                              " BETWEEN dat_s AND dat_po", out ret, true))
                {
                    return ret;
                }

                // получить учтенные в расчете пени проводки
                ret = GetCalcProvodki();
                if (!ret.result) throw new Exception(ret.text);

                // получить расшифровку сумм пени
                ret = GetPeniDebtUp();
                if (!ret.result) throw new Exception(ret.text);

                // получить расшифровку сумм перерасчетов
                ret = GetPeniDebtDown();
                if (!ret.result) throw new Exception(ret.text);

                // получить расшифровку сумм снижения долга
                ret = GetOverPayments();
                if (!ret.result) throw new Exception(ret.text);

                #region Собираем текст протокола

                if (PeniDebtUp != "")
                {
                    htmlBody +=
                      "<p>Пени = (Тек.долг-Снижение) * Пени (системная настройка) * Кол-во учтенных дней / 100 </p>";
                    htmlBody += "<b>Расшифровка сумм пени:</b><br>" + PeniDebtUp;
                }
                if (PeniDebtDown != "")
                {
                    htmlBody += "<br><b>Расшифровка сумм перерасчетов:</b><br>" + PeniDebtDown;
                }
                if (CalcProvodki != "")
                {
                    htmlBody += "<br><b>Проводки:</b><br>" + CalcProvodki;
                }
                if (OverPayments != "")
                {
                    htmlBody += "<br><b>Расшифровка сумм снижения долга:</b><br>" + OverPayments;
                }
                #endregion


                ret.text = htmlBody;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                return ret;
            }

            return ret;
        }

        private Returns GetNzpWpAndNumLs()
        {
            Returns ret = new Returns(true);
            _nzpWp = 0;
            IDataReader reader = null;

            try
            {
                string sql = " select nzp_wp, num_ls from " + Points.Pref + "_data" + DBManager.tableDelimiter + "kvar where nzp_kvar = " + _calc.nzp_kvar;
                ret = ExecRead(_connDB, out reader, sql, true);
                if (!ret.result) throw new Exception(ret.text);

                if (reader.Read())
                {
                    _nzpWp = Convert.ToInt32(reader["nzp_wp"]);
                    _numLs = Convert.ToInt32(reader["num_ls"]);
                }
                else
                {
                    throw new Exception("Не удалось определить параметры ЛС");
                }

            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                return ret;
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                    reader.Dispose();
                }
            }

            return ret;
        }

        private string ColorFontReval(string text, bool negative, bool isReval)
        {
            return isReval ? (negative ? "<font color='#0000FF'>" : "<font color='#CC0033'>") + text + "</font> " : text;
        }

        private Returns GetTotalSumPeni(int nzp_serv, out decimal sum_peni)
        {
            Returns ret = new Returns(true);
            sum_peni = 0;

            try
            {
                string sql = " select tarif as sum_peni " +
                    " from " + _calc.pref + "_charge_" + (_calc.calc_yy % 100).ToString("00") + DBManager.tableDelimiter + "charge_" + _calc.calc_mm.ToString("00") +
                    " where nzp_kvar = " + _calc.nzp_kvar +
                    "   and nzp_serv = " + nzp_serv +
                    "   and nzp_supp = " + _nzpSupp;

                Object obj = ExecScalar(_connDB, sql, out ret, true);
                if (!ret.result) throw new Exception(ret.text);

                sum_peni = Convert.ToDecimal(obj);
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                return ret;
            }

            return ret;
        }


        private Returns GetPeniDebtUp()
        {
            Returns ret = new Returns(true);
            IDataReader reader = null;
            string sql = "";
            try
            {
                //локальная таблица причин отмены  начисления пени
                var localTablePeniOff = _pref + "_charge_" + (_calcMonth.Year - 2000).ToString("00") + tableDelimiter +
                                      "peni_off_" + _calcMonth.Year + _calcMonth.Month.ToString("00") + "_" + _nzpWp;

                var tableCancelationPeni = "LEFT OUTER JOIN " + localTablePeniOff + " o ON o.peni_debt_id=d.id";
                var cancelationId = "o.peni_off_id";
                if (!TempTableInWebCashe(_connDB, localTablePeniOff))
                {
                    tableCancelationPeni = String.Empty;
                    cancelationId = "null::int peni_off_id";
                }

                //todo переделать
                ExecSQL(_connDB, "drop table t_peni_debt", false);
                sql = " CREATE TEMP TABLE t_peni_debt as " +
                      " SELECT d.nzp_supp, d.date_from, d.date_to, d.sum_debt, d.over_payments, d.sum_peni, d.date_calc, d.cnt_days,d.cnt_days_with_prm," +
                      " d.nzp_serv, s.service_small, " + cancelationId + " ," +
                      " 0::numeric as charges, 0::numeric as payments, d.sum_debt as start_debt,d.type_period," +
                      " (CASE WHEN type_period=2 THEN 0" +
                        " WHEN type_period in (-1,1) THEN (SELECT MAX(p.val_prm::numeric(10,5)) FROM " + _pref + sDataAliasRest + "prm_10 p" +
                        " WHERE p.nzp_prm = 85 and p.is_actual<>100 AND p.dat_s <=d.date_to AND p.dat_po >= d.date_from)" +
                        " WHEN type_period=0 THEN (SELECT MAX(p.val_prm::numeric(10,5)) FROM " + _pref + sDataAliasRest + "prm_10 p" +
                        " WHERE p.nzp_prm = 2119 and p.is_actual<>100 AND p.dat_s <=d.date_to AND p.dat_po >= d.date_from)" +
                        " END )::numeric(10,5) as peni_rate" +
                      " FROM " + _pref + sDataAliasRest + "peni_debt d " + tableCancelationPeni + "," +
                      _pref + sKernelAliasRest + "services s " +
                      " WHERE d.nzp_kvar = " + _nzpKvar +
                      "   AND d.nzp_serv= s.nzp_serv " +
                      "   AND d.nzp_supp = " + _nzpSupp +
                      "   AND d.nzp_wp = " + _nzpWp +
                      "   AND d.peni_calc = true" +
                    // не отключено начисление пени за этот интервал в Картотека-Операции-Не начислять пени
                      "   AND s_peni_type_debt_id=" + (int)s_peni_type_debt.IncDebt +
                      "   AND d.date_calc = " + Utils.EStrNull(_calcMonth.ToShortDateString());
                ret = ExecSQL(_connDB, sql);
                if (!ret.result)
                {
                    throw new Exception(ret.text);
                }

                var tableGroupedProvs = "temp_group_provs_" + DateTime.Now.Ticks;
                sql = "CREATE TEMP TABLE " + tableGroupedProvs + " AS " +
                      " SELECT p.nzp_serv,p.nzp_supp," +
                      " p.date_obligation + INTERVAL '1 DAY' as date_obligation, " +
                      " SUM(p.rsum_tarif) as rsum_tarif," +
                      " SUM(p.sum_nedop) as sum_nedop," +
                      " SUM(p.sum_reval) as sum_reval," +
                      " SUM(p.sum_prih) as sum_prih" +
                      " FROM  " + TempTableProvs + " p " +
                      " GROUP BY 1,2,3";
                ret = ExecSQL(_connDB, sql, true);
                if (!ret.result)
                {
                    throw new Exception(ret.text);
                }

                ExecSQL(_connDB, "DROP TABLE " + TempTableProvs, false);



                //проставляем начисления и оплаты в периоды задолженностей
                //а также восстанавливаем долг на начало периода(до применения проводок в этом периоде)
                sql = " UPDATE t_peni_debt d " +
                      " SET charges = p.rsum_tarif + p.sum_nedop+p.sum_reval," +
                      " payments =p.sum_prih," +
                      " start_debt = d.start_debt - (p.rsum_tarif + p.sum_nedop+p.sum_reval) + p.sum_prih " +
                      " FROM " + tableGroupedProvs + " p WHERE p.nzp_serv=d.nzp_serv AND p.nzp_supp=d.nzp_supp  " +
                      " AND p.date_obligation BETWEEN d.date_from AND d.date_to-INTERVAL '1 DAY'";
                ret = ExecSQL(_connDB, sql);
                if (!ret.result)
                {
                    throw new Exception(ret.text);
                }


                sql = "SELECT * FROM t_peni_debt  ORDER BY date_from,service_small ";
                ret = ExecRead(_connDB, out reader, sql, true);
                if (!ret.result) throw new Exception(ret.text);

                string messageString = "";

                var sum_peni_all = 0m;
                var existCancelPeni = false;
                while (reader.Read())
                {
                    var cnt_days_with_prm = CastValue<int>(reader["cnt_days_with_prm"]);
                    if (!existCancelPeni)
                    {
                        existCancelPeni = cnt_days_with_prm == 0;
                    }


                    messageString +=
                        "<tr>" +
                        "<td>" + CastValue<string>(reader["service_small"]) + "</td>" +
                        "<td>" +
                        (reader["date_from"] != DBNull.Value ? ((DateTime)reader["date_from"]).ToShortDateString() : "") +
                        " - " +
                        (reader["date_to"] != DBNull.Value
                            ? ((DateTime)reader["date_to"]).AddDays(-1).ToShortDateString()
                            : "") + "</td>" +
                        "<td align='right'>" + CastValue<decimal>(reader["charges"]).ToString("0.00") + "</td>" +
                        "<td align='right'>" + CastValue<decimal>(reader["payments"]).ToString("0.00") + "</td>" +
                        "<td align='right'>" + CastValue<decimal>(reader["sum_debt"]).ToString("0.00") + "</td>" +
                        "<td align='right'>" + CastValue<decimal>(reader["over_payments"]) + "</td>" +
                        "<td align='right'>" + CastValue<decimal>(reader["peni_rate"]) + "</td>" +
                        "<td align='right'>" + GetNameTypePeriodDebt(CastValue<int>(reader["type_period"])) + "</td>" +
                        "<td align='right'>" + (cnt_days_with_prm == 0
                            ? cnt_days_with_prm + " <font color='#FA021F'>(*" + CastValue<int>(reader["peni_off_id"]) +
                              ")</font>"
                            : cnt_days_with_prm + "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;") + "</td>" +
                        "<td align='right'>" + CastValue<decimal>(reader["sum_peni"]) + "</td>" +
                        "</tr>";
                    sum_peni_all += CastValue<decimal>(reader["sum_peni"]);
                }
                if (messageString != "")
                {
                    //итого по рассчитанным пени(есть еще первичные)
                    messageString += "<tr><td></br><b>Итого:<font color='#0000FF'>" + sum_peni_all.ToString("0.00") + "</font></b> </td></tr>";
                }
                if (messageString != "")
                {
                    messageString = "<table style='width: 100%;'>" +
                        "<tr>" +
                        "<td>Услуга</td>" +
                        "<td>Период</td>" +
                        "<td align='right'>Начислено</td>" +
                        "<td align='right'>Оплата</td>" +
                        "<td align='right'>Тек.долг</td>" +
                        "<td align='right'>Снижено</td>" +
                        "<td align='right'>%</td>" +
                        "<td align='right'>Период</td>" +
                        "<td align='right'>Учтенные дни</td>" +
                        "<td align='right'>Пени</td>" +
                        "</tr>" +
                        messageString +
                        "</table>";
                }

                if (existCancelPeni) //если есть отмена начисления пени показать 
                {
                    messageString += "<table>" +
                    " <tr> </br><font color='#FA021F'>*2</font> - Отмена начисления пени за период</tr>" +
                  //  " <tr> </br> <font color='#FA021F'>*3</font> - Отмена начисления пени при входящем сальдо меньшем или равном нулю</tr>" +
                    "</table>";
                }

                PeniDebtUp = messageString;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                return ret;
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                    reader.Dispose();
                }
            }

            return ret;
        }
        /// <summary>
        /// Получить число дней до даты обязательств для банка данных
        /// если параметр на банк не выставлен, то определяем значение параметра= 10 дней
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="pref"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        public int GetCountDayToDateObligation(string pref, DateTime CalcDate)
        {
            var ret = Utils.InitReturns();
            var res = 0;
            var sql = "SELECT max(val_prm) FROM " + pref + sDataAliasRest +
                      "prm_10 WHERE nzp_prm=1375 and is_actual<>100 " +
                      " and " + Utils.EStrNull(CalcDate.ToShortDateString()) + " between dat_s and dat_po ";
            res = CastValue<int>(ExecScalar(_connDB, sql, out ret, true));
            if (!ret.result) throw new Exception("Ошибка получения данных для протокола расчета");
            return res == 0 ? 10 : res;
        }

        private string GetNameTypePeriodDebt(int type)
        {
            switch (type)
            {
                case  -1:
                    return "1/300 ставки(c.)";

                case 0:
                    return "1/130 ставки";

                case 1:
                    return "1/300 ставки";

                case 2:
                    return "Без пеней";
            }
            return "";
        }

        private Returns GetPeniDebtDown()
        {
            Returns ret = new Returns(true);
            IDataReader reader = null;
            string sql = "";
            try
            {
                ExecSQL(_connDB, "drop table t_peni_debt", false);
                sql = " create temp table t_peni_debt as " +
                      " select d.date_from, d.date_to, sum(d.sum_debt) as sum_debt, sum(d.over_payments) over_payments, sum(d.sum_peni) sum_peni, max(d.cnt_days) cnt_days, " +
                       " max(d.cnt_days_with_prm) cnt_days_with_prm," +
                      " d.nzp_serv, max(s.service_small) service_small " +
                      " from " + _pref + sDataAliasRest + "peni_debt d,  " + _pref + sKernelAliasRest + "services s  " +
                      " where d.nzp_kvar = " + _nzpKvar +
                      " and d.nzp_serv= s.nzp_serv " +
                    "   and d.nzp_supp = " + _nzpSupp +
                    "   and d.nzp_wp = " + _nzpWp +
                      "   and s_peni_type_debt_id=" + (int)s_peni_type_debt.DelDebt +
                      "   and d.peni_calc = true" +
                    // не отключено начисление пени за этот интервал в Картотека-Операции-Не начислять пени
                    "   and d.date_calc = " + Utils.EStrNull(_calcMonth.ToShortDateString()) +
                      "  group by d.nzp_supp,d.date_from, d.date_to,d.nzp_serv";
                ret = ExecSQL(_connDB, sql);
                if (!ret.result) throw new Exception(ret.text);

                sql = "SELECT * FROM t_peni_debt  ORDER BY date_from,service_small ";
                ret = ExecRead(_connDB, out reader, sql, true);
                if (!ret.result) throw new Exception(ret.text);

                string messageString = "";

                var sum_peni_all = 0m;
                while (reader.Read())
                {
                    messageString +=
                        "<tr>" +
                        "<td>" + CastValue<string>(reader["service_small"]) + "</td>" +
                        "<td>" + (reader["date_from"] != DBNull.Value ? ((DateTime)reader["date_from"]).ToShortDateString() : "") +
                        " - " + (reader["date_to"] != DBNull.Value ? ((DateTime)reader["date_to"]).AddDays(-1).ToShortDateString() : "") + "</td>" +
                        "<td align='right'>" + (reader["sum_peni"] != DBNull.Value ? ((Decimal)reader["sum_peni"]).ToString("0.00") : "") + "</td>" +
                        "<td align='right'>" + (reader["sum_debt"] != DBNull.Value ? ((Decimal)reader["sum_debt"]).ToString("0.00") : "") + "</td>" +
                        "<td align='right'>" + (reader["over_payments"] != DBNull.Value ? ((Decimal)reader["over_payments"]).ToString("0.00") : "") + "</td>" +
                        "<td align='right'>" + (reader["cnt_days"] != DBNull.Value ? ((int)reader["cnt_days"]).ToString("0") : "") + "</td>" +
                        "<td align='right'>" + (reader["cnt_days_with_prm"] != DBNull.Value ? ((int)reader["cnt_days_with_prm"]).ToString("0") : "") + "</td>" +
                        "</tr>";
                    sum_peni_all += CastValue<decimal>(reader["sum_peni"]);
                }

                if (messageString != "")
                {
                    messageString = "<table style='width: 1000px;'>" +
                        "<tr>" +
                        "<td>Услуга</td>" +
                        "<td>Период</td>" +
                        "<td align='right'>Пени</td>" +
                        "<td align='right'>Перерасчет</td>" +
                        "<td align='right'>Снижено</td>" +
                        "<td align='right'>Дней долга</td>" +
                        "<td align='right'>Учтенных дней</td>" +
                        "</tr>" +
                        messageString +
                        //итого по рассчитанным пени(есть еще первичные)
                         "<tr><td></br><b>Итого:<font color='#0000FF'>" + sum_peni_all.ToString("0.00") + "</font></b> </td></tr>" +
                        "</table>";
                }

                PeniDebtDown = messageString;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                return ret;
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                    reader.Dispose();
                }
            }

            return ret;
        }

        private Returns GetOverPayments()
        {
            Returns ret = new Returns(true);
            IDataReader reader = null;

            try
            {
                ExecSQL(_connDB, "drop table temp_over_pay", false);

                string sql = "create temp table temp_over_pay (" +
                    " nzp_supp  integer, " +
                    " name_supp varchar(200), " +
                    " nzp_serv  integer, " +
                    " service varchar(200), " +
                    " date_from date, " +
                    " date_to date, " +
                    " over_payments " + DBManager.sDecimalType + "(15,2) " + ")";

                ret = ExecSQL(_connDB, sql);
                if (!ret.result) throw new Exception(ret.text);

                sql = " insert into temp_over_pay " +
                    " (nzp_supp,nzp_serv, date_from, date_to,over_payments ) " +
                    " select a.nzp_supp,a.nzp_serv, a.date_from,date_to, " +
                    " sum(sum_debt) as over_payments " +
                    " from " + _pref + "_data" + DBManager.tableDelimiter + "peni_debt a " +
                    " where a.nzp_kvar = " + _nzpKvar +
                    " and a.nzp_wp = " + _nzpWp +
                    " and a.date_calc = " + Utils.EStrNull(_calcMonth.ToShortDateString()) +
                    " and a.s_peni_type_debt_id=1 and sum_debt<0" +
                    " group by 1,2,3,4 ";
                ret = ExecSQL(_connDB, sql);
                if (!ret.result) throw new Exception(ret.text);


                sql = " update temp_over_pay t set " +
              " name_supp = (select s.name_supp from " + Points.Pref + "_kernel" + DBManager.tableDelimiter + "supplier s where s.nzp_supp = t.nzp_supp) ";
                ret = ExecSQL(_connDB, sql);
                if (!ret.result) throw new Exception(ret.text);

                sql = " update temp_over_pay t set " +
               " service = (select s.service from " + Points.Pref + "_kernel" + DBManager.tableDelimiter + "services s where s.nzp_serv = t.nzp_serv) ";
                ret = ExecSQL(_connDB, sql);
                if (!ret.result) throw new Exception(ret.text);

                sql = " select * from temp_over_pay " +
                      " order by  name_supp,service,date_from,date_to ";
                ret = ExecRead(_connDB, out reader, sql, true);
                if (!ret.result) throw new Exception(ret.text);

                string messageString = "";

                while (reader.Read())
                {
                    messageString +=
                        "<tr>" +
                        "<td>" + CastValue<string>(reader["name_supp"]).Trim() + "</td>" +
                        "<td>" + CastValue<string>(reader["service"]).Trim() + "</td>" +
                        "<td>" + CastValue<DateTime>(reader["date_from"]).ToShortDateString() + " - " + CastValue<DateTime>(reader["date_to"]).ToShortDateString() + "</td>" +
                        "<td align='right'>" + (reader["over_payments"] != DBNull.Value ? ((Decimal)reader["over_payments"]).ToString("0.00") : "") + "</td>" +
                        "</tr>";
                }

                if (messageString != "")
                {
                    messageString = "<table style='width: 800px;'>" +
                        "<tr>" +
                        "<td>Договор</td>" +
                        "<td>Услуга</td>" +
                        "<td>Период</td>" +
                        "<td  align='right'>Переплата</td>" +
                        "</tr>" +
                        messageString +
                        "</table>";
                }

                OverPayments = messageString;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                return ret;
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                    reader.Dispose();
                }
            }

            return ret;
        }

       
        private Returns GetCalcProvodki()
        {
            var ret = new Returns(true);
            IDataReader reader = null;

            TempTableProvs = "temp_table_prov_" + DateTime.Now.Ticks;
            try
            {
                //todo
                string sql = " CREATE TEMP TABLE " + TempTableProvs + " AS " +
                             " select DISTINCT ON (a.date_obligation, service,a.id)" +
                             " a.nzp_serv,a.nzp_supp, b.type_prov, a.rsum_tarif, a.sum_prih, a.sum_nedop, a.sum_reval, a.date_obligation, " +
                             " a.date_prov,d.peni_calc, trim(s.service_small) service, d.type_period, a.s_prov_types_id " +
                             " FROM " + _pref + "_data" + DBManager.tableDelimiter + "peni_provodki a, " +
                             Points.Pref + "_kernel" + DBManager.tableDelimiter + "s_prov_types b, " +
                             _pref + "_data" + DBManager.tableDelimiter + "peni_debt d" +
                             " LEFT OUTER JOIN " + _pref + sKernelAliasRest + "services s ON s.nzp_serv=d.nzp_serv, " +
                             _pref + "_data" + DBManager.tableDelimiter + "peni_provodki_refs r" +
                             " WHERE a.s_prov_types_id = b.id " +
                             "   AND a.nzp_kvar = " + _nzpKvar +
                             "   AND a.nzp_wp = " + _nzpWp +
                             "   AND a.nzp_supp = " + _nzpSupp +
                             "   AND a.date_obligation < " +
                             Utils.EStrNull((withProvCurMonth ?
                             _calcMonth.AddMonths(1) :
                             _calcMonth).ToShortDateString()) +
                             "   AND a.nzp_wp=r.nzp_wp" +
                             "   AND a.id=r.peni_provodki_id" +
                             "   AND a.date_obligation = r.date_obligation " +
                    //если считаем пени и за текущий месяц, то покажем проводки
                             "   AND d.nzp_kvar=a.nzp_kvar " +
                             "   AND d.id=r.peni_debt_id " +
                             "   AND d.date_calc = " + Utils.EStrNull(_calcMonth.ToShortDateString()) +
                             "   AND r.date_calc=d.date_calc " +
                             " ORDER BY a.date_obligation, service,a.id ";
                ret = ExecSQL(_connDB, sql, true);
                if (!ret.result) throw new Exception(ret.text);

                ret = ExecRead(_connDB, out reader, "SELECT * FROM " + TempTableProvs, true);
                if (!ret.result) throw new Exception(ret.text);

                string messageString = "";
                var countDays = GetCountDayToDateObligation(_calc.pref, _calcMonth);
                while (reader.Read())
                {
                    var dateProv = CastValue<DateTime>(reader["date_prov"]);
                    var dateObligation = CastValue<DateTime>(reader["date_obligation"]);
                    //отрицательный перерасчет с датой обязательств в прошлом
                    var isReval = ((s_prov_types)CastValue<int>(reader["s_prov_types_id"]) == s_prov_types.Reval);
                    var negativeReval = isReval && CastValue<decimal>(reader["sum_reval"]) < 0 && dateObligation < dateProv;
                    messageString +=

                        "<td>" + ColorFontReval(CastValue<string>(reader["service"]).Trim(), negativeReval, isReval) + "</td>" +
                        "<td>" + ColorFontReval((reader["type_prov"] != DBNull.Value ? ((string)reader["type_prov"]).Trim() : ""), negativeReval, isReval) + "</td>" +
                        "<td>" + ColorFontReval(dateObligation.ToShortDateString(), negativeReval, isReval) + "</td>" +
                        "<td>" +
                        ColorFontReval((negativeReval
                        ? dateObligation.AddDays(-1 * countDays).ToShortDateString() : //вычисляем дату проводки(где она по идее должна быть)
                        dateProv.ToShortDateString()), negativeReval, isReval) + //иначе пишем настоящую дату
                        "</td>" +
                        "<td align='right'>" + ColorFontReval(CastValue<decimal>(reader["rsum_tarif"]).ToString("0.00"), negativeReval, isReval) + "</td>" +
                        "<td align='right'>" + ColorFontReval(CastValue<decimal>(reader["sum_prih"]).ToString("0.00"), negativeReval, isReval) + "</td>" +
                        "<td align='right'>" + ColorFontReval(CastValue<decimal>(reader["sum_nedop"]).ToString("0.00"), negativeReval, isReval) + "</td>" +
                        "<td align='right'>" + ColorFontReval(CastValue<decimal>(reader["sum_reval"]).ToString("0.00"), negativeReval, isReval) + "</td>" +
                        "<td align='right'>" + ColorFontReval((CastValue<bool>(reader["peni_calc"]) ? "Да" : "Нет"), negativeReval, isReval) + "</td>" +
                        "</tr>";
                }

                if (messageString != "")
                {
                    messageString = "<table style='width: 900px;'>" +
                        "<tr>" +
                        "<td>Услуга</td>" +
                        "<td>Проводка</td>" +
                        "<td>Дата обязательств</td>" +
                        "<td>Месяц</td>" +
                        "<td align='right'>Начислено</td>" +
                        "<td align='right'>Оплата</td>" +
                        "<td align='right'>Недоп</td>" +
                        "<td align='right'>Перерасч</td>" +
                        "<td align='right'>Учтено</td>" +
                        "</tr>" +
                        messageString +
                        "</table>";
                }

                CalcProvodki = messageString;
            }
            catch (Exception ex)
            {
                ret.result = false;
                ret.text = ex.Message;
                return ret;
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                    reader.Dispose();
                }
            }

            return ret;
        }
    }

}
