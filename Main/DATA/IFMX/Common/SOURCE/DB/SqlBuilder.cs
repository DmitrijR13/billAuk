namespace STCLINE.KP50.DataBase
{
    using System.Text;

    public class SqlBuilder
    {
        private StringBuilder m_builder;

        public SqlBuilder()
        {
            m_builder = new StringBuilder();
        }

        public SqlBuilder Append(string append)
        {
            m_builder.Append(append);

            return this;
        }

        public SqlBuilder Select(params string[] fields)
        {
            var sb = new StringBuilder(" select ");
            for (var i = 0; i < fields.Length; ++i)
            {
                sb.Append("{" + i + "},");
            }

            m_builder.AppendFormat(sb.ToString().TrimEnd(','), fields);
            m_builder.AppendLine();

            return this;
        }

        public SqlBuilder From(params string[] tables)
        {
            var sb = new StringBuilder(" from ");
            for (var i = 0; i < tables.Length; ++i)
            {
                sb.Append("{" + i + "},");
            }

            m_builder.AppendFormat(sb.ToString().TrimEnd(','), tables);
            m_builder.AppendLine();

            return this;
        }

        public SqlBuilder Join(string table)
        {
            m_builder.AppendFormat(" left outer join {0} ", table);

            return this;
        }

        public SqlBuilder On(string predicate)
        {
            m_builder.AppendFormat(" on {0}", predicate);

            return this;
        }

        public SqlBuilder Where(string predicate)
        {
            m_builder.AppendFormat(" where {0}", predicate);

            return this;
        }

        public SqlBuilder And(string predicate)
        {
            m_builder.AppendFormat(" and {0}", predicate);

            return this;
        }

        public SqlBuilder Or(string predicate)
        {
            m_builder.AppendFormat(" or {0}", predicate);

            return this;
        }

        public override string ToString()
        {
            return m_builder.ToString();
        }

        public StringBuilder AsStringBuilder()
        {
            return m_builder;
        }
    }
}