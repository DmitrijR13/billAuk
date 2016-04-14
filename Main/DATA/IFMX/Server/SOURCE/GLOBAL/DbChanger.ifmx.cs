using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;


namespace STCLINE.KP50.DataBase
{
    //----------------------------------------------------------------------
    public partial class DbChanger : DataBaseHead
    //----------------------------------------------------------------------
    {
        List<_Point> points = null;
        RecordMonth calcMonth;
        Dictionary<int, RecordMonth> calcMonthAreas;
        
        public void SetUpdates(out Returns ret)
        {
            IDbConnection conn_db = GetConnection(Constants.cons_Kernel);
            ret = OpenDb(conn_db, true);
            if (!ret.result) return;

            try
            {
                ret = LoadPoints(conn_db);
                if (!ret.result)
                {
                    conn_db.Close();
                    return;
                }
                CreateCalcMethod(conn_db);
                Pack_0001(conn_db, out ret);
                Pack_0002(conn_db, out ret);
                Pack_0004_charge_cnts(conn_db, out ret);
                Pack_0005_counters_vals(conn_db, out ret);
                Pack_0006_alter_data_kernel_tables(conn_db, out ret);
                Pack_0007_pack_status(conn_db, out ret);
                Pack_0008_payers_and_banks(conn_db, out ret);
                Pack_0010_s_rajon_dom(conn_db, out ret);
                Pack_0011_series_geu(conn_db, out ret);
                Pack_0012_series_area(conn_db, out ret);
                Pack_0013_2_fin_procedures(conn_db, out ret);
                Pack_0014_system_params(conn_db, out ret);
                Pack_0015_sprav_procs(conn_db, out ret);
                Pack_0016_error_types(conn_db, out ret);
                //Pack_0017_alter_fin_tables(conn_db, out ret);
                Pack_0018_frm_descr(conn_db, out ret);

                Pack_0020_servpriority(conn_db, out ret);
                Pack_0021_checkchmon(conn_db, out ret);
                Pack_0022_sys_events(conn_db, out ret);
                Pack_0023_alter_charge_tables(conn_db, out ret);
                UpdatesDogovor(conn_db, out ret);

#if PG
                ExecSQL(conn_db, "set search_path to '" + Points.Pref + "_kernel'", true);
#else
                ExecSQL(conn_db, "database " + Points.Pref + "_kernel", true);
#endif
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
            finally
            {
                conn_db.Close();
            }
        }

        public void SetAfterPointLoadUpdates(out Returns ret)
        {
            ret = Utils.InitReturns();
            //var connection = GetConnection(Constants.cons_Kernel);
            //ret = OpenDb(connection, true);
            //if (!ret.result) return;

            //try
            //{
            //    ret = after01_kart(connection);
            //    SelectDatabaseOrSchema(connection, Points.Pref + "_kernel");
            //}
            //catch (Exception ex)
            //{
            //    throw new Exception(ex.Message, ex);
            //}
            //finally
            //{
            //    connection.Close();
            //}
        }

        private Returns LoadPoints(IDbConnection connection)
        {
            points = new List<_Point>();

            DbSprav db = new DbSprav();
            Returns ret;
            calcMonth = db.GetCalcMonth(connection, null, out ret);
            if (!ret.result) return ret;
                     
            if (GlobalSettings.WorkOnlyWithCentralBank)
            {
                //_Point zap = new _Point();
                //zap.nzp_wp = Points.Point.nzp_wp;
                //zap.flag = Points.Point.flag;
                //zap.point = Points.Point.point;
                //zap.pref = Points.Point.pref;
                //zap.ol_server = Points.Point.ol_server;
                //zap.nzp_server = Points.Point.nzp_server;
                //points.Add(zap);
            }
            else
            {
                bool b_yes_server = isTableHasColumn(connection, "s_point", "nzp_server");
#if PG
                ret = ExecSQL(connection, "set search_path to '" + Points.Pref + "_kernel'", true);
#else
                ret = ExecSQL(connection, "database " + Points.Pref + "_kernel", true);
#endif
                if (!ret.result) return ret;

                IDataReader reader;
                if (TempTableInWebCashe(connection, "s_point"))
                {
                    //заполнить список локальных банков данных
                    ret = ExecRead(connection, out reader,
                        " Select * From s_point Where nzp_graj > 0 Order by n", false);
                    if (ret.result)
                    {
                        while (reader.Read())
                        {
                            try
                            {
                                _Point zap = new _Point();
                                zap.nzp_wp = (int)reader["nzp_wp"];
                                zap.flag = (int)reader["flag"];
                                zap.point = ((string)reader["point"]).Trim();
                                zap.pref = ((string)reader["bd_kernel"]).Trim();
                                zap.ol_server = "";

                                if (b_yes_server)
                                {
                                    zap.nzp_server = (int)reader["nzp_server"];
                                    if (reader["bd_old"] != DBNull.Value)
                                        zap.ol_server = (string)reader["bd_old"];
                                }
                                else
                                    zap.nzp_server = -1;

                                points.Add(zap);
                            }
                            catch { }
                        }
                        reader.Close();
                    }
                }
            }
            return ret;
        }

        private void Pack_0022_sys_events(IDbConnection conn_db, out Returns ret)
        {
            ret = Utils.InitReturns();
#if PG
            if (!TempTableInWebCashe(conn_db, Points.Pref + "_data.sys_events"))
            {
                ret = ExecSQL(conn_db, "set search_path to '" + Points.Pref + "_data'", true);

                if (!ret.result) return;
                // sys_event - таблица событий в системе которая позволит сделать разбор событий
                string sql_text = "create table sys_events(" +
                                  " NZP_EVENT       serial NOT NULL, "+
                                  " DATE_           timestamp default current_timestamp year to fraction , "+
                                  " NZP_USER        integer,  "+
                                  " NZP_DICT_EVENT  integer,  "+
                                  " NZP             integer,  "+
                                  " NOTE            CHAR(200) "+
                                  " ) ";

                ret = ExecSQL(conn_db, sql_text, true);


                if (!ret.result) return;

                ExecSQL(conn_db, "CREATE INDEX ix_sys_events01 ON sys_events(NZP_USER , DATE_ , NZP)", true);

                
                ExecSQL(conn_db, "CREATE INDEX ix_sys_events02 ON sys_events(DATE_ , NZP, nzp_user)", true);

                
                ExecSQL(conn_db, "CREATE INDEX ix_sys_events03 ON sys_events( NZP)", true);

                ret = ExecSQL(conn_db, sql_text, true);
                                sql_text = "create table SYS_DICTIONARY_VALUES(" +
                        "    NZP_DICT         integer,     " +
                        "    NAME             char(200) NOT NULL , " +
                        "    NZP_DICT_PARENT  integer,    " +
                        "    NZP_TDICT        INTEGER NOT NULL,  " +
                        "    CODE             VARCHAR(20),     " +
                        "    NOTE             VARCHAR(150) " +
                        "    ) ";
                ret = ExecSQL(conn_db, sql_text, true);
#else
            if (!TempTableInWebCashe(conn_db, Points.Pref + "_data:sys_events"))
            {
                ret = ExecSQL(conn_db, "database " + Points.Pref + "_data", true);

                if (!ret.result) return;

                 // sys_event - таблица событий в системе которая позволит сделать разбор событий
                string sql_text = "create table \"are\".sys_events(" +
                                  " NZP_EVENT       serial NOT NULL, " +
                                  " DATE_           datetime year to fraction default current year to fraction , " +
                                  " NZP_USER        integer,  " +
                                  " NZP_DICT_EVENT  integer,  " +
                                  " NZP             integer,  " +
                                  " NOTE            CHAR(200) " +
                                  " ) ";

                ret = ExecSQL(conn_db, sql_text, true);


                if (!ret.result) return;

                ExecSQL(conn_db, "CREATE INDEX \"are\".ix_sys_events01 ON \"are\".sys_events(NZP_USER , DATE_ , NZP)", true);

                
                ExecSQL(conn_db, "CREATE INDEX \"are\".ix_sys_events02 ON \"are\".sys_events(DATE_ , NZP, nzp_user)", true);

                
                ExecSQL(conn_db, "CREATE INDEX \"are\".ix_sys_events03 ON \"are\".sys_events( NZP)", true);

                ret = ExecSQL(conn_db, sql_text, true);
                                sql_text = "create table \"are\".SYS_DICTIONARY_VALUES(" +
                        "    NZP_DICT         integer,     " +
                        "    NAME             char(200) NOT NULL , " +
                        "    NZP_DICT_PARENT  integer,    " +
                        "    NZP_TDICT        INTEGER NOT NULL,  " +
                        "    CODE             VARCHAR(20),     " +
                        "    NOTE             VARCHAR(150) " +
                        "    ) ";
                ret = ExecSQL(conn_db, sql_text, true);
#endif

                // типы событий
                
                sql_text = "INSERT INTO SYS_DICTIONARY_VALUES (NZP_DICT, NAME, NZP_DICT_PARENT, NZP_TDICT, CODE, NOTE)      VALUES (7428, 'Изменение расчётного месяца', NULL, 101, NULL, NULL);                     "; 
                ret = ExecSQL(conn_db, sql_text, true);
                sql_text = "INSERT INTO SYS_DICTIONARY_VALUES (NZP_DICT, NAME, NZP_DICT_PARENT, NZP_TDICT, CODE, NOTE)      VALUES (7429, 'Формирование договора с квартиросъёмщиком', NULL, 101, NULL, NULL);       "; 
                ret = ExecSQL(conn_db, sql_text, true);
                sql_text = "INSERT INTO SYS_DICTIONARY_VALUES (NZP_DICT, NAME, NZP_DICT_PARENT, NZP_TDICT, CODE, NOTE)      VALUES (7430, 'Формирование счёта на оплату по лицевому счёту', NULL, 101, NULL, NULL);";
                ret = ExecSQL(conn_db, sql_text, true);
                sql_text = "INSERT INTO SYS_DICTIONARY_VALUES (NZP_DICT, NAME, NZP_DICT_PARENT, NZP_TDICT, CODE, NOTE)      VALUES (7800, 'Печать справки (документа)', NULL, 101, NULL, NULL); ";
                ret = ExecSQL(conn_db, sql_text, true);
                sql_text = "INSERT INTO SYS_DICTIONARY_VALUES (NZP_DICT, NAME, NZP_DICT_PARENT, NZP_TDICT, CODE, NOTE)      VALUES (8214, 'Изменение адреса лицевого счёта', NULL, 101, NULL, NULL); ";
                ret = ExecSQL(conn_db, sql_text, true);
                sql_text = "INSERT INTO SYS_DICTIONARY_VALUES (NZP_DICT, NAME, NZP_DICT_PARENT, NZP_TDICT, CODE, NOTE)      VALUES (8215, 'Изменение адреса дома', NULL, 101, NULL, NULL);";
                ret = ExecSQL(conn_db, sql_text, true);
                sql_text = "INSERT INTO SYS_DICTIONARY_VALUES (NZP_DICT, NAME, NZP_DICT_PARENT, NZP_TDICT, CODE, NOTE)      VALUES (8216, 'Изменение ФИО ответственного квартиросъемщика', NULL, 101, NULL, NULL); ";
                ret = ExecSQL(conn_db, sql_text, true);
                sql_text = "INSERT INTO SYS_DICTIONARY_VALUES (NZP_DICT, NAME, NZP_DICT_PARENT, NZP_TDICT, CODE, NOTE)      VALUES (6479, 'Запуск программы', NULL, 101, NULL, NULL);                                "; ret = ExecSQL(conn_db, sql_text, true);
                sql_text = "INSERT INTO SYS_DICTIONARY_VALUES (NZP_DICT, NAME, NZP_DICT_PARENT, NZP_TDICT, CODE, NOTE)      VALUES (6480, 'Закрытие программы', NULL, 101, NULL, NULL);                              "; ret = ExecSQL(conn_db, sql_text, true);
                sql_text = "INSERT INTO SYS_DICTIONARY_VALUES (NZP_DICT, NAME, NZP_DICT_PARENT, NZP_TDICT, CODE, NOTE)      VALUES (6481, 'Добавление лицевого счёта', NULL, 101, NULL, NULL);                       "; ret = ExecSQL(conn_db, sql_text, true);
                sql_text = "INSERT INTO SYS_DICTIONARY_VALUES (NZP_DICT, NAME, NZP_DICT_PARENT, NZP_TDICT, CODE, NOTE)      VALUES (6482, 'Удаление лицевого счёта', NULL, 101, NULL, NULL);                         "; ret = ExecSQL(conn_db, sql_text, true);
                sql_text = "INSERT INTO SYS_DICTIONARY_VALUES (NZP_DICT, NAME, NZP_DICT_PARENT, NZP_TDICT, CODE, NOTE)      VALUES (6483, 'Закрытие лицевого счёта', NULL, 101, NULL, NULL);                         "; ret = ExecSQL(conn_db, sql_text, true);
                sql_text = "INSERT INTO SYS_DICTIONARY_VALUES (NZP_DICT, NAME, NZP_DICT_PARENT, NZP_TDICT, CODE, NOTE)      VALUES (6484, 'Добавление дома', NULL, 101, NULL, NULL);                                 "; ret = ExecSQL(conn_db, sql_text, true);
                sql_text = "INSERT INTO SYS_DICTIONARY_VALUES (NZP_DICT, NAME, NZP_DICT_PARENT, NZP_TDICT, CODE, NOTE)      VALUES (6485, 'Удаление дома', NULL, 101, NULL, NULL);                                   "; ret = ExecSQL(conn_db, sql_text, true);
                sql_text = "INSERT INTO SYS_DICTIONARY_VALUES (NZP_DICT, NAME, NZP_DICT_PARENT, NZP_TDICT, CODE, NOTE)      VALUES (6486, 'Добавление оплаты', NULL, 101, NULL, NULL);                               "; ret = ExecSQL(conn_db, sql_text, true);
                sql_text = "INSERT INTO SYS_DICTIONARY_VALUES (NZP_DICT, NAME, NZP_DICT_PARENT, NZP_TDICT, CODE, NOTE)      VALUES (6487, 'Удаление оплаты', NULL, 101, NULL, NULL);                                 "; ret = ExecSQL(conn_db, sql_text, true);
                sql_text = "INSERT INTO SYS_DICTIONARY_VALUES (NZP_DICT, NAME, NZP_DICT_PARENT, NZP_TDICT, CODE, NOTE)      VALUES (6488, 'Изменение оплаты', NULL, 101, NULL, NULL);                                "; ret = ExecSQL(conn_db, sql_text, true);
                sql_text = "INSERT INTO SYS_DICTIONARY_VALUES (NZP_DICT, NAME, NZP_DICT_PARENT, NZP_TDICT, CODE, NOTE)      VALUES (6489, 'Добавление показания ПУ', NULL, 101, NULL, NULL);                         "; ret = ExecSQL(conn_db, sql_text, true);
                sql_text = "INSERT INTO SYS_DICTIONARY_VALUES (NZP_DICT, NAME, NZP_DICT_PARENT, NZP_TDICT, CODE, NOTE)      VALUES (6490, 'Удаление показания ПУ', NULL, 101, NULL, NULL);                           "; ret = ExecSQL(conn_db, sql_text, true);
                sql_text = "INSERT INTO SYS_DICTIONARY_VALUES (NZP_DICT, NAME, NZP_DICT_PARENT, NZP_TDICT, CODE, NOTE)      VALUES (6491, 'Изменение показания ПУ', NULL, 101, NULL, NULL);                          "; ret = ExecSQL(conn_db, sql_text, true);
                sql_text = "INSERT INTO SYS_DICTIONARY_VALUES (NZP_DICT, NAME, NZP_DICT_PARENT, NZP_TDICT, CODE, NOTE)      VALUES (6492, 'Добавление жильца', NULL, 101, NULL, NULL);                               "; ret = ExecSQL(conn_db, sql_text, true);
                sql_text = "INSERT INTO SYS_DICTIONARY_VALUES (NZP_DICT, NAME, NZP_DICT_PARENT, NZP_TDICT, CODE, NOTE)      VALUES (6493, 'Удаление жильца', NULL, 101, NULL, NULL);                                 "; ret = ExecSQL(conn_db, sql_text, true);
                sql_text = "INSERT INTO SYS_DICTIONARY_VALUES (NZP_DICT, NAME, NZP_DICT_PARENT, NZP_TDICT, CODE, NOTE)      VALUES (6494, 'Изменение карточки жильца', NULL, 101, NULL, NULL);                       "; ret = ExecSQL(conn_db, sql_text, true);
                sql_text = "INSERT INTO SYS_DICTIONARY_VALUES (NZP_DICT, NAME, NZP_DICT_PARENT, NZP_TDICT, CODE, NOTE)      VALUES (6495, 'Изменение карточки лицевого счёта', NULL, 101, NULL, NULL);               "; ret = ExecSQL(conn_db, sql_text, true);
                sql_text = "INSERT INTO SYS_DICTIONARY_VALUES (NZP_DICT, NAME, NZP_DICT_PARENT, NZP_TDICT, CODE, NOTE)      VALUES (6595, 'Расчёт начислений', NULL, 101, NULL, NULL);                               "; ret = ExecSQL(conn_db, sql_text, true);
                sql_text = "INSERT INTO SYS_DICTIONARY_VALUES (NZP_DICT, NAME, NZP_DICT_PARENT, NZP_TDICT, CODE, NOTE)      VALUES (6596, 'Получение отчёта', NULL, 101, NULL, NULL);                                "; ret = ExecSQL(conn_db, sql_text, true);
                sql_text = "INSERT INTO SYS_DICTIONARY_VALUES (NZP_DICT, NAME, NZP_DICT_PARENT, NZP_TDICT, CODE, NOTE)      VALUES (6597, 'Добавление перекидки', NULL, 101, NULL, NULL);                            "; ret = ExecSQL(conn_db, sql_text, true);
                sql_text = "INSERT INTO SYS_DICTIONARY_VALUES (NZP_DICT, NAME, NZP_DICT_PARENT, NZP_TDICT, CODE, NOTE)      VALUES (6598, 'Удаление перекидки', NULL, 101, NULL, NULL);                              "; ret = ExecSQL(conn_db, sql_text, true);
                sql_text = "INSERT INTO SYS_DICTIONARY_VALUES (NZP_DICT, NAME, NZP_DICT_PARENT, NZP_TDICT, CODE, NOTE)      VALUES (6599, 'Изменение перекидки', NULL, 101, NULL, NULL);                             "; ret = ExecSQL(conn_db, sql_text, true);
                sql_text = "INSERT INTO SYS_DICTIONARY_VALUES (NZP_DICT, NAME, NZP_DICT_PARENT, NZP_TDICT, CODE, NOTE)      VALUES (6600, 'Добавление перерасчёта', NULL, 101, NULL, NULL);                          "; ret = ExecSQL(conn_db, sql_text, true);
                sql_text = "INSERT INTO SYS_DICTIONARY_VALUES (NZP_DICT, NAME, NZP_DICT_PARENT, NZP_TDICT, CODE, NOTE)      VALUES (6601, 'Удаление перерасчёта', NULL, 101, NULL, NULL);                            "; ret = ExecSQL(conn_db, sql_text, true);
                sql_text = "INSERT INTO SYS_DICTIONARY_VALUES (NZP_DICT, NAME, NZP_DICT_PARENT, NZP_TDICT, CODE, NOTE)      VALUES (6602, 'Добавление недопоставки', NULL, 101, NULL, NULL);                         "; ret = ExecSQL(conn_db, sql_text, true);
                sql_text = "INSERT INTO SYS_DICTIONARY_VALUES (NZP_DICT, NAME, NZP_DICT_PARENT, NZP_TDICT, CODE, NOTE)      VALUES (6603, 'Удаление недопоставки', NULL, 101, NULL, NULL);                           "; ret = ExecSQL(conn_db, sql_text, true);
                sql_text = "INSERT INTO SYS_DICTIONARY_VALUES (NZP_DICT, NAME, NZP_DICT_PARENT, NZP_TDICT, CODE, NOTE)      VALUES (6604, 'Изменение недопоставки', NULL, 101, NULL, NULL);                          "; ret = ExecSQL(conn_db, sql_text, true);
                sql_text = "INSERT INTO SYS_DICTIONARY_VALUES (NZP_DICT, NAME, NZP_DICT_PARENT, NZP_TDICT, CODE, NOTE)      VALUES (6605, 'Просмотр карточки лицевого счёта', NULL, 101, NULL, NULL);                "; ret = ExecSQL(conn_db, sql_text, true);
                sql_text = "INSERT INTO SYS_DICTIONARY_VALUES (NZP_DICT, NAME, NZP_DICT_PARENT, NZP_TDICT, CODE, NOTE)      VALUES (6606, 'Просмотр карточки дома', NULL, 101, NULL, NULL);                          "; ret = ExecSQL(conn_db, sql_text, true);
                sql_text = "INSERT INTO SYS_DICTIONARY_VALUES (NZP_DICT, NAME, NZP_DICT_PARENT, NZP_TDICT, CODE, NOTE)      VALUES (6607, 'Просмотр карточки жильца', NULL, 101, NULL, NULL);                        "; ret = ExecSQL(conn_db, sql_text, true);
                sql_text = "INSERT INTO SYS_DICTIONARY_VALUES (NZP_DICT, NAME, NZP_DICT_PARENT, NZP_TDICT, CODE, NOTE)      VALUES (6608, 'Открытие услуги', NULL, 101, NULL, NULL);                                 "; ret = ExecSQL(conn_db, sql_text, true);
                sql_text = "INSERT INTO SYS_DICTIONARY_VALUES (NZP_DICT, NAME, NZP_DICT_PARENT, NZP_TDICT, CODE, NOTE)      VALUES (6609, 'Закрытие услуги', NULL, 101, NULL, NULL);                                 "; ret = ExecSQL(conn_db, sql_text, true);
                sql_text = "INSERT INTO SYS_DICTIONARY_VALUES (NZP_DICT, NAME, NZP_DICT_PARENT, NZP_TDICT, CODE, NOTE)      VALUES (6610, 'Выполнение групповой операции', NULL, 101, NULL, NULL);                   "; ret = ExecSQL(conn_db, sql_text, true);
                sql_text = "INSERT INTO SYS_DICTIONARY_VALUES (NZP_DICT, NAME, NZP_DICT_PARENT, NZP_TDICT, CODE, NOTE)      VALUES (6611, 'Распределение оплат', NULL, 101, NULL, NULL);                             "; 
                ret = ExecSQL(conn_db, sql_text, true);
                sql_text = "INSERT INTO SYS_DICTIONARY_VALUES (NZP_DICT, NAME, NZP_DICT_PARENT, NZP_TDICT, CODE, NOTE)      VALUES (6612, 'Отмена распределения оплат', NULL, 101, NULL, NULL);                      "; 
                ret = ExecSQL(conn_db, sql_text, true);
                sql_text = "INSERT INTO SYS_DICTIONARY_VALUES (NZP_DICT, NAME, NZP_DICT_PARENT, NZP_TDICT, CODE, NOTE)      VALUES (6637, 'Подтверждение параметров лицевого счёта', NULL, 101, NULL, NULL); "; 
                ret = ExecSQL(conn_db, sql_text, true);
                sql_text = "INSERT INTO SYS_DICTIONARY_VALUES (NZP_DICT, NAME, NZP_DICT_PARENT, NZP_TDICT, CODE, NOTE)      VALUES (6638, 'Снятие признака о подтверждении параметров лицевого счёта', NULL, 101, NULL,ret = ExecSQL(conn_db, sql_text, true); NULL); ";
                ret = ExecSQL(conn_db, sql_text, true);


            }
        }

        private void Pack_0021_checkchmon(IDbConnection conn_db, out Returns ret)
        {
            ret = Utils.InitReturns();

#if PG
            ret = ExecSQL(conn_db, "set search_path to '" + Points.Pref + "_data'", true);
            if (!ret.result) return;
            

            if (!TempTableInWebCashe(conn_db, Points.Pref + "_data.checkchmon"))
            {
                 string sql_text = "create table checkchmon("+
                                  " nzp_check SERIAL NOT NULL," +
                                  " dat_check DATE,"+
                                  " month_ INTEGER,"+
                                  " yearr INTEGER,"+
                                  " note CHAR(100),"+
                                  " nzp_grp INTEGER,"+
                                  " pref CHAR(30),"+ 
                                  " name_prov CHAR(40),"+
                                  " status_ INTEGER, " +
                                  " is_critical integer" +
                                  " )";

                ret = ExecSQL(conn_db, sql_text, true);
                ret = ExecSQL(conn_db, "CREATE INDEX ix_checkchmon ON checkchmon(month_, yearr, nzp_grp)", true);
                     
                if (!ret.result) return;
            }
#else
            ret = ExecSQL(conn_db, "database " + Points.Pref + "_data", true);
            if (!ret.result) return;
            

            if (!TempTableInWebCashe(conn_db, Points.Pref + "_data:checkchmon"))
            {
                string sql_text = "create table \"are\".checkchmon(" +
                                  " nzp_check SERIAL NOT NULL," +
                                 " dat_check DATE," +
                                 " month_ INTEGER," +
                                 " yearr INTEGER," +
                                 " note CHAR(100)," +
                                 " nzp_grp INTEGER," +
                                 " pref CHAR(30)," +
                                 " name_prov CHAR(40)," +
                                  " status_ INTEGER, " +
                                  " is_critical integer" +
                                  " )";

                ret = ExecSQL(conn_db, sql_text, true);
                ret = ExecSQL(conn_db, "CREATE INDEX \"are\".ix_checkchmon ON \"are\".checkchmon(month_, yearr, nzp_grp)", true);
                     
                if (!ret.result) return;
            }
#endif

            AddFieldToTable(conn_db, "checkchmon", "is_critical", "INTEGER");

            CreateReestrPerekidok(conn_db);
        }

        private void Pack_0020_servpriority(IDbConnection conn_db, out Returns ret)
        {
#if PG
            ret = ExecSQL(conn_db, "set search_path to '" + Points.Pref + "_kernel'", true);
            if (!ret.result) return;

            if (!TempTableInWebCashe(conn_db, Points.Pref + "_kernel.servpriority"))
            {
                
                string sql_text = "create table servpriority (" +
                    " nzp_key serial not null," +
                    " dat_s date," +
                    " dat_po date," +
                    " nzp_serv integer," +
                    " ordering integer," +
                    " nzp_user integer," +
                    " dat_when datetime year to minute" +
                    ")";
                ret = ExecSQL(conn_db, sql_text, true);
                if (!ret.result) return;

                ExecSQL(conn_db, "CREATE INDEX ix_servpriority ON servpriority(nzp_serv, ordering, dat_s, dat_po)", true);                
            }
#else
            ret = ExecSQL(conn_db, "database " + Points.Pref + "_kernel", true);
            if (!ret.result) return;

            if (!TempTableInWebCashe(conn_db, Points.Pref + "_kernel:servpriority"))
            {
                
                string sql_text = "create table \"are\".servpriority (" +
                    " nzp_key serial not null," +
                    " dat_s date," +
                    " dat_po date," +
                    " nzp_serv integer," +
                    " ordering integer," +
                    " nzp_user integer," +
                    " dat_when datetime year to minute" +
                    ")";
                ret = ExecSQL(conn_db, sql_text, true);
                if (!ret.result) return;

                ExecSQL(conn_db, "CREATE INDEX \"are\".ix_servpriority ON \"are\".servpriority(nzp_serv, ordering, dat_s, dat_po)", true);                
            }
#endif

            AddFieldToTable(conn_db, "services", "ordering_std", "integer");
        }

        private void Pack_0019_sprav(IDbConnection conn_db, out Returns ret)
        {
            ret = Utils.InitReturns();
            //todo: в некоторых центральных банках нет res_y с nzp_res = 9999
        }

        private void Pack_0018_frm_descr(IDbConnection conn_db, out Returns ret)
        {
            ret = Utils.InitReturns();

            string sql;

            foreach (_Point point in points)
            {
#if PG
                ret = ExecSQL(conn_db, "set search_path to '" + point.pref + "_kernel'", true);
                if (!ret.result) continue;

                if (!TempTableInWebCashe(conn_db, "frm_descr"))
                {
                    sql = "CREATE TABLE frm_descr(" +
                        " nzp_frm INTEGER," +
                        " prot_html lvarchar)";
                    ret = ExecSQL(conn_db, sql, true);
                    if (!ret.result) return;

                    ExecSQL(conn_db, "CREATE INDEX ix_frm_descr ON frm_descr(nzp_frm)", true);
#else
                ret = ExecSQL(conn_db, "database " + point.pref + "_kernel", true);
                if (!ret.result) continue;

                if (!TempTableInWebCashe(conn_db, "frm_descr"))
                {
                    sql = "CREATE TABLE \"are\".frm_descr(" +
                        " nzp_frm INTEGER," +
                        " prot_html \"informix\".lvarchar)";
                    ret = ExecSQL(conn_db, sql, true);
                    if (!ret.result) return;

                    ExecSQL(conn_db, "CREATE INDEX \"are\".ix_frm_descr ON \"are\".frm_descr(nzp_frm)", true);
#endif

                    sql = "insert into frm_descr (nzp_frm, prot_html)" +
                    " values (-1, '<html><head><meta http-equiv=Content-Type content=''text/html; charset=windows-1251''></head>" +
                        "<body lang=RU> <div class=Section1> <p class=MsoNormal>Месяц расчета - <font color=\"#0000FF\"><b>[calcmn] [calcyr]</b></font> года[recalc].<br />" +
                        "Услуга: <font color=\"#0000FF\"><b>[service]</b></font>. " +
                        "Поставщик: <font color=\"#0000FF\"><b>[name_supp]</b></font>.<br />" +
                        "Начисление <font color=\"#0000FF\"><b>[result]</b></font> руб = тариф <font color=\"#0000FF\"><b>[tarif]</b></font> руб * " +
                        "Расход <font color=\"#0000FF\"><b>[rashod]</b></font>.<br />[nedop]<br />');";
                    ret = ExecSQL(conn_db, sql, true);

                    sql = "insert into frm_descr (nzp_frm, prot_html) values (-2, '</p></div></body></html>')";
                    ret = ExecSQL(conn_db, sql, true);

                    sql = "insert into frm_descr (nzp_frm, prot_html)" +
                        " values (500, 'Общая площадь квартиры <font color=\"#0000FF\"><b>[pl_ob]</b></font> <font color=\"#008000\">кв.м</font>.<br />" +
                        "Тариф квартирный или домовой параметр.<br />" +
                        "Если определен квартирный параметр, то домовой параметр игнорируется.Если не определен квартирный параметр, то используется домовой параметр.<br />" +
                        "<b>соц.норматив по площади на ЛС=</b><font color=\"#0000FF\"><b> [norma_pl] </b></font><font color=\"#008000\"кв.м</font><br />" +
                        "<b>кол-во зарегистрированных паспортисткой=</b><font color=\"#0000FF\"><b>[kol_gil_itog]</b></font>');";
                    ret = ExecSQL(conn_db, sql, true);
                }
            }
        }

        private void Pack_0017_alter_fin_tables(IDbConnection conn_db, out Returns ret)
        {
            ret = Utils.InitReturns();
            string sql = "select dbname from " + Points.Pref + "_kernel" + tableDelimiter + "s_baselist where idtype = 4";

            MyDataReader reader;
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result) return;

            string dbname, table, fullTableName;

            try
            {
                while (reader.Read())
                {
                    if (reader[0] == DBNull.Value)
                    {
                        continue;
                    }
                    dbname = Convert.ToString(reader[0]).Trim();

                    table = "pack";
                    fullTableName = dbname + ":" + table;

                    if (!TempTableInWebCashe(conn_db, fullTableName)) continue;
#if PG
                    ret = ExecSQL(conn_db, "set search_path to '" + dbname + "'", true);
#else
                    ret = ExecSQL(conn_db, "database " + dbname, true);
#endif
                    if (!ret.result) continue;
                    AddFieldToTable(conn_db, table, "file_name", "char(200)");
                    AddFieldToTable(conn_db, "pack_ls", "distr_month", "date");
                    AddFieldToTable(conn_db, "pack_ls", "pkod", "decimal(13,0)");
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
            finally
            {
                reader.Close();
            }
        }

        private void Pack_0023_alter_charge_tables(IDbConnection conn_db, out Returns ret)
        {
            ret = Utils.InitReturns();

           /* string sql = "select dbname from " + Points.Pref + "_kernel:s_baselist where idtype = 1";

            IDataReader reader;
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result) return;

            string dbname, table, fullTableName;

            while (reader.Read())
            {
                dbname = reader["dbname"] != DBNull.Value ? Convert.ToString(reader["dbname"]).Trim() : "";
                if (dbname == "") continue;

                ret = ExecSQL(conn_db, "database " + dbname, true);
                if (!ret.result) continue;

                for (int i = 1; i < 13; i++)
                {
                    table = "charge_" + i.ToString("00");
                    fullTableName = dbname + ":" + table;
                    if (!TempTableInWebCashe(conn_db, fullTableName)) continue;

                    AddFieldToTable(conn_db, table, "tarif_f", "decimal(14,3)");
                    AddFieldToTable(conn_db, table, "tarif_f_p", "decimal(14,3)");
                    AddFieldToTable(conn_db, table, "sum_tarif_f", "decimal(14,2)");
                    AddFieldToTable(conn_db, table, "sum_tarif_f_p", "decimal(14,2)");
                }
            }
            reader.Close();
            reader.Dispose();

            ExecSQL(conn_db, "database " + Points.Pref + "_kernel", true);*/
        }

        private void Pack_0016_error_types(IDbConnection conn_db, out Returns ret)
        {
            ret = new Returns(true);
#if PG
            if (TempTableInWebCashe(conn_db, Points.Pref + "_data.s_error_types"))
            {
                ExecSQL(conn_db, "update " + Points.Pref + "_data.s_error_types set  name = 'Отсутствуют начисления в лицевом счете' where nzp_err = 1", false);
                ExecSQL(conn_db, "update " + Points.Pref + "_data.s_error_types set name = 'Подозрение на дублирование оплаты по лицевому счёту' where nzp_err = 5 and name = 'Дублирование лицевого счета'", true);
                ExecSQL(conn_db, "insert into " + Points.Pref + "_data.s_error_types (nzp_err,name) Values (666, 'Ошибка определения лицевого счёта по платежному коду')", false);
                ExecSQL(conn_db, "update " + Points.Pref + "_data.s_error_types set  name = 'Ошибка определения лицевого счёта по платежному коду' where nzp_err = 666", false);
                ExecSQL(conn_db, "update " + Points.Pref + "_data.s_error_types set  name = 'Несоответствие штрих-кода виду оплаты' where nzp_err = 700", false);
                ExecSQL(conn_db, "update " + Points.Pref + "_data.s_error_types set  name = 'Недопустимый код получателя средств' where nzp_err = 800", false);
                ExecSQL(conn_db, "insert into " + Points.Pref + "_data.s_error_types (nzp_err, name)  values (1001, 'Оплата по закрытому лицевому счёту без суммы к оплате');", false);
            }
#else
            if (TempTableInWebCashe(conn_db, Points.Pref + "_data:s_error_types"))
            {
                ExecSQL(conn_db, "update " + Points.Pref + "_data:s_error_types set  name = 'Отсутствуют начисления в лицевом счете' where nzp_err = 1", false);
                ExecSQL(conn_db, "update " + Points.Pref + "_data:s_error_types set name = 'Подозрение на дублирование оплаты по лицевому счёту' where nzp_err = 5 and name = 'Дублирование лицевого счета'", true);
                ExecSQL(conn_db, "insert into " + Points.Pref + "_data:s_error_types (nzp_err,name) Values (666, 'Ошибка определения лицевого счёта по платежному коду')", false);
                ExecSQL(conn_db, "update " + Points.Pref + "_data:s_error_types set  name = 'Ошибка определения лицевого счёта по платежному коду' where nzp_err = 666", false);
                ExecSQL(conn_db, "update " + Points.Pref + "_data:s_error_types set  name = 'Несоответствие штрих-кода виду оплаты' where nzp_err = 700", false);
                ExecSQL(conn_db, "update " + Points.Pref + "_data:s_error_types set  name = 'Недопустимый код получателя средств' where nzp_err = 800", false);
                ExecSQL(conn_db, "insert into " + Points.Pref + "_data:s_error_types (nzp_err, name)  values (1001, 'Оплата по закрытому лицевому счёту без суммы к оплате');", false);
            }
#endif
        }

        private void Pack_0015_sprav_procs(IDbConnection conn_db, out Returns ret)
        {
#if PG
            string sql = "select * from " + Points.Pref + "_kernel.s_type_alg ";
#else
            string sql = "select * from " + Points.Pref + "_kernel:s_type_alg ";
#endif
            ret = ExecSQL(conn_db, sql, true);
            if (!ret.result)
            {
#if PG
                ret = ExecSQL(conn_db, "set search_path to '" + Points.Pref + "_kernel'", true);
#else
                ret = ExecSQL(conn_db, "database " + Points.Pref + "_kernel", true);
#endif
                if (!ret.result) return;
#if PG
                sql =
                    " CREATE TABLE s_type_alg (" +
                    "    nzp_type_alg SERIAL NOT NULL," +
                    "    name_type  CHAR(100)," +
                    "    name_small CHAR(50)," +
                    "    name_short CHAR(15)" +
                    " )";
                ret = ExecSQL(conn_db, sql, true);

                if (ret.result)
                {
                    ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel.s_type_alg (nzp_type_alg,name_type,name_small,name_short) values (  1,'Расчет по Постановлению №307-формула 4 (изменение сальдо)'                                       ,'Пост.№307-формула 4 (изменение сальдо)'            ,'П307ф4'        );", true);
                    ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel.s_type_alg (nzp_type_alg,name_type,name_small,name_short) values (  2,'Расчет ОДН (Освещение МОП)-первоначально'                                                        ,'ОДН (Освещение МОП)-первоначально'                 ,'ОДН(ОсвМОП)'   );", true);
                    ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel.s_type_alg (nzp_type_alg,name_type,name_small,name_short) values (  3,'Расчет ОДН (Освещение МОП)'                                                                      ,'ОДН (Освещение МОП)'                               ,'ОДН(ОсвМОП)'   );", true);
                    ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel.s_type_alg (nzp_type_alg,name_type,name_small,name_short) values (  4,'Расчет по Постановлению №307-формула 9(Ночной тариф отдельно)'                                   ,'Пост.№307-формула 9(Ночной тариф отдельно)'        ,'П307ф9'        );", true);
                    ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel.s_type_alg (nzp_type_alg,name_type,name_small,name_short) values (  5,'Расчет по Постановлению №307-формула 9'                                                          ,'Пост.№307-формула 9'                               ,'П307ф9'        );", true);
                    ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel.s_type_alg (nzp_type_alg,name_type,name_small,name_short) values (  6,'Расчет по Постановлению №307-формула 9 с учетом изменений в Постановлении №354'                  ,'Пост.№307-формула 9 с учетом изменений в Пост.№354','П307(ПУ+Н)'    );", true);
                    ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel.s_type_alg (nzp_type_alg,name_type,name_small,name_short) values (  7,'Расчет по Постановлению №307 и нормативов по площадям  '                                         ,'Пост.№307 и нормативов по площадям  '              ,'П307(ПУ+Нпл)'  );", true);
                    ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel.s_type_alg (nzp_type_alg,name_type,name_small,name_short) values (  8,'Расчет по Постановлению №87/262 '                                                                ,'Пост.№87/262 '                                     ,'П87/262'       );", true);
                    ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel.s_type_alg (nzp_type_alg,name_type,name_small,name_short) values (  9,'Расчет по Постановлению №307.2х тарифное электроснабжение.'                                      ,'Пост.№307.2х тарифное электроснабжение.'           ,'П307ф9т2'      );", true);
                    ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel.s_type_alg (nzp_type_alg,name_type,name_small,name_short) values ( 10,'Расчет по Постановлению №307 и нормативов по площадям.2х тарифное электроснабжение.'             ,'Пост.№307 и нормативы по площадям.2х тариф.эл.снаб','П307ф9т2пл'    );", true);
                    ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel.s_type_alg (nzp_type_alg,name_type,name_small,name_short) values ( 11,'Расчет по Постановлению №307 пропорционально площадям '                                          ,'Пост.№307 пропорционально площадям '               ,'П307отопл'     );", true);
                    ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel.s_type_alg (nzp_type_alg,name_type,name_small,name_short) values ( 12,'Расчет по Постановлению №307 пропорционально расходам распределителей'                           ,'Пост.№307 пропорционально расходам распределителей','П307отоплРРО'  );", true);
                    ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel.s_type_alg (nzp_type_alg,name_type,name_small,name_short) values ( 13,'Расчет по Постановлению №87/262 и для арендаторов по Постановлению №307 и нормативов по площадям','Пост.№87/262-для арендаторов Пост.№307'            ,'П87/262ар307пл');", true);
                    ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel.s_type_alg (nzp_type_alg,name_type,name_small,name_short) values ( 14,'Расчет по Постановлению №307 и корректировка нормативов.2х тарифное электроснабжение.'           ,'Пост.№307 и корректировка нормы.2х тариф.эл.снаб'  ,'П307(ПУ+Н)т2'  );", true);
                    ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel.s_type_alg (nzp_type_alg,name_type,name_small,name_short) values ( 15,'Расчет по Постановлению №87/262 с учетом ГрПУ '                                                  ,'Пост.№87/262 с учетом ГрПУ '                       ,'П87/262ГрПУ'   );", true);
                    ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel.s_type_alg (nzp_type_alg,name_type,name_small,name_short) values ( 16,'Расчет по Постановлению №307 и корректировка нормативов с учетом ГрПУ'                           ,'Пост.№307 и корректировка нормативов с учетом ГрПУ','П307(ПУ+Н)ГрПУ');", true);
                    ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel.s_type_alg (nzp_type_alg,name_type,name_small,name_short) values (101,'Расчет по Постановлению №307-формула 6 пропорционально количеству жильцов.'                      ,'Пост.№307-формула 6 проп. кол. жильцов'            ,'П307ф6КолЖил'  );", true);
                    ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel.s_type_alg (nzp_type_alg,name_type,name_small,name_short) values (102,'Расчет по Постановлению №307-формула 6 пропорционально общей площади.'                           ,'Пост.№307-формула 6 проп. общей площади'           ,'П307ф6ОбПл'    );", true);
                    ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel.s_type_alg (nzp_type_alg,name_type,name_small,name_short) values (103,'Расчет по Постановлению №307-формула 6 пропорционально отапливаемой площади.'                    ,'Пост.№307-формула 6 проп. отапливаемой площади'    ,'П307ф6ОтПл'    );", true);
                    ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel.s_type_alg (nzp_type_alg,name_type,name_small,name_short) values (104,'Расчет по Постановлению №307-формула 6 пропорционально количеству лицевых счетов.'               ,'Пост.№307-формула 6 проп. кол. ЛС'                 ,'П307ф6КолЛС'   );", true);
                    
                    ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel.s_type_alg (nzp_type_alg,name_type,name_small,name_short) values ( 21,'Расчет по Постановлению №354-формула 11 пропорционально площади лицевых счетов.'                 ,'Пост.№354-формула 11 проп. площ. ЛС'               ,'П354ф11ПлЛС'   );", true);
                    ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel.s_type_alg (nzp_type_alg,name_type,name_small,name_short) values ( 22,'Расчет по Постановлению №354-формула 11 (2х тарифный по Эл.снабжению) проп. площади лиц. счетов.','Пост.№354-формула 11 (2х т.)'                      ,'П354ф11ПлЛС2т' );", true);
                    ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel.s_type_alg (nzp_type_alg,name_type,name_small,name_short) values ( 23,'Расчет по Постановлению №354-формула 14 (отопление) пропорционально площади лицевых счетов.'     ,'Пост.№354-формула 14 (отопление)'                  ,'П354ф14ПлЛСОт' );", true);
                    ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel.s_type_alg (nzp_type_alg,name_type,name_small,name_short) values ( 24,'Расчет по Постановлению №354-формула 20 пропорционально площади лицевых счетов.'                 ,'Пост.№354-формула 20 проп. площ. ЛС'               ,'П354ф20ПлЛС'   );", true);
                    ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel.s_type_alg (nzp_type_alg,name_type,name_small,name_short) values ( 25,'Расчет по Постановлению №354-формула 18 пропорционально раходам лицевых счетов.'                 ,'Пост.№354-формула 18 проп. расх. ЛС'               ,'П354ф18ПлЛС'   );", true);
                    ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel.s_type_alg (nzp_type_alg,name_type,name_small,name_short) values ( 26,'Расчет по Постановлению №354-формула 12 (без ГВ) пропорционально площади лицевых счетов.'        ,'Пост.№354-формула 12 проп. площ. ЛС'               ,'П354ф12ПлЛС'   );", true);
                    ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel.s_type_alg (nzp_type_alg,name_type,name_small,name_short) values ( 31,'Расчет по Постановлению №354-формула 11 пропорционально площади лицевых счетов.'                 ,'Пост.№354-формула 11 проп. площ. ЛС'               ,'П354ф11ПлЛС'   );", true);
                    ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel.s_type_alg (nzp_type_alg,name_type,name_small,name_short) values ( 32,'Расчет по Постановлению №354-формула 11 (2х тарифный по Эл.снабжению) проп. площади лиц. счетов.','Пост.№354-формула 11 (2х т.)'                      ,'П354ф11ПлЛС2т' );", true);
                    ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel.s_type_alg (nzp_type_alg,name_type,name_small,name_short) values ( 33,'Расчет по Постановлению №354-формула 14 (отопление) пропорционально площади лицевых счетов.'     ,'Пост.№354-формула 14 (отопление)'                  ,'П354ф14ПлЛСОт' );", true);
                }

                if (ret.result)
                {
                    ExecSQL(conn_db, "CREATE UNIQUE INDEX ix_type_alg ON s_type_alg(nzp_type_alg);", true);
                    if (ret.result) ret = ExecSQL(conn_db, "analyze s_type_alg", false);
                }
#else
                sql =
                    " CREATE TABLE \"are\".s_type_alg (" +
                    "    nzp_type_alg SERIAL NOT NULL," +
                    "    name_type  CHAR(100)," +
                    "    name_small CHAR(50)," +
                    "    name_short CHAR(15)" +
                    " )";
                ret = ExecSQL(conn_db, sql, true);

                if (ret.result)
                {
                    ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel:s_type_alg (nzp_type_alg,name_type,name_small,name_short) values (  1,'Расчет по Постановлению №307-формула 4 (изменение сальдо)'                                       ,'Пост.№307-формула 4 (изменение сальдо)'            ,'П307ф4'        );", true);
                    ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel:s_type_alg (nzp_type_alg,name_type,name_small,name_short) values (  2,'Расчет ОДН (Освещение МОП)-первоначально'                                                        ,'ОДН (Освещение МОП)-первоначально'                 ,'ОДН(ОсвМОП)'   );", true);
                    ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel:s_type_alg (nzp_type_alg,name_type,name_small,name_short) values (  3,'Расчет ОДН (Освещение МОП)'                                                                      ,'ОДН (Освещение МОП)'                               ,'ОДН(ОсвМОП)'   );", true);
                    ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel:s_type_alg (nzp_type_alg,name_type,name_small,name_short) values (  4,'Расчет по Постановлению №307-формула 9(Ночной тариф отдельно)'                                   ,'Пост.№307-формула 9(Ночной тариф отдельно)'        ,'П307ф9'        );", true);
                    ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel:s_type_alg (nzp_type_alg,name_type,name_small,name_short) values (  5,'Расчет по Постановлению №307-формула 9'                                                          ,'Пост.№307-формула 9'                               ,'П307ф9'        );", true);
                    ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel:s_type_alg (nzp_type_alg,name_type,name_small,name_short) values (  6,'Расчет по Постановлению №307-формула 9 с учетом изменений в Постановлении №354'                  ,'Пост.№307-формула 9 с учетом изменений в Пост.№354','П307(ПУ+Н)'    );", true);
                    ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel:s_type_alg (nzp_type_alg,name_type,name_small,name_short) values (  7,'Расчет по Постановлению №307 и нормативов по площадям  '                                         ,'Пост.№307 и нормативов по площадям  '              ,'П307(ПУ+Нпл)'  );", true);
                    ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel:s_type_alg (nzp_type_alg,name_type,name_small,name_short) values (  8,'Расчет по Постановлению №87/262 '                                                                ,'Пост.№87/262 '                                     ,'П87/262'       );", true);
                    ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel:s_type_alg (nzp_type_alg,name_type,name_small,name_short) values (  9,'Расчет по Постановлению №307.2х тарифное электроснабжение.'                                      ,'Пост.№307.2х тарифное электроснабжение.'           ,'П307ф9т2'      );", true);
                    ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel:s_type_alg (nzp_type_alg,name_type,name_small,name_short) values ( 10,'Расчет по Постановлению №307 и нормативов по площадям.2х тарифное электроснабжение.'             ,'Пост.№307 и нормативы по площадям.2х тариф.эл.снаб','П307ф9т2пл'    );", true);
                    ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel:s_type_alg (nzp_type_alg,name_type,name_small,name_short) values ( 11,'Расчет по Постановлению №307 пропорционально площадям '                                          ,'Пост.№307 пропорционально площадям '               ,'П307отопл'     );", true);
                    ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel:s_type_alg (nzp_type_alg,name_type,name_small,name_short) values ( 12,'Расчет по Постановлению №307 пропорционально расходам распределителей'                           ,'Пост.№307 пропорционально расходам распределителей','П307отоплРРО'  );", true);
                    ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel:s_type_alg (nzp_type_alg,name_type,name_small,name_short) values ( 13,'Расчет по Постановлению №87/262 и для арендаторов по Постановлению №307 и нормативов по площадям','Пост.№87/262-для арендаторов Пост.№307'            ,'П87/262ар307пл');", true);
                    ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel:s_type_alg (nzp_type_alg,name_type,name_small,name_short) values ( 14,'Расчет по Постановлению №307 и корректировка нормативов.2х тарифное электроснабжение.'           ,'Пост.№307 и корректировка нормы.2х тариф.эл.снаб'  ,'П307(ПУ+Н)т2'  );", true);
                    ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel:s_type_alg (nzp_type_alg,name_type,name_small,name_short) values ( 15,'Расчет по Постановлению №87/262 с учетом ГрПУ '                                                  ,'Пост.№87/262 с учетом ГрПУ '                       ,'П87/262ГрПУ'   );", true);
                    ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel:s_type_alg (nzp_type_alg,name_type,name_small,name_short) values ( 16,'Расчет по Постановлению №307 и корректировка нормативов с учетом ГрПУ'                           ,'Пост.№307 и корректировка нормативов с учетом ГрПУ','П307(ПУ+Н)ГрПУ');", true);
                    ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel:s_type_alg (nzp_type_alg,name_type,name_small,name_short) values (101,'Расчет по Постановлению №307-формула 6 пропорционально количеству жильцов.'                      ,'Пост.№307-формула 6 проп. кол. жильцов'            ,'П307ф6КолЖил'  );", true);
                    ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel:s_type_alg (nzp_type_alg,name_type,name_small,name_short) values (102,'Расчет по Постановлению №307-формула 6 пропорционально общей площади.'                           ,'Пост.№307-формула 6 проп. общей площади'           ,'П307ф6ОбПл'    );", true);
                    ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel:s_type_alg (nzp_type_alg,name_type,name_small,name_short) values (103,'Расчет по Постановлению №307-формула 6 пропорционально отапливаемой площади.'                    ,'Пост.№307-формула 6 проп. отапливаемой площади'    ,'П307ф6ОтПл'    );", true);
                    ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel:s_type_alg (nzp_type_alg,name_type,name_small,name_short) values (104,'Расчет по Постановлению №307-формула 6 пропорционально количеству лицевых счетов.'               ,'Пост.№307-формула 6 проп. кол. ЛС'                 ,'П307ф6КолЛС'   );", true);
                    
                    ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel:s_type_alg (nzp_type_alg,name_type,name_small,name_short) values ( 21,'Расчет по Постановлению №354-формула 11 пропорционально площади лицевых счетов.'                 ,'Пост.№354-формула 11 проп. площ. ЛС'               ,'П354ф11ПлЛС'   );", true);
                    ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel:s_type_alg (nzp_type_alg,name_type,name_small,name_short) values ( 22,'Расчет по Постановлению №354-формула 11 (2х тарифный по Эл.снабжению) проп. площади лиц. счетов.','Пост.№354-формула 11 (2х т.)'                      ,'П354ф11ПлЛС2т' );", true);
                    ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel:s_type_alg (nzp_type_alg,name_type,name_small,name_short) values ( 23,'Расчет по Постановлению №354-формула 14 (отопление) пропорционально площади лицевых счетов.'     ,'Пост.№354-формула 14 (отопление)'                  ,'П354ф14ПлЛСОт' );", true);
                    ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel:s_type_alg (nzp_type_alg,name_type,name_small,name_short) values ( 24,'Расчет по Постановлению №354-формула 20 пропорционально площади лицевых счетов.'                 ,'Пост.№354-формула 20 проп. площ. ЛС'               ,'П354ф20ПлЛС'   );", true);
                    ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel:s_type_alg (nzp_type_alg,name_type,name_small,name_short) values ( 25,'Расчет по Постановлению №354-формула 18 пропорционально раходам лицевых счетов.'                 ,'Пост.№354-формула 18 проп. расх. ЛС'               ,'П354ф18ПлЛС'   );", true);
                    ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel:s_type_alg (nzp_type_alg,name_type,name_small,name_short) values ( 26,'Расчет по Постановлению №354-формула 12 (без ГВ) пропорционально площади лицевых счетов.'        ,'Пост.№354-формула 12 проп. площ. ЛС'               ,'П354ф12ПлЛС'   );", true);
                    ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel:s_type_alg (nzp_type_alg,name_type,name_small,name_short) values ( 31,'Расчет по Постановлению №354-формула 11 пропорционально площади лицевых счетов.'                 ,'Пост.№354-формула 11 проп. площ. ЛС'               ,'П354ф11ПлЛС'   );", true);
                    ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel:s_type_alg (nzp_type_alg,name_type,name_small,name_short) values ( 32,'Расчет по Постановлению №354-формула 11 (2х тарифный по Эл.снабжению) проп. площади лиц. счетов.','Пост.№354-формула 11 (2х т.)'                      ,'П354ф11ПлЛС2т' );", true);
                    ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel:s_type_alg (nzp_type_alg,name_type,name_small,name_short) values ( 33,'Расчет по Постановлению №354-формула 14 (отопление) пропорционально площади лицевых счетов.'     ,'Пост.№354-формула 14 (отопление)'                  ,'П354ф14ПлЛСОт' );", true);
                }

                if (ret.result)
                {
                    ExecSQL(conn_db, "CREATE UNIQUE INDEX \"are\".ix_type_alg ON s_type_alg(nzp_type_alg);", true);
                    if (ret.result) ret = ExecSQL(conn_db, "update statistics for table s_type_alg", false);
                }
#endif
            }
        }

        private void Pack_0014_system_params(IDbConnection conn_db, out Returns ret)
        {
#if PG
            // Приоритеты распределения оплат
            string sql = "select nzp_res from " + Points.Pref + "_kernel.resolution where nzp_res = 3011";
            IDataReader reader;
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result) return;

            if (!reader.Read())
            {
                ret = ExecSQL(conn_db, "delete from " + Points.Pref + "_kernel.res_y where nzp_res = 3011", true);
                ret = ExecSQL(conn_db, "delete from " + Points.Pref + "_kernel.res_x where nzp_res = 3011", true);
                ret = ExecSQL(conn_db, "delete from " + Points.Pref + "_kernel.res_values where nzp_res = 3011;", true);
                
                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel.resolution (nzp_res,name_short,name_res) values (3011,'ТРасщепПриор','Расщепление - Приоритеты распределения оплат');", true);
                
                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel.res_y (nzp_res,nzp_y,name_y) values (3011, 1,'Недействующие услуги имеют приоритет');", true);
                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel.res_y (nzp_res,nzp_y,name_y) values (3011, 2,'Действующие услуги имеют приоритет');", true);
                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel.res_y (nzp_res,nzp_y,name_y) values (3011, 3,'Действующие и недействующие услуги имеют равный приоритет');", true);
                
                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel.res_x (nzp_res,nzp_x,name_x) values (3011,1,'-');", true);
                
                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel.res_values (nzp_res,nzp_y,nzp_x,Value) values (3011, 1,1,'');", true);
                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel.res_values (nzp_res,nzp_y,nzp_x,Value) values (3011, 2,1,'');", true);
                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel.res_values (nzp_res,nzp_y,nzp_x,Value) values (3011, 3,1,'');", true);
            }
            reader.Close();

            // Способы начисления к оплате
            sql = "select nzp_res from " + Points.Pref + "_kernel.resolution where nzp_res = 3012";
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result) return;

            if (!reader.Read())
            {
                ret = ExecSQL(conn_db, "delete from " + Points.Pref + "_kernel.res_y where nzp_res = 3012", true);
                ret = ExecSQL(conn_db, "delete from " + Points.Pref + "_kernel.res_x where nzp_res = 3012", true);
                ret = ExecSQL(conn_db, "delete from " + Points.Pref + "_kernel.res_values where nzp_res = 3012;", true);

                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel.resolution (nzp_res,name_short,name_res) values (3012,'ТРасщепНачКОпл','Расщепление - Способы начисления к оплате');", true);

                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel.res_y (nzp_res,nzp_y,name_y) values (3012, 1,'Исходящее сальдо');", true);
                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel.res_y (nzp_res,nzp_y,name_y) values (3012, 2,'Положительная часть исходящего сальдо');", true);
                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel.res_y (nzp_res,nzp_y,name_y) values (3012, 3,'Начисления за месяц с учетом перерасчетов, недопоставок, изменений сальдо и переплат');", true);
                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel.res_y (nzp_res,nzp_y,name_y) values (3012, 4,'Положительная часть начислений за месяц с учетом перерасчетов, недопоставок, изменений сальдо и переплат');", true);
                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel.res_y (nzp_res,nzp_y,name_y) values (3012, 5,'Начисления за месяц с учетом перерасчетов, недопоставок и изменений сальдо');", true);
                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel.res_y (nzp_res,nzp_y,name_y) values (3012, 6,'Положительная часть начислений за месяц с учетом перерасчетов, недопоставок и изменений сальдо');", true);

                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel.res_x (nzp_res,nzp_x,name_x) values (3012,1,'-');", true);

                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel.res_values (nzp_res,nzp_y,nzp_x,Value) values (3012, 1,1,'');", true);
                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel.res_values (nzp_res,nzp_y,nzp_x,Value) values (3012, 2,1,'');", true);
                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel.res_values (nzp_res,nzp_y,nzp_x,Value) values (3012, 3,1,'');", true);
                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel.res_values (nzp_res,nzp_y,nzp_x,Value) values (3012, 4,1,'');", true);
                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel.res_values (nzp_res,nzp_y,nzp_x,Value) values (3012, 5,1,'');", true);
                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel.res_values (nzp_res,nzp_y,nzp_x,Value) values (3012, 6,1,'');", true);
            }
            reader.Close();

            sql = "select nzp_prm from " + Points.Pref + "_kernel.prm_name where nzp_prm in (1131,1132,1133,1134,1135)";
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result) return;

            if (!reader.Read())
            {
                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel.prm_name (nzp_prm,name_prm,type_prm,nzp_res,prm_num,low_,high_,digits_) values (1131,'Расщепление - Приоритет распределения оплат','sprav',3011,10,Null,Null,Null);", true);
                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel.prm_name (nzp_prm,name_prm,type_prm,nzp_res,prm_num,low_,high_,digits_) values (1132,'Расщепление - Распределять пачку сразу после загрузки','bool' ,null,10,null,null,null);", true);
                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel.prm_name (nzp_prm,name_prm,type_prm,nzp_res,prm_num,low_,high_,digits_) values (1133,'Расщепление - Формировать протокол распределения оплат','bool' ,null,10,null,null,null);", true);
                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel.prm_name (nzp_prm,name_prm,type_prm,nzp_res,prm_num,low_,high_,digits_) values (1134,'Расщепление - Способ начисления к оплате','sprav',3012,10,Null,Null,Null);", true);
                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel.prm_name (nzp_prm,name_prm,type_prm,nzp_res,prm_num,low_,high_,digits_) values (1135,'Расщепление - Плательщик заполняет оплату по услугам','bool',null,10,Null,Null,Null);", true);

                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_data.prm_10 (nzp_key, nzp, nzp_prm, dat_s, dat_po, val_prm, is_actual, nzp_user, dat_when) values (0, 0, 1131, '01.01.1900', '01.01.3000', '1', 1, 1, current);", true);
                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_data.prm_10 (nzp_key, nzp, nzp_prm, dat_s, dat_po, val_prm, is_actual, nzp_user, dat_when) values (0, 0, 1134, '01.01.1900', '01.01.3000', '2', 1, 1, current);", true);
            }

            if (!TempTableInWebCashe(conn_db, Points.Pref + "_kernel.prm_table_descr"))
            {
                sql = "create table prm_table_descr" +
                    " (" +
                    " nzp_table int not null," +
                    " table_name char(20) not null," +
                    " db_name char(30) not null," +
                    " key_field char(20) not null," +
                    " display_field char(20) not null" +
                    " )";
                ret = ExecSQL(conn_db, sql, true);

                if (ret.result)
                {
                    ExecSQL(conn_db, "CREATE UNIQUE INDEX ixprm_table_descr_1 ON prm_table_descr(nzp_table);", true);
                    ret = ExecSQL(conn_db, "insert into  prm_table_descr (nzp_table, table_name, db_name, key_field, display_field) values (1, 'services', '_kernel', 'nzp_serv', 'service');", true);
                }
            }

            //Сохранять показания ПУ напрямую в реальный банк
            sql = "select nzp_prm from " + Points.Pref + "_kernel.prm_name where nzp_prm in (1993)";
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result) return;

            if (!reader.Read())
            {
                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel.prm_name (nzp_prm,name_prm,type_prm,nzp_res,prm_num,low_,high_,digits_)" +
                    " values (1993, 'Сохранять показания ПУ без буфера', 'bool', Null, 5, Null, Null, Null);", true);

                if (ret.result) ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_data.prm_5 (nzp_key, nzp, nzp_prm, dat_s, dat_po, val_prm, is_actual, nzp_user, dat_when)" +
                    " values (0, 0, 1993, '01.01.1900', '01.01.3000', '1', 1, 1, current);", true);
            }

