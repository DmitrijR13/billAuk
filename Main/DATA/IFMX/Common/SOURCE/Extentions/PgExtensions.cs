namespace STCLINE.KP50.DataBase
{
    using System;
    using System.Data;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;

    using Npgsql;

    public static class PgExtensions
    {
        public static string PgNormalize(this string sql, IDbConnection connection = null)
        {
            // sql = sql.Replace(":", ".");
            sql = Regex.Replace(sql, @"select\s*(distinct|into)* ,", "select $1 ", RegexOptions.IgnoreCase);
            sql = Regex.Replace(sql, @"\(\s*,", " (");
            sql = Regex.Replace(sql, @",{2,}", ",");
            sql = Regex.Replace(sql, "\"are\".", " ");
            sql = Regex.Replace(sql, @"\s+are\.", " ");
            //sql = Regex.Replace(sql, @"@\w+", string.Empty);
            sql = Regex.Replace(sql, @"\s*drop table ((\s*if exists\s*){1,})*", " drop table if exists ", RegexOptions.IgnoreCase);
            sql = Regex.Replace(sql, @"\s*drop index ((\s*if exists\s*){1,})*", " drop index if exists ", RegexOptions.IgnoreCase);
            sql = Regex.Replace(sql, @"\s*drop sequence ((\s*if exists\s*){1,})*", " drop sequence if exists ", RegexOptions.IgnoreCase);
            sql = Regex.Replace(sql, @"\.{2,}", ".");
            sql = Regex.Replace(sql, @"\s*update statistics for table\s*", " analyze ", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (connection != null)
            {
                // Нужно больше исследования
                //CheckDatabaseInSchema(sql, connection);
            }

            return sql;
        }

        /// <summary>
        /// Предназначена для установки пути поиска таблицы в схеме public
        /// </summary>
        /// <param name="sql">Готовый к выполнению sql</param>
        public static void CheckDatabaseInSchema(string sql, IDbConnection connection)
        {
            const string tablePattern = @"(\w+\s*\.*\s*\w+\s*)";

            /*
             * Найдет [<schema>.]<table> в select|insert|create
             */
            var searchTablePatternFormat = string.Format(@"\s+from\s+{0}|.*insert\s+into\s{0}|.*create.*table\s+{0}", tablePattern);

            var matches = Regex.Matches(sql, searchTablePatternFormat, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

            if (matches.Count > 0)
            {
                var table = matches[0].Groups[1].Value;

                if (!table.Contains("."))
                {
                    var set = "set search_path to public";
                    using (var com = new NpgsqlCommand(set, (NpgsqlConnection)connection))
                    {
                        com.ExecuteNonQuery();
                    }
                }
            }
        }

        #region Type extensions

        public static string CastTo(this string field, string type, string precision = null)
        {
            return string.Format("CAST({0} as {1})", field, !string.IsNullOrEmpty(precision) ? string.Format("{0}({1})", type, precision) : type);
        }

        #endregion Type extensions

        #region Sql extensions

        public static string AddIntoStatement(this string sqlStatetment, string intoStatetment)
        {
            var regex = new Regex(" from", RegexOptions.IgnoreCase);
            return regex.Replace(sqlStatetment, intoStatetment + " from ", 1);
        }

        public static string UpdateSet(this string sql, string left, string right, string alias = null)
        {
            var lefts = left.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);

            var rights = right.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);

            if (lefts.Count() != rights.Count())
            {
                throw new ArgumentException("Число параметров в правой и левой части различно!");
            }

            var sb = new StringBuilder();
            sb.Append(" set ");
            for (int i = 0; i < lefts.Count(); i++)
            {
                sb.AppendFormat("{0} = {1}.{2},", lefts[i], !string.IsNullOrEmpty(alias) ? alias.Trim() : string.Empty, rights[i].Trim());
            }

            return string.Concat(sql, sb.ToString().TrimEnd(new[] { ',' }));
        }

        #endregion Sql extensions

        #region Date extensions

        public static string DatePart(this string date, string part)
        {
            return string.Format("date_part('{0}', {1})", part, date);
        }

        public static string Month(this string date)
        {
            return date.DatePart("month");
        }

        public static string Year(this string date)
        {
            return date.DatePart("year");
        }

        public static string Interval(this string interval, string units)
        {
            return string.Format("INTERVAL '{0} {1}'", interval, units);
        }

        public static string Interval(this int interval, string units)
        {
            return Interval(interval.ToString(), units);
        }

        public static string IntervalMinutes(this string interval)
        {
            return Interval(interval, "minutes");
        }

        public static string IntervalHours(this string interval)
        {
            return Interval(interval, "hours");
        }

        public static string IntervalMinutes(this int interval)
        {
            return IntervalMinutes(interval.ToString());
        }

        public static string IntervalHours(this int interval)
        {
            return IntervalHours(interval.ToString());
        }

        #endregion Date extensions
    }
}