using System;
using System.Data;
using STCLINE.KP50.DataBase;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;

namespace Bars.KP50.DataImport.SOURCE.LOADER
{
    [Obsolete("Проблема решена через партиционирование. Задумку похоронили")]
    public class DBFileInArchive : DbAdminClient
    {
//        private readonly IDbConnection con_db;

//        public DBFileInArchive(IDbConnection conDb)
//        {
//            con_db = conDb;
//        }

//        public Returns PutFileInArchive(FilesImported finder)
//        {
//            Returns ret = STCLINE.KP50.Global.Utils.InitReturns();


//            try
//            {
//                var t = new CreateTablesForLoader();
//                string sql;
//                string fields;
                
//                #region Создание  табличек в случае их отсутствия

//                #region archived_files
//                fields = "(nzp_file SERIAL NOT NULL," +
//                         " nzp_version INTEGER NOT NULL," +
//                         " loaded_name CHAR(90), " +
//                         " saved_name CHAR(90)," +
//                         " nzp_status INTEGER NOT NULL," +
//                         " created_by INTEGER NOT NULL," +
//                         " created_on DATE NOT NULL," +
//                         " file_type INTEGER," +
//                         " nzp_exc INTEGER," +
//                         " nzp_exc_log INTEGER," +
//                         " percent " + sDecimalType + "(3,2)," +
//                         " pref CHAR(20)," +
//                         " diss_status CHAR(50)," +
//                         " date_arch DATE," +
//                         " user_arch INTEGER," +
//                         " where_arch CHAR(40))";

//                ret = t.CreateOneTable(con_db, "archived_files", "_data", fields);
//                #endregion archived_files

//                #region 1 Проверка таблицы arch_file_pack

//                fields = "(id INTEGER, nzp_file INTEGER,dat_plat DATE, num_plat CHAR(20), sum_plat " + sDecimalType + "(14,2),kol_plat INTEGER)";

//                ret = t.CreateOneTable(con_db, "arch_file_pack", "_data", fields);
//                #endregion Проверка таблицы arch_file_pack

//                #region 2 Проверка таблицы arch_file_area
//#if PG
//                fields = "(id SERIAL NOT NULL, nzp_file INTEGER NOT NULL, area CHARACTER(40), jur_address CHARACTER(100), fact_address CHARACTER(100)," +
//                                        "inn CHARACTER(12), kpp CHARACTER(9), rs CHARACTER(20), bank CHARACTER(100), bik CHARACTER(20), ks CHARACTER(20), nzp_area INTEGER)";
//#else
//                fields = "(id SERIAL NOT NULL, nzp_file INTEGER NOT NULL, area CHAR(40), jur_address CHAR(100), fact_address CHAR(100)," +
//                                        "inn CHAR(12), kpp CHAR(9), rs CHAR(20), bank CHAR(100), bik CHAR(20), ks CHAR(20), nzp_area INTEGER)";
//#endif
//                ret = t.CreateOneTable(con_db, "arch_file_area", "_data", fields);
//                #endregion Проверка таблицы arch_file_area

//                #region 3 Проверка таблицы arch_file_dom
//#if PG
//                fields = "(id NUMERIC(18,0), nzp_file INTEGER NOT NULL, ukds INTEGER, town VARCHAR(100), rajon VARCHAR(100), ulica VARCHAR(100), ndom CHAR(10), nkor CHARACTER(3)," +
//                                      "area_id NUMERIC(18,0) NOT NULL, cat_blago CHARACTER(30), etazh INTEGER NOT NULL, build_year DATE, total_square NUMERIC(14,2) NOT NULL," +
//                                      "mop_square NUMERIC(14,2), useful_square NUMERIC(14,2), mo_id NUMERIC(13,0), params CHARACTER(250), ls_row_number INTEGER NOT NULL," +
//                                      "odpu_row_number INTEGER NOT NULL, nzp_ul INTEGER, nzp_dom INTEGER, comment VARCHAR(250), local_id CHAR(20), nzp_raj INTEGER, nzp_town INTEGER, " +
//                                      " nzp_geu INTEGER, uch INTEGER, kod_kladr CHARACTER(30))";
//#else
//                fields = "(id DECIMAL(18,0), nzp_file INTEGER NOT NULL, ukds INTEGER, town CHAR(100), rajon CHAR(30), ulica CHAR(40), ndom CHAR(10), nkor CHAR(3)," +
//                                      "area_id DECIMAL(18,0) NOT NULL, cat_blago CHAR(30), etazh INTEGER NOT NULL, build_year DATE, total_square DECIMAL(14,2) NOT NULL," +
//                                      "mop_square DECIMAL(14,2), useful_square DECIMAL(14,2), mo_id DECIMAL(13,0), params CHAR(250), ls_row_number INTEGER NOT NULL," +
//                                      "odpu_row_number INTEGER NOT NULL, nzp_ul INTEGER, nzp_dom INTEGER, comment CHAR(250), local_id CHAR(20), nzp_raj INTEGER, nzp_town INTEGER, " +
//                                      " nzp_geu INTEGER, uch INTEGER, kod_kladr CHAR(30))";
//#endif
//                ret = t.CreateOneTable(con_db, "arch_file_dom", "_data", fields);
//                #endregion Проверка таблицы arch_file_dom

//                #region 4 Проверка таблицы arch_file_gaz
//                fields = "(id SERIAL NOT NULL, id_prm INTEGER, name CHAR(100), nzp_file INTEGER, nzp_prm INTEGER)";
//                ret = t.CreateOneTable(con_db, "arch_file_gaz", "_data", fields);
//                #endregion Проверка таблицы arch_file_gaz

//                #region 5 Проверка таблицы arch_file_gilec
//                fields = "(nzp_file INTEGER, num_ls INTEGER, nzp_gil INTEGER, nzp_kart INTEGER, nzp_tkrt INTEGER, fam CHAR(60), ima CHAR(60), otch CHAR(60), dat_rog DATE," +
//                                        "fam_c CHAR(60), ima_c CHAR(60), otch_c CHAR(60), dat_rog_c DATE, gender NCHAR(1), nzp_dok INTEGER, serij NCHAR(10), nomer NCHAR(7), vid_dat DATE, vid_mes CHAR(70)," +
//                                        "kod_podrazd CHAR(7), strana_mr CHAR(60), region_mr CHAR(30), okrug_mr CHAR(30), gorod_mr CHAR(30), npunkt_mr CHAR(30), rem_mr CHAR(180), strana_op CHAR(60), region_op CHAR(60)," +
//                                        "okrug_op CHAR(60), gorod_op CHAR(60), npunkt_op CHAR(30), rem_op CHAR(180), strana_ku CHAR(30), region_ku CHAR(30), okrug_ku CHAR(30), gorod_ku CHAR(60), npunkt_ku CHAR(60)," +
//                                        "rem_ku CHAR(180), rem_p CHAR(40), tprp NCHAR(1), dat_prop DATE, dat_oprp DATE, dat_pvu DATE, who_pvu CHAR(40), dat_svu DATE, namereg CHAR(80), kod_namereg CHAR(7)," +
//                                        "rod CHAR(60), nzp_celp INTEGER, nzp_celu INTEGER, dat_sost DATE, dat_ofor DATE, comment CHAR(40), id SERIAL NOT NULL)";
//                ret = t.CreateOneTable(con_db, "arch_file_gilec", "_data", fields);
//                #endregion Проверка таблицы arch_file_gilec

//                #region 6 Проверка таблицы arch_file_head
//                fields = "(id SERIAL NOT NULL, nzp_file INTEGER NOT NULL, org_name CHAR(40) NOT NULL, branch_name CHAR(40) NOT NULL, inn CHAR(12) NOT NULL, kpp CHAR(9) NOT NULL," +
//                                        "file_no INTEGER NOT NULL, file_date DATE NOT NULL, sender_phone CHAR(20) NOT NULL, sender_fio CHAR(80) NOT NULL, calc_date DATE NOT NULL, row_number INTEGER NOT NULL)";
//                ret = t.CreateOneTable(con_db, "arch_file_head", "_data", fields);
//                #endregion Проверка таблицы arch_file_head

//                #region 7 Проверка таблицы arch_file_ipu
//                fields = "(id SERIAL NOT NULL, nzp_file INTEGER NOT NULL, ls_id CHAR(20), nzp_serv INTEGER, rashod_type INTEGER, serv_type INTEGER, counter_type CHAR(25), cnt_stage INTEGER," +
//                                        "mmnog INTEGER, num_cnt CHAR(20), dat_uchet DATE, val_cnt FLOAT, nzp_measure INTEGER, dat_prov DATE, dat_provnext DATE, nzp_kvar INTEGER, nzp_counter INTEGER, local_id CHAR(20)," +
//                                        "kod_serv CHAR(20), doppar CHAR(25))";
//                ret = t.CreateOneTable(con_db, "arch_file_ipu", "_data", fields);
//                #endregion Проверка таблицы arch_file_ipu

//                #region 8 Проверка таблицы arch_file_ipu_p
//                fields = "(id SERIAL NOT NULL, nzp_file INTEGER, id_ipu CHAR(20), rashod_type INTEGER, dat_uchet DATE, val_cnt FLOAT, kod_serv INTEGER)";
//                ret = t.CreateOneTable(con_db, "arch_file_ipu_p", "_data", fields);
//                #endregion Проверка таблицы arch_file_ipu_p

//                #region 9 Проверка таблицы arch_file_kvar
//#if PG
//                fields = "(id CHAR(20) NOT NULL, nzp_file INTEGER NOT NULL, ukas INTEGER," + " dom_id NUMERIC(18,0) NOT NULL, " +
//                "ls_type INTEGER NOT NULL, fam VARCHAR(60), ima VARCHAR(60), otch VARCHAR(60), birth_date DATE," +
//                "nkvar CHAR(10), nkvar_n CHAR(3), open_date DATE, opening_osnov CHAR(100), close_date DATE, closing_osnov CHAR(100), kol_gil INTEGER NOT NULL, kol_vrem_prib INTEGER NOT NULL," +
//                "kol_vrem_ub INTEGER NOT NULL, room_number INTEGER NOT NULL, total_square NUMERIC(14,2) NOT NULL, living_square NUMERIC(14,2), otapl_square NUMERIC(14,2), naim_square NUMERIC(14,2)," +
//                "is_communal INTEGER NOT NULL, is_el_plita INTEGER, is_gas_plita INTEGER, is_gas_colonka INTEGER, is_fire_plita INTEGER, gas_type INTEGER, water_type INTEGER, hotwater_type INTEGER," +
//                "canalization_type INTEGER, is_open_otopl INTEGER, params CHAR(250), service_row_number INTEGER NOT NULL, reval_params_row_number INTEGER NOT NULL, ipu_row_number INTEGER NOT NULL," +
//                "nzp_dom INTEGER, nzp_kvar INTEGER, comment CHAR(250), nzp_status INTEGER, id_urlic CHAR(20) )";
//#else
//                fields = "(id CHAR(20) NOT NULL, nzp_file INTEGER NOT NULL, ukas INTEGER," + " dom_id DECIMAL(18,0) NOT NULL, " +
// "ls_type INTEGER NOT NULL, fam CHAR(40), ima CHAR(40), otch CHAR(40), birth_date DATE," +
//                                        "nkvar CHAR(10), nkvar_n CHAR(3), open_date DATE, opening_osnov CHAR(100), close_date DATE, closing_osnov CHAR(100), kol_gil INTEGER NOT NULL, kol_vrem_prib INTEGER NOT NULL," +
//                                        "kol_vrem_ub INTEGER NOT NULL, room_number INTEGER NOT NULL, total_square DECIMAL(14,2) NOT NULL, living_square DECIMAL(14,2), otapl_square DECIMAL(14,2), naim_square DECIMAL(14,2)," +
//                                        "is_communal INTEGER NOT NULL, is_el_plita INTEGER, is_gas_plita INTEGER, is_gas_colonka INTEGER, is_fire_plita INTEGER, gas_type INTEGER, water_type INTEGER, hotwater_type INTEGER," +
//                                        "canalization_type INTEGER, is_open_otopl INTEGER, params CHAR(250), service_row_number INTEGER NOT NULL, reval_params_row_number INTEGER NOT NULL, ipu_row_number INTEGER NOT NULL," +
//                                        "nzp_dom INTEGER, nzp_kvar INTEGER, comment CHAR(250), nzp_status INTEGER, id_urlic CHAR(20) )";
//#endif
//                ret = t.CreateOneTable(con_db, "arch_file_kvar", "_data", fields);
//                #endregion Проверка таблицы arch_file_kvar

//                #region 10 Проверка таблицы arch_file_kvarp
//#if PG
//                fields = "(id CHAR(20) NOT NULL, reval_month DATE, nzp_file INTEGER NOT NULL, fam VARCHAR(60), ima VARCHAR(60), otch VARCHAR(60), birth_date DATE, nkvar CHAR(10), nkvar_n CHAR(3), open_date DATE," +
//                        "opening_osnov CHAR(100), close_date DATE, closing_osnov CHAR(100), kol_gil INTEGER NOT NULL, kol_vrem_prib INTEGER NOT NULL, kol_vrem_ub INTEGER NOT NULL, room_number INTEGER NOT NULL," +
//                        "total_square NUMERIC(14,2) NOT NULL, living_square NUMERIC(14,2), otapl_square NUMERIC(14,2), naim_square NUMERIC(14,2), is_communal INTEGER NOT NULL, is_el_plita INTEGER, is_gas_plita INTEGER," +
//                        "is_gas_colonka INTEGER, is_fire_plita INTEGER, gas_type INTEGER, water_type INTEGER, hotwater_type INTEGER, canalization_type INTEGER, is_open_otopl INTEGER, params CHAR(250), nzp_dom INTEGER," +
//                        "nzp_kvar INTEGER, comment CHAR(250), nzp_status INTEGER, local_id CHAR(20))";
//#else
//                fields = "(id CHAR(20) NOT NULL, reval_month DATE, nzp_file INTEGER NOT NULL, fam CHAR(40), ima CHAR(40), otch CHAR(40), birth_date DATE, nkvar CHAR(10), nkvar_n CHAR(3), open_date DATE," +
//                                        "opening_osnov CHAR(100), close_date DATE, closing_osnov CHAR(100), kol_gil INTEGER NOT NULL, kol_vrem_prib INTEGER NOT NULL, kol_vrem_ub INTEGER NOT NULL, room_number INTEGER NOT NULL," +
//                                        "total_square DECIMAL(14,2) NOT NULL, living_square DECIMAL(14,2), otapl_square DECIMAL(14,2), naim_square DECIMAL(14,2), is_communal INTEGER NOT NULL, is_el_plita INTEGER, is_gas_plita INTEGER," +
//                                        "is_gas_colonka INTEGER, is_fire_plita INTEGER, gas_type INTEGER, water_type INTEGER, hotwater_type INTEGER, canalization_type INTEGER, is_open_otopl INTEGER, params CHAR(250), nzp_dom INTEGER," +
//                                        "nzp_kvar INTEGER, comment CHAR(250), nzp_status INTEGER, local_id CHAR(20))";
//#endif
//                ret = t.CreateOneTable(con_db, "arch_file_kvarp", "_data", fields);
//                #endregion Проверка таблицы arch_file_kvarp

//                #region 11 Проверка таблицы arch_file_measures
//                fields = "(id SERIAL NOT NULL, id_measure INTEGER, measure CHAR(100), nzp_file INTEGER, nzp_measure INTEGER)";
//                ret = t.CreateOneTable(con_db, "arch_file_measures", "_data", fields);
//                #endregion Проверка таблицы arch_file_measures

//                #region 12 Проверка таблицы arch_file_mo
//#if PG
//                fields = "(id SERIAL NOT NULL, id_mo INTEGER, vill CHARACTER(50), nzp_vill NUMERIC(13,0), nzp_raj INTEGER, nzp_file INTEGER, raj CHARACTER(60), mo_name CHARACTER(60))";
//#else
//                fields = "(id SERIAL NOT NULL, id_mo INTEGER, vill CHAR(50), nzp_vill DECIMAL(13,0), nzp_raj INTEGER, nzp_file INTEGER, raj CHAR(60), mo_name CHAR(60))";
//#endif
//                ret = t.CreateOneTable(con_db, "arch_file_mo", "_data", fields);
//                #endregion Проверка таблицы arch_file_mo

//                #region 13 Проверка таблицы arch_file_nedopost
//#if PG
//                fields = "(id SERIAL NOT NULL, nzp_file INTEGER, ls_id CHARACTER(20), id_serv CHAR(20), type_ned NUMERIC(10,0), temper INTEGER, dat_nedstart DATE, dat_nedstop DATE, sum_ned NUMERIC(10,2))";
//#else
//                fields = "(id SERIAL NOT NULL, nzp_file INTEGER, ls_id CHAR(20), id_serv CHAR(20), type_ned DECIMAL(10,0), temper INTEGER, dat_nedstart DATE, dat_nedstop DATE, sum_ned DECIMAL(10,2))";
//#endif
//                ret = t.CreateOneTable(con_db, "arch_file_nedopost", "_data", fields);
//                #endregion Проверка таблицы arch_file_nedopost

//                #region 14 Проверка таблицы arch_file_odpu
//#if PG
//                fields = "(id SERIAL NOT NULL, nzp_file INTEGER NOT NULL, dom_id NUMERIC(18,0), nzp_serv INTEGER, rashod_type INTEGER, serv_type INTEGER, counter_type CHAR(25), cnt_stage INTEGER," +
//                        "mmnog INTEGER, num_cnt CHAR(20), dat_uchet DATE, val_cnt FLOAT, nzp_measure INTEGER, dat_prov DATE, dat_provnext DATE, nzp_dom INTEGER, nzp_counter INTEGER, local_id CHAR(20), doppar CHAR(25))";
//#else
//                fields = "(id SERIAL NOT NULL, nzp_file INTEGER NOT NULL, dom_id DECIMAL(18,0), nzp_serv INTEGER, rashod_type INTEGER, serv_type INTEGER, counter_type CHAR(25), cnt_stage INTEGER," +
//                                        "mmnog INTEGER, num_cnt CHAR(20), dat_uchet DATE, val_cnt FLOAT, nzp_measure INTEGER, dat_prov DATE, dat_provnext DATE, nzp_dom INTEGER, nzp_counter INTEGER, local_id CHAR(20), doppar CHAR(25))";
//#endif
//                ret = t.CreateOneTable(con_db, "arch_file_odpu", "_data", fields);
//                #endregion Проверка таблицы arch_file_odpu

//                #region 15 Проверка таблицы arch_file_odpu_p
//#if PG
//                fields = "(id SERIAL NOT NULL, nzp_file INTEGER, id_odpu CHAR(20), rashod_type INTEGER, dat_uchet DATE, val_cnt FLOAT, id_ipu INTEGER, kod_serv NUMERIC(10,0))";
//#else
//                fields = "(id SERIAL NOT NULL, nzp_file INTEGER, id_odpu CHAR(20), rashod_type INTEGER, dat_uchet DATE, val_cnt FLOAT, id_ipu INTEGER, kod_serv DECIMAL(10,0))";
//#endif
//                ret = t.CreateOneTable(con_db, "arch_file_odpu_p", "_data", fields);
//                #endregion Проверка таблицы arch_file_odpu_p

//                #region 16 Проверка таблицы arch_file_oplats
//#if PG
//                fields = "(id SERIAL NOT NULL, ls_id CHAR(20), type_oper INTEGER, numplat CHAR(80), dat_opl DATE, dat_uchet DATE, dat_izm DATE, " +
//                    "sum_oplat NUMERIC(14,2), ist_opl CHAR(80), mes_oplat DATE, nzp_file INTEGER, nzp_pack INTEGER, id_serv INTEGER)";
//#else
//                fields = "(id SERIAL NOT NULL, ls_id CHAR(20), type_oper INTEGER, numplat CHAR(80), dat_opl DATE, dat_uchet DATE, dat_izm DATE, " +
//                                    "sum_oplat DECIMAL(14,2), ist_opl CHAR(80), mes_oplat DATE, nzp_file INTEGER, nzp_pack INTEGER, id_serv INTEGER)";
//#endif
//                ret = t.CreateOneTable(con_db, "arch_file_oplats", "_data", fields);
//                #endregion Проверка таблицы arch_file_oplats

//                #region 17 Проверка таблицы arch_file_paramsdom
//#if PG
//                fields = "(id SERIAL NOT NULL, id_dom CHAR(20), id_prm INTEGER, val_prm CHAR(100), nzp_dom INTEGER, nzp_file INTEGER)";
//#else
//                fields = "(id SERIAL NOT NULL, id_dom CHAR(20), id_prm INTEGER, val_prm CHAR(100), nzp_dom INTEGER, nzp_file INTEGER)";
//#endif
//                ret = t.CreateOneTable(con_db, "arch_file_paramsdom", "_data", fields);
//                #endregion Проверка таблицы arch_file_paramsdom

//                #region 18 Проверка таблицы arch_file_paramsls
//#if PG
//                fields = "(id SERIAL NOT NULL, ls_id CHAR(20), id_prm INTEGER, val_prm CHAR(100), num_ls INTEGER, nzp_file INTEGER)";
//#else
//                fields = "(id SERIAL NOT NULL, ls_id CHAR(20), id_prm INTEGER, val_prm CHAR(100), num_ls INTEGER, nzp_file INTEGER)";
//#endif
//                ret = t.CreateOneTable(con_db, "arch_file_paramsls", "_data", fields);
//                #endregion Проверка таблицы arch_file_paramsls

//                #region 19 Проверка таблицы arch_file_serv
//#if PG
//                fields = "(id SERIAL NOT NULL, nzp_file INTEGER NOT NULL, ls_id CHAR(20) NOT NULL, supp_id NUMERIC(18,0) NOT NULL, nzp_serv INTEGER NOT NULL, sum_insaldo NUMERIC(14,2) NOT NULL," +
//                        "eot NUMERIC(14,3) NOT NULL, reg_tarif_percent NUMERIC(14,3) NOT NULL, reg_tarif NUMERIC(14,3) NOT NULL, nzp_measure INTEGER NOT NULL, fact_rashod NUMERIC(18,7) NOT NULL," +
//                        "norm_rashod NUMERIC(18,7) NOT NULL, is_pu_calc INTEGER NOT NULL, sum_nach NUMERIC(14,2) NOT NULL, sum_reval NUMERIC(14,2) NOT NULL, sum_subsidy NUMERIC(14,2) NOT NULL," +
//                        "sum_subsidyp NUMERIC(14,2) NOT NULL, sum_lgota NUMERIC(14,2) NOT NULL, sum_lgotap NUMERIC(14,2) NOT NULL, sum_smo NUMERIC(14,2) NOT NULL, sum_smop NUMERIC(14,2) NOT NULL," +
//                        "sum_money NUMERIC(14,2) NOT NULL, is_del INTEGER NOT NULL, sum_outsaldo NUMERIC(14,2) NOT NULL, servp_row_number INTEGER NOT NULL, nzp_kvar INTEGER, nzp_supp INTEGER)";
//#else
//                fields = "(id SERIAL NOT NULL, nzp_file INTEGER NOT NULL, ls_id CHAR(20) NOT NULL, supp_id DECIMAL(18,0) NOT NULL, nzp_serv INTEGER NOT NULL, sum_insaldo DECIMAL(14,2) NOT NULL," +
//                                        "eot DECIMAL(14,3) NOT NULL, reg_tarif_percent DECIMAL(14,3) NOT NULL, reg_tarif DECIMAL(14,3) NOT NULL, nzp_measure INTEGER NOT NULL, fact_rashod DECIMAL(18,7) NOT NULL," +
//                                        "norm_rashod DECIMAL(18,7) NOT NULL, is_pu_calc INTEGER NOT NULL, sum_nach DECIMAL(14,2) NOT NULL, sum_reval DECIMAL(14,2) NOT NULL, sum_subsidy DECIMAL(14,2) NOT NULL," +
//                                        "sum_subsidyp DECIMAL(14,2) NOT NULL, sum_lgota DECIMAL(14,2) NOT NULL, sum_lgotap DECIMAL(14,2) NOT NULL, sum_smo DECIMAL(14,2) NOT NULL, sum_smop DECIMAL(14,2) NOT NULL," +
//                                        "sum_money DECIMAL(14,2) NOT NULL, is_del INTEGER NOT NULL, sum_outsaldo DECIMAL(14,2) NOT NULL, servp_row_number INTEGER NOT NULL, nzp_kvar INTEGER, nzp_supp INTEGER)";
//#endif
//                ret = t.CreateOneTable(con_db, "arch_file_serv", "_data", fields);
//                #endregion Проверка таблицы arch_file_serv

//                #region 20 Проверка таблицы arch_file_services
//#if PG
//                fields = "(id SERIAL NOT NULL, id_serv INTEGER, service CHAR(100), service2 CHAR(100), nzp_file INTEGER, nzp_measure INTEGER, ed_izmer CHAR(30), type_serv INTEGER, nzp_serv INTEGER)";
//#else
//                fields = "(id SERIAL NOT NULL, id_serv INTEGER, service CHAR(100), service2 CHAR(100), nzp_file INTEGER, nzp_measure INTEGER, ed_izmer CHAR(30), type_serv INTEGER, nzp_serv INTEGER)";
//#endif
//                ret = t.CreateOneTable(con_db, "arch_file_services", "_data", fields);
//                #endregion Проверка таблицы arch_file_services

//                #region 21 Проверка таблицы arch_file_servls
//#if PG
//                fields = "(id SERIAL NOT NULL, nzp_file INTEGER, ls_id NUMERIC(14,0), id_serv CHAR(100), dat_start DATE, dat_stop DATE, supp_id NUMERIC(14,0))";
//#else
//                fields = "(id SERIAL NOT NULL, nzp_file INTEGER, ls_id DECIMAL(14,0), id_serv CHAR(100), dat_start DATE, dat_stop DATE, supp_id DECIMAL(14,0))";
//#endif
//                ret = t.CreateOneTable(con_db, "arch_file_servls", "_data", fields);
//                #endregion Проверка таблицы arch_file_servls

//                #region 22 Проверка таблицы arch_file_servp
//#if PG
//                fields = "(id SERIAL NOT NULL, nzp_file INTEGER NOT NULL, reval_month DATE, ls_id CHAR(20) NOT NULL, supp_id NUMERIC(18,0) NOT NULL, nzp_serv INTEGER NOT NULL, eot NUMERIC(14,3) NOT NULL," +
//                        "reg_tarif_percent NUMERIC(14,3) NOT NULL, reg_tarif NUMERIC(14,3) NOT NULL, nzp_measure INTEGER NOT NULL, fact_rashod NUMERIC(18,7) NOT NULL, norm_rashod NUMERIC(18,7) NOT NULL," +
//                        "is_pu_calc INTEGER NOT NULL, sum_reval NUMERIC(14,2) NOT NULL, sum_subsidyp NUMERIC(14,2) NOT NULL, sum_lgotap NUMERIC(14,2) NOT NULL, sum_smop NUMERIC(14,2) NOT NULL," +
//                        "nzp_kvar INTEGER, nzp_supp INTEGER)";
//#else
//                fields = "(id SERIAL NOT NULL, nzp_file INTEGER NOT NULL, reval_month DATE, ls_id CHAR(20) NOT NULL, supp_id DECIMAL(18,0) NOT NULL, nzp_serv INTEGER NOT NULL, eot DECIMAL(14,3) NOT NULL," +
//                                        "reg_tarif_percent DECIMAL(14,3) NOT NULL, reg_tarif DECIMAL(14,3) NOT NULL, nzp_measure INTEGER NOT NULL, fact_rashod DECIMAL(18,7) NOT NULL, norm_rashod DECIMAL(18,7) NOT NULL," +
//                                        "is_pu_calc INTEGER NOT NULL, sum_reval DECIMAL(14,2) NOT NULL, sum_subsidyp DECIMAL(14,2) NOT NULL, sum_lgotap DECIMAL(14,2) NOT NULL, sum_smop DECIMAL(14,2) NOT NULL," +
//                                        "nzp_kvar INTEGER, nzp_supp INTEGER)";
//#endif
//                ret = t.CreateOneTable(con_db, "arch_file_servp", "_data", fields);
//                #endregion Проверка таблицы arch_file_servp

//                #region 23 Проверка таблицы arch_file_supp
//#if PG
//                fields = "(id SERIAL NOT NULL, nzp_file INTEGER NOT NULL, supp_id NUMERIC(18,0) NOT NULL, supp_name CHAR(25) NOT NULL, jur_address CHAR(100), fact_address CHAR(100), inn CHAR(12), kpp CHAR(9)," +
//                        "rs CHAR(20), bank CHAR(100), bik CHAR(20), ks CHAR(20), nzp_supp INTEGER)";
//#else
//                fields = "(id SERIAL NOT NULL, nzp_file INTEGER NOT NULL, supp_id DECIMAL(18,0) NOT NULL, supp_name CHAR(25) NOT NULL, jur_address CHAR(100), fact_address CHAR(100), inn CHAR(12), kpp CHAR(9)," +
//                                        "rs CHAR(20), bank CHAR(100), bik CHAR(20), ks CHAR(20), nzp_supp INTEGER)";
//#endif
//                ret = t.CreateOneTable(con_db, "arch_file_supp", "_data", fields);
//                #endregion Проверка таблицы  arch_file_supp

//                #region 24 Проверка таблицы arch_file_typenedopost
//#if PG
//                fields = "(id SERIAL NOT NULL, nzp_file INTEGER, type_ned NUMERIC(10,0), ned_name CHAR(100))";
//#else
//                fields = "(id SERIAL NOT NULL, nzp_file INTEGER, type_ned DECIMAL(10,0), ned_name CHAR(100))";
//#endif
//                ret = t.CreateOneTable(con_db, "arch_file_typenedopost", "_data", fields);
//                #endregion Проверка таблицы arch_file_typenedopost

//                #region 25 Проверка таблицы arch_file_typeparams
//#if PG
//                fields = "(id SERIAL NOT NULL, id_prm INTEGER, prm_name CHAR(100), level_ INTEGER, type_prm INTEGER, nzp_file INTEGER, nzp_prm INTEGER)";
//#else
//                fields = "(id SERIAL NOT NULL, id_prm INTEGER, prm_name CHAR(100), level_ INTEGER, type_prm INTEGER, nzp_file INTEGER, nzp_prm INTEGER)";
//#endif
//                ret = t.CreateOneTable(con_db, "arch_file_typeparams", "_data", fields);
//                #endregion Проверка таблицы  arch_file_typeparams

//                #region 26 Проверка таблицы arch_file_urlic
//#if PG
//                fields = "(id SERIAL NOT NULL, nzp_file INTEGER NOT NULL, supp_id NUMERIC(18,0) NOT NULL, supp_name CHAR(100) NOT NULL, jur_address CHAR(100), fact_address CHAR(100)," +
//                     " inn CHAR(12), kpp CHAR(9), rs CHAR(20), bank CHAR(100), bik_bank CHAR(20), ks CHAR(20), tel_chief CHAR(20), tel_b CHAR(20), chief_name CHAR(100)," +
//                     " chief_post CHAR(40), b_name CHAR(100), okonh1 CHAR(20), okonh2 CHAR(20), okpo CHAR(20), bank_pr CHAR(100), bank_adr CHAR(100), bik CHAR(20)," +
//                     " rs_pr CHAR(20), ks_pr CHAR(20), post_and_name CHAR(200), nzp_supp INTEGER)";
//#else
//                fields = "(id SERIAL NOT NULL, nzp_file INTEGER NOT NULL, supp_id DECIMAL(18,0) NOT NULL, supp_name CHAR(100) NOT NULL, jur_address CHAR(100), fact_address CHAR(100)," +
//                                    " inn CHAR(12), kpp CHAR(9), rs CHAR(20), bank CHAR(100), bik_bank CHAR(20), ks CHAR(20), tel_chief CHAR(20), tel_b CHAR(20), chief_name CHAR(100)," +
//                                    " chief_post CHAR(40), b_name CHAR(100), okonh1 CHAR(20), okonh2 CHAR(20), okpo CHAR(20), bank_pr CHAR(100), bank_adr CHAR(100), bik CHAR(20)," +
//                                    " rs_pr CHAR(20), ks_pr CHAR(20), post_and_name CHAR(200), nzp_supp INTEGER)";
//#endif
//                ret = t.CreateOneTable(con_db, "arch_file_urlic", "_data", fields);
//                #endregion Проверка таблицы  arch_file_urlic

//                #region 27 Проверка таблицы arch_file_voda
//#if PG
//                fields = "(id SERIAL NOT NULL, id_prm INTEGER, name CHAR(100), nzp_file INTEGER, nzp_prm INTEGER)";
//#else
//                fields = "(id SERIAL NOT NULL, id_prm INTEGER, name CHAR(100), nzp_file INTEGER, nzp_prm INTEGER)";
//#endif
//                ret = t.CreateOneTable(con_db, "arch_file_voda", "_data", fields);
//                #endregion Проверка таблицы  arch_file_voda

//                #region 28 Проверка таблицы arch_file_vrub
//#if PG
//                fields = "(id SERIAL NOT NULL, nzp_file INTEGER, ls_id CHAR(20),  gil_id INTEGER , dat_vrvib DATE, dat_end DATE)";
//#else
//                fields = "(id SERIAL NOT NULL, nzp_file INTEGER, ls_id CHAR(20),  gil_id INTEGER , dat_vrvib DATE, dat_end DATE)";
//#endif
//                ret = t.CreateOneTable(con_db, "arch_file_vrub", "_data", fields);
//                #endregion Проверка таблицы arch_file_vrub
                
//                #region 29 Проверка таблицы arch_file_blag
//#if PG
//                fields = "(id SERIAL NOT NULL, id_prm INTEGER, name CHAR(100), nzp_file INTEGER, nzp_prm INTEGER)";
//#else
//                fields = "(id SERIAL NOT NULL, id_prm INTEGER, name CHAR(100), nzp_file INTEGER, nzp_prm INTEGER)";
//#endif
//                ret = t.CreateOneTable(con_db, "arch_file_blag", "_data", fields);
//                #endregion Проверка таблицы  arch_file_blag

//                #endregion

//                #region Архивирование
//                IDbTransaction tr_id = con_db.BeginTransaction();

//                try
//                {
//                    #region archived_files
//                    sql =
//                        " insert into " + Points.Pref + DBManager.sUploadAliasRest + "archived_files " +
//                        " (nzp_file, nzp_version, loaded_name, saved_name, nzp_status, created_by, created_on, file_type, nzp_exc, nzp_exc_log, percent, pref, diss_status, date_arch, user_arch, where_arch) " +
//                        " select nzp_file, nzp_version, loaded_name, saved_name, nzp_status, created_by, created_on, file_type, nzp_exc, nzp_exc_log, percent, pref, diss_status, " + sCurDate + ", " + finder.nzp_user + ", '" + Points.Pref + "_data' " +
//                        " from " + Points.Pref + DBManager.sUploadAliasRest + "files_imported " + 
//                        " where nzp_file in " +
//                        " (select nzp_file from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
//                        " where pref = '" + finder.bank + "' and nzp_user = " + finder.nzp_user + ")";
//                    ret = ExecSQL(con_db, tr_id, sql, true);

//                    if (ret.result)
//                    {
//                        sql =
//                            " delete from " + Points.Pref + DBManager.sUploadAliasRest + "files_imported " +
//                            " where nzp_file in " +
//                            " (select nzp_file from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
//                        " where pref = '" + finder.bank + "' and nzp_user = " + finder.nzp_user + ")";
//                        ret = ExecSQL(con_db, tr_id, sql, true);
//                    }
//                    #endregion

//                    #region 1 arch_file_area
//                    sql =
//                        " insert into " + Points.Pref + DBManager.sUploadAliasRest + "arch_file_area " +
//                        " (id, nzp_file, area, jur_address, fact_address, inn, kpp, rs, bank, bik, ks, nzp_area) " +
//                        " select id, nzp_file, area, jur_address, fact_address, inn, kpp, rs, bank, bik, ks, nzp_area " +
//                        " from " + Points.Pref + DBManager.sUploadAliasRest + "file_area " + 
//                        " where nzp_file in " +
//                        " (select nzp_file from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
//                        " where pref = '" + finder.bank + "' and nzp_user = " + finder.nzp_user + ")";
//                    ret = ExecSQL(con_db, tr_id, sql, true);

//                    if (ret.result)
//                    {
//                        sql =
//                            " delete from " + Points.Pref + DBManager.sUploadAliasRest + "file_area" +
//                            " where nzp_file in " +
//                            " (select nzp_file from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
//                        " where pref = '" + finder.bank + "' and nzp_user = " + finder.nzp_user + ")";
//                        ret = ExecSQL(con_db, tr_id, sql, true);
//                    }

//                    #endregion

//                    #region 2 arch_file_blag
//                    sql =
//                        " insert into " + Points.Pref + DBManager.sUploadAliasRest + "arch_file_blag " +
//                        " (id, id_prm, name, nzp_file, nzp_prm) " +
//                        " select id, id_prm, name, nzp_file, nzp_prm " +
//                        " from " + Points.Pref + DBManager.sUploadAliasRest + "file_blag " +
//                        " where nzp_file in " +
//                        " (select nzp_file from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
//                        " where pref = '" + finder.bank + "' and nzp_user = " + finder.nzp_user + ")";
//                    ret = ExecSQL(con_db, tr_id, sql, true);

//                    if (ret.result)
//                    {
//                        sql =
//                            " delete from " + Points.Pref + DBManager.sUploadAliasRest + "file_blag" +
//                            " where nzp_file in " +
//                            " (select nzp_file from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
//                        " where pref = '" + finder.bank + "' and nzp_user = " + finder.nzp_user + ")";
//                        ret = ExecSQL(con_db, tr_id, sql, true);
//                    }

//                    #endregion

//                    #region 3 arch_file_dom
//                    sql =
//                        " insert into " + Points.Pref + DBManager.sUploadAliasRest + "arch_file_dom " +
//                        " (id, nzp_file, ukds, town, rajon, ulica, ndom, nkor, area_id, cat_blago, " +
//                        " etazh, build_year, total_square, mop_square, useful_square, mo_id, params, " +
//                        " ls_row_number, odpu_row_number, nzp_ul, nzp_dom, comment, local_id, nzp_raj, " +
//                        " nzp_town, nzp_geu, uch, kod_kladr) " +
//                        " select id, nzp_file, ukds, town, rajon, ulica, ndom, nkor, area_id, cat_blago, " +
//                        " etazh, build_year, total_square, mop_square, useful_square, mo_id, params, " +
//                        " ls_row_number, odpu_row_number, nzp_ul, nzp_dom, comment, local_id, nzp_raj, " +
//                        " nzp_town, nzp_geu, uch, kod_kladr " +
//                        " from " + Points.Pref + DBManager.sUploadAliasRest + "file_dom " +
//                        " where nzp_file in " +
//                        " (select nzp_file from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
//                        " where pref = '" + finder.bank + "' and nzp_user = " + finder.nzp_user + ")";
//                    ret = ExecSQL(con_db, tr_id, sql, true);

//                    if (ret.result)
//                    {
//                        sql =
//                            " delete from " + Points.Pref + DBManager.sUploadAliasRest + "file_dom" +
//                            " where nzp_file in " +
//                            " (select nzp_file from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
//                        " where pref = '" + finder.bank + "' and nzp_user = " + finder.nzp_user + ")";
//                        ret = ExecSQL(con_db, tr_id, sql, true);
//                    }

//                    #endregion

//                    #region 4 arch_file_gaz
//                    sql =
//                        " insert into " + Points.Pref + DBManager.sUploadAliasRest + "arch_file_gaz " +
//                        " (id, id_prm, name, nzp_file, nzp_prm) " +
//                        " select id, id_prm, name, nzp_file, nzp_prm " +
//                        " from " + Points.Pref + DBManager.sUploadAliasRest + "file_gaz " +
//                        " where nzp_file in " +
//                        " (select nzp_file from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
//                        " where pref = '" + finder.bank + "' and nzp_user = " + finder.nzp_user + ")";
//                    ret = ExecSQL(con_db, tr_id, sql, true);

//                    if (ret.result)
//                    {
//                        sql =
//                            " delete from " + Points.Pref + DBManager.sUploadAliasRest + "file_gaz" +
//                            " where nzp_file in " +
//                            " (select nzp_file from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
//                        " where pref = '" + finder.bank + "' and nzp_user = " + finder.nzp_user + ")";
//                        ret = ExecSQL(con_db, tr_id, sql, true);
//                    }

//                    #endregion

//                    #region 5 arch_file_gilec
//                    sql =
//                        " insert into " + Points.Pref + DBManager.sUploadAliasRest + "arch_file_gilec " +
//                        " (nzp_file, num_ls, nzp_gil, nzp_kart, nzp_tkrt, fam, ima, otch, dat_rog, fam_c," +
//                        " ima_c, otch_c, dat_rog_c, gender, nzp_dok, serij, nomer, vid_dat, vid_mes," +
//                        " kod_podrazd, strana_mr, region_mr, okrug_mr, gorod_mr, npunkt_mr, rem_mr," +
//                        " strana_op, region_op, okrug_op, gorod_op, npunkt_op, rem_op, strana_ku, region_ku," +
//                        " okrug_ku, gorod_ku, npunkt_ku, rem_ku, rem_p, tprp, dat_prop, dat_oprp, dat_pvu," +
//                        " who_pvu, dat_svu, namereg, kod_namereg, rod, nzp_celp, nzp_celu, dat_sost, dat_ofor," +
//                        " comment, id) " +
//                        " select nzp_file, num_ls, nzp_gil, nzp_kart, nzp_tkrt, fam, ima, otch, dat_rog, fam_c," +
//                        " ima_c, otch_c, dat_rog_c, gender, nzp_dok, serij, nomer, vid_dat, vid_mes," +
//                        " kod_podrazd, strana_mr, region_mr, okrug_mr, gorod_mr, npunkt_mr, rem_mr, strana_op," +
//                        " region_op, okrug_op, gorod_op, npunkt_op, rem_op, strana_ku, region_ku, okrug_ku," +
//                        " gorod_ku, npunkt_ku, rem_ku, rem_p, tprp, dat_prop, dat_oprp, dat_pvu, who_pvu, dat_svu, " +
//                        " namereg, kod_namereg, rod, nzp_celp, nzp_celu, dat_sost, dat_ofor, comment, id " +
//                        " from " + Points.Pref + DBManager.sUploadAliasRest + "file_gilec " +
//                        " where nzp_file in " +
//                        " (select nzp_file from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
//                        " where pref = '" + finder.bank + "' and nzp_user = " + finder.nzp_user + ")";
//                    ret = ExecSQL(con_db, tr_id, sql, true);

//                    if (ret.result)
//                    {
//                        sql =
//                            " delete from " + Points.Pref + DBManager.sUploadAliasRest + "file_gilec" +
//                            " where nzp_file in " +
//                            " (select nzp_file from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
//                        " where pref = '" + finder.bank + "' and nzp_user = " + finder.nzp_user + ")";
//                        ret = ExecSQL(con_db, tr_id, sql, true);
//                    }

//                    #endregion

//                    #region 6 arch_file_head
//                    sql =
//                        " insert into " + Points.Pref + DBManager.sUploadAliasRest + "arch_file_head " +
//                        " (id, nzp_file, org_name, branch_name, inn, kpp, file_no, file_date," +
//                        " sender_phone, sender_fio, calc_date, row_number) " +
//                        " select id, nzp_file, org_name, branch_name, inn, kpp, file_no, file_date," +
//                        " sender_phone, sender_fio, calc_date, row_number " +
//                        " from " + Points.Pref + DBManager.sUploadAliasRest + "file_head " +
//                        " where nzp_file in " +
//                        " (select nzp_file from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
//                        " where pref = '" + finder.bank + "' and nzp_user = " + finder.nzp_user + ")";
//                    ret = ExecSQL(con_db, tr_id, sql, true);

//                    if (ret.result)
//                    {
//                        sql =
//                            " delete from " + Points.Pref + DBManager.sUploadAliasRest + "file_head" +
//                            " where nzp_file in " +
//                            " (select nzp_file from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
//                        " where pref = '" + finder.bank + "' and nzp_user = " + finder.nzp_user + ")";
//                        ret = ExecSQL(con_db, tr_id, sql, true);
//                    }

//                    #endregion

//                    #region 7 arch_file_ipu
//                    sql =
//                        " insert into " + Points.Pref + DBManager.sUploadAliasRest + "arch_file_ipu " +
//                        " (id, nzp_file, ls_id, nzp_serv, rashod_type, serv_type, counter_type, cnt_stage," +
//                        " mmnog, num_cnt, dat_uchet, val_cnt, nzp_measure, dat_prov, dat_provnext, nzp_kvar," +
//                        " nzp_counter, local_id, kod_serv, doppar) " +
//                        " select id, nzp_file, ls_id, nzp_serv, rashod_type, serv_type, counter_type, cnt_stage," +
//                        " mmnog, num_cnt, dat_uchet, val_cnt, nzp_measure, dat_prov, dat_provnext, nzp_kvar," +
//                        " nzp_counter, local_id, kod_serv, doppar " +
//                        " from " + Points.Pref + DBManager.sUploadAliasRest + "file_ipu " +
//                        " where nzp_file in " +
//                        " (select nzp_file from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
//                        " where pref = '" + finder.bank + "' and nzp_user = " + finder.nzp_user + ")";
//                    ret = ExecSQL(con_db, tr_id, sql, true);

//                    if (ret.result)
//                    {
//                        sql =
//                            " delete from " + Points.Pref + DBManager.sUploadAliasRest + "file_ipu" +
//                            " where nzp_file in " +
//                            " (select nzp_file from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
//                        " where pref = '" + finder.bank + "' and nzp_user = " + finder.nzp_user + ")";
//                        ret = ExecSQL(con_db, tr_id, sql, true);
//                    }

//                    #endregion

//                    #region 8 arch_file_ipu_p
//                    sql =
//                        " insert into " + Points.Pref + DBManager.sUploadAliasRest + "arch_file_ipu_p " +
//                        " (id, nzp_file, id_ipu, rashod_type, dat_uchet, val_cnt, kod_serv) " +
//                        " select id, nzp_file, id_ipu, rashod_type, dat_uchet, val_cnt, kod_serv " +
//                        " from " + Points.Pref + DBManager.sUploadAliasRest + "file_ipu_p " +
//                        " where nzp_file in " +
//                        " (select nzp_file from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
//                        " where pref = '" + finder.bank + "' and nzp_user = " + finder.nzp_user + ")";
//                    ret = ExecSQL(con_db, tr_id, sql, true);

//                    if (ret.result)
//                    {
//                        sql =
//                            " delete from " + Points.Pref + DBManager.sUploadAliasRest + "file_ipu_p" +
//                            " where nzp_file in " +
//                            " (select nzp_file from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
//                        " where pref = '" + finder.bank + "' and nzp_user = " + finder.nzp_user + ")";
//                        ret = ExecSQL(con_db, tr_id, sql, true);
//                    }

//                    #endregion

//                    #region 9 arch_file_kvar
//                    sql =
//                        " insert into " + Points.Pref + DBManager.sUploadAliasRest + "arch_file_kvar " +
//                        " (id, nzp_file, ukas, dom_id, ls_type, fam, ima, otch, birth_date, nkvar, nkvar_n," +
//                        " open_date, opening_osnov, close_date, closing_osnov, kol_gil, kol_vrem_prib, kol_vrem_ub," +
//                        " room_number, total_square, living_square, otapl_square, naim_square, is_communal, is_el_plita," +
//                        " is_gas_plita, is_gas_colonka, is_fire_plita, gas_type, water_type, hotwater_type, canalization_type," +
//                        " is_open_otopl, params, service_row_number, reval_params_row_number, ipu_row_number, nzp_dom, nzp_kvar," +
//                        " comment, nzp_status, id_urlic) " +
//                        " select id, nzp_file, ukas, dom_id, ls_type, fam, ima, otch, birth_date, nkvar, nkvar_n, open_date," +
//                        " opening_osnov, close_date, closing_osnov, kol_gil, kol_vrem_prib, kol_vrem_ub, room_number," +
//                        " total_square, living_square, otapl_square, naim_square, is_communal, is_el_plita, is_gas_plita," +
//                        " is_gas_colonka, is_fire_plita, gas_type, water_type, hotwater_type, canalization_type, is_open_otopl," +
//                        " params, service_row_number, reval_params_row_number, ipu_row_number, nzp_dom, nzp_kvar, comment," +
//                        " nzp_status, id_urlic " +
//                        " from " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar " +
//                        " where nzp_file in " +
//                        " (select nzp_file from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
//                        " where pref = '" + finder.bank + "' and nzp_user = " + finder.nzp_user + ")";
//                    ret = ExecSQL(con_db, tr_id, sql, true);

//                    if (ret.result)
//                    {
//                        sql =
//                            " delete from " + Points.Pref + DBManager.sUploadAliasRest + "file_kvar" +
//                            " where nzp_file in " +
//                            " (select nzp_file from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
//                        " where pref = '" + finder.bank + "' and nzp_user = " + finder.nzp_user + ")";
//                        ret = ExecSQL(con_db, tr_id, sql, true);
//                    }

//                    #endregion

//                    #region 10 arch_file_kvarp
//                    sql =
//                        " insert into " + Points.Pref + DBManager.sUploadAliasRest + "arch_file_kvarp " +
//                        " (id, reval_month, nzp_file, fam, ima, otch, birth_date, nkvar, nkvar_n, open_date," +
//                        " opening_osnov, close_date, closing_osnov, kol_gil, kol_vrem_prib, kol_vrem_ub," +
//                        " room_number, total_square, living_square, otapl_square, naim_square, is_communal," +
//                        " is_el_plita, is_gas_plita, is_gas_colonka, is_fire_plita, gas_type, water_type," +
//                        " hotwater_type, canalization_type, is_open_otopl, params, nzp_dom, nzp_kvar, comment," +
//                        " nzp_status, local_id) " +
//                        " select id, reval_month, nzp_file, fam, ima, otch, birth_date, nkvar, nkvar_n," +
//                        " open_date, opening_osnov, close_date, closing_osnov, kol_gil, kol_vrem_prib," +
//                        " kol_vrem_ub, room_number, total_square, living_square, otapl_square, naim_square," +
//                        " is_communal, is_el_plita, is_gas_plita, is_gas_colonka, is_fire_plita, gas_type," +
//                        " water_type, hotwater_type, canalization_type, is_open_otopl, params, nzp_dom," +
//                        " nzp_kvar, comment, nzp_status, local_id " +
//                        " from " + Points.Pref + DBManager.sUploadAliasRest + "file_kvarp " +
//                        " where nzp_file in " +
//                        " (select nzp_file from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
//                        " where pref = '" + finder.bank + "' and nzp_user = " + finder.nzp_user + ")";
//                    ret = ExecSQL(con_db, tr_id, sql, true);

//                    if (ret.result)
//                    {
//                        sql =
//                            " delete from " + Points.Pref + DBManager.sUploadAliasRest + "file_kvarp" +
//                            " where nzp_file in " +
//                            " (select nzp_file from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
//                        " where pref = '" + finder.bank + "' and nzp_user = " + finder.nzp_user + ")";
//                        ret = ExecSQL(con_db, tr_id, sql, true);
//                    }

//                    #endregion

//                    #region 11 arch_file_measures
//                    sql =
//                        " insert into " + Points.Pref + DBManager.sUploadAliasRest + "arch_file_measures " +
//                        " (id, id_measure, measure, nzp_file, nzp_measure) " +
//                        " select id, id_measure, measure, nzp_file, nzp_measure " +
//                        " from " + Points.Pref + DBManager.sUploadAliasRest + "file_measures " +
//                        " where nzp_file in " +
//                        " (select nzp_file from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
//                        " where pref = '" + finder.bank + "' and nzp_user = " + finder.nzp_user + ")";
//                    ret = ExecSQL(con_db, tr_id, sql, true);

//                    if (ret.result)
//                    {
//                        sql =
//                            " delete from " + Points.Pref + DBManager.sUploadAliasRest + "file_measures" +
//                            " where nzp_file in " +
//                            " (select nzp_file from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
//                        " where pref = '" + finder.bank + "' and nzp_user = " + finder.nzp_user + ")";
//                        ret = ExecSQL(con_db, tr_id, sql, true);
//                    }

//                    #endregion

//                    #region 12 arch_file_mo
//                    sql =
//                        " insert into " + Points.Pref + DBManager.sUploadAliasRest + "arch_file_mo " +
//                        " (id, id_mo, vill, nzp_vill, nzp_raj, nzp_file, raj, mo_name) " +
//                        " select id, id_mo, vill, nzp_vill, nzp_raj, nzp_file, raj, mo_name " +
//                        " from " + Points.Pref + DBManager.sUploadAliasRest + "file_mo " +
//                        " where nzp_file in " +
//                        " (select nzp_file from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
//                        " where pref = '" + finder.bank + "' and nzp_user = " + finder.nzp_user + ")";
//                    ret = ExecSQL(con_db, tr_id, sql, true);

//                    if (ret.result)
//                    {
//                        sql =
//                            " delete from " + Points.Pref + DBManager.sUploadAliasRest + "file_mo" +
//                            " where nzp_file in " +
//                            " (select nzp_file from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
//                        " where pref = '" + finder.bank + "' and nzp_user = " + finder.nzp_user + ")";
//                        ret = ExecSQL(con_db, tr_id, sql, true);
//                    }

//                    #endregion

//                    #region 13 arch_file_nedopost
//                    sql =
//                        " insert into " + Points.Pref + DBManager.sUploadAliasRest + "arch_file_nedopost " +
//                        " (id, nzp_file, ls_id, id_serv, type_ned, temper, dat_nedstart, dat_nedstop, sum_ned) " +
//                        " select id, nzp_file, ls_id, id_serv, type_ned, temper, dat_nedstart, dat_nedstop," +
//                        " sum_ned " +
//                        " from " + Points.Pref + DBManager.sUploadAliasRest + "file_nedopost " +
//                        " where nzp_file in " +
//                        " (select nzp_file from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
//                        " where pref = '" + finder.bank + "' and nzp_user = " + finder.nzp_user + ")";
//                    ret = ExecSQL(con_db, tr_id, sql, true);

//                    if (ret.result)
//                    {
//                        sql =
//                            " delete from " + Points.Pref + DBManager.sUploadAliasRest + "file_nedopost" +
//                            " where nzp_file in " +
//                            " (select nzp_file from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
//                        " where pref = '" + finder.bank + "' and nzp_user = " + finder.nzp_user + ")";
//                        ret = ExecSQL(con_db, tr_id, sql, true);
//                    }

//                    #endregion

//                    #region 14 arch_file_nedopost
//                    sql =
//                        " insert into " + Points.Pref + DBManager.sUploadAliasRest + "arch_file_nedopost " +
//                        " (id, nzp_file, ls_id, id_serv, type_ned, temper, dat_nedstart, dat_nedstop, sum_ned) " +
//                        " select id, nzp_file, ls_id, id_serv, type_ned, temper, dat_nedstart, dat_nedstop," +
//                        " sum_ned " +
//                        " from " + Points.Pref + DBManager.sUploadAliasRest + "file_nedopost " +
//                        " where nzp_file in " +
//                        " (select nzp_file from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
//                        " where pref = '" + finder.bank + "' and nzp_user = " + finder.nzp_user + ")";
//                    ret = ExecSQL(con_db, tr_id, sql, true);

//                    if (ret.result)
//                    {
//                        sql =
//                            " delete from " + Points.Pref + DBManager.sUploadAliasRest + "file_nedopost" +
//                            " where nzp_file in " +
//                            " (select nzp_file from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
//                        " where pref = '" + finder.bank + "' and nzp_user = " + finder.nzp_user + ")";
//                        ret = ExecSQL(con_db, tr_id, sql, true);
//                    }

//                    #endregion

//                    #region 15 arch_file_odpu
//                    sql =
//                        " insert into " + Points.Pref + DBManager.sUploadAliasRest + "arch_file_odpu " +
//                        " (id, nzp_file, dom_id, nzp_serv, rashod_type, serv_type, counter_type, cnt_stage, mmnog," +
//                        " num_cnt, dat_uchet, val_cnt, nzp_measure, dat_prov, dat_provnext, nzp_dom, nzp_counter," +
//                        " local_id, doppar) " +
//                        " select id, nzp_file, dom_id, nzp_serv, rashod_type, serv_type, counter_type, cnt_stage," +
//                        " mmnog, num_cnt, dat_uchet, val_cnt, nzp_measure, dat_prov, dat_provnext, nzp_dom," +
//                        " nzp_counter, local_id, doppar " +
//                        " from " + Points.Pref + DBManager.sUploadAliasRest + "file_odpu " +
//                        " where nzp_file in " +
//                        " (select nzp_file from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
//                        " where pref = '" + finder.bank + "' and nzp_user = " + finder.nzp_user + ")";
//                    ret = ExecSQL(con_db, tr_id, sql, true);

//                    if (ret.result)
//                    {
//                        sql =
//                            " delete from " + Points.Pref + DBManager.sUploadAliasRest + "file_odpu" +
//                            " where nzp_file in " +
//                            " (select nzp_file from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
//                        " where pref = '" + finder.bank + "' and nzp_user = " + finder.nzp_user + ")";
//                        ret = ExecSQL(con_db, tr_id, sql, true);
//                    }

//                    #endregion

//                    #region 16 arch_file_odpu_p
//                    sql =
//                        " insert into " + Points.Pref + DBManager.sUploadAliasRest + "arch_file_odpu_p " +
//                        " (id, nzp_file, id_odpu, rashod_type, dat_uchet, val_cnt, id_ipu, kod_serv) " +
//                        " select id, nzp_file, id_odpu, rashod_type, dat_uchet, val_cnt, id_ipu, kod_serv " +
//                        " from " + Points.Pref + DBManager.sUploadAliasRest + "file_odpu_p " +
//                        " where nzp_file in " +
//                        " (select nzp_file from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
//                        " where pref = '" + finder.bank + "' and nzp_user = " + finder.nzp_user + ")";
//                    ret = ExecSQL(con_db, tr_id, sql, true);

//                    if (ret.result)
//                    {
//                        sql =
//                            " delete from " + Points.Pref + DBManager.sUploadAliasRest + "file_odpu_p" +
//                            " where nzp_file in " +
//                            " (select nzp_file from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
//                        " where pref = '" + finder.bank + "' and nzp_user = " + finder.nzp_user + ")";
//                        ret = ExecSQL(con_db, tr_id, sql, true);
//                    }

//                    #endregion

//                    #region 17 arch_file_oplats
//                    sql =
//                        " insert into " + Points.Pref + DBManager.sUploadAliasRest + "arch_file_oplats " +
//                        " (id, ls_id, type_oper, numplat, dat_opl, dat_uchet, dat_izm, sum_oplat, ist_opl," +
//                        " mes_oplat, nzp_file, nzp_pack, id_serv) " +
//                        " select id, ls_id, type_oper, numplat, dat_opl, dat_uchet, dat_izm, sum_oplat," +
//                        " ist_opl, mes_oplat, nzp_file, nzp_pack, id_serv " +
//                        " from " + Points.Pref + DBManager.sUploadAliasRest + "file_oplats " +
//                        " where nzp_file in " +
//                        " (select nzp_file from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
//                        " where pref = '" + finder.bank + "' and nzp_user = " + finder.nzp_user + ")";
//                    ret = ExecSQL(con_db, tr_id, sql, true);

//                    if (ret.result)
//                    {
//                        sql =
//                            " delete from " + Points.Pref + DBManager.sUploadAliasRest + "file_oplats" +
//                            " where nzp_file in " +
//                            " (select nzp_file from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
//                        " where pref = '" + finder.bank + "' and nzp_user = " + finder.nzp_user + ")";
//                        ret = ExecSQL(con_db, tr_id, sql, true);
//                    }

//                    #endregion

//                    #region 18 arch_file_pack
//                    sql =
//                        " insert into " + Points.Pref + DBManager.sUploadAliasRest + "arch_file_pack " +
//                        " (id, nzp_file, dat_plat, num_plat, sum_plat, kol_plat) " +
//                        " select id, nzp_file, dat_plat, num_plat, sum_plat, kol_plat " +
//                        " from " + Points.Pref + DBManager.sUploadAliasRest + "file_pack  " +
//                        " where nzp_file in " +
//                        " (select nzp_file from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
//                        " where pref = '" + finder.bank + "' and nzp_user = " + finder.nzp_user + ")";
//                    ret = ExecSQL(con_db, tr_id, sql, true);

//                    if (ret.result)
//                    {
//                        sql =
//                            " delete from " + Points.Pref + DBManager.sUploadAliasRest + "file_pack " +
//                            " where nzp_file in " +
//                            " (select nzp_file from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
//                        " where pref = '" + finder.bank + "' and nzp_user = " + finder.nzp_user + ")";
//                        ret = ExecSQL(con_db, tr_id, sql, true);
//                    }

//                    #endregion

//                    #region 19 arch_file_paramsdom
//                    sql =
//                        " insert into " + Points.Pref + DBManager.sUploadAliasRest + "arch_file_paramsdom " +
//                        " (id, id_dom, id_prm, val_prm, nzp_dom, nzp_file)" +
//                        " select id, id_dom, id_prm, val_prm, nzp_dom, nzp_file" +
//                        " from " + Points.Pref + DBManager.sUploadAliasRest + "file_paramsdom  " +
//                        " where nzp_file in " +
//                        " (select nzp_file from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
//                        " where pref = '" + finder.bank + "' and nzp_user = " + finder.nzp_user + ")";
//                    ret = ExecSQL(con_db, tr_id, sql, true);

//                    if (ret.result)
//                    {
//                        sql =
//                            " delete from " + Points.Pref + DBManager.sUploadAliasRest + "file_paramsdom " +
//                            " where nzp_file in " +
//                            " (select nzp_file from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
//                        " where pref = '" + finder.bank + "' and nzp_user = " + finder.nzp_user + ")";
//                        ret = ExecSQL(con_db, tr_id, sql, true);
//                    }

//                    #endregion

//                    #region 19 arch_file_paramsls
//                    sql =
//                        " insert into " + Points.Pref + DBManager.sUploadAliasRest + "arch_file_paramsls " +
//                        " (id, ls_id, id_prm, val_prm, num_ls, nzp_file)" +
//                        " select id, ls_id, id_prm, val_prm, num_ls, nzp_file" +
//                        " from " + Points.Pref + DBManager.sUploadAliasRest + "file_paramsls  " +
//                        " where nzp_file in " +
//                        " (select nzp_file from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
//                        " where pref = '" + finder.bank + "' and nzp_user = " + finder.nzp_user + ")";
//                    ret = ExecSQL(con_db, tr_id, sql, true);

//                    if (ret.result)
//                    {
//                        sql =
//                            " delete from " + Points.Pref + DBManager.sUploadAliasRest + "file_paramsls " +
//                            " where nzp_file in " +
//                            " (select nzp_file from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
//                        " where pref = '" + finder.bank + "' and nzp_user = " + finder.nzp_user + ")";
//                        ret = ExecSQL(con_db, tr_id, sql, true);
//                    }

//                    #endregion

//                    #region 20 arch_file_serv
//                    sql =
//                        " insert into " + Points.Pref + DBManager.sUploadAliasRest + "arch_file_serv " +
//                        " (id, nzp_file, ls_id, supp_id, nzp_serv, sum_insaldo, eot, reg_tarif_percent," +
//                        " reg_tarif, nzp_measure, fact_rashod, norm_rashod, is_pu_calc, sum_nach, sum_reval," +
//                        " sum_subsidy, sum_subsidyp, sum_lgota, sum_lgotap, sum_smo, sum_smop, sum_money," +
//                        " is_del, sum_outsaldo, servp_row_number, nzp_kvar, nzp_supp)" +
//                        " select id, nzp_file, ls_id, supp_id, nzp_serv, sum_insaldo, eot, reg_tarif_percent," +
//                        " reg_tarif, nzp_measure, fact_rashod, norm_rashod, is_pu_calc, sum_nach, sum_reval," +
//                        " sum_subsidy, sum_subsidyp, sum_lgota, sum_lgotap, sum_smo, sum_smop, sum_money," +
//                        " is_del, sum_outsaldo, servp_row_number, nzp_kvar, nzp_supp" +
//                        " from " + Points.Pref + DBManager.sUploadAliasRest + "file_serv  " +
//                        " where nzp_file in " +
//                        " (select nzp_file from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
//                        " where pref = '" + finder.bank + "' and nzp_user = " + finder.nzp_user + ")";
//                    ret = ExecSQL(con_db, tr_id, sql, true);

//                    if (ret.result)
//                    {
//                        sql =
//                            " delete from " + Points.Pref + DBManager.sUploadAliasRest + "file_serv " +
//                            " where nzp_file in " +
//                            " (select nzp_file from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
//                        " where pref = '" + finder.bank + "' and nzp_user = " + finder.nzp_user + ")";
//                        ret = ExecSQL(con_db, tr_id, sql, true);
//                    }

//                    #endregion

//                    #region 21 arch_file_services
//                    sql =
//                        " insert into " + Points.Pref + DBManager.sUploadAliasRest + "arch_file_services " +
//                        " (id, id_serv, service, service2, nzp_file, nzp_measure, ed_izmer, type_serv, nzp_serv)" +
//                        " select id, id_serv, service, service2, nzp_file, nzp_measure, ed_izmer, type_serv," +
//                        " nzp_serv" +
//                        " from " + Points.Pref + DBManager.sUploadAliasRest + "file_services  " +
//                        " where nzp_file in " +
//                        " (select nzp_file from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
//                        " where pref = '" + finder.bank + "' and nzp_user = " + finder.nzp_user + ")";
//                    ret = ExecSQL(con_db, tr_id, sql, true);

//                    if (ret.result)
//                    {
//                        sql =
//                            " delete from " + Points.Pref + DBManager.sUploadAliasRest + "file_services " +
//                            " where nzp_file in " +
//                            " (select nzp_file from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
//                        " where pref = '" + finder.bank + "' and nzp_user = " + finder.nzp_user + ")";
//                        ret = ExecSQL(con_db, tr_id, sql, true);
//                    }

//                    #endregion

//                    #region 22 arch_file_servls
//                    sql =
//                        " insert into " + Points.Pref + DBManager.sUploadAliasRest + "arch_file_servls " +
//                        " (id, nzp_file, ls_id, id_serv, dat_start, dat_stop, supp_id)" +
//                        " select id, nzp_file, ls_id, id_serv, dat_start, dat_stop, supp_id" +
//                        " from " + Points.Pref + DBManager.sUploadAliasRest + "file_servls  " +
//                        " where nzp_file in " +
//                        " (select nzp_file from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
//                        " where pref = '" + finder.bank + "' and nzp_user = " + finder.nzp_user + ")";
//                    ret = ExecSQL(con_db, tr_id, sql, true);

//                    if (ret.result)
//                    {
//                        sql =
//                            " delete from " + Points.Pref + DBManager.sUploadAliasRest + "file_servls " +
//                            " where nzp_file in " +
//                            " (select nzp_file from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
//                        " where pref = '" + finder.bank + "' and nzp_user = " + finder.nzp_user + ")";
//                        ret = ExecSQL(con_db, tr_id, sql, true);
//                    }

//                    #endregion

//                    #region 23 arch_file_servp
//                    sql =
//                        " insert into " + Points.Pref + DBManager.sUploadAliasRest + "arch_file_servp " +
//                        " (id, nzp_file, reval_month, ls_id, supp_id, nzp_serv, eot, reg_tarif_percent," +
//                        " reg_tarif, nzp_measure, fact_rashod, norm_rashod, is_pu_calc, sum_reval," +
//                        " sum_subsidyp, sum_lgotap, sum_smop, nzp_kvar, nzp_supp)" +
//                        " select id, nzp_file, reval_month, ls_id, supp_id, nzp_serv, eot," +
//                        " reg_tarif_percent, reg_tarif, nzp_measure, fact_rashod, norm_rashod," +
//                        " is_pu_calc, sum_reval, sum_subsidyp, sum_lgotap, sum_smop, nzp_kvar, nzp_supp" +
//                        " from " + Points.Pref + DBManager.sUploadAliasRest + "file_servp  " +
//                        " where nzp_file in " +
//                        " (select nzp_file from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
//                        " where pref = '" + finder.bank + "' and nzp_user = " + finder.nzp_user + ")";
//                    ret = ExecSQL(con_db, tr_id, sql, true);

//                    if (ret.result)
//                    {
//                        sql =
//                            " delete from " + Points.Pref + DBManager.sUploadAliasRest + "file_servp " +
//                            " where nzp_file in " +
//                            " (select nzp_file from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
//                        " where pref = '" + finder.bank + "' and nzp_user = " + finder.nzp_user + ")";
//                        ret = ExecSQL(con_db, tr_id, sql, true);
//                    }

//                    #endregion

//                    #region 24 arch_file_supp
//                    sql =
//                        " insert into " + Points.Pref + DBManager.sUploadAliasRest + "arch_file_supp " +
//                        " (id, nzp_file, supp_id, supp_name, jur_address, fact_address, inn, kpp, rs, bank," +
//                        " bik, ks, nzp_supp)" +
//                        " select id, nzp_file, supp_id, supp_name, jur_address, fact_address, inn, kpp, rs," +
//                        " bank, bik, ks, nzp_supp" +
//                        " from " + Points.Pref + DBManager.sUploadAliasRest + "file_supp  " +
//                        " where nzp_file in " +
//                        " (select nzp_file from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
//                        " where pref = '" + finder.bank + "' and nzp_user = " + finder.nzp_user + ")";
//                    ret = ExecSQL(con_db, tr_id, sql, true);

//                    if (ret.result)
//                    {
//                        sql =
//                            " delete from " + Points.Pref + DBManager.sUploadAliasRest + "file_supp " +
//                            " where nzp_file in " +
//                            " (select nzp_file from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
//                        " where pref = '" + finder.bank + "' and nzp_user = " + finder.nzp_user + ")";
//                        ret = ExecSQL(con_db, tr_id, sql, true);
//                    }

//                    #endregion

//                    #region 25 arch_file_typenedopost
//                    sql =
//                        " insert into " + Points.Pref + DBManager.sUploadAliasRest + "arch_file_typenedopost " +
//                        " (id, nzp_file, type_ned, ned_name)" +
//                        " select id, nzp_file, type_ned, ned_name" +
//                        " from " + Points.Pref + DBManager.sUploadAliasRest + "file_typenedopost  " +
//                        " where nzp_file in " +
//                        " (select nzp_file from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
//                        " where pref = '" + finder.bank + "' and nzp_user = " + finder.nzp_user + ")";
//                    ret = ExecSQL(con_db, tr_id, sql, true);

//                    if (ret.result)
//                    {
//                        sql =
//                            " delete from " + Points.Pref + DBManager.sUploadAliasRest + "file_typenedopost " +
//                            " where nzp_file in " +
//                            " (select nzp_file from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
//                        " where pref = '" + finder.bank + "' and nzp_user = " + finder.nzp_user + ")";
//                        ret = ExecSQL(con_db, tr_id, sql, true);
//                    }

//                    #endregion

//                    #region 26 arch_file_typeparams
//                    sql =
//                        " insert into " + Points.Pref + DBManager.sUploadAliasRest + "arch_file_typeparams " +
//                        " (id, id_prm, prm_name, level_, type_prm, nzp_file, nzp_prm)" +
//                        " select id, id_prm, prm_name, level_, type_prm, nzp_file, nzp_prm" +
//                        " from " + Points.Pref + DBManager.sUploadAliasRest + "file_typeparams  " +
//                        " where nzp_file in " +
//                        " (select nzp_file from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
//                        " where pref = '" + finder.bank + "' and nzp_user = " + finder.nzp_user + ")";
//                    ret = ExecSQL(con_db, tr_id, sql, true);

//                    if (ret.result)
//                    {
//                        sql =
//                            " delete from " + Points.Pref + DBManager.sUploadAliasRest + "file_typeparams " +
//                            " where nzp_file in " +
//                            " (select nzp_file from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
//                        " where pref = '" + finder.bank + "' and nzp_user = " + finder.nzp_user + ")";
//                        ret = ExecSQL(con_db, tr_id, sql, true);
//                    }

//                    #endregion

//                    #region 27 arch_file_urlic
//                    sql =
//                        " insert into " + Points.Pref + DBManager.sUploadAliasRest + "arch_file_urlic " +
//                        " (id, nzp_file, supp_id, supp_name, jur_address, fact_address, inn, kpp, rs, bank," +
//                        " bik_bank, ks, tel_chief, tel_b, chief_name, chief_post, b_name, okonh1, okonh2, okpo," +
//                        " bank_pr, bank_adr, bik, rs_pr, ks_pr, post_and_name, nzp_supp)" +
//                        " select id, nzp_file, supp_id, supp_name, jur_address, fact_address, inn, kpp, rs," +
//                        " bank, bik_bank, ks, tel_chief, tel_b, chief_name, chief_post, b_name, okonh1, okonh2," +
//                        " okpo, bank_pr, bank_adr, bik, rs_pr, ks_pr, post_and_name, nzp_supp" +
//                        " from " + Points.Pref + DBManager.sUploadAliasRest + "file_urlic " +
//                        " where nzp_file in " +
//                        " (select nzp_file from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
//                        " where pref = '" + finder.bank + "' and nzp_user = " + finder.nzp_user + ")";
//                    ret = ExecSQL(con_db, tr_id, sql, true);

//                    if (ret.result)
//                    {
//                        sql =
//                            " delete from " + Points.Pref + DBManager.sUploadAliasRest + "file_urlic " +
//                            " where nzp_file in " +
//                            " (select nzp_file from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
//                        " where pref = '" + finder.bank + "' and nzp_user = " + finder.nzp_user + ")";
//                        ret = ExecSQL(con_db, tr_id, sql, true);
//                    }

//                    #endregion

//                    #region 28 arch_file_voda
//                    sql =
//                        " insert into " + Points.Pref + DBManager.sUploadAliasRest + "arch_file_voda " +
//                        " (id, id_prm, name, nzp_file, nzp_prm)" +
//                        " select id, id_prm, name, nzp_file, nzp_prm" +
//                        " from " + Points.Pref + DBManager.sUploadAliasRest + "file_voda " +
//                        " where nzp_file in " +
//                        " (select nzp_file from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
//                        " where pref = '" + finder.bank + "' and nzp_user = " + finder.nzp_user + ")";
//                    ret = ExecSQL(con_db, tr_id, sql, true);

//                    if (ret.result)
//                    {
//                        sql =
//                            " delete from " + Points.Pref + DBManager.sUploadAliasRest + "file_voda " +
//                            " where nzp_file in " +
//                            " (select nzp_file from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
//                        " where pref = '" + finder.bank + "' and nzp_user = " + finder.nzp_user + ")";
//                        ret = ExecSQL(con_db, tr_id, sql, true);
//                    }

//                    #endregion

//                    #region 29 arch_file_vrub
//                    sql =
//                        " insert into " + Points.Pref + DBManager.sUploadAliasRest + "arch_file_vrub " +
//                        " (id, nzp_file, ls_id, gil_id, dat_vrvib, dat_end)" +
//                        " select id, nzp_file, ls_id, gil_id, dat_vrvib, dat_end" +
//                        " from " + Points.Pref + DBManager.sUploadAliasRest + "file_vrub " +
//                        " where nzp_file in " +
//                        " (select nzp_file from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
//                        " where pref = '" + finder.bank + "' and nzp_user = " + finder.nzp_user + ")";
//                    ret = ExecSQL(con_db, tr_id, sql, true);

//                    if (ret.result)
//                    {
//                        sql =
//                            " delete from " + Points.Pref + DBManager.sUploadAliasRest + "file_vrub " +
//                            " where nzp_file in " +
//                            " (select nzp_file from " + Points.Pref + DBManager.sUploadAliasRest + "files_selected" +
//                        " where pref = '" + finder.bank + "' and nzp_user = " + finder.nzp_user + ")";
//                        ret = ExecSQL(con_db, tr_id, sql, true);
//                    }

//                    #endregion

//                }
//                catch (Exception ex )
//                {
//                    tr_id.Rollback();
//                    MonitorLog.WriteException("Ошибка функции PutFileInArchive ", ex);
//                    ret.result = false;
//                    ret.text = "Ошибка добавления файла в архив";
//                    ret.tag = -1;
//                    return ret;
//                }

//                #endregion

//            }
//            catch (Exception ex)
//            {
//                MonitorLog.WriteException("Ошибка функции PutFileInArchive ", ex);
//                ret.result = false;
//                ret.text = "Ошибка добавления файла в архив";
//                ret.tag = -1;
//                return ret;
//            }
//            finally
//            {
//                con_db.Close();
//            }

//            return ret;
//        }
    }
}
