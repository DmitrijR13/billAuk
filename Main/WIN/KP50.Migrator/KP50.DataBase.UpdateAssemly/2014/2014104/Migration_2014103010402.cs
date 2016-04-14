using KP50.DataBase.Migrator.Framework;
using MigrateDataBase = KP50.DataBase.Migrator.Framework.DataBase;
using System.Text;

namespace KP50.DataBase.UpdateAssembly
{
    [Migration(2014103010402, MigrateDataBase.CentralBank)]
    public class Migration_2014103010402_CentralBank : Migration
    {
        public override void Apply()
        {
            if (Database.ProviderName == "PostgreSQL")
            {
                var sql = new StringBuilder();
                sql.Append("DO $$DECLARE _SQL varchar(8000);\r\n");
                sql.Append(" \r\n");
                sql.Append("       BEGIN\r\n");
                sql.Append("        IF NOT EXISTS(SELECT 1\r\n");
                sql.Append("            FROM pg_catalog.pg_proc p  \r\n");
                sql.Append("                INNER JOIN   pg_catalog.pg_namespace n\r\n");
                sql.Append("                ON p.pronamespace = n.oid and n.nspname = 'public' \r\n");
                sql.Append("            WHERE p.proname =  'fn_contract_tula') THEN\r\n");
                sql.Append("\r\n");
                sql.Append(
                    "          _SQL = 'CREATE FUNCTION public.fn_contract_tula(IN _nzp_kvar character varying)\r\n");
                sql.Append(
                    "              RETURNS TABLE(kotlovoj_schet character varying, spec_schet character varying, contract_content text, contract_footer text) AS\r\n");
                sql.Append("          $BODY$\r\n");
                sql.Append("          begin\r\n");
                sql.Append("\r\n");
                sql.Append("            drop table if exists temp_table_contract_tula;\r\n");
                sql.Append("            create temp table temp_table_contract_tula \r\n");
                sql.Append("            (\r\n");
                sql.Append("              kotlovoj_schet          varchar(100),\r\n");
                sql.Append("              spec_schet              varchar(100),\r\n");
                sql.Append("              contract_content        text,  \r\n");
                sql.Append("              contract_footer     text\r\n");
                sql.Append("            );\r\n");
                sql.Append("\r\n");
                sql.Append("            ------------------------------------------------------------------    \r\n");
                sql.Append("            return query  \r\n");
                sql.Append(
                    "              select a.kotlovoj_schet, a.spec_schet, a.contract_content, a.contract_footer\r\n");
                sql.Append("                from temp_table_contract_tula a\r\n");
                sql.Append("                limit 1;\r\n");
                sql.Append("\r\n");
                sql.Append("            ------------------------------------------------------------------\r\n");
                sql.Append("            drop table temp_table_contract_tula;\r\n");
                sql.Append("          end;\r\n");
                sql.Append("          $BODY$\r\n");
                sql.Append("              LANGUAGE plpgsql VOLATILE\r\n");
                sql.Append("              COST 100\r\n");
                sql.Append("              ROWS 1000;';\r\n");
                sql.Append("          EXECUTE _SQL;\r\n");
                sql.Append("        END IF;\r\n");
                sql.Append("        END$$   \r\n");
                sql.Append("        LANGUAGE 'plpgsql';");
                Database.ExecuteScalar(sql.ToString());




                #region Сформировать sql запрос

                sql.Remove(0, sql.Length);
                sql.AppendLine(""); // Пустая строка в начале. Чтобы красивее было в info.log

                #region 01. Создать схему базы данных и таблицы

                sql.AppendLine("-- =============================================================================");
                sql.AppendLine("-- Создать схему базы данных и таблицы                                          ");
                sql.AppendLine("--                                                                              ");
                sql.AppendLine("-- Виталя Любич, VetalAerix@gmail.com                                           ");
                sql.AppendLine("-- 29.10.2014                                                                   ");
                sql.AppendLine("-- =============================================================================");
                sql.AppendLine("DO                                                                              ");
                sql.AppendLine("$$                                                                              ");
                sql.AppendLine("BEGIN                                                                           ");
                sql.AppendLine("                                                                                ");
                sql.AppendLine("    ------------------------------------------------------------------          ");
                sql.AppendLine("    -- Схема                                                                    ");
                sql.AppendLine("    ------------------------------------------------------------------          ");
                sql.AppendLine("                                                                                ");
                sql.AppendLine("    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'report')          ");
                sql.AppendLine("    THEN                                                                        ");
                sql.AppendLine("        CREATE SCHEMA report;                                                   ");
                sql.AppendLine("        COMMENT ON SCHEMA report IS 'Подсистема гибких отчётов';                ");
                sql.AppendLine("    END IF;                                                                     ");
                sql.AppendLine("                                                                                ");
                sql.AppendLine("    ------------------------------------------------------------------          ");
                sql.AppendLine("    -- Таблицы                                                                  ");
                sql.AppendLine("    ------------------------------------------------------------------          ");
                sql.AppendLine("                                                                                ");
                sql.AppendLine("    -- Список отчётов.                                                          ");
                sql.AppendLine(
                    "    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_class AS c LEFT JOIN pg_catalog.pg_namespace AS n ON n.oid = c.relnamespace WHERE n.nspname = 'report' AND c.relname = 'list' AND c.relkind = 'r')    ");
                sql.AppendLine("    THEN                                                                        ");
                sql.AppendLine("        CREATE TABLE report.list                                                ");
                sql.AppendLine("        (                                                                       ");
                sql.AppendLine("            id             integer NOT NULL,                                    ");
                sql.AppendLine("            title          character varying(256),                              ");
                sql.AppendLine("            description    text,                                                ");
                sql.AppendLine("            category       character varying(128),                              ");
                sql.AppendLine("            preview        boolean,                                             ");
                sql.AppendLine("            frx            character varying(256),                              ");
                sql.AppendLine("            CONSTRAINT list_pk PRIMARY KEY (id)                                 ");
                sql.AppendLine("        );                                                                      ");
                sql.AppendLine("        COMMENT ON TABLE report.list IS 'Список отчётов';                       ");
                sql.AppendLine("        COMMENT ON COLUMN report.list.id          IS 'Уникальный код';          ");
                sql.AppendLine(
                    "        COMMENT ON COLUMN report.list.title       IS 'Заголовок. Отображается в списке';    ");
                sql.AppendLine("        COMMENT ON COLUMN report.list.description IS 'Описание';                ");
                sql.AppendLine(
                    "        COMMENT ON COLUMN report.list.category    IS 'Категория (Начисления / Финансы)';    ");
                sql.AppendLine(
                    "        COMMENT ON COLUMN report.list.preview     IS 'Выводить на предпросмотр или на экспорт?';    ");
                sql.AppendLine(
                    "        COMMENT ON COLUMN report.list.frx         IS 'Наименование файла шаблона. Включая расширение (frx). Путь относительно HOST.KP50.exe';    ");
                sql.AppendLine("    END IF;                                                                     ");
                sql.AppendLine("                                                                                ");
                sql.AppendLine("    -- Параметры (условия).                                                     ");
                sql.AppendLine(
                    "    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_class AS c LEFT JOIN pg_catalog.pg_namespace AS n ON n.oid = c.relnamespace WHERE n.nspname = 'report' AND c.relname = 'parameter' AND c.relkind = 'r')    ");
                sql.AppendLine("    THEN                                                                        ");
                sql.AppendLine("        CREATE TABLE report.parameter                                           ");
                sql.AppendLine("        (                                                                       ");
                sql.AppendLine("            report        integer NOT NULL,                                     ");
                sql.AppendLine("            turn          integer,                                              ");
                sql.AppendLine("            code          character varying(128) NOT NULL,                      ");
                sql.AppendLine("            title         character varying(256),                               ");
                sql.AppendLine("            kind          character varying(256),                               ");
                sql.AppendLine("            initial       text,                                                 ");
                sql.AppendLine("            properties    text,                                                 ");
                sql.AppendLine("            CONSTRAINT parameter_pk PRIMARY KEY (report, code),                 ");
                sql.AppendLine("            CONSTRAINT parameter_report_list_id FOREIGN KEY (report)            ");
                sql.AppendLine("                REFERENCES report.list (id) MATCH SIMPLE                        ");
                sql.AppendLine("                ON UPDATE CASCADE                                               ");
                sql.AppendLine("                ON DELETE CASCADE                                               ");
                sql.AppendLine("        );                                                                      ");
                sql.AppendLine("        COMMENT ON TABLE report.parameter IS 'Параметры (условия)';             ");
                sql.AppendLine(
                    "        COMMENT ON COLUMN report.parameter.report     IS 'Код отчёта. Внешний ключ на поле id таблицы report.list';    ");
                sql.AppendLine("        COMMENT ON COLUMN report.parameter.turn       IS 'Номер по порядку';    ");
                sql.AppendLine(
                    "        COMMENT ON COLUMN report.parameter.code       IS 'Идентификатор метки при подстановке в параметризованную инструкцию из поля sql, таблицы report.data';    ");
                sql.AppendLine(
                    "        COMMENT ON COLUMN report.parameter.title      IS 'Подпись, которая отображается рядом с параметром';    ");
                sql.AppendLine(
                    "        COMMENT ON COLUMN report.parameter.kind       IS 'Тип. Соответствует C# классу из пространства имён Bars.KP50.Report';    ");
                sql.AppendLine(
                    "        COMMENT ON COLUMN report.parameter.initial    IS 'Параметризованная sql инструкция, возвращающая значение по умолчанию';    ");
                sql.AppendLine("        COMMENT ON COLUMN report.parameter.properties IS 'Расширенные свойства';    ");
                sql.AppendLine("    END IF;                                                                     ");
                sql.AppendLine("                                                                                ");
                sql.AppendLine("    -- Вызовы sql функций по получению наборов данных                           ");
                sql.AppendLine(
                    "    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_class AS c LEFT JOIN pg_catalog.pg_namespace AS n ON n.oid = c.relnamespace WHERE n.nspname = 'report' AND c.relname = 'data' AND c.relkind = 'r')    ");
                sql.AppendLine("    THEN                                                                        ");
                sql.AppendLine("        CREATE TABLE report.data                                                ");
                sql.AppendLine("        (                                                                       ");
                sql.AppendLine("            report    integer NOT NULL,                                         ");
                sql.AppendLine("            turn      integer,                                                  ");
                sql.AppendLine("            sql       text,                                                     ");
                sql.AppendLine("            title     character varying(128) NOT NULL,                          ");
                sql.AppendLine("            CONSTRAINT data_pk PRIMARY KEY (report, title),                     ");
                sql.AppendLine("            CONSTRAINT data_report_list_id FOREIGN KEY (report)                 ");
                sql.AppendLine("                REFERENCES report.list (id) MATCH SIMPLE                        ");
                sql.AppendLine("                ON UPDATE CASCADE                                               ");
                sql.AppendLine("                ON DELETE CASCADE                                               ");
                sql.AppendLine("        );                                                                      ");
                sql.AppendLine("        COMMENT ON TABLE report.data IS 'Наборы данных';                        ");
                sql.AppendLine(
                    "        COMMENT ON COLUMN report.data.report IS 'Код отчёта. Внешний ключ на поле id таблицы report.list';    ");
                sql.AppendLine(
                    "        COMMENT ON COLUMN report.data.turn   IS 'Порядок исполнения иструкций, если он имеет значение';    ");
                sql.AppendLine(
                    "        COMMENT ON COLUMN report.data.sql    IS 'Параметризованная sql инструкция. Обычно вызов функции, например select * from report.r01(@Report, @User, @Point, @Contractor). @Report, @User, @Point - системные метки. @Contractor идентификатор условия (report.parameter.title)';    ");
                sql.AppendLine(
                    "        COMMENT ON COLUMN report.data.title  IS 'Наименование набора данных. Для шаблона. Например, Q_master';    ");
                sql.AppendLine("    END IF;                                                                     ");
                sql.AppendLine("                                                                                ");
                sql.AppendLine("    -- Версия обновления.                                                       ");
                sql.AppendLine(
                    "    IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_class AS c LEFT JOIN pg_catalog.pg_namespace AS n ON n.oid = c.relnamespace WHERE n.nspname = 'report' AND c.relname = 'version' AND c.relkind = 'r')    ");
                sql.AppendLine("    THEN                                                                        ");
                sql.AppendLine("        CREATE TABLE report.version                                             ");
                sql.AppendLine("        (                                                                       ");
                sql.AppendLine("            dt             date NOT NULL,                                       ");
                sql.AppendLine("            description    character varying,                                   ");
                sql.AppendLine("            CONSTRAINT version_pk PRIMARY KEY (dt)                              ");
                sql.AppendLine("        );                                                                      ");
                sql.AppendLine("        COMMENT ON TABLE report.version IS 'Версии обновлений';                 ");
                sql.AppendLine("        COMMENT ON COLUMN report.version.dt          IS 'Дата';                 ");
                sql.AppendLine("        COMMENT ON COLUMN report.version.description IS 'Комментарий';          ");
                sql.AppendLine("    END IF;                                                                     ");
                sql.AppendLine("END;                                                                            ");
                sql.AppendLine("$$ LANGUAGE PlPgSql;                                                            ");

                #endregion

                #region 02. report.get_list (2)

                sql.AppendLine("DROP FUNCTION IF EXISTS report.get_list(character varying, integer);            ");
                sql.AppendLine("                                                                                ");
                sql.AppendLine(
                    "CREATE FUNCTION report.get_list(IN category_name character varying, IN user_id integer)    ");
                sql.AppendLine("  RETURNS TABLE(id integer, title character varying, description text) AS       ");
                sql.AppendLine("$BODY$                                                                          ");
                sql.AppendLine("-- =============================================================================");
                sql.AppendLine("-- Получить список отчетов заданной категории                                   ");
                sql.AppendLine("--                                                                              ");
                sql.AppendLine("-- -------------------------------------------------------------------          ");
                sql.AppendLine("-- Параметры                                                                    ");
                sql.AppendLine("-- -------------------------------------------------------------------          ");
                sql.AppendLine("-- category_name    - Категория (группа): \"Начисления\" или \"Финансы\"        ");
                sql.AppendLine("-- user_id          - Идентификатор пользователя                                ");
                sql.AppendLine("--                                                                              ");
                sql.AppendLine("-- -------------------------------------------------------------------          ");
                sql.AppendLine("-- Результат                                                                    ");
                sql.AppendLine("-- -------------------------------------------------------------------          ");
                sql.AppendLine("-- Таблица:                                                                     ");
                sql.AppendLine("--     id             - Уникальный идентификатор                                ");
                sql.AppendLine("--     title          - Наименование                                            ");
                sql.AppendLine("--     description    - Описание                                                ");
                sql.AppendLine("--                                                                              ");
                sql.AppendLine("-- -------------------------------------------------------------------          ");
                sql.AppendLine("-- Виталя Любич, VetalAerix@gmail.com                                           ");
                sql.AppendLine("-- 29.09.2014                                                                   ");
                sql.AppendLine("-- =============================================================================");
                sql.AppendLine("BEGIN                                                                           ");
                sql.AppendLine("    RETURN QUERY                                                                ");
                sql.AppendLine("    SELECT t.id,                                                                ");
                sql.AppendLine("           t.title,                                                             ");
                sql.AppendLine("           t.description                                                        ");
                sql.AppendLine("      FROM report.list t                                                        ");
                sql.AppendLine("     WHERE t.category = category_name                                           ");
                sql.AppendLine("     ORDER BY t.title;                                                          ");
                sql.AppendLine("END;                                                                            ");
                sql.AppendLine("$BODY$                                                                          ");
                sql.AppendLine("  LANGUAGE plpgsql VOLATILE                                                     ");
                sql.AppendLine("  COST 100                                                                      ");
                sql.AppendLine("  ROWS 1000;                                                                    ");

                #endregion

                #region 03. report.get_list

                sql.AppendLine("DROP FUNCTION IF EXISTS report.get_list(integer);                               ");
                sql.AppendLine("                                                                                ");
                sql.AppendLine("CREATE FUNCTION report.get_list(IN user_id integer)                             ");
                sql.AppendLine(
                    "  RETURNS TABLE(id integer, title character varying, description text, category character varying) AS    ");
                sql.AppendLine("$BODY$                                                                          ");
                sql.AppendLine("-- =============================================================================");
                sql.AppendLine("-- Получить список отчетов                                                      ");
                sql.AppendLine("--                                                                              ");
                sql.AppendLine("-- -------------------------------------------------------------------          ");
                sql.AppendLine("-- Параметры                                                                    ");
                sql.AppendLine("-- -------------------------------------------------------------------          ");
                sql.AppendLine("-- user_id   - Идентификатор пользователя                                       ");
                sql.AppendLine("--                                                                              ");
                sql.AppendLine("-- -------------------------------------------------------------------          ");
                sql.AppendLine("-- Результат                                                                    ");
                sql.AppendLine("-- -------------------------------------------------------------------          ");
                sql.AppendLine("-- Таблица:                                                                     ");
                sql.AppendLine("--     id             - Уникальный идентификатор                                ");
                sql.AppendLine("--     title          - Наименование                                            ");
                sql.AppendLine("--     description    - Описание                                                ");
                sql.AppendLine("--     category       - Категория (группа)                                      ");
                sql.AppendLine("--                                                                              ");
                sql.AppendLine("-- -------------------------------------------------------------------          ");
                sql.AppendLine("-- Виталя Любич, VetalAerix@gmail.com                                           ");
                sql.AppendLine("-- 27.09.2014                                                                   ");
                sql.AppendLine("-- =============================================================================");
                sql.AppendLine("BEGIN                                                                           ");
                sql.AppendLine("    RETURN QUERY                                                                ");
                sql.AppendLine("    SELECT t.id,                                                                ");
                sql.AppendLine("           t.title,                                                             ");
                sql.AppendLine("           t.description,                                                       ");
                sql.AppendLine("           t.category                                                           ");
                sql.AppendLine("      FROM report.list t                                                        ");
                sql.AppendLine("     ORDER BY t.title;                                                          ");
                sql.AppendLine("END;                                                                            ");
                sql.AppendLine("$BODY$                                                                          ");
                sql.AppendLine("  LANGUAGE plpgsql VOLATILE                                                     ");
                sql.AppendLine("  COST 100                                                                      ");
                sql.AppendLine("  ROWS 1000;                                                                    ");

                #endregion

                #region 04. report.get_default_value

                sql.AppendLine(
                    "DROP FUNCTION IF EXISTS report.get_default_value(character varying, integer, integer);    ");
                sql.AppendLine("                                                                                ");
                sql.AppendLine(
                    "CREATE FUNCTION report.get_default_value(initial_sql character varying, report_id integer, user_id integer)    ");
                sql.AppendLine("  RETURNS character varying AS                                                  ");
                sql.AppendLine("$BODY$                                                                          ");
                sql.AppendLine("-- =============================================================================");
                sql.AppendLine("-- Получить значение по умолчанию для параметра                                 ");
                sql.AppendLine("-- Выполнить функцию initial_sql                                                ");
                sql.AppendLine("--                                                                              ");
                sql.AppendLine("-- Нужно, чтобы выполнить динамический SQL в select-e функции report.parameter  ");
                sql.AppendLine("--                                                                              ");
                sql.AppendLine("-- -------------------------------------------------------------------          ");
                sql.AppendLine("-- initial_sql    - Название функции                                            ");
                sql.AppendLine("-- report_id      - Идентификатор отчёта                                        ");
                sql.AppendLine("-- user_id        - Идентификатор пользователя                                  ");
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

                #endregion

                #region 05. report.get_data

                sql.AppendLine("DROP FUNCTION IF EXISTS report.get_data(integer, integer);                      ");
                sql.AppendLine("                                                                                ");
                sql.AppendLine("CREATE FUNCTION report.get_data(IN report_id integer, IN user_id integer)       ");
                sql.AppendLine("  RETURNS TABLE(turn integer, sql text, title character varying) AS             ");
                sql.AppendLine("$BODY$                                                                          ");
                sql.AppendLine("-- =============================================================================");
                sql.AppendLine("-- Получить список параметризованных sql инструкций                             ");
                sql.AppendLine("--                                                                              ");
                sql.AppendLine("-- -------------------------------------------------------------------          ");
                sql.AppendLine("-- report_id     - Идентификатор отчета                                         ");
                sql.AppendLine("-- user_id       - Идентификатор пользователя                                   ");
                sql.AppendLine("--                                                                              ");
                sql.AppendLine("-- -------------------------------------------------------------------          ");
                sql.AppendLine("-- Результат                                                                    ");
                sql.AppendLine("-- -------------------------------------------------------------------          ");
                sql.AppendLine("-- Таблица:                                                                     ");
                sql.AppendLine("--     turn    - Порядок выполнения                                             ");
                sql.AppendLine("--     sql     - Параметризированная sql инструкция                             ");
                sql.AppendLine("--     title   - Наименование источника данных, для шаблона                     ");
                sql.AppendLine("--                                                                              ");
                sql.AppendLine("-- -------------------------------------------------------------------          ");
                sql.AppendLine("-- Виталя Любич, VetalAerix@gmail.com                                           ");
                sql.AppendLine("-- 02.10.2014                                                                   ");
                sql.AppendLine("-- =============================================================================");
                sql.AppendLine("BEGIN                                                                           ");
                sql.AppendLine("    RETURN QUERY                                                                ");
                sql.AppendLine("    SELECT t.turn,                                                              ");
                sql.AppendLine("           t.sql,                                                               ");
                sql.AppendLine("           t.title                                                              ");
                sql.AppendLine("      FROM report.data t                                                        ");
                sql.AppendLine("     WHERE t.report = report_id                                                 ");
                sql.AppendLine("     ORDER BY t.turn;                                                           ");
                sql.AppendLine("END;                                                                            ");
                sql.AppendLine("$BODY$                                                                          ");
                sql.AppendLine("  LANGUAGE plpgsql VOLATILE                                                     ");
                sql.AppendLine("  COST 100                                                                      ");
                sql.AppendLine("  ROWS 10;                                                                      ");

                #endregion

                #region 06. report.get_parameters

                sql.AppendLine("DROP FUNCTION IF EXISTS report.get_parameters(integer, integer);                ");
                sql.AppendLine("                                                                                ");
                sql.AppendLine("CREATE FUNCTION report.get_parameters(IN report_id integer, IN user_id integer) ");
                sql.AppendLine("  RETURNS TABLE(kind character varying, json text) AS                           ");
                sql.AppendLine("$BODY$                                                                          ");
                sql.AppendLine("-- =============================================================================");
                sql.AppendLine("-- Получить параметры (условия) отчёта                                          ");
                sql.AppendLine("--                                                                              ");
                sql.AppendLine("-- -------------------------------------------------------------------          ");
                sql.AppendLine("-- Параметры                                                                    ");
                sql.AppendLine("-- -------------------------------------------------------------------          ");
                sql.AppendLine("-- report_id    - Идентификатор отчета                                          ");
                sql.AppendLine("-- user_id      - Идентификатор пользователя                                    ");
                sql.AppendLine("--                                                                              ");
                sql.AppendLine("-- -------------------------------------------------------------------          ");
                sql.AppendLine("-- Результат                                                                    ");
                sql.AppendLine("-- -------------------------------------------------------------------          ");
                sql.AppendLine("-- Таблица:                                                                     ");
                sql.AppendLine("--     kind    - Класс параметра. Соответствует классу из сборки Bars.KP50.Report    ");
                sql.AppendLine("--     json    - Свойства в формате json                                        ");
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
                sql.AppendLine("                     report.get_default_value(t.initial, report_id, user_id),   ");
                sql.AppendLine(
                    "                     CASE WHEN t.properties IS not null AND t.properties <> '' THEN ', ' || t.properties ELSE '' END    ");
                sql.AppendLine("                 )                                                              ");
                sql.AppendLine("    FROM report.parameter t                                                     ");
                sql.AppendLine("    WHERE t.report = report_id                                                  ");
                sql.AppendLine("    ORDER BY t.turn;                                                            ");
                sql.AppendLine("                                                                                ");
                sql.AppendLine(
                    "    -- Если для отчёта отключен предпросмотр, то добавить параметр \"Формат печати\".    ");
                sql.AppendLine("    IF (SELECT preview FROM report.list WHERE id = report_id) = FALSE           ");
                sql.AppendLine("    THEN                                                                        ");
                sql.AppendLine("        RETURN QUERY                                                            ");
                sql.AppendLine(
                    "        SELECT 'ExportFormatParameter'::varchar, '{Code: \"ExportFormat\", Name: \"Формат печати\"}'::text;    ");
                sql.AppendLine("        -- Если нужно задать, чтобы по умолчанию был формат pdf, то добавить:   ");
                sql.AppendLine("        -- Value: \"Pdf\"                                                       ");
                sql.AppendLine("    END IF;                                                                     ");
                sql.AppendLine("END;                                                                            ");
                sql.AppendLine("$BODY$                                                                          ");
                sql.AppendLine("  LANGUAGE plpgsql VOLATILE                                                     ");
                sql.AppendLine("  COST 100                                                                      ");
                sql.AppendLine("  ROWS 10;                                                                      ");

                #endregion

                #region 07. report.get_frx

                sql.AppendLine("DROP FUNCTION IF EXISTS report.get_frx(integer, integer);                       ");
                sql.AppendLine("                                                                                ");
                sql.AppendLine("CREATE FUNCTION report.get_frx(report_id integer, user_id integer)              ");
                sql.AppendLine("  RETURNS character varying AS                                                  ");
                sql.AppendLine("$BODY$                                                                          ");
                sql.AppendLine("-- =============================================================================");
                sql.AppendLine("-- Получить путь к шаблону отчета. Относительно Bars.KP50.Houst.exe             ");
                sql.AppendLine("--                                                                              ");
                sql.AppendLine("-- -------------------------------------------------------------------          ");
                sql.AppendLine("-- report_id     - Идентификатор отчета                                         ");
                sql.AppendLine("-- user_id       - Идентификатор пользователя                                   ");
                sql.AppendLine("--                                                                              ");
                sql.AppendLine("-- -------------------------------------------------------------------          ");
                sql.AppendLine("-- Виталя Любич, VetalAerix@gmail.com                                           ");
                sql.AppendLine("-- 27.09.2014                                                                   ");
                sql.AppendLine("-- =============================================================================");
                sql.AppendLine("BEGIN                                                                           ");
                sql.AppendLine("    RETURN t.frx                                                                ");
                sql.AppendLine("      FROM report.list t                                                        ");
                sql.AppendLine("     WHERE t.id = report_id;                                                    ");
                sql.AppendLine("END;                                                                            ");
                sql.AppendLine("$BODY$                                                                          ");
                sql.AppendLine("  LANGUAGE plpgsql VOLATILE                                                     ");
                sql.AppendLine("  COST 100;                                                                     ");

                #endregion

                #region 08. report.get_info

                sql.AppendLine("DROP FUNCTION IF EXISTS report.get_info(integer, integer);                      ");
                sql.AppendLine("                                                                                ");
                sql.AppendLine("CREATE FUNCTION report.get_info(IN report_id integer, IN user_id integer)       ");
                sql.AppendLine("  RETURNS TABLE(title character varying, description text, preview boolean) AS  ");
                sql.AppendLine("$BODY$                                                                          ");
                sql.AppendLine("-- =============================================================================");
                sql.AppendLine("-- Получить информацию об отчёте                                                ");
                sql.AppendLine("--                                                                              ");
                sql.AppendLine("-- -------------------------------------------------------------------          ");
                sql.AppendLine("-- Параметры                                                                    ");
                sql.AppendLine("-- -------------------------------------------------------------------          ");
                sql.AppendLine("-- report_id    - Идентификатор отчета. Из таблицы report.list                  ");
                sql.AppendLine("-- user_id      - Идентификатор пользователя                                    ");
                sql.AppendLine("--                                                                              ");
                sql.AppendLine("-- -------------------------------------------------------------------          ");
                sql.AppendLine("-- Результат                                                                    ");
                sql.AppendLine("-- -------------------------------------------------------------------          ");
                sql.AppendLine("-- таблица с одной записью:                                                     ");
                sql.AppendLine("--     title        - Наименование отчета                                       ");
                sql.AppendLine("--     description  - Описание                                                  ");
                sql.AppendLine("--     preview      - Выводить на предпросмотр или экспорт                      ");
                sql.AppendLine("--                                                                              ");
                sql.AppendLine("-- -------------------------------------------------------------------          ");
                sql.AppendLine("-- Виталя Любич, VetalAerix@gmail.com                                           ");
                sql.AppendLine("-- 27.09.2014                                                                   ");
                sql.AppendLine("-- =============================================================================");
                sql.AppendLine("BEGIN                                                                           ");
                sql.AppendLine("    RETURN QUERY                                                                ");
                sql.AppendLine("    SELECT t.title,                                                             ");
                sql.AppendLine("           t.description,                                                       ");
                sql.AppendLine("           t.preview                                                            ");
                sql.AppendLine("      FROM report.list t                                                        ");
                sql.AppendLine("     WHERE t.id = report_id;                                                    ");
                sql.AppendLine("END;                                                                            ");
                sql.AppendLine("$BODY$                                                                          ");
                sql.AppendLine("  LANGUAGE plpgsql VOLATILE                                                     ");
                sql.AppendLine("  COST 100                                                                      ");
                sql.AppendLine("  ROWS 1;                                                                       ");

                #endregion

                #endregion

                Database.ExecuteNonQuery(sql.ToString());



            }

        }
    }
}
