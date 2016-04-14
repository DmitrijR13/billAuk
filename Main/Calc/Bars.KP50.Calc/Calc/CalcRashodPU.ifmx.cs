// Подсчет расходов

#region Подключаемые модули

using System;
using System.Data;

using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.IFMX.Kernel.source.CommonType;

#endregion Подключаемые модули

#region здесь производится подсчет расходов

namespace STCLINE.KP50.DataBase
{

    //здась находятся классы для подсчета расходов
    public partial class DbCalcCharge : DataBaseHead
    {
        #region Функция пересоздания временных таблиц по префиксам Cnt_NotCond
        [Obsolete]
        //--------------------------------------------------------------------------------
        private void Drop_NotCond(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc)
        //--------------------------------------------------------------------------------
        {
            ExecSQL(conn_db, " Drop table aid_i" + paramcalc.pref, false);
            ExecSQL(conn_db, " Drop table aid_d" + paramcalc.pref, false);
            ExecSQL(conn_db, " Drop table aid_dclose", false);
            ExecSQL(conn_db, " Drop table aid_dclose1", false);
        }


        [Obsolete("Функция определения периодов валидности заменена на GetValidPeriodsForCounters")]
        bool Cnt_NotCond(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc/*Rashod rashod*/, byte recreate, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            //recreate = 1 - значит надо пересоздать!
            //recreate = 0 - не надо!
            //recreate = 2 - удалить!

            MyDataReader reader;
            ret = ExecRead(conn_db, out reader,
            " Select * From aid_i" + paramcalc.pref
                , false);
            if (!ret.result)
            {
                recreate = 1; //по-любому надо пересоздать!
            }
            else
            {
                try
                {
                    if (reader.Read() && recreate == 0) return true;
                }
                finally
                {
                    reader.Close();
                }
            }

            Drop_NotCond(conn_db, paramcalc);

            //команда удаления временных таблиц!!!
            if (recreate == 2)
            {
                //написать код перебора префиксов!!!
                return true;
            }

            ret = ExecSQL(conn_db,
                " Create temp table aid_i" + paramcalc.pref +
                " ( nzp_key serial not null, " +
                "   nzp_counter integer, " +
                "   dat_s  date, " +
                "   dat_po date  " +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { Drop_NotCond(conn_db, paramcalc); return false; }

            ExecSQL(conn_db, " Create unique index ix_aid33_" + paramcalc.pref + " on aid_i" + paramcalc.pref + " (nzp_key) ", true);
            ExecSQL(conn_db, " Create        index ix_aid44_" + paramcalc.pref + " on aid_i" + paramcalc.pref + " (nzp_counter, dat_s, dat_po) ", true);
            ExecSQL(conn_db, sUpdStat + " aid_i" + paramcalc.pref, true);
            if (!ret.result) { Drop_NotCond(conn_db, paramcalc); return false; }

            //отберем некондиционные интервалы
            //выбираем искомые таблицы
            string[] arr = { "temp_counters",
                             "temp_cnt_spis",
                             "temp_counters_dom",
                             paramcalc.data_alias + "counters_domspis"
                           };

            //признак учета даты закрытия на текущий момент curd
            DateTime dat_799 = new DateTime(3000, 1, 1);

            ret = ExecRead(conn_db, out reader,
                " Select val_prm From " + paramcalc.data_alias + "prm_10 " +
                " Where nzp_prm = 799 " +
                "   and is_actual <> 100 " +
                "   and dat_s <= '" + paramcalc.curd.ToShortDateString() + "'" +
                "   and dat_po>= '" + paramcalc.curd.ToShortDateString() + "'"
                , true);

            if (ret.result && reader.Read())
            {
                if (reader["val_prm"] != DBNull.Value)
                {
                    string s = (string)reader["val_prm"];
                    if (!DateTime.TryParse(s.Trim(), out dat_799))
                    {
                        dat_799 = new DateTime(3000, 1, 1);
                    }
                }
                reader.Close();
            }

            for (int i = 0; i <= 3; i++)
            {
                //string swhere = " and " + rashod.where_dom;
                string swhere = " and nzp_dom in ( Select nzp_dom From t_selkvar) ";
                if (i == 0)
                {
                    //квартирные ПУ
                    swhere = " and nzp_kvar in ( Select nzp_kvar From t_selkvar) ";
                }
                if (i == 1)
                {
                    //квартирные ПУ
                    swhere = " and nzp_type=3 and nzp in ( Select nzp_kvar From t_selkvar) ";
                }

                //сначала период починки
                ret = ExecSQL(conn_db,
                    " Insert into aid_i" + paramcalc.pref + " (nzp_counter, dat_s, dat_po) " +
                    " Select " + sUniqueWord + " nzp_counter, dat_oblom, dat_poch " +
                    " From " + arr[i] +
                    " Where is_actual <> 100 " +
                    "   and dat_oblom is not null and dat_poch is not null " + swhere
                    , true);
                if (!ret.result) { Drop_NotCond(conn_db, paramcalc); return false; }

                //затем период закрытия ПУ
                //выберем все даты закрытия
                ExecSQL(conn_db, " Drop table aid_dclose", false);
                ExecSQL(conn_db, " Drop table aid_dclose1", false);
                ret = ExecSQL(conn_db,
                    " Create temp table aid_dclose" +
                    " ( nzp_counter integer, " +
                    "   dat_close date  " +
                    " ) " + sUnlogTempTable
                    , true);
                if (!ret.result) { Drop_NotCond(conn_db, paramcalc); return false; }

                ret = ExecSQL(conn_db,
                    " Create temp table aid_dclose1" +
                    " ( nzp_counter integer, " +
                    "   dat_close date  " +
                    " ) " + sUnlogTempTable
                    , true);
                if (!ret.result) { Drop_NotCond(conn_db, paramcalc); return false; }

                ret = ExecSQL(conn_db,
                    " Insert Into aid_dclose1 (nzp_counter, dat_close) " +
                    " Select nzp_counter, dat_close " +
                    " From " + arr[i] +
                    " Where is_actual <> 100 and dat_close is not null " + swhere
                    , true);
                if (!ret.result) { Drop_NotCond(conn_db, paramcalc); return false; }

                ExecSQL(conn_db, " CREATE INDEX ix_aid_dclose1 ON aid_dclose1(nzp_counter, dat_close) ", true);
                ExecSQL(conn_db, sUpdStat + " aid_dclose1 ", true);

                ret = ExecSQL(conn_db,
                    " Insert Into aid_dclose (nzp_counter, dat_close) " +
                    " Select nzp_counter, min(dat_close) as dat_close " +
                    " From aid_dclose1 " +
                    " Group by 1 "
                    , true);
                if (!ret.result) { Drop_NotCond(conn_db, paramcalc); return false; }

                ExecSQL(conn_db, " Create unique index ix_aid22_cl on aid_dclose (nzp_counter, dat_close) ", true);
                ExecSQL(conn_db, sUpdStat + " aid_dclose ", true);

                //потом max между dat_close и dat_799 (период действия)
                ret = ExecSQL(conn_db,
                    " Insert into aid_i" + paramcalc.pref + " (nzp_counter, dat_po, dat_s) " +
                    " Select " + sUniqueWord + " nzp_counter, " + sDefaultSchema + "mdy(1,1,3000), " +
                    " ( case when dat_close >=date('" + dat_799.ToShortDateString() + "')" +
                      " then dat_close else date('" + dat_799.ToShortDateString() + "') end )+1 " +
                    " From aid_dclose "
                    , true);
                if (!ret.result) { Drop_NotCond(conn_db, paramcalc); return false; }

                ViewTbl(conn_db, " select * from aid_dclose1 order by nzp_counter ");
                ViewTbl(conn_db, " select * from aid_dclose order by nzp_counter ");
                ViewTbl(conn_db, " select * from aid_i" + paramcalc.pref + " order by nzp_counter,dat_s ");

                ExecSQL(conn_db, " Drop table aid_dclose", false);
                ExecSQL(conn_db, " Drop table aid_dclose1", false);

                //выберем все даты поверки
                ExecSQL(conn_db, " Drop table aid_d" + paramcalc.pref, false);

                ret = ExecSQL(conn_db,
                    " Create temp table aid_d" + paramcalc.pref +
                    " ( nzp_counter integer, " +
                    "   dp  date, " +
                    "   dpn date  " +
                    " ) " + sUnlogTempTable
                    , true);
                if (!ret.result) { Drop_NotCond(conn_db, paramcalc); return false; }

                ret = ExecSQL(conn_db,
                    " Insert into aid_d" + paramcalc.pref + " (nzp_counter, dp, dpn) " +
                    " Select " + sUniqueWord + " nzp_counter, " + sNvlWord + "(dat_prov, " + sDefaultSchema + "mdy(1,1,1923)), " +
                      sNvlWord + "(dat_provnext, " + sDefaultSchema + "mdy(1,1,3000)) " +
                    " From " + arr[i] +
                    " Where is_actual <> 100 " + swhere +
                    "   and nzp_counter not in ( Select nzp_counter From aid_i" + paramcalc.pref + ") "
                    , true);
                if (!ret.result) { Drop_NotCond(conn_db, paramcalc); return false; }

                if (i == 0)
                {
                    ExecSQL(conn_db, " Create index ix_aid11_" + paramcalc.pref + " on aid_d" + paramcalc.pref + " (nzp_counter, dp) ", true);
                }
                ExecSQL(conn_db, sUpdStat + " aid_d" + paramcalc.pref, true);

                //начинаем анализировать даты поверки
                //лейтмотив следующий - показании не действуют после dpn до следующего dp
                ret = ExecSQL(conn_db,
                    " Insert into " + "aid_i" + paramcalc.pref + " (nzp_counter, dat_s) " +
                    " Select " + sUniqueWord + " nzp_counter, dpn " +
                    " From aid_d" + paramcalc.pref +
                    " Where dpn < " + sDefaultSchema + "mdy(1,1,3000) "
                    , true);
                if (!ret.result) { Drop_NotCond(conn_db, paramcalc); return false; }

                ExecSQL(conn_db, sUpdStat + " aid_i" + paramcalc.pref, true);

                string sql =
                        " Update aid_i" + paramcalc.pref +
                        " Set dat_po = ( Select min(dp) From aid_d" + paramcalc.pref + " a " +
                                       " Where aid_i" + paramcalc.pref + ".nzp_counter = a.nzp_counter " +
                                       "   and aid_i" + paramcalc.pref + ".dat_s <= a.dp ) " +
                        " Where 0 < ( Select count(*) From aid_d" + paramcalc.pref + " a " +
                                    " Where aid_i" + paramcalc.pref + ".nzp_counter = a.nzp_counter )";

                if (paramcalc.nzp_kvar > 0 || paramcalc.nzp_dom > 0)
                {
                    ret = ExecSQL(conn_db, sql, true);
                }
                else
                {
                    ExecByStep(conn_db, "aid_i" + paramcalc.pref, "nzp_key", sql, 50000, " ", out ret);
                }
                if (!ret.result) { Drop_NotCond(conn_db, paramcalc); return false; }
            }
            ExecSQL(conn_db, " Drop table aid_d" + paramcalc.pref, false);

            return true;
        }
        #endregion Функция пересоздания временных таблиц по префиксам

        #region выборка расходов ПУ - stek=1 & nzp_type = 1,2,3
        public bool SelRashodPUInStek1(IDbConnection conn_db, Rashod rashod, bool b_calc_kvar, string p_dat_charge, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            #region заполнение ИПУ

            string sDatUchet = "a.dat_uchet";
            //string tab = "";
            string sql = "";

            Rashod2 rashod_prm = new Rashod2(rashod.counters_xx, rashod.paramcalc);

            rashod_prm.tab = "temp_counters";
            rashod_prm.dat_s = rashod.paramcalc.dat_s;
            rashod_prm.dat_po = rashod.paramcalc.dat_po;
            rashod_prm.p_TAB = rashod_prm.tab + " a";
            rashod_prm.p_KEY = "a.nzp_crd";
            rashod_prm.p_ACTUAL = " and c.is_actual <> 100";
            rashod_prm.counters_xx = rashod.counters_xx;
            rashod_prm.p_where = rashod.where_dom.Trim() + rashod.where_kvar.Trim();
            rashod_prm.pref = rashod.paramcalc.pref;
            rashod_prm.p_type = "3";
            // для выборки дат снятия показаний ПУ
            rashod_prm.p_INSERT =
                " Insert into tpok (nzp_cr,nzp_kvar,nzp_dom,nzp_counter,nzp_counter_parent,nzp_serv,dat_uchet)" +
                " Select 0,k.nzp_kvar,k.nzp_dom,a.nzp_counter_child,a.nzp_counter,a.nzp_serv," + sDatUchet +
                " From " + rashod_prm.tab + " a, t_selkvar k" +
                " Where a.nzp_kvar = k.nzp_kvar";
            // для выборки значений показаний ПУ
            rashod_prm.p_FROM =
                " From t_selkvar k, " + rashod_prm.tab + " a, " + rashod_prm.counters_xx + " b " +
                " Where k.nzp_kvar = a.nzp_kvar " +
                "   and k.nzp_kvar = b.nzp_kvar ";
            rashod_prm.p_FROM_tmp =
                " From t_selkvar k, " + rashod_prm.tab + " a, t_inscnt b " +
                " Where k.nzp_kvar = a.nzp_kvar " +
                "   and k.nzp_kvar = b.nzp_kvar ";
            // для установки расхода нежилых помещений для ОДПУ
            rashod_prm.p_UPDdt_s = "";
            rashod_prm.p_UPDdt_po = "";

            LoadValsNew(conn_db, rashod_prm, p_dat_charge, "1", out ret);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            rashod_prm.tab = "temp_counters_gkal";
            rashod_prm.dat_s = rashod.paramcalc.dat_s;
            rashod_prm.dat_po = rashod.paramcalc.dat_po;
            rashod_prm.p_TAB = rashod_prm.tab + " a";
            rashod_prm.p_KEY = "a.nzp_crd";
            rashod_prm.p_ACTUAL = " and c.is_actual <> 100";
            rashod_prm.counters_xx = rashod.counters_xx;
            rashod_prm.p_where = rashod.where_dom.Trim() + rashod.where_kvar.Trim();
            rashod_prm.pref = rashod.paramcalc.pref;
            rashod_prm.p_type = "3";
            // для выборки дат снятия показаний ПУ
            rashod_prm.p_INSERT =
                " Insert into tpok (nzp_cr,nzp_kvar,nzp_dom,nzp_counter,nzp_counter_parent,nzp_serv,dat_uchet)" +
                " Select 0,k.nzp_kvar,k.nzp_dom,a.nzp_counter_child,a.nzp_counter,a.nzp_serv," + sDatUchet +
                " From " + rashod_prm.tab + " a, t_selkvar k" +
                " Where a.nzp_kvar = k.nzp_kvar";
            // для выборки значений показаний ПУ
            rashod_prm.p_FROM =
                " From t_selkvar k, " + rashod_prm.tab + " a, " + rashod_prm.counters_xx + " b " +
                " Where k.nzp_kvar = a.nzp_kvar " +
                "   and k.nzp_kvar = b.nzp_kvar ";
            rashod_prm.p_FROM_tmp =
                " From t_selkvar k, " + rashod_prm.tab + " a, t_inscnt b " +
                " Where k.nzp_kvar = a.nzp_kvar " +
                "   and k.nzp_kvar = b.nzp_kvar ";
            // для установки расхода нежилых помещений для ОДПУ
            rashod_prm.p_UPDdt_s = "";
            rashod_prm.p_UPDdt_po = "";

            LoadValsNew(conn_db, rashod_prm, p_dat_charge, "9", out ret);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            #endregion заполнение ИПУ

            #region заполнение домовых и групповых ПУ
            //----------------------------------------------------------------
            //заполнение домовых и групповых ПУ
            //----------------------------------------------------------------
            if (!rashod.paramcalc.b_again && !b_calc_kvar)
            {
                //----------------------------------------------------------------
                //заполнение домовых ПУ
                //----------------------------------------------------------------

                rashod_prm.tab = "temp_counters_dom";
                //rashod_prm.dat_s = rashod.paramcalc.dat_s;
                //rashod_prm.dat_po = rashod.paramcalc.dat_po;
                rashod_prm.p_TAB = rashod_prm.tab + " a";
                //rashod_prm.p_KEY = "a.nzp_crd";
                //rashod_prm.p_ACTUAL = " and c.is_actual <> 100";
                //rashod_prm.counters_xx = rashod.counters_xx;
                rashod_prm.p_where = rashod.where_dom;
                //rashod_prm.pref = rashod.paramcalc.pref;
                rashod_prm.p_type = "1";
                // для выборки дат снятия показаний ПУ
                rashod_prm.p_INSERT =
                    " Insert into tpok (nzp_cr,nzp_kvar,nzp_dom,nzp_counter,nzp_counter_parent,nzp_serv,dat_uchet)" +
                    " Select 0,0,a.nzp_dom,a.nzp_counter_child,a.nzp_counter,a.nzp_serv," + sDatUchet +
                    " From " + rashod_prm.tab + " a" +
                    " Where a." + rashod.where_dom;
                // для выборки значений показаний ПУ
                rashod_prm.p_FROM =
                    " From " + rashod_prm.tab + " a, " + rashod_prm.counters_xx + " b " +
                    " Where a." + rashod.where_dom;
                rashod_prm.p_FROM_tmp =
                    " From " + rashod_prm.tab + " a, t_inscnt b " +
                    " Where a." + rashod.where_dom;
                // для установки расхода нежилых помещений для ОДПУ
                rashod_prm.p_UPDdt_s =
                      " ,val2 = " +
                      "( Select max(ngp_cnt+ngp_lift) From " + rashod_prm.tab + " a " +
                      " Where t_inscnt.nzp_counter = a.nzp_counter_child and t_inscnt.dat_po = a.dat_uchet )";
                //" Where " + rashod.counters_xx + ".nzp_counter = a.nzp_counter and " + rashod.counters_xx + ".dat_po = a.dat_uchet )";
                rashod_prm.p_UPDdt_po =
                      " ,ngp_cnt = " +
                      "( Select max(ngp_cnt+ngp_lift) From " + rashod_prm.tab + " a " +
                      " Where t_inscnt.nzp_counter = a.nzp_counter_child and t_inscnt.dat_po = a.dat_uchet )";
                //" Where " + rashod.counters_xx + ".nzp_counter = a.nzp_counter and " + rashod.counters_xx + ".dat_po = a.dat_uchet )";

                LoadValsNew(conn_db, rashod_prm, p_dat_charge, "1", out ret);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }


                rashod_prm.tab = "temp_counters_dom_gkal";
                //rashod_prm.dat_s = rashod.paramcalc.dat_s;
                //rashod_prm.dat_po = rashod.paramcalc.dat_po;
                rashod_prm.p_TAB = rashod_prm.tab + " a";
                //rashod_prm.p_KEY = "a.nzp_crd";
                //rashod_prm.p_ACTUAL = " and c.is_actual <> 100";
                //rashod_prm.counters_xx = rashod.counters_xx;
                rashod_prm.p_where = rashod.where_dom;
                //rashod_prm.pref = rashod.paramcalc.pref;
                rashod_prm.p_type = "1";
                // для выборки дат снятия показаний ПУ
                rashod_prm.p_INSERT =
                    " Insert into tpok (nzp_cr,nzp_kvar,nzp_dom,nzp_counter,nzp_counter_parent,nzp_serv,dat_uchet)" +
                    " Select 0,0,a.nzp_dom,a.nzp_counter_child,a.nzp_counter,a.nzp_serv," + sDatUchet +
                    " From " + rashod_prm.tab + " a" +
                    " Where a." + rashod.where_dom;
                // для выборки значений показаний ПУ
                rashod_prm.p_FROM =
                    " From " + rashod_prm.tab + " a, " + rashod_prm.counters_xx + " b " +
                    " Where a." + rashod.where_dom;
                rashod_prm.p_FROM_tmp =
                    " From " + rashod_prm.tab + " a, t_inscnt b " +
                    " Where a." + rashod.where_dom;
                // для установки расхода нежилых помещений для ОДПУ
                rashod_prm.p_UPDdt_s =
                      " ,val2 = " +
                      "( Select max(ngp_cnt+ngp_lift) From " + rashod_prm.tab + " a " +
                      " Where t_inscnt.nzp_counter = a.nzp_counter_child and t_inscnt.dat_po = a.dat_uchet )";
                rashod_prm.p_UPDdt_po =
                      " ,ngp_cnt = " +
                      "( Select max(ngp_cnt+ngp_lift) From " + rashod_prm.tab + " a " +
                      " Where t_inscnt.nzp_counter = a.nzp_counter_child and t_inscnt.dat_po = a.dat_uchet )";

                LoadValsNew(conn_db, rashod_prm, p_dat_charge, "9", out ret);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                //----------------------------------------------------------------
                //заполнение средних значений ДПУ (без ПУ от ГКал)
                //----------------------------------------------------------------
                ret = ExecSQL(conn_db,
                    " Insert into " + rashod.counters_xx +
                    " ( stek,nzp_type,dat_charge,nzp_kvar,nzp_dom,nzp_counter,nzp_serv, dat_s,dat_po, val_s,val_po, val1, val4 ) " +
                    " Select 4,1, " + p_dat_charge + " ,0,p.nzp, 0,s.nzp_serv, " +
                    rashod.paramcalc.dat_s + ", " + rashod.paramcalc.dat_po + "," +
                    " 0,max(replace( " + sNvlWord + "(p.val_prm,'0'), ',', '.')" + sConvToNum + ") ," +
                    "max(replace( " + sNvlWord + "(p.val_prm,'0'), ',', '.')" + sConvToNum + "), 0 " +
                    " From ttt_prm_2 p, " + rashod.paramcalc.kernel_alias + "s_counts s, t_selkvar k " +
                    " Where p.nzp_prm = s.nzp_prm_sred_dom and s.nzp_prm_sred_dom>0 " +
                    "   and p.nzp = k.nzp_dom " +
                    " group by 1,2,3,4,5,6,7,8,9 "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                //----------------------------------------------------------------
                // заполнение групповых ПУ
                //----------------------------------------------------------------

                rashod_prm.tab = "temp_counters_group";
                //rashod_prm.dat_s = rashod.paramcalc.dat_s;
                //rashod_prm.dat_po = rashod.paramcalc.dat_po;
                rashod_prm.p_TAB = rashod_prm.tab + " a";
                rashod_prm.p_KEY = "a.nzp_cg";
                //rashod_prm.p_ACTUAL = " and c.is_actual <> 100";
                //rashod_prm.counters_xx = rashod.counters_xx;
                rashod_prm.p_where = rashod.where_dom;
                //rashod_prm.pref = rashod.paramcalc.pref;
                rashod_prm.p_type = "2";
                // для выборки дат снятия показаний ПУ
                rashod_prm.p_INSERT =
                    " Insert into tpok (nzp_cr,nzp_kvar,nzp_dom,nzp_counter,nzp_counter_parent,nzp_serv,dat_uchet)" +
                    " Select 0,0,d.nzp_dom,a.nzp_counter_child,a.nzp_counter,d.nzp_serv," + sDatUchet +
                    " From " + rashod_prm.tab + " a, " + rashod.paramcalc.data_alias + "counters_domspis d " +
                    " Where d." + rashod.where_dom +
                    "   and a.nzp_counter = d.nzp_counter ";
                // для выборки значений показаний ПУ
                rashod_prm.p_FROM =
                    " From " + rashod_prm.tab + " a, " + rashod_prm.counters_xx + " b " +
                    " Where b." + rashod.where_dom;
                rashod_prm.p_FROM_tmp =
                    " From " + rashod_prm.tab + " a, t_inscnt b " +
                    " Where b." + rashod.where_dom;
                // для установки расхода нежилых помещений для ОДПУ
                rashod_prm.p_UPDdt_s = "";
                rashod_prm.p_UPDdt_po = "";

                LoadValsNew(conn_db, rashod_prm, p_dat_charge, "1", out ret);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }


                rashod_prm.tab = "temp_counters_group_gkal";
                //rashod_prm.dat_s = rashod.paramcalc.dat_s;
                //rashod_prm.dat_po = rashod.paramcalc.dat_po;
                rashod_prm.p_TAB = rashod_prm.tab + " a";
                rashod_prm.p_KEY = "a.nzp_cg";
                //rashod_prm.p_ACTUAL = " and c.is_actual <> 100";
                //rashod_prm.counters_xx = rashod.counters_xx;
                rashod_prm.p_where = rashod.where_dom;
                //rashod_prm.pref = rashod.paramcalc.pref;
                rashod_prm.p_type = "2";
                // для выборки дат снятия показаний ПУ
                rashod_prm.p_INSERT =
                     " Insert into tpok (nzp_cr,nzp_kvar,nzp_dom,nzp_counter,nzp_counter_parent,nzp_serv,dat_uchet)" +
                    " Select 0,0,d.nzp_dom,a.nzp_counter_child,a.nzp_counter,d.nzp_serv," + sDatUchet +
                    " From " + rashod_prm.tab + " a, " + rashod.paramcalc.data_alias + "counters_domspis d " +
                    " Where d." + rashod.where_dom +
                    "   and a.nzp_counter = d.nzp_counter ";
                // для выборки значений показаний ПУ
                rashod_prm.p_FROM =
                    " From " + rashod_prm.tab + " a, " + rashod_prm.counters_xx + " b " +
                    " Where b." + rashod.where_dom;
                rashod_prm.p_FROM_tmp =
                    " From " + rashod_prm.tab + " a, t_inscnt b " +
                    " Where b." + rashod.where_dom;
                // для установки расхода нежилых помещений для ОДПУ
                rashod_prm.p_UPDdt_s = "";
                rashod_prm.p_UPDdt_po = "";

                LoadValsNew(conn_db, rashod_prm, p_dat_charge, "9", out ret);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }


                //----------------------------------------------------------------
                // заполнение общеквартирных ПУ
                //----------------------------------------------------------------

                rashod_prm.tab = "temp_counters_common_kvar_ttt";
                //rashod_prm.dat_s = rashod.paramcalc.dat_s;
                //rashod_prm.dat_po = rashod.paramcalc.dat_po;
                rashod_prm.p_TAB = rashod_prm.tab + " a";
                rashod_prm.p_KEY = "a.nzp_cg";
                //rashod_prm.p_ACTUAL = " and c.is_actual <> 100";
                //rashod_prm.counters_xx = rashod.counters_xx;
                rashod_prm.p_where = rashod.where_dom;
                //rashod_prm.pref = rashod.paramcalc.pref;
                rashod_prm.p_type = "4";
                // для выборки дат снятия показаний ПУ
                rashod_prm.p_INSERT =
                     " Insert into tpok (nzp_cr,nzp_kvar,nzp_dom,nzp_counter,nzp_counter_parent,nzp_serv,dat_uchet)" +
                    " Select distinct 0,0,cs.nzp,a.nzp_counter_child,a.nzp_counter,cs.nzp_serv," + sDatUchet +
                    " From " + rashod_prm.tab + " a, " + rashod.paramcalc.data_alias + "counters_spis cs " +
                    " Where cs.nzp in ( Select nzp_dom From t_selkvar) " +
                    "   and cs.nzp_counter = a.nzp_counter  ";
                // для выборки значений показаний ПУ
                rashod_prm.p_FROM =
                    " From " + rashod_prm.tab + " a, " + rashod_prm.counters_xx + " b " +
                    " Where b." + rashod.where_dom;
                rashod_prm.p_FROM_tmp =
                    " From " + rashod_prm.tab + " a, t_inscnt b " +
                    " Where b." + rashod.where_dom;
                // для установки расхода нежилых помещений для ОДПУ
                rashod_prm.p_UPDdt_s = "";
                rashod_prm.p_UPDdt_po = "";
                LoadValsNew(conn_db, rashod_prm, p_dat_charge, "1", out ret);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                rashod_prm.tab = "temp_counters_common_kvar_gkal";
                //rashod_prm.dat_s = rashod.paramcalc.dat_s;
                //rashod_prm.dat_po = rashod.paramcalc.dat_po;
                rashod_prm.p_TAB = rashod_prm.tab + " a";
                rashod_prm.p_KEY = "a.nzp_cg";
                //rashod_prm.p_ACTUAL = " and c.is_actual <> 100";
                //rashod_prm.counters_xx = rashod.counters_xx;
                rashod_prm.p_where = rashod.where_dom;
                //rashod_prm.pref = rashod.paramcalc.pref;
                rashod_prm.p_type = "4";
                // для выборки дат снятия показаний ПУ
                rashod_prm.p_INSERT =
                      " Insert into tpok (nzp_cr,nzp_kvar,nzp_dom,nzp_counter,nzp_counter_parent,nzp_serv,dat_uchet)" +
                      " Select distinct 0,0,cs.nzp,a.nzp_counter_child,a.nzp_counter,cs.nzp_serv," + sDatUchet +
                      " From " + rashod_prm.tab + " a, " + rashod.paramcalc.data_alias + "counters_spis cs " +
                      " Where cs.nzp in ( Select nzp_dom From t_selkvar) " +
                      "   and cs.nzp_counter = a.nzp_counter  ";
                // для выборки значений показаний ПУ
                rashod_prm.p_FROM =
                    " From " + rashod_prm.tab + " a, " + rashod_prm.counters_xx + " b " +
                    " Where b." + rashod.where_dom;
                rashod_prm.p_FROM_tmp =
                    " From " + rashod_prm.tab + " a, t_inscnt b " +
                    " Where b." + rashod.where_dom;
                // для установки расхода нежилых помещений для ОДПУ
                rashod_prm.p_UPDdt_s = "";
                rashod_prm.p_UPDdt_po = "";

                LoadValsNew(conn_db, rashod_prm, p_dat_charge, "9", out ret);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                UpdateStatistics(true, rashod.paramcalc, rashod.counters_tab, out ret);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            }
            #endregion заполнение домовых и групповых ПУ

