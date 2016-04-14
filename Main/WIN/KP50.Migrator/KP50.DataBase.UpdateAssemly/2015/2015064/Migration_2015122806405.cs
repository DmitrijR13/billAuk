using System;
using System.Collections.Generic;
using System.Data;
using KP50.DataBase.Migrator;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015064
{
    [Migration(2015122806405, MigrateDataBase.LocalBank)]
    public class Migration_2015122806405 : Migration
    {
        public override void Apply()
        {

            if (Database.ProviderName == "PostgreSQL")
            {

                SchemaQualifiedObjectName s_peni_type_debt = new SchemaQualifiedObjectName();
                s_peni_type_debt.Name = "s_peni_type_debt";
                s_peni_type_debt.Schema = CentralKernel;

                SchemaQualifiedObjectName s_prov_types = new SchemaQualifiedObjectName();
                s_prov_types.Name = "s_prov_types";
                s_prov_types.Schema = CentralKernel;


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
                        new Column("nzp_dom", DbType.Int32),
                        new Column("nzp_serv", DbType.Int32, ColumnProperty.NotNull),
                        new Column("nzp_supp", DbType.Int32, ColumnProperty.NotNull),
                        new Column("nzp_wp", DbType.Int32, ColumnProperty.NotNull),
                        new Column("s_prov_types_id", DbType.Int32, ColumnProperty.NotNull),
                        new Column("nzp_source", DbType.Int32),
                        new Column("rsum_tarif", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull, "0.00"),
                        new Column("sum_prih", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull, "0.00"),
                        new Column("sum_nedop", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull, "0.00"),
                        new Column("sum_reval", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull, "0.00"),
                        new Column("date_prov", DbType.Date),
                        new Column("date_obligation", DbType.Date),
                        new Column("created_on", DbType.DateTime),
                        new Column("created_by", DbType.Int32),
                        new Column("changed_on", DbType.DateTime),
                        new Column("changed_by", DbType.Int32),
                        new Column("peni_actions_id", DbType.Int32, ColumnProperty.NotNull));

                    if (Database.TableExists(peni_provodki))
                    {
                        if (!Database.ConstraintExists(peni_provodki, "fk_peni_provodki_s_prov_types_id"))
                        {
                            if (Database.TableExists(s_prov_types))
                            {
                                Database.AddForeignKey("fk_peni_provodki_s_prov_types_id", peni_provodki,
                                    "s_prov_types_id",
                                    s_prov_types, "id");
                            }
                        }

                        Database.AddIndex("ix1_peni_provodki", false, peni_provodki, "id", "nzp_kvar", "num_ls");
                        Database.AddIndex("ix2_peni_provodki", false, peni_provodki, "id", "nzp_kvar", "num_ls", "nzp_serv", "nzp_supp");
                        Database.AddIndex("ix3_peni_provodki", false, peni_provodki, "id", "nzp_wp", "date_obligation", "date_prov");
                        Database.AddIndex("ix4_peni_provodki", false, peni_provodki, "num_ls", "nzp_serv", "nzp_supp", "date_obligation");
                        Database.AddIndex("ix5_peni_provodki", true, peni_provodki, "id");
                        Database.AddIndex("ix6_peni_provodki", false, peni_provodki, "nzp_kvar", "nzp_serv", "nzp_supp", "date_obligation", "nzp_wp");

                        CreateTriggerOnMasterTable("peni_provodki", "date_obligation");
                    }

                #endregion Создаем родительскую таблицу проводок

                }
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
                        new Column("nzp_dom", DbType.Int32),
                        new Column("nzp_serv", DbType.Int32, ColumnProperty.NotNull),
                        new Column("nzp_supp", DbType.Int32, ColumnProperty.NotNull),
                        new Column("nzp_wp", DbType.Int32, ColumnProperty.NotNull),
                        new Column("s_prov_types_id", DbType.Int32, ColumnProperty.NotNull),
                        new Column("nzp_source", DbType.Int32),
                        new Column("rsum_tarif", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull, "0.00"),
                        new Column("sum_prih", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull, "0.00"),
                        new Column("sum_nedop", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull, "0.00"),
                        new Column("sum_reval", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull, "0.00"),
                        new Column("date_prov", DbType.DateTime),
                        new Column("date_obligation", DbType.DateTime),
                        new Column("calc_peni", DbType.Boolean, ColumnProperty.NotNull, "true"),
                        new Column("created_on", DbType.DateTime),
                        new Column("created_by", DbType.Int32),
                        new Column("changed_on", DbType.DateTime),
                        new Column("changed_by", DbType.Int32),
                        new Column("date_arch", DbType.DateTime),
                        new Column("peni_actions_id", DbType.Int32, ColumnProperty.NotNull)
                        );

                    if (Database.TableExists(peni_provodki_arch))
                    {
                        Database.AddIndex("ix1_peni_provodki_arch", false, peni_provodki_arch, "id", "nzp_kvar", "num_ls", "nzp_serv", "nzp_supp");
                        Database.AddIndex("ix2_peni_provodki_arch", false, peni_provodki_arch, "id", "nzp_wp", "date_obligation", "date_prov", "date_arch");
                        CreateTriggerOnMasterTable("peni_provodki_arch", "date_obligation");
                    }

                #endregion Создаем родительскую таблицу архива проводок



                }


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
                        new Column("nzp_serv", DbType.Int32, ColumnProperty.NotNull),
                        new Column("nzp_wp", DbType.Int32, ColumnProperty.NotNull),
                        new Column("s_peni_type_debt_id", DbType.Int32, ColumnProperty.NotNull),
                        new Column("date_from", DbType.Date),
                        new Column("date_to", DbType.Date),
                        new Column("sum_debt", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull, "0.00"),
                        new Column("over_payments", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull, "0.00"),
                        new Column("sum_debt_result", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull, "0.00"),
                        new Column("cnt_days", DbType.Int32),
                        new Column("cnt_days_with_prm", DbType.Int32, ColumnProperty.NotNull, "0"),
                        new Column("sum_peni", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull, "0.00"),
                        new Column("date_calc", DbType.Date),
                        new Column("created_on", DbType.DateTime),
                        new Column("created_by", DbType.Int32),
                        new Column("peni_actions_id", DbType.Int32, ColumnProperty.NotNull),
                        new Column("peni_calc", DbType.Boolean, ColumnProperty.NotNull, "true"),
                        new Column("type_period", DbType.Int32, ColumnProperty.NotNull, -1)
                        );

                    if (Database.TableExists(peni_debt))
                    {

                        if (!Database.ConstraintExists(peni_debt, "fk_peni_debt_s_peni_type_debt_id"))
                        {
                            SetSchema(Bank.Kernel);
                            Database.AddForeignKey("fk_peni_debt_s_peni_type_debt_id", peni_debt, "s_peni_type_debt_id", s_peni_type_debt, "id");
                        }

                        SetSchema(Bank.Data);
                        Database.AddIndex("ix1_peni_debt", true, peni_debt, "id");
                        Database.AddIndex("ix2_peni_debt", false, peni_debt, "id", "nzp_kvar", "num_ls", "nzp_supp");
                        Database.AddIndex("ix3_peni_debt", false, peni_debt, "id", "nzp_wp", "date_calc", "date_from", "date_to");
                        Database.AddIndex("ix4_peni_debt", false, peni_debt, "num_ls", "nzp_kvar", "nzp_serv", "nzp_supp", "date_from", "date_to");
                        Database.AddIndex("ix5_peni_debt", false, peni_debt, "nzp_kvar", "nzp_supp", "peni_actions_id");
                        Database.AddIndex("ix6_peni_debt", false, peni_debt, "nzp_kvar");
                        Database.ExecuteNonQuery(" CREATE INDEX  ix7_peni_debt " +
                                               " ON " + CurrentSchema + Database.TableDelimiter + "peni_debt (sum_debt,peni_calc,peni_actions_id,s_peni_type_debt_id) " +
                                               " WHERE sum_debt>0 and peni_calc=true and s_peni_type_debt_id<>2");
                        Database.AddIndex("ix8_peni_debt", false, peni_debt, "s_peni_type_debt_id");
                        Database.AddIndex("ix9_peni_debt", false, peni_debt, "date_calc", "nzp_wp", "s_peni_type_debt_id");
                        Database.AddIndex("ix10_peni_debt", false, peni_debt, "type_period");

                        CreateTriggerOnMasterTableDebt("peni_debt", "date_calc", true);
                    }

                #endregion Создаем родительскую таблицу проводок





                }


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
                        new Column("nzp_serv", DbType.Int32, ColumnProperty.NotNull, 1),
                        new Column("nzp_supp", DbType.Int32, ColumnProperty.NotNull),
                        new Column("nzp_wp", DbType.Int32, ColumnProperty.NotNull),
                        new Column("date_from", DbType.Date),
                        new Column("date_to", DbType.Date),
                        new Column("sum_peni", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull, "0.00"),
                        new Column("sum_old_reval", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull, "0.00"),
                        new Column("sum_new_reval", DbType.Decimal.WithSize(14, 2), ColumnProperty.NotNull, "0.00"),
                        new Column("date_calc", DbType.Date),
                        new Column("created_on", DbType.DateTime),
                        new Column("created_by", DbType.Int32),
                        new Column("peni_actions_id", DbType.Int32, ColumnProperty.NotNull)
                        );

                    if (Database.TableExists(peni_calc))
                    {
                        Database.AddIndex("ix1_peni_calc", false, peni_calc, "id", "nzp_kvar", "num_ls", "nzp_supp");
                        Database.AddIndex("ix2_peni_calc", false, peni_calc, "id", "nzp_wp", "date_calc", "date_from", "date_to");
                        Database.AddIndex("ix3_peni_calc", false, peni_calc, "num_ls", "nzp_kvar", "nzp_supp", "date_from", "date_to");
                        Database.AddIndex("ix4_peni_calc", false, peni_calc, "nzp_kvar", "nzp_supp", "peni_actions_id");
                        Database.AddIndex("ix5_peni_calc", false, peni_calc, "nzp_kvar");
                        Database.AddIndex("ix6_peni_calc", false, peni_calc, "nzp_serv");

                        CreateTriggerOnMasterTable("peni_calc", "date_calc", false);
                    }

                #endregion Создаем родительскую таблицу проводок


                }

                #endregion

                SetSchema(Bank.Data);
                var peni_off = new SchemaQualifiedObjectName { Name = "peni_off", Schema = CurrentSchema };

                if (!Database.TableExists(peni_off))
                {
                    Database.AddTable(peni_off,
                        new Column("nzp_wp", DbType.Int32, ColumnProperty.NotNull),
                        new Column("peni_off_id", DbType.Int32, ColumnProperty.NotNull),
                        new Column("peni_debt_id", DbType.Int32, ColumnProperty.NotNull),
                        new Column("date_calc", DbType.Date, ColumnProperty.NotNull));

                    if (Database.TableExists(peni_off))
                    {
                        Database.AddIndex("ix1_peni_off", true, peni_off, "nzp_wp", "peni_off_id", "peni_debt_id", "date_calc");
                        Database.AddIndex("ix2_peni_off", false, peni_off, "peni_debt_id");

                        CreateTriggerOnMasterTable("peni_off", "date_calc");
                    }
                }
            }

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
                             "                   create_sql:='CREATE TABLE '||full_tb_name||' ( LIKE " + CurrentSchema +
                             Database.TableDelimiter + table + " INCLUDING ALL, '||" +
                             " 											' CONSTRAINT '||tb_name||'_constrain CHECK ((" + partition_field +
                             " >= '||quote_literal(year||'-'||month||'-01')||'::DATE) '||" +
                             " 											' AND (" + partition_field +
                             " < '||quote_literal(year2||'-'||month2||'-01')||'::DATE) AND nzp_wp='||NEW.nzp_wp||'))'||" +
                             " 											' INHERITS (" + CurrentSchema + Database.TableDelimiter + table + "); ';";
            }
            else
            {
                create_sql = " year2:=year+1; " +
                             " create_sql:='CREATE TABLE '||full_tb_name||' ( LIKE " + CurrentSchema +
                         Database.TableDelimiter + table + " INCLUDING ALL, '||" +
                         " 											' CONSTRAINT '||tb_name||'_constrain CHECK ((" + partition_field +
                         " >= '||quote_literal(year||'-01-01')||'::DATE) '||" +
                         " 											' AND (" + partition_field +
                         " < '||quote_literal(year2||'-01-01')||'::DATE) AND nzp_wp='||NEW.nzp_wp||'))'||" +
                         " 											' INHERITS (" + CurrentSchema + Database.TableDelimiter + table + "); ';";
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
                         "                      month:=date_part('month',NEW." + partition_field + "); " +
                         "                      year:=date_part('year',NEW." + partition_field + ");" +
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

            Database.ExecuteNonQuery("DROP TRIGGER IF EXISTS partitioning_trigger_" + table + " ON " + CurrentSchema +
                                     Database.TableDelimiter + table + " ");
            //вешаем триггер на insert
            Database.ExecuteNonQuery("CREATE TRIGGER partitioning_trigger_" + table +
                                     " BEFORE INSERT ON " + CurrentSchema + Database.TableDelimiter +
                                     "" + table + " " +
                                     " FOR EACH ROW EXECUTE PROCEDURE " + table + "_partitioning();");
        }


        private void CreateTriggerOnMasterTableDebt(string table, string partition_field, bool eachMonth)
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
                             "                   create_sql:='CREATE TABLE '||full_tb_name||' ( LIKE " + CurrentSchema +
                             Database.TableDelimiter + table + " INCLUDING ALL, '||" +
                             " 											' CONSTRAINT '||tb_name||'_constrain CHECK ((" + partition_field +
                             " >= '||quote_literal(year||'-'||month||'-01')||'::DATE) '||" +
                             " 											' AND (" + partition_field +
                             " < '||quote_literal(year2||'-'||month2||'-01')||'::DATE) AND nzp_wp='||NEW.nzp_wp||' AND s_peni_type_debt_id='||NEW.s_peni_type_debt_id||'))'||" +
                             " 											' INHERITS (" + CurrentSchema + Database.TableDelimiter + table + "); ';";
            }
            else
            {
                create_sql = " year2:=year+1; " +
                             " create_sql:='CREATE TABLE '||full_tb_name||' ( LIKE " + CurrentSchema +
                         Database.TableDelimiter + table + " INCLUDING ALL, '||" +
                         " 											' CONSTRAINT '||tb_name||'_constrain CHECK ((" + partition_field +
                         " >= '||quote_literal(year||'-01-01')||'::DATE) '||" +
                         " 											' AND (" + partition_field +
                         " < '||quote_literal(year2||'-01-01')||'::DATE) AND nzp_wp='||NEW.nzp_wp||' AND s_peni_type_debt_id='||NEW.s_peni_type_debt_id||'))'||" +
                         " 											' INHERITS (" + CurrentSchema + Database.TableDelimiter + table + "); ';";
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
                         "		create_sql TEXT; type_debt TEXT;" +
                         "    year INTEGER; month INTEGER; year2 INTEGER; month2 INTEGER; " +
                         "      BEGIN " +
                         "         SELECT trim(bd_kernel) into pref FROM " + CentralKernel + Database.TableDelimiter +
                         "s_point WHERE nzp_wp=NEW.nzp_wp;" +
                         "     IF (NEW.s_peni_type_debt_id = 1)" +
                         "          THEN type_debt:='_up';" +
                         "          ELSE type_debt:='_down';" +
                         "      END IF;             " +
                         "         full_tb_name:=trim(pref)||'_charge_'||to_char(NEW." + partition_field + ", 'YY')||'" +
                         Database.TableDelimiter +
                         table + "_'||to_char(NEW." + partition_field + ", " + (eachMonth ? "'YYYYMM'" : "'YYYY'") +
                         ")||'_'||NEW.nzp_wp||type_debt;" +
                         "         schema_tb:=trim(pref)||'_charge_'||to_char(NEW." + partition_field + ", 'YY');" +
                         "         tb_name:= '" + table + "_'||to_char(NEW." + partition_field + ", " +
                         (eachMonth ? "'YYYYMM'" : "'YYYY'") + ")||'_'||NEW.nzp_wp||type_debt; " +
                         "         EXECUTE 'SELECT EXISTS(SELECT * FROM information_schema.tables  WHERE table_schema ='||quote_literal(schema_tb)||' AND  table_name ='|| quote_literal(tb_name)||')' " +
                         "			INTO exist;	" +
                         "           IF(exist::TEXT='(t)') " +
                         "					THEN EXECUTE 'INSERT INTO '||full_tb_name||' SELECT $1.*' USING NEW;" +
                         "                   ELSE  " +
                         "                      month:=date_part('month',NEW." + partition_field + "); " +
                         "                      year:=date_part('year',NEW." + partition_field + ");" +
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

            Database.ExecuteNonQuery("DROP TRIGGER IF EXISTS partitioning_trigger_" + table + " ON " + CurrentSchema +
                                     Database.TableDelimiter + table + " ");
            //вешаем триггер на insert
            Database.ExecuteNonQuery("CREATE TRIGGER partitioning_trigger_" + table +
                                     " BEFORE INSERT ON " + CurrentSchema + Database.TableDelimiter +
                                     "" + table + " " +
                                     " FOR EACH ROW EXECUTE PROCEDURE " + table + "_partitioning();");
        }



        public override void Revert()
        {
            DropTableCascadeFromData("peni_provodki");
            DropTableCascadeFromData("peni_provodki_arch");
            DropTableCascadeFromData("peni_debt");
            DropTableCascadeFromData("peni_calc");
        }

        private void DropTableCascadeFromData(string table)
        {
            SetSchema(Bank.Data);
            Database.ExecuteNonQuery("DROP TABLE IF EXISTS " + CurrentSchema + Database.TableDelimiter + " " + table + " CASCADE");
        }

    }
}
