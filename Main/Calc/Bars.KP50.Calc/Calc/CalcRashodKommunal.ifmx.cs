// ������� ��������

#region ������������ ������

using System;
using System.Data;

using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

#endregion ������������ ������

#region ����� ������������ ������� ��������

namespace STCLINE.KP50.DataBase
{
   
    //����� ��������� ������ ��� �������� ��������
    public partial class DbCalcCharge : DataBaseHead
    {
        #region ����������� �������� ���������� � �� ����������
        public bool CreateTempRashodKommunal(IDbConnection conn_db, Rashod rashod, bool b_calc_kvar, out Returns ret)
        {
            #region �������� ������� ������������ �� - �������� � mmnog ikvar?, ��� - nzp_kvar!, �.�. ikvar �.�. = 0
            //----------------------------------------------------------------
            //���� 3 - �������� ������� ������������ �� - �������� � mmnog ikvar?, ��� - nzp_kvar!, �.�. ikvar �.�. = 0
            //----------------------------------------------------------------
            ExecSQL(conn_db, " Drop table ttt_aid_c1 ", false);

            ret = ExecSQL(conn_db,
                " Create temp table ttt_aid_c1 " +
                " ( nzp_kvar integer, " +
                "   nzp_dom  integer, " +
                "   mmnog    integer  " +
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // �������� ����������� ������������ ��������
            ret = ExecSQL(conn_db,
                  " Insert Into ttt_aid_c1 (nzp_kvar,nzp_dom,mmnog) " +
                  " Select k.nzp_kvar,k.nzp_dom,t.nzp_kvar_base as mmnog " +
                  " From " + rashod.paramcalc.data_alias + "kvar k," + rashod.paramcalc.data_alias + "link_ls_lit t, ttt_prm_1 p  " +
                  " Where k." + rashod.where_dom + rashod.where_kvarK +
                  "   and k.nzp_kvar = t.nzp_kvar " +
                  "   and k.nzp_kvar = p.nzp " +
                  "   and p.nzp_prm = 3 " +
                  "   and p.val_prm = '2' " +
                  " Group by 1,2,3 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // �������� ��������� ������������ ��������
            ret = ExecSQL(conn_db,
                  " insert into ttt_aid_c1 (nzp_kvar,nzp_dom,mmnog) " +
                  " Select k.nzp_kvar,k.nzp_dom,max(case when k.ikvar > 0 then k.ikvar else k.nzp_kvar end) as mmnog " +
                  " From " + rashod.paramcalc.data_alias + "kvar k, ttt_prm_1 p  " +
                  " Where k." + rashod.where_dom + rashod.where_kvarK +
                  "   and k.nzp_kvar = p.nzp " +
                  "   and p.nzp_prm = 3 " +
                  "   and p.val_prm = '2' " +
                  "   and not exists (select 1 from " + rashod.paramcalc.data_alias + "link_ls_lit a where a.nzp_kvar=k.nzp_kvar)" +
                  " Group by 1,2 "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }


            ExecSQL(conn_db, " Create unique index ix_ttt_aid_c1 on ttt_aid_c1 (nzp_kvar) ", true);
            ExecSQL(conn_db, sUpdStat + " ttt_aid_c1 ", true);

            ret = ExecSQL(conn_db,
                " Update ttt_counters_xx " +
                " Set mmnog = ( Select mmnog From ttt_aid_c1 k Where ttt_counters_xx.nzp_kvar = k.nzp_kvar ) " +
                " Where exists ( Select 1 From ttt_aid_c1 k Where ttt_counters_xx.nzp_kvar = k.nzp_kvar ) "
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            // ����������� � ����������� ���� �� ������� - ttt_aid_c1 - ������ ���������� ��� ����� 29
            #endregion �������� ������� ������������ �� - �������� � mmnog ikvar?, ��� - nzp_kvar!, �.�. ikvar �.�. = 0

            #region ����������� �������� ���������� � �� ����������

            // ���� ���������� �������� ����������
            ExecSQL(conn_db, " Drop table t_ans_kommunal ", false);
            ret = ExecSQL(conn_db,
                " Create temp table t_ans_kommunal (" +
                " nzp_no serial not null," +
                " nzp_dom  integer not null, " +
                " nzp_kvar integer not null, " +
                " nkvar  " + sDecimalType + "(18,7) not null, " +
                " val1   " + sDecimalType + "(14,7) default 0.0000000, " +
                " val2   " + sDecimalType + "(14,7) default 0.0000000, " +
                " val3   " + sDecimalType + "(14,7) default 0.0000000, " +
                " val4   " + sDecimalType + "(14,7) default 0.0000000, " +
                " kf307  " + sDecimalType + "(14,7) default 0.0000000, " +
                " kf307n " + sDecimalType + "(14,7) default 0.0000000, " +
                " kf354  " + sDecimalType + "(14,7) default 0.0000000, " +
                " squ1   " + sDecimalType + "(14,7) default 0.0000000, " +
                " squ2   " + sDecimalType + "(14,7) default 0.0000000, " +
                " gil1   " + sDecimalType + "(14,7) default 0.0000000, " +
                " cnt1 integer default 0, " +
                " rastot " + sDecimalType + "(14,7) default 0.0000000, " +   //rashod
                " rasind " + sDecimalType + "(14,7) default 0.0000000, " +   //val_s 
                " kol_komn integer default 0, " +               //cnt5
                " cnt3 integer     default 0, " +               //nzp_res
                " is2075 integer   default 0  " +               // ������� ���� ����������     
                " ) " + sUnlogTempTable
                , true);
            if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

            if (!b_calc_kvar)
            {
                //----------------------------------------------------------------
                // ��������� ���� 29 ��� ���������� ���� ���������� � ������� �������� ��� ���� 354 ������ ��� ������� ����
                // ��� ������� �� 1 �/� ������������ ����� �� ������������� ��� ������������ ������� ��������
                //----------------------------------------------------------------

                // ������ ���������� � ������ � ������ ��������� - �� ttt_counters_xx ��������� ������ �������� ��!
                ret = ExecSQL(conn_db,
                    " Insert into t_ans_kommunal " +
                    " (nzp_kvar,nzp_dom,nkvar,squ1,squ2) " +
                    " Select a.nzp_kvar,a.nzp_dom,b.mmnog,max(a.squ1),max(a.sqgil)" +
                    " From ttt_counters_xx a,ttt_aid_c1 b" +
                    " Where a.nzp_kvar=b.nzp_kvar " +
                    " group by 1,2,3 "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // ��������� ������ ���������� ��������� ������������ � ������ � ������ ���������
                ExecSQL(conn_db, " Drop table t_ans_komm_sq ", false);
                ret = ExecSQL(conn_db,
                    " Create temp table t_ans_komm_sq " +
                    " ( nzp_kvar integer," +
                    "   nzp_dom  integer," +
                    "   mmnog    integer," +
                    "   sq       " + sDecimalType + "(12,4) default 0," +
                    "   sqgil    " + sDecimalType + "(12,4) default 0," +
                    "   sqot     " + sDecimalType + "(12,4) default 0 " +
                    " )  " + sUnlogTempTable
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // ������ �������� ��
                ret = ExecSQL(conn_db,
                    " Insert into t_ans_komm_sq (nzp_kvar,nzp_dom,mmnog) " +
                    " Select t.nzp_kvar,t.nzp_dom,t.mmnog " +
                    " From ttt_aid_c1 t" +
                    " Where not exists (Select 1 From t_ans_kommunal a where t.nzp_kvar=a.nzp_kvar) "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                ExecSQL(conn_db, " Create unique index ix_ans11_sq on t_ans_komm_sq (nzp_kvar) ", true);
                ExecSQL(conn_db, sUpdStat + " t_ans_komm_sq ", true);

                // ����� �������
                ret = ExecSQL(conn_db, 
                    " Update t_ans_komm_sq " +
                    " Set sq   = ( select p.val_prm from ttt_prm_1 p where p.nzp=t_ans_komm_sq.nzp_kvar and p.nzp_prm=4 )" + sConvToNum + " " +
                    " Where exists ( select 1 from ttt_prm_1 p where p.nzp=t_ans_komm_sq.nzp_kvar and p.nzp_prm=4 ) "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // ������������ �������
                ret = ExecSQL(conn_db, 
                    " Update t_ans_komm_sq " +
                    " Set sqot = ( select p.val_prm from ttt_prm_1 p where p.nzp=t_ans_komm_sq.nzp_kvar and p.nzp_prm=133 ) " + sConvToNum + " " +
                    " Where  0 < ( select count(*)  from ttt_prm_1 p where p.nzp=t_ans_komm_sq.nzp_kvar and p.nzp_prm=133 ) "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // ����� ������� - ����� ��� ���������� - ��� ���������� ������� �� ����.354
                ret = ExecSQL(conn_db, 
                    " Update t_ans_komm_sq " +
                    " Set sqgil = ( select p.val_prm from ttt_prm_1 p where p.nzp=t_ans_komm_sq.nzp_kvar and p.nzp_prm=6 ) " + sConvToNum + " " +
                    " Where   0 < ( select count(*)  from ttt_prm_1 p where p.nzp=t_ans_komm_sq.nzp_kvar and p.nzp_prm=6 ) "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                ret = ExecSQL(conn_db,
                    " Insert into t_ans_kommunal " +
                    " (nzp_kvar,nzp_dom,nkvar,squ1,squ2) " +
                    " Select nzp_kvar,nzp_dom,mmnog,max(sq),max(sqgil)" +
                    " From t_ans_komm_sq " +
                    " Where sq > 0 or sqgil > 0 " +
                    " group by 1,2,3 "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                //
                ExecSQL(conn_db, " Drop table t_ans_komm_sq ", false);
                
                #region 29 ���� ��� ���������� � ������ ���� ������ ������� !b_calc_kvar

                //----------------------------------------------------------------
                // ��������� ���� 29 ��� ���������� ���� ���������� � ������� �������� ��� ���� 354 ������ ��� ������� ����
                // ��� ������� �� 1 �/� ������������ ����� �� ������������� ��� ������������ ������� ��������
                //----------------------------------------------------------------

                // �������� ���-�� ������� ��� ���������� ��� ����� 29 - ������� ��� ������ ������������ �� ����������� ��������
                ret = ExecSQL(conn_db,
                    " Update t_ans_kommunal " +
                    " Set gil1 = ( Select max(vald) From ttt_aid_c2 k Where t_ans_kommunal.nzp_kvar = k.nzp_kvar ) " +
                    " Where 0 < ( Select count(*) From ttt_aid_c2 k Where t_ans_kommunal.nzp_kvar = k.nzp_kvar ) "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                ret = ExecSQL(conn_db,
                    " Update t_ans_kommunal " +
                    " Set cnt1 = Round(gil1) " +
                    " Where 1=1 "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                // ������ ��������� �������� � ���-�� ������� �� ���������, ��� ���� ����������
                ExecSQL(conn_db, " Drop table t_ans_itog ", false);
                ret = ExecSQL(conn_db,
                    " Create temp table t_ans_itog " +
                    " ( nzp_dom  integer not null, " +
                    "   nkvar  " + sDecimalType + "(18,7) not null, " +
                    "   squ1   " + sDecimalType + "(14,7) default 0.0000000, " +
                    "   squ2   " + sDecimalType + "(14,7) default 0.0000000, " +
                    "   gil1   " + sDecimalType + "(14,7) default 0.0000000, " +
                    "   cnt1 integer default 0 " +
                    " ) " + sUnlogTempTable
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                ret = ExecSQL(conn_db,
                    " Insert into t_ans_itog (nzp_dom,nkvar,squ1,squ2,gil1,cnt1) " +
                    " Select nzp_dom,nkvar,sum(squ1) squ1,sum(squ2) squ2,sum(gil1) gil1,sum(cnt1) cnt1 " +
                    " From t_ans_kommunal " +
                    " Group by 1,2 "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                ExecSQL(conn_db, " Create index ix_t_ans_itog on t_ans_itog (nzp_dom,nkvar) ", true);
                ExecSQL(conn_db, sUpdStat + " t_ans_itog ", true);

                ret = ExecSQL(conn_db,
                    " Update t_ans_kommunal Set " +
                    " val1 = ( Select squ1 From t_ans_itog k Where t_ans_kommunal.nzp_dom = k.nzp_dom and t_ans_kommunal.nkvar = k.nkvar )," +
                    " val2 = ( Select squ2 From t_ans_itog k Where t_ans_kommunal.nzp_dom = k.nzp_dom and t_ans_kommunal.nkvar = k.nkvar )," +
                    " val3 = ( Select gil1 From t_ans_itog k Where t_ans_kommunal.nzp_dom = k.nzp_dom and t_ans_kommunal.nkvar = k.nkvar )," +
                    " val4 = ( Select cnt1 From t_ans_itog k Where t_ans_kommunal.nzp_dom = k.nzp_dom and t_ans_kommunal.nkvar = k.nkvar ) " +
                    " Where 0 < ( Select count(*) From t_ans_itog k" +
                    "             Where t_ans_kommunal.nzp_dom = k.nzp_dom and t_ans_kommunal.nkvar = k.nkvar ) "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                string sql;
                if (Points.IsSmr)
                {
                    // ��� ������ ����-� �� ����� ������� ��������� �� 4� ������
                    sql =
                    " kf307  = case when val2>0.0001 then Round( (squ2/val2) * 10000 )/10000 else 0 end, " +
                    " kf307n = case when val3>0.0001 then gil1/val3 else 0 end  ";

                }
                else
                {
                    sql =
                    " kf307  = case when val2>0.0001 then squ2/val2 else 0 end, " +
                    " kf307n = case when val3>0.0001 then gil1/val3 else 0 end  ";
                }
                ret = ExecSQL(conn_db,
                    " Update t_ans_kommunal " +
                    " Set" + sql +
                    " Where 1=1 "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                #region ������� ���� � �������� �������
                ret = ExecSQL(conn_db,
                    " Update t_ans_kommunal " +
                    " Set is2075 =1 " +
                    " Where 0 < ( Select count(*) From ttt_prm_2 k Where t_ans_kommunal.nzp_dom = k.nzp and k.nzp_prm =2075 and k.val_prm ='1' ) "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                ret = ExecSQL(conn_db,
                    " Update t_ans_kommunal " +
                    " Set is2075 =1 " +
                    " Where 0 < ( Select count(*) From ttt_prm_1 k Where t_ans_kommunal.nzp_kvar = k.nzp and k.nzp_prm =2076 and k.val_prm ='1' ) "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                #endregion ������� ���� � �������� �������

                #endregion 29 ���� ��� ���������� � ������ ���� ������ ������� !b_calc_kvar
            }
            else
            {
                // ��� ������� �� �/� t_ans_kommunal �� ������!
                // nzp_kvar � cur_zap!!! ����� �������� �� ������� 1�� �/�! ���� ������ ������� �� ��������!?
                ret = ExecSQL(conn_db,
                    " Insert into t_ans_kommunal" +
                    " (nzp_dom,nzp_kvar,nkvar,val1,val2,val3,val4,squ1,squ2,gil1,cnt1,kf307,kf307n, rastot, rasind,kol_komn, is2075)" +
                    " select nzp_dom,cur_zap,mmnog,val1,val2,val3,val4,squ1,squ2,gil1,cnt1,kf307,kf307n, rashod, val_s, cnt5, cnt4" +
                    " From " + rashod.counters_xx +
                    " Where nzp_type = 3 and stek = 29  " + rashod.where_kvar.Replace("nzp_kvar", "cur_zap") + rashod.paramcalc.per_dat_charge
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
            }
            ExecSQL(conn_db, " Create unique index ix1_t_ans_kommunal on t_ans_kommunal (nzp_no) ", true);
            ExecSQL(conn_db, " Create unique index ix2_t_ans_kommunal on t_ans_kommunal (nzp_kvar) ", true);
            ExecSQL(conn_db, sUpdStat + " t_ans_kommunal ", true);

            ExecSQL(conn_db, " Drop table ttt_aid_c1 ", false);
            ExecSQL(conn_db, " Drop table ttt_aid_c2 ", false);

            #endregion ����������� �������� ���������� � �� ����������

            return true;
        }
        #endregion ����������� �������� ���������� � �� ����������

        #region �������� � ������ ��� ���������� �� ���� � ���������� ����� 29 - ���� ������ ���� ��� ��
        public bool SetAndSaveTempRashodKommunal(IDbConnection conn_db, Rashod rashod, bool b_calc_kvar, 
            bool bIsSaha, string p_dat_charge, out Returns ret)
        {
            ret = Utils.InitReturns();

            if (bIsSaha) { /* � ���� ������ �� ������ */ }
            else
            {
                #region ���������� ���������� ������ ��� ��������� � ������� = ��� ��-��� � t_ans_kommunal

                //2075|��������� �������� ���� ��� ���������� � ���������|||bool||2||||

                ret = ExecSQL(conn_db, 
                    " Update t_ans_kommunal " +
                    " Set" +
                    // ���������� ������ ��� ��������� ���������� � t_ans_kommunal.kol_komn
                    " kol_komn =" + sNvlWord +
                    " ((select max(cnt2) from ttt_counters_xx " +
                    "   where nzp_serv in (25,210,410) and t_ans_kommunal.nzp_kvar= ttt_counters_xx.nzp_kvar ),0) " +
                    // ��� �� ���������� cnt3 =��� ��-��� (�� ���� ����������� ������� �� � �����)
                    ",cnt3  =" + sNvlWord +
                    " ((select max(cnt3) from ttt_counters_xx " +
                    "   where nzp_serv in (25,210,410) and t_ans_kommunal.nzp_kvar= ttt_counters_xx.nzp_kvar ),0) " +
                    " Where is2075=1 "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                #endregion ���������� ���������� ������ ��� ��������� � ������� = ��� ��-��� � t_ans_kommunal

                #region ���������� ����� ����� � ��������������

                ret = ExecSQL(conn_db, 
                    " Update t_ans_kommunal " +
                    " Set rastot = " + sNvlWord +
                    "(( Select value From " + rashod.paramcalc.kernel_alias + "res_values " +
                                 " Where nzp_res = cnt3 " +
                                 "   and nzp_y = (case when val3 > 6 then 6 else val3 end) " + //���-�� �����
                                 "   and nzp_x = (case when kol_komn > 5 then 5 else kol_komn end) " + //���-�� ������
                                 " ),'0')" + sConvToNum +
                    " Where cnt3 in ( Select nzp_res From " + rashod.paramcalc.kernel_alias + "res_values ) " +
                      " and is2075=1 "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                #endregion ���������� ����� ����� � ��������������

                #region ���������� ������ (�������� ������� �� ���������, �������� �����)

                ret = ExecSQL(conn_db, 
                    " Update ttt_counters_xx " +
                    " Set  " +
                    " val1 =" + sNvlWord +
                    "((select t.rastot*t.gil1 from t_ans_kommunal t where t.nzp_kvar=ttt_counters_xx.nzp_kvar and t.is2075=1),0)  " +
                    ", rash_norm_one =" + sNvlWord +
                    "((select t.rastot        from t_ans_kommunal t where t.nzp_kvar=ttt_counters_xx.nzp_kvar and t.is2075=1),0)  " +
                    ", val1_g =" + sNvlWord +
                    "((select t.rastot*t.gil1 from t_ans_kommunal t where t.nzp_kvar=ttt_counters_xx.nzp_kvar and t.is2075=1),0)  " +
                    ", gil2 =" + sNvlWord +
                    "((select t.val3          from t_ans_kommunal t where t.nzp_kvar=ttt_counters_xx.nzp_kvar and t.is2075=1),0) " +
                    " Where nzp_serv in (25,210) " +
                    "   and cnt1_g > 0 and cnt2 > 0 " +
                    "   and 0< (select count(*) from t_ans_kommunal t where t.nzp_kvar=ttt_counters_xx.nzp_kvar and is2075=1 ) "
                    , true);
                if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                #endregion ���������� ������

                #region ��������� � 29 �����
                if (!b_calc_kvar)
                {
                    string sql =
                        " Insert into " + rashod.counters_xx +
                        " ( stek,nzp_type,dat_charge,nzp_kvar,cur_zap,nzp_dom,nzp_counter,nzp_serv,mmnog,val1,val2,val3,val4," +
                        "   squ1,squ2,gil1,cnt1,kf307,kf307n,dat_s,dat_po, rashod, val_s , cnt5, cnt4 ) " +
                        " Select 29,3, " + p_dat_charge + ", 0,nzp_kvar,nzp_dom, 0, 0, nkvar," +
                        " val1,val2,val3,val4,squ1,squ2,gil1,cnt1,kf307,kf307n," + MDY(1, 1, 1900) + ", " + MDY(1, 1, 1900) +
                        ", rastot, rasind, kol_komn, is2075 " +
                        " From t_ans_kommunal Where 1=1 ";

                    if (rashod.paramcalc.nzp_kvar > 0 || rashod.paramcalc.nzp_dom > 0)
                    {
                        ret = ExecSQL(conn_db, sql, true);
                    }
                    else
                    {
                        ExecByStep(conn_db, "t_ans_kommunal", "nzp_no", sql, 100000, " ", out ret);
                    }
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }

                    UpdateStatistics(false, rashod.paramcalc, rashod.counters_tab, out ret);
                    if (!ret.result) { DropTempTablesRahod(conn_db, rashod.paramcalc.pref); return false; }
                }
                #endregion ��������� � 29 �����
            }

            return true;
        }
        #endregion �������� � ������ ��� ���������� �� ���� � ���������� ����� 29 - ���� ������ ���� ��� ��
    }

}

#endregion ����� ������������ ������� ��������
