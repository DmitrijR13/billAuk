using System;
using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;
using System.Text;


namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2014120312101, MigrateDataBase.LocalBank)]
    public class Migration_2014120312101_LocalBank : Migration
    {
        public override void Apply()
        {
            #region Сформировать sql запрос

            StringBuilder sql = new StringBuilder();
            sql.AppendLine(""); // Пустая строка в начале. Чтобы красивее было в info.log

            #region 1. report.get_parameters
            sql.AppendLine("DROP FUNCTION IF EXISTS report.get_parameters(integer, integer);                ");
            sql.AppendLine("                                                                                ");
            sql.AppendLine("CREATE OR REPLACE FUNCTION report.get_parameters(IN report_id integer, IN user_id integer, IN object_id integer)    ");
            sql.AppendLine("  RETURNS TABLE(kind character varying, json text) AS                           ");
            sql.AppendLine("$BODY$                                                                          ");
            sql.AppendLine("-- =============================================================================");
            sql.AppendLine("-- Получить список отчетов.                                                     ");
            sql.AppendLine("--                                                                              ");
            sql.AppendLine("-- -------------------------------------------------------------------          ");
            sql.AppendLine("-- Аргументы:                                                                   ");
            sql.AppendLine("-- -------------------------------------------------------------------          ");
            sql.AppendLine("-- report_id     - Идентификатор отчета.                                        ");
            sql.AppendLine("-- user_id       - Идентификатор пользователя.                                  ");
            sql.AppendLine("-- object_id     - Идентификатор объекта (лицевого счёта / дома / ...).         ");
            sql.AppendLine("--                                                                              ");
            sql.AppendLine("-- -------------------------------------------------------------------          ");
            sql.AppendLine("-- Результат:                                                                   ");
            sql.AppendLine("-- -------------------------------------------------------------------          ");
            sql.AppendLine("-- Таблица:                                                                     ");
            sql.AppendLine("--     kind    - Класс параметра. Соответствует классу из сборки Bars.KP50.Report    ");
            sql.AppendLine("--     json    - Свойства в формате json.                                       ");
            sql.AppendLine("--                                                                              ");
            sql.AppendLine("-- -------------------------------------------------------------------          ");
            sql.AppendLine("-- Виталя Любич, VetalAerix@gmail.com                                           ");
            sql.AppendLine("-- 27.09.2014                                                                   ");
            sql.AppendLine("-- =============================================================================");
            sql.AppendLine("BEGIN                                                                           ");
            sql.AppendLine("    RETURN QUERY                                                                ");
            sql.AppendLine("    SELECT t.kind,                                                              ");
            sql.AppendLine("           format('{Code: \"%s\", Name: \"%s\"%s%s}',                           ");
            sql.AppendLine("                     t.code,                                                    ");
            sql.AppendLine("                     t.title,                                                   ");
            sql.AppendLine("                     report.get_default_value(t.initial, report_id, user_id, object_id),    ");
            sql.AppendLine("                     CASE WHEN t.properties IS not null AND t.properties <> '' THEN ', ' || t.properties ELSE '' END    ");
            sql.AppendLine("                 )                                                              ");
            sql.AppendLine("    FROM report.parameter t                                                     ");
            sql.AppendLine("    WHERE t.report = report_id                                                  ");
            sql.AppendLine("    ORDER BY t.turn;                                                            ");
            sql.AppendLine("                                                                                ");
            sql.AppendLine("    -- Если для отчёта отключен предпросмотр, то добавить параметр \"Формат печати\".    ");
            sql.AppendLine("    IF (SELECT preview FROM report.list WHERE id = report_id) = FALSE           ");
            sql.AppendLine("    THEN                                                                        ");
            sql.AppendLine("        -- Если параметр уже не добавлен \"вручную\"                            ");
            sql.AppendLine("        IF NOT EXISTS (SELECT 1 FROM report.parameter WHERE report = report_id AND code = 'ExportFormat' LIMIT 1)    ");
            sql.AppendLine("        THEN                                                                    ");
            sql.AppendLine("            RETURN QUERY                                                        ");
            sql.AppendLine("            SELECT 'ExportFormatParameter'::varchar, '{Code: \"ExportFormat\", Name: \"Формат печати\"}'::text;    ");
            sql.AppendLine("            -- Если нужно задать, чтобы по умолчанию был формат pdf, то добавить:    ");
            sql.AppendLine("            -- Value: \"Pdf\"                                                   ");
            sql.AppendLine("        END IF;                                                                 ");
            sql.AppendLine("    END IF;                                                                     ");
            sql.AppendLine("END;                                                                            ");
            sql.AppendLine("$BODY$                                                                          ");
            sql.AppendLine("LANGUAGE plpgsql VOLATILE                                                       ");
            sql.AppendLine("  COST 100                                                                      ");
            sql.AppendLine("  ROWS 1000;                                                                    ");
            sql.AppendLine("                                                                                ");
            #endregion

            #region 2. report.get_default_value
            sql.AppendLine("DROP FUNCTION IF EXISTS report.get_default_value(character varying, integer, integer);    ");
            sql.AppendLine("    ");
            sql.AppendLine("CREATE OR REPLACE FUNCTION report.get_default_value(initial_sql character varying, report_id integer, user_id integer, object_id integer)    ");
            sql.AppendLine("  RETURNS character varying AS                                                  ");
            sql.AppendLine("$BODY$                                                                          ");
            sql.AppendLine("-- =============================================================================");
            sql.AppendLine("-- Получить значение по умолчанию для параметра.                                ");
            sql.AppendLine("-- Выполнить параметризованную инструкцию initial_sql.                          ");
            sql.AppendLine("--                                                                              ");
            sql.AppendLine("-- Нужно, чтобы выполнить динамический SQL в select-e функции report.parameter  ");
            sql.AppendLine("--                                                                              ");
            sql.AppendLine("-- -------------------------------------------------------------------          ");
            sql.AppendLine("-- Аргументы:                                                                   ");
            sql.AppendLine("-- -------------------------------------------------------------------          ");
            sql.AppendLine("-- initial_sql    - Параметризованная sql инструкция.                           ");
            sql.AppendLine("-- report_id      - Идентификатор отчёта.                                       ");
            sql.AppendLine("-- user_id        - Идентификатор пользователя.                                 ");
            sql.AppendLine("-- object_id      - Идентификатор объекта (лицевого счёта / дома / ...).        ");
            sql.AppendLine("--                                                                              ");
            sql.AppendLine("-- -------------------------------------------------------------------          ");
            sql.AppendLine("-- Результат:                                                                   ");
            sql.AppendLine("-- -------------------------------------------------------------------          ");
            sql.AppendLine("-- Строка типа: \"Value: 113\".                                                 ");
            sql.AppendLine("-- Если значение по умолчанию не предусмотрено,                                 ");
            sql.AppendLine("-- то возвращается пустая строка.                                               ");
            sql.AppendLine("--                                                                              ");
            sql.AppendLine("-- -------------------------------------------------------------------          ");
            sql.AppendLine("-- Виталя Любич, VetalAerix@gmail.com                                           ");
            sql.AppendLine("-- 29.09.2014                                                                   ");
            sql.AppendLine("-- =============================================================================");
            sql.AppendLine("DECLARE                                                                         ");
            sql.AppendLine("    sql        varchar;                                                         ");
            sql.AppendLine("    value_text varchar;                                                         ");
            sql.AppendLine("BEGIN                                                                           ");
            sql.AppendLine("    IF coalesce(initial_sql, '') <> ''                                          ");
            sql.AppendLine("    THEN                                                                        ");
            sql.AppendLine("        sql := initial_sql;                                                     ");
            sql.AppendLine("        sql := replace(sql, '@Report', report_id::varchar);                     ");
            sql.AppendLine("        sql := replace(sql, '@User',   user_id::varchar);                       ");
            sql.AppendLine("        sql := replace(sql, '@Object', object_id::varchar);                     ");
            sql.AppendLine("                                                                                ");
            sql.AppendLine("        EXECUTE sql INTO value_text;                                            ");
            sql.AppendLine("        value_text := ', Value: ' || value_text;                                ");
            sql.AppendLine("    ELSE                                                                        ");
            sql.AppendLine("        value_text := '';                                                       ");
            sql.AppendLine("    END IF;                                                                     ");
            sql.AppendLine("                                                                                ");
            sql.AppendLine("    RETURN value_text;                                                          ");
            sql.AppendLine("END;                                                                            ");
            sql.AppendLine("$BODY$                                                                          ");
            sql.AppendLine("  LANGUAGE plpgsql VOLATILE                                                     ");
            sql.AppendLine("  COST 100;                                                                     ");
            sql.AppendLine("                                                                                ");
            #endregion

            #region 3. Ограничиетель. Название отчёта должно быть уникальным.
            sql.AppendLine("DO                                                                              ");
            sql.AppendLine("$$                                                                              ");
            sql.AppendLine("BEGIN                                                                           ");
            sql.AppendLine("    IF NOT EXISTS (SELECT 1 FROM information_schema.table_constraints WHERE constraint_name = 'report_unique_title' LIMIT 1)    ");
            sql.AppendLine("    THEN                                                                        ");
            sql.AppendLine("        ALTER TABLE report.list ADD CONSTRAINT report_unique_title UNIQUE (title);    ");
            sql.AppendLine("    END IF;                                                                     ");
            sql.AppendLine("END;                                                                            ");
            sql.AppendLine("$$                                                                              ");
            sql.AppendLine("LANGUAGE PlPgSql;                                                               ");
            #endregion

            Database.ExecuteNonQuery(sql.ToString());
            #endregion
        }

    }
}