            //Расчет дотаций (по умолчанию выключен)
            sql = "select nzp_prm from " + Points.Pref + "_kernel.prm_name where nzp_prm in (1992)";
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result) return;

            if (!reader.Read())
            {
                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel.prm_name (nzp_prm,name_prm,type_prm,nzp_res,prm_num,low_,high_,digits_)" +
                    " values (1992, '--- 5.0 - Есть расчет дотаций', 'bool', Null, 5, Null, Null, Null);", true);

                /*if (ret.result) ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_data:prm_5 (nzp_key, nzp, nzp_prm, dat_s, dat_po, val_prm, is_actual, nzp_user, dat_when)" +
                    " values (0, 0, 1992, '01.01.1900', '01.01.3000', '1', 1, 1, current);", true);*/
            }
            reader.Close();
            reader.Dispose();

            #region Режим перерасчета начислений
            // Приоритеты распределения оплат
            sql = "select nzp_res from " + Points.Pref + "_kernel.resolution where nzp_res = 3016";
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result) return;

            if (!reader.Read())
            {
                ret = ExecSQL(conn_db, "delete from " + Points.Pref + "_kernel.res_y where nzp_res = 3016", true);
                ret = ExecSQL(conn_db, "delete from " + Points.Pref + "_kernel.res_x where nzp_res = 3016", true);
                ret = ExecSQL(conn_db, "delete from " + Points.Pref + "_kernel.res_values where nzp_res = 3016;", true);

                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel.resolution (nzp_res,name_short,name_res) values (3016,'ТВидПереР','таблица видов перерасчетов');", true);

                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel.res_y (nzp_res,nzp_y,name_y) values (3016,1,'Автоматический перерасчет начислений');", true);
                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel.res_y (nzp_res,nzp_y,name_y) values (3016,2,'Автоматический(с отменой) перерасчет начислений');", true);
                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel.res_y (nzp_res,nzp_y,name_y) values (3016,3,'Перерасчет по требованию пользователя');", true);

                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel.res_x (nzp_res,nzp_x,name_x) values (3016,1,'Номер');", true);

                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel.res_values (nzp_res,nzp_y,nzp_x,Value) values (3016, 1,1,'');", true);
                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel.res_values (nzp_res,nzp_y,nzp_x,Value) values (3016, 2,1,'');", true);
                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel.res_values (nzp_res,nzp_y,nzp_x,Value) values (3016, 3,1,'');", true);
            }
            reader.Close();

            sql = "select nzp_prm from " + Points.Pref + "_kernel.prm_name where nzp_prm = 1990";
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result) return;

            if (!reader.Read())
            {
                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel.prm_name (nzp_prm,name_prm,type_prm,nzp_res,prm_num,low_,high_,digits_)" +
                    " values (1990,'Режим перерасчета начислений','sprav',3016, 5,Null,Null,Null);", true);

                if (ret.result) ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_data.prm_5 (nzp_key, nzp, nzp_prm, dat_s, dat_po, val_prm, is_actual, nzp_user, dat_when)" +
                    " values (0, 0, 1990, '01.01.1900', '01.01.3000', '1', 1, 1, current);", true);
            }
            #endregion
