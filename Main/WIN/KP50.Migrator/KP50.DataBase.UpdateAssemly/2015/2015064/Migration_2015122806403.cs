using System;
using System.Collections.Generic;
using System.Data;
using KP50.DataBase.Migrator;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;

namespace KP50.DataBase.UpdateAssembly._2015._2015064
{
    [Migration(2015122806403, MigrateDataBase.LocalBank)]
    public class Migration_2015122806403 : Migration
    {
        public override void Apply()
        {
            if (Database.ProviderName == "PostgreSQL")
            {
                Database.CommandTimeout = 6000;

                #region Создаем master таблицу связей между проводками и задолженностями

                SetSchema(Bank.Data);
                var peni_provodki_refs = new SchemaQualifiedObjectName { Name = "peni_provodki_refs", Schema = CurrentSchema };
                if (!Database.TableExists(peni_provodki_refs))
                {
                    Database.AddTable(peni_provodki_refs,
                        new Column("nzp_wp", DbType.Int32, ColumnProperty.NotNull),
                        new Column("peni_debt_id", DbType.Int32, ColumnProperty.NotNull),
                        new Column("peni_provodki_id", DbType.Int32, ColumnProperty.NotNull),
                        new Column("nzp_kvar", DbType.Int32, ColumnProperty.NotNull),
                        new Column("date_obligation", DbType.Date, ColumnProperty.NotNull),
                        new Column("date_calc", DbType.Date, ColumnProperty.NotNull));

                    if (Database.TableExists(peni_provodki_refs))
                    {
                        Database.AddIndex("ix1_peni_provodki_refs", false, peni_provodki_refs, "nzp_wp", "nzp_kvar", "date_obligation", "date_calc");
                        Database.AddIndex("ix2_peni_provodki_refs", false, peni_provodki_refs, "nzp_wp", "date_calc");
                        Database.AddIndex("ix3_peni_provodki_refs", false, peni_provodki_refs, "nzp_kvar", "date_obligation", "date_calc");
                        Database.AddIndex("ix4_peni_provodki_refs", false, peni_provodki_refs, "nzp_wp", "peni_provodki_id", "peni_debt_id", "date_obligation");

                        CreateTriggerOnMasterTableById("peni_provodki_refs", "peni_provodki_id", 5000000);

                    }

                    #region Создаем master таблицу связей между задолженностями и пенями

                    SetSchema(Bank.Data);
                    var peni_debt_refs = new SchemaQualifiedObjectName { Name = "peni_debt_refs", Schema = CurrentSchema };
                    if (!Database.TableExists(peni_debt_refs))
                    {
                        Database.AddTable(peni_debt_refs,
                            new Column("nzp_wp", DbType.Int32, ColumnProperty.NotNull),
                            new Column("peni_calc_id", DbType.Int32, ColumnProperty.NotNull),
                            new Column("peni_debt_id", DbType.Int32, ColumnProperty.NotNull),
                            new Column("date_calc", DbType.Date, ColumnProperty.NotNull));

                        if (Database.TableExists(peni_debt_refs))
                        {
                            Database.AddIndex("ix1_peni_debt_refs", true, peni_debt_refs, "nzp_wp", "peni_calc_id", "peni_debt_id", "date_calc");

                            CreateTriggerOnMasterTable("peni_debt_refs", "date_calc");
                        }
                    }
                    #endregion  Создаем master таблицу связей между задолженностями и пенями

                }
                #endregion  Создаем master таблицу связей между проводками и задолженностями
                
            }
        }

        private string GetFullTableName(SchemaQualifiedObjectName table)
        {
            return string.Format("{0}{1}{2}", table.Schema, Database.TableDelimiter, table.Name);
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


        /// <summary>
        /// Создание партиций по ключу с указанным размером партциий
        /// </summary>
        /// <param name="table">имя мастер таблицы</param>
        /// <param name="partition_field">поле партиционирования</param>
        /// <param name="size_part">размер партиции</param>
        private void CreateTriggerOnMasterTableById(string table, string partition_field, int size_part)
        {
            //создаем функцию распределения записи по дочерним таблицам если insert будет осуществлен в master таблицу
            //а так же создаем дочернюю таблицу если ее не существует
            SetSchema(Bank.Data);

            var sql = @"CREATE OR REPLACE FUNCTION " + table + @"_partitioning() RETURNS trigger
                        LANGUAGE plpgsql
                        AS $$
                        BEGIN
                        DECLARE pref TEXT;
                        exist record;
                        full_tb_name TEXT;
                        schema_tb TEXT;
                        tb_name TEXT;
                        create_sql TEXT;
                        num integer;
                        BEGIN
                        	SELECT	TRIM (bd_kernel) INTO pref	FROM	" + CentralKernel + Database.TableDelimiter + @"s_point WHERE	nzp_wp = NEW .nzp_wp;
                        num:= (NEW." + partition_field + @" / " + size_part + @");
                        full_tb_name := TRIM (pref) || '_data" + Database.TableDelimiter + table + @"_' || num ||'_' || NEW.nzp_wp;
                        schema_tb := TRIM (pref) || '_data';
                        tb_name := '" + table + @"_' || num || '_' || NEW.nzp_wp;
                        EXECUTE 'SELECT EXISTS(SELECT * FROM information_schema.tables  WHERE table_schema =' || quote_literal(schema_tb) || ' AND  table_name =' || quote_literal(tb_name) || ')' INTO exist;
                        IF (exist :: TEXT = '(t)') THEN
                        	EXECUTE 'INSERT INTO ' || full_tb_name || ' SELECT $1.*' USING NEW;
                        ELSE	
                        	create_sql := 'CREATE TABLE ' || full_tb_name || ' ( LIKE " + CurrentSchema + Database.TableDelimiter + table + @" INCLUDING ALL, ' || 
                                          'CHECK ((" + partition_field + @">='||num* " + size_part + @"||') 
                                           AND (" + partition_field + @"<'||(num* " + size_part + @" +  " + size_part + @")||') 
                                           AND nzp_wp=' || NEW .nzp_wp ||'))
                                           INHERITS (" + CurrentSchema + Database.TableDelimiter + table + @"); ';
                        	EXECUTE create_sql;
                        	EXECUTE 'INSERT INTO ' || full_tb_name || ' SELECT $1.*' USING NEW;
                        END IF;
                        EXCEPTION
                        WHEN undefined_table THEN RAISE;
                        END;
                        RETURN NULL;
                        END;
                        $$;";


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
            DropTableCascadeFromData("peni_provodki_refs");
            DropTableCascadeFromData("peni_debt_refs");
        }

        private void DropTableCascadeFromData(string table)
        {
            SetSchema(Bank.Data);
            Database.ExecuteNonQuery("DROP TABLE IF EXISTS " + CurrentSchema + Database.TableDelimiter + " " + table + " CASCADE");
        }


    }
}