            #region заполнение расходов ПУ (можно включить как в 2.0)

            UpdateStatistics(false, rashod.paramcalc, rashod.counters_tab, out ret);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            #region заполнить разрядность и масштабный множитель ПУ
            //----------------------------------------------------------------
            //заполнить параметры типа
            //----------------------------------------------------------------
            sql = " Update " + rashod.counters_xx + " c " +
                             " SET " +
                             " cnt_stage = b.cnt_stage, " +
                             " mmnog = b.mmnog" +
                             " FROM " + rashod.paramcalc.data_alias + "counters_spis a, " + rashod.paramcalc.kernel_alias + "s_counttypes b " +
                             " WHERE 1 = 1 and c." + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                             " AND a.nzp_cnttype = b.nzp_cnttype " +
                             " AND c.nzp_counter = a.nzp_counter ";
            if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
            }
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            //заполнение для неопределенных значений ДПУ
            ret = ExecSQL(conn_db,
                " Update " + rashod.counters_xx +
                " Set cnt_stage=10 ,mmnog=1 " +
                " Where (cnt_stage is null or mmnog is null) and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            //заполнение для средних значений ДПУ
            ret = ExecSQL(conn_db,
                " Update " + rashod.counters_xx +
                " Set cnt_stage=10 ,mmnog=1 " +
                " Where stek = 4 and nzp_type = 1 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            //заполнение для без ДПУ 
            ret = ExecSQL(conn_db,
                " Update " + rashod.counters_xx +
                " Set cnt_stage=10 ,mmnog=1 " +
                " Where cnt_stage is null and nzp_type = 1 and stek=333 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            #endregion заполнить разрядность и масштабный множитель ПУ


            #region посчитать расход общий по счетчику
            //----------------------------------------------------------------
            //посчитать расход общий по счетчику
            //----------------------------------------------------------------
            //надо подумать, как застраховаться от гигантских расходов (иначе свалится update) - ограничу 1000000
            sql =
                    " Update " + rashod.counters_xx +
                    " Set val4 = case when (" +                                                                                //val2 не учитывается!
                       " (case when val_po - val_s>-0.0001 then (val_po - val_s)*mmnog else (pow(10,cnt_stage)+val_po-val_s)*mmnog end) - (ngp_cnt+0))  > 1000000" +
                             " then 1000000 else " +
                       " (case when val_po - val_s>-0.0001 then (val_po - val_s)*mmnog else (pow(10,cnt_stage)+val_po-val_s)*mmnog end) - (ngp_cnt+0)  end " +
                    " Where 1 = 1 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge;

            if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx", sql, 100000, " ", out ret);
            }
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            #endregion посчитать расход общий по счетчику

            #region Проверка на переполнение поля numeric - сверхбольшой расход по ИПУ


            var sql2 = " SELECT count(*) from t_selkvar s, " + rashod.counters_xx + " t" +
                   " WHERE s.nzp_kvar=t.nzp_kvar AND t.val4>=1000000";
            var count_over_rash = CastValue<int>(ExecScalar(conn_db, sql2, out ret, true));
            if (count_over_rash > 0)
            {
                //пишем лс с большими показаниями ипу 
                sql2 = " INSERT INTO " + rashod.paramcalc.pref + sDataAliasRest + "link_group (nzp_group, nzp) " +
                       " SELECT 13, s.nzp_kvar from t_selkvar s, " + rashod.counters_xx + " t" +
                       " WHERE s.nzp_kvar=t.nzp_kvar AND t.val4>=1000000";
                ret = ExecSQL(conn_db, sql2, true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
                //сигнализирует в списке заданий о выполнении расчета с ошибками 
                status = FonTask.Statuses.WithErrors;
            }

            #endregion Проверка на переполнение поля numeric - сверхбольшой расход по ИПУ


            #region посчитать расход, который приходится в текущем месяце
            //----------------------------------------------------------------
            //посчитать расход, который приходится в текущем месяце
            //----------------------------------------------------------------

            ExecSQL(conn_db, " Drop table ttt_aid_virt0 ", false);
            ret = ExecSQL(conn_db,
                " Create temp table ttt_aid_virt0 " +
                " ( nzp_cntx integer, " +
                "   nzp_kvar integer, " +
                "   nzp_serv integer, " +
                "   nzp_counter integer, " +
                "   dat_s  date not null," +
                "   dat_po date not null," +
                "   val1   " + sDecimalType + "(15,7) default 0.00," +
                "   val3   " + sDecimalType + "(15,7) default 0.00," +
                "   val4   " + sDecimalType + "(15,7) default 0.00," +
                "   rvirt  " + sDecimalType + "(15,7) default 0.00," +
                "   nzp_dom  integer, " +
                "   val_s   " + sDecimalType + "(15,7) default 0.00," +
                "   val_po  " + sDecimalType + "(15,7) default 0.00," +
                "   ngp_cnt " + sDecimalType + "(15,7) default 0.00," +
                "   cnt_stage integer, " +
                "   mmnog     integer, " +
                "   kod_info  integer, " +
                "   gil1    " + sDecimalType + "(15,7) default 0.00 " +  //кол-во жильцов в лс
                " )  " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " Drop table ttt_counters_ipu ", false);
            ret = ExecSQL(conn_db,
                " Create temp table ttt_counters_ipu " +
                " ( nzp_cntx integer default 0, " +
                "   nzp_kvar integer, " +
                "   nzp_serv integer, " +
                "   nzp_counter integer, " +
                "   stek   integer, " +
                "   dat_s  date not null," +
                "   dat_po date not null," +
                "   val1   " + sDecimalType + "(15,7) default 0.00," +
                "   val3   " + sDecimalType + "(15,7) default 0.00," +
                "   val4   " + sDecimalType + "(15,7) default 0.00," +
                "   rvirt  " + sDecimalType + "(15,7) default 0.00," +
                "   nzp_dom  integer, " +
                "   val_s   " + sDecimalType + "(15,7) default 0.00," +
                "   val_po  " + sDecimalType + "(15,7) default 0.00," +
                "   ngp_cnt " + sDecimalType + "(15,7) default 0.00," +
                "   cnt_stage integer, " +
                "   mmnog     integer, " +
                "   kod_info  integer, " +
                "   gil1    " + sDecimalType + "(15,7) default 0.00, " +  //кол-во жильцов в лс
                "   nzp_period integer, " +
                "   dp       date not null, " +
                "   dp_end   date not null, " +
                "   cntd integer," +
                "   cntd_mn integer " +
                " )  " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " Insert into ttt_aid_virt0 " +
                 "(nzp_cntx,nzp_kvar,nzp_serv,nzp_counter,dat_s,dat_po,val1,val3,val4,rvirt," +
                  "nzp_dom,val_s,val_po,ngp_cnt,cnt_stage,mmnog,kod_info,gil1) " +
                " Select " +
                  "nzp_cntx,nzp_kvar,nzp_serv,nzp_counter,dat_s,dat_po,val1,val3,val4,rvirt," +
                  "nzp_dom,val_s,val_po,ngp_cnt,cnt_stage,mmnog,kod_info,gil1 " +
                " From " + rashod.counters_xx +
                " Where nzp_type = 3 and stek = 1 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " Create index ix1_ttt_aid_virt0 on ttt_aid_virt0 (nzp_kvar) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_aid_virt0 ", true);

            // размножим расходы ИПУ по периодам по-дневного расчета (nzp_cntx дублируется!) / stek=1 - на месяц / stek=11 по-дневным периодам
            ret = ExecSQL(conn_db,
                " Insert into ttt_counters_ipu " +
                 "(nzp_cntx,nzp_kvar,nzp_serv,nzp_counter,stek,dat_s,dat_po,val1,val3,val4,rvirt," +
                  "nzp_dom,val_s,val_po,ngp_cnt,cnt_stage,mmnog,kod_info,gil1," +
                  "nzp_period,dp,dp_end,cntd,cntd_mn) " +
                " Select " +
                  "c.nzp_cntx,c.nzp_kvar,c.nzp_serv,c.nzp_counter,(case when k.typ=0 then 1 else 11 end) stek,c.dat_s,c.dat_po,c.val1,c.val3,c.val4,c.rvirt," +
                  "c.nzp_dom,c.val_s,c.val_po,c.ngp_cnt,c.cnt_stage,c.mmnog,c.kod_info,c.gil1," +
                  "k.nzp_period,k.dp,k.dp_end,k.cntd,k.cntd_mn " +
                " From ttt_aid_virt0 c, t_gku_periods k " +
                " Where k.nzp_kvar=c.nzp_kvar "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // вставить ОДПУ (nzp_type=1&стек=1) и ГрПУ(nzp_type=2&стек=1)
            ret = ExecSQL(conn_db,
                " Insert into ttt_counters_ipu " +
                 "(nzp_cntx,nzp_kvar,nzp_serv,nzp_counter,stek,dat_s,dat_po,val1,val3,val4,rvirt," +
                  "nzp_dom,val_s,val_po,ngp_cnt,cnt_stage,mmnog,kod_info,gil1," +
                  "nzp_period,dp,dp_end,cntd,cntd_mn) " +
                " Select " +
                  "c.nzp_cntx,c.nzp_kvar,c.nzp_serv,c.nzp_counter,c.stek,c.dat_s,c.dat_po,c.val1,c.val3,c.val4,c.rvirt," +
                  "c.nzp_dom,c.val_s,c.val_po,c.ngp_cnt,c.cnt_stage,c.mmnog,9904 kod_info,c.gil1," +
                  " 0," + rashod.paramcalc.dat_s + "," + rashod.paramcalc.dat_po + ", " +
                  DateTime.DaysInMonth(rashod.paramcalc.calc_yy, rashod.paramcalc.calc_mm) + ", " +
                  DateTime.DaysInMonth(rashod.paramcalc.calc_yy, rashod.paramcalc.calc_mm) +
                " From " + rashod.counters_xx + " c " +
                " Where nzp_type in (1,2,4) and stek=1 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // вставить ПУ от ГКал (стек=9)
            ret = ExecSQL(conn_db,
                " Insert into ttt_counters_ipu " +
                 "(nzp_cntx,nzp_kvar,nzp_serv,nzp_counter,stek,dat_s,dat_po,val1,val3,val4,rvirt," +
                  "nzp_dom,val_s,val_po,ngp_cnt,cnt_stage,mmnog,kod_info,gil1," +
                  "nzp_period,dp,dp_end,cntd,cntd_mn) " +
                " Select " +
                  "c.nzp_cntx,c.nzp_kvar,c.nzp_serv,c.nzp_counter,c.stek,c.dat_s,c.dat_po,c.val1,c.val3,c.val4,c.rvirt," +
                  "c.nzp_dom,c.val_s,c.val_po,c.ngp_cnt,c.cnt_stage,c.mmnog,9904 kod_info,c.gil1," +
                  " 0," + rashod.paramcalc.dat_s + "," + rashod.paramcalc.dat_po + ", " +
                  DateTime.DaysInMonth(rashod.paramcalc.calc_yy, rashod.paramcalc.calc_mm) + ", " +
                  DateTime.DaysInMonth(rashod.paramcalc.calc_yy, rashod.paramcalc.calc_mm) +
                " From " + rashod.counters_xx + " c " +
                " Where stek = 9 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " Create index ix0_ttt_counters_ipu on ttt_counters_ipu (nzp_cntx,stek) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_counters_ipu ", true);

            ExecSQL(conn_db, " Drop table ttt_aid_virt0 ", false);

            //st1:=IntToStr(count_days(nedoXX.calc_yy,nedoXX.calc_mm)); //кол-во дней в месяце
            string st1 = (DateTime.DaysInMonth(rashod.paramcalc.calc_yy, rashod.paramcalc.calc_mm)).ToString();

#if PG
            string intrv1 = "interval '1 day'";
            string tExtDayBeg = "EXTRACT(day from ";
            string tExtDayEnd = ")";
#else
            string intrv1 = "1";
            string tExtDayBeg = "";
            string tExtDayEnd = "";
#endif

            // Выставить признак "Включить расчет расходов ИПУ как в 2.0 (неправильный вариант)"
            bool bGoodCalcIPU =
                !CheckValBoolPrmWithVal(conn_db, rashod.paramcalc.data_alias, 3000, "5", "1", rashod.paramcalc.dat_s, rashod.paramcalc.dat_po);

            sql =
                " Update ttt_counters_ipu " +
                " Set val1 = (case when dat_s >= dp and dat_po <= dp_end + " + intrv1;
            //" Set val1 = (case when dat_s >= " + rashod.paramcalc.dat_s + " and dat_po <= " + rashod.paramcalc.dat_po + " + " + intrv1;
            if (bGoodCalcIPU)
            {
                //правильный код!
                sql = sql.Trim() +
                " and dat_s < dat_po " +
                " then val4 " +
                " else (val4 / (dat_po - dat_s)) * " +

                  //если показание в середине месяца, то нельзя умножать на весь месяц, а только на период до конца месяца!!
                    //причем, показание в середине месяца появятся только тогда, когда это первое показание, иначе показания всегда покрывают месяц!
                    //" (case when dat_s > " + rashod.paramcalc.dat_s + " and dat_s < " + rashod.paramcalc.dat_po +
                    //"  then " + tExtDayBeg + "(" + rashod.paramcalc.dat_po + " + " + intrv1 + " - dat_s)" + tExtDayEnd +
                    //"  else " + st1 + " end) ";  // st1 = DateTime.DaysInMonth(rashod.paramcalc.calc_yy, rashod.paramcalc.calc_mm)
                  " (case when dat_s >= dp and dat_s < dp_end " +
                   " then " + tExtDayBeg + "(dp_end + " + intrv1 + " - dat_s)" + tExtDayEnd +

                   " else " +
                     " (case when dat_po > dp and dat_po <= dp_end " +
                      " then " + tExtDayBeg + "(dat_po + " + intrv1 + " - dp)" + tExtDayEnd +
                      " else " +
                          " (case when dat_s <= dp and dat_po > dp_end " +
                           " then cntd " +
                           " else 0 " +
                          " end) " +
                      " end) " +
                   " end) ";
            }
            else
            {
                //потом удалить, неправильный код!
                //для сопоставления расчета Анэса!
                sql = sql.Trim() +
                " and dat_s - " + intrv1 + " < dat_po " +
                " then val4" +
                " else (val4 / (dat_po - dat_s - " + intrv1 + ")) * " +

                //" (case when dat_s > " + rashod.paramcalc.dat_s + " and dat_s < " + rashod.paramcalc.dat_po +
                    //"  then " + tExtDayBeg + "(" + rashod.paramcalc.dat_po + " + " + intrv1 + " - dat_s)" + tExtDayEnd +
                    //"  else " + st1 + " end) " +   // st1 = DateTime.DaysInMonth(rashod.paramcalc.calc_yy, rashod.paramcalc.calc_mm)
                  " (case when dat_s > dp and dat_s < dp_end " +
                  "  then " + tExtDayBeg + "(dp_end + " + intrv1 + " - dat_s)" + tExtDayEnd +
                  "  else cntd end) " +
                    //" + (case when dat_s >= " + rashod.paramcalc.dat_s + " and dat_s < " + rashod.paramcalc.dat_po +
                  " + (case when dat_s >= dp and dat_s < dp_end " +
                     " then val4 - (val4 / (dat_po - dat_s - " + intrv1 + ")) * (dat_po - dat_s) " +
                     " else 0 " +
                     " end) ";
            }
            sql = sql.Trim() +
                " end )" +

                ", kod_info = (case when dat_s >= dp and dat_po <= dp_end + " + intrv1 +
                " and dat_s < dat_po " +
                " then 0 " +
                " else " +

                  " (case when dat_s >= dp and dat_s < dp_end " +
                   " then 0" +

                   " else " +
                     " (case when dat_po > dp and dat_po <= dp_end " +
                      " then 0" +
                      " else " +
                          " (case when dat_s <= dp and dat_po > dp_end " +
                           " then 0 " +
                           " else 9902 " + // если нет подходящего интервала - удалить!
                          " end) " +
                      " end) " +
                   " end) " +
                " end )" +

                "  Where 1 = 1 ";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ViewTbl(conn_db, " select * from ttt_counters_ipu order by nzp_kvar,stek,nzp_counter,dat_s ");

            // превратим обратно разноженные расходы ИПУ (размазанные по дням) из стека 11 для полного месяца в стек 1
            ret = ExecSQL(conn_db,
                " Insert into ttt_counters_ipu " +
                 "(nzp_cntx,nzp_kvar,nzp_serv,nzp_counter,stek,dat_s,dat_po,val1,val3,val4,rvirt," +
                  "nzp_dom,val_s,val_po,ngp_cnt,cnt_stage,mmnog,kod_info," +
                  "nzp_period,dp,dp_end,cntd,cntd_mn) " +
                " Select " +
                  " c.nzp_cntx,c.nzp_kvar,c.nzp_serv,c.nzp_counter,1 stek,min(c.dat_s),max(c.dat_po),sum(c.val1),max(c.val3),max(c.val4),max(c.rvirt)," +
                  "max(c.nzp_dom),max(c.val_s),max(c.val_po),max(c.ngp_cnt),max(c.cnt_stage),max(c.mmnog),max(c.kod_info)," +
                  " 0," + rashod.paramcalc.dat_s + "," + rashod.paramcalc.dat_po + ", " +
                  DateTime.DaysInMonth(rashod.paramcalc.calc_yy, rashod.paramcalc.calc_mm) + ", " +
                  DateTime.DaysInMonth(rashod.paramcalc.calc_yy, rashod.paramcalc.calc_mm) +
                " From ttt_counters_ipu c " +
                " Where c.stek=11 " +
                " Group by 1,2,3,4 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " Create index ix1_ttt_counters_ipu on ttt_counters_ipu (stek,kod_info) ", true);
            ExecSQL(conn_db, " Create index ix2_ttt_counters_ipu on ttt_counters_ipu (kod_info) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_counters_ipu ", true);

            ViewTbl(conn_db, " select * from ttt_counters_ipu order by nzp_kvar,stek,nzp_counter,dat_s ");

            // stek=1 есть в БД -> Update
            ret = ExecSQL(conn_db,
                " Update " + rashod.counters_xx +
                " Set val1 = " +
#if PG
 " c.val1 from ttt_counters_ipu c where c.nzp_cntx=" + rashod.counters_xx + ".nzp_cntx and c.stek in (1,9) "
#else
                " (select val1 from ttt_counters_ipu c where c.nzp_cntx=" + rashod.counters_xx + ".nzp_cntx and c.stek in (1,9) )" +
                " Where  exists (select 1 from ttt_counters_ipu c where c.nzp_cntx=" + rashod.counters_xx + ".nzp_cntx and c.stek in (1,9) ) "
#endif
, true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // удалить ОДПУ, ГрПУ и ПУ от ГКал -- их по дням нет!
            ret = ExecSQL(conn_db,
                " delete from ttt_counters_ipu " +
                " Where kod_info=9904 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // stek=11 нет в БД -> Insert - без "дырок", где kod_info = 9902
            sql =
                " Insert into " + rashod.counters_xx +
                " (nzp_kvar,nzp_serv,nzp_counter,stek,dat_charge,dat_s,dat_po,mmnog,cnt_stage,val1,val3,val4,rvirt," +
                  "nzp_type,nzp_dom,val_s,val_po,ngp_cnt,kod_info,cls1,cls2)" +
                " Select " +
                  "nzp_kvar,nzp_serv,nzp_counter,stek," + p_dat_charge + ",dp,dp_end,mmnog,cnt_stage,val1,val3,val4,rvirt," +
                  "3,nzp_dom,val_s,val_po,ngp_cnt,kod_info,cntd,cntd_mn " +
                " From ttt_counters_ipu " +
                " Where stek=11 and kod_info <> 9902 ";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            #endregion посчитать расход, который приходится в текущем месяце

            #region доп. расходы
            //----------------------------------------------------------------
            //посчитать доп.виртуальные расходы на конец месяца
            //----------------------------------------------------------------
            /*
            if (rashod.calcv)
            {
                ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx",
                    " Update " + rashod.counters_xx +
                    " Set rvirt = (case when dat_po > " + rashod.paramcalc.dat_po + " then 0 else (val1/" + DateTime.DaysInMonth(rashod.paramcalc.calc_yy, rashod.paramcalc.calc_mm) + 
                                ")*(" + rashod.paramcalc.dat_po + " - dat_po) end )  " +
                    " Where nzp_type = 3 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge
                  , 100000, " ", out ret);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }

                ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx",
                    " Update " + rashod.counters_xx +
                    " Set val1 = val1 + rvirt " +
                    " Where nzp_type = 3 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge
                  , 100000, " ", out ret);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
            }
            else
            {
                //иначе обнулить неполный расход, чтобы считался по нормативу!
                ExecByStep(conn_db, rashod.counters_xx, "nzp_cntx",
                    " Update " + rashod.counters_xx +
                    " Set val1 = 0 " +
                    " Where nzp_type = 3 and " + rashod.where_dom + rashod.where_kvar +  rashod.paramcalc.per_dat_charge +
                    "   and dat_po <= " + rashod.paramcalc.dat_po
                  , 100000, " ", out ret);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
            }
            */
            #endregion доп. расходы

            #endregion заполнение расходов ПУ (можно включить как в 2.0)

            return true;
        }
        #endregion выборка расходов ПУ - stek=1 & nzp_type = 1,2,3

        #region Функция (LoadValsNew) выборки значений счетчиков используя структуру Rashod2

        //--------------------------------------------------------------------------------
        public bool LoadValsNew(IDbConnection conn_db, Rashod2 rashod_prm, string p_dat_charge, string sstek, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            string sDatUchet = "a.dat_uchet";

            #region Выборка уникальных счетчиков в tpok с учетом выбранного списка temp_counters&t_selkvar

            ExecSQL(conn_db, " Drop table tpok ", false);
            ret = ExecSQL(conn_db,
                " Create temp table tpok " +
                " ( nzp_cr      serial, " +
                "   nzp_kvar    integer," +
                "   nzp_dom     integer," +
                "   nzp_counter integer," +
                "   nzp_counter_parent integer," +
                "   nzp_serv    integer," +
                "   dat_uchet   date    " +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod_prm.pref); return false; }

            //режем показания, которые оказались в невалидном периоде
            ret = ExecSQL(conn_db,
                rashod_prm.p_INSERT +
                "   and NOT EXISTS (" +
                "     Select 1 From aid_i" + rashod_prm.pref + " n" +
                "     Where a.nzp_counter = n.nzp_counter and a.dat_uchet >= n.dat_s and a.dat_uchet <= n.dat_po" +
                "     )"
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod_prm.pref); return false; }


            ExecSQL(conn_db, " Create index ix_tpok on tpok (nzp_counter,dat_uchet) ", true);


            #endregion Выборка уникальных счетчиков в tpok с учетом выбранного списка temp_counters&t_selkvar

            #region Подготовка таблиц ограничений периодов для выборки показаний захватывающих расчетный месяц
            //----------------------------------------------------------------
            //заполнение квартирных ПУ (только по открытым лс)
            //----------------------------------------------------------------
            //таблица показаний, где даты показаний
            //ta_mr <= dat_s
            //ta_b  >  dat_s and < dat_po

            //tb_b  >  dat_po
            //tb_mr <= dat_po

            ExecSQL(conn_db, " Drop table ta_mr ", false);
            ret = ExecSQL(conn_db, " Create temp table ta_mr (nzp_counter integer, dat_uchet date) " + sUnlogTempTable + " ", true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod_prm.pref); return false; }

            ret = ExecSQL(conn_db,
                " Insert into ta_mr (nzp_counter,dat_uchet)" +
                " Select nzp_counter,max(dat_uchet) dat_uchet " +
                " From tpok Where dat_uchet <=" + rashod_prm.dat_s +
                " Group by 1 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod_prm.pref); return false; }

            ExecSQL(conn_db, " Create unique index ix_ta_mr on ta_mr (nzp_counter) ", true);
            ExecSQL(conn_db, " Create unique index ix_ta_mr2 on ta_mr (nzp_counter,dat_uchet) ", true);
            ExecSQL(conn_db, sUpdStat + " ta_mr ", true);

            //специально взято макс справа, чтобы наиболее точно апроксимировать подневной расход ???!!!
            //надо подумать, нафига так делать, ваще не надо!
            ExecSQL(conn_db, " Drop table ta_bi ", false);
            ret = ExecSQL(conn_db, " Create temp table ta_bi (nzp_counter integer, dat_uchet date) " + sUnlogTempTable + " ", true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod_prm.pref); return false; }

            ret = ExecSQL(conn_db,
                " Insert into ta_bi (nzp_counter,dat_uchet)" +
                " Select nzp_counter,min(dat_uchet) dat_uchet " +
                " From tpok " +
                " Where dat_uchet > " + rashod_prm.dat_s +
                "   and dat_uchet <=" + rashod_prm.dat_po +
                " Group by 1 "
                , true);

            if (!ret.result) { DropTempTablesRahod(conn_db, rashod_prm.pref); return false; }

            ExecSQL(conn_db, " Create unique index ix_ta_bi on ta_bi (nzp_counter) ", true);
            ExecSQL(conn_db, sUpdStat + " ta_bi ", true);

            ExecSQL(conn_db, " Drop table tb_b ", false);
            ret = ExecSQL(conn_db, " Create temp table tb_b (nzp_counter integer, dat_uchet date) " + sUnlogTempTable + " ", true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod_prm.pref); return false; }

            ret = ExecSQL(conn_db,
                " Insert into tb_b (nzp_counter,dat_uchet)" +
                " Select nzp_counter,min(dat_uchet) dat_uchet " +
                " From tpok " +
                " Where dat_uchet > " + rashod_prm.dat_po +
                " Group by 1 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod_prm.pref); return false; }

            ExecSQL(conn_db, " Create unique index ix_tb_b on tb_b (nzp_counter) ", true);
            ExecSQL(conn_db, sUpdStat + " tb_b ", true);


            ExecSQL(conn_db, " Drop table tb_mr ", false);
            ret = ExecSQL(conn_db, " Create temp table tb_mr (nzp_counter integer, dat_uchet date) " + sUnlogTempTable + " ", true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod_prm.pref); return false; }

            ret = ExecSQL(conn_db,
                " Insert into tb_mr (nzp_counter,dat_uchet)" +
                " Select nzp_counter,max(dat_uchet) dat_uchet " +
                " From tpok " +
                " Where dat_uchet <=" + rashod_prm.dat_po +
                " Group by 1 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod_prm.pref); return false; }

            ExecSQL(conn_db, " Create unique index ix_tb_mr on tb_mr (nzp_counter) ", true);
            ExecSQL(conn_db, " Create unique index ix_tb_mr2 on tb_mr (nzp_counter,dat_uchet) ", true);
            ExecSQL(conn_db, sUpdStat + " tb_mr ", true);

            //показание в середине
            ExecSQL(conn_db, " Drop table ta_b ", false);
            ret = ExecSQL(conn_db, " Create temp table ta_b (nzp_counter integer, dat_uchet date) " + sUnlogTempTable + " ", true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod_prm.pref); return false; }

            ret = ExecSQL(conn_db,
                " Insert into ta_b (nzp_counter,dat_uchet)" +
                " Select nzp_counter,min(dat_uchet) dat_uchet " +
                " From tpok " +
                " Where dat_uchet > " + rashod_prm.dat_s +
                "   and dat_uchet < " + rashod_prm.dat_po +
                " Group by 1 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod_prm.pref); return false; }

            ExecSQL(conn_db, " Create unique index ix_ta_b on ta_b (nzp_counter) ", true);
            ExecSQL(conn_db, sUpdStat + " ta_b ", true);

            #endregion Подготовка таблиц ограничений периодов для выборки показаний захватывающих расчетный месяц

            #region Выборка показаний захватывающих расчетный месяц t_inscnt

            ExecSQL(conn_db, " Drop table t_inscnt ", false);
            ret = ExecSQL(conn_db,
                " Create temp table t_inscnt" +
                " ( nzp_cr      serial, " +
                "   nzp_kvar    integer," +
                "   nzp_dom     integer," +
                "   nzp_counter integer," +
                "   nzp_counter_parent integer," +
                "   nzp_serv    integer," +
                "   dat_s       date,   " +
                "   dat_po      date,   " +
                "   val2        " + sDecimalType + "(15,7) default 0.00 not null, " +  // начальное показание ИПУ
                "   ngp_cnt     " + sDecimalType + "(15,7) default 0.00 not null, " +  // начальное показание ИПУ
                "   val_s       " + sDecimalType + "(15,7) default 0.00 not null, " +  // начальное показание ИПУ
                "   val_po      " + sDecimalType + "(15,7) default 0.00 not null  " +  // конечное  показание ИПУ
                " )  " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod_prm.pref); return false; }

            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            //выберем показания, которые полностью покрывают месяц!
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            //           |      |
            //     ^-------------------^
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            ret = ExecSQL(conn_db,
                " Insert into t_inscnt (nzp_cr,nzp_kvar,nzp_dom,nzp_counter,nzp_counter_parent,nzp_serv,dat_s,dat_po)" +
                " Select 0,a.nzp_kvar,a.nzp_dom,a.nzp_counter,a.nzp_counter_parent,a.nzp_serv,max(a.dat_uchet),min(b.dat_uchet)" +
                " From tpok a, tpok b" +
                " Where a.nzp_counter = b.nzp_counter" +
                " and a.dat_uchet   < b.dat_uchet" +
                " and a.dat_uchet = ( Select c.dat_uchet From ta_mr c Where c.nzp_counter = a.nzp_counter )" +
                " and b.dat_uchet = ( Select p.dat_uchet From tb_b  p Where p.nzp_counter = b.nzp_counter )" +
                " Group by 1,2,3,4,5,6 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod_prm.pref); return false; }

            ExecSQL(conn_db, " Create index ix1_t_inscnt on t_inscnt (nzp_counter) ", true);
            ExecSQL(conn_db, sUpdStat + " t_inscnt ", true);
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            //выбор других интервалов, кроме уже выбранных в counters_xx
            //придеться изголяться, чтобы выбрать ближайшие показания (избежать выбора большого интервала)
            //зато понятно
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            //           |      |
            //   ^-----------^
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            ret = ExecSQL(conn_db,
                " Insert into t_inscnt (nzp_cr,nzp_kvar,nzp_dom,nzp_counter,nzp_counter_parent,nzp_serv,dat_s,dat_po)" +
                " Select 0,a.nzp_kvar,a.nzp_dom,a.nzp_counter,a.nzp_counter_parent,a.nzp_serv,max(a.dat_uchet),min(b.dat_uchet)" +
                " From tpok a, tpok b" +
                " Where a.nzp_counter = b.nzp_counter" +
                "   and a.dat_uchet   < b.dat_uchet" +
                "   and not exists ( Select 1 From t_inscnt n Where a.nzp_counter = n.nzp_counter ) " +
                "   and a.dat_uchet = ( Select c.dat_uchet From ta_mr c Where c.nzp_counter = a.nzp_counter )" +
                "   and b.dat_uchet = ( Select p.dat_uchet From ta_b  p Where p.nzp_counter = b.nzp_counter)" +
                //"   and b.dat_uchet = ( Select p.dat_uchet From tb_mr p Where p.nzp_counter = b.nzp_counter)" +
                " Group by 1,2,3,4,5,6 "
                , true);
            if (!ret.result)
            {
                DropTempTablesRahod(conn_db, rashod_prm.pref);
                return false;
            }
            ExecSQL(conn_db, sUpdStat + " t_inscnt ", true);

            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            //           |      |
            //              ^-------------^
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            ret = ExecSQL(conn_db,
                " Insert into t_inscnt (nzp_cr,nzp_kvar,nzp_dom,nzp_counter,nzp_counter_parent,nzp_serv,dat_s,dat_po)" +
                " Select 0,a.nzp_kvar,a.nzp_dom,a.nzp_counter,a.nzp_counter_parent,a.nzp_serv,max(a.dat_uchet),min(b.dat_uchet)" +
                " From tpok a, tpok b" +
                " Where a.nzp_counter = b.nzp_counter" +
                "   and a.dat_uchet   < b.dat_uchet" +
                "   and not exists ( Select 1 From t_inscnt n Where a.nzp_counter = n.nzp_counter ) " +
                //"   and a.dat_uchet = ( Select c.dat_uchet From ta_br c Where c.nzp_counter = a.nzp_counter )" +
                //"   and a.dat_uchet = ( Select c.dat_uchet From ta_b c Where c.nzp_counter = a.nzp_counter )" +
                "   and a.dat_uchet = ( Select c.dat_uchet From ta_bi c Where c.nzp_counter = a.nzp_counter )" +
                "   and b.dat_uchet = ( Select p.dat_uchet From tb_b  p Where p.nzp_counter = b.nzp_counter )" +
                //"   and b.dat_uchet = ( Select p.dat_uchet From tb_br p Where p.nzp_counter = b.nzp_counter )" +
                " Group by 1,2,3,4,5,6 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod_prm.pref); return false; }
            ExecSQL(conn_db, sUpdStat + " t_inscnt ", true);

            //Надо УБРАТЬ учет таких периодов, ибо фигня получается (ситуация не обработана!!!)
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            //           |      |
            //             ^--^
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            ret = ExecSQL(conn_db,
                " insert into t_inscnt (nzp_cr,nzp_kvar,nzp_dom,nzp_counter,nzp_counter_parent,nzp_serv,dat_s,dat_po)" +
                " Select 0,a.nzp_kvar,a.nzp_dom,a.nzp_counter,a.nzp_counter_parent,a.nzp_serv,max(a.dat_uchet),min(b.dat_uchet)" +
                " From tpok a, tpok b" +
                " Where a.nzp_counter = b.nzp_counter" +
                "   and a.dat_uchet   < b.dat_uchet" +
                "   and not exists ( Select 1 From t_inscnt n Where a.nzp_counter = n.nzp_counter ) " +
                //"   and a.dat_uchet = ( Select c.dat_uchet From ta_br c Where c.nzp_counter = a.nzp_counter )" +
                "   and a.dat_uchet = ( Select c.dat_uchet From ta_b c Where c.nzp_counter = a.nzp_counter )" +
                "   and b.dat_uchet = ( Select p.dat_uchet From tb_mr p Where p.nzp_counter = b.nzp_counter )" +
                " Group by 1,2,3,4,5,6 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod_prm.pref); return false; }
            ExecSQL(conn_db, sUpdStat + " t_inscnt ", true);
            //дописываем непопавшие интервалы
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            //           |      |
            //           ^------^
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            ret = ExecSQL(conn_db,
                  " insert into t_inscnt (nzp_cr,nzp_kvar,nzp_dom,nzp_counter,nzp_counter_parent,nzp_serv,dat_s,dat_po)" +
                " Select 0,a.nzp_kvar,a.nzp_dom,a.nzp_counter,a.nzp_counter_parent,a.nzp_serv,max(a.dat_uchet),min(b.dat_uchet)" +
                " From tpok a, tpok b" +
                " Where a.nzp_counter = b.nzp_counter" +
                "   and a.dat_uchet   < b.dat_uchet" +
                "   and not exists ( Select 1 From t_inscnt n Where a.nzp_counter = n.nzp_counter ) " +
                //"   and a.dat_uchet = ( Select c.dat_uchet From ta_br c Where c.nzp_counter = a.nzp_counter )" +
                "   and a.dat_uchet = ( Select c.dat_uchet From ta_mr c Where c.nzp_counter = a.nzp_counter and  c.dat_uchet=" + rashod_prm.dat_s +")" +
                "   and b.dat_uchet = ( Select p.dat_uchet From tb_mr p Where p.nzp_counter = b.nzp_counter and  p.dat_uchet=" + rashod_prm.dat_po + ")" +
                " Group by 1,2,3,4,5,6 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod_prm.pref); return false; }

            ExecSQL(conn_db, " Create index ix2_t_inscnt on t_inscnt (nzp_kvar) ", true);
            ExecSQL(conn_db, " Create index ix3_t_inscnt on t_inscnt (nzp_counter,dat_s) ", true);
            ExecSQL(conn_db, " Create index ix4_t_inscnt on t_inscnt (nzp_counter,dat_po) ", true);
            ExecSQL(conn_db, sUpdStat + " t_inscnt ", true);

            #endregion Выборка показаний захватывающих расчетный месяц t_inscnt

            #region Выборка начального и конечного показаний ПУ
            //----------------------------------------------------------------
            //заполнение квартирных ПУ
            //----------------------------------------------------------------
            //выбрать все показания по лс
            //надо добавить индекс counters (nzp_counter,dat_uchet)

            ExecSQL(conn_db, " Drop table tpok_s ", false);
            ExecSQL(conn_db, " Drop table tpok_po ", false);

            ret = ExecSQL(conn_db,
                " Create temp table tpok_s " +
                " ( nzp_counter integer," +
                "   val_cnt " + sDecimalType + "(16,7)," +
                "   dat_uchet   date    " +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod_prm.pref); return false; }

            ret = ExecSQL(conn_db,
                " Insert into tpok_s (nzp_counter,dat_uchet,val_cnt) " +
                " Select b.nzp_counter," + sDatUchet + " as dat_uchet, max(a.val_cnt) val_cnt " +
                rashod_prm.p_FROM_tmp +
                "   and a.nzp_counter_child = b.nzp_counter " +
                "   and b.dat_s = " + sDatUchet +
                " group by 1,2 "
                , true, 300);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod_prm.pref); return false; }

            ret = ExecSQL(conn_db,
                " Create temp table tpok_po " +
                " ( nzp_counter integer," +
                "   val_cnt " + sDecimalType + "(16,7)," +
                "   dat_uchet   date    " +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod_prm.pref); return false; }

            ret = ExecSQL(conn_db,
                " Insert into tpok_po (nzp_counter,dat_uchet,val_cnt) " +
                " Select b.nzp_counter," + sDatUchet + " as dat_uchet, max(a.val_cnt) val_cnt " +
                rashod_prm.p_FROM_tmp +
                "   and a.nzp_counter_child = b.nzp_counter " +
                "   and b.dat_po = " + sDatUchet +
                " group by 1,2 "
                , true, 300);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod_prm.pref); return false; }

            ExecSQL(conn_db, " Create unique index ix_tpok_s  on tpok_s  (nzp_counter,dat_uchet) ", true);
            ExecSQL(conn_db, " Create unique index ix_tpok_po on tpok_po (nzp_counter,dat_uchet) ", true);
            ExecSQL(conn_db, sUpdStat + " tpok_s ", true);
            ExecSQL(conn_db, sUpdStat + " tpok_po ", true);


            #endregion Выборка начального и конечного показаний ПУ

            #region Установка начального и конечного показаний ПУ

            ViewTbl(conn_db, " select * from t_inscnt order by nzp_serv,nzp_counter ");

            ret = ExecSQL(conn_db,
                " Update t_inscnt " +
                " Set val_s = ( Select max(val_cnt) From tpok_s a Where t_inscnt.nzp_counter = a.nzp_counter " +
                                                                  " and t_inscnt.dat_s = a.dat_uchet ) " +
                rashod_prm.p_UPDdt_s +
                " Where 1 = 1 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod_prm.pref); return false; }

            ret = ExecSQL(conn_db,
                " Update t_inscnt " +
                " Set val_po = ( Select max(val_cnt) From tpok_po a Where t_inscnt.nzp_counter = a.nzp_counter " +
                                                                    " and t_inscnt.dat_po = a.dat_uchet ) " +
                rashod_prm.p_UPDdt_po +
                " Where 1 = 1 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod_prm.pref); return false; }

            /*
            string sql =
                    " Update " + rashod_prm.counters_xx +
                    " Set val_s = ( Select max(val_cnt) From tpok_s a Where " + rashod_prm.counters_xx + ".nzp_counter = a.nzp_counter " +
                                                                    "   and " + rashod_prm.counters_xx + ".dat_s = a.dat_uchet ) " +
                    rashod_prm.p_UPDdt_s +
                    " Where nzp_type = " + rashod_prm.p_type + " and stek = 1 and " + rashod_prm.p_where + rashod_prm.paramcalc.per_dat_charge;

            if (rashod_prm.paramcalc.nzp_kvar > 0 || rashod_prm.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, rashod_prm.counters_xx, "nzp_cntx", sql, 50000, " ", out ret);
            }
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod_prm.pref); return false; }

            sql =
                    " Update " + rashod_prm.counters_xx +
                    " Set val_po = ( Select max(val_cnt) From tpok_po a Where " + rashod_prm.counters_xx + ".nzp_counter = a.nzp_counter " +
                                                                      "   and " + rashod_prm.counters_xx + ".dat_po = a.dat_uchet ) " +
                    rashod_prm.p_UPDdt_po +
                    " Where nzp_type = " + rashod_prm.p_type + " and stek = 1 and " + rashod_prm.p_where + rashod_prm.paramcalc.per_dat_charge;

            if (rashod_prm.paramcalc.nzp_kvar > 0 || rashod_prm.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, rashod_prm.counters_xx, "nzp_cntx", sql, 50000, " ", out ret);
            }
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod_prm.pref); return false; }
            */

            ExecSQL(conn_db, " Drop table tpok_s ", false);
            ExecSQL(conn_db, " Drop table tpok_po ", false);
            //ExecSQL(conn_db, " Drop table t_selkvar ", false); -- anes

            #endregion Установка начального и конечного показаний ПУ

            #region Заполнение постоянных таблиц с расходами rashod.counters_xx на основе t_inscnt

            ViewTbl(conn_db, " select * from t_inscnt order by nzp_serv,nzp_counter ");

            string sql =
                    " Insert into " + rashod_prm.counters_xx +
                    " ( stek,nzp_type,dat_charge,nzp_kvar,nzp_dom,nzp_counter,nzp_serv,dat_s,dat_po,val_s,val_po,val2,ngp_cnt ) " +
                    " Select " + sstek + "," + rashod_prm.p_type + ", " + p_dat_charge + ",nzp_kvar,nzp_dom,nzp_counter_parent,nzp_serv,dat_s,dat_po,val_s,val_po,val2,ngp_cnt " +
                    " From t_inscnt Where 1=1 ";

            if (rashod_prm.paramcalc.nzp_kvar > 0 || rashod_prm.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql, true);
            }
            else
            {
                ExecByStep(conn_db, "t_inscnt", "nzp_cr", sql, 100000, " ", out ret);
            }
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod_prm.pref); return false; }

            #endregion Заполнение постоянных таблиц с расходами rashod.counters_xx на основе t_inscnt

            #region Удалить временные таблицы tpok t_inscnt ttt_aid_c1

            ExecSQL(conn_db, " Drop table tpok ", false);
            ExecSQL(conn_db, " Drop table t_inscnt ", false);
            ExecSQL(conn_db, " Drop table ttt_aid_c1 ", false);

            ExecSQL(conn_db, sUpdStat + " " + rashod_prm.tab, true);

            #endregion Удалить временные таблицы tpok t_inscnt ttt_aid_c1

            return true;
        }

        #region Старая Функция (LoadVals) выборки значений счетчиков используя структуру Rashod2 - комментарий
        /*
        //--------------------------------------------------------------------------------
        bool LoadVals(IDbConnection conn_db, Rashod2 rashod2, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            //выберем показания, которые полностью покрывают месяц!
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            //           |      |
            //     ^-------------------^
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            string sql =
            rashod2.p_INSERT +
            "   and a.dat_uchet = ( Select max(c.dat_uchet) From " + rashod2.tab + " c " +
                                  " Where c.nzp_counter = a.nzp_counter " +
                                  "   and c.dat_uchet <= " + rashod2.dat_s +
                                  "   " + rashod2.p_ACTUAL + " ) " +
            "   and b.dat_uchet = ( Select min(c.dat_uchet) From " + rashod2.tab + " c " +
                                  " Where c.nzp_counter = a.nzp_counter " +
                                  "   and c.dat_uchet > " + rashod2.dat_po +
                                  "   " + rashod2.p_ACTUAL + " ) ";

            if (rashod2.paramcalc.nzp_kvar > 0 || rashod2.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql + " Group by 1,2,3,4,5,6,7 ", true);
            }
            else
            {
                ExecByStep(conn_db, rashod2.p_TAB, rashod2.p_KEY, sql, 100000, " Group by 1,2,3,4,5,6,7 ", out ret);
            }
            if (!ret.result) { return false; }

            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            //выбор других интервалов, кроме уже выбранных в counters_xx
            //придеться изголяться, чтобы выбрать ближайшие показания (избежать выбора большого интервала)
            //зато понятно
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            //           |      |
            //   ^-----------^
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            ExecSQL(conn_db, " Drop table ttt_aid_c1 ", false);

            ret = ExecSQL(conn_db,
#if PG
                " Select distinct nzp_counter Into temp ttt_aid_c1 From " + rashod2.counters_xx
#else
                " Select unique nzp_counter From " + rashod2.counters_xx +
                " Into temp ttt_aid_c1 With no log "
#endif
                , true);
            if (!ret.result) { return false; }

            ret = ExecSQL(conn_db," Create unique index ix_aid22_ttt1 on ttt_aid_c1 (nzp_counter) ", true);
            if (!ret.result) { ExecSQL(conn_db, " Drop table ttt_aid_c1 ", false); return false; }

            sql =
            rashod2.p_INSERT +
            "   and 1 > ( Select count(*) From ttt_aid_c1 n Where a.nzp_counter = n.nzp_counter ) " +
            "   and a.dat_uchet = ( Select max(c.dat_uchet) From " + rashod2.tab + " c " +
                                  " Where c.nzp_counter = a.nzp_counter " +
                                  "   and c.dat_uchet <= " + rashod2.dat_s +
                                  "   " + rashod2.p_ACTUAL + " ) " +
            "   and b.dat_uchet = ( Select max(c.dat_uchet) From " + rashod2.tab + " c " +
                                  " Where c.nzp_counter = b.nzp_counter " +
                                  "   and c.dat_uchet <= " + rashod2.dat_po +
                                  "   " + rashod2.p_ACTUAL + " ) ";

            if (rashod2.paramcalc.nzp_kvar > 0 || rashod2.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql + " Group by 1,2,3,4,5,6,7 ", true);
            }
            else
            {
                ExecByStep(conn_db, rashod2.p_TAB, rashod2.p_KEY, sql, 100000, " Group by 1,2,3,4,5,6,7 ", out ret);
            }
            if (!ret.result) { ExecSQL(conn_db, " Drop table ttt_aid_c1 ", false); return false; }

            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            //           |      |
            //              ^-------------^
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            ExecSQL(conn_db, " Drop table ttt_aid_c1 ", false);

            ret = ExecSQL(conn_db,
#if PG
                " Select distinct nzp_counter Into temp ttt_aid_c1 From " + rashod2.counters_xx
#else
                " Select unique nzp_counter From " + rashod2.counters_xx +
                " Into temp ttt_aid_c1 With no log "
#endif
                , true);
            if (!ret.result) { return false; }

            ret = ExecSQL(conn_db," Create unique index ix_aid22_ttt1 on ttt_aid_c1 (nzp_counter) ", true);
            if (!ret.result) { ExecSQL(conn_db, " Drop table ttt_aid_c1 ", false); return false; }

            sql =
            rashod2.p_INSERT +
            "   and 1 > ( Select count(*) From ttt_aid_c1 n Where a.nzp_counter = n.nzp_counter ) " +
            "   and a.dat_uchet = ( Select min(c.dat_uchet) From " + rashod2.tab + " c " +
                                  " Where c.nzp_counter = a.nzp_counter " +
                                  "   and c.dat_uchet >= " + rashod2.dat_s +
                                  "   " + rashod2.p_ACTUAL + " ) " +
            "   and b.dat_uchet = ( Select min(c.dat_uchet) From " + rashod2.tab + " c " +
                                  " Where c.nzp_counter = b.nzp_counter " +
                                  "   and c.dat_uchet >= " + rashod2.dat_po +
                                  "   " + rashod2.p_ACTUAL + " ) ";

            if (rashod2.paramcalc.nzp_kvar > 0 || rashod2.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql + " Group by 1,2,3,4,5,6,7 ", true);
            }
            else
            {
                ExecByStep(conn_db, rashod2.p_TAB, rashod2.p_KEY, sql, 100000, " Group by 1,2,3,4,5,6,7 ", out ret);
            }
            if (!ret.result) { ExecSQL(conn_db, " Drop table ttt_aid_c1 ", false); return false; }

            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            //           |      |
            //             ^--^
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            ExecSQL(conn_db, " Drop table ttt_aid_c1 ", false);

            ret = ExecSQL(conn_db,
#if PG
                " Select distinct nzp_counter Into temp ttt_aid_c1 From " + rashod2.counters_xx
#else
                " Select unique nzp_counter From " + rashod2.counters_xx +
                " Into temp ttt_aid_c1 With no log "
#endif
                ,true);
            if (!ret.result) { return false; }

            ret = ExecSQL(conn_db, " Create unique index ix_aid22_ttt1 on ttt_aid_c1 (nzp_counter) ", true);
            if (!ret.result) { ExecSQL(conn_db, " Drop table ttt_aid_c1 ", false); return false; }

            sql =
            rashod2.p_INSERT +
                "   and 1 > ( Select count(*) From ttt_aid_c1 n Where a.nzp_counter = n.nzp_counter ) " +
                "   and a.dat_uchet = ( Select min(c.dat_uchet) From " + rashod2.tab + " c " +
                                      " Where c.nzp_counter = a.nzp_counter " +
                                      "   and c.dat_uchet >= " + rashod2.dat_s +
                                      "   " + rashod2.p_ACTUAL + " ) " +
                "   and b.dat_uchet = ( Select max(c.dat_uchet) From " + rashod2.tab + " c " +
                                      " Where c.nzp_counter = b.nzp_counter " +
                                      "   and c.dat_uchet <= " + rashod2.dat_po +
                                      "   " + rashod2.p_ACTUAL + " ) ";

            if (rashod2.paramcalc.nzp_kvar > 0 || rashod2.paramcalc.nzp_dom > 0)
            {
                ret = ExecSQL(conn_db, sql + " Group by 1,2,3,4,5,6,7 ", true);
            }
            else
            {
                ExecByStep(conn_db, rashod2.p_TAB, rashod2.p_KEY, sql, 100000, " Group by 1,2,3,4,5,6,7 ", out ret);
            }
            if (!ret.result) { ExecSQL(conn_db, " Drop table ttt_aid_c1 ", false); return false; }

            ExecSQL(conn_db, " Drop table ttt_aid_c1 ", false);

            return true;
        }
        */
        #endregion Старая Функция (LoadVals) выборки значений счетчиков используя структуру Rashod2 - комментарий

        #endregion Функция (LoadValsNew) выборки значений счетчиков используя структуру Rashod2

        #region вставка ИПУ по лс - stek=3 & nzp_type = 3
        public bool CalcRashodSetPUInStek3(IDbConnection conn_db, Rashod rashod, string p_dat_charge, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            //----------------------------------------------------------------
            //итого по лс - stek=3 & nzp_type = 3

            //используется ttt_counters_ipu - состоит из:
            //   [стеки 1/11 - расходы ИПУ по периодам] + [стеки 9/19 - расходы ИПУ из средних (стек=2) по периодам]
            // стеки 1/11 и 9/19 - не пересекаются! 1 и 9 без подневного расчета & 11 и 19 с подневным расчетом!

            //----------------------------------------------------------------
            // cnt1 - кол-во жильцов (целое)
            // gil1 - кол-во жильцов (с учетом времен. выбывших, дробное)
            // val1 - нормативные расходы (уже заполнены всем)
            // val2 - расходы КПУ
            // squ1 - площадь лс (уже заполнены)
            // rvirt - вирт. расход

            // 87 постановление:
            // dop87 - 7 кВт или добавок  (87 П)
            // gl7kw - 7 кВт КПУ (учитывая корректировку) 

            #region Выбрать расходы по ИПУ отдельно + дополнение расходов из стека=2
            //
            // ... beg проставить ИПУ в stek = 3 ...
            //
            #region проставить средние для подневных расходов - для "дырок" с kod_info = 9902
            //текущий расчетный месяц
            var calcMonth = new DateTime(rashod.paramcalc.calc_yy, rashod.paramcalc.calc_mm, 1);
            var lastDayCurMonth = new DateTime(rashod.paramcalc.calc_yy, rashod.paramcalc.calc_mm, DateTime.DaysInMonth(calcMonth.Year, calcMonth.Month));

            
            //учет среднего в соответствии с параметром 1390|Учитывать среднее значение ИПУ после даты закрытия (п.354)|||bool||5||||
            //берем кусочек среднего для ПУ, у которого часть периода не считается по показанию
            ret = ExecSQL(conn_db,
                " Update ttt_counters_ipu i" +
                " Set kod_info = 9903, stek=19, val1 = c.val1* cntd/cntd_mn" +
                " From " + rashod.counters_xx + " c, temp_cnt_spis cs  " +
                " Where c.nzp_type = 3 and c.stek = 2 " +
                " and c.nzp_counter=i.nzp_counter" +
                //если выключен учет среднего по закрытым ПУ, то кусочки периодов после даты закрытия не берем
                " and c.nzp_counter=cs.nzp_counter " + (rashod.paramcalc.enableAvgOnClosedPU ? string.Empty : " and cs.dat_close>i.dp")+
                " and c.kod_info=0 " +
                " and i.stek=11 and i.kod_info = 9902" +
                " and i.dp>=c.dat_s and i.dp_end<=c.dat_po" +
                " and not exists (select 1 " + //учитываем среднее только в случае, если у ПУ нет замены с показаниями в конкретном периоде
                "                 from t_old_new_counters o, ttt_counters_ipu ii " +
                "                 where o.nzp_counter=ii.nzp_counter and i.nzp_counter=o.nzp_counter_old" +
                "                 and ii.dp=i.dp and ii.dp_end=i.dp_end and ii.kod_info=0) "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            //проставляем кусочки среднего для замененного ПУ от родительского ПУ
            ret = ExecSQL(conn_db,
             " Update ttt_counters_ipu i" +
             " Set kod_info = 9903, stek=19, val1 = c.val1* cntd/cntd_mn" +
             " From " + rashod.counters_xx + " c, temp_cnt_spis cs, t_old_new_counters o  " +
             " Where c.nzp_type = 3 and c.stek = 2 " +
             " and o.nzp_counter=i.nzp_counter" +
             " and o.nzp_counter_old=c.nzp_counter" +
                //если выключен учет среднего по закрытым ПУ, то кусочки периодов после даты закрытия не берем
             " and i.nzp_counter=cs.nzp_counter " + (rashod.paramcalc.enableAvgOnClosedPU ? string.Empty : " and cs.dat_close>i.dp") +
             " and c.kod_info=0 " +
             " and i.stek=11 and i.kod_info = 9902" +
             " and i.dp>=c.dat_s and i.dp_end<=c.dat_po" +
             " and not exists (select 1 " + //учитываем среднее только в случае, если у ПУ нет замены с показаниями в конкретном периоде
             "                 from t_old_new_counters o, ttt_counters_ipu ii " +
             "                 where o.nzp_counter=ii.nzp_counter and i.nzp_counter=o.nzp_counter_old" +
             "                 and ii.dp=i.dp and ii.dp_end=i.dp_end and ii.kod_info=0) "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

         
            // если нет подходящего интервала - удалить "дырки" с kod_info = 9902!
            ret = ExecSQL(conn_db,
                " delete from ttt_counters_ipu " +
                " where stek=11 and kod_info = 9902 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            #endregion проставить средние для подневных расходов - для "дырок" с kod_info = 9902

            #region добавить средние для ИПУ, у которых нет показаний


            ExecSQL(conn_db, " DROP TABLE ttt_ans_kpu ", false);
            //получаем ПУ по которым есть показания
            var sql = " CREATE TEMP TABLE ttt_ans_kpu AS " +
                      " SELECT DISTINCT nzp_counter FROM ttt_counters_ipu";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            //дописываем связанные ПУ, по заменам для которых есть показания
            //считаем так: если есть показание по одному ПУ, то есть показания по всем связанным ПУ
            sql = " INSERT INTO ttt_ans_kpu (nzp_counter)" +
                  " SELECT DISTINCT r.nzp_counter FROM t_old_new_counters r" +
                  " WHERE EXISTS (SELECT 1 FROM t_old_new_counters rr, ttt_counters_ipu ii" +
                  "              WHERE rr.nzp_counter=ii.nzp_counter " +
                  "              AND rr.nzp_counter_old=r.nzp_counter_old)";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " Create index ix1_ttt_ans_kpu on ttt_ans_kpu (nzp_counter) ", true);


            ExecSQL(conn_db, " Drop table ttt_aid_virt0 ", false);
            ret = ExecSQL(conn_db,
             " Create temp table ttt_aid_virt0 " +
             " ( nzp_cntx integer, " +
             "   nzp_kvar integer, " +
             "   nzp_serv integer, " +
             "   nzp_counter integer, " +
             "   dat_s  date not null," +
             "   dat_po date not null," +
             "   val1   " + sDecimalType + "(15,7) default 0.00," +
             "   val3   " + sDecimalType + "(15,7) default 0.00," +
             "   val4   " + sDecimalType + "(15,7) default 0.00," +
             "   rvirt  " + sDecimalType + "(15,7) default 0.00," +
             "   nzp_dom  integer, " +
             "   val_s   " + sDecimalType + "(15,7) default 0.00," +
             "   val_po  " + sDecimalType + "(15,7) default 0.00," +
             "   ngp_cnt " + sDecimalType + "(15,7) default 0.00," +
             "   cnt_stage integer, " +
             "   mmnog     integer, " +
             "   kod_info  integer, " +
             "   nzp_period integer, " +
             "   dp       " + sDateTimeType + ", " +
             "   dp_end   " + sDateTimeType + ", " +
             "   cntd integer," +
             "   cntd_mn integer " +
             " )  " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            //дописываем средние расходы по ПУ, если нет показаний 
            ret = ExecSQL(conn_db,
                " Insert into ttt_aid_virt0 " +
                 "(nzp_cntx,nzp_kvar,nzp_serv,nzp_counter,dat_s,dat_po,val1,val3,val4,rvirt," +
                  "nzp_dom,val_s,val_po,ngp_cnt,cnt_stage,mmnog,kod_info," +
                  "nzp_period,dp,dp_end,cntd,cntd_mn) " +
                " Select " +
                 " nzp_cntx,nzp_kvar,nzp_serv,nzp_counter,dat_s,dat_po,val1,val3,val4,rvirt," +
                  "nzp_dom,val_s,val_po,ngp_cnt,cnt_stage,mmnog,kod_info," +
                  " 0," + rashod.paramcalc.dat_s + "," + rashod.paramcalc.dat_po + ", " +
                  DateTime.DaysInMonth(rashod.paramcalc.calc_yy, rashod.paramcalc.calc_mm) + ", " +
                  DateTime.DaysInMonth(rashod.paramcalc.calc_yy, rashod.paramcalc.calc_mm) +
                " From " + rashod.counters_xx + " c " +
                " Where nzp_type = 3 and stek = 2 and kod_info=0 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                " and not exists (select 1 from ttt_ans_kpu b where c.nzp_counter=b.nzp_counter)" 
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " Create index ix1_ttt_aid_virt0 on ttt_aid_virt0 (nzp_kvar) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_aid_virt0 ", true);

            ret = ExecSQL(conn_db,
                " Insert into ttt_counters_ipu " +
                 "(nzp_cntx,nzp_kvar,nzp_serv,nzp_counter,stek,dat_s,dat_po,val1,val3,val4,rvirt," +
                  "nzp_dom,val_s,val_po,ngp_cnt,cnt_stage,mmnog,kod_info," +
                  "nzp_period,dp,dp_end,cntd,cntd_mn) " +
                " Select " +
                 " a.nzp_cntx,a.nzp_kvar,a.nzp_serv,a.nzp_counter,9 stek,a.dat_s,a.dat_po,a.val1,a.val3,a.val1,a.rvirt," +
                 " a.nzp_dom,a.val_s,a.val_po,a.ngp_cnt,a.cnt_stage,a.mmnog,a.kod_info," +
                 " a.nzp_period,a.dp,a.dp_end,a.cntd,a.cntd_mn " +
                " From ttt_aid_virt0 a "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " Insert into ttt_counters_ipu " +
                 "(nzp_kvar,nzp_serv,nzp_counter,stek,dat_s,dat_po,val1,val3,val4,rvirt," +
                  "nzp_dom,val_s,val_po,ngp_cnt,cnt_stage,mmnog,kod_info," +
                  "nzp_period,dp,dp_end,cntd,cntd_mn) " +
                " Select " +
                 " a.nzp_kvar,a.nzp_serv,a.nzp_counter,19 stek,a.dat_s,a.dat_po,a.val1 * (k.cntd * 1.00 / k.cntd_mn),a.val3,a.val1,a.rvirt," +
                 " a.nzp_dom,a.val_s,a.val_po,a.ngp_cnt,a.cnt_stage,a.mmnog,a.kod_info," +
                 " k.nzp_period,k.dp,k.dp_end,k.cntd,k.cntd_mn " +
                " From ttt_aid_virt0 a, t_gku_periods k " +
                " Where k.nzp_kvar=a.nzp_kvar and k.typ>0 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " Create index ix3_ttt_counters_ipu on ttt_counters_ipu (nzp_kvar,nzp_serv,nzp_counter) ", true);
            ExecSQL(conn_db, " Create index ix4_ttt_counters_ipu on ttt_counters_ipu (dat_s,dat_po) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_counters_ipu ", true);

            ExecSQL(conn_db, " Drop table ttt_aid_virt0 ", false);
            // stek=19 нет в БД -> Insert
            ret = ExecSQL(conn_db,
                " Insert into " + rashod.counters_xx +
                " (nzp_kvar,nzp_serv,nzp_counter,stek,dat_charge,dat_s,dat_po,mmnog,cnt_stage,val1,val3,val4,rvirt," +
                  "nzp_type,nzp_dom,val_s,val_po,ngp_cnt,kod_info,cls1,cls2)" +
                " Select " +
                  "nzp_kvar,nzp_serv,nzp_counter,stek," + p_dat_charge + ",dp,dp_end,mmnog,cnt_stage,val1,val3,val4,rvirt," +
                  "3,nzp_dom,val_s,val_po,ngp_cnt,kod_info,cntd,cntd_mn " +
                " From ttt_counters_ipu " +
                " Where stek=19 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            #endregion добавить средние для ИПУ, у которых нет показаний

            #endregion Выбрать расходы по ИПУ отдельно + дополнение расходов из стека=2

            #region убрать ИПУ если их несколько для ЛС и по каким-то нет показаний
            
            
            // убрать ИПУ если их несколько для ЛС и по каким-то нет показаний
            // ... для РТ надо подумать как лучше??? с периода ??? dat_close лежит в counters! ...
            if (false)
            {
                ExecSQL(conn_db, " Drop table ttt_aid_kpu ", false);

                ret = ExecSQL(conn_db,
                    " Create temp table ttt_aid_kpu " +
                    " ( nzp_kvar integer, " +
                    "   nzp_serv integer, " +
                    "   nzp_counter integer " +
                    " )  " + sUnlogTempTable
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // выбрать ИПУ по ЛС и услуге, по которые не учтены
                ret = ExecSQL(conn_db,
                    " Insert into ttt_aid_kpu (nzp_kvar,nzp_serv,nzp_counter)" +
                    " Select s.nzp,s.nzp_serv,s.nzp_counter " +
                    " From temp_cnt_spis s  " +
                    " Where s.nzp_type=3 and s.dat_close is Null " +
                    " and NOT EXISTS (select 1 From ttt_counters_ipu c Where c.nzp_kvar = s.nzp and c.nzp_serv = s.nzp_serv and c.nzp_counter = s.nzp_counter) "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                ExecSQL(conn_db, " Create index ix_aid33_kpu on ttt_aid_kpu (nzp_kvar,nzp_serv) ", true);
                ExecSQL(conn_db, sUpdStat + " ttt_aid_kpu ", true);

                // исключить ИПУ из расчета, по ЛС и услуге по которым есть действующие ИПУ без расходов в месяце
                ret = ExecSQL(conn_db,
                    " delete from ttt_counters_ipu " +
                    " where EXISTS (select 1 From ttt_aid_kpu c Where c.nzp_kvar = ttt_counters_ipu.nzp_kvar and c.nzp_serv = ttt_counters_ipu.nzp_serv) "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            }

            #endregion убрать ИПУ если их несколько для ЛС и по каким-то нет показаний

            #region Вставить ИПУ + Если ИПУ снят в середине месяца - часть месяца считать по нормативу

            #region Выбрать расход ИПУ по ЛС/услуге

            ExecSQL(conn_db, " Drop table ttt_aid_kpu ", false);
            ExecSQL(conn_db, " Drop table ttt_aid_kpud ", false);

            // расход ИПУ для ЛС, где нет расчета по дням
            ret = ExecSQL(conn_db,
                " Create temp table ttt_aid_kpu " +
                " ( nzp_kvar integer, " +
                "   nzp_serv integer, " +
                "   stek     integer, " +
                "   squ      " + sDecimalType + "( 8,2) default 0, " +
                "   val1     " + sDecimalType + "(15,7) default 0.00," +
                "   val1_mid " + sDecimalType + "(15,7) default 0.00," +
                "   days_mid integer default 0," +
                "   val3     " + sDecimalType + "(15,7) default 0.00," +
                "   rvirt    " + sDecimalType + "(15,7) default 0.00 " +
                " )  " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // расход ИПУ для ЛС, где есть расчет по дням
            ret = ExecSQL(conn_db,
                " Create temp table ttt_aid_kpud " +
                " ( nzp_kvar integer, " +
                "   nzp_serv integer, " +
                "   stek     integer, " +
                "   squ      " + sDecimalType + "( 8,2) default 0, " +
                "   val1     " + sDecimalType + "(15,7) default 0.00," +
                "   val3     " + sDecimalType + "(15,7) default 0.00," +
                "   rvirt    " + sDecimalType + "(15,7) default 0.00," +
                "   nzp_period integer, " +
                "   dp       " + sDateTimeType + ", " +
                "   dp_end   " + sDateTimeType + ", " +
                "   cntd integer," +
                "   cntd_mn integer " +
                " )  " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " Insert into ttt_aid_kpud (nzp_kvar,nzp_serv,stek,val1,val3,rvirt,nzp_period,dp,dp_end,cntd,cntd_mn) " +
                " Select nzp_kvar,nzp_serv,min(stek),sum(val1),sum(val3),sum(rvirt),nzp_period,max(dp),max(dp_end),max(cntd),max(cntd_mn) " +
                " From ttt_counters_ipu " +
                " Where stek in (11,19)" +
                " Group by 1,2,7 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ViewTbl(conn_db, " select * from ttt_aid_kpud order by nzp_kvar,nzp_serv ");

            ExecSQL(conn_db, " Create unique index ix_ttt_aid_kpud on ttt_aid_kpud (nzp_kvar,nzp_serv,dp,dp_end) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_aid_kpud ", true);

            ExecSQL(conn_db, " Drop table ttt_aid_virt0 ", false);
            ret = ExecSQL(conn_db,
             " Create temp table ttt_aid_virt0 " +
             " ( nzp_kvar integer, " +
             "   nzp_serv integer  " +
             " )  " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " Insert into ttt_aid_virt0 (nzp_kvar,nzp_serv) " +
                " Select nzp_kvar,nzp_serv " +
                " From ttt_aid_kpud " +
                " Group by 1,2 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " Create unique index ix1_ttt_aid_virt0 on ttt_aid_virt0 (nzp_kvar,nzp_serv) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_aid_virt0 ", true);

            ret = ExecSQL(conn_db,
                " Insert into ttt_aid_kpu (nzp_kvar, nzp_serv, stek, val1, val1_mid, days_mid, val3, rvirt) " +
                " Select a.nzp_kvar, a.nzp_serv, " +

                       " min(a.stek) as stek, " +
                       " sum(a.val1) as val1, " +

                       //показание в середине месяца
                       " 0 as val1_mid, " + //норматив
                       " max(case when a.stek =1 " +
                                  " and a.dat_s > " + rashod.paramcalc.dat_s +
                                  " and a.dat_s <=" + rashod.paramcalc.dat_po +
#if PG
 " then EXTRACT('days' from (a.dat_s - " + rashod.paramcalc.dat_s + " ))*1 else 0 end) as days_mid, " +
#else
                       " then (a.dat_s - " + rashod.paramcalc.dat_s + " )*1 else 0 end) as days_mid, " +
#endif
 " sum(a.val3) as val3, " +
                       " sum(a.rvirt) as rvirt " +

                " From ttt_counters_ipu a " +
                " Where a.stek in (1,9) " +
                " and not EXISTS (select 1 From ttt_aid_virt0 b " +
                          " Where b.nzp_kvar = a.nzp_kvar  " +
                          "   and b.nzp_serv = a.nzp_serv " +
                          " ) " +
                " Group by 1,2 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " Create unique index ix_ttt_aid_kpu on ttt_aid_kpu (nzp_kvar,nzp_serv) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_aid_kpu ", true);

            // проставить площадь для вычисления среднего на 1 кв.м по отоплению
            ret = ExecSQL(conn_db,
                " Update ttt_aid_kpu " +
                " Set squ = ( Select max(squ2) From ttt_counters_xx a Where ttt_aid_kpu.nzp_kvar = a.nzp_kvar ) " +
                " Where nzp_serv = 8 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " Update ttt_aid_kpud " +
                " Set squ = ( Select max(squ2) From ttt_counters_xx a Where ttt_aid_kpud.nzp_kvar = a.nzp_kvar ) " +
                " Where nzp_serv = 8 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // расход на 1 кв.метр для отопления
            ret = ExecSQL(conn_db,
                " Update ttt_aid_kpu " +
                " Set val3 = val1 / squ " +
                " Where nzp_serv = 8 and squ > 0 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " Update ttt_aid_kpud " +
                " Set val3 = val1 / squ " +
                " Where nzp_serv = 8 and squ > 0 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            if (Points.IsSmr) // округлить для Самары т.к. отгображение в СФ до 4х знаков
            {
                ret = ExecSQL(conn_db,
                    " Update ttt_aid_kpu " +
                    " Set val1=round(val1,4), rvirt=round(rvirt,4) " +
                    " Where nzp_serv <> 8  "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            }
            #endregion Выбрать расход ИПУ по ЛС/услуге

            #region Проставить расход ИПУ если нет по-дневного расчета
            // для всех услуг кроме отопления
             sql =
                    " Update ttt_counters_xx " +  //cnt_stage = 1 - признак наличия КПУ / = 9 - среднее ИПУ
                    " Set" +
#if PG
 " val2 = a.val1," +
                    " rvirt = a.rvirt," +
                    " cnt_stage = a.stek " +
                    "  From ttt_aid_kpu a " +
                    " Where ttt_counters_xx.nzp_kvar = a.nzp_kvar  " +
                    "   and ttt_counters_xx.nzp_serv = a.nzp_serv " +
#else
                    " (val2,rvirt,cnt_stage) = (( " +  //cnt_stage = 1 - признак наличия КПУ / = 9 - среднее ИПУ
                    " Select a.val1 From ttt_aid_kpu a " +
                    " Where ttt_counters_xx.nzp_kvar = a.nzp_kvar  " +
                    "   and ttt_counters_xx.nzp_serv = a.nzp_serv " +
                    " )," +
                    " (Select a.rvirt From ttt_aid_kpu a " +
                    " Where ttt_counters_xx.nzp_kvar = a.nzp_kvar  " +
                    "   and ttt_counters_xx.nzp_serv = a.nzp_serv " +
                    " )," +
                    " (Select a.stek" +
                    "  From ttt_aid_kpu a " +
                    " Where ttt_counters_xx.nzp_kvar = a.nzp_kvar  " +
                    "   and ttt_counters_xx.nzp_serv = a.nzp_serv " +
                    " )) " +
                    " Where EXISTS (select 1 From ttt_aid_kpu a " +
                                " Where ttt_counters_xx.nzp_kvar = a.nzp_kvar  " +
                                "   and ttt_counters_xx.nzp_serv = a.nzp_serv " +
                                " ) " +
#endif
 " and ttt_counters_xx.stek = 3 ";

            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // для отопления val4 - расход ГКал на кв.метр
            sql =
                    " Update ttt_counters_xx " +
                    " Set val4 =" +
#if PG
 " a.val3" +
                    " From ttt_aid_kpu a " +
                    " Where ttt_counters_xx.nzp_kvar = a.nzp_kvar " +
                    "   and ttt_counters_xx.nzp_serv = a.nzp_serv " +
#else
                    " (Select a.val3 From ttt_aid_kpu a " +
                    " Where ttt_counters_xx.nzp_kvar = a.nzp_kvar  " +
                    "   and ttt_counters_xx.nzp_serv = a.nzp_serv )" +
                    " Where EXISTS (select 1 From ttt_aid_kpu a " +
                                " Where ttt_counters_xx.nzp_kvar = a.nzp_kvar  " +
                                "   and ttt_counters_xx.nzp_serv = a.nzp_serv " +
                                " ) " +
#endif
 " and ttt_counters_xx.stek = 3  and ttt_counters_xx.nzp_serv = 8";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            #endregion Проставить расход ИПУ если нет по-дневного расчета

            #region Если ИПУ снят в середине месяца - часть месяца считать по нормативу - если не по-дневной расчет!
            //надо заполнить val1_mid
            ret = ExecSQL(conn_db,
                " Update ttt_aid_kpu " +
                " Set val1_mid = " +
                 " ( Select max(val1) From " + rashod.counters_xx + " a" +
                  " Where ttt_aid_kpu.nzp_kvar = a.nzp_kvar  " +
                  "   and ttt_aid_kpu.nzp_serv = a.nzp_serv " +
                  "   and a.nzp_type = 3 and a.stek = 30 " +
                  "   and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                  " ) " +
                " Where EXISTS (select 1 From " + rashod.counters_xx + " a" +
                 " Where ttt_aid_kpu.nzp_kvar = a.nzp_kvar " +
                 "   and ttt_aid_kpu.nzp_serv = a.nzp_serv " +
                 "   and a.nzp_type = 3 and a.stek = 30 " +
                 "   and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                 " ) "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            //уменьшим норматив для показаний в середине месяца для всех услуг кроме отопления 
            sql =
                " Update ttt_counters_xx " +
                " Set " +
                " (kod_info,cnt5,val1,val1_g) = (( " +
                " Select 9901 From ttt_aid_kpu a " +
                " Where ttt_counters_xx.nzp_kvar = a.nzp_kvar  " +
                "   and ttt_counters_xx.nzp_serv = a.nzp_serv " +
                "   and days_mid > 0 " +
                " )," +
                " (Select  max(days_mid) From ttt_aid_kpu a " +
                " Where ttt_counters_xx.nzp_kvar = a.nzp_kvar  " +
                "   and ttt_counters_xx.nzp_serv = a.nzp_serv " +
                "   and days_mid > 0 " +
                " )," +
                " (Select max(val1_mid/" + DateTime.DaysInMonth(rashod.paramcalc.calc_yy, rashod.paramcalc.calc_mm) + " * days_mid) From ttt_aid_kpu a " +
                " Where ttt_counters_xx.nzp_kvar = a.nzp_kvar  " +
                "   and ttt_counters_xx.nzp_serv = a.nzp_serv " +
                "   and days_mid > 0 " +
                " )," +
                " (Select max(val1_mid/" + DateTime.DaysInMonth(rashod.paramcalc.calc_yy, rashod.paramcalc.calc_mm) + " * days_mid) " +
                " From ttt_aid_kpu a " +
                " Where ttt_counters_xx.nzp_kvar = a.nzp_kvar  " +
                "   and ttt_counters_xx.nzp_serv = a.nzp_serv " +
                "   and days_mid > 0 " +
                " )) " +
                " Where EXISTS (select 1 From ttt_aid_kpu a " +
                            " Where ttt_counters_xx.nzp_kvar = a.nzp_kvar  " +
                            "   and ttt_counters_xx.nzp_serv = a.nzp_serv " +
                            "   and days_mid > 0 " +
                            " ) " +
                "   and nzp_type = 3 and stek = 3 " +
                "   and nzp_serv <> 8 ";

            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            #endregion Если ИПУ снят в середине месяца - часть месяца считать по нормативу - если не по-дневной расчет!

            #region Проставить расход ИПУ для стека=10 - по-дневного расчета
            // для всех услуг
            sql =
                    " Update ttt_counters_xx " +
                    " Set " +  //cnt_stage = 1 - признак наличия КПУ / = 9 - среднее ИПУ
#if PG
                    " val2 = a.val1," +
                    " rvirt = a.rvirt," +
                    " cnt_stage = (case when a.stek=11 then 1 else 9 end) " +
                    " From ttt_aid_kpud a " +
                    " Where ttt_counters_xx.nzp_kvar = a.nzp_kvar  " +
                    "   and ttt_counters_xx.nzp_serv = a.nzp_serv " +
                    "   and ttt_counters_xx.dat_s <=a.dp_end " +
                    "   and ttt_counters_xx.dat_po>=a.dp " +
#else
                    " (val2,rvirt,cnt_stage) = (( " +
                    " Select a.val1 From ttt_aid_kpud a " +
                    " Where ttt_counters_xx.nzp_kvar = a.nzp_kvar  " +
                    "   and ttt_counters_xx.nzp_serv = a.nzp_serv " +
                    "   and ttt_counters_xx.dat_s <=a.dp_end " +
                    "   and ttt_counters_xx.dat_po>=a.dp " +
                    " )," +
                    " (Select a.rvirt From ttt_aid_kpud a " +
                    " Where ttt_counters_xx.nzp_kvar = a.nzp_kvar  " +
                    "   and ttt_counters_xx.nzp_serv = a.nzp_serv " +
                    "   and ttt_counters_xx.dat_s <=a.dp_end " +
                    "   and ttt_counters_xx.dat_po>=a.dp " +
                    " )," +
                    " (Select (case when a.stek=11 then 1 else 9 end)" +
                    "  From ttt_aid_kpud a " +
                    " Where ttt_counters_xx.nzp_kvar = a.nzp_kvar  " +
                    "   and ttt_counters_xx.nzp_serv = a.nzp_serv " +
                    "   and ttt_counters_xx.dat_s <=a.dp_end " +
                    "   and ttt_counters_xx.dat_po>=a.dp " +
                    " )) " +
                    " Where EXISTS (select 1 From ttt_aid_kpud a " +
                                " Where ttt_counters_xx.nzp_kvar = a.nzp_kvar  " +
                                "   and ttt_counters_xx.nzp_serv = a.nzp_serv " +
                                "   and ttt_counters_xx.dat_s <=a.dp_end " +
                                "   and ttt_counters_xx.dat_po>=a.dp " +
                                " ) " +
#endif
 "  and ttt_counters_xx.stek = 10 ";

            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            sql =
                    " Update ttt_counters_xx " +
                    " Set val1 = 0,val1_g = 0 " +
                    " Where stek = 10 and cnt_stage in (1,9) ";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // для отопления val4 - расход ГКал на кв.метр
            sql =
                    " Update ttt_counters_xx " +
                    " Set val4 = " +
#if PG
 " a.val3 From ttt_aid_kpud a " +
                    " Where ttt_counters_xx.nzp_kvar = a.nzp_kvar " +
                    "   and ttt_counters_xx.nzp_serv = a.nzp_serv " +
                    "   and ttt_counters_xx.dat_s <=a.dp_end " +
                    "   and ttt_counters_xx.dat_po>=a.dp " +
#else
                    " (Select a.val3 From ttt_aid_kpud a " +
                    " Where ttt_counters_xx.nzp_kvar = a.nzp_kvar " +
                    "   and ttt_counters_xx.nzp_serv = a.nzp_serv " +
                    "   and ttt_counters_xx.dat_s <=a.dp_end " +
                    "   and ttt_counters_xx.dat_po>=a.dp ) " +
                    " Where EXISTS (select 1 From ttt_aid_kpud a " +
                                " Where ttt_counters_xx.nzp_kvar = a.nzp_kvar  " +
                                "   and ttt_counters_xx.nzp_serv = a.nzp_serv " +
                                "   and ttt_counters_xx.dat_s <=a.dp_end " +
                                "   and ttt_counters_xx.dat_po>=a.dp " +
                                " ) " +
#endif
 " and ttt_counters_xx.stek = 10 and ttt_counters_xx.nzp_serv = 8 ";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            #endregion Проставить расход ИПУ для стека=10 - по-дневного расчета

            #region перенести по-дневные расходы по ИПУ из стека=10 в стек=3

            ExecSQL(conn_db, " Drop table ttt_aid_kpud ", false);
            ExecSQL(conn_db, " Drop table ttt_aid_kpu ", false);
            ret = ExecSQL(conn_db,
                " Create temp table ttt_aid_kpud " +
                " ( nzp_kvar  integer, " +
                "   nzp_serv  integer  " +
                " )  " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " Create temp table ttt_aid_kpu " +
                " ( nzp_kvar  integer, " +
                "   nzp_serv  integer, " +
                "   cnt_stage integer, " +
                "   val1  " + sDecimalType + "(15,7) default 0.00," +
                "   val2  " + sDecimalType + "(15,7) default 0.00," +
                "   val3  " + sDecimalType + "(15,7) default 0.00," +
                "   val4  " + sDecimalType + "(15,7) default 0.00," +
                "   rvirt " + sDecimalType + "(15,7) default 0.00 " +
                " )  " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // собрать все ЛС, по которым есть ИПУ по дням
            ret = ExecSQL(conn_db,
                  " Insert into ttt_aid_kpud (nzp_kvar, nzp_serv) " +
                  " Select nzp_kvar, nzp_serv " +
                  " From ttt_counters_xx " +
                  " Where stek=10 and cnt_stage in (1,9) " +
                  " Group by 1,2 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " Create unique index ix_ttt_aid_kpud on ttt_aid_kpud (nzp_kvar,nzp_serv) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_aid_kpud ", true);

            // выбрать расходы по ЛС с ИПУ по дням - там может быть норматив по какому-либо периоду!
            ret = ExecSQL(conn_db,
                  " Insert into ttt_aid_kpu (nzp_kvar, nzp_serv, cnt_stage, val1, val2, val3, val4, rvirt) " +
                  " Select nzp_kvar, nzp_serv, " +
                  " max(cnt_stage) as cnt_stage, sum(val1) as val1, sum(val2) as val2, sum(val3) as val3," +
                  " sum(val4) as val4, sum(rvirt) as rvirt " +
                  " From ttt_counters_xx " +
                  " Where stek=10 " +
                  " and EXISTS (select 1 From ttt_aid_kpud a " +
                            " Where ttt_counters_xx.nzp_kvar = a.nzp_kvar  " +
                            "   and ttt_counters_xx.nzp_serv = a.nzp_serv " +
                            " ) " +
                  " Group by 1,2 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " Create unique index ix_ttt_aid_kpu on ttt_aid_kpu (nzp_kvar,nzp_serv) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_aid_kpu ", true);

            // установить стек=3 для ЛС, по которым есть ИПУ по дням
            sql =
                    " Update ttt_counters_xx " +
                    " Set " +  //cnt_stage = 1 - признак наличия КПУ / = 9 - среднее ИПУ
#if PG
                    " kod_info  = 9901," +
                    " val1      = a.val1," +
                    " val2      = a.val2," +
                    " val3      = a.val3," +
                    " val4      = a.val4," +
                    " rvirt     = a.rvirt," +
                    " cnt_stage = (case when a.cnt_stage=1 then 1 else 9 end) " +
                    " From ttt_aid_kpu a " +
                    " Where ttt_counters_xx.nzp_kvar = a.nzp_kvar  " +
                    "   and ttt_counters_xx.nzp_serv = a.nzp_serv " +
#else
                    " (kod_info,val1,val2,val3,val4,rvirt,cnt_stage) = (( " +
                    " Select 9901 From ttt_aid_kpu a " +
                    " Where ttt_counters_xx.nzp_kvar = a.nzp_kvar  " +
                    "   and ttt_counters_xx.nzp_serv = a.nzp_serv " +
                    " )," +
                    " (Select a.val1 From ttt_aid_kpu a " +
                    " Where ttt_counters_xx.nzp_kvar = a.nzp_kvar  " +
                    "   and ttt_counters_xx.nzp_serv = a.nzp_serv " +
                    " )," +
                    " (Select a.val2 From ttt_aid_kpu a " +
                    " Where ttt_counters_xx.nzp_kvar = a.nzp_kvar  " +
                    "   and ttt_counters_xx.nzp_serv = a.nzp_serv " +
                    " )," +
                    " (Select a.val3 From ttt_aid_kpu a " +
                    " Where ttt_counters_xx.nzp_kvar = a.nzp_kvar  " +
                    "   and ttt_counters_xx.nzp_serv = a.nzp_serv " +
                    " )," +
                    " (Select a.val4 From ttt_aid_kpu a " +
                    " Where ttt_counters_xx.nzp_kvar = a.nzp_kvar  " +
                    "   and ttt_counters_xx.nzp_serv = a.nzp_serv " +
                    " )," +
                    " (Select a.rvirt From ttt_aid_kpu a " +
                    " Where ttt_counters_xx.nzp_kvar = a.nzp_kvar  " +
                    "   and ttt_counters_xx.nzp_serv = a.nzp_serv " +
                    " )," +
                    " (Select (case when a.cnt_stage=1 then 1 else 9 end)" +
                    "  From ttt_aid_kpu a " +
                    " Where ttt_counters_xx.nzp_kvar = a.nzp_kvar  " +
                    "   and ttt_counters_xx.nzp_serv = a.nzp_serv " +
                    " )) " +
                    " Where EXISTS (select 1 From ttt_aid_kpu a " +
                                " Where ttt_counters_xx.nzp_kvar = a.nzp_kvar  " +
                                "   and ttt_counters_xx.nzp_serv = a.nzp_serv " +
                                " ) " +
#endif
 " and ttt_counters_xx.stek = 3 ";

            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            #endregion перенести по-дневные расходы по ИПУ из стека=10 в стек=3

            #endregion Вставить ИПУ + Если ИПУ снят в середине месяца - часть месяца считать по нормативу

            #region Обнулить расходы для kod_info <> 9901

            sql =
                    " Update ttt_counters_xx " +
                    " Set val1 = 0,val1_g = 0 " +
                    " Where nzp_type = 3 and stek = 3 " +
                    "   and kod_info <> 9901 and cnt_stage in (1,9) ";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            sql =
                    " Update ttt_counters_xx " +
                    " Set kod_info = 0 " +
                    " Where nzp_type = 3 and stek = 3 and kod_info > 0 ";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            #endregion Обнулить расходы для kod_info <> 9901

            return true;
        }
        #endregion вставка ИПУ по лс - stek=3 & nzp_type = 3

        #region коммуналки - уменьшим расход в пропорции общего кол-ва жильцов
        public bool CalcRashodLowPUforKommunal(IDbConnection conn_db, Rashod rashod, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            //----------------------------------------------------------------
            //коммуналки - уменьшим расход в пропорции общего кол-ва жильцов
            //----------------------------------------------------------------
            ExecSQL(conn_db, " Drop table ttt_aid_stat ", false);

            if (!Points.IsSmr)
            {
                ExecSQL(conn_db, " Drop table cnt_d ", false);
                ret = ExecSQL(conn_db,
                    " Create temp table cnt_d " +
                    " ( nzp_kvar integer, " +
                    "   nzp_serv integer  " +
                    " )  " + sUnlogTempTable
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                //найдем все пары коммуналок - НО только, если есть показания КПУ !!!
                //string aaa = (rashod.paramcalc.b_cur ? " is null " : " = MDY(" + rashod.paramcalc.cur_mm + ",28," + rashod.paramcalc.cur_yy + ") ");

                // из всего дома!
                ret = ExecSQL(conn_db,
                      " Insert Into cnt_d (nzp_kvar,nzp_serv) " + //признак коммуналки
                      " Select d.nzp_kvar,d.nzp_serv " +
                      " From " + rashod.counters_xx + " d " +
                      " Where d.nzp_type = 3 and d.stek = 3 and d." + rashod.where_dom +
                      "   and d.mmnog > 0 " +
                      " Group by 1,2 "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                ExecSQL(conn_db, " Create unique index ix_cnt_d on cnt_d (nzp_kvar,nzp_serv) ", true);
                ExecSQL(conn_db, sUpdStat + " cnt_d ", true);

                ExecSQL(conn_db, " Drop table ttt_aid_a ", false);
                ret = ExecSQL(conn_db,
                    " Create temp table ttt_aid_a " +
                    " ( nzp_kvar integer, " +
                    "   nzp_dom  integer, " +
                    "   nzp_serv integer, " +
                    "   nzp_counter integer, " +
                    "   nzp_type    integer, " +
                    "   nzp_cnttype integer, " +
                    "   num_cnt character(20), " +
                    "   gil1 " + sDecimalType + "(15,7) default 0.00, " +
                    "   val1 " + sDecimalType + "(15,7) default 0.00  " +
                    " )  " + sUnlogTempTable
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // из всего дома!
                ret = ExecSQL(conn_db,
                    " Insert Into ttt_aid_a (nzp_kvar,nzp_dom,nzp_serv,nzp_counter,nzp_type,nzp_cnttype,num_cnt,gil1,val1) " +
                    " Select a.nzp_kvar,a.nzp_dom,a.nzp_serv,a.nzp_counter," +
                    "        c1.nzp_type,c1.nzp_cnttype,c1.num_cnt,  a.gil1, a.val1 " +
                    " From " + rashod.counters_xx + " a, " +
                               rashod.paramcalc.data_alias + "counters_spis c1 " +
                    " Where a.nzp_type = 3 and a.stek = 1 and a." + rashod.where_dom + //rashod.where_kvarA +
                    "   and a.val1  > 0  " +
                    "   and exists ( Select 1 From cnt_d d Where a.nzp_kvar = d.nzp_kvar and a.nzp_serv = d.nzp_serv ) " +
                    "   and a.nzp_counter = c1.nzp_counter "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                ExecSQL(conn_db, " Create index ix_ttt_aid_a on ttt_aid_a (nzp_dom,nzp_type,nzp_cnttype,nzp_serv,num_cnt,nzp_kvar) ", true);
                ExecSQL(conn_db, sUpdStat + " ttt_aid_a ", true);

                // только для рассчитываемых ЛС!
                //еще раз перевыберем, поскольку я предположил, что показание достаточно ввести только на одном приборе! ???
                ExecSQL(conn_db, " Drop table ttt_aid_b ", false);
                ret = ExecSQL(conn_db,
                    " Create temp table ttt_aid_b " +
                    " ( nzp_kvar integer, " +
                    "   nzp_dom  integer, " +
                    "   nzp_serv integer, " +
                    "   nzp_counter integer, " +
                    "   nzp_type    integer, " +
                    "   nzp_cnttype integer, " +
                    "   num_cnt character(20), " +
                    "   gil1 " + sDecimalType + "(15,7) default 0.00 " +
                    " )  " + sUnlogTempTable
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                ret = ExecSQL(conn_db,
                    " Insert Into ttt_aid_b (nzp_kvar,nzp_dom,nzp_serv,nzp_counter,nzp_type,nzp_cnttype,num_cnt,gil1) " +
                    " Select a.nzp_kvar,a.nzp_dom,a.nzp_serv,a.nzp_counter," +
                    "        c1.nzp_type,c1.nzp_cnttype,c1.num_cnt,a.gil1 " +
                    " From ttt_counters_ipu a, " +
                               rashod.paramcalc.data_alias + "counters_spis c1 " +
                    " Where c1.nzp_type = 3 and a.stek = 1 " +
                    "   and exists ( Select 1 From cnt_d d Where a.nzp_kvar = d.nzp_kvar and a.nzp_serv = d.nzp_serv ) " +
                    "   and a.nzp_counter = c1.nzp_counter "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                ExecSQL(conn_db, " Create index ix_ttt_aid_b on ttt_aid_b (nzp_dom,nzp_type,nzp_cnttype,nzp_serv,num_cnt,nzp_kvar) ", true);
                ExecSQL(conn_db, sUpdStat + " ttt_aid_b ", true);

                ret = ExecSQL(conn_db,
                    " Create temp table ttt_aid_stat " +
                    " ( nzp_dom  integer, " +
                    "   nzp_kvar integer, " +
                    "   nzp_serv integer, " +
                    "   nzp_type    integer, " +
                    "   nzp_cnttype integer, " +
                    "   num_cnt character(20), " +
                    "   cnt_ls integer, " +
                    "   cnt_agil " + sDecimalType + "(15,7) default 0.00, " +
                    "   cnt_bgil " + sDecimalType + "(15,7) default 0.00, " +
                    "   common_val_kpu " + sDecimalType + "(15,7) default 0.00 " +
                    " )  " + sUnlogTempTable
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                ret = ExecSQL(conn_db,
                    " Insert Into ttt_aid_stat (nzp_dom,nzp_kvar,nzp_serv,nzp_type,nzp_cnttype,num_cnt,cnt_ls,cnt_agil,cnt_bgil,common_val_kpu) " +
                    " Select a.nzp_dom, a.nzp_kvar, a.nzp_serv, a.nzp_type, a.nzp_cnttype, a.num_cnt, 0 as cnt_ls, " +
                           " max(a.gil1) as cnt_agil, sum(0) as cnt_bgil, max(a.val1) as common_val_kpu " +
                    " From ttt_aid_a a, ttt_aid_b b  " +
                    " Where a.nzp_dom     = b.nzp_dom " +
                    //"   and a.nzp_counter = b.nzp_counter " +
                    "   and a.nzp_type    = b.nzp_type " +
                    "   and a.nzp_cnttype = b.nzp_cnttype " +
                    "   and a.nzp_serv    = b.nzp_serv " +
                    "   and a.num_cnt     = b.num_cnt  " +
                    "   and a.nzp_kvar   <> b.nzp_kvar " +
                    " Group by 1,2,3, 4,5,6 "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                //gil1 - кол-во жильцов

                ExecSQL(conn_db, " Create index ix1_aid_stat on ttt_aid_stat (nzp_kvar, nzp_serv) ", true);
                ExecSQL(conn_db, " Create index ix2_aid_stat on ttt_aid_stat (nzp_dom, nzp_type, nzp_cnttype, nzp_serv, num_cnt) ", true);
                ExecSQL(conn_db, sUpdStat + " ttt_aid_stat ", true);

                //вычислить общее кол-во жителей и кол-во лс в коммуналках в таблице ttt_aid_stat
                ExecSQL(conn_db, " Drop table cnt_d ", false);

                ret = ExecSQL(conn_db,
                    " Create temp table cnt_d " +
                    " ( nzp_dom  integer, " +
                    "   nzp_type    integer, " +
                    "   nzp_cnttype integer, " +
                    "   nzp_serv integer, " +
                    "   num_cnt character(20), " +
                    "   cnt_bgil " + sDecimalType + "(15,7) default 0.00, " +
                    "   cnt_ls integer " +
                    " )  " + sUnlogTempTable
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                ret = ExecSQL(conn_db,
                    " Insert Into cnt_d (nzp_dom,nzp_type,nzp_cnttype,nzp_serv,num_cnt,cnt_bgil,cnt_ls) " +
                    " Select nzp_dom, nzp_type, nzp_cnttype, nzp_serv, num_cnt,  sum(cnt_agil) as cnt_bgil, count(*) as  cnt_ls " +
                    " From ttt_aid_stat " +
                    " Group by 1,2,3,4,5 "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                ExecSQL(conn_db, " Create unique index ix2_cnt_d on cnt_d (nzp_dom, nzp_type, nzp_cnttype, nzp_serv, num_cnt) ", true);
                ExecSQL(conn_db, sUpdStat + " cnt_d ", true);

                ret = ExecSQL(conn_db,
                    " Update ttt_aid_stat " +
                    " Set " +
#if PG
 " cnt_bgil = b.cnt_bgil," +
                    " cnt_ls   = b.cnt_ls " +
                    " From cnt_d b " +
                    " Where ttt_aid_stat.nzp_dom     = b.nzp_dom " +
                    "   and ttt_aid_stat.nzp_type    = b.nzp_type " +
                    "   and ttt_aid_stat.nzp_cnttype = b.nzp_cnttype " +
                    "   and ttt_aid_stat.nzp_serv    = b.nzp_serv " +
                    "   and ttt_aid_stat.num_cnt     = b.num_cnt  "
#else
                    " (cnt_bgil, cnt_ls) = (( " +
                                " Select  b.cnt_bgil, b.cnt_ls From cnt_d b " +
                                " Where ttt_aid_stat.nzp_dom     = b.nzp_dom " +
                                "   and ttt_aid_stat.nzp_type    = b.nzp_type " +
                                "   and ttt_aid_stat.nzp_cnttype = b.nzp_cnttype " +
                                "   and ttt_aid_stat.nzp_serv    = b.nzp_serv " +
                                "   and ttt_aid_stat.num_cnt     = b.num_cnt  " +
                                                " )) " +
                    " Where exists ( " +
                                " Select 1 From cnt_d b " +
                                " Where ttt_aid_stat.nzp_dom     = b.nzp_dom " +
                                "   and ttt_aid_stat.nzp_type    = b.nzp_type " +
                                "   and ttt_aid_stat.nzp_cnttype = b.nzp_cnttype " +
                                "   and ttt_aid_stat.nzp_serv    = b.nzp_serv " +
                                "   and ttt_aid_stat.num_cnt     = b.num_cnt  " +
                                " ) "
#endif
, true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                //с долями морока!!
                //У коммуналок может быть как общие ПУ, так и индивидуальные 
                //Общие надо делить в пропорции
                //А ИПУ надо учесть безусловно 
                //Такая вот вафля!!
                //в val2 в 3-ем стеке лежим просуммированные показания (как обещго, так и ИПУ)
                //а пропорцию надо применить только на показание общего ПУ
                //пипец!

                string sql =
                        " Update ttt_counters_xx " +
                        " Set val_s = val2, " + //сохраним val2 расход по КПУ в коммуналках (что было)
                            " val_po = ( " +    //выделим в val_po расходы общих ПУ, которое и будем уменьшать
                                    " Select sum(a.common_val_kpu) From ttt_aid_stat a " +
                                    " Where ttt_counters_xx.nzp_kvar = a.nzp_kvar " +
                                    "   and ttt_counters_xx.nzp_serv = a.nzp_serv " +
                                    " ) " +
                        " Where exists ( Select 1 From ttt_aid_stat a " +
                                    " Where ttt_counters_xx.nzp_kvar = a.nzp_kvar " +
                                    "   and ttt_counters_xx.nzp_serv = a.nzp_serv " +
                                    " ) " +
                        " and nzp_type = 3 and stek = 3 " +
                        " and cnt_stage in (1,9) ";

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                sql =
                        " Update ttt_counters_xx " +
                        " Set val_s = val2 - val_po " + //val_s - это теперь расходы ИПУ (не общие ПУ!)
                        " Where nzp_type = 3 and stek = 3 " +
                          " and cnt_stage in (1,9) " +
                          " and exists ( Select 1 From ttt_aid_stat a " +
                                    " Where ttt_counters_xx.nzp_kvar = a.nzp_kvar " +
                                    "   and ttt_counters_xx.nzp_serv = a.nzp_serv " +
                                    " ) ";

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                //и уменьшим расход в пропорции общего кол-ва жильцов
                sql =
                        " Update ttt_counters_xx " + //cnt5 - общее кол-во жильцов, val2 = val_po*k (напомню, val_po - это расходы общих ПУ)
                        " Set " +
                        " (cnt5,val2) =" +
                        " (( Select max(a.cnt_bgil) From ttt_aid_stat a " +
                           " Where ttt_counters_xx.nzp_kvar = a.nzp_kvar " +
                           "   and ttt_counters_xx.nzp_serv = a.nzp_serv " +
                           "   and a.cnt_bgil > 0 " +
                         ")," +
                         "( Select max(ttt_counters_xx.val_po * ttt_counters_xx.gil1 / a.cnt_bgil) " +
                           " From ttt_aid_stat a " +
                           " Where ttt_counters_xx.nzp_kvar = a.nzp_kvar " +
                           "   and ttt_counters_xx.nzp_serv = a.nzp_serv " +
                           "   and a.cnt_bgil > 0 " +
                         ")) " +
                        " Where exists ( Select 1 From ttt_aid_stat a " +
                                    " Where ttt_counters_xx.nzp_kvar = a.nzp_kvar " +
                                    "   and ttt_counters_xx.nzp_serv = a.nzp_serv " +
                                    "   and a.cnt_bgil > 0 " +
                                    " ) " +
                        " and nzp_type = 3 and stek = 3 " +
                        " and cnt_stage in (1,9) ";

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                //и уменьшим расход в пропорции лицевых счетов, если кол-во жильцов = 0
                sql =
                        " Update ttt_counters_xx " +
                        " Set " +
                        " (cnt4,cnt5,val2) = " +
                        "(( Select max(a.cnt_ls) From ttt_aid_stat a " +
                              " Where ttt_counters_xx.nzp_kvar = a.nzp_kvar " +
                              "   and ttt_counters_xx.nzp_serv = a.nzp_serv " +
                              "   and a.cnt_bgil = 0 " +
                            ")," +
                            "( Select max(a.cnt_bgil) From ttt_aid_stat a " +
                              " Where ttt_counters_xx.nzp_kvar = a.nzp_kvar " +
                              "   and ttt_counters_xx.nzp_serv = a.nzp_serv " +
                              "   and a.cnt_bgil = 0 " +
                            ")," +
                            "( Select max(ttt_counters_xx.val_po / a.cnt_ls) " +
                              " From ttt_aid_stat a " +
                              " Where ttt_counters_xx.nzp_kvar = a.nzp_kvar " +
                              "   and ttt_counters_xx.nzp_serv = a.nzp_serv " +
                              "   and a.cnt_bgil = 0 " +
                            ")) " +
                        " Where exists ( Select 1 From ttt_aid_stat a " +
                                    " Where ttt_counters_xx.nzp_kvar = a.nzp_kvar " +
                                    "   and ttt_counters_xx.nzp_serv = a.nzp_serv " +
                                    "   and a.cnt_bgil = 0 " +
                                    " ) " +
                        " and nzp_type = 3 and stek = 3 " +
                        " and cnt_stage in (1,9) ";

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                //добавим val_s - расход по ИПУ
                sql =
                        " Update ttt_counters_xx " +
                        " Set val2 = val2 + val_s " +
                        " Where nzp_type = 3 and stek = 3 " +
                          " and cnt_stage  in (1,9) " +
                          " and exists ( Select 1 From ttt_aid_stat a " +
                                    " Where ttt_counters_xx.nzp_kvar = a.nzp_kvar " +
                                    "   and ttt_counters_xx.nzp_serv = a.nzp_serv " +
                                    " ) ";

                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                ExecSQL(conn_db, " Drop table ttt_aid_stat ", false);
            }

            return true;
        }
        #endregion коммуналки - уменьшим расход в пропорции общего кол-ва жильцов

        #region сформировать стек 39 - итоговый стек расходов от ГКал для ОДПУ и ГрПУ (аналог стека 3 для "канонических" ед.изм. из s_counts)
        public bool CalcStek39gkal(IDbConnection conn_db, Rashod rashod, string p_dat_charge, out Returns ret)
        //--------------------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            ExecSQL(conn_db, " Drop table ttt_aid_virt0 ", false);
            ret = ExecSQL(conn_db,
             " Create temp table ttt_aid_virt0 " +
             " ( nzp_cntx integer, " +
             "   nzp_kvar integer, " +
             "   nzp_serv integer, " +
             "   stek     integer, " +
             "   nzp_type integer, " +
             "   nzp_counter integer, " +
             "   dat_s  date not null," +
             "   dat_po date not null," +
             "   val1   " + sDecimalType + "(15,7) default 0.00," +
             "   val3   " + sDecimalType + "(15,7) default 0.00," +
             "   val4   " + sDecimalType + "(15,7) default 0.00," +
             "   rvirt  " + sDecimalType + "(15,7) default 0.00," +
             "   nzp_dom  integer, " +
             "   val_s   " + sDecimalType + "(15,7) default 0.00," +
             "   val_po  " + sDecimalType + "(15,7) default 0.00," +
             "   ngp_cnt " + sDecimalType + "(15,7) default 0.00," +
             "   cnt_stage integer, " +
             "   mmnog     integer, " +
             "   kod_info  integer, " +
             "   nzp_period integer, " +
             "   dp       " + sDateTimeType + ", " +
             "   dp_end   " + sDateTimeType + ", " +
             "   cntd integer," +
             "   cntd_mn integer " +
             " )  " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " Insert into ttt_aid_virt0 " +
                 "(nzp_cntx,nzp_kvar,nzp_serv,stek,nzp_type,nzp_counter,dat_s,dat_po,val1,val3,val4,rvirt," +
                  "nzp_dom,val_s,val_po,ngp_cnt,cnt_stage,mmnog,kod_info," +
                  "nzp_period,dp,dp_end,cntd,cntd_mn) " +
                " Select " +
                 " nzp_cntx,nzp_kvar,nzp_serv,stek,nzp_type,nzp_counter,dat_s,dat_po,val1,val3,val4,rvirt," +
                  "nzp_dom,val_s,val_po,ngp_cnt,cnt_stage,mmnog,kod_info," +
                  " 0," + rashod.paramcalc.dat_s + "," + rashod.paramcalc.dat_po + ", " +
                  DateTime.DaysInMonth(rashod.paramcalc.calc_yy, rashod.paramcalc.calc_mm) + ", " +
                  DateTime.DaysInMonth(rashod.paramcalc.calc_yy, rashod.paramcalc.calc_mm) +
                " From " + rashod.counters_xx +
                " Where nzp_type in (1,2) and stek = 9 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " Create index ix1_ttt_aid_virt0 on ttt_aid_virt0 (nzp_counter) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_aid_virt0 ", true);

            ExecSQL(conn_db, " Drop table ttt_ans_kpu ", false);
            ret = ExecSQL(conn_db,
                " Create temp table ttt_ans_kpu AS " +
                " Select nzp_counter" +
                " From ttt_aid_virt0 group by 1 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            //дописываем связанные ПУ, по заменам для которых есть показания
            //считаем так: если есть показание по одному ПУ, то есть показания по всем связанным ПУ
            var sql = " INSERT INTO ttt_ans_kpu (nzp_counter)" +
                      " SELECT DISTINCT r.nzp_counter FROM t_old_new_counters r" +
                      " WHERE EXISTS (SELECT 1 FROM t_old_new_counters rr, ttt_aid_virt0 ii" +
                      "              WHERE rr.nzp_counter=ii.nzp_counter " +
                      "              AND rr.nzp_counter_old=r.nzp_counter_old)";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }


            ExecSQL(conn_db, " Create index ix1_ttt_ans_kpu on ttt_ans_kpu (nzp_counter) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_ans_kpu ", true);

            ret = ExecSQL(conn_db,
                " Insert into ttt_aid_virt0 " +
                 "(nzp_cntx,nzp_kvar,nzp_serv,stek,nzp_type,nzp_counter,dat_s,dat_po,val1,val3,val4,rvirt," +
                  "nzp_dom,val_s,val_po,ngp_cnt,cnt_stage,mmnog,kod_info," +
                  "nzp_period,dp,dp_end,cntd,cntd_mn) " +
                " Select " +
                 " nzp_cntx,nzp_kvar,nzp_serv,stek,nzp_type,nzp_counter,dat_s,dat_po,val1,val3,val4,rvirt," +
                  "nzp_dom,val_s,val_po,ngp_cnt,cnt_stage,mmnog,0 kod_info," +
                  " 0," + rashod.paramcalc.dat_s + "," + rashod.paramcalc.dat_po + ", " +
                  DateTime.DaysInMonth(rashod.paramcalc.calc_yy, rashod.paramcalc.calc_mm) + ", " +
                  DateTime.DaysInMonth(rashod.paramcalc.calc_yy, rashod.paramcalc.calc_mm) +
                " From " + rashod.counters_xx +
                " Where nzp_type in (1,2) and stek = 2 and kod_info=1 and " + rashod.where_dom + rashod.where_kvar + rashod.paramcalc.per_dat_charge +
                " and not exists(select 1 from ttt_ans_kpu b where " + rashod.counters_xx + ".nzp_counter=b.nzp_counter) "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, sUpdStat + " ttt_aid_virt0 ", true);

            // stek=39 в БД
            ret = ExecSQL(conn_db,
                " Insert into " + rashod.counters_xx +
                " (nzp_kvar,nzp_serv,nzp_counter,nzp_type,nzp_dom,stek,dat_charge,dat_s,dat_po,mmnog,cnt5,val_po,val4,rvirt," +
                  "val_s,ngp_cnt,kod_info,cls1,cls2)" +
                " Select " +
                  "nzp_kvar,nzp_serv,0 nzp_counter,nzp_type,nzp_dom,39 stek," + p_dat_charge + ",min(dp),max(dp_end),0 mmnog," +
                  "max(case when stek=9 then 1 else 9 end) as cnt_stage,sum(val1),sum(val4),sum(rvirt)," +
                  "0 as val_s,sum(ngp_cnt),max(kod_info),max(cntd),max(cntd_mn) " +
                " From ttt_aid_virt0" +
                " Where nzp_type = 1" +
                " Group by 1,2,3,4,5 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ret = ExecSQL(conn_db,
                " Insert into " + rashod.counters_xx +
                " (nzp_kvar,nzp_serv,nzp_counter,nzp_type,nzp_dom,stek,dat_charge,dat_s,dat_po,mmnog,cnt5,val_po,val4,rvirt," +
                  "val_s,ngp_cnt,kod_info,cls1,cls2)" +
                " Select " +
                  "nzp_kvar,nzp_serv,nzp_counter,nzp_type,nzp_dom,39 stek," + p_dat_charge + ",dp,dp_end,mmnog," +
                  "(case when stek=9 then 1 else 9 end) as cnt_stage,val1,val4,rvirt," +
                  "0 as val_s,ngp_cnt,kod_info,cntd,cntd_mn " +
                " From ttt_aid_virt0" +
                " Where nzp_type = 2 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " Drop table ttt_aid_virt0 ", false);
            ExecSQL(conn_db, " Drop table ttt_ans_kpu ", false);

            return true;
        }
        #endregion сформировать стек 39 - итоговый стек расходов от ГКал для ОДПУ и ГрПУ (аналог стека 3 для "канонических" ед.изм. из s_counts)

        #region разбить счетчик на производные счетчики при наличии разрывов в показаниях (поломка, поверка)

        public class TableForCountersBounds
        {
            //физическая таблица с периодами поломок, поверок для ПУ
            public string counters_bounds;
            //темповая таблица с периодами поломок, поверок для ПУ 
            public string temp_counters_bounds;
            //темповая таблица с периодами поломок, поверок для ПУ которое есть в списке рассматриваемых показаний
            public string temp_counters_bounds_part;
            //список периодов для ПУ в которые он валиден или нет
            public string temp_counters_periods;
            //список дат- "вешек"  для ПУ на основе которых формируются периоды
            public string temp_counters_dates;
            //справочник типов периодов для счетчиков
            public string s_counters_bounds_types;
            //объединеннные валидные периоды
            public string temp_counters_union_periods;
            //список дат закрытия полученных из структуры показаний 
            public string temp_list_dat_closes;
            //список nzp_counter у которых есть даты поломок, поверок или даты закрытия
            public string temp_unique_counters;
            public string where_work;
            public string where_notwork;
            //таблица невалидных периодов используемая в расчете
            public string aid_i_pref;
            //заменененные ПУ
            public string temp_replaced_counters;
            //пересекающиеся периоды связанных ПУ
            public string temp_overlaps_periods;
            public TableForCountersBounds(CalcTypes.ParamCalc paramcalc)
            {
                temp_list_dat_closes = "temp_list_dat_closes_from_counters_" + DateTime.Now.Ticks;
                temp_unique_counters = "temp_unique_counters_" + DateTime.Now.Ticks;
                temp_counters_bounds_part = "temp_counters_bounds_part_" + DateTime.Now.Ticks;
                temp_counters_bounds = "temp_counters_bounds_" + DateTime.Now.Ticks;
                counters_bounds = paramcalc.pref + sDataAliasRest + "counters_bounds";
                temp_counters_periods = "counters_periods_" + DateTime.Now.Ticks;
                temp_counters_dates = "counters_dates_" + DateTime.Now.Ticks;
                s_counters_bounds_types = Points.Pref + sKernelAliasRest + "s_counters_bounds_types";
                temp_counters_union_periods = "counters_union_periods_" + DateTime.Now.Ticks;

                where_work = " type_id IN " +
                            " (SELECT id FROM " + s_counters_bounds_types + " WHERE " +
                            " alg_id=" + (int)TypeAlgoritmsBounds.Work + ")";
                where_notwork = " type_id IN " +
                               " (SELECT id FROM " + s_counters_bounds_types + " WHERE " +
                               " alg_id=" + (int)TypeAlgoritmsBounds.NotWork + ")";
                aid_i_pref = "aid_i" + paramcalc.pref;

                temp_replaced_counters = "t_replaced_cnts_" + DateTime.Now.Ticks;
                temp_overlaps_periods = "t_overlap_cnts_" + DateTime.Now.Ticks;
            }
        }

        /// <summary>
        /// Получаем валидные периоды для счетчиков
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="paramcalc"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        protected bool GetValidPeriodsForCounters(IDbConnection conn_db, CalcTypes.ParamCalc paramcalc, out Returns ret)
        {
            ret = Utils.InitReturns();
            if (!paramcalc.ExistsCounters) return true; //счетчиков нет - считать нечего

            var t = new TableForCountersBounds(paramcalc);

            ExecSQL(conn_db, "DROP TABLE " + t.temp_counters_bounds, false);
            //таблица с невалидными периодами ПУ
            var sql = " CREATE TEMP TABLE " + t.aid_i_pref +
                      " ( nzp_key SERIAL NOT NULL, " +
                      "   nzp_counter INTEGER, " +
                      "   dat_s  DATE, " +
                      "   dat_po DATE  " +
                      " ) ";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                return false;
            }

            ExecSQL(conn_db, " Create unique index ix_aid33_" + paramcalc.pref + " on " + t.aid_i_pref + " (nzp_key) ", true);
            ExecSQL(conn_db, " Create        index ix_aid44_" + paramcalc.pref + " on " + t.aid_i_pref + " (nzp_counter, dat_s, dat_po) ", true);

            try
            {

            
            ExecSQL(conn_db, "DROP TABLE " + t.temp_counters_bounds, false);

            //список всех периодов для приборов учета (поломки, поверки)
            sql = " SELECT cb.nzp_counter,cb.date_from,cb.date_to,cb.type_id,cs.dat_close" +
                      " INTO TEMP " + t.temp_counters_bounds + " " +
                      " FROM " + t.counters_bounds + " cb, temp_cnt_spis_f cs" + //temp_cnt_spis_f - полный перечень ПУ задействованных в текущем расчете
                      " WHERE cb.nzp_counter=cs.nzp_counter " +
                      " AND cb.is_actual=true " +
                      " GROUP BY cb.nzp_counter,cb.date_from,cb.date_to,cb.type_id,cs.dat_close";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                return false;
            }

            ExecSQL(conn_db, "CREATE INDEX ix1_" + t.temp_counters_bounds + " ON " + t.temp_counters_bounds + " (nzp_counter)", true);

            //параметр 799 - Дата начала учета даты закрытия ПУ при расчете
            var dateStartAllowDateClose = GetParamValue<DateTime>(conn_db, paramcalc.pref, 799, 10, out ret);
            if (dateStartAllowDateClose == DateTime.MinValue)
            {
                //при отстутствии параметра в системе все даты закрытия считаем не валидными!!
                //наследие РТ...
                dateStartAllowDateClose = new DateTime(3000, 1, 1);
            }

            //таблицы показаний
            var arr = new[] 
            { 
                "temp_counters", //не гкал. показания по ИПУ
                "temp_counters_dom",  //не гкал. показания по ДПУ
                "temp_counters_group", //не гкал. показания по ГрПу, общ.кв.ПУ
                "temp_counters_gkal",  // гкал. показания по ИПУ
                "temp_counters_dom_gkal", // гкал. показания по ДПУ
                "temp_counters_group_gkal", // гкал. показания по ГрПу, общ.кв.ПУ
                "temp_counters_common_kvar_ttt", //не гкал. коммун. пу
                "temp_counters_common_kvar_gkal" //гкал.коммун. пу
            };


            //получаем связанные ПУ
            //temp_counters_dates_start - период действия ПУ (min(dat_uchet) - max(dat_uchet))
            sql = " CREATE TEMP TABLE " + t.temp_replaced_counters + " AS  " +
                  " SELECT r.nzp_counter_old, p.nzp_counter, p.date_start as date_from, p.date_last as date_to " +
                  " FROM t_old_new_counters  r, temp_counters_dates_start p " +
                  " WHERE p.nzp_counter=r.nzp_counter";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
                return false;

            ret = ExecSQL(conn_db,
                "CREATE INDEX ix1_" + t.temp_replaced_counters + " ON " + t.temp_replaced_counters + " (nzp_counter_old,nzp_counter,date_from, date_to)",
                true);
            if (!ret.result)
                return false;

            //ограничиваем период активности ПУ датой закрытия только в случае,
            //если дата закрытия меньше, даты окончания действия ПУ (дата учета последнего показания ПУ)
            sql = " UPDATE " + t.temp_replaced_counters + " r" +
                  " SET date_to=dat_close - interval '1 day' " +
                  " FROM temp_cnt_spis_f f" +
                  " WHERE r.nzp_counter=f.nzp_counter" +
                  " AND f.dat_close IS NOT NULL AND f.dat_close<=date_to";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
                return false;
            
            //удаляем кривые периоды
            sql = "DELETE FROM  " + t.temp_replaced_counters + " WHERE date_to<date_from";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
                return false;

            //находим пересекающиеся периоды для связанных ПУ
            sql = " CREATE TEMP TABLE " + t.temp_overlaps_periods + " AS " +
                  " SELECT DISTINCT t1.nzp_counter_old, GREATEST(t1.date_from, t2.date_from) date_from, " +
                  " LEAST(t1.date_to, t2.date_to) date_to " +
                  " FROM " + t.temp_replaced_counters + " t1, " + t.temp_replaced_counters + " t2 " +
                  " WHERE t1.nzp_counter_old=t2.nzp_counter_old" +
                  " AND t1.nzp_counter<>t2.nzp_counter" +
                  //если у ПУ одно показание, то он имеет период действия (01.MM.YY-01.MM.YY),
                  //вызывает пересечение с заменяющим ПУ, но сам показание не предоставляет, так как нет второго показания
                  " AND t1.date_from<>t1.date_to AND t2.date_from<>t2.date_to " + 
                  " AND (t1.date_from, t1.date_to) OVERLAPS (t2.date_from, t2.date_to)";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
                return false;

           
            ret = ExecSQL(conn_db,
              "CREATE INDEX ix1_" + t.temp_overlaps_periods + " ON " + t.temp_overlaps_periods + " (nzp_counter_old, date_from, date_to)",
              true);
            if (!ret.result)
                return false;


            foreach (var table in arr)
            {
                if (!SplitCountersOntoValues(conn_db, ref ret, t, table, dateStartAllowDateClose))
                    return false;
            }
            }
            finally
            {
                ExecSQL(conn_db, "DROP TABLE " + t.temp_list_dat_closes);
                ExecSQL(conn_db, "DROP TABLE " + t.temp_unique_counters);
                ExecSQL(conn_db, "DROP TABLE " + t.temp_counters_bounds_part);
                ExecSQL(conn_db, "DROP TABLE " + t.temp_counters_bounds);

                ExecSQL(conn_db, "DROP TABLE " + t.temp_counters_periods);
                ExecSQL(conn_db, "DROP TABLE " + t.temp_counters_dates);

                ExecSQL(conn_db, "DROP TABLE " + t.temp_counters_union_periods);
                ExecSQL(conn_db, "DROP TABLE " + t.temp_replaced_counters);
                ExecSQL(conn_db, "DROP TABLE " + t.temp_overlaps_periods);
            }

            return true;
        }
        /// <summary>
        /// Определяем периоды действия ПУ и разбиваем их в таблицах показаний
        /// </summary>
        /// <param name="conn_db"></param>
        /// <param name="paramcalc"></param>
        /// <param name="ret"></param>
        /// <param name="t">используемые таблицы</param>
        /// <param name="table">таблица показаний</param>
        /// <param name="dateStartAllowDateClose">дата начала учета даты закрытия ПУ</param>
        /// <returns></returns>
        private bool SplitCountersOntoValues(IDbConnection conn_db, ref Returns ret,
            TableForCountersBounds t, string table, DateTime dateStartAllowDateClose)
        {

            if (!DBManager.ExecScalar<bool>(conn_db, "SELECT count(1)>0 FROM " + table, out ret, true))
            {
                return true;
            }

            //получаем периоды ПУ, которые есть в текущей таблице показаний
            ExecSQL(conn_db, "DROP TABLE " + t.temp_counters_bounds_part, false);
            var sql = " SELECT * INTO TEMP " + t.temp_counters_bounds_part + " FROM " + t.temp_counters_bounds + " b " +
                         " WHERE EXISTS (SELECT 1 FROM " + table + " t WHERE t.nzp_counter=b.nzp_counter)";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                return false;
            }

            #region indexes
            ExecSQL(conn_db, "CREATE INDEX ix1_" + t.temp_counters_bounds_part + " ON " + t.temp_counters_bounds_part + " (nzp_counter)", true);
            ExecSQL(conn_db, "CREATE INDEX ix2_" + t.temp_counters_bounds_part + " ON " + t.temp_counters_bounds_part + " (nzp_counter,date_from)", true);
            ExecSQL(conn_db, "CREATE INDEX ix3_" + t.temp_counters_bounds_part + " ON " + t.temp_counters_bounds_part + " (nzp_counter,date_to)", true);
            ExecSQL(conn_db, "CREATE INDEX ix4_" + t.temp_counters_bounds_part + " ON " + t.temp_counters_bounds_part + " (nzp_counter,type_id)", true);
            #endregion indexes

            #region Даты закрытия и ограничение по ним

            ExecSQL(conn_db, "DROP TABLE " + t.temp_list_dat_closes, false);
            //даты закрытия меньше даты начала учета дат закрытия  - удаляем
            var where_date_close = " AND t.dat_close>=" + Utils.EStrNull(dateStartAllowDateClose.ToShortDateString());

           
            //даты закрытия из списка счетчиков 
            sql = " CREATE TEMP TABLE " + t.temp_list_dat_closes + " AS " +
                  " SELECT t.nzp_counter,t.dat_close FROM temp_cnt_spis_f t, " + table + " tt" +
                  " WHERE tt.nzp_counter=t.nzp_counter AND " +
                  " t.dat_close IS NOT NULL " + where_date_close +
                  " GROUP BY 1,2";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                return false;
            }

            #region indexes
            ret = ExecSQL(conn_db, "CREATE INDEX ix1_" + t.temp_list_dat_closes + " ON " + t.temp_list_dat_closes + " (dat_close)", true);
            if (!ret.result)
            {
                return false;
            }
            ret = ExecSQL(conn_db, "CREATE INDEX ix2_" + t.temp_list_dat_closes + " ON " + t.temp_list_dat_closes + " (nzp_counter)", true);
            if (!ret.result)
            {
                return false;
            }
            #endregion

            #endregion Даты закрытия и ограничение по ним


            #region Получаем счетчики с периодами поломок, поверок, с датами закрытиями

            ExecSQL(conn_db, "DROP TABLE " + t.temp_unique_counters, false);

            //берем счетчики с датами поломок +
            //с датами поверок и поломок
            sql = " CREATE TEMP TABLE " + t.temp_unique_counters + " AS " +
                  " SELECT nzp_counter FROM " + t.temp_list_dat_closes +
                  " UNION" +
                  " SELECT nzp_counter FROM " + t.temp_counters_bounds_part;
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                {
                    return false;
                }

            #region indexes
            ret = ExecSQL(conn_db,
              "CREATE UNIQUE INDEX ix_" + t.temp_unique_counters + "_1 ON " + t.temp_unique_counters + "(nzp_counter)", true);
            if (!ret.result) return false;
            #endregion

            #endregion Получаем счетчики с периодами поломок, поверок, с датами закрытиями

            #region Создаем список дат для ПУ

            ExecSQL(conn_db, "DROP TABLE " + t.temp_counters_dates, false);
            //записать все в один столбец
            sql = "CREATE TEMP TABLE " + t.temp_counters_dates +
                  "(nzp_counter integer" +
                  ",date_val date" +
                  ",type integer);";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                return false;
            }

            //записываем показание с min(dat_uchet) с типом =1
            sql = " INSERT INTO " + t.temp_counters_dates + "(nzp_counter,date_val,type) " +
                  " SELECT nzp_counter,min(dat_uchet),1 " +
                  " FROM " + table + " t " +
                  " WHERE EXISTS (SELECT 1 FROM " + t.temp_unique_counters + " u " +
                  " WHERE u.nzp_counter=t.nzp_counter)" +
                  " GROUP BY 1;";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
                return false;

            //записываем показание с max(dat_uchet) с типом =2
            sql = " INSERT INTO " + t.temp_counters_dates + "(nzp_counter,date_val,type) " +
                  " SELECT nzp_counter,max(dat_uchet),2 " +
                  " FROM " + table + " t " +
                  " WHERE EXISTS (SELECT 1 FROM " + t.temp_unique_counters + " u " +
                  " WHERE u.nzp_counter=t.nzp_counter)" +
                  " GROUP BY 1;";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
                return false;

            //записываем начала периодов поломок,поверок с типом =3
            sql = " INSERT INTO " + t.temp_counters_dates + "(nzp_counter,date_val,type) " +
                  " SELECT nzp_counter,date_from,3" +
                  " FROM " + t.temp_counters_bounds_part +
                  " GROUP BY 1,2;";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
                return false;

            //записываем концы периодов поломок,поверок с типом =4
            sql = " INSERT INTO " + t.temp_counters_dates + "(nzp_counter,date_val,type) " +
                  " SELECT nzp_counter,date_to,4" +
                  " FROM " + t.temp_counters_bounds_part +
                  " GROUP BY 1,2;";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
                return false;

            //записываем дату закрытия ПУ с типом =5
            sql = " INSERT INTO " + t.temp_counters_dates + "(nzp_counter,date_val,type) " +
                  " SELECT nzp_counter,max(dat_close),5" + //max потому, что берем из двух источников: counters, counters_spis
                  " FROM " + t.temp_list_dat_closes +
                  " WHERE dat_close IS NOT NULL" +
                  " GROUP BY 1;";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
                return false;

            //пишем даты начала пересекающихся периодов показаний связанных ПУ
            sql = " INSERT INTO " + t.temp_counters_dates + "(nzp_counter,date_val,type) " +
                  " SELECT t.nzp_counter, o.date_from, 6" +
                  " FROM " + t.temp_replaced_counters + " t,  " + t.temp_overlaps_periods + " o" +
                  " WHERE t.nzp_counter_old=o.nzp_counter_old " +
                  " AND EXISTS (SELECT 1 FROM " + table + " ex WHERE ex.nzp_counter=t.nzp_counter)" +
                  " GROUP BY 1,2";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
                return false;

            //пишем даты окончания пересекающихся периодов показаний связанных ПУ
            sql = " INSERT INTO " + t.temp_counters_dates + "(nzp_counter,date_val,type) " +
                  " SELECT t.nzp_counter, o.date_to, 7" +
                  " FROM " + t.temp_replaced_counters + " t,  " + t.temp_overlaps_periods + " o" +
                  " WHERE t.nzp_counter_old=o.nzp_counter_old " +
                  " AND EXISTS (SELECT 1 FROM " + table + " ex WHERE ex.nzp_counter=t.nzp_counter)" +
                  " GROUP BY 1,2";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
                return false;

            #region indexes
            ExecSQL(conn_db, "CREATE INDEX ix1_" + t.temp_counters_dates + " ON " + t.temp_counters_dates + " (nzp_counter,date_val)", true);
            ExecSQL(conn_db, "CREATE INDEX ix2_" + t.temp_counters_dates + " ON " + t.temp_counters_dates + " (type)", true);
            #endregion

            ExecSQL(conn_db, "DROP TABLE " + t.temp_list_dat_closes, false);


            #endregion Создаем список дат для ПУ

            //создаем таблицу с окончательными периодами по ПУ
            ExecSQL(conn_db, "DROP TABLE " + t.temp_counters_periods, false);
            //записываем все в виде периодов
            sql = " CREATE TEMP TABLE " + t.temp_counters_periods + " AS  " +
                  " SELECT a.nzp_counter,a.date_val as date_from," +
                  " MIN(b.date_val) date_to, FALSE as valid" +
                  " FROM " + t.temp_counters_dates + " a, " + t.temp_counters_dates + " b" +
                  " WHERE a.nzp_counter=b.nzp_counter and a.date_val<b.date_val" +
                  " GROUP BY 1,2;";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
                return false;

            #region indexes
            ExecSQL(conn_db, "CREATE INDEX ix1_" + t.temp_counters_periods + " ON " + t.temp_counters_periods + " (nzp_counter)", true);
            ExecSQL(conn_db, "CREATE INDEX ix2_" + t.temp_counters_periods + " ON " + t.temp_counters_periods + " (nzp_counter,date_from,date_to)", true);
            #endregion
            #region Определяем валидные периоды для ПУ

            //дополняем таблицу период поверки на всю временную шкалу ПУ
            //при условии что периодов поверок у ПУ нет вообще
            sql = " INSERT INTO " + t.temp_counters_bounds_part + "(nzp_counter,date_from,date_to,type_id)" +
                  " SELECT nzp_counter,min(date_from),max(date_to)," + (int)TypeAlgoritmsBounds.Work +
                  " FROM " + t.temp_counters_periods + " tp " +
                  " WHERE NOT EXISTS " +
                  " (SELECT 1 FROM " + t.temp_counters_bounds_part + " p " +
                  " WHERE p.nzp_counter=tp.nzp_counter " +
                  " AND type_id=" +
                  (int)TypeBoundsCounters.Verification + ")" +
                  " GROUP BY 1";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
                return false;
            }

            //проставляем валидные периоды по датам поверок 
            sql = " UPDATE " + t.temp_counters_periods + " SET valid = true" +
                  " FROM " + t.temp_counters_bounds_part + " t " +
                  " WHERE " + t.temp_counters_periods + ".date_from>=t.date_from " +
                  " AND " + t.temp_counters_periods + ".date_to<=t.date_to" +
                  " AND  " + t.temp_counters_periods + ".nzp_counter=t.nzp_counter " +
                  " AND " + t.where_work;
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
                return false;

            //периоды которые были до первой даты поверки считаются валидными
            sql = " UPDATE " + t.temp_counters_periods + " SET valid = TRUE" +
                  " WHERE date_from<(SELECT min(date_from) " +
                  " FROM " + t.temp_counters_bounds_part + " t " +
                  " WHERE t.nzp_counter=" + t.temp_counters_periods + ".nzp_counter " +
                  " AND t.type_id=" + (int)TypeBoundsCounters.Verification + ")" +
                  " AND valid=FALSE";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
                return false;

            #endregion Определяем валидные периоды для ПУ

            #region Накладываем периоды поломок,закрытия на периоды, пересечения связанных ПУ

            //учитываем периоды поломок
            sql = " UPDATE " + t.temp_counters_periods + " SET valid = FALSE" +
                  " FROM " + t.temp_counters_bounds_part + " t " +
                  " WHERE " + t.temp_counters_periods + ".date_from>=t.date_from " +
                  " AND " + t.temp_counters_periods + ".date_to<=t.date_to" +
                  " AND  " + t.temp_counters_periods + ".nzp_counter=t.nzp_counter " +
                  " AND " + t.where_notwork;
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
                return false;


            //все периоды после даты закрытия не валидны
            sql = " UPDATE " + t.temp_counters_periods + " SET valid = FALSE" +
                  " FROM " + t.temp_counters_dates + " t" +
                  " WHERE " + t.temp_counters_periods + ".date_from>=t.date_val " +
                  " AND " + t.temp_counters_periods + ".nzp_counter=t.nzp_counter " +
                  " AND t.type=5 ";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
                return false;

            //пересекающиеся периоды показаний для связанных ПУ считаем невалидными
            sql = " UPDATE " + t.temp_counters_periods + " p SET valid = FALSE" +
                  " FROM " + t.temp_overlaps_periods + " o, " + t.temp_replaced_counters + " r " +
                  " WHERE o.nzp_counter_old=r.nzp_counter_old AND r.nzp_counter=p.nzp_counter" +
                  " AND (p.date_from, p.date_to) OVERLAPS (o.date_from, o.date_to)";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
                return false;

            #endregion Накладываем периоды поломок,закрытия на периоды, пересечения связанных ПУ


            sql = " INSERT INTO " + t.aid_i_pref + " (nzp_counter,dat_s,dat_po) " +
                  " SELECT nzp_counter,date_from,date_to FROM " + t.temp_counters_periods +
                  " WHERE valid=FALSE";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
                return false;

            //удаляем не валидные периоды
            sql = " DELETE FROM " + t.temp_counters_periods + " WHERE valid=FALSE";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
                return false;

            #region Объединение пересекающихся валидных периодов

            ExecSQL(conn_db, "DROP TABLE " + t.temp_counters_union_periods, false);

            //объединяем пересекающие периоды для избежания излишнего дробления
            var table_begin = "begin_" + t.temp_counters_union_periods;
            var table_end = "end_" + t.temp_counters_union_periods;

            ExecSQL(conn_db, "DROP TABLE " + table_begin, false);
            //получаем даты начала объединенных периодов
            sql = string.Format(" SELECT nzp_counter,date_from, ROW_NUMBER() OVER(ORDER BY nzp_counter,date_from) as rn " +
                                " INTO TEMP TABLE {0} FROM {1} s1 WHERE NOT EXISTS (SELECT null FROM {1} s2 " +
                                " WHERE s2.date_from < s1.date_from  AND s2.date_to >= s1.date_from	" +
                                " AND s2.nzp_counter = s1.nzp_counter )", table_begin, t.temp_counters_periods);
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
                return false;

            ExecSQL(conn_db, "CREATE INDEX ix1_" + table_begin + " ON " + table_begin + " (rn,nzp_counter)", true);

            ExecSQL(conn_db, "DROP TABLE " + table_end, false);
            //получаем даты окончания объединенных периодов
            sql = string.Format(" SELECT nzp_counter,date_to, ROW_NUMBER() OVER(ORDER BY nzp_counter,date_to) as rn " +
                                " INTO TEMP TABLE {0} FROM {1} s1 WHERE NOT EXISTS (SELECT null FROM {1} s2 " +
                                " WHERE s2.date_to > s1.date_to AND s2.date_from <= s1.date_to " +
                                " AND s2.nzp_counter = s1.nzp_counter )", table_end, t.temp_counters_periods);
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)

                return false;
            ExecSQL(conn_db, "CREATE INDEX ix1_" + table_end + " ON " + table_end + " (rn,nzp_counter)", true);

            ExecSQL(conn_db, "DROP TABLE " + t.temp_counters_union_periods, false);
            //склеиваем пересекающиеся валидные периоды для счетчиков
            sql = string.Format(" CREATE TEMP TABLE {0} AS  " +
                                " SELECT  t1.nzp_counter, date_from, date_to " +
                                " FROM {1} t1, {2} t2 " + " WHERE t1.rn=t2.rn " +
                                " AND t1.nzp_counter=t2.nzp_counter", t.temp_counters_union_periods, table_begin, table_end);
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
                return false;

            ExecSQL(conn_db, "CREATE INDEX ix1_" + t.temp_counters_union_periods + " ON " + t.temp_counters_union_periods + " (nzp_counter)", true);

            ExecSQL(conn_db, "DROP TABLE " + table_begin, false);
            ExecSQL(conn_db, "DROP TABLE " + table_end, false);


            #endregion Объединение пересекающихся валидных периодов

            #region Проводим разделение счетчиков по периодам валидности


            //генерации серийников для разбитых периодов
            sql = " CREATE TEMP TABLE t_other_periods AS " +
                  " SELECT ROW_NUMBER() OVER() nzp_counter_child," +
                  " ROW_NUMBER() OVER(PARTITION BY nzp_counter ORDER BY date_from,date_to) num_period," +
                  " nzp_counter,date_from,date_to FROM " + t.temp_counters_union_periods + " t_union ";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
                return false;

            ret = ExecSQL(conn_db, "CREATE INDEX ix1_t_other_periods ON t_other_periods (nzp_counter,date_from,date_to) WHERE num_period>1", true);
            if (!ret.result)
                return false;

            //расщепляем счетчики по периодам
            sql = " UPDATE " + table + " tt SET nzp_counter_child=(-1)*t.nzp_counter_child" + //инвертируем номер сгенерированного ПУ
                  " FROM t_other_periods t " +
                  " WHERE tt.dat_close is null " +
                  " and  t.nzp_counter=tt.nzp_counter and " +
                  " tt.dat_uchet BETWEEN t.date_from and t.date_to" +
                  " and t.num_period>1";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
                return false;

            ret = ExecSQL(conn_db, " Create index ix1_" + table + "_child on " + table + " (nzp_counter_child,dat_uchet) ", true);
            if (!ret.result)
                return false;


            #endregion Проводим разделение счетчиков по периодам валидности

            ExecSQL(conn_db, "DROP TABLE t_other_periods", false);

            return true;
        }

        #endregion разбить счетчик на производные счетчики при наличии разрывов в показаниях (поломка)

        #region функция - Получить значение параметра
        /// <summary>
        /// Получить значение параметра
        /// </summary>
        /// <typeparam name="T">тип параметра</typeparam>
        /// <param name="conn_db">соединение</param>
        /// <param name="pref">префикс</param>
        /// <param name="nzpPrm">номер параметра</param>
        /// <param name="prm_num">номер таблицы</param>
        /// <param name="ret">returns </param>
        /// <returns></returns>
        private T GetParamValue<T>(IDbConnection conn_db, string pref, int nzpPrm, int prm_num, out Returns ret)
        {
            var tableName = pref + sDataAliasRest + "prm_" + prm_num;
            var prm = new CalcMonthParams { pref = pref };
            var rec = Points.GetCalcMonth(prm);
            //текущий расчетный месяц этого банка
            var val = "max(val_prm)";
            if (typeof(T) == typeof(bool))
            {
                val = "max(case when val_prm=1 then 1 else 0 end)";
            }
            var CalcMonth = new DateTime(rec.year_, rec.month_, 1).ToShortDateString();
            var sql = string.Format(" Select {0} From {1} p " + " Where p.nzp_prm =  {2} and p.is_actual = 1 " +
                                    " and {3} between p.dat_s and p.dat_po",
                                    val, tableName, nzpPrm, Utils.EStrNull(CalcMonth));
            var res = CastValue<T>(ExecScalar(conn_db, sql, out ret, true));
            if (!ret.result)
            {
                MonitorLog.WriteLog("Ошибка получения параметра GetParamValue<T>: " +
                     ret.text, MonitorLog.typelog.Error, 1, 2, true);
            }
            return res;
        }
        #endregion функция - Получить значение параметра

        #region вставка расходов ИПУ по лс из группового ПУ для коммуналок - stek=1 & nzp_type = 3
        protected bool DivisionOKPU(IDbConnection conn_db, Rashod rashod, string p_dat_charge, out Returns ret)
        {
            #region  Выборка расходов ОКПУ

            // выбрать расходы ОКПУ для распределения
            ExecSQL(conn_db, " Drop table ttt_common_group ", false);
            ret = ExecSQL(conn_db,
                " Create temp table ttt_common_group " +
                " (  nzp_dom     integer not null, " +
                "    nzp_serv    integer default 0, " +
                "    nzp_counter integer default 0, " +
                "    val1        " + sDecimalType + "(15,7) default 0.00, " +
                "    val4        " + sDecimalType + "(15,7) default 0.00, " +
                "    is_pl  integer default 0, " +
                "    ls_ipu integer default 0 " +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }


            ret = ExecSQL(conn_db,
                " insert into ttt_common_group (nzp_dom, nzp_serv, nzp_counter, val1, val4, is_pl) " +
                " select c.nzp_dom, cs.nzp_serv, c.nzp_counter,sum(c.val1) val1,sum(c.val4) val4, max(cs.is_pl)" +
                " from " + rashod.counters_xx + " c, temp_cnt_spis cs " +
                " Where c.nzp_counter = cs.nzp_counter and cs.nzp_type = 4 and c.stek = 1 and c.nzp_type = 4 and " + rashod.where_dom +
                " group by 1,2,3 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " Create index ix1_ttt_common_group on ttt_common_group (nzp_counter) ", true);
            ExecSQL(conn_db, " Create index ix2_ttt_common_group on ttt_common_group (nzp_serv) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_common_group ", true);

            // установить признак для ОКПУ с вычитанием ИПУ
            var date = new DateTime(rashod.paramcalc.calc_yy, rashod.paramcalc.calc_mm, 1).ToShortDateString();
            ret = ExecSQL(conn_db,
                " Update ttt_common_group" +
                " set ls_ipu = 1" +
                " where exists (select 1 from ttt_prm_17 p " +
                              " Where p.nzp_prm = 1124 and p.dat_s <='" + date + "' and p.dat_po>'" + date + "' and nzp = ttt_common_group.nzp_counter) "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            #endregion Выборка расходов ОКПУ

            #region Темповая таблица для размножения записей

            ExecSQL(conn_db, " Drop table ttt_counters_common_kvar ", false);
            ret = ExecSQL(conn_db,
                " Create temp table ttt_counters_common_kvar " +
                " (  nzp_cntx    serial        not null, " +
                "    nzp_dom     integer       not null, " +
                "    nzp_kvar    integer       default 0 not null , " +
                "    nzp_type    integer       not null, " +               //1,2,3
                "    nzp_serv    integer       not null, " +
                "    dat_charge  date, " +
                "    cur_zap     integer       default 0 not null, " +     //0-текущее значение, >0 - ссылка на следующее значение (nzp_cntx)
                "    nzp_counter integer       default 0 , " +             //счетчик или вариатны расходов для stek=3
                "    cnt_stage   integer       default 0 , " +             //разрядность
                "    mmnog       " + sDecimalType + "(15,7) default 1.00 , " +             //масшт. множитель
                "    stek        integer       default 0 not null, " +     //3-итого по лс,дому; 1-счетчик; 2,3,4,5 - стек расходов
                "    rashod      " + sDecimalType + "(15,7) default 0.00 not null, " +  //общий расход в зависимости от stek
                "    dat_s       date          not null, " +               //"дата с" - для ПУ, для по-дневного расчета период в месяце (dp)
                "    val_s       " + sDecimalType + "(15,7) default 0.00 not null, " +  //значение (а также коэф-т)
                "    dat_po      date not null, " +                        //"дата по"- для ПУ, для по-дневного расчета период в месяце (dp_end)
                "    val_po      " + sDecimalType + "(15,7) default 0.00 not null, " +  //значение
                "    ngp_cnt       " + sDecimalType + "(14,7) default 0.0000000, " +  // расход на нежилые помещения
                "    rash_norm_one " + sDecimalType + "(14,7) default 0.0000000, " +  // норматив на 1 человека
                "    val1_g      " + sDecimalType + "(15,7) default 0.00 not null, " +  //расход по счетчику nzp_counter или нормативные расходы в расчетном месяце без учета вр.выбывших
                "    val1        " + sDecimalType + "(15,7) default 0.00 not null, " +  //расход по счетчику nzp_counter или нормативные расходы в расчетном месяце
                "    val2        " + sDecimalType + "(15,7) default 0.00 not null, " +  //дом: расход КПУ
                "    val3        " + sDecimalType + "(15,7) default 0.00         , " +  //дом: расход нормативщики
                "    val4        " + sDecimalType + "(15,7) default 0.00         , " +  //общий расход по счетчику nzp_counter
                "    rvirt       " + sDecimalType + "(15,7) default 0.00         , " +  //вирт. расход
                "    squ1        " + sDecimalType + "(15,7) default 0.00         , " +  //площадь лс, дома (по всем лс)
                "    squ2        " + sDecimalType + "(15,7) default 0.00         , " +  //площадь лс без КПУ (для домовых строк)
                "    cls1        integer       default 0 not null   , " +  //количество лс дома по услуге
                "    cls2        integer       default 0 not null   , " +  //количество лс без КПУ (для домовых строк)
                "    gil1_g      " + sDecimalType + "(15,7) default 0.00         , " +  //кол-во жильцов в лс без учета вр.выбывших
                "    gil1        " + sDecimalType + "(15,7) default 0.00         , " +  //кол-во жильцов в лс
                "    gil2        " + sDecimalType + "(15,7) default 0.00         , " +  //кол-во жильцов в лс
                "    cnt1_g      integer       default 0 not null, " +     //кол-во жильцов в лс (нормативное) без учета вр.выбывших
                "    cnt1        integer       default 0 not null, " +     //кол-во жильцов в лс (нормативное)
                "    cnt2        integer       default 0 not null, " +     //кол-во комнат в лс
                "    cnt3        integer       default 0, " +              //тип норматива в зависимости от услуги (ссылка на resolution.nzp_res)
                "    cnt4        integer       default 0, " +              //1-дом не-МКД (0-МКД)
                "    cnt5        integer       default 0, " +              //резерв
                "    dop87       " + sDecimalType + "(15,7) default 0.00         , " +  //доп.значение 87 постановления (7кВт или добавок к нормативу  (87 П) )
                "    pu7kw       " + sDecimalType + "(15,7) default 0.00         , " +  //7 кВт для КПУ (откорректированный множитель)
                "    gl7kw       " + sDecimalType + "(15,7) default 0.00         , " +  //7 кВт КПУ * gil1 (учитывая корректировку)
                "    vl210       " + sDecimalType + "(15,7) default 0.00         , " +  //расход 210 для nzp_type = 6
                "    kf307       " + sDecimalType + "(15,7) default 0.00         , " +  //коэфициент 307 для КПУ или коэфициент 87 для нормативщиков
                "    kf307n      " + sDecimalType + "(15,7) default 0.00         , " +  //коэфициент 307 для нормативщиков
                "    kf307f9     " + sDecimalType + "(15,7) default 0.00         , " +  //коэфициент 307 по формуле 9
                "    kf_dpu_kg   " + sDecimalType + "(15,7) default 0.00         , " +  //коэфициент ДПУ для распределения пропорционально кол-ву жильцов
                "    kf_dpu_plob " + sDecimalType + "(15,7) default 0.00         , " +  //коэфициент ДПУ для распределения пропорционально сумме общих площадей
                "    kf_dpu_plot " + sDecimalType + "(15,7) default 0.00         , " +  //коэфициент ДПУ для распределения пропорционально сумме отапливаемых площадей
                "    kf_dpu_ls   " + sDecimalType + "(15,7) default 0.00         , " +  //коэфициент ДПУ для распределения пропорционально кол-ву л/с
                "    dlt_in      " + sDecimalType + "(15,7) default 0.00         , " +  //входящии нераспределенный расход (остаток)
                "    dlt_cur     " + sDecimalType + "(15,7) default 0.00         , " +  //текущая дельта
                "    dlt_reval   " + sDecimalType + "(15,7) default 0.00         , " +  //перерасчет дельты за прошлые месяцы
                "    dlt_real_charge " + sDecimalType + "(15,7) default 0.00     , " +  //перерасчет дельты за прошлые месяцы
                "    dlt_calc    " + sDecimalType + "(15,7) default 0.00         , " +  //распределенный (учтенный) расход
                "    dlt_out     " + sDecimalType + "(15,7) default 0.00         , " +  //исходящии нераспределенный расход (остаток)
                "    kod_info    integer default 0," +
                "    sqgil       " + sDecimalType + "(15,7) default 0.00         ," +  //жилая площадь лс
                "    is_day_calc integer not null, " +
                "    is_use_knp integer default 0, " +
                "    is_use_ctr integer default 0," +//Количество временно выбывших
                "    nzp_period  integer not null, " +
                "    cntd integer," +
                "    cntd_mn integer, " +
                "    nzp_measure integer, " + // ед.измерения 
                "    norm_type_id integer, " + // id типа норматива - для нового режима введения нормативов
                "    norm_tables_id integer, " + // id норматива - по нему можно получить набор влияющих пар-в и их знач.
                "    val1_source " + sDecimalType + "(15,7) default 0.00 not null, " +  //val1 без учета повышающего коэффициента
                "    val4_source " + sDecimalType + "(15,7) default 0.00 not null, " +  //val4 без учета повышающего коэффициента
                "    up_kf " + sDecimalType + "(15,7) default 1.00 not null, " +   //повышающий коэффициент для нормативного расхода
                "    is_pl integer default 0, " +
                "    sum_val1 " + sDecimalType + "(15,7) default 0.00 not null, " +
                "    count_ls integer default 0, " +
                "    ls_ipu integer default 0 " +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // сформировать расходы по ЛС для учета ОКПУ
            ret = ExecSQL(conn_db,
                " INSERT INTO ttt_counters_common_kvar ( " +
                " nzp_cntx, nzp_dom, nzp_kvar, nzp_type, nzp_serv, dat_charge, " +
                " cur_zap, nzp_counter, cnt_stage, mmnog, stek, rashod," +
                " dat_s, dat_po, val_s, val_po," +
                " val3, val4, " + //" val1_g, val1, val2, rvirt," +
                " ngp_cnt, rash_norm_one, squ1, squ2, cls1, cls2, gil1_g, gil1, " +
                " gil2, cnt1_g, cnt1, cnt2, cnt3, cnt4, cnt5, dop87, pu7kw, gl7kw, " +
                " vl210, kf307, kf307n, kf307f9, kf_dpu_kg, kf_dpu_plob, kf_dpu_plot, " +
                " kf_dpu_ls, dlt_in, dlt_cur, dlt_reval, dlt_real_charge, dlt_calc, dlt_out," +
                " kod_info," +
                " norm_type_id, norm_tables_id, val1_source, val4_source, up_kf,is_pl,is_day_calc," +
                " nzp_period,cntd,cntd_mn,nzp_measure,ls_ipu) " +

                " select c.nzp_cntx, c.nzp_dom, c.nzp_kvar, c.nzp_type, c.nzp_serv, c.dat_charge, " +
                " c.cur_zap, cs.nzp_counter, c.cnt_stage, c.mmnog, c.stek, c.rashod," +
                " c.dat_s, c.dat_po, c.val_s, c.val_po, " +
                " cs.val1, cs.val4," + //" c.val1_g, c.val2, c.val3, c.rvirt," + // - расход ОКПУ
                " c.ngp_cnt, c.rash_norm_one, c.squ1, c.squ2, c.cls1, c.cls2, c.gil1_g, c.gil1, " +
                " c.gil2, c.cnt1_g, c.cnt1, c.cnt2, c.cnt3, c.cnt4, c.cnt5, c.dop87, c.pu7kw, c.gl7kw, " +
                " c.vl210, c.kf307, c.kf307n, c.kf307f9, c.kf_dpu_kg, c.kf_dpu_plob, c.kf_dpu_plot, " +
                " c.kf_dpu_ls, c.dlt_in, c.dlt_cur, c.dlt_reval, c.dlt_real_charge, c.dlt_calc, c.dlt_out," +
                " 4 kod_info," +
                " c.norm_type_id, c.norm_tables_id, c.val1_source, c.val4_source, c.up_kf,cs.is_pl,c.is_day_calc," +
                " c.nzp_period,c.cntd,c.cntd_mn,c.nzp_measure,cs.ls_ipu " +
                " from ttt_counters_xx c, ttt_common_group cs, temp_counters_link cl " +
                "  where c.nzp_kvar = cl.nzp_kvar and c.nzp_serv = cs.nzp_serv and cl.nzp_counter = cs.nzp_counter "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " Create index ix1_sqcommon_kvar on ttt_counters_common_kvar (nzp_counter,nzp_serv) ", true);
            ExecSQL(conn_db, " Create index ix2_sqcommon_kvar on ttt_counters_common_kvar (nzp_kvar,nzp_serv,ls_ipu) ", true);
            ExecSQL(conn_db, " Create index ix3_sqcommon_kvar on ttt_counters_common_kvar (stek,nzp_kvar,nzp_counter,nzp_serv) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_counters_common_kvar ", true);

            // записать сумму ИПУ по ЛС для ОКПУ с вычитанием ИПУ
            ret = ExecSQL(conn_db,
                " Update ttt_counters_common_kvar" +
                " set" +
                    " rvirt = tt.sum_val_ipu" +
                " from" +
                  " (select nzp_kvar,nzp_serv,sum(val1) sum_val_ipu from ttt_counters_ipu " +
                    " Where stek = 1 " +
                    " group by nzp_kvar,nzp_serv) tt" +
                " where tt.nzp_kvar = ttt_counters_common_kvar.nzp_kvar and tt.nzp_serv = ttt_counters_common_kvar.nzp_serv and ttt_counters_common_kvar.ls_ipu = 1 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            var tableSumForOKPU = "ttt_sum_for_okpu_" + DateTime.Now.Ticks;
            var sql = " CREATE TEMP TABLE " + tableSumForOKPU + " AS " +
                     "  SELECT nzp_counter,nzp_serv,sum(gil1) gil1, sum(squ2) squ2,count(distinct nzp_kvar) cls1,sum(squ1) as squ1,sum(val1) as val1 " +
                     "  FROM ttt_counters_common_kvar" +
                     "  WHERE stek = 3 " +
                     "  GROUP BY nzp_counter,nzp_serv";
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, " Create index ix1_ttt_sum_for_okpu on " + tableSumForOKPU + " (nzp_counter,nzp_serv) ", true);


            // обмен: расчет дома - пишем в counters_xx, расчет лс - считываем
            if (rashod.paramcalc.nzp_kvar <= 0) //расчет дома/списка домов/банка
            {

                sql = " UPDATE " + rashod.counters_xx + " c  " +
                      " SET" +
                      " gil2 = tt.gil1," +
                      " pu7kw = tt.squ2," +
                      " cls2 = tt.cls1," +
                      " gl7kw = tt.squ1" +
                      " FROM " + tableSumForOKPU + " tt " +
                      " WHERE c.nzp_counter=tt.nzp_counter " +
                      " AND c.nzp_serv=tt.nzp_serv " +
                      " AND c.nzp_type=4 AND c.stek=1";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
            }
            else
            {
                sql = " UPDATE " + tableSumForOKPU + " s SET " +
                      " gil1 = tt.gil2," +
                      " squ2 = tt.pu7kw," +
                      " cls1 = tt.cls2," +
                      " squ1 = tt.gl7kw" +
                      " FROM " + rashod.counters_xx + " tt " +
                      " WHERE s.nzp_counter=tt.nzp_counter " +
                      " AND s.nzp_serv=tt.nzp_serv " +
                      " AND tt.nzp_type=4 AND tt.stek=1";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                {
                    DropTempTablesRahod(conn_db, rashod.paramcalc.pref);
                    return false;
                }
            }

            #endregion


            // записать суммарные значения для разных видов распределний
            ret = ExecSQL(conn_db,
                " Update ttt_counters_common_kvar" +
                " set" +
                    " gil2 = tt.gil1," +
                    " pu7kw = tt.squ2," +
                    " cls2 = tt.cls1," +
                    " gl7kw = tt.squ1," +
                    " sum_val1 = tt.val1 " +
                "  from " + tableSumForOKPU + " tt" +
                " where tt.nzp_counter = ttt_counters_common_kvar.nzp_counter and tt.nzp_serv = ttt_counters_common_kvar.nzp_serv "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            ExecSQL(conn_db, "DROP TABLE " + tableSumForOKPU, false);
            #region Пропорциональное распределение

            // вычесть расходы ИПУ, где указано. здесь: vl210 = сохраняемое значение расхода для распределения по ЛС
            ret = ExecSQL(conn_db,
                " Update ttt_counters_common_kvar " +
                " set" +
                " vl210 =" +
                    " CASE WHEN ls_ipu = 0" +
                    " THEN val3" +
                    " ELSE (CASE WHEN (val3 - rvirt) > 0 THEN (val3 - rvirt) ELSE 0 END)" +
                    " END " +
                " where 1 = 1 ", true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // вычислить расход на ЛС для итогового стека = 1 (ttt_counters_common_kvar он пока =3!)
            ret = ExecSQL(conn_db,
                " Update ttt_counters_common_kvar " +
                " set" +
                " val1 = " +
                    "(CASE WHEN is_pl = 0" +
                    " THEN vl210 * gil1 / (CASE WHEN gil2=0 THEN 1 ELSE gil2 END)" +
                    " ELSE " +
                        "(CASE WHEN is_pl = 1" +
                        " THEN vl210 * squ1 / (CASE WHEN gl7kw=0 THEN 1 ELSE gl7kw END)" +
                        " ELSE " +
                            "(CASE WHEN is_pl = 2" +
                            " THEN vl210 * squ2/(CASE WHEN pu7kw=0 THEN 1 ELSE pu7kw END)" +
                            " ELSE vl210 * cls1/(CASE WHEN cls2 =0 THEN 1 ELSE cls2 END)" +
                            " END)" +
                        " END)" +
                    " END)" +
                " where stek = 3 ", true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // записать распределенное значения для ЛС/ОКПУ. здесь: kf_dpu_ls = сохраняемое значение расхода для распределения по дням
            ret = ExecSQL(conn_db,
                " Update ttt_counters_common_kvar" +
                " set" +
                    " kf_dpu_ls = tt.val1 " +
                "  from" +
                    " (select nzp_kvar,nzp_counter,nzp_serv,sum(val1) as val1 " +
                    "  from ttt_counters_common_kvar" +
                    "  where stek = 3 " +
                    "  group by nzp_kvar,nzp_counter,nzp_serv" +
                    " ) tt" +
                " where stek = 10" +
                  " and tt.nzp_kvar    = ttt_counters_common_kvar.nzp_kvar" +
                  " and tt.nzp_counter = ttt_counters_common_kvar.nzp_counter" +
                  " and tt.nzp_serv    = ttt_counters_common_kvar.nzp_serv "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // размазать распределенные значения для ЛС/ОКПУ по дням
            ret = ExecSQL(conn_db,
                " Update ttt_counters_common_kvar " +
                " set val1 = kf_dpu_ls * (cntd * 1.00 / cntd_mn) " +
                " where stek = 10 ", true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            #endregion

            #region Добавление записи в counters_xx

            ret = ExecSQL(conn_db,
                " INSERT INTO " + rashod.counters_xx + " ( " +
                " nzp_dom, nzp_kvar, nzp_type, nzp_serv, dat_charge, " +
                " cur_zap, nzp_counter, cnt_stage, mmnog," +
                " stek," +
                " rashod, dat_s, val_s, dat_po, val_po, ngp_cnt, rash_norm_one, val1_g, val1, " +
                " val2, val3, val4, rvirt, squ1, squ2, cls1, cls2, gil1_g, gil1, " +
                " gil2, cnt1_g, cnt1, cnt2, cnt3, cnt4, cnt5, dop87, pu7kw, gl7kw, " +
                " vl210, kf307, kf307n, kf307f9, kf_dpu_kg, kf_dpu_plob, kf_dpu_plot, " +
                " kf_dpu_ls, dlt_in, dlt_cur, dlt_reval, dlt_real_charge, dlt_calc, " +
                " dlt_out, kod_info, norm_type_id, norm_tables_id, val1_source, " +
                " val4_source, up_kf) " +
                " select c.nzp_dom, c.nzp_kvar, c.nzp_type, c.nzp_serv, c.dat_charge, " +
                " c.cur_zap, c.nzp_counter, c.cnt_stage, c.mmnog," +
                " (case when c.stek=3 then 1 else 11 end) as stek," +
                " c.rashod, c.dat_s, c.val_s, c.dat_po, c.val_po, c.ngp_cnt, c.rash_norm_one, c.val1_g, c.val1," +
                " c.val2, c.val3, c.val4, c.rvirt, c.squ1, c.squ2, c.cls1, c.cls2, c.gil1_g, c.gil1, " +
                " c.gil2, c.cnt1_g, c.cnt1, c.cnt2, c.cnt3, c.cnt4, c.cnt5, c.dop87, c.pu7kw, c.gl7kw, " +
                " c.vl210, c.kf307, c.kf307n, c.kf307f9, c.kf_dpu_kg, c.kf_dpu_plob, c.kf_dpu_plot, " +
                " c.kf_dpu_ls, c.dlt_in, c.dlt_cur, c.dlt_reval, c.dlt_real_charge, c.dlt_calc, " +
                " c.dlt_out, c.kod_info, c.norm_type_id, c.norm_tables_id, c.val1_source, " +
                " c.val4_source, c.up_kf" +
                " from ttt_counters_common_kvar c "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            #endregion

            #region Добавление записи в ttt_counters_ipu
            ret = ExecSQL(conn_db,
                 " Insert into ttt_counters_ipu " +
                  "(nzp_cntx,nzp_kvar,nzp_serv,nzp_counter,stek,dat_s,dat_po,val1,val3,val4,rvirt," +
                   "nzp_dom,val_s,val_po,ngp_cnt,cnt_stage,mmnog,kod_info,gil1," +
                   "nzp_period,dp,dp_end,cntd,cntd_mn) " +
                 " Select " +
                   "c.nzp_cntx,c.nzp_kvar,c.nzp_serv,c.nzp_counter,(case when stek=3 then 1 else 11 end) as stek,c.dat_s,c.dat_po,c.val1,c.val3,c.val4,c.rvirt," +
                   "c.nzp_dom,c.val_s,c.val_po,c.ngp_cnt,c.cnt_stage,c.mmnog,c.kod_info,c.gil1," +
                   "c.nzp_period,c.dat_s,c.dat_po,c.cntd,c.cntd_mn " +
                 " From ttt_counters_common_kvar c "
                 , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            #endregion

            ExecSQL(conn_db, " Drop table ttt_common_group ", false);
            ExecSQL(conn_db, " Drop table ttt_counters_common_kvar ", false);
            return true;
        }



        #endregion вставка расходов ИПУ по лс из группового ПУ для коммуналок - stek=1 & nzp_type = 3
    }

}

#endregion здесь производится подсчет расходов