#else
            // Приоритеты распределения оплат
            string sql = "select nzp_res from " + Points.Pref + "_kernel:resolution where nzp_res = 3011";
            IDataReader reader;
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result) return;

            if (!reader.Read())
            {
                ret = ExecSQL(conn_db, "delete from " + Points.Pref + "_kernel:res_y where nzp_res = 3011", true);
                ret = ExecSQL(conn_db, "delete from " + Points.Pref + "_kernel:res_x where nzp_res = 3011", true);
                ret = ExecSQL(conn_db, "delete from " + Points.Pref + "_kernel:res_values where nzp_res = 3011;", true);
                
                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel:resolution (nzp_res,name_short,name_res) values (3011,'ТРасщепПриор','Расщепление - Приоритеты распределения оплат');", true);
                
                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel:res_y (nzp_res,nzp_y,name_y) values (3011, 1,'Недействующие услуги имеют приоритет');", true);
                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel:res_y (nzp_res,nzp_y,name_y) values (3011, 2,'Действующие услуги имеют приоритет');", true);
                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel:res_y (nzp_res,nzp_y,name_y) values (3011, 3,'Действующие и недействующие услуги имеют равный приоритет');", true);
                
                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel:res_x (nzp_res,nzp_x,name_x) values (3011,1,'-');", true);
                
                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel:res_values (nzp_res,nzp_y,nzp_x,Value) values (3011, 1,1,'');", true);
                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel:res_values (nzp_res,nzp_y,nzp_x,Value) values (3011, 2,1,'');", true);
                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel:res_values (nzp_res,nzp_y,nzp_x,Value) values (3011, 3,1,'');", true);
            }
            reader.Close();

            // Способы начисления к оплате
            sql = "select nzp_res from " + Points.Pref + "_kernel:resolution where nzp_res = 3012";
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result) return;

            if (!reader.Read())
            {
                ret = ExecSQL(conn_db, "delete from " + Points.Pref + "_kernel:res_y where nzp_res = 3012", true);
                ret = ExecSQL(conn_db, "delete from " + Points.Pref + "_kernel:res_x where nzp_res = 3012", true);
                ret = ExecSQL(conn_db, "delete from " + Points.Pref + "_kernel:res_values where nzp_res = 3012;", true);

                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel:resolution (nzp_res,name_short,name_res) values (3012,'ТРасщепНачКОпл','Расщепление - Способы начисления к оплате');", true);

                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel:res_y (nzp_res,nzp_y,name_y) values (3012, 1,'Исходящее сальдо');", true);
                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel:res_y (nzp_res,nzp_y,name_y) values (3012, 2,'Положительная часть исходящего сальдо');", true);
                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel:res_y (nzp_res,nzp_y,name_y) values (3012, 3,'Начисления за месяц с учетом перерасчетов, недопоставок, изменений сальдо и переплат');", true);
                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel:res_y (nzp_res,nzp_y,name_y) values (3012, 4,'Положительная часть начислений за месяц с учетом перерасчетов, недопоставок, изменений сальдо и переплат');", true);
                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel:res_y (nzp_res,nzp_y,name_y) values (3012, 5,'Начисления за месяц с учетом перерасчетов, недопоставок и изменений сальдо');", true);
                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel:res_y (nzp_res,nzp_y,name_y) values (3012, 6,'Положительная часть начислений за месяц с учетом перерасчетов, недопоставок и изменений сальдо');", true);

                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel:res_x (nzp_res,nzp_x,name_x) values (3012,1,'-');", true);

                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel:res_values (nzp_res,nzp_y,nzp_x,Value) values (3012, 1,1,'');", true);
                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel:res_values (nzp_res,nzp_y,nzp_x,Value) values (3012, 2,1,'');", true);
                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel:res_values (nzp_res,nzp_y,nzp_x,Value) values (3012, 3,1,'');", true);
                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel:res_values (nzp_res,nzp_y,nzp_x,Value) values (3012, 4,1,'');", true);
                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel:res_values (nzp_res,nzp_y,nzp_x,Value) values (3012, 5,1,'');", true);
                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel:res_values (nzp_res,nzp_y,nzp_x,Value) values (3012, 6,1,'');", true);
            }
            reader.Close();

            sql = "select nzp_prm from " + Points.Pref + "_kernel:prm_name where nzp_prm in (1131,1132,1133,1134,1135)";
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result) return;

            if (!reader.Read())
            {
                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel:prm_name (nzp_prm,name_prm,type_prm,nzp_res,prm_num,low_,high_,digits_) values (1131,'Расщепление - Приоритет распределения оплат','sprav',3011,10,Null,Null,Null);", true);
                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel:prm_name (nzp_prm,name_prm,type_prm,nzp_res,prm_num,low_,high_,digits_) values (1132,'Расщепление - Распределять пачку сразу после загрузки','bool' ,null,10,null,null,null);", true);
                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel:prm_name (nzp_prm,name_prm,type_prm,nzp_res,prm_num,low_,high_,digits_) values (1133,'Расщепление - Формировать протокол распределения оплат','bool' ,null,10,null,null,null);", true);
                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel:prm_name (nzp_prm,name_prm,type_prm,nzp_res,prm_num,low_,high_,digits_) values (1134,'Расщепление - Способ начисления к оплате','sprav',3012,10,Null,Null,Null);", true);
                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel:prm_name (nzp_prm,name_prm,type_prm,nzp_res,prm_num,low_,high_,digits_) values (1135,'Расщепление - Плательщик заполняет оплату по услугам','bool',null,10,Null,Null,Null);", true);

                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_data:prm_10 (nzp_key, nzp, nzp_prm, dat_s, dat_po, val_prm, is_actual, nzp_user, dat_when) values (0, 0, 1131, '01.01.1900', '01.01.3000', '1', 1, 1, current);", true);
                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_data:prm_10 (nzp_key, nzp, nzp_prm, dat_s, dat_po, val_prm, is_actual, nzp_user, dat_when) values (0, 0, 1134, '01.01.1900', '01.01.3000', '2', 1, 1, current);", true);
            }

            if (!TempTableInWebCashe(conn_db, Points.Pref + "_kernel:prm_table_descr"))
            {
                sql = "create table \"are\".prm_table_descr" +
                    " (" +
                    " nzp_table int not null," +
                    " table_name char(20) not null," +
                    " db_name char(30) not null," +
                    " key_field char(20) not null," +
                    " display_field char(20) not null" +
                    " )";
                ret = ExecSQL(conn_db, sql, true);

                if (ret.result)
                {
                    ExecSQL(conn_db, "CREATE UNIQUE INDEX \"are\".ixprm_table_descr_1 ON \"are\".prm_table_descr(nzp_table);", true);
                    ret = ExecSQL(conn_db, "insert into  prm_table_descr (nzp_table, table_name, db_name, key_field, display_field) values (1, 'services', '_kernel', 'nzp_serv', 'service');", true);
                }
            }

            //Сохранять показания ПУ напрямую в реальный банк
            sql = "select nzp_prm from " + Points.Pref + "_kernel:prm_name where nzp_prm in (1993)";
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result) return;

            if (!reader.Read())
            {
                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel:prm_name (nzp_prm,name_prm,type_prm,nzp_res,prm_num,low_,high_,digits_)" +
                    " values (1993, 'Сохранять показания ПУ без буфера', 'bool', Null, 5, Null, Null, Null);", true);

                if (ret.result) ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_data:prm_5 (nzp_key, nzp, nzp_prm, dat_s, dat_po, val_prm, is_actual, nzp_user, dat_when)" +
                    " values (0, 0, 1993, '01.01.1900', '01.01.3000', '1', 1, 1, current);", true);
            }

            //Расчет дотаций (по умолчанию выключен)
            sql = "select nzp_prm from " + Points.Pref + "_kernel:prm_name where nzp_prm in (1992)";
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result) return;

            if (!reader.Read())
            {
                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel:prm_name (nzp_prm,name_prm,type_prm,nzp_res,prm_num,low_,high_,digits_)" +
                    " values (1992, '--- 5.0 - Есть расчет дотаций', 'bool', Null, 5, Null, Null, Null);", true);

                /*if (ret.result) ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_data:prm_5 (nzp_key, nzp, nzp_prm, dat_s, dat_po, val_prm, is_actual, nzp_user, dat_when)" +
                    " values (0, 0, 1992, '01.01.1900', '01.01.3000', '1', 1, 1, current);", true);*/
            }
            reader.Close();
            reader.Dispose();

            #region Режим перерасчета начислений
            // Приоритеты распределения оплат
            sql = "select nzp_res from " + Points.Pref + "_kernel:resolution where nzp_res = 3016";
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result) return;

            if (!reader.Read())
            {
                ret = ExecSQL(conn_db, "delete from " + Points.Pref + "_kernel:res_y where nzp_res = 3016", true);
                ret = ExecSQL(conn_db, "delete from " + Points.Pref + "_kernel:res_x where nzp_res = 3016", true);
                ret = ExecSQL(conn_db, "delete from " + Points.Pref + "_kernel:res_values where nzp_res = 3016;", true);

                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel:resolution (nzp_res,name_short,name_res) values (3016,'ТВидПереР','таблица видов перерасчетов');", true);

                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel:res_y (nzp_res,nzp_y,name_y) values (3016,1,'Автоматический перерасчет начислений');", true);
                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel:res_y (nzp_res,nzp_y,name_y) values (3016,2,'Автоматический(с отменой) перерасчет начислений');", true);
                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel:res_y (nzp_res,nzp_y,name_y) values (3016,3,'Перерасчет по требованию пользователя');", true);

                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel:res_x (nzp_res,nzp_x,name_x) values (3016,1,'Номер');", true);

                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel:res_values (nzp_res,nzp_y,nzp_x,Value) values (3016, 1,1,'');", true);
                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel:res_values (nzp_res,nzp_y,nzp_x,Value) values (3016, 2,1,'');", true);
                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel:res_values (nzp_res,nzp_y,nzp_x,Value) values (3016, 3,1,'');", true);
            }
            reader.Close();

            sql = "select nzp_prm from " + Points.Pref + "_kernel:prm_name where nzp_prm = 1990";
            ret = ExecRead(conn_db, out reader, sql, true);
            if (!ret.result) return;

            if (!reader.Read())
            {
                ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_kernel:prm_name (nzp_prm,name_prm,type_prm,nzp_res,prm_num,low_,high_,digits_)" +
                    " values (1990,'Режим перерасчета начислений','sprav',3016, 5,Null,Null,Null);", true);

                if (ret.result) ret = ExecSQL(conn_db, "insert into " + Points.Pref + "_data:prm_5 (nzp_key, nzp, nzp_prm, dat_s, dat_po, val_prm, is_actual, nzp_user, dat_when)" +
                    " values (0, 0, 1990, '01.01.1900', '01.01.3000', '1', 1, 1, current);", true);
            }
            #endregion
