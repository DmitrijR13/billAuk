using System;
using System.Data;
using System.Net.NetworkInformation;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2014._2014102
{
    // TODO: Set migration version as YYYYMMDDVVV
    // YYYY - Year
    // MM   - Month
    // DD   - Day
    // VVV  - Version
    [Migration(2014103010404, MigrateDataBase.CentralBank)]
    public class Migration_2014103010404_CentralBank : Migration
    {
        public override void Apply()
        {
            if (Database.ProviderName == "PostgreSQL")
            {
                #region Таблица проводок

                #region Создаем master таблицу проводок

                SetSchema(Bank.Data);
                SchemaQualifiedObjectName peni_provodki = new SchemaQualifiedObjectName();
                peni_provodki.Name = "peni_provodki";
                peni_provodki.Schema = CurrentSchema;
                if (!Database.TableExists(peni_provodki))
                {
                    Database.AddTable(peni_provodki,
                        new Column("id", DbType.Int32,
                            ColumnProperty.NotNull | ColumnProperty.Unique | ColumnProperty.PrimaryKeyWithIdentity),
                        new Column("nzp_kvar", DbType.Int32),
                        new Column("num_ls", DbType.Int32),
                        new Column("nzp_serv", DbType.Int32, ColumnProperty.NotNull),
                        new Column("nzp_supp", DbType.Int32, ColumnProperty.NotNull),
                        new Column("nzp_wp", DbType.Int32, ColumnProperty.NotNull),
                        new Column("s_prov_types_id", DbType.Int32, ColumnProperty.NotNull),
                        new Column("nzp_source", DbType.Int32),
                        new Column("rsum_tarif", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull, "0.00"),
                        new Column("sum_prih", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull, "0.00"),
                        new Column("sum_nedop", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull, "0.00"),
                        new Column("sum_real", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull, "0.00"),
                        new Column("sum_reval", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull, "0.00"),
                        new Column("date_prov", DbType.Date),
                        new Column("date_obligation", DbType.Date),
                        new Column("calc_peni", DbType.Boolean, ColumnProperty.NotNull, "true"),
                        new Column("created_on", DbType.DateTime),
                        new Column("created_by", DbType.Int32),
                        new Column("changed_on", DbType.DateTime),
                        new Column("changed_by", DbType.Int32)
                        );

                    if (Database.TableExists(peni_provodki))
                    {
                        SetSchema(Bank.Kernel);

                        if (!Database.ConstraintExists(peni_provodki, "fk_peni_provodki_s_prov_types_id"))
                        {
                            SchemaQualifiedObjectName s_prov_types = new SchemaQualifiedObjectName();
                            s_prov_types.Name = "s_prov_types";
                            s_prov_types.Schema = CurrentSchema;
                            if (Database.TableExists(s_prov_types))
                            {
                                Database.AddForeignKey("fk_peni_provodki_s_prov_types_id", peni_provodki,
                                    "s_prov_types_id",
                                    s_prov_types, "id");
                            }
                        }

                        Database.AddIndex("ix1_peni_provodki", false, peni_provodki, "id", "nzp_kvar", "num_ls");
                        Database.AddIndex("ix2_peni_provodki", false, peni_provodki, "id", "nzp_kvar", "num_ls",
                            "nzp_serv", "nzp_supp");
                        Database.AddIndex("ix3_peni_provodki", false, peni_provodki, "id", "nzp_wp", "date_obligation",
                            "date_prov", "calc_peni");

                        CreateTriggerOnMasterTable("peni_provodki", "date_obligation");
                    }

                #endregion Создаем родительскую таблицу проводок


                    #region Создаем дочерние таблицы раскиданные по charge

                    CreateChildTableInCharge("peni_provodki", "date_obligation", true);
                }

                    #endregion

                #endregion

                #region Таблица архива проводок

                #region Создаем master таблицу архива проводок

                SetSchema(Bank.Data);
                SchemaQualifiedObjectName peni_provodki_arch = new SchemaQualifiedObjectName();
                peni_provodki_arch.Name = "peni_provodki_arch";
                peni_provodki_arch.Schema = CurrentSchema;
                if (!Database.TableExists(peni_provodki_arch))
                {
                    Database.AddTable(peni_provodki_arch,
                        new Column("id", DbType.Int32,
                            ColumnProperty.NotNull | ColumnProperty.Unique | ColumnProperty.PrimaryKeyWithIdentity),
                        new Column("nzp_kvar", DbType.Int32),
                        new Column("num_ls", DbType.Int32),
                        new Column("nzp_serv", DbType.Int32, ColumnProperty.NotNull),
                        new Column("nzp_supp", DbType.Int32, ColumnProperty.NotNull),
                        new Column("nzp_wp", DbType.Int32, ColumnProperty.NotNull),
                        new Column("s_prov_types_id", DbType.Int32, ColumnProperty.NotNull),
                        new Column("nzp_source", DbType.Int32),
                        new Column("rsum_tarif", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull, "0.00"),
                        new Column("sum_prih", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull, "0.00"),
                        new Column("sum_nedop", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull, "0.00"),
                        new Column("sum_real", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull, "0.00"),
                        new Column("sum_reval", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull, "0.00"),
                        new Column("date_prov", DbType.DateTime),
                        new Column("date_obligation", DbType.DateTime),
                        new Column("calc_peni", DbType.Boolean, ColumnProperty.NotNull, "true"),
                        new Column("created_on", DbType.DateTime),
                        new Column("created_by", DbType.Int32),
                        new Column("changed_on", DbType.DateTime),
                        new Column("changed_by", DbType.Int32),
                        new Column("date_arch", DbType.DateTime)
                        );

                    if (Database.TableExists(peni_provodki_arch))
                    {
                        Database.AddIndex("ix1_peni_provodki_arch", false, peni_provodki_arch, "id", "nzp_kvar",
                            "num_ls");
                        Database.AddIndex("ix2_peni_provodki_arch", false, peni_provodki_arch, "id", "nzp_kvar",
                            "num_ls", "nzp_serv", "nzp_supp");
                        Database.AddIndex("ix3_peni_provodki_arch", false, peni_provodki_arch, "id", "nzp_wp",
                            "date_obligation", "date_prov", "calc_peni", "date_arch");

                        CreateTriggerOnMasterTable("peni_provodki_arch", "date_obligation");
                    }

                #endregion Создаем родительскую таблицу архива проводок


                    #region Создаем дочерние таблицы раскиданные по charge
                    CreateChildTableInCharge("peni_provodki_arch", "date_obligation", true);

                }

                    #endregion

                #endregion

                #region Таблица задолженностей

                #region Создаем master таблицу

                SetSchema(Bank.Data);
                SchemaQualifiedObjectName peni_debt = new SchemaQualifiedObjectName();
                peni_debt.Name = "peni_debt";
                peni_debt.Schema = CurrentSchema;
                if (!Database.TableExists(peni_debt))
                {
                    Database.AddTable(peni_debt,
                        new Column("id", DbType.Int32, ColumnProperty.NotNull | ColumnProperty.Unique | ColumnProperty.PrimaryKeyWithIdentity),
                        new Column("nzp_kvar", DbType.Int32),
                        new Column("num_ls", DbType.Int32),
                        new Column("nzp_supp", DbType.Int32, ColumnProperty.NotNull),
                        new Column("nzp_wp", DbType.Int32, ColumnProperty.NotNull),
                        new Column("s_peni_type_debt_id", DbType.Int32, ColumnProperty.NotNull),
                        new Column("peni_calc_id", DbType.Int32),
                        new Column("date_from", DbType.Date),
                        new Column("date_to", DbType.Date),
                        new Column("sum_debt", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull, "0.00"),
                        new Column("sum_peni", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull, "0.00"),
                        new Column("date_calc", DbType.Date),
                        new Column("created_on", DbType.DateTime),
                        new Column("created_by", DbType.Int32)
                        );

                    if (Database.TableExists(peni_debt))
                    {

                        if (!Database.ConstraintExists(peni_debt, "fk_peni_debt_s_peni_type_debt_id"))
                        {
                            SetSchema(Bank.Kernel);
                            SchemaQualifiedObjectName s_peni_type_debt = new SchemaQualifiedObjectName();
                            s_peni_type_debt.Name = "s_peni_type_debt";
                            s_peni_type_debt.Schema = CurrentSchema;
                            Database.AddForeignKey("fk_peni_debt_s_peni_type_debt_id", peni_debt, "s_peni_type_debt_id", s_peni_type_debt, "id");
                        }

                        Database.AddIndex("ix1_peni_debt", false, peni_debt, "id", "nzp_kvar", "num_ls", "nzp_supp");
                        Database.AddIndex("ix2_peni_debt", false, peni_debt, "id", "nzp_wp", "date_calc", "date_from", "date_to");

                        CreateTriggerOnMasterTable("peni_debt", "date_calc", false);
                    }

                #endregion Создаем родительскую таблицу проводок

                    #region Создаем дочерние таблицы раскиданные по charge

                    CreateChildTableInCharge("peni_debt", "date_calc", false);

                }

                    #endregion

                #endregion

                #region Таблица рассчитанных пени

                #region Создаем master таблицу

                SetSchema(Bank.Data);
                SchemaQualifiedObjectName peni_calc = new SchemaQualifiedObjectName();
                peni_calc.Name = "peni_calc";
                peni_calc.Schema = CurrentSchema;
                if (!Database.TableExists(peni_calc))
                {
                    Database.AddTable(peni_calc,
                        new Column("id", DbType.Int32, ColumnProperty.NotNull | ColumnProperty.Unique | ColumnProperty.PrimaryKeyWithIdentity),
                        new Column("nzp_kvar", DbType.Int32),
                        new Column("num_ls", DbType.Int32),
                        new Column("nzp_supp", DbType.Int32, ColumnProperty.NotNull),
                        new Column("nzp_wp", DbType.Int32, ColumnProperty.NotNull),
                        new Column("date_from", DbType.Date),
                        new Column("date_to", DbType.Date),
                        new Column("sum_peni", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull, "0.00"),
                        new Column("sum_old_reval", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull, "0.00"),
                        new Column("sum_new_reval", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull, "0.00"),
                        new Column("sum_peni_total", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull, "0.00"),
                        new Column("date_calc", DbType.Date),
                        new Column("created_on", DbType.DateTime),
                        new Column("created_by", DbType.Int32)
                        );

                    if (Database.TableExists(peni_calc))
                    {
                        Database.AddIndex("ix1_peni_calc", false, peni_calc, "id", "nzp_kvar", "num_ls", "nzp_supp");
                        Database.AddIndex("ix2_peni_calc", false, peni_calc, "id", "nzp_wp", "date_calc", "date_from", "date_to");

                        CreateTriggerOnMasterTable("peni_calc", "date_calc", false);
                    }

                #endregion Создаем родительскую таблицу проводок

                    #region Создаем дочерние таблицы раскиданные по charge
                    CreateChildTableInCharge("peni_calc", "date_calc", false);
                }

                    #endregion

                #endregion
            }
        }

        public override void Revert()
        {
            DropTableCascade("peni_provodki");
            DropTableCascade("peni_provodki_arch");
            DropTableCascade("peni_debt");
            DropTableCascade("peni_calc");
        }

        private void DropTableCascade(string table)
        {
            Database.ExecuteNonQuery("DROP TABLE IF EXISTS " + CentralData + Database.TableDelimiter + " " + table + " CASCADE");
        }

        private void CreateTriggerOnMasterTable(string table, string partition_field, bool eachMonth = true)
        {
            //создаем функцию распределения записи по дочерним таблицам если insert будет осуществлен в master таблицу
            //а так же создаем дочернюю таблицу если ее не существует
            SetSchema(Bank.Data);

            string create_sql = "";

            if (eachMonth)
            {
                create_sql = "                       IF(month=12) " +
                             "                       THEN year2=year+1; month2=1;" +
                             "                       ELSE year2= year; month2=month+1;" +
                             "                       END IF; " +
                             "                   create_sql:='CREATE TABLE '||full_tb_name||' ( LIKE " + CentralData +
                             Database.TableDelimiter + table + " INCLUDING ALL, '||" +
                             " 											' CONSTRAINT '||tb_name||'_constrain CHECK ((" + partition_field +
                             " >= '||quote_literal(year||'-'||month||'-01')||'::DATE) '||" +
                             " 											' AND (" + partition_field +
                             " < '||quote_literal(year2||'-'||month2||'-01')||'::DATE) AND nzp_wp='||NEW.nzp_wp||'))'||" +
                             " 											' INHERITS (" + CentralData + Database.TableDelimiter + table + "); ';";
            }
            else
            {
                create_sql = " year2:=year+1; " +
                             " create_sql:='CREATE TABLE '||full_tb_name||' ( LIKE " + CentralData +
                         Database.TableDelimiter + table + " INCLUDING ALL, '||" +
                         " 											' CONSTRAINT '||tb_name||'_constrain CHECK ((" + partition_field +
                         " >= '||quote_literal(year||'-01-01')||'::DATE) '||" +
                         " 											' AND (" + partition_field +
                         " < '||quote_literal(year2||'-01-01')||'::DATE) AND nzp_wp='||NEW.nzp_wp||'))'||" +
                         " 											' INHERITS (" + CentralData + Database.TableDelimiter + table + "); ';";
            }
            string sql = " CREATE OR REPLACE FUNCTION " + table + "_partitioning() RETURNS \"trigger\"" +
                         " LANGUAGE \"plpgsql\" " +
                         "  AS $$ " +
                         "   BEGIN " +
                         "    	DECLARE " +
                         " 		pref TEXT;" +
                         "		exist record; " +
                         "		full_tb_name TEXT; " +
                         "		schema_tb TEXT; " +
                         "		tb_name TEXT; " +
                         "		create_sql TEXT;" +
                         "    year INTEGER; month INTEGER; year2 INTEGER; month2 INTEGER;	 " +
                         "      BEGIN " +
                         "         SELECT trim(bd_kernel) into pref FROM " + CentralKernel + Database.TableDelimiter +
                         "s_point WHERE nzp_wp=NEW.nzp_wp;" +
                         "         full_tb_name:=trim(pref)||'_charge_'||to_char(NEW." + partition_field + ", 'YY')||'" +
                         Database.TableDelimiter +
                         table + "_'||to_char(NEW." + partition_field + ", " + (eachMonth ? "'YYYYMM'" : "'YYYY'") +
                         ")||'_'||NEW.nzp_wp;" +
                         "         schema_tb:=trim(pref)||'_charge_'||to_char(NEW." + partition_field + ", 'YY');" +
                         "         tb_name:= '" + table + "_'||to_char(NEW." + partition_field + ", " +
                         (eachMonth ? "'YYYYMM'" : "'YYYY'") + ")||'_'||NEW.nzp_wp; " +
                         "         EXECUTE 'SELECT EXISTS(SELECT * FROM information_schema.tables  WHERE table_schema ='||quote_literal(schema_tb)||' AND  table_name ='|| quote_literal(tb_name)||')' " +
                         "			INTO exist;	" +
                         "           IF(exist::TEXT='(t)') " +
                         "					THEN EXECUTE 'INSERT INTO '||full_tb_name||' SELECT $1.*' USING NEW;" +
                         "                   ELSE  " +
                         "                      month:=date_part('month',NEW.date_obligation); " +
                         "                      year:=date_part('year',NEW.date_obligation);" +
                         create_sql +
                         "                   EXECUTE create_sql; " +
                         "					EXECUTE 'INSERT INTO '||full_tb_name||' SELECT $1.*' USING NEW;" +
                         "          END IF;          " +
                         "      EXCEPTION " +
                         "   WHEN undefined_table THEN" +
                         "       RETURN NEW;" +
                         "   END;" +
                         "   RETURN NULL; " +
                         "  END;" +
                         "  $$; ";
            Database.ExecuteNonQuery(sql);

            //вешаем триггер на insert
            Database.ExecuteNonQuery("CREATE TRIGGER partitioning_trigger_" + table +
                                     " BEFORE INSERT ON " + CentralData + Database.TableDelimiter +
                                     "" + table + " " +
                                     " FOR EACH ROW EXECUTE PROCEDURE " + table + "_partitioning();");
        }

        private void ConsoleLog(string bank)
        {
            Console.WriteLine("Applying 2014103010404: Migration 2014103010404 Bank: " + bank);
        }

        private void CreateChildTableInCharge(string table, string partition_field, bool eachMonth = true)
        {
            SetSchema(Bank.Kernel);
            IDataReader reader = Database.ExecuteReader("SELECT * from " + CentralKernel + Database.TableDelimiter + "s_point");

            while (reader.Read())
            {
                var pref = reader["bd_kernel"] != DBNull.Value ? reader["bd_kernel"].ToString().Trim() : "";
                var nzp_wp = reader["nzp_wp"] != DBNull.Value ? Convert.ToInt32(reader["nzp_wp"]) : 0;

                if (pref != "" && nzp_wp > 0)
                {
                    IDataReader reader2 =
                        Database.ExecuteReader("SELECT yearr FROM " + CentralKernel + Database.TableDelimiter + "s_baselist WHERE nzp_wp=" + nzp_wp +
                                               " GROUP BY yearr");
                    while (reader2.Read())
                    {
                        var year = reader2["yearr"] != DBNull.Value ? Convert.ToInt32(reader2["yearr"]) : 0;
                        if (year > 0)
                        {
                            Database.ExecuteNonQuery("SET search_path = " + pref + "_charge_" +
                                                     (year - 2000).ToString("00"));
                            //костыль на проверку существования схемы
                            if (Database.TableExists("charge_01"))
                            {

                                if (eachMonth)
                                {
                                    for (int i = 1; i <= 12; i++)
                                    {
                                        var tableName = table + "_" + year + i.ToString("00") + "_" + nzp_wp + "";
                                        var tableMaster = CentralData + Database.TableDelimiter + table;
                                        if (!Database.TableExists(tableName))
                                            Database.ExecuteNonQuery(" CREATE TABLE " + tableName + " ( " +
                                                                     " LIKE " + tableMaster + " INCLUDING ALL, " +
                                                                     " CONSTRAINT " + tableName +
                                                                     "_constraint " +
                                                                     " CHECK ((" + partition_field + " >= '" + year + "-" +
                                                                     i +
                                                                     "-01'::DATE) AND (" + partition_field + " < '" +
                                                                     (i == 12 ? (year + 1) : year) +
                                                                     "-" + (i == 12 ? 1 : i + 1) + "-01'::DATE) " +
                                                                     " AND nzp_wp=" + nzp_wp + ")) INHERITS (" +
                                                                     tableMaster + ");");
                                    }
                                }
                                else
                                {
                                    var tableName = table + "_" + year + "_" + nzp_wp + "";
                                    var tableMaster = CentralData + Database.TableDelimiter + table;
                                    if (!Database.TableExists(tableName))
                                        Database.ExecuteNonQuery(" CREATE TABLE " + tableName + " ( " +
                                                                 " LIKE " + tableMaster +
                                                                 " INCLUDING ALL, " +
                                                                 " CONSTRAINT " + tableName +
                                                                 "_" + partition_field + " " +
                                                                 " CHECK ((" + partition_field + " >= '" + year + "-01-01'::DATE) " +
                                                                 " AND (" + partition_field + " < '" + (year + 1) + "-01-01'::DATE) " +
                                                                 " AND nzp_wp=" + nzp_wp + ")) INHERITS (" + tableMaster + ")");
                                }
                            }
                        }
                    }
                }
                ConsoleLog(pref);
            }
        }
    }
}




