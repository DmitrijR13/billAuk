using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace STCLINE.KP50.DataBase
{
    public class CreateTablesForLoader : DbAdminClient
    {
        /// <summary>
        /// Создание отсутствующих таблицы в БД, необходимых для загрузки
        /// </summary>
        /// <param name="conn_db"></param>
        /// <returns></returns>
        public Returns Run(IDbConnection conn_db)
        {
            Returns ret = Utils.InitReturns();

            try
            {
                #region 1 Проверка таблицы file_pack
#if PG
                string fields = "(id INTEGER, nzp_file INTEGER, dat_plat DATE, num_plat character(20), sum_plat NUMERIC(14,2), kol_plat INTEGER)";
#else
                string fields = "(id INTEGER, nzp_file INTEGER, dat_plat DATE, num_plat CHAR(20), sum_plat DECIMAL(14,2), kol_plat INTEGER)";
#endif
                ret = CreateOneTable(conn_db, "file_pack", "_upload", fields);
                #endregion Проверка таблицы file_pack

                #region 2 Проверка таблицы file_area
#if PG
                fields = "(id SERIAL NOT NULL, nzp_file INTEGER NOT NULL, area CHARACTER(40), jur_address CHARACTER(100), fact_address CHARACTER(100)," +
                                        "inn CHARACTER(12), kpp CHARACTER(9), rs CHARACTER(20), bank CHARACTER(100), bik CHARACTER(20), ks CHARACTER(20), nzp_area INTEGER)";
#else
                fields = "(id SERIAL NOT NULL, nzp_file INTEGER NOT NULL, area CHAR(40), jur_address CHAR(100), fact_address CHAR(100)," +
                                        "inn CHAR(12), kpp CHAR(9), rs CHAR(20), bank CHAR(100), bik CHAR(20), ks CHAR(20), nzp_area INTEGER)";
#endif
                ret = CreateOneTable(conn_db, "file_area", "_upload", fields);
                #endregion Проверка таблицы file_area

                #region 3 Проверка таблицы file_dom
#if PG
                fields = "(id NUMERIC(18,0), nzp_file INTEGER NOT NULL, ukds INTEGER, town VARCHAR(100), rajon VARCHAR(100), ulica VARCHAR(100), ndom CHAR(10), nkor CHARACTER(3)," +
                                      "area_id NUMERIC(18,0) NOT NULL, cat_blago CHARACTER(30), etazh INTEGER NOT NULL, build_year DATE, total_square NUMERIC(14,2) NOT NULL," +
                                      "mop_square NUMERIC(14,2), useful_square NUMERIC(14,2), mo_id NUMERIC(13,0), params CHARACTER(250), ls_row_number INTEGER NOT NULL," +
                                      "odpu_row_number INTEGER NOT NULL, nzp_ul INTEGER, nzp_dom INTEGER, comment VARCHAR(250), local_id CHAR(20), nzp_raj INTEGER, nzp_town INTEGER, " +
                                      " nzp_geu INTEGER, uch INTEGER, kod_kladr CHARACTER(30))";
#else
                fields = "(id DECIMAL(18,0), nzp_file INTEGER NOT NULL, ukds INTEGER, town CHAR(100), rajon CHAR(30), ulica CHAR(40), ndom CHAR(10), nkor CHAR(3)," +
                                      "area_id DECIMAL(18,0) NOT NULL, cat_blago CHAR(30), etazh INTEGER NOT NULL, build_year DATE, total_square DECIMAL(14,2) NOT NULL," +
                                      "mop_square DECIMAL(14,2), useful_square DECIMAL(14,2), mo_id DECIMAL(13,0), params CHAR(250), ls_row_number INTEGER NOT NULL," +
                                      "odpu_row_number INTEGER NOT NULL, nzp_ul INTEGER, nzp_dom INTEGER, comment CHAR(250), local_id CHAR(20), nzp_raj INTEGER, nzp_town INTEGER, " + 
                                      " nzp_geu INTEGER, uch INTEGER, kod_kladr CHAR(30))";
#endif
                ret = CreateOneTable(conn_db, "file_dom", "_upload", fields);
                #endregion Проверка таблицы file_dom

                #region 4 Проверка таблицы file_gaz
                fields = "(id SERIAL NOT NULL, id_prm INTEGER, name CHAR(100), nzp_file INTEGER, nzp_prm INTEGER)";
                ret = CreateOneTable(conn_db, "file_gaz", "_upload", fields);
                #endregion Проверка таблицы file_gaz

                #region 5 Проверка таблицы file_gilec
                fields = "(nzp_file INTEGER, num_ls INTEGER, nzp_gil INTEGER, nzp_kart INTEGER, nzp_tkrt INTEGER, fam CHAR(60), ima CHAR(60), otch CHAR(60), dat_rog DATE," +
                                        "fam_c CHAR(60), ima_c CHAR(60), otch_c CHAR(60), dat_rog_c DATE, gender NCHAR(1), nzp_dok INTEGER, serij NCHAR(10), nomer NCHAR(7), vid_dat DATE, vid_mes CHAR(70)," +
                                        "kod_podrazd CHAR(7), strana_mr CHAR(60), region_mr CHAR(30), okrug_mr CHAR(30), gorod_mr CHAR(30), npunkt_mr CHAR(30), rem_mr CHAR(180), strana_op CHAR(60), region_op CHAR(60)," +
                                        "okrug_op CHAR(60), gorod_op CHAR(60), npunkt_op CHAR(30), rem_op CHAR(180), strana_ku CHAR(30), region_ku CHAR(30), okrug_ku CHAR(30), gorod_ku CHAR(60), npunkt_ku CHAR(60)," +
                                        "rem_ku CHAR(180), rem_p CHAR(40), tprp NCHAR(1), dat_prop DATE, dat_oprp DATE, dat_pvu DATE, who_pvu CHAR(40), dat_svu DATE, namereg CHAR(80), kod_namereg CHAR(7)," +
                                        "rod CHAR(60), nzp_celp INTEGER, nzp_celu INTEGER, dat_sost DATE, dat_ofor DATE, comment CHAR(40), id SERIAL NOT NULL)";
                ret = CreateOneTable(conn_db, "file_gilec", "_upload", fields);
                #endregion Проверка таблицы file_gilec

                #region 6 Проверка таблицы file_head
                fields = "(id SERIAL NOT NULL, nzp_file INTEGER NOT NULL, org_name CHAR(40) NOT NULL, branch_name CHAR(40) NOT NULL, inn CHAR(12) NOT NULL, kpp CHAR(9) NOT NULL," +
                                        "file_no INTEGER NOT NULL, file_date DATE NOT NULL, sender_phone CHAR(20) NOT NULL, sender_fio CHAR(80) NOT NULL, calc_date DATE NOT NULL, row_number INTEGER NOT NULL)";
                ret = CreateOneTable(conn_db, "file_head", "_upload", fields);
                #endregion Проверка таблицы file_head

                #region 7 Проверка таблицы file_ipu
                fields = "(id SERIAL NOT NULL, nzp_file INTEGER NOT NULL, ls_id CHAR(20), nzp_serv INTEGER, rashod_type INTEGER, serv_type INTEGER, counter_type CHAR(25), cnt_stage INTEGER," +
                                        "mmnog INTEGER, num_cnt CHAR(20), dat_uchet DATE, val_cnt FLOAT, nzp_measure INTEGER, dat_prov DATE, dat_provnext DATE, nzp_kvar INTEGER, nzp_counter INTEGER, local_id CHAR(20)," +
                                        "kod_serv CHAR(20), doppar CHAR(25))";
                ret = CreateOneTable(conn_db, "file_ipu", "_upload", fields);
                #endregion Проверка таблицы file_ipu

                #region 8 Проверка таблицы file_ipu_p
                fields = "(id SERIAL NOT NULL, nzp_file INTEGER, id_ipu CHAR(20), rashod_type INTEGER, dat_uchet DATE, val_cnt FLOAT, kod_serv INTEGER)";
                ret = CreateOneTable(conn_db, "file_ipu_p", "_upload", fields);
                #endregion Проверка таблицы file_ipu_p

                #region 9 Проверка таблицы file_kvar
#if PG
                fields = "(id CHAR(20) NOT NULL, nzp_file INTEGER NOT NULL, ukas INTEGER," + " dom_id NUMERIC(18,0) NOT NULL, " +
                "ls_type INTEGER NOT NULL, fam VARCHAR(60), ima VARCHAR(60), otch VARCHAR(60), birth_date DATE," +
                "nkvar CHAR(10), nkvar_n CHAR(3), open_date DATE, opening_osnov CHAR(100), close_date DATE, closing_osnov CHAR(100), kol_gil INTEGER NOT NULL, kol_vrem_prib INTEGER NOT NULL," +
                "kol_vrem_ub INTEGER NOT NULL, room_number INTEGER NOT NULL, total_square NUMERIC(14,2) NOT NULL, living_square NUMERIC(14,2), otapl_square NUMERIC(14,2), naim_square NUMERIC(14,2)," +
                "is_communal INTEGER NOT NULL, is_el_plita INTEGER, is_gas_plita INTEGER, is_gas_colonka INTEGER, is_fire_plita INTEGER, gas_type INTEGER, water_type INTEGER, hotwater_type INTEGER," +
                "canalization_type INTEGER, is_open_otopl INTEGER, params CHAR(250), service_row_number INTEGER NOT NULL, reval_params_row_number INTEGER NOT NULL, ipu_row_number INTEGER NOT NULL," +
                "nzp_dom INTEGER, nzp_kvar INTEGER, comment CHAR(250), nzp_status INTEGER, id_urlic CHAR(20) )";
#else
                fields = "(id CHAR(20) NOT NULL, nzp_file INTEGER NOT NULL, ukas INTEGER," + " dom_id DECIMAL(18,0) NOT NULL, " +
 "ls_type INTEGER NOT NULL, fam CHAR(40), ima CHAR(40), otch CHAR(40), birth_date DATE," +
                                        "nkvar CHAR(10), nkvar_n CHAR(3), open_date DATE, opening_osnov CHAR(100), close_date DATE, closing_osnov CHAR(100), kol_gil INTEGER NOT NULL, kol_vrem_prib INTEGER NOT NULL," +
                                        "kol_vrem_ub INTEGER NOT NULL, room_number INTEGER NOT NULL, total_square DECIMAL(14,2) NOT NULL, living_square DECIMAL(14,2), otapl_square DECIMAL(14,2), naim_square DECIMAL(14,2)," +
                                        "is_communal INTEGER NOT NULL, is_el_plita INTEGER, is_gas_plita INTEGER, is_gas_colonka INTEGER, is_fire_plita INTEGER, gas_type INTEGER, water_type INTEGER, hotwater_type INTEGER," +
                                        "canalization_type INTEGER, is_open_otopl INTEGER, params CHAR(250), service_row_number INTEGER NOT NULL, reval_params_row_number INTEGER NOT NULL, ipu_row_number INTEGER NOT NULL," +
                                        "nzp_dom INTEGER, nzp_kvar INTEGER, comment CHAR(250), nzp_status INTEGER, id_urlic CHAR(20) )";
#endif
                ret = CreateOneTable(conn_db, "file_kvar", "_upload", fields);
                #endregion Проверка таблицы file_kvar

                #region 10 Проверка таблицы file_kvarp
#if PG
                fields = "(id CHAR(20) NOT NULL, reval_month DATE, nzp_file INTEGER NOT NULL, fam VARCHAR(60), ima VARCHAR(60), otch VARCHAR(60), birth_date DATE, nkvar CHAR(10), nkvar_n CHAR(3), open_date DATE," +
                        "opening_osnov CHAR(100), close_date DATE, closing_osnov CHAR(100), kol_gil INTEGER NOT NULL, kol_vrem_prib INTEGER NOT NULL, kol_vrem_ub INTEGER NOT NULL, room_number INTEGER NOT NULL," +
                        "total_square NUMERIC(14,2) NOT NULL, living_square NUMERIC(14,2), otapl_square NUMERIC(14,2), naim_square NUMERIC(14,2), is_communal INTEGER NOT NULL, is_el_plita INTEGER, is_gas_plita INTEGER," +
                        "is_gas_colonka INTEGER, is_fire_plita INTEGER, gas_type INTEGER, water_type INTEGER, hotwater_type INTEGER, canalization_type INTEGER, is_open_otopl INTEGER, params CHAR(250), nzp_dom INTEGER," +
                        "nzp_kvar INTEGER, comment CHAR(250), nzp_status INTEGER, local_id CHAR(20))";
#else
                fields = "(id CHAR(20) NOT NULL, reval_month DATE, nzp_file INTEGER NOT NULL, fam CHAR(40), ima CHAR(40), otch CHAR(40), birth_date DATE, nkvar CHAR(10), nkvar_n CHAR(3), open_date DATE," +
                                        "opening_osnov CHAR(100), close_date DATE, closing_osnov CHAR(100), kol_gil INTEGER NOT NULL, kol_vrem_prib INTEGER NOT NULL, kol_vrem_ub INTEGER NOT NULL, room_number INTEGER NOT NULL," +
                                        "total_square DECIMAL(14,2) NOT NULL, living_square DECIMAL(14,2), otapl_square DECIMAL(14,2), naim_square DECIMAL(14,2), is_communal INTEGER NOT NULL, is_el_plita INTEGER, is_gas_plita INTEGER," +
                                        "is_gas_colonka INTEGER, is_fire_plita INTEGER, gas_type INTEGER, water_type INTEGER, hotwater_type INTEGER, canalization_type INTEGER, is_open_otopl INTEGER, params CHAR(250), nzp_dom INTEGER," +
                                        "nzp_kvar INTEGER, comment CHAR(250), nzp_status INTEGER, local_id CHAR(20))";
#endif
                ret = CreateOneTable(conn_db, "file_kvarp", "_upload", fields);
                #endregion Проверка таблицы file_kvarp

                #region 11 Проверка таблицы file_measures
                fields = "(id SERIAL NOT NULL, id_measure INTEGER, measure CHAR(100), nzp_file INTEGER, nzp_measure INTEGER)";
                ret = CreateOneTable(conn_db, "file_measures", "_upload", fields);
                #endregion Проверка таблицы file_measures

                #region 12 Проверка таблицы file_mo
#if PG
                fields = "(id SERIAL NOT NULL, id_mo INTEGER, vill CHARACTER(50), nzp_vill NUMERIC(13,0), nzp_raj INTEGER, nzp_file INTEGER, raj CHARACTER(60), mo_name CHARACTER(60))";
#else
                fields = "(id SERIAL NOT NULL, id_mo INTEGER, vill CHAR(50), nzp_vill DECIMAL(13,0), nzp_raj INTEGER, nzp_file INTEGER, raj CHAR(60), mo_name CHAR(60))";
#endif
                ret = CreateOneTable(conn_db, "file_mo", "_upload", fields);
                #endregion Проверка таблицы file_mo

                #region 13 Проверка таблицы file_nedopost
#if PG
                fields = "(id SERIAL NOT NULL, nzp_file INTEGER, ls_id CHARACTER(20), id_serv CHAR(20), type_ned NUMERIC(10,0), temper INTEGER, dat_nedstart DATE, dat_nedstop DATE, sum_ned NUMERIC(10,2))";
#else
                fields = "(id SERIAL NOT NULL, nzp_file INTEGER, ls_id CHAR(20), id_serv CHAR(20), type_ned DECIMAL(10,0), temper INTEGER, dat_nedstart DATE, dat_nedstop DATE, sum_ned DECIMAL(10,2))";
#endif
                ret = CreateOneTable(conn_db, "file_nedopost", "_upload", fields);
                #endregion Проверка таблицы file_nedopost

                #region 14 Проверка таблицы file_odpu
#if PG
                fields = "(id SERIAL NOT NULL, nzp_file INTEGER NOT NULL, dom_id NUMERIC(18,0), nzp_serv INTEGER, rashod_type INTEGER, serv_type INTEGER, counter_type CHAR(25), cnt_stage INTEGER," +
                        "mmnog INTEGER, num_cnt CHAR(20), dat_uchet DATE, val_cnt FLOAT, nzp_measure INTEGER, dat_prov DATE, dat_provnext DATE, nzp_dom INTEGER, nzp_counter INTEGER, local_id CHAR(20), doppar CHAR(25))";
#else
                fields = "(id SERIAL NOT NULL, nzp_file INTEGER NOT NULL, dom_id DECIMAL(18,0), nzp_serv INTEGER, rashod_type INTEGER, serv_type INTEGER, counter_type CHAR(25), cnt_stage INTEGER," +
                                        "mmnog INTEGER, num_cnt CHAR(20), dat_uchet DATE, val_cnt FLOAT, nzp_measure INTEGER, dat_prov DATE, dat_provnext DATE, nzp_dom INTEGER, nzp_counter INTEGER, local_id CHAR(20), doppar CHAR(25))";
#endif
                ret = CreateOneTable(conn_db, "file_odpu", "_upload", fields);
                #endregion Проверка таблицы file_odpu

                #region 15 Проверка таблицы file_odpu_p
#if PG
                fields = "(id SERIAL NOT NULL, nzp_file INTEGER, id_odpu CHAR(20), rashod_type INTEGER, dat_uchet DATE, val_cnt FLOAT, id_ipu INTEGER, kod_serv NUMERIC(10,0))";
#else
                fields = "(id SERIAL NOT NULL, nzp_file INTEGER, id_odpu CHAR(20), rashod_type INTEGER, dat_uchet DATE, val_cnt FLOAT, id_ipu INTEGER, kod_serv DECIMAL(10,0))";
#endif
                ret = CreateOneTable(conn_db, "file_odpu_p", "_upload", fields);
                #endregion Проверка таблицы file_odpu_p

                #region 16 Проверка таблицы file_oplats
#if PG
                fields = "(id SERIAL NOT NULL, ls_id CHAR(20), type_oper INTEGER, numplat CHAR(80), dat_opl DATE, dat_uchet DATE, dat_izm DATE, " +
                    "sum_oplat NUMERIC(14,2), ist_opl CHAR(80), mes_oplat DATE, nzp_file INTEGER, nzp_pack INTEGER, id_serv INTEGER)";
#else
                fields = "(id SERIAL NOT NULL, ls_id CHAR(20), type_oper INTEGER, numplat CHAR(80), dat_opl DATE, dat_uchet DATE, dat_izm DATE, " +
                                    "sum_oplat DECIMAL(14,2), ist_opl CHAR(80), mes_oplat DATE, nzp_file INTEGER, nzp_pack INTEGER, id_serv INTEGER)";
#endif
                ret = CreateOneTable(conn_db, "file_oplats", "_upload", fields);
                #endregion Проверка таблицы file_oplats

                #region 17 Проверка таблицы file_paramsdom
#if PG
                fields = "(id SERIAL NOT NULL, id_dom CHAR(20), id_prm INTEGER, val_prm CHAR(100), nzp_dom INTEGER, nzp_file INTEGER)";
#else
                fields = "(id SERIAL NOT NULL, id_dom CHAR(20), id_prm INTEGER, val_prm CHAR(100), nzp_dom INTEGER, nzp_file INTEGER)";
#endif
                ret = CreateOneTable(conn_db, "file_paramsdom", "_upload", fields);
                #endregion Проверка таблицы file_paramsdom

                #region 18 Проверка таблицы file_paramsls
#if PG
                fields = "(id SERIAL NOT NULL, ls_id CHAR(20), id_prm INTEGER, val_prm CHAR(100), num_ls INTEGER, nzp_file INTEGER)";
#else
                fields = "(id SERIAL NOT NULL, ls_id CHAR(20), id_prm INTEGER, val_prm CHAR(100), num_ls INTEGER, nzp_file INTEGER)";
#endif
                ret = CreateOneTable(conn_db, "file_paramsls", "_upload", fields);
                #endregion Проверка таблицы file_paramsls

                #region 19 Проверка таблицы file_pasp
#if PG
                fields = "(nzp_gil SERIAL NOT NULL, fam CHARACTER(20) NOT NULL, ima CHARACTER(20) NOT NULL, otch CHARACTER(20), dat_rog DATE NOT NULL, gender CHARACTER(1), prop CHAR(30), pr_lista CHAR(2)," +
                        "country CHARACTER(5), region CHARACTER(5), selo CHARACTER(30), ulica CHARACTER(40), ndom CHARACTER(10), nkor CHAR(3), nkvar CHARACTER(10), nkvar_n CHARACTER(3), doctype CHARACTER(30), serij CHARACTER(10), nomer CHARACTER(7)," +
                        "vid_dat DATE, rog_country CHARACTER(5), rog_region CHARACTER(30), rog_selo CHARACTER(30), dat_sost DATE, dat_ofor DATE)";
#else
                fields = "(nzp_gil SERIAL NOT NULL, fam NCHAR(20) NOT NULL, ima NCHAR(20) NOT NULL, otch NCHAR(20), dat_rog DATE NOT NULL, gender NCHAR(1), prop CHAR(30), pr_lista CHAR(2)," +
                                        "country CHAR(5), region CHAR(5), selo CHAR(30), ulica CHAR(40), ndom CHAR(10), nkor CHAR(3), nkvar CHAR(10), nkvar_n CHAR(3), doctype CHAR(30), serij CHAR(10), nomer CHAR(7)," +
                                        "vid_dat DATE, rog_country CHAR(5), rog_region CHAR(30), rog_selo CHAR(30), dat_sost DATE, dat_ofor DATE)";
#endif
                ret = CreateOneTable(conn_db, "file_pasp", "_upload", fields);
                #endregion Проверка таблицы file_pasp

                #region 20 Проверка таблицы file_serv
#if PG
                fields = "(id SERIAL NOT NULL, nzp_file INTEGER NOT NULL, ls_id CHAR(20) NOT NULL, supp_id NUMERIC(18,0) NOT NULL, nzp_serv INTEGER NOT NULL, sum_insaldo NUMERIC(14,2) NOT NULL," +
                        "eot NUMERIC(14,3) NOT NULL, reg_tarif_percent NUMERIC(14,3) NOT NULL, reg_tarif NUMERIC(14,3) NOT NULL, nzp_measure INTEGER NOT NULL, fact_rashod NUMERIC(18,7) NOT NULL," +
                        "norm_rashod NUMERIC(18,7) NOT NULL, is_pu_calc INTEGER NOT NULL, sum_nach NUMERIC(14,2) NOT NULL, sum_reval NUMERIC(14,2) NOT NULL, sum_subsidy NUMERIC(14,2) NOT NULL," +
                        "sum_subsidyp NUMERIC(14,2) NOT NULL, sum_lgota NUMERIC(14,2) NOT NULL, sum_lgotap NUMERIC(14,2) NOT NULL, sum_smo NUMERIC(14,2) NOT NULL, sum_smop NUMERIC(14,2) NOT NULL," +
                        "sum_money NUMERIC(14,2) NOT NULL, is_del INTEGER NOT NULL, sum_outsaldo NUMERIC(14,2) NOT NULL, servp_row_number INTEGER NOT NULL, nzp_kvar INTEGER, nzp_supp INTEGER)";
#else
                fields = "(id SERIAL NOT NULL, nzp_file INTEGER NOT NULL, ls_id CHAR(20) NOT NULL, supp_id DECIMAL(18,0) NOT NULL, nzp_serv INTEGER NOT NULL, sum_insaldo DECIMAL(14,2) NOT NULL," +
                                        "eot DECIMAL(14,3) NOT NULL, reg_tarif_percent DECIMAL(14,3) NOT NULL, reg_tarif DECIMAL(14,3) NOT NULL, nzp_measure INTEGER NOT NULL, fact_rashod DECIMAL(18,7) NOT NULL," +
                                        "norm_rashod DECIMAL(18,7) NOT NULL, is_pu_calc INTEGER NOT NULL, sum_nach DECIMAL(14,2) NOT NULL, sum_reval DECIMAL(14,2) NOT NULL, sum_subsidy DECIMAL(14,2) NOT NULL," +
                                        "sum_subsidyp DECIMAL(14,2) NOT NULL, sum_lgota DECIMAL(14,2) NOT NULL, sum_lgotap DECIMAL(14,2) NOT NULL, sum_smo DECIMAL(14,2) NOT NULL, sum_smop DECIMAL(14,2) NOT NULL," +
                                        "sum_money DECIMAL(14,2) NOT NULL, is_del INTEGER NOT NULL, sum_outsaldo DECIMAL(14,2) NOT NULL, servp_row_number INTEGER NOT NULL, nzp_kvar INTEGER, nzp_supp INTEGER)";
#endif
                ret = CreateOneTable(conn_db, "file_serv", "_upload", fields);
                #endregion Проверка таблицы file_serv

                #region 21 Проверка таблицы file_services
#if PG
                fields = "(id SERIAL NOT NULL, id_serv INTEGER, service CHAR(100), service2 CHAR(100), nzp_file INTEGER, nzp_measure INTEGER, ed_izmer CHAR(30), type_serv INTEGER, nzp_serv INTEGER)";
#else
                fields = "(id SERIAL NOT NULL, id_serv INTEGER, service CHAR(100), service2 CHAR(100), nzp_file INTEGER, nzp_measure INTEGER, ed_izmer CHAR(30), type_serv INTEGER, nzp_serv INTEGER)";
#endif
                ret = CreateOneTable(conn_db, "file_services", "_upload", fields);
                #endregion Проверка таблицы file_services

                #region 22 Проверка таблицы file_servls
#if PG
                fields = "(id SERIAL NOT NULL, nzp_file INTEGER, ls_id NUMERIC(14,0), id_serv CHAR(100), dat_start DATE, dat_stop DATE, supp_id NUMERIC(14,0))";
#else
                fields = "(id SERIAL NOT NULL, nzp_file INTEGER, ls_id DECIMAL(14,0), id_serv CHAR(100), dat_start DATE, dat_stop DATE, supp_id DECIMAL(14,0))";
#endif
                ret = CreateOneTable(conn_db, "file_servls", "_upload", fields);
                #endregion Проверка таблицы file_servls

                #region 23 Проверка таблицы file_servp
#if PG
                fields = "(id SERIAL NOT NULL, nzp_file INTEGER NOT NULL, reval_month DATE, ls_id CHAR(20) NOT NULL, supp_id NUMERIC(18,0) NOT NULL, nzp_serv INTEGER NOT NULL, eot NUMERIC(14,3) NOT NULL," +
                        "reg_tarif_percent NUMERIC(14,3) NOT NULL, reg_tarif NUMERIC(14,3) NOT NULL, nzp_measure INTEGER NOT NULL, fact_rashod NUMERIC(18,7) NOT NULL, norm_rashod NUMERIC(18,7) NOT NULL," +
                        "is_pu_calc INTEGER NOT NULL, sum_reval NUMERIC(14,2) NOT NULL, sum_subsidyp NUMERIC(14,2) NOT NULL, sum_lgotap NUMERIC(14,2) NOT NULL, sum_smop NUMERIC(14,2) NOT NULL," +
                        "nzp_kvar INTEGER, nzp_supp INTEGER)";
#else
                fields = "(id SERIAL NOT NULL, nzp_file INTEGER NOT NULL, reval_month DATE, ls_id CHAR(20) NOT NULL, supp_id DECIMAL(18,0) NOT NULL, nzp_serv INTEGER NOT NULL, eot DECIMAL(14,3) NOT NULL," +
                                        "reg_tarif_percent DECIMAL(14,3) NOT NULL, reg_tarif DECIMAL(14,3) NOT NULL, nzp_measure INTEGER NOT NULL, fact_rashod DECIMAL(18,7) NOT NULL, norm_rashod DECIMAL(18,7) NOT NULL," +
                                        "is_pu_calc INTEGER NOT NULL, sum_reval DECIMAL(14,2) NOT NULL, sum_subsidyp DECIMAL(14,2) NOT NULL, sum_lgotap DECIMAL(14,2) NOT NULL, sum_smop DECIMAL(14,2) NOT NULL," +
                                        "nzp_kvar INTEGER, nzp_supp INTEGER)";
#endif
                ret = CreateOneTable(conn_db, "file_servp", "_upload", fields);
                #endregion Проверка таблицы file_servp

                #region 24 Проверка таблицы file_supp
#if PG
                fields = "(id SERIAL NOT NULL, nzp_file INTEGER NOT NULL, supp_id NUMERIC(18,0) NOT NULL, supp_name CHAR(25) NOT NULL, jur_address CHAR(100), fact_address CHAR(100), inn CHAR(12), kpp CHAR(9)," +
                        "rs CHAR(20), bank CHAR(100), bik CHAR(20), ks CHAR(20), nzp_supp INTEGER)";
#else
                fields = "(id SERIAL NOT NULL, nzp_file INTEGER NOT NULL, supp_id DECIMAL(18,0) NOT NULL, supp_name CHAR(25) NOT NULL, jur_address CHAR(100), fact_address CHAR(100), inn CHAR(12), kpp CHAR(9)," +
                                        "rs CHAR(20), bank CHAR(100), bik CHAR(20), ks CHAR(20), nzp_supp INTEGER)";
#endif
                ret = CreateOneTable(conn_db, "file_supp", "_upload", fields);
                #endregion Проверка таблицы  file_supp

                #region 25 Проверка таблицы file_typenedopost
#if PG
                fields = "(id SERIAL NOT NULL, nzp_file INTEGER, type_ned NUMERIC(10,0), ned_name CHAR(100))";
#else
                fields = "(id SERIAL NOT NULL, nzp_file INTEGER, type_ned DECIMAL(10,0), ned_name CHAR(100))";
#endif
                ret = CreateOneTable(conn_db, "file_typenedopost", "_upload", fields);
                #endregion Проверка таблицы file_typenedopost

                #region 26 Проверка таблицы file_typeparams
#if PG
                fields = "(id SERIAL NOT NULL, id_prm INTEGER, prm_name CHAR(100), level_ INTEGER, type_prm INTEGER, nzp_file INTEGER, nzp_prm INTEGER)";
#else
                fields = "(id SERIAL NOT NULL, id_prm INTEGER, prm_name CHAR(100), level_ INTEGER, type_prm INTEGER, nzp_file INTEGER, nzp_prm INTEGER)";
#endif
                ret = CreateOneTable(conn_db, "file_typeparams", "_upload", fields);
                #endregion Проверка таблицы  file_typeparams

                #region 27 Проверка таблицы file_urlic
#if PG
                fields = "(id SERIAL NOT NULL, nzp_file INTEGER NOT NULL,  supp_name CHAR(100) NOT NULL, jur_address CHAR(100), fact_address CHAR(100)," +
                     " inn CHAR(12), kpp CHAR(9), rs CHAR(20), bank CHAR(100), bik_bank CHAR(20), ks CHAR(20), tel_chief CHAR(20), tel_b CHAR(20), chief_name CHAR(100)," +
                     " chief_post CHAR(40), b_name CHAR(100), okonh1 CHAR(20), okonh2 CHAR(20), okpo CHAR(20), bank_pr CHAR(100), bank_adr CHAR(100), bik CHAR(20)," +
                     " rs_pr CHAR(20), ks_pr CHAR(20), post_and_name CHAR(200), nzp_supp INTEGER)";
#else
                fields = "(id SERIAL NOT NULL, nzp_file INTEGER NOT NULL,  supp_name CHAR(100) NOT NULL, jur_address CHAR(100), fact_address CHAR(100)," +
                                    " inn CHAR(12), kpp CHAR(9), rs CHAR(20), bank CHAR(100), bik_bank CHAR(20), ks CHAR(20), tel_chief CHAR(20), tel_b CHAR(20), chief_name CHAR(100)," +
                                    " chief_post CHAR(40), b_name CHAR(100), okonh1 CHAR(20), okonh2 CHAR(20), okpo CHAR(20), bank_pr CHAR(100), bank_adr CHAR(100), bik CHAR(20)," +
                                    " rs_pr CHAR(20), ks_pr CHAR(20), post_and_name CHAR(200), nzp_supp INTEGER)";
#endif
                ret = CreateOneTable(conn_db, "file_urlic", "_upload", fields);
                #endregion Проверка таблицы  file_urlic

                #region 28 Проверка таблицы file_voda
#if PG
                fields = "(id SERIAL NOT NULL, id_prm INTEGER, name CHAR(100), nzp_file INTEGER, nzp_prm INTEGER)";
#else
                fields = "(id SERIAL NOT NULL, id_prm INTEGER, name CHAR(100), nzp_file INTEGER, nzp_prm INTEGER)";
#endif
                ret = CreateOneTable(conn_db, "file_voda", "_upload", fields);
                #endregion Проверка таблицы  file_voda

                #region 29 Проверка таблицы file_vrub
#if PG
                fields = "(id SERIAL NOT NULL, nzp_file INTEGER, ls_id CHAR(20),  gil_id INTEGER , dat_vrvib DATE, dat_end DATE)";
#else
                fields = "(id SERIAL NOT NULL, nzp_file INTEGER, ls_id CHAR(20),  gil_id INTEGER , dat_vrvib DATE, dat_end DATE)";
#endif
                ret = CreateOneTable(conn_db, "file_vrub", "_upload", fields);
                #endregion Проверка таблицы file_vrub

                #region 30 Проверка таблицы files_imported
#if PG
                fields = "(nzp_file SERIAL NOT NULL, nzp_version INTEGER NOT NULL, loaded_name CHAR(90), saved_name CHAR(90), nzp_status INTEGER NOT NULL, created_by INTEGER NOT NULL," +
                        "created_on timestamp without time zone NOT NULL NOT NULL, file_type INTEGER, nzp_exc INTEGER, nzp_exc_log INTEGER)";
#else
                fields = "(nzp_file SERIAL NOT NULL, nzp_version INTEGER NOT NULL, loaded_name CHAR(90), saved_name CHAR(90), nzp_status INTEGER NOT NULL, created_by INTEGER NOT NULL," +
                                        "created_on DATETIME YEAR to SECOND NOT NULL, file_type INTEGER, nzp_exc INTEGER, nzp_exc_log INTEGER)";
#endif
                ret = CreateOneTable(conn_db, "files_imported", "_upload", fields);
                #endregion Проверка таблицы  files_imported

                #region 31 Проверка таблицы file_blag
#if PG
                fields = "(id SERIAL NOT NULL, id_prm INTEGER, name CHAR(100), nzp_file INTEGER, nzp_prm INTEGER)";
#else
                fields = "(id SERIAL NOT NULL, id_prm INTEGER, name CHAR(100), nzp_file INTEGER, nzp_prm INTEGER)";
#endif
                ret = CreateOneTable(conn_db, "file_blag", "_upload", fields);
                #endregion Проверка таблицы  file_blag

                #region 32 Проверка таблицы file_uchs
                fields = "( uch INTEGER, geu CHAR(50), iddom CHAR(15), nzp_dom INTEGER, nzp_geu INTEGER)";
                ret = CreateOneTable(conn_db, "file_uchs", "_upload", fields);
                #endregion Проверка таблицы  file_uchs

                #region 33 Проверка таблицы file_ulica
#if PG
                fields = "(file_ulica_id VARCHAR(100), file_ulica_street VARCHAR(100), nzp_ul INTEGER, f_ulica VARCHAR(100))";
#else
                fields = "(file_ulica_id VARCHAR(30), file_ulica_street VARCHAR(30), nzp_ul INTEGER, f_ulica CHAR(60))";
#endif
                ret = CreateOneTable(conn_db, "file_ulica", "_upload", fields);
                #endregion Проверка таблицы  file_ulica

                #region 34 Проверка таблицы file_section
#if PG
                fields = "(id SERIAL NOT NULL, num_sec INTEGER, sec_name CHAR(100), nzp_file INTEGER, is_need_load INTEGER default 1)";
#else
                fields = "(id SERIAL NOT NULL, num_sec INTEGER, sec_name CHAR(100), nzp_file INTEGER, is_need_load INTEGER default 1)";
#endif
                ret = CreateOneTable(conn_db, "file_section ", "_upload", fields);
                #endregion Проверка таблицы  file_section

                #region 35 Проверка таблицы file_serv_tuning
#if PG
                fields = " (   id SERIAL NOT NULL,   nzp_serv INTEGER,   nzp_supp INTEGER,   nzp_measure INTEGER,   nzp_frm INTEGER) ";
#else
                fields = " (   id SERIAL NOT NULL,   nzp_serv INTEGER,   nzp_supp INTEGER,   nzp_measure INTEGER,   nzp_frm INTEGER) ";
#endif
                ret = CreateOneTable(conn_db, "file_serv_tuning ", "_upload", fields);
                #endregion 35 Проверка таблицы file_serv_tuning

                #region 36 Проверка таблицы file_sql
#if PG
                fields = " (id integer NOT NULL, nzp_file integer, sql_zapr CHAR(2000)) ";
#else
                fields = " (id integer NOT NULL, nzp_file integer, sql_zapr CHAR(2000)) ";
#endif
                ret = CreateOneTable(conn_db, "file_sql ", "_upload", fields);
                #endregion 36 Проверка таблицы file_sql

                #region 36 Проверка таблицы upload_progress
#if PG
                fields = " (id SERIAL NOT NULL, date_upload timestamp without time zone, progress NUMERIC(14, 2), upload_type INTEGER) ";
#else
                fields = " (id SERIAL NOT NULL, date_upload DATETIME YEAR to SECOND, progress DECIMAL(14,2), upload_type INTEGER) ";
#endif
                ret = CreateOneTable(conn_db, "upload_progress", "_upload", fields);
                #endregion 36 Проверка таблицы upload_progress

                #region 37 Проверка таблицы file_del_unrel_info
#if PG
                fields = " (id SERIAL NOT NULL, nzp_file INTEGER NOT NULL, is_success INTEGER NOT NULL) ";
#else
                fields = " (id SERIAL NOT NULL, nzp_file INTEGER NOT NULL, is_success INTEGER NOT NULL) ";
#endif
                ret = CreateOneTable(conn_db, "file_del_unrel_info", "_upload", fields);
                #endregion 37 Проверка таблицы file_del_unrel_info

            }
            catch
            {
                ret.result = false;
            }
            return ret;
        }

        /// <summary>
        /// ПРоверка наличия и создание одной таблички
        /// </summary>
        /// <param name="conn_db">подключение к БД</param>
        /// <param name="table_name">название таблицы</param>
        /// <param name="dbname_withou_pref">название БД без префикса</param>
        /// <param name="fields">поля создаваемой таблички в круглых скобках как в SQL-запросе</param>
        /// <returns></returns>
        public Returns CreateOneTable(IDbConnection conn_db, string tablename, string dbname_withoupref, string fields)
        {
            Returns ret = Utils.InitReturns();
            string table_name = tablename.Trim();
            string dbname = dbname_withoupref.Trim();

            try
            {
                #region Проверка таблицы
#if PG
                string sql =
                    "select table_name as tabname " +
                    " from information_schema.tables " +
                    " where table_schema ='" + Points.Pref + dbname + "' and table_name ='" + table_name + "'";
#else
            string sql = 
            " select * from " + Points.Pref + dbname + tableDelimiter + "systables a "+
            " where a.tabname = '" + table_name + "' and a.tabid > 99";
#endif
                var dt = ClassDBUtils.OpenSQL(sql, conn_db);
                if (dt.resultData.Rows.Count == 0)
                {
#if PG
                    sql = " SET search_path TO  '" + Points.Pref + dbname + "'";
#else
                    sql = "DATABASE " + Points.Pref + dbname + "; ";
#endif
                    ret = ExecSQL(conn_db, sql, true);
                    if (ret.result)
                    {
                        sql = "CREATE TABLE " + table_name + fields;
                        ret = ExecSQL(conn_db, sql, true);
                    }
                }
                #endregion Проверка таблицы
            }
            catch (Exception ex)
            {
                MonitorLog.WriteLog("Ошибка создания таблицы " + table_name + " в функцие CreateOneTable : " + ex.Message + ex.StackTrace, MonitorLog.typelog.Error, true);
                ret.result = false;
                return ret;
            }

            return ret;
        }

    }
}
