using STCLINE.KP50.Global;
using STCLINE.KP50.Interfaces;
using STCLINE.KP50.Utility;
using System;
using System.Collections.Generic;
using System.Data;
namespace STCLINE.KP50.DataBase
{
    public class DbBase : DataBaseHead
    {
        public DbBase()
            : base()
        {

        }

        public List<T> ExecRead<T>(IDbConnection connectionID, string _sql, Converter<IDataRecord, T> converter)
        {

            IDataReader reader;
            Returns ret = base.ExecRead(connectionID, out reader, _sql, true);
            if (ret.result)
            {
                try
                {
                    List<T> list = OrmConvert.ConvertDataReader(reader, converter);
                    return list;
                }
                finally
                {
                    reader.Close();
                }
            }
            else
            {
                throw new Exception(ret.text);
            }
        }
        public DataTable OpenSql(IDbConnection connectionID, string _sql)
        {
            IntfResultTableType res = ClassDBUtils.OpenSQL(_sql, connectionID);
            res.GetReturnsType().ThrowExceptionIfError();
            return res.resultData;
        }

    }
}