#endif
        }

        private void Pack_0013_2_fin_procedures(IDbConnection conn_db, out Returns ret)
        {
#if PG
            ret = ExecSQL(conn_db, "set search_path to '" + Points.Pref + "_kernel'", true);
            if (!ret.result) return;

            string sql = "drop function getSumPrih(decimal(14,2),decimal(14,2), integer)";
            ExecSQL(conn_db, sql, false);

            sql = "create function getSumPrih(sum_etalon decimal(14,2),sum_prih decimal(14,2), isdel integer)"+
                " returns decimal(14,2) as"+
                " $BODY$ " +
                " DECLARE sum_out decimal(14,2);"+
                " begin " +
                " if isdel=0 "+
                " then "+
                    " sum_out= sum_prih;"+
                " else"+
                    " if sum_prih > sum_etalon"+
                    " then"+
                         " sum_out= sum_etalon;"+
                    " else"+
                         " sum_out= sum_prih;"+
                    " end if;"+
                " end if;"+
                " return sum_out;"+
                " end; $BODY$ LANGUAGE plpgsql";

            ret = ExecSQL(conn_db, sql, true);
            if (ret.result)
            {
                sql = "grant execute on function getSumPrih(decimal(14,2),decimal(14,2), integer) to public;";
                ret = ExecSQL(conn_db, sql, true);
            }

            sql = "drop function getSumOstatok(integer);";
            ExecSQL(conn_db, sql, false);

            sql = "create function getSumOstatok(pnzp_pack_ls integer)"+
                " returns decimal(14,2) as "+
                " $BODY$ " +
                " DECLARE sum_out decimal(14,2);"+
                " begin " +
                " sum_out = 0;"+
                " select AVG(g_sum_ls)-SUM(sum_prih_d+sum_prih_a+sum_prih_u+sum_prih_s) into sum_out"+
                    " from t_opl where nzp_pack_ls = pnzp_pack_ls;"+
                " return sum_out;"+
                " end; $BODY$ LANGUAGE plpgsql";

            ret = ExecSQL(conn_db, sql, true);
            if (ret.result)
            {
                sql = "grant execute on function getSumOstatok(integer) to public;";
                ret = ExecSQL(conn_db, sql, true);
            }
#else
            ret = ExecSQL(conn_db, "database " + Points.Pref + "_kernel", true);
            if (!ret.result) return;

            string sql = "drop function \"are\".getSumPrih(decimal(14,2),decimal(14,2), integer)";
            ExecSQL(conn_db, sql, false);

            sql = "create function \"are\".getSumPrih(sum_etalon decimal(14,2),sum_prih decimal(14,2), isdel integer)" +
                " returning decimal(14,2);" +
                " define sum_out decimal(14,2);" +
                " if isdel=0 " +
                " then " +
                    " let sum_out= sum_prih;" +
                " else" +
                    " if sum_prih > sum_etalon" +
                    " then" +
                         " let sum_out= sum_etalon;" +
                    " else" +
                         " let sum_out= sum_prih;" +
                    " end if;" +
                " end if;" +
                " return sum_out;" +
                " end function;";

            ret = ExecSQL(conn_db, sql, true);
            if (ret.result)
            {
                sql = "grant execute on function \"are\".getSumPrih(decimal(14,2),decimal(14,2), integer) to public as are;";
                ret = ExecSQL(conn_db, sql, true);
            }

            sql = "drop function \"are\".getSumOstatok(integer);";
            ExecSQL(conn_db, sql, false);

            sql = "create function \"are\".getSumOstatok(pnzp_pack_ls integer)" +
                " returning decimal(14,2);" +
                " define sum_out decimal(14,2);" +
                " let sum_out = 0;" +
                " select AVG(g_sum_ls)-SUM(sum_prih_d+sum_prih_a+sum_prih_u+sum_prih_s) into sum_out" +
                    " from t_opl where nzp_pack_ls = pnzp_pack_ls;" +
                " return sum_out;" +
                " end function;";

            ret = ExecSQL(conn_db, sql, true);
            if (ret.result)
            {
                sql = "grant execute on function \"are\".getSumOstatok(integer) to public as are;";
                ret = ExecSQL(conn_db, sql, true);
            }
#endif
        }

        private void Pack_0012_series_area(IDbConnection conn_db, out Returns ret)
        {
            ret = Utils.InitReturns();
            IDataReader reader;
#if PG
            string sql = "select kod from " + Points.Pref + "_data.series where kod = 6 ";
#else
            string sql = "select kod from " + Points.Pref + "_data:series where kod = 6 ";
#endif
            ret = ExecRead(conn_db, out reader, sql, true);
            if (ret.result)
            {
                if (!reader.Read())
                {
#if PG
                    sql = " insert into " + Points.Pref + "_data.series (v_min, v_max, cur_val, kod) " +
                          " values (1, 9999, 1001, 6)";
#else
                    sql = " insert into " + Points.Pref + "_data:series (v_min, v_max, cur_val, kod) " +
                          " values (1, 9999, 1001, 6)";
#endif
                    ret = ExecSQL(conn_db, sql, true);
                }
                CloseReader(ref reader);
            }
        }

        private void Pack_0011_series_geu(IDbConnection conn_db, out Returns ret)
        {
            ret = Utils.InitReturns();
            IDataReader reader;

#if PG
            string sql = "select kod from " + Points.Pref + "_data.series where kod = 5 ";
#else
            string sql = "select kod from " + Points.Pref + "_data:series where kod = 5 ";
#endif
            ret = ExecRead(conn_db, out reader, sql, true);
            if (ret.result)
            {
                if (!reader.Read())
                {
#if PG
                    sql = " insert into " + Points.Pref + "_data.series (v_min, v_max, cur_val, kod) " +
                          " values (1, 9999, 1001, 5)";
#else
                    sql = " insert into " + Points.Pref + "_data:series (v_min, v_max, cur_val, kod) " +
                          " values (1, 9999, 1001, 5)";
#endif
                    ret = ExecSQL(conn_db, sql, true);
                }
                CloseReader(ref reader);
            }
        }

        private void Pack_0010_s_rajon_dom(IDbConnection conn_db, out Returns ret)
        {
            ret = Utils.InitReturns();

            string sql;

            string[] prefs = new string[points.Count + 1];

            prefs[0] = Points.Pref;
            for (int i = 0; i < points.Count; i++) prefs[i + 1] = points[i].pref;

            foreach (string pref in prefs)
            {
#if PG
                ret = ExecSQL(conn_db, "set search_path to '" + pref + "_data'", true);
#else
                ret = ExecSQL(conn_db, "database " + pref + "_data", true);
#endif
                if (!ret.result) continue;

#if PG
                if (!TempTableInWebCashe(conn_db, pref + "_data.s_rajon_dom"))
                {
                    sql = "CREATE TABLE s_rajon_dom(" +
                        " nzp_raj_dom SERIAL NOT NULL," +
                        " rajon_dom CHAR(30) NOT NULL," +
                        " alt_rajon_dom CHAR(30))";
                    ret = ExecSQL(conn_db, sql, true);

                    if (ret.result)
                    {
                        sql = "CREATE UNIQUE INDEX ix_raj_dom1 ON s_rajon_dom(nzp_raj_dom)";
                        ret = ExecSQL(conn_db, sql, true);
                    }
                }
#else
                if (!TempTableInWebCashe(conn_db, pref + "_data:s_rajon_dom"))
                {
                    sql = "CREATE TABLE \"pasp\".s_rajon_dom(" +
                        " nzp_raj_dom SERIAL NOT NULL," +
                        " rajon_dom CHAR(30) NOT NULL," +
                        " alt_rajon_dom CHAR(30))";
                    ret = ExecSQL(conn_db, sql, true);

                    if (ret.result)
                    {
                        sql = "CREATE UNIQUE INDEX \"pasp\".ix_raj_dom1 ON \"pasp\".s_rajon_dom(nzp_raj_dom)";
                        ret = ExecSQL(conn_db, sql, true);
                    }
                }
#endif
            }
        }

        /// <summary>Изменение структуры и добавление таблиц в ХХХХ_data
        /// </summary>
        private void Pack_0008_payers_and_banks(IDbConnection conn_db, out Returns ret)
        {
            ret = new Returns(true);
#if PG
            if (!TempTableInWebCashe(conn_db, Points.Pref + "_kernel.s_payer") || !TempTableInWebCashe(conn_db, Points.Pref + "_kernel.s_bank")) return;
            ret = ExecSQL(conn_db, "set search_path to '" + Points.Pref + "_kernel'", true);
#else
            if (!TempTableInWebCashe(conn_db, Points.Pref + "_kernel:s_payer") || !TempTableInWebCashe(conn_db, Points.Pref + "_kernel:s_bank")) return;
            ret = ExecSQL(conn_db, "database " + Points.Pref + "_kernel", true);
#endif
            if (!ret.result) return;

            AddFieldToTable(conn_db, "s_payer", "is_erc", "INTEGER default 0");
            
            AddFieldToTable(conn_db, "s_payer", "inn", "CHAR(12)");
            AddFieldToTable(conn_db, "s_payer", "kpp", "CHAR(9)");
            AddFieldToTable(conn_db, "s_payer", "nzp_type", "INTEGER");

            string sql;

#if PG
            if (!TempTableInWebCashe(conn_db, Points.Pref + "_kernel.s_payer_types"))
            { 
                sql = "create table s_payer_types "+
                    " ( "+
                    " nzp_payer_type serial not null, "+
                    " type_name char(50), "+
                    " is_system integer default 0 "+
                    " )";

                ret = ExecSQL(conn_db, sql, true);
                if (ret.result)
                {
                    ExecSQL(conn_db, "create unique index ix_s_payer_types_1 on s_payer_types(nzp_payer_type);", true);
                    ExecSQL(conn_db, "ALTER TABLE s_payer_types ADD CONSTRAINT PRIMARY KEY (nzp_payer_type) CONSTRAINT pk_s_payer_types;", true);
                    ExecSQL(conn_db, "insert into s_payer_types (nzp_payer_type, type_name, is_system) values (1, 'Системный', 1);", true);
                    ExecSQL(conn_db, "insert into s_payer_types (nzp_payer_type, type_name, is_system) values (2, 'Поставщик услуг', 1);", true);
                    ExecSQL(conn_db, "insert into s_payer_types (nzp_payer_type, type_name, is_system) values (3, 'Управляющая организация', 1);", true);
                    ExecSQL(conn_db, "insert into s_payer_types (nzp_payer_type, type_name, is_system) values (4, 'Организация, осуществляющая прием платежей', 1);", true);
                    ExecSQL(conn_db, "insert into s_payer_types (nzp_payer_type, type_name, is_system) values (100, 'Прочие', 1);", true);
                    ExecSQL(conn_db, "ALTER TABLE s_payer ADD CONSTRAINT (FOREIGN KEY (nzp_type) REFERENCES s_payer_types CONSTRAINT fk_s_payer_1);", true);
                }
            }
#else
            if (!TempTableInWebCashe(conn_db, Points.Pref + "_kernel:s_payer_types"))
            { 
                sql = "create table \"are\".s_payer_types " +
                    " ( " +
                    " nzp_payer_type serial not null, " +
                    " type_name char(50), " +
                    " is_system integer default 0 " +
                    " )";

                ret = ExecSQL(conn_db, sql, true);
                if (ret.result)
                {
                    ExecSQL(conn_db, "create unique index \"are\".ix_s_payer_types_1 on s_payer_types(nzp_payer_type);", true);
                    ExecSQL(conn_db, "ALTER TABLE s_payer_types ADD CONSTRAINT PRIMARY KEY (nzp_payer_type) CONSTRAINT \"are\".pk_s_payer_types;", true);
                    ExecSQL(conn_db, "insert into s_payer_types (nzp_payer_type, type_name, is_system) values (1, 'Системный', 1);", true);
                    ExecSQL(conn_db, "insert into s_payer_types (nzp_payer_type, type_name, is_system) values (2, 'Поставщик услуг', 1);", true);
                    ExecSQL(conn_db, "insert into s_payer_types (nzp_payer_type, type_name, is_system) values (3, 'Управляющая организация', 1);", true);
                    ExecSQL(conn_db, "insert into s_payer_types (nzp_payer_type, type_name, is_system) values (4, 'Организация, осуществляющая прием платежей', 1);", true);
                    ExecSQL(conn_db, "insert into s_payer_types (nzp_payer_type, type_name, is_system) values (100, 'Прочие', 1);", true);
                    ExecSQL(conn_db, "ALTER TABLE s_payer ADD CONSTRAINT (FOREIGN KEY (nzp_type) REFERENCES s_payer_types CONSTRAINT \"are\".fk_s_payer_1);", true);
                }
            }
#endif

            IDataReader reader;

            #region Суперпачка
#if PG
            sql = "select nzp_payer from " + Points.Pref + "_kernel.s_payer where nzp_payer = 79999";
#else
            sql = "select nzp_payer from " + Points.Pref + "_kernel:s_payer where nzp_payer = 79999";
#endif
            ret = ExecRead(conn_db, out reader, sql, true);
            if (ret.result)
            {
                if (!reader.Read())
                {
#if PG
                    sql = " insert into " + Points.Pref + "_kernel.s_payer (nzp_payer, payer, npayer, nzp_supp, is_erc) " +
#else
                    sql = " insert into " + Points.Pref + "_kernel:s_payer (nzp_payer, payer, npayer, nzp_supp, is_erc) " +
#endif
                            " values (79999, '* Суперпачки', 'Суперпачки', 0, 0)";
                    ret = ExecSQL(conn_db, sql, true);
                }
                CloseReader(ref reader);
            }

#if PG
            sql = "select nzp_bank from " + Points.Pref + "_kernel.s_bank where nzp_bank = 1000";
#else
            sql = "select nzp_bank from " + Points.Pref + "_kernel:s_bank where nzp_bank = 1000";
#endif
            ret = ExecRead(conn_db, out reader, sql, true);
            if (ret.result)
            {
                if (!reader.Read())
                {
#if PG
                    sql = "insert into " + Points.Pref + "_kernel.s_bank (nzp_bank, bank, nzp_payer) values (1000, 'Суперпачка', 79999)";
#else
                    sql = "insert into " + Points.Pref + "_kernel:s_bank (nzp_bank, bank, nzp_payer) values (1000, 'Суперпачка', 79999)";
#endif
                    ret = ExecSQL(conn_db, sql, true);
                }
                CloseReader(ref reader);
            }
            #endregion

            #region Ручной платеж
            
#if PG
                    sql = "select nzp_payer from " + Points.Pref + "_kernel.s_payer where nzp_payer = 1998";
#else
                    sql = "select nzp_payer from " + Points.Pref + "_kernel:s_payer where nzp_payer = 1998";
#endif
            ret = ExecRead(conn_db, out reader, sql, true);
            if (ret.result)
            {
                if (!reader.Read())
                {
#if PG
                    sql = " insert into " + Points.Pref + "_kernel.s_payer (nzp_payer, payer, npayer, nzp_supp, is_erc) " +
#else
                    sql = " insert into " + Points.Pref + "_kernel:s_payer (nzp_payer, payer, npayer, nzp_supp, is_erc) " +
#endif
                            " values (1998, '* Ручной платеж', 'Ручной платеж', 0, 0)";
                    ret = ExecSQL(conn_db, sql, true);
                }
                CloseReader(ref reader);
            }
#if PG
            sql = "select nzp_bank from " + Points.Pref + "_kernel.s_bank where nzp_bank = 1998";
#else
            sql = "select nzp_bank from " + Points.Pref + "_kernel:s_bank where nzp_bank = 1998";
#endif
            ret = ExecRead(conn_db, out reader, sql, true);
            if (ret.result)
            {
                if (!reader.Read())
                {
#if PG
                    sql = "insert into " + Points.Pref + "_kernel.s_bank (nzp_bank, bank, nzp_payer) values (1998, 'Ручной платеж', 1998)";
#else
                    sql = "insert into " + Points.Pref + "_kernel:s_bank (nzp_bank, bank, nzp_payer) values (1998, 'Ручной платеж', 1998)";
#endif
                    ret = ExecSQL(conn_db, sql, true);
                }
                CloseReader(ref reader);
            }
            #endregion

            #region Безналичный платеж
#if PG
            sql = "select nzp_payer from " + Points.Pref + "_kernel.s_payer where nzp_payer = 79998";
#else
            sql = "select nzp_payer from " + Points.Pref + "_kernel:s_payer where nzp_payer = 79998";
#endif
            ret = ExecRead(conn_db, out reader, sql, true);
            if (ret.result)
            {
                if (!reader.Read())
                {
#if PG
                    sql = " insert into " + Points.Pref + "_kernel.s_payer (nzp_payer, payer, npayer, nzp_supp, is_erc) values (79998, '* Безналичный платеж','Безналичный платеж', 0, 0)";
#else
                    sql = " insert into " + Points.Pref + "_kernel:s_payer (nzp_payer, payer, npayer, nzp_supp, is_erc) values (79998, '* Безналичный платеж','Безналичный платеж', 0, 0)";
#endif
                    ret = ExecSQL(conn_db, sql, true);
                }
                CloseReader(ref reader);
            }

#if PG
            sql = "select nzp_bank from " + Points.Pref + "_kernel.s_bank where nzp_bank = 79998";
#else
            sql = "select nzp_bank from " + Points.Pref + "_kernel:s_bank where nzp_bank = 79998";
#endif
            ret = ExecRead(conn_db, out reader, sql, true);
            if (ret.result)
            {
                if (!reader.Read())
                {
#if PG
                    sql = " insert into " + Points.Pref + "_kernel.s_bank (nzp_bank, bank, nzp_payer) values (79998, 'Безналичный платеж',79998)";
#else
                    sql = " insert into " + Points.Pref + "_kernel:s_bank (nzp_bank, bank, nzp_payer) values (79998, 'Безналичный платеж',79998)";
#endif
                    ret = ExecSQL(conn_db, sql, true);
                }
                CloseReader(ref reader);
            }
            #endregion

            #region Диспетчерская
#if PG
            sql = "select nzp_payer from " + Points.Pref + "_kernel.s_payer where nzp_payer = " + Payers.DispatchingOffice.GetHashCode();
#else
            sql = "select nzp_payer from " + Points.Pref + "_kernel:s_payer where nzp_payer = " + Payers.DispatchingOffice.GetHashCode();
#endif
            ret = ExecRead(conn_db, out reader, sql, true);
            if (ret.result)
            {
                if (!reader.Read())
                {
#if PG
                    sql = " insert into " + Points.Pref + "_kernel.s_payer (nzp_payer, payer, npayer, nzp_supp, is_erc) values (" + Payers.DispatchingOffice.GetHashCode() + ", 'Диспетчерская служба','Диспетчерская служба', 0, 0)";
#else
                    sql = " insert into " + Points.Pref + "_kernel:s_payer (nzp_payer, payer, npayer, nzp_supp, is_erc) values (" + Payers.DispatchingOffice.GetHashCode() + ", 'Диспетчерская служба','Диспетчерская служба', 0, 0)";
#endif
                    ret = ExecSQL(conn_db, sql, true);
                }
                CloseReader(ref reader);
            }
            #endregion
            
        }


        /// <summary>Изменение структуры и добавление таблиц в ХХХХ_data
        /// </summary>
        private void Pack_0007_pack_status(IDbConnection conn_db, out Returns ret)
        {
            ret = new Returns(true);
#if PG
            if (!TempTableInWebCashe(conn_db, Points.Pref + "_kernel.s_status")) return;
            string sql = "select nzp_st from " + Points.Pref + "_kernel.s_status where nzp_st = 41";
#else
            if (!TempTableInWebCashe(conn_db, Points.Pref + "_kernel:s_status")) return;
            string sql = "select nzp_st from " + Points.Pref + "_kernel:s_status where nzp_st = 41";
#endif

            IDataReader reader;
            ret = ExecRead(conn_db, out reader, sql, true);
            if (ret.result)
            {
                if (!reader.Read())
                {
#if PG
                    sql = "insert into " + Points.Pref + "_kernel.s_status (nzp_st, name_st, kod_st) values (41, 'Ожидает распределения', 4)";
#else
                    sql = "insert into " + Points.Pref + "_kernel:s_status (nzp_st, name_st, kod_st) values (41, 'Ожидает распределения', 4)";
#endif
                    ret = ExecSQL(conn_db, sql, true);
                }
                CloseReader(ref reader);
            }

#if PG
            sql = "select nzp_st from " + Points.Pref + "_kernel.s_status where nzp_st = 42";
#else
            sql = "select nzp_st from " + Points.Pref + "_kernel:s_status where nzp_st = 42";
#endif
            ret = ExecRead(conn_db, out reader, sql, true);
            if (ret.result)
            {
                if (!reader.Read())
                {
#if PG
                    sql = "insert into " + Points.Pref + "_kernel.s_status (nzp_st, name_st, kod_st) values (42, 'Ожидает отмены распределения', 4)";
#else
                    sql = "insert into " + Points.Pref + "_kernel:s_status (nzp_st, name_st, kod_st) values (42, 'Ожидает отмены распределения', 4)";
#endif
                    ret = ExecSQL(conn_db, sql, true);
                }
                CloseReader(ref reader);
            }

#if PG
            sql = "select nzp_st from " + Points.Pref + "_kernel.s_status where nzp_st = 51";
#else
            sql = "select nzp_st from " + Points.Pref + "_kernel:s_status where nzp_st = 51";
#endif
            ret = ExecRead(conn_db, out reader, sql, true);
            if (ret.result)
            {
                if (!reader.Read())
                {
#if PG
                    sql = "insert into " + Points.Pref + "_kernel.s_status (nzp_st, name_st, kod_st) values (51, 'Не закрыта', 5)";
#else
                    sql = "insert into " + Points.Pref + "_kernel:s_status (nzp_st, name_st, kod_st) values (51, 'Не закрыта', 5)";
#endif
                    ret = ExecSQL(conn_db, sql, true);
                }
                CloseReader(ref reader);
            }

        }


        /// <summary>Изменение структуры и добавление таблиц в ХХХХ_data
        /// </summary>
        private void Pack_0006_alter_data_kernel_tables(IDbConnection conn_db, out Returns ret)
        {
            ret = Utils.InitReturns();

            string sql;
            IDataReader reader;

            foreach (_Point zap in points)
            {
#if PG
                ret = ExecSQL(conn_db, "set search_path to '" + zap.pref + "_data'", true);
#else
                ret = ExecSQL(conn_db, "database " + zap.pref + "_data", true);
#endif
                if (!ret.result) continue;

                AddFieldToTable(conn_db, "users", "web_user", "integer default 0");

                AddFieldToTable(conn_db, "tarif", "month_calc", "date");

                AlterTableForBlock(conn_db, "counters_spis");
                AlterTableForBlock(conn_db, "nedop_kvar");

                // добавление полей в таблицы
                AlterTablesCnts(conn_db, "counters_spis");
                AlterTablesCnts(conn_db, "nedop_kvar");

                AlterTablesCnts(conn_db, "counters");
                AlterTablesCnts(conn_db, "counters_dom");
                AlterTablesCnts(conn_db, "counters_group");

                CreateCountersArx(conn_db);

                CreateDual(conn_db);
#if PG
                sql = "select distinct prm_num from " + zap.pref + "_kernel.prm_name";
#else
                sql = "select unique prm_num from " + zap.pref + "_kernel:prm_name";
#endif
                ret = ExecRead(conn_db, out reader, sql, true);
                if (!ret.result) continue;

                string prm_xx;
                while (reader.Read())
                {
                    if (reader["prm_num"] != DBNull.Value) prm_xx = Convert.ToString(reader["prm_num"]).Trim();
                    else prm_xx = "";
                    if (prm_xx == "") continue;
                    prm_xx = "prm_" + prm_xx;
                    if (TableInWebCashe(conn_db, prm_xx))
                    {
                        AlterTableForBlock(conn_db, prm_xx);
                        AlterTablesCnts(conn_db, prm_xx);
                    }
                }
                reader.Close();
                reader.Dispose();

                //процедура get_counter
#if PG
                sql = "select * from information_schema.routines where routine_name = 'get_counter' and routine_schema = '" + zap.pref + "_data'";
#else
                sql = "select * from sysprocedures where procname = 'get_counter'";
#endif
                ret = ExecRead(conn_db, out reader, sql, true);
                if (!ret.result) continue;

                if (!reader.Read())
                {
#if PG
                    sql = "create procedure get_counter (pNzpKvar integer, pUsl char(4), pNS char(2), pDatCnt char(8)) "+
                        " returns integer as "+
                        " $BODY$ " +
                        " define xKod char(20); "+
                        " define xNumCnt char(20); "+
                        " define xLengthNumCnt integer; "+
                        " define xNzpCounter integer; "+
                        " define xDatCnt date; "+
                        " begin " +
                        " let xDatCnt=mdy(substr(pDatCnt,5,2), substr(pDatCnt,7,2), substr(pDatCnt,1,4)); "+
                        " let xKod=''; "+
                        " foreach select kod into xKod from perecod where tabid=1 and nzp=pNzpKvar "+
                                " exit foreach; "+
                        " end foreach; "+
                        " if xKod='' then return 0; end if; "+
                        " let xNumCnt=trim(xKod)||trim(pUsl)||trim(pNS); "+
                        " let xLengthNumCnt=length(xNumCnt); "+
                        " let xNzpCounter=0; "+
                        " foreach select nzp_counter "+
                                " into xNzpCounter "+
                                " from counters_spis "+
                                " where nzp=pNzpKvar and  "+
                                      " xDatCnt>=dat_when and  "+
                                      " xDatCnt<=nvl(dat_close,mdy(1,1,3000)) and "+
                                      " (trim(num_cnt)=trim(xNumCnt) or "+
                                      " substr(num_cnt,1,xLengthNumCnt+1)=trim(xNumCnt)||'_') "+
                        " end foreach; "+
                        " return xNzpCounter; " +
                        " end; $BODY$";
#else
                    sql = "create procedure \"are\".get_counter (pNzpKvar integer, pUsl char(4), pNS char(2), pDatCnt char(8)) " +
                        " returning integer; " +
                        " define xKod char(20); " +
                        " define xNumCnt char(20); " +
                        " define xLengthNumCnt integer; " +
                        " define xNzpCounter integer; " +
                        " define xDatCnt date; " +
                        " let xDatCnt=mdy(substr(pDatCnt,5,2), substr(pDatCnt,7,2), substr(pDatCnt,1,4)); " +
                        " let xKod=''; " +
                        " foreach select kod into xKod from perecod where tabid=1 and nzp=pNzpKvar " +
                                " exit foreach; " +
                        " end foreach; " +
                        " if xKod='' then return 0; end if; " +
                        " let xNumCnt=trim(xKod)||trim(pUsl)||trim(pNS); " +
                        " let xLengthNumCnt=length(xNumCnt); " +
                        " let xNzpCounter=0; " +
                        " foreach select nzp_counter " +
                                " into xNzpCounter " +
                                " from counters_spis " +
                                " where nzp=pNzpKvar and  " +
                                      " xDatCnt>=dat_when and  " +
                                      " xDatCnt<=nvl(dat_close,mdy(1,1,3000)) and " +
                                      " (trim(num_cnt)=trim(xNumCnt) or " +
                                      " substr(num_cnt,1,xLengthNumCnt+1)=trim(xNumCnt)||'_') " +
                        " end foreach; " +
                        " return xNzpCounter; " +
                        " end procedure;";
#endif
                    ret = ExecSQL(conn_db, sql, true);
                }
                reader.Close();
                reader.Dispose();
            }

#if PG
            ret = ExecSQL(conn_db, "set search_path to '" + Points.Pref + "_kernel'", true);
#else
            ret = ExecSQL(conn_db, "database " + Points.Pref + "_kernel", true);
#endif
            if (ret.result)
            {
                #region s_vill
#if PG
                if (!TempTableInWebCashe(conn_db, Points.Pref + "_kernel.s_vill"))
                {
                    sql = "CREATE TABLE s_vill( " +
                        " no SERIAL NOT NULL, " +
                        " nzp_vill DECIMAL(13,0), " +
                        " vill CHAR(250), " +
                        " nzp_status INTEGER, " +
                        " kod_raj INTEGER, " +
                        " dat_add TIMESTAMP, " +
                        " undown_level INTEGER default 0, " +
                        " nzp_user DECIMAL(13,0) NOT NULL, " +
                        " dat_when TIMESTAMP, " +
                        " unload_stand INTEGER default 1, " +
                        " unload_level INTEGER default 1) " +
                        " LOCK MODE PAGE;";

                    ret = ExecSQL(conn_db, sql, true);
                    if (ret.result)
                    {
                        ExecSQL(conn_db, "CREATE INDEX ix_s_vill_kraj ON s_vill(kod_raj);", true);
                        ExecSQL(conn_db, "CREATE UNIQUE INDEX ux_s_vill_no ON s_vill(no);", true);
                        ExecSQL(conn_db, "CREATE UNIQUE INDEX ux_s_vill_nzp ON s_vill(nzp_vill);", true);
                    }
                }
#else
                if (!TempTableInWebCashe(conn_db, Points.Pref + "_kernel:s_vill"))
                {
                    sql = "CREATE TABLE \"are\".s_vill( " +
                        " no SERIAL NOT NULL, " +
                        " nzp_vill DECIMAL(13,0), " +
                        " vill CHAR(250), " +
                        " nzp_status INTEGER, " +
                        " kod_raj INTEGER, " +
                        " dat_add DATETIME YEAR to SECOND, " +
                        " undown_level INTEGER default 0, " +
                        " nzp_user DECIMAL(13,0) NOT NULL, " +
                        " dat_when DATETIME YEAR to SECOND NOT NULL, " +
                        " unload_stand INTEGER default 1, " +
                        " unload_level INTEGER default 1) " +
                        " LOCK MODE PAGE;";

                    ret = ExecSQL(conn_db, sql, true);
                    if (ret.result)
                    {
                        ExecSQL(conn_db, "CREATE INDEX \"are\".ix_s_vill_kraj ON \"are\".s_vill(kod_raj);", true);
                        ExecSQL(conn_db, "CREATE UNIQUE INDEX \"are\".ux_s_vill_no ON \"are\".s_vill(no);", true);
                        ExecSQL(conn_db, "CREATE UNIQUE INDEX \"are\".ux_s_vill_nzp ON \"are\".s_vill(nzp_vill);", true);
                    }
                }
#endif
                #endregion

                #region s_reg, s_reg_prm
#if PG
                if (!TempTableInWebCashe(conn_db, Points.Pref + "_kernel.s_reg"))
                {
                    sql = "CREATE TABLE s_reg( " +
                        " nzp_reg SERIAL NOT NULL, " +
                        " name_reg CHAR(80)) " +
                        " LOCK MODE PAGE;";

                    ret = ExecSQL(conn_db, sql, true);
                    if (ret.result)
                    {
                        ExecSQL(conn_db, "CREATE UNIQUE INDEX ix_s_reg ON s_reg(nzp_reg);", true);
                    }
                }

                if (!TempTableInWebCashe(conn_db, Points.Pref + "_kernel.s_reg_prm"))
                {
                    sql = "CREATE TABLE s_reg_prm( " +
                        " nzp_reg INTEGER NOT NULL, " +
                        " nzp_prm INTEGER NOT NULL, " +
                        " nzp_serv INTEGER default 0, " +
                        " numer INTEGER NOT NULL, " +
                        " is_show INTEGER NOT NULL) " +
                        " LOCK MODE PAGE;";

                    ret = ExecSQL(conn_db, sql, true);
                    if (ret.result)
                    {
                        ExecSQL(conn_db, "CREATE INDEX ix_s_reg_prm1 ON s_reg_prm(nzp_reg, nzp_prm);", true);
                        ExecSQL(conn_db, "CREATE INDEX ix_s_reg_prm2 ON s_reg_prm(nzp_reg, numer, is_show);", true);
                        ExecSQL(conn_db, "CREATE INDEX ix_s_reg_prm3 ON s_reg_prm(nzp_prm);", true);
                    }
                }
#else
                if (!TempTableInWebCashe(conn_db, Points.Pref + "_kernel:s_reg"))
                {
                    sql = "CREATE TABLE \"are\".s_reg( " +
                        " nzp_reg SERIAL NOT NULL, " +
                        " name_reg CHAR(80)) " +
                        " LOCK MODE PAGE;";

                    ret = ExecSQL(conn_db, sql, true);
                    if (ret.result)
                    {
                        ExecSQL(conn_db, "CREATE UNIQUE INDEX \"are\".ix_s_reg ON \"are\".s_reg(nzp_reg);", true);
                    }
                }

                if (!TempTableInWebCashe(conn_db, Points.Pref + "_kernel:s_reg_prm"))
                {
                    sql = "CREATE TABLE \"are\".s_reg_prm( " +
                        " nzp_reg INTEGER NOT NULL, " +
                        " nzp_prm INTEGER NOT NULL, " +
                        " nzp_serv INTEGER default 0, " +
                        " numer INTEGER NOT NULL, " +
                        " is_show INTEGER NOT NULL) " +
                        " LOCK MODE PAGE;";

                    ret = ExecSQL(conn_db, sql, true);
                    if (ret.result)
                    {
                        ExecSQL(conn_db, "CREATE INDEX \"are\".ix_s_reg_prm1 ON \"are\".s_reg_prm(nzp_reg, nzp_prm);", true);
                        ExecSQL(conn_db, "CREATE INDEX \"are\".ix_s_reg_prm2 ON \"are\".s_reg_prm(nzp_reg, numer, is_show);", true);
                        ExecSQL(conn_db, "CREATE INDEX \"are\".ix_s_reg_prm3 ON \"are\".s_reg_prm(nzp_prm);", true);
                    }
                }
#endif
                #endregion

                #region s_reason
#if PG
                if (!TempTableInWebCashe(conn_db, Points.Pref + "_kernel.s_reason"))
                {
                    sql = "CREATE TABLE s_reason("+
                       " nzp_reason INTEGER NOT NULL,"+
                       " reason CHAR(40));";

                    ret = ExecSQL(conn_db, sql, true);
                    if (ret.result)
                    {
                        ExecSQL(conn_db, "insert into s_reason(nzp_reason, reason) values (0, 'неопределено');", true);
                        ExecSQL(conn_db, "insert into s_reason(nzp_reason, reason) values (1, 'параметры');", true);
                        ExecSQL(conn_db, "insert into s_reason(nzp_reason, reason) values (2, 'услуги');", true);
                        ExecSQL(conn_db, "insert into s_reason(nzp_reason, reason) values (3, 'недопоставки');", true);
                        ExecSQL(conn_db, "insert into s_reason(nzp_reason, reason) values (4, 'счетчики');", true);
                        ExecSQL(conn_db, "insert into s_reason(nzp_reason, reason) values (5, 'льготы');", true);
                        ExecSQL(conn_db, "insert into s_reason(nzp_reason, reason) values (6, 'жильцы');", true);
                        ExecSQL(conn_db, "insert into s_reason(nzp_reason, reason) values (7, 'вручную');", true);
                        ExecSQL(conn_db, "insert into s_reason(nzp_reason, reason) values (8, 'домовые счетчики');", true);
                        ExecSQL(conn_db, "insert into s_reason(nzp_reason, reason) values (9, 'расход жильца'); ", true);
                        ExecSQL(conn_db, "CREATE UNIQUE INDEX ix_s_reason ON s_reason(nzp_reason);", true);
                        ExecSQL(conn_db, "ALTER TABLE s_reason ADD CONSTRAINT PRIMARY KEY (nzp_reason) CONSTRAINT pk_s_reason;", true);
                        ExecSQL(conn_db, "analyze s_reason;", false);
                    }
                }
#else
                if (!TempTableInWebCashe(conn_db, Points.Pref + "_kernel:s_reason"))
                {
                    sql = "CREATE TABLE \"are\".s_reason(" +
                       " nzp_reason INTEGER NOT NULL," +
                       " reason CHAR(40));";

                    ret = ExecSQL(conn_db, sql, true);
                    if (ret.result)
                    {
                        ExecSQL(conn_db, "insert into \"are\".s_reason(nzp_reason, reason) values (0, 'неопределено');", true);
                        ExecSQL(conn_db, "insert into \"are\".s_reason(nzp_reason, reason) values (1, 'параметры');", true);
                        ExecSQL(conn_db, "insert into \"are\".s_reason(nzp_reason, reason) values (2, 'услуги');", true);
                        ExecSQL(conn_db, "insert into \"are\".s_reason(nzp_reason, reason) values (3, 'недопоставки');", true);
                        ExecSQL(conn_db, "insert into \"are\".s_reason(nzp_reason, reason) values (4, 'счетчики');", true);
                        ExecSQL(conn_db, "insert into \"are\".s_reason(nzp_reason, reason) values (5, 'льготы');", true);
                        ExecSQL(conn_db, "insert into \"are\".s_reason(nzp_reason, reason) values (6, 'жильцы');", true);
                        ExecSQL(conn_db, "insert into \"are\".s_reason(nzp_reason, reason) values (7, 'вручную');", true);
                        ExecSQL(conn_db, "insert into \"are\".s_reason(nzp_reason, reason) values (8, 'домовые счетчики');", true);
                        ExecSQL(conn_db, "insert into \"are\".s_reason(nzp_reason, reason) values (9, 'расход жильца'); ", true);
                        ExecSQL(conn_db, "CREATE UNIQUE INDEX \"are\".ix_s_reason ON \"are\".s_reason(nzp_reason);", true);
                        ExecSQL(conn_db, "ALTER TABLE s_reason ADD CONSTRAINT PRIMARY KEY (nzp_reason) CONSTRAINT \"are\".pk_s_reason;", true);
                        ExecSQL(conn_db, "update statistics for table s_reason;", false);
                    }
                }
#endif
                #endregion

                #region serv_must_calc
#if PG
                if (!TempTableInWebCashe(conn_db, Points.Pref + "_kernel.serv_must_calc"))
                {
                    sql = "CREATE TABLE serv_must_calc(" +
                        " nzp_reason INTEGER," +
                        " nzp_serv INTEGER);";

                    ret = ExecSQL(conn_db, sql, true);
                    if (ret.result)
                    {
                        ExecSQL(conn_db, "insert into serv_must_calc(nzp_reason,nzp_serv) values (6,6);", true);
                        ExecSQL(conn_db, "insert into serv_must_calc(nzp_reason,nzp_serv) values (6,7);", true);
                        ExecSQL(conn_db, "insert into serv_must_calc(nzp_reason,nzp_serv) values (6,9);", true);
                        ExecSQL(conn_db, "insert into serv_must_calc(nzp_reason,nzp_serv) values (6,14);", true);
                        ExecSQL(conn_db, "insert into serv_must_calc(nzp_reason,nzp_serv) values (6,25);", true);

                        ExecSQL(conn_db, "CREATE UNIQUE INDEX ix_serv_must_calc_1 ON serv_must_calc(nzp_reason, nzp_serv);", true);
                        ExecSQL(conn_db, "ALTER TABLE serv_must_calc ADD CONSTRAINT (FOREIGN KEY (nzp_reason) REFERENCES s_reason CONSTRAINT fk_smc_1);", false);
                        ExecSQL(conn_db, "analyze serv_must_calc;", false);
                    }
                }
#else
                if (!TempTableInWebCashe(conn_db, Points.Pref + "_kernel:serv_must_calc"))
                {
                    sql = "CREATE TABLE \"are\".serv_must_calc(" +
                        " nzp_reason INTEGER," +
                        " nzp_serv INTEGER);";

                    ret = ExecSQL(conn_db, sql, true);
                    if (ret.result)
                    {
                        ExecSQL(conn_db, "insert into serv_must_calc(nzp_reason,nzp_serv) values (6,6);", true);
                        ExecSQL(conn_db, "insert into serv_must_calc(nzp_reason,nzp_serv) values (6,7);", true);
                        ExecSQL(conn_db, "insert into serv_must_calc(nzp_reason,nzp_serv) values (6,9);", true);
                        ExecSQL(conn_db, "insert into serv_must_calc(nzp_reason,nzp_serv) values (6,14);", true);
                        ExecSQL(conn_db, "insert into serv_must_calc(nzp_reason,nzp_serv) values (6,25);", true);

                        ExecSQL(conn_db, "CREATE UNIQUE INDEX \"are\".ix_serv_must_calc_1 ON \"are\".serv_must_calc(nzp_reason, nzp_serv);", true);
                        ExecSQL(conn_db, "ALTER TABLE serv_must_calc ADD CONSTRAINT (FOREIGN KEY (nzp_reason) REFERENCES s_reason CONSTRAINT \"are\".fk_smc_1);", false);
                        ExecSQL(conn_db, "update statistics for table serv_must_calc;", false);
                    }
                }
#endif
                #endregion

                #region grpserv_schet
#if PG
                if (!TempTableInWebCashe(conn_db, Points.Pref + "_kernel.grpserv_schet"))
                {
                    sql = " CREATE TABLE grpserv_schet( " +
#else
                if (!TempTableInWebCashe(conn_db, Points.Pref + "_kernel:grpserv_schet"))
                {
                    sql = " CREATE TABLE \"are\".grpserv_schet( " +
#endif
                          " nzp_grpserv INTEGER, " +
                          " nzp_serv INTEGER) " +
                          " LOCK MODE ROW";

                    ret = ExecSQL(conn_db, sql, true);

                }
                #endregion

                #region s_serv_schet
#if PG
                if (!TempTableInWebCashe(conn_db, Points.Pref + "_kernel.s_serv_schet"))
                {
                    sql = " CREATE TABLE s_serv_schet( " +
#else
                        if (!TempTableInWebCashe(conn_db, Points.Pref + "_kernel:s_serv_schet"))
                {
                    sql = " CREATE TABLE \"are\".s_serv_schet( " +
#endif
                          " nzp_grpserv SERIAL NOT NULL, " +
                          " name_grpserv CHAR(50), " +
                          " itog_name CHAR(50)) " +
                          " LOCK MODE ROW";

                    ret = ExecSQL(conn_db, sql, true);

                }
                #endregion

                #region s_listfactura
#if PG
                if (!TempTableInWebCashe(conn_db, Points.Pref + "_kernel.s_listfactura"))
                {
                    sql = "CREATE TABLE s_listfactura( " +
#else
                        if (!TempTableInWebCashe(conn_db, Points.Pref + "_kernel:s_listfactura"))
                {
                    sql = "CREATE TABLE \"are\".s_listfactura( " +
#endif
                        " nzp_lf SERIAL NOT NULL, " +
                        " name_rus CHAR(100), " +
                        " file_name CHAR(100), " +
                        " kind INTEGER, " +
                        " townfilter INTEGER, " +
                        " default_ INTEGER default 0) " +
                        " LOCK MODE ROW";

                    ret = ExecSQL(conn_db, sql, true);
                    if (ret.result)
                    {
#if PG
                        ExecSQL(conn_db, "INSERT INTO s_listfactura(name_rus, file_name, kind, townfilter, default_)" +
#else
                        ExecSQL(conn_db, "INSERT INTO \"are\".s_listfactura(name_rus, file_name, kind, townfilter, default_)" +
#endif
                                         "VALUES ('Демо счет', '~/App_Data/demo.frx', 100, 100, 1)", true);

                    }
                }
                #endregion
            }
#if PG
            ret = ExecSQL(conn_db, "set search_path to'" + Points.Pref + "_data'", true);
#else
            ret = ExecSQL(conn_db, "database " + Points.Pref + "_data", true);
#endif
            if (ret.result)
            {
                CreateDual(conn_db);

                AddFieldToTable(conn_db, "users", "web_user", "integer default 0");
                AddFieldToTable(conn_db, "kvar", "nzp_wp", "integer");
                AddFieldToTable(conn_db, "dom", "nzp_wp", "integer");
                AddFieldToTable(conn_db, "dom", "pref", "char(100)");

#if PG
                sql = "select distinct prm_num from " + Points.Pref + "_kernel.prm_name";
#else
                sql = "select unique prm_num from " + Points.Pref + "_kernel:prm_name";
#endif
                ret = ExecRead(conn_db, out reader, sql, true);
                if (ret.result)
                {
                    string prm_xx;
                    while (reader.Read())
                    {
                        if (reader["prm_num"] != DBNull.Value) prm_xx = Convert.ToString(reader["prm_num"]).Trim();
                        else prm_xx = "";
                        if (prm_xx == "") continue;
                        prm_xx = "prm_" + prm_xx;
                        if (TableInWebCashe(conn_db, prm_xx))
                        {
                            AlterTableForBlock(conn_db, prm_xx);
                            AlterTablesCnts(conn_db, prm_xx);
                        }
                    }
                    reader.Close();
                }
#if PG
                if (!TempTableInWebCashe(conn_db, Points.Pref+"_data.rajon_vill"))
                {
                    sql = "CREATE TABLE rajon_vill( " +
#else
                if (!TempTableInWebCashe(conn_db, Points.Pref + "_data:rajon_vill"))
                {
                    sql = "CREATE TABLE \"are\".rajon_vill( " +
#endif
                        " nzp SERIAL NOT NULL, " +
                        " nzp_raj INTEGER, " +
                        " nzp_vill DECIMAL(13,0)) " +
                        " LOCK MODE ROW";

                    ret = ExecSQL(conn_db, sql, true);
                    if (ret.result)
                    {
#if PG
                        ExecSQL(conn_db, "CREATE UNIQUE INDEX ix_rajon_vill_1 ON rajon_vill(nzp)", true);
                        ExecSQL(conn_db, "CREATE INDEX ix_rajon_vill_2 ON rajon_vill(nzp_raj)", true);
#else
                        ExecSQL(conn_db, "CREATE UNIQUE INDEX \"are\".ix_rajon_vill_1 ON \"are\".rajon_vill(nzp)", true);
                        ExecSQL(conn_db, "CREATE INDEX \"are\".ix_rajon_vill_2 ON \"are\".rajon_vill(nzp_raj)", true);
#endif
                    }
                }

                #region s_remark
#if PG
                if (!TempTableInWebCashe(conn_db, Points.Pref + "_data.s_remark"))
                {
                    sql = "CREATE TABLE s_remark( " +
#else
                        if (!TempTableInWebCashe(conn_db, Points.Pref + "_data:s_remark"))
                {
                    sql = "CREATE TABLE \"are\".s_remark( " +
#endif
                        " nzp_area INTEGER default 0, " +
                        " nzp_geu INTEGER default 0, " +
                        " nzp_dom INTEGER default 0, " +
                        " remark CHAR(250)) " +
                        " LOCK MODE ROW";

                    ret = ExecSQL(conn_db, sql, true);
                    if (ret.result)
                    {
#if PG
                        ExecSQL(conn_db, "CREATE INDEX ix_s_remark_01 ON s_remark(nzp_area)", true);
                        ExecSQL(conn_db, "CREATE INDEX ix_s_remark_02 ON s_remark(nzp_geu)", true);
                        ExecSQL(conn_db, "CREATE INDEX ix_s_remark_03 ON s_remark(nzp_dom)", true);
#else
                        ExecSQL(conn_db, "CREATE INDEX \"are\".ix_s_remark_01 ON \"are\".s_remark(nzp_area)", true);
                        ExecSQL(conn_db, "CREATE INDEX \"are\".ix_s_remark_02 ON \"are\".s_remark(nzp_geu)", true);
                        ExecSQL(conn_db, "CREATE INDEX \"are\".ix_s_remark_03 ON \"are\".s_remark(nzp_dom)", true);
#endif
                    }
                }
                #endregion
#if PG
                if (!TempTableInWebCashe(conn_db, Points.Pref + "_data.prefer"))
                {
                    sql = "CREATE TABLE prefer( " +
                        " nzp_pref SERIAL NOT NULL," +
                        " nzp_user INTEGER," +
                        " p_mode CHAR(20)," +
                        " p_type CHAR(20)," +
                        " p_name CHAR(20)," +
                        " p_value CHAR(100)) ";
#else
                        if (!TempTableInWebCashe(conn_db, Points.Pref + "_data:prefer"))
                {
                    sql = "CREATE TABLE \"are\".prefer( " +
                        " nzp_pref SERIAL NOT NULL," +
                        " nzp_user INTEGER," +
                        " p_mode CHAR(20)," +
                        " p_type CHAR(20)," +
                        " p_name CHAR(20)," +
                        " p_value CHAR(100)) " +
                        " LOCK MODE PAGE";
#endif

                    ret = ExecSQL(conn_db, sql, true);
                }
            }
        }

        //----------------------------------------------------------------------
        private void Pack_0005_counters_vals(IDbConnection conn_db, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            foreach (_Point zap in points)
            {
#if PG
                ExecSQL(conn_db, "set search_path to '" + zap.pref + "_charge_" + (calcMonth.year_ - 2000).ToString("00") + "'", true);
                string owner = "";
#else
                ExecSQL(conn_db, "database " + zap.pref + "_charge_" + (calcMonth.year_ - 2000).ToString("00"), true);
                string owner = "are.";
#endif
                string counters_vals = "counters_vals";

                if (!TempTableInWebCashe(conn_db, counters_vals))
                {
                    ret = ExecSQL(conn_db,
                        " create table " + owner + counters_vals + " ( " +
                            " nzp_cv           serial not null, " +
                            " nzp              integer not null, " +
                            " nzp_type         integer not null, " +
                            " nzp_counter      integer not null, " +
                            " month_           integer not null, " +
                            " dat_uchet        date, " +
                            " val_cnt          float, " +
                            " ngp_cnt          decimal(14,7) default 0.0000000, " +
                            " ngp_lift         decimal(14,7) default 0.0000000, " +
                            " nzp_user         integer, " +
                            " dat_when         date, " +
                            " ist              integer default 0, " +
                            " is_new           integer," +
                            " iscalc           integer default 0" +
                            " ); ", true);
                    if (!ret.result)
                    {
                        ret.text = "Ошибка создания таблицы counters_vals";
                        break;
                    }

                    string ix = owner + "ix_counters_vals";
                    ret = ExecSQL(conn_db, " Create unique index " + ix + "_1 on " + owner + counters_vals + " (nzp_cv) ", true);
                    if (ret.result) ret = ExecSQL(conn_db, " Create index " + ix + "_2 on " + owner + counters_vals + " (nzp_counter, month_, ist, dat_uchet) ", true);
                    if (ret.result) ret = ExecSQL(conn_db, " Create index " + ix + "_3 on " + owner + counters_vals + " (nzp_type, nzp, month_, ist, dat_uchet) ", true);
                    if (ret.result) ret = ExecSQL(conn_db, " Create index " + ix + "_5 on " + owner + counters_vals + " (nzp, iscalc) ", true);
                }

                ret = AddFieldToTable(conn_db, counters_vals, "is_new", "integer");
                if (!ret.result)
                {
                    break;
                }

                if (!isTableHasColumn(conn_db, counters_vals, "iscalc"))
                {
                    ret = ExecSQL(conn_db, "alter table counters_vals add iscalc integer default 0", true);
                    if (!ret.result)
                    {
                        ret.text = "Ошибка изменения структуры таблицы counters_vals при добавлении поля iscalc ";
                        break;
                    }
#if PG
                    ret = ExecSQL(conn_db, "create index ix_counters_vals_5 on counters_vals(nzp,iscalc) ", true);
#else
                    ret = ExecSQL(conn_db, "create index are.ix_counters_vals_5 on counters_vals(nzp,iscalc) ", true);
#endif
                    if (!ret.result)
                    {
                        ret.text = "Ошибка создания индекса are.ix_counters_vals_5 on counters_vals(nzp,iscalc) ";
                        break;
                    }
                }

#if PG
#else
                ExecSQL(conn_db, "alter table " + counters_vals + " lock mode (row)", false);
#endif
            }
        }
        //----------------------------------------------------------------------
        private void Pack_0004_charge_cnts(IDbConnection conn_db, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            foreach (_Point zap in points)
            {
#if PG
                ExecSQL(conn_db, "set search_path to '" + zap.pref + "_charge_" + (calcMonth.year_ - 2000).ToString("00") + "'", true);
#else
                ExecSQL(conn_db, "database " + zap.pref + "_charge_" + (calcMonth.year_ - 2000).ToString("00"), true);
#endif

                //charge_cnts
                if (!isTableHasColumn(conn_db, "charge_cnts", "nzp_counter"))
                {
                    ret = ExecSQL(conn_db, "alter table charge_cnts add nzp_counter integer ", true);
                    if (!ret.result)
                    {
                        ret.text = "Ошибка изменения структуры таблицы charge_cnts при добавлении поля nzp_counter ";
                        break;
                    }
                    ret = ExecSQL(conn_db, "alter table charge_cnts add iscalc integer default 0", true);
                    if (!ret.result)
                    {
                        ret.text = "Ошибка изменения структуры таблицы charge_cnts при добавлении поля iscalc ";
                        break;
                    }
#if PG
                    ret = ExecSQL(conn_db, "create index ix_chcnts_4 on charge_cnts(nzp_counter) ", true);
#else
                    ret = ExecSQL(conn_db, "create index are.ix_chcnts_4 on charge_cnts(nzp_counter) ", true);
#endif
                    if (!ret.result)
                    {
                        ret.text = "Ошибка изменения структуры таблицы charge_cnts при добавлении поля nzp_counter ";
                        break;
                    }
#if PG
                    ret = ExecSQL(conn_db, "create index ix_chcnts_5 on charge_cnts(nzp_kvar,iscalc) ", true);
#else
                    ret = ExecSQL(conn_db, "create index are.ix_chcnts_5 on charge_cnts(nzp_kvar,iscalc) ", true);
#endif
                    if (!ret.result)
                    {
                        ret.text = "Ошибка изменения структуры таблицы charge_cnts при добавлении поля nzp_counter ";
                        break;
                    }

                    ret = ExecSQL(conn_db, "alter table charge_cnts add prev2_dat date", true);
                    if (!ret.result)
                    {
                        ret.text = "Ошибка изменения структуры таблицы charge_cnts при добавлении поля prev2_dat ";
                        break;
                    }
                }
                //charge_nedo
                if (!isTableHasColumn(conn_db, "charge_nedo", "iscalc"))
                {
                    ret = ExecSQL(conn_db, "alter table charge_nedo add iscalc integer default 0", true);
                    if (!ret.result)
                    {
                        ret.text = "Ошибка изменения структуры таблицы charge_nedo при добавлении поля iscalc ";
                        break;
                    }
#if PG
                    ret = ExecSQL(conn_db, "create index ix_chnedo_5 on charge_nedo(nzp_kvar,iscalc) ", true);
#else
                    ret = ExecSQL(conn_db, "create index are.ix_chnedo_5 on charge_nedo(nzp_kvar,iscalc) ", true);
#endif
                    if (!ret.result)
                    {
                        ret.text = "Ошибка изменения структуры таблицы charge_cnts при добавлении поля nzp_counter ";
                        break;
                    }
                }

                if (!isTableHasColumn(conn_db, "perekidka", "nzp_reestr"))
                {
                    ret = ExecSQL(conn_db, "alter table perekidka add nzp_reestr integer", true);
                    if (!ret.result)
                    {
                        ret.text = "Ошибка изменения структуры таблицы perekidka при добавлении поля isnzp_reestrcalc ";
                        break;
                    }
                }
            }
        }
        //----------------------------------------------------------------------
        private void Pack_0003_series(IDbConnection conn_db, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

#if PG
            ret = ExecSQL(conn_db, "set search_path to '" + Points.Pref + "_data'", true);

            if (!ProcedureInWebCashe(conn_db, "get_series"))
            {
                StringBuilder sql = new StringBuilder();
                ret = ExecSQL(conn_db, "drop procedure get_series ", false);

                sql.Append(" CREATE PROCEDURE get_series(pKod INTEGER) " + Environment.NewLine);
                sql.Append(" RETURNS TABLE (INTEGER cur_val, CHAR(255) ret_Message) " + Environment.NewLine);
                sql.Append(" BEGIN " + Environment.NewLine);
                sql.Append(" DEFINE xCurVal INTEGER; " + Environment.NewLine);
                sql.Append(" DEFINE xRetErr INTEGER; " + Environment.NewLine);
                sql.Append(" DEFINE xRetMess CHAR(255); " + Environment.NewLine);
                sql.Append(" DEFINE xVMin INTEGER; " + Environment.NewLine);
                sql.Append(" DEFINE xVMax INTEGER; " + Environment.NewLine);
                sql.Append(" ON EXCEPTION COMMIT WORK; RETURN xRetErr, xRetMess; END EXCEPTION " + Environment.NewLine);

                sql.Append(" IF pKod=9325 THEN " + Environment.NewLine);
                sql.Append("    RETURN 1, 'Номер версии 1, 04.07.2012'; " + Environment.NewLine);
                sql.Append(" END IF; " + Environment.NewLine);

                sql.Append(" LET xRetErr=-1; LET xRetMess='Ошибка блокирования series'; " + Environment.NewLine);
                sql.Append(" BEGIN WORK; " + Environment.NewLine);
                sql.Append(" LOCK TABLE series IN EXCLUSIVE MODE; " + Environment.NewLine);
                sql.Append(" LET xRetErr=-2; LET xRetMess='Ошибка обращения к series'; " + Environment.NewLine);
                sql.Append(" LET xCurVal=0;   " + Environment.NewLine);
                sql.Append(" FOREACH " + Environment.NewLine);
                sql.Append(" SELECT NVL(v_min,0), NVL(v_max,0), cur_val " + Environment.NewLine);
                sql.Append(" INTO xVMin, xVMax, xCurVal " + Environment.NewLine);
                sql.Append(" FROM series " + Environment.NewLine);
                sql.Append(" WHERE kod=pKod " + Environment.NewLine);
                sql.Append(" EXIT FOREACH; " + Environment.NewLine);
                sql.Append(" END FOREACH; " + Environment.NewLine);
                sql.Append(" IF NVL(xCurVal,0)=0 THEN " + Environment.NewLine);
                sql.Append("    COMMIT WORK;  " + Environment.NewLine);
                sql.Append("    RETURN -3, 'Внутренняя ошибка series';  " + Environment.NewLine);
                sql.Append(" END IF; " + Environment.NewLine);
                sql.Append(" IF NOT xCurVal BETWEEN xVMin AND xVMax THEN " + Environment.NewLine);
                sql.Append("    COMMIT WORK;  " + Environment.NewLine);
                sql.Append("    RETURN -4, 'Недопустимые значения series';  " + Environment.NewLine);
                sql.Append(" END IF; " + Environment.NewLine);
                sql.Append(" LET xRetErr=-5; LET xRetMess='Ошибка изменения series'; " + Environment.NewLine);
                sql.Append(" UPDATE series SET cur_val=xCurVal+1 WHERE kod=pKod; " + Environment.NewLine);
                sql.Append(" LET xRetErr=-6; LET xRetMess='Ошибка сохранения series'; " + Environment.NewLine);
                sql.Append(" COMMIT WORK;  " + Environment.NewLine);
                sql.Append(" RETURN xCurVal, ''; " + Environment.NewLine);
                sql.Append(" END; " + Environment.NewLine);
#else
                ret = ExecSQL(conn_db, "database " + Points.Pref + "_data", true);

            if (!ProcedureInWebCashe(conn_db, "get_series"))
            {
                StringBuilder sql = new StringBuilder();
                ret = ExecSQL(conn_db, "drop procedure get_series ", false);

                sql.Append(" CREATE PROCEDURE are.get_series(pKod INTEGER) " + Environment.NewLine);
                sql.Append(" RETURNING INTEGER AS cur_val, CHAR(255) AS ret_Message; " + Environment.NewLine);
                sql.Append(" DEFINE xCurVal INTEGER; " + Environment.NewLine);
                sql.Append(" DEFINE xRetErr INTEGER; " + Environment.NewLine);
                sql.Append(" DEFINE xRetMess CHAR(255); " + Environment.NewLine);
                sql.Append(" DEFINE xVMin INTEGER; " + Environment.NewLine);
                sql.Append(" DEFINE xVMax INTEGER; " + Environment.NewLine);
                sql.Append(" ON EXCEPTION COMMIT WORK; RETURN xRetErr, xRetMess; END EXCEPTION " + Environment.NewLine);

                sql.Append(" IF pKod=9325 THEN " + Environment.NewLine);
                sql.Append("    RETURN 1, 'Номер версии 1, 04.07.2012'; " + Environment.NewLine);
                sql.Append(" END IF; " + Environment.NewLine);

                sql.Append(" LET xRetErr=-1; LET xRetMess='Ошибка блокирования series'; " + Environment.NewLine);
                sql.Append(" BEGIN WORK; " + Environment.NewLine);
                sql.Append(" LOCK TABLE series IN EXCLUSIVE MODE; " + Environment.NewLine);
                sql.Append(" LET xRetErr=-2; LET xRetMess='Ошибка обращения к series'; " + Environment.NewLine);
                sql.Append(" LET xCurVal=0;   " + Environment.NewLine);
                sql.Append(" FOREACH " + Environment.NewLine);
                sql.Append(" SELECT NVL(v_min,0), NVL(v_max,0), cur_val " + Environment.NewLine);
                sql.Append(" INTO xVMin, xVMax, xCurVal " + Environment.NewLine);
                sql.Append(" FROM series " + Environment.NewLine);
                sql.Append(" WHERE kod=pKod " + Environment.NewLine);
                sql.Append(" EXIT FOREACH; " + Environment.NewLine);
                sql.Append(" END FOREACH; " + Environment.NewLine);
                sql.Append(" IF NVL(xCurVal,0)=0 THEN " + Environment.NewLine);
                sql.Append("    COMMIT WORK;  " + Environment.NewLine);
                sql.Append("    RETURN -3, 'Внутренняя ошибка series';  " + Environment.NewLine);
                sql.Append(" END IF; " + Environment.NewLine);
                sql.Append(" IF NOT xCurVal BETWEEN xVMin AND xVMax THEN " + Environment.NewLine);
                sql.Append("    COMMIT WORK;  " + Environment.NewLine);
                sql.Append("    RETURN -4, 'Недопустимые значения series';  " + Environment.NewLine);
                sql.Append(" END IF; " + Environment.NewLine);
                sql.Append(" LET xRetErr=-5; LET xRetMess='Ошибка изменения series'; " + Environment.NewLine);
                sql.Append(" UPDATE series SET cur_val=xCurVal+1 WHERE kod=pKod; " + Environment.NewLine);
                sql.Append(" LET xRetErr=-6; LET xRetMess='Ошибка сохранения series'; " + Environment.NewLine);
                sql.Append(" COMMIT WORK;  " + Environment.NewLine);
                sql.Append(" RETURN xCurVal, ''; " + Environment.NewLine);
                sql.Append(" END PROCEDURE; " + Environment.NewLine);
#endif

                ret = ExecSQL(conn_db, sql.ToString(), true);
            }
        }
        //----------------------------------------------------------------------
        private void Pack_0002(IDbConnection conn_db, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();
            /*string sql;
            if (points != null)
            {
                foreach (_Point point in points)
                {
                    if (!TempTableInWebCashe(conn_db, point.pref + "_data:dependencies"))
                    {
                        ret = ExecSQL(conn_db, "database " + point.pref + "_data", true);
                        if (!ret.result) continue;

                        sql = "create table \"pasp\".dependencies (" +
                            " nzp_dep serial not null ," +
                            " tabname_from char(30)," +
                            " fieldname_from char(30)," +
                            " tabname_to char(30)," +
                            " fieldname_to char(30) )";
                        ret = ExecSQL(conn_db, sql, true);
                        if (!ret.result) continue;

                        ExecSQL(conn_db, "CREATE UNIQUE INDEX \"pasp\".ix_dependencies_1 ON \"pasp\".dependencies(nzp_dep)", false);
                        ExecSQL(conn_db, "CREATE UNIQUE INDEX \"pasp\".ix_dependencies_2 ON \"pasp\".dependencies(tabname_from, fieldname_from)", false);
                    }

                    ExecSQL(conn_db, "delete from " + point.pref + "_data:dependencies", false);
                    ExecSQL(conn_db, "insert into " + point.pref + "_data:dependencies values (0, 'kart', 'nzp_celp', 's_cel', 'nzp_cel')", false);
                    ExecSQL(conn_db, "insert into " + point.pref + "_data:dependencies values (0, 'kart', 'nzp_celu', 's_cel', 'nzp_cel')", false);
                    ExecSQL(conn_db, "insert into " + point.pref + "_data:dependencies values (0, 'kart_arx', 'nzp_celp', 's_cel', 'nzp_cel')", false);
                    ExecSQL(conn_db, "insert into " + point.pref + "_data:dependencies values (0, 'kart_arx', 'nzp_celu', 's_cel', 'nzp_cel')", false);
                    ExecSQL(conn_db, "insert into " + point.pref + "_data:dependencies values (0, 'kart', 'nzp_dok', 's_dok', 'nzp_dok')", false);
                    ExecSQL(conn_db, "insert into " + point.pref + "_data:dependencies values (0, 'kart_arx', 'nzp_dok', 's_dok', 'nzp_dok')", false);
                    ExecSQL(conn_db, "insert into " + point.pref + "_data:dependencies values (0, 'sobstw', 'nzp_dok', 's_dok', 'nzp_dok')", false);
                    ExecSQL(conn_db, "insert into " + point.pref + "_data:dependencies values (0, 'kart', 'nzp_rod', 's_rod', 'nzp_rod')", false);
                    ExecSQL(conn_db, "insert into " + point.pref + "_data:dependencies values (0, 'kart_arx', 'nzp_rod', 's_rod', 'nzp_rod')", false);
                    ExecSQL(conn_db, "insert into " + point.pref + "_data:dependencies values (0, 'sobstw', 'nzp_rod', 's_rod', 'nzp_rod')", false);
                    ExecSQL(conn_db, "insert into " + point.pref + "_data:dependencies values (0, 'grgd', 'nzp_grgd', 's_grgd', 'nzp_grgd')", false);
                    ExecSQL(conn_db, "insert into " + point.pref + "_data:dependencies values (0, 'dom', 'nzp_raj', 's_rajon_dom', 'nzp_raj_dom')", false);
                    ExecSQL(conn_db, "insert into " + point.pref + "_data:dependencies values (0, 'sobstw', 'nzp_dok_sv', 's_dok_sv', 'nzp_dok_sv')", false);
                    ExecSQL(conn_db, "insert into " + point.pref + "_data:dependencies values (0, 'kart', 'nzp_lnku', 's_land', 'nzp_land')", false);
                    ExecSQL(conn_db, "insert into " + point.pref + "_data:dependencies values (0, 'kart', 'nzp_lnmr', 's_land', 'nzp_land')", false);
                    ExecSQL(conn_db, "insert into " + point.pref + "_data:dependencies values (0, 'kart', 'nzp_lnop', 's_land', 'nzp_land')", false);
                    ExecSQL(conn_db, "insert into " + point.pref + "_data:dependencies values (0, 'kart_arx', 'nzp_lnku', 's_land', 'nzp_land')", false);
                    ExecSQL(conn_db, "insert into " + point.pref + "_data:dependencies values (0, 'kart_arx', 'nzp_lnmr', 's_land', 'nzp_land')", false);
                    ExecSQL(conn_db, "insert into " + point.pref + "_data:dependencies values (0, 'kart_arx', 'nzp_lnop', 's_land', 'nzp_land')", false);
                    ExecSQL(conn_db, "insert into " + point.pref + "_data:dependencies values (0, 'kart', 'nzp_stku', 's_stat', 'nzp_stat')", false);
                    ExecSQL(conn_db, "insert into " + point.pref + "_data:dependencies values (0, 'kart', 'nzp_stmr', 's_stat', 'nzp_stat')", false);
                    ExecSQL(conn_db, "insert into " + point.pref + "_data:dependencies values (0, 'kart', 'nzp_stop', 's_stat', 'nzp_stat')", false);
                    ExecSQL(conn_db, "insert into " + point.pref + "_data:dependencies values (0, 'kart_arx', 'nzp_stku', 's_stat', 'nzp_stat')", false);
                    ExecSQL(conn_db, "insert into " + point.pref + "_data:dependencies values (0, 'kart_arx', 'nzp_stmr', 's_stat', 'nzp_stat')", false);
                    ExecSQL(conn_db, "insert into " + point.pref + "_data:dependencies values (0, 'kart_arx', 'nzp_stop', 's_stat', 'nzp_stat')", false);
                    ExecSQL(conn_db, "insert into " + point.pref + "_data:dependencies values (0, 'kart', 'nzp_tnku', 's_town', 'nzp_town')", false);
                    ExecSQL(conn_db, "insert into " + point.pref + "_data:dependencies values (0, 'kart', 'nzp_tnmr', 's_town', 'nzp_town')", false);
                    ExecSQL(conn_db, "insert into " + point.pref + "_data:dependencies values (0, 'kart', 'nzp_tnop', 's_town', 'nzp_town')", false);
                    ExecSQL(conn_db, "insert into " + point.pref + "_data:dependencies values (0, 'kart_arx', 'nzp_tnku', 's_town', 'nzp_town')", false);
                    ExecSQL(conn_db, "insert into " + point.pref + "_data:dependencies values (0, 'kart_arx', 'nzp_tnmr', 's_town', 'nzp_town')", false);
                    ExecSQL(conn_db, "insert into " + point.pref + "_data:dependencies values (0, 'kart_arx', 'nzp_tnop', 's_town', 'nzp_town')", false);
                    ExecSQL(conn_db, "insert into " + point.pref + "_data:dependencies values (0, 'kart', 'nzp_rnku', 's_rajon', 'nzp_raj')", false);
                    ExecSQL(conn_db, "insert into " + point.pref + "_data:dependencies values (0, 'kart', 'nzp_rnmr', 's_rajon', 'nzp_raj')", false);
                    ExecSQL(conn_db, "insert into " + point.pref + "_data:dependencies values (0, 'kart', 'nzp_rnop', 's_rajon', 'nzp_raj')", false);
                    ExecSQL(conn_db, "insert into " + point.pref + "_data:dependencies values (0, 'kart_arx', 'nzp_rnku', 's_rajon', 'nzp_raj')", false);
                    ExecSQL(conn_db, "insert into " + point.pref + "_data:dependencies values (0, 'kart_arx', 'nzp_rnmr', 's_rajon', 'nzp_raj')", false);
                    ExecSQL(conn_db, "insert into " + point.pref + "_data:dependencies values (0, 'kart_arx', 'nzp_rnop', 's_rajon', 'nzp_raj')", false);

                    ExecSQL(conn_db, "update statistics for table " + point.pref + "_data@" + conn_db.Server + ":dependencies;", false);
                }
            }*/

            // добавление первичного ключа в l_foss
            List<_Point> list = new List<_Point>();
            list.Add(new _Point() { pref = Points.Pref });
            if (points != null) list.AddRange(points);
            foreach (_Point point in list)
            {
#if PG
                ret = ExecSQL(conn_db, "set search_path to '" + point.pref + "_kernel'", true);
#else
                ret = ExecSQL(conn_db, "database " + point.pref + "_kernel", true);
#endif
                if (!ret.result) continue;

                if (!TempTableInWebCashe(conn_db, "l_foss")) return; // xxx_kernel:l_foss

                if (!isTableHasColumn(conn_db, "l_foss", "nzp_foss"))
                {
                    ret = ExecSQL(conn_db, "alter table l_foss add nzp_foss integer before nzp_serv", true);
#if PG
                    if (ret.result) ret = ExecSQL(conn_db, "update l_foss set nzp_foss = oid", true);
#else
                    if (ret.result) ret = ExecSQL(conn_db, "update l_foss set nzp_foss = rowid", true);
#endif
                    if (ret.result) ret = ExecSQL(conn_db, "alter table l_foss modify nzp_foss serial not null", true);
                    if (ret.result) ret = ExecSQL(conn_db, "create unique index ix_l_foss_1 on l_foss (nzp_foss)", true);
                    if (ret.result) ret = ExecSQL(conn_db, "update statistics for table l_foss", false);
                }
            }
        }
        //----------------------------------------------------------------------
        private void Pack_0001(IDbConnection conn_db, out Returns ret)
        //----------------------------------------------------------------------
        {
            ret = Utils.InitReturns();

            string table = "s_payer";
            string sql = "";

            if (TableInWebCashe(conn_db, table))
            {
                string index = "ix3_" + table;
                if (!isHasIndex(conn_db, index))
                {
#if PG
                    sql = "create index " + index + " on " + table + " (nzp_supp) ";
                    ret = ExecSQL(conn_db, sql, true);

                    ret = ExecSQL(conn_db, "analyze " + table, true);
#else
                    sql = "create index are." + index + " on " + table + " (nzp_supp) ";
                    ret = ExecSQL(conn_db, sql, true);

                    ret = ExecSQL(conn_db, "update statistics for table " + table, true);
#endif
                }
            }

            DbCalcCharge dbc = new DbCalcCharge();
            dbc.AlterTableReport(out ret);
            dbc.Close();
        }

        private Returns AlterTablesCnts(IDbConnection conn_db, string table)
        {
            Returns ret = Utils.InitReturns();

            //поле, отвечающее за перерасчет указанного периода
            if (!isTableHasColumn(conn_db, table, "month_calc"))
            {
                string sql = "alter table " + table + " add month_calc date";
                ret = ExecSQL(conn_db, sql, true);
                if (!ret.result)
                {
                    ret.text = "Ошибка изменения структуры таблицы " + table + " при добавлении поля month_calc ";
                }
                else
                {
#if PG
                    sql = "create index x3_" + table + " on " + table + " (month_calc) ";
#else
                    sql = "create index are.x3_" + table + " on " + table + " (month_calc) ";
#endif
                    ret = ExecSQL(conn_db, sql, true);
                    if (!ret.result)
                    {
                        ret.text = "Ошибка создания индекса для " + table + ".month_calc ";
                    }
                }
            }

            AddFieldToTable(conn_db, table, "user_del", "integer");
            AddFieldToTable(conn_db, table, "dat_del", "date");
            AddFieldToTable(conn_db, table, "dat_s", "date");
            AddFieldToTable(conn_db, table, "dat_po", "date");

            return ret;
        }

        /// <summary> Добавляет к таблице поля, необходимые для блокировки записи
        /// </summary>
        /// <param name="conn_db">Соединение с базой данных, в которой находится таблица</param>
        /// <param name="table">Имя таблицы без наименования базы данных</param>
        /// <returns></returns>
        private Returns AlterTableForBlock(IDbConnection conn_db, string table)
        {
            return AlterTableForBlock(conn_db, table, "");
        }
        private Returns AlterTableForBlock(IDbConnection conn_db, string table, string database)
        {
            Returns ret = new Returns(true);
#if PG
            if (database != "") ret = ExecSQL(conn_db, "set search_path to '" + database + "'", false);
#else
            if (database != "") ret = ExecSQL(conn_db, "database " + database, false);
#endif
            if (ret.result)
            {
#if PG
                ret = AddFieldToTable(conn_db, table, "dat_block", "timestamp");
#else
                ret = AddFieldToTable(conn_db, table, "dat_block", "datetime year to minute");
#endif
                if (ret.result)
                    ret = AddFieldToTable(conn_db, table, "user_block", "integer");
            }
            return ret;
        }

        /// <summary> Создать таблицу COUNTERS_ARX, если ее еще нет
        /// </summary>
        /// <param name="pref">Префикс БД</param>
        /// <param name="conn_db">Открытое соединение с БД</param>
        /// <returns>Результат выполнения операции</returns>
        private Returns CreateCountersArx(IDbConnection conn_db)
        {
            Returns ret;
            if (!TableInWebCashe(conn_db, "counters_arx"))
            {
#if PG
                ret = ExecSQL(conn_db, " create table counters_arx " +
#else
                ret = ExecSQL(conn_db, " create table are.counters_arx " +
#endif
                                       "  ( nzp_arx      serial not null, " +
                                       "    nzp_counter  integer not null, " +  //1,2,3
                                       "    pole         char(40)," +
                                       "    val_old      char(20)," +
                                       "    val_new      char(20)," +
                                       "    nzp_user     integer, " +
                                       "    dat_calc     date, " + //расчетный месяц
                                       "    dat_when     date )", true);
                if (!ret.result) return ret;

#if PG
                string ix = "ix_counters_arx";
#else
                string ix = "are.ix_counters_arx";
#endif
                Returns ret2 = ExecSQL(conn_db, " Create unique index " + ix + "_1 on counters_arx (nzp_arx) ", true);
                if (ret2.result) ret2 = ExecSQL(conn_db, " Create index " + ix + "_2 on counters_arx (nzp_counter) ", true);
            }
            else
            {
                ret = AddFieldToTable(conn_db, "counters_arx", "dat_calc", "date");
            }
            return ret;
        }
        
        private void CreateDual(IDbConnection conn_db)
        {
            if (!TableInWebCashe(conn_db, "dual"))
            {
#if PG
                ExecSQL(conn_db, " Create table dual (nzp integer default 0)", true);
                ExecSQL(conn_db, " Create unique index ix1_dual on are.dual (nzp)", true);
                ExecSQL(conn_db, " Insert into dual Values (1)", true);
#else
                ExecSQL(conn_db, " Create table are.dual (nzp integer default 0)", true);
                ExecSQL(conn_db, " Create unique index are.ix1_dual on are.dual (nzp)", true);
                ExecSQL(conn_db, " Insert into are.dual Values (1)", true);
#endif
            }
        }

        /// <summary> Создать таблицу REESTR_PEREKIDOK, если ее еще нет
        /// </summary>
        private Returns CreateReestrPerekidok(IDbConnection conn_db)
        {
            Returns ret = Utils.InitReturns();

            if (!TableInWebCashe(conn_db, "reestr_perekidok"))
            {
#if PG
                ret = ExecSQL(conn_db, " create table reestr_perekidok " +
                                       "  ( nzp_reestr     serial not null, " +
                                       "    month_    integer not null, " +
                                       "    year_     integer not null, " +
                                       "    comment        char(250)," +
                                       "    sposob_raspr   integer," +
                                       "    nzp_oper     integer, " +
                                       "    nzp_serv     integer, " +
                                       "    nzp_supp     integer, " +
                                       "    nzp_serv_on     integer, " +
                                       "    nzp_supp_on     integer, " +
                                       "    saldo_part      integer, " +
                                       "    sum       DECIMAL(14,2) default 0.00, " +
                                       "    is_actual     integer, " +
                                       "    changed_by     integer, " + //расчетный месяц
                                       "    changed_on     timestamp, " +
                                       "    created_by     integer, " + //расчетный месяц
                                       "    created_on     timestamp )", true);
                if (!ret.result) return ret;

                string ix = "ix_reestr_perekidok";
#else
                ret = ExecSQL(conn_db, " create table are.reestr_perekidok " +
                                       "  ( nzp_reestr     serial not null, " +
                                       "    month_    integer not null, " +
                                       "    year_     integer not null, " +
                                       "    comment        char(250)," +
                                       "    sposob_raspr   integer," +
                                       "    nzp_oper     integer, " +
                                       "    nzp_serv     integer, " +
                                       "    nzp_supp     integer, " +
                                       "    nzp_serv_on     integer, " +
                                       "    nzp_supp_on     integer, " +
                                       "    saldo_part      integer, " +
                                       "    sum       DECIMAL(14,2) default 0.00, " +
                                       "    is_actual     integer, " +
                                       "    changed_by     integer, " + //расчетный месяц
                                       "    changed_on     DATETIME YEAR to MINUTE, " +
                                       "    created_by     integer, " + //расчетный месяц
                                       "    created_on     DATETIME YEAR to MINUTE )", true);
                if (!ret.result) return ret;

                string ix = "are.ix_reestr_perekidok";
#endif
                Returns ret2 = ExecSQL(conn_db, " Create unique index " + ix + "_1 on reestr_perekidok (nzp_reestr) ", true);
            }

            if (!isTableHasColumn(conn_db, "reestr_perekidok", "month_", Points.Pref + "_data"))
            {
                ret = ExecSQL(conn_db, "alter table reestr_perekidok add month_ integer", true);
                if (!ret.result)
                {
                    ret.text = "Ошибка изменения структуры таблицы reestr_perekidok при добавлении поля month_ ";                    
                }
            }

            if (!isTableHasColumn(conn_db, "reestr_perekidok", "year_", Points.Pref + "_data"))
            {
                ret = ExecSQL(conn_db, "alter table reestr_perekidok add year_ integer", true);
                if (!ret.result)
                {
                    ret.text = "Ошибка изменения структуры таблицы reestr_perekidok при добавлении поля year_ ";
                }
            }

            if (!isTableHasColumn(conn_db, "reestr_perekidok", "month_2", Points.Pref + "_data"))
            {
                ret = ExecSQL(conn_db, "alter table reestr_perekidok add month_2 integer", true);
                if (!ret.result)
                {
                    ret.text = "Ошибка изменения структуры таблицы reestr_perekidok при добавлении поля month_2 ";
                }
            }

            if (!isTableHasColumn(conn_db, "reestr_perekidok", "year_2", Points.Pref + "_data"))
            {
                ret = ExecSQL(conn_db, "alter table reestr_perekidok add year_2 integer", true);
                if (!ret.result)
                {
                    ret.text = "Ошибка изменения структуры таблицы reestr_perekidok при добавлении поля year_2 ";
                }
            }

            if (isTableHasColumn(conn_db, "reestr_perekidok", "calc_month", Points.Pref+"_data"))
            {
                ret = ExecSQL(conn_db, "alter table reestr_perekidok drop calc_month", true);
                if (!ret.result)
                {
                    ret.text = "Ошибка изменения структуры таблицы reestr_perekidok при удалении поля calc_month ";
                }
            }

            return ret;
        }
        
        /// <summary>
        /// Создать таблицу список методик расчета CALC_METHOD
        /// </summary>
        private Returns CreateCalcMethod(IDbConnection conn_db)
        {
            Returns ret = Utils.InitReturns();

            if (!TableInWebCashe(conn_db, "calc_method"))
            {
#if PG
                ret = ExecSQL(conn_db, " create table calc_method " +
                                       "  ( nzp_calc_method     serial not null, " +                                     
                                       "    method_name        char(250) )", true);
                if (!ret.result) return ret;

                string ix = "calc_method";
#else
                ret = ExecSQL(conn_db, " create table are.calc_method " +
                                       "  ( nzp_calc_method     serial not null, " +                                     
                                       "    method_name        char(250) )", true);
                if (!ret.result) return ret;

                string ix = "are.calc_method";
#endif
                Returns ret2 = ExecSQL(conn_db, " Create unique index " + ix + "_1 on calc_method (nzp_calc_method) ", true);

                string sql_text = "INSERT INTO calc_method (method_name) VALUES ('по ЛС');";
                ret = ExecSQL(conn_db, sql_text, true);
                sql_text = "INSERT INTO calc_method (method_name) VALUES ('по общей площади ЛС');";
                ret = ExecSQL(conn_db, sql_text, true);
                sql_text = "INSERT INTO calc_method (method_name) VALUES ('по отапливаемой площади ЛС');";
                ret = ExecSQL(conn_db, sql_text, true);
                sql_text = "INSERT INTO calc_method (method_name) VALUES ('по количеству жильцов ЛС');";
                ret = ExecSQL(conn_db, sql_text, true);
                sql_text = "INSERT INTO calc_method (method_name) VALUES ('по новому расходу');";
                ret = ExecSQL(conn_db, sql_text, true);
                sql_text = "INSERT INTO calc_method (method_name) VALUES ('по квартире, с учетом кол-ва л/с в коммунальной квартире');";
                ret = ExecSQL(conn_db, sql_text, true);
            }

            if (!TableInWebCashe(conn_db, "formuls_opis"))
            {
#if PG
                ret = ExecSQL(conn_db, " CREATE TABLE formuls_opis( " +
#else
                ret = ExecSQL(conn_db, " CREATE TABLE are.formuls_opis( " +
#endif
                                       "  nzp_ops SERIAL NOT NULL, " +
                                       "  nzp_frm INTEGER default 0 NOT NULL, " +
                                       "  nzp_frm_kod INTEGER default 0 NOT NULL, " +
                                       "  nzp_frm_typ INTEGER default 0 NOT NULL, " +
                                       "  nzp_prm_tarif_ls INTEGER default 0 NOT NULL, " +
                                       "  nzp_prm_tarif_lsp INTEGER default 0 NOT NULL, " +
                                       "  nzp_prm_tarif_dm INTEGER default 0 NOT NULL, " +
                                       "  nzp_prm_tarif_su INTEGER default 0 NOT NULL, " +
                                       "  nzp_prm_tarif_bd INTEGER default 0 NOT NULL, " +
                                       "  nzp_frm_typrs INTEGER default 0 NOT NULL, " +
                                       "  nzp_prm_rash INTEGER default 0 NOT NULL, " +
                                       "  nzp_prm_rash1 INTEGER default 0 NOT NULL, " +
                                       "  nzp_prm_rash2 INTEGER default 0 NOT NULL, " +
                                       "  dat_s DATE, " +
                                       "  dat_po DATE)", true);
                if (!ret.result) return ret;
#if PG
                string ix = "formuls_opis";
#else
                string ix = "are.formuls_opis";
#endif
                Returns ret3 = ExecSQL(conn_db, " Create unique index " + ix + "_1 on formuls_opis (nzp_ops, nzp_frm) ", true);
            }

            return ret;
        }
         
        private void UpdatesDogovor(IDbConnection conn_db, out Returns ret)
        {
            ret = Utils.InitReturns();
#if PG
            ret = ExecSQL(conn_db, "set search_path to '" + Points.Pref + "_data'", true);
#else
            ret = ExecSQL(conn_db, "database " + Points.Pref + "_data", true);
#endif
            if (ret.result)
            {
                ret = AddFieldToTable(conn_db, "fn_dogovor", "dat_s", "date");
                ret = AddFieldToTable(conn_db, "fn_dogovor", "dat_po", "date");
                ret = AddFieldToTable(conn_db, "fn_dogovor", "max_sum", "DECIMAL(14,2)");
            }
        }

        private Returns after01_kart(IDbConnection connection)
        {
            Returns ret = Utils.InitReturns();

            if (Points.Region == Regions.Region.Tatarstan || Points.Region == Regions.Region.Samarskaya_obl) return ret;

            foreach (var p in Points.PointList)
            {
                SelectDatabaseOrSchema(connection, p.pref + "_data");

                ret = DropFieldsFromTable(connection, "kart",
                    new string[] {"photo", "fam_t", "ima_t", "otch_t", "rem_mr_t", "vid_mes_t"});

                ret = AddFieldToTable(connection, "kart", "dat_smert", "date before fam_c");

                if (!isTableHasColumn(connection, "kart", "strana_mr"))
                {
                    ret = ExecSQL(connection, "alter table kart add( " +
                                              " strana_mr char(30)   before  rem_mr," +
                                              " region_mr char(30)   before  rem_mr," +
                                              " okrug_mr char(30)   before  rem_mr," +
                                              " gorod_mr char(30)   before  rem_mr," +
                                              " npunkt_mr char(30)   before  rem_mr," +

                                              " strana_op char(30)   before  rem_op," +
                                              " region_op char(30)   before  rem_op," +
                                              " okrug_op char(30)   before  rem_op," +
                                              " gorod_op char(30)   before  rem_op," +
                                              " npunkt_op char(30)   before  rem_op," +

                                              " strana_ku char(30)   before  rem_ku," +
                                              " region_ku char(30)   before  rem_ku," +
                                              " okrug_ku char(30)   before  rem_ku," +
                                              " gorod_ku char(30)   before  rem_ku," +
                                              " npunkt_ku char(30)   before  rem_ku" +
                                              " )", true);
                }
            }


/*create table "pasp".s_strana(
nzp_strana  serial not null,
strana      char(30)
);
create unique index "pasp".ix_strn1 on  s_strana(nzp_strana);
insert into s_strana select nzp_land,land from s_land where nzp_land=1;

create table    "pasp".s_region(
nzp_region  serial not null,
region      char(30)
);
create unique index "pasp".ix_rgn1 on  s_region(nzp_region);
insert into s_region select nzp_stat,stat from s_stat where nzp_land=1;

create table    "pasp".s_okrug(
nzp_okrug  serial not null,
okrug      char(30)
);
create unique index "pasp".ix_okrg1 on  s_okrug(nzp_okrug);
--insert into s_okrug select nzp_town,town from s_town where nzp_stat=104259 and town matches "* Р-Н";
--insert into s_okrug select  nzp_raj_dom, rajon_dom from  s_rajon_dom r;

create table    "pasp".s_gorod(
nzp_gorod  serial not null,
gorod      char(30)
);
create unique index "pasp".ix_grd1 on  s_gorod(nzp_gorod);
--insert into s_gorod select nzp_town,town from s_town where nzp_stat=104259 and town matches "* Г";

create table    "pasp".s_npunkt(
nzp_npunkt  serial not null,
npunkt      char(30)
);
create unique index "pasp".ix_npnkt1 on  s_npunkt(nzp_npunkt);
--insert into s_npunkt select nzp_raj,rajon from s_rajon r where r.nzp_town in 
--(select t.nzp_town from s_town t
--where t.nzp_stat=104259);




alter table kart add(
dat_fio_c date before genotip, -- дата смены ФИО
rodstvo char (30) before nzp_celp  --родственное отношение текстом
);

update kart set rodstvo= (select r.rod from s_rod r where r.nzp_rod=kart.nzp_rod);*/

            return ret;
        }
    }
}